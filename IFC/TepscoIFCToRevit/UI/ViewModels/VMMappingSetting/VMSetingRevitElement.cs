using Autodesk.Revit.DB;

namespace TepscoIFCToRevit.UI.ViewModels.VMMappingSetting
{
    public class VMSetingRevitElement : BindableBase
    {
        #region fields and properties

        private string _name;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private ElementId _id;

        public ElementId Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        private int _key;

        public int Key
        {
            get => _key;
            set => SetProperty(ref _key, value);
        }

        public int intergerIdValue;

        #endregion fields and properties

        public override string ToString()
        {
            return Name;
        }

        public VMSetingRevitElement(Element elem)
        {
            _name = elem.Name;
            _id = elem.Id;
        }

        public VMSetingRevitElement(string name, ElementId elementId)
        {
            _name = name;
            _id = elementId;
            intergerIdValue = elementId.IntegerValue;
        }

        public VMSetingRevitElement()
        {
        }
    }
}