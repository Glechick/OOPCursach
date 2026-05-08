using System;
using System.Collections.Generic;
using System.Linq;

namespace SVMKurs.Algorithms
{
    /// <summary>
    /// Обучение линейного SVM методом SMO.
    /// Работает только с метками -1 и +1.
    /// </summary>
    public class SvmTrainer3D
    {
        private readonly double C;          // Параметр регуляризации
        private readonly double Tolerance;  // Допуск для условий KKT
        private readonly double Epsilon;    // Минимальное изменение альфы

        public SvmTrainer3D(double c = 1.0, double tolerance = 1e-3, double epsilon = 1e-3)
        {
            C = c;
            Tolerance = tolerance;
            Epsilon = epsilon;
        }

        /// <summary>
        /// Обучает линейный SVM и возвращает готовую модель.
        /// </summary>
        public LinearSvm3D Train(List<Point3D> points)
        {
            if (points == null || points.Count == 0)
                throw new ArgumentException("Набор данных пуст.");

            int n = points.Count;

            // Альфы
            double[] alpha = new double[n];

            // Смещение
            double b = 0;

            // Ошибки E_i = f(x_i) - y_i
            double[] errors = new double[n];

            // Предварительно вычисляем скалярные произведения (линейное ядро)
            double[,] kernel = new double[n, n];
            for (int i = 0; i < n; i++)
            {
                var a = points[i].ToArray();
                for (int j = 0; j < n; j++)
                {
                    var b2 = points[j].ToArray();
                    kernel[i, j] = a[0] * b2[0] + a[1] * b2[1] + a[2] * b2[2];
                }
            }

            bool changed;
            int passes = 0;
            int maxPasses = 10;

            // Основной цикл SMO
            do
            {
                changed = false;

                for (int i = 0; i < n; i++)
                {
                    double Ei = ComputeError(i);

                    // Проверка условий KKT
                    bool violatesKKT =
                        (points[i].Label * Ei < -Tolerance && alpha[i] < C) ||
                        (points[i].Label * Ei > Tolerance && alpha[i] > 0);

                    if (!violatesKKT)
                        continue;

                    // Выбираем j != i
                    int j = SelectSecondIndex(i, n);
                    double Ej = ComputeError(j);

                    double alpha_i_old = alpha[i];
                    double alpha_j_old = alpha[j];

                    int yi = points[i].Label;
                    int yj = points[j].Label;

                    // Вычисляем границы L и H
                    double L, H;
                    if (yi != yj)
                    {
                        L = Math.Max(0, alpha[j] - alpha[i]);
                        H = Math.Min(C, C + alpha[j] - alpha[i]);
                    }
                    else
                    {
                        L = Math.Max(0, alpha[i] + alpha[j] - C);
                        H = Math.Min(C, alpha[i] + alpha[j]);
                    }

                    if (Math.Abs(L - H) < 1e-12)
                        continue;

                    // Вычисляем η
                    double eta = 2 * kernel[i, j] - kernel[i, i] - kernel[j, j];
                    if (eta >= 0)
                        continue;

                    // Обновляем α_j
                    alpha[j] -= yj * (Ei - Ej) / eta;

                    // Ограничиваем α_j
                    if (alpha[j] > H)
                        alpha[j] = H;
                    else if (alpha[j] < L)
                        alpha[j] = L;

                    if (Math.Abs(alpha[j] - alpha_j_old) < Epsilon)
                        continue;

                    // Обновляем α_i
                    alpha[i] += yi * yj * (alpha_j_old - alpha[j]);

                    // Обновляем bias
                    double b1 = b - Ei
                        - yi * (alpha[i] - alpha_i_old) * kernel[i, i]
                        - yj * (alpha[j] - alpha_j_old) * kernel[i, j];

                    double b2 = b - Ej
                        - yi * (alpha[i] - alpha_i_old) * kernel[i, j]
                        - yj * (alpha[j] - alpha_j_old) * kernel[j, j];

                    if (alpha[i] > 0 && alpha[i] < C)
                        b = b1;
                    else if (alpha[j] > 0 && alpha[j] < C)
                        b = b2;
                    else
                        b = (b1 + b2) / 2.0;

                    changed = true;
                }

                passes = changed ? 0 : passes + 1;

            } while (passes < maxPasses);

            // Формируем список опорных векторов
            var sv = new List<SupportVector3D>();
            for (int i = 0; i < n; i++)
            {
                if (alpha[i] > 1e-6)
                {
                    var p = points[i];
                    sv.Add(new SupportVector3D(p.X, p.Y, p.Z, p.Label, alpha[i]));
                }
            }

            // Создаём модель
            var model = new LinearSvm3D();
            model.SetModel(sv, b);

            return model;

            // Локальные функции

            double ComputeError(int idx)
            {
                double sum = 0;
                for (int k = 0; k < n; k++)
                    sum += alpha[k] * points[k].Label * kernel[k, idx];
                return sum + b - points[idx].Label;
            }

            int SelectSecondIndex(int i, int total)
            {
                Random rnd = new Random();
                int j;
                do
                {
                    j = rnd.Next(total);
                } while (j == i);
                return j;
            }
        }
    }
}
