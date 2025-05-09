using Autodesk.Revit.DB;
using RevitUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using TepscoIFCToRevit.Common;
using TepscoIFCToRevit.Data.GeometryDatas;

namespace TepscoIFCToRevit.Data
{
    public class ElementIFC
    {
        public ObjectIFCType ElemType { get; set; }
        public Element LinkElem { get; set; }
        public Document LinkDoc { get; set; }
        public static Document Doc { get; set; }

        public Line Location { get; set; }
        public Line Length { get; set; }
        public Line Width { get; set; }

        public bool IsCircle { get; set; }

        public GeometryData GeomertyData { get; set; }

        public ElementIFC(Document doc, Element linkElem, Document linkdDoc, ObjectIFCType type, List<GeometryObject> geometries = null, Solid solid = null, bool isRailing = false)
        {
            Doc = doc;
            LinkElem = linkElem;
            LinkDoc = linkdDoc;
            ElemType = type;
            IsCircle = false;

            if (geometries == null && solid == null)
                geometries = GeometryUtils.GetIfcGeometriess(LinkElem);

            InitGeometry(geometries, solid, isRailing);
        }

        public void InitGeometry(List<GeometryObject> geometries, Solid solid = null, bool isRailing = false)
        {
            GeomertyData = new GeometryData(geometries, solid, isRailing);
            if (GeomertyData.OverMaxVertices || GeomertyData.Vertices.Count < 8)
            {
                return;
            }

            InitDimensions(GeomertyData, out DataDimension location, out DataDimension length, out DataDimension width);

            if (location != null)
            {
                //DrawLine(location.LineLocation);
                //DrawLine(length.LineLocation);
                //DrawLine(width.LineLocation);
                if (location.IsCircle && !length.IsCircle && !width.IsCircle)
                {
                    if (isRailing && (!location.IsCylinder || !length.IsCylinder || !width.IsCylinder))
                    {
                        return;
                    }
                    if ((ElemType != ObjectIFCType.Pipe && ElemType != ObjectIFCType.Duct) || ValidationPipeDuctCylinder(location.LineLocation))
                    {
                        Location = location.LineLocation;
                        Length = length.LineLocation;
                        Width = width.LineLocation;
                        IsCircle = true;

                        XYZ center = Location.GetEndPoint(0);
                        Plane plane = Plane.CreateByNormalAndOrigin(Location.Direction, center);
                        List<double> radius = new List<double>();
                        GeomertyData.Vertices.ForEach(x => radius.Add(UtilsPlane.ProjectOnto(plane, x).DistanceTo(center)));
                        double diameter = 2 * radius.Max();
                        if (RevitUtilities.Common.IsGreaterThan(diameter, Length.Length))
                        {
                            Length = ExtendLine(Length, diameter);
                        }
                        if (RevitUtilities.Common.IsGreaterThan(diameter, Width.Length))
                        {
                            Width = ExtendLine(Width, diameter);
                        }
                    }
                }
                else
                {
                    if (ElemType == ObjectIFCType.PipeSP && length != null && width != null)
                    {
                        double sizeLocation = location.LineLocation.Length;
                        double sizeLength = length.LineLocation.Length;
                        double sizeWidth = width.LineLocation.Length;

                        if (RevitUtils.IsGreaterThan(sizeLocation, sizeLength)
                            && RevitUtils.IsGreaterThan(sizeLocation, sizeWidth))
                        {
                            Location = location.LineLocation;
                            Length = length.LineLocation;
                            Width = width.LineLocation;
                        }
                        else if (RevitUtils.IsGreaterThan(sizeLength, sizeLocation)
                            && RevitUtils.IsGreaterThan(sizeLength, sizeWidth))
                        {
                            Location = length.LineLocation;
                            Length = location.LineLocation;
                            Width = width.LineLocation;
                        }
                        else
                        {
                            Location = width.LineLocation;
                            Length = length.LineLocation;
                            Width = location.LineLocation;
                        }
                    }
                    else
                    {
                        if (!RevitUtils.IsGreaterThan(location.LineLocation.Length / 3, length.LineLocation.Length))
                        {
                            PrioritizeByObject(location, length, width);
                        }
                        else if ((ElemType != ObjectIFCType.Pipe && ElemType != ObjectIFCType.Duct) || ValidationEndPointCylinder(location.LineLocation, length.LineLocation.Length))
                        {
                            Location = location.LineLocation;
                            Length = length.LineLocation;
                            Width = width.LineLocation;
                        }
                    }
                }

                if (Location != null)
                {
                    if (ElemType == ObjectIFCType.Beam)
                    {
                        //Plane topPlan = GeomertyData.Faces.OrderBy(x => x.Area).Select(x => x.Plane).FirstOrDefault(x => GeometryUtils.IsTopFace(x, GeomertyData.Vertices));

                        List<FaceData> topPlans = GeomertyData.Faces.OrderBy(x => x.Area).ToList();

                        Plane topPlan = SortFaceByProject(Location.Direction, topPlans.Select(x => x.Plane).ToList(), GeomertyData.Vertices);

                        if (topPlan != null)
                        {
                            XYZ start = UtilsPlane.ProjectOnto(topPlan, Location.GetEndPoint(0));
                            XYZ end = UtilsPlane.ProjectOnto(topPlan, Location.GetEndPoint(1));
                            if (start.DistanceTo(end) > RevitUtils.MIN_LENGTH)
                            {
                                Location = Line.CreateBound(start, end);
                            }
                            else
                            {
                                Location = null;
                            }
                        }
                        else
                        {
                            Location = null;
                        }
                    }
                    //if (Location != null)
                    //{
                    //    DrawLine(Location);
                    //}
                }
            }
        }

