namespace SVMKurs.Algorithms
{
    /// <summary>
    /// Линейный бинарный SVM-классификатор.
    /// Использует линейное ядро K(x, x_i) = x · x_i.
    /// Хранит опорные векторы, их альфы и смещение b.
    /// </summary>
    public class LinearSvm3D
    {
        /// <summary>
        /// Опорные векторы (точки, для которых α_i > 0).
        /// </summary>
        public List<SupportVector3D> SupportVectors { get; private set; } = new();

        /// <summary>
        /// Смещение (bias).
        /// </summary>
        public double Bias
        {
            get; private set;
        }

        /// <summary>
        /// Признак того, что модель обучена.
        /// </summary>
        public bool IsTrained => SupportVectors.Count > 0;

        /// <summary>
        /// Весовой вектор w = Σ α_i y_i x_i.
        /// </summary>
        public (double wx, double wy, double wz) WeightVector
        {
            get; private set;
        }

        public LinearSvm3D()
        {
        }

        /// <summary>
        /// Устанавливает модель после обучения SMO.
        /// </summary>
        public void SetModel(List<SupportVector3D> sv, double bias)
        {
            SupportVectors = sv ?? throw new ArgumentNullException(nameof(sv));
            Bias = bias;

            // Вычисляем весовой вектор
            double wx = 0, wy = 0, wz = 0;

            foreach (var s in SupportVectors)
            {
                wx += s.Alpha * s.Label * s.X;
                wy += s.Alpha * s.Label * s.Y;
                wz += s.Alpha * s.Label * s.Z;
            }

            WeightVector = (wx, wy, wz);
        }

        /// <summary>
        /// Линейное ядро: скалярное произведение двух точек.
        /// </summary>
        private static double Kernel(double[] a, double[] b)
        {
            return a[0] * b[0] + a[1] * b[1] + a[2] * b[2];
        }

        /// <summary>
        /// Вычисляет значение решающей функции f(x).
        /// </summary>
        public double Decision(double x, double y, double z)
        {
            if (!IsTrained)
                throw new InvalidOperationException("Модель SVM не обучена.");

            double sum = 0;
            double[] point = { x, y, z };

            foreach (var sv in SupportVectors)
            {
                double[] svPoint = sv.ToArray();
                sum += sv.Alpha * sv.Label * Kernel(point, svPoint);
            }

            return sum + Bias;
        }

        /// <summary>
        /// Предсказывает метку класса (-1 или +1).
        /// </summary>
        public int Predict(double x, double y, double z)
        {
            return Decision(x, y, z) >= 0 ? 1 : -1;
        }

        /// <summary>
        /// Возвращает уверенность классификации (модуль decision value).
        /// </summary>
        public double Confidence(double x, double y, double z)
        {
            return Math.Abs(Decision(x, y, z));
        }
    }
}
