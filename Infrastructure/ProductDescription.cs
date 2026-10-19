using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Html;

namespace CCMobile.Infrastructure;

// Store Quill data, never arbitrary HTML. Every text/URL is encoded before rendering.
public static class ProductDescription
{
    public const string Prefix = "CCMOBILE-DELTA-V1:";
    public const int MaxTextLength = 20000;
    public const int MaxStorageLength = 200000;
    public const string ValidationMessage = "Mô tả tối đa 20.000 ký tự; nội dung định dạng không hợp lệ hoặc quá phức tạp.";

    public static bool IsValid(string? value) => TryRender(value, out _);

    public static IHtmlContent Render(string? value)
    {
        if (TryRender(value, out var html)) return new HtmlString(html);
        return new HtmlString("<p>Nội dung mô tả đang được cập nhật.</p>");
    }

    private static string Encode(string text) => HtmlEncoder.Default.Encode(text);

    private static bool Flag(JsonElement attributes, string key) =>
        attributes.ValueKind == JsonValueKind.Object &&
        attributes.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.True;

    private static string? TextAttribute(JsonElement attributes, string key) =>
        attributes.ValueKind == JsonValueKind.Object &&
        attributes.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() : null;

    private static bool TryRender(string? value, out string html)
    {
        html = "";
        if (string.IsNullOrEmpty(value)) return true;
        if (value.Length > MaxStorageLength) return false;
        if (!value.StartsWith(Prefix, StringComparison.Ordinal))
        {
            if (value.Length > MaxTextLength) return false;
            html = "<div class=\"cc-description-plain\">" + Encode(value) + "</div>";
            return true;
        }

        try
        {
            using var document = JsonDocument.Parse(value[Prefix.Length..],
                new JsonDocumentOptions { MaxDepth = 8 });
            if (document.RootElement.ValueKind != JsonValueKind.Object ||
                !document.RootElement.TryGetProperty("ops", out var ops) ||
                ops.ValueKind != JsonValueKind.Array || ops.GetArrayLength() > 4000) return false;

            var output = new StringBuilder();
            var line = new StringBuilder();
            string? openList = null;
            var length = 0;

            void FinishLine(JsonElement attributes)
            {
                var listType = TextAttribute(attributes, "list");
                var listTag = listType == "bullet" ? "ul" : listType == "ordered" ? "ol" : null;
                if (openList != listTag)
                {
                    if (openList != null) output.Append("</").Append(openList).Append('>');
                    if (listTag != null) output.Append('<').Append(listTag).Append('>');
                    openList = listTag;
                }
                var tag = "p";
                if (listTag != null) tag = "li";
                else if (attributes.ValueKind == JsonValueKind.Object &&
                    attributes.TryGetProperty("header", out var header) &&
                    header.TryGetInt32(out var level) && level is 2 or 3)
                    tag = "h" + level;
                output.Append('<').Append(tag).Append('>')
                    .Append(line.Length == 0 ? "<br>" : line.ToString())
                    .Append("</").Append(tag).Append('>');
                line.Clear();
            }

            foreach (var op in ops.EnumerateArray())
            {
                if (op.ValueKind != JsonValueKind.Object ||
                    !op.TryGetProperty("insert", out var insert) ||
                    insert.ValueKind != JsonValueKind.String ||
                    op.TryGetProperty("delete", out _) || op.TryGetProperty("retain", out _)) return false;
                var text = insert.GetString() ?? "";
                length += text.Length;
                if (length > MaxTextLength) return false;
                op.TryGetProperty("attributes", out var attributes);
                var pieces = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
                for (var i = 0; i < pieces.Length; i++)
                {
                    var fragment = Encode(pieces[i]);
                    if (fragment.Length > 0)
                    {
                        if (Flag(attributes, "bold")) fragment = "<strong>" + fragment + "</strong>";
                        if (Flag(attributes, "italic")) fragment = "<em>" + fragment + "</em>";
                        var link = TextAttribute(attributes, "link");
                        if (link is { Length: <= 2048 } &&
                            Uri.TryCreate(link, UriKind.Absolute, out var uri) &&
                            uri.Scheme is "https" or "http")
                            fragment = "<a href=\"" + Encode(uri.AbsoluteUri) +
                                "\" rel=\"nofollow noopener noreferrer\">" + fragment + "</a>";
                        line.Append(fragment);
                    }
                    if (i < pieces.Length - 1) FinishLine(attributes);
                }
            }
            if (line.Length > 0) FinishLine(default);
            if (openList != null) output.Append("</").Append(openList).Append('>');
            html = output.ToString();
            return true;
        }
        catch (JsonException) { return false; }
        catch (InvalidOperationException) { return false; }
        catch (FormatException) { return false; }
    }
}
