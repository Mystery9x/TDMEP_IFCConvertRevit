using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using TepscoIFCToRevit.Common;

namespace TepscoIFCToRevit.Data.StructuralData
{
    public class WallData : ElementConvert
    {
        #region Property

        public bool ValidObject { get; set; } = true;

        public IList<Curve> WallProfile { get; set; } = new List<Curve>();

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

        public double BaseOffsetValue { get; set; }
        public double UnconnectedHeight { get; set; }

        #endregion Property

        #region Constructor

        public WallData(UIDocument uIDoc,
                        LinkElementData linkElementData,
                        ElementId wallTypeId,
                        RevitLinkInstance revLinkIns,
                        ConvertParamData paramNameInIFC = null)
        {
            if (uIDoc != null
                && linkElementData?.LinkElement?.IsValidObject != null
                && wallTypeId != null
                && wallTypeId != ElementId.InvalidElementId
                && revLinkIns != null)
            {
                _uiDoc = uIDoc;
                _doc = uIDoc.Document;
                LinkEleData = linkElementData;
                TypeId = wallTypeId;
                LinkInstance = revLinkIns;
                ParameterData = paramNameInIFC;
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
            CreateWall();
            SetBaseOffsetToNewWall();
            SetUnconnectedHeightToNewWall();
        }

        /// <summary>
        /// Get geometry from IFC element
        /// </summary>
        private void GetGeometryFromIFCElement()
        {
            List<Plane> planes = new List<Plane>();
            List<Solid> solidIFCs = new List<Solid>();
            List<Line> lineIFCs = new List<Line>();

            // Get all solid from ifc element
            solidIFCs = UtilsSolid.GetAllSolids(LinkEleData.LinkElement);

            // Get all line from ifc element
            lineIFCs = RevitUtils.GetAllLines(LinkEleData.LinkElement);

            // Union solid
            Solid mainSolIFC = RevitUtils.UnionSolids(solidIFCs);

            // Get all planar face solid
            List<PlanarFace> planarFaces = UtilsPlane.GetPlanarFaceSolid(mainSolIFC);

            // Get bottom planar face
            PlanarFace bottomPlanarface = planarFaces.Where(item => RevitUtilities.Common.IsParallel(item?.FaceNormal, XYZ.BasisZ)).OrderBy(item => item?.Origin.Z).LastOrDefault();
            if (bottomPlanarface == null)
                return;

            // Get top planar face
            PlanarFace topPlanarface = planarFaces.Where(item => RevitUtilities.Common.IsParallel(item?.FaceNormal, XYZ.BasisZ)).OrderBy(item => item?.Origin.Z).FirstOrDefault();
            if (topPlanarface == null)
                return;

            // Get valid unconnected height

            XYZ centerPnt_bottom = FindCenterOfPlanarFace(bottomPlanarface);
            Plane topPlane = Plane.CreateByNormalAndOrigin(topPlanarface.FaceNormal, topPlanarface.Origin);
            topPlane.Project(centerPnt_bottom, out UV topPntUV, out double distance);

            UnconnectedHeight = distance;
            if (UnconnectedHeight <= 0)
                return;

            // Get wall profile
            GetProfileOfWall(lineIFCs);
        }

        /// <summary>
        /// Get location line of wall
        /// </summary>
        /// <param name="lineIFCs"></param>
        /// <param name="solidIFCs"></param>
        private void GetProfileOfWall(List<Line> lineIFCs)
        {
            if (lineIFCs?.Count > 0)
            {
                List<XYZ> points = new List<XYZ>();
                foreach (Line line in lineIFCs)
                {
                    Line lineTransform = Line.CreateBound(LinkTransform.OfPoint(line.GetEndPoint(0)), LinkTransform.OfPoint(line.GetEndPoint(1)));
                    points.Add(lineTransform.GetEndPoint(0));
                    points.Add(lineTransform.GetEndPoint(1));
                    WallProfile.Add(lineTransform);
                }

                // Get level closet
                XYZ centerPnt = new XYZ(points.Sum(item => item.X) / points.Count, points.Sum(item => item.Y) / points.Count, points.Sum(item => item.Z) / points.Count);
                LevelId = RevitUtils.GetLevelClosetTo(_doc, centerPnt, true);
                BaseOffsetValue = centerPnt.Z - LevelCloset.Elevation;
            }
            else
            {
                ElementIFC wallIFC = new ElementIFC(_doc, LinkEleData.LinkElement, LinkInstance.GetLinkDocument(), ObjectIFCType.Wall);
                List<XYZ> pointOnElement = wallIFC.GeomertyData.Vertices;

                if (pointOnElement?.Count > 0)
                {
                    // get max Z
                    double minZ = pointOnElement.Min(x => x.Z);

                    // get boundray point
                    pointOnElement = pointOnElement.Select(x => new XYZ(x.X, x.Y, minZ)).ToList();
                    List<XYZ> points = ConvexHull.GetConvexHull(pointOnElement);
                    points = RevitUtils.MergePoints(points);

                    List<XYZ> pointLocations = new List<XYZ>() { wallIFC.Location.GetEndPoint(0), wallIFC.Location.GetEndPoint(1) };
                    Line locationLine = wallIFC.GetLocationLineTranformOfObject(LinkInstance, pointLocations);
                    WallProfile.Add(locationLine);

                    // Get level closet
                    XYZ centerPnt = new XYZ(points.Sum(item => item.X) / points.Count, points.Sum(item => item.Y) / points.Count, points.Sum(item => item.Z) / points.Count);
                    LevelId = RevitUtils.GetLevelClosetTo(_doc, centerPnt, true);
                    BaseOffsetValue = centerPnt.Z - LevelCloset.Elevation;
                }
            }
        }

        /// <summary>
        /// Set base offset to new wall
        /// </summary>
        private void SetBaseOffsetToNewWall()
        {
            try
            {
                if (BaseOffsetValue != double.MinValue)
                    UtilsParameter.SetValueParameterBuiltIn(ConvertElem, BuiltInParameter.WALL_BASE_OFFSET, BaseOffsetValue);
            }
            catch (Exception)
            { }
        }

        /// <summary>
        /// Set unconnected height to new wall
        /// </summary>
        private void SetUnconnectedHeightToNewWall()
        {
            try
            {
                if (BaseOffsetValue != double.MinValue)
                    UtilsParameter.SetValueParameterBuiltIn(ConvertElem, BuiltInParameter.WALL_USER_HEIGHT_PARAM, UnconnectedHeight);
            }
            catch (Exception)
            { }
        }

        /// <summary>
        /// Create wall
        /// </summary>
        private void CreateWall()
        {
            try
            {
                if (WallProfile?.Count > 0)
                {
                    ConvertElem = Wall.Create(_doc, WallProfile.First(), TypeId, LevelId, UnconnectedHeight, BaseOffsetValue, false, true);
                }
            }
            catch (Exception)
            { }
        }

        /// <summary>
        /// Find center of planar face
        /// </summary>
        /// <param name="face"></param>
        /// <returns></returns>
        private XYZ FindCenterOfPlanarFace(Face face)
        {
            // Lấy các đỉnh của planar face
            List<XYZ> vertices = face.Triangulate().Vertices.ToList();

            RevitUtils.FindMaxMinPoint(vertices, out XYZ min, out XYZ max);
            return (min + max) / 2;
        }

        #endregion Method
    }
}