namespace Majex.Potok.Core;

public abstract class DynexException : Exception
{
    readonly List<BaseDynex> _callStack = [];

    public IReadOnlyList<BaseDynex> CallStack => _callStack;

    public abstract DynexException Clone();

    public DynexException CloneAndAddCallStackItem(BaseDynex item)
    {
        DynexException result = Clone();
        result._callStack.Add(item);
        return result;
    }

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

    protected DynexException(DynexException other)
    {
        _callStack = new List<BaseDynex>(other._callStack);
    }
}

public sealed class DynexNoValueException : DynexException
{
    public static readonly DynexNoValueException BaseInstance = new();

    public DynexNoValueException() { }

    public DynexNoValueException(DynexNoValueException other) : base(other) { }

    public override DynexException Clone()
    {
        return new DynexNoValueException(this);
    }

    public override bool ExceptionEquals(DynexException? other)
    {
        return base.ExceptionEquals(other);
    }
}

public sealed class DynexLoopException : DynexException
{
    public DynexLoopException() { }

    public DynexLoopException(DynexLoopException other) : base(other) { }

    public override DynexException Clone()
    {
        return new DynexLoopException(this);
    }

    public override bool ExceptionEquals(DynexException? other)
    {
        return base.ExceptionEquals(other);
    }
}

public sealed class DynexFlowConvergenceException : DynexException
{
    public DynexFlowConvergenceException() { }

    public DynexFlowConvergenceException(DynexFlowConvergenceException other) : base(other) { }

    public override DynexException Clone()
    {
        return new DynexFlowConvergenceException(this);
    }

    public override bool ExceptionEquals(DynexException? other)
    {
        return base.ExceptionEquals(other);
    }
}