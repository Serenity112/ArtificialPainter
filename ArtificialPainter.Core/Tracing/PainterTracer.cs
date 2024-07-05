using ArtificialPainter.Core.GraphicsCore.Canvas;
using ArtificialPainter.Core.GraphicsCore.Extensions;
using ArtificialPainter.Core.GraphicsCore.MatrixProccessing;
using ArtificialPainter.Core.MathLibrary.Extensions;
using ArtificialPainter.Core.Serialization;
using ArtificialPainter.Core.Settings;
using ArtificialPainter.Core.Strokes;
using ArtificialPainter.Core.Strokes.Library;
using ArtificialPainter.Core.Strokes.Phong;
using ArtificialPainter.Core.Strokes.StrokeShape;
using ArtificialPainter.Core.Tracing.Path.CircleTracer;
using ArtificialPainter.Core.Tracing.Path.LinearTracer;
using ArtificialPainter.Core.Tracing.PointDeciders;
using ArtificialPainter.Core.Utils;
using System.Collections;
using System.Drawing;

namespace ArtificialPainter.Core.Tracing;

public class PainterTracer : IEnumerable<(ArtCanvas, ArtCanvas)>
{
    private ArtCanvas _originalModel;
    private ArtCanvas _artificialModel;
    private ArtCanvas _artificialRender;

    private PathSettings _pathSettings;

    private ArtModelSerializer _artModelSerializer;

    public event Action<int> NotifyGenerationsChange;
    public event Action<string> NotifyStatusChange;

    private int _segments;
    private int Points => _segments + 1;
    private int _genetaions;

    private CancellationToken _token;

    private ArtUserInput _artUserInput;

    public CanvasShapeGenerator CanvasShapeGenerator;

    private readonly object _lock = new();

    private int _randomSeed = -1;

    private readonly StrokeLibrary _strokeLibrary;

    public PainterTracer(
        ArtCanvas originalCanvas,
        ArtModelSerializer serealizer,
        PathSettings pathSettings,
        CancellationToken cancellationToken)
    {
        _artModelSerializer = serealizer;
        _pathSettings = pathSettings;
        _originalModel = originalCanvas;
        _token = cancellationToken;
        _segments = serealizer.UserInput.MaxStrokeSegments;
        _genetaions = serealizer.UserInput.TotalGenerations;
        CanvasShapeGenerator = new CanvasShapeGenerator(_originalModel);

        _strokeLibrary = new StrokeLibrary(_pathSettings, _artModelSerializer, 1);
    }

    public IEnumerator<(ArtCanvas, ArtCanvas)> GetEnumerator()
    {
        NotifyStatusChange?.Invoke($"{TracingState.Prepare}");

        _artificialRender = new ArtCanvas(_originalModel.Width, _originalModel.Height);
        _artificialRender.FillColor(Color.White);

        _artificialModel = new ArtCanvas(_originalModel.Width, _originalModel.Height);
        _artificialModel.FillColor(Color.White);

        IPointDecider decider;

        // Генерация рисунка по слоям
        for (int generation = 0; generation < _artModelSerializer.UserInput.TotalGenerations; generation++)
        {
            NotifyGenerationsChange?.Invoke(generation);
            NotifyStatusChange?.Invoke($"{TracingState.Blurring} | Поколение: {generation}");

            var localGenerationData = _artModelSerializer.Generations[generation];

            var blurredOriginalModel = GaussianBlur.ApplyBlur(_originalModel, localGenerationData.BlurSigma);

            var brightnessMap = new BrightnessMap(blurredOriginalModel, new BrightnessMapOptions(localGenerationData.BlurSigma, DerivativesKernelType.Sobel, localGenerationData.StrokeWidth_Max * 2));

            Point newPoint;

            NotifyStatusChange?.Invoke($"{TracingState.Tracing} | Поколение: {generation}");

            switch (generation)
            {
                // Отрисовка первого слоя. Он рисуется, пока не будет достигнуто N% закрашенности. Рисуется случайно из точек, что ещё не были затрнуты.
                case 0:
                    decider = new RandomPointDecider(_originalModel, _randomSeed);

                    while (decider.IsDeciderAvaliable())
                    {
                        if (CheckToken())
                        {
                            NotifyStatusChange?.Invoke($"{TracingState.Cancelled} | Поколение: {generation}");
                            yield break;
                        }

                        try
                        {
                            MakeNewStroke(decider, localGenerationData, brightnessMap);
                        }
                        catch (Exception)
                        {
                            break;
                        }

                        decider.PostStroke();

                        yield return (_artificialRender, _artificialModel);
                    }
                    break;
                // Отрисовка обычных слоёв
                default:

                    decider = new RandomPointDecider(_originalModel, _randomSeed);

                    for (int iteration = 0; iteration < localGenerationData.LayerIterations; iteration++)
                    {
                        if (CheckToken())
                        {
                            NotifyStatusChange?.Invoke($"{TracingState.Cancelled} | Поколение: {generation}");
                            yield break;
                        }

                        try
                        {
                            MakeNewStroke(decider, localGenerationData, brightnessMap);
                        }
                        catch (Exception)
                        {
                            break;
                        }

                        decider.PostStroke();

                        yield return (_artificialRender, _artificialModel);
                    }
                    break;
            }

            yield return (_artificialRender, _artificialModel);
        }
    }

