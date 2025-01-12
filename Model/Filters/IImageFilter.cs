using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Filters
{
    public interface IImageFilter
    {
        void ApplyFilter(Mat inputArray, Mat outputArray, int kernelSize);
    }
}
