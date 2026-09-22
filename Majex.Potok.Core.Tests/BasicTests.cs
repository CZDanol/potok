namespace Majex.Potok.Core.Tests;

using Majex.Potok.Core;
using Microsoft.VisualStudio.TestPlatform.Utilities;

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
        var a = new Dynex<float>(root / "a", () => 3);
        var b = new Dynex<float>(root / "b", () => throw new NoValueException(null));
        var sum = new Dynex<float>(root / "sum", () => a.Eval() + b.Eval());

        Assert.False(b.TryEval(out var _));
        Assert.False(sum.TryEval(out var _));
        Assert.Throws<NoValueException>(() => sum.Eval());
    }

    [Fact]
    public void RebindTest()
    {
        var root = Identifier.Root;
        var a = new Dynex<float>(root / "a", () => 2);
        var b = new Dynex<float>(root / "b", () => 2);

        var sum = new RebindableDynex<float>(root / "sum");
        Assert.Throws<NoValueException>(() => sum.Eval());

        sum.Rebind(3);
        Assert.Equal(3, sum.Eval());

        sum.Rebind(() => a.Eval() + b.Eval());
        Assert.Equal(4, sum.Eval());
    }

    [Fact]
    public void RebindUpdateTest()
    {
        var root = Identifier.Root;
        var a = new RebindableDynex<float>(root / "a", 3);
        var b = new RebindableDynex<float>(root / "b", 4);
        var sum = new Dynex<float>(root / "sum", () => a.Eval() + b.Eval());

        Assert.Equal(7, sum.Eval());
        b.Rebind(8);
        Assert.Equal(11, sum.Eval());
    }

    [Fact]
    public void Nullable()
    {
        var root = Identifier.Root;
        var a = new Dynex<float>(root / "a", () => throw new NoValueException(null));
        var b = new Dynex<float>(root / "b", () => throw new NoValueException(null));
        var lt = new Dynex<bool>(root / "lt", () => a.Eval() < b.Eval());
        Assert.False(lt.TryEval(out var _));
    }

    [Fact]
    public void LazyUpdates()
    {
        var debugger = new DynexDebugger();
        IDynexDebugger.Instance.Value = debugger;

        var root = Identifier.Root;
        var a = new RebindableDynex<float>(root / "a", 3);
        var b = new Dynex<float>(root / "b", () =>
        {
            a.Eval();
            return 5;
        });
        var c = new Dynex<float>(root / "c", () => b.Eval());

        Assert.Equal([], debugger.RecomputeLog);

        Assert.Equal(5, c.Eval());
        Assert.Equal([a, b, c], debugger.RecomputeLog);
        debugger.RecomputeLog.Clear();

        a.Rebind(1);
        Assert.Equal(5, c.Eval());
        Assert.Equal([a, b], debugger.RecomputeLog);
    }
}
