namespace Majex.Potok.Core.Tests;

using Majex.Potok.Core;

public class BasicTests
{
    [Fact]
    public void Sum()
    {
        var root = Identifier.Root;
        var a = new Dynex<float>(root / "a", () => 3);
        var b = new Dynex<float>(root / "b", () => 4);
        var sum = new Dynex<float>(root / "sum", () => a.Eval() + b.Eval());

        Assert.Equal(3, a.Eval());
        Assert.Equal(4, b.Eval());
        Assert.Equal(7, sum.Eval());
    }

    [Fact]
    public void NullSum()
    {
        var root = Identifier.Root;
        var a = new Dynex<float?>(root / "a", () => 3);
        var b = new Dynex<float?>(root / "b", () => null);
        var sum = new Dynex<float?>(root / "sum", () => a.Eval() + b.Eval());

        Assert.Null(b.Eval());
        Assert.Null(sum.Eval());
    }

    [Fact]
    public void RebindTest()
    {
        var root = Identifier.Root;
        var a = new Dynex<float?>(root / "a", () => 2);
        var b = new Dynex<float?>(root / "b", () => 2);

        var sum = new RebindableDynex<float?>(root / "sum");
        Assert.Null(sum.Eval());

        sum.Rebind(3);
        Assert.Equal(3, sum.Eval());

        sum.Rebind(() => a.Eval() + b.Eval());
        Assert.Equal(4, sum.Eval());
    }

    [Fact]
    public void RebindUpdateTest()
    {
        var root = Identifier.Root;
        var a = new RebindableDynex<float?>(root / "a", 3);
        var b = new RebindableDynex<float?>(root / "b", 4);
        var sum = new Dynex<float?>(root / "sum", () => a.Eval() + b.Eval());

        Assert.Equal(7, sum.Eval());
        b.Rebind(8);
        Assert.Equal(11, sum.Eval());
    }
}
