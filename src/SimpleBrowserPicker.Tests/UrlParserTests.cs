using SimpleBrowserPicker.Services;
using Xunit;

namespace SimpleBrowserPicker.Tests;

public class UrlParserTests
{
    // -----------------------------------------------------------------------
    // Existing domain-only behaviour — must not regress
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("https://github.com/foo",        "github.com",     true)]
    [InlineData("https://www.github.com/foo",    "github.com",     true)]
    [InlineData("https://gist.github.com/foo",   "github.com",     false)]
    [InlineData("https://gist.github.com/foo",   "*.github.com",   true)]
    [InlineData("https://github.com/foo",        "*.github.com",   false)]
    [InlineData("https://notgithub.com/foo",     "github.com",     false)]
    public void DomainOnly_MatchesSameAsBefore(string url, string pattern, bool expected)
    {
        Assert.Equal(expected, UrlParser.UrlMatches(url, pattern));
    }

    [Theory]
    // Path prefix (no wildcard)
    [InlineData("https://github.com/gist/abc",      "github.com/gist",    true)]
    [InlineData("https://github.com/gist/abc/def",  "github.com/gist",    true)]
    [InlineData("https://github.com/orgs/foo",      "github.com/gist",    false)]
    // Path wildcard
    [InlineData("https://github.com/gist/abc",      "github.com/gist/*",  true)]
    [InlineData("https://github.com/orgs/foo",      "github.com/gist/*",  false)]
    // Trailing-slash patterns
    [InlineData("https://github.com/gist/abc",      "github.com/gist/",   true)]
    [InlineData("https://github.com/gist",          "github.com/gist",    true)]
    // Subdomain wildcard + path
    [InlineData("https://maps.google.com/maps/dir", "*.google.com/maps",  true)]
    [InlineData("https://maps.google.com/search",   "*.google.com/maps",  false)]
    // Domain mismatch, path would match
    [InlineData("https://notgithub.com/gist/abc",   "github.com/gist",    false)]
    // Wildcard requires a segment after the slash — use prefix pattern (no *) to also match the bare path
    [InlineData("https://github.com/gist",          "github.com/gist/*",  false)]
    public void PathPattern_MatchesCorrectly(string url, string pattern, bool expected)
    {
        Assert.Equal(expected, UrlParser.UrlMatches(url, pattern));
    }

    // -----------------------------------------------------------------------
    // Unwrap - SafeLinks, Google redirects, depth limit
    // -----------------------------------------------------------------------

    [Fact]
    public void Unwrap_PlainUrl_ReturnsUnchanged()
    {
        Assert.Equal("https://example.com/page", UrlParser.Unwrap("https://example.com/page"));
    }

    [Fact]
    public void Unwrap_SafeLinks_ExtractsInnerUrl()
    {
        string safeLink = "https://eur01.safelinks.protection.outlook.com/?url=https%3A%2F%2Fexample.com%2Fpage&data=abc";
        Assert.Equal("https://example.com/page", UrlParser.Unwrap(safeLink));
    }

    [Fact]
    public void Unwrap_DoubleWrappedSafeLinks_UnwrapsBoth()
    {
        string inner = Uri.EscapeDataString("https://example.com/final");
        string outer = Uri.EscapeDataString($"https://eur01.safelinks.protection.outlook.com/?url={inner}&data=abc");
        string doubleWrapped = $"https://nam02.safelinks.protection.outlook.com/?url={outer}&data=xyz";
        Assert.Equal("https://example.com/final", UrlParser.Unwrap(doubleWrapped));
    }

    [Fact]
    public void Unwrap_GoogleRedirect_ExtractsInnerUrl()
    {
        string redirect = "https://www.google.com/url?q=https%3A%2F%2Fexample.com%2Fpage&sa=D";
        Assert.Equal("https://example.com/page", UrlParser.Unwrap(redirect));
    }

    [Theory]
    [InlineData("https://www.google.de/url?q=https%3A%2F%2Fexample.com&sa=D")]
    [InlineData("https://www.google.co.uk/url?q=https%3A%2F%2Fexample.com&sa=D")]
    [InlineData("https://www.google.com.au/url?q=https%3A%2F%2Fexample.com&sa=D")]
    public void Unwrap_GoogleRegionalRedirect_ExtractsInnerUrl(string redirect)
    {
        Assert.Equal("https://example.com", UrlParser.Unwrap(redirect));
    }

    [Fact]
    public void Unwrap_InvalidUrl_ReturnsOriginal()
    {
        Assert.Equal("not-a-url", UrlParser.Unwrap("not-a-url"));
    }

    [Fact]
    public void Unwrap_SafeLinks_NoUrlParam_ReturnsOriginal()
    {
        string safeLink = "https://eur01.safelinks.protection.outlook.com/?data=abc";
        Assert.Equal(safeLink, UrlParser.Unwrap(safeLink));
    }

    // -----------------------------------------------------------------------
    // ExtractDomain
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("https://www.example.com/page", "www.example.com")]
    [InlineData("https://EXAMPLE.COM/PAGE",     "example.com")]
    [InlineData("http://sub.domain.co.uk/",     "sub.domain.co.uk")]
    [InlineData("not-a-url",                    "")]
    public void ExtractDomain_ReturnsLowercaseHost(string url, string expected)
    {
        Assert.Equal(expected, UrlParser.ExtractDomain(url));
    }

    // -----------------------------------------------------------------------
    // GetOfficeProtocolUri
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("https://sharepoint.com/doc.xlsx", "ms-excel:ofe|u|https://sharepoint.com/doc.xlsx")]
    [InlineData("https://sharepoint.com/doc.xls",  "ms-excel:ofe|u|https://sharepoint.com/doc.xls")]
    [InlineData("https://sharepoint.com/doc.xlsm", "ms-excel:ofe|u|https://sharepoint.com/doc.xlsm")]
    [InlineData("https://sharepoint.com/doc.docx", "ms-word:ofe|u|https://sharepoint.com/doc.docx")]
    [InlineData("https://sharepoint.com/doc.doc",  "ms-word:ofe|u|https://sharepoint.com/doc.doc")]
    [InlineData("https://sharepoint.com/doc.pptx", "ms-powerpoint:ofe|u|https://sharepoint.com/doc.pptx")]
    [InlineData("https://sharepoint.com/doc.ppt",  "ms-powerpoint:ofe|u|https://sharepoint.com/doc.ppt")]
    public void GetOfficeProtocolUri_KnownExtensions_ReturnsProtocol(string url, string expected)
    {
        Assert.Equal(expected, UrlParser.GetOfficeProtocolUri(url));
    }

    [Theory]
    [InlineData("https://example.com/page.html")]
    [InlineData("https://example.com/page.pdf")]
    [InlineData("https://example.com/")]
    [InlineData("not-a-url")]
    public void GetOfficeProtocolUri_NonOffice_ReturnsNull(string url)
    {
        Assert.Null(UrlParser.GetOfficeProtocolUri(url));
    }
}
