using Markdig;

namespace MSLearnMCPChatbot.Services;

/// <summary>
/// Converts Markdown content to sanitized HTML for rendering in the chat UI.
/// </summary>
public static class MarkdownService
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    public static string ToHtml(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;

        return Markdown.ToHtml(markdown, Pipeline);
    }
}
