using System.Security.Cryptography;
using System.Text;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Web2App.Core;

/// <summary>A loaded and validated <c>.web2app</c> file.</summary>
public sealed class Web2AppFile
{
    public const string Extension = ".web2app";

    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    private Web2AppFile(string path, Web2AppDefinition definition, Uri url, Uri? iconUrl, string storageName)
    {
        Path = path;
        Definition = definition;
        Url = url;
        IconUrl = iconUrl;
        StorageName = storageName;
        AppId = CreateAppId(path);
    }

    /// <summary>Full path of the file.</summary>
    public string Path { get; }

    /// <summary>The file content as written.</summary>
    public Web2AppDefinition Definition { get; }

    /// <summary>Start page.</summary>
    public Uri Url { get; }

    /// <summary>Configured icon, resolved against <see cref="Url"/>; null means "use the favicon".</summary>
    public Uri? IconUrl { get; }

    /// <summary>Effective storage name (configured, or derived from the file name), safe as a folder name.</summary>
    public string StorageName { get; }

    /// <summary>Configured title, or the file name when there is none.</summary>
    public string DisplayName => string.IsNullOrWhiteSpace(Definition.Title)
        ? System.IO.Path.GetFileNameWithoutExtension(Path)
        : Definition.Title.Trim();

    /// <summary>
    /// Stable identity of this app file, used as the Windows AppUserModelID (taskbar grouping and pinning)
    /// and as the key of per-app caches. Derived from the full path, so each file is its own app.
    /// </summary>
    public string AppId { get; }

    public static Web2AppFile Load(string path)
    {
        var fullPath = System.IO.Path.GetFullPath(path);
        var fileName = System.IO.Path.GetFileName(fullPath);
        var yaml = File.ReadAllText(fullPath);

        Web2AppDefinition? definition;
        try
        {
            definition = Deserializer.Deserialize<Web2AppDefinition?>(yaml);
        }
        catch (YamlException ex)
        {
            var reason = ex.InnerException?.Message ?? ex.Message;
            throw new Web2AppFormatException($"{fileName}: invalid YAML at line {ex.Start.Line}: {reason}", ex);
        }

        if (definition is null)
        {
            throw new Web2AppFormatException($"{fileName}: the file is empty.");
        }

        return FromDefinition(fullPath, definition);
    }

    public static Web2AppFile FromDefinition(string path, Web2AppDefinition definition)
    {
        var fullPath = System.IO.Path.GetFullPath(path);
        var fileName = System.IO.Path.GetFileName(fullPath);

        if (string.IsNullOrWhiteSpace(definition.Url))
        {
            throw new Web2AppFormatException($"{fileName}: 'url' is required.");
        }

        if (!Uri.TryCreate(definition.Url.Trim(), UriKind.Absolute, out var url) || !IsWeb(url))
        {
            throw new Web2AppFormatException($"{fileName}: 'url' must be an absolute http or https address.");
        }

        Uri? iconUrl = null;
        if (!string.IsNullOrWhiteSpace(definition.Icon))
        {
            if (!Uri.TryCreate(url, definition.Icon.Trim(), out iconUrl) || !IsWeb(iconUrl))
            {
                throw new Web2AppFormatException($"{fileName}: 'icon' must be an http or https address, absolute or relative to 'url'.");
            }
        }

        var storage = string.IsNullOrWhiteSpace(definition.Storage)
            ? System.IO.Path.GetFileNameWithoutExtension(fullPath)
            : definition.Storage;
        var storageName = StorageNames.Sanitize(storage)
            ?? throw new Web2AppFormatException($"{fileName}: '{storage}' cannot be used as a storage name.");

        return new Web2AppFile(fullPath, definition, url, iconUrl, storageName);
    }

    private static bool IsWeb(Uri uri) => uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;

    private static string CreateAppId(string fullPath)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(fullPath.ToUpperInvariant()));
        return "Web2App." + Convert.ToHexString(hash, 0, 8);
    }
}
