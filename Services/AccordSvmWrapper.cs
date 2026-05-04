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
    public class AccordSvmWrapper
    {
        private MulticlassSupportVectorMachine<Linear> _svm;
        private TrainingConfiguration _config;

        public bool IsTrained
        {
            get; private set;
        }

        public void Train(List<MulticlassPoint3D> samples, TrainingConfiguration config)
        {
            _config = config;

            double[][] inputs = samples.Select(s => new double[] { s.X, s.Y, s.Z }).ToArray();
            int[] outputs = samples.Select(s => s.Label).ToArray();

            var teacher = new MulticlassSupportVectorLearning<Linear>()
            {
                Learner = (p) => new LinearDualCoordinateDescent()
                {
                    Complexity = config.C,
                    Tolerance = config.Tolerance,
                    MaxIterations = config.MaxIterations
                }
            };

            _svm = teacher.Learn(inputs, outputs);
            IsTrained = true;
        }

        public int Predict(double x, double y, double z)
        {
            if (_svm == null)
                throw new InvalidOperationException("Модель не обучена");

            return _svm.Decide(new double[] { x, y, z });
        }
    }
}