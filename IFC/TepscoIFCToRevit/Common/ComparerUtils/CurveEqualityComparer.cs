using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Linq;

namespace TepscoIFCToRevit.Common.ComparerUtils
{
    public class CurveEqualityComparer : IEqualityComparer<Curve>
    {
        private readonly double _tolerance;

        public CurveEqualityComparer(double tolerance = RevitUtils.TOLERANCE)
        {
            _tolerance = tolerance;
        }

        public bool Equals(Curve x, Curve y)
        {
            return x != null
                   && y != null
                   && ValidateProperties(x, y)
                   && ValidateGeometry(x, y);
        }

        private bool ValidateProperties(Curve x, Curve y)
        {
            return x.GetType().Equals(y.GetType())
                && x.IsBound == y.IsBound
                //&& x.IsClosed == y.IsClosed
                && x.IsCyclic == y.IsCyclic
                && RevitUtils.IsEqual(x.Length, y.Length);
        }

        private bool ValidateGeometry(Curve x, Curve y)
        {
            var xPoints = x.Tessellate().ToList();
            var yPoints = y.Tessellate().ToList();
            return xPoints.OrderedPairMatch(yPoints, RevitUtils.IsEqual, _tolerance);
        }

        public int GetHashCode(Curve obj)
        {
            return base.GetHashCode();
        }
    }
}