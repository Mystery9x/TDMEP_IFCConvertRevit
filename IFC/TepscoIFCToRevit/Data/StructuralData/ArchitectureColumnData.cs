using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using TepscoIFCToRevit.Common;

namespace TepscoIFCToRevit.Data.StructuralData
{
    public class ArchitectureColumnData : ElementConvert
    {
        #region Property

        public bool ValidObject { get; set; } = true;

        public XYZ Facing { get; set; }

        public FamilySymbol FamilySymbolArchiColumn
        {
            get
            {
                if (_doc != null && TypeId != null && _doc.GetElement(TypeId) is FamilySymbol symbol)
                {
                    if (symbol.IsActive)
                    {
                        symbol.Activate();
                    }
                    return symbol;
                }
                return null;
            }
        }

        public Level BaseLevelCloset
        {
            get
            {
                if (_doc != null && BaseLevelId != null && _doc.GetElement(BaseLevelId) is Level level)
                {
                    return level;
                }
                return null;
            }
        }

        public Level TopLevelCloset
        {
            get
            {
                if (_doc != null && TopLevelId != null && _doc.GetElement(TopLevelId) is Level level)
                {
                    return level;
                }
                return null;
            }
        }

        public double BaseOffset { get; set; }

        public double TopOffset { get; set; }

        public XYZ OriginPnt { get; set; }
        public Line ArchiColumnLine { get; set; }
        public Line BeforeTransformLine { get; set; }
        public ElementId BaseLevelId { get; set; }
        public ElementId TopLevelId { get; set; }

        public Line LocationLineTransform = null;

        #endregion Property

        #region Constructor

        public ArchitectureColumnData(UIDocument uIDoc,
                                      LinkElementData linkElementData,
                                      ElementId familySymbolId,
                                      RevitLinkInstance revLinkIns,
                                      ConvertParamData nameParamInIFC = null)
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
                ParameterData = nameParamInIFC;

                try
                {
                    GetLocationLineIFCElement();
                    GetLevelIdFromIFCElement();
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
        /// Initialize
        /// </summary>
        public void Initialize()
        {
            CreateStructuralColumn();
        }

        /// <summary>
        /// Get level Id from IFC element
        /// </summary>
        private void GetLevelIdFromIFCElement()
        {
            if (ArchiColumnLine != null && ArchiColumnLine.Length > 0.0)
            {
                BaseLevelId = RevitUtils.GetLevelClosetTo(_doc, ArchiColumnLine.GetEndPoint(0));
                TopLevelId = RevitUtils.GetLevelClosetTo(_doc, ArchiColumnLine.GetEndPoint(1));
                BaseOffset = ArchiColumnLine.GetEndPoint(0).Z - BaseLevelCloset.Elevation;
                TopOffset = TopLevelCloset.Elevation - ArchiColumnLine.GetEndPoint(1).Z;
            }
        }

        /// <summary>
        /// Create Structural Column
        /// </summary>
        private void CreateStructuralColumn()
        {
            try
            {
                if (!FamilySymbolArchiColumn.IsActive)
                {
                    FamilySymbolArchiColumn.Activate();
                }
                if (RevitUtils.IsParallel(XYZ.BasisZ, ArchiColumnLine.Direction))
                {
                    ConvertElem = _doc.Create.NewFamilyInstance(ArchiColumnLine.GetEndPoint(0), FamilySymbolArchiColumn, BaseLevelCloset, Autodesk.Revit.DB.Structure.StructuralType.Column);

                    if (ConvertElem is FamilyInstance column)
                    {
                        RevitUtils.GetLevelOffsetForColumn(column, ArchiColumnLine, BaseLevelCloset, TopLevelCloset, out double baseOffset, out double topOffset);

                        BaseOffset = baseOffset;
                        TopOffset = topOffset;

                        RevitUtils.SetParameterForColumn(column, TopLevelId, BaseOffset, TopOffset);
                    }
                }
                else
                    ConvertElem = _doc.Create.NewFamilyInstance(ArchiColumnLine, FamilySymbolArchiColumn, BaseLevelCloset, Autodesk.Revit.DB.Structure.StructuralType.Column);
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Get location line from IFC element
        /// </summary>
        private void GetLocationLineIFCElement()
        {
            IFCdata = new ElementIFC(_doc, LinkEleData.LinkElement, LinkInstance.GetLinkDocument(), ObjectIFCType.Column);

            if (IFCdata.Location != null)
            {
                XYZ startPoint = IFCdata.Location.GetEndPoint(0);
                XYZ endPoint = IFCdata.Location.GetEndPoint(1);

                if (RevitUtils.IsGreaterThan(startPoint.Z, endPoint.Z))
                {
                    XYZ between = startPoint;
                    startPoint = endPoint;
                    endPoint = between;
                }

                List<XYZ> locationPoints = new List<XYZ>() { startPoint, endPoint };
                ArchiColumnLine = IFCdata.GetLocationLineTranformOfObject(LinkInstance, locationPoints);

                if (ArchiColumnLine != null && ArchiColumnLine.Length >= RevitUtilities.Common.MIN_LENGTH)
                {
                    OriginPnt = startPoint;
                    BeforeTransformLine = Line.CreateBound(startPoint, endPoint);
                    if (IFCdata.Length != null)
                        Facing = IFCdata.GetTransformFacingOfObjectIFC(LinkInstance, IFCdata.Length.Direction);
                }
            }
        }

        #endregion Method
    }
}