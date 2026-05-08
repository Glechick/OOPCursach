using System;

namespace SVMKurs.Algorithms
{
    /// <summary>
    /// Точка в 3D‑пространстве для обучения бинарного SVM.
    /// </summary>
    public class Point3D
    {
        public double X
        {
            get;
        }
        public double Y
        {
            get;
        }
        public double Z
        {
            get;
        }
        public int Label
        {
            get;
        }

        public Point3D(double x, double y, double z, int label)
        {
            X = x;
            Y = y;
            Z = z;
            Label = label;
        }

        /// <summary>
        /// Возвращает координаты точки в виде массива [x, y, z].
        /// </summary>
        public double[] ToArray() => new[] { X, Y, Z };

        public override string ToString() => $"({X:F3}, {Y:F3}, {Z:F3}) → {Label}";
    }
}
