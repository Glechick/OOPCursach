using System;
using System.Collections.Generic;
using System.Linq;

namespace SVMKurs.Algorithms
{
    /// <summary>
    /// SMO алгоритм для обучения 3D Linear SVM
    /// </summary>
    public class SvmTrainer3D
    {
        private readonly LinearSvm3D _svm;

        public SvmTrainer3D(LinearSvm3D svm)
        {
            _svm = svm;
        }

        /// <summary>
        /// Обучение SVM на 3D точках
        /// </summary>
        /// <param name="points">Точки для обучения (бинарная классификация)</param>
        /// <param name="C">Параметр регуляризации (штраф за ошибки)</param>
        /// <param name="maxIterations">Максимальное количество итераций</param>
        public void Train(List<Point3D> points, double C = 1.0, int maxIterations = 100)
        {
            if (points.Count < 2)
                throw new ArgumentException("Нужно минимум 2 точки для обучения");

            // Проверяем, что есть оба класса
            var uniqueLabels = points.Select(p => p.Label).Distinct().ToList();
            if (uniqueLabels.Count != 2)
                throw new ArgumentException($"Нужны оба класса (-1 и 1), а найдено: {string.Join(", ", uniqueLabels)}");

            int n = points.Count;
            double[][] data = points.Select(p => p.ToArray()).ToArray();
            int[] labels = points.Select(p => p.Label).ToArray();

            double[] alphas = new double[n];
            double bias = 0;
            var random = new Random();

            for (int iter = 0; iter < maxIterations; iter++)
            {
                double alphaChanged = 0;

                for (int i = 0; i < n; i++)
                {
                    double error_i = GetError(i, data, labels, alphas, bias);

                    // Проверяем условия ККТ
                    bool condition1 = labels[i] * error_i < -1e-5 && alphas[i] < C;
                    bool condition2 = labels[i] * error_i > 1e-5 && alphas[i] > 0;

                    if (condition1 || condition2)
                    {
                        // Выбираем вторую точку случайно
                        int j = random.Next(n);
                        while (j == i)
                            j = random.Next(n);

                        double error_j = GetError(j, data, labels, alphas, bias);

                        double alpha_i_old = alphas[i];
                        double alpha_j_old = alphas[j];

                        // Вычисляем границы L и H
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

                        // Вычисляем eta для 3D
                        double eta = 2 * Kernel(data[i], data[j]) -
                                     Kernel(data[i], data[i]) -
                                     Kernel(data[j], data[j]);

                        if (eta >= 0)
                            continue;

                        // Обновляем alpha_j
                        alphas[j] = alpha_j_old - (labels[j] * (error_i - error_j)) / eta;
                        alphas[j] = Math.Min(H, Math.Max(L, alphas[j]));

                        if (Math.Abs(alphas[j] - alpha_j_old) < 1e-10)
                            continue;

                        // Обновляем alpha_i
                        alphas[i] = alpha_i_old + labels[i] * labels[j] * (alpha_j_old - alphas[j]);

                        // Обновляем bias
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

            // Вычисляем веса для 3D
            double w0 = 0, w1 = 0, w2 = 0;
            for (int i = 0; i < n; i++)
            {
                if (alphas[i] > 1e-7)
                {
                    w0 += alphas[i] * labels[i] * data[i][0];
                    w1 += alphas[i] * labels[i] * data[i][1];
                    w2 += alphas[i] * labels[i] * data[i][2];

                    // Сохраняем опорные векторы
                    _svm.AddSupportVector(data[i][0], data[i][1], data[i][2], labels[i]);
                }
            }

            _svm.SetWeights(w0, w1, w2, bias);
        }

        /// <summary>
        /// Линейное ядро для 3D: K(v1, v2) = x1*x2 + y1*y2 + z1*z2
        /// </summary>
        private double Kernel(double[] v1, double[] v2)
        {
            return v1[0] * v2[0] + v1[1] * v2[1] + v1[2] * v2[2];
        }

        /// <summary>
        /// Вычисление ошибки для точки i
        /// </summary>
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