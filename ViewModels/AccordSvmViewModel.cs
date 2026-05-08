 using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;
using SVMKurs.Models;
using SVMKurs.Services;
using SVMKurs.Algorithms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace SVMKurs.ViewModels
{
    /// <summary>
    /// ViewModel для SVM на базе Accord.NET.
    /// Выполняет обучение, расчёт метрик и построение ROC-кривой.
    /// </summary>
    public class AccordSvmViewModel : INotifyPropertyChanged
    {
        private AccordSvmWrapper _model;
        private TrainingConfiguration _config;

        private List<Point3D> _allPoints;
        private List<Point3D> _trainPoints;
        private List<Point3D> _testPoints;

        private int _trainPercentage = 70;
        private int _trainCount;
        private int _testCount;
        private int _totalCount;

        private double _accuracy;
        private double _precision;
        private double _recall;
        private double _f1Score;

        private string _statusMessage;

        private PlotModel _rocPlotModel;

        /// <summary>
        /// Модель графика ROC-кривой.
        /// </summary>
        public PlotModel RocPlotModel
        {
            get => _rocPlotModel;
            set
            {
                _rocPlotModel = value;
                OnPropertyChanged();
            }
        }

        public event Action TrainingCompleted;

        /// <summary>
        /// Параметры обучения Accord SVM.
        /// </summary>
        public TrainingConfiguration Config
        {
            get => _config;
            set
            {
                _config = value;
                OnPropertyChanged();
            }
        }


        /// <summary>
        /// Список типов Margin для выбора в интерфейсе.
        /// </summary>
        public System.Collections.ObjectModel.ObservableCollection<string> MarginTypes
        {
            get; set;
        }

        /// <summary>
        /// Список функций потерь для выбора в интерфейсе.
        /// </summary>
        public System.Collections.ObjectModel.ObservableCollection<string> LossFunctions
        {
            get; set;
        }

        /// <summary>
        /// Процент обучающей выборки.
        /// </summary>
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

        /// <summary>
        /// Процент тестовой выборки.
        /// </summary>
        public int TestPercentage => 100 - _trainPercentage;

        public int TrainCount
        {
            get => _trainCount; set
            {
                _trainCount = value;
                OnPropertyChanged();
            }
        }
        public int TestCount
        {
            get => _testCount; set
            {
                _testCount = value;
                OnPropertyChanged();
            }
        }
        public int TotalCount
        {
            get => _totalCount; set
            {
                _totalCount = value;
                OnPropertyChanged();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage; set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        public double Accuracy
        {
            get => _accuracy; set
            {
                _accuracy = value;
                OnPropertyChanged();
            }
        }
        public double Precision
        {
            get => _precision; set
            {
                _precision = value;
                OnPropertyChanged();
            }
        }
        public double Recall
        {
            get => _recall; set
            {
                _recall = value;
                OnPropertyChanged();
            }
        }
        public double F1Score
        {
            get => _f1Score; set
            {
                _f1Score = value;
                OnPropertyChanged();
            }
        }

        private double _macroAuc;
        public double MacroAuc
        {
            get => _macroAuc;
            set
            {
                _macroAuc = value;
                OnPropertyChanged();
            }
        }


        /// <summary>
        /// Команда запуска обучения.
        /// </summary>
        public ICommand TrainCommand
        {
            get;
        }

        public AccordSvmViewModel()
        {
            _model = new AccordSvmWrapper();
            _config = new TrainingConfiguration();

            _allPoints = new List<Point3D>();

            MarginTypes = new System.Collections.ObjectModel.ObservableCollection<string>
            {
                "Soft Margin",
                "Hard Margin"
            };

            LossFunctions = new System.Collections.ObjectModel.ObservableCollection<string>
            {
                "Hinge",
                "SquaredHinge"
            };

            TrainCommand = new RelayCommand(_ => Train());
        }

        /// <summary>
        /// Устанавливает данные и выполняет первичное разделение.
        /// </summary>
        public void SetData(List<Point3D> allPoints)
        {
            _allPoints = allPoints;
            TotalCount = allPoints.Count;
            UpdateCounts();
            ApplySplit();
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

            var rnd = new Random(42);
            var shuffled = _allPoints.OrderBy(_ => rnd.Next()).ToList();

            _trainPoints = shuffled.Take(TrainCount).ToList();
            _testPoints = shuffled.Skip(TrainCount).ToList();

            StatusMessage = $"Разделение: обучение={_trainPoints.Count}, тест={_testPoints.Count}";
        }

        /// <summary>
        /// Выполняет обучение модели и расчёт метрик.
        /// </summary>
        private void Train()
        {
            if (_trainPoints.Count == 0)
            {
                StatusMessage = "Сначала примените разделение выборки!";
                return;
            }

            try
            {
                StatusMessage = "Обучение Accord SVM...";

                _model = new AccordSvmWrapper();
                _model.Train(_trainPoints, _config);

                int classCount = _allPoints.Select(p => p.Label).Distinct().Count();

                var trueLabels = _testPoints.Select(p => p.Label).ToArray();
                var predictedLabels = _testPoints.Select(p => _model.Predict(p.X, p.Y, p.Z)).ToArray();

                var predictedProb = _testPoints
                    .Select(p => PredictProbaArray(p.X, p.Y, p.Z, classCount))
                    .ToArray();

                var metrics = MetricsCalculator.Calculate(
                    trueLabels,
                    predictedProb,
                    predictedLabels,
                    classCount);

                Accuracy = metrics.Accuracy;
                Precision = metrics.Precision;
                Recall = metrics.Recall;
                F1Score = metrics.F1Score;
                MacroAuc = metrics.MacroAuc;


                UpdateRoc(metrics);

                StatusMessage = $"Обучение завершено! Точность: {Accuracy:P1}";
                TrainingCompleted?.Invoke();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка: {ex.Message}";
            }
        }

        /// <summary>
        /// Преобразует словарь вероятностей Accord в массив double[].
        /// </summary>
        private double[] PredictProbaArray(double x, double y, double z, int classCount)
        {
            var dict = _model.PredictProba(x, y, z);

            var arr = new double[classCount];
            for (int i = 0; i < classCount; i++)
                arr[i] = dict.ContainsKey(i) ? dict[i] : 0.0;

            return arr;
        }

        /// <summary>
        /// Строит ROC-кривую с использованием OxyPlot.
        /// </summary>
        private void UpdateRoc(Metrics m)
        {
            var model = new PlotModel { Title = "ROC-кривая" };

            model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Minimum = 0,
                Maximum = 1,
                Title = "FPR"
            });

            model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Minimum = 0,
                Maximum = 1,
                Title = "TPR"
            });

            var roc = new LineSeries
            {
                Color = OxyColors.Blue,
                StrokeThickness = 2,
                Title = "ROC"
            };

            foreach (var p in m.RocMacro)
                roc.Points.Add(new DataPoint(p.fpr, p.tpr));

            model.Series.Add(roc);

            var diag = new LineSeries
            {
                Color = OxyColors.Gray,
                StrokeThickness = 1,
                Title = "Random"
            };

            diag.Points.Add(new DataPoint(0, 0));
            diag.Points.Add(new DataPoint(1, 1));

            model.Series.Add(diag);

            RocPlotModel = model;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        /// <summary>
        /// Возвращает обученную модель Accord SVM.
        /// </summary>
        public AccordSvmWrapper GetAccordModel()
        {
            return _model;
        }
    }
}