        private bool ValidationEndPointCylinder(Line location, double length)
        {
            double distance = length / 10;
            if (distance < RevitUtils.MIN_LENGTH)
            {
                distance = RevitUtils.MIN_LENGTH;
            }

            XYZ center = location.GetEndPoint(0);
            Plane plane = Plane.CreateByNormalAndOrigin(location.Direction, center);

            List<XYZ> startPoints = new List<XYZ>();
            List<XYZ> endPoints = new List<XYZ>();
            foreach (var p in GeomertyData.Vertices)
            {
                if (UtilsPlane.GetSignedDistance(plane, p) < location.Length / 2)
                {
                    startPoints.Add(p);
                }
                else
                {
                    endPoints.Add(p);
                }
            }

            RevitUtils.FindMaxMinPoint(startPoints, out XYZ min, out XYZ max);
            XYZ mid = (min + max) / 2;
            if (location.Distance(mid) > distance)
            {
                return false;
            }

            RevitUtils.FindMaxMinPoint(endPoints, out min, out max);
            mid = (min + max) / 2;
            if (location.Distance(mid) > distance)
            {
                return false;
            }

            return true;
        }

        private void PrioritizeByObject(DataDimension location, DataDimension length, DataDimension width)
        {
            if (ElemType == ObjectIFCType.Beam || ElemType == ObjectIFCType.Wall)
            {
                double angle0 = location.LineLocation.Direction.AngleTo(XYZ.BasisZ);
                if (angle0 > Math.PI / 2)
                {
                    angle0 = Math.PI - angle0;
                }
                double angle1 = length.LineLocation.Direction.AngleTo(XYZ.BasisZ);
                if (angle1 > Math.PI / 2)
                {
                    angle1 = Math.PI - angle1;
                }
                double angle2 = width.LineLocation.Direction.AngleTo(XYZ.BasisZ);
                if (angle2 > Math.PI / 2)
                {
                    angle2 = Math.PI - angle2;
                }

                if (angle0 < angle1 && angle0 < angle2)
                {
                    if (location.LineLocation.Length > width.LineLocation.Length)
                    {
                        Location = length.LineLocation;
                        Length = location.LineLocation;
                        Width = width.LineLocation;
                    }
                    else
                    {
                        Location = length.LineLocation;
                        Length = width.LineLocation;
                        Width = location.LineLocation;
                    }
                }
                else
                {
                    Location = location.LineLocation;
                    Length = length.LineLocation;
                    Width = width.LineLocation;
                }
            }
            else if (ElemType == ObjectIFCType.Column)
            {
                double angle0 = location.LineLocation.Direction.AngleTo(XYZ.BasisZ);
                if (angle0 > Math.PI / 2)
                {
                    angle0 = Math.PI - angle0;
                }
                double angle1 = length.LineLocation.Direction.AngleTo(XYZ.BasisZ);
                if (angle1 > Math.PI / 2)
                {
                    angle1 = Math.PI - angle1;
                }
                double angle2 = width.LineLocation.Direction.AngleTo(XYZ.BasisZ);
                if (angle2 > Math.PI / 2)
                {
                    angle2 = Math.PI - angle2;
                }

                if (angle1 < angle0 && angle1 < angle2)
                {
                    if (location.LineLocation.Length > width.LineLocation.Length)
                    {
                        Location = length.LineLocation;
                        Length = location.LineLocation;
                        Width = width.LineLocation;
                    }
                    else
                    {
                        Location = length.LineLocation;
                        Length = width.LineLocation;
                        Width = location.LineLocation;
                    }
                }
                else if (angle2 < angle0 && angle2 < angle1)
                {
                    if (location.LineLocation.Length > length.LineLocation.Length)
                    {
                        Location = width.LineLocation;
                        Length = location.LineLocation;
                        Width = length.LineLocation;
                    }
                    else
                    {
                        Location = width.LineLocation;
                        Length = length.LineLocation;
                        Width = location.LineLocation;
                    }
                }
                else
                {
                    Location = location.LineLocation;
                    Length = length.LineLocation;
                    Width = width.LineLocation;
                }
            }
            else if (ElemType == ObjectIFCType.Duct || ElemType == ObjectIFCType.CableTray)
            {
                Location = location.LineLocation;
                Length = length.LineLocation;
                Width = width.LineLocation;

                BoundingBoxXYZ box = GeometryUtils.GetBoudingBoxExtend(LinkElem, null);
                Outline outline = new Outline(box.Min, box.Max);

                BoundingBoxIntersectsFilter boxFilter = new BoundingBoxIntersectsFilter(outline);
                var filterElems = new FilteredElementCollector(LinkDoc).WhereElementIsNotElementType().WherePasses(boxFilter).ToElements().Where(x => !x.Id.Equals(LinkElem.Id)).ToList();

                BoundingBoxXYZ boxElem = null;
                XYZ midElem = (box.Min + box.Max) / 2;
                if (filterElems?.Count > 0)
                {
                    double distance = double.NaN;
                    foreach (var elem in filterElems)
                    {
                        BoundingBoxXYZ boxXYZ = GeometryUtils.GetBoudingBoxExtend(elem, null);
                        if (boxXYZ == null)
                        {
                            continue;
                        }
                        XYZ mid = (boxXYZ.Min + boxXYZ.Max) / 2;
                        double d = midElem.DistanceTo(mid);

                        if (double.IsNaN(distance) || distance < d)
                        {
                            boxElem = boxXYZ;
                            distance = d;
                        }
                    }
                }
                if (boxElem != null)
                {
                    XYZ mid = (boxElem.Min + boxElem.Max) / 2;

                    double d0 = location.LineLocation.Distance(mid);
                    double d1 = length.LineLocation.Distance(mid);
                    double d2 = width.LineLocation.Distance(mid);

                    if (d1 < d0 && d1 < d2)
                    {
                        Location = length.LineLocation;
                        Length = location.LineLocation;
                        Width = width.LineLocation;
                    }
                    else if (d2 < d0 && d2 < d1)
                    {
                        Location = width.LineLocation;
                        Length = location.LineLocation;
                        Width = length.LineLocation;
                    }
                }

                if (ElemType == ObjectIFCType.Duct && !ValidationPipeDuct(Location))
                {
                    Location = null;
                }
            }
            else if (ElemType != ObjectIFCType.Pipe)
            {
                Location = location.LineLocation;
                Length = length.LineLocation;
                Width = width.LineLocation;
            }
        }

