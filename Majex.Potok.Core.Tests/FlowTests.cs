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

        // preSettle = false -> a will not get recomputed before rebinding
        // and thus will not have any value determined at all
        a.Rebind(() => b.Eval(), preSettle: false);

        // Neither a or b were ever evaluated and their value depends on each other
        // that means that it's impossible to determine the value
        Assert.Throws<DynexLoopException>(() => a.Eval());
        Assert.Throws<DynexLoopException>(() => b.Eval());
    }

    [Fact]
    public void HidenLoopException()
    {
        var flow = new DynexFlow();
        var root = flow.Root;

        var c = new RebindableDynex<float>(root / "c");
        var a = new RebindableDynex<float>(root / "a", 3);
        var b = new Dynex<float>(root / "b", () => a.Eval());

        a.Rebind(() => b.Eval(), preSettle: false);
        c.Rebind(() => b.Eval(), preSettle: false);
        Assert.Throws<DynexLoopException>(() => c.Eval());
    }

    [Fact]
    public void LoopExceptionAvoidedWithPreSettle()
    {
        var flow = new DynexFlow();
        var root = flow.Root;
        var a = new RebindableDynex<float>(root / "a", 3);
        var b = new Dynex<float>(root / "b", () => a.Eval());

        // preSettle = true (default for Rebind), so a will get recomputed before rebinding
        // that means that it will get a cached value of 3
        a.Rebind(() => b.Eval());

        // We have a dependency loop here as well, but "b" was pre-settled on 3,
        // so a will get settled on 3 as well and the flow stabilizes immediately.
        Assert.Equal(3, a.Eval());
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
    }
}