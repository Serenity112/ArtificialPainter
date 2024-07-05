using ArtificialPainter.Core.Strokes.Normal;
using System.Drawing;
using System.Text.RegularExpressions;

namespace ArtificialPainter.Core.Strokes.Library;

public static partial class StrokeLibraryReader
{
    public static void ReadAllStrokes(string rootPath, StrokeLibraryContainer container, double resizeCoef = 1.0)
    {
        Parallel.ForEach(Directory.GetFiles(rootPath, "*.*", SearchOption.AllDirectories), filePath =>
        {
            // Игонрируем карты нормалей при первом прочтении
            if (filePath.EndsWith("n.*"))
                return;

            string fileName = filePath;
            int dotIndex = fileName.IndexOf('.');
            string fileNameNoFormat = fileName[..dotIndex];
            string fileNameNormalMap = $"{fileNameNoFormat}n.png";

            ArtStroke artStroke;
            int points;

            // Маска мазка
            using (FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read))
            {
                artStroke = new ArtStroke(StrokeImageReader.ReadBitmapCropped((Bitmap)Image.FromStream(fileStream), StrokeType.Stroke));
                artStroke.ResizeStroke(resizeCoef);

                foreach (var kvp in ExtractAttributesFromPath(rootPath, filePath))
                {
                    StrokeProperty property = StrokePropertyCollection<double>.StrokePropertyByAlias(kvp.Key);
                    artStroke.Properties.SetProperty(property, kvp.Value);
                }
                RecaclulateIndirectProperties(artStroke.Properties);

                points = (int)artStroke.Properties.GetProperty(StrokeProperty.Points);
            }

            // Нормаль мазка. Если она не найдена, то будет null в свойстве stroke
            try
            {
                using FileStream fileStream = new(fileNameNormalMap, FileMode.Open, FileAccess.Read);
                var artStrokeNormalMap = new ArtStrokeNormalMap(StrokeImageReader.ReadBitmapCropped((Bitmap)Image.FromStream(fileStream), StrokeType.NormalMap));
                artStrokeNormalMap.ResizeStroke(resizeCoef);
                artStroke.NormalMap = artStrokeNormalMap;
            }
            catch
            {
            }

            container.AddStroke(points, artStroke);
        });
    }

    // Посчитать непрямые свойства, которые идут на основе других
    private static void RecaclulateIndirectProperties(StrokePropertyCollection<double> collection)
    {
        try
        {
            double width = collection.GetProperty(StrokeProperty.Width) * StrokeLibrary.MmTpPxCoef;
            collection.SetProperty(StrokeProperty.Width, width);

            double length = collection.GetProperty(StrokeProperty.Length) * StrokeLibrary.MmTpPxCoef;
            collection.SetProperty(StrokeProperty.Length, length);

            collection.SetProperty(StrokeProperty.LtoW, length / width);

            double angle = collection.GetProperty(StrokeProperty.Angle1);
            collection.SetProperty(StrokeProperty.Angle1, 180 - angle);
        }
        catch { }
    }

    // Получние аттрибутов из пути /w1l5/s1a1 -> {w, 1}, {l, 5}, {s, 1}, {a, 1}
    private static Dictionary<string, int> ExtractAttributesFromPath(string rootPath, string filePath)
    {
        var attributes = new Dictionary<string, int>();

        string relativePath = Path.GetRelativePath(rootPath, filePath);
        string noFormatPath = relativePath[..relativePath.IndexOf('.')];

        string[] components = noFormatPath.Split(Path.DirectorySeparatorChar);

        foreach (var component in components)
        {
            foreach (var kvp in ExtractAttributesFromName(component))
            {
                attributes.TryAdd(kvp.Key, kvp.Value);
            }
        }

        return attributes;
    }

    // Получение аттрибутов из строки. w1l5 -> {w, 1}, {l, 5}
    private static Dictionary<string, int> ExtractAttributesFromName(string name)
    {
        var attributes = new Dictionary<string, int>();
        var regex = StrokePathRegex();
        var matches = regex.Matches(name);

        foreach (Match match in matches)
        {
            string key = match.Groups[1].Value;
            string value = match.Groups[2].Value;
            attributes[key] = Convert.ToInt32(value);
        }

        return attributes;
    }

    [GeneratedRegex(@"([a-zA-Z]+)(\d+)")]
    private static partial Regex StrokePathRegex();
}
