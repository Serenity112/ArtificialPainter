using System.Drawing;

namespace ArtificialPainter.Core.Tracing.PointDeciders;

public interface IPointDecider
{
    // Получение новой точки
    public Point GetNewPoint();

    // Доступны ли новые точки
    public bool IsDeciderAvaliable();

    // Обратная связь после выбора точки
    public void PointCallback(Point point);

    // Обновить состояне после всего мазка
    public void PostStroke();
}
