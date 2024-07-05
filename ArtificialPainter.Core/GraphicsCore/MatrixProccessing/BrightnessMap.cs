using ArtificialPainter.Core.GraphicsCore.Canvas;
using ArtificialPainter.Core.GraphicsCore.Extensions;
using ArtificialPainter.Core.GraphicsCore.Structs;

namespace ArtificialPainter.Core.GraphicsCore.MatrixProccessing;

public class BrightnessMap : Matrix2D<double>
{
    private readonly BrightnessMapOptions _brightnessMapOptions;

    private readonly ArtCanvas _artCanvas;

    public BrightnessMap(ArtCanvas artCanvas, BrightnessMapOptions brightnessMapOptions) : base(artCanvas.Width, artCanvas.Height)
    {
        _brightnessMapOptions = brightnessMapOptions;

        _artCanvas = artCanvas;
    }

    public void CreateBrightnessMap()
    {
        // Перевод в серый цвет
        var grayCanvas = _artCanvas.ToGrayScale();

#if DEBUG
        // grayCanvas.WriteToFile("");
#endif

        // Выбор оператора
        var (Xker, Yker) = _brightnessMapOptions.Kernel switch
        {
            DerivativesKernelType.Sobel => DerivativesKernels.SobelKernel,
            DerivativesKernelType.Symmetric => DerivativesKernels.SymmetricKernel,
            _ => DerivativesKernels.SobelKernel
        };

        // Создание матриц
        var dx = MatrixConvolution.ApplyConvolution(grayCanvas, Xker);
        var dy = MatrixConvolution.ApplyConvolution(grayCanvas, Yker);

        var averagingFilter = GetAveragingFilter();
        dx = MatrixConvolution.ApplyConvolution(dx, averagingFilter);
        dy = MatrixConvolution.ApplyConvolution(dy, averagingFilter);

        // Нормали
        foreach (var (x, y) in this)
        {
            this[x, y] = Math.Atan2(dy[y, x], dx[y, x]);
        }

#if DEBUG
        //WriteBrightnessMapToFile();
#endif
    }

    private void WriteBrightnessMapToFile()
    {

    }

    private Matrix2D<double> GetAveragingFilter()
    {
        var m = (int)_brightnessMapOptions.AveragingFilterCoef;
        if (m % 2 == 0)
            m++;

        var filter = new Matrix2D<double>(m, m);
        for (int x = 0; x < m; x++)
        {
            for (int y = 0; y < m; y++)
            {
                filter[x, y] = 1.0 / (m * m);
            }
        }
        return filter;
    }
}

public class BrightnessMapOptions(double blurSigma, DerivativesKernelType kernerl, double averagingFilterCoef)
{
    public double BlurSigma => blurSigma;
    public DerivativesKernelType Kernel => kernerl;
    public double AveragingFilterCoef => averagingFilterCoef;
}

file class DerivativesKernels
{
    public static (Matrix2D<double> Xker, Matrix2D<double> Yker) SobelKernel { get; private set; }

    public static (Matrix2D<double> Xker, Matrix2D<double> Yker) SymmetricKernel { get; private set; }

    static DerivativesKernels()
    {
        var SobelKernelX = new Matrix2D<double>(
            new double[,]{
                { -1, 0, 1 },
                { -2, 0, 2 },
                { -1, 0, 1 }});

        var SobelKernelY = new Matrix2D<double>(
            new double[,]{
                { 1, 2, 1 },
                { 0, 0, 0 },
                { -1, -2, -1 }});

        SobelKernel = (SobelKernelX, SobelKernelY);

        double p1 = 0.183;
        var SymmetricKernelX = new Matrix2D<double>(
            new double[,]{
                { -p1, 0, p1 },
                { 2 * p1 - 1, 0, 1 - 2 * p1 },
                { -p1, 0, p1 }});

        var SymmetricKernelY = new Matrix2D<double>(
            new double[,]{
                { p1, 1 - 2 * p1, p1},
                { 0, 0, 0 },
                { -p1, 2 * p1 - 1, -p1 }});

        SymmetricKernel = (SymmetricKernelX, SymmetricKernelY);
    }
}

public enum DerivativesKernelType
{
    Sobel,
    Symmetric,
}
