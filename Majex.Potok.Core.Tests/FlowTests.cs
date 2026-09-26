namespace Majex.Potok.Core.Tests;

using Majex.Potok.Core;

public class FlowTests
{
    [Fact]
    public void DependencyLoop()
    {
        var flow = new DynexFlow();
        var root = flow.Root;
        var a = new RebindableDynex<float>(root / "a", 3);
        var b = new Dynex<float>(root / "b", () => a.Eval());
        a.Rebind(() => b.Eval());

        Assert.Throws<DynexLoopException>(() => a.Eval());
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