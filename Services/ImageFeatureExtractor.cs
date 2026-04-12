using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SVMKurs.Services
{
    public class ImageFeatureExtractor
    {
        /// <summary>
        /// Превращает изображение в вектор признаков (2 числа для совместимости с демо)
        /// </summary>
        public (double X, double Y) ExtractFeatures(BitmapSource image)
        {
            // Конвертируем в формат для анализа
            var formattedImage = new FormatConvertedBitmap(image, PixelFormats.Bgr32, null, 0);
            int stride = (formattedImage.PixelWidth * formattedImage.Format.BitsPerPixel + 7) / 8;
            byte[] pixels = new byte[formattedImage.PixelHeight * stride];
            formattedImage.CopyPixels(pixels, stride, 0);

            double sumR = 0, sumG = 0, sumB = 0;
            int totalPixels = 0;

            // Анализируем пиксели
            for (int i = 0; i < pixels.Length; i += 4)
            {
                byte b = pixels[i];
                byte g = pixels[i + 1];
                byte r = pixels[i + 2];

                sumR += r;
                sumG += g;
                sumB += b;
                totalPixels++;
            }

            // Признак 1: средняя яркость красного (0-1)
            double avgR = sumR / totalPixels / 255.0;
            // Признак 2: соотношение красного к синему
            double avgB = sumB / totalPixels / 255.0;
            double ratioRB = avgR / (avgB + 0.01); // +0.01 чтобы избежать деления на 0

            // Нормализуем в диапазон 0-10 для совместимости с демо
            double x = avgR * 10;
            double y = Math.Min(ratioRB * 5, 10);

            return (x, y);
        }

        public string GetDescription()
        {
            return "Признаки: средняя яркость красного канала и соотношение R/B";
        }
    }
}