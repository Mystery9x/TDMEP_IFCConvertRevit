using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using RevitUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using TepscoIFCToRevit.Common;

namespace TepscoIFCToRevit.Data.MEPData
{
    public class DuctData : ElementConvert
    {
        public bool IsElbow { get; set; } = false;

        public ConnectorProfileType DuctConnectorProfileType { get; set; }

        public Connector Connector1 { get; set; }

        public Connector Connector2 { get; set; }

        public ElementId SystemTypeId { get; set; }

        public double RealDiameterDuct { get; set; }

        public double DiameterDuct { get; set; }

        public double RealWidthDuct { get; set; }

        public double WidthDuct { get; set; }

        public double RealHeightDuct { get; set; }

        public double HeightDuct { get; set; }

        #region Constructor

        public DuctData(UIDocument uIDoc, LinkElementData linkElementData, ElementId ductTypeId, RevitLinkInstance revLinkIns, ConvertParamData paramData = null)
        {
            if (uIDoc != null
                && linkElementData != null
                && linkElementData.LinkElement != null
                && linkElementData.LinkElement.IsValidObject
                && revLinkIns != null)
            {
                _uiDoc = uIDoc;
                _doc = _uiDoc.Document;
                LinkInstance = revLinkIns;
                LinkEleData = linkElementData;
                TypeId = ductTypeId;
                ParameterData = paramData;
            }
        }

        public DuctData(ElementId ductTypeId, Duct duct)
        {
            if (ductTypeId != null
                && duct != null)
            {
                ConvertElem = duct;
                TypeId = ductTypeId;
            }
        }

        #endregion Constructor

        #region Method

        /// <summary>
        /// Initialize
        /// </summary>
        public void Initialize()
        {
            if (Location == null)
            {
                return;
            }

            var systemTypeClass = RevitUtils.GetSytemTypeId(MEP_CURVE_TYPE.DUCT);
            FilteredElementCollector ductTypes = new FilteredElementCollector(_doc).OfClass(systemTypeClass);
            foreach (MEPSystemType MEPSysType in ductTypes.Cast<MEPSystemType>())
            {
                SystemTypeId = MEPSysType.Id;
                break;
            }

            GetShape();
            CreateDuctPointToPoint();

            if (DuctConnectorProfileType == ConnectorProfileType.Round)
                SetDiameterDuct();
            else if (DuctConnectorProfileType == ConnectorProfileType.Rectangular)
                SetHeightWidthDuct();

            SetConnector();

            if (ConvertElem?.IsValidObject == true)
            {
                RevitUtils.SetValueParamterConvert(_uiDoc, ConvertElem, LinkEleData, ParameterData);
            }
        }

        private void GetDiameterWidthHeightFromDuctType(bool isRalling)
        {
            try
            {
                List<double> sizeRectagular = new List<double>();
                List<double> sizeRound = new List<double>();
                DuctSizeSettings ductSizeSettings = DuctSizeSettings.GetDuctSizeSettings(_uiDoc.Document);
                foreach (var item in ductSizeSettings)
                {
                    DuctSizes ductSizes = item.Value;
                    if (item.Key == DuctShape.Rectangular)
                    {
                        foreach (MEPSize mepSize in ductSizes)
                        {
                            if (mepSize == null)
                                continue;

                            double size = mepSize.NominalDiameter;

                            sizeRectagular.Add(size);
                        }
                    }
                    else if (item.Key == DuctShape.Round)
                    {
                        foreach (MEPSize mepSize in ductSizes)
                        {
                            if (mepSize == null)
                                continue;

                            double size = mepSize.NominalDiameter;

                            sizeRound.Add(size);
                        }
                    }
                }

                double closetDiameter = FindClosestNumber(RealDiameterDuct, sizeRound);
                DiameterDuct = closetDiameter != double.MinValue ? closetDiameter : RealDiameterDuct;

                double closetWidth = FindClosestNumber(RealWidthDuct, sizeRectagular);
                WidthDuct = closetWidth != double.MinValue ? closetWidth : RealWidthDuct;

                double closetHeight = FindClosestNumber(RealHeightDuct, sizeRectagular);
                HeightDuct = closetHeight != double.MinValue ? closetHeight : RealHeightDuct;

                if (isRalling)
                {
                    DiameterDuct = RealDiameterDuct;
                    WidthDuct = RealWidthDuct;
                    HeightDuct = RealHeightDuct;
                }
            }
            catch (Exception)
            { }
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
            catch (Exception)
            { }
            return double.MinValue;
        }

