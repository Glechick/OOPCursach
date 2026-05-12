namespace SVMKurs.Algorithms
{
    /// <summary>
    /// Обучает линейный SVM методом SMO (Sequential Minimal Optimization).
    /// </summary>
    public class SvmTrainer3D
    {
        private readonly double C;
        private readonly double Tolerance;
        private readonly string LossFunction;


        /// <summary>
        /// Конструктор трейнера SVM.
        /// </summary>
        /// <param name="c">Параметр регуляризации.</param>
        /// <param name="tolerance">Точность сходимости.</param>
        /// <param name="lossFunction">Функция потерь: Hinge или SquaredHinge.</param>
        public SvmTrainer3D(double c = 1.0, double tolerance = 1e-3, string lossFunction = "Hinge")
        {
            C = c;
            Tolerance = tolerance;
            LossFunction = lossFunction;
        }

        /// <summary>
        /// Обучает линейный SVM и возвращает модель.
        /// </summary>
        /// <param name="points">Список точек с метками -1 или +1.</param>
        /// <returns>Обученная модель LinearSvm3D.</returns>
        public LinearSvm3D Train(List<Point3D> points)
        {
            if (points == null || points.Count == 0)
                throw new ArgumentException("Набор данных пуст.");

            int n = points.Count;
            double[] alpha = new double[n];
            double b = 0;
            double[] errors = new double[n];

            // Предварительно вычисляем скалярные произведения
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

            // Инициализируем ошибки
            for (int i = 0; i < n; i++)
                errors[i] = -points[i].Label;

            bool changed;
            int passes = 0;
            int maxPasses = 500;

            do
            {
                changed = false;

                for (int i = 0; i < n; i++)
                {
                    double Ei = ComputeError(i, alpha, b, points, kernel);
                    errors[i] = Ei;

                    bool violatesKKT = (points[i].Label * Ei < -Tolerance && alpha[i] < C) ||
                                      (points[i].Label * Ei > Tolerance && alpha[i] > 0);

                    if (!violatesKKT)
                        continue;

                    int j = SelectSecondIndex(i, errors, n);
                    double Ej = ComputeError(j, alpha, b, points, kernel);

                    double alpha_i_old = alpha[i];
                    double alpha_j_old = alpha[j];

                    int yi = points[i].Label;
                    int yj = points[j].Label;

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

                    if (Math.Abs(L - H) < 1e-10)
                        continue;

                    double eta = 2 * kernel[i, j] - kernel[i, i] - kernel[j, j];
                    if (eta >= 0)
                        continue;

                    double step;
                    if (LossFunction == "SquaredHinge")
                    {
                        step = yj * (Ei - Ej) / (eta + 1.0 / C);
                    }
                    else
                    {
                        step = yj * (Ei - Ej) / eta;
                    }

                    alpha[j] = alpha_j_old - step;

                    if (alpha[j] > H)
                        alpha[j] = H;
                    else if (alpha[j] < L)
                        alpha[j] = L;

                    if (Math.Abs(alpha[j] - alpha_j_old) < 1e-12)
                        continue;

                    alpha[i] = alpha_i_old + yi * yj * (alpha_j_old - alpha[j]);

                    double b1 = b - Ei - yi * (alpha[i] - alpha_i_old) * kernel[i, i]
                                    - yj * (alpha[j] - alpha_j_old) * kernel[i, j];

                    double b2 = b - Ej - yi * (alpha[i] - alpha_i_old) * kernel[i, j]
                                    - yj * (alpha[j] - alpha_j_old) * kernel[j, j];

                    if (alpha[i] > 0 && alpha[i] < C)
                        b = b1;
                    else if (alpha[j] > 0 && alpha[j] < C)
                        b = b2;
                    else
                        b = (b1 + b2) / 2.0;

                    for (int k = 0; k < n; k++)
                    {
                        errors[k] = ComputeError(k, alpha, b, points, kernel);
                    }

                    changed = true;
                }

                passes = changed ? 0 : passes + 1;

            } while (passes < maxPasses);

            var sv = new List<SupportVector3D>();
            for (int i = 0; i < n; i++)
            {
                if (alpha[i] > 1e-6)
                {
                    var p = points[i];
                    sv.Add(new SupportVector3D(p.X, p.Y, p.Z, p.Label, alpha[i]));
                }
            }

            var model = new LinearSvm3D();
            model.SetModel(sv, b);
            return model;
        }

        private double ComputeError(int idx, double[] alpha, double b, List<Point3D> points, double[,] kernel)
        {
            double sum = 0;
            for (int k = 0; k < points.Count; k++)
                sum += alpha[k] * points[k].Label * kernel[k, idx];
            return sum + b - points[idx].Label;
        }

        private int SelectSecondIndex(int i, double[] errors, int total)
        {
            double maxDiff = -1;
            int j = -1;

            for (int k = 0; k < total; k++)
            {
                if (k == i)
                    continue;
                double diff = Math.Abs(errors[i] - errors[k]);
                if (diff > maxDiff)
                {
                    maxDiff = diff;
                    j = k;
                }
            }

            if (j == -1)
            {
                Random rnd = new Random();
                do
                {
                    j = rnd.Next(total);
                } while (j == i);
            }

            return j;
        }
    }
}