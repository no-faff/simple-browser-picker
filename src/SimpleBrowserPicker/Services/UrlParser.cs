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
        return Unwrap(rawUrl, maxDepth: 10);
    }

    private static string Unwrap(string rawUrl, int maxDepth)
    {
        if (maxDepth <= 0)
            return rawUrl;

        if (!Uri.TryCreate(rawUrl, UriKind.Absolute, out Uri? uri))
            return rawUrl;

        // Outlook SafeLinks
        if (uri.Host.EndsWith("safelinks.protection.outlook.com", StringComparison.OrdinalIgnoreCase))
        {
            string? inner = HttpUtility.ParseQueryString(uri.Query)["url"];
            if (!string.IsNullOrWhiteSpace(inner))
                return Unwrap(inner, maxDepth - 1);
        }

        // Google redirect (all regional variants: google.com, google.co.uk, google.de, etc.)
        if (uri.Host.Contains("google.", StringComparison.OrdinalIgnoreCase) &&
            uri.AbsolutePath == "/url")
        {
            string? inner = HttpUtility.ParseQueryString(uri.Query)["q"];
            if (!string.IsNullOrWhiteSpace(inner))
                return Unwrap(inner, maxDepth - 1);
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

    /// <summary>
    /// Returns true if <paramref name="url"/> matches <paramref name="pattern"/>.
    ///
    /// Pattern syntax:
    ///   "github.com"          — domain-only, matches github.com and www.github.com
    ///   "*.google.com"        — subdomain wildcard, matches any subdomain of google.com
    ///   "github.com/gist"     — domain + path prefix, matches any URL under /gist
    ///   "github.com/gist/*"   — domain + path wildcard, same but explicit
    ///   "*.corp.com/internal" — combined subdomain wildcard + path prefix
    /// </summary>
    public static bool UrlMatches(string url, string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            return false;

        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
            return false;

        pattern = pattern.ToLowerInvariant();

        int slashIdx = pattern.IndexOf('/');
        if (slashIdx < 0)
        {
            // Domain-only pattern — existing behaviour
            return DomainMatches(uri.Host.ToLowerInvariant(), pattern);
        }

        string domainPart = pattern[..slashIdx];
        string pathPart   = pattern[slashIdx..]; // includes the leading /

        if (!DomainMatches(uri.Host.ToLowerInvariant(), domainPart))
            return false;

        return PathMatches(uri.AbsolutePath.ToLowerInvariant(), pathPart);
    }

    private static bool PathMatches(string urlPath, string pathPattern)
    {
        if (!pathPattern.Contains('*'))
            return urlPath.StartsWith(pathPattern, StringComparison.Ordinal);

        // Convert wildcard pattern to regex: escape, then replace \* with .*
        string regexPattern = "^" +
            System.Text.RegularExpressions.Regex.Escape(pathPattern)
                .Replace(@"\*", ".*") +
            "$";
        return System.Text.RegularExpressions.Regex.IsMatch(
            urlPath, regexPattern, System.Text.RegularExpressions.RegexOptions.None);
    }

}
