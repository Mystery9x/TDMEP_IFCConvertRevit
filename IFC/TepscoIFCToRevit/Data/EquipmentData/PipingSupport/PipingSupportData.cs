using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using TepscoIFCToRevit.Common;
using TepscoIFCToRevit.UI;

namespace TepscoIFCToRevit.Data.EquipmentData.PipingSupport
{
    public class PipingSupportData : BindableBase
    {
        #region Variable

        protected UIDocument _uiDoc = null;
        protected Document _doc = null;

        protected bool _isCreateManual;
        protected FamilySymbol _pipingSPType;
        protected Family _familySelect = null;

        #endregion Variable

        #region Property

        public RevitLinkInstance LinkInstance { get; set; }

        public Transform LinkTransform => LinkInstance?.GetTotalTransform();

        public LinkElementData LinkEleData { get; set; }

        public ConvertParamData ParameterData { get; set; }

        public List<FamilyInstance> ConvertInstances { get; set; } = new List<FamilyInstance>();

        #endregion Property

        #region Constructor

        public PipingSupportData(UIDocument uIDocument,
                                 LinkElementData linkEleData,
                                 RevitLinkInstance revLnkIns,
                                 bool isCreateManual,
                                 FamilySymbol pipingSPFamily = null,
                                 Family familySelect = null,
                                 ConvertParamData parameterData = null)
        {
            if (uIDocument != null
                && linkEleData != null
                && linkEleData.LinkElement != null
                && linkEleData.LinkElement.IsValidObject
                && revLnkIns != null)
            {
                _uiDoc = uIDocument;
                _doc = _uiDoc.Document;
                LinkInstance = revLnkIns;
                LinkEleData = linkEleData;
                _pipingSPType = pipingSPFamily;
                _isCreateManual = isCreateManual;
                _familySelect = familySelect;
                ParameterData = parameterData;
            }
        }

        #endregion Constructor

        /// <summary>
        /// Create the family instance shelf (piping support)
        /// </summary>
        public void Initialize()
        {
            try
            {
                List<Solid> solidIFCs = UtilsSolid.GetAllSolids(LinkEleData.LinkElement);
                if (solidIFCs.Count <= 0)
                    return;
                bool isMergeSolid = CheckMergeSolid(solidIFCs);

                Solid mainSol = solidIFCs[0];
                if (isMergeSolid)
                    mainSol = UtilsSolid.MergeSolids(solidIFCs);

                if (mainSol != null)
                {
                    ElementIFC dataIFC = null;
                    if (_familySelect.Name.Contains("STK")
                        || _familySelect.Name.Contains("設備サポート_I形(円形)"))
                    {
                        dataIFC = new ElementIFC(_doc, LinkEleData.LinkElement, LinkEleData.LinkElement.Document, ObjectIFCType.PipeSP, null, mainSol);

                        if (!dataIFC.IsCircle)
                            return;

                        if (!_isCreateManual && _familySelect != null)
                        {
                            if (_familySelect.Name.Contains("設備サポート_I形(円形)"))
                                _pipingSPType = _doc.GetElement(_familySelect.GetFamilySymbolIds().FirstOrDefault()) as FamilySymbol;
                            else
                                _pipingSPType = AutoGetFamilySybol(dataIFC);
                        }

                        CreateShapeCylinder(dataIFC, mainSol);
                    }
                    else
                    {
                        // Get all planar face
                        List<PlanarFace> faces = UtilsPlane.GetPlanarFaceSolid(mainSol);

                        if (ValidateInput(faces))
                            return;

                        if (faces.Count == 10 && DivideShape(mainSol, faces, out Solid solid_0, out Solid solid_1))
                        {
                            CreateShapeRectange(solid_0);
                            CreateShapeRectange(solid_1);
                        }
                        else if (faces.Count == 6)
                        {
                            CreateShapeRectange(mainSol);
                        }
                    }

                    foreach (var item in ConvertInstances)
                    {
                        RevitUtils.SetValueParamterConvert(_uiDoc, item, LinkEleData, ParameterData);
                    }
                }
            }
            catch (Exception) { }
        }

