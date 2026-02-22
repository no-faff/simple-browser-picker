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
}
