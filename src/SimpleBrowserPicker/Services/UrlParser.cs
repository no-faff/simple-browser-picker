using System.Web;

namespace SimpleBrowserPicker.Services;

/// <summary>
/// Parses URLs and unwraps known redirect wrappers.
/// </summary>
public static class UrlParser
{
    /// <summary>
    /// Unwraps redirect wrappers (SafeLinks, Google redirect) and returns
    /// the canonical URL. Returns the original string if no wrapper detected.
    /// </summary>
    public static string Unwrap(string rawUrl)
    {
        if (!Uri.TryCreate(rawUrl, UriKind.Absolute, out Uri? uri))
            return rawUrl;

        // Outlook SafeLinks
        if (uri.Host.EndsWith("safelinks.protection.outlook.com", StringComparison.OrdinalIgnoreCase))
        {
            string? inner = HttpUtility.ParseQueryString(uri.Query)["url"];
            if (!string.IsNullOrWhiteSpace(inner))
                return Unwrap(inner); // recurse in case of double-wrapping
        }

        // Google redirect
        if ((uri.Host.EndsWith("google.com", StringComparison.OrdinalIgnoreCase) ||
             uri.Host.EndsWith("google.co.uk", StringComparison.OrdinalIgnoreCase)) &&
            uri.AbsolutePath == "/url")
        {
            string? inner = HttpUtility.ParseQueryString(uri.Query)["q"];
            if (!string.IsNullOrWhiteSpace(inner))
                return Unwrap(inner);
        }

        return rawUrl;
    }

    /// <summary>
    /// Extracts the registrable domain (strips leading "www." and any
    /// subdomains when a wildcard rule is being evaluated).
    /// Returns empty string if the URL is invalid.
    /// </summary>
    public static string ExtractDomain(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
            return string.Empty;

        return uri.Host.ToLowerInvariant();
    }

    /// <summary>
    /// Returns the Office protocol URI if the URL points to a known Office
    /// file type (e.g. ms-excel:ofe|u|...), or null if not applicable.
    /// This lets SharePoint documents open directly in desktop Office apps.
    /// </summary>
    public static string? GetOfficeProtocolUri(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
            return null;

        string path = uri.AbsolutePath.ToLowerInvariant();
        string? protocol = null;

        if (path.EndsWith(".xlsx") || path.EndsWith(".xls") || path.EndsWith(".xlsm"))
            protocol = "ms-excel";
        else if (path.EndsWith(".docx") || path.EndsWith(".doc") || path.EndsWith(".docm"))
            protocol = "ms-word";
        else if (path.EndsWith(".pptx") || path.EndsWith(".ppt") || path.EndsWith(".pptm"))
            protocol = "ms-powerpoint";

        if (protocol is null) return null;
        return $"{protocol}:ofe|u|{url}";
    }

    /// <summary>
    /// Checks whether a URL's domain matches a rule pattern.
    ///
    /// Rules:
    ///   "github.com"   matches "github.com" and "www.github.com"
    ///   "*.google.com" matches any subdomain (but not google.com itself)
    /// </summary>
    public static bool DomainMatches(string urlDomain, string rulePattern)
    {
        urlDomain   = urlDomain.ToLowerInvariant();
        rulePattern = rulePattern.ToLowerInvariant();

        if (rulePattern.StartsWith("*."))
        {
            string root = rulePattern[2..]; // e.g. "google.com"
            return urlDomain.EndsWith("." + root);
        }

        // Exact match or www. prefix
        return urlDomain == rulePattern || urlDomain == "www." + rulePattern;
    }
}