        public Plane SortFaceByProject(XYZ direction, List<Plane> planes, List<XYZ> verticals)
        {
            List<Plane> planeGetTop = new List<Plane>();

            // check vector project with vector basic z
            if (planes?.Count > 0 && verticals?.Count > 0)
            {
                foreach (Plane plane in planes)
                {
                    double ange = plane.Normal.AngleTo(XYZ.BasisZ.Negate());
                    if (RevitUtils.IsGreaterThanOrEqual(ange, Math.PI / 2)
                        && !RevitUtils.IsParallel(plane.Normal, direction))
                    {
                        if (GeometryUtils.IsTopFace(plane, GeomertyData.Vertices))
                        {
                            planeGetTop.Add(plane);
                        }
                    }
                }
            }
            return planeGetTop.Last();
        }

        private Line ExtendLine(Line line, double length)
        {
            double extend = (length - line.Length) / 2;
            XYZ start = line.GetEndPoint(0) - extend * line.Direction;
            XYZ end = line.GetEndPoint(1) + extend * line.Direction;

            return Line.CreateBound(start, end);
        }

        private bool ValidationPipeDuct(Line location)
        {
            XYZ center = location.GetEndPoint(0);
            Plane plane = Plane.CreateByNormalAndOrigin(location.Direction, center);
            List<XYZ> points = new List<XYZ>();
            GeomertyData.Vertices.ForEach(x => points.Add(UtilsPlane.ProjectOnto(plane, x)));

            double distance = center.DistanceTo(points.First());
            if (points.Any(x => !RevitUtils.IsEqual(x.DistanceTo(center), distance, distance / 10)))
            {
                return false;
            }

            return true;
        }

