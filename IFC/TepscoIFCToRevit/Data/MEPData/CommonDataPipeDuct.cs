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
    public class CommonDataPipeDuct
    {
        /// <summary>
        /// Remove pipe ovelap in list pipes
        /// </summary>
        public static List<Element> FilterPipeOverlap(List<Element> pipes, XYZ center)
        {
            if (pipes?.Count > 1)
            {
                List<Element> remove = new List<Element>();
                int count = pipes.Count;
                for (int i = 0; i < count - 1; i++)
                {
                    for (int j = i + 1; j < count; j++)
                    {
                        Element pipeI = pipes[i];
                        Element pipeJ = pipes[j];

                        Element filter = FindPipeOverlap(pipeI, pipeJ, center);
                        if (filter != null)
                        {
                            remove.Add(filter);
                        }
                    }
                }

                return pipes.Except(remove).ToList();
            }

            return new List<Element>();
        }

        private static Element FindPipeOverlap(Element pipe1, Element pipe2, XYZ center)
        {
            if (pipe1.Location is LocationCurve lc1 && lc1.Curve is Line location1 &&
                pipe2.Location is LocationCurve lc2 && lc2.Curve is Line location2)
            {
                if (RevitUtils.IsBetween(location1.GetEndPoint(0), location2.GetEndPoint(0), location2.GetEndPoint(1))
                    || RevitUtils.IsBetween(location1.GetEndPoint(1), location2.GetEndPoint(0), location2.GetEndPoint(1)))
                {
                    if (location1.Distance(center) < location2.Distance(center))
                    {
                        return pipe2;
                    }
                    else
                    {
                        return pipe1;
                    }
                }
            }
            return null;
        }

        public static bool ValidateConnected(Element elm, BoundingBoxXYZ box)
        {
            ConnectorSet connectorSet = null;
            if (elm is Pipe pipe)
                connectorSet = pipe.ConnectorManager.Connectors;
            else if (elm is Duct duct)
                connectorSet = duct.ConnectorManager.Connectors;

            if (connectorSet != null)
            {
                XYZ center = (box.Min + box.Max) / 2;

                Dictionary<Connector, double> groupConnector = new Dictionary<Connector, double>();
                foreach (var item in connectorSet)
                {
                    if (item is Connector connector)
                    {
                        XYZ origin = connector.Origin;
                        double distance = origin.DistanceTo(center);
                        groupConnector.Add(connector, distance);
                    }
                }

                if (groupConnector.Count > 0)
                {
                    groupConnector = groupConnector.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
                    if (groupConnector.FirstOrDefault().Key.IsConnected)
                        return false;
                }
            }

            return true;
        }

        public static List<Pipe> OrderPipeDuctToFitting(List<Pipe> pipes, XYZ centerBox)
        {
            List<Pipe> validPipes = new List<Pipe>();

            List<Pipe> mainPipes = new List<Pipe>();
            List<int> indexMainPipes = new List<int>();

            for (int i = 0; i < pipes.Count - 1; i++)
            {
                for (int j = i + 1; j < pipes.Count; j++)
                {
                    Line linei = (pipes[i].Location as LocationCurve).Curve as Line;
                    Line linej = (pipes[j].Location as LocationCurve).Curve as Line;

                    Line lineiUnbound = Line.CreateUnbound(linei.GetEndPoint(0), linei.Direction);

                    if (RevitUtils.IsParallel(linei.Direction, linej.Direction)
                        && RevitUtils.IsEqual(lineiUnbound.Distance(linej.GetEndPoint(0)), 0, 0.1)
                        && RevitUtils.IsEqual(lineiUnbound.Distance(linej.GetEndPoint(1)), 0, 0.1))
                    {
                        if (!mainPipes.Select(x => x.Id).Any(x => x == pipes[i].Id))
                        {
                            mainPipes.Add(pipes[i]);
                            indexMainPipes.Add(i);
                        }

                        if (!mainPipes.Select(x => x.Id).Any(x => x == pipes[j].Id))
                        {
                            mainPipes.Add(pipes[j]);
                            indexMainPipes.Add(i);
                        }
                    }
                }
            }

            if (indexMainPipes?.Count > 0)
            {
                validPipes.AddRange(mainPipes);
                indexMainPipes.ForEach(x => pipes.RemoveAt(x));
            }
            else
                return null;

            Dictionary<Pipe, double> distanceMap = new Dictionary<Pipe, double>();
            for (int i = 0; i < pipes.Count; i++)
            {
                double distanMin = 0;
                if (pipes[i].Location is LocationCurve lcCurve
                   && lcCurve.Curve is Line lcLine)
                {
                    XYZ start = lcLine.GetEndPoint(0);
                    XYZ end = lcLine.GetEndPoint(1);

                    double distanceStart = centerBox.DistanceTo(start);
                    double distanceEnd = centerBox.DistanceTo(end);

                    if (distanceStart < distanceEnd)
                        distanMin = distanceStart;
                    else
                        distanMin = distanceEnd;

                    distanceMap.Add(pipes[i], distanMin);
                }
            }

            if (distanceMap?.Count > 0)
            {
                distanceMap = distanceMap.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
                validPipes.Add(distanceMap.FirstOrDefault().Key);
            }
            return validPipes;
        }

        public static List<Pipe> SplitPipe(Document doc,
                                           List<Pipe> pipes,
                                           XYZ intersecPoint,
                                           PipeType pipeType,
                                           ref List<Pipe> convertedPipes,
                                           ref List<PipeData> PipeDatasConverted)
        {
            List<Pipe> pipesCreateTee = new List<Pipe>();
            using (Transaction reTrans = new Transaction(doc, "TEST"))
            {
                reTrans.Start();
                Curve c1 = (pipes[0].Location as LocationCurve).Curve;
                Curve c2 = (pipes[1].Location as LocationCurve).Curve;

                Pipe pipeSplit = null;

                if (RevitUtils.IsGreaterThan(c1.Length, c2.Length))
                {
                    pipeSplit = pipes[0];
                    pipesCreateTee.Add(pipes[1]);
                }
                else
                {
                    pipeSplit = pipes[1];
                    pipesCreateTee.Add(pipes[0]);
                }
                // Remove pipe in list pipe convert
                string index = null;
                for (int i = 0; i < PipeDatasConverted.Count; i++)
                {
                    Pipe pipe = PipeDatasConverted[i].ConvertElem as Pipe;
                    if (pipe != null && pipe.IsValidObject && pipe.Id.ToString().Equals(pipeSplit.Id.ToString()))
                    {
                        index = i.ToString();
                        break;
                    }
                }
                if (!string.IsNullOrEmpty(index))
                {
                    if (PipeDatasConverted.Count == convertedPipes.Count)
                    {
                        convertedPipes.RemoveAt(Convert.ToInt32(index));
                        PipeDatasConverted.RemoveAt(Convert.ToInt32(index));
                    }
                }

                // split pipe
                List<Pipe> newPipes = CommonDataPipeDuct.SplitPipeByIntersectorPoint(doc,
                                                                     pipeSplit,
                                                                     pipes[0].MEPSystem,
                                                                     pipeType,
                                                                     intersecPoint);

                if (newPipes?.Count > 0)
                {
                    pipesCreateTee.AddRange(newPipes);
                    convertedPipes.AddRange(newPipes);
                    foreach (Pipe pipe in newPipes)
                    {
                        PipeData pipeData = new PipeData(pipeType.GetTypeId(), pipe);
                        PipeDatasConverted.Add(pipeData);
                    }
                }

                reTrans.Commit();
            }

            return pipesCreateTee;
        }

        public static List<Pipe> SplitPipeByIntersectorPoint(Document doc, Element segment, MEPSystem system, PipeType pipeType, XYZ pointSplit)
        {
            ElementId levelId = segment.get_Parameter(BuiltInParameter.RBS_START_LEVEL_PARAM).AsElementId();

            // selecting one pipe and taking its location.
            Curve c1 = (segment.Location as LocationCurve).Curve;

            var startPoint = c1.GetEndPoint(0);
            var endPoint = c1.GetEndPoint(1);

            // creating first pipe

            ElementId systemtype = system.GetTypeId();
            List<Pipe> splitPipes = new List<Pipe>();
            if (pipeType != null
                && pointSplit != null)
            {
                Pipe pipeTemplate = segment as Pipe;
                double diameter = pipeTemplate.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).AsDouble();

                double distancePipe = pointSplit.DistanceTo(startPoint);
                double distancePipe1 = pointSplit.DistanceTo(endPoint);

                if (RevitUtils.IsGreaterThan(distancePipe, diameter / 2)
                   && RevitUtils.IsGreaterThan(distancePipe1, diameter / 2))
                {
                    Pipe pipe = Pipe.Create(doc, systemtype, pipeType.Id, levelId, pointSplit, startPoint);
                    Pipe pipe1 = Pipe.Create(doc, systemtype, pipeType.Id, levelId, pointSplit, endPoint);

                    splitPipes.Add(pipe);
                    splitPipes.Add(pipe1);
                    //Copy parameters from previous pipe to the following Pipe.

                    CopyParameters(pipeTemplate, pipe);
                    CopyParameters(pipeTemplate, pipe1);

                    doc.Delete(segment.Id);
                }
            }
            return splitPipes;
        }

        public static void CopyParameters(Pipe source, Pipe target)
        {
            double diameter = source.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).AsDouble();
            target.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).Set(diameter);
        }

        public static double GetDiameterOfDuct(Duct duct)
        {
            return duct.get_Parameter(BuiltInParameter.RBS_CURVE_DIAMETER_PARAM).AsDouble();
        }

        public static List<double> GetHeightWithOfDuct(Duct duct)
        {
            double width = duct.get_Parameter(BuiltInParameter.RBS_CURVE_WIDTH_PARAM).AsDouble();
            double height = duct.get_Parameter(BuiltInParameter.RBS_CURVE_HEIGHT_PARAM).AsDouble();
            return new List<double> { width, height };
        }

        /// <summary>
        /// Set Diameter Duct
        /// </summary>
        public static void SetDiameterDuct(Duct sourDuct, Duct targetDuct)
        {
            try
            {
                if (targetDuct != null && sourDuct != null)
                {
                    double DiameterDuct = GetDiameterOfDuct(sourDuct);
                    UtilsParameter.SetValueParameterBuiltIn(targetDuct, BuiltInParameter.RBS_CURVE_DIAMETER_PARAM, DiameterDuct);
                }
            }
            catch (Exception)
            { }
        }

        /// <summary>
        /// Set Height Width Duct
        /// </summary>
        public static void SetHeightWidthDuct(Duct sourDuct, Duct targetDuct)
        {
            try
            {
                if (targetDuct != null && sourDuct != null)
                {
                    List<double> WidthHeigth = GetHeightWithOfDuct(sourDuct);

                    UtilsParameter.SetValueParameterBuiltIn(targetDuct, BuiltInParameter.RBS_CURVE_WIDTH_PARAM, WidthHeigth[0]);
                    UtilsParameter.SetValueParameterBuiltIn(targetDuct, BuiltInParameter.RBS_CURVE_HEIGHT_PARAM, WidthHeigth[1]);
                }
            }
            catch (Exception)
            { }
        }

        /// <summary>
        /// create fitting Y object
        /// </summary>
        public static FamilyInstance CreatWyeFittingForPipe(Document doc, PipeType pipeType, List<Pipe> pipesCreateTee, XYZ intersecPoint)
        {
            FamilyInstance fittingWye = null;

            try
            {
                using (Transaction reTrans = new Transaction(doc, "TEST"))
                {
                    if (pipesCreateTee?.Count == 3
                    && intersecPoint != null)
                    {
                        //  project loction pipe in connerplane
                        Pipe pipebranch = pipesCreateTee[0];
                        Pipe pipeMain1 = pipesCreateTee[1];
                        Pipe pipeMain2 = pipesCreateTee[2];

                        if (pipebranch.Location is LocationCurve curve1
                           && curve1.Curve is Line lcBranch
                           && pipeMain1.Location is LocationCurve curve2
                           && curve2.Curve is Line lcMain1
                           && pipeMain2.Location is LocationCurve curve3
                           && curve3.Curve is Line lcMain2)
                        {
                            bool isFirstNearest = lcMain1.GetEndPoint(0).DistanceTo(lcBranch.GetEndPoint(0)) < lcMain1.GetEndPoint(0).DistanceTo(lcBranch.GetEndPoint(1));
                            Plane plane = isFirstNearest ?
                                          Plane.CreateByThreePoints(lcMain1.GetEndPoint(0), lcMain1.GetEndPoint(1), lcBranch.GetEndPoint(1)) :
                                          Plane.CreateByThreePoints(lcMain1.GetEndPoint(0), lcMain1.GetEndPoint(1), lcBranch.GetEndPoint(0));
                            reTrans.Start("change location");
                            Line revertLocationBranch = isFirstNearest ?
                                               Line.CreateBound(lcBranch.GetEndPoint(0), lcBranch.GetEndPoint(1)) :
                                               Line.CreateBound(lcBranch.GetEndPoint(1), lcBranch.GetEndPoint(0));
                            //(pipebranch.Location as LocationCurve).Curve = newLocationBranch;
                            reTrans.Commit();

                            // find connectors to create wyefitting

                            List<Connector> connectorTees = new List<Connector>();

                            // connector pipe main1
                            ConnectorManager manager1 = pipeMain1.ConnectorManager;
                            if (intersecPoint.DistanceTo(manager1.Lookup(0).Origin) < intersecPoint.DistanceTo(manager1.Lookup(1).Origin))
                                connectorTees.Add(manager1.Lookup(0));
                            else
                                connectorTees.Add(manager1.Lookup(1));

                            // connector pipe main 2
                            ConnectorManager manager2 = pipeMain2.ConnectorManager;
                            if (intersecPoint.DistanceTo(manager2.Lookup(0).Origin) < intersecPoint.DistanceTo(manager2.Lookup(1).Origin))
                                connectorTees.Add(manager2.Lookup(0));
                            else
                                connectorTees.Add(manager2.Lookup(1));

                            // connector pipe branch
                            ConnectorManager managerBranch = pipebranch.ConnectorManager;
                            if (intersecPoint.DistanceTo(managerBranch.Lookup(0).Origin) < intersecPoint.DistanceTo(managerBranch.Lookup(1).Origin))
                                connectorTees.Add(managerBranch.Lookup(0));
                            else
                                connectorTees.Add(managerBranch.Lookup(1));

                            bool isCreateIllusion = false;
                            double valueAngle = revertLocationBranch.Direction.AngleTo(lcMain1.Direction);
                            if (!RevitUtils.IsEqual(valueAngle, Math.PI / 2))
                                isCreateIllusion = true;

                            // create illusion pipe (branch pipe) which perpendicular with pipe
                            Pipe pipeIllusion = null;
                            if (isCreateIllusion)
                            {
                                Line lcLineMain1 = (pipeMain1.Location as LocationCurve).Curve as Line;

                                XYZ direction = plane.Normal.CrossProduct(lcLineMain1.Direction);
                                XYZ startPoint = intersecPoint;

                                ElementId levelId = pipebranch.get_Parameter(BuiltInParameter.RBS_START_LEVEL_PARAM).AsElementId();

                                reTrans.Start("Create pipe");
                                pipeIllusion = Pipe.Create(doc, pipebranch.MEPSystem.GetTypeId(), pipeType.Id, levelId, startPoint, startPoint + direction * 5);
                                CommonDataPipeDuct.CopyParameters(pipeMain1, pipeIllusion);
                                reTrans.Commit();

                                // connector pipe illusion
                                ConnectorManager manager = pipeIllusion.ConnectorManager;
                                if (intersecPoint.DistanceTo(manager.Lookup(0).Origin) < intersecPoint.DistanceTo(manager.Lookup(1).Origin))
                                    connectorTees.Add(manager.Lookup(0));
                                else
                                    connectorTees.Add(manager.Lookup(1));
                            }

                            try
                            {
                                reTrans.Start("create tee");
                                // create tee fitting
                                fittingWye = doc.Create.NewTeeFitting(connectorTees[0], connectorTees[1], connectorTees.LastOrDefault());

                                if (isCreateIllusion)
                                {
                                    connectorTees.RemoveAt(connectorTees.Count - 1);
                                    doc.Delete(pipeIllusion.Id);
                                }
                                List<XYZ> vectors = new List<XYZ>() { lcMain1.Direction, lcMain2.Direction, revertLocationBranch.Direction };

                                valueAngle = GetActualYFittingAngle(fittingWye, vectors);
                                if (!double.IsNaN(valueAngle))
                                    UtilsParameter.SetValueAllParameterName(fittingWye, "Angle", valueAngle);

                                if (isCreateIllusion)
                                {
                                    var connectorTeeFittings = fittingWye.MEPModel.ConnectorManager.Connectors;
                                    foreach (var iteam in connectorTeeFittings)
                                    {
                                        if (iteam is Connector connector
                                            && !connector.IsConnected)
                                            connector.ConnectTo(connectorTees[2]);
                                    }
                                }
                                FailureHandlingOptions fhOpts = reTrans.GetFailureHandlingOptions();
                                REVWarning2 supWarning = new REVWarning2();
                                fhOpts.SetFailuresPreprocessor(supWarning);
                                reTrans.SetFailureHandlingOptions(fhOpts);

                                reTrans.Commit(fhOpts);
                            }
                            catch (Exception)
                            {
                                reTrans.RollBack();
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
            return fittingWye;
        }

        private static double GetActualYFittingAngle(FamilyInstance fitting, List<XYZ> vectorToPipesFromFittingCenter)
        {
            var connectors = fitting.MEPModel.ConnectorManager.Connectors.Cast<Connector>().ToList();

            Connector secondary = connectors.FirstOrDefault(x => GetConnectorRole(x) == ConnectorRoles.Secondary);
            XYZ secondaryDir = secondary.CoordinateSystem.BasisZ;
            XYZ branchDir = vectorToPipesFromFittingCenter.FirstOrDefault(x => !RevitUtils.IsParallel(x, secondaryDir, 1e-1));

            if (branchDir != null)
            {
                double actualAngle = secondaryDir.AngleTo(branchDir);
                return actualAngle;
            }
            return double.NaN;
        }

        /// <summary>
        /// create fitting Y object for duct
        /// </summary>
        public static FamilyInstance CreatWyeFittingForDuct(Document doc, DuctType ductType, List<Duct> ductsCreateTee, XYZ intersecPoint)
        {
            FamilyInstance fittingWye = null;

            try
            {
                using (Transaction reTrans = new Transaction(doc, "TEST"))
                {
                    if (ductsCreateTee?.Count == 3
                    && intersecPoint != null)
                    {
                        //  project loction duct in connerplane
                        Duct ductbranch = ductsCreateTee[0];
                        Duct ductMain1 = ductsCreateTee[1];
                        Duct ductMain2 = ductsCreateTee[2];

                        if (ductbranch.Location is LocationCurve curve1
                           && curve1.Curve is Line lcBranch
                           && ductMain1.Location is LocationCurve curve2
                           && curve2.Curve is Line lcMain1
                           && ductMain2.Location is LocationCurve curve3
                           && curve3.Curve is Line lcMain2)
                        {
                            bool isFirstNearest = lcMain1.GetEndPoint(0).DistanceTo(lcBranch.GetEndPoint(0)) < lcMain1.GetEndPoint(0).DistanceTo(lcBranch.GetEndPoint(1));
                            Plane plane = isFirstNearest ?
                                          Plane.CreateByThreePoints(lcMain1.GetEndPoint(0), lcMain1.GetEndPoint(1), lcBranch.GetEndPoint(1)) :
                                          Plane.CreateByThreePoints(lcMain1.GetEndPoint(0), lcMain1.GetEndPoint(1), lcBranch.GetEndPoint(0));
                            reTrans.Start("change location");
                            Line newLocationBranch = isFirstNearest ?
                                               Line.CreateBound(lcBranch.GetEndPoint(0), lcBranch.GetEndPoint(1)) :
                                               Line.CreateBound(lcBranch.GetEndPoint(1), lcBranch.GetEndPoint(0));
                            (ductbranch.Location as LocationCurve).Curve = newLocationBranch;
                            reTrans.Commit();

                            // find connectors to create wyefitting

                            List<Connector> connectorTees = new List<Connector>();

                            // connector duct main1
                            ConnectorManager manager1 = ductMain1.ConnectorManager;
                            if (intersecPoint.DistanceTo(manager1.Lookup(0).Origin) < intersecPoint.DistanceTo(manager1.Lookup(1).Origin))
                                connectorTees.Add(manager1.Lookup(0));
                            else
                                connectorTees.Add(manager1.Lookup(1));

                            // connector duct main 2
                            ConnectorManager manager2 = ductMain2.ConnectorManager;
                            if (intersecPoint.DistanceTo(manager2.Lookup(0).Origin) < intersecPoint.DistanceTo(manager2.Lookup(1).Origin))
                                connectorTees.Add(manager2.Lookup(0));
                            else
                                connectorTees.Add(manager2.Lookup(1));

                            // connector duct branch
                            ConnectorManager managerBranch = ductbranch.ConnectorManager;
                            if (intersecPoint.DistanceTo(managerBranch.Lookup(0).Origin) < intersecPoint.DistanceTo(managerBranch.Lookup(1).Origin))
                                connectorTees.Add(managerBranch.Lookup(0));
                            else
                                connectorTees.Add(managerBranch.Lookup(1));

                            bool isCreateIllusion = false;
                            double valueAngle = newLocationBranch.Direction.AngleTo(lcMain1.Direction);
                            if (!RevitUtils.IsEqual(valueAngle, Math.PI / 2))
                                isCreateIllusion = true;

                            // create illusion duct (branch duct) which perpendicular with duct
                            Duct ductIllusion = null;
                            if (isCreateIllusion)
                            {
                                Line lcLineMain1 = (ductMain1.Location as LocationCurve).Curve as Line;

                                XYZ direction = plane.Normal.CrossProduct(lcLineMain1.Direction);
                                XYZ startPoint = intersecPoint;

                                ElementId levelId = ductbranch.get_Parameter(BuiltInParameter.RBS_START_LEVEL_PARAM).AsElementId();

                                reTrans.Start("Create pipe");
                                ductIllusion = Duct.Create(doc, ductbranch.MEPSystem.GetTypeId(), ductType.Id, levelId, startPoint, startPoint + direction * 5);

                                //Copy parameters from previous duct to the following duct.

                                if (ductType.Shape == ConnectorProfileType.Round)
                                    CommonDataPipeDuct.SetDiameterDuct(ductIllusion, ductMain1);
                                else if (ductType.Shape == ConnectorProfileType.Rectangular)
                                    CommonDataPipeDuct.SetHeightWidthDuct(ductIllusion, ductMain1);

                                reTrans.Commit();

                                // connector duct illusion
                                ConnectorManager manager = ductIllusion.ConnectorManager;
                                if (intersecPoint.DistanceTo(manager.Lookup(0).Origin) < intersecPoint.DistanceTo(manager.Lookup(1).Origin))
                                    connectorTees.Add(manager.Lookup(0));
                                else
                                    connectorTees.Add(manager.Lookup(1));
                            }

                            reTrans.Start("create tee");
                            // create tee fitting
                            fittingWye = doc.Create.NewTeeFitting(connectorTees[0], connectorTees[1], connectorTees.LastOrDefault());

                            if (isCreateIllusion)
                            {
                                connectorTees.RemoveAt(connectorTees.Count - 1);
                                doc.Delete(ductIllusion.Id);
                            }
                            List<XYZ> vectors = new List<XYZ>() { lcMain1.Direction, lcMain2.Direction, newLocationBranch.Direction };

                            valueAngle = GetActualYFittingAngle(fittingWye, vectors);
                            if (!double.IsNaN(valueAngle))
                                UtilsParameter.SetValueAllParameterName(fittingWye, "Angle", valueAngle);

                            if (isCreateIllusion)
                            {
                                var connectorTeeFittings = fittingWye.MEPModel.ConnectorManager.Connectors;
                                foreach (var iteam in connectorTeeFittings)
                                {
                                    if (iteam is Connector connector
                                        && !connector.IsConnected)
                                        connector.ConnectTo(connectorTees[2]);
                                }
                            }
                            reTrans.Commit();
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
            return fittingWye;
        }

        private static ConnectorRoles GetConnectorRole(Connector connector)
        {
            if (connector != null)
            {
                var info = connector.GetMEPConnectorInfo();
                if (info.IsValidObject)
                {
                    if (info.IsPrimary)
                        return ConnectorRoles.Primary;
                    else if (info.IsSecondary)
                        return ConnectorRoles.Secondary;
                    else
                        return ConnectorRoles.Branch;
                }
            }
            return ConnectorRoles.Undefined;
        }
    }

    public enum ConnectorRoles
    {
        Undefined,
        Primary,
        Secondary,
        Branch,
    }
}