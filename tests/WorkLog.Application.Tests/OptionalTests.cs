using WorkLog.Application.Models;

namespace WorkLog.Application.Tests;

public class OptionalTests
{
    [Fact]
    public void Unspecified_ShouldHaveIsSpecifiedFalse()
    {
        var optional = Optional<int>.Unspecified;

        Assert.False(optional.IsSpecified);
    }

    [Fact]
    public void Specified_ShouldPreserveValue()
    {
        var optional = Optional<int>.Specified(120);

        Assert.True(optional.IsSpecified);
        Assert.Equal(120, optional.Value);
    }

    [Fact]
    public void Specified_ShouldAllowNull()
    {
        var optional = Optional<string>.Specified(null);

        Assert.True(optional.IsSpecified);
        Assert.Null(optional.Value);
    }
}