        private bool ValidationPipeDuctCylinder(Line location)
        {
            XYZ center = location.GetEndPoint(0);
            Plane plane = Plane.CreateByNormalAndOrigin(location.Direction, center);

            List<XYZ> points = new List<XYZ>();
            GeomertyData.Vertices.ForEach(x => points.Add(UtilsPlane.ProjectOnto(plane, x)));

            double distance1 = center.DistanceTo(points.First());

            List<XYZ> pointsOuterDiameter = new List<XYZ>();
            List<XYZ> pointInterDiameter = new List<XYZ>();

            if (points?.Count < 6)
                return false;

            foreach (XYZ point in points)
            {
                if (RevitUtils.IsEqual(point.DistanceTo(center), distance1, distance1 / 10))
                    pointsOuterDiameter.Add(point);
                else
                    pointInterDiameter.Add(point);
            }

            if (pointInterDiameter.Count == 0)
                return true;
            else
            {
                if (pointInterDiameter.Count < 6 || pointsOuterDiameter.Count < 6)
                    return false;

                double distance2 = center.DistanceTo(pointInterDiameter.First());
                if (pointInterDiameter.Any(x => !RevitUtils.IsEqual(x.DistanceTo(center), distance2, distance2 / 10)))
                    return false;
            }

            return true;
        }

        #region Location

