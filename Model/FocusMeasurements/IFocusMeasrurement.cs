using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.FocusMeasurements
{
    public interface IFocusMeasrurement
    {
        void CalculateFocusMeasurements(IList<Mat> images, int ksize);
        List<float> GetFocusMeasurements(int x, int y);
    }
}
