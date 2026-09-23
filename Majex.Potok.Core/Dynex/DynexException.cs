namespace Majex.Potok.Core;

public abstract class DynexException : Exception
{
    public readonly List<BaseDynex> CallStack = [];

    public abstract DynexException Clone();

    public DynexException CloneAndAddCallStackItem(BaseDynex item)
    {
        DynexException result = Clone();
        result.CallStack.Add(item);
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
        return other != null && CallStack.SequenceEqual(other.CallStack);
    }

    protected DynexException() { }

    protected DynexException(DynexException other)
    {
        CallStack = new List<BaseDynex>(other.CallStack);
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