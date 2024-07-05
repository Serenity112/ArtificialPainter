using ArtificialPainter.Core.GraphicsCore.Canvas;
using ArtificialPainter.Core.GraphicsCore.Extensions;
using ArtificialPainter.Core.MathLibrary;
using ArtificialPainter.Core.MathLibrary.Extensions;
using ArtificialPainter.Core.Tracing.Path.CircleTracer;
using ArtificialPainter.Core.Tracing.Path.Shapes;
using ArtificialPainter.Core.Utils;
using System.Drawing;

namespace ArtificialPainter.Core.Tracing.Path.LinearTracer;

public static class LinearPathTracer
{
    public static LinearTracingPathResult GetLinearPath(
        ArtCanvas artCanvas,
        LinearTracingPathSearchData searchData,
        HashSet<Point> previousSegmentsPathCoordinates, // Точки прошлых сегментов
        MeanColorCalculator previousSegmentsCalculato // Калькулятор среднего цвета прошлых сегментов
    )
    {
        int radius = searchData.Width / 2;
        var pointStart = searchData.PointStart;
        var pointEnd = searchData.PointEnd;
        var pathVector = new PointF(pointEnd.X - pointStart.X, pointEnd.Y - pointStart.Y);
        float angle = MathF.Atan2(pathVector.Y, pathVector.Y);

        // 1-3 линия, 2-4 линия
        var p1 = pointStart.GetOffsetF(angle + MathF.PI / 2, radius);
        var p2 = pointStart.GetOffsetF(angle - MathF.PI / 2, radius);
        var p3 = pointEnd.GetOffsetF(angle + MathF.PI / 2, radius);
        var p4 = pointEnd.GetOffsetF(angle - MathF.PI / 2, radius);
        PointF[] points = [p1, p3, p4, p2]; // Порядок, чтобы шли по кругу

        var rectShape = new RectangleShape(points);

        var (leftBottom, rightTop) = GetOuterRectangle(points);

        HashSet<Point> localPathCoordinates = [];
        var localCalculator = previousSegmentsCalculato.Copy();

        // Точки в прямоугольнике
        for (int x = leftBottom.X; x < rightTop.X; x++)
        {
            for (int y = leftBottom.Y; y < rightTop.Y; y++)
            {
                var point = new Point(x, y);

                if (rectShape.IsInside(point) &&
                    artCanvas.IsInside(x, y) &&
                    !localPathCoordinates.Contains(point) &&
                    !previousSegmentsPathCoordinates.Contains(point))
                {
                    localPathCoordinates.Add(point);
                    localCalculator.AddColor(artCanvas[x, y]);
                }
            }
        }

        // Точки в окружностях
        AddCirclePoints(MaskCircleTracer.ApplyCircleMask(artCanvas, pointStart, radius));
        AddCirclePoints(MaskCircleTracer.ApplyCircleMask(artCanvas, pointEnd, radius));

        var meanColor = localCalculator.GetMeanColor();
        double standardDeviation = GraphicsMath.GetStandardDeviation(artCanvas, meanColor, [localPathCoordinates, previousSegmentsPathCoordinates]);

        return new LinearTracingPathResult(localPathCoordinates, searchData, localCalculator, standardDeviation);

        void AddCirclePoints(in CircleMaskResult result)
        {
            foreach (var point in result.Coordinates)
            {
                // Условие bitmap.IsInside выполняется внутри ApplyCircleMask, а условие rect.IsInside не нужно, т.к. его точки уже в хэшсетах
                if (!localPathCoordinates.Contains(point) &&
                    !previousSegmentsPathCoordinates.Contains(point))
                {
                    localPathCoordinates.Add(point);
                    localCalculator.AddColor(artCanvas[point.X, point.Y]);
                }
            }
        }
    }

    private static (Point leftBottom, Point rightTop) GetOuterRectangle(in PointF[] points)
    {
        int minX = (int)points.Min(point => point.X);
        int maxX = (int)points.Max(point => point.X);

        int minY = (int)points.Min(point => point.Y);
        int maxY = (int)points.Max(point => point.Y);

        return (new Point(minX, minY), new Point(maxX, maxY));
    }
}

public class LinearTracingPathResult(HashSet<Point> coordinates, LinearTracingPathSearchData searchData, MeanColorCalculator calculator, double standardDeviation)
{
    public HashSet<Point> Coordinates { get; set; } = coordinates;
    public LinearTracingPathSearchData TracingPathSearchData { get; set; } = searchData;
    public MeanColorCalculator Calculator { get; set; } = calculator;
    public double StandardDeviation { get; set; } = standardDeviation;
}

public class LinearTracingPathSearchData
{
    public int Length { get; set; }
    public int Width { get; set; }
    public Point PointStart { get; set; }
    public Point PointEnd { get; set; }
}
