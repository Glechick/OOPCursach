using Microsoft.Win32;
using SVMKurs.Algorithms;
using SVMKurs.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace SVMKurs.ViewModels
{
    /// <summary>
    /// ViewModel для сравнения результатов двух моделей SVM:
    /// собственной реализации и Accord‑модели.
    /// Позволяет загрузить изображение, извлечь признаки,
    /// выполнить классификацию и вывести Top‑3 по вероятностям.
    /// </summary>
    public class CompareViewModel : INotifyPropertyChanged
    {
        private BitmapImage _testImage;
        private byte[] _testImageBytes;
        private double[] _features;

        private string _mySvmResult;
        private string _mySvmConfidence;
        private string _accordSvmResult;
        private string _accordSvmConfidence;
        private string _statusMessage;

        /// <summary>
        /// Моя SVM‑модель (реализация IClassifierModel).
        /// </summary>
        private IClassifierModel _mySvmModel;

        /// <summary>
        /// Accord‑модель (реализация IClassifierModel).
        /// </summary>
        private IClassifierModel _accordSvmModel;

        private readonly ShapeFeatureExtractor _extractor;
        private Dictionary<int, string> _classNames;

        /// <summary>
        /// Top‑3 предсказаний для моей SVM‑модели.
        /// </summary>
        public ObservableCollection<PredictionItem> MySvmTopK
        {
            get;
        }
            = new ObservableCollection<PredictionItem>();

        /// <summary>
        /// Top‑3 предсказаний для Accord‑модели.
        /// </summary>
        public ObservableCollection<PredictionItem> AccordTopK
        {
            get;
        }
            = new ObservableCollection<PredictionItem>();

        /// <summary>
        /// Загруженное тестовое изображение.
        /// </summary>
        public BitmapImage TestImage
        {
            get => _testImage;
            set
            {
                _testImage = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Устанавливает словарь имён классов по их индексам.
        /// </summary>
        public void SetClassNames(Dictionary<int, string> classNames)
        {
            _classNames = classNames;
        }

        public string MySvmResult
        {
            get => _mySvmResult;
            set
            {
                _mySvmResult = value;
                OnPropertyChanged();
            }
        }

        public string MySvmConfidence
        {
            get => _mySvmConfidence;
            set
            {
                _mySvmConfidence = value;
                OnPropertyChanged();
            }
        }

        public string AccordSvmResult
        {
            get => _accordSvmResult;
            set
            {
                _accordSvmResult = value;
                OnPropertyChanged();
            }
        }

        public string AccordSvmConfidence
        {
            get => _accordSvmConfidence;
            set
            {
                _accordSvmConfidence = value;
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

        public ICommand LoadImageCommand
        {
            get;
        }
        public ICommand ClassifyCommand
        {
            get;
        }

        /// <summary>
        /// Создаёт новый экземпляр CompareViewModel.
        /// Инициализирует команды и коллекции Top‑3.
        /// </summary>
        public CompareViewModel()
        {
            _extractor = new ShapeFeatureExtractor();

            LoadImageCommand = new RelayCommand(_ => LoadImage());
            ClassifyCommand = new RelayCommand(_ => Classify(), _ => _testImageBytes != null);

            // Инициализируем по 3 элемента, чтобы XAML‑привязки по индексам не падали.
            for (int i = 0; i < 3; i++)
            {
                MySvmTopK.Add(new PredictionItem("", 0));
                AccordTopK.Add(new PredictionItem("", 0));
            }
        }

        /// <summary>
        /// Устанавливает модели для сравнения (мою и Accord),
        /// обе реализуют единый интерфейс IClassifierModel.
        /// </summary>
        public void SetModels(IClassifierModel mySvm, IClassifierModel accordSvm)
        {
            _mySvmModel = mySvm;
            _accordSvmModel = accordSvm;

            StatusMessage = "Модели загружены";

            if (_features != null)

                Classify();
        }

        /// <summary>
        /// Загружает изображение с диска и извлекает признаки.
        /// </summary>
        private void LoadImage()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    _testImageBytes = System.IO.File.ReadAllBytes(dialog.FileName);
                    _features = _extractor.ExtractFeatures(_testImageBytes);

                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.StreamSource = new System.IO.MemoryStream(_testImageBytes);
                    bmp.EndInit();
                    TestImage = bmp;

                    StatusMessage = $"Признаки: [{_features[0]:F2}, {_features[1]:F2}, {_features[2]:F2}]";

                    Classify();
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Ошибка: {ex.Message}";
                }
            }
        }

        /// <summary>
        /// Выполняет классификацию текущего изображения обеими моделями.
        /// </summary>
        private void Classify()
        {
            if (_features == null)
                return;

            ClassifyMySvm();
            ClassifyAccordSvm();

            StatusMessage = "Классификация завершена";
        }

        /// <summary>
        /// Классифицирует изображение моей SVM‑моделью.
        /// </summary>
        private void ClassifyMySvm()
        {
            try
            {
                if (_mySvmModel == null || !_mySvmModel.IsTrained)
                {
                    MySvmResult = "Модель не обучена";
                    MySvmConfidence = "";
                    return;
                }

                var proba = _mySvmModel.PredictProba(_features[0], _features[1], _features[2]);
                var sorted = proba.OrderByDescending(v => v.Value).ToList();

                int predictedClass = sorted.First().Key;

                string className =
                    _classNames != null && _classNames.ContainsKey(predictedClass)
                    ? _classNames[predictedClass]
                    : $"Class_{predictedClass}";

                MySvmResult = className;
                MySvmConfidence = $"{sorted.First().Value:P1}";

                for (int i = 0; i < 3 && i < sorted.Count; i++)
                {
                    var kv = sorted[i];
                    string name =
                        _classNames != null && _classNames.ContainsKey(kv.Key)
                        ? _classNames[kv.Key]
                        : $"Class_{kv.Key}";

                    MySvmTopK[i].ClassName = name;
                    MySvmTopK[i].Probability = kv.Value;
                }
            }
            catch (Exception ex)
            {
                MySvmResult = "Ошибка";
                MySvmConfidence = ex.Message;
            }
        }

        /// <summary>
        /// Классифицирует изображение Accord‑моделью.
        /// </summary>
        private void ClassifyAccordSvm()
        {
            try
            {
                if (_accordSvmModel == null || !_accordSvmModel.IsTrained)
                {
                    AccordSvmResult = "Модель не обучена";
                    AccordSvmConfidence = "";
                    return;
                }

                var proba = _accordSvmModel.PredictProba(_features[0], _features[1], _features[2]);
                var sorted = proba.OrderByDescending(v => v.Value).ToList();

                int predictedClass = sorted.First().Key;

                string className =
                    _classNames != null && _classNames.ContainsKey(predictedClass)
                    ? _classNames[predictedClass]
                    : $"Class_{predictedClass}";

                AccordSvmResult = className;
                AccordSvmConfidence = $"{sorted.First().Value:P1}";

                for (int i = 0; i < 3 && i < sorted.Count; i++)
                {
                    var kv = sorted[i];
                    string name =
                        _classNames != null && _classNames.ContainsKey(kv.Key)
                        ? _classNames[kv.Key]
                        : $"Class_{kv.Key}";

                    AccordTopK[i].ClassName = name;
                    AccordTopK[i].Probability = kv.Value;
                }
            }
            catch (Exception ex)
            {
                AccordSvmResult = "Ошибка";
                AccordSvmConfidence = ex.Message;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// Элемент списка Top‑3: имя класса и его вероятность.
    /// </summary>
    public class PredictionItem : INotifyPropertyChanged
    {
        private string _className;
        private double _probability;

        public string ClassName
        {
            get => _className;
            set
            {
                _className = value;
                OnPropertyChanged();
            }
        }

        public double Probability
        {
            get => _probability;
            set
            {
                _probability = value;
                OnPropertyChanged();
            }
        }

        public PredictionItem(string className, double prob)
        {
            _className = className;
            _probability = prob;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
