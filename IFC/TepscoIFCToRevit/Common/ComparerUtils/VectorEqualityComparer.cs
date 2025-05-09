using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;

namespace TepscoIFCToRevit.Common.ComparerUtils
{
    public class VectorEqualityComparer : IEqualityComparer<XYZ>
    {
        private double _degree;
        private double _tolerance;

        /// <summary>
        /// tolerance has degree unit
        /// </summary>
        /// <param name="degreeAngle"></param>
        public VectorEqualityComparer(double degreeAngle = 0, double tolerance = 0)
        {
            _degree = degreeAngle;
            _tolerance = tolerance;
        }

        public bool Equals(XYZ x, XYZ y)
        {
            return Math.Abs(x.AngleTo(y) - _degree * Math.PI / 180) < _tolerance * Math.PI / 180;
        }

        public int GetHashCode(XYZ obj)
        {
            return 0;
        }
    }
}