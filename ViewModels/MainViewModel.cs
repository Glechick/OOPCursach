using SVMKurs.Algorithms;
using SVMKurs.Services;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

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

            DataManagementViewModel.MyModelLoaded += OnMyModelLoaded;
            DataManagementViewModel.AccordModelLoaded += OnAccordModelLoaded;

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

            MySvmViewModel.SetData(allPoints, classNames);
            AccordSvmViewModel.SetData(allPoints);

            CompareViewModel.SetClassNames(classNames);
            CompareViewModel.SetModels(
                DataManagementViewModel.GetMyModel(),    
                DataManagementViewModel.GetAccordModel()
            );

            GlobalStatus = $"Данные обновлены: {allPoints.Count} изображений, {classNames.Count} классов";
        }

        /// <summary>
        /// Обрабатывает завершение обучения одной из моделей.
        /// Передаёт обученные модели в CompareViewModel и DataManagementViewModel.
        /// </summary>
        private void OnTrainingCompleted()
        {
            DataManagementViewModel.SetMyModel(MySvmViewModel.GetModel());
            DataManagementViewModel.SetAccordModel(AccordSvmViewModel.GetAccordModel());


            // Передаём обе модели в CompareViewModel
            CompareViewModel.SetModels(
                MySvmViewModel.GetModel(),
                AccordSvmViewModel.GetAccordModel());

            GlobalStatus = "Модели обновлены после обучения";
        }

        private void OnMyModelLoaded(MulticlassSvm3D model)
        {
            MySvmViewModel.SetModel(model);

            MySvmViewModel.Evaluate();

            GlobalStatus = "Загруженная модель синхронизирована и оценена";
        }

        private void OnAccordModelLoaded(AccordSvmWrapper model)
        {
            // Передаём модель в AccordSvmViewModel
            AccordSvmViewModel.SetModel(model);

            // Оцениваем модель на текущих тестовых данных
            AccordSvmViewModel.Evaluate();

            GlobalStatus = "Загруженная Accord модель синхронизирована и оценена";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
