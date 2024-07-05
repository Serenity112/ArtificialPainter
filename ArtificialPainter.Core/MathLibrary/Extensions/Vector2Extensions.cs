using System.Numerics;

namespace ArtificialPainter.Core.MathLibrary.Extensions;

public static class Vector2Extensions
{
    public static float AngleWithVector(this in Vector2 v1, in Vector2 v2)
    {
        return MathF.Acos(Vector2.Dot(v1, v2) / v1.Length() * v2.Length());
    }

    public static float VectorProduct(this in Vector2 v1, in Vector2 v2)
    {
        return v1.X * v2.Y - v2.X * v1.Y;
    }
}
