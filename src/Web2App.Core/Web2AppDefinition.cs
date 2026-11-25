namespace Web2App.Core;

/// <summary>
/// The raw content of a <c>.web2app</c> file, exactly as written in the YAML.
/// Unknown keys are ignored so that newer files still open in older launchers.
/// </summary>
public sealed class Web2AppDefinition
{
    /// <summary>Window and taskbar title. When absent, the page title is used.</summary>
    public string? Title { get; set; }

    /// <summary>Start page of the app (http or https). Required.</summary>
    public string? Url { get; set; }

    /// <summary>Icon URL, absolute or relative to <see cref="Url"/>. When absent, the page favicon is used.</summary>
    public string? Icon { get; set; }

    /// <summary>Storage name. Apps with the same storage name share cookies, logins and site data.
    /// When absent, the file name without its extension is used.</summary>
    public string? Storage { get; set; }
}
