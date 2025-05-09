using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using TepscoIFCToRevit.Common;
using TepscoIFCToRevit.Data.GeometryDatas;

namespace TepscoIFCToRevit.Data.EquipmentData.Electrical
{
    public class ConduitTerminalBoxData
    {
        public UIDocument _uiDocument = null;
        private Document _doc = null;

        #region Property

        public LinkElementData LinkEleData { get; set; }
        public RevitLinkInstance LinkInstance { get; set; }
        public FamilyInstance ConvertElem { get; set; }
        public XYZ Vector { get; set; }
        public XYZ CenterPoint { get; set; }
        public bool ValidObject { get; set; } = true;
        public FamilySymbol ConduitTerminalType { get; set; }
        public Transform RevLinkTransform => LinkInstance?.GetTotalTransform();
        public ConvertParamData ParameterData { get; set; }

        #endregion Property

        #region Constructor

        public ConduitTerminalBoxData(UIDocument uIDocument,
                                    LinkElementData linkEleData,
                                    ElementId conduitTerminalFamilyId,
                                    RevitLinkInstance revLnkIns,
                                    ConvertParamData parameterData = null)
        {
            try
            {
                if (uIDocument != null
                && linkEleData != null
                && linkEleData.LinkElement != null
                && linkEleData.LinkElement.IsValidObject
                && revLnkIns != null)
                {
                    _uiDocument = uIDocument;
                    _doc = _uiDocument.Document;
                    LinkInstance = revLnkIns;
                    LinkEleData = linkEleData;
                    ConduitTerminalType = uIDocument.Document.GetElement(conduitTerminalFamilyId) as FamilySymbol;
                    ParameterData = parameterData;
                }
            }
            catch (Exception)
            {
                ValidObject = false;
            }
        }

        #endregion Constructor

        #region Method

        public FamilyInstance CreateConduitTerminalBoxWithNewFamily()
        {
            List<Solid> solidIFCs = UtilsSolid.GetAllSolids(LinkEleData.LinkElement);
            if (solidIFCs.Count <= 0)
                return null;

            bool isMergeSolid = CheckMergeSolid(solidIFCs);

            // Union solid
            Solid mainSol = solidIFCs[0];
            if (isMergeSolid)
                mainSol = UtilsSolid.MergeSolids(solidIFCs);

            ConduitTerminalPlacingData data = GetSectionDimensionInforRectange(mainSol);

            List<PlanarFace> faces = UtilsPlane.GetPlanarFaceSolid(mainSol);

            if (data.IsValid() && faces.Count == 6)
            {
                ConvertElem = CreateInstanceByDirectShape(data.FacePerpendicularWithLocation.Origin, data.FacePerpendicularWithLocation.FaceNormal, data.MainLine, data.MainLine.Direction);
                _doc.Regenerate();

                if (ConvertElem != null && ConvertElem.IsValidObject)
                {
                    SetParameterDimensionInstance(ConvertElem, data.Width, data.Height, data.Length);
                    RotateElementBox(data, ConvertElem);
                }

                Solid curSolid = UtilsSolid.GetTotalSolid(ConvertElem);

                if (curSolid != null)
                {
                    XYZ curLoc = curSolid.ComputeCentroid();
                    if (!IsPointInsideSolidRectangleBox(mainSol, curLoc))
                        ConvertElem.IsWorkPlaneFlipped = !ConvertElem.IsWorkPlaneFlipped;
                    _doc.Regenerate();

                    curSolid = UtilsSolid.GetTotalSolid(ConvertElem);
                    XYZ centroid = RevLinkTransform.OfPoint(mainSol.ComputeCentroid());
                    MoveInstanceAfterCreate(curSolid, centroid, ConvertElem);
                }
            }

            if (ConvertElem != null)
                return ConvertElem;
            else
                return null;
        }

        /// <summary>
        /// Rotate element
        /// </summary>
        /// <param name="familyInstance"></param>
        private void RotateElement(FamilyInstance familyInstance, bool isBoxDoor)
        {
            if (isBoxDoor)
            {
                if (familyInstance != null)
                {
                    List<Solid> solids = UtilsSolid.GetAllSolids(familyInstance);
                    Solid mainSol = UtilsSolid.MergeSolids(solids);
                    XYZ centerNew = mainSol.ComputeCentroid();

                    List<PlanarFace> lstPlanarFace = new List<PlanarFace>();
                    foreach (var item in mainSol.Faces)
                    {
                        if (item is PlanarFace pFace)
                            lstPlanarFace.Add(pFace);
                    }

                    PlanarFace pFaceSelects = lstPlanarFace.OrderByDescending(x => x.Area).FirstOrDefault();
                    XYZ pointPrj = pFaceSelects.Project(centerNew).XYZPoint;
                    XYZ newvector = (pointPrj - centerNew).Normalize();

                    double angle = Vector.AngleOnPlaneTo(newvector, XYZ.BasisZ);

                    double angleTransform = RevLinkTransform.BasisX.AngleTo(XYZ.BasisX);

                    XYZ CenterPointTransForm = RevLinkTransform.OfPoint(CenterPoint);

                    Line lineLoc = Line.CreateUnbound(centerNew, XYZ.BasisZ);
                    ElementTransformUtils.RotateElement(_doc, familyInstance.Id, lineLoc, Math.PI * 2 - angle + angleTransform);
                    ElementTransformUtils.MoveElement(_doc, familyInstance.Id, CenterPointTransForm - centerNew);
                }
            }
            else
            {
                List<Solid> solids = UtilsSolid.GetAllSolids(familyInstance);
                Solid mainSol = UtilsSolid.MergeSolids(solids);
                var centerNew = mainSol.ComputeCentroid();

                List<PlanarFace> lstPlanarFace = new List<PlanarFace>();
                foreach (var item in mainSol.Faces)
                {
                    if (item is PlanarFace pFace)
                        lstPlanarFace.Add(pFace);
                }

                List<GeometryObject> geometries = GeometryUtils.GetIfcGeometriess(familyInstance);
                GeometryData geomertyData = new GeometryData(geometries);

                Line linePrj1 = null;
                var ptConvex1 = CalculateOrientedBoundingBox(_doc, geomertyData.Vertices, ref linePrj1);

                XYZ pointPrj = linePrj1.Project(centerNew).XYZPoint;
                XYZ newvector = (pointPrj - centerNew).Normalize();

                var angle = Vector.AngleOnPlaneTo(newvector, XYZ.BasisZ);

                Line lineLoc = Line.CreateUnbound(centerNew, XYZ.BasisZ);
                ElementTransformUtils.RotateElement(_doc, familyInstance.Id, lineLoc, Math.PI * 2 - angle);
                ElementTransformUtils.MoveElement(_doc, familyInstance.Id, CenterPoint - centerNew);
            }
        }

