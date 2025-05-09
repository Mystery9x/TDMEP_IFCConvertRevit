using Autodesk.Revit.DB;
using RevitUtilities;
using System.Collections.Generic;
using System.Linq;
using TepscoIFCToRevit.Common;
using TepscoIFCToRevit.Common.ComparerUtils;
using Revit_Mesh = Autodesk.Revit.DB.Mesh;

namespace TepscoIFCToRevit.Data.GeometryDatas
{
    public class GeometryData
    {
        //public XYZ Location { get; set; }
        //public List<Face> Faces { get; set; }
        //private List<Plane> Planes { get; set; }

        //public List<MeshPlaneData> MeshPlanes { get; set; }

        //public List<Curve> Edges { get; set; }
        //public XYZ BoxMin { get; set; }
        //public XYZ BoxMax { get; set; }
        //public XYZ Mid { get; set; }

        //public XYZ PointMin { get; set; }
        //public XYZ PointMax { get; set; }

        public int MaxVertices { get; set; } = 25000;

        public bool OverMaxVertices { get; set; }

        public List<FaceData> Faces { get; set; }

        public List<XYZ> Vertices { get; set; }

        public GeometryData()
        {
            InitLists();
        }

        public GeometryData(List<GeometryObject> geometries, Solid solid = null, bool isRailing = false)
        {
            MaxVertices = isRailing ? 2500 : 25000;
            InitLists();
            GetIfcGemetryData(geometries, solid);
        }

        private void InitLists()
        {
            //Faces = new List<Face>();
            //Planes = new List<Plane>();
            //Edges = new List<Curve>();

            OverMaxVertices = false;
            Faces = new List<FaceData>();
            Vertices = new List<XYZ>();
        }

        #region Extract geometry

        public void GetIfcGemetryData(List<GeometryObject> geometries, Solid solidObject = null)
        {
            if (geometries?.Count > 0)
            {
                foreach (var geo in geometries)
                {
                    if (geo is Solid solid)
                        GetIfcSolidData(solid);
                    else if (geo is Revit_Mesh mesh)
                        GetIfcMeshData(mesh);
                    //else if (geo is Curve curve)
                    //    GetIfcCurveData(ref data, curve);
                }
            }

            if (solidObject != null)
                GetIfcSolidData(solidObject);

            if (Vertices?.Count > 0)
            {
                // max points in check is 50000

                if (Vertices.Count > MaxVertices)
                {
                    OverMaxVertices = true;
                    return;
                }
                Vertices = Vertices.Distinct(new PointEqualityComparer(RevitUtils.TOLERANCE)).ToList();
                InitMergedFace();
            }

            //Location = Vertices.Average();
            //Edges = Edges.Distinct(new CurveEqualityComparer(RevitUtils.TOLERANCE)).ToList();

            //RevitUtils.FindMaxMinPoint(Vertices, out XYZ min, out XYZ max);
            //BoxMin = min;
            //BoxMax = max;

            //Mid = (BoxMax + BoxMin) / 2;
            //InitPointMinMax();
        }

        private void InitMergedFace()
        {
            List<FaceData> facesMereged = new List<FaceData>();
            if (Faces?.Count > 0)
            {
                while (Faces.Count > 0)
                {
                    Plane p = Faces[0].Plane;
                    var parallelFaces = Faces.Where(x => RevitUtils.IsParallel(x.Plane.Normal, p.Normal)).ToList();
                    if (parallelFaces.Count == 0)
                        break;

                    // merge and take sample plane
                    var data = parallelFaces.GroupBy(x => UtilsPlane.GetSignedDistance(p, x.Plane.Origin), new DistanceEqualityComparer(1e-5))
                                            .ToDictionary(x => x.Key, x => x.ToList());
                    //.ForEach(x => facesMereged.Add(x.Value[0]));
                    foreach (var item in data)
                    {
                        double area = 0;
                        foreach (var face in item.Value)
                        {
                            area += face.Area;
                        }
                        facesMereged.Add(new FaceData() { Area = area, Plane = item.Value.First().Plane });
                    }

                    // remove all merged planes from oroginal list
                    parallelFaces.Select(x => Faces.IndexOf(x))
                                 .OrderByDescending(x => x)
                                 .ForEach(x => Faces.RemoveAt(x));
                }
            }
            Faces = facesMereged;
        }

