using System.Drawing;
using System.Numerics;

namespace ArtificialPainter.Core.MathLibrary.Extensions;

public static class PointExtensions
{
    public static Point GetOffset(this in Point point, float angle, float length)
    {
        int x = point.X + (int)(length * MathF.Cos(angle));
        int y = point.Y + (int)(length * MathF.Sin(angle));
        return new Point(x, y);
    }

    public static PointF GetOffsetF(this in Point point, float angle, float length)
    {
        float x = point.X + (length * MathF.Cos(angle));
        float y = point.Y + (length * MathF.Sin(angle));
        return new PointF(x, y);
    }

    public static Point GetOffsetClamp(this in Point point, float angle, float length, int clampX, int clampY)
    {
        int x = Math.Clamp(point.X + (int)(length * MathF.Cos(angle)), 0, clampX);
        int y = Math.Clamp(point.Y + (int)(length * MathF.Sin(angle)), 0, clampY);
        return new Point(x, y);
    }

    public static float AngleBy3Points(this in Point currenPoint, in Point p2, in Point p3)
    {
        var v1 = new Vector2(p2.X - currenPoint.X, p2.Y - currenPoint.Y);
        var v2 = new Vector2(p2.X - p3.X, p2.Y - p3.Y);
        return Math.Sign(v1.VectorProduct(v2)) * v1.AngleWithVector(v2);
    }

    public static Point RotatePoint(this in Point point, in float angle)
    {
        var sin = MathF.Sin(angle);
        var cos = MathF.Cos(angle);

        int xnew = (int)(point.X * cos - point.Y * sin);
        int ynew = (int)(point.X * sin + point.Y * cos);

        return new Point(xnew, ynew);
    }
}
