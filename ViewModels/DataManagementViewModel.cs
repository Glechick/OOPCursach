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
    public class DataManagementViewModel : INotifyPropertyChanged
    {
        private ShapeFeatureExtractor _extractor;
        private JsonStorageService _storage;

        private ObservableCollection<ShapeClass> _shapeClasses;
        private ShapeClass _selectedClass;
        private ShapeImage _selectedImage;
        private string _statusMessage;

        public event Action DataChanged;

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
                foreach (var kvp in data)
                {
                    var shapeClass = new ShapeClass
                    {
                        Name = kvp.Key,
                        Id = ShapeClasses.Count,
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
                }
                StatusMessage = $"✅ Загружено {ShapeClasses.Count} классов из JSON";
                DataChanged?.Invoke();
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Ошибка загрузки: {ex.Message}";
            }
        }

        private void SaveMyModel()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "SVM Model (*.svm)|*.svm",
                DefaultExt = "svm",
                FileName = $"mysvm_model_{DateTime.Now:yyyyMMdd_HHmmss}.svm"
            };
            if (dialog.ShowDialog() == true)
            {
                StatusMessage = $"✅ Модель (мой SVM) сохранена: {dialog.FileName}";
            }
        }

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
                StatusMessage = $"✅ Модель (Accord) сохранена: {dialog.FileName}";
            }
        }

        private void LoadMyModel()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "SVM Model (*.svm)|*.svm",
                Title = "Загрузить модель (мой SVM)"
            };
            if (dialog.ShowDialog() == true)
            {
                StatusMessage = $"✅ Модель (мой SVM) загружена: {dialog.FileName}";
            }
        }

        private void LoadAccordModel()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Accord Model (*.accord)|*.accord",
                Title = "Загрузить модель (Accord)"
            };
            if (dialog.ShowDialog() == true)
            {
                StatusMessage = $"✅ Модель (Accord) загружена: {dialog.FileName}";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}