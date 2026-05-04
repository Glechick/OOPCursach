using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using SVMKurs.Algorithms;
using SVMKurs.Services;

namespace SVMKurs.ViewModels
{
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

        private MulticlassSvm3D _mySvmModel;
        private AccordSvmWrapper _accordSvmModel;
        private ShapeFeatureExtractor _extractor;

        public BitmapImage TestImage
        {
            get => _testImage;
            set
            {
                _testImage = value;
                OnPropertyChanged();
            }
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

        public CompareViewModel()
        {
            _extractor = new ShapeFeatureExtractor();
            _mySvmModel = null;
            _accordSvmModel = null;
            LoadImageCommand = new RelayCommand(_ => LoadImage());
            ClassifyCommand = new RelayCommand(_ => Classify(), _ => _testImageBytes != null);
        }

        public void SetModels(MulticlassSvm3D mySvm, AccordSvmWrapper accordSvm)
        {
            _mySvmModel = mySvm;
            _accordSvmModel = accordSvm;
            StatusMessage = "Модели загружены в окно сравнения";

            if (_features != null)
                Classify();
        }

        private void LoadImage()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg",
                Title = "Выберите изображение"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    _testImageBytes = System.IO.File.ReadAllBytes(dialog.FileName);
                    _features = _extractor.ExtractFeatures(_testImageBytes);

                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = new System.IO.MemoryStream(_testImageBytes);
                    bitmap.EndInit();
                    TestImage = bitmap;

                    StatusMessage = $"Признаки: [{_features[0]:F2}, {_features[1]:F2}, {_features[2]:F2}]";
                    Classify();
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Ошибка: {ex.Message}";
                }
            }
        }

        private void Classify()
        {
            if (_features == null)
                return;

            // Мой SVM
            try
            {
                if (_mySvmModel != null && _mySvmModel.IsTrained)
                {
                    var result = _mySvmModel.Predict(_features[0], _features[1], _features[2]);
                    MySvmResult = result.PredictedClassName;
                    MySvmConfidence = $"Уверенность: {result.Confidence:P1}";
                }
                else if (_mySvmModel == null)
                {
                    MySvmResult = "Модель не загружена";
                    MySvmConfidence = "Сначала обучите модель во вкладке 'Мой SVM'";
                }
                else
                {
                    MySvmResult = "Модель не обучена";
                    MySvmConfidence = "Сначала обучите модель во вкладке 'Мой SVM'";
                }
            }
            catch (Exception ex)
            {
                MySvmResult = "Ошибка";
                MySvmConfidence = ex.Message;
            }

            // Accord SVM
            try
            {
                if (_accordSvmModel != null && _accordSvmModel.IsTrained)
                {
                    int predicted = _accordSvmModel.Predict(_features[0], _features[1], _features[2]);
                    AccordSvmResult = $"Class_{predicted}";
                    AccordSvmConfidence = "Уверенность: вычисляется";
                }
                else if (_accordSvmModel == null)
                {
                    AccordSvmResult = "Модель не загружена";
                    AccordSvmConfidence = "Сначала обучите модель во вкладке 'Accord SVM'";
                }
                else
                {
                    AccordSvmResult = "Модель не обучена";
                    AccordSvmConfidence = "Сначала обучите модель во вкладке 'Accord SVM'";
                }
            }
            catch (Exception ex)
            {
                AccordSvmResult = "Ошибка";
                AccordSvmConfidence = ex.Message;
            }

            StatusMessage = "Классификация завершена";
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}