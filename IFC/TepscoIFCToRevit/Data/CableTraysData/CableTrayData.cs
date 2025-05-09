using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.UI;
using RevitUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Windows.Controls;
using TepscoIFCToRevit.Common;
using Line = Autodesk.Revit.DB.Line;

namespace TepscoIFCToRevit.Data.CableTraysData
{
    public class CableTrayData : ElementConvert
    {
        public bool IsElbow { get; set; } = false;
        public ConnectorProfileType CableConnectorProfileType { get; set; }

        public PlanarFace FacePerpendicularWithLocation = null;

        public double RealWidthCableTray { get; set; }

        public double WidthCableTray { get; set; }

        public double RealHeightCableTray { get; set; }

        public double HeightCableTray { get; set; }

        public CableTrayData(UIDocument uiDoc,
                             LinkElementData linkElementData,
                             ElementId cableTrayTypeId,
                             RevitLinkInstance revLinkIns,
                             ConvertParamData paramData = null)
        {
            if (uiDoc != null
                && linkElementData != null
                && linkElementData.LinkElement != null
                && linkElementData.LinkElement.IsValidObject
                && revLinkIns != null)
            {
                _uiDoc = uiDoc;
                _doc = _uiDoc.Document;
                LinkEleData = linkElementData;
                TypeId = cableTrayTypeId;
                LinkInstance = revLinkIns;
                ParameterData = paramData;
            }
        }

        public void Initialize()
        {
            if (Location == null)
                return;

            GetShape();
            GetDiameterWidthHeightFromCableTrayType();
            CreateCableTrayPointToPoint();
            SetHeightWidthCableTray();

            if (ConvertElem?.IsValidObject == true && ConvertElem is CableTray cable)
            {
                RotateCableTray(cable);
                RevitUtils.SetValueParamterConvert(_uiDoc, ConvertElem, LinkEleData, ParameterData);
            }
        }

        private void GetShape()
        {
            try
            {
                if (_uiDoc != null && TypeId != null && TypeId != ElementId.InvalidElementId)
                {
                    if (_uiDoc.Document.GetElement(TypeId) is CableTrayType cableTrayType)
                        CableConnectorProfileType = cableTrayType.Shape;
                }
            }
            catch (Exception) { }
        }

        private void CreateCableTrayPointToPoint()
        {
            try
            {
                XYZ direction = (EndPoint - StartPoint).Normalize();
                if (StartPoint.DistanceTo(EndPoint) <= RevitUtilities.Common.MIN_LENGTH)
                {
                    EndPoint = StartPoint + direction * RevitUtilities.Common.MIN_LENGTH;
                }
                if (RevitUtils.IsEqual(direction, XYZ.BasisZ.Negate()))
                {
                    XYZ temp = StartPoint;
                    StartPoint = EndPoint;
                    EndPoint = temp;
                }

                ConvertElem = CableTray.Create(_doc, TypeId, StartPoint, EndPoint, LevelId);
            }
            catch (Exception) { }
        }

        public void GetGeometryFromIFCElement(List<GeometryObject> geometries = null)
        {
            try
            {
                IFCdata = new ElementIFC(_doc, LinkEleData.LinkElement, LinkInstance.GetLinkDocument(), ObjectIFCType.CableTray, geometries, null);
                if (IFCdata.Location != null)
                {
                    StartPoint = LinkTransform.OfPoint(IFCdata.Location.GetEndPoint(0));
                    EndPoint = LinkTransform.OfPoint(IFCdata.Location.GetEndPoint(1));
                    Location = Line.CreateBound(StartPoint, EndPoint);
                    LevelId = RevitUtils.GetLevelClosetTo(_doc, StartPoint);
                    RealWidthCableTray = IFCdata.Length.Length;
                    RealHeightCableTray = IFCdata.Width.Length;
                }
            }
            catch (Exception) { }
        }

        private void GetDiameterWidthHeightFromCableTrayType()
        {
            try
            {
                List<double> sizeRectagular = new List<double>();
                CableTraySizes sizeSettings = CableTraySizes.GetCableTraySizes(_uiDoc.Document);
                foreach (var item in sizeSettings)
                {
                    if (item == null)
                        continue;

                    double size = item.NominalDiameter;

                    sizeRectagular.Add(size);
                }

                double closetWidth = FindClosestNumber(RealWidthCableTray, sizeRectagular);
                WidthCableTray = closetWidth != double.MinValue ? closetWidth : RealWidthCableTray;

                double closetHeight = FindClosestNumber(RealHeightCableTray, sizeRectagular);
                HeightCableTray = closetHeight != double.MinValue ? closetHeight : RealHeightCableTray;
            }
            catch (Exception) { }
        }

        private void SetHeightWidthCableTray()
        {
            try
            {
                if (ConvertElem != null && WidthCableTray > 0)
                {
                    if (!UtilsParameter.SetValueParameterBuiltIn(ConvertElem, BuiltInParameter.RBS_CURVE_WIDTH_PARAM, WidthCableTray))
                    {
                        ConvertElem.LookupParameter("Width")?.Set(WidthCableTray);
                        ConvertElem.LookupParameter("幅")?.Set(WidthCableTray);
                    }
                }

                if (ConvertElem != null && HeightCableTray > 0)
                {
                    if (!UtilsParameter.SetValueParameterBuiltIn(ConvertElem, BuiltInParameter.RBS_CURVE_HEIGHT_PARAM, HeightCableTray))
                    {
                        ConvertElem.LookupParameter("Height")?.Set(HeightCableTray);
                        ConvertElem.LookupParameter("高さ")?.Set(HeightCableTray);
                    }
                }
            }
            catch (Exception) { }
        }

