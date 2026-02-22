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
}
