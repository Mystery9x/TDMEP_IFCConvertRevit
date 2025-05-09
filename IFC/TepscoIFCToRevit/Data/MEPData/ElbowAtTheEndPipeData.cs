using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using RevitUtilities;
using System.Collections.Generic;
using System.Linq;
using TepscoIFCToRevit.Common;
using TepscoIFCToRevit.Common.ComparerUtils;
using TepscoIFCToRevit.Data.GeometryDatas;
using TepscoIFCToRevit.Data.RailingsData;

namespace TepscoIFCToRevit.Data.MEPData
{
    public class ElbowAtTheEndPipeData
    {
        private Document _doc;

        private RevitLinkInstance _linkInstance;

        public Transform RevLinkTransform
        {
            get
            {
                if (_linkInstance != null)
                    return _linkInstance.GetTotalTransform();
                return null;
            }
        }

        public ElbowAtTheEndPipeData(Document doc, RevitLinkInstance revLinkIns)
        {
            _doc = doc;
            _linkInstance = revLinkIns;
        }

        public List<Pipe> GetPipeInterSectWithElbow(List<Pipe> pipesFilter, PipeData elbow)
        {
            BoundingBoxXYZ fittingBox = GeometryUtils.GetBoudingBoxExtend(elbow.LinkEleData.LinkElement, elbow.LinkTransform);

            if (pipesFilter.Count > 1)
            {
                Pipe pipeDimenter = pipesFilter.FirstOrDefault();
                pipesFilter = pipesFilter.Where(x => RevitUtilities.Common.IsEqual(x.Diameter, pipeDimenter.Diameter)).ToList();
            }

            if (RevitUtils.IsLessThanOrEqual(pipesFilter.Count, 1)
                && pipesFilter[0].Location is LocationCurve lcCurve0
                && lcCurve0.Curve is Line lcLine)
                return pipesFilter;

            return null;
        }

        /// <summary>
        /// create elbow of the end pipe
        /// by how to create one more virtual pipe and connect two pipes
        /// </summary>
        /// <param name="pipeConverteds"></param>
        /// <param name="pipeConnotConv"></param>
        /// <param name="elbow"></param>
        /// <returns></returns>
        public FamilyInstance CreateEblowIntheEndPipe(List<Pipe> pipes, PipeData elbow)
        {
            if (pipes[0].Location is LocationCurve lcCurve0
               && lcCurve0.Curve is Line lcLine)
            {
                BoundingBoxXYZ boxElem = elbow.LinkEleData.LinkElement.get_BoundingBox(null);

                XYZ mid = (boxElem.Max + boxElem.Min) / 2;
                Line line = Line.CreateUnbound(lcLine.GetEndPoint(0), lcLine.Direction);

                double project = line.Project(mid).Distance;
                if (!RevitUtils.IsEqual(project, 0, 0.002))
                    return CreateElbow(pipes[0], lcLine, boxElem, elbow.LinkTransform, elbow.LinkEleData.LinkElement);
            }

            return null;
        }

        /// <summary>
        /// create elbow of the end duct
        /// by how to create one more virtual duct and connect two ducts
        /// </summary>
        /// <param name="ductConverteds"></param>
        /// <param name="elbow"></param>
        /// <returns></returns>
        public FamilyInstance CreateEblowOfTheEndDuct(List<Duct> ducts, DuctData elbow)
        {
            BoundingBoxXYZ boxFitting = GeometryUtils.GetBoudingBoxExtend(elbow.LinkEleData.LinkElement, elbow.LinkTransform);

            if (RevitUtils.IsEqual(ducts.Count, 1)
                && ducts[0].Location is LocationCurve lcCurve0 && lcCurve0.Curve is Line lcLine)
            {
                BoundingBoxXYZ boxElem = elbow.LinkEleData.LinkElement.get_BoundingBox(null);

                XYZ mid = (boxElem.Max + boxElem.Min) / 2;
                Line line = Line.CreateUnbound(lcLine.GetEndPoint(0), lcLine.Direction);

                double project = line.Project(mid).Distance;
                if (!RevitUtils.IsEqual(project, 0, 0.002))
                    return CreateElbow(ducts[0], lcLine, boxFitting, elbow.LinkTransform, elbow.LinkEleData.LinkElement);
            }

            return null;
        }

