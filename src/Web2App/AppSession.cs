using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using Microsoft.Web.WebView2.Core;
using Web2App.Core;

namespace Web2App;

/// <summary>
/// State shared by all windows of one running app: the WebView2 environment bound to the app's storage,
/// and the app icon (cached on disk so the next launch and taskbar pins show it immediately).
/// </summary>
internal sealed class AppSession : IDisposable
{
    private static readonly HttpClient Http = CreateHttpClient();

    private Task<CoreWebView2Environment>? environment;
    private byte[]? currentPng;
    private IntPtr iconHandle;

    public AppSession(Web2AppFile app)
    {
        App = app;
        IconPath = Web2AppPaths.IconFile(app.AppId);
    }

    public event EventHandler? IconChanged;

    public Web2AppFile App { get; }

    /// <summary>Path of the cached .ico for this app; may not exist yet.</summary>
    public string IconPath { get; }

    public Icon? Icon { get; private set; }

    /// <summary>All windows of the app share one environment, which is required for in-app popups.</summary>
    public Task<CoreWebView2Environment> GetEnvironmentAsync() =>
        environment ??= CoreWebView2Environment.CreateAsync(
            browserExecutableFolder: null,
            userDataFolder: Web2AppPaths.StorageDirectory(App.StorageName));

    public void LoadCachedIcon()
    {
        try
        {
            if (!File.Exists(IconPath))
            {
                return;
            }

            var png = IconFile.ExtractLargestPng(File.ReadAllBytes(IconPath));
            using var bitmap = png is null ? null : IconImages.Decode(png);
            if (bitmap is not null)
            {
                currentPng = png;
                SetIcon(bitmap);
            }
        }
        catch (IOException)
        {
            // A missing or locked cache only means the icon shows up a moment later.
        }
    }

    /// <summary>Downloads the icon configured in the file; false when it is missing or not a supported image.</summary>
    public async Task<bool> TryLoadConfiguredIconAsync()
    {
        if (App.IconUrl is null)
        {
            return false;
        }

        try
        {
            var data = await Http.GetByteArrayAsync(App.IconUrl);
            using var bitmap = IconImages.Decode(data);
            if (bitmap is null)
            {
                return false;
            }

            Apply(bitmap);
            return true;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return false;
        }
    }

    /// <summary>Uses the current favicon of the page as the app icon.</summary>
    public async Task UpdateFromFaviconAsync(CoreWebView2 webView)
    {
        try
        {
            if (string.IsNullOrEmpty(webView.FaviconUri))
            {
                return;
            }

            await using var stream = await webView.GetFaviconAsync(CoreWebView2FaviconImageFormat.Png);
            if (stream is null)
            {
                return;
            }

            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer);
            using var bitmap = IconImages.Decode(buffer.ToArray());
            if (bitmap is not null)
            {
                Apply(bitmap);
            }
        }
        catch (COMException)
        {
            // The page navigated away while the favicon was being read; the next change will retry.
        }
    }

    public void Dispose()
    {
        if (iconHandle != IntPtr.Zero)
        {
            NativeMethods.DestroyIconHandle(iconHandle);
            iconHandle = IntPtr.Zero;
        }
    }

    private void Apply(Bitmap source)
    {
        using var normalized = IconImages.Normalize(source);
        var png = IconImages.ToPng(normalized);
        if (currentPng is not null && png.AsSpan().SequenceEqual(currentPng))
        {
            return;
        }

        currentPng = png;
        SaveCache(png);
        SetIcon(normalized);
    }

    private void SaveCache(byte[] png)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(IconPath)!);
            var temp = IconPath + ".tmp";
            File.WriteAllBytes(temp, IconFile.FromPng(png));
            File.Move(temp, IconPath, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // The icon still applies to the running windows; only the cache for next time is lost.
        }
    }

    private void SetIcon(Bitmap bitmap)
    {
        var previousHandle = iconHandle;
        iconHandle = bitmap.GetHicon();
        Icon = Icon.FromHandle(iconHandle);
        IconChanged?.Invoke(this, EventArgs.Empty);

        if (previousHandle != IntPtr.Zero)
        {
            NativeMethods.DestroyIconHandle(previousHandle);
        }
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Web2App", "0.1"));
        return client;
    }
}