        private void InitDimensions(GeometryData data, out DataDimension location, out DataDimension length, out DataDimension width)
        {
            location = null;
            length = null;
            width = null;
            XYZ vectorLocation = GetVectorLocation(data.Faces);
            if (vectorLocation != null)
            {
                RevitUtils.SortPointsToDirection(data.Vertices, vectorLocation);
                Plane plane = Plane.CreateByNormalAndOrigin(vectorLocation, data.Vertices.First());
                XYZ project = UtilsPlane.ProjectOnto(plane, data.Vertices.Last());
                if (project.DistanceTo(data.Vertices.Last()) < RevitUtils.MIN_LENGTH)
                {
                    return;
                }
                Line dim1 = Line.CreateBound(project, data.Vertices.Last());
                XYZ mid1 = (dim1.GetEndPoint(0) + dim1.GetEndPoint(1)) / 2;
                plane = Plane.CreateByNormalAndOrigin(vectorLocation, mid1);
                List<XYZ> points = GeometryUtils.SimplePoints(plane, data.Vertices);
                int count = points.Count;
                if (count > 3)
                {
                    List<Line> edges = new List<Line>();
                    for (int i = 0; i < count; i++)
                    {
                        XYZ pLast = i == 0 ? points[count - 1] : points[i - 1];
                        if (points[i].DistanceTo(pLast) > RevitUtils.MIN_LENGTH)
                        {
                            Line line = Line.CreateBound(points[i], pLast);
                            edges.Add(line);

                            //DrawLine(line);
                        }
                    }

                    XYZ direction = null;
                    double lengthMax = 0;
                    for (int i = 0; i < edges.Count - 1; i++)
                    {
                        for (int j = i + 1; j < edges.Count; j++)
                        {
                            Line lineI = edges[i];
                            Line lineJ = edges[j];

                            if (RevitUtils.IsParallel(lineI.Direction, lineJ.Direction))
                            {
                                double total = lineI.Length + lineJ.Length;
                                if (total > lengthMax)
                                {
                                    direction = lineI.Direction;
                                    lengthMax = total;
                                }
                            }
                        }
                    }

                    if (direction == null && count > 6)
                    {
                        RevitUtils.FindMaxMinPoint(points, out XYZ min, out XYZ max);
                        XYZ center = (min + max) / 2;
                        center = UtilsPlane.ProjectOnto(plane, center);

                        if (GeometryUtils.IsCircle(points, center) == true)
                        {
                            direction = points.First() - center;
                        }
                    }

                    if (direction != null && direction.GetLength() > 0)
                    {
                        RevitUtils.SortPointsToDirection(points, direction);
                        Line centerLine = Line.CreateUnbound(mid1, direction);
                        Line dim2 = Line.CreateBound(centerLine.Project(points.First()).XYZPoint, centerLine.Project(points.Last()).XYZPoint);
                        XYZ mid2 = (dim2.GetEndPoint(0) + dim2.GetEndPoint(1)) / 2;

                        direction = direction.CrossProduct(dim1.Direction);
                        RevitUtils.SortPointsToDirection(points, direction);
                        centerLine = Line.CreateUnbound(mid1, direction);
                        Line dim3 = Line.CreateBound(centerLine.Project(points.First()).XYZPoint, centerLine.Project(points.Last()).XYZPoint);
                        XYZ mid3 = (dim3.GetEndPoint(0) + dim3.GetEndPoint(1)) / 2;

                        XYZ mid = mid1;
                        XYZ move = mid2 + mid3 - 2 * mid1;
                        if (!RevitUtils.IsEqual(move, XYZ.Zero))
                        {
                            Transform transform = Transform.CreateTranslation(move);
                            mid = transform.OfPoint(mid1);
                        }

                        List<DataDimension> dimensions = new List<DataDimension>();
                        if (!RevitUtils.IsEqual(mid, mid1))
                        {
                            Transform transform = Transform.CreateTranslation(mid - mid1);
                            dim1 = Line.CreateBound(transform.OfPoint(dim1.GetEndPoint(0)), transform.OfPoint(dim1.GetEndPoint(1)));
                        }
                        dimensions.Add(new DataDimension(dim1, points, mid));

                        if (!RevitUtils.IsEqual(mid, mid2))
                        {
                            Transform transform = Transform.CreateTranslation(mid - mid2);
                            dim2 = Line.CreateBound(transform.OfPoint(dim2.GetEndPoint(0)), transform.OfPoint(dim2.GetEndPoint(1)));
                        }
                        dimensions.Add(new DataDimension(dim2, data.Vertices, mid));

                        if (!RevitUtils.IsEqual(mid, mid3))
                        {
                            Transform transform = Transform.CreateTranslation(mid - mid3);
                            dim3 = Line.CreateBound(transform.OfPoint(dim3.GetEndPoint(0)), transform.OfPoint(dim3.GetEndPoint(1)));
                        }
                        dimensions.Add(new DataDimension(dim3, data.Vertices, mid));

                        if (dimensions.Any(x => x.IsCylinder))
                        {
                            dimensions = dimensions.OrderByDescending(x => x.IsCircle).ThenByDescending(x => x.LineLocation.Length).ToList();
                            location = dimensions[0];
                            length = dimensions[1];
                            width = dimensions[2];
                        }

                        //DrawLine(dim1);
                        //DrawLine(dim2);
                        //DrawLine(dim3);
                    }
                }
            }
        }

        private XYZ GetVectorLocation(List<FaceData> planes)
        {
            XYZ vectorParallel = null;
            XYZ vectorPerpendicular = null;

            if (planes?.Count > 1)
            {
                double areaParallelMax = 0;
                double areaPerpendicularMax = 0;
                int count = planes.Count;
                for (int i = 0; i < count - 1; i++)
                {
                    for (int j = i + 1; j < count; j++)
                    {
                        FaceData planeI = planes[i];
                        FaceData planeJ = planes[j];

                        double area = planeI.Area + planeJ.Area;
                        if (RevitUtils.IsParallel(planeI.Plane.Normal, planeJ.Plane.Normal))
                        {
                            if (area > areaParallelMax)
                            {
                                vectorParallel = planeI.Plane.Normal;
                                areaParallelMax = area;
                            }
                        }
                        else if (RevitUtils.IsPerpendicular(planeI.Plane.Normal, planeJ.Plane.Normal))
                        {
                            if (area > areaPerpendicularMax)
                            {
                                vectorPerpendicular = planeI.Plane.Normal.CrossProduct(planeJ.Plane.Normal);
                                areaPerpendicularMax = area;
                            }
                        }
                    }
                }
            }

            return vectorParallel ?? vectorPerpendicular;
        }

