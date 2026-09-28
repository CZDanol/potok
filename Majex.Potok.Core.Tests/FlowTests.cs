namespace Majex.Potok.Core.Tests;

using Majex.Potok.Core;

public class FlowTests
{
    [Fact]
    public void LoopException()
    {
        var flow = new DynexFlow();
        var root = flow.Root;
        var a = new RebindableDynex<float>(root / "a", 3);
        var b = new Dynex<float>(root / "b", () => a.Eval());

        // a will not get recomputed before rebinding
        // and thus will not have any value determined at all
        a.Rebind(() => b.Eval());

        // Neither a or b were ever evaluated and their value depends on each other
        // that means that it's impossible to determine the value
        Assert.Throws<DynexLoopException>(() => a.Eval());
        Assert.Throws<DynexLoopException>(() => b.Eval());

        Assert.False(a.TryEval(out var _));
        Assert.False(b.TryEval(out var _));
    }

    [Fact]
    public void HidenLoopException()
    {
        var flow = new DynexFlow();
        var root = flow.Root;

        var c = new RebindableDynex<float>(root / "c");
        var a = new RebindableDynex<float>(root / "a", 3);
        var b = new Dynex<float>(root / "b", () => a.Eval());

        a.Rebind(() => b.Eval());
        c.Rebind(() => b.Eval());

        var ce = Assert.Throws<DynexLoopException>(() => c.Eval());
        Assert.Equal([a, b], ce.Loop);

        var ae = Assert.Throws<DynexLoopException>(() => a.Eval());
        Assert.Equal([a, b], ae.Loop);

        var be = Assert.Throws<DynexLoopException>(() => a.Eval());
        Assert.Equal([a, b], be.Loop);
    }

    [Fact]
    public void LoopExceptionAvoidedWithPreSettle()
    {
        var flow = new DynexFlow();
        var root = flow.Root;
        var a = new RebindableDynex<float>(root / "a", 3);
        var b = new Dynex<float>(root / "b", () => a.Eval());

        // a will get recomputed before rebinding
        // that means that it will get a cached value of 3
        flow.Settle();

        a.Rebind(() => b.Eval());

        // We have a dependency loop here as well, but "b" was pre-settled on 3,
        // so a will get settled on 3 as well and the flow stabilizes immediately.
        Assert.Equal(3, a.Eval());
    }

    [Fact]
    public void AcyclicBatchedUpdateIsNotALoop()
    {
        var flow = new DynexFlow();
        var root = flow.Root;

        var a = new RebindableDynex<float>(root / "a", 1);
        var b = new RebindableDynex<float>(root / "b", 1);
        var c = new RebindableDynex<float>(root / "c", 1);

        var d = new Dynex<float>(root / "d", () => a.Eval());
        var e = new Dynex<float>(root / "e", () => d.Eval());
        var f = new Dynex<float>(root / "f", () => b.Eval() + (e.Eval() >= 10 ? 100 : 0));

        // Evaluation order inside the lambda matters (C# evaluates left to right).
        var g = new Dynex<float>(root / "g",
            () => f.Eval() + d.Eval() + e.Eval() + f.Eval() + c.Eval());

        // Initial settle: d = 1, e = 1, f = 1, g = 1 + 1 + 1 + 1 + 1
        Assert.Equal(5, g.Eval());

        // Batched updates - queue order becomes [g, f, d]
        c.SetValue(2);
        b.SetValue(2);
        a.SetValue(2);

        // Expected: d = 2, e = 2, f = 2, g = 2 + 2 + 2 + 2 + 2
        Assert.Equal(10, g.Eval());
    }

    [Fact]
    public void ConvergenceFailure()
    {
        var flow = new DynexFlow();
        var root = flow.Root;
        var a = new RebindableDynex<float>(root / "a", 3);
        var b = new Dynex<float>(root / "b", () => a.Eval());
        var c = new Dynex<float>(root / "c", () => b.Eval());
        Assert.Equal(3, c.Eval());

        a.Rebind(() => c.Eval() + 1);

        Assert.Throws<DynexFlowConvergenceException>(() => c.Eval());
        Assert.False(c.TryEval(out var _));
    }
}