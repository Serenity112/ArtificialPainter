using ArtificialPainter.Core.Strokes;
using System.Drawing;

namespace ArtificialPainter.Core.Tracing;

public class SegmentedTracingPathResult
{
    public StrokePropertyCollection<float> Properties { get; set; }

    public Color MeanColor { get; set; }

    public double StandardDeviation { get; set; }

    public HashSet<Point> Coordinates { get; set; }

    public float MainAngle { get; set; }

    public List<Point> SkeletPath { get; set; }

    public SegmentedTracingPathResult()
    {
        Properties = [];
        Coordinates = [];
        SkeletPath = [];
    }
}
