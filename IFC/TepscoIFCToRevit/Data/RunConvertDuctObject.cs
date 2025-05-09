using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using RevitUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using TepscoIFCToRevit.Common;
using TepscoIFCToRevit.Data.MEPData;

namespace TepscoIFCToRevit.Data
{
    public class RunConvertDuctObject
    {
        #region Convert Duct

        public static bool ConvertForDuct(Document doc,
                                          DuctData data,
                                          ref List<DuctData> datasConverted,
                                          ref List<DuctData> datasNotConvented)
        {
            if (doc?.IsValidObject != true)
            {
                return false;
            }

            bool isSuccess = true;
            using (Transaction tr = new Transaction(doc, "Create Duct"))
            {
                try
                {
                    // check any ducts has been place in location place
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

                            data.Initialize();
                            if (data.ConvertElem?.IsValidObject == true)
                            {
                                REVWarning1 supWarning = new REVWarning1(true);
                                fhOpts.SetFailuresPreprocessor(supWarning);
                                tr.SetFailureHandlingOptions(fhOpts);
                                tr.Commit();

                                double angle = RevitUtils.GetRotateFacingElement(data.ConvertElem, data.Location, data.IFCdata, data.LinkInstance);
                                if (!RevitUtils.IsEqual(angle, 0) && !RevitUtils.IsEqual(angle, Math.PI))
                                {
                                    tr.Start("Rotate");
                                    ElementTransformUtils.RotateElement(doc, data.ConvertElem.Id, data.Location, angle);
                                    tr.Commit();
                                }

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

        /// <summary>
        /// Get rotate of duct
        /// </summary>
        ///

        public static double GetRotateFacingDuct(Duct duct, Line location, Autodesk.Revit.DB.Transform transform, Element elementIFC)
        {
            double angle = 0;
            if (duct != null)
            {
                ConnectorProfileType ductShape = duct.DuctType.Shape;
                if (ductShape != ConnectorProfileType.Round)
                {
                    XYZ vertorIFC = GetFacingDuctIFC(elementIFC, ductShape, location, transform);
                    XYZ vertorFacing = GetFacingDuct(duct, ductShape);
                    if (vertorIFC != null && vertorFacing != null)
                    {
                        XYZ normal = location.Direction;
                        normal = transform.OfVector(normal);
                        angle = vertorFacing.AngleOnPlaneTo(vertorIFC, normal);
                    }
                }
            }
            return angle;
        }

        /// <summary>
        /// Get facing of IFC duct element
        /// </summary>
        private static XYZ GetFacingDuctIFC(Element ductIFC, ConnectorProfileType ductShape, Line location, Autodesk.Revit.DB.Transform transform)
        {
            XYZ direction = null;

            XYZ startDuct = location.GetEndPoint(0);
            Plane plane = Plane.CreateByNormalAndOrigin(location.Direction, startDuct);
            List<Solid> solids = UtilsSolid.GetAllSolids(ductIFC);
            if (solids.Count > 0)
            {
                List<Line> lines = new List<Line>();
                IList<XYZ> points = new List<XYZ>();
                foreach (PlanarFace face in solids.First().Faces)
                {
                    foreach (Line line in UtilsPlane.GetLinesOfFace(face))
                    {
                        if (RevitUtils.IsPerpendicular(line.Direction, location.Direction, 0.005))
                        {
                            lines.Add(line);
                            XYZ point0 = UtilsPlane.ProjectOnto(plane, line.GetEndPoint(0));
                            XYZ point1 = UtilsPlane.ProjectOnto(plane, line.GetEndPoint(1));
                            points.Add(point0);
                            points.Add(point1);
                        }
                    }
                }

                if (ductShape == ConnectorProfileType.Rectangular)
                {
                    if (lines.Count > 0)
                    {
                        lines = UtilsCurve.MergeLineStraightIntersect(lines);
                        if (lines.Count > 0 && lines.Count <= 16)
                        {
                            Line minLine = lines.OrderBy(x => x.Length).First();
                            XYZ mid = (minLine.GetEndPoint(0) + minLine.GetEndPoint(1)) / 2;
                            mid = UtilsPlane.ProjectOnto(plane, mid);

                            direction = mid - startDuct;
                            direction = transform.OfVector(direction);
                        }
                    }
                }
                else
                {
                    if (points.Count > 0)
                    {
                        XYZ maxPoint = points.OrderBy(x => x.DistanceTo(startDuct)).Last();
                        maxPoint = UtilsPlane.ProjectOnto(plane, maxPoint);
                        direction = maxPoint - startDuct;
                        direction = transform.OfVector(direction);
                    }
                }
            }
            else
            {
                List<Mesh> meshs = GeometryUtils.GetIfcGeometriess(ductIFC)
                                                .Where(x => x is Mesh)
                                                .Cast<Mesh>()
                                                .ToList();
                if (meshs.Count > 0)
                {
                    List<XYZ> points = meshs.First().Vertices.Where(x => UtilsPlane.IsOnPlane(x, plane))
                                                             .OrderBy(x => x.DistanceTo(startDuct))
                                                             .ToList();
                    int count = points.Count;

                    if (count > 0)
                    {
                        if (ductShape == ConnectorProfileType.Rectangular)
                        {
                            if (count > 3)
                            {
                                XYZ lastPoint = points[count - 1];
                                List<XYZ> pointsCheck = new List<XYZ>
                                {
                                    points[count - 2],
                                    points[count - 3],
                                    points[count - 4]
                                };
                                XYZ mid = (lastPoint + pointsCheck.OrderBy(x => x.DistanceTo(lastPoint)).First()) / 2;
                                mid = UtilsPlane.ProjectOnto(plane, mid);

                                direction = mid - startDuct;
                                direction = transform.OfVector(direction);
                            }
                        }
                        else
                        {
                            XYZ maxPoint = points.Last();
                            maxPoint = UtilsPlane.ProjectOnto(plane, maxPoint);
                            direction = maxPoint - startDuct;
                            direction = transform.OfVector(direction);
                        }
                    }
                }
            }
            return direction;
        }

        /// <summary>
        /// Get facing of revit duct element
        /// </summary>
        private static XYZ GetFacingDuct(Duct duct, ConnectorProfileType ductShape)
        {
            XYZ direction = null;

            List<Solid> solids = UtilsSolid.GetAllSolids(duct);
            if (solids.Count > 0 && duct.Location is LocationCurve lcCurve && lcCurve.Curve is Line lcLine)
            {
                foreach (PlanarFace face in solids.First().Faces)
                {
                    if (RevitUtils.IsParallel(face.FaceNormal, lcLine.Direction))
                    {
                        Plane plane = Plane.CreateByNormalAndOrigin(lcLine.Direction, lcLine.Origin);

                        if (ductShape == ConnectorProfileType.Rectangular)
                        {
                            IOrderedEnumerable<Line> lines = UtilsPlane.GetLinesOfFace(face).OrderBy(x => x.Length);
                            if (lines?.Count() > 0)
                            {
                                Line minLine = lines.First();
                                XYZ mid = (minLine.GetEndPoint(0) + minLine.GetEndPoint(1)) / 2;
                                mid = UtilsPlane.ProjectOnto(plane, mid);

                                direction = mid - lcLine.Origin;
                            }
                        }
                        else
                        {
                            IOrderedEnumerable<XYZ> points = UtilsPlane.GetPointsOfFace(face).OrderBy(x => x.DistanceTo(lcLine.Origin));
                            if (points?.Count() > 0)
                            {
                                XYZ point = points.Last();
                                point = UtilsPlane.ProjectOnto(plane, point);

                                direction = point - lcLine.Origin;
                            }
                        }
                        break;
                    }
                }
            }
            return direction;
        }

        #endregion Convert Duct

        #region Convert TeeFitting by BuilIn

        public static FamilyInstance CreateDuctTeeFitting(Document doc, List<Duct> ductInterSec, DuctData ductDataItm)
        {
            if (doc?.IsValidObject != true)
            {
                return null;
            }

            if (IsCreateDuctTeeFitting(doc, ductDataItm, ref ductInterSec))
            {
                var fitting = CreateDuctTeeFitting(doc, ductInterSec);
                if (fitting?.IsValidObject == true)
                    return fitting;
            }
            return null;
        }

        public static bool IsCreateDuctTeeFitting(Document doc, DuctData tee, ref List<Duct> ducts)
        {
            BoundingBoxXYZ boxFitting = GeometryUtils.GetBoudingBoxExtend(tee.LinkEleData.LinkElement, tee.LinkTransform);
            XYZ center = (boxFitting.Min + boxFitting.Max) / 2;
            ducts = CommonDataPipeDuct.FilterPipeOverlap(ducts.Cast<Element>().ToList(), center).Cast<Duct>().ToList();

            if (ducts.Count == 3
                && ducts[0].Location is LocationCurve lcCurve0
                && lcCurve0.Curve is Line lcLine0
                && ducts[1].Location is LocationCurve lcCurve1
                && lcCurve1.Curve is Line lcLine1
                && ducts[2].Location is LocationCurve lcCurve2
                && lcCurve2.Curve is Line lcLine2)
            {
                // check case pipe touch pipes other but not intersect
                if (!GeometryUtils.IsNotTouchSolids(lcLine0, lcLine1, lcLine2))
                    return false;

                // check solid of pipe has intersect
                if (GeometryUtils.IsIntersectSolid(ducts.Cast<Element>().ToList()))
                    return false;

                for (int i = 0; i < ducts.Count; i++)
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

        private static FamilyInstance CreateDuctTeeFitting(Document doc, List<Duct> ducts)
        {
            if (doc?.IsValidObject != true)
            {
                return null;
            }

            FamilyInstance fitting = null;
            using (Transaction tr = new Transaction(doc, "Create tee"))
            {
                try
                {
                    FailureHandlingOptions failureRollBack = tr.GetFailureHandlingOptions();
                    failureRollBack.SetFailuresPreprocessor(new REVWarning3());
                    failureRollBack.SetClearAfterRollback(true);

                    tr.Start();
                    fitting = TeeFittingData.CreateTeeWyeFitting(doc, ducts[0], ducts[1], ducts[2]);
                    if (fitting?.IsValidObject == true)
                    {
                        tr.Commit(failureRollBack);
                    }
                    else
                    {
                        tr.RollBack();
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

        #endregion Convert TeeFitting by BuilIn

        #region Convert transaction Fitting by builin

        public static FamilyInstance CreateTransactionFittingByBuilIn(Document doc, List<Duct> ducts)
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

                    if (ducts.Count == 2
                        && ducts[0].Location is LocationCurve lcCurve0
                        && lcCurve0.Curve is Line lcLine0
                        && ducts[1].Location is LocationCurve lcCurve1
                        && lcCurve1.Curve is Line lcLine1 && RevitUtils.IsLineStraightOverlap(lcLine0, lcLine1, 0.02))

                    {
                        if (RevitUtils.GetSymbolSeted(doc, ducts[0], RoutingPreferenceRuleGroupType.Transitions) == null)
                            return null;
                        tr.Start("Create_Transition");

                        RevitUtils.GetConnectorClosedTo(ducts[0].ConnectorManager, ducts[1].ConnectorManager, out Connector con1, out Connector con2);
                        if (con1 != null && con2 != null && !con1.IsConnectedTo(con2))
                        {
                            if (con1.Shape == con2.Shape)
                            {
                                ConnectorProfileType shape = con1.Shape;

                                if (shape == ConnectorProfileType.Round)
                                {
                                    if (con1.Radius == con2.Radius)
                                    {
                                        if (tr.HasStarted())
                                        {
                                            tr.RollBack();
                                        }
                                        return null;
                                    }
                                }
                                else if (shape == ConnectorProfileType.Oval || shape == ConnectorProfileType.Rectangular)
                                {
                                    if (con1.Height == con2.Height && con1.Width == con2.Width)
                                    {
                                        if (tr.HasStarted())
                                        {
                                            tr.RollBack();
                                        }
                                        return null;
                                    }
                                }

                                XYZ origin = con1.Origin;

                                fitting = doc.Create.NewTransitionFitting(con1, con2);

                                con1 = RevitUtils.GetConnectorNearest(origin, fitting?.MEPModel?.ConnectorManager, out con2);
                                if (con1 != null && origin != null)
                                {
                                    XYZ transition = (con1.Origin - origin);
                                    ElementTransformUtils.MoveElement(doc, fitting.Id, transition);
                                }
                            }
                        }

                        if (fitting?.IsValidObject == true)
                            tr.Commit(failureRollBack);
                        else
                            tr.RollBack();
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

        #endregion Convert transaction Fitting by builin

        #region Convert elbow

        public static FamilyInstance ConvertElbowDuct(Document doc,
                                            RevitLinkInstance revLnkIns,
                                            List<Duct> ductIntersec,
                                            List<Duct> ductConvert,
                                            BoundingBoxXYZ boxFitting,
                                            DuctData ductDataItm)
        {
            try
            {
                var fitting = CreateDuctElbowFitting(doc, ductIntersec, ductDataItm);
                if (fitting?.IsValidObject == true)
                    return fitting;
                else
                {
                    ductIntersec = GeometryUtils.FindDuctNearestBox(doc, ductConvert.Where(x => x != null && x.IsValidObject).Select(x => x.Id).ToList(), boxFitting, false);
                    ElbowAtTheEndPipeData elbowAtTheEnData = new ElbowAtTheEndPipeData(doc, revLnkIns);
                    fitting = elbowAtTheEnData.CreateEblowOfTheEndDuct(ductIntersec, ductDataItm);
                    if (fitting?.IsValidObject == true)
                    {
                        return fitting;
                    }
                }
            }
            catch (Exception) { }
            return null;
        }

        private static FamilyInstance CreateDuctElbowFitting(Document doc, List<Duct> ducts, DuctData elbow)
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

                    if (ducts.Count == 2
                        && ducts[0].Location is LocationCurve lcCurve0
                        && lcCurve0.Curve is Line lcLine0
                        && ducts[1].Location is LocationCurve lcCurve1
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

                            using (SubTransaction reSubTrans = new SubTransaction(doc))
                            {
                                reSubTrans.Start();
                                (ducts[0].Location as LocationCurve).Curve = newLocation0;
                                (ducts[1].Location as LocationCurve).Curve = newLocation1;
                                reSubTrans.Commit();
                            }

                            using (SubTransaction reSubTrans = new SubTransaction(doc))
                            {
                                reSubTrans.Start();
                                fitting = GeometryUtils.CreateDuctConnector(ducts[0], ducts[1], out Connector pipeConnector1, out Connector pipeConnector2);
                                reSubTrans.Commit();
                            }

                            doc.Regenerate();
                            if (fitting?.IsValidObject == true)
                            {
                                tr.Commit(failureRollBack);
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

        private static void DisconnectFrom(MEPCurve mepCurve)
        {
            if (mepCurve != null)
            {
                Connector con1 = mepCurve.ConnectorManager.Lookup(0);
                Connector con2 = mepCurve.ConnectorManager.Lookup(1);

                if (con1 != null && con1.IsConnected)
                {
                    foreach (Connector item in con1.AllRefs)
                    {
                        if (item != null && item.IsConnectedTo(con1))
                        {
                            con1.DisconnectFrom(item);
                        }
                    }
                }

                if (con2 != null && con2.IsConnected)
                {
                    foreach (Connector item in con2.AllRefs)
                    {
                        if (item != null && item.IsConnectedTo(con2))
                        {
                            con2.DisconnectFrom(item);
                        }
                    }
                }
            }
        }

        #endregion Convert elbow

        #region Create fitting Y,T

        public static bool IsCreateTeeFitingWithTwoDucts(Document doc, List<Duct> ducts, DuctData tee, out List<Duct> filterDucts, out XYZ interSecPoint)

        {
            filterDucts = new List<Duct>();
            interSecPoint = null;
            BoundingBoxXYZ boxFitting = GeometryUtils.GetBoudingBoxExtend(tee.LinkEleData.LinkElement, tee.LinkTransform);

            List<ElementId> ductIds = ducts.Where(x => x != null && x.IsValidObject).Select(x => x.Id).ToList();
            if (ductIds?.Count > 0 && boxFitting != null)
            {
                Outline outline = new Outline(boxFitting.Min, boxFitting.Max);

                boxFitting = new BoundingBoxXYZ()
                {
                    Max = outline.MaximumPoint,
                    Min = outline.MinimumPoint,
                };

                BoundingBoxIntersectsFilter boxFilter = new BoundingBoxIntersectsFilter(outline);
                filterDucts = new FilteredElementCollector(doc, ductIds).WherePasses(boxFilter)
                                                                             .Cast<Duct>()
                                                                             .ToList();
            }

            XYZ center = (boxFitting.Min + boxFitting.Max) / 2;
            filterDucts = CommonDataPipeDuct.FilterPipeOverlap(filterDucts.Cast<Element>().ToList(), center).Cast<Duct>().ToList();

            if (filterDucts?.Count == 2
               && filterDucts[0].Location is LocationCurve lcCure0
               && lcCure0.Curve is Line lcLine0
               && filterDucts[1].Location is LocationCurve lcCure1
               && lcCure1.Curve is Line lcLine1
               && !RevitUtils.IsParallel(lcLine0.Direction, lcLine1.Direction, 5 * RevitUtils.ANGLE_TOLERANCE))
            {
                XYZ startPoint0 = lcLine0.GetEndPoint(0);
                XYZ endPoint0 = lcLine0.GetEndPoint(1);

                XYZ startPoint1 = lcLine1.GetEndPoint(0);
                XYZ endPoint1 = lcLine1.GetEndPoint(1);

                XYZ normal = lcLine0.Direction.CrossProduct(lcLine1.Direction);
                Plane plane = Plane.CreateByNormalAndOrigin(normal, startPoint0);

                XYZ startProject = UtilsPlane.ProjectOnto(plane, startPoint1);

                double sizeDuct = 0;
                if (filterDucts.FirstOrDefault().DuctType.Shape == ConnectorProfileType.Round)
                {
                    sizeDuct = filterDucts.FirstOrDefault().Diameter;
                }
                else
                    sizeDuct = filterDucts.FirstOrDefault().Height;

                if (startProject.DistanceTo(startPoint1) > sizeDuct / 5)
                {
                    return false;
                }

                Line unboundLine0 = Line.CreateUnbound(startPoint0, lcLine0.Direction);
                Line unboundLine1 = Line.CreateUnbound(startProject, lcLine1.Direction);

                SetComparisonResult result = unboundLine0.Intersect(unboundLine1, out IntersectionResultArray resultArray);

                if (result != SetComparisonResult.Disjoint)
                {
                    var intersection = resultArray.Cast<IntersectionResult>().First();
                    interSecPoint = intersection.XYZPoint;

                    List<string> idDucts = filterDucts.Select(x => x.Id.ToString()).ToList();
                    List<Duct> ductInbox = filterDucts.Where(x => GeometryUtils.IsEndElementInBox(x, boxFitting))
                                          .Where(x => idDucts.Contains(x.Id.ToString()))
                                          .Cast<Duct>()
                                          .ToList();

                    if (ductInbox.Count == 1)
                    {
                        XYZ startMain = null;
                        XYZ endMain = null;
                        if (ductInbox[0].Id.ToString().Equals(filterDucts[0].Id.ToString()))
                        {
                            startMain = lcLine1.GetEndPoint(0);
                            endMain = lcLine1.GetEndPoint(1);
                        }
                        else
                        {
                            startMain = lcLine0.GetEndPoint(0);
                            endMain = lcLine0.GetEndPoint(1);
                        }

                        if (RevitUtils.IsBetween(interSecPoint, startMain, endMain))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public static List<Duct> SplitDuct(Document doc,
                                         List<Duct> ducts,
                                         XYZ intersecPoint,
                                         DuctType ductType,
                                         ref List<Duct> convertedDucts,
                                         ref List<DuctData> ductDatasConverted)
        {
            List<Duct> ductsCreateTee = new List<Duct>();
            using (Transaction reTrans = new Transaction(doc, "TEST"))
            {
                reTrans.Start();
                Curve c1 = (ducts[0].Location as LocationCurve).Curve;
                Curve c2 = (ducts[1].Location as LocationCurve).Curve;

                Duct ductSplit = null;

                if (RevitUtils.IsGreaterThan(c1.Length, c2.Length))
                {
                    ductSplit = ducts[0];
                    ductsCreateTee.Add(ducts[1]);
                }
                else
                {
                    ductSplit = ducts[1];
                    ductsCreateTee.Add(ducts[0]);
                }
                // Remove pipe in list pipe convert
                string index = null;
                for (int i = 0; i < ductDatasConverted.Count; i++)
                {
                    Duct duct = ductDatasConverted[i].ConvertElem as Duct;
                    if (duct != null && duct.IsValidObject && duct.Id.ToString().Equals(ductSplit.Id.ToString()))
                    {
                        index = i.ToString();
                        break;
                    }
                }
                if (!string.IsNullOrEmpty(index))
                {
                    if (ductDatasConverted.Count == convertedDucts.Count)
                    {
                        convertedDucts.RemoveAt(Convert.ToInt32(index));
                        ductDatasConverted.RemoveAt(Convert.ToInt32(index));
                    }
                }

                //// split duct
                List<Duct> newDucts = SplitPipeByIntersectorPoint(doc,
                                                                     ductSplit,
                                                                     ducts[0].MEPSystem,
                                                                     ductType,
                                                                     intersecPoint);

                ductsCreateTee.AddRange(newDucts);
                convertedDucts.AddRange(newDucts);
                foreach (Duct duct in newDucts)
                {
                    DuctData pipeData = new DuctData(ductType.GetTypeId(), duct);
                    ductDatasConverted.Add(pipeData);
                }
                reTrans.Commit();
            }

            return ductsCreateTee;
        }

        public static List<Duct> SplitPipeByIntersectorPoint(Document doc, Element segment, MEPSystem system, DuctType ductType, XYZ pointSplit)
        {
            ElementId levelId = segment.get_Parameter(BuiltInParameter.RBS_START_LEVEL_PARAM).AsElementId();

            // selecting one pipe and taking its location.
            Curve c1 = (segment.Location as LocationCurve).Curve;

            var startPoint = c1.GetEndPoint(0);
            var endPoint = c1.GetEndPoint(1);

            // creating first pipe

            ElementId systemtype = system.GetTypeId();
            List<Duct> splitDucts = new List<Duct>();
            if (ductType != null
                && pointSplit != null)
            {
                Duct duct = Duct.Create(doc, systemtype, ductType.Id, levelId, pointSplit, startPoint);
                Duct duct1 = Duct.Create(doc, systemtype, ductType.Id, levelId, pointSplit, endPoint);

                splitDucts.Add(duct);
                splitDucts.Add(duct1);
                //Copy parameters from previous pipe to the following Pipe.
                Duct sourDuct = segment as Duct;

                if (ductType.Shape == ConnectorProfileType.Round)
                {
                    CommonDataPipeDuct.SetDiameterDuct(sourDuct, duct);
                    CommonDataPipeDuct.SetDiameterDuct(sourDuct, duct1);
                }
                else if (ductType.Shape == ConnectorProfileType.Rectangular)
                {
                    CommonDataPipeDuct.SetHeightWidthDuct(sourDuct, duct);
                    CommonDataPipeDuct.SetHeightWidthDuct(sourDuct, duct1);
                }

                doc.Delete(segment.Id);
            }
            return splitDucts;
        }

        #endregion Create fitting Y,T
    }
}