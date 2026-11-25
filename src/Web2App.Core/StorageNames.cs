namespace Web2App.Core;

/// <summary>Turns a storage name into a folder name that is valid on Windows.</summary>
public static class StorageNames
{
    private static readonly char[] InvalidChars = ['<', '>', ':', '"', '/', '\\', '|', '?', '*'];

    private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
    };

    /// <summary>Returns the folder name for <paramref name="name"/>, or null when nothing usable is left.</summary>
    public static string? Sanitize(string name)
    {
        var chars = name.Trim()
            .Select(c => char.IsControl(c) || Array.IndexOf(InvalidChars, c) >= 0 ? '_' : c)
            .ToArray();
        var result = new string(chars).TrimEnd('.', ' ');

        if (result.Length == 0 || result.All(c => c == '.'))
        {
            return null;
        }

        return ReservedNames.Contains(result) ? result + "_" : result;
    }
}
