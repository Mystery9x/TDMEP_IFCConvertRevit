using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using RevitUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using TepscoIFCToRevit.Common;
using TepscoIFCToRevit.Data.GeometryDatas;
using TepscoIFCToRevit.Data.MEPData;
using TepscoIFCToRevit.UI;
using Revit_Mesh = Autodesk.Revit.DB.Mesh;

namespace TepscoIFCToRevit.Data.RailingsData
{
    public class RailingData : BindableBase
    {
        protected UIDocument _uiDoc = null;
        protected Document _doc = null;
        protected RevitLinkInstance _linkInstance = null;
        protected LinkElementData _linkEleData = null;

        public RevitLinkInstance LinkInstance
        {
            get => _linkInstance;
            set => SetProperty(ref _linkInstance, value);
        }

        public Transform LinkTransform => LinkInstance?.GetTotalTransform();

        public LinkElementData LinkEleData
        {
            get => _linkEleData;
            set => SetProperty(ref _linkEleData, value);
        }

        public ElementId PipeTypeId { get; set; }

        public ElementId DuctTypeId { get; set; }

        public PipeData PipeDataConvert { get; set; }

        public DuctData DuctDataConvert { get; set; }

        public RaillingType RaillingType { get; set; }

        public List<GeometryObject> Geometries { get; set; }

        public ConvertParamData ParameterData { get; set; }

        public List<Element> ConvertedElements { get; set; } = new List<Element>();

        public RailingData(UIDocument uIDocument,
                           LinkElementData linkEleData,
                           RevitLinkInstance revLnkIns,
                           RaillingType raillingType,
                           ElementId pipeTypeID = null,
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
                RaillingType = raillingType;
                PipeTypeId = pipeTypeID;
                ParameterData = parameterData;

                Geometries = GeometryUtils.GetIfcGeometriess(LinkEleData.LinkElement);
            }
        }

        public Element Initialize(GeometryObject geo)
        {
            Element converted = null;
            switch (this.RaillingType)
            {
                case RaillingType.Auto:
                {
                    converted = CreateDuct(geo, true);

                    if (converted?.IsValidObject != true)
                        converted = CreatePipe(geo, true);
                    break;
                }
                case RaillingType.Pipe:
                {
                    converted = CreatePipe(geo, true);
                    break;
                }
                case RaillingType.Duct:
                {
                    converted = CreateDuct(geo, true);
                    break;
                }
                case RaillingType.ModelInPlace:
                {
                    converted = CreateModelInplace(geo);
                    break;
                }
            }

            if (converted?.IsValidObject == true)
            {
                using (Transaction tr = new Transaction(_doc))
                {
                    try
                    {
                        tr.Start("Create Parameter");
                        SetSystemTypeForPipeDuct(_doc, converted);
                        RevitUtils.SetValueParamterConvert(_uiDoc, converted, LinkEleData, ParameterData, true);
                        tr.Commit();
                    }
                    catch (Exception)
                    {
                        if (tr.HasStarted())
                        {
                            tr.RollBack();
                        }
                    }
                }

                ConvertedElements.Add(converted);
            }

            return converted;
        }

        #region Elbow

        public Element CreateElbow(List<MEPCurve> mepConnects, GeometryObject geo, Transform linkTransform)
        {
            // get pipe filter

            if (geo is Solid solidIFC)
            {
                if ((mepConnects?.Count == 1 && ValidEndElbow(mepConnects[0], solidIFC, linkTransform)) ||
                    (mepConnects?.Count == 2
                     && mepConnects[0].Location is LocationCurve lcCurve0
                     && lcCurve0.Curve is Line lcLine0
                     && mepConnects[1].Location is LocationCurve lcCurve1
                     && lcCurve1.Curve is Line lcLine1
                     && !UtilsCurve.IsLineStraight(lcLine0, lcLine1)))
                    return ElbowRaillingData.CreateElbow(_doc, mepConnects, LinkEleData.LinkElement, LinkTransform, solidIFC);
            }

            return null;
        }

