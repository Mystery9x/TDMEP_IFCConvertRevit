using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Linq;
using TepscoIFCToRevit.Common;
using TepscoIFCToRevit.Common.ComparerUtils;
using TepscoIFCToRevit.Data.GeometryDatas;

namespace TepscoIFCToRevit.Geometry.CenterLineFactory
{
    public class CylinderFactory : GeometryFactory
    {
        public CylinderFactory(Document doc) : base(doc)
        {
        }

        public override List<Line> GetCenterLine(GeometryData geometry)
        {
            Line rough = GetRoughCenterLine(geometry);
            if (rough != null)
            {
                var centricVectors = geometry.Faces.Select(x => x.Plane)
                                                        .Where(x => x != null && RevitUtils.IsPerpendicular(x.Normal, rough.Direction))
                                                        .Select(x => x.Normal)
                                                        .Distinct(new PointEqualityComparer(RevitUtils.TOLERANCE))
                                                        .ToList();
            }
            return new List<Line>() { rough };
        }

        private Line GetRoughCenterLine(GeometryData geometry)
        {
            var faces = GetEndFaces(geometry);
            if (GetStartEndFaces(faces, out List<MeshPlaneData> startFaces, out List<MeshPlaneData> endFaces))
            {
                var center_0 = startFaces.SelectMany(x => x.Vertices).Average();
                var center_1 = endFaces.SelectMany(x => x.Vertices).Average();
                return Line.CreateBound(center_0, center_1);
            }
            return null;
        }

        private List<MeshPlaneData> GetEndFaces(GeometryData geometry)
        {
            //var faces = geometry.MeshPlanes
            //                    .Where(x => x.Vertices.Count > 4)
            //                    .ToList();
            //return faces;

            return null;
        }

        private bool GetStartEndFaces(List<MeshPlaneData> bothEndFaces,
                                      out List<MeshPlaneData> startFaces,
                                      out List<MeshPlaneData> endFaces)
        {
            startFaces = new List<MeshPlaneData>();

            if (bothEndFaces?.Count > 0)
            {
                startFaces.Add(bothEndFaces[0]);
                int count = 0;
                List<int> indices = new List<int>();

                do
                {
                    indices.Add(bothEndFaces.IndexOf(startFaces[count]));
                    for (int i = 0; i < bothEndFaces.Count; i++)
                    {
                        if (!indices.Contains(i) && IsNeighborFace(startFaces[count], bothEndFaces[i]))
                        {
                            indices.Add(i);
                            startFaces.Add(bothEndFaces[i]);
                        }
                    }

                    count++;
                }
                while (count < startFaces.Count);
            }

            endFaces = bothEndFaces.Except(startFaces).ToList();
            return startFaces?.Count > 0 && endFaces?.Count > 0;
        }

        private bool IsNeighborFace(MeshPlaneData face_0, MeshPlaneData face_1)
        {
            if (face_0 != null
                && face_1 != null
                && face_0.Vertices?.Count > 0
                && face_1.Vertices?.Count > 0)
            {
                var intersectCount = face_0.Vertices.Intersect(face_1.Vertices, new PointEqualityComparer(1e-5)).Count();
                return intersectCount == 2;
            }
            return false;
        }
    }
}