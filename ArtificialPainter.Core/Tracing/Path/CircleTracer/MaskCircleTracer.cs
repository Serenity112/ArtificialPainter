using ArtificialPainter.Core.GraphicsCore.Canvas;
using ArtificialPainter.Core.GraphicsCore.Extensions;
using ArtificialPainter.Core.GraphicsCore.Structs;
using System.Drawing;

namespace ArtificialPainter.Core.Tracing.Path.CircleTracer;

public static class MaskCircleTracer
{
    private static readonly Dictionary<int, Matrix2D<bool>> _circleMasks = [];

    private static readonly object _locker = new();

    static MaskCircleTracer()
    {
        for (int i = 0; i < 15; i++)
        {
            GetMask(i);
        }
    }

    public static CircleMaskResult ApplyCircleMask(ArtCanvas artCanvas, Point point, int radius)
    {
        var mask = GetMask(radius);
        HashSet<Point> coordinates = [];

        foreach (var (x, y) in mask)
        {
            if (mask[x, y])
            {
                int canvasX = point.X + x - radius;
                int canvasY = point.Y + y - radius;

                if (artCanvas.IsInside(canvasX, canvasY))
                {
                    coordinates.Add(new Point(canvasX, canvasY));
                }
            }
        }

        return new CircleMaskResult(coordinates);
    }

    private static Matrix2D<bool> GetMask(int radius)
    {
        if (_circleMasks.TryGetValue(radius, out Matrix2D<bool>? value))
        {
            return value;
        }
        else
        {
            lock (_locker)
            {
                var mask = CreateNewCircleMask(radius);
                _circleMasks.TryAdd(radius, mask);
                return mask;
            }
        }
    }

    private static Matrix2D<bool> CreateNewCircleMask(int radius)
    {
        int diameter = 2 * radius + 1;
        var mask = new Matrix2D<bool>(diameter, diameter);

        int centerX = radius;
        int centerY = radius;

        for (int x = 0; x < diameter; x++)
        {
            for (int y = 0; y < diameter; y++)
            {
                int distanceSquared = (x - centerX) * (x - centerX) + (y - centerY) * (y - centerY);
                mask[x, y] = distanceSquared <= radius * radius;
            }
        }

        return mask;
    }
}

public struct CircleMaskResult(HashSet<Point> coordinates)
{
    public HashSet<Point> Coordinates { get; private set; } = coordinates;
}
