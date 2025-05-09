using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using RevitUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using TepscoIFCToRevit.Common;
using TepscoIFCToRevit.Data.MEPData;

namespace TepscoIFCToRevit.Data
{
    public class RunConvertPipeObject
    {
        /// <summary>
        /// convert object ifc to pipe of revit
        /// </summary>
        public static bool ConvertForPipe(Document doc,
                                          PipeData data,
                                          ref List<PipeData> datasConverted,
                                          ref List<PipeData> datasNotConvented)
        {
            if (doc?.IsValidObject != true)
            {
                return false;
            }

            bool isSuccess = true;
            using (Transaction tr = new Transaction(doc, "Create Pipe"))
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

                            data.Initialize();
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

        #region Create tee fitting

        public static FamilyInstance CreateTeeFitting(Document doc, PipeData pipeDataItm, List<Pipe> pipeIntersects)
        {
            try
            {
                if (IsValidCreateTeeFitting(pipeDataItm, ref pipeIntersects))
                {
                    var fitting = CreatePipeTeeFitting(doc, pipeIntersects);
                    if (fitting?.IsValidObject == true)
                        return fitting;
                }
            }
            catch (Exception) { }
            return null;
        }

        private static bool IsValidCreateTeeFitting(PipeData tee, ref List<Pipe> pipes)
        {
            // Because the direction of the pipes has an error, it needs to be increased tolerance

            BoundingBoxXYZ boxFitting = GeometryUtils.GetBoudingBoxExtend(tee.LinkEleData.LinkElement, tee.LinkTransform);

            XYZ center = (boxFitting.Min + boxFitting.Max) / 2;
            pipes = CommonDataPipeDuct.FilterPipeOverlap(pipes.Cast<Element>().ToList(), center).Cast<Pipe>().ToList();

            pipes = GetPipeVailid(pipes, out List<Line> _);

            if (pipes?.Count > 3)
                pipes = CommonDataPipeDuct.OrderPipeDuctToFitting(pipes, center);

            if (pipes?.Count == 3
                && pipes[0].Location is LocationCurve lcCurve0
                && lcCurve0.Curve is Line lcLine0
                && pipes[1].Location is LocationCurve lcCurve1
                && lcCurve1.Curve is Line lcLine1
                && pipes[2].Location is LocationCurve lcCurve2
                && lcCurve2.Curve is Line lcLine2) // case has 3 pipe and check 3 pipes has create become Y, T geomertry
            {
                // check case pipe touch pipes other but not intersect
                if (!GeometryUtils.IsNotTouchSolids(lcLine0, lcLine1, lcLine2))
                    return false;

                // Check whether the fitting geometry is a T, Y or not
                for (int i = 0; i < pipes.Count; i++)
                {
                    // pipe[i] is pipe in location perpendicular with two other pipes

                    if (i == 0
                        && RevitUtils.IsParallel(lcLine1.Direction, lcLine2.Direction, 0.03)
                        && RevitUtils.IsEqual(pipes[1].Diameter, pipes[2].Diameter))
                    {
                        // shape T
                        if (RevitUtils.IsPerpendicular(lcLine0.Direction, lcLine1.Direction, 0.03)
                            && RevitUtils.IsPerpendicular(lcLine0.Direction, lcLine2.Direction, 0.03))
                            return true;

                        // shap Y
                        XYZ direction1 = lcLine1.Direction;
                        XYZ direction2 = lcLine2.Direction;
                        if (RevitUtils.IsEqual(lcLine1.Direction, lcLine2.Direction, 0.03))
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
                        && RevitUtils.IsParallel(lcLine0.Direction, lcLine2.Direction, 0.03)
                        && RevitUtils.IsEqual(pipes[0].Diameter, pipes[2].Diameter))
                    {
                        // shap T
                        if (RevitUtils.IsPerpendicular(lcLine1.Direction, lcLine0.Direction, 0.03)
                            && RevitUtils.IsPerpendicular(lcLine1.Direction, lcLine2.Direction, 0.03))
                            return true;

                        // shap Y
                        XYZ direction1 = lcLine0.Direction;
                        XYZ direction2 = lcLine2.Direction;
                        if (RevitUtils.IsEqual(lcLine0.Direction, lcLine2.Direction, 0.03))
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
                        && RevitUtils.IsParallel(lcLine0.Direction, lcLine1.Direction, 0.03)
                        && RevitUtils.IsEqual(pipes[0].Diameter, pipes[1].Diameter))

                    {
                        // shap T
                        if (RevitUtils.IsPerpendicular(lcLine2.Direction, lcLine0.Direction, 0.03)
                             && RevitUtils.IsPerpendicular(lcLine2.Direction, lcLine1.Direction, 0.03))
                            return true;

                        // shap Y
                        XYZ direction1 = lcLine0.Direction;
                        XYZ direction2 = lcLine1.Direction;
                        if (RevitUtils.IsEqual(lcLine0.Direction, lcLine1.Direction, 0.03))
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

        /// <summary>
        /// remove pipes invalid when get pipes intersector with bounding box of fitting
        /// </summary>
        /// <param name="pipes"></param>
        /// <returns></returns>
        private static List<Pipe> GetPipeVailid(List<Pipe> pipes, out List<Line> projectLines)
        {
            projectLines = new List<Line>();

            if (pipes?.Count > 1)
            {
                List<Line> lcLines = new List<Line>();
                List<Pipe> pipeStraight = new List<Pipe>();
                foreach (Pipe pipe in pipes)
                {
                    if (pipe.Location is LocationCurve lcCurve
                        && lcCurve.Curve is Line lcLine)
                    {
                        lcLines.Add(lcLine);
                        pipeStraight.Add(pipe);
                    }
                }

                if (lcLines.Count > 0)
                    projectLines.AddRange(lcLines);

                Plane plane = null;
                for (int i = 0; i < lcLines.Count - 1; i++)
                {
                    for (int j = 0; j < lcLines.Count; j++)
                    {
                        if (!RevitUtils.IsParallel(lcLines[i].Direction, lcLines[j].Direction, 0.04))
                        {
                            plane = Plane.CreateByThreePoints(lcLines[i].GetEndPoint(0), lcLines[i].GetEndPoint(1), lcLines[j].GetEndPoint(0));
                            break;
                        }
                    }
                }

                List<Pipe> validPipes = new List<Pipe>();
                if (plane != null)
                {
                    List<Line> unboundLines = new List<Line>();
                    projectLines.Clear();

                    foreach (var line in lcLines)
                    {
                        XYZ start = UtilsPlane.ProjectOnto(plane, line.GetEndPoint(0));
                        XYZ end = UtilsPlane.ProjectOnto(plane, line.GetEndPoint(1));
                        unboundLines.Add(Line.CreateUnbound(start, end - start));

                        if (!RevitUtils.IsEqual(start, end))
                            projectLines.Add(Line.CreateBound(start, end));
                    }

                    if (pipeStraight?.Count > 3)
                    {
                        for (int i = 0; i < unboundLines.Count; i++)
                        {
                            int countInter = 0;
                            for (int j = 0; j < unboundLines.Count; j++)
                            {
                                if (i != j
                                    && (RevitUtils.IntersectLine(unboundLines[i], unboundLines[j])
                                        || (RevitUtils.IsParallel(unboundLines[i].Direction, unboundLines[j].Direction, 0.05)
                                            && RevitUtils.IsEqual(unboundLines[i].Distance(unboundLines[j].Origin), 0, 0.05))))
                                {
                                    countInter++;
                                }

                                if (countInter == 2)
                                {
                                    validPipes.Add(pipeStraight[i]);
                                    break;
                                }
                            }

                            if (countInter < 2)
                                projectLines.RemoveAt(i);
                        }
                    }
                }

                if (validPipes.Count > 0)
                    pipes = validPipes;
            }

            return pipes;
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

        private static FamilyInstance CreatePipeTeeFitting(Document doc, List<Pipe> pipeIntersects)
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

                    if (RevitUtils.GetSymbolSeted(doc, pipeIntersects[0], RoutingPreferenceRuleGroupType.Junctions) == null)
                        return null;

                    tr.Start();
                    fitting = TeeFittingData.CreateTeeWyeFitting(doc, pipeIntersects[0], pipeIntersects[1], pipeIntersects[2]);

                    if (fitting?.IsValidObject == true)
                        tr.Commit(failureRollBack);
                    else
                        tr.RollBack();
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

        public static bool IsCreateTeeFitingWithTwoPipes(Document doc, List<Pipe> pipes, PipeData tee, out List<Pipe> filterPipes, out XYZ interSecPoint)

        {
            filterPipes = new List<Pipe>();
            interSecPoint = null;
            BoundingBoxXYZ boxFitting = GeometryUtils.GetBoudingBoxExtend(tee.LinkEleData.LinkElement, tee.LinkTransform);

            List<ElementId> pipeIds = pipes.Where(x => x != null && x.IsValidObject).Select(x => x.Id).ToList();
            if (pipeIds?.Count > 0 && boxFitting != null)
            {
                Outline outline = new Outline(boxFitting.Min, boxFitting.Max);

                boxFitting = new BoundingBoxXYZ()
                {
                    Max = outline.MaximumPoint,
                    Min = outline.MinimumPoint,
                };

                BoundingBoxIntersectsFilter boxFilter = new BoundingBoxIntersectsFilter(outline);
                filterPipes = new FilteredElementCollector(doc, pipeIds).WherePasses(boxFilter)
                                                                             .Cast<Pipe>()
                                                                             .ToList();
            }

            XYZ center = (boxFitting.Min + boxFitting.Max) / 2;
            filterPipes = CommonDataPipeDuct.FilterPipeOverlap(filterPipes.Cast<Element>().ToList(), center).Cast<Pipe>().ToList();

            if (filterPipes?.Count == 2
               && filterPipes[0].Location is LocationCurve lcCure0
               && lcCure0.Curve is Line lcLine0
               && filterPipes[1].Location is LocationCurve lcCure1
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
                if (startProject.DistanceTo(startPoint1) > filterPipes[0].Diameter / 5)
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

                    List<string> idPipes = filterPipes.Select(x => x.Id.ToString()).ToList();
                    List<Pipe> pipeInbox = filterPipes.Where(x => GeometryUtils.IsEndElementInBox(x, boxFitting))
                                          .Where(x => idPipes.Contains(x.Id.ToString()))
                                          .Cast<Pipe>()
                                          .ToList();

                    if (pipeInbox.Count == 1)
                    {
                        XYZ startMain = null;
                        XYZ endMain = null;
                        if (pipeInbox[0].Id.ToString().Equals(filterPipes[0].Id.ToString()))
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

        #endregion Create tee fitting

        #region Create Transaction Fitting

        public static FamilyInstance CreateTransactionFitting(Document doc, PipeData pipeDataItm, List<Pipe> pipes)
        {
            if (doc?.IsValidObject != true)
            {
                return null;
            }

            FamilyInstance fitting = null;
            using (Transaction tr = new Transaction(doc, "Create transaction fetting"))
            {
                try
                {
                    FailureHandlingOptions failureRollBack = tr.GetFailureHandlingOptions();
                    failureRollBack.SetFailuresPreprocessor(new REVWarning3());
                    failureRollBack.SetClearAfterRollback(true);

                    if (pipes.Count == 2
                        && pipes[0].Location is LocationCurve lcCurve0
                        && lcCurve0.Curve is Line lcLine0
                        && pipes[1].Location is LocationCurve lcCurve1
                        && lcCurve1.Curve is Line lcLine1 && RevitUtils.IsLineStraightOverlap(lcLine0, lcLine1, 0.02))

                    {
                        if (RevitUtils.GetSymbolSeted(doc, pipes[0], RoutingPreferenceRuleGroupType.Transitions) == null)
                            return null;

                        tr.Start();
                        RevitUtils.GetConnectorClosedTo(pipes[0].ConnectorManager, pipes[1].ConnectorManager, out Connector con1, out Connector con2);

                        if (con1 != null && con2 != null
                            && !con1.IsConnected && !con2.IsConnected
                            && !con1.IsConnectedTo(con2) && con1.Radius != con2.Radius)
                        {
                            XYZ origin = con1.Origin;
                            fitting = doc.Create.NewTransitionFitting(con1, con2);
                            doc.Regenerate();

                            con1 = RevitUtils.GetConnectorNearest(origin, fitting?.MEPModel?.ConnectorManager, out con2);
                            if (con1 != null && origin != null)
                            {
                                XYZ transition = (con1.Origin - origin);
                                ElementTransformUtils.MoveElement(doc, fitting.Id, transition);
                            }
                        }

                        if (fitting?.IsValidObject == true)
                            tr.Commit(failureRollBack);
                        else
                        {
                            tr.RollBack();
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

        #endregion Create Transaction Fitting

        #region CreateElbow and elbow at the end

        public static FamilyInstance CreateElbow(Document doc,
                                                 RevitLinkInstance revLnkIns,
                                                 List<Pipe> pipesFilter,
                                                 PipeData pipeDataItm,
                                                 BoundingBoxXYZ boxFitting,
                                                 List<Pipe> pipesConverted)
        {
            FamilyInstance fitting = null;
            try
            {
                fitting = CreatePipeElbowFitting(doc, pipesFilter, pipeDataItm);
                if (fitting?.IsValidObject != true)
                {
                    pipesFilter = GeometryUtils.FindPipeNearestBox(doc, pipesConverted.Where(x => x != null && x.IsValidObject).Select(x => x.Id).ToList(), boxFitting, false);
                    ElbowAtTheEndPipeData elbowAtTheEnData = new ElbowAtTheEndPipeData(doc, revLnkIns);
                    List<Pipe> intersectorPipes = elbowAtTheEnData.GetPipeInterSectWithElbow(pipesFilter, pipeDataItm);
                    if (intersectorPipes?.Count == 1)
                    {
                        fitting = elbowAtTheEnData.CreateEblowIntheEndPipe(intersectorPipes, pipeDataItm);
                    }
                }
            }
            catch (Exception) { }

            return fitting?.IsValidObject == true ? fitting : null;
        }

        private static FamilyInstance CreatePipeElbowFitting(Document doc, List<Pipe> pipes, PipeData elbow)
        {
            if (doc?.IsValidObject != true)
            {
                return null;
            }

            FamilyInstance fitting = null;
            using (Transaction tr = new Transaction(doc, "Create elbow"))
            {
                try
                {
                    FailureHandlingOptions failureRollBack = tr.GetFailureHandlingOptions();
                    failureRollBack.SetFailuresPreprocessor(new REVWarning3());
                    failureRollBack.SetClearAfterRollback(true);

                    BoundingBoxXYZ boxFitting = GeometryUtils.GetBoudingBoxExtend(elbow.LinkEleData.LinkElement, elbow.LinkTransform);
                    if (pipes.Count > 1)
                    {
                        Pipe pipeDimenter = pipes.FirstOrDefault();
                        pipes = pipes.Where(x => RevitUtilities.Common.IsEqual(x.Diameter, pipeDimenter.Diameter)).ToList();
                    }

                    if (pipes.Count > 2)
                    {
                        Dictionary<XYZ, List<Pipe>> lcLinePipes = new Dictionary<XYZ, List<Pipe>>();

                        foreach (Pipe pipe in pipes)
                        {
                            if (pipe.Location is LocationCurve lc
                                 && lc.Curve is Line lcLine)
                            {
                                XYZ exitKey = lcLinePipes.Keys.Where(x => RevitUtils.IsParallel(lcLine.Direction, x)).ToList().FirstOrDefault();
                                if (exitKey != null)
                                    lcLinePipes[exitKey].Add(pipe);
                                else
                                    lcLinePipes.Add(lcLine.Direction, new List<Pipe>() { pipe });
                            }
                            else
                                return null;
                        }

                        if (lcLinePipes.Count == 2)
                        {
                            XYZ direction1 = lcLinePipes.Keys.FirstOrDefault();
                            XYZ direction2 = lcLinePipes.Keys.LastOrDefault();
                            double angle = direction1.AngleTo(direction2);
                            if (RevitUtils.IsGreaterThan(angle, 0)
                                && RevitUtils.IsLessThan(angle, Math.PI))
                            {
                                List<Pipe> pipeOrder1 = lcLinePipes.FirstOrDefault().Value.ToList();
                                List<Pipe> pipeOrder2 = lcLinePipes.LastOrDefault().Value.ToList();

                                pipes.Clear();
                                pipes.Add(RemovePipeParrall(pipeOrder1));
                                pipes.Add(RemovePipeParrall(pipeOrder2));
                            }
                        }
                        else
                            return null;
                    }

                    if (pipes.Count == 2
                        && pipes[0].Location is LocationCurve lcCurve0
                        && lcCurve0.Curve is Line lcLine0
                        && pipes[1].Location is LocationCurve lcCurve1
                        && lcCurve1.Curve is Line lcLine1
                        && !UtilsCurve.IsLineStraight(lcLine0, lcLine1))
                    {
                        // create fitting
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

                        if (RevitUtils.IsLessThan(Math.Abs(UtilsPlane.GetSignedDistance(plane0, midFitting)), RevitUtilities.Common.MIN_LENGTH) &&
                           RevitUtils.IsLessThan(Math.Abs(UtilsPlane.GetSignedDistance(plane1, midFitting)), RevitUtilities.Common.MIN_LENGTH))
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
                                (pipes[0].Location as LocationCurve).Curve = newLocation0;
                                (pipes[1].Location as LocationCurve).Curve = newLocation1;
                                reSubTrans.Commit();
                            }

                            using (SubTransaction reSubTrans = new SubTransaction(doc))
                            {
                                reSubTrans.Start();
                                fitting = GeometryUtils.CreatePipeConnector(pipes[0], pipes[1], out Connector pipeConnector1, out Connector pipeConnector2);
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

        public static Pipe RemovePipeParrall(List<Pipe> pipeOrders)
        {
            Pipe pipeMaxLength = pipeOrders.FirstOrDefault();
            if (pipeOrders.Count > 0)
            {
                LocationCurve locationCurve = pipeMaxLength.Location as LocationCurve;
                double lengthMax = locationCurve.Curve.Length;

                foreach (Pipe pipe in pipeOrders)
                {
                    if (pipe.Location is LocationCurve lc
                       && RevitUtils.IsGreaterThan(lc.Curve.Length, lengthMax))
                    {
                        lengthMax = lc.Curve.Length;
                        pipeMaxLength = pipe;
                    }
                }
            }
            return pipeMaxLength;
        }

        #endregion CreateElbow and elbow at the end
    }
}