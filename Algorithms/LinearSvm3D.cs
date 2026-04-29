using System;
using System.Collections.Generic;

namespace SVMKurs.Algorithms
{
    /// <summary>
    /// Ваш собственный Linear SVM для 3D пространства
    /// Решающая функция: f(x,y,z) = w0*x + w1*y + w2*z + b
    /// </summary>
    public class LinearSvm3D
    {
        public double W0
        {
            get; private set;
        }  // вес для X (компактность)
        public double W1
        {
            get; private set;
        }  // вес для Y (вытянутость)
        public double W2
        {
            get; private set;
        }  // вес для Z (угловатость)
        public double Bias
        {
            get; private set;
        } // смещение

        public bool IsTrained
        {
            get; private set;
        }
        public List<SupportVector3D> SupportVectors
        {
            get; private set;
        }

        public LinearSvm3D()
        {
            SupportVectors = new List<SupportVector3D>();
            IsTrained = false;
        }

        /// <summary>
        /// Решающая функция: f(x,y,z) = w0*x + w1*y + w2*z + b
        /// </summary>
        public double DecisionFunction(double x, double y, double z)
        {
            if (!IsTrained)
                throw new InvalidOperationException("Модель не обучена");

            return W0 * x + W1 * y + W2 * z + Bias;
        }

        /// <summary>
        /// Классификация точки в 3D пространстве
        /// </summary>
        public ClassificationResult3D Predict(double x, double y, double z)
        {
            double decision = DecisionFunction(x, y, z);
            int predictedClass = decision >= 0 ? 1 : -1;

            // Уверенность через сигмоид (от 0.5 до 1.0)
            double confidence = 1.0 / (1.0 + Math.Exp(-Math.Abs(decision)));

            return new ClassificationResult3D
            {
                PredictedClass = predictedClass,
                DecisionValue = decision,
                Confidence = confidence
            };
        }

        /// <summary>
        /// Установка весов модели (вызывается после обучения)
        /// </summary>
        public void SetWeights(double w0, double w1, double w2, double bias)
        {
            W0 = w0;
            W1 = w1;
            W2 = w2;
            Bias = bias;
            IsTrained = true;
        }

        /// <summary>
        /// Добавление опорного вектора
        /// </summary>
        public void AddSupportVector(double x, double y, double z, int label)
        {
            SupportVectors.Add(new SupportVector3D
            {
                X = x,
                Y = y,
                Z = z,
                Label = label
            });
        }

        /// <summary>
        /// Сброс модели
        /// </summary>
        public void Reset()
        {
            W0 = 0;
            W1 = 0;
            W2 = 0;
            Bias = 0;
            IsTrained = false;
            SupportVectors.Clear();
        }

        /// <summary>
        /// Получение уравнения разделяющей плоскости для визуализации
        /// </summary>
        public string GetEquation()
        {
            if (!IsTrained)
                return "Модель не обучена";
            return $"f(x,y,z) = {W0:F3}·x + {W1:F3}·y + {W2:F3}·z + {Bias:F3} = 0";
        }

        /// <summary>
        /// Расстояние от точки до разделяющей плоскости
        /// </summary>
        public double DistanceToPlane(double x, double y, double z)
        {
            double norm = Math.Sqrt(W0 * W0 + W1 * W1 + W2 * W2);
            if (norm < 1e-10)
                return 0;
            return Math.Abs(DecisionFunction(x, y, z)) / norm;
        }
    }

    /// <summary>
    /// Точка в 3D пространстве для обучения
    /// </summary>
    public class Point3D
    {
        public double X
        {
            get; set;
        }  // компактность
        public double Y
        {
            get; set;
        }  // вытянутость
        public double Z
        {
            get; set;
        }  // угловатость
        public int Label
        {
            get; set;
        } // -1 или 1 для бинарной классификации

        public Point3D(double x, double y, double z, int label)
        {
            X = x;
            Y = y;
            Z = z;
            Label = label;
        }

        public double[] ToArray() => new[] { X, Y, Z };
    }

    /// <summary>
    /// Опорный вектор в 3D
    /// </summary>
    public class SupportVector3D
    {
        public double X
        {
            get; set;
        }
        public double Y
        {
            get; set;
        }
        public double Z
        {
            get; set;
        }
        public int Label
        {
            get; set;
        }
    }

    /// <summary>
    /// Результат классификации
    /// </summary>
    public class ClassificationResult3D
    {
        public int PredictedClass
        {
            get; set;
        }
        public double DecisionValue
        {
            get; set;
        }
        public double Confidence
        {
            get; set;
        }

        public string PredictedClassName => PredictedClass == 1 ? "Класс A" : "Класс B";
    }
}