using ArtificialPainter.Core.GraphicsCore.Canvas;
using ArtificialPainter.Core.MathLibrary;
using ArtificialPainter.Core.Utils;
using System.Drawing;

namespace ArtificialPainter.Core.Strokes.StrokeShape;

public class ArtStrokeShape : IStrokeShape
{
    private static readonly Color _fillerColor = Color.FromArgb(255, 255, 255, 255);

    private ArtStroke _artStrokeShape;
    private readonly ArtStroke _sourceStroke;
    private readonly ArtStrokeEdge _strokeEdge;

    private int Width;
    private int Height;

    public ArtStrokeShape(ArtStroke artStroke)
    {
        _sourceStroke = artStroke;
        _strokeEdge = new ArtStrokeEdge();

        Width = artStroke.Width;
        Height = artStroke.Height;
    }

    public ArtStroke GetShape()
    {
        return _artStrokeShape;
    }

    public void Rotate(float rotationAngle, Color fillColor)
    {
        double relAngle = rotationAngle - MathF.PI / 2;
        double cosA = Math.Abs(Math.Cos(relAngle));
        double sinA = Math.Abs(Math.Sin(relAngle));
        int newWidth = (int)(cosA * Width + sinA * Height);
        int newHeight = (int)(cosA * Height + sinA * Width);

        double angle = rotationAngle + 1.5 * MathF.PI;
        double sin = Math.Sin(angle);
        double cos = Math.Cos(angle);

        HashSet<Point> rotatedEdges = [];
        foreach (var edge in _strokeEdge.Edges.Values)
        {
            for (int i = 0; i < edge.Count - 1; i++)
            {
                var p1 = RotatePoint(edge.ElementAt(i));
                var p2 = RotatePoint(edge.ElementAt(i + 1));

                if (MathF.Abs(p1.X - p2.X) > 1 || MathF.Abs(p1.Y - p2.Y) > 1)
                {
                    foreach (var p in GraphicsMath.GetLinePoints(p1, p2))
                    {
                        rotatedEdges.Add(p);
                    }
                }
                rotatedEdges.Add(p1);
                rotatedEdges.Add(p2);
            }
        }

        _artStrokeShape.Unlock();
        var sourceBitmap = _artStrokeShape.Bitmap;

        var rotatedBitmap = new ArtCanvas(newWidth, newHeight);
        rotatedBitmap.Unlock();
        rotatedBitmap.Bitmap.SetResolution(sourceBitmap.HorizontalResolution, sourceBitmap.VerticalResolution);

        using (Graphics g = Graphics.FromImage(rotatedBitmap.Bitmap))
        {
            using (SolidBrush brush = new SolidBrush(fillColor))
            {
                g.FillRectangle(brush, 0, 0, newWidth, newHeight);
            }

            g.TranslateTransform(rotatedBitmap.Width / 2, rotatedBitmap.Height / 2);
            g.RotateTransform((float)(-relAngle * 180 / Math.PI));
            g.TranslateTransform(-sourceBitmap.Width / 2, -sourceBitmap.Height / 2);
            g.DrawImage(sourceBitmap, new Point(0, 0));
        }

        sourceBitmap.Dispose();

        _artStrokeShape = new ArtStroke(rotatedBitmap.Bitmap);

        Width = newWidth;
        Height = newHeight;

        foreach (var p in rotatedEdges)
        {
            _artStrokeShape[p.X, p.Y] = Color.Red;
        }

        Point RotatePoint(Point p)
        {
            var point = new Point(p.X - Width / 2, p.Y - Height / 2);
            int xnew = (int)(p.X * cos - p.Y * sin);
            int ynew = (int)(p.X * sin + p.Y * cos);
            return new Point(xnew + newWidth / 2, ynew + newHeight / 2);
        }
    }

    public ShapeType GetPixelShapeType(Point point)
    {
        if (_artStrokeShape[point.X, point.Y].R == 255 && _artStrokeShape[point.X, point.Y].G == 0 && _artStrokeShape[point.X, point.Y].B == 0) // Color == Color.Red fix
            return ShapeType.Filler;
        else
            return ShapeType.Edge;
    }

