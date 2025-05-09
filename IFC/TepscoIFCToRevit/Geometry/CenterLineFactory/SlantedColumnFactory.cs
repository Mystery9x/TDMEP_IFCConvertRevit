using Autodesk.Revit.DB;
using RevitUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using TepscoIFCToRevit.Common;
using TepscoIFCToRevit.Data.GeometryDatas;

namespace TepscoIFCToRevit.Geometry.CenterLineFactory
{
    public class SlantedColumnFactory : BarShapeFactory
    {
        public SlantedColumnFactory(Document doc) : base(doc)
        {
        }

        public void GetDimensionVectors(GeometryData geo, out XYZ hand, out XYZ facing, out Line lcLine)
        {
            hand = null;
            facing = null;

            var centerLine = GetCenterLine(geo).FirstOrDefault();
            lcLine = null;

            if (centerLine != null)
            {
                List<Plane> sideFaces = GetSidePlanes(geo, centerLine.Direction);
                Plane farPlane = sideFaces.OrderBy(x => Math.Abs(UtilsPlane.GetSignedDistance(x, centerLine.Origin))).Last();
                if (farPlane != null)
                {
                    var lengthDimPlanes = sideFaces.Where(x => RevitUtils.IsParallel(x.Normal, farPlane.Normal))
                                                    .OrderBy(x => x.Origin, farPlane.Normal);
                    if (lengthDimPlanes?.Count >= 2)
                    {
                        hand = lengthDimPlanes.First().Normal;
                        var up = GetUpDirection(geo, centerLine.Direction);
                        facing = up.CrossProduct(hand);

                        lcLine = centerLine;
                    }
                }
            }
        }

        private XYZ GetUpDirection(GeometryData geo, XYZ refVector)
        {
            XYZ up = refVector;
            var spatials = geo.Faces
                              .Select(x => x.Plane)
                              .Where(x => RevitUtils.IsParallel(x.Normal, refVector, 1e-1))
                              .OrderBy(x => x.Origin, refVector);

            if (spatials?.Count >= 2)
            {
                var first = spatials.First().Origin;
                var last = spatials.Last().Origin;

                XYZ start = (first + last) / 2;
                XYZ end = UtilsPlane.ProjectOnto(spatials.Last(), start);
                XYZ vec = (end - start).Normalize();
                if (!vec.IsZeroLength())
                    up = vec;
            }

            if (!up.IsZeroLength())
            {
                double angle = XYZ.BasisZ.AngleTo(up);
                if (RevitUtils.IsGreaterThan(angle, Math.PI / 2))
                    up = up.Negate();
                return up;
            }
            return up;
        }

        private List<Plane> GetSidePlanes(GeometryData geo, XYZ refVector)
        {
            return geo.Faces
                    .Select(x => x.Plane)
                    .Where(x => RevitUtils.IsPerpendicular(x.Normal, refVector, 1e-1))
                    .ToList();
        }
    }
}