    private void MakeNewStroke(IPointDecider decider, ArtSingleGeneration localGenerationData, BrightnessMap brightnessMap)
    {
        var newPoint = decider.GetNewPoint();

        var tasks = new Task[2];
        var tracingResults = new (SegmentedTracingPathResult tracing, ArtStroke stroke, double standardDeviation)[2];

        CreateTraceSearchTask(0, LineDirection.Forward);
        CreateTraceSearchTask(1, LineDirection.Backwards);

        Task.WaitAll(tasks);

        // Находим минимальный по дисперсии мазок из двух
        var optimalTracingResult = tracingResults.MinBy(r => r.standardDeviation)!;
        var finalArtStroke = optimalTracingResult.stroke;
        var finalTracing = optimalTracingResult.tracing;

        // Фильтр
        if (optimalTracingResult.standardDeviation > localGenerationData.StandardDeviation_Reject_Bound)
            return;

        WritePixelsModel(optimalTracingResult.tracing.Coordinates, optimalTracingResult.tracing.MeanColor);
        WritePixelsRender(optimalTracingResult.stroke, optimalTracingResult.tracing, newPoint, decider);

        // Поиск путей
        void CreateTraceSearchTask(int index, LineDirection direction)
        {
            int i = index;
            tasks[i] = Task.Run(async () =>
            {
                var segmentedTracingResult = await GetSegmentedTracePath(_originalModel, localGenerationData, newPoint, brightnessMap, direction);
                var artStroke = _strokeLibrary.ClassifyStroke(segmentedTracingResult, localGenerationData)!;
                double currentResizeCoef = StrokeLibrary.CalculateResizeCoefficient(segmentedTracingResult, artStroke);

                // Применяем модель Фонга на  мазок
                if (segmentedTracingResult.MeanColor.GetAverage() > PhongReflection.BorderColorBrightness && artStroke.NormalMap != null)
                {
                    var parameters = new PhongReflectionParameters(segmentedTracingResult.MeanColor);
                    var artStrokeNormalMap = artStroke.NormalMap;
                    artStroke.PhongModel = PhongReflection.ApplyReflection(artStroke, artStrokeNormalMap, parameters);
                }

                // Делаем ресайз
                var resizedArtStroke = artStroke.ResizeStroke(currentResizeCoef);

                // Расчёт контура, если такая функция включена
                if (ProgramData.GenerateShapesMap)
                {
                    artStroke.InitShape();
                    artStroke.Shape?.CalculateShape();
                }

                // Делаем поворот как самого мазка, так и его модели фонга
                artStroke.RotateStroke(segmentedTracingResult.MainAngle, Color.White);

                artStroke.PhongModel?.ResizeStroke(currentResizeCoef);
                artStroke.PhongModel?.RotateStroke(segmentedTracingResult.MainAngle, Color.White);
                artStroke.Shape?.Rotate(segmentedTracingResult.MainAngle, Color.Black);

                tracingResults[i].stroke = artStroke;
                tracingResults[i].tracing = segmentedTracingResult;
                tracingResults[i].standardDeviation = CalculateStrokeCanvasError(artStroke, segmentedTracingResult, newPoint);
            });
        }
    }

