using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;
using SVMKurs.Algorithms;
using SVMKurs.Models;
using SVMKurs.Services;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Collections.ObjectModel;

namespace SVMKurs.ViewModels
{
    /// <summary>
    /// ViewModel пользовательской реализации SVM.
    /// Обеспечивает обучение, расчёт метрик, построение ROC-кривой
    /// и управление параметрами алгоритма.
    /// </summary>
    public class MySvmViewModel : INotifyPropertyChanged
    {
        private MulticlassSvm3D _model;
        private TrainingConfiguration _config;

        private List<(double x, double y, double z, int label)> _allPoints;
        private List<(double x, double y, double z, int label)> _trainPoints;
        private List<(double x, double y, double z, int label)> _testPoints;

        private Dictionary<int, string> _classNames;

        private int _trainPercentage = 70;
        private int _trainCount;
        private int _testCount;
        private int _totalCount;

        private string _statusMessage;

        private double _accuracy;
        private double _precision;
        private double _recall;
        private double _f1Score;
        private double _macroAuc;

        private PlotModel _rocPlotModel;

        public event Action TrainingCompleted;
        public event PropertyChangedEventHandler PropertyChanged;

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
                UpdateSplit();
            }
        }

        /// <summary>
        /// Процент тестовой выборки.
        /// </summary>
        public int TestPercentage => 100 - TrainPercentage;

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
        /// Список функций потерь.
        /// </summary>
        public ObservableCollection<string> LossFunctions
        {
            get; set;
        }

        /// <summary>
        /// Конфигурация обучения.
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
        /// Сообщение о состоянии обучения.
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
        /// Точность.
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
        /// Precision.
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
        /// Recall.
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
        /// F1 Score.
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

        /// <summary>
        /// Macro AUC.
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
        /// ROC-кривая.
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
        /// Признак того, что модель обучена
        /// </summary>
        public bool IsTrained => _model != null && _model.IsTrained;

        /// <summary>
        /// Команда обучения.
        /// </summary>
        public ICommand TrainCommand
        {
            get;
        }

        /// <summary>
        /// Создаёт новый экземпляр MySvmViewModel.
        /// </summary>
        public MySvmViewModel()
        {
            _model = new MulticlassSvm3D();
            _config = new TrainingConfiguration();

            _allPoints = new List<(double, double, double, int)>();
            _classNames = new Dictionary<int, string>();

            LossFunctions = new ObservableCollection<string> { "Hinge", "SquaredHinge" };

            TrainCommand = new RelayCommand(_ => Train());
        }

        /// <summary>
        /// Устанавливает данные и пересчитывает разбиение.
        /// </summary>
        public void SetData(List<Point3D> allPoints, Dictionary<int, string> classNames)
        {
            _allPoints = allPoints.Select(p => (p.X, p.Y, p.Z, p.Label)).ToList();
            _classNames = classNames;

            TotalCount = _allPoints.Count;
            UpdateSplit();
        }

        /// <summary>
        /// Пересчитывает train/test разбиение.
        /// </summary>
        private void UpdateSplit()
        {
            if (_allPoints.Count == 0)
            {
                TrainCount = 0;
                TestCount = 0;
                _trainPoints = new List<(double, double, double, int)>();
                _testPoints = new List<(double, double, double, int)>();
                return;
            }

            TrainCount = (int)(TotalCount * TrainPercentage / 100.0);
            TestCount = TotalCount - TrainCount;

            var rnd = new Random(42);
            var shuffled = _allPoints.OrderBy(_ => rnd.Next()).ToList();

            _trainPoints = shuffled.Take(TrainCount).ToList();
            _testPoints = shuffled.Skip(TrainCount).ToList();

            StatusMessage = $"Разделение: train={TrainCount}, test={TestCount}";
        }

        /// <summary>
        /// Обучает модель, считает метрики и строит ROC.
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
                StatusMessage = "Обучение...";

                var samples = _trainPoints
                    .Select(p => new Point3D(p.x, p.y, p.z, p.label))
                    .ToList();

                _model = new MulticlassSvm3D();
                _model.Train(samples, _config);

                var trueLabels = _testPoints.Select(p => p.label).ToArray();
                var predictedLabels = _testPoints.Select(p => _model.Predict(p.x, p.y, p.z)).ToArray();
                var predictedProb = _testPoints
                    .Select(p => _model.PredictProba(p.x, p.y, p.z).Values.ToArray())
                    .ToArray();

                var metrics = MetricsCalculator.Calculate(
                    trueLabels,
                    predictedProb,
                    predictedLabels,
                    _classNames.Count);

                Accuracy = metrics.Accuracy;
                Precision = metrics.Precision;
                Recall = metrics.Recall;
                F1Score = metrics.F1Score;
                MacroAuc = metrics.MacroAuc;

                UpdateRoc(metrics);

                StatusMessage = $"Обучение завершено. Точность: {Accuracy:P1}";
                TrainingCompleted?.Invoke();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка: {ex.Message}";
            }
        }

        /// <summary>
        /// Обновляет ROC-кривую.
        /// </summary>
        private void UpdateRoc(Metrics m)
        {
            var model = new PlotModel { Title = "ROC-кривая" };

            model.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Minimum = 0, Maximum = 1, Title = "FPR" });
            model.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 1, Title = "TPR" });

            var roc = new LineSeries { Color = OxyColors.Green, StrokeThickness = 2 };

            foreach (var p in m.RocMacro)
                roc.Points.Add(new DataPoint(p.fpr, p.tpr));

            model.Series.Add(roc);
            RocPlotModel = model;
        }

        /// <summary>
        /// Оценивает загруженную модель на тестовых данных без переобучения
        /// </summary>
        public void Evaluate()
        {
            if (_model == null || !_model.IsTrained)
            {
                StatusMessage = "Нет обученной модели для оценки";
                return;
            }

            if (_testPoints == null || _testPoints.Count == 0)
            {
                StatusMessage = "Нет тестовых данных для оценки";
                return;
            }

            try
            {
                StatusMessage = "Оценка модели на тестовых данных...";

                var trueLabels = _testPoints.Select(p => p.label).ToArray();
                var predictedLabels = _testPoints.Select(p => _model.Predict(p.x, p.y, p.z)).ToArray();
                var predictedProb = _testPoints
                    .Select(p => _model.PredictProba(p.x, p.y, p.z).Values.ToArray())
                    .ToArray();

                var metrics = MetricsCalculator.Calculate(
                    trueLabels,
                    predictedProb,
                    predictedLabels,
                    _classNames.Count);

                Accuracy = metrics.Accuracy;
                Precision = metrics.Precision;
                Recall = metrics.Recall;
                F1Score = metrics.F1Score;
                MacroAuc = metrics.MacroAuc;
                UpdateRoc(metrics);

                StatusMessage = $"Оценка завершена. Точность: {Accuracy:P1}";
                TrainingCompleted?.Invoke();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка оценки: {ex.Message}";
            }
        }

        /// <summary>
        /// Возвращает обученную модель.
        /// </summary>
        public MulticlassSvm3D GetModel() => _model;

        public void SetModel(MulticlassSvm3D model)
        {
            _model = model;
            OnPropertyChanged(nameof(IsTrained));
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}