using Autodesk.Revit.DB;
using RevitUtilities;

namespace TepscoIFCToRevit.Data.GeometryDatas
{
    public class PlanarFaceData
    {
        public PlanarFace Face { get; set; }
        public Plane Plane { get; set; }
        public XYZ Point { get; set; }
        public int Id { get; set; }

        public PlanarFaceData(PlanarFace face, XYZ point, int id = -1)
        {
            Id = id;
            Face = face;
            Plane = Plane.CreateByNormalAndOrigin(face.FaceNormal, face.Origin);
            Point = UtilsPlane.ProjectOnto(Plane, point);
        }

        public PlanarFaceData(XYZ point, int id = -1)
        {
            Id = id;
            Face = null;
            Plane = null;
            Point = point;
        }
    }
}