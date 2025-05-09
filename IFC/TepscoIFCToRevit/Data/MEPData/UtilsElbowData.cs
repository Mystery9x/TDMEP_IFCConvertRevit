using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using RevitUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using TepscoIFCToRevit.Common;

namespace TepscoIFCToRevit.Data.MEPData
{
    public static class UtilsElbowData
    {
        #region GetPipeDuctProper

        public static List<MEPCurve> GetPipeDuctPoper(Document doc, RevitLinkInstance linkInstance, Element linkElem, Solid solidIFC, List<Element> elementConvert)
        {
            Transform linkTransform = linkInstance?.GetTotalTransform();
            ElementIFC elementIFC = new ElementIFC(doc, linkElem, linkInstance.GetLinkDocument(), ObjectIFCType.Pipe, null, solidIFC, true);
            if (elementIFC.IsCircle)
                return null;

            BoundingBoxXYZ boxSolid = solidIFC.GetBoundingBox();

            BoundingBoxXYZ boxFitting = GeometryUtils.GetBoudingBox(boxSolid, linkTransform);

            List<MEPCurve> mepConnects = new List<MEPCurve>();
            if (boxFitting != null)
            {
                List<Element> eleInterSectors = GeometryUtils.FindPipeDuctNearestBox(doc, elementConvert.Where(x => x != null && x.IsValidObject).Select(x => x.Id).ToList(), boxFitting);

                List<Element> eleProper = new List<Element>();
                if (eleInterSectors?.Count > 0)
                {
                    List<XYZ> verticalElbows = GetVerticalIFC(linkElem).Select(x => linkTransform.OfPoint(x)).ToList();
                    foreach (var pipeIntersec in eleInterSectors)
                    {
                        if (pipeIntersec.Location is LocationCurve lcCurve
                           && lcCurve.Curve is Line lcLine)
                        {
                            List<XYZ> pointIFCLinePlane = new List<XYZ>();
                            XYZ pointOrigin = null;
                            XYZ start = lcLine.GetEndPoint(0);
                            XYZ end = lcLine.GetEndPoint(1);

                            if (GeometryUtils.IsPointInBox(start, boxFitting))
                                pointOrigin = lcLine.GetEndPoint(0);
                            else if (GeometryUtils.IsPointInBox(end, boxFitting))
                                pointOrigin = lcLine.GetEndPoint(1);

                            if (pointOrigin != null)
                            {
                                Plane plane = Plane.CreateByNormalAndOrigin(lcLine.Direction, pointOrigin);

                                foreach (XYZ pointIFC in verticalElbows)
                                {
                                    if (RevitUtils.IsEqual(RevitUtils.GetSignedDistance(plane, pointIFC), 0))
                                        pointIFCLinePlane.Add(pointIFC);
                                }

                                bool isCylinder = IsCylinderFace(plane, pointIFCLinePlane, pointOrigin, out bool isCricle);
                                if (isCylinder)
                                    eleProper.Add(pipeIntersec);
                            }
                        }
                    }
                }

                if (eleProper?.Count > 0
                    && IsCheckSameType(eleProper, out List<Pipe> pipes, out List<Duct> ducts))
                {
                    if (pipes?.Count > 0)
                        mepConnects.AddRange(pipes);
                    else
                        mepConnects.AddRange(ducts);

                    if (!IsCoplanar(mepConnects))
                        return null;
                }
            }
            return mepConnects;
        }

        public static bool IsCoplanar(List<MEPCurve> mepConects)
        {
            if (mepConects.Count <= 4)
            {
                try
                {
                    List<XYZ> points = new List<XYZ>();
                    foreach (MEPCurve mep in mepConects)
                    {
                        if (mep.Location is LocationCurve lcCurve0
                           && lcCurve0.Curve is Line lcLine0)
                        {
                            points.Add(lcLine0.GetEndPoint(0));
                            points.Add(lcLine0.GetEndPoint(1));
                        }
                        else
                            return false;
                    }

                    if (points.Count > 0)
                    {
                        Plane plane = Plane.CreateByThreePoints(points[0], points[1], points[2]);

                        for (int i = 3; i < points.Count; i++)
                        {
                            double distance = RevitUtils.GetSignedDistance(plane, points[i]);
                            if (!RevitUtils.IsEqual(distance, 0, 6 / 304.8))
                                return false;
                        }
                    }
                }
                catch (Exception) { }
            }
            else
                return false;

            return true;
        }

        public static bool IsCheckSameType(List<Element> elements, out List<Pipe> pipes, out List<Duct> ducts)
        {
            pipes = new List<Pipe>();
            ducts = new List<Duct>();
            ConnectorProfileType type = ConnectorProfileType.Invalid;
            foreach (Element element in elements)
            {
                if (element is Pipe pipe)
                    pipes.Add(pipe);
                else if (element is Duct duct)
                {
                    if (type == ConnectorProfileType.Invalid)
                        type = duct.DuctType.Shape;
                    else
                    {
                        if (type != duct.DuctType.Shape)
                            return false;
                    }
                    ducts.Add(duct);
                }
            }

            if (pipes.Count == elements.Count
               || ducts.Count == elements.Count)
                return true;

            return false;
        }

        public static bool IsCylinderFace(Plane face, List<XYZ> points, XYZ center, out bool isCircle)
        {
            isCircle = false;
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

        public static List<XYZ> GetVerticalIFC(Element elem)
        {
            List<GeometryObject> geometries = GeometryUtils.GetIfcGeometriess(elem);
            List<XYZ> verticals = new List<XYZ>();
            if (geometries?.Count > 0)
            {
                foreach (var geo in geometries)
                {
                    if (geo is Solid solid
                        && solid != null
                        && solid.Volume > 0
                        && solid.Faces?.Size > 0)
                    {
                        var meshes = solid.Faces
                                    .Cast<Face>()
                                    .Select(x => x.Triangulate())
                                    .ToList();

                        foreach (var mesh in meshes)
                            verticals.AddRange(mesh.Vertices);
                    }
                    else if (geo is Mesh mesh)
                        verticals.AddRange(mesh.Vertices);
                }
            }
            return verticals;
        }

        #endregion GetPipeDuctProper
    }
}