    private async Task<SegmentedTracingPathResult> GetSegmentedTracePath(
        ArtCanvas artCanvas,
        ArtSingleGeneration generationData,
        Point startingPoint,
        BrightnessMap brightnessMap,
        LineDirection gradientDirection
    )
    {
        var segmentedTracingResult = new SegmentedTracingPathResult();
        segmentedTracingResult.SkeletPath.Add(startingPoint);

        double tolerance = generationData.StandardDeviation_Stroke_Bound;
        int strokeLengthMax = generationData.StrokeLength_Max;
        int strokeLengthMin = 1; // Длина, меньше которой мазок не засчитывается

        segmentedTracingResult.Properties.SetProperty(StrokeProperty.Points, 1);

        var roi = await CircleTracer.TraceIterative(generationData, artCanvas, startingPoint);
        var segmentedCalculator = roi.Calculator;
        var segmentedPathCoordinates = roi.Coordinates;
        segmentedTracingResult.Properties.SetProperty(StrokeProperty.Width, roi.Width);

        var pathSegmentLengths = new List<int>();

        float segmentedLength = 0;

        // Условие, чтобы досрочно прервать формирование мазка
        bool cancel_stroke_path = false;

        // Построение каждого сегмента
        for (int segmentPoint = 1; segmentPoint < Points; segmentPoint++)
        {
            if (cancel_stroke_path)
                break;

            var currentGradientPoint = segmentedTracingResult.SkeletPath[segmentPoint - 1];
            float gradientVectorAngle = 0.0f;

            switch (segmentPoint)
            {
                case 1:
                    var rotationDirection = gradientDirection switch
                    {
                        LineDirection.Forward => RotationDeirection.Clockwise,
                        LineDirection.Backwards => RotationDeirection.CounterClockwise,
                        _ => RotationDeirection.None,
                    };
                    gradientVectorAngle = (float)brightnessMap[currentGradientPoint.X, currentGradientPoint.Y].NormalAngle(rotationDirection);
                    segmentedTracingResult.MainAngle = gradientVectorAngle;
                    break;

                default:
                    var previousGradientPoint = segmentedTracingResult.SkeletPath[segmentPoint - 2];
                    (float absoluteAngle, float angleBetweenVectors) = GetNewGradientAngle(previousGradientPoint, currentGradientPoint, (float)brightnessMap[currentGradientPoint.X, currentGradientPoint.Y]);

                    if (!ValidateAngle(angleBetweenVectors))
                    {
                        cancel_stroke_path = true;
                        break;
                    }

                    segmentedTracingResult.Properties.SetProperty(StrokeProperty.Angle1, angleBetweenVectors.RadToDeg());
                    break;
            }

            if (cancel_stroke_path)
                break;

            var tracingTasks = new Task[strokeLengthMax - strokeLengthMin + 1];
            var tracingPaths = new LinearTracingPathResult[strokeLengthMax - strokeLengthMin + 1];
            for (int length = strokeLengthMin; length <= strokeLengthMax; length++)
            {
                int index = length - strokeLengthMin;
                int lengthIndexed = length;

                tracingTasks[index] = Task.Run(() =>
                {
                    var offsetedPoint = currentGradientPoint.GetOffsetClamp(gradientVectorAngle, lengthIndexed, artCanvas.Width - 1, artCanvas.Height - 1);
                    var searchData = new LinearTracingPathSearchData()
                    {
                        Length = lengthIndexed,
                        Width = roi.Width,
                        PointStart = currentGradientPoint,
                        PointEnd = offsetedPoint
                    };
                    var linearTracePath = LinearPathTracer.GetLinearPath(artCanvas, searchData, segmentedPathCoordinates, segmentedCalculator);
                    tracingPaths[index] = linearTracePath;
                });
            }

            await Task.WhenAll(tracingTasks);

            cancel_stroke_path = true;
            for (int i = tracingPaths.Length - 1; i >= 0; i--)
            {
                var tracingPath = tracingPaths[i];

                // Найден путь, у которого дисперсия допустимая
                if (tracingPath.StandardDeviation <= tolerance)
                {
                    var pathLength = tracingPath.TracingPathSearchData.Length;

                    // Путь является минимально возможным - значит окончить мазок
                    if (pathLength <= strokeLengthMin)
                    {
                        cancel_stroke_path = true;
                        break;
                    }
                    else
                    {
                        segmentedLength += pathLength;
                        segmentedTracingResult.StandardDeviation = tracingPath.StandardDeviation;
                        segmentedTracingResult.Properties.SetProperty(StrokeProperty.Length, segmentedLength);
                        segmentedTracingResult.Properties.SetProperty(StrokeProperty.LtoW, segmentedLength / roi.Width);

                        var pathLengthFraction = (segmentedLength - pathLength) * 100 / segmentedLength;
                        if (segmentPoint > 2 && !ValidateFraction(pathLengthFraction))
                            break;

                        segmentedTracingResult.Properties.SetProperty(StrokeProperty.Fraction, pathLengthFraction);
                        segmentedCalculator = tracingPath.Calculator;
                        segmentedPathCoordinates.UnionWith(tracingPath.Coordinates);

                        segmentedTracingResult.Properties.SetProperty(StrokeProperty.Points, segmentPoint);

                        segmentedTracingResult.SkeletPath.Add(tracingPath.TracingPathSearchData.PointEnd);
                        pathSegmentLengths.Add(pathLength);

                        cancel_stroke_path = false;
                        break;
                    }
                }
            }
        }

        segmentedTracingResult.Coordinates = segmentedPathCoordinates;
        segmentedTracingResult.MeanColor = segmentedCalculator.GetMeanColor();

        return segmentedTracingResult;
    }

