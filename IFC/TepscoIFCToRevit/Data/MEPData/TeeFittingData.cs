using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using RevitUtilities;
using System;
using System.Collections.Generic;
using TepscoIFCToRevit.Common;

namespace TepscoIFCToRevit.Data.MEPData
{
    public class TeeFittingData
    {
        #region Variable & Properties

        private Document _doc = null;
        public RevitLinkInstance LinkInstance = null;

        // New Fitting Id

        public ElementId NewCreateFittingId = ElementId.InvalidElementId;
        public FamilyInstance NewCreateFitting = null;

        // Connector 1
        private Connector mainConnector1 = null;

        // Connector 2
        private Connector mainConnector2 = null;

        // Connector 3
        private Connector branchConnector = null;

        public MEPCurve MepCurve1 = null;
        public MEPCurve MepCurve2 = null;
        public MEPCurve MepCurve3 = null;

        #endregion Variable & Properties

        #region Constructor

        public TeeFittingData(Document document, List<Connector> lstConnector)
        {
            _doc = document;
            GetMainConnector(lstConnector, out mainConnector1, out mainConnector2, out branchConnector);
        }

        #endregion Constructor

        #region Method

        public bool IsCreateTeeFitting()
        {
            MepCurve1 = mainConnector1?.Owner as MEPCurve;
            MepCurve2 = mainConnector2?.Owner as MEPCurve;
            MepCurve3 = branchConnector?.Owner as MEPCurve;

            if (_doc == null || MepCurve1 == null || MepCurve1 == null || MepCurve1 == null)
                return false;

            return true;
        }

        /// <summary>
        /// Create fitting Y, T
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="mEPCurveMain1"></param>
        /// <param name="mEPCurveMain2"></param>
        /// <param name="mEPCurve3"></param>
        /// <returns></returns>
        public static FamilyInstance CreateTeeWyeFitting(Document doc, MEPCurve mEPCurveMain1, MEPCurve mEPCurveMain2, MEPCurve mEPCurve3, bool isDetermine = true)
        {
            if (doc == null || mEPCurveMain1 == null || mEPCurveMain2 == null || mEPCurve3 == null)
                return null;

            if (isDetermine)
            {
                if (!DetermineMainCurve(ref mEPCurveMain1, ref mEPCurveMain2, ref mEPCurve3))
                    return null;
            }

            FamilySymbol familySymbol = RevitUtils.GetFamilySymbolWye(doc, mEPCurveMain1, RoutingPreferenceRuleGroupType.Junctions);

            RevitUtils.GetConnectorClosedTo(mEPCurveMain1.ConnectorManager, mEPCurveMain2.ConnectorManager, out Connector con1, out Connector con2);

            XYZ location = (con1.Origin + con2.Origin) / 2;

            FamilyInstance fittingWye = doc.Create.NewFamilyInstance(location, familySymbol, mEPCurveMain1.ReferenceLevel, StructuralType.NonStructural);
            if (fittingWye != null)
            {
                UtilsParameter.SetValueParameterBuiltIn(fittingWye, BuiltInParameter.INSTANCE_ELEVATION_PARAM, location.Z - mEPCurveMain1.ReferenceLevel?.Elevation);
                RevitUtils.GetInformationConectorWye(fittingWye, null, out Connector main1, out Connector main2, out Connector conY);

                ConnectorProfileType shape = main1.Shape;

                if (shape == ConnectorProfileType.Round)
                {
                    main1.Radius = mEPCurveMain1.Diameter / 2;
                    main2.Radius = mEPCurveMain1.Diameter / 2;
                    conY.Radius = mEPCurve3.Diameter / 2;
                }
                else if (shape == ConnectorProfileType.Oval || shape == ConnectorProfileType.Rectangular)
                {
                    main1.Height = mEPCurveMain1.Height;
                    main1.Width = mEPCurveMain1.Width;

                    main2.Height = mEPCurveMain1.Height;
                    main2.Width = mEPCurveMain1.Width;

                    conY.Height = mEPCurve3.Height;
                    conY.Width = mEPCurve3.Width;
                }

                double valueAngle = GetAngleOfFittingY(mEPCurveMain1, mEPCurve3);
                UtilsParameter.SetValueAllParameterName(fittingWye, "Angle", valueAngle);

                Line axisRotate = ((LocationCurve)mEPCurveMain1.Location).Curve as Line;
                RevitUtils.RotateLine(doc, fittingWye, axisRotate);
                doc.Regenerate();

                if (IsFlipFitting(mEPCurve3, fittingWye))
                {
                    FlipFitting(doc, fittingWye);
                    doc.Regenerate();
                }

                double angle = GetAngleRotate(mEPCurve3, fittingWye, axisRotate);

                ElementTransformUtils.RotateElement(doc, fittingWye.Id, axisRotate, angle);
                doc.Regenerate();

                MoveFitting(mEPCurveMain1, mEPCurveMain2, fittingWye, location);
                doc.Regenerate();

                ElementTransformUtils.MoveElement(doc, fittingWye.Id, axisRotate.Direction.Normalize() * 1 / 304.8);
                doc.Regenerate();

                ElementTransformUtils.MoveElement(doc, fittingWye.Id, axisRotate.Direction.Normalize() * -1 / 304.8);
                doc.Regenerate();
            }

            return fittingWye;
        }

