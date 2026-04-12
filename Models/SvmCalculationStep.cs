using System.Collections.Generic;

namespace SVMKurs.Models
{
    /// <summary>
    /// Запись шага вычислений для демонстрации
    /// </summary>
    public class SvmCalculationStep
    {
        public int StepNumber
        {
            get; set;
        }
        public string Description
        {
            get; set;
        }
        public string Formula
        {
            get; set;
        }
        public double[] Values
        {
            get; set;
        }
        public double Result
        {
            get; set;
        }
        public string Explanation
        {
            get; set;
        }
    }
}