using OpenCvSharp;
using NumSharp;
using System.Diagnostics;
using Model.FocusMeasurements;
using Model.Filters;
using Model.FocusDistance;

namespace Model
{
    public class DepthMeasurement
    {
        private IList<Mat> _imageList = new List<Mat>();
        private IFocusMeasrurement _focusMeasurement;
        private IImageFilter? _preProcessingFilter;
        private Mat? _trueDepthMap;

        public const int HistogramBars = 500;

        public IFocusMeasrurement FocusMeasurement { get { return _focusMeasurement; } }
        private Mat DepthMap { get; set; }
        public Mat FilteredDepthMap { get; private set; }
        public Mat NormalizedDepthMap { get; private set; }

        public Mat ErrorMap { get; private set; }
        public Mat NormalizedErrorMap { get; private set; }
        public int[] ErrorHistogram { get; private set; } = new int[HistogramBars];
        public double MeanError { get; private set; }
        public double MaxError { get; private set; }
        public double MinError { get; private set; }
        public double MedianError { get; private set; }

        public DepthMeasurement(IList<string> imagesFiles, IFocusMeasrurement focusMeasurement, IImageFilter? preProcessingFilter = null, string trueDepthMapFile = "")
        {
            if (imagesFiles.Count < 1)
                throw new ArgumentException("No images provided");

            foreach (var imagesFile in imagesFiles)
            {
                // Load the image and store in the list
                var image = Cv2.ImRead(imagesFile);
                Cv2.CvtColor(image, image, ColorConversionCodes.BGR2GRAY);
                _imageList.Add(image);
            }

            DepthMap = new Mat(_imageList[0].Size(), MatType.CV_32F);
            FilteredDepthMap = new Mat(_imageList[0].Size(), MatType.CV_32F);
            ErrorMap = new Mat(_imageList[0].Size(), MatType.CV_32F);
            NormalizedDepthMap = new Mat(_imageList[0].Size(), MatType.CV_8U);
            NormalizedErrorMap = new Mat(_imageList[0].Size(), MatType.CV_8U);

            // Check if a file exists at the given path
            if (!string.IsNullOrEmpty(trueDepthMapFile) || File.Exists(trueDepthMapFile))
            {
                _trueDepthMap = Cv2.ImRead(trueDepthMapFile, ImreadModes.Unchanged);
            }

            _focusMeasurement = focusMeasurement;
            _preProcessingFilter = preProcessingFilter;
        }

        public void CalculateFilter(FilterType blurType, int kernelSize = 5)
        {
            if (kernelSize < 0)
                throw new ArgumentOutOfRangeException("Kernel size must be non-negative");

            switch(blurType)
            {
                case FilterType.None:
                    Cv2.CopyTo(DepthMap, FilteredDepthMap);
                    break;
                case FilterType.Box:
                    Cv2.BoxFilter(DepthMap, FilteredDepthMap, MatType.CV_32F, new Size(kernelSize, kernelSize));
                    break;
                case FilterType.Gaussian:
                    Cv2.GaussianBlur(DepthMap, FilteredDepthMap, new Size(kernelSize, kernelSize), 0, 0);
                    break;
                case FilterType.Median:
                    CVF32MedianBlur(DepthMap, FilteredDepthMap, kernelSize);
                    break;
            }

            Cv2.Normalize(FilteredDepthMap, NormalizedDepthMap, 0, 255, NormTypes.MinMax, dtype: MatType.CV_8U);
        }

        public void CalculateFocusMeasurements(int kernelSize, int preFilterKernelSize = 3)
        {
            foreach (var image in _imageList)
            {
                _preProcessingFilter?.ApplyFilter(image, image, preFilterKernelSize);
            }

            _focusMeasurement.CalculateFocusMeasurements(_imageList, kernelSize);
        }

        public void CalculateDepth(IFocusDistanceCalculator focusDistanceCalculator)
        {
            for(int i = 0; i < DepthMap.Rows; i++)
            {
                for(int j = 0; j < DepthMap.Cols; j++)
                {
                    IList<float> focusMeasurements = _focusMeasurement.GetFocusMeasurements(j, i);
                    int maxIndex = focusMeasurements.IndexOf(focusMeasurements.Max());

                    if(maxIndex > 0 && maxIndex < focusMeasurements.Count - 1)
                    {
                        float[] x = [maxIndex - 1, maxIndex, maxIndex + 1];
                        float[] y = [focusMeasurements[maxIndex - 1], focusMeasurements[maxIndex], focusMeasurements[maxIndex + 1]];

                        (float sBar, float Mp, float sigma) = Helpers.FitGaussian(x, y);

                        DepthMap.At<float>(i, j) = focusDistanceCalculator.CalculateDistance(sBar);

                    }
                    else
                    {
                        DepthMap.At<float>(i, j) = focusDistanceCalculator.CalculateDistance(maxIndex);
                    }
                }
            }
        }

        public void CalculateError()
        {
            if (_trueDepthMap is null)
                return;

            for (int i = 0; i < ErrorMap.Rows; i++)
            {
                for (int j = 0; j < ErrorMap.Cols; j++)
                {
                    var trueDepth = _trueDepthMap.At<float>(i, j);
                    var filteredDepth = FilteredDepthMap.At<float>(i, j);
                    ErrorMap.At<float>(i, j) = Math.Abs(trueDepth - filteredDepth);
                }
            }

            Cv2.Normalize(ErrorMap, NormalizedErrorMap, 0, 255, NormTypes.MinMax, dtype: MatType.CV_8U);

            // Calculate error stats
            MeanError = Cv2.Mean(ErrorMap).ToDouble();
            double minError, maxError;
            ErrorMap.MinMaxLoc(out minError, out maxError);

            MinError = minError;
            MaxError = maxError;
            MedianError = ErrorMap.Median<float>();

            // Zero the error histogram array
            for (int i = 0; i < HistogramBars; i++)
            {
                ErrorHistogram[i] = 0;
            }

            for (int i = 0; i < ErrorMap.Rows; i++)
            {
                for (int j = 0; j < ErrorMap.Cols; j++)
                {
                    var error = ErrorMap.At<float>(i, j);

                    double dIndex = (error * (HistogramBars - 1) / maxError);
                    int index = (int)dIndex;
                    ErrorHistogram[index] += 1;
                }
            }
        }

        private void CVF32MedianBlur(Mat src, Mat dist, int kernelSize)
        {
            for(int i = 0; i < src.Rows; i++)
            {
                for(int j = 0; j < src.Cols; j++)
                {
                    List<float> values = new List<float>();
                    for(int k = i - kernelSize/2; k < i + kernelSize/2; k++)
                    {
                        for(int l = j - kernelSize/2; l < j + kernelSize/2; l++)
                        {
                            int kk = k;
                            int ll = l;

                            if(kk < 0) kk = 0; else if(kk >= src.Rows) kk = src.Rows - 1;
                            if(ll < 0) ll = 0; else if(ll >= src.Cols) ll = src.Cols - 1;

                            var value = src.At<float>(kk, ll);
                            values.Add(value);
                        }
                    }
                    values.Sort();
                    dist.At<float>(i, j) = values[values.Count/2];
                }
            }
        }
    }

    public enum FilterType
    {
        None = 0,
        Box = 1,
        Gaussian = 2,
        Median = 3
    }
}
