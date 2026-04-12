using SVMKurs.Models;

/// <summary>
/// Детальный результат классификации с пояснениями
/// </summary>
public class DetailedClassificationResult
{
    public int PredictedClass
    {
        get; set;
    }
    public string PredictedClassName
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

    // Детали вычислений
    public List<SvmCalculationStep> CalculationSteps
    {
        get; set;
    }

    // Визуализация формулы
    public string DecisionFunctionFormula
    {
        get; set;
    }
    public double Weight1
    {
        get; set;
    }
    public double Weight2
    {
        get; set;
    }
    public double Bias
    {
        get; set;
    }

    // Графическое представление
    public string GraphPosition
    {
        get; set;
    }
    public double DistanceToBoundary
    {
        get; set;
    }

    public DetailedClassificationResult()
    {
        CalculationSteps = new List<SvmCalculationStep>();
    }

    public override string ToString()
    {
        return $"{PredictedClassName} (Уверенность: {Confidence:P0}) | f(x) = {DecisionValue:F3}";
    }
}