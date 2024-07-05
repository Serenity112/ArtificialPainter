namespace ArtificialPainter.Core.Strokes.Library;

public readonly struct StrokeLibraryContainer
{
    // int - количество точек
    private readonly Dictionary<int, List<ArtStroke>> _artStrokes;

    public StrokeLibraryContainer()
    {
        _artStrokes = new(){
            { 1, [] },
            { 2, [] },
            { 3, [] }
        };
    }

    public readonly void AddStroke(int points, ArtStroke artStroke)
    {
        _artStrokes[points].Add(artStroke);
    }

    public readonly List<ArtStroke> this[int index]
    {
        get => _artStrokes[index];
    }
}
