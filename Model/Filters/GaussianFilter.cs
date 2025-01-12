using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Filters
{
    public class GaussianFilter : IImageFilter
    {
        public void ApplyFilter(Mat inputArray, Mat outputArray, int kernelSize)
        {
            Cv2.GaussianBlur(inputArray, outputArray, new Size(kernelSize, kernelSize), 0, 0);
        }
    }
}
