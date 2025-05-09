using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using TepscoIFCToRevit.Command;
using TepscoIFCToRevit.Data;
using TepscoIFCToRevit.UI.ViewModels.VMMappingSetting;

namespace TepscoIFCToRevit.UI.ViewModels
{
    public class VMSettingIfc : BindableBase
    {
        #region Variable & Properties

        public VMSettingGroup GroupParent { get; set; }

        private IFCObjectData m_model = null;

        public ObservableCollection<ParameterData> Parameters
        {
            get => m_model.KeyParameters;
            set
            {
                m_model.KeyParameters = value;
                OnPropertyChanged(nameof(Parameters));
            }
        }

        private ParameterData m_selParaKey;

        public ParameterData SelParaKey
        {
            get => m_selParaKey;
            set
            {
                m_selParaKey = value;
                OnPropertyChanged(nameof(SelParaKey));
            }
        }

        private string m_beginSelParaKey = string.Empty;
        public string BeginSelParaKey { get => m_beginSelParaKey; set => m_beginSelParaKey = value; }

        public bool KeyFormat_Contain
        {
            get => m_model.KeyFormat_Contain;
            set
            {
                m_model.KeyFormat_Contain = value;
                OnPropertyChanged(nameof(KeyFormat_Contain));
            }
        }

        public bool KeyFormat_Equal
        {
            get => m_model.KeyFormat_Equal;
            set
            {
                m_model.KeyFormat_Equal = value;
                OnPropertyChanged(nameof(KeyFormat_Equal));
            }
        }

        public string KeyValue
        {
            get => m_model.KeyValue;
            set
            {
                m_model.KeyValue = value;
                OnPropertyChanged(nameof(KeyValue));
            }
        }

        private List<Reference> _pickObject;

        public List<Reference> PickObject
        {
            get => _pickObject;
            set => SetProperty(ref _pickObject, value);
        }

        private string _countElementSelected;

        public string CountElementSelected
        {
            get => _countElementSelected;
            set => SetProperty(ref _countElementSelected, value);
        }

        public ICommand SelectElementCommand { get; set; }

        #endregion Variable & Properties

        #region Constructor

        public VMSettingIfc(IFCObjectData model, VMSettingGroup groupParent)
        {
            m_model = model;
            SelParaKey = Parameters.FirstOrDefault();
            GroupParent = groupParent;

            CountElementSelected = PickObject?.Count > 0 ? Define.LABLE_COUNT_ELEMENT_SELECTED + PickObject.Count : Define.LABLE_COUNT_ELEMENT_SELECTED + 0;
            SelectElementCommand = new RelayCommand<object>(SelectElementCommandInvoke);
        }

        private void SelectElementCommandInvoke(object obj)
        {
            try
            {
                if (MappingSettingCommand.dlg is Window wDow)
                {
                    wDow.Hide();

                    PickObject = App._UIDoc.Selection.PickObjects(ObjectType.LinkedElement, "Pick Link Element").ToList();

                    Document doc = App._UIDoc.Document;
                    GroupParent.LstSelectElemId = GetLinkToElementId(PickObject, doc);
                    CountElementSelected = Define.LABLE_COUNT_ELEMENT_SELECTED + PickObject.Count;
                }
            }
            catch (Exception) { }
            finally
            {
                if (MappingSettingCommand.dlg is Window wDow)
                {
                    wDow.ShowDialog();
                }
            }
        }

        private List<string> GetLinkToElementId(List<Reference> lstRef, Document doc)
        {
            List<string> lstLinkId = new List<string>();

            if (lstRef.Count == 0)
                return lstLinkId;

            foreach (var reference in lstRef)
            {
                RevitLinkInstance revitLinkInstance = doc.GetElement(reference.ElementId) as RevitLinkInstance;
                Document docLink = revitLinkInstance.GetLinkDocument();

                if (docLink != null)
                {
                    Element linkedelement = docLink.GetElement(reference.LinkedElementId);

                    if (linkedelement != null)
                    {
                        LinkElementData linkElementData = new LinkElementData(linkedelement);
                        lstLinkId.Add(linkElementData.LinkElement.UniqueId);
                    }
                }
            }

            return lstLinkId.Distinct().ToList();
        }

        #endregion Constructor
    }
}