        private bool ValidEndElbow(MEPCurve mepCurve, Solid solidIFC, Transform linkTransform)
        {
            try
            {
                BoundingBoxXYZ boxSolid = solidIFC.GetBoundingBox();
                if (boxSolid != null && mepCurve?.Location is LocationCurve lcCurve && lcCurve.Curve is Line lcLine && lcLine.IsBound)
                {
                    Line lineCheck = Line.CreateUnbound(lcLine.Origin, lcLine.Direction);
                    XYZ mid = (boxSolid.Min + boxSolid.Max) / 2;
                    if (linkTransform != null)
                    {
                        mid = linkTransform.OfPoint(mid);
                    }
                    mid = lineCheck.Project(mid).XYZPoint;

                    return !RevitUtils.IsBetween(mid, lcLine.GetEndPoint(0), lcLine.GetEndPoint(1));
                }
            }
            catch (Exception) { }

            return false;
        }

        #endregion Elbow

        #region Tee

        public Element CreateTee(List<MEPCurve> mepConnects, GeometryObject geo)
        {
            // get pipe filter

            if (geo is Solid solidIFC)
            {
                if (mepConnects?.Count == 3
                   && mepConnects[0].Location is LocationCurve lcCurve0
                   && lcCurve0.Curve is Line lcLine0

                   && mepConnects[1].Location is LocationCurve lcCurve1
                   && lcCurve1.Curve is Line lcLine1

                   && mepConnects[2].Location is LocationCurve lcCurve2
                   && lcCurve2.Curve is Line lcLine2
                   && ValidCreateTee(mepConnects, new List<Line> { lcLine0, lcLine1, lcLine2 }))
                    return TeeRailingsData.CreateTeeFitting(_doc,
                                                            mepConnects,
                                                            LinkEleData.LinkElement,
                                                            LinkTransform,
                                                         new List<Solid> { solidIFC });
            }

            return null;
        }

        public bool ValidCreateTee(List<MEPCurve> pipes, List<Line> lcLines)
        {
            Line line0 = lcLines[0];
            Line line1 = lcLines[1];
            Line line2 = lcLines[2];

            XYZ direction0 = line1.Direction;
            XYZ direction1 = line1.Direction;
            XYZ direction2 = line2.Direction;

            List<Solid> solidTees = pipes.Select(x => UtilsSolid.GetTotalSolid(x))
                                         .Where(x => RevitUtils.IsGreaterThan(x.Volume, 0))
                                         .ToList();

            for (int i = 0; i < solidTees.Count - 1; i++)
            {
                for (int j = i + 1; j < solidTees.Count; j++)
                {
                    try
                    {
                        Solid interSolid = BooleanOperationsUtils.ExecuteBooleanOperation(solidTees[i], solidTees[j], BooleanOperationsType.Intersect);
                        if (!RevitUtils.IsEqual(Math.Abs(interSolid.Volume), 0))
                            return false;
                    }
                    catch (Exception)
                    { }
                }
            }

            // check case pipe touch pipes other but not intersect
            if (!GeometryUtils.IsNotTouchSolids(line0, line1, line2))
                return false;

            // Check whether the fitting geometry is a T, Y or not
            for (int i = 0; i < pipes.Count; i++)
            {
                if (i == 0
                    && RevitUtils.IsLineStraight(line1, line2, 0.03))
                    return CheckShapeOFTee(direction0, direction1, direction2);

                if (i == 1
                    && RevitUtils.IsLineStraight(line0, line2, 0.03))
                    return CheckShapeOFTee(direction1, direction0, direction2);

                if (i == 2
                    && RevitUtils.IsLineStraight(line0, line1, 0.03))
                    return CheckShapeOFTee(direction2, direction0, direction1);
            }
            return false;
        }

        private bool CheckShapeOFTee(XYZ directionBranch, XYZ directionMain1, XYZ directionMain2)
        {
            // shape T
            if (RevitUtils.IsPerpendicular(directionBranch, directionMain1, 0.03)
                && RevitUtils.IsPerpendicular(directionBranch, directionMain2, 0.03))
                return true;

            // shap Y

            XYZ negateDirection;
            if (RevitUtils.IsEqual(directionMain1, directionMain2, 0.03))
                negateDirection = directionMain2.Negate();
            else
                negateDirection = directionMain2;

            double ange = directionMain1.AngleTo(directionBranch);
            double ange1 = negateDirection.AngleTo(directionBranch);

            if (IsAngleQuater(ange)
             || IsAngleQuater(ange1))
                return false;
            else if ((RevitUtils.IsGreaterThan(ange, 0) && RevitUtils.IsLessThan(ange, Math.PI / 2)
                  && RevitUtils.IsGreaterThan(ange1, Math.PI / 2) && RevitUtils.IsLessThan(ange1, Math.PI)
              || (RevitUtils.IsGreaterThan(ange, Math.PI / 2) && RevitUtils.IsLessThan(ange, Math.PI)
                  && RevitUtils.IsGreaterThan(ange1, 0) && RevitUtils.IsLessThan(ange1, Math.PI / 2))))
                return true;

            return false;
        }

