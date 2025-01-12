using OpenCvSharp;
using NumSharp;
using System.Diagnostics;

namespace Model
{
    internal class Helpers
    {

        public static (float sBar, float Mp, float sigma) FitGaussian(float[] x, float[] y)
        {
            if (x == null || y == null)
            {
                throw new ArgumentException("x and y should not be null");
            }
            if (x.Length != 3 || y.Length != 3)
            {
                throw new ArgumentException("x and y should have 3 elements");
            }
            if (x[0] >= x[1] || x[1] >= x[2])
            {
                throw new ArgumentException("x should be strictly increasing");
            }

            double num = Math.Log(y[1] / y[2]) * (x[1] * x[1] - x[0] * x[0]) - Math.Log(y[1] / y[0]) * (x[1] * x[1] - x[2] * x[2]);
            double denom = 2 * (x[2] - x[1]) * Math.Log((y[1] * y[1]) / (y[0] * y[2]));

            float sBar;
            if (denom == 0 || y[0] == 0 || y[1] == 0 || y[2] == 0)
            {
                // Get y argmax
                int index = y.ToList().IndexOf(y.Max());
                sBar = x[index];
            }
            else
            {
                sBar = (float)num / (float)denom;
            }

            float sigma = 1;
            float Mp = 1;
            return (sBar, Mp, sigma);
        }
    }
}
