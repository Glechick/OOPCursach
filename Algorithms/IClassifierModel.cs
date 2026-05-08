namespace SVMKurs.Algorithms
{
    public interface IClassifierModel
    {
        bool IsTrained
        {
            get;
        }

        int Predict(double x, double y, double z);

        Dictionary<int, double> PredictProba(double x, double y, double z);
    }
}
