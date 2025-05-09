using Autodesk.Revit.DB;
using System.Collections.Generic;
using TepscoIFCToRevit.Common;

namespace TepscoIFCToRevit.Data.GeometryDatas
{
    public class MeshPlaneData
    {
        public Plane Plane { get; set; }
        public List<XYZ> Vertices { get; set; }

        public MeshPlaneData(Plane plane, List<XYZ> vertices)
        {
            Plane = plane;
            Vertices = vertices;
        }
    }

    public class FaceData
    {
        public Plane Plane { get; set; }
        public double Area { get; set; }

        public FaceData()
        { }

        public FaceData(XYZ p0, XYZ p1, XYZ p2)
        {
            try
            {
                Plane = Plane.CreateByThreePoints(p0, p1, p2);

                double length = p0.DistanceTo(p1);
                if (length > RevitUtils.MIN_LENGTH)
                {
                    Line line = Line.CreateUnbound(p0, p1 - p0);
                    Area = 0.5 * length * line.Distance(p2);
                }
            }
            catch (System.Exception) { }
        }
    }
}