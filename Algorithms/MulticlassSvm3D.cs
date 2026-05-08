using SVMKurs.Models;
using SVMKurs.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SVMKurs.Algorithms
{
    /// <summary>
    /// Многоклассовый SVM-классификатор для трёхмерных данных.
    /// Использует стратегию "один-против-одного".
    /// </summary>
    public class MulticlassSvm3D : IClassifierModel
    {
        private readonly List<(int classA, int classB, LinearSvm3D svm)> _models =
            new List<(int, int, LinearSvm3D)>();

        private List<int> _classes = new();

        /// <summary>
        /// Признак того, что модель обучена.
        /// </summary>
        public bool IsTrained => _models.Count > 0;

        /// <summary>
        /// Обучает модель на наборе данных Point3D.
        /// </summary>
        public void Train(List<Point3D> data, TrainingConfiguration config)
        {
            if (data == null || data.Count == 0)
                throw new ArgumentException("Набор данных пуст.");

            _models.Clear();

            _classes = data.Select(d => d.Label)
                           .Distinct()
                           .OrderBy(v => v)
                           .ToList();

            if (_classes.Count < 2)
                throw new InvalidOperationException("Нужно минимум два класса.");

            for (int i = 0; i < _classes.Count; i++)
            {
                for (int j = i + 1; j < _classes.Count; j++)
                {
                    int classA = _classes[i];
                    int classB = _classes[j];

                    var subset = new List<Point3D>();

                    foreach (var p in data)
                    {
                        if (p.Label == classA)
                            subset.Add(new Point3D(p.X, p.Y, p.Z, +1));
                        else if (p.Label == classB)
                            subset.Add(new Point3D(p.X, p.Y, p.Z, -1));
                    }

                    if (subset.Count < 2)
                        continue;

                    var trainer = new SvmTrainer3D(config.C);
                    var svm = trainer.Train(subset);

                    _models.Add((classA, classB, svm));
                }
            }
        }

        /// <summary>
        /// Предсказывает класс.
        /// </summary>
        public int Predict(double x, double y, double z)
        {
            if (!IsTrained)
                throw new InvalidOperationException("Модель не обучена.");

            var votes = new Dictionary<int, int>();

            foreach (var (classA, classB, svm) in _models)
            {
                int result = svm.Predict(x, y, z);
                int winner = result == 1 ? classA : classB;

                if (!votes.ContainsKey(winner))
                    votes[winner] = 0;

                votes[winner]++;
            }

            return votes.OrderByDescending(v => v.Value).First().Key;
        }

        /// <summary>
        /// Возвращает вероятности по классам.
        /// </summary>
        public Dictionary<int, double> PredictProba(double x, double y, double z)
        {
            if (!IsTrained)
                throw new InvalidOperationException("Модель не обучена.");

            var scores = _classes.ToDictionary(c => c, c => 0.0);

            foreach (var (classA, classB, svm) in _models)
            {
                double d = svm.Decision(x, y, z);

                double pA = 1.0 / (1.0 + Math.Exp(-d));
                double pB = 1.0 - pA;

                scores[classA] += pA;
                scores[classB] += pB;
            }

            double max = scores.Values.Max();
            var exp = scores.ToDictionary(k => k.Key, v => Math.Exp(v.Value - max));
            double sum = exp.Values.Sum();

            return exp.ToDictionary(k => k.Key, v => v.Value / sum);
        }

        /// <summary>
        /// Возвращает вероятности в виде массива.
        /// </summary>
        public double[] PredictProbaArray(double x, double y, double z)
        {
            var dict = PredictProba(x, y, z);
            return _classes.Select(c => dict[c]).ToArray();
        }

        /// <summary>
        /// Преобразует модель в сериализуемую структуру.
        /// </summary>
        public SvmModelData ToData()
        {
            return new SvmModelData
            {
                Classes = _classes.ToList(),

                Models = _models.Select(m => new SvmBinaryModelData
                {
                    ClassA = m.classA,
                    ClassB = m.classB,
                    Bias = m.svm.Bias,

                    SupportVectors = m.svm.SupportVectors.Select(s => new SupportVectorData
                    {
                        X = s.X,
                        Y = s.Y,
                        Z = s.Z,
                        Label = s.Label,
                        Alpha = s.Alpha
                    }).ToList()
                }).ToList()
            };
        }

        /// <summary>
        /// Восстанавливает модель из сериализуемой структуры.
        /// </summary>
        public void LoadFromData(SvmModelData data)
        {
            _classes = data.Classes.ToList();
            _models.Clear();

            foreach (var m in data.Models)
            {
                var sv = m.SupportVectors
                .Select(s => new SupportVector3D(s.X, s.Y, s.Z, s.Label, s.Alpha))
                .ToList();

                var svm = new LinearSvm3D();
                svm.SetModel(sv, m.Bias);

                _models.Add((m.ClassA, m.ClassB, svm));
            }
        }
    }
}
