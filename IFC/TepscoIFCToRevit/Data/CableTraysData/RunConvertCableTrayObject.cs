using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using RevitUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using TepscoIFCToRevit.Common;
using TepscoIFCToRevit.Data.MEPData;
using Line = Autodesk.Revit.DB.Line;

namespace TepscoIFCToRevit.Data.CableTraysData
{
    public class RunConvertCableTrayObject
    {
        public static bool ConvertCableTray(Document doc,
                                            CableTrayData data,
                                            ref List<CableTrayData> datasConverted,
                                            ref List<CableTrayData> datasNotConvented)
        {
            if (doc?.IsValidObject != true)
            {
                return false;
            }

            bool isSuccess = true;
            using (Transaction tr = new Transaction(doc, "Create Cable Tray"))
            {
                try
                {
                    data.GetGeometryFromIFCElement();
                    if (data.Location != null)
                    {
                        bool isCreate = true;
                        if (datasConverted?.Count > 0 && datasConverted.Any(x => RevitUtils.IsDuplicateLine(x.Location, data.Location)))
                        {
                            isCreate = false;
                        }

                        if (isCreate)
                        {
                            tr.Start();
                            FailureHandlingOptions fhOpts = tr.GetFailureHandlingOptions();

                            if (!IsFittingCableTray(data))
                            {
                                data.Initialize();
                            }

                            if (data.ConvertElem?.IsValidObject == true)
                            {
                                REVWarning1 supWarning = new REVWarning1(true);
                                fhOpts.SetFailuresPreprocessor(supWarning);
                                tr.SetFailureHandlingOptions(fhOpts);
                                tr.Commit(fhOpts);
                                datasConverted.Add(data);
                            }
                            else
                            {
                                tr.RollBack();
                                isSuccess = false;
                                datasNotConvented.Add(data);
                            }
                        }
                    }
                    else
                    {
                        isSuccess = false;
                        datasNotConvented.Add(data);
                    }
                }
                catch (Exception)
                {
                    if (tr.HasStarted())
                    {
                        tr.RollBack();
                    }
                    isSuccess = false;
                    datasNotConvented.Add(data);
                }
            }
            return isSuccess;
        }

        public static bool IsFittingCableTray(CableTrayData cableTrayData)
        {
            Plane referencePlane = Plane.CreateByNormalAndOrigin(cableTrayData.Location.Direction, cableTrayData.StartPoint);

            var ifcData = cableTrayData.IFCdata;

            List<XYZ> lstVertices = new List<XYZ>();

            ifcData.GeomertyData.Vertices.ForEach(x => lstVertices.Add(cableTrayData.LinkTransform.OfPoint(x)));

            if (lstVertices != null
                && lstVertices.Count > 0)
            {
                List<XYZ> pointOnPlane = GeometryUtils.GetPointsOnPlane(lstVertices, referencePlane);

                if (pointOnPlane.Count > 3)
                {
                    Line lengthLine = Line.CreateUnbound(cableTrayData.LinkTransform.OfPoint(ifcData.Length.Origin), cableTrayData.LinkTransform.OfVector(ifcData.Length.Direction));
                    Line widthLine = Line.CreateUnbound(cableTrayData.LinkTransform.OfPoint(ifcData.Width.Origin), cableTrayData.LinkTransform.OfVector(ifcData.Width.Direction));

                    List<XYZ> projectLength = new List<XYZ>();
                    List<XYZ> projectWidth = new List<XYZ>();
                    foreach (var p in pointOnPlane)
                    {
                        projectLength.Add(lengthLine.Project(p).XYZPoint);
                        projectWidth.Add(widthLine.Project(p).XYZPoint);
                    }
                    RevitUtilities.Common.SortPointsToDirection(projectLength, lengthLine.Direction);
                    RevitUtilities.Common.SortPointsToDirection(projectWidth, widthLine.Direction);

                    double length = projectLength.First().DistanceTo(projectLength.Last());
                    double width = projectWidth.First().DistanceTo(projectWidth.Last());

                    return !(RevitUtils.IsEqual(length, ifcData.Length.Length) && RevitUtils.IsEqual(width, ifcData.Width.Length));
                }
            }

            return true;
        }

