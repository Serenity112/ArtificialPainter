using System.Drawing;

namespace ArtificialPainter.Core.Utils;

public class RandomPoolGenerator
{
    private readonly HashSet<Point> _coordsData;

    private readonly double _initialSize;
    private readonly Random _random;

    private readonly int _width;
    private readonly int _height;

    public RandomPoolGenerator(int width, int height, int randomSeed = -1)
    {
        _coordsData = [];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                _coordsData.Add(new Point(x, y));
            }
        }
        _initialSize = _coordsData.Count;
        _width = width;
        _height = height;

        _random = randomSeed == -1 ? new Random() : new Random(randomSeed);
    }

    public void RemoveFromPool(HashSet<Point> points)
    {
        foreach (var point in points)
        {
            _coordsData.Remove(point);
        }
    }

    public void RemoveFromPool(Point point)
    {
        _coordsData.Remove(point);
    }

    public bool PoolAvaliable()
    {
        return (_coordsData.Count > 0);
    }

    public double PoolPercent()
    {
        return _coordsData.Count / _initialSize;
    }

    public Point GetRandomPoint()
    {
        int randX = _random.Next(_width);
        int randY = _random.Next(_height);
        return new Point(randX, randY);
    }
    public Point GetFromPoolRandom()
    {
        int rand = _random.Next(_coordsData.Count);
        var point = _coordsData.ElementAt(rand);
        _coordsData.Remove(point);
        return point;
    }

    public IEnumerable<Point> GetFromPool()
    {
        foreach (var coords in _coordsData)
        {
            yield return coords;
        }
    }
}
