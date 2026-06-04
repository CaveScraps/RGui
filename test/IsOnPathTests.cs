using Xunit;

namespace RGui.Tests;

public class IsOnPathTests
{
    [Fact]
    public void ReturnsTrueForCommandThatExists()
    {
        // dotnet is guaranteed to be on PATH since we're running these tests with it
        Assert.True(RGuiUtils.IsOnPath("dotnet"));
    }

    [Fact]
    public void ReturnsFalseForCommandThatDoesNotExist()
    {
        Assert.False(RGuiUtils.IsOnPath("rgui-definitely-not-a-real-command-xyz"));
    }
}
