namespace Majex.Potok.Core;

public abstract class DynexException : Exception
{
    readonly List<BaseDynex> _callStack = [];

    public IReadOnlyList<BaseDynex> CallStack => _callStack;

    public abstract DynexException CloneAndAddCallStackItem(BaseDynex item);

    public static bool ExceptionEquals(DynexException? a, DynexException? b)
    {
        if (a == b)
        {
            return true;
        }
        if (a == null || b == null)
        {
            return false;
        }
        return a.ExceptionEquals(b);
    }
    public virtual bool ExceptionEquals(DynexException? other)
    {
        return other != null && GetType() == other.GetType() && _callStack.SequenceEqual(other._callStack);
    }

    protected DynexException() { }

    protected DynexException(DynexException other, BaseDynex addStackItem)
    {
        _callStack.EnsureCapacity(other._callStack.Count + 1);
        foreach (var item in other._callStack)
        {
            _callStack.Add(item);

            // Prevent loop accumulation in the call stack
            // Note: bottommost stack items are first in the list
            if (item == addStackItem)
            {
                _callStack.Clear();
            }
        }

        _callStack.Add(addStackItem);
    }
}

public sealed class DynexNoValueException : DynexException
{
    public static readonly DynexNoValueException BaseInstance = new();

    public DynexNoValueException() { }

    public DynexNoValueException(DynexNoValueException other, BaseDynex addStackItem)
     : base(other, addStackItem) { }

    public override DynexException CloneAndAddCallStackItem(BaseDynex item)
    {
        return new DynexNoValueException(this, item);
    }

    public override bool ExceptionEquals(DynexException? other)
    {
        return base.ExceptionEquals(other);
    }
}

public sealed class DynexLoopException : DynexException
{
    private List<BaseDynex> _loop = new();

    public IReadOnlyList<BaseDynex> Loop => _loop;

    public DynexLoopException(List<BaseDynex> loop)
    {
        _loop = loop;
    }

    public DynexLoopException(DynexLoopException other, BaseDynex addStackItem)
     : base(other, addStackItem)
    {
        _loop = other._loop;
    }

    public override DynexException CloneAndAddCallStackItem(BaseDynex item)
    {
        return new DynexLoopException(this, item);
    }

    public override bool ExceptionEquals(DynexException? other)
    {
        return base.ExceptionEquals(other)
            && _loop.SequenceEqual(((DynexLoopException)other!)._loop);
    }
}

public sealed class DynexFlowConvergenceException : DynexException
{
    public DynexFlowConvergenceException() { }

    public DynexFlowConvergenceException(DynexFlowConvergenceException other, BaseDynex addStackItem)
     : base(other, addStackItem) { }

    public override DynexException CloneAndAddCallStackItem(BaseDynex item)
    {
        return new DynexFlowConvergenceException(this, item);
    }

    public override bool ExceptionEquals(DynexException? other)
    {
        return base.ExceptionEquals(other);
    }
}