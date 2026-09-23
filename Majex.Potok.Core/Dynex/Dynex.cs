namespace Majex.Potok.Core;

using System.Diagnostics;
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

    readonly record struct CachedValue
    {
        readonly T Value = default!;

        /// <summary>
        /// If not null, denotes that we don't have a value (even if we're not dirty anymore)
        /// but we have this exception instead.
        /// </summary>
        readonly DynexException? Exception = DynexNoValueException.BaseInstance;

        public CachedValue(T value)
        {
            Value = value;
            Exception = null;
        }

        public CachedValue(DynexException exception)
        {
            Exception = exception;
        }

        public bool TryGet(out T value)
        {
            value = Value;
            return Exception == null;
        }

        public T Get()
        {
            if (Exception != null)
            {
                throw Exception;
            }
            else
            {
                return Value;
            }
        }

        bool IEquatable<CachedValue>.Equals(CachedValue other)
        {
            return EqualityComparer<T>.Default.Equals(Value, other.Value)
                && DynexException.ExceptionEquals(Exception, other.Exception);
        }
    }

    CachedValue _cachedValue = default;

    public bool TryEval(out T value)
    {
        ReportDependency();
        Recompute();
        return _cachedValue.TryGet(out value);
    }

    public T Eval()
    {
        ReportDependency();
        Recompute();
        return _cachedValue.Get();
    }

    protected static readonly Func<T> _noValueFunc = () => throw DynexNoValueException.BaseInstance;
    protected static readonly Func<T> _unreachabelFunc = () => throw new UnreachableException();

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

    protected void SetValue(T value)
    {
        var newValue = new CachedValue(value);
        bool emitValueChange = (_cachedValue != newValue);

        // Invalidate potential dependencies from previous state
        _revision++;

        // _evalFunc must never get called,
        // there is no way for this dynex to get dirty,
        // because it's just a static value with no dependencies.
        _evalFunc = _unreachabelFunc;
        _isDirty = false;

        _cachedValue = newValue;

        if (emitValueChange)
        {
            InvalidateDependants();
        }
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

        CachedValue prevValue = _cachedValue;

        _revision++;
        Snapshot? prevRecomputed = _dynexBeingRecomputed.Value;
        _dynexBeingRecomputed.Value = new Snapshot(_weakThis, _revision);
        try
        {
            _cachedValue = new CachedValue(_evalFunc());
        }
        catch (DynexException e)
        {
            _cachedValue = new CachedValue(e.CloneAndAddCallStackItem(this));
        }
        finally
        {
            _dynexBeingRecomputed.Value = prevRecomputed;
        }

        _isDirty = false;

#if DEBUG
        IDynexDebugger.Instance.Value?.OnRecompute(this);
#endif

        if (!_cachedValue.Equals(prevValue))
        {
            InvalidateDependants();
        }
    }
}