    private static bool ValidateAngle(float angleBetweenVectors)
    {
        return angleBetweenVectors % Math.Tau < 2.8;
    }

    private static bool ValidateFraction(double fractionPercent)
    {
        return fractionPercent > 10 && fractionPercent < 90;
    }

    private static (float absoluteAngle, float angleBetweenVectors) GetNewGradientAngle(Point previousPoint, Point currentPoint, float gradientMapAngle)
    {
        // Взяли нормаль к углу
        gradientMapAngle = gradientMapAngle.NormalAngle();

        // Нашли 3ю точку по вектору
        var newPoint = currentPoint.GetOffset(gradientMapAngle, 10);

        // Относительный угол между тремя точками, от 0 до PI
        float angleBetwreenVectors = previousPoint.AngleBy3Points(currentPoint, newPoint);

        // Если относительный угол между сегментами мазка оказался острым, разворачиваем на 180 градусов всё
        if (angleBetwreenVectors < MathF.PI / 2)
        {
            angleBetwreenVectors = MathF.PI - angleBetwreenVectors;
            gradientMapAngle += MathF.PI;
            newPoint = currentPoint.GetOffset(gradientMapAngle, 10);
        }

        // На этом этапе у нас точно есть правильный расчётный сегмента из трёх точек
        return (gradientMapAngle, angleBetwreenVectors);
    }

