using Autodesk.Revit.DB;
using System.Collections.Generic;

namespace TepscoIFCToRevit.Common.ComparerUtils
{
    public class PointEqualityComparer : IEqualityComparer<XYZ>
    {
        private readonly double _tolerance;

        public PointEqualityComparer(double tolerance = RevitUtils.TOLERANCE)
        {
            _tolerance = tolerance;
        }

        public bool Equals(XYZ x, XYZ y)
        {
            if (x != null && y != null)
            {
                double distance = x.DistanceTo(y);
                return distance <= _tolerance;
            }
            return false;
        }

        public int GetHashCode(XYZ obj)
        {
            return base.GetHashCode();
        }
    }
}