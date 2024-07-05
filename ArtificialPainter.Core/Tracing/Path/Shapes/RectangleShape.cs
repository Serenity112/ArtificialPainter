using System.Drawing;

namespace ArtificialPainter.Core.Tracing.Path.Shapes;

public class RectangleShape : IShape
{
    private readonly List<Func<PointF, bool>> _constraints = [];

    // Линии: 0-1 1-2 2-3 3-0
    public RectangleShape(PointF[] points)
    {
        var p1 = points[0];
        var p2 = points[1];
        var p3 = points[2];

        bool allUniqueX = (p1.X != p2.X && p2.X != p3.X);
        bool allUniqueY = (p1.Y != p2.Y && p2.Y != p3.Y);

        if (allUniqueX && allUniqueY)
        {
            RectangleShapeConstraintsCalculator.CalculateLinearConstraints(points, _constraints);
        }
    }

    public bool IsInside(Point point)
    {
        return _constraints.Count == 0 || _constraints.All(func => func(point));
    }
}

file class RectangleShapeConstraintsCalculator
{
    // Ограничения - вертикальные линии. Точки на вход - с параллельных линий
    public static void CalculateVertivalConstraints(Point p1, Point p2, List<Func<Point, bool>> constraints)
    {
        // Нам нужно чтобы p1.x было меньше p2.x
        if (p1.X > p2.X)
            (p1, p2) = (p2, p1);

        constraints.Add(p => p.X >= p1.X);
        constraints.Add(p => p.X <= p2.X);
    }

    // Ограничения - горизонтальные линии. Точки на вход - с параллельных линий
    public static void CalculateHorizontalConstraints(Point p1, Point p2, List<Func<Point, bool>> constraints)
    {
        // Нам нужно чтобы p1.y было меньше p2.y
        if (p1.Y > p2.Y)
            (p1, p2) = (p2, p1);

        constraints.Add(p => p.Y >= p1.Y);
        constraints.Add(p => p.Y <= p2.Y);
    }

    // Ограничения - наклонные линии.
    public static void CalculateLinearConstraints(in PointF[] points, List<Func<PointF, bool>> constraints)
    {
        var p1 = points[0];
        var p2 = points[1];
        var p3 = points[2];
        var p4 = points[3];

        // Первая пара параллельных прямых
        var line1 = GetLineCoefs(p1, p2);
        var line2 = GetLineCoefs(p3, p4);

        // Нам нужно чтобы line1.b было меньше line2.b
        if (line1.b > line2.b)
        {
            (line1, line2) = (line2, line1);
        }

        constraints.Add(p => p.Y >= GetLineY(p.X, line1.k, line1.b));
        constraints.Add(p => p.Y <= GetLineY(p.X, line2.k, line2.b));


        // Вторая пара параллельных прямых
        var line3 = GetLineCoefs(p2, p3);
        var line4 = GetLineCoefs(p1, p4);

        // Нам нужно чтобы line3.b было меньше line4.b
        if (line3.b > line4.b)
        {
            (line3, line4) = (line4, line3);
        }

        constraints.Add(p => p.Y >= GetLineY(p.X, line3.k, line3.b));
        constraints.Add(p => p.Y <= GetLineY(p.X, line4.k, line4.b));

        double GetLineY(in float x, in float k, in float b)
        {
            return k * x + b;
        }

        (float k, float b) GetLineCoefs(in PointF pp1, in PointF pp2)
        {
            float k = (pp2.Y - pp1.Y) * 1.0f / (pp2.X - pp1.X);
            float b = pp1.Y - k * pp1.X;
            return (k, b);
        }
    }
}
