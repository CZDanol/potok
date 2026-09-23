namespace Majex.Potok.Core;

public class RebindableDynex<T> : Dynex<T>
{
    public RebindableDynex(Identifier id) : base(id, static () => throw DynexNoValueException.BaseInstance)
    {

    }

    public RebindableDynex(Identifier id, T value) : base(id, () => value)
    {

    }

    public RebindableDynex(Identifier id, Func<T> evalFunc) : base(id, evalFunc)
    {

    }

    new public void Rebind(Func<T> evalFunc)
    {
        base.Rebind(evalFunc);
    }

    public void Rebind(T value)
    {
        Rebind(() => value);
    }
}