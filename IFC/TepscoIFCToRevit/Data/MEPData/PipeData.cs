using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using RevitUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using TepscoIFCToRevit.Common;
using TepscoIFCToRevit.Data.RailingsData;

namespace TepscoIFCToRevit.Data.MEPData
{
    public class PipeData : ElementConvert
    {
        public bool IsElbow { get; set; } = false;

        public Connector Connector1 { get; set; }

        public Connector Connector2 { get; set; }

        public ElementId SystemTypeId { get; set; }

        public double RealDiameterPipe { get; set; }

        public double DiameterPipe { get; set; }

        public PipeType ProcessPipeType
        {
            get
            {
                PipeType result;
                if (_doc != null && TypeId != ElementId.InvalidElementId && TypeId != null)
                {
                    result = _doc.GetElement(TypeId) as PipeType;
                }
                else
                {
                    result = new FilteredElementCollector(_doc).OfClass(typeof(PipeType)).FirstElement() as PipeType;
                }
                return result;
            }
        }

        public PipeData(UIDocument uIDoc,
                        LinkElementData linkElementData,
                        ElementId pipeTypeId,
                        RevitLinkInstance revLinkIns,
                        ConvertParamData parameterData = null)
        {
            if (uIDoc != null
                && linkElementData != null
                && linkElementData.LinkElement != null
                && linkElementData.LinkElement.IsValidObject
                && revLinkIns != null)
            {
                _uiDoc = uIDoc;
                _doc = _uiDoc.Document;
                TypeId = pipeTypeId;
                LinkInstance = revLinkIns;
                LinkEleData = linkElementData;
                ParameterData = parameterData;
                Geometries = GeometryUtils.GetIfcGeometriess(LinkEleData.LinkElement);
            }
        }

        public PipeData(ElementId pipeTypeId, Pipe pipe)
        {
            TypeId = pipeTypeId;
            ConvertElem = pipe;
        }

        /// <summary>
        /// Initialize
        /// </summary>
        public void Initialize()
        {
            if (Location == null)
            {
                return;
            }

            var systemTypeClass = RevitUtils.GetSytemTypeId(MEP_CURVE_TYPE.PIPE);
            FilteredElementCollector pipeTypes = new FilteredElementCollector(_doc).OfClass(systemTypeClass);
            foreach (MEPSystemType MEPSysType in pipeTypes.Cast<MEPSystemType>())
            {
                SystemTypeId = MEPSysType.Id;
                break;
            }

            GetDiameterFromPipeType();
            CreatePipePointToPoint();
            SetDiameterPipe();
            SetConnector();

            if (ConvertElem?.IsValidObject == true)
            {
                RevitUtils.SetValueParamterConvert(_uiDoc, ConvertElem, LinkEleData, ParameterData);
            }
        }

        public void GetGeometryFromIFCElement(List<GeometryObject> geometries = null, bool isRailing = false)
        {
            try
            {
                IFCdata = new ElementIFC(_doc, LinkEleData.LinkElement, LinkInstance.GetLinkDocument(), ObjectIFCType.Pipe, geometries, null, isRailing);
                if (IFCdata.Location != null)
                {
                    StartPoint = LinkTransform.OfPoint(IFCdata.Location.GetEndPoint(0));
                    EndPoint = LinkTransform.OfPoint(IFCdata.Location.GetEndPoint(1));
                    Location = Line.CreateBound(StartPoint, EndPoint);
                    RealDiameterPipe = IFCdata.Length.Length;
                }
            }
            catch (Exception)
            { }
        }

        /// <summary>
        /// Create Pipe Point To Point
        /// </summary>
        private void CreatePipePointToPoint()
        {
            try
            {
                ConvertElem = Pipe.Create(_doc, SystemTypeId, TypeId, LevelId, StartPoint, EndPoint);
                if (ConvertElem != null)
                {
                    UtilsParameter.SetValueParameterBuiltIn(ConvertElem, BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS, LinkEleData.LinkElement.Id.IntegerValue.ToString());
                }
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Set Diameter Pipe
        /// </summary>
        private void SetDiameterPipe()
        {
            try
            {
                if (ConvertElem != null && DiameterPipe > 0)
                {
                    UtilsParameter.SetValueParameterBuiltIn(ConvertElem, BuiltInParameter.RBS_PIPE_DIAMETER_PARAM, DiameterPipe);
                }
            }
            catch (Exception) { }
        }

        private void SetConnector()
        {
            if (ConvertElem is MEPCurve pipe &&
                pipe.ConnectorManager != null &&
                pipe.ConnectorManager.Connectors.Size == 2)
            {
                var connectors = new List<Connector>();
                foreach (Connector connector in pipe.ConnectorManager.Connectors)
                {
                    connectors.Add(connector);
                }

                Connector1 = connectors[0];
                Connector2 = connectors[1];
            }
        }

        public void GetDiameterFromPipeType()
        {
            try
            {
                List<double> result = new List<double>();
                if (ProcessPipeType.RoutingPreferenceManager != null)
                {
                    int count = ProcessPipeType.RoutingPreferenceManager.GetNumberOfRules(RoutingPreferenceRuleGroupType.Segments);
                    for (int i = 0; i < count; i++)
                    {
                        var rule = ProcessPipeType.RoutingPreferenceManager.GetRule(RoutingPreferenceRuleGroupType.Segments, i);

                        if (_doc.GetElement(rule.MEPPartId) is PipeSegment segment)
                        {
                            foreach (var mepsize in segment.GetSizes())
                            {
                                if (mepsize == null)
                                    continue;
                                result.Add(mepsize.NominalDiameter);
                            }
                        }
                    }
                }
                if (result.Count <= 0)
                    return;

                double closetDiameter = FindClosestNumber(RealDiameterPipe, result);
                DiameterPipe = closetDiameter != double.MinValue ? closetDiameter : RealDiameterPipe;
                LevelId = RevitUtils.GetLevelClosetTo(_doc, StartPoint);
            }
            catch (Exception) { }
        }

        public double FindClosestNumber(double radius, List<double> list)
        {
            try
            {
                if (list == null || list.Count <= 0)
                    return double.MinValue;

                double closest = list[0];
                double difference = Math.Abs(radius - closest);
                for (int i = 1; i < list.Count; i++)
                {
                    double currentDifference = Math.Abs(radius - list[i]);
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

        public Element CreateElbow(List<MEPCurve> mepConnects, GeometryObject geo)
        {
            // get pipe filter

            if (geo is Solid solidIFC)
            {
                if (mepConnects?.Count == 2
                   && mepConnects[0].Location is LocationCurve lcCurve0
                   && lcCurve0.Curve is Line lcLine0
                   && mepConnects[1].Location is LocationCurve lcCurve1
                   && lcCurve1.Curve is Line lcLine1
                   && !UtilsCurve.IsLineStraight(lcLine0, lcLine1))
                    return ElbowRaillingData.CreateElbow(_doc, mepConnects, LinkEleData.LinkElement, LinkTransform, solidIFC);
            }

            return null;
        }
    }
}