namespace Majex.Potok.Core;

using System.Diagnostics;
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
    /// When <see cref="_dependants"/>  grow to this size,
    /// do a sweep that removes null values.
    /// </summary>
    private int _dependantsSweepOn = 16;

    /// <summary>
    /// True if the dynex is not up-to-date and needs recomputation.
    /// </summary>
    protected bool _isDirty = false;

    protected bool _isRecomputing = false;

    protected BaseDynexWeakRef _weakThis;

    /// <remarks>
    /// Changed at the beginning of every recompute.
    /// </remarks>
    protected uint _revision = 0;

    protected uint _lastSettleRunID = 0;

    protected uint _recomputeCountWithinSettleRun = 0;

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
        if (Flow.RecomputingDynexesStack.Count > 0)
        {
            // Someone asked for a value of this dynex while recomputing dependant.            

            // Clean up the dependants list if it grew too much.
            // Don't sweep every time, because the sweep has a linear complexity.          
            if (_dependants.Count >= _dependantsSweepOn)
            {
                SweepDependants(false);
            }

            // DynexBeingRecomputed is dependent on this dynex
            // -> add it to the dependants list.
            var dependant = Flow.RecomputingDynexesStack.Peek();
            _dependants.Add(new Snapshot(dependant._weakThis, dependant._revision));

            // Check for loops - but only after reporting the dependants
            if (_isRecomputing)
            {
                List<BaseDynex> loop = new();
                foreach (var item in Flow.RecomputingDynexesStack)
                {
                    loop.Add(item);
                    if (item == this)
                    {
                        break;
                    }
                }
                throw new DynexLoopException(loop);
            }

            // We need to provide some value, make sure we're not dirty.
            Recompute();

            // We can actually end up dirty again after recompute
            // Debug.Assert(!_isDirty);
        }
        else
        {
            // Someone asked for this dynexes value directly (outside of the recomputation tree).

            // Recompute all dirty dynexes before giving the answer.
            // Some of the dependencies or this dynex itself could be dirty.
            Flow.Settle();

            Debug.Assert(!_isDirty);
        }
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
    /// Removes null references from the <see cref="_dependants"/> list to shrink its size.
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
    protected void Rebind(Func<T> evalFunc)
    {
        Debug.Assert(!_isRecomputing);

        if (_evalFunc == evalFunc)
        {
            return;
        }

        _evalFunc = evalFunc;
        Invalidate();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    protected void SetValue(T value)
    {
        Debug.Assert(!_isRecomputing);

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

        // Settle run logic (if we're within <see cref="DynexFlow.Settle"/> )
        if (Flow.SettleRunID != 0)
        {
            if (_lastSettleRunID != Flow.SettleRunID)
            {
                _lastSettleRunID = Flow.SettleRunID;
                _recomputeCountWithinSettleRun = 0;
            }

            if (++_recomputeCountWithinSettleRun == Flow.MaxDynexSettlementIterations)
            {
                throw new DynexFlowConvergenceException();
            }
        }

        CachedValue prevValue = _cachedValue;
        try
        {
            Flow.RecomputingDynexesStack.Push(this);
            _isRecomputing = true;

            _revision++;
            _isDirty = false;

            _cachedValue = new CachedValue(_evalFunc());
        }
        catch (DynexException e)
        {
            _cachedValue = new CachedValue(e.CloneAndAddCallStackItem(this));
            // A DynexException exception is still considered a known and well defined state
            // -> we're not dirty, we're clean, Get() will throw the stored exception
        }
        catch (Exception)
        {
            // Uknown exception from outside of our control
            // it might be repeatable, it might not, we don't know
            // the dynex should stay in the dirty state
            _isDirty = true;
            throw;
        }
        finally
        {
            _isRecomputing = false;
            var popped = Flow.RecomputingDynexesStack.Pop();
            Debug.Assert(popped == this);
        }

#if DEBUG
        IDynexDebugger.Instance.Value?.OnRecompute(this);
#endif

        if (_cachedValue != prevValue)
        {
            InvalidateDependants();
        }
    }
}

