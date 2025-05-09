using Autodesk.Revit.DB;
using System.Collections.Generic;

namespace TepscoIFCToRevit.Data
{
    public class LinkElementData
    {
        public Category LinkElementCategory { get; set; }

        public Element LinkElement { get; set; }

        public List<ParameterData> SourceParameterDatas { get; set; }

        public LinkElementData(Element linkElement)
        {
            LinkElement = linkElement;
            SourceParameterDatas = new List<ParameterData>();
            Initialize();
        }

        private void Initialize()
        {
            try
            {
                if (LinkElement == null || LinkElement is ElementType || !LinkElement.IsValidObject)
                    return;
                LinkElementCategory = LinkElement.Category;

                Document linkDocument = LinkElement.Document;
                if (linkDocument == null)
                    return;

                foreach (var para in LinkElement.GetOrderedParameters())
                {
                    ParameterData formatPara = new ParameterData(para);
                    SourceParameterDatas.Add(formatPara);
                }
            }
            catch (System.Exception)
            {
                LinkElement = null;
                SourceParameterDatas = new List<ParameterData>();
            }
        }
    }
}