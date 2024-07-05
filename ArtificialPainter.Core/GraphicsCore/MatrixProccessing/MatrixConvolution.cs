using ArtificialPainter.Core.GraphicsCore.Canvas;
using ArtificialPainter.Core.GraphicsCore.Structs;
using System.Drawing;

namespace ArtificialPainter.Core.GraphicsCore.MatrixProccessing;

public static class MatrixConvolution
{
    public static ArtCanvas ApplyConvolution(ArtCanvas artCanvas, Matrix2D<double> kernel)
    {
        int height = artCanvas.Height;
        int width = artCanvas.Width;

        var resultCanvas = new ArtCanvas(width, height);

        int halfKernelWidth = kernel.Width / 2;
        int halfKernelHeight = kernel.Height / 2;

        foreach (var (x, y) in artCanvas)
        {
            (double r, double g, double b) = (0, 0, 0);

            for (int m = -halfKernelWidth; m <= halfKernelWidth; m++)
            {
                for (int n = -halfKernelHeight; n <= halfKernelHeight; n++)
                {
                    int kX = Math.Clamp(x + n, 0, width - 1);
                    int kY = Math.Clamp(y + m, 0, height - 1);

                    double kernelValue = kernel[m + halfKernelWidth, n + halfKernelHeight];
                    var color = artCanvas[kX, kY];

                    r += color.R * kernelValue;
                    g += color.G * kernelValue;
                    b += color.B * kernelValue;
                }
            }

            int R = (int)Math.Clamp(r, 0, 255);
            int G = (int)Math.Clamp(g, 0, 255);
            int B = (int)Math.Clamp(b, 0, 255);

            resultCanvas[x, y] = Color.FromArgb(R, G, B);
        }

        return resultCanvas;
    }

    public static Matrix2D<double> ApplyConvolution(Matrix2D<double> matrix, Matrix2D<double> kernel)
    {
        int height = matrix.Height;
        int width = matrix.Width;

        var resultMatrix = new Matrix2D<double>(width, height);

        int halfKernelWidth = kernel.Width / 2;
        int halfKernelHeight = kernel.Height / 2;

        foreach (var (x, y) in matrix)
        {
            double sum = 0;

            for (int m = -halfKernelWidth; m <= halfKernelWidth; m++)
            {
                for (int n = -halfKernelHeight; n <= halfKernelHeight; n++)
                {
                    int kX = Math.Clamp(x + n, 0, width - 1);
                    int kY = Math.Clamp(y + m, 0, height - 1);

                    double kernelValue = kernel[m + halfKernelWidth, n + halfKernelHeight];
                    sum += matrix[kY, kX] * kernelValue;
                }
            }
            resultMatrix[x, y] = sum;
        }

        return resultMatrix;
    }
}
