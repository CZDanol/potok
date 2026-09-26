namespace Majex.Potok.Core;

public struct Identifier
{
    public static readonly char SegmentSeparator = '.';

    public readonly DynexFlow Flow;

    readonly string[] _segments;

    public Identifier(DynexFlow flow)
    {
        Flow = flow;
        _segments = [];
    }

    private Identifier(DynexFlow flow, string[] segments)
    {
        Flow = flow;
        _segments = segments;
    }

    public readonly override string ToString()
    {
        return string.Join(SegmentSeparator, _segments);
    }

    public static Identifier operator /(Identifier id, string segment)
    {
        string[] resultSegments = new string[id._segments.Length + 1];
        id._segments.CopyTo(resultSegments);
        resultSegments[^1] = segment;
        return new Identifier(id.Flow, resultSegments);
    }
}