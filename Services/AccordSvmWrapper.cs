using Accord.IO;
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
    public class AccordSvmWrapper : IClassifierModel
    {
        private MulticlassSupportVectorMachine<Linear> _svm;

        public bool IsTrained
        {
            get; private set;
        }

        public AccordSvmWrapper()
        {
            IsTrained = false;
        }

        public AccordSvmWrapper(MulticlassSupportVectorMachine<Linear> model)
        {
            _svm = model;
            IsTrained = model != null && model.NumberOfClasses > 0;
        }

        public void Train(List<Point3D> samples, TrainingConfiguration config)
        {
            if (samples == null || samples.Count == 0)
                throw new ArgumentException("Обучающие данные отсутствуют.");

            double[][] inputs = samples
                .Select(p => new[] { p.X, p.Y, p.Z })
                .ToArray();

            int[] outputs = samples
                .Select(p => p.Label)
                .ToArray();

            var teacher = new MulticlassSupportVectorLearning<Linear>()
            {
                Learner = (param) => new LinearDualCoordinateDescent()
                {
                    Complexity = config.C,
                    Tolerance = config.Tolerance
                }
            };

            _svm = teacher.Learn(inputs, outputs);
            IsTrained = _svm != null && _svm.NumberOfClasses > 0;
        }

        public int Predict(double x, double y, double z)
        {
            if (!IsTrained || _svm == null)
                throw new InvalidOperationException("Модель Accord не обучена.");

            return _svm.Decide(new[] { x, y, z });
        }

        public Dictionary<int, double> PredictProba(double x, double y, double z)
        {
            if (!IsTrained || _svm == null)
                throw new InvalidOperationException("Модель Accord не обучена.");

            double[] scores = _svm.Scores(new[] { x, y, z });
            double max = scores.Max();
            double[] exp = scores.Select(v => Math.Exp(v - max)).ToArray();
            double sum = exp.Sum();

            var result = new Dictionary<int, double>();
            for (int i = 0; i < exp.Length; i++)
                result[i] = exp[i] / sum;

            return result;
        }

        public double[] PredictProbaArray(double x, double y, double z)
        {
            if (!IsTrained || _svm == null)
                throw new InvalidOperationException("Модель Accord не обучена.");

            var probs = PredictProba(x, y, z);
            int classCount = _svm.NumberOfClasses;
            var result = new double[classCount];

            for (int i = 0; i < classCount; i++)
                result[i] = probs.ContainsKey(i) ? probs[i] : 0;

            return result;
        }

        public void SaveToFile(string filePath)
        {
            if (!IsTrained || _svm == null)
                throw new InvalidOperationException("Модель не обучена");

            Serializer.Save(_svm, filePath);
        }

        public static AccordSvmWrapper LoadFromFile(string filePath)
        {
            var model = Serializer.Load<MulticlassSupportVectorMachine<Linear>>(filePath);
            if (model == null)
                throw new Exception("Не удалось загрузить Accord модель.");

            return new AccordSvmWrapper(model);
        }

        public MulticlassSupportVectorMachine<Linear> GetRawModel() => _svm;

        public void SetRawModel(MulticlassSupportVectorMachine<Linear> model)
        {
            _svm = model;
            IsTrained = model != null && model.NumberOfClasses > 0;
        }

        public SvmModelData ToData()
        {
            if (!IsTrained || _svm == null)
                throw new InvalidOperationException("Модель не обучена");

            return new SvmModelData
            {
                Classes = Enumerable.Range(0, _svm.NumberOfClasses).ToList(),
                Models = new List<SvmBinaryModelData>(),
                Metadata = new Dictionary<string, string>
                {
                    ["Framework"] = "Accord.NET",
                    ["Kernel"] = "Linear",
                    ["Classes"] = _svm.NumberOfClasses.ToString()
                }
            };
        }

        public void LoadFromData(SvmModelData data)
        {
            throw new NotSupportedException("Accord использует бинарную сериализацию.");
        }
    }
}