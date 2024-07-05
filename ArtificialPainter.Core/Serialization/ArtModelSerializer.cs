using System.Text.Json.Serialization;

namespace ArtificialPainter.Core.Serialization;

public class ArtModelSerializer
{
    public ArtUserInput UserInput { get; set; }

    public List<ArtSingleGeneration> Generations { get; set; }

    public ArtModelSerializer(ArtUserInput inputData)
    {
        UserInput = inputData;
        Generations = [];
        int generationsNumber = inputData.TotalGenerations;

        double[] borders_normal = new double[generationsNumber];
        for (int gen = 0; gen < generationsNumber; gen++)
            borders_normal[gen] = Math.Round(gen * 1.0 / (generationsNumber - 1), 3);

        double[] borders_pairs = new double[generationsNumber + 1];
        for (int gen = 0; gen <= generationsNumber; gen++)
            borders_pairs[gen] = Math.Round(gen * 1.0 / generationsNumber, 3);

        for (int generation = 0; generation < generationsNumber; generation++)
        {
            var layerGeneration = new ArtSingleGeneration();
            double factor_down = borders_pairs[generation];
            double factor_up = borders_pairs[generation + 1];
            double factor_norm = borders_normal[generation];

            // Ширина кисти
            int stroke_width_interval = inputData.StrokeWidth_Max - inputData.StrokeWidth_Min;
            layerGeneration.StrokeWidth_Min = (int)(inputData.StrokeWidth_Min + stroke_width_interval * factor_down);
            layerGeneration.StrokeWidth_Max = (int)(inputData.StrokeWidth_Min + stroke_width_interval * factor_up);

            // Длина мазка
            int stroke_length_interval = inputData.StrokeLength_Max - inputData.StrokeLength_Min;
            layerGeneration.StrokeLength_Max = (int)(inputData.StrokeLength_Min + stroke_length_interval * factor_norm);

            // Блюр
            int blur_interval = inputData.BlurSigma_Max - inputData.BlurSigma_Min;
            layerGeneration.BlurSigma = (int)(inputData.BlurSigma_Min + blur_interval * factor_norm);

            // СКО мазка
            int standardDeviation_stroke_interval = inputData.StandardDeviation_Stroke_Max - inputData.StandardDeviation_Stroke_Min;
            layerGeneration.StandardDeviation_Stroke_Bound = (int)(inputData.StandardDeviation_Stroke_Min + standardDeviation_stroke_interval * factor_norm);

            // СКО регионов
            int standardDeviation_tile_interval = inputData.StandardDeviation_Tile_Max - inputData.StandardDeviation_Tile_Min;
            layerGeneration.StandardDeviation_Tile_Bound = (int)(inputData.StandardDeviation_Tile_Min + standardDeviation_tile_interval * factor_norm);

            // СКО отклонения мазка
            int StandardDeviation_reject_interval = inputData.StandardDeviation_Reject_Max - inputData.StandardDeviation_Reject_Min;
            layerGeneration.StandardDeviation_Reject_Bound = (int)(inputData.StandardDeviation_Reject_Min + StandardDeviation_reject_interval * factor_norm);

            // Итерации, можно смело изменять формулу            
            int layerIterations = inputData.Width * inputData.Height / (layerGeneration.StrokeWidth_Max * layerGeneration.StrokeWidth_Max);
            layerGeneration.LayerIterations = layerIterations;

            Generations.Add(layerGeneration);
        }

        Generations.Reverse();
    }
}

// Данные о конкретном уровне
public class ArtSingleGeneration
{
    public int LayerIterations { get; set; }

    public int StrokeWidth_Min { get; set; }
    public int StrokeWidth_Max { get; set; }

    public int StrokeLength_Max { get; set; }

    public int BlurSigma { get; set; }

    public int StandardDeviation_Stroke_Bound { get; set; }
    public int StandardDeviation_Tile_Bound { get; set; }
    public int StandardDeviation_Reject_Bound { get; set; }
}

// Пользовательский ввод
public class ArtUserInput
{
    [JsonIgnore]
    public static readonly ArtUserInput Default = new()
    {
        Width = 300,
        Height = 300,

        TotalGenerations = 7,
        MaxStrokeSegments = 2,

        StrokeWidth_Min = 6,
        StrokeWidth_Max = 80,

        StrokeLength_Min = 0,
        StrokeLength_Max = 50,

        BlurSigma_Min = 4,
        BlurSigma_Max = 30,

        StandardDeviation_Stroke_Min = 340,
        StandardDeviation_Stroke_Max = 1500,

        StandardDeviation_Tile_Min = 5000,
        StandardDeviation_Tile_Max = 20000,

        StandardDeviation_Reject_Min = 5000,
        StandardDeviation_Reject_Max = 20000,
    };

    public int Width { get; set; }
    public int Height { get; set; }

    // Количество поколений рисовки
    public int TotalGenerations { get; set; }

    // Сегментность мазков
    public int MaxStrokeSegments { get; set; }

    // Минимальная и максимальная толщина кисти
    public int StrokeWidth_Min { get; set; }
    public int StrokeWidth_Max { get; set; }

    // Минимальная и максимальная длина мазка
    public int StrokeLength_Min { get; set; }
    public int StrokeLength_Max { get; set; }

    // Диапазон размытия изображений
    public int BlurSigma_Min { get; set; }
    public int BlurSigma_Max { get; set; }

    // Диапазон СКО для генерации
    public int StandardDeviation_Stroke_Min { get; set; }
    public int StandardDeviation_Stroke_Max { get; set; }

    // Диапазон СКО для регионов
    public int StandardDeviation_Tile_Min { get; set; }
    public int StandardDeviation_Tile_Max { get; set; }

    // Диапазон СКО для отклонения мазка
    public int StandardDeviation_Reject_Min { get; set; }
    public int StandardDeviation_Reject_Max { get; set; }
}
