using System.Diagnostics;
using System.Net;

namespace Web2App;

/// <summary>Decides which new windows stay in the app and which go to the default browser.</summary>
internal static class LinkPolicy
{
    /// <summary>
    /// True when both addresses belong to the same site: same host, or same last two host labels
    /// (mail.example.com and www.example.com are the same site).
    /// </summary>
    public static bool IsSameSite(Uri appUrl, Uri target)
    {
        if (target.Scheme != Uri.UriSchemeHttp && target.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        return string.Equals(SiteOf(appUrl.Host), SiteOf(target.Host), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Opens a link with the default handler; only web and mail links, never local files or other schemes.</summary>
    public static void OpenExternally(Uri target)
    {
        if (target.Scheme != Uri.UriSchemeHttp && target.Scheme != Uri.UriSchemeHttps && target.Scheme != Uri.UriSchemeMailto)
        {
            return;
        }

        Process.Start(new ProcessStartInfo(target.AbsoluteUri) { UseShellExecute = true })?.Dispose();
    }

    private static string SiteOf(string host)
    {
        if (IPAddress.TryParse(host, out _))
        {
            return host;
        }

        var labels = host.TrimEnd('.').Split('.');
        return labels.Length <= 2 ? host : string.Join('.', labels[^2..]);
    }
}