        private FamilySymbol AutoGetFamilySybol(ElementIFC dataIFC)
        {
            FamilySymbol symbolPipeSPAuto = null;

            List<FamilySymbol> familySymbols = new List<FamilySymbol>();
            _familySelect.GetFamilySymbolIds().ForEach(x => familySymbols.Add(_doc.GetElement(x) as FamilySymbol));

            if (familySymbols?.Count == 0)
                return null;
            else if (familySymbols.Count == 1)
                symbolPipeSPAuto = familySymbols.FirstOrDefault();
            else
            {
                if (dataIFC.IsCircle)
                {
                    if (dataIFC.Width.Length > 0
                      && (_familySelect.Name.Contains("設備サポート_I形(円形)")
                         || _familySelect.Name.Contains("STK")))
                        symbolPipeSPAuto = FindFamilySymbolFitCircleIFC(familySymbols, dataIFC);
                }
                else
                {
                    if (dataIFC.Length.Length > 0
                        && dataIFC.Width.Length > 0)
                        symbolPipeSPAuto = FindFamilySymbolFitRectangleIFC(familySymbols, dataIFC);
                }
            }

            return symbolPipeSPAuto;
        }

        private double GetHeightIFCCompare(double height, double width, double heightIFC, double widthIFC, out List<double> pairEdges)
        {
            double heightCompare;
            pairEdges = new List<double>();
            if (RevitUtils.IsEqual(heightIFC, widthIFC))
                heightCompare = heightIFC;
            else
            {
                EnumCompareSizeSymbol enumCompareSize = EnumCompareSizeSymbol.Equal;

                if (RevitUtils.IsLessThan(height, width))
                    enumCompareSize = EnumCompareSizeSymbol.less;
                else if (RevitUtils.IsGreaterThan(height, width))
                    enumCompareSize = EnumCompareSizeSymbol.Greater;

                if (enumCompareSize == EnumCompareSizeSymbol.less)
                {
                    if (RevitUtils.IsLessThan(heightIFC, widthIFC))
                        heightCompare = heightIFC;
                    else
                        heightCompare = widthIFC;
                }
                else if (enumCompareSize == EnumCompareSizeSymbol.Greater)
                {
                    if (RevitUtils.IsGreaterThan(heightIFC, widthIFC))
                        heightCompare = heightIFC;
                    else
                        heightCompare = widthIFC;
                }
                else
                {
                    if (RevitUtils.IsLessThan(Math.Abs(height - heightIFC), Math.Abs(height - widthIFC)))
                        heightCompare = heightIFC;
                    else
                        heightCompare = widthIFC;
                }
            }

            pairEdges.Add(height);
            pairEdges.Add(heightCompare);
            pairEdges.Add(width);

            if (RevitUtils.IsEqual(heightCompare, heightIFC))
                pairEdges.Add(widthIFC);
            else
                pairEdges.Add(heightIFC);

            return heightCompare;
        }

        private enum EnumCompareSizeSymbol
        {
            Greater,
            less,
            Equal
        }

        public FamilySymbol FindFamilySymbolFitRectangleIFC(List<FamilySymbol> familySymbols, ElementIFC dataIFC)
        {
            FamilySymbol symbolPipeSPAuto = null;

            Dictionary<double, List<FamilySymbol>> familySymbolCompares = new Dictionary<double, List<FamilySymbol>>();

            FamilySymbol familySymbols1 = FindFamilySymbolFitIFC(familySymbols,
                                                                 new List<Line> { dataIFC.Length, dataIFC.Width },
                                                                 out List<double> pairEdge1);

            symbolPipeSPAuto = GetTolerance(familySymbols1, pairEdge1, ref familySymbolCompares);
            if (symbolPipeSPAuto != null)
                return symbolPipeSPAuto;

            FamilySymbol familySymbols2 = FindFamilySymbolFitIFC(familySymbols,
                                                                new List<Line> { dataIFC.Length, dataIFC.Location },
                                                                out List<double> pairEdge2);

            symbolPipeSPAuto = GetTolerance(familySymbols2, pairEdge2, ref familySymbolCompares);
            if (symbolPipeSPAuto != null)
                return symbolPipeSPAuto;

            FamilySymbol familySymbols3 = FindFamilySymbolFitIFC(familySymbols,
                                                                new List<Line> { dataIFC.Width, dataIFC.Location },
                                                                out List<double> pairEdge3);

            symbolPipeSPAuto = GetTolerance(familySymbols3, pairEdge3, ref familySymbolCompares);
            if (symbolPipeSPAuto != null)
                return symbolPipeSPAuto;

            if (familySymbolCompares != null)
            {
                familySymbolCompares = familySymbolCompares.OrderBy(x => x.Key).ToDictionary(entry => (double)entry.Key,
                                                                                            entry => entry.Value);

                symbolPipeSPAuto = familySymbolCompares.FirstOrDefault().Value.FirstOrDefault();
            }

            return symbolPipeSPAuto;
        }

