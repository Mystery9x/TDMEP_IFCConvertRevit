using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using TepscoIFCToRevit.Common;

namespace TepscoIFCToRevit.Data.StructuralData
{
    public class CeilingData : ElementConvert
    {
        #region Property

        public bool ValidObject { get; set; } = true;
        public CurveArray CeilingProfile { get; set; } = new CurveArray();

        public FloorType ProcessFloorType
        {
            get
            {
                if (_doc != null && TypeId != null && _doc.GetElement(TypeId) is FloorType type)
                {
                    return type;
                }
                return null;
            }
        }

        public CeilingType ProcessCeilingType
        {
            get
            {
                if (_doc != null && TypeId != null && _doc.GetElement(TypeId) is CeilingType type)
                {
                    return type;
                }
                return null;
            }
        }

        public new ElementId LevelId;

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

        #endregion Property

        #region Constructor

        public CeilingData(UIDocument uIDoc,
                           LinkElementData linkElementData,
                           ElementId familySymbolId,
                           RevitLinkInstance revLinkIns,
                           ConvertParamData paramData = null)
        {
            if (uIDoc != null
               && linkElementData?.LinkElement?.IsValidObject != null
               && familySymbolId != null
               && familySymbolId != ElementId.InvalidElementId)
            {
                _doc = uIDoc.Document;
                LinkInstance = revLinkIns;
                LinkEleData = linkElementData;
                TypeId = familySymbolId;
                ParameterData = paramData;
                try
                {
                    GetGeometryFromIFCElement();
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
            CreateCeiling();
        }

        /// <summary>
        /// Get Geometry From IFC Element
        /// </summary>
        private void GetGeometryFromIFCElement()
        {
            List<Plane> planes = new List<Plane>();
            List<Solid> solidIFCs = new List<Solid>();

            // Get all solid from ifc element
            solidIFCs = UtilsSolid.GetAllSolids(LinkEleData.LinkElement);

            // Union solid
            Solid mainSolIFC = RevitUtils.UnionSolids(solidIFCs);

            // Get all planar face solid
            List<PlanarFace> planarFaces = UtilsPlane.GetPlanarFaceSolid(mainSolIFC);

            if (planarFaces?.Count <= 0)
            {
                return;
            }
            List<CurveLoop> bottomCurveLoop = new List<CurveLoop>();

            if (planarFaces?.Count == 1)
            {
                if (!RevitUtilities.Common.IsParallel(planarFaces[0].FaceNormal, XYZ.BasisZ))
                    return;

                bottomCurveLoop = planarFaces[0].GetEdgesAsCurveLoops().ToList();
                LevelId = RevitUtils.GetLevelClosetTo(_doc, LinkTransform.OfPoint(planarFaces[0].Origin));
            }
            else
            {
                // Filter parallel planar face
                List<List<PlanarFace>> groupPlanarFace = RevitUtils.GroupParallelPlanarFaces(planarFaces);

                // Get bottom planar face
                PlanarFace bottomPlanarface = planarFaces.Where(item => RevitUtilities.Common.IsParallel(item?.FaceNormal, XYZ.BasisZ)).OrderBy(item => item?.Origin.Z).LastOrDefault();
                if (bottomPlanarface == null)
                    return;

                // Get List PlanarFace contain bottomPlanarFace
                List<PlanarFace> validPlanarFaces = groupPlanarFace.Where(item => item.Contains(bottomPlanarface)).FirstOrDefault();
                if (validPlanarFaces == null || validPlanarFaces.Count <= 1)
                    return;

                validPlanarFaces = validPlanarFaces.OrderBy(item => item.Origin.Z).ToList();

                // Get currve loop from bottom planarface
                bottomCurveLoop = bottomPlanarface.GetEdgesAsCurveLoops().ToList();

                LevelId = RevitUtils.GetLevelClosetTo(_doc, LinkTransform.OfPoint(bottomPlanarface.Origin));
            }

            if (bottomCurveLoop == null || bottomCurveLoop.Count <= 0)
                return;

            var mainCurveLoop = bottomCurveLoop[0];
            mainCurveLoop.Transform(LinkTransform);
            mainCurveLoop.ToList().ForEach(item => CeilingProfile.Append(item));
        }

        private void CreateCeiling()
        {
            try
            {
#if DEBUG_2020 || RELEASE_2020
                ConvertElem = _doc.Create.NewFloor(CeilingProfile, ProcessFloorType, LevelCloset, true);

#else
                CurveLoop loop = new CurveLoop();
                foreach (Curve curve in CeilingProfile)
                {
                    loop.Append(curve);
                }

                IList<CurveLoop> loopCellings = new List<CurveLoop>() { loop };
                ConvertElem = Ceiling.Create(_doc, loopCellings, ProcessCeilingType.Id, LevelCloset.Id);
                if (ConvertElem?.IsValidObject == true)
                {
                    double height = loop.GetPlane().Origin.Z;
                    double offset = height - LevelCloset.ProjectElevation;
                    UtilsParameter.SetValueParameterBuiltIn(ConvertElem, BuiltInParameter.CEILING_HEIGHTABOVELEVEL_PARAM, offset);
                }
#endif
            }
            catch (Exception)
            { }
        }

        #endregion Method
    }
}