namespace FlyPPTTimer.Services;

public static class AlertSoundStorage
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3", ".wav", ".wma", ".m4a"
    };

    public static string ImportSound(string sourcePath, string slot, string? destinationDirectory = null)
    {
        if (!File.Exists(sourcePath)) throw new FileNotFoundException("所选提示音文件不存在。", sourcePath);
        var extension = Path.GetExtension(sourcePath).ToLowerInvariant();
        if (!SupportedExtensions.Contains(extension))
            throw new InvalidDataException("请选择 MP3、WAV、WMA 或 M4A 音频文件。");

        var directory = destinationDirectory ?? AppPaths.AlertSoundsDirectory;
        Directory.CreateDirectory(directory);
        var safeSlot = string.Concat(slot.Where(char.IsLetterOrDigit));
        if (string.IsNullOrWhiteSpace(safeSlot)) safeSlot = "alert";
        var destination = Path.Combine(directory, safeSlot + extension);
        if (!string.Equals(Path.GetFullPath(sourcePath), Path.GetFullPath(destination), StringComparison.OrdinalIgnoreCase))
            File.Copy(sourcePath, destination, true);
        return destination;
    }
}
