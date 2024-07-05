namespace ArtificialPainter.Core.GraphicsCore.Euclidean;

public interface IDimentional
{
}

public interface IDimentional2D : IDimentional
{
    int Width { get; init; }

    int Height { get; init; }
}