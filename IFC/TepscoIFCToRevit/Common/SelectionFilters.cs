using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace TepscoIFCToRevit.Common
{
    // classes for Revit element selection filter

    public class ColumnSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            return elem is FamilyInstance instance
                && instance.StructuralType == Autodesk.Revit.DB.Structure.StructuralType.Column;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }

    /// <summary>
    /// Filter element in revitLink
    /// </summary>
    public class GeonericModelSelectionFilter_Linked : ISelectionFilter
    {
        private Document doc = null;

        public GeonericModelSelectionFilter_Linked(Document document)
        {
            doc = document;
        }

        public bool AllowElement(Element elem)
        {
            return true;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            //return false;
            RevitLinkInstance revitlinkinstance = doc.GetElement(reference) as RevitLinkInstance;
            Autodesk.Revit.DB.Document docLink = revitlinkinstance.GetLinkDocument();
            Element eGeoMolLink = docLink.GetElement(reference.LinkedElementId);
            if (eGeoMolLink.Category.Id.IntegerValue == (int)BuiltInCategory.OST_GenericModel
                || eGeoMolLink.Category.Id.IntegerValue == (int)BuiltInCategory.OST_PipeCurves
                || eGeoMolLink.Category.Id.IntegerValue == (int)BuiltInCategory.OST_PipeFitting
                || eGeoMolLink.Category.Id.IntegerValue == (int)BuiltInCategory.OST_DuctCurves
                || eGeoMolLink.Category.Id.IntegerValue == (int)BuiltInCategory.OST_DuctFitting
                || eGeoMolLink.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Columns
                || eGeoMolLink.Category.Id.IntegerValue == (int)BuiltInCategory.OST_StructuralColumns
                || eGeoMolLink.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Floors
                || eGeoMolLink.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Walls
                || eGeoMolLink.Category.Id.IntegerValue == (int)BuiltInCategory.OST_StructuralFraming
                || eGeoMolLink.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Ceilings
                || eGeoMolLink.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Railings
                || eGeoMolLink.Category.Id.IntegerValue == (int)BuiltInCategory.OST_StairsRailing)
            {
                return true;
            }
            return false;
        }
    }
}