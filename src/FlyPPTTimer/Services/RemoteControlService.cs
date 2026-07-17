using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Reflection;
using System.Security.Cryptography;
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
    private readonly PowerPointControlService? _powerPoint;
    private readonly LogService _log;
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private readonly Dictionary<string, DateTime> _clients = [];
    private readonly SemaphoreSlim _connectionSlots = new(16, 16);
    private long _revision;
    internal const int MaxHeaderBytes = 16 * 1024;
    internal const int MaxBodyBytes = 64 * 1024;

    public RemoteControlService(Func<AppConfig> getConfig, Action<AppConfig> saveConfig, AppCommandService commands, PowerPointControlService? powerPoint, LogService log)
    {
        _getConfig = getConfig;
        _saveConfig = saveConfig;
        _commands = commands;
        _powerPoint = powerPoint;
        _log = log;
        if (_powerPoint is not null) _powerPoint.StateChanged += (_, _) => NotifyStateChanged();
    }

    public bool IsRunning { get; private set; }
    public PowerPointControlService? PresentationController => _powerPoint;
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

    public void NotifyStateChanged() => Interlocked.Increment(ref _revision);

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
            NotifyStateChanged();
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
            NotifyStateChanged();
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
        NotifyStateChanged();
        _log.Info("Remote token regenerated; old links invalidated.");
    }

    public void DisconnectAll()
    {
        RegenerateToken();
        _log.Info("All remote devices disconnected and their links invalidated.");
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
        if (!await _connectionSlots.WaitAsync(TimeSpan.FromSeconds(2), token)) return;
        try
        {
            client.ReceiveTimeout = 8000;
            client.SendTimeout = 8000;
            using var stream = client.GetStream();
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
            timeout.CancelAfter(TimeSpan.FromSeconds(20));
            var (headerText, bodyPrefix) = await ReadHeaders(stream, timeout.Token);
            var lines = headerText.Split("\r\n", StringSplitOptions.None);
            var requestLine = lines.FirstOrDefault() ?? "";
            if (string.IsNullOrWhiteSpace(requestLine)) return;

            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var contentLength = 0;
            foreach (var line in lines.Skip(1))
            {
                var idx = line.IndexOf(':');
                if (idx <= 0) continue;
                var name = line[..idx].Trim();
                var value = line[(idx + 1)..].Trim();
                headers[name] = value;
                if (name.Equals("Content-Length", StringComparison.OrdinalIgnoreCase)) int.TryParse(value, out contentLength);
            }
            if (contentLength < 0 || contentLength > MaxBodyBytes) throw new InvalidDataException("请求体过大。");

            var bodyBytes = await ReadBody(stream, bodyPrefix, contentLength, timeout.Token);
            var body = Encoding.UTF8.GetString(bodyBytes);

            var parts = requestLine.Split(' ');
            if (parts.Length < 2) return;
            var method = parts[0].ToUpperInvariant();
            var rawUrl = parts[1];
            var remote = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
            var ok = HandleRequest(method, rawUrl, body, remote, out var contentType, out var response, out var status);
            await WriteResponse(stream, status, contentType, response, timeout.Token);
            if (!ok) _log.Warn($"Remote request rejected: {remote} {RedactUrl(rawUrl)}");
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested) { _log.Warn("Remote request timed out."); }
        catch (Exception ex)
        {
            if (!token.IsCancellationRequested) _log.Error("Remote client handling failed.", ex);
        }
        finally { _connectionSlots.Release(); }
    }

    internal static async Task<(string Header, byte[] BodyPrefix)> ReadHeaders(Stream stream, CancellationToken token)
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[2048];
        while (buffer.Length < MaxHeaderBytes)
        {
            var read = await stream.ReadAsync(chunk, token);
            if (read == 0) throw new EndOfStreamException("连接在请求头完成前关闭。");
            buffer.Write(chunk, 0, read);
            var data = buffer.GetBuffer();
            var length = (int)buffer.Length;
            for (var i = Math.Max(0, length - read - 3); i <= length - 4; i++)
            {
                if (data[i] != 13 || data[i + 1] != 10 || data[i + 2] != 13 || data[i + 3] != 10) continue;
                if (i + 4 > MaxHeaderBytes) throw new InvalidDataException("请求头过大。");
                var header = Encoding.ASCII.GetString(data, 0, i);
                var prefixLength = length - i - 4;
                var prefix = new byte[prefixLength];
                Buffer.BlockCopy(data, i + 4, prefix, 0, prefixLength);
                return (header, prefix);
            }
        }
        throw new InvalidDataException("请求头过大。");
    }

    internal static async Task<byte[]> ReadBody(Stream stream, byte[] prefix, int contentLength, CancellationToken token)
    {
        if (contentLength < 0 || contentLength > MaxBodyBytes) throw new InvalidDataException("请求体过大。");
        var body = new byte[contentLength];
        var copied = Math.Min(contentLength, prefix.Length);
        prefix.AsSpan(0, copied).CopyTo(body);
        var offset = copied;
        while (offset < contentLength)
        {
            var read = await stream.ReadAsync(body.AsMemory(offset, contentLength - offset), token);
            if (read == 0) throw new EndOfStreamException("请求体未完整发送。");
            offset += read;
        }
        return body;
    }

    private bool HandleRequest(string method, string rawUrl, string body, string remote, out string contentType, out string response, out int status)
    {
        contentType = "application/json; charset=utf-8";
        response = "{}";
        status = 200;

        var uri = new Uri("http://localhost" + rawUrl);
        var token = GetQuery(uri.Query, "token");
        var config = _getConfig();
        if (!config.RemoteControl.Enabled || !IsRunning || !FixedTimeTokenEquals(token, config.RemoteControl.Token))
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
                if (_powerPoint is null)
                {
                    status = 503;
                    response = ToJson(StateWithClientCount(false, "演示控制服务当前不可用。"));
                    return false;
                }
                var result = _powerPoint.Queue(command);
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
        state.PresentationState = _powerPoint?.GetState() ?? new PresentationState { Error = "演示控制服务当前不可用。" };
        state.ConnectedClients = ConnectedClients;
        state.Revision = Volatile.Read(ref _revision);
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

    internal static bool FixedTimeTokenEquals(string supplied, string expected)
    {
        var left = SHA256.HashData(Encoding.UTF8.GetBytes(supplied ?? ""));
        var right = SHA256.HashData(Encoding.UTF8.GetBytes(expected ?? ""));
        return CryptographicOperations.FixedTimeEquals(left, right);
    }

    internal static string RedactUrl(string rawUrl)
    {
        try
        {
            var uri = new Uri("http://localhost" + rawUrl);
            return uri.AbsolutePath;
        }
        catch { return "<invalid-url>"; }
    }

    private static async Task WriteResponse(NetworkStream stream, int status, string contentType, string response, CancellationToken token)
    {
        var body = Encoding.UTF8.GetBytes(response);
        var header = Encoding.UTF8.GetBytes(
            $"HTTP/1.1 {status} {(status == 200 ? "OK" : "ERROR")}\r\n" +
            $"Content-Type: {contentType}\r\n" +
            $"Content-Length: {body.Length}\r\n" +
            "Cache-Control: no-store\r\n" +
            "Content-Security-Policy: default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self'; connect-src 'self'; img-src 'self' data:; object-src 'none'; base-uri 'none'; frame-ancestors 'none'\r\n" +
            "Referrer-Policy: no-referrer\r\n" +
            "X-Content-Type-Options: nosniff\r\n" +
            "Connection: close\r\n\r\n");
        await stream.WriteAsync(header, token);
        await stream.WriteAsync(body, token);
    }

    private static string ToJson<T>(T value) => JsonSerializer.Serialize(value, JsonOptions);

    public void Dispose() => Stop();
}
