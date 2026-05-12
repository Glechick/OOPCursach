using SVMKurs.Algorithms;
using SVMKurs.Models;

namespace SVMKurs.Services
{
    /// <summary>
    /// Универсальный интерфейс для классификаторов SVM.
    /// </summary>
    public interface IClassifierModel
    {
        /// <summary>
        /// Признак обученности модели.
        /// </summary>
        bool IsTrained
        {
            get;
        }

        /// <summary>
        /// Обучает модель на предоставленных данных.
        /// </summary>
        void Train(List<Point3D> data, TrainingConfiguration config);

        /// <summary>
        /// Предсказывает класс для точки.
        /// </summary>
        int Predict(double x, double y, double z);

        /// <summary>
        /// Возвращает вероятности принадлежности к классам.
        /// </summary>
        Dictionary<int, double> PredictProba(double x, double y, double z);

        /// <summary>
        /// Возвращает вероятности в виде массива.
        /// </summary>
        double[] PredictProbaArray(double x, double y, double z);

        /// <summary>
        /// Преобразует модель в сериализуемые данные.
        /// </summary>
        SvmModelData ToData();

        /// <summary>
        /// Восстанавливает модель из сериализованных данных.
        /// </summary>
        void LoadFromData(SvmModelData data);
    }
}