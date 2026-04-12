namespace SVMKurs.Models
{
    /// <summary>
    /// Простой пример данных для демонстрации работы SVM
    /// </summary>
    public class SampleData
    {
        public int Id
        {
            get; set;
        }
        public string Name
        {
            get; set;
        }
        public double Feature1
        {
            get; set;
        }  // Признак 1 (например, "толщина линии")
        public double Feature2
        {
            get; set;
        }  // Признак 2 (например, "наклон")
        public int TrueClass
        {
            get; set;
        }    // Реальный класс: 1 или -1
        public string ClassName => TrueClass == 1 ? "Класс A" : "Класс B";

        // Визуальное представление данных
        public string VisualRepresentation
        {
            get; set;
        }

        public SampleData(int id, string name, double f1, double f2, int trueClass)
        {
            Id = id;
            Name = name;
            Feature1 = f1;
            Feature2 = f2;
            TrueClass = trueClass;
        }
    }
}