using ArtificialPainter.Core.GraphicsCore.Euclidean;
using ArtificialPainter.Core.GraphicsCore.Extensions;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;

namespace ArtificialPainter.Core.GraphicsCore.Canvas;

#nullable disable
public unsafe class ArtCanvas : EuclideanSpace2DBase<Color>, IArtCanvas
{
    public override int Width { get; init; }

    public override int Height { get; init; }

    public string Title { get; init; }

    private BitmapData _bitmapData;

    private unsafe byte* _pixelData;

    public Bitmap Bitmap { get; private set; }

    public ArtCanvas(Bitmap bitmap, string title = null)
    {
        Bitmap = bitmap;
        Width = bitmap.Width;
        Height = bitmap.Height;
        Title = title ?? Guid.NewGuid().ToString();

        Lock();
    }

    public ArtCanvas(int width, int height, string title = null) : this(new Bitmap(width, height), title)
    {
    }

    public override Color this[int x, int y]
    {
        get
        {
            if (this.IsInside(x, y))
            {
                int position = (Height - y - 1) * _bitmapData.Stride + x * 3;
                byte blue = _pixelData[position];
                byte green = _pixelData[position + 1];
                byte red = _pixelData[position + 2];
                return Color.FromArgb(255, red, green, blue);
            }
            else
            {
                throw new IndexOutOfRangeException("Координаты вне холста");
            }
        }
        set
        {
            if (this.IsInside(x, y))
            {
                int position = (Height - y - 1) * _bitmapData.Stride + x * 3;
                _pixelData[position] = value.B;
                _pixelData[position + 1] = value.G;
                _pixelData[position + 2] = value.R;
            }
        }
    }

    public void Lock()
    {
        var rect = new Rectangle(0, 0, Width, Height);
        _bitmapData = Bitmap.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
        _pixelData = (byte*)_bitmapData.Scan0;
    }

    public void Unlock()
    {
        Bitmap.UnlockBits(_bitmapData);
    }

    public void WriteToFile(string outputPath, string title = null)
    {
        try
        {
            Bitmap.Save($"{outputPath}\\{title ?? Title}.{ImageFormat.Png}");
        }
        catch
        {
            Debug.WriteLine($"Ошибка записи {title ?? Title} в файл {outputPath}");
        }
    }

    public IArtCanvas Copy()
    {
        return new ArtCanvas((Bitmap)Bitmap.Clone(), Title);
    }

    public void FillColor(Color color)
    {
        Unlock();
        using (var g = Graphics.FromImage(Bitmap))
        {
            using var brush = new SolidBrush(color);
            g.FillRectangle(brush, 0, 0, Width, Height);
        }
        Lock();
    }
}
#nullable restore
