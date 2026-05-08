using SVMKurs.Algorithms;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SVMKurs.ViewModels
{
    /// <summary>
    /// Главная ViewModel приложения.
    /// Управляет загрузкой данных, передачей их в модели,
    /// обработкой событий обучения и обновлением глобального статуса.
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        private string _globalStatus;

        public DataManagementViewModel DataManagementViewModel
        {
            get; set;
        }
        public MySvmViewModel MySvmViewModel
        {
            get; set;
        }
        public AccordSvmViewModel AccordSvmViewModel
        {
            get; set;
        }
        public CompareViewModel CompareViewModel
        {
            get; set;
        }

        public string GlobalStatus
        {
            get => _globalStatus;
            set
            {
                _globalStatus = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Конструктор. Инициализирует все ViewModel и подписывается на события.
        /// </summary>
        public MainViewModel()
        {
            DataManagementViewModel = new DataManagementViewModel();
            MySvmViewModel = new MySvmViewModel();
            AccordSvmViewModel = new AccordSvmViewModel();
            CompareViewModel = new CompareViewModel();

            DataManagementViewModel.DataChanged += OnDataChanged;

            // подписываемся на завершение обучения
            MySvmViewModel.TrainingCompleted += OnTrainingCompleted;
            AccordSvmViewModel.TrainingCompleted += OnTrainingCompleted;

            OnDataChanged();
            GlobalStatus = "Готов к работе";
        }

        /// <summary>
        /// Обрабатывает обновление данных: формирует список точек и имён классов,
        /// передаёт их в модели.
        /// </summary>
        private void OnDataChanged()
        {
            var allPoints = new List<Point3D>();
            var classNames = new Dictionary<int, string>();
            CompareViewModel.SetClassNames(classNames);

            CompareViewModel.SetModels(
                DataManagementViewModel.GetMyModel(),
                null // Accord не трогать
            );

            int classId = 0;
            foreach (var shapeClass in DataManagementViewModel.ShapeClasses)
            {
                classNames[classId] = shapeClass.Name;

                foreach (var image in shapeClass.Images)
                {
                    allPoints.Add(new Point3D(
                        image.Feature1,
                        image.Feature2,
                        image.Feature3,
                        classId));
                }

                classId++;
            }

            // Передаём данные в модели
            MySvmViewModel.SetData(allPoints, classNames);
            AccordSvmViewModel.SetData(allPoints);

            GlobalStatus = $"Данные обновлены: {allPoints.Count} изображений, {classNames.Count} классов";
        }

        /// <summary>
        /// Обрабатывает завершение обучения одной из моделей.
        /// Передаёт обученные модели в CompareViewModel и DataManagementViewModel.
        /// </summary>
        private void OnTrainingCompleted()
        {
            // Передаём обученную пользовательскую модель в DataManagementViewModel
            DataManagementViewModel.SetMyModel(MySvmViewModel.GetModel());

            // Передаём обе модели в CompareViewModel
            CompareViewModel.SetModels(
                MySvmViewModel.GetModel(),
                AccordSvmViewModel.GetAccordModel());

            GlobalStatus = "Модели обновлены после обучения";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
