using System.Collections;

namespace ArtificialPainter.Core.GraphicsCore.Euclidean;

public abstract class EuclideanSpace2DBase<TDimentionItem> : IEuclideanSpace2D<TDimentionItem>
{
    public abstract TDimentionItem this[int x, int y]
    {
        get;
        set;
    }

    public abstract int Width { get; init; }
    public abstract int Height { get; init; }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public IEnumerator<(int x, int y)> GetEnumerator()
    {
        foreach (int x in Enumerable.Range(0, Width))
        {
            foreach (int y in Enumerable.Range(0, Height))
            {
                yield return (x, y);
            }
        }
    }
}
