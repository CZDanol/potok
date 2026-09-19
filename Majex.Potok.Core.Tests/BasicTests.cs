namespace Majex.Potok.Core.Tests;

using Majex.Potok.Core.Dynex;

public class BasicTests
{
    [Fact]
    public void Sum()
    {
        var a = new Dynex<float>(() => 3);
        Assert.Equal(3, a.Eval());

        var b = new Dynex<float>(() => 4);
        Assert.Equal(4, b.Eval());

        var sum = new Dynex<float>(() => a.Eval() + b.Eval());
        Assert.Equal(7, sum.Eval());
    }

    [Fact]
    public void NullSum()
    {
        var a = new Dynex<float?>(() => 3);
        Assert.Equal(3, a.Eval());

        var b = new Dynex<float?>(() => null);
        Assert.Null(b.Eval());

        var sum = new Dynex<float?>(() => a.Eval() + b.Eval());
        Assert.Null(sum.Eval());
    }
}