        /// <summary>
        /// Flip fitting
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="fitting"></param>
        public static void FlipFitting(Document doc, FamilyInstance fitting)
        {
            RevitUtils.GetInformationConectorWye(fitting, null, out Connector conSt, out Connector conEnd, out Connector conNhanhWye);

            if (conSt == null || conEnd == null || conNhanhWye == null)
                return;

            XYZ locationBeforeSt = conSt.Origin;

            RevitUtils.DisconnectFrom(fitting, out Connector connectedSt, out Connector connectedEnd, out Element eleSt, out Element eleEnd);

            Line axis = Line.CreateBound(conSt.Origin, conEnd.Origin);

            XYZ projectPoint = RevitUtils.GetPointProject(axis, conNhanhWye.Origin);

            XYZ direction = (conNhanhWye.Origin - projectPoint).Normalize();

            Line axisFlip = Line.CreateUnbound(projectPoint, direction);

            fitting.Location.Rotate(axisFlip, Math.PI);

            if (conSt != null && connectedEnd != null && !conSt.IsConnectedTo(connectedEnd))
                conSt.ConnectTo(connectedEnd);

            if (conEnd != null && connectedSt != null && !conEnd.IsConnectedTo(connectedSt))
                conEnd.ConnectTo(connectedSt);

            XYZ locationAfterEnd = conEnd.Origin;

            XYZ translation = locationBeforeSt - locationAfterEnd;

            ElementTransformUtils.MoveElement(doc, fitting.Id, translation);
        }

        public static void MoveFitting(MEPCurve mEPCurve1, MEPCurve mEPCurve2, FamilyInstance fitting, XYZ locationY)
        {
            if (mEPCurve1 != null && fitting != null
             && mEPCurve1.Location is LocationCurve locationCurve1
             && mEPCurve2 != null
             && mEPCurve2.Location is LocationCurve locationCurve2
             && fitting.Location is LocationPoint locationPoint)
            {
                ((LocationPoint)fitting.Location).Point = locationY;
                mEPCurve1.Document.Regenerate();

                RevitUtils.GetInformationConectorWye(fitting, null, out Connector conSt, out Connector conEnd, out Connector conNhanhWye);

                List<Connector> connectors = new List<Connector>() { conSt, conEnd };

                Plane plane = Plane.CreateByNormalAndOrigin(conSt.CoordinateSystem.BasisZ, locationY);

                XYZ vecSt = (conSt.Origin - UtilsPlane.ProjectOnto(plane, conSt.Origin)).Normalize();
                XYZ vecEnd = (conEnd.Origin - UtilsPlane.ProjectOnto(plane, conEnd.Origin)).Normalize();

                XYZ vec1 = (locationCurve1.Curve.GetEndPoint(0) - UtilsPlane.ProjectOnto(plane, locationCurve1.Curve.GetEndPoint(0))).Normalize();
                XYZ vec2 = (locationCurve1.Curve.GetEndPoint(1) - UtilsPlane.ProjectOnto(plane, locationCurve1.Curve.GetEndPoint(1))).Normalize();

                Connector conOrigin = null;
                if (vec1.DotProduct(vecSt) > 0 && vec2.DotProduct(vecSt) > 0)
                {
                    conOrigin = conSt;
                }
                else if (vec1.DotProduct(vecEnd) > 0 && vec2.DotProduct(vecEnd) > 0)
                {
                    conOrigin = conEnd;
                }

                Connector con11 = RevitUtils.GetConnectorNearest(locationY, mEPCurve1.ConnectorManager, out Connector con12);

                XYZ tran = null;
                if (con11 != null && conOrigin != null)
                    tran = (con11.Origin - conOrigin.Origin);

                if (tran != null)
                    ElementTransformUtils.MoveElement(fitting.Document, fitting.Id, tran);

                fitting.Document.Regenerate();
            }
        }

