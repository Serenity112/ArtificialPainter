using ArtificialPainter.Core.GraphicsCore.Canvas;
using ArtificialPainter.Core.Utils;
using System.Drawing;

namespace ArtificialPainter.Core.Tracing.PointDeciders;

public class RandomPointDecider(ArtCanvas original, int randomSeed = -1) : IPointDecider
{
    private readonly RandomPoolGenerator _pool = new(original.Width, original.Height, randomSeed);

    private const double FirstLayerFill = 0.01;

    public Point GetNewPoint()
    {
        return _pool.GetFromPoolRandom();
    }

    public bool IsDeciderAvaliable()
    {
        return (_pool.PoolAvaliable() && _pool.PoolPercent() > FirstLayerFill);
    }

    public void PointCallback(Point point)
    {
        _pool.RemoveFromPool(point);
    }

    public void PostStroke()
    {
    }
}
