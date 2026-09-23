namespace Majex.Potok.Core;

using BaseDynexWeakRef = WeakReference<BaseDynex>;

public class BaseDynex
{
    protected readonly struct Snapshot(BaseDynexWeakRef dynex, uint revision)
    {
        readonly BaseDynexWeakRef Dynex = dynex;
        readonly uint Revision = revision;

        public readonly BaseDynex? ResolveDynex()
        {
            return (Dynex.TryGetTarget(out var dynex) && dynex._revision == Revision) ? dynex : null;
        }
    }
    static protected readonly ThreadLocal<Snapshot?> _dynexBeingRecomputed = new();

    protected readonly Identifier _id;

    /// <summary>
    /// Dynexes that depend on this one.
    /// When this dynex changes value/state,
    /// they need to be invalidated.
    /// </summary>
    protected List<Snapshot> _dependants = [];

    /// <summary>
    /// When _dependants grow to this size,
    /// do a sweep that removes null values.
    /// </summary>
    private int _dependantsSweepOn = 16;

    /// <summary>
    /// True if the dynex is not up-to-date and needs recomputation.
    /// </summary>
    protected bool _isDirty = true;

    protected BaseDynexWeakRef _weakThis;

    /// <remarks>
    /// Changed at the beginning of every recompute.
    /// </remarks>
    protected uint _revision = 0;

    protected BaseDynex(Identifier id)
    {
        _id = id;
        _weakThis = new BaseDynexWeakRef(this);
    }

    /// <summary>
    /// Marks a dynex that is currently being recomputed (if any)
    /// as dependent on this one.
    /// </summary>
    /// <remarks>
    /// To be called only in TryEval.
    /// Used for detecting nested Eval() calls within the dynex evalFunc.
    /// </remarks>
    protected void ReportDependency()
    {
        var dependant = _dynexBeingRecomputed.Value;
        if (dependant == null)
        {
            return;
        }

        if (_dependants.Count >= _dependantsSweepOn)
        {
            SweepDependants(false);
        }

        _dependants.Add(dependant.Value);
    }

    protected void Invalidate()
    {
        if (_isDirty)
        {
            return;
        }

        _isDirty = true;
        InvalidateDependants();
    }

    /// <summary>
    /// Removes null references from the _dependants list to shrink its size.
    /// </summary>
    /// <param name="invalidate">If true, also invalidates all dependants.</param>
    protected void SweepDependants(bool invalidate)
    {
        // Go through all the dependencies and remove all that are not valid anymore
        // Not valid = either collected by the GC or invalidated bcs of Recompute
        int validCount = _dependants.Count;
        int i = 0;
        while (i < validCount)
        {
            BaseDynex? dependant = _dependants[i].ResolveDynex();
            if (dependant == null)
            {
                // Swap remove
                _dependants[i] = _dependants[validCount - 1];
                validCount--;
            }
            else if (invalidate)
            {
                dependant.Invalidate();
                i++;
            }
        }
        _dependants.RemoveRange(validCount, _dependants.Count - validCount);
        _dependantsSweepOn = Math.Max((int)(_dependants.Count * 1.5), 16);
    }

    protected void InvalidateDependants()
    {
        SweepDependants(true);
    }

    public override string ToString()
    {
        return _id.ToString();
    }
}

public class Dynex<T>(Identifier id, Func<T> evalFunc) : BaseDynex(id)
{
    Func<T> _evalFunc = evalFunc;

    T _cachedValue = default!;

    /// <summary>
    /// If not null, denotes that we don't have a value (even if we're not dirty anymore)
    /// but we have this exception instead.
    /// </summary>
    DynexException? _valueException = null;

    public bool TryEval(out T value)
    {
        ReportDependency();
        Recompute();
        if (_valueException == null)
        {
            value = _cachedValue;
            return true;
        }
        else
        {
            value = default!;
            return false;
        }

    }

    public T Eval()
    {
        if (!TryEval(out var result))
        {
            throw _valueException!;
        }
        return result;
    }

    /// <remarks>
    /// MUST stay private. Use RebindableDynex if you want rebinding.
    /// </remarks>
    protected void Rebind(Func<T> evalFunc)
    {
        if (_evalFunc == evalFunc)
        {
            return;
        }

        _evalFunc = evalFunc;
        Invalidate();
    }

    /// <summary>
    /// Ensures that the value is not dirty.
    /// </summary>
    void Recompute()
    {
        if (!_isDirty)
        {
            return;
        }

        T prevValue = _cachedValue;

        _revision++;
        Snapshot? prevRecomputed = _dynexBeingRecomputed.Value;
        _dynexBeingRecomputed.Value = new Snapshot(_weakThis, _revision);
        try
        {
            _valueException = null;
            _cachedValue = _evalFunc();
        }
        catch (DynexException e)
        {
            _valueException = e.Clone();
            _valueException.CallStack.Add(this);
        }
        finally
        {
            _dynexBeingRecomputed.Value = prevRecomputed;
        }

        _isDirty = false;

#if DEBUG
        IDynexDebugger.Instance.Value?.OnRecompute(this);
#endif

        if (!EqualityComparer<T>.Default.Equals(prevValue, _cachedValue))
        {
            InvalidateDependants();
        }
    }
}

