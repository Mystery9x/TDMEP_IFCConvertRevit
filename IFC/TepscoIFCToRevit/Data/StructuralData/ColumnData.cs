using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using TepscoIFCToRevit.Common;

namespace TepscoIFCToRevit.Data.StructuralData
{
    public class ColumnData : ElementConvert
    {
        #region Property

        public bool ValidObject { get; set; } = true;

        public XYZ Facing { get; set; }

        public FamilySymbol FamilySymbolColumn
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

        public ElementId BaseLevelId { get; set; }
        public ElementId TopLevelId { get; set; }

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
        public Line ColumnLine { get; set; }
        public Line BeforeTransformLine { get; set; }

        #endregion Property

        #region Constructor

        public ColumnData(UIDocument uIDoc,
                          LinkElementData linkElementData,
                          ElementId familySymbolId,
                          RevitLinkInstance revLinkIns,
                          ConvertParamData paramNameIFC = null)
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
                ParameterData = paramNameIFC;

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
            if (ColumnLine != null && ColumnLine.Length > 0.0)
            {
                BaseLevelId = RevitUtils.GetLevelClosetTo(_doc, ColumnLine.GetEndPoint(0));
                TopLevelId = RevitUtils.GetLevelClosetTo(_doc, ColumnLine.GetEndPoint(1));
                BaseOffset = ColumnLine.GetEndPoint(0).Z - BaseLevelCloset.Elevation;
                TopOffset = TopLevelCloset.Elevation - ColumnLine.GetEndPoint(1).Z;
            }
        }

        /// <summary>
        /// Create Structural Column
        /// </summary>
        private void CreateStructuralColumn()
        {
            try
            {
                if (!FamilySymbolColumn.IsActive)
                {
                    FamilySymbolColumn.Activate();
                }
                if (ColumnLine != null && RevitUtils.IsParallel(XYZ.BasisZ, ColumnLine.Direction))
                {
                    ConvertElem = _doc.Create.NewFamilyInstance(ColumnLine.GetEndPoint(0), FamilySymbolColumn, BaseLevelCloset, Autodesk.Revit.DB.Structure.StructuralType.Column);
                    if (ConvertElem is FamilyInstance column)
                    {
                        RevitUtils.GetLevelOffsetForColumn(column, ColumnLine, BaseLevelCloset, TopLevelCloset, out double baseOffset, out double topOffset);

                        BaseOffset = baseOffset;
                        TopOffset = topOffset;

                        RevitUtils.SetParameterForColumn(column, TopLevelId, BaseOffset, TopOffset);
                    }
                }
                else
                {
                    if (ColumnLine != null && BaseLevelCloset != null)
                        ConvertElem = _doc.Create.NewFamilyInstance(ColumnLine, FamilySymbolColumn, BaseLevelCloset, Autodesk.Revit.DB.Structure.StructuralType.Column);
                }
            }
            catch (Exception)
            { }
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
                ColumnLine = IFCdata.GetLocationLineTranformOfObject(LinkInstance, locationPoints);

                if (ColumnLine != null && ColumnLine.Length >= RevitUtilities.Common.MIN_LENGTH)
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