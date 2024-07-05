using System.Drawing;

namespace ArtificialPainter.Core.Strokes.StrokeShape;

public interface IStrokeShape
{
    void Rotate(float rotationAngle, Color fillColor);

    ShapeType GetPixelShapeType(Point point);

    void CalculateShape();
}
