using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using RevitUtilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TepscoIFCToRevit.Common.ComparerUtils;
using TepscoIFCToRevit.Data;
using RevitView = Autodesk.Revit.DB.View;

namespace TepscoIFCToRevit.Common
{
    /// <summary>
    /// class to handle genric Revit operation (pick objects, get faces....)
    /// </summary>
    ///
    public static class RevitUtils
    {
        public const double TOLERANCE = 1.0e-5;
        public const double ANGLE_TOLERANCE = 5.0e-3;
        public const double MIN_LENGTH = 1 / 304.8;
        public const double MM_TO_FT = 0.0032808399;
        public const double PERMISSIBLE_RATE = 65 / 100;

        #region Compare

        public static bool IsBetween(XYZ point, XYZ first, XYZ second, double tolerance = MIN_LENGTH)
        {
            double d = point.DistanceTo(first) + point.DistanceTo(second);
            return Math.Abs(d - first.DistanceTo(second)) < tolerance;
        }

        /// <summary>
        /// determine if 2 float point numbers are almost euqal within a tolerance range
        /// </summary>
        public static bool IsEqual(double first, double second, double tolerance = TOLERANCE)
        {
            double result = Math.Abs(first - second);
            return result <= tolerance;
        }

        /// <summary>
        /// determine if 2 vectors/ points are almost equal within a toelrance range
        /// </summary>
        public static bool IsEqual(XYZ first, XYZ second, double tolerance = TOLERANCE)
        {
            return IsEqual(first.X, second.X, tolerance)
                && IsEqual(first.Y, second.Y, tolerance)
                && IsEqual(first.Z, second.Z, tolerance);
        }

        /// <summary>
        /// determine if the first float point number is less than the
        /// second float point number within a tolerance range
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        public static bool IsLessThan(double first, double second, double tolerance = TOLERANCE)
        {
            if (!IsEqual(first, second, tolerance))
                return first < second;
            return false;
        }

        /// <summary>
        /// determine if the first float point number is less than or equal to
        /// the second float point number within a tolerance range
        /// </summary>
        public static bool IsLessThanOrEqual(double first, double second, double tolerance = TOLERANCE)
        {
            return IsEqual(first, second, tolerance) || first < second;
        }

        /// <summary>
        /// determine if the first float point numebr is greater than
        /// the second float point number within a tolerance range
        /// </summary>
        public static bool IsGreaterThan(double first, double second, double tolerance = TOLERANCE)
        {
            if (!IsEqual(first, second, tolerance))
                return first > second;
            return false;
        }

        /// <summary>
        /// determine if the firstfloat point number is greater than or
        /// equal to the second float point number within a tolerance range
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        public static bool IsGreaterThanOrEqual(double first, double second, double tolerance = TOLERANCE)
        {
            return IsEqual(first, second, tolerance) || first > second;
        }

        #endregion Compare

        #region Points and vectors

        /// <summary>
        /// Check if 2 lines overlap or straight
        /// </summary>
        /// <param name="linei"></param>
        /// <param name="linej"></param>
        /// <returns></returns>
        public static bool IsLineStraightOverlap(Line linei, Line linej, double tolerance = 0.01)
        {
            if (linei != null && linei.IsBound
                && linej != null && linej.IsBound
                && IsParallel(linei.Direction, linej.Direction, tolerance))
            {
                XYZ endPoint = linei.GetEndPoint(0);
                XYZ endPoint2 = linej.GetEndPoint(0);

                if (IsEqual(endPoint, endPoint2, tolerance)
                    || IsParallel(linei.Direction, endPoint2 - endPoint, tolerance))
                    return true;

                SetComparisonResult setComparisonResult = linei.Intersect(linej);

                if (setComparisonResult == SetComparisonResult.Disjoint
                    || setComparisonResult == SetComparisonResult.Equal)
                    return true;
            }

            return false;
        }

        public static bool IsLineStraight(Line linei, Line linej, double tolerance = 0.01)
        {
            if (linei != null && linei.IsBound
                && linej != null && linej.IsBound
                && IsParallel(linei.Direction, linej.Direction, tolerance))
            {
                XYZ endPoint = linei.GetEndPoint(0);
                XYZ endPoint2 = linej.GetEndPoint(0);

                if (IsEqual(endPoint, endPoint2, tolerance)
                    || IsParallel(linei.Direction, endPoint2 - endPoint, tolerance))
                    return true;
            }

            return false;
        }

