using System;

namespace SVMKurs.Models
{
    [Serializable]
    public class TrainingConfiguration
    {
        public double C { get; set; } = 1.0;
        public double Tolerance { get; set; } = 1e-4;
        public string LossFunction { get; set; } = "Hinge";  // Hinge или SquaredHinge
    }
}