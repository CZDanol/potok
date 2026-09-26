namespace Majex.Potok.Core;

public class DynexFlow
{
    public readonly Identifier Root;

    public DynexFlow()
    {
        Root = new Identifier(this);
    }

    internal BaseDynex? DynexBeingRecomputed = null;
}