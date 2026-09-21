using WpfHarness.Harness.Abstractions;
using WpfHarness.Harness.Models;
using WpfHarness.Harness.Storage;
using WpfHarness.Harness.Tools;

namespace WpfHarness.Harness.Tests;

public class SessionTests
{
    [Theory]
    [InlineData("", "新任务")]
    [InlineData("   ", "新任务")]
    [InlineData("你好", "你好")]
    [InlineData("这是一个刚好十八字的标题哦哦哦", "这是一个刚好十八字的标题哦哦哦")]
    public void DeriveTitle_ShortOrEmpty_ReturnsAsIs(string input, string expected) =>
        Assert.Equal(expected, Session.DeriveTitle(input));

    [Fact]
    public void DeriveTitle_TooLong_TruncatesTo18WithEllipsis()
    {
        var title = Session.DeriveTitle("这是一段非常非常长的用户输入内容用于验证截断逻辑");
        Assert.EndsWith("…", title);
        Assert.Equal(19, title.Length);
    }

    [Fact]
    public void DeriveTitle_Newlines_FlattenedToSpaces() =>
        Assert.Equal("第一行 第二行", Session.DeriveTitle("第一行\r\n第二行"));
}
