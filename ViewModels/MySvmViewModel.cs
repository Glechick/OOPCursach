using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SVMKurs.Algorithms;
using SVMKurs.Models;
using SVMKurs.Services;

namespace SVMKurs.ViewModels
{
    public class MySvmViewModel : INotifyPropertyChanged
    {
        private MulticlassSvm3D _model;
        private TrainingConfiguration _config;

        private List<MulticlassPoint3D> _allPoints;
        private List<MulticlassPoint3D> _trainPoints;
        private List<MulticlassPoint3D> _testPoints;
        private Dictionary<int, string> _classNames;

        private int _trainPercentage = 70;
        private int _trainCount;
        private int _testCount;
        private int _totalCount;
        private double _accuracy;
        private double _precision;
        private double _recall;
        private double _f1Score;
        private string _statusMessage;
        private List<RocPoint> _rocPoints;

        public event Action TrainingCompleted;
        public event Action<List<RocPoint>> RequestDrawRoc;

        public TrainingConfiguration Config
        {
            get => _config;
            set
            {
                _config = value;
                OnPropertyChanged();
            }
        }

        public int TrainPercentage
        {
            get => _trainPercentage;
            set
            {
                _trainPercentage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TestPercentage));
                UpdateCounts();
                ApplySplit();
            }
        }

        public int TestPercentage => 100 - _trainPercentage;

        public int TrainCount
        {
            get => _trainCount;
            set
            {
                _trainCount = value;
                OnPropertyChanged();
            }
        }

        public int TestCount
        {
            get => _testCount;
            set
            {
                _testCount = value;
                OnPropertyChanged();
            }
        }

        public int TotalCount
        {
            get => _totalCount;
            set
            {
                _totalCount = value;
                OnPropertyChanged();
            }
        }

        public double Accuracy
        {
            get => _accuracy;
            set
            {
                _accuracy = value;
                OnPropertyChanged();
            }
        }

        public double Precision
        {
            get => _precision;
            set
            {
                _precision = value;
                OnPropertyChanged();
            }
        }

        public double Recall
        {
            get => _recall;
            set
            {
                _recall = value;
                OnPropertyChanged();
            }
        }

        public double F1Score
        {
            get => _f1Score;
            set
            {
                _f1Score = value;
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

        public List<RocPoint> RocPoints
        {
            get => _rocPoints;
            set
            {
                _rocPoints = value;
                OnPropertyChanged();
            }
        }

        public ICommand TrainCommand
        {
            get;
        }
        public ObservableCollection<string> MarginTypes
        {
            get; set;
        }
        public ObservableCollection<string> LossFunctions
        {
            get; set;
        }

        public MySvmViewModel()
        {
            _model = new MulticlassSvm3D();
            _config = new TrainingConfiguration();
            _allPoints = new List<MulticlassPoint3D>();
            _classNames = new Dictionary<int, string>();

            MarginTypes = new ObservableCollection<string> { "Soft Margin", "Hard Margin" };
            LossFunctions = new ObservableCollection<string> { "Hinge", "SquaredHinge" };

            TrainCommand = new RelayCommand(_ => Train());
        }

        public void SetData(List<MulticlassPoint3D> allPoints, Dictionary<int, string> classNames)
        {
            _allPoints = allPoints;
            _classNames = classNames;
            TotalCount = allPoints.Count;
            UpdateCounts();
            ApplySplit();
        }

        public MulticlassSvm3D GetModel()
        {
            return _model;
        }

        private void UpdateCounts()
        {
            TrainCount = (int)(TotalCount * _trainPercentage / 100.0);
            TestCount = TotalCount - TrainCount;
        }

        private void ApplySplit()
        {
            if (_allPoints.Count == 0)
            {
                StatusMessage = "Нет данных для разделения";
                return;
            }

            var random = new Random(42);
            var shuffled = new List<MulticlassPoint3D>(_allPoints);
            for (int i = 0; i < shuffled.Count; i++)
            {
                int r = random.Next(i, shuffled.Count);
                (shuffled[r], shuffled[i]) = (shuffled[i], shuffled[r]);
            }

            _trainPoints = shuffled.Take(TrainCount).ToList();
            _testPoints = shuffled.Skip(TrainCount).ToList();

            StatusMessage = $"Разделение: обучение={_trainPoints.Count}, тест={_testPoints.Count}";
        }

        private void Train()
        {
            if (_trainPoints == null || _trainPoints.Count == 0)
            {
                StatusMessage = "Сначала примените разделение выборки!";
                return;
            }

            try
            {
                StatusMessage = "Обучение моего SVM...";

                _model = new MulticlassSvm3D();
                _model.Train(_trainPoints, _classNames, _config.C);

                var metrics = MetricsCalculator.Calculate(_model, _testPoints, "MySVM");

                Accuracy = metrics.Accuracy;
                Precision = metrics.Precision;
                Recall = metrics.Recall;
                F1Score = metrics.F1Score;
                _rocPoints = metrics.RocCurve;

                StatusMessage = $"Обучение завершено! Точность: {Accuracy:P1}";
                RequestDrawRoc?.Invoke(_rocPoints);
                TrainingCompleted?.Invoke();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка: {ex.Message}";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}