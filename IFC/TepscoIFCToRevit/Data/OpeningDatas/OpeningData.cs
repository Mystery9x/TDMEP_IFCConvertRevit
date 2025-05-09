using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using TepscoIFCToRevit.Common;

namespace TepscoIFCToRevit.Data.OpeningDatas
{
    public class OpeningData : ElementConvert
    {
        public OpeningData(UIDocument uiDoc,
                           LinkElementData linkElementData,
                           ElementId typeId,
                           RevitLinkInstance revLinkIns,
                           ConvertParamData paramData = null)
        {
            if (uiDoc != null
               && linkElementData != null
               && linkElementData.LinkElement != null
               && linkElementData.LinkElement.IsValidObject
               && revLinkIns != null)
            {
                _uiDoc = uiDoc;
                _doc = _uiDoc.Document;
                LinkEleData = linkElementData;
                TypeId = typeId;
                LinkInstance = revLinkIns;
                ParameterData = paramData;
            }
        }

        public void Initialize()
        {
            try
            {
                GetGeometryFromIFCElement();
                if (Location != null)
                {
                    ConvertElem = CreateInstanceByDirectShape();
                }

                if (ConvertElem?.IsValidObject != true)
                {
                    return;
                }

                UtilsParameter.SetValueAllParameterName(ConvertElem, "高さ", Math.Round(Location.Length, 5));
                UtilsParameter.SetValueAllParameterName(ConvertElem, "半径", Math.Round(IFCdata.Length.Length, 5) / 2);
                UtilsParameter.SetValueAllParameterName(ConvertElem, "寸法a", Math.Round(IFCdata.Length.Length, 5));
                UtilsParameter.SetValueAllParameterName(ConvertElem, "寸法", 0.0);

                RevitUtils.SetValueParamterConvert(_uiDoc, ConvertElem, LinkEleData, ParameterData);
            }
            catch (System.Exception) { }
        }

        public void GetGeometryFromIFCElement(List<GeometryObject> geometries = null, bool isRailing = false)
        {
            try
            {
                IFCdata = new ElementIFC(_doc, LinkEleData.LinkElement, LinkInstance.GetLinkDocument(), ObjectIFCType.Opening, geometries, null, isRailing);
                if (IFCdata.Location != null)
                {
                    StartPoint = LinkTransform.OfPoint(IFCdata.Location.GetEndPoint(0));
                    EndPoint = LinkTransform.OfPoint(IFCdata.Location.GetEndPoint(1));
                    Location = Line.CreateBound(StartPoint, EndPoint);
                }
            }
            catch (Exception)
            { }
        }

