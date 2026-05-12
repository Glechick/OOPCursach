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
using Accord.MachineLearning.VectorMachines;
using Accord.Statistics.Kernels;
using Accord.IO;

namespace SVMKurs.ViewModels
{
    /// <summary>
    /// ViewModel для управления данными: классами фигур, изображениями
    /// и сохранением/загрузкой обученных моделей SVM.
    /// </summary>
    public class DataManagementViewModel : INotifyPropertyChanged
    {
        public event Action ModelsUpdated;

        private readonly ShapeFeatureExtractor _extractor;
        private readonly JsonStorageService _storage;

        public event Action<MulticlassSvm3D> MyModelLoaded;
        public event Action<AccordSvmWrapper> AccordModelLoaded;

        private ObservableCollection<ShapeClass> _shapeClasses;
        private ShapeClass _selectedClass;
        private ShapeImage _selectedImage;
        private string _statusMessage;

        /// <summary>
        /// Моя SVM‑модель (реализация IClassifierModel).
        /// </summary>
        private IClassifierModel _mySvmModel;

        /// <summary>
        /// Accord‑модель (реализация IClassifierModel).
        /// </summary>
        private IClassifierModel _accordModel;

        /// <summary>
        /// Возвращает Accord модель.
        /// </summary>
        public IClassifierModel GetAccordModel() => _accordModel;

        /// <summary>
        /// Событие, вызываемое при изменении данных (классы/изображения).
        /// </summary>
        public event Action DataChanged;

        /// <summary>
        /// Коллекция классов фигур.
        /// </summary>
        public ObservableCollection<ShapeClass> ShapeClasses
        {
            get => _shapeClasses;
            set
            {
                _shapeClasses = value;
                OnPropertyChanged();
            }
        }

        public IClassifierModel GetMyModel() => _mySvmModel;

        /// <summary>
        /// Текущий выбранный класс.
        /// </summary>
        public ShapeClass SelectedClass
        {
            get => _selectedClass;
            set
            {
                _selectedClass = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Текущее выбранное изображение.
        /// </summary>
        public ShapeImage SelectedImage
        {
            get => _selectedImage;
            set
            {
                _selectedImage = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Статус выполнения операций.
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

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
        public ICommand SaveDataCommand
        {
            get;
        }
        public ICommand LoadDataCommand
        {
            get;
        }
        public ICommand SaveMyModelCommand
        {
            get;
        }
        public ICommand SaveAccordModelCommand
        {
            get;
        }
        public ICommand LoadMyModelCommand
        {
            get;
        }
        public ICommand LoadAccordModelCommand
        {
            get;
        }

        /// <summary>
        /// Создаёт новый экземпляр DataManagementViewModel.
        /// Инициализирует команды и загружает сохранённые данные (если есть).
        /// </summary>
        public DataManagementViewModel()
        {
            _extractor = new ShapeFeatureExtractor();
            _storage = new JsonStorageService();
            _shapeClasses = new ObservableCollection<ShapeClass>();

            AddClassCommand = new RelayCommand(_ => AddClass());
            DeleteClassCommand = new RelayCommand(_ => DeleteClass(), _ => SelectedClass != null);
            AddImagesCommand = new RelayCommand(_ => AddImages(), _ => SelectedClass != null);
            DeleteImageCommand = new RelayCommand(_ => DeleteImage(), _ => SelectedImage != null);
            SaveDataCommand = new RelayCommand(_ => SaveData());
            LoadDataCommand = new RelayCommand(_ => LoadData());
            SaveMyModelCommand = new RelayCommand(_ => SaveMyModel());
            SaveAccordModelCommand = new RelayCommand(_ => SaveAccordModel());
            LoadMyModelCommand = new RelayCommand(_ => LoadMyModel());
            LoadAccordModelCommand = new RelayCommand(_ => LoadAccordModel());

            LoadData();
        }

        /// <summary>
        /// Устанавливает мою SVM‑модель (после обучения).
        /// Ожидается MulticlassSvm3D, реализующий IClassifierModel.
        /// </summary>
        public void SetMyModel(IClassifierModel model)
        {
            _mySvmModel = model;
        }

        /// <summary>
        /// Устанавливает Accord‑модель (после обучения).
        /// Ожидается AccordSvmWrapper, реализующий IClassifierModel.
        /// </summary>
        public void SetAccordModel(IClassifierModel model)
        {
            _accordModel = model;
        }

        /// <summary>
        /// Добавляет новый класс фигур.
        /// </summary>
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
                        new TextBlock { Text = "Введите название класса:", Margin = new Thickness(0,0,0,10) },
                        new TextBox { Name = "ClassName", Margin = new Thickness(0,0,0,10) },
                        new Button { Content = "Добавить", HorizontalAlignment = HorizontalAlignment.Center, Width = 100 }
                    }
                }
            };

            var textBox = (TextBox)((StackPanel)dialog.Content).Children[1];
            var button = (Button)((StackPanel)dialog.Content).Children[2];

            button.Click += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(textBox.Text))
                {
                    ShapeClasses.Add(new ShapeClass
                    {
                        Name = textBox.Text,
                        Id = ShapeClasses.Count,
                        Images = new ObservableCollection<ShapeImage>()
                    });
                    dialog.Close();
                    StatusMessage = $"✅ Класс '{textBox.Text}' добавлен";
                    DataChanged?.Invoke();
                }
            };