        public static bool IntersectLine(Line linei, Line linej, double tolerance = 0.01)
        {
            if (linei != null
                && linej != null)
            {
                SetComparisonResult setComparisonResult = linei.Intersect(linej/*, out IntersectionResultArray resultPoints*/);
                if (setComparisonResult != SetComparisonResult.Disjoint)
                {
                    //var intersection = resultPoints.Cast<IntersectionResult>().First();
                    //point = intersection.XYZPoint;

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Check if 2 lines overlap
        /// </summary>
        /// <param name="linei"></param>
        /// <param name="linej"></param>
        /// <returns></returns>
        public static bool IsDuplicateLine(Line linei, Line linej, double tolerance = TOLERANCE)
        {
            if (linei != null && linei.IsBound
                && linej != null && linej.IsBound
                && (IsEqual(linei.GetEndPoint(0), linej.GetEndPoint(0)) && IsEqual(linei.GetEndPoint(1), linej.GetEndPoint(1)) ||
                (IsEqual(linei.GetEndPoint(0), linej.GetEndPoint(1)) && IsEqual(linei.GetEndPoint(1), linej.GetEndPoint(0)))))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// determine if 2 vectors are perpendicular to each other within a tolerance range
        /// </summary>
        public static bool IsPerpendicular(XYZ first, XYZ second, double tolerance = ANGLE_TOLERANCE)
        {
            double product = first.DotProduct(second);
            return IsEqual(product, 0, tolerance);
        }

        /// <summary>
        /// determine if 2 vector are parallel to each other within a tolerance range
        /// </summary>
        public static bool IsParallel(XYZ first, XYZ second, double tolerance = ANGLE_TOLERANCE)
        {
            XYZ product = first.CrossProduct(second);
            double length = product.GetLength();
            return IsEqual(length, 0, tolerance);
        }

        /// <summary>
        /// Transform Point to standard Coordinate
        /// </summary>
        public static XYZ TransformPointToStandardCoordinate(Plane plane, UV originalPoint)
        {
            XYZ point = null;

            if (plane != null && originalPoint != null)
            {
                // set point as the origin of the plane in standard coordinate
                point = plane.Origin - XYZ.Zero;

                // calculate the translation vector from plane orgin
                XYZ uVec = plane.XVec.Normalize() * originalPoint.U;
                XYZ vVec = plane.YVec.Normalize() * originalPoint.V;
                XYZ translation = uVec + vVec;

                // move the point its location
                point += translation;
            }

            return point;
        }

        /// <summary>
        ///  Sort list points follow direction
        /// </summary>
        /// <param name="points"></param>
        /// <param name="direction"></param>
        public static void SortPointsToDirection(List<XYZ> points, XYZ direction)
        {
            if (points != null && points.Count > 1 && direction != null)
            {
                for (int i = 0; i < points.Count - 1; i++)
                {
                    for (int j = i + 1; j < points.Count; j++)
                    {
                        XYZ directionCheck = points[j] - points[i];

                        if (!IsEqual(points[i], points[j]) && Math.Abs(directionCheck.AngleTo(direction)) > Math.PI / 2)
                        {
                            (points[j], points[i]) = (points[i], points[j]);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// filter out all points that are nots vertices of polygon
        /// </summary>
        /// <param name="vertices"></param>
        /// <returns></returns>
        public static List<XYZ> MergePoints(List<XYZ> vertices)
        {
            List<XYZ> mergedPoints = new List<XYZ>();

            if (vertices?.Count > 0)
            {
                vertices = Distinct(vertices);
                List<XYZ> pointsOnTheSameEdge = new List<XYZ>();
                int baseIndex = GetStartIndex(vertices);

                for (int currentIndex = 0; currentIndex <= vertices.Count; currentIndex++)
                {
                    XYZ prePoint = pointsOnTheSameEdge.LastOrDefault();
                    XYZ currentPoint = vertices[baseIndex];
                    baseIndex = (baseIndex + 1) % vertices.Count;
                    XYZ nextPoint = vertices[baseIndex];

                    pointsOnTheSameEdge.Add(currentPoint);
                    if (pointsOnTheSameEdge.Count > 1)
                    {
                        XYZ currentDir = (currentPoint - prePoint).Normalize();
                        XYZ nextDir = (nextPoint - currentPoint).Normalize();

                        if (!IsEqual(currentDir, nextDir))
                        {
                            mergedPoints.Add(pointsOnTheSameEdge.First());
                            mergedPoints.Add(pointsOnTheSameEdge.Last());
                            pointsOnTheSameEdge.Clear();
                            pointsOnTheSameEdge.Add(currentPoint);
                        }
                    }
                }
                mergedPoints = Distinct(mergedPoints);
            }
            return mergedPoints;
        }

        /// <summary>
        /// filter out all duplicated points
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        public static List<XYZ> Distinct(List<XYZ> points)
        {
            List<XYZ> diffPoints = new List<XYZ>();
            if (points?.Count > 0)
            {
                foreach (XYZ point in points)
                {
                    if (diffPoints.All(x => !IsEqual(x, point)))
                        diffPoints.Add(point);
                }
            }
            return diffPoints;
        }

        /// <summary>
        /// get the index of the first point whose
        /// location is at the corner or a polygon
        /// formed by given points
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        public static int GetStartIndex(List<XYZ> points)
        {
            if (points?.Count > 0)
            {
                int count = points.Count;
                for (int index = 0; index < count; index++)
                {
                    XYZ prePoint = points[(index + count - 1) % count];
                    XYZ currentPoint = points[index];
                    XYZ nextPoint = points[(index + 1) % count];

                    XYZ nextDir = (nextPoint - currentPoint).Normalize();
                    XYZ preDir = (currentPoint - prePoint).Normalize();

                    if (!IsEqual(nextDir, preDir))
                        return index;
                }
            }
            return 0;
        }

        /// <summary>
        /// Find min max of list points
        /// </summary>
        public static void FindMaxMinPoint(List<XYZ> points, out XYZ min, out XYZ max)
        {
            min = null;
            max = null;
            if (points?.Count > 0)
            {
                double maxX = points.Max(x => x.X);
                double maxY = points.Max(x => x.Y);
                double maxZ = points.Max(x => x.Z);

                double minX = points.Min(x => x.X);
                double minY = points.Min(x => x.Y);
                double minZ = points.Min(x => x.Z);

                min = new XYZ(minX, minY, minZ);
                max = new XYZ(maxX, maxY, maxZ);
            }
        }

        public static XYZ GetPointProject(Line line, XYZ point)
        {
            if (line != null && point != null)
            {
                line.MakeUnbound();

                IntersectionResult intersectionResult = line.Project(point);
                if (intersectionResult != null)
                {
                    return intersectionResult.XYZPoint;
                }
            }

            return null;
        }

        public static double GetSignedDistance(XYZ planeOrigin, XYZ planeNormal, XYZ point)
        {
            planeNormal = planeNormal.Normalize();
            XYZ vector = point - planeOrigin;
            return planeNormal.DotProduct(vector);
        }

        #endregion Points and vectors

        #region Filter elements

        private static FilteredElementCollector GetElementCollector(Document doc, RevitView view)
        {
            return view != null ? new FilteredElementCollector(doc, view.Id)
                                : new FilteredElementCollector(doc);
        }

        public static List<FamilyInstance> GetAllColumns(Document doc, RevitView view = null)
        {
            List<FamilyInstance> result = new List<FamilyInstance>();
            result = getAllArchitectureColumns(doc, view).ToList();
            result.AddRange(GetAllStructureColumns(doc, view));

            return result;
        }

        /// <summary>
        /// get all columns in revit document or given view
        /// </summary>
        public static IEnumerable<FamilyInstance> GetAllStructureColumns(Document doc, RevitView view = null)
        {
            return GetElementCollector(doc, view)
                    .WhereElementIsNotElementType()
                    .OfCategory(BuiltInCategory.OST_StructuralColumns)
                    .OfClass(typeof(FamilyInstance))
                    .Cast<FamilyInstance>();
        }

        public static IEnumerable<FamilyInstance> getAllArchitectureColumns(Document doc, RevitView view = null)
        {
            return GetElementCollector(doc, view)
                  .WhereElementIsNotElementType()
                  .OfCategory(BuiltInCategory.OST_Columns)
                  .OfClass(typeof(FamilyInstance))
                  .Cast<FamilyInstance>();
        }

        /// <summary>
        /// get all floors in revit document or given view
        /// </summary>
        public static IEnumerable<Floor> GetAllFloors(Document doc, RevitView view = null)
        {
            return GetElementCollector(doc, view)
                    .WhereElementIsNotElementType()
                    .OfCategory(BuiltInCategory.OST_Floors)
                    .OfClass(typeof(Floor))
                    .Cast<Floor>();
        }

        public static List<Wall> GetAllWall(Document doc, RevitView view = null)
        {
            return GetElementCollector(doc, view)
                   .OfClass(typeof(Wall))
                   .WhereElementIsNotElementType()
                   .Cast<Wall>()
                   .ToList();
        }

        public static IEnumerable<Element> GetAllBeams(Document doc, RevitView view = null)
        {
            return GetElementCollector(doc, view)
                    .OfCategory(BuiltInCategory.OST_StructuralFraming)
                    .WhereElementIsNotElementType()
                    .OfClass(typeof(FamilyInstance))
                    .Cast<Element>();
        }

        public static IEnumerable<RevitLinkInstance> GetLinkInstances(Document doc)
        {
            return new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_RvtLinks)
                    .OfClass(typeof(RevitLinkInstance))
                    .Cast<RevitLinkInstance>()
                    .Where(item => item.GetLinkDocument() != null);
        }

        /// <summary>
        /// Get Sytem Type Id
        /// </summary>
        /// <param name="enumType"></param>
        /// <returns></returns>
        public static Type GetSytemTypeId(MEP_CURVE_TYPE enumType)
        {
            if (enumType == MEP_CURVE_TYPE.PIPE)
                return typeof(PipingSystemType);
            else if (enumType == MEP_CURVE_TYPE.OVAL_DUCT
                     || enumType == MEP_CURVE_TYPE.RECTANGULAR_DUCT
                     || enumType == MEP_CURVE_TYPE.ROUND_DUCT
                     || enumType == MEP_CURVE_TYPE.DUCT)
                return typeof(MechanicalSystemType);
            else
                return null;
        }

        #endregion Filter elements

        #region Solids

        public static bool IsPointInSolid(Solid solid, XYZ point)
        {
            SolidCurveIntersectionOptions sco = new SolidCurveIntersectionOptions();
            sco.ResultType = SolidCurveIntersectionMode.CurveSegmentsInside;

            Line line = Line.CreateBound(point, point.Add(XYZ.BasisX));

            double tolerance = 0.000001;

            SolidCurveIntersection sci = solid.IntersectWithCurve(line, sco);

            for (int i = 0; i < sci.SegmentCount; i++)
            {
                Curve c = sci.GetCurveSegment(i);

                if (point.IsAlmostEqualTo(c.GetEndPoint(0), tolerance) || point.IsAlmostEqualTo(c.GetEndPoint(1), tolerance))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Union solids
        /// </summary>
        /// <param name="solidIFCs"></param>
        /// <returns></returns>
        public static Solid UnionSolids(List<Solid> solidIFCs)
        {
            try
            {
                if (solidIFCs?.Count > 0)
                {
                    Solid result = solidIFCs.First();

                    for (int i = 1; i < solidIFCs.Count; i++)
                    {
                        result = BooleanOperationsUtils.ExecuteBooleanOperation(result, solidIFCs[i], BooleanOperationsType.Union);
                    }

                    return result;
                }
            }
            catch (Exception)
            { }
            return null;
        }

        #endregion Solids

        #region Faces

        /// <summary>
        /// project a point onto
        /// </summary>
        /// <param name="plane"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static XYZ ProjectOnto(Plane plane, XYZ point)
        {
            double distance = GetSignedDistance(plane, point);
            XYZ projectedPoint = point - plane.Normal * distance;
            return projectedPoint;
        }

        /// <summary>
        /// Get the signed distance from the given point to the given plane.
        /// Possitive value means that the point is on the same side with
        /// plane normal vector, negative means vice versa.
        /// </summary>
        /// <param name="plane"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static double GetSignedDistance(Plane plane, XYZ point)
        {
            XYZ vector = point - plane.Origin;
            return plane.Normal.DotProduct(vector);
        }

        /// <summary>
        /// Get the project of point on a face (face create by origin and normal)
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="normal"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static XYZ ProjectOntoFaceByOriginAndNormal(XYZ origin, XYZ normal, XYZ point)
        {
            XYZ vector = point - origin;
            double distance = normal.DotProduct(vector);
            XYZ projectedPoint = point - normal * distance;
            return projectedPoint;
        }

        /// <summary>
        /// Group parallel planar faces
        /// </summary>
        /// <param name="planarFaces"></param>
        /// <returns></returns>
        public static List<List<PlanarFace>> GroupParallelPlanarFaces(List<PlanarFace> planarFaces)
        {
            List<List<PlanarFace>> result = new List<List<PlanarFace>>();
            HashSet<PlanarFace> visited = new HashSet<PlanarFace>();

            foreach (PlanarFace pf1 in planarFaces)
            {
                if (!visited.Contains(pf1))
                {
                    List<PlanarFace> group = new List<PlanarFace>() { pf1 };
                    visited.Add(pf1);

                    XYZ pf1Normal = pf1.FaceNormal;

                    foreach (PlanarFace pf2 in planarFaces)
                    {
                        if (!visited.Contains(pf2))
                        {
                            XYZ pf2Normal = pf2.FaceNormal;
                            bool isParalee = RevitUtilities.Common.IsParallel(pf1Normal, pf2Normal);

                            if (isParalee)
                            {
                                group.Add(pf2);
                                visited.Add(pf2);
                            }
                        }
                    }

                    group.Sort(new FaceComparer());
                    result.Add(group);
                }
            }

            return result;
        }

        #endregion Faces

        #region Vector

        /// <summary>
        /// Get rotate element convert
        /// </summary>
        public static double GetRotateFacingElement(Element elem, Line location, ElementIFC dataIFC, RevitLinkInstance revLnkIns)
        {
            double angle = 0;
            if (elem != null && dataIFC != null && dataIFC.Location != null)
            {
                //XYZ vertorFacing = GetFacingElement(elem, location);
                XYZ vertorFacing = GetFacingElement1(elem, location);

                if (vertorFacing != null && !dataIFC.IsCircle)
                {
                    if (IsEqual(dataIFC.Length.Length, dataIFC.Width.Length))
                    {
                        XYZ facingIFC = dataIFC.GetTransformFacingOfObjectIFC(revLnkIns, dataIFC.Length.Direction);
                        XYZ handleIFC = dataIFC.GetTransformFacingOfObjectIFC(revLnkIns, dataIFC.Width.Direction);
                        angle = vertorFacing.AngleOnPlaneTo(facingIFC, location.Direction);

                        double angleCheck = vertorFacing.AngleOnPlaneTo(facingIFC.Negate(), location.Direction);
                        if (angleCheck < angle)
                        {
                            angle = angleCheck;
                        }

                        angleCheck = vertorFacing.AngleOnPlaneTo(handleIFC, location.Direction);
                        if (angleCheck < angle)
                        {
                            angle = angleCheck;
                        }

                        angleCheck = vertorFacing.AngleOnPlaneTo(handleIFC.Negate(), location.Direction);
                        if (angleCheck < angle)
                        {
                            angle = angleCheck;
                        }
                    }
                    else
                    {
                        XYZ facingIFC = dataIFC.GetTransformFacingOfObjectIFC(revLnkIns, dataIFC.Length.Direction);
                        angle = vertorFacing.AngleOnPlaneTo(facingIFC, location.Direction);
                    }
                }
            }
            return angle;
        }

        public static XYZ GetFacingElement1(Element elem, Line lcLine)
        {
            XYZ direction = null;

            List<Solid> solids = UtilsSolid.GetAllSolids(elem);
            if (solids.Count > 0 && lcLine != null)
            {
                foreach (PlanarFace face in solids.First().Faces)
                {
                    if (IsParallel(face.FaceNormal, lcLine.Direction))
                    {
                        Plane plane = Plane.CreateByNormalAndOrigin(lcLine.Direction, lcLine.Origin);

                        List<XYZ> verticals = face.Triangulate().Vertices.ToList();
                        FindMaxMinPoint(verticals, out XYZ min, out XYZ max);
                        XYZ center = (min + max) / 2;

                        IOrderedEnumerable<Line> lines = UtilsPlane.GetLinesOfFace(face).OrderBy(x => x.Length);
                        if (lines?.Count() > 0)
                        {
                            Line minLine = lines.First();
                            XYZ mid = (minLine.GetEndPoint(0) + minLine.GetEndPoint(1)) / 2;
                            mid = UtilsPlane.ProjectOnto(plane, mid);

                            direction = center - mid;
                        }

                        break;
                    }
                }
            }
            return direction;
        }

        #endregion Vector

        #region MEP

        public static FamilySymbol GetIShapePipingSupport(Document doc)
        {
            var supportType = new FilteredElementCollector(doc)
                            .WhereElementIsElementType()
                            .OfCategory(BuiltInCategory.OST_GenericModel)
                            .OfClass(typeof(FamilySymbol))
                            .Cast<FamilySymbol>()
                            .FirstOrDefault(x => x.Name == "設備サポート_I形");
            return supportType;
        }

        public static FamilySymbol GetIShapeCylinderPipingSupport(Document doc)
        {
            var supportType = new FilteredElementCollector(doc)
                            .WhereElementIsElementType()
                            .OfCategory(BuiltInCategory.OST_GenericModel)
                            .OfClass(typeof(FamilySymbol))
                            .Cast<FamilySymbol>()
                            .FirstOrDefault(x => x.Name == "設備サポート_I形(円形)");
            return supportType;
        }

        public static ElementId GetLevelClosetTo(Document document, XYZ origin, bool lowerLevel = false)
        {
            try
            {
                FilteredElementCollector collector = new FilteredElementCollector(document);
                List<Level> levels = collector.OfClass(typeof(Level)).Cast<Level>().ToList();

                Level nearestLevel = null;
                double minDistance = double.MaxValue;
                foreach (Level level in levels)
                {
                    double distance = level.Elevation - origin.Z;
                    if (lowerLevel == false)
                    {
                        if (Math.Abs(distance) < Math.Abs(minDistance))
                        {
                            minDistance = distance;
                            nearestLevel = level;
                        }
                    }
                    else
                    {
                        distance = origin.Z - level.Elevation;
                        if (distance < 0)
                            continue;

                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            nearestLevel = level;
                        }
                    }
                }

                if (nearestLevel == null)
                {
                    nearestLevel = levels.OrderBy(item => item.Elevation).FirstOrDefault();
                }

                return nearestLevel.Id;
            }
            catch (Exception)
            { }
            return ElementId.InvalidElementId;
        }

        public static bool IsLineIntersect(Line line1, Line line2)
        {
            if (line1.Intersect(line2) != SetComparisonResult.Disjoint)
                return true;

            XYZ startPoint1 = line1.GetEndPoint(0);
            XYZ endPoint1 = line1.GetEndPoint(1);

            XYZ startPoint2 = line2.GetEndPoint(0);
            XYZ endPoint2 = line2.GetEndPoint(1);

            var result = line2.Project(startPoint1);
            if (result != null && IsEqual(0, result.Distance, 1e-3))
                return true;

            result = line2.Project(endPoint1);
            if (result != null && IsEqual(0, result.Distance, 1e-3))
                return true;

            result = line1.Project(startPoint2);
            if (result != null && IsEqual(0, result.Distance, 1e-3))
                return true;

            result = line1.Project(endPoint2);
            if (result != null && IsEqual(0, result.Distance, 1e-3))
                return true;

            return false;
        }

        public static FamilyInstance CreateTee(Document document, Connector c3, Connector c4, Connector c5)
        {
            if (c3 == null || c4 == null || c5 == null)
                return null;
            try
            {
                var fitting = document.Create.NewTeeFitting(c3, c4, c5);

                return fitting;
            }
            catch (System.Exception)
            {
                return null;
            }
        }

        public static Element Clone(Document doc, Element element)
        {
            //Create new pipe
            var newPlace = new XYZ(0, 0, 0);
            var elemIds = ElementTransformUtils.CopyElement(
             doc, element.Id, newPlace);

            var clone = doc.GetElement(elemIds.ToList()[0]);

            return clone;
        }

        public static Connector GetConnectorClosestTo(Element e,
                                                      XYZ p)
        {
            ConnectorManager cm = GetConnectorManager(e);

            return null == cm
              ? null
              : GetConnectorClosestTo(cm.Connectors, p);
        }

        /// <summary>
        /// Get connector neares
        /// </summary>
        /// <param name="connectorManager1"></param>
        /// <param name="connectorManager2"></param>
        /// <param name="con1"></param>
        /// <param name="con2"></param>
        public static void GetConnectorClosedTo(ConnectorManager connectorManager1, ConnectorManager connectorManager2, out Connector con1, out Connector con2)
        {
            con1 = null;
            con2 = null;

            if (connectorManager1 != null && connectorManager2 != null)

            {
                double distanceMin = double.MaxValue;

                foreach (Connector item1 in connectorManager1.Connectors)
                {
                    foreach (Connector item2 in connectorManager2.Connectors)
                    {
                        double distance = item1.Origin.DistanceTo(item2.Origin);
                        if (distance < distanceMin)
                        {
                            con1 = item1;
                            con2 = item2;
                            distanceMin = distance;
                        }
                    }
                }
            }
        }

        private static Connector GetConnectorClosestTo(ConnectorSet connectors,
                                                       XYZ p)
        {
            Connector targetConnector = null;
            double minDist = double.MaxValue;

            foreach (Connector c in connectors)
            {
                double d = c.Origin.DistanceTo(p);

                if (d < minDist)
                {
                    targetConnector = c;
                    minDist = d;
                }
            }
            return targetConnector;
        }

        private static ConnectorManager GetConnectorManager(Element e)
        {
            MEPCurve mc = e as MEPCurve;
            FamilyInstance fi = e as FamilyInstance;

            if (null == mc && null == fi)
            {
                throw new ArgumentException(
                  "Element is neither an MEP curve nor a fitting.");
            }

            return null == mc
              ? fi.MEPModel.ConnectorManager
              : mc.ConnectorManager;
        }

        /// <summary>
        /// Lấy symbol wye của ống
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="ele"></param>
        /// <returns></returns>
        public static FamilySymbol GetFamilySymbolWye(Document doc, Element ele, RoutingPreferenceRuleGroupType preferenceRuleGroupType)
        {
            try
            {
                RoutingPreferenceManager rpm = null;

                if (ele is Pipe pipe)
                {
                    rpm = pipe.PipeType.RoutingPreferenceManager;
                }
                else if (ele is Duct duct)
                {
                    rpm = duct.DuctType.RoutingPreferenceManager;
                }
                int numberOfRule = rpm.GetNumberOfRules(preferenceRuleGroupType);
                for (int i = 0; i < numberOfRule; i++)
                {
                    RoutingPreferenceRule rule = rpm.GetRule(preferenceRuleGroupType, i);
                    FamilySymbol symbol = doc.GetElement(rule.MEPPartId) as FamilySymbol;
                    if (symbol != null)
                        return symbol;
                }
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Lấy symbol set RoutingPreference cho pipe type hay chưa
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="pipe"></param>
        /// <param name="checkType">
        /// Tee: RoutingPreferenceRuleGroupType.Junctions
        /// Elbow: RoutingPreferenceRuleGroupType.Elbows
        /// </param>
        /// <returns></returns>
        public static FamilySymbol GetSymbolSeted(Document doc, Pipe pipe, RoutingPreferenceRuleGroupType checkType)
        {
            try
            {
                if (doc != null && pipe != null && pipe.IsValidObject)
                {
                    RoutingPreferenceManager rpm = pipe.PipeType.RoutingPreferenceManager;

                    if (checkType == RoutingPreferenceRuleGroupType.Junctions &&
                      rpm.PreferredJunctionType != PreferredJunctionType.Tee)
                        return null;

                    int numberOfRule = rpm.GetNumberOfRules(checkType);

                    if (numberOfRule > 0)
                    {
                        RoutingPreferenceRule rule = rpm.GetRule(checkType, numberOfRule - 1);

                        if (rule.MEPPartId != null &&
                            rule.MEPPartId != ElementId.InvalidElementId)
                        {
                            return doc.GetElement(rule.MEPPartId) as FamilySymbol;
                        }
                    }
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Lấy symbol set RoutingPreference cho pipe type hay chưa
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="pipe"></param>
        /// <param name="checkType">
        /// Tee: RoutingPreferenceRuleGroupType.Junctions
        /// Elbow: RoutingPreferenceRuleGroupType.Elbows
        /// </param>
        /// <returns></returns>
        public static FamilySymbol GetSymbolSeted(Document doc, Duct pipe, RoutingPreferenceRuleGroupType checkType)
        {
            try
            {
                if (doc != null && pipe != null && pipe.IsValidObject)
                {
                    RoutingPreferenceManager rpm = pipe.DuctType.RoutingPreferenceManager;

                    if (checkType == RoutingPreferenceRuleGroupType.Junctions &&
                      rpm.PreferredJunctionType != PreferredJunctionType.Tee)
                        return null;

                    int numberOfRule = rpm.GetNumberOfRules(checkType);

                    if (numberOfRule > 0)
                    {
                        RoutingPreferenceRule rule = rpm.GetRule(checkType, numberOfRule - 1);

                        if (rule.MEPPartId != null &&
                            rule.MEPPartId != ElementId.InvalidElementId)
                        {
                            return doc.GetElement(rule.MEPPartId) as FamilySymbol;
                        }
                    }
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Get connector main
        /// </summary>
        /// <param name="fitting"></param>
        /// <param name="vector"></param>
        /// <param name="mainConnect1"></param>
        /// <param name="mainConnect2"></param>
        public static void GetConnectorMain(FamilyInstance fitting, XYZ vector, out Connector mainConnect1, out Connector mainConnect2)
        {
            mainConnect1 = null;
            mainConnect2 = null;

            MechanicalFitting mechanicalFitting = fitting.MEPModel as MechanicalFitting;
            if (mechanicalFitting != null && mechanicalFitting.PartType == PartType.Tee && vector == null && fitting.MEPModel.ConnectorManager.Connectors.Size == 3)
            {
                //Main : hướng connector của 2 connector fai song song voi nhau (nguoc chieu nhau)

                foreach (Connector c1 in fitting.MEPModel.ConnectorManager.Connectors)
                {
                    foreach (Connector c2 in fitting.MEPModel.ConnectorManager.Connectors)
                    {
                        if (c1.Id == c2.Id)
                        {
                            continue;
                        }
                        else
                        {
                            var z1 = c1.CoordinateSystem.BasisZ;
                            var z2 = c2.CoordinateSystem.BasisZ;

                            if (IsParallel(z1, z2, 0.0001) == true)
                            {
                                mainConnect1 = c1;
                                mainConnect2 = c2;
                                break;
                            }
                        }
                    }

                    if (mainConnect1 != null && mainConnect2 != null)
                        break;
                }
            }
            else
            {
                foreach (Connector con in fitting.MEPModel.ConnectorManager.Connectors)
                {
                    if (vector != null)
                    {
                        if (IsParallel(vector, con.CoordinateSystem.BasisZ, 0.0001) == false)
                        {
                            continue;
                        }
                    }

                    if (mainConnect1 == null)
                        mainConnect1 = con;
                    else
                    {
                        mainConnect2 = con;
                        break;
                    }
                }
            }

            if (mainConnect1 != null && mainConnect2 != null)
            {
                //Connect nao gan location of fitting thi do la 1

                var p = (fitting.Location as LocationPoint).Point;
                if (mainConnect1.Origin.DistanceTo(p) > mainConnect2.Origin.DistanceTo(p))
                {
                    Connector temp = mainConnect1;
                    mainConnect1 = mainConnect2;

                    mainConnect2 = temp;
                }
            }
        }

        public static void DisconnectFrom(FamilyInstance fittingWye, out Connector connectedSt, out Connector connectedEnd, out Element eleSt, out Element eleEnd)
        {
            connectedSt = null;
            connectedEnd = null;
            eleSt = null;
            eleEnd = null;
            if (fittingWye != null)
            {
                GetInformationConectorWye(fittingWye, null, out Connector conSt, out Connector conEnd, out Connector conNhanhWye);

                if (conSt != null && conSt.IsConnected)
                {
                    foreach (Connector item in conSt.AllRefs)
                    {
                        if (item != null && item.IsConnectedTo(conSt))
                        {
                            conSt.DisconnectFrom(item);

                            if (item != null && item.Owner != null)
                            {
                                connectedSt = item;
                                eleSt = item.Owner;
                            }
                        }
                    }
                }

                if (conEnd != null && conEnd.IsConnected)
                {
                    foreach (Connector item in conEnd.AllRefs)
                    {
                        if (item != null && item.IsConnectedTo(conEnd))
                        {
                            conEnd.DisconnectFrom(item);

                            if (item != null && item.Owner != null)
                            {
                                connectedEnd = item;
                                eleEnd = item.Owner;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Lấy ra connector gần nhất và xa nhất với 1 điểm cho trước
        /// </summary>
        /// <param name="point"></param>
        /// <param name="pipe"></param>
        /// <param name="outFarest"></param>
        /// <returns></returns>
        public static Connector GetConnectorNearest(XYZ point, ConnectorManager connectorManager, out Connector outFarest)
        {
            Connector retval = null;
            outFarest = null;

            if (point != null && connectorManager != null)
            {
                double max = double.MaxValue;
                double min = double.MinValue;

                foreach (Connector item in connectorManager.Connectors)
                {
                    double distance = item.Origin.DistanceTo(point);

                    // lấy connector gần nhất
                    if (distance < max)
                    {
                        max = distance;
                        retval = item;
                    }
                    // lấy connector xa nhất
                    if (distance > min)
                    {
                        min = distance;
                        outFarest = item;
                    }
                }
            }

            return retval;
        }

        public static void RotateLine(Document doc, FamilyInstance wye, Line axisLine)
        {
            GetInformationConectorWye(wye, null, out Connector connector2, out Connector connector3, out Connector conTee);

            Line rotateLine = Line.CreateBound(connector2.Origin, connector3.Origin);

            if (IsParallel(axisLine.Direction, rotateLine.Direction))
                return;

            XYZ vector = rotateLine.Direction.CrossProduct(axisLine.Direction);
            XYZ intersection = GetUnBoundIntersection(rotateLine, axisLine);

            if (intersection != null)
            {
                double angle = rotateLine.Direction.AngleTo(axisLine.Direction);

                Line line = Line.CreateUnbound(intersection, vector);

                ElementTransformUtils.RotateElement(doc, wye.Id, line, angle);
                doc.Regenerate();
            }
            else
            {
                intersection = (connector2.Origin + connector3.Origin) / 2;
                double angle = rotateLine.Direction.AngleTo(axisLine.Direction);

                Line line = Line.CreateUnbound(intersection, vector);

                ElementTransformUtils.RotateElement(doc, wye.Id, line, angle);
                doc.Regenerate();
            }
        }

        /// <summary>
        /// return intersection of the 2 lines given that are unbound
        /// </summary>
        /// <param name="Line1"></param>
        /// <param name="Line2"></param>
        /// <returns></returns>
        public static XYZ GetUnBoundIntersection(Line Line1, Line Line2)
        {
            if (Line1 != null && Line2 != null)
            {
                Curve ExtendedLine1 = Line.CreateUnbound(Line1.Origin, Line1.Direction);
                Curve ExtendedLine2 = Line.CreateUnbound(Line2.Origin, Line2.Direction);
                SetComparisonResult setComparisonResult = ExtendedLine1.Intersect(ExtendedLine2, out IntersectionResultArray resultArray);
                if (resultArray != null &&
                    resultArray.Size > 0)
                {
                    foreach (IntersectionResult result in resultArray)
                        if (result != null)
                            return result.XYZPoint;
                }
            }
            return null;
        }

        /// <summary>
        /// Get information of connector tee
        /// </summary>
        /// <param name="fitting"></param>
        /// <param name="vector"></param>
        /// <param name="main1"></param>
        /// <param name="main2"></param>
        /// <param name="tee"></param>
        public static void GetInformationConectorWye(FamilyInstance fitting, XYZ vector, out Connector main1, out Connector main2, out Connector tee)
        {
            main1 = null;
            main2 = null;
            tee = null;
            if (fitting != null)
            {
                //Get fitting info

                GetConnectorMain(fitting, vector, out main1, out main2);

                foreach (Connector c in fitting.MEPModel.ConnectorManager.Connectors)
                {
                    if (c.Id != main1.Id && c.Id != main2.Id)
                    {
                        tee = c;
                        break;
                    }
                }
            }
        }

        #endregion MEP

        #region Set Parameter for colum

        /// <summary>
        /// Get level offset for column
        /// </summary>
        /// <param name="Column"></param>
        /// <param name="locationLine"></param>
        /// <param name="baseLevelCloset"></param>
        /// <param name="topLevelCloset"></param>
        /// <param name="topLevelId"></param>
        /// <param name="baseOffset"></param>
        /// <param name="topOffset"></param>
        public static void GetLevelOffsetForColumn(FamilyInstance Column,
                                             Line locationLine,
                                             Level baseLevelCloset,
                                             Level topLevelCloset,
                                             //out ElementId topLevelId,
                                             out double baseOffset,
                                             out double topOffset)
        {
            //topLevelId = null;
            baseOffset = double.NaN;
            topOffset = double.NaN;

            if (Column != null)
            {
                // Get level top offset
                ElementId levelId_TopOffset = UtilsParameter.GetElementIdBuiltInParameter(Column, BuiltInParameter.SCHEDULE_TOP_LEVEL_PARAM);
                if (levelId_TopOffset != null && levelId_TopOffset != ElementId.InvalidElementId)
                {
                    //topLevelId = levelId_TopOffset;
                    baseOffset = locationLine.GetEndPoint(0).Z - baseLevelCloset.Elevation;
                    topOffset = locationLine.GetEndPoint(1).Z - topLevelCloset.Elevation;
                }
            }
        }

        /// Set Parameter  Column
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public static void SetParameterForColumn(FamilyInstance column, ElementId topLevelId, double baseOffset, double topOffset)
        {
            // Top Level
            try
            {
                if (topLevelId != null)
                    UtilsParameter.SetValueParameterBuiltIn(column, BuiltInParameter.SCHEDULE_TOP_LEVEL_PARAM, topLevelId);
            }
            catch (Exception)
            { }

            // Top offset
            try
            {
                if (topOffset != double.MinValue)
                    UtilsParameter.SetValueParameterBuiltIn(column, BuiltInParameter.SCHEDULE_TOP_LEVEL_OFFSET_PARAM, topOffset);
            }
            catch (Exception)
            { }

            // Base offset
            try
            {
                if (baseOffset != double.MinValue)
                    UtilsParameter.SetValueParameterBuiltIn(column, BuiltInParameter.SCHEDULE_BASE_LEVEL_OFFSET_PARAM, baseOffset);
            }
            catch (Exception)
            { }
        }

        #endregion Set Parameter for colum

        #region Get All Line Of LinkElm

        /// <summary>
        /// Get all lines
        /// </summary>
        /// <param name="element"></param>
        /// <param name="options"></param>
        /// <param name="view"></param>
        /// <param name="transform"></param>
        /// <param name="getSymbolGeometry"></param>
        /// <param name="includeLightSource"></param>
        /// <returns></returns>
        public static List<Line> GetAllLines(Autodesk.Revit.DB.Element element,
                                             Autodesk.Revit.DB.Options options = null,
                                             View view = null,
                                             Transform transform = null,
                                             bool getSymbolGeometry = false,
                                             bool includeLightSource = false)
        {
            List<Line> result = new List<Line>();
            if (element != null && element.IsValidObject)
            {
                Document document = element.Document;
                if (options == null)
                {
                    options = new Autodesk.Revit.DB.Options
                    {
                        ComputeReferences = true,
                        DetailLevel = Autodesk.Revit.DB.ViewDetailLevel.Fine
                    };
                }

                if (view != null)
                {
                    options.View = view;
                }

                GeometryElement geometryObject = element.get_Geometry(options);
                GetLineFromGeometry(document, result, geometryObject, view, transform, getSymbolGeometry, includeLightSource);
            }

            return result;
        }

        /// <summary>
        /// Get line from geometry
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="lines"></param>
        /// <param name="geometryObject"></param>
        /// <param name="view"></param>
        /// <param name="transform"></param>
        /// <param name="getSymbolGeometry"></param>
        /// <param name="includeLightSource"></param>
        public static void GetLineFromGeometry(Document doc,
                                        List<Line> lines,
                                        GeometryObject geometryObject,
                                        View view = null,
                                        Transform transform = null,
                                        bool getSymbolGeometry = false,
                                        bool includeLightSource = false)
        {
            Line line = geometryObject as Line;
            if ((object)line != null && line.Length > 0.0 && IsLineGraphicallyVisible(doc, view, line) && (includeLightSource || !IsLightSource(doc, line)))
            {
                if (transform != null)
                {
                    Line item = Line.CreateBound(transform.OfPoint(line.GetEndPoint(0)), transform.OfPoint(line.GetEndPoint(1)));
                    lines.Add(item);
                }
                else
                {
                    lines.Add(line);
                }

                return;
            }

            GeometryElement geometryElement = geometryObject as GeometryElement;
            if ((object)geometryElement != null)
            {
                IEnumerator<GeometryObject> enumerator = geometryElement.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    GeometryObject current = enumerator.Current;
                    GetLineFromGeometry(doc, lines, current, view, transform, getSymbolGeometry, includeLightSource);
                }

                return;
            }

            GeometryInstance geometryInstance = geometryObject as GeometryInstance;
            if ((object)geometryInstance != null)
            {
                IEnumerator<GeometryObject> enumerator2 = (getSymbolGeometry ? geometryInstance.GetSymbolGeometry() : geometryInstance.GetInstanceGeometry()).GetEnumerator();
                Transform geoInsTransform = geometryInstance.Transform;

                while (enumerator2.MoveNext())
                {
                    GeometryObject current2 = enumerator2.Current;
                    GetLineFromGeometry(doc, lines, current2, view, geoInsTransform, getSymbolGeometry, includeLightSource);
                }
            }
        }

        /// <summary>
        /// Check object is line graphically visible
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="view"></param>
        /// <param name="line"></param>
        /// <returns></returns>
        public static bool IsLineGraphicallyVisible(Document doc,
                                                    View view,
                                                    Line line)
        {
            if (doc != null && view != null && line.GraphicsStyleId != null && line.GraphicsStyleId != Autodesk.Revit.DB.ElementId.InvalidElementId)
            {
                GraphicsStyle graphicsStyle = doc.GetElement(line.GraphicsStyleId) as GraphicsStyle;
                if (graphicsStyle != null && graphicsStyle.GraphicsStyleCategory != null)
                {
                    return graphicsStyle.GraphicsStyleCategory.get_Visible(view);
                }
            }

            return true;
        }

        /// <summary>
        /// Check object is light source
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="line"></param>
        /// <returns></returns>
        public static bool IsLightSource(Document doc,
                                         Line line)
        {
            bool result = false;
            if (doc != null && line != null && line.GraphicsStyleId != null)
            {
                GraphicsStyle graphicsStyle = doc.GetElement(line.GraphicsStyleId) as GraphicsStyle;
                if (graphicsStyle != null)
                {
                    result = graphicsStyle.GraphicsStyleCategory?.Id.Equals(Autodesk.Revit.DB.Category.GetCategory(doc, Autodesk.Revit.DB.BuiltInCategory.OST_LightingFixtureSource).Id) ?? false;
                }
            }

            return result;
        }

        #endregion Get All Line Of LinkElm

        #region Railling

        public static void SetValueParamterConvert(UIDocument uidoc,
                                                   Element elem,
                                                   LinkElementData LinkEleData,
                                                   ConvertParamData paramData,
                                                   bool isRailing = false)
        {
            if (elem?.IsValidObject != true)
            {
                return;
            }
            if (paramData != null)
            {
                List<Parameter> SetParams = elem.GetParameters(paramData.ParamInRevit).ToList();
                foreach (Parameter param in SetParams)
                {
                    if (!param.IsReadOnly)
                    {
                        if (string.IsNullOrWhiteSpace(paramData.InputValue))
                        {
                            ParameterData data = LinkEleData.SourceParameterDatas?.FirstOrDefault(x => x.Name.Equals(paramData.Data.Name));
                            if (data != null)
                            {
                                param.Set(data.Value);
                            }
                        }
                        else
                        {
                            param.Set(paramData.InputValue);
                        }
                    }
                }
            }

            if (isRailing)
            {
                Solid solid = UtilsSolid.GetTotalSolid(elem);
                if (solid != null && !IsEqual(solid.Volume, 0))
                {
                    List<Parameter> paramVolumes = elem.GetParameters(ChangeNameParam(uidoc, "容積")).ToList();
                    foreach (var paraVolume in paramVolumes)
                    {
                        if (!paraVolume.IsReadOnly)
                        {
                            paraVolume?.Set(solid.Volume);
                        }
                    }
                }
            }
        }

        public static void AddShareParameter(UIDocument uidoc, BuiltInCategory builtInCategory, string namePara)
        {
            string path = FileUtils.GetFileShareParamFolder();

            if (!File.Exists(path))
                IO.ShowWanring(Define.FileShareDoesNotExist);
            else
                AddSharedParamsToElements(uidoc, builtInCategory, path, namePara);
        }

        public static void AddSharedParamsToElements(UIDocument uidoc, BuiltInCategory builtInCategory, string path, string namePara)
        {
            try
            {
                if (namePara != null || namePara != string.Empty)
                {
                    Document doc = uidoc.Document;
                    string currentPath = doc.Application.SharedParametersFilename;
                    if (!string.Equals(currentPath, path))
                        doc.Application.SharedParametersFilename = path;

                    DefinitionFile definitionFile = doc.Application.OpenSharedParameterFile();

                    if (definitionFile != null)
                    {
                        Dictionary<string, object> paramDic = GetSharedParameterData(uidoc, builtInCategory, namePara);
                        DefinitionGroup group = definitionFile.Groups.get_Item(Define.GROUP_SHARE_PARAMETER);

                        using (Transaction tr = new Transaction(doc, "CreatePara"))
                        {
                            tr.Start();

                            if (group == null)
                            {
                                group = definitionFile.Groups.Create(Define.GROUP_SHARE_PARAMETER);

                                foreach (var param in paramDic)
                                {
                                    ExternalDefinitionCreationOptions options = CreateNewDefinition(param.Key, param.Value);
                                    group.Definitions.Create(options);
                                }
                            }
                            else
                            {
                                // check name parameter user slect has exits in group file share parameter
                                foreach (var item in paramDic)
                                {
                                    var findPara = group.Definitions.get_Item(item.Key);
                                    if (findPara == null)
                                    {
                                        ExternalDefinitionCreationOptions options = CreateNewDefinition(item.Key, item.Value);
                                        group.Definitions.Create(options);
                                    }
                                }
                            }

                            CategorySet categorySet_Model = doc.Application.Create.NewCategorySet();

                            // CategorySet
                            if (builtInCategory == BuiltInCategory.OST_PipeCurves || builtInCategory == BuiltInCategory.OST_Railings)
                            {
                                var targetCategory_PipeCurve = doc.Settings.Categories.get_Item(BuiltInCategory.OST_PipeCurves);
                                categorySet_Model.Insert(targetCategory_PipeCurve);

                                var targetCategory_PipeFitting = doc.Settings.Categories.get_Item(BuiltInCategory.OST_PipeFitting);
                                categorySet_Model.Insert(targetCategory_PipeFitting);
                            }
                            else if (builtInCategory == BuiltInCategory.OST_DuctCurves || builtInCategory == BuiltInCategory.OST_Railings)
                            {
                                var targetCategory_DuctCurve = doc.Settings.Categories.get_Item(BuiltInCategory.OST_DuctCurves);
                                categorySet_Model.Insert(targetCategory_DuctCurve);

                                var targetCategory_DuctFitting = doc.Settings.Categories.get_Item(BuiltInCategory.OST_DuctFitting);
                                categorySet_Model.Insert(targetCategory_DuctFitting);
                            }
                            else if (builtInCategory == BuiltInCategory.OST_ElectricalEquipment)
                            {
                                var targetCategory_Electrical = doc.Settings.Categories.get_Item(BuiltInCategory.OST_ElectricalEquipment);
                                categorySet_Model.Insert(targetCategory_Electrical);

                                var targetCategory_Mechanical = doc.Settings.Categories.get_Item(BuiltInCategory.OST_MechanicalEquipment);
                                categorySet_Model.Insert(targetCategory_Mechanical);
                            }
                            else if (builtInCategory == BuiltInCategory.OST_CableTray)
                            {
                                var targetCategory_CableTray = doc.Settings.Categories.get_Item(BuiltInCategory.OST_CableTray);
                                categorySet_Model.Insert(targetCategory_CableTray);

                                var targetCategory_CableTrayFitting = doc.Settings.Categories.get_Item(BuiltInCategory.OST_CableTrayFitting);
                                categorySet_Model.Insert(targetCategory_CableTrayFitting);
                            }
                            else
                            {
                                var targetCategory = doc.Settings.Categories.get_Item(builtInCategory);
                                categorySet_Model.Insert(targetCategory);
                            }

                            foreach (var key in paramDic.Keys)
                            {
                                Definition targetDef = group.Definitions.get_Item(key);

                                if (targetDef != null)
                                {
                                    InstanceBinding binding = doc.Application.Create.NewInstanceBinding(categorySet_Model);

                                    if (!doc.ParameterBindings.Contains(targetDef))
                                        doc.ParameterBindings.Insert(targetDef, binding);
                                    else
                                    {
                                        foreach (InstanceBinding item in doc.ParameterBindings)
                                        {
                                            var x = item;
                                        }
                                        //Add the new binding to the document
                                        doc.ParameterBindings.ReInsert(targetDef, binding);
                                    }
                                }
                            }

                            doc.Regenerate();

                            tr.Commit();
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static void AddShareParameter(UIDocument uidoc, List<SharedParamData> paramDatas)
        {
            string path = FileUtils.GetFileShareParamFolder();

            if (!File.Exists(path))
                IO.ShowWanring(Define.FileShareDoesNotExist);
            else
            {
                foreach (var data in paramDatas)
                {
                    AddSharedParamsToElements(uidoc, data, path);
                }
            }
        }

        public static void AddSharedParamsToElements(UIDocument uidoc, SharedParamData paramData, string path)
        {
            try
            {
                if (paramData != null && !string.IsNullOrEmpty(paramData.ParamName))
                {
                    Document doc = uidoc.Document;
                    string currentPath = doc.Application.SharedParametersFilename;
                    if (!string.Equals(currentPath, path))
                        doc.Application.SharedParametersFilename = path;

                    DefinitionFile definitionFile = doc.Application.OpenSharedParameterFile();

                    if (definitionFile != null)
                    {
                        using (Transaction tr = new Transaction(doc, "Create Parameter"))
                        {
                            tr.Start();

                            CategorySet categorySet_Model = GetCategorySet(doc, paramData.Categories);

                            // creat group if not exist
                            DefinitionGroup group = definitionFile.Groups.get_Item(Define.GROUP_SHARE_PARAMETER) ?? definitionFile.Groups.Create(Define.GROUP_SHARE_PARAMETER);

                            // check name parameter user select has exits in group file share parameter
                            Definition definition = group.Definitions.get_Item(paramData.ParamName);
                            if (definition == null)
                            {
                                ExternalDefinitionCreationOptions options = CreateNewDefinition(paramData.ParamName, paramData.DataType);
                                group.Definitions.Create(options);
                                definition = group.Definitions.get_Item(paramData.ParamName);
                                if (definition != null)
                                {
                                    InstanceBinding binding = doc.Application.Create.NewInstanceBinding(categorySet_Model);
                                    doc.ParameterBindings.Insert(definition, binding);
                                }
                            }
                            else
                            {
                                BindingMap bm = doc.ParameterBindings;
                                DefinitionBindingMapIterator it = bm.ForwardIterator();
                                List<ElementId> builtInSet = categorySet_Model.Cast<Category>().Select(x => x.Id).ToList(); ;

                                bool isCreate = true;
                                Definition oldDef = null;
                                while (it.MoveNext())
                                {
                                    Definition def = it.Key;
                                    if (def.Name.Equals(definition.Name) && def.ParameterGroup == definition.ParameterGroup)
                                    {
                                        if (it.Current is ElementBinding map && map.Categories != null && map.Categories.Size == categorySet_Model.Size)
                                        {
                                            List<ElementId> builtIn = map.Categories.Cast<Category>().Select(x => x.Id).ToList();
                                            if (!builtInSet.Any(x => !builtIn.Contains(x)))
                                            {
                                                isCreate = false;
                                            }
                                        }
                                        oldDef = def;
                                    }
                                }

                                if (isCreate)
                                {
                                    InstanceBinding binding = doc.Application.Create.NewInstanceBinding(categorySet_Model);
                                    if (oldDef != null)
                                    {
                                        doc.ParameterBindings.ReInsert(oldDef, binding);
                                    }
                                    else
                                    {
                                        doc.ParameterBindings.Insert(definition, binding);
                                    }
                                }
                            }

                            doc.Regenerate();
                            tr.Commit();
                        }
                    }
                }
            }
            catch (Exception) { }
        }

        private static CategorySet GetCategorySet(Document doc, List<BuiltInCategory> categories)
        {
            CategorySet categorySet_Model = doc.Application.Create.NewCategorySet();
            foreach (var builtInCategory in categories)
            {
                if (builtInCategory == BuiltInCategory.OST_PipeCurves || builtInCategory == BuiltInCategory.OST_Railings)
                {
                    var targetCategory_PipeCurve = doc.Settings.Categories.get_Item(BuiltInCategory.OST_PipeCurves);
                    categorySet_Model.Insert(targetCategory_PipeCurve);

                    var targetCategory_PipeFitting = doc.Settings.Categories.get_Item(BuiltInCategory.OST_PipeFitting);
                    categorySet_Model.Insert(targetCategory_PipeFitting);
                }
                else if (builtInCategory == BuiltInCategory.OST_DuctCurves || builtInCategory == BuiltInCategory.OST_Railings)
                {
                    var targetCategory_DuctCurve = doc.Settings.Categories.get_Item(BuiltInCategory.OST_DuctCurves);
                    categorySet_Model.Insert(targetCategory_DuctCurve);

                    var targetCategory_DuctFitting = doc.Settings.Categories.get_Item(BuiltInCategory.OST_DuctFitting);
                    categorySet_Model.Insert(targetCategory_DuctFitting);
                }
                else if (builtInCategory == BuiltInCategory.OST_ElectricalEquipment)
                {
                    var targetCategory_Electrical = doc.Settings.Categories.get_Item(BuiltInCategory.OST_ElectricalEquipment);
                    categorySet_Model.Insert(targetCategory_Electrical);

                    var targetCategory_Mechanical = doc.Settings.Categories.get_Item(BuiltInCategory.OST_MechanicalEquipment);
                    categorySet_Model.Insert(targetCategory_Mechanical);
                }
                else if (builtInCategory == BuiltInCategory.OST_CableTray)
                {
                    var targetCategory_CableTray = doc.Settings.Categories.get_Item(BuiltInCategory.OST_CableTray);
                    categorySet_Model.Insert(targetCategory_CableTray);

                    var targetCategory_CableTrayFitting = doc.Settings.Categories.get_Item(BuiltInCategory.OST_CableTrayFitting);
                    categorySet_Model.Insert(targetCategory_CableTrayFitting);
                }
                else
                {
                    var targetCategory = doc.Settings.Categories.get_Item(builtInCategory);
                    categorySet_Model.Insert(targetCategory);
                }
            }
            return categorySet_Model;
        }

        public static Dictionary<string, object> GetSharedParameterData(UIDocument uidoc, BuiltInCategory builtInCategory, string namePara)
        {
            Dictionary<string, object> keyValuePairs = new Dictionary<string, object>();
#if DEBUG_2023 || RELEASE_2023

            if (namePara != null
                && namePara != string.Empty)
            {
                keyValuePairs.Add(namePara, SpecTypeId.String.Text);
            }

            if (builtInCategory == BuiltInCategory.OST_Railings)
                keyValuePairs.Add(ChangeNameParam(uidoc, "容積"), SpecTypeId.String.Text);
            return keyValuePairs;
#else
            if (namePara != null
                && namePara != string.Empty)
            {
                keyValuePairs.Add(namePara, ParameterType.Text);
            }

            if (builtInCategory == BuiltInCategory.OST_Railings)
                keyValuePairs.Add(ChangeNameParam(uidoc, "容積"), ParameterType.Volume);

            return keyValuePairs;
#endif
        }

        public static string ChangeNameParam(UIDocument uidoc, string name)
        {
            var language = uidoc.Application.Application.Language;
            if (language == LanguageType.Japanese)
            {
                name = "容積";
            }
            else
            {
                name = "VOLUME";
            }

            return name;
        }

        public static ExternalDefinitionCreationOptions CreateNewDefinition(string name, object paramTypeId)
        {
#if DEBUG_2023 || RELEASE_2023
            ForgeTypeId forgeTypeId = paramTypeId as ForgeTypeId;
            return new ExternalDefinitionCreationOptions(name, forgeTypeId);
#else
            ParameterType paramType = (ParameterType)paramTypeId;
            return new ExternalDefinitionCreationOptions(name, paramType);
#endif
        }

        #endregion Railling
    }
}