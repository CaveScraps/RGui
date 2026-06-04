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
}
