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

    protected List<DynexDependencyToken> _dependants = new();

    private int _dependantsSweepOn = 16;

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

public class Dynex<T> : BaseDynex
{
    Func<T> _evalFunc;

    T _cachedValue = default!;

    bool _hasValue = false;


    public Dynex(Identifier id, Func<T> evalFunc) : base(id)
    {
        _evalFunc = evalFunc;
    }

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

    protected void Rebind(Func<T> evalFunc)
    {
        if (_evalFunc == evalFunc)
        {
            return;
        }

        _evalFunc = evalFunc;
        Invalidate();
    }

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

