namespace Majex.Potok.Core;

public class RebindableDynex<T> : Dynex<T>
{
    public RebindableDynex(Identifier id) : base(id, _noValueFunc)
    {

    }

    public RebindableDynex(Identifier id, T value) : base(id, _unreachableFunc)
    {
        SetValue(value, preSettle: false);
    }

    public RebindableDynex(Identifier id, Func<T> evalFunc) : base(id, evalFunc)
    {
        Rebind(evalFunc, preSettle: false);
    }

    new public void Rebind(Func<T> evalFunc, bool preSettle = true)
    {
        base.Rebind(evalFunc, preSettle);
    }

    new public void SetValue(T value, bool preSettle = true)
    {
        base.SetValue(value, preSettle);
    }
}