using ArtificialPainter.Core.Strokes.Normal;
using ArtificialPainter.Core.Utils;
using System.Drawing;
using System.Numerics;

namespace ArtificialPainter.Core.Strokes.Phong;

public class PhongReflection
{
    public const double BorderColorBrightness = (200 + 200 + 200) / 3;

    public static ArtStrokePhong ApplyReflection(ArtStroke stroke, ArtStrokeNormalMap normalsMap, PhongReflectionParameters parameters)
    {
        var reflectionStroke = new ArtStrokePhong(new Bitmap(stroke.Width, stroke.Height))
        {
            //SP = stroke.SP,
            //PivotPoint = stroke.PivotPoint
        };

        var ambientColor = parameters.AmbientColor;
        var specularColor = parameters.SpecularColor;
        var lightDirection = parameters.LightDirection;

        foreach (var (x, y) in stroke)
        {
            var normalPixel = normalsMap[x, y];

            if (stroke[x, y].R < BlackBorderConstants.BlackBorderStrong)
            {
                float r = normalPixel.R / 255f * 2 - 1;
                float g = normalPixel.G / 255f * 2 - 1;
                float b = (normalPixel.B - 128) / 128f * -1;
                var normalVector = new Vector3(r, g, b);

                // Эмбиент составляющая
                float ambient = parameters.AmbientStrenght;

                // Диффузная составляющая
                float diffusion = Math.Clamp(Vector3.Dot(normalVector, lightDirection), 0, 255);

                // Бликовая составляющая
                Vector3 reflection = normalVector * 2 * diffusion - lightDirection;
                double specular = (float)Math.Pow(Vector3.Dot(reflection, parameters.ObserverDirection), parameters.Shininess);

                double red = ambient * ambientColor.R + parameters.DiffuseStrenght * diffusion * ambientColor.R + parameters.SpecularStrenght * specular * specularColor.R;
                double green = ambient * ambientColor.G + parameters.DiffuseStrenght * diffusion * ambientColor.G + parameters.SpecularStrenght * specular * specularColor.G;
                double blue = ambient * ambientColor.B + parameters.DiffuseStrenght * diffusion * ambientColor.B + parameters.SpecularStrenght * specular * specularColor.B;

                red = Math.Clamp(red, 0, 255);
                green = Math.Clamp(green, 0, 255);
                blue = Math.Clamp(blue, 0, 255);

                reflectionStroke[x, y] = Color.FromArgb((byte)red, (byte)green, (byte)blue);
            }
            else
            {
                reflectionStroke[x, y] = Color.White;
            }
        }

        return reflectionStroke;
    }
}

public class PhongReflectionParameters(Color ambientColor)
{
    public float AmbientStrenght { get; set; } = 0.5f;
    public float DiffuseStrenght { get; set; } = 0.5f;
    public float SpecularStrenght { get; set; } = 0.5f;
    public int Shininess { get; set; } = 40;

    public Vector3 LightDirection { get; set; } = new(0.16f, 0.16f, -0.97f);
    public Vector3 ObserverDirection { get; set; } = new(0f, 0f, -1f);

    public Color AmbientColor { get; set; } = ambientColor;
    public Color SpecularColor { get; set; } = Color.FromArgb(255, 255, 255);
}
