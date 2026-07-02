using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using FlyPPTTimer.Models;

namespace FlyPPTTimer.Services;

public sealed class RemoteControlService : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly Func<AppConfig> _getConfig;
    private readonly Action<AppConfig> _saveConfig;
    private readonly AppCommandService _commands;
    private readonly LogService _log;
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private readonly Dictionary<string, DateTime> _clients = [];

    public RemoteControlService(Func<AppConfig> getConfig, Action<AppConfig> saveConfig, AppCommandService commands, LogService log)
    {
        _getConfig = getConfig;
        _saveConfig = saveConfig;
        _commands = commands;
        _log = log;
    }

    public bool IsRunning { get; private set; }
    public string StatusText { get; private set; } = "未启动";
    public int CurrentPort { get; private set; }
    public int ConnectedClients
    {
        get
        {
            lock (_clients)
            {
                PruneClients();
                return _clients.Count;
            }
        }
    }

    public void Start()
    {
        if (IsRunning) return;
        var config = _getConfig();
        if (!config.RemoteControl.Enabled)
        {
            StatusText = "未启动";
            return;
        }

        try
        {
            var port = config.RemoteControl.UseRandomPort || config.RemoteControl.Port <= 0 ? 0 : config.RemoteControl.Port;
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            CurrentPort = ((IPEndPoint)_listener.LocalEndpoint).Port;
            config.RemoteControl.Port = CurrentPort;
            _saveConfig(config);
            _cts = new CancellationTokenSource();
            IsRunning = true;
            StatusText = "已启动";
            _log.Info($"Remote control service started on 0.0.0.0:{CurrentPort}");
            _ = Task.Run(() => AcceptLoop(_cts.Token));
        }
        catch (Exception ex)
        {
            StatusText = "启动失败";
            _log.Error("Remote control service failed to start.", ex);
        }
    }

    public void Stop()
    {
        if (!IsRunning && _listener is null) return;
        try
        {
            _cts?.Cancel();
            _listener?.Stop();
        }
        catch (Exception ex)
        {
            _log.Error("Remote control service stop failed.", ex);
        }
        finally
        {
            _cts?.Dispose();
            _cts = null;
            _listener = null;
            IsRunning = false;
            StatusText = "未启动";
            lock (_clients) _clients.Clear();
            _log.Info("Remote control service stopped; all remote connections invalidated.");
        }
    }

    public void Restart()
    {
        Stop();
        Start();
    }

    public void RegenerateToken()
    {
        var config = _getConfig();
        config.RemoteControl.Token = ConfigService.GenerateToken();
        _saveConfig(config);
        lock (_clients) _clients.Clear();
        _log.Info("Remote token regenerated; old links invalidated.");
    }

    public void DisconnectAll()
    {
        lock (_clients) _clients.Clear();
        _log.Info("All remote devices disconnected.");
    }

    private async Task AcceptLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested && _listener is not null)
        {
            try
            {
                var client = await _listener.AcceptTcpClientAsync(token);
                _ = Task.Run(() => HandleClient(client, token), token);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                if (!token.IsCancellationRequested) _log.Error("Remote accept failed.", ex);
            }
        }
    }

    private async Task HandleClient(TcpClient client, CancellationToken token)
    {
        using var _ = client;
        try
        {
            using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
            var requestLine = await reader.ReadLineAsync(token) ?? "";
            if (string.IsNullOrWhiteSpace(requestLine)) return;

            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string? line;
            var contentLength = 0;
            while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync(token)))
            {
                var idx = line.IndexOf(':');
                if (idx <= 0) continue;
                var name = line[..idx].Trim();
                var value = line[(idx + 1)..].Trim();
                headers[name] = value;
                if (name.Equals("Content-Length", StringComparison.OrdinalIgnoreCase)) int.TryParse(value, out contentLength);
            }

            var body = "";
            if (contentLength > 0)
            {
                var buffer = new char[contentLength];
                var read = await reader.ReadBlockAsync(buffer.AsMemory(0, contentLength), token);
                body = new string(buffer, 0, read);
            }

            var parts = requestLine.Split(' ');
            if (parts.Length < 2) return;
            var method = parts[0].ToUpperInvariant();
            var rawUrl = parts[1];
            var remote = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
            var ok = HandleRequest(method, rawUrl, body, remote, out var contentType, out var response, out var status);
            await WriteResponse(stream, status, contentType, response, token);
            if (!ok) _log.Warn($"Remote request rejected: {remote} {rawUrl}");
        }
        catch (Exception ex)
        {
            if (!token.IsCancellationRequested) _log.Error("Remote client handling failed.", ex);
        }
    }

    private bool HandleRequest(string method, string rawUrl, string body, string remote, out string contentType, out string response, out int status)
    {
        contentType = "application/json; charset=utf-8";
        response = "{}";
        status = 200;

        var uri = new Uri("http://localhost" + rawUrl);
        var token = GetQuery(uri.Query, "token");
        var config = _getConfig();
        if (!config.RemoteControl.Enabled || !IsRunning || token != config.RemoteControl.Token)
        {
            status = 403;
            response = ToJson(new { ok = false, error = "令牌无效或远程控制已关闭" });
            return false;
        }

        TrackClient(remote);
        if (uri.AbsolutePath == "/" || uri.AbsolutePath.Equals("/index.html", StringComparison.OrdinalIgnoreCase))
        {
            contentType = "text/html; charset=utf-8";
            response = BuildPage(config.RemoteControl.Token);
            return true;
        }

        if (uri.AbsolutePath.Equals("/state", StringComparison.OrdinalIgnoreCase))
        {
            response = ToJson(StateWithClientCount());
            return true;
        }

        if (uri.AbsolutePath.Equals("/command", StringComparison.OrdinalIgnoreCase) && method == "POST")
        {
            var command = JsonSerializer.Deserialize<RemoteCommand>(body, JsonOptions) ?? new RemoteCommand();
            if (!_commands.ExecuteRemoteCommand(command))
            {
                status = 400;
                response = ToJson(new { ok = false, error = "命令不被允许" });
                return false;
            }

            response = ToJson(StateWithClientCount());
            return true;
        }

        status = 404;
        response = ToJson(new { ok = false, error = "未找到" });
        return false;
    }

    private RemoteState StateWithClientCount()
    {
        var state = _commands.GetRemoteState();
        state.ConnectedClients = ConnectedClients;
        return state;
    }

    private void TrackClient(string remote)
    {
        var key = remote.Split(':')[0];
        lock (_clients)
        {
            _clients[key] = DateTime.Now;
            PruneClients();
        }
    }

    private void PruneClients()
    {
        foreach (var old in _clients.Where(x => DateTime.Now - x.Value > TimeSpan.FromSeconds(30)).Select(x => x.Key).ToList())
        {
            _clients.Remove(old);
        }
    }

    private static string GetQuery(string query, string key)
    {
        foreach (var pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 2 && Uri.UnescapeDataString(parts[0]) == key) return Uri.UnescapeDataString(parts[1]);
        }

        return "";
    }

    private static async Task WriteResponse(NetworkStream stream, int status, string contentType, string response, CancellationToken token)
    {
        var body = Encoding.UTF8.GetBytes(response);
        var header = Encoding.UTF8.GetBytes(
            $"HTTP/1.1 {status} {(status == 200 ? "OK" : "ERROR")}\r\n" +
            $"Content-Type: {contentType}\r\n" +
            $"Content-Length: {body.Length}\r\n" +
            "Cache-Control: no-store\r\n" +
            "Access-Control-Allow-Origin: *\r\n" +
            "Connection: close\r\n\r\n");
        await stream.WriteAsync(header, token);
        await stream.WriteAsync(body, token);
    }

    private static string ToJson<T>(T value) => JsonSerializer.Serialize(value, JsonOptions);

    private static string BuildPage(string token) => $$$"""
<!doctype html>
<html lang="zh-CN">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1,viewport-fit=cover">
<title>演讲计时器遥控</title>
<style>
:root{color-scheme:light;--accent:#11665f;--ink:#122527;--muted:#607276;--surface:#f3f7f8;--card:#fff;--soft:#eaf3f2;--danger:#a83d3d;--warn:#8a6500}
*{box-sizing:border-box}
body{margin:0;background:var(--surface);color:var(--ink);font-family:system-ui,'Microsoft YaHei UI','Microsoft YaHei',sans-serif}
main{max-width:560px;margin:0 auto;padding:18px 14px 26px}
header{display:flex;align-items:flex-start;justify-content:space-between;gap:14px;margin:4px 2px 14px}
h1{font-size:23px;line-height:1.25;margin:0}
.subtitle{font-size:14px;color:var(--muted);margin-top:5px;line-height:1.45}
.badge{flex:0 0 auto;border-radius:999px;padding:8px 12px;font-size:14px;font-weight:700;background:#e9eef0;color:var(--muted);white-space:nowrap}
.badge.ok{background:#dff3e8;color:#08703b}.badge.connecting{background:#fff2cb;color:var(--warn)}.badge.bad{background:#fde2e2;color:var(--danger)}
.card{background:var(--card);border-radius:14px;padding:14px;margin:12px 0;box-shadow:0 1px 0 rgba(17,43,47,.04)}
.timer{display:grid;grid-template-columns:1fr auto;gap:12px;align-items:center}
.time{font-size:48px;font-weight:800;line-height:1;text-align:left;color:var(--accent);letter-spacing:0}
.state{font-size:14px;line-height:1.7;color:var(--muted);text-align:right;white-space:nowrap}
.sync{display:flex;flex-wrap:wrap;gap:8px 12px;margin-top:12px;font-size:13px;color:var(--muted)}
.grid{display:grid;grid-template-columns:1fr 1fr;gap:10px}
button,input,select{width:100%;font:inherit;font-size:17px;border:0;border-radius:10px;min-height:48px;background:var(--soft);color:var(--ink);outline:none}
button{background:var(--accent);color:#fff;font-weight:700;padding:0 12px;white-space:nowrap}
button.secondary{background:var(--soft);color:var(--ink)}
button.warning{background:#f4ede0;color:#6c4d00}
button:disabled{opacity:.45;filter:grayscale(.4)}
.form{display:grid;grid-template-columns:1fr;gap:10px}
select,input{padding:0 14px}
.message{min-height:28px;font-size:14px;line-height:1.6;color:var(--muted)}
.message.bad{color:var(--danger);font-weight:700}
.meta{display:grid;grid-template-columns:1fr 1fr;gap:8px;color:var(--muted);font-size:13px;margin-top:10px}
.meta span{background:#f6fafb;border-radius:9px;padding:9px 10px}
@media (max-width:380px){.time{font-size:40px}.timer{grid-template-columns:1fr}.state{text-align:left}.grid{grid-template-columns:1fr}button,input,select{font-size:16px}}
</style>
</head>
<body>
<main>
<header>
  <div>
    <h1>演讲计时器遥控</h1>
    <div class="subtitle">手机和电脑在同一网络时，可同步控制计时器。</div>
  </div>
  <div id="connBadge" class="badge connecting">连接中</div>
</header>

<section class="card">
  <div class="timer">
    <div class="time" id="time">--:--</div>
    <div class="state" id="state">状态：连接中<br>模式：--</div>
  </div>
  <div class="sync">
    <span id="connText">连接中</span>
    <span id="lastSync">最后同步：--</span>
  </div>
  <div class="meta">
    <span id="windowState">窗口：--</span>
    <span id="muteState">静音：--</span>
  </div>
</section>

<section class="card grid">
  <button data-command="timer.start">开始</button>
  <button data-command="timer.pause" class="secondary">暂停</button>
  <button data-command="timer.resume" class="secondary">继续</button>
  <button data-command="timer.stop" class="warning">停止并重置</button>
  <button data-command="window.toggle" class="secondary">显示/隐藏</button>
  <button data-command="window.flash">触发闪烁</button>
  <button data-command="mute.toggle" class="secondary">静音/取消静音</button>
</section>

<section class="card form">
  <select id="mode"><option value="countdown">倒计时</option><option value="countup">正计时</option></select>
  <button id="modeButton" class="secondary">切换计时模式</button>
  <input id="duration" inputmode="numeric" placeholder="00:08:00" value="00:08:00">
  <button id="durationButton" class="secondary">修改默认时长</button>
</section>

<section class="card message" id="message">正在连接电脑端服务...</section>
</main>
<script>
const token='{{{token}}}';
let connected=false;
let lastState=null;
const commandButtons=[...document.querySelectorAll('[data-command]')];
const connBadge=document.getElementById('connBadge');
const connText=document.getElementById('connText');
const lastSync=document.getElementById('lastSync');
const message=document.getElementById('message');
function withToken(path){return path+(path.includes('?')?'&':'?')+'token='+encodeURIComponent(token);}
async function api(path,opt={}){
  const controller=new AbortController();
  const timer=setTimeout(()=>controller.abort(),4000);
  try{
    const r=await fetch(withToken(path),{cache:'no-store',...opt,signal:controller.signal});
    const text=await r.text();
    const data=text?JSON.parse(text):{};
    if(!r.ok) throw new Error(data.error||'连接失败');
    return data;
  }finally{clearTimeout(timer);}
}
function setConnection(kind,text){
  connBadge.className='badge '+kind;
  connBadge.textContent=text;
  connText.textContent=text;
  connected=kind==='ok';
  setButtons();
}
function setButtons(){
  const state=(lastState?.state||lastState?.State||'').trim();
  commandButtons.forEach(b=>b.disabled=!connected);
  document.getElementById('modeButton').disabled=!connected;
  document.getElementById('durationButton').disabled=!connected;
  if(!connected) return;
  const isRunning=state==='运行中';
  const isPaused=state==='暂停';
  const isStopped=state==='停止';
  const byCommand=cmd=>document.querySelector(`[data-command="${cmd}"]`);
  byCommand('timer.start').disabled=isRunning;
  byCommand('timer.pause').disabled=!isRunning;
  byCommand('timer.resume').disabled=!isPaused;
  byCommand('timer.stop').disabled=isStopped;
}
function paint(s){
  lastState=s;
  const display=s.displayText||s.DisplayText||'--:--';
  const state=s.state||s.State||'--';
  const mode=s.mode||s.Mode||'--';
  const windowVisible=s.windowVisible??s.WindowVisible;
  const muted=s.muted??s.Muted;
  const clients=s.connectedClients??s.ConnectedClients??0;
  document.getElementById('time').textContent=display;
  document.getElementById('state').innerHTML='状态：'+state+'<br>模式：'+mode;
  document.getElementById('windowState').textContent='窗口：'+(windowVisible?'显示':'隐藏');
  document.getElementById('muteState').textContent='静音：'+(muted?'是':'否');
  const now=new Date().toLocaleTimeString('zh-CN',{hour12:false});
  lastSync.textContent='最后同步：'+now+'，设备 '+clients;
  message.className='card message';
  message.textContent='已连接，命令会同步到电脑端计时器窗口。';
  setConnection('ok','已连接');
}
async function poll(){
  if(!connected) setConnection('connecting','连接中');
  try{paint(await api('/state'));}
  catch(e){
    connected=false;
    message.className='card message bad';
    message.textContent='连接失败/请重连：'+(e.name==='AbortError'?'请求超时':e.message);
    setConnection('bad','已断开');
  }
}
async function cmd(command){
  if(!connected){
    message.className='card message bad';
    message.textContent='连接失败/请重连，当前命令未发送。';
    return;
  }
  setConnection('connecting','连接中');
  message.className='card message';
  message.textContent='正在同步命令...';
  try{paint(await api('/command',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({command})}));}
  catch(e){
    connected=false;
    message.className='card message bad';
    message.textContent='连接失败/请重连：'+(e.name==='AbortError'?'请求超时':e.message);
    setConnection('bad','已断开');
  }
}
async function setDuration(){
  if(!connected) return;
  const duration=document.getElementById('duration').value.trim();
  try{paint(await api('/command',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({command:'timer.setDuration',duration})}));}
  catch(e){message.className='card message bad';message.textContent='修改失败：'+e.message;}
}
async function setMode(){
  if(!connected) return;
  const mode=document.getElementById('mode').value;
  try{paint(await api('/command',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({command:'timer.setMode',mode})}));}
  catch(e){message.className='card message bad';message.textContent='切换失败：'+e.message;}
}
commandButtons.forEach(b=>b.addEventListener('click',()=>cmd(b.dataset.command)));
document.getElementById('durationButton').addEventListener('click',setDuration);
document.getElementById('modeButton').addEventListener('click',setMode);
setButtons();
poll();
setInterval(poll,1000);
</script>
</body>
</html>
""";

    public void Dispose() => Stop();
}
