using System;

namespace SVMKurs.Algorithms
{
    /// <summary>
    /// Опорный вектор SVM: координаты точки, её метка и коэффициент альфа.
    /// </summary>
    public class SupportVector3D
    {
        /// <summary>
        /// Координата X.
        /// </summary>
        public double X
        {
            get;
        }

        /// <summary>
        /// Координата Y.
        /// </summary>
        public double Y
        {
            get;
        }

        /// <summary>
        /// Координата Z.
        /// </summary>
        public double Z
        {
            get;
        }

        /// <summary>
        /// Метка класса (-1 или +1).
        /// </summary>
        public int Label
        {
            get;
        }

        /// <summary>
        /// Коэффициент альфа (вес опорного вектора).
        /// </summary>
        public double Alpha
        {
            get;
        }

        public SupportVector3D(double x, double y, double z, int label, double alpha)
        {
            if (label != -1 && label != 1)
                throw new ArgumentException("Метка класса должна быть равна -1 или +1.");

            X = x;
            Y = y;
            Z = z;
            Label = label;
            Alpha = alpha;
        }

        /// <summary>
        /// Возвращает координаты точки в виде массива [x, y, z].
        /// </summary>
        public double[] ToArray() => new[] { X, Y, Z };

        public override string ToString() =>
            $"SV: ({X:F3}, {Y:F3}, {Z:F3}), метка={Label}, α={Alpha:F6}";
    }
}
