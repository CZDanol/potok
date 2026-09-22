namespace Majex.Potok.Core;

public class NoValueException(BaseDynex? offender) : Exception
{
    public BaseDynex? Offender = offender;
}

public class BaseDynex
{
    static protected readonly ThreadLocal<BaseDynex?> _dynexBeingRecomputed = new();

    protected readonly Identifier _id;

    protected HashSet<BaseDynex> _dependants = new();

    protected HashSet<BaseDynex> _dependencies = new();

    protected bool _isDirty = true;

    protected BaseDynex(Identifier id)
    {
        _id = id;
    }

    protected void ReportDepency()
    {
        var dependant = _dynexBeingRecomputed.Value;
        if (dependant != null)
        {
            _dependants.Add(dependant);
            dependant._dependencies.Add(this);
        }
    }

    protected void ClearDependencies()
    {
        foreach (var dependency in _dependencies)
        {
            dependency._dependants.Remove(this);
        }
        _dependencies.Clear();
    }

    protected void Invalidate()
    {
        if (_isDirty)
        {
            return;
        }

        _isDirty = true;
        InvalidateDependencies();
    }

    protected void InvalidateDependencies()
    {
        foreach (var dep in _dependants)
        {
            dep.Invalidate();
        }
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
        ReportDepency();
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
        ClearDependencies();

        var prevRecomputed = _dynexBeingRecomputed.Value;
        try
        {
            _dynexBeingRecomputed.Value = this;
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
            InvalidateDependencies();
        }
    }
}

