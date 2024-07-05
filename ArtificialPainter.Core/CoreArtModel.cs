using ArtificialPainter.Core.GraphicsCore.Canvas;
using ArtificialPainter.Core.Serialization;
using ArtificialPainter.Core.Settings;
using ArtificialPainter.Core.Tracing;
using System.Drawing;

namespace ArtificialPainter.Core;

public class CoreArtModel(Bitmap bitmap, ArtModelSerializer serializer, PathSettings pathSettings)
{
    private readonly ArtCanvas _originalCanvas = new(bitmap);

    private readonly ArtModelSerializer _modelSerializer = serializer;

    private PathSettings _pathSettings = pathSettings;

    public PainterTracer CreateTracer(CancellationToken token)
    {
        var tracer = new PainterTracer(_originalCanvas, _modelSerializer, _pathSettings, token);
        return tracer;
    }
}
