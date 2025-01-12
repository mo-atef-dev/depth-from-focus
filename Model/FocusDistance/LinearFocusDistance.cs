using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.FocusDistance
{
    public class LinearFocusDistance : IFocusDistanceCalculator
    {
        private float _initialDistance;
        private float _incrementalDistance;

        public LinearFocusDistance(float initialDistance, float incrementalDistance)
        {
            _initialDistance = initialDistance;
            _incrementalDistance = incrementalDistance;
        }

        public float CalculateDistance(float interpolatedFocusIndex)
        {
            return _initialDistance + _incrementalDistance * interpolatedFocusIndex;
        }
    }
}
