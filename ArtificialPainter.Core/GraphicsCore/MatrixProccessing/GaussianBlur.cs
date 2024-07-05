using ArtificialPainter.Core.GraphicsCore.Canvas;
using ArtificialPainter.Core.GraphicsCore.Structs;

namespace ArtificialPainter.Core.GraphicsCore.MatrixProccessing;

public static class GaussianBlur
{
    public static Matrix2D<double> ApplyBlur(Matrix2D<double> matrix, double blurSigma)
    {
        if (blurSigma == 0)
            return matrix;

        // испоьзуем правило 6-сигма
        int kernelSize = (int)Math.Ceiling(6 * blurSigma);
        if (kernelSize % 2 == 0)
            kernelSize++;

        var kernelX = Generate1DGaussianKernel(kernelSize, blurSigma, KernelDirection.xKernel);
        var kernelY = Generate1DGaussianKernel(kernelSize, blurSigma, KernelDirection.yKernel);

        var blurX = MatrixConvolution.ApplyConvolution(matrix, kernelX);
        var blurXY = MatrixConvolution.ApplyConvolution(blurX, kernelY);

        return blurXY;
    }

    public static ArtCanvas ApplyBlur(ArtCanvas artCanvas, double blurSigma)
    {
        if (blurSigma == 0)
            return artCanvas;

        // испоьзуем правило 6-сигма
        int kernelSize = (int)Math.Ceiling(6 * blurSigma);
        if (kernelSize % 2 == 0)
            kernelSize++;

        var kernelX = Generate1DGaussianKernel(kernelSize, blurSigma, KernelDirection.xKernel);
        var kernelY = Generate1DGaussianKernel(kernelSize, blurSigma, KernelDirection.yKernel);

        var blurX = MatrixConvolution.ApplyConvolution(artCanvas, kernelX);
        var blurXY = MatrixConvolution.ApplyConvolution(blurX, kernelY);

        return blurXY;
    }

    private static Matrix2D<double> Generate1DGaussianKernel(int kernelSize, double blurSigma, KernelDirection direction)
    {
        int halfSize = kernelSize / 2;

        Matrix2D<double> kernel;

        if (direction == KernelDirection.xKernel)
        {
            kernel = new Matrix2D<double>(kernelSize, 1);
            for (int i = -halfSize; i <= halfSize; i++)
            {
                kernel[0, i + halfSize] = G(i, blurSigma);
            }
        }
        else
        {
            kernel = new Matrix2D<double>(1, kernelSize);
            for (int i = -halfSize; i <= halfSize; i++)
            {
                kernel[i + halfSize, 0] = G(i, blurSigma);
            }
        }
        return kernel;
    }

    private static double G(int x, double sigma)
    {
        return 1.0 / Math.Sqrt(2 * Math.PI * sigma * sigma) * Math.Exp(-(x * x) / (2 * sigma * sigma));
    }

    private enum KernelDirection
    {
        xKernel,
        yKernel
    }
}
