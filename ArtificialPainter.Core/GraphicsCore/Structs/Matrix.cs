using ArtificialPainter.Core.GraphicsCore.Euclidean;

namespace ArtificialPainter.Core.GraphicsCore.Structs;

public abstract class Matrix<T> : EuclideanSpace2DBase<T>
{
}

public class Matrix2D<T> : Matrix<T>, IEuclideanSpace2D<T>
{
    public override int Width { get; init; }

    public override int Height { get; init; }

    protected T[,] _matrixCore;

    public override T this[int x, int y]
    {
        get => _matrixCore[x, y];
        set => _matrixCore[x, y] = value;
    }

    public Matrix2D(int width, int height)
    {
        _matrixCore = new T[width, height];
        Width = width;
        Height = height;
    }

    public Matrix2D(T[,] rawData)
    {
        _matrixCore = rawData;
        Width = rawData.GetLength(0);
        Height = rawData.GetLength(1);
    }
}