        public FamilyInstance CreateInstanceByDirectShape()
        {
            XYZ point = StartPoint;
            XYZ direction = (StartPoint - EndPoint).Normalize();
            if (EndPoint.Z > StartPoint.Z)
            {
                point = EndPoint;
                direction = direction.Negate();
            }

            Solid mainSol = CreateSolidByFaceWithNormalAndOrigin(direction, point);
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
                                         .FirstOrDefault(x => IsPlanarFaceDupplicate(x, point, direction));
            // create the piping support instance
            XYZ centroid = mainSol.ComputeCentroid();
            XYZ location = RevitUtils.ProjectOntoFaceByOriginAndNormal(point, direction, centroid);

            if (location != null && _doc.GetElement(TypeId) is FamilySymbol symbol)
            {
                if (!symbol.IsActive)
                {
                    symbol.Activate();
                }
                FamilyInstance instance = _doc.Create.NewFamilyInstance(planarPut, location, LinkTransform.OfVector(IFCdata.Length.Direction), symbol);
                // remove the temporary direct shape
                _doc.Delete(directShape.Id);

                return instance;
            }

            return null;
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

        public void ProcessCutElement()
        {
            try
            {
                BoundingBoxXYZ box = GeometryUtils.GetBoudingBoxExtend(LinkEleData.LinkElement, LinkTransform);
                BoundingBoxIntersectsFilter boxFilter = GeometryUtils.GetBoxFilter(box);

                var filterElements = new FilteredElementCollector(_doc).WhereElementIsNotElementType().OfClass(typeof(Floor)).WherePasses(boxFilter);
                foreach (var elem in filterElements)
                {
                    if (IsCanCut(elem, ConvertElem))
                        InstanceVoidCutUtils.AddInstanceVoidCut(_doc, elem, ConvertElem);
                    else if (!JoinGeometryUtils.AreElementsJoined(_doc, ConvertElem, elem))
                        JoinGeometryUtils.JoinGeometry(_doc, ConvertElem, elem);
                }

                filterElements = new FilteredElementCollector(_doc).WhereElementIsNotElementType().OfClass(typeof(Ceiling)).WherePasses(boxFilter);
                foreach (var elem in filterElements)
                {
                    if (IsCanCut(elem, ConvertElem))
                        InstanceVoidCutUtils.AddInstanceVoidCut(_doc, elem, ConvertElem);
                    else if (!JoinGeometryUtils.AreElementsJoined(_doc, ConvertElem, elem))
                        JoinGeometryUtils.JoinGeometry(_doc, ConvertElem, elem);
                }

                filterElements = new FilteredElementCollector(_doc).WhereElementIsNotElementType().OfClass(typeof(Wall)).WherePasses(boxFilter);
                foreach (var elem in filterElements)
                {
                    if (IsCanCut(elem, ConvertElem))
                        InstanceVoidCutUtils.AddInstanceVoidCut(_doc, elem, ConvertElem);
                    else if (!JoinGeometryUtils.AreElementsJoined(_doc, ConvertElem, elem))
                        JoinGeometryUtils.JoinGeometry(_doc, ConvertElem, elem);
                }

                filterElements = new FilteredElementCollector(_doc).WhereElementIsNotElementType().OfCategory(BuiltInCategory.OST_StructuralColumns).WherePasses(boxFilter);
                foreach (var elem in filterElements)
                {
                    if (IsCanCut(elem, ConvertElem))
                        InstanceVoidCutUtils.AddInstanceVoidCut(_doc, elem, ConvertElem);
                    else if (!JoinGeometryUtils.AreElementsJoined(_doc, ConvertElem, elem))
                        JoinGeometryUtils.JoinGeometry(_doc, ConvertElem, elem);
                }

                filterElements = new FilteredElementCollector(_doc).WhereElementIsNotElementType().OfCategory(BuiltInCategory.OST_StructuralColumns).WherePasses(boxFilter);
                foreach (var elem in filterElements)
                {
                    if (IsCanCut(elem, ConvertElem))
                        InstanceVoidCutUtils.AddInstanceVoidCut(_doc, elem, ConvertElem);
                    else if (!JoinGeometryUtils.AreElementsJoined(_doc, ConvertElem, elem))
                        JoinGeometryUtils.JoinGeometry(_doc, ConvertElem, elem);
                }

                filterElements = new FilteredElementCollector(_doc).WhereElementIsNotElementType().OfCategory(BuiltInCategory.OST_StructuralFoundation).WherePasses(boxFilter);
                foreach (var elem in filterElements)
                {
                    if (IsCanCut(elem, ConvertElem))
                        InstanceVoidCutUtils.AddInstanceVoidCut(_doc, elem, ConvertElem);
                    else if (!JoinGeometryUtils.AreElementsJoined(_doc, ConvertElem, elem))
                        JoinGeometryUtils.JoinGeometry(_doc, ConvertElem, elem);
                }

                filterElements = new FilteredElementCollector(_doc).WhereElementIsNotElementType().OfCategory(BuiltInCategory.OST_StructuralFraming).WherePasses(boxFilter);
                foreach (var elem in filterElements)
                {
                    if (IsCanCut(elem, ConvertElem))
                        InstanceVoidCutUtils.AddInstanceVoidCut(_doc, elem, ConvertElem);
                    else if (!JoinGeometryUtils.AreElementsJoined(_doc, ConvertElem, elem))
                        JoinGeometryUtils.JoinGeometry(_doc, ConvertElem, elem);
                }
            }
            catch (Exception) { }
        }

        private bool IsCanCut(Element orgElem, Element opening)
        {
            return InstanceVoidCutUtils.CanBeCutWithVoid(orgElem) && InstanceVoidCutUtils.IsVoidInstanceCuttingElement(opening);
        }
    }
}