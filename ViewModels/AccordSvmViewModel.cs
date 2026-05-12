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
    /// ViewModel для SVM на базе библиотеки Accord.NET.
    /// Обеспечивает обучение, оценку метрик и построение ROC-кривой.
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

        /// <summary>
        /// Событие, возникающее после завершения обучения или оценки модели.
        /// </summary>
        public event Action TrainingCompleted;

        /// <summary>
        /// Конфигурация обучения SVM (C, Tolerance, LossFunction).
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
        /// Признак того, что модель обучена и готова к использованию.
        /// </summary>
        public bool IsTrained => _model != null && _model.IsTrained;

        /// <summary>
        /// Список доступных функций потерь для выбора в UI.
        /// </summary>
        public System.Collections.ObjectModel.ObservableCollection<string> LossFunctions
        {
            get; set;
        }

        /// <summary>
        /// Процент данных, используемых для обучения (остальные идут на тест).
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
        /// Процент данных, используемых для тестирования.
        /// </summary>
        public int TestPercentage => 100 - _trainPercentage;

        /// <summary>
        /// Количество обучающих объектов.
        /// </summary>
        public int TrainCount
        {
            get => _trainCount;
            set
            {
                _trainCount = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Количество тестовых объектов.
        /// </summary>
        public int TestCount
        {
            get => _testCount;
            set
            {
                _testCount = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Общее количество объектов.
        /// </summary>
        public int TotalCount
        {
            get => _totalCount;
            set
            {
                _totalCount = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Сообщение о состоянии процесса (обучение, оценка, ошибки).
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

        /// <summary>
        /// Точность классификации (Accuracy).
        /// </summary>
        public double Accuracy
        {
            get => _accuracy;
            set
            {
                _accuracy = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Средняя точность (Precision) по всем классам.
        /// </summary>
        public double Precision
        {
            get => _precision;
            set
            {
                _precision = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Средняя полнота (Recall) по всем классам.
        /// </summary>
        public double Recall
        {
            get => _recall;
            set
            {
                _recall = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Средняя F1-мера по всем классам.
        /// </summary>
        public double F1Score
        {
            get => _f1Score;
            set
            {
                _f1Score = value;
                OnPropertyChanged();
            }
        }

        private double _macroAuc;

        /// <summary>
        /// Площадь под macro-ROC-кривой (AUC).
        /// </summary>
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
        /// Команда запуска обучения модели.
        /// </summary>
        public ICommand TrainCommand
        {
            get;
        }

        /// <summary>
        /// Конструктор ViewModel. Инициализирует параметры и коллекции.
        /// </summary>
        public AccordSvmViewModel()
        {
            _model = new AccordSvmWrapper();
            _config = new TrainingConfiguration();

            _allPoints = new List<Point3D>();

            LossFunctions = new System.Collections.ObjectModel.ObservableCollection<string>
            {
                "Hinge",
                "SquaredHinge"
            };

            TrainCommand = new RelayCommand(_ => Train());
        }

        /// <summary>
        /// Устанавливает загруженную из файла модель.
        /// </summary>
        /// <param name="model">Загруженная модель AccordSvmWrapper.</param>
        public void SetModel(AccordSvmWrapper model)
        {
            if (model != null && model.IsTrained)
            {
                _model = model;
                StatusMessage = "Accord модель загружена из файла";
                OnPropertyChanged(nameof(IsTrained));
            }
        }

        /// <summary>
        /// Устанавливает данные для обучения и тестирования.
        /// </summary>
        /// <param name="allPoints">Список всех точек с координатами и метками классов.</param>
        public void SetData(List<Point3D> allPoints)
        {
            _allPoints = allPoints;
            TotalCount = allPoints.Count;
            UpdateCounts();
            ApplySplit();
        }

        /// <summary>
        /// Обновляет количество обучающих и тестовых объектов на основе процента разделения.
        /// </summary>
        private void UpdateCounts()
        {
            TrainCount = (int)(TotalCount * _trainPercentage / 100.0);
            TestCount = TotalCount - TrainCount;
        }

        /// <summary>
        /// Выполняет случайное разделение данных на обучающую и тестовую выборки.
        /// </summary>
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
        /// Выполняет обучение модели Accord SVM.
        /// </summary>
        private void Train()
        {
            if (_trainPoints == null || _trainPoints.Count == 0)
            {
                StatusMessage = "Нет данных для обучения. Сначала загрузите изображения.";
                return;
            }

            try
            {
                StatusMessage = "Обучение Accord SVM...";

                _model = new AccordSvmWrapper();
                _model.Train(_trainPoints, _config);

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

                    var metrics = MetricsCalculator.Calculate(trueLabels, predictedProb, predictedLabels, classCount);

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
                    System.Diagnostics.Debug.WriteLine($"Accord ошибка: {ex}");
                    if (ex.InnerException != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Inner: {ex.InnerException.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка: {ex.Message}";
            }
        }

        /// <summary>
        /// Преобразует словарь вероятностей в массив для передачи в MetricsCalculator.
        /// </summary>
        /// <param name="x">Координата X.</param>
        /// <param name="y">Координата Y.</param>
        /// <param name="z">Координата Z.</param>
        /// <param name="classCount">Общее количество классов.</param>
        /// <returns>Массив вероятностей, где индекс = номер класса.</returns>
        private double[] PredictProbaArray(double x, double y, double z, int classCount)
        {
            var dict = _model.PredictProba(x, y, z);
            var arr = new double[classCount];
            for (int i = 0; i < classCount; i++)
                arr[i] = dict.ContainsKey(i) ? dict[i] : 0.0;
            return arr;
        }

        /// <summary>
        /// Строит ROC-кривую с использованием библиотеки OxyPlot.
        /// </summary>
        /// <param name="m">Метрики, содержащие точки ROC-кривой.</param>
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

        /// <summary>
        /// Оценивает загруженную модель на тестовых данных без переобучения.
        /// </summary>
        public void Evaluate()
        {
            if (_model == null || !_model.IsTrained)
            {
                StatusMessage = "Нет обученной Accord модели для оценки";
                return;
            }

            if (_testPoints == null || _testPoints.Count == 0)
            {
                StatusMessage = "Нет тестовых данных для оценки";
                return;
            }

            try
            {
                int classCount = _allPoints.Select(p => p.Label).Distinct().Count();

                var trueLabels = _testPoints.Select(p => p.Label).ToArray();
                var predictedLabels = _testPoints.Select(p => _model.Predict(p.X, p.Y, p.Z)).ToArray();
                var predictedProb = _testPoints
                    .Select(p => PredictProbaArray(p.X, p.Y, p.Z, classCount))
                    .ToArray();

                var metrics = MetricsCalculator.Calculate(trueLabels, predictedProb, predictedLabels, classCount);

                Accuracy = metrics.Accuracy;
                Precision = metrics.Precision;
                Recall = metrics.Recall;
                F1Score = metrics.F1Score;
                MacroAuc = metrics.MacroAuc;
                UpdateRoc(metrics);

                StatusMessage = $"Accord модель оценена. Точность: {Accuracy:P1}";
                TrainingCompleted?.Invoke();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка оценки Accord: {ex.Message}";
            }
        }

        /// <summary>
        /// Возвращает обученную модель Accord SVM.
        /// </summary>
        /// <returns>Модель AccordSvmWrapper.</returns>
        public AccordSvmWrapper GetAccordModel()
        {
            return _model;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Вызывает событие изменения свойства.
        /// </summary>
        /// <param name="name">Имя изменившегося свойства.</param>
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}