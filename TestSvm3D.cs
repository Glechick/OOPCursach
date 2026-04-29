using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using SVMKurs.Algorithms;

namespace SVMKurs
{
    public static class TestSvm3D
    {
        public static void Run(TextBox outputTextBox)
        {
            try
            {
                outputTextBox.Clear();

                outputTextBox.AppendText("ТЕСТ 3D LINEAR SVM");
                outputTextBox.AppendText(Environment.NewLine);
                outputTextBox.AppendText(Environment.NewLine);

                // ========== 1. ДАННЫЕ ==========
                outputTextBox.AppendText("1. ТЕСТОВЫЕ ДАННЫЕ");
                outputTextBox.AppendText(Environment.NewLine);
                outputTextBox.AppendText("----------------------------------------");
                outputTextBox.AppendText(Environment.NewLine);

                var classNames = new Dictionary<int, string>
                {
                    {0, "Круг"},
                    {1, "Квадрат"},
                    {2, "Треугольник"}
                };

                var samples = new List<MulticlassPoint3D>
                {
                    new MulticlassPoint3D(12.5, 0.95, 0.12, 0),
                    new MulticlassPoint3D(12.8, 0.92, 0.10, 0),
                    new MulticlassPoint3D(12.3, 0.98, 0.11, 0),
                    new MulticlassPoint3D(16.0, 0.85, 0.65, 1),
                    new MulticlassPoint3D(15.8, 0.88, 0.62, 1),
                    new MulticlassPoint3D(16.2, 0.82, 0.68, 1),
                    new MulticlassPoint3D(20.5, 0.70, 0.92, 2),
                    new MulticlassPoint3D(21.0, 0.68, 0.95, 2),
                    new MulticlassPoint3D(19.8, 0.72, 0.89, 2)
                };

                outputTextBox.AppendText($"Круг (класс 0): 3 точки");
                outputTextBox.AppendText(Environment.NewLine);
                outputTextBox.AppendText($"Квадрат (класс 1): 3 точки");
                outputTextBox.AppendText(Environment.NewLine);
                outputTextBox.AppendText($"Треугольник (класс 2): 3 точки");
                outputTextBox.AppendText(Environment.NewLine);
                outputTextBox.AppendText(Environment.NewLine);

                // ========== 2. ОБУЧЕНИЕ ==========
                outputTextBox.AppendText("2. ОБУЧЕНИЕ МОДЕЛИ");
                outputTextBox.AppendText(Environment.NewLine);
                outputTextBox.AppendText("----------------------------------------");
                outputTextBox.AppendText(Environment.NewLine);

                var classifier = new MulticlassSvm3D();
                classifier.Train(samples, classNames, C: 1.0);

                outputTextBox.AppendText($"Статус: Обучена");
                outputTextBox.AppendText(Environment.NewLine);
                outputTextBox.AppendText($"Классов: {classifier.NumberOfClasses}");
                outputTextBox.AppendText(Environment.NewLine);
                outputTextBox.AppendText($"Бинарных классификаторов: 3");
                outputTextBox.AppendText(Environment.NewLine);
                outputTextBox.AppendText(Environment.NewLine);

                // ========== 3. ТЕСТИРОВАНИЕ ==========
                outputTextBox.AppendText("3. ТЕСТИРОВАНИЕ");
                outputTextBox.AppendText(Environment.NewLine);
                outputTextBox.AppendText("----------------------------------------");
                outputTextBox.AppendText(Environment.NewLine);

                var testPoints = new List<(double x, double y, double z, int expected, string name)>
                {
                    (12.6, 0.94, 0.11, 0, "Круг"),
                    (16.1, 0.86, 0.64, 1, "Квадрат"),
                    (20.2, 0.69, 0.93, 2, "Треугольник"),
                    (14.0, 0.90, 0.40, 0, "Круг (пограничный)")
                };

                int correct = 0;
                foreach (var tp in testPoints)
                {
                    var result = classifier.Predict(tp.x, tp.y, tp.z);
                    bool isCorrect = tp.expected == result.PredictedClass;
                    if (isCorrect)
                        correct++;

                    outputTextBox.AppendText($"Точка: X={tp.x:F2} Y={tp.y:F2} Z={tp.z:F2}");
                    outputTextBox.AppendText(Environment.NewLine);
                    outputTextBox.AppendText($"  Ожидается: {tp.name}");
                    outputTextBox.AppendText(Environment.NewLine);
                    outputTextBox.AppendText($"  Предсказано: {result.PredictedClassName} (уверенность: {result.Confidence:P1})");
                    outputTextBox.AppendText(Environment.NewLine);
                    outputTextBox.AppendText($"  Результат: {(isCorrect ? "ВЕРНО" : "ОШИБКА")}");
                    outputTextBox.AppendText(Environment.NewLine);
                    outputTextBox.AppendText(Environment.NewLine);
                }

                // ========== 4. МЕТРИКИ ==========
                outputTextBox.AppendText("4. МЕТРИКИ");
                outputTextBox.AppendText(Environment.NewLine);
                outputTextBox.AppendText("----------------------------------------");
                outputTextBox.AppendText(Environment.NewLine);

                double accuracy = classifier.Evaluate(samples);
                outputTextBox.AppendText($"Точность на обучающей выборке: {accuracy:P1}");
                outputTextBox.AppendText(Environment.NewLine);

                double testAccuracy = (double)correct / testPoints.Count;
                outputTextBox.AppendText($"Точность на тестовой выборке: {testAccuracy:P1}");
                outputTextBox.AppendText(Environment.NewLine);
                outputTextBox.AppendText(Environment.NewLine);

                // ========== 5. МАТРИЦА ОШИБОК ==========
                outputTextBox.AppendText("5. МАТРИЦА ОШИБОК");
                outputTextBox.AppendText(Environment.NewLine);
                outputTextBox.AppendText("----------------------------------------");
                outputTextBox.AppendText(Environment.NewLine);

                var matrix = classifier.GetConfusionMatrix(samples);

                outputTextBox.AppendText("         Круг  Квадрат  Треугольник");
                outputTextBox.AppendText(Environment.NewLine);

                string[] classNamesList = { "Круг", "Квадрат", "Треугольник" };
                for (int i = 0; i < 3; i++)
                {
                    outputTextBox.AppendText($"{classNamesList[i],-9} ");
                    for (int j = 0; j < 3; j++)
                    {
                        outputTextBox.AppendText($"{matrix[i, j],-8} ");
                    }
                    outputTextBox.AppendText(Environment.NewLine);
                }
                outputTextBox.AppendText(Environment.NewLine);

                // ========== 6. ИТОГ ==========
                outputTextBox.AppendText("6. ИТОГ");
                outputTextBox.AppendText(Environment.NewLine);
                outputTextBox.AppendText("----------------------------------------");
                outputTextBox.AppendText(Environment.NewLine);

                if (accuracy >= 0.9)
                    outputTextBox.AppendText("Результат: ОТЛИЧНО (точность > 90%)");
                else if (accuracy >= 0.7)
                    outputTextBox.AppendText("Результат: ХОРОШО (точность > 70%)");
                else
                    outputTextBox.AppendText("Результат: ТРЕБУЕТ УЛУЧШЕНИЯ");

                outputTextBox.AppendText(Environment.NewLine);
                outputTextBox.AppendText(Environment.NewLine);
                outputTextBox.AppendText("Тест завершён.");

                outputTextBox.ScrollToEnd();
            }
            catch (Exception ex)
            {
                outputTextBox.AppendText($"ОШИБКА: {ex.Message}");
                outputTextBox.AppendText(Environment.NewLine);
                outputTextBox.AppendText(ex.StackTrace);
            }
        }
    }
}