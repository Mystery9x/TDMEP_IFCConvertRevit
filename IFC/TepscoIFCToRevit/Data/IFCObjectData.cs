using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using TepscoIFCToRevit.Common;
using TepscoIFCToRevit.Common.ComparerUtils;

namespace TepscoIFCToRevit.Data
{
    public class IFCObjectData
    {
        #region Variable & Properties

        public UIDocument m_uIDoc = null;
        public List<RevitLinkInstance> m_revLinks = new List<RevitLinkInstance>();

        public ObservableCollection<ParameterData> KeyParameters { get; set; }
        public bool KeyFormat_Contain { get; set; }
        public bool KeyFormat_Equal { get; set; }
        public string KeyValue { get; set; }

        #endregion Variable & Properties

        #region Constructor

        public IFCObjectData(UIDocument uIDocument, List<RevitLinkInstance> revitLinkInstances, IFCObjectData iFCObjectMergeData)
        {
            m_uIDoc = uIDocument;
            m_revLinks = revitLinkInstances;
            KeyParameters = new ObservableCollection<ParameterData>();
            KeyFormat_Contain = true;
            KeyFormat_Equal = false;
            KeyValue = string.Empty;
            if (iFCObjectMergeData == null
                || iFCObjectMergeData.KeyParameters == null
                || iFCObjectMergeData.KeyParameters.Count <= 0
                || iFCObjectMergeData.m_uIDoc != uIDocument)
                Initialize();
            else
            {
                foreach (var paraData in iFCObjectMergeData.KeyParameters)
                {
                    if (paraData.ProcessParameter == null)
                        continue;

                    var cloneParaData = new ParameterData(paraData.ProcessParameter);
                    KeyParameters.Add(cloneParaData);
                }
            }
        }

        #endregion Constructor

        #region Method

        private void Initialize()
        {
            if (m_uIDoc != null && m_revLinks != null && m_revLinks.Count > 0)
            {
                HashSet<ParameterData> filterParam = new HashSet<ParameterData>();
                foreach (RevitLinkInstance revLnkIns in m_revLinks)
                {
                    var elesOfLnk = new FilteredElementCollector(revLnkIns.GetLinkDocument())
                        .WhereElementIsNotElementType()
                        .Where(item => item.Category != null && item.Category.Id.IntegerValue != (int)BuiltInCategory.OST_Viewers)
                        .Select(item => new LinkElementData(item))
                        .Where(item => (BuiltInCategory)item.LinkElement.Category?.Id.IntegerValue == BuiltInCategory.OST_PipeCurves
                                       || (BuiltInCategory)item.LinkElement.Category?.Id.IntegerValue == BuiltInCategory.OST_DuctCurves
                                       || (BuiltInCategory)item.LinkElement.Category?.Id.IntegerValue == BuiltInCategory.OST_GenericModel
                                       || (BuiltInCategory)item.LinkElement.Category?.Id.IntegerValue == BuiltInCategory.OST_PipeFitting
                                       || (BuiltInCategory)item.LinkElement.Category?.Id.IntegerValue == BuiltInCategory.OST_DuctFitting
                                       || (BuiltInCategory)item.LinkElement.Category?.Id.IntegerValue == BuiltInCategory.OST_StructuralColumns
                                       || (BuiltInCategory)item.LinkElement.Category?.Id.IntegerValue == BuiltInCategory.OST_Columns
                                       || (BuiltInCategory)item.LinkElement.Category?.Id.IntegerValue == BuiltInCategory.OST_Floors
                                       || (BuiltInCategory)item.LinkElement.Category?.Id.IntegerValue == BuiltInCategory.OST_Walls
                                       || (BuiltInCategory)item.LinkElement.Category?.Id.IntegerValue == BuiltInCategory.OST_StructuralFraming
                                       || (BuiltInCategory)item.LinkElement.Category?.Id.IntegerValue == BuiltInCategory.OST_Ceilings
                                       || (BuiltInCategory)item.LinkElement.Category?.Id.IntegerValue == BuiltInCategory.OST_Railings
                                       || (BuiltInCategory)item.LinkElement.Category?.Id.IntegerValue == BuiltInCategory.OST_StairsRailing)

                        .Select(item => item.LinkElement)
                        .GroupBy(x => x.Category.Id)
                        .Select(item => item.ToList());

                    foreach (var grp in elesOfLnk)
                    {
                        Element element = grp.FirstOrDefault();
                        if (element == null)
                            continue;

                        foreach (var para in element.GetOrderedParameters())
                        {
                            filterParam.Add(new ParameterData(para));
                        }
                        filterParam = filterParam.Where(item => item.ProcessParameter != null && !string.IsNullOrWhiteSpace(item.Name))
                                                 .Distinct(new ParameterDataComparer())
                                                 .OrderBy(item => item.Name)
                                                 .ToHashSet();
                    }
                }

                KeyParameters = new ObservableCollection<ParameterData>(filterParam);
            }
        }

        #endregion Method
    }

    public class ParameterData
    {
        public Parameter ProcessParameter { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public bool ValidParameter { get; set; }

        public override string ToString()
        {
            return Name;
        }

        public bool IsEqual(ParameterData data)
        {
            if (!string.IsNullOrWhiteSpace(Name) && !string.IsNullOrEmpty(data.Name) && Name.Equals(data.Name))
                return true;
            return false;
        }

        public ParameterData(Parameter parameter)
        {
            try
            {
                ValidParameter = true;
                ProcessParameter = parameter;
                Name = ProcessParameter.Definition != null ? ProcessParameter.Definition.Name : string.Empty;
                if (!string.IsNullOrEmpty(Name))
                    Value = GetValueParameter(ProcessParameter);

                if (string.IsNullOrEmpty(Value))
                    ValidParameter = false;
            }
            catch (System.Exception)
            {
                ValidParameter = false;
            }
        }

        public string GetValueParameter(Parameter parameter)
        {
            try
            {
                if (parameter != null)
                {
                    if (parameter.StorageType == StorageType.ElementId)
                        return parameter.AsValueString();
                    if (parameter.StorageType == StorageType.Double)
                        return parameter.AsValueString();
                    if (parameter.StorageType == StorageType.Integer)
                        return parameter.AsInteger().ToString();
                    if (parameter.StorageType == StorageType.String)
                        return parameter.AsString();

                    return parameter.AsValueString();
                }
                else
                    return string.Empty;
            }
            catch (System.Exception)
            { }
            return string.Empty;
        }
    }

    public class ConvertParamData
    {
        public ParameterData Data { get; set; }
        public string ParamInRevit { get; set; }
        public string InputValue { get; set; }

        public ConvertParamData(ParameterData data, string paramInRevit, string inputValue)
        {
            Data = data;
            ParamInRevit = paramInRevit;
            InputValue = inputValue;
        }
    }
}