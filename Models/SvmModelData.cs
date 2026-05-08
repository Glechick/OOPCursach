using System.Collections.Generic;

namespace SVMKurs.Services
{
    /// <summary>
    /// Структура для сериализации многоклассовой SVM-модели.
    /// </summary>
    public class SvmModelData
    {
        public List<int> Classes
        {
            get; set;
        }
        public List<SvmBinaryModelData> Models
        {
            get; set;
        }
    }

    /// <summary>
    /// Данные одной бинарной модели SVM.
    /// </summary>
    public class SvmBinaryModelData
    {
        public int ClassA
        {
            get; set;
        }
        public int ClassB
        {
            get; set;
        }
        public double Bias
        {
            get; set;
        }
        public List<SupportVectorData> SupportVectors
        {
            get; set;
        }
    }

    /// <summary>
    /// Данные одного опорного вектора.
    /// </summary>
    public class SupportVectorData
    {
        public double X
        {
            get; set;
        }
        public double Y
        {
            get; set;
        }
        public double Z
        {
            get; set;
        }
        public int Label
        {
            get; set;
        }
        public double Alpha
        {
            get; set;
        }
    }
}
