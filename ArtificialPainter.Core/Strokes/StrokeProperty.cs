namespace ArtificialPainter.Core.Strokes;

public enum StrokeProperty
{
    Points, // Количество точек мазка

    Width, // Ширина мазка
    Length, // Суммарная длина мазка по сегментам
    LtoW, // Отношение Width к Length

    Angle1, // Угол между 1м и 2м сегментами
    Fraction // Доля 1го сегмента от общей длины
}