        /// <summary>
        /// Check if there is a need to flip the fitting
        /// </summary>
        /// <param name="mEPCurve"></param>
        /// <param name="fitting"></param>
        /// <returns></returns>
        public static bool IsFlipFitting(MEPCurve mEPCurve, FamilyInstance fitting)
        {
            if (mEPCurve != null && fitting != null
              && mEPCurve.Location is LocationCurve locationCurve
              && fitting.Location is LocationPoint locationPoint)
            {
                RevitUtils.GetInformationConectorWye(fitting, null, out Connector conSt, out Connector conEnd, out Connector conNhanhWye);

                if (conSt != null && conEnd != null && conNhanhWye != null)
                {
                    Line axis = Line.CreateBound(conSt.Origin, conEnd.Origin);

                    XYZ point1 = RevitUtils.GetPointProject(axis, conNhanhWye.Origin);

                    XYZ vec1 = (point1 - locationPoint.Point).Normalize();

                    Connector con1 = RevitUtils.GetConnectorNearest(locationPoint.Point, mEPCurve.ConnectorManager, out Connector con2);

                    XYZ point11 = RevitUtils.GetPointProject(axis, con1.Origin);
                    XYZ point12 = RevitUtils.GetPointProject(axis, con2.Origin);

                    XYZ vec2 = (point12 - point11).Normalize();

                    if (vec1.DotProduct(vec2) > 0)
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Calculation of the angle of branch pipe and main pipe
        /// </summary>
        /// <param name="mEPCurve1"></param>
        /// <param name="mEPCurve2"></param>
        /// <returns></returns>
        public static double GetAngleOfFittingY(MEPCurve mEPCurve1, MEPCurve mEPCurve2)
        {
            if (mEPCurve1 != null && mEPCurve2 != null
               && mEPCurve1.Location is LocationCurve locationCurve1
               && mEPCurve2.Location is LocationCurve locationCurve2)
            {
                Line line1 = locationCurve1.Curve as Line;
                Line line2 = locationCurve2.Curve as Line;
                line1.MakeUnbound();
                line2.MakeUnbound();

                SetComparisonResult setComparisonResult = line1.Intersect(line2, out IntersectionResultArray array);
                if (setComparisonResult != SetComparisonResult.Disjoint && array.Size >= 1)
                {
                    XYZ intersect = array.get_Item(0).XYZPoint;

                    XYZ point1 = GetPointFarestWithPoint(intersect, new List<XYZ>() { (locationCurve1.Curve.GetEndPoint(0)), (locationCurve1.Curve.GetEndPoint(1)) });

                    XYZ point2 = GetPointFarestWithPoint(intersect, new List<XYZ>() { (locationCurve2.Curve.GetEndPoint(0)), (locationCurve2.Curve.GetEndPoint(1)) });

                    XYZ vec1 = (point1 - intersect).Normalize();
                    XYZ vec2 = (point2 - intersect).Normalize();

                    return vec1.AngleTo(vec2);
                }
                double value = (line1.Direction).Normalize().DotProduct((line2.Direction).Normalize());

                if (RevitUtils.IsEqual(value, 0, 0.1))
                    return Math.PI / 2;
            }

            return Math.PI / 4;
        }

        /// <summary>
        /// Get the furthest point from a list of points
        /// </summary>
        /// <param name="pointSource"></param>
        /// <param name="points"></param>
        /// <returns></returns>
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

        public static double GetAngleRotate(MEPCurve mEPCurve, FamilyInstance fittingY, Line axisRotate)
        {
            if (mEPCurve != null && fittingY != null
                && mEPCurve.Location is LocationCurve locationCurve
                && fittingY.Location is LocationPoint locationPoint)
            {
                RevitUtils.GetInformationConectorWye(fittingY, null, out Connector main1, out Connector main2, out Connector conY);

                if (main1 != null && main2 != null && conY != null)
                {
                    Plane plane = Plane.CreateByNormalAndOrigin(main1.CoordinateSystem.BasisZ, main1.Origin);

                    XYZ point1 = UtilsPlane.ProjectOnto(plane, locationPoint.Point);
                    XYZ point2 = UtilsPlane.ProjectOnto(plane, conY.Origin);
                    XYZ vec1 = (point2 - point1).Normalize();

                    XYZ point3 = UtilsPlane.ProjectOnto(plane, locationCurve.Curve.GetEndPoint(0));
                    XYZ point4 = UtilsPlane.ProjectOnto(plane, locationCurve.Curve.GetEndPoint(1));

                    double distance1 = point3.DistanceTo(point1);
                    double distance2 = point4.DistanceTo(point1);

                    if (distance1 > distance2)
                    {
                        XYZ temp = point3;
                        point3 = point4;
                        point4 = temp;
                    }

                    XYZ vec2 = (point4 - point3).Normalize();

                    //return vec1.AngleTo(vec2);

                    double angle = vec1.AngleOnPlaneTo(vec2, main1.CoordinateSystem.BasisZ);

                    double distanceToY1 = double.MaxValue;
                    double distanceToY2 = double.MaxValue;

                    SubTransaction subTransaction = new SubTransaction(fittingY.Document);
                    try
                    {
                        subTransaction.Start();

                        ElementTransformUtils.RotateElement(fittingY.Document, fittingY.Id, axisRotate, angle);
                        fittingY.Document.Regenerate();

                        Connector con11 = RevitUtils.GetConnectorNearest(conY.Origin, mEPCurve.ConnectorManager, out Connector con12);

                        distanceToY1 = con11.Origin.DistanceTo(conY.Origin);
                    }
                    finally
                    {
                        if (subTransaction.HasStarted())
                            subTransaction.RollBack();
                    }

                    RevitUtils.GetInformationConectorWye(fittingY, null, out main1, out main2, out conY);
                    try
                    {
                        subTransaction.Start();

                        ElementTransformUtils.RotateElement(fittingY.Document, fittingY.Id, axisRotate, -angle);
                        fittingY.Document.Regenerate();

                        Connector con11 = RevitUtils.GetConnectorNearest(conY.Origin, mEPCurve.ConnectorManager, out Connector con12);

                        distanceToY2 = con11.Origin.DistanceTo(conY.Origin);
                    }
                    finally
                    {
                        if (subTransaction.HasStarted())
                            subTransaction.RollBack();
                    }

                    if (distanceToY1 > distanceToY2)
                        return -angle;

                    return angle;
                }
            }

            return 0.0;
        }

        /// <summary>
        /// Determine : main pipe , branch pipe
        /// </summary>
        /// <param name="mainCurve1">main pipe</param>
        /// <param name="mainCurve2">main pipe</param>
        /// <param name="auxiliaryCurve">branch pipe</param>
        /// <returns></returns>
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

        /// <summary>
        /// Check connector is main or branch
        /// </summary>
        /// <param name="lstConnector"></param>
        /// <param name="main1"></param>
        /// <param name="main2"></param>
        /// <param name="brach"></param>
        private void GetMainConnector(List<Connector> lstConnector, out Connector main1, out Connector main2, out Connector brach)
        {
            XYZ vector = (lstConnector[0].Origin - lstConnector[1].Origin).Normalize();
            XYZ center = (lstConnector[0].Origin + lstConnector[1].Origin) / 2;
            XYZ checkVector = (center - lstConnector[2].Origin).Normalize();

            if (RevitUtils.IsPerpendicular(vector, checkVector, 0.1))
            {
                main1 = lstConnector[0];
                main2 = lstConnector[1];
                brach = lstConnector[2];
                return;
            }

            vector = (lstConnector[1].Origin - lstConnector[2].Origin).Normalize();
            center = (lstConnector[1].Origin + lstConnector[2].Origin) / 2;
            checkVector = (center - lstConnector[0].Origin).Normalize();

            if (RevitUtils.IsPerpendicular(vector, checkVector, 0.1))
            {
                main1 = lstConnector[1];
                main2 = lstConnector[2];
                brach = lstConnector[0];
                return;
            }

            vector = (lstConnector[2].Origin - lstConnector[0].Origin).Normalize();
            center = (lstConnector[2].Origin + lstConnector[0].Origin) / 2;
            checkVector = (center - lstConnector[1].Origin).Normalize();

            if (RevitUtils.IsPerpendicular(vector, checkVector, 0.1))
            {
                main1 = lstConnector[2];
                main2 = lstConnector[0];
                brach = lstConnector[1];
                return;
            }

            main1 = null;
            main2 = null;
            brach = null;
        }

        #endregion Method
    }
}