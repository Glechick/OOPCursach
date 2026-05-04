
namespace SVMKurs.Models
{
    /// <summary>
    /// Точка для ROC-кривой
    /// </summary>
    public class RocPoint
    {
        public double Fpr
        {
            get; set;
        }
        public double Tpr
        {
            get; set;
        }
        public double Threshold
        {
            get; set;
        }
    }
}