        private FamilySymbol GetTolerance(FamilySymbol familySymbol, List<double> pairEdge, ref Dictionary<double, List<FamilySymbol>> symbolCompares)
        {
            double tolerance;
            if (familySymbol != null
                && pairEdge.Count > 0)
            {
                tolerance = Math.Abs(pairEdge[0] - pairEdge[1]) + Math.Abs(pairEdge[2] - pairEdge[3]);
                if (RevitUtils.IsEqual(tolerance, 0))
                    return familySymbol;
                else
                {
                    if (symbolCompares.ContainsKey(tolerance))
                        symbolCompares[tolerance].Add(familySymbol);
                    else
                        symbolCompares.Add(tolerance, new List<FamilySymbol> { familySymbol });
                }
            }
            return null;
        }

        public FamilySymbol FindFamilySymbolFitIFC(List<FamilySymbol> familySymbols, List<Line> lineIFCs, out List<double> pairEdgeCompires)
        {
            pairEdgeCompires = new List<double>();
            FamilySymbol symbolPipeSPAuto = null;
            double sizeIFC = 0;

            Dictionary<double, List<FamilySymbol>> familySymbolFits = new Dictionary<double, List<FamilySymbol>>();
            double heightIFC = lineIFCs[0].Length;
            double widthIFC = lineIFCs[1].Length;

            foreach (FamilySymbol familySymbol in familySymbols)
            {
                double sizeSymbol = (double)UtilsParameter.GetFirstValueParameterName(familySymbol, "高さ");
                double width = (double)UtilsParameter.GetFirstValueParameterName(familySymbol, "幅");

                if ((RevitUtils.IsEqual(sizeSymbol, heightIFC, 2 / 304.8)
                    && RevitUtils.IsEqual(width, widthIFC, 2 / 304.8))
                    || (RevitUtils.IsEqual(sizeSymbol, widthIFC, 2 / 304.8)
                        && RevitUtils.IsEqual(width, heightIFC, 2 / 304.8)))
                {
                    pairEdgeCompires = new List<double>() { sizeSymbol, heightIFC, width, widthIFC };
                    return familySymbol;
                }

                sizeIFC = GetHeightIFCCompare(sizeSymbol, width, heightIFC, widthIFC, out List<double> pairEdges);
                double tolerance = Math.Abs(sizeIFC - sizeSymbol);

                if (!familySymbolFits.ContainsKey(tolerance))
                {
                    familySymbolFits.Add(tolerance, new List<FamilySymbol>() { familySymbol });
                }
                else
                {
                    familySymbolFits[tolerance].Add(familySymbol);
                }
            }

            if (familySymbolFits.Count > 0)
            {
                familySymbolFits = familySymbolFits.OrderBy(x => x.Key).ToDictionary(entry => (double)entry.Key,
                                                                                               entry => entry.Value);

                if (familySymbolFits.FirstOrDefault().Value.Count() == 1)
                {
                    symbolPipeSPAuto = familySymbolFits.FirstOrDefault().Value[0];
                    double height = (double)UtilsParameter.GetFirstValueParameterName(symbolPipeSPAuto, "高さ");
                    double width = (double)UtilsParameter.GetFirstValueParameterName(symbolPipeSPAuto, "幅");
                    GetHeightIFCCompare(height, width, heightIFC, widthIFC, out List<double> pairEdges);
                    pairEdgeCompires = pairEdges;
                }
                else
                {
                    List<FamilySymbol> symbols = familySymbolFits.FirstOrDefault().Value;
                    double compare = 0;

                    foreach (FamilySymbol familySymbol in symbols)
                    {
                        double height = (double)UtilsParameter.GetFirstValueParameterName(familySymbol, "高さ");
                        double width = (double)UtilsParameter.GetFirstValueParameterName(familySymbol, "幅");
                        GetHeightIFCCompare(height, width, heightIFC, widthIFC, out List<double> pairEdges);
                        double compareWith = pairEdges.LastOrDefault();

                        double tolerance = Math.Abs(compareWith - width);
                        if (symbolPipeSPAuto == null)
                        {
                            symbolPipeSPAuto = familySymbol;
                            pairEdgeCompires = pairEdges;
                            compare = tolerance;
                        }
                        else if (RevitUtils.IsLessThan(tolerance, compare))
                        {
                            symbolPipeSPAuto = familySymbol;
                            pairEdgeCompires = pairEdges;
                            compare = tolerance;
                        }
                    }
                }
            }

            return symbolPipeSPAuto;
        }

