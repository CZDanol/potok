using Majex.Potok.Core.Dynex;

namespace Majex.Potok.Core.Dynex.RebindableDynex;

public class RebindableDynex<T> : Dynex<T?>
{
    public RebindableDynex() : base(() => default)
    {

    }

    public RebindableDynex(T value) : base(() => value)
    {

    }

    public RebindableDynex(Func<T> evalFunc) : base(evalFunc)
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