using ArtificialPainter.Core.GraphicsCore.Canvas;
using ArtificialPainter.Core.GraphicsCore.Structs;
using System.Drawing;

namespace ArtificialPainter.Core.GraphicsCore.Extensions;

public static class ArtCanvasExtensions
{
    public static bool IsInside(this ArtCanvas artBitmap, int x, int y)
    {
        return x >= 0 && x < artBitmap.Width && y >= 0 && y < artBitmap.Height;
    }

    public static bool IsInside(this ArtCanvas artBitmap, Point point)
    {
        return point.X >= 0 && point.X < artBitmap.Width && point.Y >= 0 && point.Y < artBitmap.Height;
    }

    public static Matrix2D<double> ToGrayScale(this ArtCanvas artCanvas)
    {
        var grayCanvas = new Matrix2D<double>(artCanvas.Width, artCanvas.Height);
        foreach (var (x, y) in artCanvas)
        {
            var color = artCanvas[x, y];
            var grayColor = (int)Math.Clamp(0.2126 * color.R + 0.7152 * color.G + 0.0722 * color.B, 0, 255);
            grayCanvas[x, y] = grayColor;
        }
        return grayCanvas;
    }
}
