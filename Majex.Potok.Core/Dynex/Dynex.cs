namespace Majex.Potok.Core;

using System.Diagnostics;
using System.Reflection;
using BaseDynexWeakRef = WeakReference<BaseDynex>;

public abstract class BaseDynex
{
    public DynexFlow Flow => _id.Flow;

    protected internal readonly struct Snapshot(BaseDynexWeakRef dynex, uint revision)
    {
        readonly BaseDynexWeakRef Dynex = dynex;
        readonly uint Revision = revision;

        public readonly BaseDynex? ResolveDynex()
        {
            return (Dynex.TryGetTarget(out var dynex) && dynex._revision == Revision) ? dynex : null;
        }
    }

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
    protected bool _isDirty = false;

    protected BaseDynexWeakRef _weakThis;

    /// <remarks>
    /// Changed at the beginning of every recompute.
    /// </remarks>
    protected uint _revision = 0;

    protected BaseDynex(Identifier id)
    {
        _id = id;
        _weakThis = new BaseDynexWeakRef(this);
        Invalidate();
    }

    /// <summary>
    /// Ensures that the value is not dirty.
    /// </summary>
    internal protected abstract void Recompute();

    protected void EvalImpl()
    {
        var dependant = Flow.DynexBeingRecomputed;
        if (dependant != null)
        {
            // Someone asked for a value of this dynex while recomputing dependant.

            if (Flow.TopDynexBeingRecomputed == this)
            {
                throw new DynexLoopException();
            }

            // Clean up the dependants list if it grew too much.
            // Don't sweep every time, because the sweep has a linear complexity.          
            if (_dependants.Count >= _dependantsSweepOn)
            {
                SweepDependants(false);
            }

            // DynexBeingRecomputed is dependent on this dynex
            // -> add it to the dependants list.
            _dependants.Add(new Snapshot(dependant._weakThis, dependant._revision));

            // We need to provide some value, make sure we're not dirty.
            Recompute();
        }
        else
        {
            // Someone asked for this dynexes value directly (outside of the recomputation tree).

            // Recompute all dirty dynexes before giving the answer.
            // Some of the dependencies or this dynex itself could be dirty.
            Flow.Settle();
        }

        Debug.Assert(!_isDirty);
    }

    protected void Invalidate()
    {
        if (_isDirty)
        {
            return;
        }

        _isDirty = true;
        Flow.DirtyDynexes.Enqueue(this);
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

    internal readonly record struct CachedValue
    {
        readonly T Value = default!;

        /// <summary>
        /// If not null, denotes that we don't have a value (even if we're not dirty anymore)
        /// but we have this exception instead.
        /// </summary>
        readonly DynexException? Exception = DynexNoValueException.BaseInstance;

        public CachedValue()
        {

        }

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

        public bool Equals(CachedValue other)
        {
            return EqualityComparer<T>.Default.Equals(Value, other.Value)
                && DynexException.ExceptionEquals(Exception, other.Exception);
        }

        public override int GetHashCode() => throw new NotImplementedException();
    }

    CachedValue _cachedValue = new();

    public bool TryEval(out T value)
    {
        EvalImpl();
        return _cachedValue.TryGet(out value);
    }

    public T Eval()
    {
        EvalImpl();
        return _cachedValue.Get();
    }

    protected static readonly Func<T> _noValueFunc = () => throw DynexNoValueException.BaseInstance;
    protected static readonly Func<T> _unreachableFunc = () => throw new UnreachableException();

    /// <remarks>
    /// MUST stay private. Use RebindableDynex if you want rebinding.
    /// </remarks>
    /// <param name="preSettle">Settle the flow before rebinding.</param>
    protected void Rebind(Func<T> evalFunc, bool preSettle = true)
    {
        if (_evalFunc == evalFunc)
        {
            return;
        }

        if (preSettle)
        {
            Flow.Settle();
        }

        _evalFunc = evalFunc;
        Invalidate();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <param name="preSettle">Settle the flow before rebinding.</param>
    protected void SetValue(T value, bool preSettle = true)
    {
        if (preSettle)
        {
            Flow.Settle();
        }

        var newValue = new CachedValue(value);
        bool emitValueChange = (_cachedValue != newValue);

        // Invalidate potential dependencies from previous state
        _revision++;

        // _evalFunc must never get called,
        // there is no way for this dynex to get dirty,
        // because it's just a static value with no dependencies.
        _evalFunc = _unreachableFunc;
        _isDirty = false;

        _cachedValue = newValue;

        if (emitValueChange)
        {
            InvalidateDependants();
        }
    }

    internal protected override void Recompute()
    {
        if (!_isDirty)
        {
            return;
        }

        CachedValue prevValue = _cachedValue;

        _revision++;
        BaseDynex? prevRecomputed = Flow.DynexBeingRecomputed;
        Flow.DynexBeingRecomputed = this;
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
            Flow.DynexBeingRecomputed = prevRecomputed;
        }

        _isDirty = false;

#if DEBUG
        IDynexDebugger.Instance.Value?.OnRecompute(this);
#endif

        if (_cachedValue != prevValue)
        {
            InvalidateDependants();
        }
    }
}

