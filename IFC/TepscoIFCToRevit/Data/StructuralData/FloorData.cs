using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using TepscoIFCToRevit.Common;

namespace TepscoIFCToRevit.Data.StructuralData
{
    public class FloorData : ElementConvert
    {
        #region Property

        public bool ValidObject { get; set; } = true;
        public CurveArray FloorProfile { get; set; } = new CurveArray();

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

        public new ElementId LevelId { get; set; }

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

        public FloorData(UIDocument uIDoc, LinkElementData linkElementData, ElementId floorTypeId, RevitLinkInstance revLinkIns, ConvertParamData paramData = null)
        {
            if (uIDoc != null
                && linkElementData?.LinkElement?.IsValidObject != null
                && floorTypeId != null
                && floorTypeId != ElementId.InvalidElementId
                && revLinkIns != null)
            {
                _uiDoc = uIDoc;
                _doc = uIDoc.Document;
                LinkEleData = linkElementData;
                TypeId = floorTypeId;
                LinkInstance = revLinkIns;
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
            CreateFloor();
        }

        /// <summary>
        /// Get geometry from IFC element
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

            // Ignor slope celling
            if (planarFaces.Any(item => !RevitUtilities.Common.IsPerpendicular(item.FaceNormal, XYZ.BasisZ) && !RevitUtilities.Common.IsParallel(item.FaceNormal, XYZ.BasisZ)))
                return;

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
            List<CurveLoop> bottomCurveLoop = bottomPlanarface.GetEdgesAsCurveLoops().ToList();
            if (bottomCurveLoop == null || bottomCurveLoop.Count <= 0)
                return;

            var mainCurveLoop = bottomCurveLoop[0];
            mainCurveLoop.Transform(LinkTransform);
            mainCurveLoop.ToList().ForEach(item => FloorProfile.Append(item));
            LevelId = RevitUtils.GetLevelClosetTo(_doc, LinkTransform.OfPoint(bottomPlanarface.Origin));
        }

        /// <summary>
        /// Create floor
        /// </summary>
        private void CreateFloor()
        {
            try
            {
#if DEBUG_2020 || RELEASE_2020
                ConvertElem = _doc.Create.NewFloor(FloorProfile, ProcessFloorType, LevelCloset, true);
#else
                CurveLoop loop = new CurveLoop();
                foreach (Curve curve in FloorProfile)
                {
                    loop.Append(curve);
                }
                IList<CurveLoop> loopFloors = new List<CurveLoop>() { loop };
                ConvertElem = Floor.Create(_doc, loopFloors, ProcessFloorType.Id, LevelCloset?.Id);

                if (ConvertElem?.IsValidObject == true)
                {
                    double height = loop.GetPlane().Origin.Z;
                    double offset = height - LevelCloset.ProjectElevation;
                    UtilsParameter.SetValueParameterBuiltIn(ConvertElem, BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM, offset);
                }
#endif
            }
            catch (Exception)
            { }
        }

        #endregion Method
    }
}