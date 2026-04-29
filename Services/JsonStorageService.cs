using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using SVMKurs.Algorithms;

namespace SVMKurs.Services
{
    /// <summary>
    /// Сервис для сохранения и загрузки признаков изображений в JSON
    /// </summary>
    public class JsonStorageService
    {
        private readonly string _filePath;

        public JsonStorageService(string fileName = "shapes_data.json")
        {
            _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
        }

        /// <summary>
        /// Сохранить данные в JSON
        /// </summary>
        public void SaveData(Dictionary<string, List<StoredImage>> data)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(data, options);
            File.WriteAllText(_filePath, json);
        }

        /// <summary>
        /// Загрузить данные из JSON
        /// </summary>
        public Dictionary<string, List<StoredImage>> LoadData()
        {
            if (!File.Exists(_filePath))
                return new Dictionary<string, List<StoredImage>>();

            string json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<Dictionary<string, List<StoredImage>>>(json)
                   ?? new Dictionary<string, List<StoredImage>>();
        }

        /// <summary>
        /// Проверить, существует ли файл
        /// </summary>
        public bool DataExists() => File.Exists(_filePath);

        /// <summary>
        /// Получить путь к файлу
        /// </summary>
        public string GetFilePath() => _filePath;
    }

    /// <summary>
    /// Сохраняемое изображение
    /// </summary>
    public class StoredImage
    {
        public string FileName
        {
            get; set;
        }
        public string FileHash
        {
            get; set;
        }
        public double Feature1
        {
            get; set;
        }  // компактность
        public double Feature2
        {
            get; set;
        }  // вытянутость
        public double Feature3
        {
            get; set;
        }  // угловатость
        public DateTime ProcessedAt
        {
            get; set;
        }

        public double[] Features => new[] { Feature1, Feature2, Feature3 };
    }
}