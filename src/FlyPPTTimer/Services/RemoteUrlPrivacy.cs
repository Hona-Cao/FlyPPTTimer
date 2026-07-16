namespace FlyPPTTimer.Services;

public static class RemoteUrlPrivacy
{
    public static string MaskToken(string url)
    {
        const string marker = "token=";
        var index = url.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        return index < 0 ? url : url[..(index + marker.Length)] + "••••••";
    }
}
