using System.Collections.Generic;

namespace SVMKurs.Models
{
    /// <summary>
    /// Метрики качества классификации
    /// </summary>
    public class Metrics
    {
        public double Accuracy
        {
            get; set;
        }
        public double Precision
        {
            get; set;
        }
        public double Recall
        {
            get; set;
        }
        public double F1Score
        {
            get; set;
        }
        public double[] ClassPrecision
        {
            get; set;
        }
        public double[] ClassRecall
        {
            get; set;
        }
        public double[] ClassF1
        {
            get; set;
        }
        public int[][] ConfusionMatrix
        {
            get; set;
        }
        public List<RocPoint> RocCurve
        {
            get; set;
        }
        public double RocAuc
        {
            get; set;
        }
        public double TrainingTimeMs
        {
            get; set;
        }
        public double PredictionTimeMs
        {
            get; set;
        }
        public int Epoch
        {
            get; set;
        }
    }
}