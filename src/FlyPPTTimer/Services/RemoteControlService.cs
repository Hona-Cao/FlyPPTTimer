using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Reflection;
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
    private readonly PowerPointControlService _powerPoint;
    private readonly LogService _log;
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private readonly Dictionary<string, DateTime> _clients = [];

    public RemoteControlService(Func<AppConfig> getConfig, Action<AppConfig> saveConfig, AppCommandService commands, PowerPointControlService powerPoint, LogService log)
    {
        _getConfig = getConfig;
        _saveConfig = saveConfig;
        _commands = commands;
        _powerPoint = powerPoint;
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
            response = ReadWebResource("index.html").Replace("__FLYPPT_TOKEN__", JavaScriptEncode(config.RemoteControl.Token));
            return true;
        }

        if (uri.AbsolutePath.Equals("/assets/app.css", StringComparison.OrdinalIgnoreCase))
        {
            contentType = "text/css; charset=utf-8";
            response = ReadWebResource("app.css");
            return true;
        }

        if (uri.AbsolutePath.Equals("/assets/app.js", StringComparison.OrdinalIgnoreCase))
        {
            contentType = "application/javascript; charset=utf-8";
            response = ReadWebResource("app.js");
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
            string message;
            if (command.Command.StartsWith("ppt.", StringComparison.Ordinal))
            {
                var result = _powerPoint.Execute(command);
                if (!result.Success)
                {
                    status = 400;
                    response = ToJson(StateWithClientCount(false, result.Message));
                    return false;
                }
                message = result.Message;
            }
            else if (!_commands.ExecuteRemoteCommand(command))
            {
                status = 400;
                response = ToJson(new { ok = false, error = "命令不被允许" });
                return false;
            }
            else message = "命令已执行";

            response = ToJson(StateWithClientCount(true, message));
            return true;
        }

        status = 404;
        response = ToJson(new { ok = false, error = "未找到" });
        return false;
    }

    private RemoteState StateWithClientCount(bool ok = true, string message = "")
    {
        var state = _commands.GetRemoteState();
        state.Ok = ok;
        state.Message = message;
        state.PresentationState = _powerPoint.GetState();
        state.ConnectedClients = ConnectedClients;
        return state;
    }

    private static string ReadWebResource(string fileName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames().FirstOrDefault(x => x.EndsWith($"Web.{fileName}", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"缺少远程控制网页资源：{fileName}");
        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private static string JavaScriptEncode(string value) => value.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\r", "").Replace("\n", "");

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

    public void Dispose() => Stop();
}