        public FamilySymbol FindFamilySymbolFitCircleIFC(List<FamilySymbol> familySymbols, ElementIFC dataIFC)
        {
            FamilySymbol symbolPipeSPAuto = null;
            double sizeIFC = 0;

            Dictionary<double, List<FamilySymbol>> familySymbolFits = new Dictionary<double, List<FamilySymbol>>();

            foreach (FamilySymbol familySymbol in familySymbols)
            {
                double sizeSymbol = (double)UtilsParameter.GetFirstValueParameterName(familySymbol, "直径");
                sizeIFC = dataIFC.Width.Length;

                if (RevitUtils.IsGreaterThan(sizeSymbol, sizeIFC, 2 / 304.8))
                    continue;
                else
                {
                    double tolerance = Math.Abs(sizeIFC - sizeSymbol);

                    if (!familySymbolFits.ContainsKey(tolerance))
                        familySymbolFits.Add(tolerance, new List<FamilySymbol>() { familySymbol });
                    else
                        familySymbolFits[tolerance].Add(familySymbol);
                }
            }

            if (familySymbolFits.Count > 0)
            {
                familySymbolFits = familySymbolFits.OrderBy(x => x.Key).ToDictionary(entry => (double)entry.Key, entry => entry.Value);
                if (familySymbolFits.FirstOrDefault().Value.Count() == 1)
                    symbolPipeSPAuto = familySymbolFits.FirstOrDefault().Value[0];
            }

            return symbolPipeSPAuto;
        }

        private List<Line> GetEdgeIFCToCompare(List<double> edgeSymbols, List<Line> edgeIFCs, out Line mainLine)
        {
            mainLine = null;
            List<Line> lineIFC = new List<Line>();

            foreach (double edgeSymbol in edgeSymbols)
            {
                Line line = GetLine(edgeSymbol, ref edgeIFCs);
                if (line != null)
                    lineIFC.Add(line);
            }

            mainLine = edgeIFCs.Except(lineIFC).FirstOrDefault();

            return lineIFC;
        }

        private Line GetLine(double edgeSymbol, ref List<Line> edgeIFCs)
        {
            double compare = 0;
            Line line = null;
            int j = -1;
            for (int i = 0; i < edgeIFCs.Count; i++)
            {
                double length = edgeIFCs[i].Length;
                double tolerance = Math.Abs(length - edgeSymbol);
                if (line == null)
                {
                    line = edgeIFCs[i];
                    compare = tolerance;
                    j = i;
                }
                else if (RevitUtils.IsLessThan(tolerance, compare))
                {
                    line = edgeIFCs[i];
                    compare = tolerance;
                    j = i;
                }
            }

            edgeIFCs.RemoveAt(j);
            return line;
        }

        /// <summary>
        /// Some object ifc has been created from family pipe support
        /// have two solids and them as the same
        /// so if merge solids has able erro when get face after merge them
        /// </summary>
        /// <param name="solidIFCs"></param>
        /// <returns></returns>
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

        #region divide T shape solid

