using System.Reflection;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Runtime.Serialization;

namespace Majex.Potok.Core.Dynex;

public class BaseDynex
{
    static protected readonly ThreadLocal<BaseDynex?> _dynexBeingRecomputed = new();

    protected HashSet<BaseDynex> _dependants = new();

    protected HashSet<BaseDynex> _dependencies = new();

    protected bool _isDirty = true;

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
}

public class Dynex<T> : BaseDynex
{
    Func<T> _evalFunc;

    T? _cachedValue;


    public Dynex(Func<T> evalFunc)
    {
        _evalFunc = evalFunc;
    }

    public T? Eval()
    {
        ReportDepency();
        Recompute();
        return _cachedValue;
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
        _dynexBeingRecomputed.Value = this;
        try
        {
            _cachedValue = _evalFunc();
        }
        finally
        {
            _dynexBeingRecomputed.Value = prevRecomputed;
        }

        _isDirty = false;

        if (!EqualityComparer<T>.Default.Equals(prevValue, _cachedValue))
        {
            InvalidateDependencies();
        }
    }
}

