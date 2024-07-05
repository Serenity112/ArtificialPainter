using ArtificialPainter.Core.GraphicsCore.Canvas;
using ArtificialPainter.Core.MathLibrary;
using ArtificialPainter.Core.Serialization;
using ArtificialPainter.Core.Utils;
using System.Drawing;

namespace ArtificialPainter.Core.Tracing.Path.CircleTracer;

public class CircleTracer
{
    public static async Task<CircleTracingResult> TraceIterative(ArtSingleGeneration genData, ArtCanvas artCanvas, Point centerPoint)
    {
        int radiusMin = genData.StrokeWidth_Min / 2;
        int radiusMax = genData.StrokeWidth_Max / 2;
        int radiusInterval = radiusMax - radiusMin + 1;
        double standardDeviationBound = genData.StandardDeviation_Stroke_Bound;

        var tasks = new Task[radiusInterval];
        var ROIs = new CircleTracingResult[radiusInterval];

        for (int radius = radiusMin; radius <= radiusMax; radius++)
        {
            int index = radius - radiusMin;
            int radiusCurrent = radius;

            tasks[index] = Task.Run(() =>
            {
                var circleMask = MaskCircleTracer.ApplyCircleMask(artCanvas, centerPoint, radiusCurrent);
                var meanColorCalculator = new MeanColorCalculator(artCanvas, circleMask.Coordinates);
                double standardDeviation = GraphicsMath.GetStandardDeviation(artCanvas, meanColorCalculator.GetMeanColor(), [circleMask.Coordinates]);

                ROIs[index] = new CircleTracingResult()
                {
                    CenterPoint = centerPoint,
                    Coordinates = circleMask.Coordinates,
                    Calculator = meanColorCalculator,
                    Width = radiusCurrent * 2,
                    StandardDeviation = standardDeviation
                };
            });
        }

        await Task.WhenAll(tasks);

        foreach (var roi in ROIs)
        {
            if (roi.StandardDeviation <= standardDeviationBound)
            {
                return roi;
            }
        }

        return ROIs[0];
    }
}

public struct CircleTracingResult
{
    public Point CenterPoint { get; set; }

    public HashSet<Point> Coordinates { get; set; }

    public MeanColorCalculator Calculator { get; set; }

    public int Width { get; set; }

    public double StandardDeviation { get; set; }
}