        private bool DivideShape(Solid mainSol, List<PlanarFace> faces, out Solid solid_0, out Solid solid_1)
        {
            XYZ up = GetUpVector(faces);

            var sidefaces = faces.Where(x => RevitUtils.IsPerpendicular(x.FaceNormal, up)).ToList();
            XYZ normal = sidefaces[0].FaceNormal;

            List<PlanarFace> list_0 = sidefaces.Where(x => RevitUtils.IsParallel(x.FaceNormal, normal)).ToList();
            Plane cutPlane = GetCutPlane(list_0);
            if (cutPlane == null)
            {
                List<PlanarFace> list_1 = sidefaces.Except(list_0).ToList();
                cutPlane = GetCutPlane(list_1);
            }

            try
            {
                solid_0 = BooleanOperationsUtils.CutWithHalfSpace(mainSol, cutPlane);
                cutPlane = Plane.CreateByNormalAndOrigin(cutPlane.Normal.Negate(), cutPlane.Origin);
                solid_1 = BooleanOperationsUtils.CutWithHalfSpace(mainSol, cutPlane);
            }
            catch (Exception)
            {
                solid_0 = null;
                solid_1 = null;
            }
            return solid_0 != null && solid_1 != null;
        }

        private Plane GetCutPlane(List<PlanarFace> faces)
        {
            if (faces?.Count > 0)
            {
                XYZ dir = faces[0].FaceNormal;
                var ordered = faces.OrderBy(x => x.Origin, dir);
                double dist_1 = RevitUtils.GetSignedDistance(ordered[0].Origin, ordered[0].FaceNormal, ordered[1].Origin);
                double dist_2 = RevitUtils.GetSignedDistance(ordered[0].Origin, ordered[0].FaceNormal, ordered[2].Origin);
                if (RevitUtils.IsEqual(dist_1, dist_2))
                    return Plane.CreateByNormalAndOrigin(ordered[1].FaceNormal, ordered[1].Origin);
            }
            return null;
        }

        private XYZ GetUpVector(List<PlanarFace> faces)
        {
            if (faces?.Count == 10)
            {
                var vecs = faces.ConvertAll(x => x.FaceNormal);
                var upVector = vecs.FirstOrDefault(v => vecs.Count(x => RevitUtils.IsPerpendicular(v, x)) == 8);
                return upVector;
            }
            return null;
        }

        #endregion divide T shape solid

        #region Shape I

        protected void CreateShapeRectange(Solid mainSol)
        {
            if (mainSol == null)
                return;
            try
            {
                if (!_isCreateManual
                    && _familySelect != null
                    && !_familySelect.Name.Contains("設備サポート_I形"))
                {
                    ElementIFC dataIFC = new ElementIFC(_doc, LinkEleData.LinkElement, LinkEleData.LinkElement.Document, ObjectIFCType.PipeSP, null, mainSol);
                    _pipingSPType = AutoGetFamilySybol(dataIFC);
                }

                if (_pipingSPType == null
                    && _familySelect.Name.Contains("設備サポート_I形"))
                {
                    _pipingSPType = _doc.GetElement(_familySelect.GetFamilySymbolIds().FirstOrDefault()) as FamilySymbol;
                }

                if (!_pipingSPType.IsActive)
                    _pipingSPType.Activate();

                PipingSupportPlacingData data = GetSectionDimensionInforRectange(mainSol);
                if (data.IsValid())
                {
                    XYZ directionIntance = null;
                    if (data.LHeight != null)
                        directionIntance = data.LHeight.Direction;
                    else if (data.LWith != null)
                        directionIntance = data.LWith.Direction;

                    FamilyInstance instance;
                    if (_pipingSPType.Name == "設備サポート_I形")
                        instance = CreateInstanceByDirectShape(data.FacePerpendicularWithLocation.Origin, data.FacePerpendicularWithLocation.FaceNormal, data.MainLine, data.MainLine.Direction);
                    else
                        instance = CreateInstanceByDirectShape(data.FacePlace.Origin, data.FacePlace.FaceNormal, data.MainLine, directionIntance);

                    if (instance?.IsValidObject == true)
                    {
                        _doc.Regenerate();

                        if (_pipingSPType.Name == "設備サポート_I形")
                        {
                            SetParameterDimensionInstanceI(instance, data.Width, data.Height, data.Length);
                            RotateElementI(data, instance);
                        }
                        else
                            SetParameterDimensionInstance(instance, data.FacePerpendicularWithLocation.Origin, data.FacePerpendicularWithLocation.FaceNormal, data.MainLine);

                        Solid curSolid = UtilsSolid.GetTotalSolid(instance);
                        if (curSolid != null)
                        {
                            if (!RevitUtils.IsEqual(data.Width, data.Height)
                                 && _pipingSPType.Name != "設備サポート_I形")
                            {
                                XYZ lineCompare = data.LWith.Direction;
                                if (data.Width < data.Height)
                                    lineCompare = data.LHeight.Direction;

                                List<Line> edges = GetLinesFromSolid(curSolid).Where(x => x.Length > RevitUtils.MIN_LENGTH).ToList();
                                RotationSectionHozontal(instance.Id, curSolid.ComputeCentroid(), edges, data.FacePerpendicularWithLocation, data.Direction, lineCompare);
                            }

                            XYZ curLoc = curSolid.ComputeCentroid();
                            if (!IsPointInsideSolidRectangleBox(mainSol, curLoc))
                                instance.IsWorkPlaneFlipped = !instance.IsWorkPlaneFlipped;
                            _doc.Regenerate();

                            curSolid = UtilsSolid.GetTotalSolid(instance);
                            XYZ centroid = LinkTransform.OfPoint(mainSol.ComputeCentroid());
                            MoveInstance_IAfterCreate(curSolid, centroid, instance);
                        }

                        ConvertInstances.Add(instance);
                    }
                }
            }
            catch (Exception) { }
        }

