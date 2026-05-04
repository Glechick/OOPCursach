using System;
using System.Collections.Generic;
using System.Linq;

namespace SVMKurs.Algorithms
{
    /// <summary>
    /// Мультиклассовый классификатор на основе 3D Linear SVM
    /// Использует стратегию One-vs-One
    /// </summary>
    public class MulticlassSvm3D
    {
        private List<LinearSvm3D> _binarySvms = new List<LinearSvm3D>();
        private List<(int classA, int classB)> _classPairs = new List<(int, int)>();
        private Dictionary<int, string> _classNames = new Dictionary<int, string>();

        public bool IsTrained
        {
            get; private set;
        }
        public int NumberOfClasses => _classNames.Count;

        /// <summary>
        /// Обучение мультиклассового SVM
        /// </summary>
        /// <param name="samples">Точки с метками классов (0, 1, 2...)</param>
        /// <param name="classNames">Имена классов (круг, квадрат...)</param>
        /// <param name="C">Параметр регуляризации</param>
        public void Train(List<MulticlassPoint3D> samples, Dictionary<int, string> classNames, double C = 1.0)
        {
            _classNames = classNames;
            var uniqueClasses = classNames.Keys.OrderBy(k => k).ToList();
            int numClasses = uniqueClasses.Count;

            if (numClasses < 2)
                throw new ArgumentException("Нужно минимум 2 класса для обучения");

            _binarySvms.Clear();
            _classPairs.Clear();

            // Обучаем SVM для каждой пары классов
            for (int i = 0; i < numClasses; i++)
            {
                for (int j = i + 1; j < numClasses; j++)
                {
                    int classA = uniqueClasses[i];
                    int classB = uniqueClasses[j];

                    // Берём только точки двух текущих классов
                    var binaryPoints = samples
                        .Where(p => p.Label == classA || p.Label == classB)
                        .Select(p => new Point3D(p.X, p.Y, p.Z, p.Label == classA ? 1 : -1))
                        .ToList();

                    if (binaryPoints.Count == 0)
                        continue;

                    // Обучаем бинарный SVM
                    var svm = new LinearSvm3D();
                    var trainer = new SvmTrainer3D(svm);
                    trainer.Train(binaryPoints, C);

                    _binarySvms.Add(svm);
                    _classPairs.Add((classA, classB));
                }
            }

            IsTrained = true;
        }

        /// <summary>
        /// Классификация новой точки (голосование)
        /// </summary>
        public MulticlassResult3D Predict(double x, double y, double z)
        {
            if (!IsTrained)
                throw new InvalidOperationException("Модель не обучена");

            // Голосование
            int[] votes = new int[NumberOfClasses];
            double[] confidenceScores = new double[NumberOfClasses];
            double[] decisionValues = new double[NumberOfClasses];

            for (int k = 0; k < _binarySvms.Count; k++)
            {
                var result = _binarySvms[k].Predict(x, y, z);
                int winner = result.PredictedClass == 1 ? _classPairs[k].classA : _classPairs[k].classB;
                votes[winner]++;
                confidenceScores[winner] += result.Confidence;
                decisionValues[winner] += result.DecisionValue;
            }

            // Находим класс с максимальным количеством голосов
            int predictedClass = Array.IndexOf(votes, votes.Max());

            // Усредняем уверенность
            double avgConfidence = confidenceScores[predictedClass] / votes[predictedClass];
            double avgDecision = decisionValues[predictedClass] / votes[predictedClass];

            return new MulticlassResult3D
            {
                PredictedClass = predictedClass,
                PredictedClassName = _classNames[predictedClass],
                Confidence = avgConfidence,
                DecisionValue = avgDecision,
                AllVotes = votes.ToList()
            };
        }

        /// <summary>
        /// Получить все бинарные классификаторы (для анализа)
        /// </summary>
        public List<LinearSvm3D> GetBinaryClassifiers() => _binarySvms;

        /// <summary>
        /// Сброс модели
        /// </summary>
        public void Reset()
        {
            _binarySvms.Clear();
            _classPairs.Clear();
            _classNames.Clear();
            IsTrained = false;
        }

        /// <summary>
        /// Получить описание модели
        /// </summary>
        public string GetModelInfo()
        {
            if (!IsTrained)
                return "Модель не обучена";

            string info = $"Мультиклассовый SVM (One-vs-One)\n";
            info += $"Классов: {NumberOfClasses}\n";
            info += $"Бинарных классификаторов: {_binarySvms.Count}\n\n";

            for (int i = 0; i < _binarySvms.Count; i++)
            {
                var (classA, classB) = _classPairs[i];
                info += $"{_classNames[classA]} vs {_classNames[classB]}: {_binarySvms[i].GetEquation()}\n";
            }

            return info;
        }

        /// <summary>
        /// Оценка качества модели на тестовой выборке
        /// </summary>
        /// <returns>Точность (accuracy) от 0 до 1</returns>
        public double Evaluate(List<MulticlassPoint3D> testSamples)
        {
            if (!IsTrained)
                throw new InvalidOperationException("Модель не обучена");

            int correct = 0;
            foreach (var sample in testSamples)
            {
                var result = Predict(sample.X, sample.Y, sample.Z);
                if (result.PredictedClass == sample.Label)
                    correct++;
            }

            return (double)correct / testSamples.Count;
        }

        /// <summary>
        /// Получение матрицы ошибок (confusion matrix)
        /// </summary>
        public int[,] GetConfusionMatrix(List<MulticlassPoint3D> testSamples)
        {
            if (!IsTrained)
                throw new InvalidOperationException("Модель не обучена");

            int n = NumberOfClasses;
            int[,] matrix = new int[n, n];

            foreach (var sample in testSamples)
            {
                var result = Predict(sample.X, sample.Y, sample.Z);
                matrix[sample.Label, result.PredictedClass]++;
            }

            return matrix;
        }
    }

    /// <summary>
    /// Точка для мультиклассового обучения
    /// </summary>
    public class MulticlassPoint3D
    {
        public double X
        {
            get; set;
        }
        public double Y
        {
            get; set;
        }
        public double Z
        {
            get; set;
        }
        public int Label
        {
            get; set;
        }  // 0, 1, 2... для разных классов

        public MulticlassPoint3D(double x, double y, double z, int label)
        {
            X = x;
            Y = y;
            Z = z;
            Label = label;
        }
    }

    /// <summary>
    /// Результат мультиклассовой классификации
    /// </summary>
    public class MulticlassResult3D
    {
        public int PredictedClass
        {
            get; set;
        }
        public string PredictedClassName
        {
            get; set;
        }
        public double Confidence
        {
            get; set;
        }
        public double DecisionValue
        {
            get; set;
        }
        public List<int> AllVotes
        {
            get; set;
        }

        public override string ToString()
        {
            return $"{PredictedClassName} (уверенность: {Confidence:P1}, голосов: {AllVotes?.Max() ?? 0})";
        }
    }
}