        /// <summary>
        /// create elbow at the end pipe
        /// and deleted virtual pipe
        /// </summary>
        /// <param name="mepObject"></param>
        /// <param name="lcLine"></param>
        /// <param name="boxFitting"></param>
        /// <param name="transform"></param>
        /// <param name="elbow"></param>
        /// <returns></returns>
        private FamilyInstance CreateElbow(MEPCurve mepObject, Line lcLine, BoundingBoxXYZ boxFitting, Transform transform, Element elbow)
        {
            FamilyInstance fitting = null;

            double? dimension = UtilsParameter.GetValueParameterBuiltIn(mepObject, BuiltInParameter.RBS_PIPE_DIAMETER_PARAM);
            double length = (boxFitting.Max - boxFitting.Min).GetLength();
            if (length / dimension > 3.5)
            {
                return CreateElbow(new List<MEPCurve>() { mepObject }, elbow, transform);
            }

            XYZ direction = FindDirectionOfFitting(lcLine, boxFitting);
            XYZ center = GetCenterPointOfSectionEndFitting(elbow, direction, lcLine.Direction);
            if (center != null)
            {
                ElementId levelId = RevitUtils.GetLevelClosetTo(_doc, direction);

                var systemTypeClass = RevitUtils.GetSytemTypeId(MEP_CURVE_TYPE.PIPE);
                FilteredElementCollector pipeTypes = new FilteredElementCollector(_doc).OfClass(systemTypeClass);
                ElementId systemTypeId = pipeTypes.FirstOrDefault()?.Id;

                using (Transaction reTrans = new Transaction(_doc, "createFitting"))
                {
                    reTrans.Start("Create New Pipe");

                    Line linePipe = Line.CreateUnbound(lcLine.GetEndPoint(0), lcLine.Direction);
                    center = transform.OfPoint(center);
                    XYZ startPipe = linePipe.Project(center).XYZPoint;

                    XYZ vector = startPipe - center;

                    if (RevitUtils.IsPerpendicular(vector, lcLine.Direction))
                    {
                        MEPCurve result = null;

                        if (mepObject is Pipe pipe)
                        {
                            result = Pipe.Create(_doc, systemTypeId, pipe.PipeType.Id, levelId, startPipe, center);
                            UtilsParameter.SetValueParameterBuiltIn(result, BuiltInParameter.RBS_PIPE_DIAMETER_PARAM, dimension);
                        }
                        else if (mepObject is Duct duct)
                        {
                            result = Duct.Create(_doc, systemTypeId, duct.DuctType.Id, levelId, startPipe, center);
                            UtilsParameter.SetValueParameterBuiltIn(result, BuiltInParameter.RBS_PIPE_DIAMETER_PARAM, dimension);
                        }

                        reTrans.Commit();

                        // Create Fitting
                        reTrans.Start("Create Fitting");

                        FailureHandlingOptions fhOpts = reTrans.GetFailureHandlingOptions();

                        fitting = GeometryUtils.CreateMepConnector(mepObject,
                                                                   result,
                                                                   out Connector pipeConnector1,
                                                                   out Connector pipeConnector2);

                        REVWarning2 supWarning = new REVWarning2();
                        fhOpts.SetFailuresPreprocessor(supWarning);
                        reTrans.SetFailureHandlingOptions(fhOpts);

                        reTrans.Commit();

                        //Delete pipe has just been create
                        reTrans.Start("Delete Pipe");
                        if (result.IsValidObject && result != null && result.Id != null)
                            _doc.Delete(result.Id);
                        reTrans.Commit();
                    }
                }
            }

            return fitting;
        }

        public FamilyInstance CreateElbow(List<MEPCurve> mepConnects, Element elbow, Transform transform)
        {
            // get pipe filter
            var data = GeometryUtils.GetIfcGeometriess(elbow);
            List<Solid> solids = new List<Solid>();
            foreach (var geo in data)
            {
                if (geo is Solid solidIFC)
                {
                    solids.Add(solidIFC);
                }
            }

            if (solids.Count > 0)
            {
                solids.OrderBy(x => x.Volume);

                if (mepConnects?.Count == 1 ||
                    (mepConnects?.Count == 2
                     && mepConnects[0].Location is LocationCurve lcCurve0
                     && lcCurve0.Curve is Line lcLine0
                     && mepConnects[1].Location is LocationCurve lcCurve1
                     && lcCurve1.Curve is Line lcLine1
                     && !UtilsCurve.IsLineStraight(lcLine0, lcLine1)))
                    return ElbowRaillingData.CreateElbow(_doc, mepConnects, elbow, transform, solids.LastOrDefault());
            }

            return null;
        }

