using System;
using System.Collections.Generic;
using System.Linq;
using SVMKurs.Models;

namespace SVMKurs.Algorithms
{
    /// <summary>
    /// Демонстрационный SVM с подробным выводом вычислений
    /// </summary>
    public class DemonstrationSvm
    {
        // Веса обученной модели
        public double W1
        {
            get; private set;
        }  // Вес для признака 1
        public double W2
        {
            get; private set;
        }  // Вес для признака 2
        public double Bias
        {
            get; private set;
        } // Смещение

        public bool IsTrained
        {
            get; private set;
        }

        // Обучающие данные для демонстрации
        private List<SampleData> _trainingData;

        // Опорные векторы
        public List<SampleData> SupportVectors
        {
            get; private set;
        }

        public DemonstrationSvm()
        {
            SupportVectors = new List<SampleData>();
            InitializeDemoData();
        }

        /// <summary>
        /// Создаем простые демо-данные для наглядности
        /// </summary>
        private void InitializeDemoData()
        {
            _trainingData = new List<SampleData>
            {
                // Класс A (1) - данные с высоким признаком 1
                new SampleData(1, "Образец A1", 8.5, 3.2, 1),
                new SampleData(2, "Образец A2", 7.8, 4.1, 1),
                new SampleData(3, "Образец A3", 9.0, 2.5, 1),
                new SampleData(4, "Образец A4", 8.2, 3.8, 1),
                
                // Класс B (-1) - данные с низким признаком 1
                new SampleData(5, "Образец B1", 2.5, 6.8, -1),
                new SampleData(6, "Образец B2", 3.2, 5.5, -1),
                new SampleData(7, "Образец B3", 1.8, 7.2, -1),
                new SampleData(8, "Образец B4", 2.9, 6.1, -1),
            };
        }

        /// <summary>
        /// Демонстрация процесса обучения с пояснениями
        /// </summary>
        public string TrainWithExplanation()
        {
            if (_trainingData == null || _trainingData.Count == 0)
                return "Нет данных для обучения";

            var explanation = new List<string>();
            explanation.Add("=== ОБУЧЕНИЕ LINEAR SVM ===\n");
            explanation.Add("1. Анализ обучающих данных:");
            explanation.Add($"   - Класс A (+1): {_trainingData.Count(d => d.TrueClass == 1)} образцов");
            explanation.Add($"   - Класс B (-1): {_trainingData.Count(d => d.TrueClass == -1)} образцов");
            explanation.Add($"   - Признаки: X₁ (толщина), X₂ (наклон)\n");

            // Находим центры классов
            var class1Points = _trainingData.Where(d => d.TrueClass == 1).ToList();
            var class2Points = _trainingData.Where(d => d.TrueClass == -1).ToList();

            double center1_x = class1Points.Average(p => p.Feature1);
            double center1_y = class1Points.Average(p => p.Feature2);
            double center2_x = class2Points.Average(p => p.Feature1);
            double center2_y = class2Points.Average(p => p.Feature2);

            explanation.Add("2. Вычисление центров классов:");
            explanation.Add($"   - Центр класса A: ({center1_x:F2}, {center1_y:F2})");
            explanation.Add($"   - Центр класса B: ({center2_x:F2}, {center2_y:F2})\n");

            // Вычисляем вектор между центрами
            double vectorX = center1_x - center2_x;
            double vectorY = center1_y - center2_y;

            explanation.Add("3. Определение разделяющей линии:");
            explanation.Add($"   - Вектор между центрами: ({vectorX:F2}, {vectorY:F2})");

            // Нормализуем веса
            double norm = Math.Sqrt(vectorX * vectorX + vectorY * vectorY);
            W1 = vectorX / norm;
            W2 = vectorY / norm;

            explanation.Add($"   - Нормализованные веса: w₁ = {W1:F3}, w₂ = {W2:F3}");

            // Вычисляем bias как среднюю точку между проекциями центров
            double proj1 = W1 * center1_x + W2 * center1_y;
            double proj2 = W1 * center2_x + W2 * center2_y;
            Bias = -(proj1 + proj2) / 2;

            explanation.Add($"   - Смещение (bias): b = {Bias:F3}");
            explanation.Add($"   - Уравнение: f(x) = {W1:F3}·x₁ + {W2:F3}·x₂ + ({Bias:F3}) = 0\n");

            // Находим опорные векторы (ближайшие к границе)
            explanation.Add("4. Поиск опорных векторов (Support Vectors):");

            foreach (var point in _trainingData)
            {
                double decision = W1 * point.Feature1 + W2 * point.Feature2 + Bias;
                point.VisualRepresentation = $"f({point.Feature1:F1}, {point.Feature2:F1}) = {decision:F3}";

                // Если точка близка к границе (|decision| < 1.0)
                if (Math.Abs(decision) < 1.0)
                {
                    SupportVectors.Add(point);
                    explanation.Add($"   * {point.Name}: f = {decision:F3} (опорный вектор)");
                }
            }

            explanation.Add($"\n   Найдено {SupportVectors.Count} опорных векторов\n");

            IsTrained = true;

            explanation.Add("=== ОБУЧЕНИЕ ЗАВЕРШЕНО ===");
            explanation.Add("Модель готова к классификации!");

            return string.Join(Environment.NewLine, explanation);
        }

