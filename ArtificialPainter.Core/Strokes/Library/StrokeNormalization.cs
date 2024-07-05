using static ArtificialPainter.Core.Strokes.Library.StrokeNormalization;

namespace ArtificialPainter.Core.Strokes.Library;

public class StrokeNormalization
{
    public class NormalizationParam(double initialValue)
    {
        public double MinValue { get; set; } = initialValue;

        public double MaxValue { get; set; } = initialValue;

        public double Interval => MaxValue - MinValue + 1;

        public void AddValue(double value)
        {
            MinValue = value < MinValue ? value : MinValue;
            MaxValue = value > MaxValue ? value : MaxValue;
        }

        public double Normalize(double value)
        {
            return Math.Clamp((value - MinValue) / Interval, 0, 1);
        }
    }
}

public class StrokePropertyNormalizator : StrokePropertyCollection<NormalizationParam>
{
    public void AddP(StrokeProperty key, double value)
    {
        if (!ContainsPropery(key))
        {
            SetProperty(key, new NormalizationParam(value));
        }
        else
        {
            GetProperty(key).AddValue(value);
        }
    }

    public double Normalize(StrokeProperty key, double value)
    {
        return GetProperty(key).Normalize(value);
    }
}
