namespace Majex.Potok.Core;

using DynexDependencyToken = WeakReference<BaseDynex?>;

public class NoValueException(BaseDynex? offender) : Exception
{
    public BaseDynex? Offender = offender;
}

public class BaseDynex
{
    static protected readonly ThreadLocal<DynexDependencyToken?> _dynexBeingRecomputed = new();

    protected readonly Identifier _id;

    /// <summary>
    /// Dynexes that depend on this one.
    /// When this dynex changes value/state,
    /// they need to be invalidated.
    /// </summary>
    protected List<DynexDependencyToken> _dependants = new();

    /// <summary>
    /// When _dependants grow to this size,
    /// do a sweep that removes null values.
    /// </summary>
    private int _dependantsSweepOn = 16;

    /// <summary>
    /// True if the dynex is not up-to-date and needs recomputation.
    /// </summary>
    protected bool _isDirty = true;

    /// <summary>
    /// Weak reference to the current state of this dynex.
    /// </summary>
    /// <remarks>
    /// When the state changes (before every recompute),
    /// it gets invalidated and a new token is generated.
    /// 
    /// Dependency tracking MUST be realized through this token,
    /// otherwise things will break.
    /// </remarks>
    protected DynexDependencyToken _dependencyToken;

    protected BaseDynex(Identifier id)
    {
        _id = id;
        _dependencyToken = new DynexDependencyToken(this);
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

        _dependants.Add(dependant);
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
            if (!_dependants[i].TryGetTarget(out var dependant))
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
    /// Provided that !_isDirty, denotes whether _cachedValue is a valid value.
    /// </summary>
    /// <remarks>
    /// Used to denote situations where the value cannot be provided.
    /// Akin to null, but nullables don't play well with generics
    /// (and expression evaluation in general).
    /// </remarks>
    bool _hasValue = false;

    public bool TryEval(out T value)
    {
        ReportDependency();
        Recompute();
        if (_hasValue)
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
            throw new NoValueException(this);
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

        var prevValue = _cachedValue;
        var prevRecomputed = _dynexBeingRecomputed.Value;
        try
        {
            // Invalidate the old token and issue a new one
            _dependencyToken.SetTarget(null);
            _dependencyToken = new DynexDependencyToken(this);
            _dynexBeingRecomputed.Value = _dependencyToken;
            _hasValue = false;
            _cachedValue = _evalFunc();
            _hasValue = true;
        }
        catch (NoValueException)
        {
            // _hasValue stays false
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

