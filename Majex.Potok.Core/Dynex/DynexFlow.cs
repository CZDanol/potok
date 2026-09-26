namespace Majex.Potok.Core;

public class DynexFlow
{
    public readonly Identifier Root;

    internal BaseDynex? DynexBeingRecomputed = null;

    internal readonly Queue<BaseDynex> DirtyDynexes = new();

    public DynexFlow()
    {
        Root = new Identifier(this);
    }

    /// <summary>
    /// Settles the flow, makes sure that no dynexes are dirty.
    /// </summary>
    public void Settle()
    {
        while (DirtyDynexes.Count > 0)
        {
            DirtyDynexes.Dequeue().Recompute();
        }
    }

}