using ArtificialPainter.Core.GraphicsCore.Canvas;
using ArtificialPainter.Core.MathLibrary.Extensions;
using ArtificialPainter.Core.Strokes.Normal;
using ArtificialPainter.Core.Strokes.Phong;
using ArtificialPainter.Core.Strokes.StrokeShape;
using ArtificialPainter.Core.Utils;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace ArtificialPainter.Core.Strokes;

public class ArtStroke(Bitmap bitmap) : ArtCanvas(bitmap)
{
    public ArtStrokeNormalMap? NormalMap { get; set; }

    public IStrokeShape? Shape { get; set; }

    public ArtStrokePhong? PhongModel { get; set; }

    public StrokePropertyCollection<double> Properties { get; set; } = [];

    public Point PivotPoint { get; private set; }

    public new ArtStroke Copy()
    {
        var clonedBitmap = (Bitmap)Bitmap.Clone();
        return new ArtStroke(clonedBitmap)
        {
            Properties = Properties,
            PivotPoint = PivotPoint,
            NormalMap = NormalMap
        };
    }

    public ArtStroke FlipStroke(RotateFlipType flipType)
    {
        var mirroredBitmap = (Bitmap)Bitmap.Clone();
        mirroredBitmap.RotateFlip(flipType);
        var flippedArtStroke = new ArtStroke(mirroredBitmap)
        {
            NormalMap = NormalMap?.FlipStroke(flipType) as ArtStrokeNormalMap
        };

        return flippedArtStroke;
    }

    public ArtStroke ResizeStroke(double coefficient)
    {
        if (coefficient == 1)
            return Copy();

        coefficient = Math.Clamp(coefficient, 0.001, 100000);

        int newWidth = (int)Math.Ceiling(Width * coefficient);
        int newHeight = (int)Math.Ceiling(Height * coefficient);
        var resizedBitmap = new Bitmap(newWidth, newHeight);
        resizedBitmap.SetResolution(Bitmap.HorizontalResolution, Bitmap.VerticalResolution);

        using (Graphics g = Graphics.FromImage(resizedBitmap))
        {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.DrawImage(resizedBitmap, new Rectangle(0, 0, Width, Height));
        }

        var resizedStroke = new ArtStroke(resizedBitmap);

        return resizedStroke;
    }

    public ArtStroke RotateStroke(float rotationAngle, Color fillColor)
    {
        float relAngle = rotationAngle - MathF.PI / 2;

        float cosA = MathF.Abs(MathF.Cos(relAngle));
        float sinA = MathF.Abs(MathF.Sin(relAngle));
        int newWidth = (int)(cosA * Width + sinA * Height);
        int newHeight = (int)(cosA * Height + sinA * Width);

        var originalPivot = RecalculatePivotPoint(BlackBorderConstants.BlackBorderMedium);
        originalPivot = new Point(originalPivot.X - Width / 2, originalPivot.Y - Height / 2);
        originalPivot = originalPivot.RotatePoint(rotationAngle + 1.5f * MathF.PI);
        var newPivotPoint = new Point(originalPivot.X + newWidth / 2, originalPivot.Y + newHeight / 2);

        var rotatedBitmap = new Bitmap(newWidth, newHeight);
        rotatedBitmap.SetResolution(Bitmap.HorizontalResolution, Bitmap.VerticalResolution);

        using (Graphics graphics = Graphics.FromImage(rotatedBitmap))
        {
            using (var brush = new SolidBrush(fillColor))
            {
                graphics.FillRectangle(brush, 0, 0, newWidth, newHeight);
            }

            graphics.TranslateTransform(rotatedBitmap.Width / 2, rotatedBitmap.Height / 2);
            graphics.RotateTransform((float)(-relAngle * 180 / MathF.PI));
            graphics.TranslateTransform(-Bitmap.Width / 2, -Bitmap.Height / 2);
            graphics.DrawImage(rotatedBitmap, new Point(0, 0));
        }

        var resizedStroke = new ArtStroke(rotatedBitmap)
        {
            PivotPoint = newPivotPoint,
            Properties = Properties
        };

        return resizedStroke;
    }

    public void InitShape()
    {
        Shape = new ArtStrokeShape(this);
    }

    public Point RecalculatePivotPoint(byte blackBorder)
    {
        int x1 = 0;
        int x2 = Width;

        for (int i = 0; i < Width; i++)
        {
            if (this[i, 3].R <= blackBorder)
            {
                x1 = i;
                break;
            }
        }

        for (int i = Width - 1; i > 0; i--)
        {
            if (this[i, 3].R <= blackBorder)
            {
                x2 = i;
                break;
            }
        }
        PivotPoint = new Point((x1 + x2) / 2, 0);
        return PivotPoint;
    }

    public float GetAlpha(Point point)
    {
        // Мазкок в оттенках серого, раскаршивается только при наложении. Поэтому любая компонента из RGB - это альфа
        return this[point.X, point.Y].R;
    }
}
