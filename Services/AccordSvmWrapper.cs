using Accord.MachineLearning.VectorMachines;
using Accord.MachineLearning.VectorMachines.Learning;
using Accord.Statistics.Kernels;
using SVMKurs.Algorithms;
using SVMKurs.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SVMKurs.Services
{
    /// <summary>
    /// Обёртка над Accord.NET для обучения и использования
    /// многоклассовой SVM с линейным ядром.
    /// Реализует единый интерфейс IClassifierModel, чтобы
    /// CompareViewModel мог работать с любой моделью одинаково.
    /// </summary>
    public class AccordSvmWrapper : IClassifierModel
    {
        /// <summary>
        /// Внутренняя Accord‑модель SVM.
        /// </summary>
        private MulticlassSupportVectorMachine<Linear> _svm;

        /// <summary>
        /// Признак того, что модель обучена и готова к использованию.
        /// </summary>
        public bool IsTrained
        {
            get; private set;
        }

        /// <summary>
        /// Конструктор по умолчанию (используется при загрузке модели).
        /// </summary>
        public AccordSvmWrapper()
        {
        }

        /// <summary>
        /// Конструктор, принимающий уже готовую Accord‑модель.
        /// Используется при загрузке модели из файла.
        /// </summary>
        public AccordSvmWrapper(MulticlassSupportVectorMachine<Linear> model)
        {
            _svm = model;
            IsTrained = model != null;
        }

        /// <summary>
        /// Обучает Accord SVM по списку трёхмерных точек.
        /// </summary>
        public void Train(List<Point3D> samples, TrainingConfiguration config)
        {
            if (samples == null || samples.Count == 0)
                throw new ArgumentException("Список обучающих данных пуст.");

            double[][] inputs = samples
                .Select(s => new double[] { s.X, s.Y, s.Z })
                .ToArray();

            int[] outputs = samples
                .Select(s => s.Label)
                .ToArray();

            var teacher = new MulticlassSupportVectorLearning<Linear>()
            {
                Learner = (p) => new LinearDualCoordinateDescent()
                {
                    Complexity = config.C,
                    Tolerance = config.Tolerance
                }
            };

            _svm = teacher.Learn(inputs, outputs);
            IsTrained = true;
        }

        /// <summary>
        /// Предсказывает класс по трём признакам.
        /// </summary>
        public int Predict(double x, double y, double z)
        {
            if (!IsTrained || _svm == null)
                throw new InvalidOperationException("Модель Accord не обучена.");

            return _svm.Decide(new double[] { x, y, z });
        }

        /// <summary>
        /// Возвращает вероятности принадлежности к каждому классу.
        /// Реализовано через softmax по значениям decision function.
        /// </summary>
        public Dictionary<int, double> PredictProba(double x, double y, double z)
        {
            if (!IsTrained || _svm == null)
                throw new InvalidOperationException("Модель Accord не обучена.");

            double[] scores = _svm.Scores(new double[] { x, y, z });

            double max = scores.Max();
            double[] exp = scores.Select(s => Math.Exp(s - max)).ToArray();
            double sum = exp.Sum();

            var result = new Dictionary<int, double>();
            for (int i = 0; i < exp.Length; i++)
                result[i] = exp[i] / sum;

            return result;
        }

        /// <summary>
        /// Возвращает внутреннюю Accord‑модель для сохранения.
        /// </summary>
        public MulticlassSupportVectorMachine<Linear> GetRawModel() => _svm;

        /// <summary>
        /// Устанавливает внутреннюю Accord‑модель (после загрузки из файла).
        /// </summary>
        public void SetRawModel(MulticlassSupportVectorMachine<Linear> model)
        {
            _svm = model;
            IsTrained = model != null;
        }
    }
}
