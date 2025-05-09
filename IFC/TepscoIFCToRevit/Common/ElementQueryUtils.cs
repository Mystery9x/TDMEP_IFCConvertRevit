using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TepscoIFCToRevit.Common
{
    public static class ElementQueryUtils
    {
        public static List<RevitLinkInstance> GetAllLinkInstances(Document doc)
        {
            return new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_RvtLinks)
                    .OfClass(typeof(RevitLinkInstance))
                    .Cast<RevitLinkInstance>()
                    .ToList();
        }

        public static List<T> GetTypeByCategory<T>(Document doc, BuiltInCategory category, Func<T, bool> condition = null)
        {
            var col = new FilteredElementCollector(doc)
                    .WhereElementIsElementType()
                    .OfCategory(category)
                    .OfClass(typeof(T))
                    .Cast<T>();

            if (condition != null)
                col = col.Where(x => condition.Invoke(x));
            return col.ToList();
        }

        public static List<string> ExtractTypeName(Document doc, string familyPath)
        {
            return doc.Application
                    .OpenDocumentFile(familyPath)
                    .FamilyManager.Types
                    .Cast<FamilyType>()
                    .Select(x => x.Name)
                    .ToList();
        }
    }
}