using Autodesk.Revit.DB;
using System.Collections.Generic;
using TepscoIFCToRevit.Data.GeometryDatas;

namespace TepscoIFCToRevit.Geometry.CenterLineFactory
{
    public class BarShapeFactory : GeometryFactory
    {
        public List<Line> Lines { get; set; }

        public BarShapeFactory(Document doc) : base(doc)
        {
            Lines = new List<Line>();
        }

        public override List<Line> GetCenterLine(GeometryData geometry)
        {
            //List<Line> lines = new List<Line>();
            //XYZ min = MatchElev(geometry.BoxMin, 0);
            //XYZ max = MatchElev(geometry.BoxMax, 0);
            //XYZ dir = (geometry.BoxMax - geometry.BoxMin).Normalize();

            //List<XYZ> points = geometry.Vertices.Distinct(new PointEqualityComparer(RevitUtils.TOLERANCE)).ToList();
            //points = points.OrderBy(dir);

            //var groups = Group(points, min, max, 1, 5);

            //if (groups?.Count > 1)
            //{
            //    XYZ center = (geometry.BoxMin + geometry.BoxMax) / 2;

            //    List<XYZ> endPoints = new List<XYZ>();
            //    endPoints.AddRange(groups.First());
            //    endPoints.AddRange(groups.Last().Select(x => RevitUtils.GetMirrorPoint(x, center)));

            //    endPoints = endPoints.Where(x => x != null)
            //                        .Distinct(new PointEqualityComparer(RevitUtils.TOLERANCE))
            //                        .ToList();

            //    XYZ endMin = endPoints.GetBoxMin();
            //    XYZ endMax = endPoints.GetBoxMax();
            //    XYZ end = (endMin + endMax) / 2;
            //    XYZ start = RevitUtils.GetMirrorPoint(end, center);

            //    lines.Add(Line.CreateBound(start, end));
            //}
            //return lines;
            return null;
        }

        private XYZ MatchElev(XYZ point, double elev)
        {
            if (point != null && !double.IsNaN(elev))
            {
                return new XYZ(point.X, point.Y, elev);
            }
            return null;
        }

        private List<List<XYZ>> Group(List<XYZ> points, XYZ start, XYZ end, int count, int max)
        {
            List<XYZ> left = new List<XYZ>();
            List<XYZ> right = new List<XYZ>();
            List<List<XYZ>> groups = new List<List<XYZ>>();

            XYZ mid = (start + end) / 2;
            double midDist = mid.DistanceTo(start);

            Line line = Line.CreateUnbound(start, end - start);

            foreach (var p in points)
            {
                var projected = line.Project(p).XYZPoint;
                double dist = projected.DistanceTo(start);

                if (dist <= midDist)
                    left.Add(p);
                else
                    right.Add(p);
            }

            if (count < max)
            {
                if (left.Count > 0)
                {
                    var groupLeft = Group(left, start, mid, count + 1, max);
                    groups.AddRange(groupLeft);
                }

                if (right.Count > 0)
                {
                    var groupRight = Group(right, mid, end, count + 1, max);
                    groups.AddRange(groupRight);
                }
            }
            else
            {
                if (left.Count > 0)
                    groups.Add(left);
                if (right.Count > 0)
                    groups.Add(right);
            }
            return groups;
        }
    }
}