        private void RotationSectionHozontal(ElementId idElem,
                                             XYZ centerSolid,
                                             List<Line> edges,
                                             PlanarFace face,
                                             XYZ direction,
                                             XYZ lineIFCCompare)
        {
            XYZ up = face.FaceNormal;
            XYZ hoz = direction.CrossProduct(up);

            GetHeightWithSolid(edges, up, hoz, out Line lwith, out Line lheight);

            if (lwith != null
                && lheight != null
                && !RevitUtils.IsEqual(lheight.Length, lwith.Length))
            {
                Line lineIntance = lwith;
                if (lwith.Length < lheight.Length)
                    lineIntance = lheight;

                double angle = lineIFCCompare.AngleTo(lineIntance.Direction);
                if (!RevitUtils.IsEqual(angle, 0)
                   || !RevitUtils.IsEqual(angle, Math.PI))
                {
                    Line lineAxis = Line.CreateUnbound(centerSolid, direction);
                    ElementTransformUtils.RotateElement(_doc, idElem, lineAxis, angle);
                }
            }
        }

        private void CreateShapeCylinder(ElementIFC dataIfc, Solid mainSolid)
        {
            if (!_pipingSPType.IsActive)
                _pipingSPType.Activate();

            Line mainLine = dataIfc.Location;
            Plane plane = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, mainLine.GetEndPoint(0));

            if (RevitUtils.IsParallel(mainLine.Direction, XYZ.BasisZ))
                plane = Plane.CreateByNormalAndOrigin(XYZ.BasisX, mainLine.GetEndPoint(0));

            XYZ normal = plane.Normal;

            // check main line has lie in plane place pipe support
            XYZ projectStart = RevitUtils.ProjectOnto(plane, mainLine.GetEndPoint(0));
            XYZ projectEnd = RevitUtils.ProjectOnto(plane, mainLine.GetEndPoint(1));

            double angle = mainLine.Direction.AngleTo(Line.CreateBound(projectStart, projectEnd).Direction);
            if (!RevitUtils.IsEqual(0, angle)
               && !RevitUtils.IsEqual(Math.PI, angle))
                normal = GetNormalForTempdirectShape(mainLine.Direction, plane.Origin);

