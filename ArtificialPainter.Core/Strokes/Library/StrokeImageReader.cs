using ArtificialPainter.Core.Utils;
using System.Drawing;

namespace ArtificialPainter.Core.Strokes.Library;

public class StrokeImageReader
{
    public static Bitmap ReadBitmapCropped(Bitmap original, StrokeType strokeType)
    {
        if (strokeType == StrokeType.Stroke)
            ConvertToGrayScale(original);

        return CropImage(original, GetCropRectangle(original, strokeType));
    }

    private static void ConvertToGrayScale(Bitmap original)
    {
        for (int x = 0; x < original.Width; x++)
        {
            for (int y = 0; y < original.Height; y++)
            {
                var pixelColor = original.GetPixel(x, y);
                int grayValue = (int)(pixelColor.R * 0.3 + pixelColor.G * 0.59 + pixelColor.B * 0.11);
                var grayColor = Color.FromArgb(grayValue, grayValue, grayValue);
                original.SetPixel(x, y, grayColor);
            }
        }
    }

    private static Rectangle GetCropRectangle(Bitmap image, StrokeType strokeOrNormal)
    {
        int left = 0, top = 0, right = image.Width - 1, bottom = image.Height - 1;

        Func<Color, bool> borderCheck = strokeOrNormal == StrokeType.Stroke ?
            (col) => col.R < BlackBorderConstants.BlackBorderMedium :
            (col) => col.R != 128; // 128 - фоновый голубой лцвет на картах нормалей

        // Находим левый край
        for (int x = 0; x < image.Width; x++)
        {
            for (int y = 0; y < image.Height; y++)
            {
                if (borderCheck(image.GetPixel(x, y)))
                {
                    left = x;
                    break;
                }
            }
            if (left != 0)
                break;
        }

        // Находим верхний край
        for (int y = 0; y < image.Height; y++)
        {
            for (int x = left; x < image.Width; x++)
            {
                if (borderCheck(image.GetPixel(x, y)))
                {
                    top = y;
                    break;
                }
            }
            if (top != 0)
                break;
        }

        // Находим правый край
        for (int x = image.Width - 1; x >= left; x--)
        {
            for (int y = top; y < image.Height; y++)
            {
                if (borderCheck(image.GetPixel(x, y)))
                {
                    right = x;
                    break;
                }
            }
            if (right != image.Width - 1)
                break;
        }

        // Находим нижний край
        for (int y = image.Height - 1; y >= top; y--)
        {
            for (int x = left; x <= right; x++)
            {
                if (borderCheck(image.GetPixel(x, y)))
                {
                    bottom = y;
                    break;
                }
            }
            if (bottom != image.Height - 1)
                break;
        }

        return new Rectangle(left, top, right - left + 1, bottom - top + 1);
    }

    private static Bitmap CropImage(Bitmap originalBitmap, in Rectangle cropRectangle)
    {
        var croppedBitmap = new Bitmap(cropRectangle.Width, cropRectangle.Height);

        for (int x = cropRectangle.Left; x < cropRectangle.Right; x++)
        {
            for (int y = cropRectangle.Bottom; y < cropRectangle.Top; y++)
            {
                var pixelColor = originalBitmap.GetPixel(x, y);
                croppedBitmap.SetPixel(x, y, pixelColor);
            }
        }

        return croppedBitmap;
    }
}
