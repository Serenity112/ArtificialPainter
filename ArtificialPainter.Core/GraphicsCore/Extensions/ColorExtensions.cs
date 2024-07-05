using System.Drawing;

namespace ArtificialPainter.Core.GraphicsCore.Extensions;

public static class ColorExtensions
{
    public static Color Add(this Color color, Color colorToAdd)
    {
        int R = Math.Clamp(color.R + colorToAdd.R, 0, 255);
        int G = Math.Clamp(color.G + colorToAdd.G, 0, 255);
        int B = Math.Clamp(color.B + colorToAdd.B, 0, 255);
        return Color.FromArgb(R, G, B);
    }
}
