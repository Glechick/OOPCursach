namespace SVMKurs.Models
{
    [Serializable]
    public class TrainingConfiguration
    {
        public double C { get; set; } = 1.0;
        public string MarginType { get; set; } = "Soft Margin";
        public string LossFunction { get; set; } = "Hinge";
        public double Tolerance { get; set; } = 1e-5;
        public int MaxIterations { get; set; } = 100;

        public override string ToString()
        {
            return $"C={C}, Margin={MarginType}, Loss={LossFunction}";
        }
    }
}