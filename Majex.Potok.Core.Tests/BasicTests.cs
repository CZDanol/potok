namespace Majex.Potok.Core.Tests;

using Majex.Potok.Core.Dynex;
using Majex.Potok.Core.Dynex.RebindableDynex;

public class BasicTests
{
    [Fact]
    public void Sum()
    {
        var a = new Dynex<float>(() => 3);
        var b = new Dynex<float>(() => 4);
        var sum = new Dynex<float>(() => a.Eval() + b.Eval());

        Assert.Equal(3, a.Eval());
        Assert.Equal(4, b.Eval());
        Assert.Equal(7, sum.Eval());
    }

    [Fact]
    public void NullSum()
    {
        var a = new Dynex<float?>(() => 3);
        var b = new Dynex<float?>(() => null);
        var sum = new Dynex<float?>(() => a.Eval() + b.Eval());

        Assert.Null(b.Eval());
        Assert.Null(sum.Eval());
    }

    [Fact]
    public void RebindTest()
    {
        var a = new Dynex<float?>(() => 2);
        var b = new Dynex<float?>(() => 2);

        var sum = new RebindableDynex<float?>();
        Assert.Null(sum.Eval());

        sum.Rebind(3);
        Assert.Equal(3, sum.Eval());

        sum.Rebind(() => a.Eval() + b.Eval());
        Assert.Equal(4, sum.Eval());
    }

    [Fact]
    public void RebindUpdateTest()
    {
        var a = new RebindableDynex<float?>(3);
        var b = new RebindableDynex<float?>(4);
        var sum = new Dynex<float?>(() => a.Eval() + b.Eval());

        Assert.Equal(7, sum.Eval());
        b.Rebind(8);
        Assert.Equal(11, sum.Eval());
    }
}
