namespace ArtificialPainter.Core.Strokes;

public class StrokePropertyCollection<T> : Dictionary<StrokeProperty, T>
{
    private readonly object _locker = new();

    public StrokePropertyCollection()
    {
    }

    public virtual bool ContainsPropery(StrokeProperty key)
    {
        return ContainsKey(key);
    }

    public virtual T GetProperty(StrokeProperty key)
    {
        return this[key];
    }

    public virtual void SetProperty(StrokeProperty key, T value)
    {
        if (ContainsKey(key))
        {
            this[key] = value;
        }
        else
        {
            lock (_locker)
            {
                TryAdd(key, value);
            }
        }
    }

    public static StrokeProperty StrokePropertyByAlias(string key)
    {
        return StrokePropertyAliases[key];
    }

    private static readonly Dictionary<string, StrokeProperty> StrokePropertyAliases = new()
        {
            { "pt" , StrokeProperty.Points },
            { "w" , StrokeProperty.Width },
            { "l" , StrokeProperty.Length },
            { "a" , StrokeProperty.Angle1 },
            { "s" , StrokeProperty.Fraction },
        };
}
