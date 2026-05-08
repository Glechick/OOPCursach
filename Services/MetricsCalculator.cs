using System;
using System.Collections.Generic;
using System.Linq;
using SVMKurs.Models;

namespace SVMKurs.Services
{
    /// <summary>
    /// Вычисляет метрики качества классификации, включая ROC-кривые и AUC.
    /// Поддерживает бинарную и многоклассовую классификацию.
    /// </summary>
    public static class MetricsCalculator
    {
        /// <summary>
        /// Выполняет полный расчёт метрик по истинным меткам, предсказанным меткам и вероятностям.
        /// </summary>
        public static Metrics Calculate(
            int[] trueLabels,
            double[][] predictedProbabilities,
            int[] predictedLabels,
            int classCount)
        {
            var metrics = new Metrics();

            metrics.Accuracy = trueLabels.Zip(predictedLabels, (t, p) => t == p ? 1.0 : 0.0).Average();
            metrics.ConfusionMatrix = BuildConfusionMatrix(trueLabels, predictedLabels, classCount);

            metrics.ClassPrecision = new double[classCount];
            metrics.ClassRecall = new double[classCount];
            metrics.ClassF1 = new double[classCount];

            for (int c = 0; c < classCount; c++)
            {
                int tp = metrics.ConfusionMatrix[c][c];
                int fp = Enumerable.Range(0, classCount).Where(i => i != c).Sum(i => metrics.ConfusionMatrix[i][c]);
                int fn = Enumerable.Range(0, classCount).Where(i => i != c).Sum(i => metrics.ConfusionMatrix[c][i]);

                metrics.ClassPrecision[c] = tp + fp == 0 ? 0 : (double)tp / (tp + fp);
                metrics.ClassRecall[c] = tp + fn == 0 ? 0 : (double)tp / (tp + fn);

                metrics.ClassF1[c] = (metrics.ClassPrecision[c] + metrics.ClassRecall[c]) == 0
                    ? 0
                    : 2 * metrics.ClassPrecision[c] * metrics.ClassRecall[c] /
                      (metrics.ClassPrecision[c] + metrics.ClassRecall[c]);
            }

            metrics.Precision = metrics.ClassPrecision.Average();
            metrics.Recall = metrics.ClassRecall.Average();
            metrics.F1Score = metrics.ClassF1.Average();

            metrics.RocCurves = new Dictionary<int, List<(double fpr, double tpr)>>();

            for (int c = 0; c < classCount; c++)
                metrics.RocCurves[c] = ComputeRocForClass(trueLabels, predictedProbabilities, c);

            metrics.RocMacro = ComputeMacroRoc(metrics.RocCurves, classCount);
            metrics.MacroAuc = ComputeAuc(metrics.RocMacro);
            metrics.MicroAuc = ComputeMicroAuc(trueLabels, predictedProbabilities, classCount);

            return metrics;
        }

        /// <summary>
        /// Формирует матрицу ошибок.
        /// </summary>
        private static int[][] BuildConfusionMatrix(int[] trueLabels, int[] predictedLabels, int classCount)
        {
            var matrix = new int[classCount][];
            for (int i = 0; i < classCount; i++)
                matrix[i] = new int[classCount];

            for (int i = 0; i < trueLabels.Length; i++)
                matrix[trueLabels[i]][predictedLabels[i]]++;

            return matrix;
        }

        /// <summary>
        /// Строит ROC-кривую для одного класса.
        /// </summary>
        private static List<(double fpr, double tpr)> ComputeRocForClass(
            int[] trueLabels,
            double[][] predictedProbabilities,
            int classId)
        {
            var scores = predictedProbabilities.Select(p => p[classId]).ToArray();
            var labels = trueLabels.Select(t => t == classId ? 1 : 0).ToArray();

            var thresholds = scores.Distinct().OrderByDescending(x => x).ToList();
            var roc = new List<(double fpr, double tpr)>();

            foreach (var th in thresholds)
            {
                int tp = 0, fp = 0, tn = 0, fn = 0;

                for (int i = 0; i < scores.Length; i++)
                {
                    int predicted = scores[i] >= th ? 1 : 0;

                    if (predicted == 1 && labels[i] == 1)
                        tp++;
                    else if (predicted == 1 && labels[i] == 0)
                        fp++;
                    else if (predicted == 0 && labels[i] == 0)
                        tn++;
                    else
                        fn++;
                }

                double tpr = tp + fn == 0 ? 0 : (double)tp / (tp + fn);
                double fpr = fp + tn == 0 ? 0 : (double)fp / (fp + tn);

                roc.Add((fpr, tpr));
            }

            roc.Add((0, 0));
            roc.Add((1, 1));

            return roc.OrderBy(p => p.fpr).ToList();
        }

        /// <summary>
        /// Вычисляет усреднённую (macro) ROC-кривую.
        /// </summary>
        private static List<(double fpr, double tpr)> ComputeMacroRoc(
            Dictionary<int, List<(double fpr, double tpr)>> curves,
            int classCount)
        {
            var macro = new List<(double fpr, double tpr)>();
            var allFpr = curves.Values.SelectMany(c => c.Select(p => p.fpr)).Distinct().OrderBy(x => x);

            foreach (var fpr in allFpr)
            {
                double avgTpr = curves.Values
                    .Select(curve => curve.OrderBy(p => Math.Abs(p.fpr - fpr)).First().tpr)
                    .Average();

                macro.Add((fpr, avgTpr));
            }

            return macro;
        }

        /// <summary>
        /// Вычисляет площадь под ROC-кривой (AUC).
        /// </summary>
        private static double ComputeAuc(List<(double fpr, double tpr)> roc)
        {
            double auc = 0;

            for (int i = 1; i < roc.Count; i++)
            {
                double x1 = roc[i - 1].fpr;
                double x2 = roc[i].fpr;
                double y1 = roc[i - 1].tpr;
                double y2 = roc[i].tpr;

                auc += (x2 - x1) * (y1 + y2) / 2.0;
            }

            return auc;
        }

        /// <summary>
        /// Вычисляет micro-AUC по всем классам.
        /// </summary>
        private static double ComputeMicroAuc(
            int[] trueLabels,
            double[][] predictedProbabilities,
            int classCount)
        {
            var allScores = new List<double>();
            var allLabels = new List<int>();

            for (int i = 0; i < trueLabels.Length; i++)
            {
                for (int c = 0; c < classCount; c++)
                {
                    allScores.Add(predictedProbabilities[i][c]);
                    allLabels.Add(trueLabels[i] == c ? 1 : 0);
                }
            }

            var roc = ComputeRocForClass(allLabels.ToArray(), allScores.Select(s => new[] { s }).ToArray(), 0);
            return ComputeAuc(roc);
        }
    }
}