        private bool IsAngleQuater(double ange)
        {
            if (RevitUtils.IsEqual(ange, 0, 0.02)
                           || RevitUtils.IsEqual(ange, Math.PI / 2, 0.02)
                           || RevitUtils.IsEqual(ange, 2 * Math.PI / 3, 0.02)
                           || RevitUtils.IsEqual(ange, 2 * Math.PI, 0.02))
                return true;
            return false;
        }

        #endregion Tee

        #region CrossFit

        public Element CreateCrossFit(List<MEPCurve> mepConnects, GeometryObject geo)
        {
            // get pipe filter

            if (geo is Solid solidIFC)
            {
                if (mepConnects?.Count == 4
                   && mepConnects[0].Location is LocationCurve lcCurve0
                   && lcCurve0.Curve is Line lcLine0

                   && mepConnects[1].Location is LocationCurve lcCurve1
                   && lcCurve1.Curve is Line lcLine1

                   && mepConnects[2].Location is LocationCurve lcCurve2
                   && lcCurve2.Curve is Line lcLine2

                   && mepConnects[3].Location is LocationCurve lcCurve3
                   && lcCurve3.Curve is Line lcLine3

                   && CheckCrossfitValid(new List<Line> { lcLine0, lcLine1, lcLine2, lcLine3 }))
                    return CrossRailingData.CreateCrossRoad(_doc,
                                                      LinkEleData.LinkElement,
                                                      mepConnects,
                                                      LinkTransform,
                                                      solidIFC);
            }

            return null;
        }