        public static FamilyInstance CreateElbow(Document doc, List<CableTray> cables, CableTrayData elbow)
        {
            if (doc?.IsValidObject != true)
            {
                return null;
            }

            FamilyInstance fitting = null;
            using (Transaction tr = new Transaction(doc))
            {
                try
                {
                    FailureHandlingOptions failureRollBack = tr.GetFailureHandlingOptions();
                    failureRollBack.SetFailuresPreprocessor(new REVWarning3());
                    failureRollBack.SetClearAfterRollback(true);

                    BoundingBoxXYZ boxFitting = GeometryUtils.GetBoudingBoxExtend(elbow.LinkEleData.LinkElement, elbow.LinkTransform, RevitUtilities.Common.MIN_LENGTH * 5);
                    if (cables.Count == 2
                        && cables[0].Location is LocationCurve lcCurve0
                        && lcCurve0.Curve is Line lcLine0
                        && cables[1].Location is LocationCurve lcCurve1
                        && lcCurve1.Curve is Line lcLine1
                        && !UtilsCurve.IsLineStraight(lcLine0, lcLine1))
                    {
                        XYZ midFitting = (boxFitting.Min + boxFitting.Max) / 2;

                        Plane plane0;
                        double d0;
                        if (lcLine1.GetEndPoint(0).DistanceTo(midFitting) > lcLine1.GetEndPoint(1).DistanceTo(midFitting))
                        {
                            plane0 = Plane.CreateByThreePoints(lcLine0.GetEndPoint(0), lcLine0.GetEndPoint(1), lcLine1.GetEndPoint(0));
                            d0 = UtilsPlane.GetSignedDistance(plane0, lcLine1.GetEndPoint(1));
                        }
                        else
                        {
                            plane0 = Plane.CreateByThreePoints(lcLine0.GetEndPoint(0), lcLine0.GetEndPoint(1), lcLine1.GetEndPoint(1));
                            d0 = UtilsPlane.GetSignedDistance(plane0, lcLine1.GetEndPoint(0));
                        }

                        Plane plane1;
                        double d1;
                        if (lcLine0.GetEndPoint(0).DistanceTo(midFitting) > lcLine0.GetEndPoint(1).DistanceTo(midFitting))
                        {
                            plane1 = Plane.CreateByThreePoints(lcLine1.GetEndPoint(0), lcLine1.GetEndPoint(1), lcLine0.GetEndPoint(0));
                            d1 = UtilsPlane.GetSignedDistance(plane1, lcLine0.GetEndPoint(1));
                        }
                        else
                        {
                            plane1 = Plane.CreateByThreePoints(lcLine1.GetEndPoint(0), lcLine1.GetEndPoint(1), lcLine0.GetEndPoint(1));
                            d1 = UtilsPlane.GetSignedDistance(plane1, lcLine0.GetEndPoint(0));
                        }

                        if (Math.Abs(UtilsPlane.GetSignedDistance(plane0, midFitting)) < RevitUtilities.Common.MIN_LENGTH &&
                            Math.Abs(UtilsPlane.GetSignedDistance(plane1, midFitting)) < RevitUtilities.Common.MIN_LENGTH)
                        {
                            Line newLocation0;
                            Line newLocation1;
                            XYZ direction;
                            XYZ project;
                            if (Math.Abs(d0) > Math.Abs(d1))
                            {
                                if (lcLine0.GetEndPoint(0).DistanceTo(midFitting) > lcLine0.GetEndPoint(1).DistanceTo(midFitting))
                                {
                                    project = UtilsPlane.ProjectOnto(plane1, lcLine0.GetEndPoint(1));
                                    direction = (project - lcLine0.GetEndPoint(0)).Normalize();
                                    newLocation0 = Line.CreateBound(lcLine0.GetEndPoint(0), project + RevitUtilities.Common.MIN_LENGTH * direction);
                                }
                                else
                                {
                                    project = UtilsPlane.ProjectOnto(plane1, lcLine0.GetEndPoint(0));
                                    direction = (project - lcLine0.GetEndPoint(1)).Normalize();
                                    newLocation0 = Line.CreateBound(project + RevitUtilities.Common.MIN_LENGTH * direction, lcLine0.GetEndPoint(1));
                                }
                                newLocation1 = lcLine1.GetEndPoint(0).DistanceTo(midFitting) > lcLine1.GetEndPoint(1).DistanceTo(midFitting)
                                    ? Line.CreateBound(lcLine1.GetEndPoint(0), lcLine1.GetEndPoint(1) + RevitUtilities.Common.MIN_LENGTH * lcLine1.Direction)
                                    : Line.CreateBound(lcLine1.GetEndPoint(0) - RevitUtilities.Common.MIN_LENGTH * lcLine1.Direction, lcLine1.GetEndPoint(1));
                            }
                            else
                            {
                                if (lcLine1.GetEndPoint(0).DistanceTo(midFitting) > lcLine1.GetEndPoint(1).DistanceTo(midFitting))
                                {
                                    project = UtilsPlane.ProjectOnto(plane0, lcLine1.GetEndPoint(1));
                                    direction = (project - lcLine1.GetEndPoint(0)).Normalize();
                                    newLocation1 = Line.CreateBound(lcLine1.GetEndPoint(0), project + RevitUtilities.Common.MIN_LENGTH * direction);
                                }
                                else
                                {
                                    project = UtilsPlane.ProjectOnto(plane0, lcLine1.GetEndPoint(0));
                                    direction = (project - lcLine1.GetEndPoint(1)).Normalize();
                                    newLocation1 = Line.CreateBound(project + RevitUtilities.Common.MIN_LENGTH * direction, lcLine1.GetEndPoint(1));
                                }
                                newLocation0 = lcLine0.GetEndPoint(0).DistanceTo(midFitting) > lcLine0.GetEndPoint(1).DistanceTo(midFitting)
                                    ? Line.CreateBound(lcLine0.GetEndPoint(0), lcLine0.GetEndPoint(1) + RevitUtilities.Common.MIN_LENGTH * lcLine0.Direction)
                                    : Line.CreateBound(lcLine0.GetEndPoint(0) - RevitUtilities.Common.MIN_LENGTH * lcLine0.Direction, lcLine0.GetEndPoint(1));
                            }

                            tr.Start("Create Fitting");
                            using (SubTransaction subTr = new SubTransaction(doc))
                            {
                                subTr.Start();
                                (cables[0].Location as LocationCurve).Curve = newLocation0;
                                (cables[1].Location as LocationCurve).Curve = newLocation1;
                                subTr.Commit();
                            }

                            using (SubTransaction reSubTrans = new SubTransaction(doc))
                            {
                                reSubTrans.Start();
                                fitting = GeometryUtils.CreateCableTrayConnector(cables[0], cables[1], out Connector pipeConnector1, out Connector pipeConnector2);
                                reSubTrans.Commit();
                            }

                            doc.Regenerate();
                            if (fitting?.IsValidObject == true)
                            {
                                tr.Commit(failureRollBack);

                                Parameter paramType = elbow.LinkEleData.LinkElement.GetParameters("ObjectTypeOverride")?.FirstOrDefault();
                                string typeIfcName = paramType?.AsString();
                                string typeName = fitting.Symbol.FamilyName + ":" + fitting.Symbol.Name;
                                if (!string.IsNullOrWhiteSpace(typeIfcName) && !typeName.Equals(typeIfcName))
                                {
                                    foreach (ElementId symbolId in fitting.Symbol.GetSimilarTypes())
                                    {
                                        if (doc.GetElement(symbolId) is FamilySymbol symbol)
                                        {
                                            typeName = symbol.FamilyName + ":" + symbol.Name;
                                            if (!typeName.Equals(typeIfcName))
                                            {
                                                continue;
                                            }

                                            tr.Start("Change type");
                                            fitting.Symbol = symbol;
                                            tr.Commit();

                                            break;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                tr.RollBack();
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    if (tr.HasStarted())
                    {
                        tr.RollBack();
                    }
                }
            }

            return fitting?.IsValidObject == true ? fitting : null;
        }

        public static FamilyInstance CreateTeeFitting(Document doc, List<CableTray> cableTrayInterSec, CableTrayData cableTrayDataItm)
        {
            if (doc?.IsValidObject != true)
            {
                return null;
            }
            if (IsCreateCableTrayTeeFitting(cableTrayDataItm, ref cableTrayInterSec))
            {
                var fitting = CreateCableTrayTeeFitting(doc, cableTrayInterSec);
                if (fitting?.IsValidObject == true)
                    return fitting;
            }

            return null;
        }

        private static FamilyInstance CreateCableTrayTeeFitting(Document doc, List<CableTray> cableTrays)
        {
            if (doc?.IsValidObject != true)
            {
                return null;
            }

            FamilyInstance fitting = null;
            using (Transaction tr = new Transaction(doc))
            {
                try
                {
                    FailureHandlingOptions failureRollBack = tr.GetFailureHandlingOptions();
                    failureRollBack.SetFailuresPreprocessor(new REVWarning3());
                    failureRollBack.SetClearAfterRollback(true);

                    tr.Start("Create tee");

                    fitting = CreateCableTrayTeeWyeFitting(doc, cableTrays[0], cableTrays[1], cableTrays[2]);
                    if (fitting?.IsValidObject == true)
                        tr.Commit(failureRollBack);
                    else
                        tr.RollBack();
                }
                catch (Exception)
                {
                    if (tr.HasStarted())
                        tr.RollBack();
                }
            }

            return fitting?.IsValidObject == true ? fitting : null;
        }

        public static FamilyInstance CreateCableTrayCrossFitting(Document doc, List<CableTray> cableTrays)
        {
            if (doc?.IsValidObject != true)
            {
                return null;
            }

            FamilyInstance fitting = null;
            using (Transaction tr = new Transaction(doc))
            {
                try
                {
                    FailureHandlingOptions failureRollBack = tr.GetFailureHandlingOptions();
                    failureRollBack.SetFailuresPreprocessor(new REVWarning3());
                    failureRollBack.SetClearAfterRollback(true);

                    tr.Start("Create cross");

                    fitting = CreateCrossFitting(doc, cableTrays[0], cableTrays[1], cableTrays[2], cableTrays[3]);
                    if (fitting?.IsValidObject == true)
                        tr.Commit(failureRollBack);
                    else
                    {
                        tr.RollBack();
                    }
                }
                catch (Exception)
                {
                    if (tr.HasStarted())
                        tr.RollBack();
                }
            }

            return fitting?.IsValidObject == true ? fitting : null;
        }

        public static FamilyInstance CreateCableTrayTeeWyeFitting(Document doc, MEPCurve cableTrays1, MEPCurve cableTrays2, MEPCurve cableTrays3)
        {
            if (doc == null || cableTrays1 == null || cableTrays2 == null || cableTrays3 == null)
                return null;

            if (!DetermineMainCurve(ref cableTrays1, ref cableTrays2, ref cableTrays3))
                return null;

            RevitUtils.GetConnectorClosedTo(cableTrays1.ConnectorManager, cableTrays2.ConnectorManager, out Connector con1, out Connector con2);
            RevitUtils.GetConnectorClosedTo(cableTrays2.ConnectorManager, cableTrays3.ConnectorManager, out _, out Connector con3);

            Connector conMain1 = null;
            Connector conMain2 = null;
            Connector conB = null;

            if (RevitUtils.IsParallel(con1.CoordinateSystem.BasisZ, con2.CoordinateSystem.BasisZ))
            {
                conMain1 = con1;
                conMain2 = con2;
                conB = con3;
            }
            else if (RevitUtils.IsParallel(con1.CoordinateSystem.BasisZ, con3.CoordinateSystem.BasisZ))
            {
                conMain1 = con1;
                conMain2 = con3;
                conB = con2;
            }
            else if (RevitUtils.IsParallel(con2.CoordinateSystem.BasisZ, con3.CoordinateSystem.BasisZ))
            {
                conMain1 = con2;
                conMain2 = con3;
                conB = con1;
            }

            return doc.Create.NewTeeFitting(conMain1, conMain2, conB);
        }

        public static FamilyInstance CreateCrossFitting(Document doc, MEPCurve cableTrays1, MEPCurve cableTrays2, MEPCurve cableTrays3, MEPCurve cableTrays4)
        {
            if (doc == null || cableTrays1 == null || cableTrays2 == null || cableTrays3 == null || cableTrays4 == null)
                return null;

            RevitUtils.GetConnectorClosedTo(cableTrays1.ConnectorManager, cableTrays2.ConnectorManager, out Connector con1, out Connector con2);
            RevitUtils.GetConnectorClosedTo(cableTrays2.ConnectorManager, cableTrays3.ConnectorManager, out con2, out Connector con3);
            RevitUtils.GetConnectorClosedTo(cableTrays3.ConnectorManager, cableTrays4.ConnectorManager, out con3, out Connector con4);

            Connector conMain1 = null;
            Connector conMain2 = null;
            Connector conMain3 = null;
            Connector conMain4 = null;

            if (RevitUtils.IsParallel(con1.CoordinateSystem.BasisZ, con2.CoordinateSystem.BasisZ))
            {
                conMain1 = con1;
                conMain2 = con2;
                conMain3 = con3;
                conMain4 = con4;
            }
            else if (RevitUtils.IsParallel(con1.CoordinateSystem.BasisZ, con3.CoordinateSystem.BasisZ))
            {
                conMain1 = con1;
                conMain2 = con3;
                conMain3 = con2;
                conMain4 = con4;
            }
            else if (RevitUtils.IsParallel(con2.CoordinateSystem.BasisZ, con3.CoordinateSystem.BasisZ))
            {
                conMain1 = con2;
                conMain2 = con3;
                conMain3 = con1;
                conMain4 = con4;
            }
            else if (RevitUtils.IsParallel(con1.CoordinateSystem.BasisZ, con4.CoordinateSystem.BasisZ))
            {
                conMain1 = con1;
                conMain2 = con4;
                conMain3 = con2;
                conMain4 = con3;
            }

            return doc.Create.NewCrossFitting(conMain1, conMain2, conMain3, conMain4);
        }

        public static XYZ GetPointFarestWithPoint(XYZ pointSource, List<XYZ> points)
        {
            double minValue = double.MinValue;
            XYZ retval = null;
            foreach (var item in points)
            {
                double distance = pointSource.DistanceTo(item);
                if (distance > minValue)
                {
                    distance = minValue;
                    retval = item;
                }
            }

            return retval;
        }

        public static void MappingConnector(Document doc, FamilyInstance fitting, List<MEPCurve> mepCurves)
        {
            if (fitting != null)
            {
                XYZ location = (fitting.Location as LocationPoint).Point;
                List<Connector> connectors = new List<Connector>();
                foreach (MEPCurve mep in mepCurves)
                    connectors.Add(GetNearestConnector(mep.ConnectorManager, location));

                foreach (var connector in connectors)
                {
                    Connector con = GetNearestConnector(fitting.MEPModel.ConnectorManager, connector.Origin);
                    connector.ConnectTo(con);
                }
            }

            doc.Regenerate();
        }

        public static Connector GetNearestConnector(ConnectorManager connectorManager, XYZ point)
        {
            List<Connector> connectors = new List<Connector>();
            foreach (Connector connector in connectorManager.Connectors)
                connectors.Add(connector);
            return connectors.OrderBy(x => x.Origin.DistanceTo(point)).FirstOrDefault();
        }

        public static bool DetermineMainCurve(ref MEPCurve mainCurve1, ref MEPCurve mainCurve2, ref MEPCurve auxiliaryCurve)
        {
            MEPCurve mEPCurve1;
            MEPCurve mEPCurve2;
            MEPCurve mEPCurve3;

            if (mainCurve1 != null && mainCurve2 != null && auxiliaryCurve != null
                && mainCurve1.Location is LocationCurve location1
                && mainCurve2.Location is LocationCurve location2
                && auxiliaryCurve.Location is LocationCurve location3)
            {
                if (RevitUtils.IsLineStraightOverlap(location1.Curve as Line, location2.Curve as Line, 0.02))
                {
                    return true;
                }
                else if (RevitUtils.IsLineStraightOverlap(location2.Curve as Line, location3.Curve as Line, 0.02))
                {
                    mEPCurve1 = mainCurve2;
                    mEPCurve2 = auxiliaryCurve;
                    mEPCurve3 = mainCurve1;

                    mainCurve1 = mEPCurve1;
                    mainCurve2 = mEPCurve2;
                    auxiliaryCurve = mEPCurve3;
                    return true;
                }
                else if (RevitUtils.IsLineStraightOverlap(location3.Curve as Line, location1.Curve as Line, 0.02))
                {
                    mEPCurve1 = auxiliaryCurve;
                    mEPCurve2 = mainCurve1;
                    mEPCurve3 = mainCurve2;

                    mainCurve1 = mEPCurve1;
                    mainCurve2 = mEPCurve2;
                    auxiliaryCurve = mEPCurve3;

                    return true;
                }
            }

            return false;
        }

        public static bool IsCreateCableTrayTeeFitting(CableTrayData tee, ref List<CableTray> cableTrays)
        {
            BoundingBoxXYZ boxFitting = GeometryUtils.GetBoudingBoxExtend(tee.LinkEleData.LinkElement, tee.LinkTransform);
            XYZ center = (boxFitting.Min + boxFitting.Max) / 2;
            cableTrays = CommonDataPipeDuct.FilterPipeOverlap(cableTrays.Cast<Element>().ToList(), center).Cast<CableTray>().ToList();

            if (cableTrays.Count == 3
                && cableTrays[0].Location is LocationCurve lcCurve0
                && lcCurve0.Curve is Line lcLine0
                && cableTrays[1].Location is LocationCurve lcCurve1
                && lcCurve1.Curve is Line lcLine1
                && cableTrays[2].Location is LocationCurve lcCurve2
                && lcCurve2.Curve is Line lcLine2)
            {
                // check case pipe touch pipes other but not intersect
                if (!GeometryUtils.IsNotTouchSolids(lcLine0, lcLine1, lcLine2))
                    return false;

                // check solid of pipe has intersect
                if (GeometryUtils.IsIntersectSolid(cableTrays.Cast<Element>().ToList()))
                    return false;

                for (int i = 0; i < cableTrays.Count; i++)
                {
                    // pipe[i] is pipe in location perpendicular with two other pipes

                    if (i == 0
                        && RevitUtils.IsParallel(lcLine1.Direction, lcLine2.Direction))
                    {
                        // shape T
                        if (RevitUtils.IsPerpendicular(lcLine0.Direction, lcLine1.Direction)
                            && RevitUtils.IsPerpendicular(lcLine0.Direction, lcLine2.Direction))
                            return true;

                        // shap Y
                        XYZ direction1 = lcLine1.Direction;
                        XYZ direction2 = lcLine2.Direction;
                        if (RevitUtils.IsEqual(lcLine1.Direction, lcLine2.Direction))
                            direction2 = direction2.Negate();

                        double ange = direction1.AngleTo(lcLine0.Direction);
                        double ange1 = direction2.AngleTo(lcLine0.Direction);

                        if (IsAngleQuater(ange)
                         || IsAngleQuater(ange1))
                            return false;
                        else if ((RevitUtils.IsGreaterThan(ange, 0) && RevitUtils.IsLessThan(ange, Math.PI / 2)
                              && RevitUtils.IsGreaterThan(ange1, Math.PI / 2) && RevitUtils.IsLessThan(ange1, Math.PI)
                          || (RevitUtils.IsGreaterThan(ange, Math.PI / 2) && RevitUtils.IsLessThan(ange, Math.PI)
                              && RevitUtils.IsGreaterThan(ange1, 0) && RevitUtils.IsLessThan(ange1, Math.PI / 2))))
                            return true;
                    }

                    if (i == 1
                        && RevitUtils.IsParallel(lcLine0.Direction, lcLine2.Direction))
                    {
                        // shap T
                        if (RevitUtils.IsPerpendicular(lcLine1.Direction, lcLine0.Direction)
                            && RevitUtils.IsPerpendicular(lcLine1.Direction, lcLine2.Direction))
                            return true;

                        // shap Y
                        XYZ direction1 = lcLine0.Direction;
                        XYZ direction2 = lcLine2.Direction;
                        if (RevitUtils.IsEqual(lcLine0.Direction, lcLine2.Direction))
                            direction2 = direction2.Negate();

                        double ange = direction1.AngleTo(lcLine1.Direction);
                        double ange1 = direction2.AngleTo(lcLine1.Direction);

                        if (IsAngleQuater(ange)
                         || IsAngleQuater(ange1))
                            return false;
                        else if ((RevitUtils.IsGreaterThan(ange, 0) && RevitUtils.IsLessThan(ange, Math.PI / 2)
                              && RevitUtils.IsGreaterThan(ange1, Math.PI / 2) && RevitUtils.IsLessThan(ange1, Math.PI)
                          || (RevitUtils.IsGreaterThan(ange, Math.PI / 2) && RevitUtils.IsLessThan(ange, Math.PI)
                              && RevitUtils.IsGreaterThan(ange1, 0) && RevitUtils.IsLessThan(ange1, Math.PI / 2))))
                            return true;
                    }

                    if (i == 2
                        && RevitUtils.IsParallel(lcLine0.Direction, lcLine1.Direction))

                    {
                        // shap T
                        if (RevitUtils.IsPerpendicular(lcLine2.Direction, lcLine0.Direction)
                             && RevitUtils.IsPerpendicular(lcLine2.Direction, lcLine1.Direction))
                            return true;

                        // shap Y
                        XYZ direction1 = lcLine0.Direction;
                        XYZ direction2 = lcLine1.Direction;
                        if (RevitUtils.IsEqual(lcLine0.Direction, lcLine1.Direction))
                            direction2 = direction2.Negate();

                        double ange = direction1.AngleTo(lcLine2.Direction);
                        double ange1 = direction2.AngleTo(lcLine2.Direction);

                        if (IsAngleQuater(ange)
                           || IsAngleQuater(ange1))
                            return false;
                        else if ((RevitUtils.IsGreaterThan(ange, 0) && RevitUtils.IsLessThan(ange, Math.PI / 2)
                              && RevitUtils.IsGreaterThan(ange1, Math.PI / 2) && RevitUtils.IsLessThan(ange1, Math.PI)
                          || (RevitUtils.IsGreaterThan(ange, Math.PI / 2) && RevitUtils.IsLessThan(ange, Math.PI)
                              && RevitUtils.IsGreaterThan(ange1, 0) && RevitUtils.IsLessThan(ange1, Math.PI / 2))))
                            return true;
                    }
                }
            }
            return false;
        }

        private static bool IsAngleQuater(double ange)
        {
            if (RevitUtils.IsEqual(ange, 0, 0.02)
                           || RevitUtils.IsEqual(ange, Math.PI / 2, 0.02)
                           || RevitUtils.IsEqual(ange, 2 * Math.PI / 3, 0.02)
                           || RevitUtils.IsEqual(ange, 2 * Math.PI, 0.02))
                return true;
            return false;
        }
    }
}