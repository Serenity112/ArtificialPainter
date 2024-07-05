using System.Drawing;

namespace ArtificialPainter.Core.Strokes.StrokeShape;

public class ArtStrokeShapeData
{
    public int IntialSize { get; set; }

    public int CurrentSize { get; set; }

    public List<Point> SkeletPoints { get; set; }

    public Rectangle Bounds { get; set; }

    public Color ShapeColor { get; set; }

    public ArtStrokeShapeData()
    {
        SkeletPoints = [];
    }

    public double GetFraction()
    {
        return ((double)CurrentSize) / IntialSize;
    }
}
