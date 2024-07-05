using ArtificialPainter.Core.Serialization;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace ArtGenerator.Views;

public partial class NewArtPage : Page
{
    private const string StylesFolder = "Styles";
    private const string CurrentDataFolder = "CurrentData";

    private string _stylesFolderPath;
    private string _currentDataFolderPath;

    private string _jsonDataFilePath;

    private string _inputPath;

    private int _width;
    private int _height;

    private FileSystemWatcher watcher;

    private BitmapImage _bitmapImage;

    public event Action NotifyImageLoaded;
    public event Action NotifyImageProccessed;
    public event Action NotifyFisrtLoad;

    public NewArtPage(string inputPath)
    {
        InitializeComponent();
        EnsureFoldersExists();
        InitWatcher();
        SubscribeEvents();
        Reload();

        this.Title = inputPath;
        _inputPath = inputPath;
    }

    private void SubscribeEvents()
    {
        NotifyImageLoaded += () =>
        {
            button_open_json.IsEnabled = true;
            button_confirm.IsEnabled = true;
            button_confirm.IsEnabled = true;
            button_pick_style.IsEnabled = true;
            button_save_style.IsEnabled = true;
            button_load.IsEnabled = true;
            button_save_changes.IsEnabled = true;
        };

        NotifyImageProccessed = () =>
        {
            button_confirm.IsEnabled = false;
            button_confirm.IsEnabled = false;
            button_pick_style.IsEnabled = false;
            button_save_style.IsEnabled = false;
            button_load.IsEnabled = false;
        };

        NotifyFisrtLoad = () =>
        {
            button_load.IsEnabled = true;

            button_confirm.IsEnabled = false;
            button_confirm.IsEnabled = false;
            button_pick_style.IsEnabled = false;
            button_save_style.IsEnabled = false;
            button_open_json.IsEnabled = false;
        };
    }

    public void Reload()
    {
        NotifyFisrtLoad?.Invoke();
    }

    private void InitWatcher()
    {
        watcher = new FileSystemWatcher();
        watcher.Path = _currentDataFolderPath;
        watcher.Filter = "*.*";

        watcher.NotifyFilter = NotifyFilters.LastWrite;
        watcher.Changed += ValidateJson;
        watcher.EnableRaisingEvents = true;
    }

    private void EnsureFoldersExists()
    {
        var executableLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

        _stylesFolderPath = Path.Combine(executableLocation, StylesFolder);
        _currentDataFolderPath = Path.Combine(executableLocation, CurrentDataFolder);

        if (!Directory.Exists(_stylesFolderPath))
        {
            Directory.CreateDirectory(_stylesFolderPath);
        }

        if (!Directory.Exists(_currentDataFolderPath))
        {
            Directory.CreateDirectory(_currentDataFolderPath);
        }
    }

    private string ResaveJson()
    {
        var artModel = new ArtModelSerializer(ReadArtUserInput());
        string jsonData = JsonConvert.SerializeObject(artModel, Formatting.Indented);
        File.WriteAllText(_jsonDataFilePath, jsonData);
        return jsonData;
    }

    // Открыть json для редактирования
    private void button_open_json_Click(object sender, RoutedEventArgs e)
    {
        ResaveJson();

        Process.Start("notepad.exe", _jsonDataFilePath);
    }

    // Сохранить изменения введённые вручную
    private void button_save_changes_click(object sender, RoutedEventArgs e)
    {
        ResaveJson();
    }

