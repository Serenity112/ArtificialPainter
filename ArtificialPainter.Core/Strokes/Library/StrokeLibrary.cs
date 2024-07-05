using ArtificialPainter.Core.Serialization;
using ArtificialPainter.Core.Settings;
using ArtificialPainter.Core.Tracing;
using System.Drawing;

namespace ArtificialPainter.Core.Strokes.Library;

public class StrokeLibrary
{
    public static double MmTpPxCoef { get; private set; } = 8.6;

    private readonly StrokeLibraryContainer _libraryContainer;
    private readonly Dictionary<int, StrokePropertyNormalizator> _strokeNormalizators;

    private readonly object _locker = new();
    private readonly string _sourceLibraryPath;

    public StrokeLibrary(PathSettings pathSettings, ArtModelSerializer serializer, double resizeCoefficient = 1)
    {
        _sourceLibraryPath = pathSettings.LibraryPath;
        MmTpPxCoef *= resizeCoefficient;

        _libraryContainer = new();

        _strokeNormalizators = new()
            {
                { 1, new() },
                { 2, new() },
                { 3, new() }
            };

        InitNormalizator(serializer);
        StrokeLibraryReader.ReadAllStrokes(_sourceLibraryPath, _libraryContainer, resizeCoefficient);
    }

    private void InitNormalizator(ArtModelSerializer serializer)
    {
        var input = serializer.UserInput;

        // Мазки с разными сегментами имеют разные параметры нормализации и разные границы в библиотеке

        // 2 точки
        _strokeNormalizators[2].AddP(StrokeProperty.Width, 1 * MmTpPxCoef);
        _strokeNormalizators[2].AddP(StrokeProperty.Width, 9 * MmTpPxCoef);

        _strokeNormalizators[2].AddP(StrokeProperty.LtoW, input.StrokeLength_Max / input.StrokeWidth_Min);
        _strokeNormalizators[2].AddP(StrokeProperty.LtoW, input.StrokeLength_Min / input.StrokeWidth_Max);

        // 3 точки
        _strokeNormalizators[3].AddP(StrokeProperty.Width, 1 * MmTpPxCoef);
        _strokeNormalizators[3].AddP(StrokeProperty.Width, 6 * MmTpPxCoef);

        _strokeNormalizators[3].AddP(StrokeProperty.Angle1, 90);
        _strokeNormalizators[3].AddP(StrokeProperty.Angle1, 180);

        _strokeNormalizators[3].AddP(StrokeProperty.Fraction, 0);
        _strokeNormalizators[3].AddP(StrokeProperty.Fraction, 100);

        _strokeNormalizators[3].AddP(StrokeProperty.LtoW, input.StrokeLength_Max / input.StrokeWidth_Min);
        _strokeNormalizators[3].AddP(StrokeProperty.LtoW, input.StrokeLength_Min / input.StrokeWidth_Max);
    }

    public ArtStroke? ClassifyStroke(SegmentedTracingPathResult tracingPath, ArtSingleGeneration generationData)
    {
        lock (_locker)
        {
            double points = tracingPath.Properties.GetProperty(StrokeProperty.Points);

            var result = points switch
            {
                1 => ClassifyPt1(tracingPath),
                2 => ClassifyPt2(tracingPath),
                3 => ClassifyPt3(tracingPath),
                _ => null
            };

            return result;
        }
    }

    private ArtStroke ClassifyPt1(SegmentedTracingPathResult tracingPath)
    {
        double width = tracingPath.Properties.GetProperty(StrokeProperty.Width);

        return _libraryContainer[1]
            .MinBy(sourceStroke => Math.Abs(sourceStroke.Properties.GetProperty(StrokeProperty.Width) - width))!.Copy();
    }

