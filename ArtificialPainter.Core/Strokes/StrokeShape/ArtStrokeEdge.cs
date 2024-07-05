using System.Drawing;

namespace ArtificialPainter.Core.Strokes.StrokeShape;

public class ArtStrokeEdge
{
    public Dictionary<Edge, HashSet<Point>> Edges = [];

    public ArtStrokeEdge()
    {
        Edges[Edge.Left] = [];
        Edges[Edge.Top] = [];
        Edges[Edge.Right] = [];
        Edges[Edge.Bottom] = [];
    }
}

public enum Edge
{
    Left,
    Right,
    Top,
    Bottom
}