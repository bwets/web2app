using Web2App.Core;

namespace Web2App;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        if (args.Length != 1)
        {
            ShowError($"Usage: Web2App <file{Web2AppFile.Extension}>");
            return 2;
        }

        Web2AppFile app;
        try
        {
            app = Web2AppFile.Load(args[0]);
        }
        catch (Exception ex) when (ex is Web2AppFormatException or IOException or UnauthorizedAccessException)
        {
            ShowError(ex.Message);
            return 1;
        }

        using var instance = SingleInstance.Acquire(app.AppId);
        if (instance is null)
        {
            return 0;
        }

        // Before any window exists, so every window of this app groups under its own taskbar button.
        NativeMethods.SetProcessAppId(app.AppId);

        using var session = new AppSession(app);
        session.LoadCachedIcon();

        var window = new AppWindow(session, isMain: true);
        instance.ActivationRequested += () =>
        {
            if (window.IsHandleCreated)
            {
                window.BeginInvoke(window.ShowFromOtherLaunch);
            }
        };

        Application.Run(window);
        return 0;
    }

    private static void ShowError(string message) =>
        MessageBox.Show(message, "Web2App", MessageBoxButtons.OK, MessageBoxIcon.Error);
}
