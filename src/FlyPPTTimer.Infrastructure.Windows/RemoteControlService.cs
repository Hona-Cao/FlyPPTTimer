using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FlyPPTTimer.Core.Abstractions;
using FlyPPTTimer.Core.Models;
using FlyPPTTimer.Core.Services;

namespace FlyPPTTimer.Infrastructure.Windows;

public sealed class RemoteControlService : IRemoteControlService
{
    private const int MaxHeaderBytes = 16 * 1024;
    private const int MaxBodyBytes = 64 * 1024;
    private readonly ApplicationController _controller;
    private readonly Func<string, string> _assetLoader;
    private readonly ILogService _log;
    private readonly SemaphoreSlim _slots = new(16, 16);
    private readonly Dictionary<string, DateTime> _clients = [];
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;

    public RemoteControlService(ApplicationController controller, Func<string, string> assetLoader, ILogService log) { _controller = controller; _assetLoader = assetLoader; _log = log; }
    public bool IsRunning { get; private set; }
    public int CurrentPort { get; private set; }
    public int ConnectedClients { get { lock (_clients) { PruneClients(); return _clients.Count; } } }

    public void Start()
    {
        if (IsRunning || !_controller.Config.RemoteControl.Enabled) return;
        try
        {
            var settings = _controller.Config.RemoteControl; var port = settings.UseRandomPort || settings.Port <= 0 ? 0 : settings.Port;
            _listener = new(IPAddress.Any, port); _listener.Start(); CurrentPort = ((IPEndPoint)_listener.LocalEndpoint).Port;
            settings.Port = CurrentPort; _controller.SaveRules(); _cts = new(); IsRunning = true; _ = Task.Run(() => AcceptLoop(_cts.Token));
            _log.Info($"手机遥控已启动：0.0.0.0:{CurrentPort}");
        }
        catch (Exception ex) { _log.Error("手机遥控启动失败。", ex); }
    }
    public void Stop()
    {
        try { _cts?.Cancel(); _listener?.Stop(); } catch { }
        _cts?.Dispose(); _cts = null; _listener = null; IsRunning = false; lock (_clients) _clients.Clear();
    }
    public void Restart() { Stop(); Start(); }
    public void DisconnectAll()
    {
        _controller.Config.RemoteControl.Token = ConfigService.GenerateToken(); lock (_clients) _clients.Clear(); _controller.SaveRules();
        _log.Info("已断开所有设备并生成新令牌。");
    }