    private void ValidateJson(object sender, FileSystemEventArgs e)
    {
        try
        {
            var jsonString = File.ReadAllText(_jsonDataFilePath);
            var artModel = JsonConvert.DeserializeObject<ArtModelSerializer>(jsonString, new JsonSerializerSettings()
            {
                Formatting = Formatting.Indented
            });

            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                UserInputToTextBox(artModel.UserInput);
                json_error_label.Visibility = Visibility.Hidden;
            }));
        }
        catch (Exception ex)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                json_error_label.Visibility = Visibility.Visible;
            }));
        }
    }

    // Выбрать стиль
    private void button_pick_style_Click(object sender, RoutedEventArgs e)
    {
        OpenFileDialog openFileDialog = new OpenFileDialog();
        openFileDialog.Filter = "JSON Files (*.json)|*.json";
        openFileDialog.InitialDirectory = _stylesFolderPath;
        openFileDialog.Title = "Выбрать JSON стиль";

        if (openFileDialog.ShowDialog() == true)
        {
            string jsonFilePath = openFileDialog.FileName;

            try
            {
                string serializedJson = File.ReadAllText(jsonFilePath);
                ArtModelSerializer artModel = JsonConvert.DeserializeObject<ArtModelSerializer>(serializedJson)!;
                UserInputToTextBox(artModel.UserInput);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при открытии файла: " + ex.Message);
            }
        }
    }

    // Сохранить стиль
    private void button_save_style_Click(object sender, RoutedEventArgs e)
    {
        // Пересохранение текущих настроек с экрана
        var jsonData = ResaveJson();

        // Сохранить сам стиль
        var saveFileDialog = new SaveFileDialog();
        saveFileDialog.Filter = "JSON Files (*.json)|*.json";
        saveFileDialog.InitialDirectory = _stylesFolderPath;
        saveFileDialog.Title = "Сохранить JSON стиль";

        if (saveFileDialog.ShowDialog() == true)
        {
            try
            {
                File.WriteAllText(saveFileDialog.FileName, jsonData);
                MessageBox.Show("Стиль сохранён");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения стиля: " + ex.Message);
            }
        }
    }

    // Подтвердить
    private void button_confirm_Click(object sender, RoutedEventArgs e)
    {
        watcher.Dispose();

        try
        {
            string serializedJson = ResaveJson();
            var artModel = JsonConvert.DeserializeObject<ArtModelSerializer>(serializedJson)!;
            MainWindow mainWindow = (Application.Current.MainWindow as MainWindow)!;

            using var outStream = new MemoryStream();
            BitmapEncoder enc = new BmpBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create(_bitmapImage));
            enc.Save(outStream);
            var bitmap = new Bitmap(outStream);

            NotifyImageProccessed?.Invoke();
            mainWindow.ReceiveData(artModel, bitmap);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Ошибка: " + ex.Message);
        }
    }

    // Загрузка изображения
    private void button_load_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog();
        openFileDialog.InitialDirectory = _inputPath;

        if (openFileDialog.ShowDialog() == true)
        {
            string filePath = openFileDialog.FileName;
            Title = openFileDialog.SafeFileName.Split('.')[0];

            var logo = new BitmapImage();
            logo.BeginInit();
            logo.UriSource = new Uri(filePath);
            logo.EndInit();
            _bitmapImage = logo;
            targe_image.Source = logo;

            _width = (int)logo.Width;
            _height = (int)logo.Height;

            // Сохранение json-а в файл
            var userInput = ArtUserInput.Default;
            userInput.Width = _width;
            userInput.Height = _height;
            var artModel = new ArtModelSerializer(userInput);
            var jsonData = JsonConvert.SerializeObject(artModel, Formatting.Indented);
            _jsonDataFilePath = Path.Combine(_currentDataFolderPath, $"{Title}.json");

            try
            {
                File.WriteAllText(_jsonDataFilePath, jsonData);
                NotifyImageLoaded?.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении файла: " + ex.Message);
            }
        }
    }

    private ArtUserInput ReadArtUserInput()
    {
        return new ArtUserInput()
        {
            Width = _width,
            Height = _height,

            TotalGenerations = Convert.ToInt32(input_gen.Text),

            MaxStrokeSegments = Convert.ToInt32(input_segments.Text),

            StrokeWidth_Min = Convert.ToInt32(input_brushW_min.Text),
            StrokeWidth_Max = Convert.ToInt32(input_brushW_max.Text),

            StrokeLength_Min = Convert.ToInt32(input_brushL_min.Text),
            StrokeLength_Max = Convert.ToInt32(input_brushL_max.Text),

            BlurSigma_Min = Convert.ToInt32(input_blur_min.Text),
            BlurSigma_Max = Convert.ToInt32(input_blur_max.Text),

            StandardDeviation_Stroke_Min = Convert.ToInt32(input_standart_deviation_min.Text),
            StandardDeviation_Stroke_Max = Convert.ToInt32(input_standart_deviation_max.Text),

            StandardDeviation_Tile_Min = Convert.ToInt32(input_tile_standart_deviation_min.Text),
            StandardDeviation_Tile_Max = Convert.ToInt32(input_tile_standart_deviation_max.Text),

            StandardDeviation_Reject_Min = Convert.ToInt32(input_reject_standart_deviation_min.Text),
            StandardDeviation_Reject_Max = Convert.ToInt32(input_reject_standart_deviation_max.Text)
        };
    }

    private void UserInputToTextBox(ArtUserInput input)
    {
        _width = input.Width;
        _height = input.Height;

        input_gen.Text = input.TotalGenerations.ToString();

        input_segments.Text = input.MaxStrokeSegments.ToString();

        input_brushW_min.Text = input.StrokeWidth_Min.ToString();
        input_brushW_max.Text = input.StrokeWidth_Max.ToString();

        input_brushL_min.Text = input.StrokeLength_Min.ToString();
        input_brushL_max.Text = input.StrokeLength_Max.ToString();

        input_blur_min.Text = input.BlurSigma_Min.ToString();
        input_blur_max.Text = input.BlurSigma_Max.ToString();

        input_standart_deviation_min.Text = input.StandardDeviation_Stroke_Min.ToString();
        input_standart_deviation_max.Text = input.StandardDeviation_Stroke_Max.ToString();

        input_tile_standart_deviation_min.Text = input.StandardDeviation_Tile_Min.ToString();
        input_tile_standart_deviation_max.Text = input.StandardDeviation_Tile_Max.ToString();

        input_reject_standart_deviation_min.Text = input.StandardDeviation_Reject_Min.ToString();
        input_reject_standart_deviation_max.Text = input.StandardDeviation_Reject_Max.ToString();
    }
}
