using ArtificialPainter.Core.GraphicsCore.Canvas;
using ArtificialPainter.Core.GraphicsCore.Structs;
using ArtificialPainter.Core.MathLibrary;
using System.Drawing;

namespace ArtificialPainter.Core.Strokes.StrokeShape;

public class CanvasShapeGenerator
{
    private readonly Matrix2D<List<(ushort index, ShapeType shapeType)>> _shapesCanvas;

    private ushort _currentIndex = 0;

    private readonly Dictionary<int, ArtStrokeShapeData> _shapeDatas;

    private ArtStrokeShapeData _currentShapeData;

    private const double VisibilityFraction = 0.03;

    public CanvasShapeGenerator(ArtCanvas artCanvas)
    {
        _shapeDatas = [];
        _shapesCanvas = new(artCanvas.Width, artCanvas.Height);

        foreach (var (x, y) in _shapesCanvas)
        {
            _shapesCanvas[x, y] = [];
        }
        _currentShapeData = new ArtStrokeShapeData();
    }

    public void OpenNewStroke(Color color)
    {
        _currentIndex++;

        _currentShapeData = new ArtStrokeShapeData()
        {
            ShapeColor = color,
        };

        _shapeDatas.Add(_currentIndex, _currentShapeData);
    }

    public void AddPixel(Point point, ShapeType shapeType)
    {
        var pointSet = _shapesCanvas[point.X, point.Y];
        ushort index = pointSet.Count > 0 ? pointSet.LastOrDefault().index : (ushort)0;

        // Значит на этой точке уже есть какой-то мазок
        if (index != 0)
        {
            var shapeData = _shapeDatas[index];
            shapeData.CurrentSize -= 1;

            if (shapeData.GetFraction() < VisibilityFraction)
            {
                ClearShapeFromCanvas(index);
            }
        }

        // Заполняем пиксель признаком, что это новый мазок
        _shapesCanvas[point.X, point.Y].Add((_currentIndex, shapeType));
        _currentShapeData.IntialSize += 1;
        _currentShapeData.CurrentSize += 1;
    }

    private void ClearShapeFromCanvas(ushort index)
    {
        foreach (var (x, y) in _shapesCanvas)
        {
            foreach (var tuple in _shapesCanvas[x, y])
            {
                if (tuple.index == index)
                {
                    _shapesCanvas[x, y].Remove(tuple);
                    break;
                }
            }
        }

        _shapeDatas.Remove(index);
    }

    public void AddStrokeSkelet(List<Point> skeletPoints)
    {
        _currentShapeData.SkeletPoints = skeletPoints;
    }

    public (ArtCanvas shapesCanvas, ArtCanvas skeletCanvas) CreateCanvases()
    {
        var shapesCanvas = new ArtCanvas(_shapesCanvas.Width, _shapesCanvas.Height);
        shapesCanvas.FillColor(Color.White);

        foreach (var (x, y) in _shapesCanvas)
        {
            foreach (var pixel in _shapesCanvas[x, y])
            {
                ushort index = pixel.index;
                var isEdge = pixel.shapeType == ShapeType.Edge;
                var shapeData = _shapeDatas[index];

                if (isEdge)
                {
                    shapesCanvas[x, y] = shapeData.ShapeColor;
                }
                else
                {
                    shapesCanvas[x, y] = Color.White;
                }
            }
        }

        var skeletCanvas = new ArtCanvas(_shapesCanvas.Width, _shapesCanvas.Height);
        skeletCanvas.FillColor(Color.White);

        foreach (var shape in _shapeDatas)
        {
            var skeletPoints = shape.Value.SkeletPoints;

            for (int i = 0; i < skeletPoints.Count - 1; i++)
            {
                var p1 = skeletPoints[i];
                var p2 = skeletPoints[i + 1];

                foreach (var point in GraphicsMath.GetLinePoints(p1, p2))
                {
                    skeletCanvas[point.X, point.Y] = shape.Value.ShapeColor;
                }
            }
        }

        return (shapesCanvas, skeletCanvas);
    }
}