        #region Process Geometry Generic Model

        /// <summary>
        /// Get geometry from IFC element
        /// </summary>
        public void GetGeometryFromIFCElement(List<GeometryObject> geomertries = null, bool isRailling = false)
        {
            try
            {
                IFCdata = new ElementIFC(_doc, LinkEleData.LinkElement, LinkInstance.GetLinkDocument(), ObjectIFCType.Duct, geomertries, null, isRailling);
                if (IFCdata.Location != null)
                {
                    StartPoint = LinkTransform.OfPoint(IFCdata.Location.GetEndPoint(0));
                    EndPoint = LinkTransform.OfPoint(IFCdata.Location.GetEndPoint(1));
                    Location = Line.CreateBound(StartPoint, EndPoint);
                    RealDiameterDuct = IFCdata.Length.Length;
                    RealHeightDuct = IFCdata.Length.Length;
                    RealWidthDuct = IFCdata.Width.Length;

                    LevelId = RevitUtils.GetLevelClosetTo(_uiDoc.Document, StartPoint);
                    GetDiameterWidthHeightFromDuctType(isRailling);
                }
            }
            catch (Exception)
            { }
        }

        #endregion Process Geometry Generic Model

        #region Create Duct

        /// <summary>
        /// Create Duct Point To Point
        /// </summary>
        private void CreateDuctPointToPoint()
        {
            try
            {
                if (StartPoint.DistanceTo(EndPoint) <= RevitUtilities.Common.MIN_LENGTH)
                {
                    EndPoint = StartPoint + (EndPoint - StartPoint).Normalize() * RevitUtilities.Common.MIN_LENGTH;
                }
                ConvertElem = Duct.Create(_doc, SystemTypeId, TypeId, LevelId, StartPoint, EndPoint);
            }
            catch (Exception)
            { }
        }

        /// <summary>
        /// Set Diameter Duct
        /// </summary>
        private void SetDiameterDuct()
        {
            try
            {
                if (ConvertElem != null && DiameterDuct > 0)
                {
                    UtilsParameter.SetValueParameterBuiltIn(ConvertElem, BuiltInParameter.RBS_CURVE_DIAMETER_PARAM, DiameterDuct);
                }
            }
            catch (Exception)
            { }
        }

        /// <summary>
        /// Set Height Width Duct
        /// </summary>
        private void SetHeightWidthDuct()
        {
            try
            {
                if (ConvertElem != null && WidthDuct > 0)
                {
                    UtilsParameter.SetValueParameterBuiltIn(ConvertElem, BuiltInParameter.RBS_CURVE_WIDTH_PARAM, WidthDuct);
                }

                if (ConvertElem != null && HeightDuct > 0)
                {
                    UtilsParameter.SetValueParameterBuiltIn(ConvertElem, BuiltInParameter.RBS_CURVE_HEIGHT_PARAM, HeightDuct);
                }
            }
            catch (Exception)
            { }
        }

        /// <summary>
        /// Get Shape Duct
        /// </summary>
        private void GetShape()
        {
            try
            {
                if (_uiDoc != null && TypeId != null && TypeId != ElementId.InvalidElementId)
                {
                    if (_uiDoc.Document.GetElement(TypeId) is DuctType ductType)
                        DuctConnectorProfileType = ductType.Shape;
                }
            }
            catch (Exception)
            { }
        }

        #endregion Create Duct

        #region Set Pipe Connector

        private void SetConnector()
        {
            if (ConvertElem is MEPCurve duct &&
                duct.ConnectorManager != null &&
                duct.ConnectorManager.Connectors.Size == 2)
            {
                var connectors = new List<Connector>();
                foreach (Connector connector in duct.ConnectorManager.Connectors)
                {
                    connectors.Add(connector);
                }

                Connector1 = connectors[0];
                Connector2 = connectors[1];
            }
        }

        #endregion Set Pipe Connector

        #endregion Method
    }
}