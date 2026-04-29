using System;
using System.Collections.Generic;
using SVMKurs.Algorithms;

namespace SVMKurs
{
    public static class TestSvm3D
    {
        public static void Run()
        {
            Console.WriteLine("=== Тест 3D SVM ===\n");

            // 1. Создаём тестовые данные для 3 классов
            var classNames = new Dictionary<int, string>
            {
                {0, "Круг"},
                {1, "Квадрат"},
                {2, "Треугольник"}
            };

            var samples = new List<MulticlassPoint3D>
            {
                // Круг (класс 0) - компактность высокая, вытянутость низкая, угловатость низкая
                new MulticlassPoint3D(12.5, 0.95, 0.12, 0),
                new MulticlassPoint3D(12.8, 0.92, 0.10, 0),
                new MulticlassPoint3D(12.3, 0.98, 0.11, 0),
                
                // Квадрат (класс 1) - компактность средняя, вытянутость средняя, угловатость средняя
                new MulticlassPoint3D(16.0, 0.85, 0.65, 1),
                new MulticlassPoint3D(15.8, 0.88, 0.62, 1),
                new MulticlassPoint3D(16.2, 0.82, 0.68, 1),
                
                // Треугольник (класс 2) - компактность низкая, вытянутость высокая, угловатость высокая
                new MulticlassPoint3D(20.5, 0.70, 0.92, 2),
                new MulticlassPoint3D(21.0, 0.68, 0.95, 2),
                new MulticlassPoint3D(19.8, 0.72, 0.89, 2)
            };

            // 2. Обучаем
            Console.WriteLine("Обучение модели...");
            var classifier = new MulticlassSvm3D();
            classifier.Train(samples, classNames, C: 1.0);

            Console.WriteLine(classifier.GetModelInfo());

            // 3. Тестируем
            Console.WriteLine("\n=== Тестирование ===");

            var testPoints = new List<MulticlassPoint3D>
            {
                new MulticlassPoint3D(12.6, 0.94, 0.11, 0),  // должен быть круг
                new MulticlassPoint3D(16.1, 0.86, 0.64, 1),  // должен быть квадрат
                new MulticlassPoint3D(20.2, 0.69, 0.93, 2),  // должен быть треугольник
                new MulticlassPoint3D(14.0, 0.90, 0.40, 0)   // пограничный - круг?
            };

            foreach (var point in testPoints)
            {
                var result = classifier.Predict(point.X, point.Y, point.Z);
                string isCorrect = point.Label == result.PredictedClass ? "✓" : "✗";
                Console.WriteLine($"Точка ({point.X:F1}, {point.Y:F2}, {point.Z:F2}) → {result} {isCorrect}");
            }

            // 4. Оценка точности
            double accuracy = classifier.Evaluate(samples);
            Console.WriteLine($"\nТочность на обучающей выборке: {accuracy:P1}");

            // 5. Матрица ошибок
            var matrix = classifier.GetConfusionMatrix(samples);
            Console.WriteLine("\nМатрица ошибок (строка = истинный класс, столбец = предсказанный):");
            Console.Write("     ");
            for (int i = 0; i < 3; i++)
                Console.Write($"{classNames[i],-8}");
            Console.WriteLine();
            for (int i = 0; i < 3; i++)
            {
                Console.Write($"{classNames[i],-4} ");
                for (int j = 0; j < 3; j++)
                {
                    Console.Write($"{matrix[i, j],-8}");
                }
                Console.WriteLine();
            }
        }
    }
}