            FamilyInstance instance = CreateInstanceByDirectShape(plane.Origin, normal, mainLine, mainLine.Direction);
            if (instance?.IsValidObject == true)
            {
                _doc.Regenerate();
                if (_pipingSPType.Name.Contains("設備サポート_I形(円形)"))
                {
                    SetParameterDimensionInstanceI(instance, dataIfc.Width.Length, null, dataIfc.Location.Length);
                    Solid curSolid = UtilsSolid.GetTotalSolid(instance);
                    XYZ centroid = LinkTransform.OfPoint(mainSolid.ComputeCentroid());
                    MoveInstance_IAfterCreate(curSolid, centroid, instance);
                }
                else
                    SetParameterDimensionInstance(instance, plane.Origin, normal, mainLine);
                ConvertInstances.Add(instance);
            }
        }

        protected PipingSupportPlacingData GetSectionDimensionInforRectange(Solid solid)
        {
            List<Line> edges = GetLinesFromSolid(solid).Where(x => x.Length > RevitUtils.MIN_LENGTH).ToList();
            List<PlanarFace> planarFaces = UtilsPlane.GetPlanarFaceSolid(solid);
            PipingSupportPlacingData data = new PipingSupportPlacingData();

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

            if (!_pipingSPType.Name.Contains("設備サポート_I形(円形)")
                && !_pipingSPType.Name.Contains("STK")
                && !_pipingSPType.Name.Contains("設備サポート_I形"))
            {
                double sizeSymbol = (double)UtilsParameter.GetFirstValueParameterName(_pipingSPType, "高さ");
                double width = (double)UtilsParameter.GetFirstValueParameterName(_pipingSPType, "幅");
                List<Line> edgeIFCs = GetEdgeIFCToCompare(new List<double> { sizeSymbol, width }
                                                          , new List<Line> { data.LHeight, data.LWith, data.MainLine }
                                                          , out Line mainLine);

                data.MainLine = mainLine;
                data.Direction = mainLine.Direction;
                data.LHeight = edgeIFCs[0];
                data.LWith = edgeIFCs[1];

                if (RevitUtils.IsParallel(data.MainLine.Direction, XYZ.BasisZ))
                    data.FacePerpendicularWithLocation = GetSideFace(planarFaces, data.MainLine);
                else
                    data.FacePerpendicularWithLocation = GetBottomFace(planarFaces, data.MainLine);

                data.FacePlace = planarFaces.FirstOrDefault(x => RevitUtils.IsParallel(x.FaceNormal, data.MainLine.Direction, 0.05));
            }

            data.Length = data.MainLine != null ? data.MainLine.Length : 0;
            data.Width = data.LWith != null ? data.LWith.Length : 0;
            data.Height = data.LHeight != null ? data.LHeight.Length : 0;
            return data;
        }

        private void GetHeightWithSolid(List<Line> edges, XYZ upVector, XYZ hozVector, out Line with, out Line height)
        {
            with = edges.Where(x => RevitUtils.IsParallel(x.Direction, hozVector))/*.OrderBy(x => x.Origin, line.Direction)*/
                               .Max(x => x.Length, (double x0, double x1) => x0 < x1);

            height = edges.Where(x => RevitUtils.IsParallel(x.Direction, upVector)).Max(x => x.Length, (double x0, double x1) => x0 < x1);
        }

        /// <summary>
        /// get the bottom face of given planar faces,
        /// handle the case where piping support is diagonal
        /// to vertical direction
        /// </summary>
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

        /// <summary>
        /// get the bottom face of given planar faces,
        /// handle the case where piping support is diagonal
        /// to vertical direction
        /// </summary>
        private PlanarFace GetSideFace(List<PlanarFace> faces, Line centerLine)
        {
            return faces.FirstOrDefault(x => !RevitUtils.IsParallel(x.FaceNormal, centerLine.Direction));     // exclude start, end faces
        }

