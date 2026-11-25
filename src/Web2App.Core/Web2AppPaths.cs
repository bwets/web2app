namespace Web2App.Core;

/// <summary>Where Web2App keeps its data, under <c>%LOCALAPPDATA%\Web2App</c>.</summary>
public static class Web2AppPaths
{
    public static string Root { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Web2App");

    /// <summary>WebView2 user data folder for a storage name (cookies, logins, site data).</summary>
    public static string StorageDirectory(string storageName) => Path.Combine(Root, "Storage", storageName);

    /// <summary>Cached icon of an app, written by the launcher; usable for shortcuts and taskbar pins.</summary>
    public static string IconFile(string appId) => Path.Combine(Root, "Icons", appId + ".ico");
}
