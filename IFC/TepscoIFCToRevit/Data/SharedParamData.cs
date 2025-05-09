using Autodesk.Revit.DB;
using System.Collections.Generic;

namespace TepscoIFCToRevit.Data
{
    public class SharedParamData
    {
        public string ParamName { get; set; }
        public List<BuiltInCategory> Categories { get; set; }
        public object DataType { get; set; }

        public SharedParamData(string paramName, List<BuiltInCategory> categories, object dataType)
        {
            ParamName = paramName;
            Categories = categories;
            DataType = dataType;
        }
    }
}