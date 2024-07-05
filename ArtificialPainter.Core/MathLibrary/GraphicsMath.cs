using ArtificialPainter.Core.GraphicsCore.Canvas;
using ArtificialPainter.Core.MathLibrary.Extensions;
using System.Drawing;

namespace ArtificialPainter.Core.MathLibrary;

public static class GraphicsMath
{
    public static IEnumerable<Point> GetLinePoints(Point pointStart, Point pointEnd)
    {
        var points = new HashSet<Point>();

        int x1 = pointStart.X, y1 = pointStart.Y;
        int x2 = pointEnd.X, y2 = pointEnd.Y;

        int dx = Math.Abs(x2 - x1);
        int dy = Math.Abs(y2 - y1);
        int sx = x1 < x2 ? 1 : -1;
        int sy = y1 < y2 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            points.Add(new Point(x1, y1));

            if (x1 == x2 && y1 == y2)
                break;

            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x1 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y1 += sy;
            }
        }

        return points;
    }

    public static double GetStandardDeviation(ArtCanvas artCanvas, in Color meanColor, HashSet<Point>[] pixelSets)
    {
        double sum = 0.0;
        int count = 0;

        foreach (var set in pixelSets)
        {
            foreach (var pixel in set)
            {
                count++;
                sum += artCanvas[pixel.X, pixel.Y].CalculateSquaredEuclideanDistance(meanColor);
            }
        }

        return Math.Sqrt(sum / count - 1);
    }
}
