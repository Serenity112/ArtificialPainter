namespace ArtificialPainter.Core.GraphicsCore.Euclidean;

public interface IEuclideanSpace2D<TDimentionItem> : IDimentional2D, IEnumerable<(int x, int y)>
{
    public int Dimentions => 2;

    TDimentionItem this[int x, int y] { get; set; }
}
