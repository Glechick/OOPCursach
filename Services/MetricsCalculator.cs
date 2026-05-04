using System;
using System.Collections.Generic;
using System.Linq;
using SVMKurs.Models;
using SVMKurs.Algorithms;

namespace SVMKurs.Services
{
    /// <summary>
    /// Расчётчик метрик качества классификации
    /// </summary>
    public static class MetricsCalculator
    {
        /// <summary>
        /// Рассчитывает все метрики для обученной модели
        /// </summary>
        public static Metrics Calculate(MulticlassSvm3D model, List<MulticlassPoint3D> testSamples, string algorithmName)
        {
            var metrics = new Metrics();
            int numClasses = testSamples.Select(s => s.Label).Distinct().Count();

            var confusion = new int[numClasses][];
            for (int i = 0; i < numClasses; i++)
                confusion[i] = new int[numClasses];

            foreach (var sample in testSamples)
            {
                var result = model.Predict(sample.X, sample.Y, sample.Z);
                confusion[sample.Label][result.PredictedClass]++;
            }

            metrics.ConfusionMatrix = confusion;

            metrics.ClassPrecision = new double[numClasses];
            metrics.ClassRecall = new double[numClasses];
            metrics.ClassF1 = new double[numClasses];

            for (int i = 0; i < numClasses; i++)
            {
                int tp = confusion[i][i];
                int fp = 0, fn = 0;
                for (int j = 0; j < numClasses; j++)
                {
                    if (j != i)
                        fp += confusion[j][i];
                    if (j != i)
                        fn += confusion[i][j];
                }

                metrics.ClassPrecision[i] = tp + fp > 0 ? (double)tp / (tp + fp) : 0;
                metrics.ClassRecall[i] = tp + fn > 0 ? (double)tp / (tp + fn) : 0;
                metrics.ClassF1[i] = metrics.ClassPrecision[i] + metrics.ClassRecall[i] > 0
                    ? 2 * metrics.ClassPrecision[i] * metrics.ClassRecall[i] / (metrics.ClassPrecision[i] + metrics.ClassRecall[i])
                    : 0;
            }

            metrics.Precision = metrics.ClassPrecision.Average();
            metrics.Recall = metrics.ClassRecall.Average();
            metrics.F1Score = metrics.ClassF1.Average();

            int correct = 0;
            for (int i = 0; i < numClasses; i++)
                correct += confusion[i][i];
            metrics.Accuracy = (double)correct / testSamples.Count;

            return metrics;
        }

        /// <summary>
        /// Рассчитывает ROC-кривую для бинарной классификации
        /// </summary>
        public static List<RocPoint> CalculateRocCurve(List<(int True, int Predicted, double Score)> results, int numClasses)
        {
            var rocPoints = new List<RocPoint>();
            var sorted = results.OrderByDescending(r => r.Score).ToList();
            int totalPos = results.Count(r => r.True == 1);
            int totalNeg = results.Count(r => r.True == 0);

            int tp = 0, fp = 0;
            foreach (var r in sorted)
            {
                if (r.True == 1)
                    tp++;
                else
                    fp++;

                double tpr = totalPos > 0 ? (double)tp / totalPos : 0;
                double fpr = totalNeg > 0 ? (double)fp / totalNeg : 0;

                rocPoints.Add(new RocPoint { Fpr = fpr, Tpr = tpr, Threshold = r.Score });
            }

            return rocPoints;
        }

        /// <summary>
        /// Вычисляет площадь под ROC-кривой (AUC)
        /// </summary>
        public static double CalculateAuc(List<RocPoint> rocPoints)
        {
            if (rocPoints.Count < 2)
                return 0.5;

            double auc = 0;
            for (int i = 1; i < rocPoints.Count; i++)
            {
                auc += (rocPoints[i].Fpr - rocPoints[i - 1].Fpr) *
                       (rocPoints[i].Tpr + rocPoints[i - 1].Tpr) / 2;
            }
            return auc;
        }
    }
}