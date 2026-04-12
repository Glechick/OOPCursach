using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace SVMKurs.ViewModels
{
    /// <summary>
    /// Модель представления для демонстрации SVM
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly LinearSvm _svm;
        private readonly SvmTrainer _trainer;

        private ObservableCollection<TrainingSample> _trainingSamples;
        private double _testFeature1;
        private double _testFeature2;
        private ClassificationResult _classificationResult;
        private bool _isTrained;
        private string _statusMessage;

        public event Action RequestRedraw;

        public ObservableCollection<TrainingSample> TrainingSamples
        {
            get => _trainingSamples;
            set
            {
                _trainingSamples = value;
                OnPropertyChanged();
            }
        }

        public double TestFeature1
        {
            get => _testFeature1;
            set
            {
                _testFeature1 = value;
                OnPropertyChanged();
                ClassifyTestPoint();
            }
        }

        public double TestFeature2
        {
            get => _testFeature2;
            set
            {
                _testFeature2 = value;
                OnPropertyChanged();
                ClassifyTestPoint();
            }
        }

        public ClassificationResult ClassificationResult
        {
            get => _classificationResult;
            set
            {
                _classificationResult = value;
                OnPropertyChanged();
            }
        }

        public bool IsTrained
        {
            get => _isTrained;
            set
            {
                _isTrained = value;
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

        // Команды
        public ICommand TrainCommand
        {
            get;
        }
        public ICommand ResetCommand
        {
            get;
        }
        public ICommand GenerateDataCommand
        {
            get;
        }

        public MainViewModel()
        {
            _svm = new LinearSvm();
            _trainer = new SvmTrainer(_svm);

            // Инициализация тестовых данных
            TestFeature1 = 5;
            TestFeature2 = 5;

            // Создание обучающей выборки
            InitializeTrainingData();

            // Команды
            TrainCommand = new RelayCommand(_ => TrainModel(), _ => TrainingSamples.Count >= 2);
            ResetCommand = new RelayCommand(_ => ResetModel());
            GenerateDataCommand = new RelayCommand(_ => GenerateNewData());

            StatusMessage = "Модель не обучена. Нажмите 'Обучить SVM'";
        }

        private void InitializeTrainingData()
        {
            TrainingSamples = new ObservableCollection<TrainingSample>
            {
                // Класс 1 (синий) - "Безопасные"
                new TrainingSample { Name = "A1", Feature1 = 2, Feature2 = 8, TrueClass = 1 },
                new TrainingSample { Name = "A2", Feature1 = 3, Feature2 = 7, TrueClass = 1 },
                new TrainingSample { Name = "A3", Feature1 = 4, Feature2 = 9, TrueClass = 1 },
                new TrainingSample { Name = "A4", Feature1 = 1, Feature2 = 9, TrueClass = 1 },
                new TrainingSample { Name = "A5", Feature1 = 3, Feature2 = 8.5, TrueClass = 1 },
                
                // Класс -1 (красный) - "Опасные"
                new TrainingSample { Name = "B1", Feature1 = 7, Feature2 = 2, TrueClass = -1 },
                new TrainingSample { Name = "B2", Feature1 = 8, Feature2 = 3, TrueClass = -1 },
                new TrainingSample { Name = "B3", Feature1 = 6, Feature2 = 1.5, TrueClass = -1 },
                new TrainingSample { Name = "B4", Feature1 = 9, Feature2 = 2.5, TrueClass = -1 },
                new TrainingSample { Name = "B5", Feature1 = 7.5, Feature2 = 4, TrueClass = -1 }
            };
        }

        private void TrainModel()
        {
            try
            {
                StatusMessage = "Обучение SVM...";
                var points = TrainingSamples.Select(s => new Point2D(s.Feature1, s.Feature2, s.TrueClass)).ToList();
                _trainer.Train(points, C: 1.0);
                IsTrained = true;
                StatusMessage = $"Обучение завершено! Найдено {_svm.SupportVectors.Count} опорных векторов";
                ClassifyTestPoint();
                RequestRedraw?.Invoke();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка обучения: {ex.Message}";
            }
        }

        private void ResetModel()
        {
            _svm.Reset();
            IsTrained = false;
            ClassificationResult = null;
            StatusMessage = "Модель сброшена";
            RequestRedraw?.Invoke();
        }

        private void GenerateNewData()
        {
            ResetModel();
            var random = new Random();

            for (int i = 0; i < TrainingSamples.Count; i++)
            {
                var sample = TrainingSamples[i];
                if (sample.TrueClass == 1)
                {
                    sample.Feature1 = 2 + random.NextDouble() * 3;
                    sample.Feature2 = 7 + random.NextDouble() * 2;
                }
                else
                {
                    sample.Feature1 = 6 + random.NextDouble() * 3;
                    sample.Feature2 = 1 + random.NextDouble() * 3;
                }
            }

            // Обновляем коллекцию
            var temp = TrainingSamples.ToList();
            TrainingSamples.Clear();
            foreach (var sample in temp)
                TrainingSamples.Add(sample);

            StatusMessage = "Сгенерированы новые данные";
            RequestRedraw?.Invoke();
        }

        private void ClassifyTestPoint()
        {
            if (!IsTrained)
                return;

            var result = _svm.Predict(TestFeature1, TestFeature2);
            ClassificationResult = new ClassificationResult
            {
                PredictedClass = result.PredictedClass,
                PredictedClassName = result.PredictedClass == 1 ? "Безопасный" : "Опасный",
                DecisionValue = result.DecisionValue,
                Confidence = result.Confidence
            };

            RequestRedraw?.Invoke();
        }

        public (double Slope, double Intercept) GetDecisionBoundary()
        {
            return _svm.GetDecisionBoundary();
        }

        public System.Collections.Generic.List<TrainingSample> GetSupportVectors()
        {
            return _svm.SupportVectors.Select(sv => new TrainingSample
            {
                Feature1 = sv.X,
                Feature2 = sv.Y,
                TrueClass = sv.Label,
                Name = $"SV_{sv.X:F1}_{sv.Y:F1}"
            }).ToList();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    // Вспомогательные классы

    public class TrainingSample : INotifyPropertyChanged
    {
        private double _feature1;
        private double _feature2;

        public string Name
        {
            get; set;
        }

        public double Feature1
        {
            get => _feature1;
            set
            {
                _feature1 = value;
                OnPropertyChanged();
            }
        }

        public double Feature2
        {
            get => _feature2;
            set
            {
                _feature2 = value;
                OnPropertyChanged();
            }
        }

        public int TrueClass
        {
            get; set;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class ClassificationResult
    {
        public int PredictedClass
        {
            get; set;
        }
        public string PredictedClassName
        {
            get; set;
        }
        public double DecisionValue
        {
            get; set;
        }
        public double Confidence
        {
            get; set;
        }
    }

    public class Point2D
    {
        public double X
        {
            get; set;
        }
        public double Y
        {
            get; set;
        }
        public int Label
        {
            get; set;
        }

        public Point2D(double x, double y, int label)
        {
            X = x;
            Y = y;
            Label = label;
        }
    }

    // Классы алгоритма SVM

    public class LinearSvm
    {
        public double W0
        {
            get; private set;
        }
        public double W1
        {
            get; private set;
        }
        public double Bias
        {
            get; private set;
        }
        public bool IsTrained
        {
            get; private set;
        }
        public List<(double X, double Y, int Label)> SupportVectors
        {
            get; private set;
        }

        public LinearSvm()
        {
            SupportVectors = new List<(double, double, int)>();
            IsTrained = false;
        }

        public (int PredictedClass, double DecisionValue, double Confidence) Predict(double x, double y)
        {
            if (!IsTrained)
                throw new InvalidOperationException("Модель не обучена");

            double decision = W0 * x + W1 * y + Bias;
            int predictedClass = decision >= 0 ? 1 : -1;
            double confidence = 1.0 / (1.0 + Math.Exp(-Math.Abs(decision)));

            return (predictedClass, decision, confidence);
        }

        public (double Slope, double Intercept) GetDecisionBoundary()
        {
            if (!IsTrained || Math.Abs(W1) < 1e-10)
                return (0, 0);

            double slope = -W0 / W1;
            double intercept = -Bias / W1;

            return (slope, intercept);
        }

        public void SetWeights(double w0, double w1, double bias)
        {
            W0 = w0;
            W1 = w1;
            Bias = bias;
            IsTrained = true;
        }

        public void AddSupportVector(double x, double y, int label)
        {
            SupportVectors.Add((x, y, label));
        }

        public void Reset()
        {
            W0 = 0;
            W1 = 0;
            Bias = 0;
            IsTrained = false;
            SupportVectors.Clear();
        }
    }

    public class SvmTrainer
    {
        private readonly LinearSvm _svm;

        public SvmTrainer(LinearSvm svm)
        {
            _svm = svm;
        }

        public void Train(List<Point2D> points, double C = 1.0, int maxIterations = 100)
        {
            if (points.Count < 2)
                throw new ArgumentException("Нужно минимум 2 точки для обучения");

            int n = points.Count;
            double[][] data = points.Select(p => new[] { p.X, p.Y }).ToArray();
            int[] labels = points.Select(p => p.Label).ToArray();

            double[] alphas = new double[n];
            double bias = 0;

            Random random = new Random();

            for (int iter = 0; iter < maxIterations; iter++)
            {
                double alphaChanged = 0;

                for (int i = 0; i < n; i++)
                {
                    double error_i = GetError(i, data, labels, alphas, bias);

                    if ((labels[i] * error_i < -1e-5 && alphas[i] < C) ||
                        (labels[i] * error_i > 1e-5 && alphas[i] > 0))
                    {
                        int j = i;
                        while (j == i)
                            j = random.Next(n);

                        double error_j = GetError(j, data, labels, alphas, bias);

                        double alpha_i_old = alphas[i];
                        double alpha_j_old = alphas[j];

                        double L, H;
                        if (labels[i] != labels[j])
                        {
                            L = Math.Max(0, alphas[j] - alphas[i]);
                            H = Math.Min(C, C + alphas[j] - alphas[i]);
                        }
                        else
                        {
                            L = Math.Max(0, alphas[i] + alphas[j] - C);
                            H = Math.Min(C, alphas[i] + alphas[j]);
                        }

                        if (Math.Abs(L - H) < 1e-10)
                            continue;

                        double eta = 2 * Kernel(data[i], data[j]) -
                                     Kernel(data[i], data[i]) -
                                     Kernel(data[j], data[j]);

                        if (eta >= 0)
                            continue;

                        alphas[j] = alpha_j_old - (labels[j] * (error_i - error_j)) / eta;
                        alphas[j] = Math.Min(H, Math.Max(L, alphas[j]));

                        if (Math.Abs(alphas[j] - alpha_j_old) < 1e-10)
                            continue;

                        alphas[i] = alpha_i_old + labels[i] * labels[j] * (alpha_j_old - alphas[j]);

                        double b1 = bias - error_i -
                                   labels[i] * (alphas[i] - alpha_i_old) * Kernel(data[i], data[i]) -
                                   labels[j] * (alphas[j] - alpha_j_old) * Kernel(data[i], data[j]);

                        double b2 = bias - error_j -
                                   labels[i] * (alphas[i] - alpha_i_old) * Kernel(data[i], data[j]) -
                                   labels[j] * (alphas[j] - alpha_j_old) * Kernel(data[j], data[j]);

                        bias = (b1 + b2) / 2;
                        alphaChanged++;
                    }
                }

                if (alphaChanged < 1e-5)
                    break;
            }

            double w0 = 0, w1 = 0;
            for (int i = 0; i < n; i++)
            {
                if (alphas[i] > 0)
                {
                    w0 += alphas[i] * labels[i] * data[i][0];
                    w1 += alphas[i] * labels[i] * data[i][1];

                    if (alphas[i] > 1e-5)
                    {
                        _svm.AddSupportVector(data[i][0], data[i][1], labels[i]);
                    }
                }
            }

            _svm.SetWeights(w0, w1, bias);
        }

        private double Kernel(double[] x1, double[] x2)
        {
            return x1[0] * x2[0] + x1[1] * x2[1];
        }

        private double GetError(int i, double[][] data, int[] labels, double[] alphas, double bias)
        {
            double sum = 0;
            for (int j = 0; j < data.Length; j++)
            {
                sum += alphas[j] * labels[j] * Kernel(data[j], data[i]);
            }
            return sum + bias - labels[i];
        }
    }
}