using System.Drawing;

namespace ArtificialPainter.Core.MathLibrary.Extensions;

public static class ColorExtensions
{
    public static Color CalculateAlpha(this in Color backColor, in Color frontColor, double alpha)
    {
        return Color.FromArgb(
            Math.Clamp((int)(alpha * frontColor.R + (1 - alpha) * backColor.R), 0, 255),
            Math.Clamp((int)(alpha * frontColor.G + (1 - alpha) * backColor.G), 0, 255),
            Math.Clamp((int)(alpha * frontColor.B + (1 - alpha) * backColor.B), 0, 255));
    }

    public static double CalculateSquaredEuclideanDistance(this in Color color1, in Color color2)
    {
        double R_sq = Math.Pow(color1.R - color2.R, 2);
        double G_sq = Math.Pow(color1.G - color2.G, 2);
        double B_sq = Math.Pow(color1.B - color2.B, 2);
        return R_sq + G_sq + B_sq;
    }

    public static double GetAverage(this in Color color)
    {
        return (byte)Math.Clamp((color.R + color.G + color.B) / 3, 0, 255);
    }
}
