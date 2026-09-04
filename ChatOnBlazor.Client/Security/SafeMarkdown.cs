using Ganss.Xss;
using Markdig;

namespace ChatOnBlazor.Client.Security;

public static class SafeMarkdown
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .DisableHtml()
        .Build();

    public static string ToHtml(string? markdown)
    {
        var generatedHtml = Markdown.ToHtml(markdown ?? string.Empty, Pipeline);
        return new HtmlSanitizer().Sanitize(generatedHtml);
    }
}