    private ArtStroke ClassifyPt2(SegmentedTracingPathResult targetStroke)
    {
        double target_ltow = _strokeNormalizators[2].Normalize(StrokeProperty.LtoW, targetStroke.Properties.GetProperty(StrokeProperty.LtoW));
        double target_width = _strokeNormalizators[2].Normalize(StrokeProperty.Width, targetStroke.Properties.GetProperty(StrokeProperty.Width));

        return _libraryContainer[2]
           .MinBy(sourceStroke =>
           {
               double source_ltow = _strokeNormalizators[2].Normalize(StrokeProperty.LtoW, sourceStroke.Properties.GetProperty(StrokeProperty.LtoW));
               double source_width = _strokeNormalizators[3].Normalize(StrokeProperty.Width, sourceStroke.Properties.GetProperty(StrokeProperty.Width));

               double df_ltow = Math.Abs(target_ltow - source_ltow);
               double df_width = Math.Abs(target_width - source_width);

               return Math.Sqrt(Math.Pow(df_ltow, 2) + 0.33 * Math.Pow(df_width, 2));
           })!.Copy();
    }

    private ArtStroke ClassifyPt3(SegmentedTracingPathResult targetStroke)
    {
        double target_ltow = _strokeNormalizators[3].Normalize(StrokeProperty.LtoW, targetStroke.Properties.GetProperty(StrokeProperty.LtoW));
        double target_angle = _strokeNormalizators[3].Normalize(StrokeProperty.Angle1, Math.Abs(targetStroke.Properties.GetProperty(StrokeProperty.Angle1)));
        double target_fraction = _strokeNormalizators[3].Normalize(StrokeProperty.Fraction, targetStroke.Properties.GetProperty(StrokeProperty.Fraction));
        double target_width = _strokeNormalizators[3].Normalize(StrokeProperty.Width, targetStroke.Properties.GetProperty(StrokeProperty.Width));

        var result = _libraryContainer[3]
           .MinBy(sourceStroke =>
           {
               double source_ltow = _strokeNormalizators[3].Normalize(StrokeProperty.LtoW, sourceStroke.Properties.GetProperty(StrokeProperty.LtoW));
               double source_angle = _strokeNormalizators[3].Normalize(StrokeProperty.Angle1, sourceStroke.Properties.GetProperty(StrokeProperty.Angle1));
               double source_fraction = _strokeNormalizators[3].Normalize(StrokeProperty.Fraction, sourceStroke.Properties.GetProperty(StrokeProperty.Fraction));
               double source_width = _strokeNormalizators[3].Normalize(StrokeProperty.Width, sourceStroke.Properties.GetProperty(StrokeProperty.Width));

               double df_ltow = Math.Abs(target_ltow - source_ltow);
               double df_angle = Math.Abs(target_angle - source_angle);
               double df_fraction = Math.Abs(target_fraction - source_fraction);
               double df_width = Math.Abs(target_width - source_width);

               return Math.Sqrt(
                   Math.Pow(df_ltow, 2) +
                   Math.Pow(df_angle, 2) +
                   Math.Pow(df_fraction, 2) +
                   0.33 * Math.Pow(df_width, 2)); // Весовой коэффициенит для учёта реальных размеров
           })!.Copy();

        // Отзеркаливание мазка, если углы не совпадают
        try
        {
            double a1 = targetStroke.Properties.GetProperty(StrokeProperty.Angle1);
            double a2 = result.Properties.GetProperty(StrokeProperty.Angle1);

            if (Math.Sign(a1) != Math.Sign(a2))
            {
                result = result.FlipStroke(RotateFlipType.RotateNoneFlipX);
            }
        }
        catch
        {
        }

        return result;
    }

    public static double CalculateResizeCoefficient(SegmentedTracingPathResult tracingResult, ArtStroke strokeData, ResizeCoefficient resizeCoefficient = ResizeCoefficient.Width)
    {
        return resizeCoefficient switch
        {
            ResizeCoefficient.Width => tracingResult.Properties.GetProperty(StrokeProperty.Width) / strokeData.Properties.GetProperty(StrokeProperty.Width),
            ResizeCoefficient.Length => tracingResult.Properties.GetProperty(StrokeProperty.Length) / strokeData.Properties.GetProperty(StrokeProperty.Length),
            ResizeCoefficient.Middle => (
                                CalculateResizeCoefficient(tracingResult, strokeData, ResizeCoefficient.Width) +
                                CalculateResizeCoefficient(tracingResult, strokeData, ResizeCoefficient.Length)) / 2,
            _ => 1,
        };
    }
}

public enum ResizeCoefficient
{
    Width,
    Length,
    Middle,
}