        protected void SetParameterDimensionInstance(Element element, double? width, double? height, double? length)
        {
            if (element != null)
            {
                if (width != null && width > 0)
                    UtilsParameter.SetValueAllParameterName(element, "長さ2", width);

                if (ConduitTerminalType.Name.Contains("プルボックス") ||
                    ConduitTerminalType.Name.Contains("盤") ||
                    ConduitTerminalType.Name.Contains("盤_機械設備")
                    && height != null
                    && height > 0)
                    UtilsParameter.SetValueAllParameterName(element, "長さ3", height);

                if (length != null && length > 0)
                    UtilsParameter.SetValueAllParameterName(element, "長さ1", length);
                _doc.Regenerate();
            }
        }

        protected void RotateElementBox(ConduitTerminalPlacingData data, Element element)
        {
            Line orgLine = data.MainLine;

            XYZ orgDir = RevLinkTransform.OfVector(data.Direction);

            Solid mainSol = UtilsSolid.GetTotalSolid(element);
            if (mainSol == null)
                return;

            List<Line> edges = GetLinesFromSolid(mainSol);
            // because edges use to get center line of element so lines paralel with normal referent plane
            // can't rotate by referent plane
            if (!RevitUtils.IsParallel(data.FacePerpendicularWithLocation.FaceNormal, data.Direction))
                edges = edges.Where(x => RevitUtils.IsPerpendicular(data.FacePerpendicularWithLocation.FaceNormal, x.Direction, 0.1)).ToList();

            List<PlanarFace> planarFaces = UtilsPlane.GetPlanarFaceSolid(mainSol);

            Line maxLine = edges.Max(x => x.Length, (double x0, double x1) => x0 < x1);
            Line curLine = GetCenterLine(planarFaces, maxLine.Direction);

            if (curLine != null)
            {
                XYZ dir1 = curLine.Direction.CrossProduct(orgDir).Normalize();
                if (!dir1.IsZeroLength())
                {
                    XYZ curLoc = mainSol.ComputeCentroid();
                    Line axis1 = Line.CreateUnbound(curLoc, dir1);
                    double angle1 = curLine.Direction.AngleTo(orgDir);
                    if (angle1 > 0.02
                        && !RevitUtils.IsEqual(Math.PI, angle1))
                    {
                        ElementTransformUtils.RotateElement(_doc, element.Id, axis1, angle1);
                    }
                }
            }
            _doc.Regenerate();
        }

        protected void MoveInstanceAfterCreate(Solid curSolid, XYZ oldLoc, Element element)
        {
            XYZ curLoc = curSolid.ComputeCentroid();
            ElementTransformUtils.MoveElement(_doc, element.Id, oldLoc - curLoc);
            _doc.Regenerate();
        }

        protected bool IsPointInsideSolidRectangleBox(Solid solid, XYZ curPoint)
        {
            if (solid == null || curPoint == null)
                return false;
            XYZ point = RevLinkTransform.Inverse.OfPoint(curPoint);

            XYZ centroid = solid.ComputeCentroid();
            if (centroid.IsAlmostEqualTo(point, 1e-5))
                return true;

            try
            {
                Line line = Line.CreateBound(centroid, point);

                foreach (Face face in solid.Faces)
                {
                    if (face.Intersect(line, out IntersectionResultArray results) == SetComparisonResult.Overlap &&
                          results != null && results.Size > 0)
                    {
                        return false;
                    }
                }
            }
            catch (Exception) { }
            return true;
        }

        protected FamilyInstance CreateInstanceByDirectShape(XYZ faceOrigin, XYZ faceNormal, Line mainLine, XYZ directionInstance)
        {
            if (directionInstance != null)
            {
                faceNormal = RevLinkTransform.OfVector(faceNormal);
                XYZ point = RevLinkTransform.OfPoint(faceOrigin);

                // create a direct shape to host the hosting face for
                // the piping support since face can not be created alone

                Solid mainSol = CreateSolidByFaceWithNormalAndOrigin(faceNormal, point);
                DirectShape directShape = DirectShape.CreateElement(_doc, Category.GetCategory(_doc, BuiltInCategory.OST_GenericModel).Id);
                directShape.SetShape(new List<GeometryObject>() { mainSol });
                _doc.Regenerate();

                // find the valid hosting face from direct shape solid
                Options options = new Options
                {
                    ComputeReferences = true
                };
                List<Solid> solids = UtilsSolid.GetAllSolids(directShape, options);
                PlanarFace planarPut = solids.SelectMany(x => x.Faces.Cast<PlanarFace>())
                                             .Where(x => x != null)
                                             .FirstOrDefault(x => IsPlanarFaceDupplicate(x, point, faceNormal));

                // create the piping support instance
                XYZ centroid = mainSol.ComputeCentroid();
                XYZ location = RevitUtils.ProjectOntoFaceByOriginAndNormal(point, faceNormal, centroid);

                if (location != null)
                {
                    FamilyInstance instance = _doc.Create.NewFamilyInstance(planarPut, location, directionInstance, ConduitTerminalType);
                    // remove the temporary direct shape
                    _doc.Delete(directShape.Id);

                    return instance;
                }
            }

            return null;
        }

