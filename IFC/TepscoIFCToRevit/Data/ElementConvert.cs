using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Collections.Generic;

namespace TepscoIFCToRevit.Data
{
    public class ElementConvert
    {
        public UIDocument _uiDoc = null;
        public Document _doc = null;

        public RevitLinkInstance LinkInstance { get; set; }

        public Transform LinkTransform
        {
            get
            {
                if (LinkInstance != null)
                    return LinkInstance.GetTotalTransform();
                return null;
            }
        }

        public LinkElementData LinkEleData { get; set; }

        public ElementId TypeId { get; set; }

        public Element ConvertElem { get; set; }

        public XYZ StartPoint { get; set; }

        public XYZ EndPoint { get; set; }

        public ElementId LevelId { get; set; }

        public List<GeometryObject> Geometries { get; set; }

        public ElementIFC IFCdata { get; set; }

        public Line Location { get; set; }

        public ConvertParamData ParameterData { get; set; }

        public ElementConvert()
        { }
    }
}