    public void CalculateShape()
    {
        var artCanvas = new ArtCanvas(Width, Height);
        artCanvas.FillColor(Color.Black);

        int x1_prev = 0;
        int x2_prev = 0;

        // Горизонталь
        for (int y = 0; y < Height; y++)
        {
            byte shape_found = 0;

            int x1 = 0;
            int x2 = 0;

            // Скан слева
            for (int x = 0; x < Width; x++)
            {
                if (_sourceStroke.GetAlpha(new Point(x, y)) < BlackBorderConstants.BlackBorderMedium)
                {
                    shape_found += 1;
                    x1 = x;
                    break;
                }
            }

            // Скан справа
            for (int x = Width - 1; x >= 0; x--)
            {
                if (_sourceStroke.GetAlpha(new Point(x, y)) < BlackBorderConstants.BlackBorderMedium)
                {
                    shape_found += 1;
                    x2 = x;
                    break;
                }
            }

            if (shape_found < 2)
            {
                // Значит мы над мазком раньше чем планировалось
                if (x1_prev != 0 && x2_prev != 0)
                {
                    foreach (var point in GraphicsMath.GetLinePoints(new Point(x1_prev, y), new Point(x2_prev, y)))
                        artCanvas[point.X, point.Y] = _fillerColor;
                    break;
                }

                // Ждём, пока не будет  найдена первая пара иксов, слево и спрао
                continue;
            }

            // Нижний 
            if (x1_prev == 0 && x2_prev == 0)
            {
                x1 = (x1 + x2) / 2;
                x2 = x1;
                artCanvas[x1, y] = _fillerColor;
            }

            // Верхний
            else
            if (y == Height - 1)
            {
                x1 = (x1 + x2) / 2;
                x2 = x1;
                artCanvas[x1, y] = _fillerColor;
            }

            //Обычная грань
            else
            {
                x1 = (x1 + x1_prev) / 2;
                x2 = (x2 + x2_prev) / 2;

                for (int x_i = x1; x_i <= x2; x_i++)
                {
                    artCanvas[x_i, y] = _fillerColor;
                }
            }

            x1_prev = x1;
            x2_prev = x2;
        }

        int y1_prev = 0;
        int y2_prev = 0;
        // Вертикаль
        for (int x = 0; x < Width; x++)
        {
            byte shape_found = 0;

            int y1 = 0;
            int y2 = 0;

            // Скан снизу
            for (int y = 0; y < Height; y++)
            {
                if (artCanvas[x, y].G == 255)
                {
                    shape_found += 1;
                    y1 = y;
                    break;
                }
            }

            // Скан сверху
            for (int y = Height - 1; y >= 0; y--)
            {
                if (artCanvas[x, y].G == 255)
                {
                    shape_found += 1;
                    y2 = y;
                    break;
                }
            }

            if (shape_found == 0)
            {
                if (y1_prev != 0 && y2_prev != 0)
                {
                    foreach (var point in GraphicsMath.GetLinePoints(new Point(x, y1_prev), new Point(x, y2_prev)))
                        _strokeEdge.Edges[Edge.Right].Add(point);
                    break;
                }
                continue;
            }

            // Левый 
            if (y1_prev == 0 && y2_prev == 0)
            {
                foreach (var point in GraphicsMath.GetLinePoints(new Point(x, y1), new Point(x, y2)))
                    _strokeEdge.Edges[Edge.Left].Add(point);
            }
            // Правый
            else if (x == Width - 1)
            {
                foreach (var p in GraphicsMath.GetLinePoints(new Point(x, y1), new Point(x, y2)))
                {
                    _strokeEdge.Edges[Edge.Right].Add(p);
                }
            }

            //Обычная грань
            else
            {
                for (int y_i = y1; y_i <= y2; y_i++)
                    artCanvas[x, y_i] = _fillerColor;

                foreach (var p in GraphicsMath.GetLinePoints(new Point(x - 1, y1_prev), new Point(x, y1)))
                    _strokeEdge.Edges[Edge.Bottom].Add(p);

                foreach (var p in GraphicsMath.GetLinePoints(new Point(x - 1, y2_prev), new Point(x, y2)))
                    _strokeEdge.Edges[Edge.Top].Add(p);
            }

            y1_prev = y1;
            y2_prev = y2;
        }

        artCanvas.Unlock();
        _artStrokeShape = new ArtStroke(artCanvas.Bitmap);
    }
}
