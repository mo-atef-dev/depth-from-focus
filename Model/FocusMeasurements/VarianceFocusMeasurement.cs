using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.FocusMeasurements
{
    public class VarianceFocusMeasurement : IFocusMeasrurement
    {
        private readonly List<Mat> _focusMeasurements = new List<Mat>();

        public void CalculateFocusMeasurements(IList<Mat> images, int ksize)
        {
            foreach (Mat image in images)
            {
                Mat variance = new Mat(image.Size(), MatType.CV_32F);
                
                // Create a copy of image with CV_32F type
                Mat image32f = new Mat(image.Size(), MatType.CV_32F);
                image.ConvertTo(image32f, MatType.CV_32F);

                GetVariance(image32f, variance, ksize);

                _focusMeasurements.Add(variance);
                image32f.Dispose();
            }
        }

        public List<float> GetFocusMeasurements(int x, int y)
        {
            List<float> focusMeasurements = new List<float>();
            foreach (Mat image in _focusMeasurements)
            {
                focusMeasurements.Add(image.At<float>(y, x));
            }
            return focusMeasurements;
        }

        private void GetVariance(Mat inputArray, Mat outputArray, int kernelSize)
        {
            Mat average = new Mat(outputArray.Size(), MatType.CV_32F);
            Cv2.BoxFilter(inputArray, average, MatType.CV_32F, new Size(kernelSize, kernelSize), normalize: true, borderType: BorderTypes.Reflect);

            for (int i = 0; i < inputArray.Rows; i++)
            {
                for (int j = 0; j < inputArray.Cols; j++)
                {
                    int leftEdge = i > kernelSize/2 ? i - kernelSize/2 : 0;
                    int rightEdge = i < inputArray.Rows - kernelSize/2 ? i + kernelSize/2: inputArray.Rows;
                    int topEdge = j > kernelSize/2 ? j - kernelSize/2 : 0;
                    int bottomEdge = j < inputArray.Cols - kernelSize/2 ? j + kernelSize/2: inputArray.Cols;
                    
                    for (int k = leftEdge; k < rightEdge; k++)
                    {
                        for (int l = topEdge; l < bottomEdge; l++)
                        {
                            float diff = (inputArray.At<float>(k, l) - average.At<float>(i, j));
                            outputArray.At<float>(i, j) += diff*diff;
                        }
                    }
                }
            }

            average.Dispose();
        }
    }
}