        #endregion Location

        #region Transform IFC

        /// <summary>
        /// get location line tranform of object
        /// </summary>
        /// <param name="pointLocations"></param>
        /// <returns></returns>
        public Line GetLocationLineTranformOfObject(RevitLinkInstance revLinkIns, List<XYZ> pointLocations)
        {
            if (revLinkIns != null
                && pointLocations?.Count > 0
                && !RevitUtils.IsEqual(pointLocations[0], pointLocations[1]))
            {
                Transform ifcTransform = revLinkIns.GetTotalTransform();

                Line lineLocation = Line.CreateBound(ifcTransform.OfPoint(pointLocations[0]), ifcTransform.OfPoint(pointLocations[1]));
                if (lineLocation != null && lineLocation.Length >= RevitUtilities.Common.MIN_LENGTH)
                    return lineLocation;
            }
            return null;
        }

        public XYZ GetTransformFacingOfObjectIFC(RevitLinkInstance revLinkIns, XYZ facing)
        {
            // Transform facing of object ifc
            if (revLinkIns != null && revLinkIns != null)
            {
                Transform ifcTransform = revLinkIns.GetTotalTransform();
                return ifcTransform.OfVector(facing);
            }

            return null;
        }

        #endregion Transform IFC

        #region Test Draw

        public static void DrawPoint(XYZ point)
        {
            Plane plane = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, point);
            SketchPlane sp = SketchPlane.Create(Doc, plane);

            Line line = Line.CreateBound(point - 10 / 304.8 * XYZ.BasisX, point + 10 / 304.8 * XYZ.BasisX);
            Line line1 = Line.CreateBound(point - 10 / 304.8 * XYZ.BasisY, point + 10 / 304.8 * XYZ.BasisY);

            var model = Doc.Create.NewModelCurve(line, sp);
            var model1 = Doc.Create.NewModelCurve(line1, sp);
        }

        public static Element DrawLine(Line line)
        {
            if (line != null)
            {
                XYZ refVector = line.Direction.IsAlmostEqualTo(XYZ.BasisX) || line.Direction.IsAlmostEqualTo(XYZ.BasisX.Negate()) ? XYZ.BasisZ : XYZ.BasisX;
                XYZ normal = line.Direction.CrossProduct(refVector);
                Plane plane = Plane.CreateByNormalAndOrigin(normal, line.Origin);
                SketchPlane sp = SketchPlane.Create(Doc, plane);
                return Doc.Create.NewModelCurve(line, sp);
            }
            return null;
        }

        #endregion Test Draw
    }

    public class DataDimension
    {
        public Line LineLocation { get; set; }
        public bool IsCylinder { get; set; }
        public bool IsCircle { get; set; }

        public DataDimension(Line location, List<XYZ> points, XYZ mid)
        {
            LineLocation = location;
            IsCylinder = IsCylinderFace(location, points, mid, out bool isCircle);
            IsCircle = isCircle;
        }

        public bool IsCylinderFace(Line location, List<XYZ> points, XYZ center, out bool isCircle)
        {
            isCircle = false;
            Plane face = Plane.CreateByNormalAndOrigin(location.Direction, center);
            if (face != null && points?.Count > 0 && center != null)
            {
                List<XYZ> simplePoints = GeometryUtils.SimplePoints(face, points);
                if (simplePoints?.Count > 3)
                {
                    center = UtilsPlane.ProjectOnto(face, center);
                    bool? checking = GeometryUtils.IsCircle(simplePoints, center);
                    if (checking == false)
                    {
                        return false;
                    }
                    isCircle = checking == true;

                    return true;
                }
            }
            return false;
        }
    }
}