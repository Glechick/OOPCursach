using Microsoft.Win32;
using SVMKurs.Algorithms;
using SVMKurs.Models;
using SVMKurs.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SVMKurs.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private readonly ShapeFeatureExtractor _featureExtractor;
        private readonly JsonStorageService _storageService;

        private ObservableCollection<ShapeClass> _shapeClasses;
        private ShapeClass _selectedClass;
        private ShapeImage _selectedImage;
        private string _statusMessage;
        private bool _isProcessing;

        // SVM модели
        private MulticlassSvm3D _mySvm;
        private bool _isMySvmTrained;
        private string _mySvmMetrics;

        // Accord SVM
        private bool _isAccordSvmTrained;
        private string _accordSvmMetrics;

        public ObservableCollection<ShapeClass> ShapeClasses
        {
            get => _shapeClasses;
            set
            {
                _shapeClasses = value;
                OnPropertyChanged();
            }
        }

        public ShapeClass SelectedClass
        {
            get => _selectedClass;
            set
            {
                _selectedClass = value;
                OnPropertyChanged();
            }
        }

        public ShapeImage SelectedImage
        {
            get => _selectedImage;
            set
            {
                _selectedImage = value;
                OnPropertyChanged();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set
            {
                _isProcessing = value;
                OnPropertyChanged();
            }
        }

        public bool IsMySvmTrained
        {
            get => _isMySvmTrained;
            set
            {
                _isMySvmTrained = value;
                OnPropertyChanged();
            }
        }

        public string MySvmMetrics
        {
            get => _mySvmMetrics;
            set
            {
                _mySvmMetrics = value;
                OnPropertyChanged();
            }
        }

        // Команды
        public ICommand AddClassCommand
        {
            get;
        }
        public ICommand DeleteClassCommand
        {
            get;
        }
        public ICommand AddImagesCommand
        {
            get;
        }
        public ICommand DeleteImageCommand
        {
            get;
        }
        public ICommand TrainMySvmCommand
        {
            get;
        }
        public ICommand TestImageCommand
        {
            get;
        }
        public ICommand SaveDataCommand
        {
            get;
        }
        public ICommand LoadDataCommand
        {
            get;
        }

        public MainWindowViewModel()
        {
            _featureExtractor = new ShapeFeatureExtractor();
            _storageService = new JsonStorageService();
            _mySvm = new MulticlassSvm3D();

            ShapeClasses = new ObservableCollection<ShapeClass>();

            // Инициализация команд
            AddClassCommand = new RelayCommand(_ => AddClass());
            DeleteClassCommand = new RelayCommand(_ => DeleteClass(), _ => SelectedClass != null);
            AddImagesCommand = new RelayCommand(_ => AddImages(), _ => SelectedClass != null);
            DeleteImageCommand = new RelayCommand(_ => DeleteImage(), _ => SelectedImage != null);
            TrainMySvmCommand = new RelayCommand(_ => TrainMySvm(), _ => ShapeClasses.Count >= 2 && !IsProcessing);
            SaveDataCommand = new RelayCommand(_ => SaveData());
            LoadDataCommand = new RelayCommand(_ => LoadData());

            // Загружаем сохранённые данные при старте
            LoadData();
        }

        private void AddClass()
        {
            var dialog = new Window
            {
                Title = "Новый класс",
                Width = 300,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new StackPanel
                {
                    Margin = new Thickness(10),
                    Children =
                    {
                        new System.Windows.Controls.TextBlock { Text = "Введите название класса:", Margin = new Thickness(0,0,0,10) },
                        new System.Windows.Controls.TextBox { Name = "ClassName", Margin = new Thickness(0,0,0,10) },
                        new System.Windows.Controls.Button
                        {
                            Content = "Добавить",
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Width = 100
                        }
                    }
                }
            };

            var textBox = (System.Windows.Controls.TextBox)((StackPanel)dialog.Content).Children[1];
            var button = (System.Windows.Controls.Button)((StackPanel)dialog.Content).Children[2];

            button.Click += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(textBox.Text))
                {
                    var newClass = new ShapeClass
                    {
                        Name = textBox.Text,
                        Id = ShapeClasses.Count,
                        Images = new ObservableCollection<ShapeImage>()
                    };
                    ShapeClasses.Add(newClass);
                    dialog.Close();
                    StatusMessage = $"Добавлен класс: {textBox.Text}";
                }
            };

            dialog.ShowDialog();
        }

        private void DeleteClass()
        {
            if (SelectedClass != null)
            {
                var result = MessageBox.Show($"Удалить класс '{SelectedClass.Name}' и все его изображения?",
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    ShapeClasses.Remove(SelectedClass);
                    StatusMessage = $"Класс удалён";
                    IsMySvmTrained = false;
                }
            }
        }

        private async void AddImages()
        {
            var dialog = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp",
                Title = $"Выберите изображения для класса '{SelectedClass.Name}'"
            };

            if (dialog.ShowDialog() == true)
            {
                IsProcessing = true;
                StatusMessage = $"Обработка {dialog.FileNames.Length} изображений...";

                int processed = 0;
                int newImages = 0;

                foreach (var filePath in dialog.FileNames)
                {
                    try
                    {
                        var fileHash = HashService.ComputeFileHash(filePath);

                        // Проверяем, нет ли уже такого изображения
                        bool exists = ShapeClasses.Any(c => c.Images.Any(i => i.FileHash == fileHash));

                        if (exists)
                        {
                            processed++;
                            StatusMessage = $"Пропущен дубликат: {Path.GetFileName(filePath)}";
                            continue;
                        }

                        var imageBytes = File.ReadAllBytes(filePath);
                        var features = await System.Threading.Tasks.Task.Run(() =>
                            _featureExtractor.ExtractFeatures(imageBytes));

                        var shapeImage = new ShapeImage
                        {
                            FileName = Path.GetFileName(filePath),
                            FilePath = filePath,
                            FileHash = fileHash,
                            ImageBytes = imageBytes,
                            Features = features,
                            IsProcessed = true
                        };

                        SelectedClass.Images.Add(shapeImage);
                        newImages++;
                        processed++;

                        StatusMessage = $"Обработано: {processed}/{dialog.FileNames.Length}";
                    }
                    catch (Exception ex)
                    {
                        StatusMessage = $"Ошибка: {Path.GetFileName(filePath)} - {ex.Message}";
                    }
                }

                StatusMessage = $"Добавлено {newImages} изображений в класс '{SelectedClass.Name}'";
                IsProcessing = false;
                IsMySvmTrained = false; // Данные изменились, нужно переобучение
            }
        }

        private void DeleteImage()
        {
            if (SelectedImage != null && SelectedClass != null)
            {
                SelectedClass.Images.Remove(SelectedImage);
                StatusMessage = $"Удалено изображение: {SelectedImage.FileName}";
                IsMySvmTrained = false;
            }
        }

        private void TrainMySvm()
        {
            try
            {
                IsProcessing = true;
                StatusMessage = "Обучение вашего SVM...";

                // Собираем все точки для обучения
                var allPoints = new System.Collections.Generic.List<MulticlassPoint3D>();
                var classNames = new System.Collections.Generic.Dictionary<int, string>();

                int classId = 0;
                foreach (var shapeClass in ShapeClasses)
                {
                    classNames[classId] = shapeClass.Name;

                    foreach (var image in shapeClass.Images)
                    {
                        allPoints.Add(new MulticlassPoint3D(
                            image.Feature1,
                            image.Feature2,
                            image.Feature3,
                            classId));
                    }
                    classId++;
                }

                if (allPoints.Count < 10)
                {
                    StatusMessage = "Недостаточно данных для обучения (нужно минимум 10 изображений)";
                    IsProcessing = false;
                    return;
                }

                // Обучение
                _mySvm = new MulticlassSvm3D();
                _mySvm.Train(allPoints, classNames, C: 1.0);

                // Вычисление метрик
                double accuracy = _mySvm.Evaluate(allPoints);
                var matrix = _mySvm.GetConfusionMatrix(allPoints);

                MySvmMetrics = $"Точность: {accuracy:P1}\n" +
                               $"Классов: {_mySvm.NumberOfClasses}\n" +
                               $"Бинарных классификаторов: {_mySvm.GetBinaryClassifiers().Count}";

                IsMySvmTrained = true;
                StatusMessage = $"Обучение завершено! Точность: {accuracy:P1}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка обучения: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private void SaveData()
        {
            try
            {
                var data = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<StoredImage>>();

                foreach (var shapeClass in ShapeClasses)
                {
                    var images = new System.Collections.Generic.List<StoredImage>();
                    foreach (var image in shapeClass.Images)
                    {
                        images.Add(new StoredImage
                        {
                            FileName = image.FileName,
                            FileHash = image.FileHash,
                            Feature1 = image.Feature1,
                            Feature2 = image.Feature2,
                            Feature3 = image.Feature3,
                            ProcessedAt = DateTime.Now
                        });
                    }
                    data[shapeClass.Name] = images;
                }

                _storageService.SaveData(data);
                StatusMessage = $"Данные сохранены в: {_storageService.GetFilePath()}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка сохранения: {ex.Message}";
            }
        }

        private void LoadData()
        {
            try
            {
                if (!_storageService.DataExists())
                {
                    StatusMessage = "Сохранённых данных нет. Добавьте изображения в классы.";
                    return;
                }

                var data = _storageService.LoadData();
                ShapeClasses.Clear();

                foreach (var kvp in data)
                {
                    var shapeClass = new ShapeClass
                    {
                        Name = kvp.Key,
                        Id = ShapeClasses.Count,
                        Images = new ObservableCollection<ShapeImage>()
                    };

                    foreach (var storedImage in kvp.Value)
                    {
                        shapeClass.Images.Add(new ShapeImage
                        {
                            FileName = storedImage.FileName,
                            FileHash = storedImage.FileHash,
                            Features = storedImage.Features,
                            IsProcessed = true
                        });
                    }

                    ShapeClasses.Add(shapeClass);
                }

                StatusMessage = $"Загружено {ShapeClasses.Count} классов, всего изображений: {ShapeClasses.Sum(c => c.Images.Count)}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка загрузки: {ex.Message}";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}