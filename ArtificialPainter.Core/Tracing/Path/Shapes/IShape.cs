using System.Drawing;

namespace ArtificialPainter.Core.Tracing.Path.Shapes;

public interface IShape
{
    bool IsInside(Point point);
}
