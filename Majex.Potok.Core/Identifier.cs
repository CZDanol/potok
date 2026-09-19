namespace Majex.Potok.Core;

public struct Identifier
{
    public static readonly Identifier Root = new();

    public static readonly char SegmentSeparator = '.';

    readonly string[] _segments;

    public Identifier()
    {
        _segments = [];
    }

    public Identifier(string singleSegment)
    {
        _segments = [singleSegment];
    }

    public Identifier(string[] segments)
    {
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
        return new Identifier(resultSegments);
    }
}