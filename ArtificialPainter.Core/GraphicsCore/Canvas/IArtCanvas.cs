using System.Drawing;

namespace ArtificialPainter.Core.GraphicsCore.Canvas;

public interface IArtCanvas
{
    IArtCanvas Copy();

    void FillColor(Color color);

    void WriteToFile(string path, string title);
}
