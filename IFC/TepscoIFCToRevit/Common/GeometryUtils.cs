using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using RevitUtilities;
using Simplifynet;
using System;
using System.Collections.Generic;
using System.Linq;
using TepscoIFCToRevit.Common.ComparerUtils;
using TepscoIFCToRevit.Data.MEPData;
using Revit_Mesh = Autodesk.Revit.DB.Mesh;
using SympPoint = Simplifynet.Point;

namespace TepscoIFCToRevit.Common
{
    public class GeometryUtils
    {
        #region Common

        public static List<XYZ> GetPointsOnPlane(List<XYZ> points, Plane plane)
        {
            List<XYZ> result = new List<XYZ>();

            if (plane != null && points?.Count > 0)
            {
                foreach (var p in points)
                {
                    if (Math.Abs(UtilsPlane.GetSignedDistance(plane, p)) < RevitUtils.MIN_LENGTH)
                    {
                        result.Add(p);
                    }
                }
            }

            return result;
        }

        public static bool IsTopFace(Plane plane, List<XYZ> points)
        {
            if (plane != null && points != null)
            {
                if (RevitUtils.IsPerpendicular(plane.Normal, XYZ.BasisZ))
                {
                    return false;
                }

                foreach (var point in points)
                {
                    XYZ project = UtilsPlane.ProjectOnto(plane, point);
                    XYZ vector = point - project;
                    if (!RevitUtils.IsEqual(vector, XYZ.Zero) && vector.AngleTo(XYZ.BasisZ.Negate()) > Math.PI / 2)
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }

        public static bool? IsCircle(List<XYZ> points, XYZ center, double part = 10)
        {
            if (points?.Count > 3 && center != null)
            {
                int count = points.Count;
                double distance = center.DistanceTo(points.First());

                if (points.Any(x => !RevitUtils.IsEqual(x.DistanceTo(center), distance, distance / part)))
                {
                    return false;
                }
                if (count >= 6)
                {
                    for (int i = 0; i < count; i++)
                    {
                        XYZ pI = points[i];
                        XYZ pBefor = i == 0 ? points[count - 1] : points[i - 1];
                        XYZ mid = (pI + pBefor) / 2;
                        if (!RevitUtils.IsEqual(mid.DistanceTo(center), distance, 2 * distance / part))
                        {
                            return null;
                        }
                    }
                    return true;
                }

                return null;
            }

            return false;
        }

        /// <summary>
        /// Find mirror of list points
        /// </summary>
        ///
        public static List<XYZ> SimplePoints(Plane plane, IEnumerable<XYZ> points, double tolerance = RevitUtils.MIN_LENGTH)
        {
            List<XYZ> pointSimples = new List<XYZ>();

            // change standard point to UV point
            foreach (XYZ p in points)
            {
                plane.Project(p, out UV uv, out double d);
                XYZ point = new XYZ(uv.U, uv.V, 0);
                if (pointSimples.Any(x => RevitUtils.IsEqual(x, point)))
                {
                    continue;
                }
                pointSimples.Add(point);
            }

            // use class simplelify and convexhull to remove junk point
            pointSimples = ConvexHull.GetConvexHull(pointSimples);
            List<SympPoint> simplys = new List<SympPoint>();
            foreach (XYZ point in pointSimples)
            {
                SympPoint simpPoint = new SympPoint(point.X, point.Y);
                simplys.Add(simpPoint);
            }
            SimplifyUtility simplifyUtility = new SimplifyUtility();
            simplys = simplifyUtility.Simplify(simplys.ToArray(), tolerance, false);

            List<XYZ> pointSectionHorizons = new List<XYZ>();
            foreach (var point in simplys)
            {
                XYZ pointStander = RevitUtils.TransformPointToStandardCoordinate(plane, new UV(point.X, point.Y));
                if (pointSectionHorizons?.Count == 0)
                    pointSectionHorizons.Add(pointStander);
                else if (!pointSectionHorizons.Any(x => RevitUtils.IsEqual(x, pointStander)))
                    pointSectionHorizons.Add(pointStander);
            }

            pointSimples.Clear();
            int count = pointSectionHorizons.Count;
            if (count > 3)
            {
                for (int i = 0; i < count; i++)
                {
                    XYZ pi = pointSectionHorizons[i];
                    XYZ pBefor = pointSimples.Count == 0 ? pointSectionHorizons[count - 1] : pointSimples.Last();
                    XYZ pNext = i == count - 1 ? pointSimples.Count == 0 ? pointSectionHorizons[0] : pointSimples.First() : pointSectionHorizons[i + 1];

                    if (!RevitUtils.IsEqual(pi.DistanceTo(pBefor) + pi.DistanceTo(pNext), pNext.DistanceTo(pBefor)))
                    {
                        pointSimples.Add(pi);
                    }
                }
            }

            return pointSimples;
        }

        public static List<GeometryObject> GetIfcGeometriess(Element elemInLinkedDoc)
        {
            List<GeometryObject> geometries = new List<GeometryObject>();
            if (elemInLinkedDoc != null)
            {
                Options options = new Options()
                {
                    IncludeNonVisibleObjects = true,
                    //View = null,
                    ComputeReferences = true,
                    DetailLevel = ViewDetailLevel.Fine,
                };

                GeometryElement geo = elemInLinkedDoc.get_Geometry(options);
                GetGeometries(ref geometries, geo);
            }
            return geometries;
        }

        public static void GetGeometries(ref List<GeometryObject> geometries, GeometryObject geo)
        {
            if (geo is GeometryElement geoElem)
            {
                foreach (var obj in geoElem)
                    GetGeometries(ref geometries, obj);
            }
            else if (geo is GeometryInstance geoInst)
            {
                foreach (var obj in geoInst.GetInstanceGeometry())
                    GetGeometries(ref geometries, obj);
            }
            else if (geo is Solid || geo is Revit_Mesh || geo is Curve)
                geometries.Add(geo);
        }

        /// <summary>
        /// Get box extend of element link
        /// </summary>
        public static BoundingBoxXYZ GetBoudingBoxExtend(Element elem, Transform transform, double extend = RevitUtilities.Common.MIN_LENGTH)
        {
            BoundingBoxXYZ boxOld = elem.get_BoundingBox(null);
            if (boxOld != null)
            {
                XYZ max = boxOld.Max;
                XYZ min = boxOld.Min;

                var list = new List<XYZ>()
                {
                    min,
                    new XYZ(max.X, min.Y, min.Z),
                    new XYZ(max.X, max.Y, min.Z),
                    new XYZ(min.X, max.Y, min.Z),

                    new XYZ(min.X, min.Y, max.Z),
                    new XYZ(max.X, min.Y, max.Z),
                    max,
                    new XYZ(min.X, max.Y, max.Z),
                };

                if (transform != null)
                {
                    list = list.ConvertAll(x => transform.OfPoint(x));
                }

                double maxX = list.Select(p => p.X).Max();
                double maxY = list.Select(p => p.Y).Max();
                double maxZ = list.Select(p => p.Z).Max();

                double minX = list.Select(p => p.X).Min();
                double minY = list.Select(p => p.Y).Min();
                double minZ = list.Select(p => p.Z).Min();

                max = new XYZ(maxX, maxY, maxZ);
                min = new XYZ(minX, minY, minZ);

                XYZ direction = (max - min).Normalize();

                BoundingBoxXYZ newBox = new BoundingBoxXYZ
                {
                    Min = min - extend * direction,
                    Max = max + extend * direction
                };

                return newBox;
            }
            else
                return null;
        }

        /// <summary>
        /// Get box extend of element link
        /// </summary>
        public static BoundingBoxXYZ GetBoudingBox(BoundingBoxXYZ boxOld, Transform transform = null, double extend = RevitUtilities.Common.MIN_LENGTH)
        {
            if (boxOld == null)
            {
                return null;
            }

            Transform transSolid = boxOld.Transform;
            XYZ max = boxOld.Max;
            XYZ min = boxOld.Min;

            if (transSolid != null)
            {
                max = transSolid.OfPoint(boxOld.Max);
                min = transSolid.OfPoint(boxOld.Min);
            }

            if (transform != null)
            {
                max = transform.OfPoint(max);
                min = transform.OfPoint(min);
            }

            //note: transform can make boundingbox is not a outline ( max coordinate is not greater than min coordinate)
            double maxX = Math.Max(max.X, min.X);
            double maxY = Math.Max(max.Y, min.Y);
            double maxZ = Math.Max(max.Z, min.Z);

            double minX = Math.Min(max.X, min.X);
            double minY = Math.Min(max.Y, min.Y);
            double minZ = Math.Min(max.Z, min.Z);

            max = new XYZ(maxX, maxY, maxZ);
            min = new XYZ(minX, minY, minZ);

            XYZ direction = (max - min).Normalize();

            BoundingBoxXYZ newBox = new BoundingBoxXYZ
            {
                Min = min - extend * direction,
                Max = max + extend * direction
            };

            return newBox;
        }

        #endregion Common

        #region Cylinder

        /// <summary>
        /// Get location line of Cylinder
        /// </summary>
        public static Line GetLocationCylinder(Element elem, out List<Line> geometry, bool isDuct = false)
        {
            geometry = new List<Line>();
            try
            {
                Line location = null;
                XYZ centerPoint = null;
                XYZ faceNormal = null;

                List<Plane> planes = new List<Plane>();
                List<Solid> solids = UtilsSolid.GetAllSolids(elem);
                if (solids.Count > 0)
                {
                    List<Plane> planesSolid = GetStartEndFaceCylinder(solids, ref faceNormal, ref centerPoint, ref geometry);
                    if (planesSolid.Count > 0)
                    {
                        planes.AddRange(planesSolid);
                    }
                }

                List<Mesh> meshs = GeometryUtils.GetIfcGeometriess(elem)
                           .Where(x => x is Mesh)
                           .Cast<Mesh>()
                           .ToList();

                if (meshs.Count > 0)
                {
                    if (isDuct && centerPoint == null)
                    {
                        BoundingBoxXYZ box = elem.get_BoundingBox(null);
                        if (box != null)
                        {
                            centerPoint = (box.Min + box.Max) / 2;
                        }
                    }
                    List<Plane> planesMesh = GetStartEndFaceCylinder(meshs, ref faceNormal, ref centerPoint, ref geometry);
                    if (planesMesh.Count > 0)
                    {
                        planes.AddRange(planesMesh);
                    }
                }

                if (planes.Count > 1 && faceNormal != null && centerPoint != null)
                {
                    List<XYZ> pointCenters = planes.Select(x => UtilsPlane.ProjectOnto(x, centerPoint)).ToList();
                    RevitUtilities.Common.SortPointsToDirection(pointCenters, faceNormal);
                    if (pointCenters.First().DistanceTo(pointCenters.Last()) > RevitUtilities.Common.MIN_LENGTH)
                    {
                        location = Line.CreateBound(pointCenters.First(), pointCenters.Last());
                    }
                }

                return location;
            }
            catch (Exception)
            { }
            return null;
        }

        /// <summary>
        /// Get start and end face of Cylinder with meshes
        /// </summary>
        private static List<Plane> GetStartEndFaceCylinder(List<Mesh> meshs, ref XYZ faceNormal, ref XYZ centerPoint, ref List<Line> geometry)
        {
            List<Plane> planes = new List<Plane>();
            foreach (Mesh mesh in meshs)
            {
                List<Plane> planesMesh = new List<Plane>();
                IList<XYZ> vertices = mesh.Vertices;
                int number = vertices.Count;
                if (number <= 8)
                    continue;

                double tolerance = 5.0e-4;
                if (faceNormal != null)
                {
                    foreach (XYZ point in vertices)
                    {
                        Plane plane = Plane.CreateByNormalAndOrigin(faceNormal, point);
                        if (!RevitUtilities.Common.IsDuplicate(plane, planesMesh))
                        {
                            planesMesh.Add(plane);
                        }

                        if (planesMesh.Count == 2)
                        {
                            break;
                        }
                    }
                }
                else
                {
                    List<XYZ> pointsCheck = new List<XYZ>();
                    List<Plane> planesCheck = FindPlaneCheckMesh(ref pointsCheck, vertices);
                    if (planesCheck.Count > 0)
                    {
                        List<XYZ> pointsFind = new List<XYZ>();
                        List<XYZ> pointsOnPlane = vertices.Where(x => UtilsPlane.IsOnPlane(x, planesCheck, tolerance)).ToList();
                        int count = pointsOnPlane.Count();
                        if (count >= number / 2 && count > 3)
                        {
                            pointsFind.AddRange(pointsOnPlane);
                        }
                        else
                        {
                            List<XYZ> pointsMax = new List<XYZ>();
                            List<XYZ> pointsOrder = SortPointCheck(pointsCheck);
                            pointsCheck.Clear();
                            pointsCheck.AddRange(pointsOrder);
                            do
                            {
                                planesCheck.Clear();
                                planesCheck = FindPlaneCheckMesh(ref pointsCheck, vertices);
                                if (planesCheck.Count > 0)
                                {
                                    foreach (Plane plane in planesCheck)
                                    {
                                        pointsOnPlane = vertices.Where(x => UtilsPlane.IsOnPlane(x, plane, tolerance)).ToList();
                                        count = pointsOnPlane.Count();
                                        if (count >= number / 2 && count > 3)
                                        {
                                            pointsFind.AddRange(pointsOnPlane);
                                            break;
                                        }
                                        if (pointsOnPlane.Count > pointsMax.Count)
                                        {
                                            pointsMax.Clear();
                                            pointsMax.AddRange(pointsOnPlane);
                                        }
                                    }
                                }
                            } while (pointsFind.Count == 0 && planesCheck.Count > 0);
                            if (pointsFind.Count == 0 && pointsMax.Count > 0)
                            {
                                pointsFind.AddRange(pointsMax);
                            }
                        }

                        Plane planeFind = FindPlane(pointsFind, out XYZ centerFind);
                        if (planeFind != null)
                        {
                            faceNormal = planeFind.Normal;
                            planesMesh.Add(planeFind);

                            List<XYZ> pointsNegate = vertices.Where(x => !pointsFind.Contains(x)).OrderBy(x => Math.Abs(UtilsPlane.GetSignedDistance(planeFind, x))).ToList();
                            if (pointsNegate.Count() > 0)
                            {
                                Plane plane = Plane.CreateByNormalAndOrigin(faceNormal, pointsNegate.Last());

                                count = pointsNegate.Where(x => UtilsPlane.IsOnPlane(x, plane, tolerance)).Count();
                                if (count >= pointsFind.Count())
                                {
                                    if (centerPoint == null)
                                    {
                                        centerPoint = centerFind;
                                    }

                                    planesMesh.Add(plane);
                                }
                                else
                                {
                                    planesMesh.Clear();
                                    planeFind = FindPlane(pointsNegate, out centerFind);
                                    if (planeFind != null)
                                    {
                                        if (centerPoint == null)
                                        {
                                            centerPoint = centerFind;
                                        }

                                        faceNormal = planeFind.Normal;
                                        planesMesh.Add(planeFind);

                                        XYZ point = pointsFind.OrderBy(x => Math.Abs(UtilsPlane.GetSignedDistance(planeFind, x))).Last();
                                        plane = Plane.CreateByNormalAndOrigin(faceNormal, point);
                                        planesMesh.Add(plane);
                                    }
                                }
                            }
                        }
                    }
                }
                if (planesMesh.Count == 2)
                {
                    planes.AddRange(planesMesh);

                    Plane plane = planesMesh[0];
                    XYZ center = UtilsPlane.ProjectOnto(plane, centerPoint);
                    List<XYZ> points = vertices.Where(x => UtilsPlane.IsOnPlane(x, plane, tolerance)).ToList();
                    int count = points.Count;
                    if (count > 2)
                    {
                        for (int i = 0; i < count; i++)
                        {
                            XYZ start = i == 0 ? points[count - 1] : points[i - 1];
                            XYZ vecter = points[i] - start;

                            if (vecter.GetLength() < RevitUtils.MIN_LENGTH)
                            {
                                continue;
                            }

                            Line line = Line.CreateUnbound(start, vecter);
                            var pj = line.Project(center);
                            if (pj.Distance < RevitUtils.MIN_LENGTH || geometry.Any(x => RevitUtils.IsEqual(x.Length, pj.Distance, RevitUtils.MIN_LENGTH)))
                            {
                                continue;
                            }
                            geometry.Add(Line.CreateBound(center, pj.XYZPoint));
                        }
                    }
                }
            }
            return planes;
        }

        /// <summary>
        /// Find plane from list point
        /// </summary>
        private static Plane FindPlane(List<XYZ> points, out XYZ centerFind)
        {
            Plane plane = null;
            centerFind = null;
            try
            {
                if (points?.Count > 2)
                {
                    XYZ point0 = points[0];
                    XYZ point1 = points.OrderBy(x => x.DistanceTo(point0)).Last();
                    XYZ point2 = points.OrderBy(x => Math.Abs(x.DistanceTo(point0) - x.DistanceTo(point1))).First();

                    plane = Plane.CreateByThreePoints(point0, point1, point2);
                    Arc arc = Arc.Create(point0, point1, point2);
                    centerFind = arc.Center;
                }
            }
            catch (Exception) { }
            return plane;
        }

        /// <summary>
        /// Find center point of Cylinder
        /// </summary>
        private static XYZ FindCenterCylinder(Solid solid, XYZ normal)
        {
            XYZ center = null;
            try
            {
                BoundingBoxXYZ box = solid.GetBoundingBox();
                Transform transform = box.Transform;
                XYZ mid = (box.Min + box.Max) / 2;
                center = transform.OfPoint(mid);

                Plane plane0 = Plane.CreateByNormalAndOrigin(normal, center - 0.5 * RevitUtilities.Common.MIN_LENGTH * normal);
                Plane plane1 = Plane.CreateByNormalAndOrigin(normal.Negate(), center + 0.5 * RevitUtilities.Common.MIN_LENGTH * normal);
                Solid solidCheck = BooleanOperationsUtils.CutWithHalfSpace(solid, plane0);

                if (solidCheck != null)
                {
                    BooleanOperationsUtils.CutWithHalfSpaceModifyingOriginalSolid(solidCheck, plane1);
                    box = solidCheck.GetBoundingBox();
                    transform = box.Transform;
                    mid = (box.Min + box.Max) / 2;
                    center = transform.OfPoint(mid);
                }
            }
            catch (Exception) { }
            return center;
        }

        /// <summary>
        /// Sort point check to lenth
        /// </summary>
        private static List<XYZ> SortPointCheck(List<XYZ> pointsCheck)
        {
            List<XYZ> sort = new List<XYZ>();
            if (pointsCheck?.Count == 3)
            {
                Dictionary<XYZ, double> dic = new Dictionary<XYZ, double>();
                foreach (XYZ point in pointsCheck)
                {
                    double length = pointsCheck.Select(x => x.DistanceTo(point)).Max();
                    dic.Add(point, length);
                }
                sort.AddRange(dic.OrderBy(x => x.Value).Select(item => item.Key));
            }
            return sort;
        }

        /// <summary>
        /// Find plane to check mesh
        /// </summary>
        private static List<Plane> FindPlaneCheckMesh(ref List<XYZ> pointsCheck, IList<XYZ> vertices)
        {
            List<Plane> planes = new List<Plane>();
            foreach (XYZ point in vertices)
            {
                if (!RevitUtilities.Common.IsDuplicate(point, pointsCheck))
                {
                    if (pointsCheck.Count > 2)
                    {
                        pointsCheck.Add(point);
                        try
                        {
                            planes.Add(Plane.CreateByThreePoints(point, pointsCheck[0], pointsCheck[1]));
                            planes.Add(Plane.CreateByThreePoints(point, pointsCheck[0], pointsCheck[2]));
                        }
                        catch (Exception) { }
                        if (planes.Count > 0)
                            break;
                    }
                    else if (pointsCheck.Count == 2)
                    {
                        try
                        {
                            planes.Add(Plane.CreateByThreePoints(point, pointsCheck[0], pointsCheck[1]));
                            pointsCheck.Add(point);
                            break;
                        }
                        catch (Exception) { }
                    }
                    else
                    {
                        pointsCheck.Add(point);
                    }
                }
            }
            return planes;
        }

        /// <summary>
        /// Get start and end face of Cylinder with solids
        /// </summary>
        public static List<Plane> GetStartEndFaceCylinder(List<Solid> solids, ref XYZ faceNormal, ref XYZ centerPoint, ref List<Line> geometry)
        {
            List<Plane> planes = new List<Plane>();
            foreach (Solid solid in solids)
            {
                List<PlanarFace> planarFaces = UtilsPlane.GetPlanarFaceSolid(solid);

                if (planarFaces.Count == 6)
                {
                    planarFaces = planarFaces.OrderBy(item => item.Area).ToList();
                    planarFaces.RemoveAt(0);
                    planarFaces.RemoveAt(0);
                }
                else
                {
                    planarFaces = planarFaces.Where(x => x.EdgeLoops.Size > 0 && x.EdgeLoops.get_Item(0).Size < 5).ToList();

                    if (planarFaces.Count > 10)
                    {
                        planarFaces = planarFaces.Where(item => item.Area > 9e-5).ToList();
                    }
                }

                if (faceNormal == null)
                {
                    faceNormal = FindCylinderNormal(planarFaces);
                }

                if (faceNormal != null)
                {
                    if (centerPoint == null)
                    {
                        centerPoint = FindCenterCylinder(solid, faceNormal);
                    }

                    PlanarFace planarFace = planarFaces.OrderBy(x => x.Area).LastOrDefault();
                    IList<Line> lines = UtilsPlane.GetLinesOfFace(planarFace);
                    Line lineMax = lines.LastOrDefault();
                    if (lineMax != null)
                    {
                        planes.Add(Plane.CreateByNormalAndOrigin(faceNormal, lineMax.GetEndPoint(0)));
                        planes.Add(Plane.CreateByNormalAndOrigin(faceNormal, lineMax.GetEndPoint(1)));
                    }

                    XYZ project = planarFace.Project(centerPoint)?.XYZPoint;
                    if (centerPoint.DistanceTo(project) > RevitUtils.MIN_LENGTH)
                    {
                        geometry.Add(Line.CreateBound(centerPoint, project));
                    }

                    foreach (var face in planarFaces)
                    {
                        var pj = face.Project(centerPoint);
                        if (pj != null)
                        {
                            if (pj.Distance < RevitUtils.MIN_LENGTH || geometry.Any(x => RevitUtils.IsEqual(x.Length, pj.Distance, RevitUtils.MIN_LENGTH)))
                            {
                                continue;
                            }
                            geometry.Add(Line.CreateBound(centerPoint, pj.XYZPoint));
                        }
                    }
                }
            }
            return planes;
        }

        /// <summary>
        /// Find Cylinder direction
        /// </summary>
        public static XYZ FindCylinderNormal(List<PlanarFace> planarFaces)
        {
            XYZ findNormal = null;

            int count = planarFaces.Count();
            if (count > 3)
            {
                for (int i = 0; i < count; i++)
                {
                    XYZ normal = null;
                    if (i == count - 1)
                    {
                        if (!RevitUtilities.Common.IsParallel(planarFaces[i].FaceNormal, planarFaces[0].FaceNormal))
                        {
                            normal = planarFaces[i].FaceNormal.CrossProduct(planarFaces[0].FaceNormal);
                        }
                    }
                    else
                    {
                        if (!RevitUtilities.Common.IsParallel(planarFaces[i].FaceNormal, planarFaces[i + 1].FaceNormal))
                        {
                            normal = planarFaces[i].FaceNormal.CrossProduct(planarFaces[i + 1].FaceNormal);
                        }
                    }
                    if (normal != null)
                    {
                        if (findNormal != null)
                        {
                            if (findNormal.AngleTo(normal) > Math.PI / 2)
                                findNormal += normal.Negate();
                            else
                                findNormal += normal;
                        }
                        else
                            findNormal = normal;
                    }
                }
            }
            return findNormal;
        }

        #endregion Cylinder

        /// <summary>
        /// Get all mesh of element
        /// </summary>
        public static List<Mesh> GetAllMeshes(Element elem, bool getInstGeo)
        {
            List<Mesh> meshes = new List<Mesh>();
            Options options = new Options();
            GeometryElement geoElem = elem.get_Geometry(options);
            GetMeshFromGeometry(elem.Document, geoElem, getInstGeo, ref meshes);

            return meshes;
        }

        /// <summary>
        /// Recursively get mesh from geometry element
        /// </summary>
        public static void GetMeshFromGeometry(Document doc, GeometryElement geoElem, bool getInstGeo, ref List<Mesh> meshs)
        {
            foreach (GeometryObject geoObj in geoElem)
            {
                if (geoObj is Mesh mesh)
                {
                    meshs.Add(mesh);
                }
                else if (geoObj is GeometryInstance geoInst)
                {
                    GeometryElement innerGeo = getInstGeo ? geoInst.GetInstanceGeometry() : geoInst.GetSymbolGeometry();
                    GetMeshFromGeometry(doc, innerGeo, getInstGeo, ref meshs);
                }
            }
        }

        #region Fitting

        /// Find pipe has end in box
        /// </summary>
        public static List<Pipe> FindPipeNearestBox(Document doc, List<ElementId> pipeIds, BoundingBoxXYZ box, bool isCheckConnect = true)
        {
            List<Pipe> pipes = new List<Pipe>();
            if (pipeIds?.Count > 0 && box != null)
            {
                BoundingBoxIntersectsFilter boxFilter = GetBoxFilter(box);
                pipes = new FilteredElementCollector(doc, pipeIds).WherePasses(boxFilter)
                                                                             .Where(x => IsEndElementInBox(x, box))
                                                                             .Cast<Pipe>()
                                                                             .ToList();
                if (pipes.Count() > 1)
                {
                    Pipe pipe = pipes.First();
                    pipes = pipes.Where(x => x.PipeType.Id.Equals(pipe.PipeType.Id) && (!isCheckConnect || CommonDataPipeDuct.ValidateConnected(x, box))).ToList();
                }
            }
            return pipes;
        }

        public static List<Element> FindPipeDuctNearestBox(Document doc, List<ElementId> elementIds, BoundingBoxXYZ box)
        {
            List<Element> elements = new List<Element>();
            if (elementIds?.Count > 0 && box != null)
            {
                BoundingBoxIntersectsFilter boxFilter = GetBoxFilter(box);
                elements = new FilteredElementCollector(doc, elementIds).WherePasses(boxFilter)
                                                                        .Where(x => IsEndElementInBox(x, box))
                                                                        .Where(x => x is Pipe || x is Duct)
                                                                        .Cast<Element>()
                                                                        .ToList();
            }
            return elements;
        }

        public static BoundingBoxIntersectsFilter GetBoxFilter(BoundingBoxXYZ box)
        {
            Outline outline = new Outline(box.Min, box.Max);
            BoundingBoxIntersectsFilter boxFilter = new BoundingBoxIntersectsFilter(outline);
            return boxFilter;
        }

        public static List<Duct> FindDuctNearestBox(Document doc, List<ElementId> pipeIds, BoundingBoxXYZ box, bool isCheckConnect = true)
        {
            /// Case 1: found 2 pipes
            /// Case 2; dound 1 pipe and a elbow fitting
            /// This code be low for Case 1

            List<Duct> ducts = new List<Duct>();
            if (pipeIds?.Count > 0 && box != null)
            {
                Outline outline = new Outline(box.Min, box.Max);

                box = new BoundingBoxXYZ()
                {
                    Max = outline.MaximumPoint,
                    Min = outline.MinimumPoint,
                };

                BoundingBoxIntersectsFilter boxFilter = new BoundingBoxIntersectsFilter(outline);
                ducts = new FilteredElementCollector(doc, pipeIds).WherePasses(boxFilter)
                                                                            .Where(x => IsEndElementInBox(x, box))
                                                                            .Cast<Duct>()
                                                                            .ToList();
                if (ducts.Count() > 1)
                {
                    Duct pipe = ducts.First();
                    ducts = ducts.Where(x => x.DuctType.Id.Equals(pipe.DuctType.Id) && (!isCheckConnect || CommonDataPipeDuct.ValidateConnected(x, box))).ToList();
                }
            }
            return ducts;
        }

        public static List<CableTray> FindCableTrayNearestBox(Document doc, List<ElementId> cableTrayIds, BoundingBoxXYZ box)
        {
            /// Case 1: found 2 pipes
            /// Case 2; dound 1 pipe and a elbow fitting
            /// This code be low for Case 1

            List<CableTray> cableTrays = new List<CableTray>();
            if (cableTrayIds?.Count > 0 && box != null)
            {
                Outline outline = new Outline(box.Min, box.Max);

                box = new BoundingBoxXYZ()
                {
                    Max = outline.MaximumPoint,
                    Min = outline.MinimumPoint,
                };

                BoundingBoxIntersectsFilter boxFilter = new BoundingBoxIntersectsFilter(outline);
                cableTrays = new FilteredElementCollector(doc, cableTrayIds).WherePasses(boxFilter)
                                                                            .Where(x => IsEndElementInBox(x, box))
                                                                            .Cast<CableTray>()
                                                                            .ToList();
            }
            return cableTrays;
        }

        /// <summary>
        /// Check end pipe in box
        /// </summary>
        public static bool IsEndElementInBox(Element element, BoundingBoxXYZ box)
        {
            bool isInBox = false;
            if (element != null && element.IsValidObject && element.Location is LocationCurve lcCurve && lcCurve.Curve is Line lcLine)
            {
                if (IsPointInBox(lcLine.GetEndPoint(0), box) &&
                    IsPointInBox(lcLine.GetEndPoint(1), box))
                    isInBox = false;
                else if (IsPointInBox(lcLine.GetEndPoint(0), box) ||
                     IsPointInBox(lcLine.GetEndPoint(1), box))
                {
                    isInBox = true;
                }
            }

            return isInBox;
        }

        /// <summary>
        /// Check point in box
        /// </summary>
        public static bool IsPointInBox(XYZ point, BoundingBoxXYZ box)
        {
            bool isInBox = false;

            if (point != null && box != null)
            {
                XYZ min = box.Min;
                XYZ max = box.Max;

                if (point.X > min.X && point.X < max.X
                    && point.Y > min.Y && point.Y < max.Y
                    && point.Z > min.Z && point.Z < max.Z)
                {
                    isInBox = true;
                }
            }

            return isInBox;
        }

        /// <summary>
        /// Create pipe connector
        /// </summary>
        public static FamilyInstance CreatePipeConnector(Pipe pipe1, Pipe pipe2, out Connector pipeConnector1, out Connector pipeConnector2)
        {
            FamilyInstance fitting = null;
            pipeConnector1 = null;
            pipeConnector2 = null;
            try
            {
                ConnectorManager manager1 = pipe1.ConnectorManager;
                ElementId pipeTypeId1 = pipe1.GetTypeId();
                Connector start1 = manager1.Lookup(0);
                Connector end1 = manager1.Lookup(1);

                ConnectorManager manager2 = pipe2.ConnectorManager;
                ElementId pipeTypeId2 = pipe2.GetTypeId();
                Connector start2 = manager2.Lookup(0);
                Connector end2 = manager2.Lookup(1);

                if (pipeTypeId1.Equals(pipeTypeId2))
                {
                    double d1 = start2.Origin.DistanceTo(start1.Origin);
                    double d2 = start2.Origin.DistanceTo(end1.Origin);
                    double d3 = start1.Origin.DistanceTo(start2.Origin);
                    double d4 = start1.Origin.DistanceTo(end2.Origin);

                    Connector connector1 = end1;
                    if (d1 < d2)
                    {
                        connector1 = start1;
                    }

                    Connector connector2 = start2;
                    if (d4 < d3)
                    {
                        connector2 = end2;
                    }

                    fitting = pipe1.Document.Create.NewElbowFitting(connector1, connector2);

                    pipeConnector1 = connector1;
                    pipeConnector2 = connector2;
                }
            }
            catch (Exception)
            {
            }

            return fitting;
        }

        /// <summary>
        /// Create pipe connector
        /// </summary>
        public static FamilyInstance CreateDuctConnector(Duct duct1, Duct duct2, out Connector connector1, out Connector connector2)
        {
            FamilyInstance fitting = null;
            connector1 = null;
            connector2 = null;
            try
            {
                ConnectorManager manager1 = duct1.ConnectorManager;
                ElementId ductTypeId1 = duct1.GetTypeId();
                Connector start1 = manager1.Lookup(0);
                Connector end1 = manager1.Lookup(1);

                ConnectorManager manager2 = duct2.ConnectorManager;
                ElementId ductTypeId2 = duct2.GetTypeId();
                Connector start2 = manager2.Lookup(0);
                Connector end2 = manager2.Lookup(1);

                if (ductTypeId1.Equals(ductTypeId2))
                {
                    double d1 = start2.Origin.DistanceTo(start1.Origin);
                    double d2 = start2.Origin.DistanceTo(end1.Origin);
                    double d3 = start1.Origin.DistanceTo(start2.Origin);
                    double d4 = start1.Origin.DistanceTo(end2.Origin);

                    connector1 = end1;
                    if (d1 < d2)
                    {
                        connector1 = start1;
                    }

                    connector2 = start2;
                    if (d4 < d3)
                    {
                        connector2 = end2;
                    }

                    fitting = duct1.Document.Create.NewElbowFitting(connector1, connector2);
                }
            }
            catch (Exception) { }

            return fitting;
        }

        public static FamilyInstance CreateCableTrayConnector(CableTray cable1, CableTray cable2, out Connector connector1, out Connector connector2)
        {
            FamilyInstance fitting = null;
            connector1 = null;
            connector2 = null;
            try
            {
                ConnectorManager manager1 = cable1.ConnectorManager;
                ElementId ductTypeId1 = cable1.GetTypeId();
                Connector start1 = manager1.Lookup(0);
                Connector end1 = manager1.Lookup(1);

                ConnectorManager manager2 = cable2.ConnectorManager;
                ElementId ductTypeId2 = cable2.GetTypeId();
                Connector start2 = manager2.Lookup(0);
                Connector end2 = manager2.Lookup(1);

                if (ductTypeId1.Equals(ductTypeId2))
                {
                    double d1 = start2.Origin.DistanceTo(start1.Origin);
                    double d2 = start2.Origin.DistanceTo(end1.Origin);
                    double d3 = start1.Origin.DistanceTo(start2.Origin);
                    double d4 = start1.Origin.DistanceTo(end2.Origin);

                    connector1 = end1;
                    if (d1 < d2)
                    {
                        connector1 = start1;
                    }

                    connector2 = start2;
                    if (d4 < d3)
                    {
                        connector2 = end2;
                    }

                    Document doc = cable1.Document;
                    fitting = doc.Create.NewElbowFitting(connector1, connector2);
                }
            }
            catch (Exception) { }

            return fitting;
        }

        /// <summary>
        /// Create pipe connector
        /// </summary>
        public static FamilyInstance CreateMepConnector(MEPCurve mep1, MEPCurve mep2, out Connector mepConnector1, out Connector mepConnector2)
        {
            FamilyInstance fitting = null;
            mepConnector1 = null;
            mepConnector2 = null;
            try
            {
                ConnectorManager manager1 = mep1.ConnectorManager;
                ElementId ductTypeId1 = mep1.GetTypeId();
                Connector start1 = manager1.Lookup(0);
                Connector end1 = manager1.Lookup(1);

                ConnectorManager manager2 = mep2.ConnectorManager;
                ElementId ductTypeId2 = mep2.GetTypeId();
                Connector start2 = manager2.Lookup(0);
                Connector end2 = manager2.Lookup(1);

                if (ductTypeId1.Equals(ductTypeId2))
                {
                    double d1 = start2.Origin.DistanceTo(start1.Origin);
                    double d2 = start2.Origin.DistanceTo(end1.Origin);
                    double d3 = start1.Origin.DistanceTo(start2.Origin);
                    double d4 = start1.Origin.DistanceTo(end2.Origin);

                    Connector connector1 = end1;
                    if (d1 < d2)
                    {
                        connector1 = start1;
                    }

                    Connector connector2 = start2;
                    if (d4 < d3)
                    {
                        connector2 = end2;
                    }

                    fitting = mep1.Document.Create.NewElbowFitting(connector1, connector2);

                    mepConnector1 = connector1;
                    mepConnector2 = connector2;
                }
            }
            catch (Exception)
            {
            }

            return fitting;
        }

        #endregion Fitting

        #region Check Shape Fitting T or Y

        public static bool IsNotTouchSolids(Line lcLine0, Line lcLine1, Line lcLine2)
        {
            // check case pipe touch pipes other but not intersect
            List<XYZ> lcPoints = new List<XYZ>
            {
                lcLine0.GetEndPoint(0),
                lcLine0.GetEndPoint(1),
                lcLine1.GetEndPoint(0),
                lcLine1.GetEndPoint(1),
                lcLine2.GetEndPoint(0),
                lcLine2.GetEndPoint(1)
            };

            List<XYZ> distinPoints = lcPoints.Distinct(new PointEqualityComparer(RevitUtils.TOLERANCE)).ToList();
            if (RevitUtils.IsEqual(lcPoints.Count, distinPoints.Count))
                return true;
            return false;
        }

        public static bool IsIntersectSolid(List<Element> elements)
        {
            // check solid of pipe has intersect
            List<Solid> solids = new List<Solid>();
            foreach (Element el in elements)
            {
                List<GeometryObject> geometryObjs = GeometryUtils.GetIfcGeometriess(el);
                if (geometryObjs?.Count > 0)
                {
                    foreach (GeometryObject geometry in geometryObjs)
                    {
                        if (geometry != null && geometry is Solid solid)
                            solids.Add(solid);
                    }
                }
            }

            if (solids?.Count > 0)
            {
                for (int i = 0; i < solids.Count - 1; i++)
                {
                    for (int j = i + 1; j < solids.Count; j++)
                    {
                        Solid interSolid = BooleanOperationsUtils.ExecuteBooleanOperation(solids[i], solids[j], BooleanOperationsType.Intersect);
                        if (interSolid != null && interSolid.Volume > 0)
                            return true;
                    }
                }
            }
            return false;
        }

        #endregion Check Shape Fitting T or Y
    }
}