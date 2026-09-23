namespace Majex.Potok.Core;

public class RebindableDynex<T> : Dynex<T>
{
    public RebindableDynex(Identifier id) : base(id, _noValueFunc)
    {

    }

    public RebindableDynex(Identifier id, T value) : base(id, _unreachabelFunc)
    {
        SetValue(value);
    }

    public RebindableDynex(Identifier id, Func<T> evalFunc) : base(id, evalFunc)
    {
        Rebind(evalFunc);
    }

    new public void Rebind(Func<T> evalFunc)
    {
        base.Rebind(evalFunc);
    }

    new public void SetValue(T value)
    {
        base.SetValue(value);
    }
}