    private async Task AcceptLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested && _listener is not null)
        {
            try { var client = await _listener.AcceptTcpClientAsync(token); _ = Task.Run(() => HandleClient(client, token), token); }
            catch (OperationCanceledException) { }
            catch (Exception ex) { if (!token.IsCancellationRequested) _log.Error("远控连接失败。", ex); }
        }
    }
    private async Task HandleClient(TcpClient client, CancellationToken serviceToken)
    {
        using var owned = client;
        if (!await _slots.WaitAsync(TimeSpan.FromSeconds(2), serviceToken)) return;
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(serviceToken); timeout.CancelAfter(TimeSpan.FromSeconds(20));
            using var stream = client.GetStream(); var request = await ReadRequest(stream, timeout.Token);
            var remote = (client.Client.RemoteEndPoint as IPEndPoint)?.Address.ToString() ?? "unknown";
            var result = await Route(request, remote, timeout.Token); await WriteResponse(stream, result, timeout.Token);
        }
        catch (OperationCanceledException) when (!serviceToken.IsCancellationRequested) { _log.Warn("远控请求超时。"); }
        catch (Exception ex) { if (!serviceToken.IsCancellationRequested) _log.Error("远控请求处理失败。", ex); }
        finally { _slots.Release(); }
    }

    private async Task<HttpResult> Route(HttpRequest request, string remote, CancellationToken token)
    {
        var uri = new Uri("http://localhost" + request.Url); var supplied = Query(uri.Query, "token"); var expected = _controller.Config.RemoteControl.Token;
        if (!IsRunning || !FixedEquals(supplied, expected)) return Json(403, new { ok = false, error = "令牌无效或远程控制已关闭" });
        Track(remote);
        if (uri.AbsolutePath is "/" or "/index.html") return new(200, "text/html; charset=utf-8", _assetLoader("index.html").Replace("__FLYPPT_TOKEN__", Js(expected)));
        if (uri.AbsolutePath == "/assets/app.css") return new(200, "text/css; charset=utf-8", _assetLoader("app.css"));
        if (uri.AbsolutePath == "/assets/app.js") return new(200, "application/javascript; charset=utf-8", _assetLoader("app.js"));
        if (uri.AbsolutePath == "/state") return Json(200, BuildState(true, ""));
        if (uri.AbsolutePath == "/command" && request.Method == "POST")
        {
            var command = JsonSerializer.Deserialize<RemoteEnvelope>(request.Body, JsonOptions) ?? new();
            if (command.Command.StartsWith("ppt.", StringComparison.Ordinal))
            {
                var pptCommand = command.Command switch
                {
                    "ppt.openAndStartBeginning" => "ppt.openPresentation",
                    "ppt.openAndStartCurrent" => "ppt.openPresentation",
                    _ => command.Command
                };
                var result = await _controller.ExecutePresentationAsync(new(pptCommand, command.PresentationId, command.SlideNumber), token);
                if (result.Success && command.Command is "ppt.openAndStartBeginning" or "ppt.openAndStartCurrent")
                    result = await _controller.ExecutePresentationAsync(new(command.Command.EndsWith("Beginning", StringComparison.Ordinal) ? "ppt.startFromBeginning" : "ppt.startFromCurrent"), token);
                return Json(result.Success ? 200 : 400, BuildState(result.Success, result.Message));
            }
            if (!_controller.ExecuteTimer(command.Command)) return Json(400, new { ok = false, error = "命令不在白名单中" });
            return Json(200, BuildState(true, "命令已执行"));
        }
        return Json(404, new { ok = false, error = "未找到" });
    }

    private object BuildState(bool ok, string message)
    {
        var state = _controller.State.Current;
        var timer = new
        {
            mode = state.Timer.Mode == TimerMode.Countdown ? "倒计时" : "正计时",
            state = state.Timer.State switch { TimerState.Running => "运行中", TimerState.Paused => "暂停", TimerState.Finished => "已结束", _ => "停止" },
            running = state.Timer.State == TimerState.Running,
            durationMs = (long)state.Timer.Duration.TotalMilliseconds,
            elapsedMs = (long)state.Timer.Elapsed.TotalMilliseconds,
            remainingMs = (long)state.Timer.Remaining.TotalMilliseconds,
            displayText = state.Timer.DisplayText,
            windowVisible = state.Config.Placement.Visible,
            muted = state.Muted
        };
        return new { ok, message, version = "0.13.0", timerState = timer, presentationState = state.Presentation, connectedClients = ConnectedClients, muted = state.Muted };
    }
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true };
    private static HttpResult Json(int status, object value) => new(status, "application/json; charset=utf-8", JsonSerializer.Serialize(value, JsonOptions));
    private static bool FixedEquals(string left, string right) => CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(left ?? "")), SHA256.HashData(Encoding.UTF8.GetBytes(right ?? "")));
    private static string Query(string query, string key) { foreach (var pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries)) { var parts = pair.Split('=', 2); if (parts.Length == 2 && Uri.UnescapeDataString(parts[0]) == key) return Uri.UnescapeDataString(parts[1]); } return ""; }
    private static string Js(string value) => value.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\r", "").Replace("\n", "");
    private void Track(string address) { lock (_clients) { _clients[address] = DateTime.Now; PruneClients(); _controller.State.UpdateRemote(_clients.Count); } }
    private void PruneClients() { foreach (var old in _clients.Where(x => DateTime.Now - x.Value > TimeSpan.FromSeconds(30)).Select(x => x.Key).ToList()) _clients.Remove(old); }

    private static async Task<HttpRequest> ReadRequest(Stream stream, CancellationToken token)
    {
        using var data = new MemoryStream(); var buffer = new byte[2048]; var headerEnd = -1;
        while (data.Length < MaxHeaderBytes && headerEnd < 0)
        {
            var read = await stream.ReadAsync(buffer, token); if (read == 0) throw new EndOfStreamException(); data.Write(buffer, 0, read);
            var bytes = data.GetBuffer(); for (var i = Math.Max(0, (int)data.Length - read - 3); i <= data.Length - 4; i++) if (bytes[i] == 13 && bytes[i + 1] == 10 && bytes[i + 2] == 13 && bytes[i + 3] == 10) { headerEnd = i; break; }
        }
        if (headerEnd < 0) throw new InvalidDataException("请求头过大或不完整。");
        var all = data.ToArray(); var header = Encoding.ASCII.GetString(all, 0, headerEnd); var lines = header.Split("\r\n"); var first = lines[0].Split(' '); if (first.Length < 2) throw new InvalidDataException("请求行无效。");
        var length = 0; foreach (var line in lines.Skip(1)) { var index = line.IndexOf(':'); if (index > 0 && line[..index].Equals("Content-Length", StringComparison.OrdinalIgnoreCase) && !int.TryParse(line[(index + 1)..].Trim(), out length)) throw new InvalidDataException("Content-Length 无效。"); }
        if (length < 0 || length > MaxBodyBytes) throw new InvalidDataException("请求体过大。");
        var body = new byte[length]; var prefixOffset = headerEnd + 4; var copied = Math.Min(all.Length - prefixOffset, length); if (copied > 0) Buffer.BlockCopy(all, prefixOffset, body, 0, copied); var offset = copied;
        while (offset < length) { var read = await stream.ReadAsync(body.AsMemory(offset, length - offset), token); if (read == 0) throw new EndOfStreamException("请求体不完整。"); offset += read; }
        return new(first[0].ToUpperInvariant(), first[1], Encoding.UTF8.GetString(body));
    }
    private static async Task WriteResponse(Stream stream, HttpResult result, CancellationToken token)
    {
        var body = Encoding.UTF8.GetBytes(result.Body); var header = Encoding.UTF8.GetBytes($"HTTP/1.1 {result.Status} {(result.Status == 200 ? "OK" : "ERROR")}\r\nContent-Type: {result.ContentType}\r\nContent-Length: {body.Length}\r\nCache-Control: no-store\r\nContent-Security-Policy: default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self'; connect-src 'self'; img-src 'self' data:; object-src 'none'; base-uri 'none'\r\nReferrer-Policy: no-referrer\r\nX-Content-Type-Options: nosniff\r\nConnection: close\r\n\r\n");
        await stream.WriteAsync(header, token); await stream.WriteAsync(body, token);
    }
    public void Dispose() { Stop(); _slots.Dispose(); }
    private sealed record HttpRequest(string Method, string Url, string Body);
    private sealed record HttpResult(int Status, string ContentType, string Body);
    private sealed class RemoteEnvelope { public string Command { get; set; } = ""; public string? PresentationId { get; set; } public int? SlideNumber { get; set; } }
}