        /// <summary>
        /// Is point inside solid rectangle box
        /// </summary>
        protected bool IsPointInsideSolidRectangleBox(Solid solid, XYZ curPoint)
        {
            if (solid == null || curPoint == null)
                return false;
            XYZ point = LinkTransform.Inverse.OfPoint(curPoint);

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

        /// <summary>
        /// Set parameter width,height,length for instance
        /// </summary>
        protected void SetParameterDimensionInstanceI(Element element, double? width, double? height, double? length)
        {
            if (element != null)
            {
                if (width != null && width > 0)
                    UtilsParameter.SetValueAllParameterName(element, "幅1", width);

                if (_pipingSPType.Name.Contains("設備サポート_I形")
                    && height != null
                    && height > 0)
                    UtilsParameter.SetValueAllParameterName(element, "長さ2", height);

                if (length != null && length > 0)
                    UtilsParameter.SetValueAllParameterName(element, "長さ1", length);
                _doc.Regenerate();
            }
        }

        protected void SetParameterDimensionInstance(Element element, XYZ origin, XYZ faceNormal, Line mainLine)
        {
            if (element != null && mainLine.Length > 0)
            {
                UtilsParameter.SetValueAllParameterName(element, "長さ", mainLine.Length);
                if (element.Name.Contains("STK"))
                    UtilsParameter.SetValueAllParameterName(element, "角度", 0.0);
                else
                    UtilsParameter.SetValueAllParameterName(element, "角度", Math.PI / 2);

                _doc.Regenerate();
            }
        }

        /// <summary>
        /// Rotate instance shape I
        /// </summary>
        /// <param name="orgLine"></param>
        /// <param name="element"></param>
        protected void RotateElementI(PipingSupportPlacingData data, Element element)
        {
            Line orgLine = data.MainLine;

            XYZ orgDir = LinkTransform.OfVector(data.Direction);

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

        /// <summary>
        /// Get center line of solid rectangle box
        /// </summary>
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

        /// <summary>
        /// Move the I Shape instance to the origin location. Take the midpoint of the I-shaped line as a landmark
        /// </summary>
        protected void MoveInstance_IAfterCreate(Solid curSolid, XYZ oldLoc, Element element)
        {
            XYZ curLoc = curSolid.ComputeCentroid();
            ElementTransformUtils.MoveElement(_doc, element.Id, oldLoc - curLoc);
            _doc.Regenerate();
        }

        #endregion Shape I

        #region Common Methods

        /// <summary>
        /// if the solid is not a recangular box of tee box return false
        /// </summary>
        protected bool ValidateInput(List<PlanarFace> planarFaces)
        {
            //face number must be 6 or 10
            if (planarFaces.Count != 6 || planarFaces.Count != 10)
                return false;
            foreach (var planarFace in planarFaces)
            {
                if (!planarFaces.Any(x => !x.Origin.IsAlmostEqualTo(planarFace.Origin) &&
                                       (RevitUtils.IsParallel(planarFace.FaceNormal, x.FaceNormal) || Math.Round(planarFace.FaceNormal.DotProduct(x.FaceNormal), 5) == 0)))
                { return false; }
            }
            return true;
        }

        /// <summary>
        /// Create solid by face with normal and origin
        /// </summary>
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

                //DrawLine(l0);
                //DrawLine(l1);
                //DrawLine(l2);
                //DrawLine(l3);

                CurveLoop loop = CurveLoop.Create(new List<Curve>() { l0, l1, l2, l3 });
                return GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop>() { loop }, normal.Negate(), 10);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Is planar face dupplicate
        /// </summary>
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

        /// <summary>
        /// Create a relative center point of a planar face by averaging its vetices
        /// </summary>
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

        protected FamilyInstance CreateInstanceByDirectShape(XYZ faceOrigin, XYZ faceNormal, Line mainLine, XYZ directionInstance)
        {
            if (directionInstance != null)
            {
                XYZ point = LinkTransform.OfPoint(faceOrigin);

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
                    FamilyInstance instance = _doc.Create.NewFamilyInstance(planarPut, location, directionInstance, _pipingSPType);
                    // remove the temporary direct shape
                    _doc.Delete(directShape.Id);

                    return instance;
                }
            }

            return null;
        }

        private XYZ GetNormalForTempdirectShape(XYZ mainLineDir, XYZ mainLineOrg)
        {
            Plane plane = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, mainLineOrg);
            XYZ point = mainLineOrg + mainLineDir * 10;
            XYZ projected = RevitUtils.ProjectOnto(plane, point);
            XYZ vector = projected - mainLineOrg;

            XYZ tempNormal = vector.CrossProduct(mainLineDir);
            XYZ normal = mainLineDir.CrossProduct(tempNormal).Normalize();
            return normal;
        }

        #endregion Common Methods
    }
}