            dialog.ShowDialog();
        }

        /// <summary>
        /// Удаляет выбранный класс.
        /// </summary>
        private void DeleteClass()
        {
            if (SelectedClass != null)
            {
                var result = MessageBox.Show($"Удалить класс '{SelectedClass.Name}'?", "Подтверждение", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    string name = SelectedClass.Name;
                    ShapeClasses.Remove(SelectedClass);
                    StatusMessage = $"✅ Класс '{name}' удалён";
                    DataChanged?.Invoke();
                }
            }
        }

        /// <summary>
        /// Добавляет изображения в выбранный класс и извлекает признаки.
        /// </summary>
        private async void AddImages()
        {
            var dialog = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp",
                Title = "Выберите изображения"
            };

            if (dialog.ShowDialog() == true && SelectedClass != null)
            {
                int added = 0;
                foreach (var filePath in dialog.FileNames)
                {
                    try
                    {
                        var hash = HashService.ComputeFileHash(filePath);
                        bool exists = ShapeClasses.Any(c => c.Images.Any(i => i.FileHash == hash));
                        if (exists)
                            continue;

                        var bytes = File.ReadAllBytes(filePath);
                        var features = await System.Threading.Tasks.Task.Run(() => _extractor.ExtractFeatures(bytes));

                        SelectedClass.Images.Add(new ShapeImage
                        {
                            FileName = Path.GetFileName(filePath),
                            FilePath = filePath,
                            FileHash = hash,
                            ImageBytes = bytes,
                            Features = features,
                            IsProcessed = true
                        });
                        added++;
                    }
                    catch (Exception ex)
                    {
                        StatusMessage = $"❌ Ошибка: {ex.Message}";
                    }
                }
                StatusMessage = $"✅ Добавлено {added} изображений в класс '{SelectedClass.Name}'";
                DataChanged?.Invoke();
            }
        }

        /// <summary>
        /// Удаляет выбранное изображение из выбранного класса.
        /// </summary>
        private void DeleteImage()
        {
            if (SelectedImage != null && SelectedClass != null)
            {
                string name = SelectedImage.FileName;
                SelectedClass.Images.Remove(SelectedImage);
                StatusMessage = $"✅ Удалено изображение: {name}";
                DataChanged?.Invoke();
            }
        }

        /// <summary>
        /// Сохраняет признаки изображений в JSON.
        /// </summary>
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
                _storage.SaveData(data);
                StatusMessage = $"✅ Данные сохранены в JSON: {_storage.GetFilePath()}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Ошибка сохранения: {ex.Message}";
            }
        }

        /// <summary>
        /// Загружает признаки изображений из JSON.
        /// </summary>
        private void LoadData()
        {
            try
            {
                if (!_storage.DataExists())
                {
                    StatusMessage = "⚠️ Сохранённых данных нет";
                    return;
                }

                var data = _storage.LoadData();
                ShapeClasses.Clear();

                int newId = 0;
                foreach (var kvp in data)
                {
                    var shapeClass = new ShapeClass
                    {
                        Name = kvp.Key,
                        Id = newId,
                        Images = new ObservableCollection<ShapeImage>()
                    };

                    foreach (var stored in kvp.Value)
                    {
                        shapeClass.Images.Add(new ShapeImage
                        {
                            FileName = stored.FileName,
                            FileHash = stored.FileHash,
                            Features = stored.Features,
                            IsProcessed = true
                        });
                    }
                    ShapeClasses.Add(shapeClass);
                    newId++;
                }

                StatusMessage = $"✅ Загружено {ShapeClasses.Count} классов из JSON";
                DataChanged?.Invoke();
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Ошибка загрузки: {ex.Message}";
            }
        }

        /// <summary>
        /// Сохраняет мою SVM‑модель в файл
        /// </summary>
        private void SaveMyModel()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "My SVM (*.mysvm)|*.mysvm",
                DefaultExt = "mysvm",
                FileName = $"my_svm_{DateTime.Now:yyyyMMdd_HHmmss}.mysvm"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    if (_mySvmModel == null || !_mySvmModel.IsTrained)
                    {
                        StatusMessage = "⚠️ Моя модель не обучена.";
                        return;
                    }

                    var concrete = _mySvmModel as MulticlassSvm3D;
                    if (concrete == null)
                    {
                        StatusMessage = "❌ Тип модели неверный.";
                        return;
                    }

                    var data = concrete.ToData();

                    var json = System.Text.Json.JsonSerializer.Serialize(
                        data,
                        new System.Text.Json.JsonSerializerOptions { WriteIndented = true }
                    );

                    File.WriteAllText(dialog.FileName, json);

                    StatusMessage = $"✅ Моя модель сохранена: {dialog.FileName}";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"❌ Ошибка сохранения: {ex.Message}";
                }
            }
        }


        /// <summary>
        /// Сохраняет Accord‑модель в файл 
        /// </summary>
        private void SaveAccordModel()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Accord Model (*.accord)|*.accord",
                DefaultExt = "accord",
                FileName = $"accord_model_{DateTime.Now:yyyyMMdd_HHmmss}.accord"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    if (_accordModel == null || !_accordModel.IsTrained)
                    {
                        StatusMessage = "⚠️ Модель Accord не обучена.";
                        return;
                    }

                    var wrapper = _accordModel as AccordSvmWrapper;
                    if (wrapper == null)
                    {
                        StatusMessage = "❌ Неверный тип модели Accord";
                        return;
                    }

                    using (var stream = new FileStream(dialog.FileName, FileMode.Create))
                    {
                        Accord.IO.Serializer.Save(wrapper.GetRawModel(), stream);
                    }

                    StatusMessage = $"✅ Модель Accord сохранена: {Path.GetFileName(dialog.FileName)}";
                    MessageBox.Show("Модель Accord сохранена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    StatusMessage = $"❌ Ошибка сохранения Accord: {ex.Message}";
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Загружает мою SVM‑модель из файла.
        /// </summary>
        private void LoadMyModel()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "My SVM (*.mysvm)|*.mysvm",
                Title = "Загрузить мою модель"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var json = File.ReadAllText(dialog.FileName);
                    var data = System.Text.Json.JsonSerializer.Deserialize<SvmModelData>(json);

                    if (data == null || data.Models == null || data.Models.Count == 0)
                    {
                        StatusMessage = "❌ Неверный формат файла модели";
                        return;
                    }

                    var model = new MulticlassSvm3D();
                    model.LoadFromData(data);

                    _mySvmModel = model;

                    MyModelLoaded?.Invoke(model);

                    StatusMessage = $"✅ Моя модель загружена: {Path.GetFileName(dialog.FileName)}";
                    MessageBox.Show($"Модель загружена!\nКлассов: {data.Classes.Count}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    DataChanged?.Invoke();
                }
                catch (Exception ex)
                {
                    StatusMessage = $"❌ Ошибка загрузки: {ex.Message}";
                    MessageBox.Show($"Ошибка загрузки:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


        /// <summary>
        /// Загружает Accord‑модель из файла.
        /// </summary>
        private void LoadAccordModel()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Accord Model (*.accord)|*.accord",
                Title = "Загрузить модель Accord"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var wrapper = new AccordSvmWrapper();
                    using (var stream = new FileStream(dialog.FileName, FileMode.Open))
                    {
                        var raw = Accord.IO.Serializer.Load<Accord.MachineLearning.VectorMachines.MulticlassSupportVectorMachine<Accord.Statistics.Kernels.Linear>>(stream);
                        wrapper.SetRawModel(raw);
                        _accordModel = wrapper;
                    }

                    StatusMessage = $"✅ Модель Accord загружена: {Path.GetFileName(dialog.FileName)}";
                    AccordModelLoaded?.Invoke(wrapper);
                    MessageBox.Show("Модель Accord загружена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    DataChanged?.Invoke();
                }
                catch (Exception ex)
                {
                    StatusMessage = $"❌ Ошибка загрузки Accord: {ex.Message}";
                    MessageBox.Show($"Ошибка: {ex.Message}\n\nВозможно, файл поврежден или несовместимой версии.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
