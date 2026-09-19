using Majex.Potok.Core.Dynex;

namespace Majex.Potok.Core.Dynex.RebindableDynex;

public class RebindableDynex<T> : Dynex<T?>
{
    public RebindableDynex() : base(() => default)
    {

    }

    public void Rebind(Func<T> evalFunc)
    {
        if (_evalFunc == evalFunc)
        {
            return;
        }

        _evalFunc = evalFunc;
        _isDirty = true;
    }

    public void Rebind(T value)
    {
        Rebind(() => value);
    }
}