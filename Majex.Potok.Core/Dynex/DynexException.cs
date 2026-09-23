namespace Majex.Potok.Core;

public abstract class DynexException : Exception
{
    public readonly List<BaseDynex> CallStack = [];

    public abstract DynexException Clone();

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
}