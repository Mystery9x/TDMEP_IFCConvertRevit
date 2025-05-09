using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using TepscoIFCToRevit.Common;

namespace TepscoIFCToRevit.Data.StructuralData
{
    public class BeamData : ElementConvert
    {
        #region Property

        public bool ValidObject { get; set; } = true;

        public FamilySymbol FamilySymbolBeam
        {
            get
            {
                if (_doc != null && TypeId != null && _doc.GetElement(TypeId) is FamilySymbol symbol)
                {
                    return symbol;
                }
                return null;
            }
        }

        public Level LevelCloset
        {
            get
            {
                if (_doc != null && LevelId != null && _doc.GetElement(LevelId) is Level level)
                {
                    return level;
                }
                return null;
            }
        }

        public Line BeamLine
        {
            get
            {
                Line result = null;
                if (StartPoint != XYZ.Zero && EndPoint != XYZ.Zero)
                {
                    result = Line.CreateBound(StartPoint, EndPoint);
                }
                return result;
            }
        }

        public new ElementId LevelId { get; set; }

        #endregion Property

        #region Constructor

        public BeamData(UIDocument uIDoc,
                        LinkElementData linkElementData,
                        ElementId familySymbolId,
                        RevitLinkInstance revLinkIns,
                        ConvertParamData paramInIFC = null)
        {
            if (uIDoc != null
               && linkElementData?.LinkElement?.IsValidObject != null
               && familySymbolId != null
               && familySymbolId != ElementId.InvalidElementId)
            {
                _uiDoc = uIDoc;
                _doc = uIDoc.Document;
                LinkInstance = revLinkIns;
                LinkEleData = linkElementData;
                TypeId = familySymbolId;
                ParameterData = paramInIFC;
                try
                {
                    GetGeoemetryFromIFCElement();
                }
                catch (Exception)
                {
                    ValidObject = false;
                }
            }
            else
            {
                ValidObject = false;
            }
        }

        #endregion Constructor

        #region Method

        /// <summary>
        /// Create Beam
        /// </summary>
        public void CreateBeam()
        {
            try
            {
                if (!FamilySymbolBeam.IsActive)
                {
                    FamilySymbolBeam.Activate();
                }
                ConvertElem = _doc.Create.NewFamilyInstance(BeamLine, FamilySymbolBeam, LevelCloset, Autodesk.Revit.DB.Structure.StructuralType.Beam);
            }
            catch (System.Exception) { }
        }

        public void SetGeometryPositionOfBeam()
        {
            if (ConvertElem is FamilyInstance instance)
            {
                var yzJustificationParam = instance.get_Parameter(BuiltInParameter.YZ_JUSTIFICATION);
                if (yzJustificationParam != null)
                {
                    yzJustificationParam.Set(0);

                    SetGeometryWithOptionUniform(instance);
                }
            }
        }

        private void SetGeometryWithOptionUniform(FamilyInstance beam)
        {
            try
            {
                if (beam != null)
                {
                    // position
                    var yJustificationParam = beam.get_Parameter(BuiltInParameter.Y_JUSTIFICATION);
                    yJustificationParam?.Set(2);

                    var zJustificationParam = beam.get_Parameter(BuiltInParameter.Z_JUSTIFICATION);
                    zJustificationParam?.Set(0);

                    // offset
                    var yOffset = beam.get_Parameter(BuiltInParameter.Y_OFFSET_VALUE);
                    yOffset?.Set(0);

                    var zOffset = beam.get_Parameter(BuiltInParameter.Z_OFFSET_VALUE);
                    zOffset?.Set(0);
                }
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Get location line from IFC element
        /// </summary>
        private void GetGeoemetryFromIFCElement()
        {
            IFCdata = new ElementIFC(_doc, LinkEleData.LinkElement, LinkInstance.GetLinkDocument(), ObjectIFCType.Beam);
            if (IFCdata.Location != null)
            {
                List<XYZ> locationPoints = new List<XYZ>() { IFCdata.Location.GetEndPoint(0), IFCdata.Location.GetEndPoint(1) };
                Line locationLine = IFCdata.GetLocationLineTranformOfObject(LinkInstance, locationPoints);

                if (locationLine != null && locationLine.Length >= RevitUtilities.Common.MIN_LENGTH)
                {
                    StartPoint = locationLine.GetEndPoint(0);
                    EndPoint = locationLine.GetEndPoint(1);
                }
            }
        }

        #endregion Method
    }
}