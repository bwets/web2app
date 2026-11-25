using System.Runtime.InteropServices;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace Web2App;

/// <summary>
/// A window of the app. The main window loads the app URL; popup windows host pages the app opened
/// with <c>window.open</c> (sign-in flows, compose windows, ...).
/// </summary>
internal sealed class AppWindow : Form
{
    private readonly AppSession session;
    private readonly bool isMain;
    private readonly WebView2 webView = new() { Dock = DockStyle.Fill };
    private bool useFavicon;

    public AppWindow(AppSession session, bool isMain)
    {
        this.session = session;
        this.isMain = isMain;

        Text = isMain ? session.App.DisplayName : string.Empty;
        ClientSize = new Size(LogicalToDeviceUnits(1280), LogicalToDeviceUnits(860));
        StartPosition = FormStartPosition.WindowsDefaultLocation;
        if (session.Icon is { } icon)
        {
            Icon = icon;
        }

        Controls.Add(webView);
        session.IconChanged += OnIconChanged;
    }

    public CoreWebView2 CoreWebView2 => webView.CoreWebView2;

    public async Task InitializeAsync()
    {
        var environment = await session.GetEnvironmentAsync();
        await webView.EnsureCoreWebView2Async(environment);

        var core = webView.CoreWebView2;
        core.NewWindowRequested += OnNewWindowRequested;
        core.WindowCloseRequested += (_, _) => Close();
        core.DocumentTitleChanged += (_, _) =>
        {
            if (!isMain || string.IsNullOrWhiteSpace(session.App.Definition.Title))
            {
                Text = core.DocumentTitle;
            }
        };

        if (isMain)
        {
            core.FaviconChanged += async (_, _) =>
            {
                if (useFavicon)
                {
                    await session.UpdateFromFaviconAsync(core);
                }
            };
        }
    }

    /// <summary>Shows the window again when the app is launched a second time.</summary>
    public void ShowFromOtherLaunch()
    {
        if (WindowState == FormWindowState.Minimized)
        {
            WindowState = FormWindowState.Normal;
        }

        Activate();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        if (isMain)
        {
            UpdateShellProperties();
        }
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        if (!isMain)
        {
            return;
        }

        try
        {
            await InitializeAsync();
        }
        catch (WebView2RuntimeNotFoundException)
        {
            MessageBox.Show(this, "The Microsoft Edge WebView2 Runtime is not installed.", "Web2App", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Close();
            return;
        }

        CoreWebView2.Navigate(session.App.Url.AbsoluteUri);

        if (await session.TryLoadConfiguredIconAsync())
        {
            return;
        }

        useFavicon = true;
        await session.UpdateFromFaviconAsync(CoreWebView2);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        session.IconChanged -= OnIconChanged;
        base.OnFormClosed(e);
    }

    private async void OnNewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
    {
        var features = e.WindowFeatures;
        var isPopup = features.HasPosition || features.HasSize;
        var isBlank = !Uri.TryCreate(e.Uri, UriKind.Absolute, out var target) || target.Scheme == "about";

        // Plain links to another site (target=_blank) belong in the browser; popups and same-site windows stay here.
        if (!isPopup && !isBlank && !LinkPolicy.IsSameSite(session.App.Url, target!))
        {
            e.Handled = true;
            LinkPolicy.OpenExternally(target!);
            return;
        }

        var deferral = e.GetDeferral();
        try
        {
            var window = new AppWindow(session, isMain: false);
            if (features.HasSize)
            {
                window.ClientSize = new Size(window.LogicalToDeviceUnits((int)features.Width), window.LogicalToDeviceUnits((int)features.Height));
            }

            if (features.HasPosition)
            {
                window.StartPosition = FormStartPosition.Manual;
                window.Location = new Point(window.LogicalToDeviceUnits((int)features.Left), window.LogicalToDeviceUnits((int)features.Top));
            }

            window.Show();
            await window.InitializeAsync();
            e.NewWindow = window.CoreWebView2;
            e.Handled = true;
        }
        catch (COMException)
        {
            e.Handled = true;
        }
        finally
        {
            deferral.Complete();
        }
    }

    private void OnIconChanged(object? sender, EventArgs e)
    {
        Icon = session.Icon;
        if (isMain && IsHandleCreated)
        {
            UpdateShellProperties();
        }
    }

    private void UpdateShellProperties()
    {
        var command = $"\"{Environment.ProcessPath}\" \"{session.App.Path}\"";
        var icon = File.Exists(session.IconPath) ? session.IconPath + ",0" : null;
        try
        {
            NativeMethods.SetWindowAppProperties(Handle, session.App.AppId, command, session.App.DisplayName, icon);
        }
        catch (COMException)
        {
            // Without these the window still works; it only groups and pins as a generic Web2App window.
        }
    }
}
