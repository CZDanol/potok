namespace Majex.Potok.Core.Tests;

using Majex.Potok.Core;

public class BasicTests
{
    [Fact]
    public void Sum()
    {
        var flow = new DynexFlow();
        var root = flow.Root;
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
        var flow = new DynexFlow();
        var root = flow.Root;
        var a = new Dynex<float>(root / "a", () => 3);
        var b = new Dynex<float>(root / "b", () => throw DynexNoValueException.BaseInstance);
        var sum = new Dynex<float>(root / "sum", () => a.Eval() + b.Eval());

        Assert.False(b.TryEval(out var _));
        Assert.False(sum.TryEval(out var _));
        Assert.Throws<DynexNoValueException>(() => sum.Eval());
    }

    [Fact]
    public void RebindTest()
    {
        var flow = new DynexFlow();
        var root = flow.Root;
        var a = new Dynex<float>(root / "a", () => 2);
        var b = new Dynex<float>(root / "b", () => 2);

        var sum = new RebindableDynex<float>(root / "sum");
        Assert.Throws<DynexNoValueException>(() => sum.Eval());

        sum.SetValue(3);
        Assert.Equal(3, sum.Eval());

        sum.Rebind(() => a.Eval() + b.Eval());
        Assert.Equal(4, sum.Eval());
    }

    [Fact]
    public void RebindUpdateTest()
    {
        var flow = new DynexFlow();
        var root = flow.Root;
        var a = new RebindableDynex<float>(root / "a", 3);
        var b = new RebindableDynex<float>(root / "b", 4);
        var sum = new Dynex<float>(root / "sum", () => a.Eval() + b.Eval());

        Assert.Equal(7, sum.Eval());
        b.SetValue(8);
        Assert.Equal(11, sum.Eval());
    }

    [Fact]
    public void Nullable()
    {
        var flow = new DynexFlow();
        var root = flow.Root;
        var a = new Dynex<float>(root / "a", () => throw DynexNoValueException.BaseInstance);
        var b = new Dynex<float>(root / "b", () => throw DynexNoValueException.BaseInstance);
        var lt = new Dynex<bool>(root / "lt", () => a.Eval() < b.Eval());
        Assert.False(lt.TryEval(out var _));
    }

    [Fact]
    public void LazyUpdates()
    {
        var debugger = new DynexDebugger();
        IDynexDebugger.Instance.Value = debugger;

        var flow = new DynexFlow();
        var root = flow.Root;
        var a = new RebindableDynex<float>(root / "a", 3);
        var b = new Dynex<float>(root / "b", () =>
        {
            a.Eval();
            return 5;
        });
        var c = new Dynex<float>(root / "c", () => b.Eval());

        Assert.Equal([], debugger.RecomputeLog);

        Assert.Equal(5, c.Eval());
        Assert.Equal([b, c], debugger.RecomputeLog);
        debugger.RecomputeLog.Clear();

        a.SetValue(1);
        Assert.Equal(5, c.Eval());
        Assert.Equal([b], debugger.RecomputeLog);
    }

    [Fact]
    public void CachedValueDefaultConstructor()
    {
        Dynex<float>.CachedValue test = new Dynex<float>.CachedValue();
        Assert.False(test.TryGet(out var _));
        Assert.Throws<DynexNoValueException>(() => test.Get());
    }

    [Fact]
    public void ExceptionEquality()
    {
        var flow = new DynexFlow();
        var root = flow.Root;
        var a = new RebindableDynex<float>(root / "a", 3);
        var b = new RebindableDynex<float>(root / "b", 4);

        Assert.True(DynexException.ExceptionEquals(
            new DynexNoValueException(),
             new DynexNoValueException()
             ));

        Assert.True(DynexException.ExceptionEquals(
            new DynexNoValueException().CloneAndAddCallStackItem(a),
            new DynexNoValueException().CloneAndAddCallStackItem(a)
        ));

        Assert.False(DynexException.ExceptionEquals(
            new DynexNoValueException().CloneAndAddCallStackItem(a),
            new DynexNoValueException().CloneAndAddCallStackItem(b)
        ));
    }
}
