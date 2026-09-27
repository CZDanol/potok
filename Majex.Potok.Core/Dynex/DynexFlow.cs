namespace Majex.Potok.Core;

public class DynexFlow
{
    public readonly Identifier Root;

    public uint MaxSettlementIterations = 1024;

    internal BaseDynex? DynexBeingRecomputed = null;

    /// <summary>
    /// A value that is different for each top-level <see cref="BaseDynex.Recompute"/> call.
    /// </summary>
    /// <remarks>
    /// Used to identify dependency loops, akin to DFS coloring concept
    /// (visited ~ (<see cref="RecomputeRunID"/> == <see cref="BaseDynex._lastRecomputeRunID"/>)).
    /// </remarks>
    internal uint RecomputeRunID = 0;

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
            DirtyDynexes.Peek().Recompute();

            if (remainingIterations-- == 0)
            {
                throw new DynexFlowConvergenceException();
            }

            DirtyDynexes.Dequeue();
        }
    }

}