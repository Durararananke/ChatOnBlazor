using ChatOnBlazor.Client.Security;

namespace ChatOnBlazor.Tests;

public sealed class SafeMarkdownTests
{
    [Fact]
    public void ToHtml_RemovesExecutableHtmlAndUnsafeLinks()
    {
        const string markdown = "<img src=x onerror=alert(1)> [bad](javascript:alert(1)) **safe**";

        var html = SafeMarkdown.ToHtml(markdown);

        Assert.DoesNotContain("<img", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("href=", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("javascript:", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("&lt;img", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<strong>safe</strong>", html, StringComparison.Ordinal);
    }
}
