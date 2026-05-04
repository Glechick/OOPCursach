using SVMKurs.Algorithms;
using SVMKurs.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SVMKurs.ViewModels
{
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

        public MainViewModel()
        {
            DataManagementViewModel = new DataManagementViewModel();
            MySvmViewModel = new MySvmViewModel();
            AccordSvmViewModel = new AccordSvmViewModel();
            CompareViewModel = new CompareViewModel();

            DataManagementViewModel.DataChanged += OnDataChanged;
            MySvmViewModel.TrainingCompleted += OnTrainingCompleted;
            AccordSvmViewModel.TrainingCompleted += OnTrainingCompleted;

            OnDataChanged();

            GlobalStatus = "Готов к работе";
        }

        private void OnDataChanged()
        {
            var allPoints = new System.Collections.Generic.List<MulticlassPoint3D>();
            var classNames = new System.Collections.Generic.Dictionary<int, string>();

            int classId = 0;
            foreach (var shapeClass in DataManagementViewModel.ShapeClasses)
            {
                classNames[classId] = shapeClass.Name;
                foreach (var image in shapeClass.Images)
                {
                    allPoints.Add(new MulticlassPoint3D(
                        image.Feature1,
                        image.Feature2,
                        image.Feature3,
                        classId));
                }
                classId++;
            }

            MySvmViewModel.SetData(allPoints, classNames);
            AccordSvmViewModel.SetData(allPoints, classNames);
            CompareViewModel.SetModels(MySvmViewModel.GetModel(), AccordSvmViewModel.GetAccordModel());

            GlobalStatus = $"Данные обновлены: {allPoints.Count} изображений, {classNames.Count} классов";
        }

        private void OnTrainingCompleted()
        {
            CompareViewModel.SetModels(MySvmViewModel.GetModel(), AccordSvmViewModel.GetAccordModel());
            GlobalStatus = "Модели обновлены после обучения";
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}