        /// <summary>
        /// fine direction of elbow
        /// this direction will perpendicular with direction of pipe
        /// </summary>
        /// <param name="lcLinePipe"></param>
        /// <param name="boxFitting"></param>
        /// <returns></returns>
        private XYZ FindDirectionOfFitting(Line lcLinePipe, BoundingBoxXYZ boxFitting)
        {
            XYZ midBoxFitting = (boxFitting.Max + boxFitting.Min) / 2;
            XYZ normal = lcLinePipe.Direction;

            Line newLine = Line.CreateUnbound(lcLinePipe.GetEndPoint(0), lcLinePipe.Direction);

            XYZ originPlane = newLine.Project(midBoxFitting).XYZPoint;

            Plane plane = Plane.CreateByNormalAndOrigin(normal, originPlane);

            XYZ projectPoint = UtilsPlane.ProjectOnto(plane, midBoxFitting);
            XYZ direction = projectPoint - originPlane;

            return direction;
        }

        /// <summary>
        /// fine all point of fitting at the end by direction fitting
        /// </summary>
        /// <param name="elbow"></param>
        /// <param name="direction"></param>
        /// <param name="directionPipe"></param>
        /// <returns></returns>
        private XYZ GetCenterPointOfSectionEndFitting(Element elbow, XYZ direction, XYZ directionPipe)
        {
            List<GeometryObject> geometries = GeometryUtils.GetIfcGeometriess(elbow);
            GeometryData geomertyData = new GeometryData(geometries);

            // sort point

            List<XYZ> allPointObject = geomertyData.Vertices.OrderBy(direction).ToList();
            Plane plane = Plane.CreateByNormalAndOrigin(direction, allPointObject.LastOrDefault());

            List<XYZ> pointEndPipes = allPointObject.Where(x => RevitUtils.IsEqual(UtilsPlane.GetSignedDistance(plane, x), 0)).ToList();
            pointEndPipes = pointEndPipes.Distinct(new PointEqualityComparer(RevitUtils.TOLERANCE)).ToList();

            if (pointEndPipes?.Count > 6
                && CheckShapeElbowIsValid(geomertyData.Vertices, pointEndPipes, directionPipe))
            {
                RevitUtils.FindMaxMinPoint(pointEndPipes, out XYZ min, out XYZ max);
                XYZ center = (min + max) / 2;

                double radius = center.DistanceTo(pointEndPipes[0]);

                foreach (XYZ point in pointEndPipes)
                {
                    double distance = point.DistanceTo(center);
                    if (!RevitUtils.IsEqual(distance, radius, 0.5))
                    {
                        return null;
                    }
                }

                return center;
            }
            return null;
        }

        /// <summary>
        /// check shape  at the end of fitting by the direction fitting is valid
        /// except some cases shape fitting is the same as shap T
        /// </summary>
        /// <param name="allPointElbow"></param>
        /// <param name="pointEndPipes"></param>
        /// <param name="directionPipe"></param>
        /// <returns></returns>
        private bool CheckShapeElbowIsValid(List<XYZ> allPointElbow, List<XYZ> pointEndPipes, XYZ directionPipe)
        {
            // sort point by direction pipe
            List<XYZ> pointByDirectionPipe = allPointElbow.OrderBy(directionPipe).ToList();
            Plane planeEndElbow = Plane.CreateByNormalAndOrigin(directionPipe, pointByDirectionPipe.LastOrDefault());
            List<XYZ> pointEndEblows = pointEndPipes.Where(x => !RevitUtils.IsEqual(UtilsPlane.GetSignedDistance(planeEndElbow, x), 0, 0.05)).ToList();

            if (pointEndEblows.Count > 0)
                return true;

            return false;
        }
    }
}