        //private void InitMergedPlanes()
        //{
        //    MeshPlanes = new List<MeshPlaneData>();
        //    if (Planes?.Count > 0 && Vertices?.Count > 0)
        //    {
        //        var mergedPlanes = MergePlanes(Planes);
        //        foreach (var mp in mergedPlanes)
        //        {
        //            var points = GetPointsOnPlane(mp, Vertices);
        //            MeshPlaneData planeData = new MeshPlaneData(mp, points);
        //            MeshPlanes.Add(planeData);
        //        }
        //    }
        //}

        private void GetIfcSolidData(Solid solid)
        {
            if (solid != null && solid.Faces?.Size > 0)
            {
                var meshes = solid.Faces
                            .Cast<Face>()
                            .Select(x => x.Triangulate())
                            .ToList();
                foreach (var mesh in meshes)
                {
                    GetIfcMeshData(mesh);
                }
            }
        }

        private void GetIfcMeshData(Revit_Mesh mesh)
        {
            if (mesh != null)
            {
                int count = mesh.NumTriangles;
                if (count > MaxVertices)
                {
                    OverMaxVertices = true;
                    return;
                }

                for (int i = 0; i < count; i++)
                {
                    MeshTriangle triangle = mesh.get_Triangle(i);

                    XYZ p0 = triangle.get_Vertex(0);
                    XYZ p1 = triangle.get_Vertex(1);
                    XYZ p2 = triangle.get_Vertex(2);

                    var face = new FaceData(p0, p1, p2);
                    if (face?.Area > 0 && face.Plane != null)
                    {
                        Faces.Add(face);
                    }
                }

                Vertices.AddRange(mesh.Vertices);
            }
        }

        private void GetIfcCurveData(ref GeometryData data, Curve curve)
        {
            if (curve != null)
            {
                //data.Edges.Add(curve);
                data.Vertices.Add(curve.GetEndPoint(0));
                data.Vertices.Add(curve.GetEndPoint(1));
            }
        }

        private List<XYZ> GetPointsOnPlane(Plane plane, List<XYZ> points)
        {
            List<XYZ> ptOnPlane = new List<XYZ>();
            foreach (var point in points)
            {
                double dist = UtilsPlane.GetSignedDistance(plane, point);
                if (RevitUtils.IsEqual(dist, 0, RevitUtils.TOLERANCE))
                    ptOnPlane.Add(point);
            }
            return ptOnPlane;
        }

        private List<Plane> MergePlanes(List<Plane> planes)
        {
            List<Plane> mergedPlanes = new List<Plane>();
            if (planes?.Count > 0)
            {
                while (planes.Count > 0)
                {
                    Plane p = planes[0];
                    var parallelPlanes = planes.Where(x => RevitUtils.IsParallel(x.Normal, p.Normal)).ToList();
                    if (parallelPlanes.Count == 0)
                        break;

                    // merge and take sample plane
                    parallelPlanes.GroupBy(x => UtilsPlane.GetSignedDistance(p, x.Origin), new DistanceEqualityComparer(1e-5))
                                    .ToDictionary(x => x.Key, x => x.ToList())
                                    .ForEach(x => mergedPlanes.Add(x.Value[0]));

                    // remove all merged planes from oroginal list
                    parallelPlanes.Select(x => planes.IndexOf(x))
                                    .OrderByDescending(x => x)
                                    .ForEach(x => planes.RemoveAt(x));
                }
            }
            return mergedPlanes;
        }

        #endregion Extract geometry

        #region Get min, max

        //private void InitPointMinMax()
        //{
        //    if (GetFurthestPoints(Vertices, out XYZ min, out XYZ max))
        //    {
        //        if (min.Z > max.Z)
        //        {
        //            PointMax = min;
        //            PointMin = max;
        //        }
        //        else
        //        {
        //            PointMax = max;
        //            PointMin = min;
        //        }
        //    }
        //    else
        //    {
        //        PointMax = null;
        //        PointMin = null;
        //    }
        //}

        private bool GetFurthestPoints(List<XYZ> points, out XYZ first, out XYZ second)
        {
            first = null;
            second = null;
            if (points?.Count > 0)
            {
                double maxDist = 0;
                for (int i = 0; i < points.Count - 1; i++)
                {
                    for (int j = i + 1; j < points.Count; j++)
                    {
                        double dist = points[i].DistanceTo(points[j]);
                        if (dist > maxDist)
                        {
                            first = points[i];
                            second = points[j];
                            maxDist = dist;
                        }
                    }
                }
            }
            else
            {
                first = null;
                second = null;
            }
            return first != null && second != null;
        }

        #endregion Get min, max
    }
}