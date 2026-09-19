namespace Majex.Potok.Core;

#if DEBUG
public interface IDynexDebugger
{
    static readonly ThreadLocal<IDynexDebugger?> Instance = new();

    void OnRecompute(BaseDynex dynex) { }
}

public class DynexDebugger : IDynexDebugger
{
    public readonly List<BaseDynex> RecomputeLog = new();

    public void OnRecompute(BaseDynex dynex)
    {
        RecomputeLog.Add(dynex);
    }
}
#endif