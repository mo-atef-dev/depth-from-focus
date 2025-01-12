using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.FocusDistance
{
    public interface IFocusDistanceCalculator
    {
        float CalculateDistance(float interpolatedFocusIndex);
    }
}
