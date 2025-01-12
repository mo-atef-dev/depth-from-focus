using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using Microsoft.Win32;
using Model;
using Model.Filters;
using Model.FocusDistance;
using Model.FocusMeasurements;
using OpenCvSharp.WpfExtensions;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Focus_Measurement_Tool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public BindingList<string> ImageNames { get; } = new BindingList<string>();
        public string TrueDepthFileName { get; private set; } = "";

        private ICollection<double> _columnSeries = new ObservableCollection<double>();
        private ICollection<Point> _errorSeries = new ObservableCollection<Point>();
        private DepthMeasurement? _depthMeasurement = null;

        public MainWindow()
        {
            // Enable OpenEXR IO for OpenCV
            Environment.SetEnvironmentVariable("OPENCV_IO_ENABLE_OPENEXR", "1");

            InitializeComponent();

            ISeries[] FocusSeries = [new ColumnSeries<double>(_columnSeries)];
            ISeries[] ErrorHistogramSeries = [new ColumnSeries<Point>(_errorSeries)
            {
                Mapping = (sample, chartPoint) => new (sample.X, sample.Y),
            }];
            Axis[] ErrorHistogramXAxes = [new Axis()
            {
            }];
            FocusChart.Series = FocusSeries;
            ErrorHistogramChart.Series = ErrorHistogramSeries;
            //ErrorHistogramChart.XAxes = ErrorHistogramXAxes;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // Open a file dialog to select an image file
            OpenFileDialog openFileDialog = new();
            openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.gif;*.bmp|All Files|*.*";
            openFileDialog.Multiselect = true;
            if (openFileDialog.ShowDialog() == true)
            {
                // Get the selected image file path
                string imagePath = openFileDialog.FileName;
                MainImage.Source = new BitmapImage(new Uri(imagePath));

                ImageNames.Clear();
                foreach (string fileName in openFileDialog.FileNames)
                {
                    ImageNames.Add(fileName);
                }
            }
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Set main image to the selected image
            if(ImagesList.SelectedItem is null || !File.Exists((string)ImagesList.SelectedItem)) return;

            MainImage.Source = new BitmapImage(new Uri((string)ImagesList.SelectedItem));
        }

        private void MainImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if(MainImage.Source is null || MainImage.Source is not BitmapImage || _depthMeasurement is null)
            {
                return;
            }

            _columnSeries.Clear();
            int x = (int)(e.GetPosition(MainImage).X * ((BitmapImage)MainImage.Source).PixelWidth / MainImage.ActualWidth);
            int y = (int)(e.GetPosition(MainImage).Y * ((BitmapImage)MainImage.Source).PixelHeight / MainImage.ActualHeight);
            
            foreach (double measurement in _depthMeasurement.FocusMeasurement.GetFocusMeasurements(x, y))
            {
                _columnSeries.Add(measurement);
            }
        }

        private async void ButtonDepth_Click(object sender, RoutedEventArgs e)
        {
            if(ImageNames.Count < 2)
            {
                MessageBox.Show("Please load at least 2 images", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _depthMeasurement = new DepthMeasurement(ImageNames, new GradientFocusMeasurement(), new GaussianFilter(), TrueDepthFileName);
            var filterType = Enum.Parse<FilterType>(FilterTypeChoice.SelectedValue.ToString() ?? FilterType.None.ToString());
            var kernelSize = Int32.Parse(KernelSizeBox.Text);
            var preFilterKernelSize = Int32.Parse(PreFilterKernelSizeBox.Text);
            var initialDepth = float.Parse(InitialDepthBox.Text);
            var depthIncrement = float.Parse(DepthIncrementBox.Text);

            var focusKernelSize = Int32.Parse(FocusKernelSizeBox.Text);

            var focusDistanceCalculator = new LinearFocusDistance(initialDepth, depthIncrement);

            await PerformLongOperation(() =>
            {
                _depthMeasurement.CalculateFocusMeasurements(focusKernelSize, preFilterKernelSize);
                _depthMeasurement.CalculateDepth(focusDistanceCalculator);
                _depthMeasurement.CalculateFilter(filterType, kernelSize);
                _depthMeasurement.CalculateError();
            });

            DepthImage.Source = _depthMeasurement.NormalizedDepthMap.ToWriteableBitmap();
            ErrorImage.Source = _depthMeasurement.NormalizedErrorMap.ToWriteableBitmap();

            // Set error statistics
            _errorSeries.Clear();
            for (int i = 0; i < _depthMeasurement.ErrorHistogram.Length; i++)
            {
                var point = new Point(i*_depthMeasurement.MaxError/500, _depthMeasurement.ErrorHistogram[i]);
                _errorSeries.Add(point);
            }

            ((ColumnSeries<Point>)ErrorHistogramChart.Series.First()).MaxBarWidth = _depthMeasurement.MaxError/DepthMeasurement.HistogramBars;

            MaxErrorLabel.Content = _depthMeasurement.MaxError.ToString();
            MeanErrorLabel.Content = _depthMeasurement.MeanError.ToString();
            MedianErrorLabel.Content = _depthMeasurement.MedianError.ToString();
        }

        private void ButtonTrueDepth_Click(object sender, RoutedEventArgs e)
        {
            // Open a file dialog to select an image file
            OpenFileDialog openFileDialog = new();
            openFileDialog.Filter = "Image Files|*.exr|All Files|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                // Get the selected image file path
                TrueDepthFileName = openFileDialog.FileName;

                // Read the depth image file
                OpenCvSharp.Mat depthMap = OpenCvSharp.Cv2.ImRead(TrueDepthFileName, OpenCvSharp.ImreadModes.Unchanged);
                OpenCvSharp.Mat normalizedDepthMap = new OpenCvSharp.Mat(depthMap.Size(), OpenCvSharp.MatType.CV_8U);

                // Normalize the depth map
                OpenCvSharp.Cv2.Normalize(depthMap, normalizedDepthMap, 0, 255, OpenCvSharp.NormTypes.MinMax, dtype: OpenCvSharp.MatType.CV_8U);

                // Display the normalized depth map
                TrueDepthImage.Source = normalizedDepthMap.ToWriteableBitmap();

                depthMap.Dispose();
                normalizedDepthMap.Dispose();
            }
        }

        private async void RecalculateFilterButton_Click(object sender, RoutedEventArgs e)
        {
            if (_depthMeasurement == null)
                return;

            var filterType = Enum.Parse<FilterType>(FilterTypeChoice.SelectedValue.ToString() ?? FilterType.None.ToString());
            var kernelSize = Int32.Parse(KernelSizeBox.Text);

            await PerformLongOperation(() =>
            {
                _depthMeasurement.CalculateFilter(filterType, kernelSize);
                _depthMeasurement.CalculateError();
            });

            DepthImage.Source = _depthMeasurement.NormalizedDepthMap.ToWriteableBitmap();
            ErrorImage.Source = _depthMeasurement.NormalizedErrorMap.ToWriteableBitmap();

            // Set error statistics
            _errorSeries.Clear();
            for (int i = 0; i < _depthMeasurement.ErrorHistogram.Length; i++)
            {
                var point = new Point(i * _depthMeasurement.MaxError / 500, _depthMeasurement.ErrorHistogram[i]);
                _errorSeries.Add(point);
            }

            ((ColumnSeries<Point>)ErrorHistogramChart.Series.First()).MaxBarWidth = _depthMeasurement.MaxError / DepthMeasurement.HistogramBars;

            MaxErrorLabel.Content = _depthMeasurement.MaxError.ToString();
            MeanErrorLabel.Content = _depthMeasurement.MeanError.ToString();
            MedianErrorLabel.Content = _depthMeasurement.MedianError.ToString();
        }

        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            if(_depthMeasurement?.FilteredDepthMap != null)
            {
                // Open a save file dialog to select a file path
                SaveFileDialog saveFileDialog = new();
                saveFileDialog.Filter = "EXR Files|*.exr|All Files|*.*";
                if (saveFileDialog.ShowDialog() == true)
                {
                    // Save the image file
                    _depthMeasurement.FilteredDepthMap.SaveImage(saveFileDialog.FileName);
                }
            }
            else
            {
                MessageBox.Show("No depth map to save", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }    
        }

        private void ButtonSaveNormalizedError_Click(object sender, RoutedEventArgs e)
        {
            if (_depthMeasurement?.NormalizedErrorMap != null)
            {
                // Open a save file dialog to select a file path
                SaveFileDialog saveFileDialog = new();
                saveFileDialog.Filter = "PNG Files|*.png|All Files|*.*";
                if (saveFileDialog.ShowDialog() == true)
                {
                    // Save the image file
                    _depthMeasurement.NormalizedErrorMap.SaveImage(saveFileDialog.FileName);
                }
            }
            else
            {
                MessageBox.Show("No error map to save", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task PerformLongOperation(Action operation)
        {
            var task = Task.Run(operation);

            (new ProgressModal(task)).ShowDialog();

            await task;
        }
    }
}