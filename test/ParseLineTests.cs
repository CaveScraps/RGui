using System.IO;
using Xunit;

namespace RGui.Tests;

public class ParseLineTests
{
    private const string SearchRoot = "/search/root";

    [Fact]
    public void ReturnsNullForPlainTextLine()
    {
        var result = RGuiUtils.ParseLine("just a plain text line", SearchRoot);

        Assert.Null(result);
    }

    [Fact]
    public void ReturnsNullForUnrecognisedJson()
    {
        var result = RGuiUtils.ParseLine("""{"type":"begin","data":{}}""", SearchRoot);

        Assert.Null(result);
    }

    [Fact]
    public void ParsesValidMatchCorrectly()
    {
        var line = """{"type":"match","data":{"path":{"text":"/search/root/src/file.cs"},"line_number":42,"lines":{"text":"    var x = 1;\n"}}}""";

        var result = RGuiUtils.ParseLine(line, SearchRoot);

        Assert.NotNull(result);
        Assert.Equal("/search/root/src/file.cs", result.FilePath);
        Assert.Equal(42, result.LineNumber);
        Assert.Equal($"{Path.Combine("src", "file.cs")}:42", result.PathPart);
        Assert.Equal("    var x = 1;", result.MatchText);
    }

    [Fact]
    public void TrimsTrailingWhitespaceFromMatchText()
    {
        var line = """{"type":"match","data":{"path":{"text":"/search/root/file.cs"},"line_number":1,"lines":{"text":"some text   \n"}}}""";

        var result = RGuiUtils.ParseLine(line, SearchRoot);

        Assert.NotNull(result);
        Assert.Equal("some text", result.MatchText);
    }

    [Fact]
    public void DecodesNonUtf8MatchTextFromBytes()
    {
        // ripgrep emits base64 "bytes" instead of "text" when the matched line
        // isn't valid UTF-8. "YWJjgA==" is the bytes 61 62 63 80 ("abc" + a lone
        // 0x80), which decodes to "abc" followed by the U+FFFD replacement char.
        var line = """{"type":"match","data":{"path":{"text":"/search/root/file.cs"},"line_number":7,"lines":{"bytes":"YWJjgA=="}}}""";

        var result = RGuiUtils.ParseLine(line, SearchRoot);

        Assert.NotNull(result);
        Assert.Equal(7, result.LineNumber);
        Assert.Equal("abc�", result.MatchText);
    }

    [Fact]
    public void DecodesNonUtf8PathFromBytes()
    {
        // Non-UTF-8 file names arrive as base64 "bytes" too. "L3NlYXJjaC9yb290L4Bm"
        // is "/search/root/" + 0x80 + "f", which must still yield a result rather
        // than being silently dropped.
        var line = """{"type":"match","data":{"path":{"bytes":"L3NlYXJjaC9yb290L4Bm"},"line_number":1,"lines":{"text":"x\n"}}}""";

        var result = RGuiUtils.ParseLine(line, SearchRoot);

        Assert.NotNull(result);
        Assert.Equal("/search/root/�f", result.FilePath);
    }
}
