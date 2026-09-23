namespace Majex.Potok.Core;

public abstract class DynexException : Exception, IEquatable<DynexException>
{
    public readonly List<BaseDynex> CallStack = [];

    public abstract DynexException Clone();

    public DynexException CloneAndAddCallStackItem(BaseDynex item)
    {
        DynexException result = Clone();
        result.CallStack.Add(item);
        return result;
    }

    public virtual bool Equals(DynexException? other)
    {
        return other != null && CallStack.Equals(other.CallStack);
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

    public override bool Equals(DynexException? other)
    {
        return base.Equals(other);
    }
}