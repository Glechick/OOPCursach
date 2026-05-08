using System.Collections.Generic;

namespace SVMKurs.Models
{
    /// <summary>
    /// Набор метрик качества классификации.
    /// Поддерживает бинарную и многоклассовую оценку.
    /// </summary>
    public class Metrics
    {
        /// <summary>
        /// Доля правильных предсказаний.
        /// </summary>
        public double Accuracy
        {
            get; set;
        }

        /// <summary>
        /// Средняя точность (Precision) по классам.
        /// </summary>
        public double Precision
        {
            get; set;
        }

        /// <summary>
        /// Средняя полнота (Recall) по классам.
        /// </summary>
        public double Recall
        {
            get; set;
        }

        /// <summary>
        /// Среднее значение F1‑меры по классам.
        /// </summary>
        public double F1Score
        {
            get; set;
        }

        /// <summary>
        /// Точность по каждому классу.
        /// </summary>
        public double[] ClassPrecision
        {
            get; set;
        }

        /// <summary>
        /// Полнота по каждому классу.
        /// </summary>
        public double[] ClassRecall
        {
            get; set;
        }

        /// <summary>
        /// F1‑мера по каждому классу.
        /// </summary>
        public double[] ClassF1
        {
            get; set;
        }

        /// <summary>
        /// Матрица ошибок: [истинный класс][предсказанный класс].
        /// </summary>
        public int[][] ConfusionMatrix
        {
            get; set;
        }

        /// <summary>
        /// ROC‑кривые по каждому классу.
        /// </summary>
        public Dictionary<int, List<(double fpr, double tpr)>> RocCurves
        {
            get; set;
        }

        /// <summary>
        /// Усреднённая ROC‑кривая (macro).
        /// </summary>
        public List<(double fpr, double tpr)> RocMacro
        {
            get; set;
        }

        /// <summary>
        /// Micro‑ROC (по всем классам).
        /// </summary>
        public List<(double fpr, double tpr)> RocMicro
        {
            get; set;
        }

        /// <summary>
        /// Площадь под macro‑ROC.
        /// </summary>
        public double MacroAuc
        {
            get; set;
        }

        /// <summary>
        /// Площадь под micro‑ROC.
        /// </summary>
        public double MicroAuc
        {
            get; set;
        }

        /// <summary>
        /// Время обучения модели в миллисекундах.
        /// </summary>
        public double TrainingTimeMs
        {
            get; set;
        }

        /// <summary>
        /// Время предсказания в миллисекундах.
        /// </summary>
        public double PredictionTimeMs
        {
            get; set;
        }

        /// <summary>
        /// Номер эпохи (если используется обучение по эпохам).
        /// </summary>
        public int Epoch
        {
            get; set;
        }
    }
}
