using Autodesk.Revit.DB;
using System.Collections.Generic;
using TepscoIFCToRevit.Data.GeometryDatas;

namespace TepscoIFCToRevit.Geometry.CenterLineFactory
{
    public abstract class GeometryFactory
    {
        protected Document _doc;

        public GeometryFactory(Document doc)
        {
            _doc = doc;
        }

        public abstract List<Line> GetCenterLine(GeometryData geometry);
    }
}