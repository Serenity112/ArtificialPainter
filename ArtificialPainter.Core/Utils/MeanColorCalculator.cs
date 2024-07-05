using ArtificialPainter.Core.GraphicsCore.Canvas;
using System.Drawing;

namespace ArtificialPainter.Core.Utils;

public class MeanColorCalculator
{
    private int _redCounter = 0;
    private int _greenCounter = 0;
    private int _blueCounter = 0;
    private int _totalCounter = 0;

    public MeanColorCalculator()
    {
    }

    public MeanColorCalculator(ArtCanvas artCanvas, HashSet<Point> pixels) : this()
    {
        foreach (var pixel in pixels)
        {
            _redCounter += artCanvas[pixel.X, pixel.Y].R;
            _greenCounter += artCanvas[pixel.X, pixel.Y].G;
            _blueCounter += artCanvas[pixel.X, pixel.Y].B;
            _totalCounter++;
        }
    }

    public MeanColorCalculator Copy()
    {
        return new MeanColorCalculator()
        {
            _redCounter = _redCounter,
            _greenCounter = _greenCounter,
            _blueCounter = _blueCounter,
            _totalCounter = _totalCounter
        };
    }

    public void MergeWith(MeanColorCalculator calculatorToMerge)
    {
        _redCounter += calculatorToMerge._redCounter;
        _greenCounter += calculatorToMerge._greenCounter;
        _blueCounter += calculatorToMerge._blueCounter;
        _totalCounter += calculatorToMerge._totalCounter;
    }

    public void AddColor(in Color color)
    {
        _redCounter += color.R;
        _greenCounter += color.G;
        _blueCounter += color.B;
        _totalCounter++;
    }

    public void Reset()
    {
        _redCounter = 0;
        _greenCounter = 0;
        _blueCounter = 0;
        _totalCounter = 0;
    }

    public Color GetMeanColor()
    {
        return Color.FromArgb(byte.MaxValue,
            Math.Clamp(_redCounter / _totalCounter, 0, 255),
            Math.Clamp(_greenCounter / _totalCounter, 0, 255),
            Math.Clamp(_blueCounter / _totalCounter, 0, 255));
    }
}
