using SVMKurs.Algorithms;
using SVMKurs.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace SvmDemo.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly DemonstrationSvm _svm;

        private ObservableCollection<SampleData> _trainingSamples;
        private SampleData _selectedSample;
        private DetailedClassificationResult _classificationResult;
        private string _trainingLog;
        private string _currentFormula;
        private double _testFeature1;
        private double _testFeature2;
        private bool _isTrained;

        public ObservableCollection<SampleData> TrainingSamples
        {
            get => _trainingSamples;
            set
            {
                _trainingSamples = value;
                OnPropertyChanged();
            }
        }

        public SampleData SelectedSample
        {
            get => _selectedSample;
            set
            {
                _selectedSample = value;
                OnPropertyChanged();
                if (value != null)
                    ClassifySample(value);
            }
        }

        public DetailedClassificationResult ClassificationResult
        {
            get => _classificationResult;
            set
            {
                _classificationResult = value;
                OnPropertyChanged();
            }
        }

        public string TrainingLog
        {
            get => _trainingLog;
            set
            {
                _trainingLog = value;
                OnPropertyChanged();
            }
        }

        public string CurrentFormula
        {
            get => _currentFormula;
            set
            {
                _currentFormula = value;
                OnPropertyChanged();
            }
        }

        public double TestFeature1
        {
            get => _testFeature1;
            set
            {
                _testFeature1 = value;
                OnPropertyChanged();
                if (_isTrained)
                    ClassifyTestPoint();
            }
        }

        public double TestFeature2
        {
            get => _testFeature2;
            set
            {
                _testFeature2 = value;
                OnPropertyChanged();
                if (_isTrained)
                    ClassifyTestPoint();
            }
        }

        public bool IsTrained
        {
            get => _isTrained;
            set
            {
                _isTrained = value;
                OnPropertyChanged();
            }
        }

        // Команды
        public ICommand TrainCommand
        {
            get;
        }
        public ICommand ResetCommand
        {
            get;
        }
        public ICommand ClassifyCustomCommand
        {
            get;
        }

        public event Action RequestRedraw;

        public MainViewModel()
        {
            _svm = new DemonstrationSvm();

            TrainingSamples = new ObservableCollection<SampleData>();
            _testFeature1 = 5;
            _testFeature2 = 5;

            TrainCommand = new RelayCommand(_ => Train());
            ResetCommand = new RelayCommand(_ => Reset());
            ClassifyCustomCommand = new RelayCommand(_ => ClassifyTestPoint());

            LoadTrainingData();
        }

        private void LoadTrainingData()
        {
            var data = _svm.GetTrainingData();
            TrainingSamples.Clear();
            foreach (var sample in data)
            {
                TrainingSamples.Add(sample);
            }
        }

        private void Train()
        {
            TrainingLog = _svm.TrainWithExplanation();
            IsTrained = true;

            var boundary = _svm.GetDecisionBoundary();
            CurrentFormula = $"Разделяющая линия: y = {boundary.Slope:F3}·x + {boundary.Intercept:F3}";

            // Обновляем визуализацию
            RequestRedraw?.Invoke();

            // Классифицируем текущую тестовую точку
            ClassifyTestPoint();
        }

        private void ClassifySample(SampleData sample)
        {
            if (!IsTrained)
                return;

            ClassificationResult = _svm.ClassifyWithDetails(sample.Feature1, sample.Feature2, sample.Name);
        }

        private void ClassifyTestPoint()
        {
            if (!IsTrained)
                return;

            ClassificationResult = _svm.ClassifyWithDetails(TestFeature1, TestFeature2, "Тестовая точка");
            RequestRedraw?.Invoke();
        }

        private void Reset()
        {
            _svm = new DemonstrationSvm();
            IsTrained = false;
            ClassificationResult = null;
            TrainingLog = null;
            CurrentFormula = null;
            LoadTrainingData();
            RequestRedraw?.Invoke();
        }

        public (double Slope, double Intercept) GetDecisionBoundary()
        {
            return _svm.GetDecisionBoundary();
        }

        public List<SampleData> GetSupportVectors()
        {
            return _svm.SupportVectors;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}