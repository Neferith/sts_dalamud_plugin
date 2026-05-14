using Markdig;

namespace Sts.Web.Helpers;

public static class MarkdownHelper
{
    private const string SplitMarker = "---split---";
    private const string ImageMarker = "---image---";

    private static readonly MarkdownPipeline Pipeline =
        new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();

    public static string Render(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return string.Empty;

        var blocks = content
            .Split(SplitMarker, StringSplitOptions.None)
            .Select(NormalizeBlock)
            .Where(static b => !string.IsNullOrWhiteSpace(b));

        var normalized = string.Join("\n\n---\n\n", blocks);

        return Markdown.ToHtml(normalized, Pipeline);
    }

    private static string NormalizeBlock(string raw)
    {
        var parts = raw.Split(ImageMarker, 2);

        var text = parts[0].Trim('\n', '\r');

        var imageUrl = parts.Length > 1
            ? parts[1].Trim('\n', '\r', ' ')
            : null;

        if (string.IsNullOrWhiteSpace(imageUrl))
            return text;

        return $"{text}\n\n![]({imageUrl})";
    }
}