        private double FindClosestNumber(double x, List<double> list)
        {
            try
            {
                if (list == null || list.Count <= 0)
                    return double.MinValue;

                double closest = list[0];
                double difference = Math.Abs(x - closest);
                for (int i = 1; i < list.Count; i++)
                {
                    double currentDifference = Math.Abs(x - list[i]);
                    if (currentDifference < difference)
                    {
                        closest = list[i];
                        difference = currentDifference;
                    }
                }
                return closest;
            }
            catch (Exception) { }

            return double.MinValue;
        }

        private void RotateCableTray(CableTray cableTray)
        {
            try
            {
                List<XYZ> lstCentroidSolid = new List<XYZ>();

                List<XYZ> CentroidAllSolid = new List<XYZ>();

                List<XYZ> CentroidAllSolidWithOut2Solid = new List<XYZ>();

                List<Solid> solidIFC = UtilsSolid.GetAllSolids(LinkEleData.LinkElement);

                List<Solid> solidIfcWithOut2Solid = solidIFC;

                if (cableTray == null || solidIFC == null)
                    return;

                if (cableTray?.IsValidObject == true)
                {
                    if (!RevitUtils.IsParallel(Location.Direction, XYZ.BasisZ))
                        RotateElementWithSolid(cableTray, solidIFC);
                    else
                    {
                        Line lineWidth = IFCdata.Width;

                        BoundingBoxXYZ box = GeometryUtils.GetBoudingBoxExtend(IFCdata.LinkElem, null);
                        Outline outline = new Outline(box.Min, box.Max);

                        BoundingBoxIntersectsFilter boxFilter = new BoundingBoxIntersectsFilter(outline);
                        var filterElems = new FilteredElementCollector(IFCdata.LinkDoc).WhereElementIsNotElementType().WherePasses(boxFilter).ToElements().Where(x => !x.Id.Equals(IFCdata.LinkElem.Id)).ToList();

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

                            Plane plane = Plane.CreateByNormalAndOrigin(lineWidth.Direction, midElem);
                            XYZ project = UtilsPlane.ProjectOnto(plane, mid);
                            XYZ direction = (mid - project).Normalize();
                            if (RevitUtils.IsEqual(direction, lineWidth.Direction))
                            {
                                lineWidth = lineWidth.CreateReversed() as Line;
                            }
                        }

                        Line line = Line.CreateBound(LinkTransform.OfPoint(lineWidth.GetEndPoint(0)), LinkTransform.OfPoint(lineWidth.GetEndPoint(1)));
                        Line lineAx = RevitUtils.IsParallel(XYZ.BasisZ, Location.Direction) ? Line.CreateUnbound(LinkTransform.OfPoint(IFCdata.Location.GetEndPoint(0)), XYZ.BasisZ) : Line.CreateBound(LinkTransform.OfPoint(IFCdata.Location.GetEndPoint(0)), LinkTransform.OfPoint(IFCdata.Location.GetEndPoint(1)));
                        double angleRotate = cableTray.CurveNormal.AngleOnPlaneTo(line.Direction, lineAx.Direction);
                        ElementTransformUtils.RotateElement(cableTray.Document, cableTray.Id, lineAx, angleRotate);
                    }
                    _doc.Regenerate();
                }
            }
            catch (Exception) { }
        }

        private void RotateElementWithSolid(CableTray cableTray, List<Solid> smallSolids)
        {
            if (cableTray == null && smallSolids == null)
                return;

            if (Location != null && smallSolids != null)
            {
                List<XYZ> lstCentroidSol = new List<XYZ>();
                List<Line> linesIfc = new List<Line>();
                List<Line> linesElem = new List<Line>();

                foreach (var point in smallSolids)
                {
                    XYZ pointAfterTrans = LinkTransform.OfPoint(point.ComputeCentroid());

                    lstCentroidSol.Add(pointAfterTrans);
                }

                var locCur = (cableTray.Location as LocationCurve).Curve;
                Line locLine = locCur as Line;
                XYZ refVector = locLine.Direction.CrossProduct((cableTray as CableTray).CurveNormal);
                var distances = GetRungDistance(locLine, lstCentroidSol, refVector);

                var lstsol = lstCentroidSol.OrderBy(Location.Direction);

                double RemainLength = locLine.Length - distances.Last();

                if (distances.First() < RemainLength)
                {
                    XYZ centerLoc = (locLine.GetEndPoint(0) + locLine.GetEndPoint(1)) / 2;
                    Line lineAx = Line.CreateBound(centerLoc, centerLoc + cableTray.CurveNormal);
                    ElementTransformUtils.RotateElement(cableTray.Document, cableTray.Id, lineAx, Math.PI);

                    cableTray.RungSpace = RemainLength;
                }
                else
                {
                    cableTray.RungSpace = distances.First();
                }
            }
        }

        private List<double> GetRungDistance(Line locLine, List<XYZ> points, XYZ refVector)
        {
            XYZ dir = locLine.Direction;
            XYZ org = locLine.GetEndPoint(0);

            return points.OrderBy(dir).ConvertAll(x =>
            {
                XYZ vec = x - org;
                double angle = dir.AngleOnPlaneTo(vec, refVector);
                return vec.GetLength() * Math.Cos(angle);
            });
        }
    }
}