        /// <summary>
        /// Классификация с подробным выводом вычислений
        /// </summary>
        public DetailedClassificationResult ClassifyWithDetails(double feature1, double feature2, string sampleName = "Тестовый образец")
        {
            if (!IsTrained)
                throw new InvalidOperationException("Сначала обучите модель!");

            var result = new DetailedClassificationResult
            {
                Weight1 = W1,
                Weight2 = W2,
                Bias = Bias,
                DecisionFunctionFormula = $"f(x) = {W1:F3}·x₁ + {W2:F3}·x₂ + {Bias:F3}"
            };

            // Шаг 1: Умножение признаков на веса
            result.CalculationSteps.Add(new SvmCalculationStep
            {
                StepNumber = 1,
                Description = "Умножение признаков на веса",
                Formula = $"w₁·x₁ = {W1:F3} × {feature1:F3}",
                Values = new[] { W1, feature1 },
                Result = W1 * feature1,
                Explanation = $"Первый признак (толщина) умножается на его вес"
            });

            double step1Result = W1 * feature1;

            result.CalculationSteps.Add(new SvmCalculationStep
            {
                StepNumber = 2,
                Description = "Умножение второго признака на вес",
                Formula = $"w₂·x₂ = {W2:F3} × {feature2:F3}",
                Values = new[] { W2, feature2 },
                Result = W2 * feature2,
                Explanation = $"Второй признак (наклон) умножается на его вес"
            });

            double step2Result = W2 * feature2;

            // Шаг 3: Суммирование
            double sum = step1Result + step2Result;
            result.CalculationSteps.Add(new SvmCalculationStep
            {
                StepNumber = 3,
                Description = "Суммирование взвешенных признаков",
                Formula = $"w₁·x₁ + w₂·x₂ = {step1Result:F3} + {step2Result:F3}",
                Values = new[] { step1Result, step2Result },
                Result = sum,
                Explanation = $"Складываем оба произведения"
            });

            // Шаг 4: Добавление bias
            double decision = sum + Bias;
            result.CalculationSteps.Add(new SvmCalculationStep
            {
                StepNumber = 4,
                Description = "Добавление смещения (bias)",
                Formula = $"(w₁·x₁ + w₂·x₂) + b = {sum:F3} + {Bias:F3}",
                Values = new[] { sum, Bias },
                Result = decision,
                Explanation = $"Прибавляем смещение для получения значения decision function"
            });

            result.DecisionValue = decision;

            // Шаг 5: Принятие решения
            result.PredictedClass = decision >= 0 ? 1 : -1;
            result.PredictedClassName = result.PredictedClass == 1 ? "Класс A" : "Класс B";

            // Вычисляем расстояние до разделяющей границы
            result.DistanceToBoundary = Math.Abs(decision) / Math.Sqrt(W1 * W1 + W2 * W2);

            result.CalculationSteps.Add(new SvmCalculationStep
            {
                StepNumber = 5,
                Description = "Принятие решения",
                Formula = $"sign(f(x)) = sign({decision:F3})",
                Values = new[] { decision },
                Result = result.PredictedClass,
                Explanation = decision >= 0 ?
                    $"f(x) ≥ 0 → Класс A (+1)" :
                    $"f(x) < 0 → Класс B (-1)"
            });

            // Шаг 6: Вычисление уверенности (сигмоид)
            result.Confidence = 1.0 / (1.0 + Math.Exp(-Math.Abs(decision)));

            result.CalculationSteps.Add(new SvmCalculationStep
            {
                StepNumber = 6,
                Description = "Вычисление уверенности",
                Formula = $"confidence = 1/(1 + e^(-|{decision:F3}|))",
                Values = new[] { Math.Abs(decision) },
                Result = result.Confidence,
                Explanation = $"Чем дальше от границы, тем выше уверенность. Расстояние до границы: {result.DistanceToBoundary:F3}"
            });

            // Определяем позицию на графике
            if (decision > 1)
                result.GraphPosition = "Далеко справа от границы (высокая уверенность в классе A)";
            else if (decision > 0)
                result.GraphPosition = "Близко к границе справа (низкая уверенность в классе A)";
            else if (decision > -1)
                result.GraphPosition = "Близко к границе слева (низкая уверенность в классе B)";
            else
                result.GraphPosition = "Далеко слева от границы (высокая уверенность в классе B)";

            return result;
        }

        /// <summary>
        /// Получить обучающие данные для отображения
        /// </summary>
        public List<SampleData> GetTrainingData() => _trainingData;

        /// <summary>
        /// Получить уравнение разделяющей линии
        /// </summary>
        public (double Slope, double Intercept) GetDecisionBoundary()
        {
            if (!IsTrained || Math.Abs(W2) < 1e-10)
                return (0, 0);

            // w1*x + w2*y + b = 0 → y = (-w1/w2)*x + (-b/w2)
            double slope = -W1 / W2;
            double intercept = -Bias / W2;
            return (slope, intercept);
        }
    }
}