        public bool CheckCrossfitValid(List<Line> lineConnects)
        {
            try
            {
                List<Line> pair1 = new List<Line>();
                List<Line> pair2 = new List<Line>();
                // check has two pair parallel and straight
                bool isBreak = false;
                for (int i = 0; i < lineConnects.Count; i++)
                {
                    if (isBreak)
                        break;

                    Line lineI = lineConnects[i];
                    for (int j = 0; j < lineConnects.Count; j++)
                    {
                        if (j != i)
                        {
                            Line lineJ = lineConnects[j];
                            if (UtilsCurve.IsLineStraight(lineI, lineJ))
                            {
                                pair1.Add(lineI);
                                pair1.Add(lineJ);

                                List<Line> remainLines = lineConnects.Except(pair1).ToList();
                                if (UtilsCurve.IsLineStraight(remainLines[0], remainLines[1]))
                                {
                                    pair2.Add(remainLines[0]);
                                    pair2.Add(remainLines[1]);

                                    if (pair1.Count == 2
                                        && pair2.Count == 2)
                                    {
                                        isBreak = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                // check instersector point has cross fit
                if (pair1.Count == 2
                   && pair2.Count == 2)
                {
                    Line line0 = pair1[0];
                    Line line1 = pair2[0];

                    XYZ normalPlane = line0.Direction.CrossProduct(line1.Direction);
                    Plane plane = Plane.CreateByNormalAndOrigin(normalPlane, line0.GetEndPoint(0));

                    XYZ projectPoint = RevitUtils.ProjectOnto(plane, line1.GetEndPoint(0));

                    Line lineUnbound0 = Line.CreateUnbound(line0.GetEndPoint(0), line0.Direction);
                    Line lineUnbound1 = Line.CreateUnbound(projectPoint, line1.Direction);

                    SetComparisonResult result = lineUnbound0.Intersect(lineUnbound1, out IntersectionResultArray resultArray);

                    if (result != SetComparisonResult.Disjoint)
                    {
                        var intersection = resultArray.Cast<IntersectionResult>().First();
                        XYZ interSecPoint = intersection.XYZPoint;

                        XYZ project0 = RevitUtils.ProjectOnto(plane, pair1[1].GetEndPoint(0));
                        XYZ project1 = RevitUtils.ProjectOnto(plane, pair1[1].GetEndPoint(1));
                        XYZ start = pair1[0].GetEndPoint(0);
                        XYZ end = pair1[0].GetEndPoint(1);

                        if (project0.DistanceTo(start) < RevitUtils.MIN_LENGTH ||
                            project0.DistanceTo(end) < RevitUtils.MIN_LENGTH ||
                            project1.DistanceTo(start) < RevitUtils.MIN_LENGTH ||
                            project1.DistanceTo(end) < RevitUtils.MIN_LENGTH)
                        {
                            return false;
                        }
                        Line lineMerge0 = Line.CreateBound(project0, start);
                        Line lineMerge1 = Line.CreateBound(project0, end);
                        Line lineMerge2 = Line.CreateBound(project1, start);
                        Line lineMerge3 = Line.CreateBound(project1, end);
                        List<Line> lines = new List<Line>() { lineMerge0, lineMerge1, lineMerge2, lineMerge3 }.OrderBy(line => line.Length).ToList();

                        XYZ min = lines.Last().GetEndPoint(0);
                        XYZ max = lines.Last().GetEndPoint(1);

                        if (interSecPoint.DistanceTo(min) < RevitUtils.MIN_LENGTH ||
                            interSecPoint.DistanceTo(max) < RevitUtils.MIN_LENGTH)
                        {
                            return false;
                        }
                        Line intersec1 = Line.CreateBound(interSecPoint, min);
                        Line intersec2 = Line.CreateBound(interSecPoint, max);

                        if (RevitUtils.IsEqual(intersec1.Length + intersec2.Length, lines.Last().Length))
                            return true;
                    }
                }
            }
            catch (Exception) { }

            return false;
        }

        #endregion CrossFit

        #region Convert Railling

        public Element CreateModelInplace(GeometryObject geo = null)
        {
            DirectShape directShape = null;

            if (geo != null)
            {
                using (Transaction tr = new Transaction(_doc, "CreatePipe"))
                {
                    try
                    {
                        tr.Start();
                        List<GeometryObject> geoSolids = new List<GeometryObject>();
                        if (geo is Mesh mesh)
                        {
                            geoSolids = CreateSolidFromMesh(mesh);
                        }
                        else
                            geoSolids.Add(geo);

                        DirectShapeLibrary directShapeLibrary = DirectShapeLibrary.GetDirectShapeLibrary(_doc);
                        DirectShapeType directShapeType = DirectShapeType.Create(_doc, "Railling", new ElementId(BuiltInCategory.OST_StairsRailing));
                        directShapeType.SetShape(geoSolids);

                        directShapeLibrary.AddDefinitionType("Railling", directShapeType.Id);
                        directShape = DirectShape.CreateElementInstance(_doc, directShapeType.Id, directShapeType.Category.Id, "Railling", LinkTransform);
                        tr.Commit();
                    }
                    catch (Exception)
                    {
                        if (tr.HasStarted())
                        {
                            tr.RollBack();
                        }
                    }
                }
            }

            return directShape;
        }

        private Element CreatePipe(GeometryObject geo = null, bool isRailing = false)
        {
            PipeDataConvert = new PipeData(_uiDoc, LinkEleData, PipeTypeId, LinkInstance);

            using (Transaction tr = new Transaction(_doc, "CreatePipe"))
            {
                tr.Start();

                List<GeometryObject> geoObj = new List<GeometryObject> { geo };
                PipeDataConvert.GetGeometryFromIFCElement(geoObj, isRailing);
                PipeDataConvert.Initialize();

                tr.Commit();
            }

            return PipeDataConvert.ConvertElem;
        }

        private Element CreateDuct(GeometryObject geo = null, bool isRailing = false)
        {
            DuctDataConvert = new DuctData(_uiDoc, LinkEleData, DuctTypeId, LinkInstance);

            using (Transaction tr = new Transaction(_doc, "CreateDuct"))
            {
                tr.Start();
                FailureHandlingOptions fhOpts = tr.GetFailureHandlingOptions();

                List<GeometryObject> geoObj = new List<GeometryObject> { geo };

                DuctDataConvert.GetGeometryFromIFCElement(geoObj, isRailing);
                if (DuctDataConvert.Location != null)
                {
                    if (!DuctDataConvert.IFCdata.IsCircle)
                        DuctDataConvert.TypeId = GetDuctRectangleType().Id;
                    else
                        DuctDataConvert.TypeId = GetDuctCircleType().Id;

                    DuctDataConvert.Initialize();
                    if (DuctDataConvert.ConvertElem != null)
                    {
                        REVWarning2 supWarning = new REVWarning2();
                        fhOpts.SetFailuresPreprocessor(supWarning);
                        tr.SetFailureHandlingOptions(fhOpts);
                    }
                    tr.Commit(fhOpts);

                    if (DuctDataConvert.ConvertElem != null)
                    {
                        double angle = RevitUtils.GetRotateFacingElement(DuctDataConvert.ConvertElem, DuctDataConvert.Location, DuctDataConvert.IFCdata, DuctDataConvert.LinkInstance);
                        if (!RevitUtils.IsEqual(angle, 0) && !RevitUtils.IsEqual(angle, Math.PI))
                        {
                            tr.Start("ROTATE");
                            ElementTransformUtils.RotateElement(_doc, DuctDataConvert.ConvertElem.Id, DuctDataConvert.Location, angle);
                            tr.Commit();
                        }
                    }
                }
            }

            return DuctDataConvert.ConvertElem;
        }

        private DuctType GetDuctRectangleType()
        {
            return new FilteredElementCollector(App._UIDoc.Document)
                                              .OfClass(typeof(DuctType))
                                              .Cast<DuctType>()
                                              .FirstOrDefault(x => x.Shape == ConnectorProfileType.Rectangular);
        }

        private DuctType GetDuctCircleType()
        {
            return new FilteredElementCollector(App._UIDoc.Document)
                                              .OfClass(typeof(DuctType))
                                              .Cast<DuctType>()
                                              .FirstOrDefault(x => x.Shape == ConnectorProfileType.Round);
        }

        #endregion Convert Railling

        private List<GeometryObject> CreateSolidFromMesh(Revit_Mesh mesh)
        {
            List<GeometryObject> geos = new List<GeometryObject>();
            try
            {
                TessellatedShapeBuilder builder = new TessellatedShapeBuilder();
                builder.OpenConnectedFaceSet(true);
                for (int i = 0; i < mesh.NumTriangles; i++)
                {
                    var triangle = mesh.get_Triangle(i);

                    List<XYZ> faceVertices = new List<XYZ>()
                {
                triangle.get_Vertex(0),
                triangle.get_Vertex(1),
                triangle.get_Vertex(2)
                };

                    TessellatedFace face = new TessellatedFace(faceVertices, ElementId.InvalidElementId);
                    builder.AddFace(face);
                }

                builder.CloseConnectedFaceSet();
                builder.Target = TessellatedShapeBuilderTarget.Solid;
                builder.Fallback = TessellatedShapeBuilderFallback.Abort;
                builder.Build();
                var buildResult = builder.GetBuildResult();

                if (buildResult != null
                 && buildResult.IsValidObject
                  && buildResult.Outcome == TessellatedShapeBuilderOutcome.Solid)
                {
                    geos = buildResult.GetGeometricalObjects().ToList();
                }
            }
            catch (Exception)
            {
                geos = new List<GeometryObject>() { mesh };
            }

            return geos;
        }

        public void SetSystemTypeForPipeDuct(Document doc, Element element)
        {
            if (element == null)
                return;

            List<Material> lstMaterial = new FilteredElementCollector(doc)
                                       .OfClass(typeof(Material))
                                       .Cast<Material>()
                                       .ToList();

            try
            {
                if (element is Pipe pipe)
                {
                    List<PipingSystemType> lstPipingSystem = new FilteredElementCollector(doc)
                                            .OfClass(typeof(PipingSystemType))
                                            .Cast<PipingSystemType>()
                                            .ToList();

                    PipingSystemType elemSystemType = lstPipingSystem.FirstOrDefault(x => x.Name == Define.NameSystemType) ?? CreateMaterialForPipeOrDuct(doc, lstMaterial, element) as PipingSystemType;
                    pipe.SetSystemType(elemSystemType.Id);
                }
                else if (element is Duct duct)
                {
                    List<MechanicalSystemType> lstDuctSystem = new FilteredElementCollector(doc)
                                            .OfClass(typeof(MechanicalSystemType))
                                            .Cast<MechanicalSystemType>()
                                            .ToList();

                    MechanicalSystemType mechanicalSystemType = lstDuctSystem.FirstOrDefault(x => x.Name == Define.NameSystemType) ?? CreateMaterialForPipeOrDuct(doc, lstMaterial, element) as MechanicalSystemType;
                    duct.SetSystemType(mechanicalSystemType.Id);
                }
            }
            catch (Exception) { }
        }

        private MEPSystemType CreateMaterialForPipeOrDuct(Document doc, List<Material> lstMaterial, Element elemPipeOrDuct)
        {
            PipingSystemType PipingSystemType = null;

            MechanicalSystemType MechSystemType = null;

            if (elemPipeOrDuct is Pipe pipe)
            {
                PipingSystemType = PipingSystemType.Create(doc, MEPSystemClassification.Sanitary, Define.NameSystemType);

                PipingSystemType.TwoLineRiseType = RiseDropSymbol.Outline;
                PipingSystemType.TwoLineDropType = RiseDropSymbol.YinYang;
                PipingSystemType.SingleLineBendDropType = RiseDropSymbol.BendThreeQuarterCircle;
                PipingSystemType.SingleLineBendRiseType = RiseDropSymbol.Outline;

                if (lstMaterial != null)
                {
                    Element materialExis = lstMaterial.FirstOrDefault(x => x.Name == Define.NameMaterialRailings);

                    if (materialExis == null)
                    {
                        ElementId NewMaterial = Material.Create(doc, Define.NameMaterialRailings);

                        // Set material properties
                        if (doc.GetElement(NewMaterial) is Material NewElemMaterial)
                        {
                            NewElemMaterial.Color = new Color(255, 255, 153);
                            NewElemMaterial.CutBackgroundPatternColor = new Color(120, 120, 120);
                            NewElemMaterial.CutForegroundPatternColor = new Color(120, 120, 120);
                            NewElemMaterial.SurfaceBackgroundPatternColor = new Color(120, 120, 120);
                            NewElemMaterial.SurfaceForegroundPatternColor = new Color(120, 120, 120);
                            NewElemMaterial.MaterialClass = "一般";
                            NewElemMaterial.Shininess = 64;
                            NewElemMaterial.Smoothness = 50;
                            NewElemMaterial.Transparency = 0;
                            NewElemMaterial.UseRenderAppearanceForShading = true;
                            PipingSystemType.MaterialId = NewElemMaterial.Id;
                        }
                    }
                    else
                    {
                        PipingSystemType.MaterialId = materialExis.Id;
                    }
                }

                return PipingSystemType;
            }
            else if (elemPipeOrDuct is Duct duct)
            {
                MechSystemType = MechanicalSystemType.Create(doc, MEPSystemClassification.ExhaustAir, Define.NameSystemType);

                MechSystemType.RiseDropSettings = RiseDropSymbol.Slash;

                if (lstMaterial != null)
                {
                    Element materialExis = lstMaterial.FirstOrDefault(x => x.Name == Define.NameMaterialRailings) as Material;

                    if (materialExis == null)
                    {
                        ElementId NewMaterial = Material.Create(doc, Define.NameMaterialRailings);

                        // Set material properties
                        Material NewElemMaterial = doc.GetElement(NewMaterial) as Material;
                        if (NewElemMaterial != null)
                        {
                            NewElemMaterial.Color = new Color(255, 255, 153);
                            NewElemMaterial.CutBackgroundPatternColor = new Color(120, 120, 120);
                            NewElemMaterial.CutForegroundPatternColor = new Color(120, 120, 120);
                            NewElemMaterial.SurfaceBackgroundPatternColor = new Color(120, 120, 120);
                            NewElemMaterial.SurfaceForegroundPatternColor = new Color(120, 120, 120);
                            NewElemMaterial.MaterialClass = "一般";
                            NewElemMaterial.Shininess = 64;
                            NewElemMaterial.Smoothness = 50;
                            NewElemMaterial.Transparency = 0;
                            NewElemMaterial.UseRenderAppearanceForShading = true;
                        }

                        MechSystemType.MaterialId = NewElemMaterial?.Id;
                    }
                    else
                    {
                        MechSystemType.MaterialId = materialExis.Id;
                    }
                }

                return MechSystemType;
            }
            else
                return null;
        }
    }
}