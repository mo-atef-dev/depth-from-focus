using OpenCvSharp;

namespace Model.FocusMeasurements
{
    public class GradientFocusMeasurement : IFocusMeasrurement
    {
        private readonly List<Mat> _focusMeasurements = new List<Mat>();

        public GradientFocusMeasurement()
        {

        }

        public List<float> GetFocusMeasurements(int x, int y)
        {
            List<float> focusMeasurements = new List<float>();
            foreach (Mat laplacian in _focusMeasurements)
            {
                focusMeasurements.Add(laplacian.At<float>(y, x));
            }
            return focusMeasurements;
        }

        private void GetGradientMagnitude(Mat inputArray, Mat outputArray)
        {
            for (int i = 0; i < inputArray.Rows; i++)
            {
                for (int j = 0; j < inputArray.Cols; j++)
                {
                    int center = inputArray.At<byte>(i, j);
                    int left = i > 0 ? inputArray.At<byte>(i - 1, j) : center;
                    int right = i < inputArray.Rows - 1 ? inputArray.At<byte>(i + 1, j) : center;
                    int top = j > 0 ? inputArray.At<byte>(i, j - 1) : center;
                    int bottom = j < inputArray.Cols - 1 ? inputArray.At<byte>(i, j + 1) : center;
                    int centerDiff = Math.Abs(left - right) / 2 + Math.Abs(top - bottom) / 2;
                    outputArray.At<float>(i, j) = centerDiff;
                }
            }
        }

        private void GetFocusMeasure(Mat laplacian, Mat focusMeasurement, int kernelSize)
        {
            // Square the laplacian matrix and apply a box filter to get measurement
            Cv2.Pow(laplacian, 2, focusMeasurement);
            Cv2.BoxFilter(focusMeasurement, focusMeasurement, MatType.CV_32F, new Size(kernelSize, kernelSize), normalize: false);
        }

        public void CalculateFocusMeasurements(IList<Mat> images, int ksize)
        {
            foreach (Mat image in images)
            {
                Mat laplacian = new Mat(image.Size(), MatType.CV_32F);
                GetGradientMagnitude(image, laplacian);
                GetFocusMeasure(laplacian, laplacian, ksize);
                _focusMeasurements.Add(laplacian);
            }
        }
    }
}