        protected bool IsPlanarFaceDupplicate(PlanarFace face, XYZ origin, XYZ normal)
        {
            if (face != null
                && origin != null
                && normal != null
                && RevitUtils.IsParallel(face.FaceNormal, normal))
            {
                Plane plane = Plane.CreateByNormalAndOrigin(normal, origin);
                double distance = Math.Abs(UtilsPlane.GetSignedDistance(plane, face.Origin));
                return RevitUtilities.Common.IsEqual(distance, 0);
            }
            return false;
        }

        protected Solid CreateSolidByFaceWithNormalAndOrigin(XYZ normal, XYZ point)
        {
            try
            {
                Plane plane = Plane.CreateByNormalAndOrigin(normal, XYZ.Zero);
                XYZ project = UtilsPlane.ProjectOnto(plane, new XYZ(1, 1, 1));
                XYZ vector1 = project.Normalize();
                XYZ vector2 = vector1.CrossProduct(normal).Normalize();
                XYZ p0 = point + vector1 + vector2;
                XYZ p1 = point + vector1 - vector2;
                XYZ p2 = point - vector1 - vector2;
                XYZ p3 = point - vector1 + vector2;

                Line l0 = Line.CreateBound(p0, p1);
                Line l1 = Line.CreateBound(p1, p2);
                Line l2 = Line.CreateBound(p2, p3);
                Line l3 = Line.CreateBound(p3, p0);

                CurveLoop loop = CurveLoop.Create(new List<Curve>() { l0, l1, l2, l3 });
                return GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop>() { loop }, normal.Negate(), 10);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static bool CheckMergeSolid(List<Solid> solidIFCs)
        {
            bool isMergeSolid = true;
            if (solidIFCs?.Count == 2
                && RevitUtils.IsEqual(solidIFCs[0].Volume, solidIFCs[1].Volume))
            {
                List<XYZ> normalFacesSolid1 = UtilsPlane.GetPlanarFaceSolid(solidIFCs[0]).Select(x => x.FaceNormal).ToList();

                List<XYZ> nomalFacesSolid2 = UtilsPlane.GetPlanarFaceSolid(solidIFCs[1]).Select(x => x.FaceNormal).ToList();
                foreach (var normalSolid1 in normalFacesSolid1)
                {
                    if (!isMergeSolid)
                        break;

                    foreach (var nomalSolid2 in nomalFacesSolid2)
                    {
                        if (!RevitUtils.IsEqual(normalSolid1, nomalSolid2))
                        {
                            isMergeSolid = false;
                            break;
                        }
                    }
                }
            }
            return isMergeSolid;
        }

        public List<XYZ> CalculateOrientedBoundingBox(Document doc, List<XYZ> rotatedPoints, ref Line lineProject)
        {
            List<XYZ> newPts = new List<XYZ>();
            foreach (var item in rotatedPoints)
            {
                newPts.Add(new XYZ(item.X, item.Y, 0));
            }

            List<XYZ> ptConvex = ConvexHull.GetConvexHull(newPts);

            List<Line> lstLine = new List<Line>();
            for (int i = 0; i < ptConvex.Count; i++)
            {
                XYZ nextPt = i == ptConvex.Count - 1 ? ptConvex[0] : ptConvex[i + 1];
                Line line = Line.CreateBound(ptConvex[i], nextPt);
                lstLine.Add(line);
            }

            lineProject = lstLine.OrderByDescending(x => GetLineCenter(x).Y).FirstOrDefault();

            return ptConvex;
        }

        private XYZ GetLineCenter(Line line)
        {
            return (line.GetEndPoint(0) + line.GetEndPoint(1)) / 2;
        }

        protected ConduitTerminalPlacingData GetSectionDimensionInforRectange(Solid solid)
        {
            List<Line> edges = GetLinesFromSolid(solid).Where(x => x.Length > RevitUtils.MIN_LENGTH).ToList();
            List<PlanarFace> planarFaces = UtilsPlane.GetPlanarFaceSolid(solid);
            ConduitTerminalPlacingData data = new ConduitTerminalPlacingData();

            Line maxLine = edges.Max(x => x.Length, (double x0, double x1) => x0 < x1);
            Line line = GetCenterLine(planarFaces, maxLine.Direction);
            if (line != null)
            {
                data.MainLine = line;
                data.Direction = maxLine.Direction;

                if (RevitUtils.IsParallel(line.Direction, XYZ.BasisZ))
                    data.FacePerpendicularWithLocation = GetSideFace(planarFaces, line);
                else
                    data.FacePerpendicularWithLocation = GetBottomFace(planarFaces, line);

                data.FacePlace = planarFaces.FirstOrDefault(x => RevitUtils.IsParallel(x.FaceNormal, data.MainLine.Direction, 0.05));

                if (data.FacePerpendicularWithLocation != null)
                {
                    XYZ up = data.FacePerpendicularWithLocation.FaceNormal;
                    XYZ hoz = data.Direction.CrossProduct(up);

                    GetHeightWithSolid(edges, up, hoz, out Line lwith, out Line lheight);
                    data.LHeight = lheight;
                    data.LWith = lwith;
                }
            }

            data.Length = data.MainLine != null ? data.MainLine.Length : 0;
            data.Width = data.LWith != null ? data.LWith.Length : 0;
            data.Height = data.LHeight != null ? data.LHeight.Length : 0;
            return data;
        }

        private PlanarFace GetBottomFace(List<PlanarFace> faces, Line centerLine)
        {
            var topBotFaces = faces.Where(x => !RevitUtils.IsParallel(x.FaceNormal, centerLine.Direction))      // exclude start, end faces
                                    .Where(x => !RevitUtils.IsPerpendicular(x.FaceNormal, XYZ.BasisZ))          // exclude left, right faces
                                    .ToList();

            XYZ normal = topBotFaces.Select(x => x.FaceNormal)
                                    .OrderBy(x => x.AngleTo(XYZ.BasisZ))
                                    .First();

            return topBotFaces.OrderBy(x => x.Origin, normal).First();
        }

        private PlanarFace GetSideFace(List<PlanarFace> faces, Line centerLine)
        {
            return faces.FirstOrDefault(x => !RevitUtils.IsParallel(x.FaceNormal, centerLine.Direction));     // exclude start, end faces
        }

        private void GetHeightWithSolid(List<Line> edges, XYZ upVector, XYZ hozVector, out Line with, out Line height)
        {
            with = edges.Where(x => RevitUtils.IsParallel(x.Direction, hozVector))/*.OrderBy(x => x.Origin, line.Direction)*/
                               .Max(x => x.Length, (double x0, double x1) => x0 < x1);

            height = edges.Where(x => RevitUtils.IsParallel(x.Direction, upVector)).Max(x => x.Length, (double x0, double x1) => x0 < x1);
        }

        private List<Line> GetLinesFromSolid(Solid solid)
        {
            if (solid != null)
            {
                return solid.Edges
                        .Cast<Edge>()
                        .Select(x => x.AsCurve())
                        .Cast<Line>()
                        .Where(x => x != null)
                        .ToList();
            }
            return new List<Line>();
        }

        protected Line GetCenterLine(List<PlanarFace> solidFaces, XYZ direction)
        {
            if (direction != null)
            {
                List<PlanarFace> planarFaces = solidFaces.Where(x => RevitUtils.IsParallel(x.FaceNormal, direction))
                                                   .OrderBy(x => x.Origin, direction)
                                                   .ToList();

                if (planarFaces?.Count >= 2)
                {
                    XYZ start = CenterOfFace(planarFaces.First());
                    XYZ end = CenterOfFace(planarFaces.Last());
                    return Line.CreateBound(start, end);
                }
            }

            return null;
        }

        protected XYZ CenterOfFace(PlanarFace face)
        {
            if (face != null)
            {
                return face.GetEdgesAsCurveLoops()
                           .SelectMany(x => x)
                           .Select(x => x.GetEndPoint(0))
                           .Average();
            }
            return null;
        }

        public bool CreateFamilyInstanceElectricWithTwoDoor(Document doc, ConduitTerminalBoxData cdtTmnBoxData)
        {
            if (cdtTmnBoxData == null)
                return false;

            Document docRevLink = cdtTmnBoxData.LinkInstance.GetLinkDocument();
            Element linkElem = docRevLink.GetElement(cdtTmnBoxData.LinkEleData.LinkElement.Id);

            List<Solid> solidsLinkElement = UtilsSolid.GetAllSolids(linkElem);

            List<Solid> solidsParallel = FindSolidsParallel(solidsLinkElement, 10e-4);

            Solid mainSolid = UtilsSolid.MergeSolids(solidsLinkElement);
            Solid handSol = FindSolidsWithSimilarVolume(solidsLinkElement, 10e-4).FirstOrDefault();
            XYZ centerHand = handSol.ComputeCentroid();

            XYZ centerElemLink = LinkInstance.GetTransform().OfPoint(mainSolid.ComputeCentroid());

            List<PlanarFace> lstPlanarFaceHand = new List<PlanarFace>();

            foreach (var face in handSol.Faces)
            {
                if (face is PlanarFace pFace && !RevitUtils.IsParallel(pFace.FaceNormal, XYZ.BasisZ))
                    lstPlanarFaceHand.Add(pFace);
            }

            PlanarFace planarFacesHandArea = lstPlanarFaceHand.OrderByDescending(x => x.Area).FirstOrDefault();
            Plane plane = Plane.CreateByNormalAndOrigin(planarFacesHandArea.FaceNormal, planarFacesHandArea.Origin);
            XYZ ptProjectHand = UtilsPlane.ProjectOnto(plane, centerHand);

            XYZ vector = (ptProjectHand - centerHand).Normalize();

            //Calculate width, height, depth
            List<PlanarFace> lstPlanarFace = new List<PlanarFace>();
            foreach (var face in mainSolid.Faces)
            {
                if (face is PlanarFace pFace)
                    lstPlanarFace.Add(pFace);
            }

            BoundingBoxXYZ boxIFC = linkElem.get_BoundingBox(null);
            XYZ centerBox = (boxIFC.Max + boxIFC.Min) / 2;

            PlanarFace pFaceSelect = lstPlanarFace.Where(x => RevitUtils.IsParallel(x.FaceNormal, vector))
                                                  .OrderByDescending(x => GetDisOnFace(x, centerBox))
                                                  .FirstOrDefault();

            PlanarFace pFaceSelectTop = lstPlanarFace.Where(x => RevitUtils.IsParallel(x.FaceNormal, XYZ.BasisZ))
                                                     .OrderByDescending(x => GetDisOnFace(x, centerBox))
                                                     .FirstOrDefault();

            List<PlanarFace> pFaceSelectLeftRights = lstPlanarFace.Where(x => RevitUtils.IsPerpendicular(x.FaceNormal, pFaceSelect.FaceNormal) &&
                                                                              !RevitUtils.IsParallel(x.FaceNormal, XYZ.BasisZ))
                                                                              .ToList();

            PlanarFace pFaceSelectLeftRight = pFaceSelectLeftRights.OrderByDescending(x => GetDisOnFace(x, centerBox))
                                                                   .FirstOrDefault();

            // Depth ,height, width conduit terminal box
            double dDepthBox = pFaceSelect.Project(centerBox).Distance * 2;
            double dHeightBox = pFaceSelectTop.Project(centerBox).Distance * 2;
            double dWidthBox = pFaceSelectLeftRight.Project(centerBox).Distance * 2;

            // Get family symbol to conduit terminal box
            FamilySymbol familySymbol = cdtTmnBoxData.ConduitTerminalType;

            if (!familySymbol.IsActive)
            {
                familySymbol.Activate();
                App._UIDoc.Document.Regenerate();
            }

            // Create conduit terminal box
            ConvertElem = App._UIDoc.Document.Create.NewFamilyInstance(new XYZ(0, 0, 0), familySymbol, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
            if (ConvertElem?.IsValidObject != true)
                return false;

            // Set parameter to conduit terminal box
            UtilsParameter.SetValueAllParameterName(ConvertElem, "Depth", dDepthBox);
            UtilsParameter.SetValueAllParameterName(ConvertElem, "Height", dHeightBox);
            UtilsParameter.SetValueAllParameterName(ConvertElem, "Width", dWidthBox);

            doc.Regenerate();

            if (ConvertElem != null)
            {
                //Get Solid of conduit terminal box
                List<Solid> lstSolidsBox = UtilsSolid.GetAllSolids(ConvertElem);
                Solid mainSolBox = UtilsSolid.MergeSolids(lstSolidsBox);

                XYZ CenterMove = mainSolBox.ComputeCentroid();

                //Move conduit terminal box
                ElementTransformUtils.MoveElement(App._UIDoc.Document, ConvertElem.Id, centerElemLink - CenterMove);
                doc.Regenerate();

                //Rotate conduit terminal box
                // Get geometry family instance
                List<Solid> solidsConduit = UtilsSolid.GetAllSolids(ConvertElem);
                Solid mainSolConduit = UtilsSolid.MergeSolids(solidsConduit);
                Solid handSolDoor = FindSolidsWithSimilarVolume(solidsConduit, 10e-4).FirstOrDefault();
                XYZ centerHandDoor = handSolDoor.ComputeCentroid();
                XYZ centerNew = mainSolConduit.ComputeCentroid();
                XYZ centerNewTransForm = RevLinkTransform.OfPoint(centerElemLink);

                //Rotate terminal box
                Line lineAxis = Line.CreateUnbound(centerElemLink, XYZ.BasisZ);

                XYZ facingLink = lstPlanarFaceHand.FirstOrDefault().FaceNormal.Normalize();
                facingLink = RevLinkTransform.OfVector(facingLink);

                double angleRotate = ConvertElem.HandOrientation.AngleOnPlaneTo(facingLink, XYZ.BasisZ);
                ElementTransformUtils.RotateElement(App._UIDoc.Document, ConvertElem.Id, lineAxis, angleRotate);

                return true;
            }
            return false;
        }

        private List<Solid> FindSolidsParallel(List<Solid> solids, double tolerance)
        {
            List<Solid> parallelSolids = new List<Solid>();

            if (solids == null || solids.Count < 2)
                return null;

            for (int i = 0; i < solids.Count; i++)
            {
                Solid currentSolid = solids[i];
                bool isParallel;

                for (int j = i + 1; j < solids.Count; j++)
                {
                    Solid otherSolid = solids[j];
                    isParallel = true;

                    foreach (Face currentFace in currentSolid.Faces)
                    {
                        XYZ currentNormal = currentFace.ComputeNormal(new UV(0, 0));

                        foreach (Face otherFace in otherSolid.Faces)
                        {
                            XYZ otherNormal = otherFace.ComputeNormal(new UV(0, 0));

                            if (!AreNormalsParallel(currentNormal, otherNormal, tolerance))
                            {
                                isParallel = false;
                                break;
                            }
                        }

                        if (!isParallel)
                        {
                            break;
                        }
                    }

                    if (isParallel)
                    {
                        parallelSolids.Add(otherSolid);
                    }
                }
            }

            return parallelSolids;
        }

        private bool AreNormalsParallel(XYZ normal1, XYZ normal2, double tolerance)
        {
            double dotProduct = normal1.DotProduct(normal2);

            // So sánh tích vô hướng với 1 hoặc -1 với phạm vi dung sai
            return Math.Abs(Math.Abs(dotProduct) - 1.0) < tolerance;
        }

        public bool DealingWithCaseOfAnElectricalCabinetWithTwoDoors(ConduitTerminalBoxData cdtTmnBoxData, Transaction reTrans, FailureHandlingOptions fhOpts)
        {
            try
            {
                if (cdtTmnBoxData == null)
                    return false;

                if (!reTrans.HasStarted())
                    return false;

                RevitLinkInstance revLnkIns = cdtTmnBoxData.LinkInstance;
                Document docLinked = revLnkIns.GetLinkDocument();
                Element linkedelement = docLinked.GetElement(cdtTmnBoxData.LinkEleData.LinkElement.Id);
                LinkElementData linkElementData = new LinkElementData(linkedelement);

                // Handling get depth width height
                List<Solid> solids = UtilsSolid.GetAllSolids(linkedelement);
                Solid mainSol = UtilsSolid.MergeSolids(solids);
                Solid handSol = FindSolidsWithSimilarVolume(solids, 10e-4).FirstOrDefault();
                XYZ centerHand = handSol.ComputeCentroid();

                Solid mainSolIFC = UtilsSolid.MergeSolids(solids);

                XYZ centerTrans = revLnkIns.GetTransform().OfPoint(mainSol.ComputeCentroid());
                XYZ center = centerTrans;
                List<PlanarFace> lstPlanarFaceHand = new List<PlanarFace>();

                foreach (var item in handSol.Faces)
                {
                    if (item is PlanarFace pFace && !RevitUtils.IsParallel(pFace.FaceNormal, XYZ.BasisZ))
                        lstPlanarFaceHand.Add(pFace);
                }

                PlanarFace planarFacesHandArea = lstPlanarFaceHand.OrderByDescending(x => x.Area).FirstOrDefault();
                Plane plane = Plane.CreateByNormalAndOrigin(planarFacesHandArea.FaceNormal, planarFacesHandArea.Origin);
                XYZ ptProjectHand = UtilsPlane.ProjectOnto(plane, center);

                XYZ vector = (ptProjectHand - center).Normalize();

                //Calculate width, height, depth
                List<PlanarFace> lstPlanarFace = new List<PlanarFace>();
                foreach (var face in mainSol.Faces)
                {
                    if (face is PlanarFace pFace)
                        lstPlanarFace.Add(pFace);
                }

                BoundingBoxXYZ boxIFC = linkedelement.get_BoundingBox(null);
                XYZ centerBox = (boxIFC.Max + boxIFC.Min) / 2;

                PlanarFace pFaceSelect = lstPlanarFace.Where(x => RevitUtils.IsParallel(x.FaceNormal, vector))
                                                      .OrderByDescending(x => GetDisOnFace(x, centerBox))
                                                      .FirstOrDefault();

                PlanarFace pFaceSelectTop = lstPlanarFace.Where(x => RevitUtils.IsParallel(x.FaceNormal, XYZ.BasisZ))
                                                         .OrderByDescending(x => GetDisOnFace(x, centerBox))
                                                         .FirstOrDefault();

                List<PlanarFace> pFaceSelectLeftRights = lstPlanarFace.Where(x => RevitUtils.IsPerpendicular(x.FaceNormal, pFaceSelect.FaceNormal) &&
                                                                                  !RevitUtils.IsParallel(x.FaceNormal, XYZ.BasisZ))
                                                                                  .ToList();

                PlanarFace pFaceSelectLeftRight = pFaceSelectLeftRights.OrderByDescending(x => GetDisOnFace(x, centerBox))
                                                                       .FirstOrDefault();

                // Depth ,height, width conduit terminal box
                double dDepthBox = pFaceSelect.Project(centerBox).Distance * 2;
                double dHeightBox = pFaceSelectTop.Project(centerBox).Distance * 2;
                double dWidthBox = pFaceSelectLeftRight.Project(centerBox).Distance * 2;

                // Get family symbol to conduit terminal box
                FamilySymbol familySymbol = cdtTmnBoxData.ConduitTerminalType;
                if (!familySymbol.IsActive)
                {
                    familySymbol.Activate();
                    App._UIDoc.Document.Regenerate();
                }

                ConvertElem = App._UIDoc.Document.Create.NewFamilyInstance(new XYZ(0, 0, 0), familySymbol, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);

                if (ConvertElem == null)
                    return false;

                // Set parameter to conduit terminal box
                UtilsParameter.SetValueAllParameterName(ConvertElem, "Depth", dDepthBox);
                UtilsParameter.SetValueAllParameterName(ConvertElem, "Height", dHeightBox);
                UtilsParameter.SetValueAllParameterName(ConvertElem, "Width", dWidthBox);

                // Commit
                REVWarning2 supWarning = new REVWarning2();
                fhOpts.SetFailuresPreprocessor(supWarning);
                reTrans.SetFailureHandlingOptions(fhOpts);
                reTrans.Commit();

                // Rotate terminal box
                reTrans.Start("ROTATE_TERMINAL_BOX");

                // Get geometry family instance
                solids = UtilsSolid.GetAllSolids(ConvertElem);
                mainSol = UtilsSolid.MergeSolids(solids);
                handSol = FindSolidsWithSimilarVolume(solids, 10e-4).FirstOrDefault();
                centerHand = handSol.ComputeCentroid();

                XYZ centerNew = mainSol.ComputeCentroid();

                lstPlanarFaceHand = new List<PlanarFace>();
                foreach (var item in handSol.Faces)
                {
                    if (item is PlanarFace pFace && !RevitUtils.IsParallel(pFace.FaceNormal, XYZ.BasisZ))
                        lstPlanarFaceHand.Add(pFace);
                }

                planarFacesHandArea = lstPlanarFaceHand.OrderByDescending(x => x.Area).FirstOrDefault();
                plane = Plane.CreateByNormalAndOrigin(planarFacesHandArea.FaceNormal, planarFacesHandArea.Origin);
                ptProjectHand = UtilsPlane.ProjectOnto(plane, centerNew);

                XYZ vectorFromHand = (ptProjectHand - centerNew).Normalize();
                double angle = vector.AngleOnPlaneTo(vectorFromHand, XYZ.BasisZ);

                double AngleTransform = revLnkIns.GetTotalTransform().BasisX.AngleTo(XYZ.BasisX);
                Line lineLoc = Line.CreateUnbound(centerNew, XYZ.BasisZ);

                ConduitTerminalPlacingData data = GetSectionDimensionInforRectange(mainSolIFC);

                //Rotate element
                //RotateElementBox(data, NewCreateConduitTerminal);
                ElementTransformUtils.RotateElement(App._UIDoc.Document, ConvertElem.Id, lineLoc, Math.PI * 2 - (angle + AngleTransform));

                App._UIDoc.Document.Regenerate();

                //Move element
                ElementTransformUtils.MoveElement(App._UIDoc.Document, ConvertElem.Id, center - centerNew);

                supWarning = new REVWarning2();
                fhOpts.SetFailuresPreprocessor(supWarning);
                reTrans.SetFailureHandlingOptions(fhOpts);
                reTrans.Commit();

                return true;
            }
            catch (Exception)
            {
                reTrans.RollBack();
            }
            return false;
        }

        public bool DealingWithCaseOfAnElectricalCabinetConduit(ConduitTerminalBoxData cdtTmnBoxData, Transaction reTrans, FailureHandlingOptions fhOpts)
        {
            try
            {
                if (cdtTmnBoxData == null)
                    return false;

                if (!reTrans.HasStarted())
                    return false;

                RevitLinkInstance revLnkIns = cdtTmnBoxData.LinkInstance;
                Document docLinked = revLnkIns.GetLinkDocument();
                Element linkedelement = docLinked.GetElement(cdtTmnBoxData.LinkEleData.LinkElement.Id);
                LinkElementData linkElementData = new LinkElementData(linkedelement);

                XYZ center = XYZ.Zero;
                XYZ vector = XYZ.Zero;
                List<XYZ> ptConvex = new List<XYZ>();
                Line linePrj = null;

                List<Solid> solids_1 = UtilsSolid.GetAllSolids(linkedelement);
                Solid mainSol_1 = UtilsSolid.MergeSolids(solids_1);
                center = mainSol_1.ComputeCentroid();

                Solid mainSolIFC = UtilsSolid.MergeSolids(solids_1);

                List<PlanarFace> lstPlanarFace_1 = new List<PlanarFace>();
                foreach (var item in mainSol_1.Faces)
                {
                    if (item is PlanarFace pFace)
                        lstPlanarFace_1.Add(pFace);
                }

                List<GeometryObject> geometries = GeometryUtils.GetIfcGeometriess(linkElementData.LinkElement);
                GeometryData geomertyData = new GeometryData(geometries);

                ptConvex = CalculateOrientedBoundingBox(geomertyData.Vertices, ref linePrj);
                XYZ pointPrj_1 = linePrj.Project(center).XYZPoint;
                vector = (pointPrj_1 - center).Normalize();

                //Set width height
                PlanarFace pFaceSelectTop = lstPlanarFace_1.Where(x => RevitUtils.IsParallel(x.FaceNormal, XYZ.BasisZ))
                                                           .FirstOrDefault();

                BoundingBoxXYZ boxIFC = linkedelement.get_BoundingBox(null);
                XYZ centerBox = (boxIFC.Max + boxIFC.Min) / 2;

                Dictionary<double, XYZ> lstValueWidthDepth = new Dictionary<double, XYZ>();
                foreach (var face in lstPlanarFace_1)
                {
                    if (RevitUtils.IsParallel(face.FaceNormal, XYZ.BasisZ))
                        continue;

                    var check = face.Project(centerBox).XYZPoint;
                    double dis = check.DistanceTo(centerBox) * 2;
                    if (!lstValueWidthDepth.Any(x => RevitUtils.IsEqual(x.Key, dis)))
                        lstValueWidthDepth.Add(dis, face.FaceNormal);
                }

                double dHeightBox = pFaceSelectTop.Project(centerBox).Distance * 2;

                double dWidthBox = double.MinValue;
                double dDepthBox = double.MinValue;

                var lstValueWidthDepthList = lstValueWidthDepth.ToList();

                if (lstValueWidthDepth.Count == 2)
                {
                    //Set widh height depth
                    dWidthBox = lstValueWidthDepthList[0].Key;
                    dDepthBox = lstValueWidthDepthList[1].Key;
                }
                else if (lstValueWidthDepth.Count == 1)
                {
                    dWidthBox = lstValueWidthDepthList[0].Key;
                    dDepthBox = lstValueWidthDepthList[0].Key;
                }

                //Create instance

                FamilySymbol familySymbol = cdtTmnBoxData.ConduitTerminalType;
                if (!familySymbol.IsActive)
                {
                    familySymbol.Activate();
                    App._UIDoc.Document.Regenerate();
                }

                ConvertElem = App._UIDoc.Document.Create.NewFamilyInstance(new XYZ(0, 0, 0), familySymbol, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
                if (ConvertElem?.IsValidObject != true)
                    return false;

                // Set parameter to conduit terminal box
                UtilsParameter.SetValueAllParameterName(ConvertElem, "Depth", dDepthBox);
                UtilsParameter.SetValueAllParameterName(ConvertElem, "Height", dHeightBox);
                UtilsParameter.SetValueAllParameterName(ConvertElem, "Width", dWidthBox);

                if (ConvertElem != null)
                {
                    _doc.Regenerate();

                    List<Solid> solids = UtilsSolid.GetAllSolids(ConvertElem);
                    Solid mainSol = UtilsSolid.MergeSolids(solids);

                    var centerNew = mainSol.ComputeCentroid();
                    XYZ centerNewTransForm = RevLinkTransform.OfPoint(center);

                    XYZ CentrerTrans = centerNewTransForm - centerNew;

                    ElementTransformUtils.MoveElement(_doc, ConvertElem.Id, CentrerTrans);

                    _doc.Regenerate();

                    //Rotate terminal box
                    Line lineAxis = Line.CreateUnbound(centerNewTransForm, XYZ.BasisZ);

                    XYZ facingLink = lstValueWidthDepthList[0].Value.Normalize();
                    facingLink = RevLinkTransform.OfVector(facingLink);

                    double angleRotate = ConvertElem.HandOrientation.AngleOnPlaneTo(facingLink, XYZ.BasisZ);

                    ElementTransformUtils.RotateElement(App._UIDoc.Document, ConvertElem.Id, lineAxis, angleRotate);
                }

                return true;
            }
            catch (Exception)
            {
                reTrans.RollBack();
            }
            return false;
        }

        public bool DealingWithCaseOfAnElectricalCabinetNormal(ConduitTerminalBoxData cdtTmnBoxData, Transaction reTrans, FailureHandlingOptions fhOpts)
        {
            try
            {
                if (cdtTmnBoxData == null)
                    return false;

                if (!reTrans.HasStarted())
                    return false;

                RevitLinkInstance revLnkIns = cdtTmnBoxData.LinkInstance;
                Document docLinked = revLnkIns.GetLinkDocument();
                Element linkedelement = docLinked.GetElement(cdtTmnBoxData.LinkEleData.LinkElement.Id);
                LinkElementData linkElementData = new LinkElementData(linkedelement);

                XYZ center = XYZ.Zero;
                XYZ vector = XYZ.Zero;
                List<XYZ> ptConvex = new List<XYZ>();
                Line linePrj = null;

                List<XYZ> allPoints = GetAllPointOfObject(linkElementData.LinkElement);

                List<Solid> solids_1 = UtilsSolid.GetAllSolids(linkedelement);
                Solid mainSol_1 = UtilsSolid.MergeSolids(solids_1);
                center = mainSol_1.ComputeCentroid();

                Solid mainSolIFC = UtilsSolid.MergeSolids(solids_1);

                List<PlanarFace> lstPlanarFace_1 = new List<PlanarFace>();
                foreach (var item in mainSol_1.Faces)
                {
                    if (item is PlanarFace pFace)
                        lstPlanarFace_1.Add(pFace);
                }

                ptConvex = CalculateOrientedBoundingBox(allPoints, ref linePrj);
                XYZ pointPrj_1 = linePrj.Project(center).XYZPoint;
                vector = (pointPrj_1 - center).Normalize();

                FamilySymbol familySymbol = ConduitTerminalType;
                if (!familySymbol.IsActive)
                {
                    familySymbol.Activate();
                    App._UIDoc.Document.Regenerate();
                }

                ConvertElem = App._UIDoc.Document.Create.NewFamilyInstance(new XYZ(0, 0, 0), familySymbol, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
                if (ConvertElem?.IsValidObject != true)
                    return false;

                if (ConvertElem != null)
                {
                    _doc.Regenerate();

                    List<Solid> solids = UtilsSolid.GetAllSolids(ConvertElem);
                    Solid mainSol = UtilsSolid.MergeSolids(solids);
                    XYZ centerNew = mainSol.ComputeCentroid();
                    XYZ centerNewTransForm = RevLinkTransform.OfPoint(center);

                    List<PlanarFace> lstPlanarFace = new List<PlanarFace>();
                    foreach (var item in mainSol.Faces)
                    {
                        if (item is PlanarFace pFace)
                            lstPlanarFace.Add(pFace);
                    }
                    var ptcheck = GetAllPointOfObject(ConvertElem);
                    Line linePrj1 = null;
                    var ptConvex1 = CalculateOrientedBoundingBox(ptcheck, ref linePrj1);
                    XYZ pointPrj = linePrj1.Project(centerNew).XYZPoint;
                    XYZ newvector = (pointPrj - centerNew).Normalize();
                    var angle = vector.AngleOnPlaneTo(newvector, XYZ.BasisZ);
                    Line lineLoc = Line.CreateUnbound(centerNew, XYZ.BasisZ);

                    double AngleTransform = revLnkIns.GetTotalTransform().BasisX.AngleTo(XYZ.BasisX);

                    ElementTransformUtils.RotateElement(App._UIDoc.Document, cdtTmnBoxData.ConvertElem.Id, lineLoc, Math.PI * 2 - (angle + AngleTransform));
                    _doc.Regenerate();
                    ElementTransformUtils.MoveElement(App._UIDoc.Document, ConvertElem.Id, centerNewTransForm - centerNew);
                    //RotateElementBox(data, NewCreateConduitTerminal);
                }

                return true;
            }
            catch (Exception)
            {
                reTrans.RollBack();
            }
            return false;
        }

        public List<XYZ> CalculateOrientedBoundingBox(List<XYZ> rotatedPoints, ref Line lineProject)
        {
            List<XYZ> newPts = new List<XYZ>();
            foreach (var item in rotatedPoints)
            {
                newPts.Add(new XYZ(item.X, item.Y, 0));
            }

            List<XYZ> ptConvex = ConvexHull.GetConvexHull(newPts);

            List<Line> lstLine = new List<Line>();
            for (int i = 0; i < ptConvex.Count; i++)
            {
                XYZ nextPt = i == ptConvex.Count - 1 ? ptConvex[0] : ptConvex[i + 1];
                Line line = Line.CreateBound(ptConvex[i], nextPt);
                lstLine.Add(line);
            }

            lineProject = lstLine.OrderByDescending(x => x.Length).FirstOrDefault();

            return ptConvex;
        }

        public static List<XYZ> GetAllPointOfObject(Element element)
        {
            List<Solid> solidIFCs = UtilsSolid.GetAllSolids(element, null, null).ToList();
            List<XYZ> pointOfElm = new List<XYZ>();

            List<Mesh> lstMesh = GeometryUtils.GetAllMeshes(element, true);
            if (lstMesh?.Count > 0)
            {
                lstMesh.ForEach(mesh => pointOfElm.AddRange(mesh.Vertices));
            }

            if (solidIFCs?.Count > 0)
            {
                // union solid
                Solid unionSol = RevitUtils.UnionSolids(solidIFCs);
                if (unionSol == null)
                    return null;

                // get all planar face
                var allPlFaces = UtilsPlane.GetPlanarFaceSolid(unionSol);

                foreach (var face in allPlFaces)
                {
                    var pointOnFace = UtilsPlane.GetPointsOfFace(face);
                    pointOfElm.AddRange(pointOnFace);
                }
            }

            return pointOfElm;
        }

        private List<Solid> FindSolidsWithSimilarVolume(List<Solid> solids, double volumeTolerance)
        {
            List<Solid> resultSolids = new List<Solid>();

            Dictionary<double, List<Solid>> volumeMap = new Dictionary<double, List<Solid>>();

            foreach (Solid solid in solids)
            {
                double volume = solid.Volume;

                bool foundMatchingVolume = false;

                foreach (var kvp in volumeMap)
                {
                    // Compare the current solid's volume with existing volumes
                    if (Math.Abs(volume - kvp.Key) <= volumeTolerance)
                    {
                        kvp.Value.Add(solid);
                        foundMatchingVolume = true;
                        break;
                    }
                }

                if (!foundMatchingVolume)
                    volumeMap[volume] = new List<Solid> { solid };
            }

            foreach (var kvp in volumeMap)
            {
                // If there are multiple solids with similar volumes
                if (kvp.Value.Count > 1)
                    resultSolids.AddRange(kvp.Value);
            }

            return resultSolids;
        }

        private double GetDisOnFace(PlanarFace pFace, XYZ center)
        {
            try
            {
                Plane plane = Plane.CreateByNormalAndOrigin(pFace.FaceNormal, pFace.Origin);
                XYZ ptProjectHand = UtilsPlane.ProjectOnto(plane, center);
                return ptProjectHand.DistanceTo(center);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        #endregion Method
    }
}