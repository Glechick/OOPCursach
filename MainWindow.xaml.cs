using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using SVMKurs.ViewModels;

namespace SVMKurs
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();

            _viewModel = new MainViewModel();
            DataContext = _viewModel;

            _viewModel.RequestRedraw += DrawPlot;

            Loaded += (s, e) => DrawPlot();
        }

        private void DrawPlot()
        {
            CanvasPlot.Children.Clear();

            DrawGrid();
            DrawTrainingPoints();
            DrawSupportVectors();

            if (_viewModel.IsTrained)
            {
                DrawDecisionBoundary();
                DrawTestPoint();
            }
        }

        private void DrawGrid()
        {
            // Сетка 10x10
            for (int i = 0; i <= 10; i++)
            {
                var vLine = new Line
                {
                    X1 = MapX(i),
                    Y1 = MapY(0),
                    X2 = MapX(i),
                    Y2 = MapY(10),
                    Stroke = Brushes.LightGray,
                    StrokeThickness = 0.5
                };
                CanvasPlot.Children.Add(vLine);

                var hLine = new Line
                {
                    X1 = MapX(0),
                    Y1 = MapY(i),
                    X2 = MapX(10),
                    Y2 = MapY(i),
                    Stroke = Brushes.LightGray,
                    StrokeThickness = 0.5
                };
                CanvasPlot.Children.Add(hLine);
            }

            // Подписи
            for (int i = 0; i <= 10; i += 2)
            {
                var label = new TextBlock
                {
                    Text = i.ToString(),
                    FontSize = 8,
                    Foreground = Brushes.Gray
                };
                Canvas.SetLeft(label, MapX(i) - 5);
                Canvas.SetTop(label, MapY(0) + 2);
                CanvasPlot.Children.Add(label);
            }

            // Подписи осей
            var xLabel = new TextBlock { Text = "X₁ (толщина)", FontSize = 10, Foreground = Brushes.DarkGray };
            Canvas.SetLeft(xLabel, MapX(5) - 30);
            Canvas.SetTop(xLabel, MapY(0) + 20);
            CanvasPlot.Children.Add(xLabel);

            var yLabel = new TextBlock { Text = "X₂ (наклон)", FontSize = 10, Foreground = Brushes.DarkGray };
            Canvas.SetLeft(yLabel, MapX(0) - 35);
            Canvas.SetTop(yLabel, MapY(5) - 10);
            CanvasPlot.Children.Add(yLabel);
        }

        private void DrawTrainingPoints()
        {
            foreach (var point in _viewModel.TrainingSamples)
            {
                var ellipse = new Ellipse
                {
                    Width = 24,
                    Height = 24,
                    Fill = point.TrueClass == 1 ? Brushes.Blue : Brushes.Red,
                    Opacity = 0.8
                };

                Canvas.SetLeft(ellipse, MapX(point.Feature1) - 12);
                Canvas.SetTop(ellipse, MapY(point.Feature2) - 12);
                CanvasPlot.Children.Add(ellipse);

                // Подпись
                var label = new TextBlock
                {
                    Text = point.Name,
                    FontSize = 9,
                    Foreground = Brushes.Black,
                    Background = Brushes.White
                };
                Canvas.SetLeft(label, MapX(point.Feature1) - 15);
                Canvas.SetTop(label, MapY(point.Feature2) - 30);
                CanvasPlot.Children.Add(label);
            }
        }

        private void DrawSupportVectors()
        {
            var supportVectors = _viewModel.GetSupportVectors();
            foreach (var sv in supportVectors)
            {
                var border = new Ellipse
                {
                    Width = 32,
                    Height = 32,
                    Stroke = Brushes.Orange,
                    StrokeThickness = 3,
                    Fill = Brushes.Transparent
                };

                Canvas.SetLeft(border, MapX(sv.Feature1) - 16);
                Canvas.SetTop(border, MapY(sv.Feature2) - 16);
                CanvasPlot.Children.Add(border);
            }
        }

        private void DrawDecisionBoundary()
        {
            var boundary = _viewModel.GetDecisionBoundary();

            double x1 = 0;
            double y1 = boundary.Slope * x1 + boundary.Intercept;
            double x2 = 10;
            double y2 = boundary.Slope * x2 + boundary.Intercept;

            if (y1 >= 0 && y1 <= 10 && y2 >= 0 && y2 <= 10)
            {
                var line = new Line
                {
                    X1 = MapX(x1),
                    Y1 = MapY(y1),
                    X2 = MapX(x2),
                    Y2 = MapY(y2),
                    Stroke = Brushes.Black,
                    StrokeThickness = 2.5,
                    StrokeDashArray = new DoubleCollection { 2, 2 }
                };
                CanvasPlot.Children.Add(line);
            }
        }

        private void DrawTestPoint()
        {
            double x = _viewModel.TestFeature1;
            double y = _viewModel.TestFeature2;

            var result = _viewModel.ClassificationResult;
            Brush color = result?.PredictedClass == 1 ? Brushes.Green : Brushes.OrangeRed;

            var ellipse = new Ellipse
            {
                Width = 30,
                Height = 30,
                Fill = color,
                Stroke = Brushes.Black,
                StrokeThickness = 2,
                Opacity = 0.9
            };

            Canvas.SetLeft(ellipse, MapX(x) - 15);
            Canvas.SetTop(ellipse, MapY(y) - 15);
            CanvasPlot.Children.Add(ellipse);

            // Эффект пульсации
            var pulse = new Ellipse
            {
                Width = 40,
                Height = 40,
                Stroke = color,
                StrokeThickness = 2,
                Fill = Brushes.Transparent,
                Opacity = 0.5
            };
            Canvas.SetLeft(pulse, MapX(x) - 20);
            Canvas.SetTop(pulse, MapY(y) - 20);
            CanvasPlot.Children.Add(pulse);

            // Подпись
            var label = new TextBlock
            {
                Text = $"ТЕСТ\n{result?.PredictedClassName}",
                FontSize = 8,
                FontWeight = FontWeights.Bold,
                Foreground = color,
                Background = Brushes.White,
                TextAlignment = TextAlignment.Center
            };
            Canvas.SetLeft(label, MapX(x) - 20);
            Canvas.SetTop(label, MapY(y) - 45);
            CanvasPlot.Children.Add(label);
        }

        private static double MapX(double x) => x * 45 + 30;
        private static double MapY(double y) => 470 - y * 45;
    }
}