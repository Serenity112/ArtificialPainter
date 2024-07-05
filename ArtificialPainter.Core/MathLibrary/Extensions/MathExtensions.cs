using ArtificialPainter.Core.Utils;

namespace ArtificialPainter.Core.MathLibrary.Extensions;

public static class MathExtensions
{
    public static double NormalAngle(this double angle, RotationDeirection rotationDeirection = RotationDeirection.Clockwise)
    {
        double addAngle = rotationDeirection switch
        {
            RotationDeirection.Clockwise => Math.PI / 2,
            RotationDeirection.CounterClockwise => -Math.PI / 2,
            _ => 0
        };
        return (angle + addAngle) % Math.Tau;
    }

    public static float NormalAngle(this float angle, RotationDeirection rotationDeirection = RotationDeirection.Clockwise)
    {
        float addAngle = rotationDeirection switch
        {
            RotationDeirection.Clockwise => MathF.PI / 2,
            RotationDeirection.CounterClockwise => -MathF.PI / 2,
            _ => 0
        };
        return (angle + addAngle) % MathF.Tau;
    }

    public static double PiAngle(this double angle) => (angle + Math.PI) % Math.Tau;

    public static float PiAngle(this float angle) => (angle + MathF.PI) % MathF.Tau;

    public static float RadToDeg(this float radians) => radians * 180 / MathF.PI;

    public static float DegToRad(this float degrees) => degrees * MathF.PI / 180;
}
