using OpenCvSharp;
using NumSharp;
using System.Runtime.InteropServices;
using NumSharp.Backends.Unmanaged;
using System.Threading.Channels;

namespace Model
{
    internal static class OpenCVSharpExtensions
    {
        public static T Median<T>(this Mat src) where T : unmanaged
        {
            var channels = src.Channels();

            src.GetArray<T>(out T[] array);

            Array.Sort(array);

            return array[array.Length / 2];
        }

        public static NDArray ConvertMatToNDArray(this Mat src)
        {
            throw new NotSupportedException("Unsupported Mat depth");

            // Determine the data type of the Mat elements
            var depth = src.Depth();
            var type = ToNPTypeCode(depth);
            var channels = src.Channels();
            var shape = new int[] { src.Rows, src.Cols, channels };

            NDArray nd = new NDArray(type, shape);

            for (int i = 0; i < src.Rows; i++)
            {
                for (int j = 0; j < src.Cols; j++)
                {
                    for (int k = 0; k < channels; k++)
                    {
                        //nd[i, j, k] = src.Get(type, i, j, k);
                    }
                }
            }

            return nd;
        }

        private static Type ToNPTypeCode(int type)
        {
            switch (type)
            {
                case MatType.CV_8U:
                    return typeof(byte);
                case MatType.CV_16U:
                    return typeof(ushort);
                case MatType.CV_16S:
                    return typeof(short);
                case MatType.CV_32S:
                    return typeof(int);
                case MatType.CV_32F:
                    return typeof(float);
                case MatType.CV_64F:
                    return typeof(double);
                default:
                    throw new NotSupportedException("Unsupported Mat depth");
            }
        }
    }
}
