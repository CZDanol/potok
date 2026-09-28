using System.Diagnostics;

namespace Majex.Potok.Core;

public class DynexFlow
{
    public readonly Identifier Root;

    /// <summary>
    /// How many times each dynex can be recomputed within a <see cref="Settle"/>
    /// before the flow is deemed non-converging.
    /// </summary>
    public uint MaxDynexSettlementIterations = 16;

    internal BaseDynex? DynexBeingRecomputed = null;

    /// <summary>
    /// A value that is different for each top-level <see cref="BaseDynex.Recompute"/> call.
    /// </summary>
    /// <remarks>
    /// Used to identify dependency loops, akin to DFS coloring concept
    /// (visited ~ (<see cref="RecomputeRunID"/> == <see cref="BaseDynex._lastRecomputeRunID"/>)).
    /// </remarks>
    internal uint RecomputeRunID = 0;

    /// <summary>
    /// A value that is different for each <see cref="Settle"/> call.
    /// 0 is there is no <see cref="Settle"/> running. 
    /// </summary>
    internal uint SettleRunID => _settleRunID;

    public bool IsSettling => _settleRunID != 0;

    private uint _settleRunID = 0;

    private uint _lastSettleRunID = 0;

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
        if (IsSettling)
        {
            throw new InvalidOperationException("DynexFlow.Settle nesting");
        }

        try
        {
            _settleRunID = _lastSettleRunID + 1;
            _lastSettleRunID = _settleRunID;

            while (DirtyDynexes.Count > 0)
            {
                DirtyDynexes.Peek().Recompute();

                // Defer dequeue in case the Recompute throws
                DirtyDynexes.Dequeue();
            }
        }
        finally
        {
            _settleRunID = 0;
        }
    }

}