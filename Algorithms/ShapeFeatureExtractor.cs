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
    /// Класс для извлечения признаков из изображений фигур.
    /// Выполняет масштабирование, бинаризацию, морфологическую обработку,
    /// поиск контура и вычисление трёх признаков: компактности, вытянутости и угловатости.
    /// </summary>
    public class ShapeFeatureExtractor
    {
        /// <summary>
        /// Целевой размер изображения после масштабирования.
        /// </summary>
        private const int TARGET_SIZE = 224;

        /// <summary>
        /// Извлекает признаки фигуры из изображения.
        /// </summary>
        /// <param name="imageBytes">Массив байтов изображения.</param>
        /// <returns>
        /// Массив из трёх признаков:
        /// [0] — компактность (0..1),
        /// [1] — вытянутость (0..1),
        /// [2] — угловатость (0..1).
        /// </returns>
        public double[] ExtractFeatures(byte[] imageBytes)
        {
            using var ms = new MemoryStream(imageBytes);
            var bitmap = LoadBitmap(ms);

            var resized = ResizeImage(bitmap, TARGET_SIZE, TARGET_SIZE);

            var binary = AdaptiveBinarize(resized);

            binary = MorphClose(binary);

            var contour = FindContour(binary);

            if (contour == null || contour.Count < 5)
                return new double[] { 0, 0, 0 };

            double compactness = NormalizeCompactness(CalcCompactness(contour));
            double elongation = CalcElongation(contour);
            double angularity = CalcAngularity(contour);

            return new[] { compactness, elongation, angularity };
        }

        /// <summary>
        /// Загружает изображение из потока в объект BitmapImage.
        /// </summary>
        /// <param name="stream">Поток с изображением.</param>
        /// <returns>Загруженное изображение.</returns>
        private BitmapImage LoadBitmap(Stream stream)
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.StreamSource = stream;
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }

        /// <summary>
        /// Масштабирует изображение до заданного размера.
        /// </summary>
        /// <param name="source">Исходное изображение.</param>
        /// <param name="width">Ширина результата.</param>
        /// <param name="height">Высота результата.</param>
        /// <returns>Масштабированное изображение.</returns>
        private BitmapImage ResizeImage(BitmapImage source, int width, int height)
        {
            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen())
                dc.DrawImage(source, new Rect(0, 0, width, height));

            var target = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            target.Render(visual);

            using var ms = new MemoryStream();
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(target));
            encoder.Save(ms);

            return LoadBitmap(ms);
        }

        /// <summary>
        /// Выполняет адаптивную бинаризацию изображения.
        /// Используется локальный порог в окне 15×15.
        /// </summary>
        /// <param name="bmp">Изображение.</param>
        /// <returns>Двумерный массив true/false, где true — пиксель фигуры.</returns>
        private bool[,] AdaptiveBinarize(BitmapImage bmp)
        {
            int w = bmp.PixelWidth;
            int h = bmp.PixelHeight;

            var result = new bool[w, h];

            int stride = w * 4;
            byte[] pixels = new byte[h * stride];
            bmp.CopyPixels(pixels, stride, 0);

            const int window = 15;
            const int half = window / 2;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int sum = 0;
                    int count = 0;

                    for (int dy = -half; dy <= half; dy++)
                    {
                        for (int dx = -half; dx <= half; dx++)
                        {
                            int xx = x + dx;
                            int yy = y + dy;

                            if (xx < 0 || xx >= w || yy < 0 || yy >= h)
                                continue;

                            int idx = yy * stride + xx * 4;
                            int gray = (pixels[idx] + pixels[idx + 1] + pixels[idx + 2]) / 3;

                            sum += gray;
                            count++;
                        }
                    }

                    int threshold = sum / count;

                    int index = y * stride + x * 4;
                    int pixelGray = (pixels[index] + pixels[index + 1] + pixels[index + 2]) / 3;

                    result[x, y] = pixelGray < threshold - 10;
                }
            }

            return result;
        }

        /// <summary>
        /// Выполняет морфологическое замыкание (дилатация → эрозия),
        /// устраняя мелкие разрывы в фигуре.
        /// </summary>
        /// <param name="binary">Бинарное изображение.</param>
        /// <returns>Обработанное изображение.</returns>
        private bool[,] MorphClose(bool[,] binary)
        {
            int w = binary.GetLength(0);
            int h = binary.GetLength(1);

            var dil = new bool[w, h];
            var ero = new bool[w, h];

            for (int y = 1; y < h - 1; y++)
                for (int x = 1; x < w - 1; x++)
                {
                    bool any = false;
                    for (int dy = -1; dy <= 1; dy++)
                        for (int dx = -1; dx <= 1; dx++)
                            if (binary[x + dx, y + dy])
                                any = true;

                    dil[x, y] = any;
                }

            for (int y = 1; y < h - 1; y++)
                for (int x = 1; x < w - 1; x++)
                {
                    bool all = true;
                    for (int dy = -1; dy <= 1; dy++)
                        for (int dx = -1; dx <= 1; dx++)
                            if (!dil[x + dx, y + dy])
                                all = false;

                    ero[x, y] = all;
                }

            return ero;
        }

        /// <summary>
        /// Находит внешний контур фигуры методом обхода границы.
        /// </summary>
        /// <param name="binary">Бинарное изображение.</param>
        /// <returns>Список точек контура или null.</returns>
        private List<Point> FindContour(bool[,] binary)
        {
            int w = binary.GetLength(0);
            int h = binary.GetLength(1);

            int sx = -1;
            int sy = -1;

            for (int y = 0; y < h && sx == -1; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (binary[x, y])
                    {
                        sx = x;
                        sy = y;
                        break;
                    }
                }
            }

            if (sx == -1)
                return null;

            var contour = new List<Point>();

            int x0 = sx;
            int y0 = sy;
            int dir = 0;

            int[] dx = { 1, 1, 0, -1, -1, -1, 0, 1 };
            int[] dy = { 0, 1, 1, 1, 0, -1, -1, -1 };

            int cx = x0;
            int cy = y0;

            for (int steps = 0; steps < w * h; steps++)
            {
                contour.Add(new Point(cx, cy));

                bool found = false;

                for (int i = 0; i < 8; i++)
                {
                    int nd = (dir + i) % 8;
                    int nx = cx + dx[nd];
                    int ny = cy + dy[nd];

                    if (nx >= 0 && nx < w && ny >= 0 && ny < h && binary[nx, ny])
                    {
                        cx = nx;
                        cy = ny;
                        dir = (nd + 6) % 8;
                        found = true;
                        break;
                    }
                }

                if (!found)
                    break;

                if (contour.Count > 20 && Math.Abs(cx - x0) < 2 && Math.Abs(cy - y0) < 2)
                    break;
            }

            return Simplify(contour, 60);
        }


        /// <summary>
        /// Упрощает контур, равномерно уменьшая количество точек.
        /// </summary>
        /// <param name="contour">Исходный контур.</param>
        /// <param name="targetCount">Желаемое количество точек.</param>
        /// <returns>Упрощённый контур.</returns>
        private List<Point> Simplify(List<Point> contour, int targetCount)
        {
            if (contour.Count <= targetCount)
                return contour;

            int step = contour.Count / targetCount;
            return contour.Where((p, i) => i % step == 0).ToList();
        }

        /// <summary>
        /// Вычисляет компактность фигуры.
        /// </summary>
        /// <param name="contour">Контур фигуры.</param>
        /// <returns>Значение компактности.</returns>
        private double CalcCompactness(List<Point> contour)
        {
            double area = CalcArea(contour);
            double per = CalcPerimeter(contour);
            if (area < 1)
                return 0;
            return (per * per) / area;
        }

        /// <summary>
        /// Вычисляет вытянутость фигуры.
        /// </summary>
        /// <param name="contour">Контур фигуры.</param>
        /// <returns>Значение вытянутости.</returns>
        private double CalcElongation(List<Point> contour)
        {
            double minX = contour.Min(p => p.X);
            double maxX = contour.Max(p => p.X);
            double minY = contour.Min(p => p.Y);
            double maxY = contour.Max(p => p.Y);

            double w = maxX - minX;
            double h = maxY - minY;

            if (w < 1 || h < 1)
                return 1;

            return Math.Min(w, h) / Math.Max(w, h);
        }

        /// <summary>
        /// Вычисляет угловатость фигуры (0 - гладкая, 1 - очень угловатая)
        /// </summary>
        private double CalcAngularity(List<Point> contour)
        {
            if (contour.Count < 5)
                return 0;

            var pts = Simplify(contour, 60);
            int n = pts.Count;

            // Считаем количество "острых" углов (меньше 90 градусов)
            int sharpCorners = 0;
            double totalAngleDeviation = 0;

            for (int i = 0; i < n; i++)
            {
                Point p0 = pts[(i - 1 + n) % n];
                Point p1 = pts[i];
                Point p2 = pts[(i + 1) % n];

                // Векторы
                double v1x = p0.X - p1.X;
                double v1y = p0.Y - p1.Y;
                double v2x = p2.X - p1.X;
                double v2y = p2.Y - p1.Y;

                double len1 = Math.Sqrt(v1x * v1x + v1y * v1y);
                double len2 = Math.Sqrt(v2x * v2x + v2y * v2y);
                if (len1 < 1 || len2 < 1)
                    continue;

                // Косинус угла
                double cos = (v1x * v2x + v1y * v2y) / (len1 * len2);
                cos = Math.Max(-1, Math.Min(1, cos));
                double angle = Math.Acos(cos) * 180 / Math.PI; // в градусах

                // Угол меньше 120 градусов считается "угловатым"
                if (angle < 120)
                {
                    sharpCorners++;
                    // Чем острее угол, тем больше вклад
                    totalAngleDeviation += (120 - angle) / 120;
                }
            }

            if (sharpCorners == 0)
                return 0;

            // Нормируем результат
            double angularity = Math.Min(1, totalAngleDeviation / (sharpCorners * 0.5));

            return angularity;
        }


        /// <summary>
        /// Вычисляет площадь фигуры методом "shoelace".
        /// </summary>
        /// <param name="contour">Контур фигуры.</param>
        /// <returns>Площадь фигуры.</returns>
        private double CalcArea(List<Point> contour)
        {
            double area = 0;
            for (int i = 0; i < contour.Count; i++)
            {
                int j = (i + 1) % contour.Count;
                area += contour[i].X * contour[j].Y - contour[j].X * contour[i].Y;
            }
            return Math.Abs(area) / 2.0;
        }

        /// <summary>
        /// Вычисляет периметр фигуры.
        /// </summary>
        /// <param name="contour">Контур фигуры.</param>
        /// <returns>Периметр фигуры.</returns>
        private double CalcPerimeter(List<Point> contour)
        {
            double p = 0;
            for (int i = 0; i < contour.Count; i++)
            {
                int j = (i + 1) % contour.Count;
                double dx = contour[i].X - contour[j].X;
                double dy = contour[i].Y - contour[j].Y;
                p += Math.Sqrt(dx * dx + dy * dy);
            }
            return p;
        }

        /// <summary>
        /// Нормализует компактность в диапазон 0..1.
        /// </summary>
        /// <param name="compactness">Исходная компактность.</param>
        /// <returns>Нормализованное значение.</returns>
        private double NormalizeCompactness(double compactness)
        {
            return Math.Max(0, Math.Min(1, (compactness - 10) / 25));
        }
    }
}
