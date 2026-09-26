namespace Majex.Potok.Core;

public class DynexFlow
{
    public readonly Identifier Root;

    public uint MaxSettlementIterations = 1024;

    internal BaseDynex? DynexBeingRecomputed = null;

    internal BaseDynex? TopDynexBeingRecomputed = null;

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
        uint remainingIterations = MaxSettlementIterations;

        while (DirtyDynexes.Count > 0)
        {
            BaseDynex dynex = DirtyDynexes.Dequeue();
            TopDynexBeingRecomputed = dynex;
            dynex.Recompute();

            if (remainingIterations-- == 0)
            {
                throw new DynexFlowConvergenceException();
            }
        }

        TopDynexBeingRecomputed = null;
    }

}