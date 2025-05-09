using System.Collections.Generic;

namespace TepscoIFCToRevit.Common.ComparerUtils
{
    public class DistanceEqualityComparer : IEqualityComparer<double>
    {
        private double _tolerance;

        public DistanceEqualityComparer(double tolerance)
        {
            _tolerance = tolerance;
        }

        public bool Equals(double x, double y)
        {
            return RevitUtils.IsEqual(x, y, _tolerance);
        }

        public int GetHashCode(double obj)
        {
            return base.GetHashCode();
        }
    }
}