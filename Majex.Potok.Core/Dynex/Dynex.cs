using System.Reflection;
using System.Diagnostics;

namespace Majex.Potok.Core.Dynex;

public class Dynex<T>
{
    protected Func<T> _evalFunc;
    protected bool _isDirty = true;
    private T? _cachedValue;

    public Dynex(Func<T> evalFunc)
    {
        _evalFunc = evalFunc;
    }

    public T? Eval()
    {
        Recompute();
        return _cachedValue;
    }

    void Recompute()
    {
        if (!_isDirty)
        {
            return;
        }

        _cachedValue = _evalFunc();
        _isDirty = false;
    }

}