    private double CalculateStrokeCanvasError(ArtStroke artStroke, SegmentedTracingPathResult segmentedTracingPathResult, Point strokeStartPoint)
    {
        strokeStartPoint = strokeStartPoint.GetOffsetClamp((float)segmentedTracingPathResult.MainAngle + MathF.PI, segmentedTracingPathResult.Properties.GetProperty(StrokeProperty.Width) / 2, _artificialRender.Width, _artificialRender.Height);

        double disperson = 0;
        int counter = 0;

        foreach (var (localX, localY) in artStroke)
        {
            var globalPoint = new Point(strokeStartPoint.X - artStroke.PivotPoint.X + localX, strokeStartPoint.Y - artStroke.PivotPoint.Y + localY);

            if (_artificialRender.IsInside(globalPoint))
            {
                var strokeAlpha = artStroke.GetAlpha(new Point(localX, localY));
                if (strokeAlpha < BlackBorderConstants.BlackBorderMedium)
                {
                    Color renderColor;
                    Color originalColor = _originalModel[globalPoint.X, globalPoint.Y];

                    if (artStroke.PhongModel != null)
                    {
                        renderColor = artStroke.PhongModel![localX, localY];
                    }
                    else
                    {
                        renderColor = _artificialRender[globalPoint.X, globalPoint.Y].CalculateAlpha(segmentedTracingPathResult.MeanColor, (255.0 - strokeAlpha) / 255.0);
                    }

                    disperson += renderColor.CalculateSquaredEuclideanDistance(originalColor);
                    counter++;
                }
            }
        }

        if (counter == 0)
            counter = 1;

        return Math.Sqrt(disperson / counter);
    }

    private void WritePixelsModel(HashSet<Point> coordonates, Color color)
    {
        foreach (var c in coordonates)
        {
            _artificialModel[c.X, c.Y] = color;
        }
    }

    private void WritePixelsRender(ArtStroke artStroke, SegmentedTracingPathResult segmentedTracingPathResult, Point strokeStartPoint, IPointDecider? decider = null)
    {
        strokeStartPoint = strokeStartPoint.GetOffsetClamp((float)segmentedTracingPathResult.MainAngle + MathF.PI, segmentedTracingPathResult.Properties.GetProperty(StrokeProperty.Width) / 2, _artificialRender.Width, _artificialRender.Height);

        if (ProgramData.GenerateShapesMap)
        {
            CanvasShapeGenerator.OpenNewStroke(segmentedTracingPathResult.MeanColor);
            CanvasShapeGenerator.AddStrokeSkelet(segmentedTracingPathResult.SkeletPath);
        }

        foreach (var (localX, localY) in artStroke)
        {
            var globalPoint = new Point(strokeStartPoint.X - artStroke.PivotPoint.X + localX, strokeStartPoint.Y - artStroke.PivotPoint.Y + localY);

            if (_artificialRender.IsInside(globalPoint))
            {
                // Наложение обычного мазка
                var strokeAlpha = artStroke.GetAlpha(new Point(localX, localY));
                if (strokeAlpha < BlackBorderConstants.BlackBorderMedium)
                {
                    // Наложение мазка с фонгом
                    if (artStroke.PhongModel != null)
                    {
                        _artificialRender[globalPoint.X, globalPoint.Y] = artStroke.PhongModel[localX, localY];
                    }
                    // Наложение обычного мазка
                    else
                    {
                        var colorToDraw = _artificialRender[globalPoint.X, globalPoint.Y].CalculateAlpha(segmentedTracingPathResult.MeanColor, (255.0 - strokeAlpha) / 255.0);
                        _artificialRender[globalPoint.X, globalPoint.Y] = colorToDraw;
                    }

                    decider?.PointCallback(globalPoint);
                }

                if (ProgramData.GenerateShapesMap && artStroke.Shape != null)
                {
                    var shapePixel = artStroke.Shape.GetPixelShapeType(new Point(localX, localY));
                    CanvasShapeGenerator.AddPixel(globalPoint, shapePixel);
                }
            }
        }
    }

    private bool CheckToken() => _token.IsCancellationRequested;

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

file class TracingState
{
    public static readonly TracingState Prepare = new() { Locale = "Подготовка" };
    public static readonly TracingState Blurring = new() { Locale = "Блюр" };
    public static readonly TracingState Tracing = new() { Locale = "Отрисовка" };
    public static readonly TracingState Shapes = new() { Locale = "Создание формы/скелетов" };
    public static readonly TracingState Cancelled = new() { Locale = "Отменено" };

    public string Locale { get; init; } = string.Empty;

    public override string ToString() => Locale;
}
