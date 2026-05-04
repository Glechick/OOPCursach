using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SVMKurs.Algorithms
{
    /// <summary>
    /// Извлечение признаков из изображений фигур
    /// </summary>
    public class ShapeFeatureExtractor
    {
        private const int TARGET_SIZE = 224; // Целевой размер изображения

        /// <summary>
        /// Извлекает признаки из изображения
        /// </summary>
        public double[] ExtractFeatures(byte[] imageBytes)
        {
            using var ms = new MemoryStream(imageBytes);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = ms;
            bitmap.EndInit();

            // Масштабирование до 224x224
            var resizedBitmap = ResizeImage(bitmap, TARGET_SIZE, TARGET_SIZE);

            // Конвертируем в формат для анализа
            var pixels = GetPixelArray(resizedBitmap);

            // Находим контур
            var contour = FindContour(pixels, TARGET_SIZE, TARGET_SIZE);

            if (contour == null || contour.Count < 3)
                return new double[] { 0, 0, 0 };

            double compactness = CalculateCompactness(contour);
            double elongation = CalculateElongation(contour);
            double angularity = CalculateAngularity(contour);

            compactness = NormalizeCompactness(compactness);

            return new double[] { compactness, elongation, angularity };
        }

        /// <summary>
        /// Изменяет размер изображения
        /// </summary>
        private BitmapImage ResizeImage(BitmapImage source, int targetWidth, int targetHeight)
        {
            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen())
            {
                dc.DrawImage(source, new Rect(0, 0, targetWidth, targetHeight));
            }

            var renderTarget = new RenderTargetBitmap(targetWidth, targetHeight, 96, 96, PixelFormats.Pbgra32);
            renderTarget.Render(visual);

            var bitmapImage = new BitmapImage();
            using (var ms = new MemoryStream())
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(renderTarget));
                encoder.Save(ms);

                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = ms;
                bitmapImage.EndInit();
                bitmapImage.Freeze();
            }

            return bitmapImage;
        }

        /// <summary>
        /// Получение массива пикселей (черно-белых)
        /// </summary>
        private bool[,] GetPixelArray(BitmapImage bitmap)
        {
            int width = bitmap.PixelWidth;
            int height = bitmap.PixelHeight;
            var result = new bool[width, height];

            // Получаем пиксели
            int stride = (width * bitmap.Format.BitsPerPixel + 7) / 8;
            byte[] pixels = new byte[height * stride];
            bitmap.CopyPixels(pixels, stride, 0);

            // Используем разницу с белым цветом (255,255,255)
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * stride + x * 4;
                    if (index + 2 >= pixels.Length)
                        continue;

                    int r = pixels[index + 2];
                    int g = pixels[index + 1];
                    int b = pixels[index];

                    // Расстояние до белого цвета (чем больше расстояние, тем вероятнее фигура)
                    int distanceToWhite = Math.Abs(r - 255) + Math.Abs(g - 255) + Math.Abs(b - 255);

                    // Если цвет сильно отличается от белого - это фигура
                    result[x, y] = distanceToWhite > 100;
                }
            }

            // Отладочная информация: количество найденных пикселей фигуры
            int foregroundCount = 0;
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    if (result[x, y])
                        foregroundCount++;

            // Если фигура не найдена, пробуем инвертировать
            if (foregroundCount < 100)
            {
                for (int y = 0; y < height; y++)
                    for (int x = 0; x < width; x++)
                        result[x, y] = !result[x, y];

                foregroundCount = 0;
                for (int y = 0; y < height; y++)
                    for (int x = 0; x < width; x++)
                        if (result[x, y])
                            foregroundCount++;
            }

            // Закрываем разрывы в контуре (морфологическое замыкание)
            if (foregroundCount > 0)
            {
                result = CloseContour(result, width, height);
            }

            return result;
        }

        /// <summary>
        /// Морфологическое замыкание для устранения разрывов в контуре
        /// </summary>
        private bool[,] CloseContour(bool[,] binary, int width, int height)
        {
            var result = new bool[width, height];

            // Копируем исходное изображение
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    result[x, y] = binary[x, y];

            // Заполняем маленькие разрывы (3x3 ядро)
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    if (!binary[x, y])
                    {
                        // Проверяем окружение 3x3
                        int neighbors = 0;
                        for (int dy = -1; dy <= 1; dy++)
                            for (int dx = -1; dx <= 1; dx++)
                                if (binary[x + dx, y + dy])
                                    neighbors++;

                        // Если вокруг много соседей, заполняем точку
                        if (neighbors >= 5)
                            result[x, y] = true;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Поиск контура
        /// </summary>
        private List<Point> FindContour(bool[,] binary, int width, int height)
        {
            // Поиск первой точки
            int startX = -1, startY = -1;
            for (int y = 0; y < height && startX == -1; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (binary[x, y])
                    {
                        startX = x;
                        startY = y;
                        break;
                    }
                }
            }

            if (startX == -1)
                return null;

            var contour = new List<Point>();

            // 8 направлений для движения
            int[] dx = { 1, 1, 0, -1, -1, -1, 0, 1 };
            int[] dy = { 0, 1, 1, 1, 0, -1, -1, -1 };

            // Добавляем диагональные направления для лучшей связности
            int[] dx2 = { 1, 1, 1, 0, -1, -1, -1, 0 };
            int[] dy2 = { -1, 0, 1, 1, 1, 0, -1, -1 };

            int currentX = startX;
            int currentY = startY;
            int direction = 0;
            int maxPoints = width * height;
            int pointsCount = 0;

            do
            {
                contour.Add(new Point(currentX, currentY));
                pointsCount++;

                if (pointsCount > maxPoints)
                {
                    System.Diagnostics.Debug.WriteLine("Превышен лимит точек контура");
                    break;
                }

                bool found = false;

                // Сначала ищем в текущем направлении и соседних
                for (int i = 0; i < 8; i++)
                {
                    int newDir = (direction + i) % 8;
                    int nx = currentX + dx[newDir];
                    int ny = currentY + dy[newDir];

                    if (nx >= 0 && nx < width && ny >= 0 && ny < height && binary[nx, ny])
                    {
                        // Проверяем, не вернулись ли в начало (замкнули контур)
                        if (contour.Count > 10 && Math.Abs(nx - startX) <= 2 && Math.Abs(ny - startY) <= 2)
                        {
                            System.Diagnostics.Debug.WriteLine($"Контур замкнут! Всего точек: {contour.Count}");
                            return SimplifyContour(contour, 50);
                        }

                        currentX = nx;
                        currentY = ny;
                        direction = (newDir + 5) % 8;
                        found = true;
                        break;
                    }
                }

                // Если не нашли в основных направлениях, пробуем диагональные
                if (!found)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        int nx = currentX + dx2[i];
                        int ny = currentY + dy2[i];

                        if (nx >= 0 && nx < width && ny >= 0 && ny < height && binary[nx, ny])
                        {
                            if (contour.Count > 10 && Math.Abs(nx - startX) <= 2 && Math.Abs(ny - startY) <= 2)
                            {
                                System.Diagnostics.Debug.WriteLine($"Контур замкнут! Всего точек: {contour.Count}");
                                return SimplifyContour(contour, 50);
                            }

                            currentX = nx;
                            currentY = ny;
                            direction = (i + 5) % 8;
                            found = true;
                            break;
                        }
                    }
                }

                if (!found)
                {
                    break;
                }

            } while (pointsCount < maxPoints);


            if (contour.Count < 10)
            {
                return null;
            }

            return SimplifyContour(contour, 50);
        }

        private List<Point> SimplifyContour(List<Point> contour, int targetPoints)
        {
            if (contour.Count <= targetPoints)
                return contour;

            var result = new List<Point>();
            int step = contour.Count / targetPoints;

            for (int i = 0; i < contour.Count; i += step)
                result.Add(contour[i]);

            return result;
        }

        private double CalculateCompactness(List<Point> contour)
        {
            double area = CalculateArea(contour);
            double perimeter = CalculatePerimeter(contour);

            if (area < 0.001)
                return 0;
            return (perimeter * perimeter) / area;
        }

        private double CalculateElongation(List<Point> contour)
        {
            double minX = contour.Min(p => p.X);
            double maxX = contour.Max(p => p.X);
            double minY = contour.Min(p => p.Y);
            double maxY = contour.Max(p => p.Y);

            double width = maxX - minX;
            double height = maxY - minY;

            if (width < 0.001 || height < 0.001)
                return 1.0;

            return Math.Min(width, height) / Math.Max(width, height);
        }

        private double CalculateAngularity(List<Point> contour)
        {
            if (contour.Count < 10)
                return 0;

            var simplified = DouglasPeucker(contour, 5.0);
            int vertexCount = simplified.Count;

            double angularity = 1.0 - Math.Min(1.0, (vertexCount - 3) / 20.0);
            return Math.Max(0, Math.Min(1, angularity));
        }

        private double CalculateArea(List<Point> contour)
        {
            double area = 0;
            int n = contour.Count;

            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                area += contour[i].X * contour[j].Y;
                area -= contour[j].X * contour[i].Y;
            }

            return Math.Abs(area) / 2.0;
        }

        private double CalculatePerimeter(List<Point> contour)
        {
            double perimeter = 0;
            int n = contour.Count;

            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                double dx = contour[i].X - contour[j].X;
                double dy = contour[i].Y - contour[j].Y;
                perimeter += Math.Sqrt(dx * dx + dy * dy);
            }

            return perimeter;
        }

        private double NormalizeCompactness(double compactness)
        {
            double normalized = (compactness - 10) / 20;
            return Math.Max(0, Math.Min(1, normalized));
        }

        private List<Point> DouglasPeucker(List<Point> points, double epsilon)
        {
            if (points.Count < 3)
                return points;

            var result = new List<Point>();
            DouglasPeuckerRecursive(points, 0, points.Count - 1, epsilon, result);

            result.Insert(0, points[0]);
            result.Add(points[points.Count - 1]);

            return result;
        }

        private void DouglasPeuckerRecursive(List<Point> points, int start, int end, double epsilon, List<Point> result)
        {
            if (start >= end)
                return;

            double maxDist = 0;
            int index = start;

            for (int i = start + 1; i < end; i++)
            {
                double dist = PointLineDistance(points[i], points[start], points[end]);
                if (dist > maxDist)
                {
                    maxDist = dist;
                    index = i;
                }
            }

            if (maxDist > epsilon)
            {
                DouglasPeuckerRecursive(points, start, index, epsilon, result);
                result.Add(points[index]);
                DouglasPeuckerRecursive(points, index, end, epsilon, result);
            }
        }

        private double PointLineDistance(Point point, Point lineStart, Point lineEnd)
        {
            double dx = lineEnd.X - lineStart.X;
            double dy = lineEnd.Y - lineStart.Y;

            if (Math.Abs(dx) < 0.001 && Math.Abs(dy) < 0.001)
                return Math.Sqrt(Math.Pow(point.X - lineStart.X, 2) + Math.Pow(point.Y - lineStart.Y, 2));

            double t = ((point.X - lineStart.X) * dx + (point.Y - lineStart.Y) * dy) / (dx * dx + dy * dy);
            t = Math.Max(0, Math.Min(1, t));

            double projX = lineStart.X + t * dx;
            double projY = lineStart.Y + t * dy;

            return Math.Sqrt(Math.Pow(point.X - projX, 2) + Math.Pow(point.Y - projY, 2));
        }
    }
}