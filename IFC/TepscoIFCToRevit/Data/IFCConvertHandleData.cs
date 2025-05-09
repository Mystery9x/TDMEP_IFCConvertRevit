using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using RevitUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Markup;
using TepscoIFCToRevit.Common;
using TepscoIFCToRevit.Common.ComparerUtils;
using TepscoIFCToRevit.Data.AccessoryDatas;
using TepscoIFCToRevit.Data.CableTraysData;
using TepscoIFCToRevit.Data.EquipmentData.Electrical;
using TepscoIFCToRevit.Data.EquipmentData.PipingSupport;
using TepscoIFCToRevit.Data.MEPData;
using TepscoIFCToRevit.Data.OpeningDatas;
using TepscoIFCToRevit.Data.RailingsData;
using TepscoIFCToRevit.Data.StructuralData;
using TepscoIFCToRevit.UI.ViewModels;
using TepscoIFCToRevit.UI.ViewModels.VMMappingSetting;
using TepscoIFCToRevit.UI.Views;

namespace TepscoIFCToRevit.Data
{
    public class IFCConvertHandleData
    {
        #region Property

        private readonly Document _doc = App._UIDoc.Document;

        public bool FlagSkip = false;
        public bool FlagCancel { get; set; }
        public VMSettingMain VMSettingMain { get; set; }
        public VMConvertIFCToRevit VMConvIFCMain { get; set; }

        private ProgressBarConvert _progressBar = null;
        private int _incrementValue = 0;
        private int _sumObjectConvert = 0;

        // data mep befor convert

        public List<PipeData> PipeDatasBeforeConvert = new List<PipeData>();
        public List<DuctData> DuctDatasBeforeConvert = new List<DuctData>();
        public List<PipingSupportData> PipingSpDatasBeforeConvert = new List<PipingSupportData>();
        public List<ConduitTerminalBoxData> ConduitTmnBoxDatasBeforeConvert = new List<ConduitTerminalBoxData>();
        public List<CableTrayData> CableTrayDatasBeforeConvert = new List<CableTrayData>();
        public List<AccessoryData> AccessoryDatasBeforeConvert = new List<AccessoryData>();
        public List<List<RailingData>> GroupRailingsDatasBeforeConvert = new List<List<RailingData>>();

        // data structure befor convert

        public List<ColumnData> ColumnDatasBeforeConvert = new List<ColumnData>();
        public List<ArchitectureColumnData> ArchiColumnDatasBeforeConvert = new List<ArchitectureColumnData>();
        public List<BeamData> BeamDatasBeforeConvert = new List<BeamData>();
        public List<FloorData> FloorDatasBeforeConvert = new List<FloorData>();
        public List<WallData> WallDatasBeforeConvert = new List<WallData>();
        public List<CeilingData> CeilingDatasBeforeConvert = new List<CeilingData>();
        public List<OpeningData> OpeningDatasBeforeConvert = new List<OpeningData>();

        // data mep convert

        public List<PipeData> PipeDatasConverted = new List<PipeData>();
        public List<DuctData> DuctDatasConverted = new List<DuctData>();

        public List<PipingSupportData> PipingSupportDatasConverted = new List<PipingSupportData>();
        public List<ConduitTerminalBoxData> ConduitTmnBoxDatasConverted = new List<ConduitTerminalBoxData>();
        public Dictionary<ElementId, List<Element>> RaillingConverted = new Dictionary<ElementId, List<Element>>();
        public List<CableTrayData> CableTrayDatasConverted = new List<CableTrayData>();
        public List<AccessoryData> AccessoryDatasConverted = new List<AccessoryData>();

        public List<PipeData> PipeDatasNotConverted = new List<PipeData>();
        public List<DuctData> DuctDatasNotConverted = new List<DuctData>();
        public List<PipingSupportData> PipingSupportDatasNotConverted = new List<PipingSupportData>();
        public List<ConduitTerminalBoxData> ConduitTmnBoxDatasNotConverted = new List<ConduitTerminalBoxData>();

        public List<RailingData> RaillingNotConverted = new List<RailingData>();
        public List<CableTrayData> CableTrayDatasNotConverted = new List<CableTrayData>();

        public List<AccessoryData> AccessoryDatasNotConverted = new List<AccessoryData>();

        // data structure convert

        public List<ColumnData> ColumnDatasConverted = new List<ColumnData>();
        public List<ArchitectureColumnData> ArchiColumnDatasConverted = new List<ArchitectureColumnData>();
        public List<BeamData> BeamDatasConverted = new List<BeamData>();
        public List<FloorData> FloorDatasConverted = new List<FloorData>();
        public List<WallData> WallDatasConverted = new List<WallData>();
        public List<CeilingData> CeilingDatasConverted = new List<CeilingData>();
        public List<OpeningData> OpeningDatasConverted = new List<OpeningData>();

        public List<ColumnData> ColumnDatasNotConverted = new List<ColumnData>();
        public List<ArchitectureColumnData> ArchiColumnDatasNotConverted = new List<ArchitectureColumnData>();
        public List<BeamData> BeamDatasNotConverted = new List<BeamData>();
        public List<FloorData> FloorDatasNotConverted = new List<FloorData>();
        public List<WallData> WallDatasNotConverted = new List<WallData>();
        public List<CeilingData> CeilingDatasNotConverted = new List<CeilingData>();
        public List<OpeningData> OpeningDatasNotConverted = new List<OpeningData>();

        public RevitLinkInstance LinkInstance = null;
        public List<LinkElementData> IFCElements = new List<LinkElementData>();

        #endregion Property

        public IFCConvertHandleData(VMSettingMain vMSettingMain, VMConvertIFCToRevit vMConvIFCMain, RevitLinkInstance revLnkIns)
        {
            if (vMSettingMain != null && revLnkIns != null)
            {
                VMSettingMain = vMSettingMain;
                VMConvIFCMain = vMConvIFCMain;
                LinkInstance = revLnkIns;
                Initialize();
            }
        }

        #region Method

        /// <summary>
        /// Initialize
        /// </summary>
        private void Initialize()
        {
            GetAllElementInRevitLinkInstance();
            BeforeProcess();
            Process();
        }

        /// <summary>
        /// Get All Element In Revit Link Instance
        /// </summary>
        private void GetAllElementInRevitLinkInstance()
        {
            try
            {
                Document linkDoc = LinkInstance.GetLinkDocument();
                if (linkDoc == null)
                    return;

                new FilteredElementCollector(linkDoc)
                .WhereElementIsNotElementType()
                .Where(item => item?.Category != null)
                .ForEach(item =>
                {
                    LinkElementData linkEle = new LinkElementData(item);
                    if (linkEle.SourceParameterDatas != null
                    && linkEle.SourceParameterDatas.Count > 0)
                        IFCElements.Add(linkEle);
                });
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Before Process
        /// </summary>
        private void BeforeProcess()
        {
            BeforeProcessIFCToMEP();
            BeforeProcessIFCToStructural();
        }

        /// <summary>
        /// Process
        /// </summary>
        private void Process()
        {
            int objectRailling = 0;
            if (GroupRailingsDatasBeforeConvert?.Count > 0)
            {
                foreach (List<RailingData> rallingDatas in GroupRailingsDatasBeforeConvert)
                {
                    rallingDatas.ForEach(x => objectRailling += x.Geometries.Count);
                }
            }

            // Count object convert
            _sumObjectConvert = (int)(PipeDatasBeforeConvert?.Count
                                + DuctDatasBeforeConvert?.Count
                                + PipingSpDatasBeforeConvert?.Count
                                + ColumnDatasBeforeConvert?.Count
                                + ArchiColumnDatasBeforeConvert?.Count
                                + FloorDatasBeforeConvert?.Count
                                + WallDatasBeforeConvert?.Count
                                + BeamDatasBeforeConvert?.Count
                                + CeilingDatasBeforeConvert?.Count
                                + ConduitTmnBoxDatasBeforeConvert?.Count
                                + objectRailling
                                + CableTrayDatasBeforeConvert?.Count
                                + AccessoryDatasBeforeConvert?.Count
                                + OpeningDatasBeforeConvert?.Count);

            // Initialize Progress Bar
            System.Diagnostics.Process process = System.Diagnostics.Process.GetCurrentProcess();
            IntPtr intPtr = process.MainWindowHandle;
            _progressBar = new ProgressBarConvert(LinkInstance.Name, Define.LABLE_PROCESS);
            _progressBar.prgSingle.Minimum = 1;
            _progressBar.prgSingle.Maximum = _sumObjectConvert;
            _progressBar.prgSingle.Value = 1;
            WindowInteropHelper helper = new WindowInteropHelper(_progressBar)
            {
                Owner = intPtr
            };
            _progressBar.Show();

            // Process Convert
            using (TransactionGroup grTr = new TransactionGroup(_doc, "Convert_IFC"))
            {
                try
                {
                    grTr.Start();
                    AddShareParameter(VMSettingMain);

                    ProcessIFCToMEP();
                    ProcessIFCToStructural();

                    if (FlagCancel == true)
                        grTr.RollBack();
                    else
                        grTr.Assimilate();
                }
                catch (Exception)
                {
                    grTr.RollBack();
                }
                finally
                {
                    _progressBar?.Dispose();
                }
            }
        }

        #region MEP Method

        /// <summary>
        /// Before Process IFC To MEP
        /// </summary>
        private void BeforeProcessIFCToMEP()
        {
            try
            {
                BeforeProcessIFCToMEP_Pipe();
                BeforeProcessIFCToMEP_Duct();
                BeforeProcessIFCToMEP_PipingSupport();
                BeforeProcessIFCToStructural_ConduitTmnBox();
                BeforeProcessIFCToMep_Railings();
                BeforeProcessIFCToMep_CableTray();
                BeforeProcessIFCToMep_Accessory();
            }
            catch (Exception) { }
        }

        private bool IsMatchIfcSettingCondition(LinkElementData data, List<VMSettingIfc> settingObjs)
        {
            if (data != null)
            {
                foreach (ParameterData para in data.SourceParameterDatas)
                {
                    if (!string.IsNullOrEmpty(para.Value))
                    {
                        foreach (VMSettingIfc settingIfc in settingObjs)
                        {
                            if (settingIfc.SelParaKey != null && para.Name == settingIfc.SelParaKey.Name)
                            {
                                bool isContainMatch = settingIfc.KeyFormat_Contain && para.Value.Contains(settingIfc.KeyValue);
                                bool isEqualMatch = settingIfc.KeyFormat_Equal && para.Value == settingIfc.KeyValue;

                                if (isContainMatch
                                   || isEqualMatch)
                                    return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        private bool IsMatchCategory(LinkElementData data, params BuiltInCategory[] categories)
        {
            if (data.LinkElementCategory != null && categories?.Length > 0)
            {
                return categories.Any(cat => (BuiltInCategory)data.LinkElementCategory.Id.IntegerValue == cat);
            }
            return false;
        }

        /// <summary>
        /// Before ProcessI FC To MEP_Pipe
        /// </summary>
        private void BeforeProcessIFCToMEP_Pipe()
        {
            string pipeCateName = Define.GetCategoryLabel(BuiltInCategory.OST_PipeCurves).ToUpper();
            VMConvertIFCtoRevTargetObject flag = VMConvIFCMain.TargetObjsMEP.FirstOrDefault(item => string.Equals(pipeCateName, item.Content));
            if (flag != null && flag.IsChecked == false)
                return;

            var temp = new List<LinkElementData>(IFCElements);
            VMSettingCategory vmCategory = VMSettingMain.SettingCategories.FirstOrDefault(item => item.ProcessBuiltInCategory == BuiltInCategory.OST_PipeCurves);
            if (vmCategory != null)
            {
                string parameterName = vmCategory.IsCheckedGetParamByCategory ? vmCategory.NameParameterInRevit : string.Empty;
                ConvertParamData dataParam = new ConvertParamData(vmCategory.SelParaKey, parameterName, vmCategory.ValueParameter);

                if (vmCategory.SettingGrps?.Count > 0)
                {
                    foreach (var vMSetGrp in vmCategory.SettingGrps)
                    {
                        List<VMSettingIfc> settingObjs = new List<VMSettingIfc>();
                        List<LinkElementData> filteredElements = new List<LinkElementData>();

                        if (vMSetGrp is VMSettingGroupCondition)
                        {
                            settingObjs = vMSetGrp.SettingObjs.Where(item => !string.IsNullOrWhiteSpace(item.KeyValue)).ToList();

                            if (settingObjs.Count <= 0)
                                continue;

                            filteredElements = temp.Where(e => IsMatchIfcSettingCondition(e, settingObjs)).ToList();
                            PipeDatasBeforeConvert.AddRange(filteredElements.Select(item => new PipeData(App._UIDoc, item, vMSetGrp.SelType?.Id, LinkInstance, dataParam)).ToList());
                            temp = temp.Except(filteredElements, new DefaultObjectComparer<LinkElementData>()).ToList();
                        }
                        if (vMSetGrp is VMSettingGroupSelection)
                        {
                            if (vMSetGrp.LstSelectElemId != null && vMSetGrp.LstSelectElemId.Count > 0)
                            {
                                foreach (var elementIFC in vMSetGrp.LstSelectElemId)
                                {
                                    filteredElements.AddRange(temp.Where(e => e.LinkElement.UniqueId.Equals(elementIFC)));
                                }

                                PipeDatasBeforeConvert.AddRange(filteredElements.Select(item => new PipeData(App._UIDoc, item, vMSetGrp.SelType?.Id, LinkInstance, dataParam)).ToList());
                                temp = temp.Except(filteredElements, new DefaultObjectComparer<LinkElementData>()).ToList();
                            }
                        }
                    }
                }

                if (vmCategory.IsCheckedGetEleByCategory)
                {
                    var items = temp.Where(x => IsMatchCategory(x, BuiltInCategory.OST_PipeCurves, BuiltInCategory.OST_PipeFitting))
                                    .Select(x => new PipeData(App._UIDoc, x, vmCategory.SelType?.Id, LinkInstance, dataParam));
                    PipeDatasBeforeConvert.AddRange(items);
                }
            }
        }

        /// <summary>
        /// Before Process IFC To MEP_Duct
        /// </summary>
        private void BeforeProcessIFCToMEP_Duct()
        {
            VMConvertIFCtoRevTargetObject flag = VMConvIFCMain.TargetObjsMEP.FirstOrDefault(item => string.Equals(Define.GetCategoryLabel(BuiltInCategory.OST_DuctCurves).ToUpper(), item.Content));
            if (flag != null && flag.IsChecked == false)
                return;

            var temp = new List<LinkElementData>(IFCElements);
            VMSettingCategory vmCategory = VMSettingMain.SettingCategories.FirstOrDefault(item => item.ProcessBuiltInCategory == BuiltInCategory.OST_DuctCurves);
            if (vmCategory != null)
            {
                string parameterName = vmCategory.IsCheckedGetParamByCategory ? vmCategory.NameParameterInRevit : string.Empty;
                ConvertParamData dataParam = new ConvertParamData(vmCategory.SelParaKey, parameterName, vmCategory.ValueParameter);

                if (vmCategory.SettingGrps?.Count > 0)
                {
                    foreach (var vMSetGrp in vmCategory.SettingGrps)
                    {
                        List<VMSettingIfc> settingObjs = new List<VMSettingIfc>();
                        List<LinkElementData> filteredElements = new List<LinkElementData>();

                        if (vMSetGrp is VMSettingGroupCondition)
                        {
                            settingObjs = vMSetGrp.SettingObjs.Where(item => !string.IsNullOrWhiteSpace(item.KeyValue)).ToList();
                            if (settingObjs.Count <= 0)
                                continue;

                            filteredElements = temp.Where(e => IsMatchIfcSettingCondition(e, settingObjs)).ToList();
                            DuctDatasBeforeConvert.AddRange(filteredElements.Select(item => new DuctData(App._UIDoc, item, vMSetGrp.SelType?.Id, LinkInstance, dataParam)).ToList());
                            temp = temp.Except(filteredElements, new DefaultObjectComparer<LinkElementData>()).ToList();
                        }

                        if (vMSetGrp is VMSettingGroupSelection)
                        {
                            if (vMSetGrp.LstSelectElemId != null && vMSetGrp.LstSelectElemId.Count > 0)
                            {
                                foreach (var item in vMSetGrp.LstSelectElemId)
                                {
                                    filteredElements.AddRange(temp.Where(e => e.LinkElement.UniqueId.Equals(item)));
                                }

                                DuctDatasBeforeConvert.AddRange(filteredElements.Select(item => new DuctData(App._UIDoc, item, vMSetGrp.SelType?.Id, LinkInstance, dataParam)).ToList());
                                temp = temp.Except(filteredElements, new DefaultObjectComparer<LinkElementData>()).ToList();
                            }
                        }
                    }
                }

                if (vmCategory.IsCheckedGetEleByCategory)
                {
                    List<DuctData> ductDatas = temp.Where(x => IsMatchCategory(x, BuiltInCategory.OST_DuctCurves, BuiltInCategory.OST_DuctFitting))
                                                   .Select(x => new DuctData(App._UIDoc, x, vmCategory.SelType?.Id, LinkInstance, dataParam))
                                                   .ToList();
                    DuctDatasBeforeConvert.AddRange(ductDatas);
                }
            }
        }

        private void BeforeProcessIFCToMEP_PipingSupport()
        {
            VMConvertIFCtoRevTargetObject flag = VMConvIFCMain.TargetObjsMEP.FirstOrDefault(item => string.Equals(Define.GetCategoryLabel(BuiltInCategory.OST_GenericModel), item.Content, StringComparison.OrdinalIgnoreCase));
            if (flag != null && flag.IsChecked == false)
                return;

            var temp = new List<LinkElementData>(IFCElements);
            VMSettingCategory vmCategory = VMSettingMain.SettingCategories.FirstOrDefault(item => item.ProcessBuiltInCategory == BuiltInCategory.OST_GenericModel);
            if (vmCategory != null)
            {
                string parameterName = vmCategory.IsCheckedGetParamByCategory ? vmCategory.NameParameterInRevit : string.Empty;
                ConvertParamData dataParam = new ConvertParamData(vmCategory.SelParaKey, parameterName, vmCategory.ValueParameter);

                if (vmCategory.SettingGrps?.Count > 0)
                {
                    foreach (var vMSetGrp in vmCategory.SettingGrps)
                    {
                        List<VMSettingIfc> settingObjs = new List<VMSettingIfc>();
                        List<LinkElementData> filteredElements = new List<LinkElementData>();

                        if (vMSetGrp is VMSettingGroupCondition)
                        {
                            settingObjs = vMSetGrp.SettingObjs.Where(item => !string.IsNullOrWhiteSpace(item.KeyValue)).ToList();
                            if (settingObjs.Count <= 0)
                                continue;

                            filteredElements = temp.Where(e => IsMatchIfcSettingCondition(e, settingObjs)).ToList();

                            if (vmCategory.IsCreateManual
                                && vMSetGrp.SelType != null)
                            {
                                FamilySymbol familySymbol = _doc.GetElement(vMSetGrp.SelType.Id) as FamilySymbol;
                                Family familySelect = _doc.GetElement(vMSetGrp.SettingTypeItems[0].SelectedFamily.Id) as Family;
                                PipingSpDatasBeforeConvert.AddRange(filteredElements.ConvertAll(item => new PipingSupportData(App._UIDoc,
                                                                                                                              item,
                                                                                                                              LinkInstance,
                                                                                                                              vmCategory.IsCreateManual,
                                                                                                                              familySymbol,
                                                                                                                              familySelect,
                                                                                                                              dataParam)));
                            }
                            else if (!vmCategory.IsCreateManual
                                    && vMSetGrp.SettingTypeItems[0].SelectedFamily != null)
                            {
                                Family familySelect = _doc.GetElement(vMSetGrp.SettingTypeItems[0].SelectedFamily.Id) as Family;
                                PipingSpDatasBeforeConvert.AddRange(filteredElements.ConvertAll(item => new PipingSupportData(App._UIDoc,
                                                                                                                              item,
                                                                                                                              LinkInstance,
                                                                                                                              vmCategory.IsCreateManual,
                                                                                                                              null,
                                                                                                                              familySelect,
                                                                                                                              dataParam)));
                            }

                            temp = temp.Except(filteredElements, new DefaultObjectComparer<LinkElementData>()).ToList();
                        }

                        if (vMSetGrp is VMSettingGroupSelection)
                        {
                            if (vMSetGrp.LstSelectElemId != null && vMSetGrp.LstSelectElemId.Count > 0)
                            {
                                foreach (var elementIFC in vMSetGrp.LstSelectElemId)
                                    filteredElements.AddRange(temp.Where(e => e.LinkElement.UniqueId.Equals(elementIFC)));

                                if (vmCategory.IsCreateManual
                                    && vMSetGrp.SelType != null)
                                {
                                    FamilySymbol familySymbol = _doc.GetElement(vMSetGrp.SelType.Id) as FamilySymbol;
                                    Family familySelect = _doc.GetElement(vMSetGrp.SettingTypeItems[0].SelectedFamily.Id) as Family;
                                    PipingSpDatasBeforeConvert.AddRange(filteredElements.ConvertAll(item => new PipingSupportData(App._UIDoc,
                                                                                                                                  item,
                                                                                                                                  LinkInstance,
                                                                                                                                  vmCategory.IsCreateManual,
                                                                                                                                  familySymbol,
                                                                                                                                  familySelect,
                                                                                                                                  dataParam)));
                                }
                                else if (!vmCategory.IsCreateManual
                                        && vMSetGrp.SettingTypeItems[0].SelectedFamily != null)
                                {
                                    Family familySelect = _doc.GetElement(vMSetGrp.SettingTypeItems[0].SelectedFamily.Id) as Family;
                                    PipingSpDatasBeforeConvert.AddRange(filteredElements.ConvertAll(item => new PipingSupportData(App._UIDoc,
                                                                                                                                  item,
                                                                                                                                  LinkInstance,
                                                                                                                                  vmCategory.IsCreateManual,
                                                                                                                                  null,
                                                                                                                                  familySelect,
                                                                                                                                  dataParam)));
                                }

                                temp = temp.Except(filteredElements, new DefaultObjectComparer<LinkElementData>()).ToList();
                            }
                        }
                    }
                }

                if (vmCategory.IsCheckedGetEleByCategory)
                {
                    List<LinkElementData> filteredElements_ByCat = temp.Where(x => IsMatchCategory(x, BuiltInCategory.OST_GenericModel)).ToList();

                    filteredElements_ByCat.ForEach(item =>
                    {
                        if (vmCategory.SelType != null)
                        {
                            ElementId elementId = vmCategory.SelType.Id;
                            ElementId elementIdFamily = vmCategory.SelFamily.Id;
                            FamilySymbol familySymbol = _doc.GetElement(elementId) as FamilySymbol;
                            Family family = _doc.GetElement(elementIdFamily) as Family;

                            PipingSupportData pipingSpData = new PipingSupportData(App._UIDoc, item, LinkInstance, vmCategory.IsCreateManual, familySymbol, family, dataParam);

                            PipingSpDatasBeforeConvert.Add(pipingSpData);
                        }
                    });
                }
            }
        }

        private void BeforeProcessIFCToStructural_ConduitTmnBox()
        {
            VMConvertIFCtoRevTargetObject flag = VMConvIFCMain.TargetObjsMEP.FirstOrDefault(item => string.Equals(Define.GetCategoryLabel(BuiltInCategory.OST_ElectricalEquipment).ToUpper(), item.Content));
            if (flag != null && flag.IsChecked == false)
                return;

            var temp = new List<LinkElementData>(IFCElements);
            VMSettingCategory vmCategory = VMSettingMain.SettingCategories.FirstOrDefault(item => item.ProcessBuiltInCategory == BuiltInCategory.OST_ElectricalEquipment);
            if (vmCategory != null)
            {
                string parameterName = vmCategory.IsCheckedGetParamByCategory ? vmCategory.NameParameterInRevit : string.Empty;
                ConvertParamData dataParam = new ConvertParamData(vmCategory.SelParaKey, parameterName, vmCategory.ValueParameter);

                if (vmCategory.SettingGrps?.Count > 0)
                {
                    foreach (var vMSetGrp in vmCategory.SettingGrps)
                    {
                        List<VMSettingIfc> settingObjs = new List<VMSettingIfc>();
                        List<LinkElementData> filteredElements = new List<LinkElementData>();

                        if (vMSetGrp is VMSettingGroupCondition)
                        {
                            settingObjs = vMSetGrp.SettingObjs.Where(item => !string.IsNullOrWhiteSpace(item.KeyValue)).ToList();
                            if (settingObjs.Count <= 0)
                                continue;

                            filteredElements = temp.Where(e => IsMatchIfcSettingCondition(e, settingObjs)).ToList();

                            if (vMSetGrp.SelType != null)
                            {
                                ConduitTmnBoxDatasBeforeConvert.AddRange(filteredElements.Select(item => new ConduitTerminalBoxData(App._UIDoc,
                                                                                                                                    item,
                                                                                                                                    vMSetGrp.SelType.Id,
                                                                                                                                    LinkInstance,
                                                                                                                                    dataParam))
                                                                                                                                    .Where(x => x.ValidObject)
                                                                                                                                    .ToList());
                                temp = temp.Except(filteredElements, new DefaultObjectComparer<LinkElementData>()).ToList();
                            }
                        }

                        if (vMSetGrp is VMSettingGroupSelection)
                        {
                            if (vMSetGrp.LstSelectElemId != null && vMSetGrp.SelType != null && vMSetGrp.LstSelectElemId.Count > 0)
                            {
                                foreach (var elementIFC in vMSetGrp.LstSelectElemId)
                                {
                                    filteredElements.AddRange(temp.Where(e => e.LinkElement.UniqueId.Equals(elementIFC)));
                                }

                                ConduitTmnBoxDatasBeforeConvert.AddRange(filteredElements.Select(item => new ConduitTerminalBoxData(App._UIDoc,
                                                                                                                                    item,
                                                                                                                                    vMSetGrp.SelType.Id,
                                                                                                                                    LinkInstance,
                                                                                                                                    dataParam))
                                                                                                                                    .Where(x => x.ValidObject)
                                                                                                                                    .ToList());

                                temp = temp.Except(filteredElements, new DefaultObjectComparer<LinkElementData>()).ToList();
                            }
                        }
                    }
                }

                if (vmCategory.IsCheckedGetEleByCategory)
                {
                    if (vmCategory.SelType != null)
                    {
                        List<ConduitTerminalBoxData> conduitTmnBoxDatas = temp.Where(x => IsMatchCategory(x, BuiltInCategory.OST_ElectricalEquipment))
                                                                          .Select(x => new ConduitTerminalBoxData(App._UIDoc, x, vmCategory.SelType.Id, LinkInstance, dataParam))
                                                                          .Where(x => x.ValidObject)
                                                                          .ToList();
                        ConduitTmnBoxDatasBeforeConvert.AddRange(conduitTmnBoxDatas);
                    }
                }
            }
        }

        private void BeforeProcessIFCToMep_Railings()
        {
            string RailingsCateName = Define.GetCategoryLabel(BuiltInCategory.OST_Railings).ToUpper();
            VMConvertIFCtoRevTargetObject flag = VMConvIFCMain.TargetObjsMEP.FirstOrDefault(item => string.Equals(RailingsCateName, item.Content));
            if (flag != null && flag.IsChecked == false)
                return;

            var temp = new List<LinkElementData>(IFCElements);
            VMSettingCategory vmCategory = VMSettingMain.SettingCategories.FirstOrDefault(item => item.ProcessBuiltInCategory == BuiltInCategory.OST_Railings);

            if (vmCategory != null)
            {
                if (vmCategory.SettingGrps?.Count > 0)
                {
                    string parameterName = vmCategory.IsCheckedGetParamByCategory ? vmCategory.NameParameterInRevit : string.Empty;
                    ConvertParamData dataParam = new ConvertParamData(vmCategory.SelParaKey, parameterName, vmCategory.ValueParameter);

                    foreach (var vMSetGrp in vmCategory.SettingGrps)
                    {
                        List<VMSettingIfc> settingObjs = new List<VMSettingIfc>();

                        if (vMSetGrp is VMSettingGroupCondition)
                        {
                            settingObjs = vMSetGrp.SettingObjs.Where(item => !string.IsNullOrWhiteSpace(item.KeyValue)).ToList();
                            if (settingObjs?.Count >= 0)
                            {
                                List<LinkElementData> filteredElements = temp.Where(e => IsMatchIfcSettingCondition(e, settingObjs)).ToList();

                                ElementId pipeTypeId = new FilteredElementCollector(_doc).OfClass(typeof(PipeType)).FirstOrDefault().Id;

                                if (pipeTypeId != null && vMSetGrp.SelType != null)
                                {
                                    if (Enum.TryParse(vMSetGrp.SelType.Name, out RaillingType raillingType))
                                    {
                                        List<RailingData> rallingDatas = new List<RailingData>();

                                        rallingDatas.AddRange(filteredElements.Select(item => new RailingData(App._UIDoc,
                                                                                                              item,
                                                                                                              LinkInstance,
                                                                                                              raillingType,
                                                                                                              pipeTypeId,
                                                                                                              dataParam)).ToList());
                                        if (rallingDatas.Count > 0)
                                            GroupRailingsDatasBeforeConvert.Add(rallingDatas);
                                    }

                                    temp = temp.Except(filteredElements, new DefaultObjectComparer<LinkElementData>()).ToList();
                                }
                            }
                        }
                        else
                        {
                            ElementId pipeTypeId = new FilteredElementCollector(_doc).OfClass(typeof(PipeType)).FirstOrDefault().Id;

                            if (Enum.TryParse(vMSetGrp.SelType.Name, out RaillingType raillingType))
                            {
                                List<RailingData> rallingDatas = new List<RailingData>();
                                List<LinkElementData> filteredElements = new List<LinkElementData>();

                                if (vMSetGrp.LstSelectElemId != null && vMSetGrp.LstSelectElemId.Count > 0)
                                {
                                    foreach (var elementIFC in vMSetGrp.LstSelectElemId)
                                    {
                                        //LinkElementData elementData = temp.FirstOrDefault(e => e.LinkElement.UniqueId.Equals(elementIFC.ToString()));

                                        //if (elementData != null)
                                        //{
                                        filteredElements.AddRange(temp.Where(e => e.LinkElement.UniqueId.Equals(elementIFC)));
                                        //}
                                    }

                                    rallingDatas.AddRange(filteredElements.Select(item => new RailingData(App._UIDoc,
                                                                                                          item,
                                                                                                          LinkInstance,
                                                                                                          raillingType,
                                                                                                          pipeTypeId,
                                                                                                          dataParam)).ToList());
                                    if (rallingDatas.Count > 0)
                                        GroupRailingsDatasBeforeConvert.Add(rallingDatas);
                                }

                                temp = temp.Except(filteredElements, new DefaultObjectComparer<LinkElementData>()).ToList();
                            }
                        }
                    }
                }
            }
        }

        private void BeforeProcessIFCToMep_CableTray()
        {
            string CableTrayCateName = Define.GetCategoryLabel(BuiltInCategory.OST_CableTray).ToUpper();
            VMConvertIFCtoRevTargetObject flag = VMConvIFCMain.TargetObjsMEP.FirstOrDefault(item => string.Equals(CableTrayCateName, item.Content));
            if (flag != null && flag.IsChecked == false)
                return;

            var temp = new List<LinkElementData>(IFCElements);
            VMSettingCategory vmCategory = VMSettingMain.SettingCategories.FirstOrDefault(item => item.ProcessBuiltInCategory == BuiltInCategory.OST_CableTray);

            if (vmCategory != null)
            {
                string parameterName = vmCategory.IsCheckedGetParamByCategory ? vmCategory.NameParameterInRevit : string.Empty;
                ConvertParamData dataParam = new ConvertParamData(vmCategory.SelParaKey, parameterName, vmCategory.ValueParameter);

                if (vmCategory.SettingGrps?.Count > 0)
                {
                    foreach (var vMSetGrp in vmCategory.SettingGrps)
                    {
                        List<VMSettingIfc> settingObjs = new List<VMSettingIfc>();

                        if (vMSetGrp is VMSettingGroupCondition && vMSetGrp.SelType != null)
                        {
                            settingObjs = vMSetGrp.SettingObjs.Where(item => !string.IsNullOrWhiteSpace(item.KeyValue)).ToList();

                            if (settingObjs.Count <= 0)
                                continue;

                            List<LinkElementData> filteredElements = new List<LinkElementData>();

                            filteredElements = temp.Where(e => IsMatchIfcSettingCondition(e, settingObjs)).ToList();
                            CableTrayDatasBeforeConvert.AddRange(filteredElements.Select(item => new CableTrayData(App._UIDoc,
                                                                                                                   item,
                                                                                                                   vMSetGrp.SelType.Id,
                                                                                                                   LinkInstance,
                                                                                                                   dataParam)).ToList());
                            temp = temp.Except(filteredElements, new DefaultObjectComparer<LinkElementData>()).ToList();
                        }
                        else
                        {
                            List<LinkElementData> filteredElements = new List<LinkElementData>();

                            if (vMSetGrp.LstSelectElemId != null && vMSetGrp.SelType != null && vMSetGrp.LstSelectElemId.Count > 0)
                            {
                                foreach (var elementIFC in vMSetGrp.LstSelectElemId)
                                {
                                    filteredElements.AddRange(temp.Where(e => e.LinkElement.UniqueId.Equals(elementIFC)));
                                }

                                CableTrayDatasBeforeConvert.AddRange(filteredElements.Select(item => new CableTrayData(App._UIDoc,
                                                                                                                       item,
                                                                                                                       vMSetGrp.SelType.Id,
                                                                                                                       LinkInstance,
                                                                                                                       dataParam)).ToList());
                                temp = temp.Except(filteredElements, new DefaultObjectComparer<LinkElementData>()).ToList();
                            }
                        }
                    }
                }

                if (vmCategory.IsCheckedGetEleByCategory)
                {
                    var items = temp.Where(x => IsMatchCategory(x, BuiltInCategory.OST_CableTray, BuiltInCategory.OST_CableTrayFitting))
                                    .Select(x => new CableTrayData(App._UIDoc, x, vmCategory.SelType?.Id, LinkInstance, dataParam));

                    CableTrayDatasBeforeConvert.AddRange(items);
                }
            }
        }

        private void BeforeProcessIFCToMep_Accessory()
        {
            string accessoryCateName = Define.GetCategoryLabel(BuiltInCategory.OST_PipeAccessory).ToUpper();
            VMConvertIFCtoRevTargetObject flag = VMConvIFCMain.TargetObjsMEP.FirstOrDefault(item => string.Equals(accessoryCateName, item.Content));
            if (flag != null && flag.IsChecked == false)
                return;

            var temp = new List<LinkElementData>(IFCElements);
            VMSettingCategory vmCategory = VMSettingMain.SettingCategories.FirstOrDefault(item => item.ProcessBuiltInCategory == BuiltInCategory.OST_PipeAccessory);
            if (vmCategory != null)
            {
                string parameterName = vmCategory.IsCheckedGetParamByCategory ? vmCategory.NameParameterInRevit : string.Empty;
                ConvertParamData dataParam = new ConvertParamData(vmCategory.SelParaKey, parameterName, vmCategory.ValueParameter);

                if (vmCategory.SettingGrps?.Count > 0)
                {
                    foreach (var vMSetGrp in vmCategory.SettingGrps)
                    {
                        List<VMSettingIfc> settingObjs = new List<VMSettingIfc>();
                        List<LinkElementData> filteredElements = new List<LinkElementData>();

                        if (vMSetGrp is VMSettingGroupCondition && vMSetGrp.SelType != null)
                        {
                            settingObjs = vMSetGrp.SettingObjs.Where(item => !string.IsNullOrWhiteSpace(item.KeyValue)).ToList();

                            if (settingObjs.Count <= 0)
                                continue;

                            filteredElements = temp.Where(e => IsMatchIfcSettingCondition(e, settingObjs)).ToList();
                            AccessoryDatasBeforeConvert.AddRange(filteredElements.Select(item => new AccessoryData(App._UIDoc, item, vMSetGrp.SelType.Id, LinkInstance, dataParam)).ToList());
                            temp = temp.Except(filteredElements, new DefaultObjectComparer<LinkElementData>()).ToList();
                        }
                        else if (vMSetGrp is VMSettingGroupSelection)
                        {
                            if (vMSetGrp.LstSelectElemId != null && vMSetGrp.SelType != null && vMSetGrp.LstSelectElemId.Count > 0)
                            {
                                foreach (var elementIFC in vMSetGrp.LstSelectElemId)
                                {
                                    filteredElements.AddRange(temp.Where(e => e.LinkElement.UniqueId.Equals(elementIFC)));
                                }

                                AccessoryDatasBeforeConvert.AddRange(filteredElements.Select(item => new AccessoryData(App._UIDoc, item, vMSetGrp.SelType.Id, LinkInstance, dataParam)).ToList());
                                temp = temp.Except(filteredElements, new DefaultObjectComparer<LinkElementData>()).ToList();
                            }
                        }
                    }
                }

                if (vmCategory.IsCheckedGetEleByCategory)
                {
                    var items = temp.Where(x => IsMatchCategory(x, BuiltInCategory.OST_PipeAccessory))
                                    .Select(x => new AccessoryData(App._UIDoc, x, vmCategory.SelType?.Id, LinkInstance, dataParam));
                    AccessoryDatasBeforeConvert.AddRange(items);
                }
            }
        }

        /// <summary>
        /// Process IFC To MEP
        /// </summary>
        private void ProcessIFCToMEP()
        {
            ProcessIFCToMEP_Pipe();
            ProcessIFCTo_Duct();
            ProcessIFCToMEP_PipingSupport();
            ProcessIFCToMEP_ConduitTmnBox();
            ProcessIFCTo_Railings();
            ProcessIFCTo_CableTray();
            ProcessIFCTo_Accessory();
        }

        private void ProcessIFCToMEP_Pipe()
        {
            if (PipeDatasBeforeConvert.Count <= 0
                || FlagCancel == true
                || FlagSkip == true)
                return;

            // Pipe
            List<PipeData> datasNotConverted = new List<PipeData>();
            foreach (var data in PipeDatasBeforeConvert)
            {
                FlagCancel = _progressBar.IsCancel;
                FlagSkip = _progressBar.IsSkip;
                if (FlagCancel == true || FlagSkip == true)
                    break;

                bool isSuccess = RunConvertPipeObject.ConvertForPipe(_doc, data, ref PipeDatasConverted, ref datasNotConverted);
                if (isSuccess)
                {
                    _incrementValue++;
                    string mess = string.Format("{0} / {1}{2}", _incrementValue, _sumObjectConvert, Define.MESS_PROGESSBAR_OBJECT_COVERT);
                    _progressBar.SetMessage(mess);
                    _progressBar.IncrementProgressBar();
                }
            }

            // Fitting
            List<Pipe> pipesConverted = PipeDatasConverted.Select(x => x.ConvertElem as Pipe).ToList();
            List<ElementId> idsIFCCanotConvert = datasNotConverted.Select(x => x.LinkEleData.LinkElement.Id).ToList();
            List<ElementId> junkElementsIFC = new List<ElementId>();

            foreach (PipeData data in datasNotConverted)
            {
                FlagCancel = _progressBar.IsCancel;
                FlagSkip = _progressBar.IsSkip;
                if (FlagCancel == true || FlagSkip == true)
                    break;

                _incrementValue++;
                string mess = string.Format("{0} / {1}{2}", _incrementValue, _sumObjectConvert, Define.MESS_PROGESSBAR_OBJECT_COVERT);
                _progressBar.SetMessage(mess);
                _progressBar.IncrementProgressBar();

                if (junkElementsIFC?.Count > 0 && junkElementsIFC.Any(x => x.Equals(data.LinkEleData.LinkElement.Id)))
                    continue;

                BoundingBoxXYZ boxFitting = GeometryUtils.GetBoudingBoxExtend(data.LinkEleData.LinkElement, data.LinkTransform);
                if (boxFitting == null)
                    continue;

                XYZ centerFitting = (boxFitting.Max + boxFitting.Min) / 2;
                List<Pipe> pipeFilters = GeometryUtils.FindPipeNearestBox(_doc, pipesConverted.Where(x => x != null && x.IsValidObject).Select(x => x.Id).ToList(), boxFitting);

                // create tee fitting
                _progressBar.tbxMessage.Text = Define.MESS_PROGESSBAR_CREATE_FETTING;
                var fitting = RunConvertPipeObject.CreateTeeFitting(_doc, data, pipeFilters);

                // Case : Bult-in PIPE FITTING (Create Transition Fitting)
                if (fitting?.IsValidObject != true)
                {
                    _progressBar.tbxMessage.Text = Define.MESS_PROGESSBAR_CREATE_TRANSITION_FETTING;
                    fitting = RunConvertPipeObject.CreateTransactionFitting(_doc, data, pipeFilters);
                }

                // Case elbow and elbow at the end pipe
                if (fitting?.IsValidObject != true)
                    fitting = RunConvertPipeObject.CreateElbow(_doc, LinkInstance, pipeFilters, data, boxFitting, pipesConverted);

                if (fitting?.IsValidObject != true)
                {
                    foreach (var geo in data.Geometries)
                    {
                        if (geo is Solid solidIFC)
                        {
                            List<MEPCurve> mepConnects = UtilsElbowData.GetPipeDuctPoper(_doc, data.LinkInstance, data.LinkEleData.LinkElement, solidIFC, new List<Element>(pipeFilters));

                            if (fitting?.IsValidObject != true)
                                fitting = data.CreateElbow(mepConnects, geo) as FamilyInstance;
                            else break;
                        }
                    }
                }

                // case place fitting in between pipe
                if (fitting?.IsValidObject != true
                   && RunConvertPipeObject.IsCreateTeeFitingWithTwoPipes(_doc, pipesConverted, data, out List<Pipe> pipes, out XYZ intersecPoint))
                {
                    using (TransactionGroup grTr = new TransactionGroup(_doc, "Create end fiting"))
                    {
                        grTr.Start("Create fiting");
                        PipeType pipeType = _doc.GetElement(PipeDatasConverted[0].TypeId) as PipeType;

                        List<Pipe> pipesCreateTee = CommonDataPipeDuct.SplitPipe(_doc,
                                                                                 pipes,
                                                                                 intersecPoint,
                                                                                 pipeType,
                                                                                 ref pipesConverted,
                                                                                 ref PipeDatasConverted);
                        fitting = CommonDataPipeDuct.CreatWyeFittingForPipe(_doc, pipeType, pipesCreateTee, intersecPoint);
                        if (fitting?.IsValidObject != true)
                        {
                            grTr.RollBack();
                        }
                        else
                            grTr.Commit();
                    }
                }

                if (fitting?.IsValidObject == true)
                {
                    data.IsElbow = true;
                    data.ConvertElem = fitting;
                    PipeDatasConverted.Add(data);

                    // if file ifc return multipe fitting in the same location
                    // then remove fittings invalid
                    BoundingBoxIntersectsFilter boxFilter = GeometryUtils.GetBoxFilter(boxFitting);
                    List<Element> elementIFCs = new FilteredElementCollector(LinkInstance.Document, idsIFCCanotConvert).WherePasses(boxFilter)
                                                                                                                       .Cast<Element>()
                                                                                                                       .ToList();

                    using (Transaction tr = new Transaction(_doc))
                    {
                        tr.Start("Set value parameter");
                        RevitUtils.SetValueParamterConvert(App._UIDoc, fitting, data.LinkEleData, data.ParameterData);
                        tr.Commit();
                    }

                    if (elementIFCs.Count > 0)
                    {
                        foreach (Element elmIFC in elementIFCs)
                        {
                            BoundingBoxXYZ boxIFC = GeometryUtils.GetBoudingBoxExtend(elmIFC, data.LinkTransform);

                            if (boxIFC != null)
                            {
                                XYZ centerIFC = (boxIFC.Max + boxIFC.Min) / 2;
                                double tolerance = centerIFC.DistanceTo(boxIFC.Max) / 5;
                                if (RevitUtils.IsEqual(centerFitting, centerIFC, tolerance))
                                {
                                    junkElementsIFC.Add(elmIFC.Id);
                                }
                            }
                        }
                    }
                }
                else
                {
                    PipeDatasNotConverted.Add(data);
                }
            }
        }

        private void ProcessIFCTo_Duct()
        {
            if (DuctDatasBeforeConvert.Count <= 0) return;
            if (FlagCancel == true || FlagSkip == true) return;

            // Duct
            List<DuctData> datasNotConverted = new List<DuctData>();
            foreach (var data in DuctDatasBeforeConvert)
            {
                FlagCancel = _progressBar.IsCancel;
                FlagSkip = _progressBar.IsSkip;
                if (FlagCancel == true || FlagSkip == true)
                    break;

                bool isSuccess = RunConvertDuctObject.ConvertForDuct(_doc, data, ref DuctDatasConverted, ref datasNotConverted);
                if (isSuccess)
                {
                    _incrementValue++;
                    _progressBar.tbxMessage.Text = _incrementValue.ToString() + " / " + _sumObjectConvert.ToString() + Define.MESS_PROGESSBAR_OBJECT_COVERT;
                    _progressBar.IncrementProgressBar();
                }
            }

            List<Duct> ductsConverted = DuctDatasConverted.Select(x => x.ConvertElem as Duct).ToList();
            foreach (var data in datasNotConverted)
            {
                FlagCancel = _progressBar.IsCancel;
                FlagSkip = _progressBar.IsSkip;
                if (FlagCancel == true || FlagSkip == true)
                    break;

                _incrementValue++;
                _progressBar.tbxMessage.Text = _incrementValue.ToString() + " / " + _sumObjectConvert.ToString() + Define.MESS_PROGESSBAR_OBJECT_COVERT;
                _progressBar.IncrementProgressBar();

                // get pipe filter
                BoundingBoxXYZ boxFitting = GeometryUtils.GetBoudingBoxExtend(data.LinkEleData.LinkElement, data.LinkTransform);
                if (boxFitting == null)
                    continue;

                XYZ centerFitting = (boxFitting.Max + boxFitting.Min) / 2;
                List<Duct> ductFilters = GeometryUtils.FindDuctNearestBox(_doc, ductsConverted.Where(x => x != null && x.IsValidObject).Select(x => x.Id).ToList(), boxFitting);

                // create duct fitting
                _progressBar.tbxMessage.Text = Define.MESS_PROGESSBAR_CREATE_FETTING;
                var fitting = RunConvertDuctObject.CreateDuctTeeFitting(_doc, ductFilters, data);

                // Case : Bult-in PIPE FITTING (Create Transition Fitting)
                if (fitting?.IsValidObject != true)
                {
                    _progressBar.tbxMessage.Text = Define.MESS_PROGESSBAR_CREATE_TRANSITION_FETTING;
                    fitting = RunConvertDuctObject.CreateTransactionFittingByBuilIn(_doc, ductFilters);
                }

                if (fitting?.IsValidObject != true)
                {
                    fitting = RunConvertDuctObject.ConvertElbowDuct(_doc, LinkInstance, ductFilters, ductsConverted, boxFitting, data);
                    if (fitting?.IsValidObject == true)
                    {
                        Parameter paramType = data.LinkEleData.LinkElement.GetParameters("ObjectTypeOverride")?.FirstOrDefault();
                        string typeIfcName = paramType?.AsString();
                        string typeName = fitting.Symbol.FamilyName + ":" + fitting.Symbol.Name;
                        if (!string.IsNullOrWhiteSpace(typeIfcName) && !typeName.Equals(typeIfcName))
                        {
                            foreach (ElementId symbolId in fitting.Symbol.GetSimilarTypes())
                            {
                                if (_doc.GetElement(symbolId) is FamilySymbol symbol)
                                {
                                    typeName = symbol.FamilyName + ":" + symbol.Name;
                                    if (!typeName.Equals(typeIfcName))
                                    {
                                        continue;
                                    }

                                    Transaction tr = new Transaction(_doc);
                                    tr.Start("Change type");
                                    fitting.Symbol = symbol;
                                    tr.Commit();

                                    break;
                                }
                            }
                        }
                    }
                }

                // case place fitting in between pipe
                if (fitting?.IsValidObject != true
                   && RunConvertDuctObject.IsCreateTeeFitingWithTwoDucts(_doc, ductsConverted, data, out List<Duct> ducts, out XYZ intersecPoint))
                {
                    using (TransactionGroup grTr = new TransactionGroup(_doc, "Create fiting"))
                    {
                        grTr.Start();
                        DuctType ductType = _doc.GetElement(DuctDatasConverted[0].TypeId) as DuctType;

                        List<Duct> ductsCreateTee = RunConvertDuctObject.SplitDuct(_doc,
                                                                                 ducts,
                                                                                 intersecPoint,
                                                                                 ductType,
                                                                                 ref ductsConverted,
                                                                                 ref DuctDatasConverted);

                        fitting = CommonDataPipeDuct.CreatWyeFittingForDuct(_doc, ductType, ductsCreateTee, intersecPoint);
                        if (fitting?.IsValidObject == true)
                        {
                            grTr.RollBack();
                        }
                        else
                            grTr.Commit();
                    }
                }

                if (fitting?.IsValidObject == true)
                {
                    data.IsElbow = true;
                    data.ConvertElem = fitting;
                    DuctDatasConverted.Add(data);

                    using (Transaction tr = new Transaction(_doc))
                    {
                        tr.Start("Set value parameter");
                        RevitUtils.SetValueParamterConvert(App._UIDoc, fitting, data.LinkEleData, data.ParameterData);
                        tr.Commit();
                    }
                }
                else
                {
                    DuctDatasNotConverted.Add(data);
                }
            }
        }

        private void ProcessIFCToMEP_PipingSupport()
        {
            if (PipingSpDatasBeforeConvert.Count <= 0
               || FlagCancel == true
               || FlagSkip == true)
                return;

            foreach (var data in PipingSpDatasBeforeConvert)
            {
                FlagCancel = _progressBar.IsCancel;
                FlagSkip = _progressBar.IsSkip;
                if (FlagCancel == true || FlagSkip == true)
                    break;

                using (Transaction tr = new Transaction(_doc))
                {
                    try
                    {
                        FailureHandlingOptions fhOpts = tr.GetFailureHandlingOptions();
                        REVWarning1 supWarning = new REVWarning1(true);
                        fhOpts.SetFailuresPreprocessor(supWarning);
                        tr.SetFailureHandlingOptions(fhOpts);

                        REVWarning2 supWarning2 = new REVWarning2();
                        fhOpts.SetFailuresPreprocessor(supWarning2);
                        tr.SetFailureHandlingOptions(fhOpts);

                        tr.Start("Convert");
                        data.Initialize();
                        tr.Commit();
                    }
                    catch (Exception)
                    {
                        tr.RollBack();
                    }
                }

                _incrementValue++;
                string mess = string.Format("{0} / {1}{2}", _incrementValue, _sumObjectConvert, Define.MESS_PROGESSBAR_OBJECT_COVERT);
                _progressBar.SetMessage(mess);
                _progressBar.IncrementProgressBar();

                if (data.ConvertInstances?.Count > 0)
                {
                    PipingSupportDatasConverted.Add(data);
                }
                else
                {
                    PipingSupportDatasNotConverted.Add(data);
                }
            }
        }

        private void ProcessIFCTo_Railings()
        {
            if (GroupRailingsDatasBeforeConvert.Count <= 0
               || FlagCancel == true
               || FlagSkip == true)
                return;

            using (TransactionGroup grTr = new TransactionGroup(_doc))
            {
                grTr.Start("CONVERT_Railings");

                foreach (List<RailingData> groupData in GroupRailingsDatasBeforeConvert)
                {
                    List<Element> elemConverted = new List<Element>();
                    Dictionary<RailingData, List<GeometryObject>> dataNotConvert = new Dictionary<RailingData, List<GeometryObject>>();

                    foreach (var data in groupData)
                    {
                        BuiltInCategory buid = (BuiltInCategory)data.LinkEleData.LinkElement.Category.Id.IntegerValue;

                        FlagCancel = _progressBar.IsCancel;
                        FlagSkip = _progressBar.IsSkip;
                        if (FlagCancel == true || FlagSkip == true)
                            break;

                        List<GeometryObject> geometrieNotConverts = new List<GeometryObject>();
                        foreach (GeometryObject geo in data.Geometries)
                        {
                            try
                            {
                                if (geo is Solid)
                                {
                                    var converted = data.Initialize(geo);
                                    if (converted?.IsValidObject == true)
                                    {
                                        elemConverted.Add(converted);

                                        _incrementValue++;
                                        string mess = string.Format("{0} / {1}{2}", _incrementValue, _sumObjectConvert, Define.MESS_PROGESSBAR_OBJECT_COVERT);
                                        _progressBar.SetMessage(mess);
                                        _progressBar.IncrementProgressBar();
                                    }
                                    else
                                        geometrieNotConverts.Add(geo);
                                }
                                else
                                    geometrieNotConverts.Add(geo);
                            }
                            catch (Exception)
                            {
                                if (!grTr.HasEnded())
                                    grTr.RollBack();
                            }
                        }

                        if (geometrieNotConverts.Count > 0)
                        {
                            if (dataNotConvert.ContainsKey(data))
                            {
                                dataNotConvert[data].AddRange(geometrieNotConverts);
                            }
                            else
                            {
                                dataNotConvert.Add(data, geometrieNotConverts);
                            }
                        }
                    }

                    // create fitting
                    List<Element> fittings = new List<Element>();
                    if (elemConverted?.Count > 0)
                    {
                        var data = groupData.FirstOrDefault();
                        fittings = CreateAgainFitting(data?.LinkEleData, data?.ParameterData, ref elemConverted, true);
                    }

                    foreach (var data in dataNotConvert)
                    {
                        bool isConverted = true;
                        Transform linkTransform = data.Key.LinkInstance?.GetTotalTransform();

                        foreach (var geo in data.Value)
                        {
                            try
                            {
                                Element instance = null;
                                if (elemConverted?.Count > 0
                                    && geo is Solid solidIFC)
                                {
                                    List<MEPCurve> mepConnects = UtilsElbowData.GetPipeDuctPoper(_doc, data.Key.LinkInstance, data.Key.LinkEleData.LinkElement, solidIFC, elemConverted);
                                    instance = data.Key.CreateCrossFit(mepConnects, geo);

                                    if (instance == null
                                        || !instance.IsValidObject)
                                        instance = data.Key.CreateTee(mepConnects, geo);

                                    if (instance == null
                                        || !instance.IsValidObject)
                                        instance = data.Key.CreateElbow(mepConnects, geo, linkTransform);
                                }

                                if ((instance == null
                                        || !instance.IsValidObject)
                                    && (data.Key.RaillingType == RaillingType.Auto
                                        || data.Key.RaillingType == RaillingType.ModelInPlace))
                                    instance = data.Key.CreateModelInplace(geo);

                                if (instance?.IsValidObject == true)
                                {
                                    using (Transaction tr = new Transaction(_doc))
                                    {
                                        tr.Start("Set value parameter");
                                        RevitUtils.SetValueParamterConvert(App._UIDoc, instance, data.Key.LinkEleData, data.Key.ParameterData, true);
                                        tr.Commit();
                                    }

                                    fittings.Add(instance);
                                    data.Key.ConvertedElements.Add(instance);
                                }
                                else
                                {
                                    isConverted = false;
                                }
                            }
                            catch (Exception)
                            {
                                isConverted = false;
                            }
                        }

                        if (!isConverted)
                        {
                            RaillingNotConverted.Add(data.Key);
                        }

                        _incrementValue++;
                        string mess = string.Format("{0} / {1}{2}", _incrementValue, _sumObjectConvert, Define.MESS_PROGESSBAR_OBJECT_COVERT);
                        _progressBar.SetMessage(mess);
                        _progressBar.IncrementProgressBar();
                    }

                    RevitLinkInstance linkInstance = groupData.FirstOrDefault()?.LinkInstance;
                    ElementId linkId = linkInstance?.Id;

                    if (elemConverted?.Count > 0)
                    {
                        if (RaillingConverted.ContainsKey(linkId))
                        {
                            RaillingConverted[linkId].AddRange(elemConverted);
                        }
                        else
                        {
                            RaillingConverted.Add(linkId, new List<Element>(elemConverted));
                        }
                    }
                    if (fittings?.Count > 0)
                    {
                        if (RaillingConverted.ContainsKey(linkId))
                        {
                            RaillingConverted[linkId].AddRange(fittings);
                        }
                        else
                        {
                            RaillingConverted.Add(linkId, new List<Element>(fittings));
                        }
                    }
                }

                grTr.Assimilate();
            }
        }

        private void ProcessIFCTo_CableTray()
        {
            if (CableTrayDatasBeforeConvert.Count <= 0) return;
            if (FlagCancel == true || FlagSkip == true) return;

            using (TransactionGroup grTr = new TransactionGroup(_doc))
            {
                grTr.Start("Convert cableTray");

                List<CableTrayData> datasNotConverted = new List<CableTrayData>();
                foreach (var data in CableTrayDatasBeforeConvert)
                {
                    FlagCancel = _progressBar.IsCancel;
                    FlagSkip = _progressBar.IsSkip;
                    if (FlagCancel == true || FlagSkip == true)
                        break;

                    bool isSuccess = RunConvertCableTrayObject.ConvertCableTray(_doc, data, ref CableTrayDatasConverted, ref datasNotConverted);
                    if (isSuccess)
                    {
                        _incrementValue++;
                        string mess = string.Format("{0} / {1}{2}", _incrementValue, _sumObjectConvert, Define.MESS_PROGESSBAR_OBJECT_COVERT);
                        _progressBar.SetMessage(mess);
                        _progressBar.IncrementProgressBar();
                    }
                }

                List<CableTray> cableTrayConverted = CableTrayDatasConverted.Select(x => x.ConvertElem as CableTray).ToList();
                if (datasNotConverted.Count > 0)
                {
                    Element fitting = null;
                    List<XYZ> centers = new List<XYZ>();

                    foreach (var data in datasNotConverted)
                    {
                        FlagCancel = _progressBar.IsCancel;
                        FlagSkip = _progressBar.IsSkip;
                        if (FlagCancel == true || FlagSkip == true)
                            break;

                        _incrementValue++;
                        string mess = string.Format("{0} / {1}{2}", _incrementValue, _sumObjectConvert, Define.MESS_PROGESSBAR_OBJECT_COVERT);
                        _progressBar.SetMessage(mess);
                        _progressBar.IncrementProgressBar();

                        // get cableTray filter
                        BoundingBoxXYZ boxFitting = GeometryUtils.GetBoudingBoxExtend(data.LinkEleData.LinkElement, data.LinkTransform);
                        if (boxFitting == null)
                            continue;

                        XYZ centerFitting = (boxFitting.Max + boxFitting.Min) / 2;
                        centers.Add(centerFitting);

                        List<CableTray> cableTrayFilters = FindNearestBox(_doc, cableTrayConverted.Select(x => x.Id).ToList(), boxFitting);

                        // Fitting tee
                        fitting = RunConvertCableTrayObject.CreateTeeFitting(_doc, cableTrayFilters, data);

                        // Fitting cross
                        if (fitting?.IsValidObject != true)
                            fitting = RunConvertCableTrayObject.CreateCableTrayCrossFitting(_doc, cableTrayFilters);

                        // Fitting elbow
                        if (fitting?.IsValidObject != true)
                            fitting = RunConvertCableTrayObject.CreateElbow(_doc, cableTrayFilters, data);

                        if (fitting?.IsValidObject == true)
                        {
                            data.IsElbow = true;
                            data.ConvertElem = fitting;
                            CableTrayDatasConverted.Add(data);

                            using (Transaction tr = new Transaction(_doc))
                            {
                                tr.Start("Create Parameter for fitting");
                                RevitUtils.SetValueParamterConvert(App._UIDoc, fitting, data.LinkEleData, data.ParameterData);
                                tr.Commit();
                            }
                        }
                        else
                        {
                            CableTrayDatasNotConverted.Add(data);
                        }
                    }
                }

                grTr.Assimilate();
            }
        }

        private void ProcessIFCTo_Accessory()
        {
            if (AccessoryDatasBeforeConvert.Count <= 0
               || FlagCancel == true
               || FlagSkip == true)
                return;

            foreach (var data in AccessoryDatasBeforeConvert)
            {
                FlagCancel = _progressBar.IsCancel;
                FlagSkip = _progressBar.IsSkip;
                if (FlagCancel == true || FlagSkip == true)
                    break;

                _incrementValue++;
                string mess = string.Format("{0} / {1}{2}", _incrementValue, _sumObjectConvert, Define.MESS_PROGESSBAR_OBJECT_COVERT);
                _progressBar.SetMessage(mess);
                _progressBar.IncrementProgressBar();

                using (Transaction tr = new Transaction(_doc, "Convert"))
                {
                    try
                    {
                        FailureHandlingOptions fhOpts = tr.GetFailureHandlingOptions();
                        REVWarning1 supWarning = new REVWarning1(true);
                        fhOpts.SetFailuresPreprocessor(supWarning);
                        tr.SetFailureHandlingOptions(fhOpts);

                        REVWarning2 supWarning2 = new REVWarning2();
                        fhOpts.SetFailuresPreprocessor(supWarning2);
                        tr.SetFailureHandlingOptions(fhOpts);

                        tr.Start();
                        data.Initialize();
                        tr.Commit();

                        if (data.ConvertElem?.IsValidObject == true)
                        {
                            tr.Start();
                            data.Roatate();
                            tr.Commit();
                        }
                    }
                    catch (Exception)
                    {
                        if (tr.HasStarted())
                            tr.RollBack();
                    }
                }

                if (data.ConvertElem?.IsValidObject == true)
                {
                    AccessoryDatasConverted.Add(data);
                }
                else
                {
                    AccessoryDatasNotConverted.Add(data);
                }
            }
        }

        /// <summary>
        /// Process IFC To MEP_ConduitTmnBox
        /// </summary>
        private void ProcessIFCToMEP_ConduitTmnBox()
        {
            if (ConduitTmnBoxDatasBeforeConvert.Count <= 0) return;
            if (FlagCancel == true || FlagSkip == true) return;

            foreach (var conduitTerminalBoxData in ConduitTmnBoxDatasBeforeConvert)
            {
                FlagCancel = _progressBar.IsCancel;
                FlagSkip = _progressBar.IsSkip;
                if (FlagCancel == true || FlagSkip == true)
                    break;

                _incrementValue++;
                _progressBar.tbxMessage.Text = _incrementValue.ToString() + " / " + _sumObjectConvert.ToString() + Define.MESS_PROGESSBAR_OBJECT_COVERT;
                _progressBar.IncrementProgressBar();

                using (Transaction tr = new Transaction(_doc))
                {
                    try
                    {
                        tr.Start("Create Electrical Equipment");

                        if (!conduitTerminalBoxData.ConduitTerminalType.IsActive)
                            conduitTerminalBoxData.ConduitTerminalType.Activate();

                        FailureHandlingOptions fhOpts = tr.GetFailureHandlingOptions();
                        RevitLinkInstance revLnkIns = conduitTerminalBoxData.LinkInstance;
                        Document docLinked = revLnkIns.GetLinkDocument();

                        bool isElectricCabinetTwoDoor = conduitTerminalBoxData.ConduitTerminalType.FamilyName == "21300_制御盤";
                        if (isElectricCabinetTwoDoor)
                        {
                            bool result = conduitTerminalBoxData.CreateFamilyInstanceElectricWithTwoDoor(_doc, conduitTerminalBoxData);
                            if (result)
                                ConduitTmnBoxDatasConverted.Add(conduitTerminalBoxData);
                            else
                                ConduitTmnBoxDatasNotConverted.Add(conduitTerminalBoxData);
                        }
                        else
                        {
                            bool isConduit = conduitTerminalBoxData.ConduitTerminalType.FamilyName == "conduit";
                            bool isNewFamilyElectric = conduitTerminalBoxData.ConduitTerminalType.FamilyName == "プルボックス"
                                                    || conduitTerminalBoxData.ConduitTerminalType.FamilyName == "盤"
                                                    || conduitTerminalBoxData.ConduitTerminalType.FamilyName == "盤_機械設備";

                            if (isConduit)
                            {
                                bool result = conduitTerminalBoxData.DealingWithCaseOfAnElectricalCabinetConduit(conduitTerminalBoxData, tr, fhOpts);
                                if (result)
                                    ConduitTmnBoxDatasConverted.Add(conduitTerminalBoxData);
                                else
                                    ConduitTmnBoxDatasNotConverted.Add(conduitTerminalBoxData);
                            }
                            else if (isNewFamilyElectric)
                            {
                                var result = conduitTerminalBoxData.CreateConduitTerminalBoxWithNewFamily();
                                _doc.Regenerate();
                                if (result?.IsValidObject == true)
                                    ConduitTmnBoxDatasConverted.Add(conduitTerminalBoxData);
                                else
                                    ConduitTmnBoxDatasNotConverted.Add(conduitTerminalBoxData);
                            }
                            else
                            {
                                bool result = conduitTerminalBoxData.DealingWithCaseOfAnElectricalCabinetNormal(conduitTerminalBoxData, tr, fhOpts);
                                if (result)
                                    ConduitTmnBoxDatasConverted.Add(conduitTerminalBoxData);
                                else
                                    ConduitTmnBoxDatasNotConverted.Add(conduitTerminalBoxData);
                            }
                        }

                        if (conduitTerminalBoxData.ConvertElem?.IsValidObject == true)
                        {
                            RevitUtils.SetValueParamterConvert(App._UIDoc,
                                                               conduitTerminalBoxData.ConvertElem,
                                                               conduitTerminalBoxData.LinkEleData,
                                                               conduitTerminalBoxData.ParameterData);
                        }
                        tr.Commit();
                    }
                    catch (System.Exception)
                    {
                        tr.RollBack();
                    }
                }
            }
        }

        private List<Element> CreateAgainFitting(LinkElementData linkData, ConvertParamData paramData, ref List<Element> raillingConverted, bool isRailing = false)
        {
            List<Element> fittings = new List<Element>();
            if (linkData == null)
            {
                return fittings;
            }
            Transaction tr = new Transaction(_doc, "Update data");
            try
            {
                List<Element> listRemoves = new List<Element>();
                foreach (var t in raillingConverted)
                {
                    if (t is MEPCurve mep
                        && mep.Location is LocationCurve lcCurve
                        && lcCurve.Curve is Line lcLine)
                    {
                        double diameter = double.NaN;
                        Pipe pipe = null;
                        Duct duct = null;
                        if (mep is Pipe)
                        {
                            pipe = mep as Pipe;
                            if (pipe.PipeType.Shape == ConnectorProfileType.Round)
                                diameter = pipe.Diameter;
                        }
                        else if (mep is Duct)
                        {
                            duct = mep as Duct;
                            if (duct.DuctType.Shape == ConnectorProfileType.Round)
                                diameter = duct.Diameter;
                            else if (duct.DuctType.Shape == ConnectorProfileType.Rectangular)
                                diameter = duct.Height;
                        }

                        if (double.IsNaN(diameter) ||
                            RevitUtils.IsGreaterThan(lcLine.Length / diameter, 2.5))
                        {
                            continue;
                        }

                        Solid solidT = UtilsSolid.GetTotalSolid(t);
                        BoundingBoxXYZ boxSolid = solidT?.GetBoundingBox();
                        BoundingBoxXYZ boxFitting = GeometryUtils.GetBoudingBox(boxSolid);
                        if (boxFitting == null)
                        {
                            continue;
                        }

                        List<ElementId> filterIds = raillingConverted.Where(x => x?.IsValidObject == true && !x.Id.Equals(t.Id) && x.Category.Id.Equals(t.Category.Id) && !listRemoves.Any(f => f.Id.Equals(x.Id))).Select(x => x.Id).ToList();
                        if (!(filterIds?.Count > 0))
                        {
                            continue;
                        }

                        List<Element> eleInterSectors = GeometryUtils.FindPipeDuctNearestBox(_doc, filterIds, boxFitting);
                        if (!(eleInterSectors?.Count > 1))
                            continue;

                        Element tee = eleInterSectors.FirstOrDefault(x => ValidateT(x, lcLine.Direction));
                        if (tee == null)
                        {
                            continue;
                        }

                        Solid solidTee = UtilsSolid.GetTotalSolid(tee);
                        List<Solid> soilds = new List<Solid>
                        {
                            solidT,
                            solidTee
                        };

                        List<Element> mepConnect = eleInterSectors.Where(x => !x.Id.Equals(tee.Id)).ToList();

                        BoundingBoxXYZ boxSolidT = solidTee?.GetBoundingBox();
                        BoundingBoxXYZ boxFittingT = GeometryUtils.GetBoudingBox(boxSolidT);
                        if (boxFittingT == null)
                        {
                            continue;
                        }

                        filterIds = filterIds.Where(x => !x.Equals(tee.Id) && !mepConnect.Any(c => c.Id.Equals(x))).ToList();
                        eleInterSectors = GeometryUtils.FindPipeDuctNearestBox(_doc, filterIds, boxFittingT);
                        if (!(eleInterSectors?.Count > 0))
                            continue;

                        mepConnect.AddRange(eleInterSectors);
                        if (mepConnect?.Count == 3)
                        {
                            FamilyInstance instance = TeeRailingsData.CreateTeeFitting(_doc,
                                                                                       mepConnect.Cast<MEPCurve>().ToList(),
                                                                                       null,
                                                                                       null,
                                                                                       soilds);

                            if (instance?.IsValidObject == true)
                            {
                                listRemoves.Add(t);
                                listRemoves.Add(tee);
                                fittings.Add(instance);
                            }
                        }
                    }
                }
                if (listRemoves?.Count > 0)
                {
                    tr.Start();
                    foreach (var instance in fittings)
                    {
                        RevitUtils.SetValueParamterConvert(App._UIDoc, instance, linkData, paramData, isRailing);
                    }
                    foreach (var instance in listRemoves)
                    {
                        var itemRemove = raillingConverted.FirstOrDefault(x => x.Id.Equals(instance.Id));
                        if (itemRemove != null)
                        {
                            raillingConverted.Remove(itemRemove);
                        }
                    }
                    _doc.Delete(listRemoves.Select(x => x.Id).ToList());
                    tr.Commit();
                }
            }
            catch (Exception)
            {
                if (tr.HasStarted())
                {
                    tr.RollBack();
                }
            }

            return fittings;
        }

        private bool ValidateT(Element mep, XYZ direction)
        {
            if (mep != null
                && mep.Location is LocationCurve lcCurve
                && lcCurve.Curve is Line lcLine
                && RevitUtils.IsPerpendicular(lcLine.Direction, direction))
            {
                double diameter = double.NaN;
                if (mep is Pipe)
                {
                    Pipe pipe = mep as Pipe;
                    if (pipe.PipeType.Shape == ConnectorProfileType.Round)
                        diameter = pipe.Diameter;
                }
                else if (mep is Duct)
                {
                    Duct duct = mep as Duct;
                    if (duct.DuctType.Shape == ConnectorProfileType.Round)
                        diameter = duct.Diameter;
                    else if (duct.DuctType.Shape == ConnectorProfileType.Rectangular)
                        diameter = duct.Height;
                }

                if (double.IsNaN(diameter) ||
                    RevitUtils.IsGreaterThan(lcLine.Length / diameter, 2.5))
                {
                    return false;
                }

                return true;
            }

            return false;
        }

        private List<Element> CreateAgainFitting(RailingData railingData, ref List<Element> raillingConverted)
        {
            List<Element> fittings = new List<Element>();
            if (railingData == null)
            {
                return fittings;
            }

            List<Element> listRemoves = new List<Element>();
            foreach (var tee1 in raillingConverted)
            {
                if (tee1 is MEPCurve mep
                    && mep.Location is LocationCurve lcCurve
                   && lcCurve.Curve is Line lcLine)
                {
                    double diameter = double.NaN;
                    Pipe pipe = null;
                    Duct duct = null;
                    if (mep is Pipe)
                    {
                        pipe = mep as Pipe;
                        if (pipe.PipeType.Shape == ConnectorProfileType.Round)
                            diameter = pipe.Diameter;
                    }
                    else if (mep is Duct)
                    {
                        duct = mep as Duct;
                        if (duct.DuctType.Shape == ConnectorProfileType.Round)
                            diameter = duct.Diameter;
                        else if (duct.DuctType.Shape == ConnectorProfileType.Rectangular)
                            diameter = duct.Height;
                    }

                    if (diameter != double.NaN
                        && RevitUtils.IsLessThanOrEqual(lcLine.Length / diameter, 2.5))
                    {
                        BoundingBoxXYZ boxSolid = UtilsSolid.GetTotalSolid(tee1).GetBoundingBox();
                        BoundingBoxXYZ boxFitting = GeometryUtils.GetBoudingBox(boxSolid);
                        if (boxFitting == null)
                            continue;
                        List<Element> eleInterSectors = GeometryUtils.FindPipeDuctNearestBox(_doc,
                                                                                             raillingConverted.Where(x => x != null && x.IsValidObject && !listRemoves.Any(fitting => fitting.Id.ToString().Equals(x.Id.ToString())))
                                                                                             .Select(x => x.Id)
                                                                                             .ToList(), boxFitting)
                                                                                             .Where(x => x is Pipe || x is Duct).ToList();

                        if (!(eleInterSectors?.Count > 0))
                            continue;

                        Dictionary<double, List<Element>> sortElem = SortLength(tee1, eleInterSectors);
                        if (!(sortElem?.Count > 0))
                            continue;

                        Element tee = sortElem.FirstOrDefault().Value.FirstOrDefault();
                        if (tee != null
                           && tee.Location is LocationCurve lcCurve1
                             && lcCurve1.Curve is Line lcLine1)
                        {
                            double diamterInter = double.NaN;
                            if (pipe != null)
                            {
                                if (tee is Pipe pipeInter
                                    && pipeInter.PipeType.Shape == ConnectorProfileType.Round)
                                {
                                    diamterInter = pipeInter.Diameter;
                                }
                                else
                                    break;
                            }
                            else if (duct != null
                                     && tee is Duct ductInter)
                            {
                                if (ductInter.DuctType.Shape == ConnectorProfileType.Round)
                                    diamterInter = ductInter.Diameter;
                                else if (ductInter.DuctType.Shape == ConnectorProfileType.Rectangular)
                                    diamterInter = ductInter.Height;
                            }
                            else
                                break;

                            if (diamterInter != double.NaN
                                && RevitUtils.IsLessThanOrEqual(lcLine1.Length / diameter, 2.5)
                                && !RevitUtils.IsParallel(lcLine.Direction, lcLine1.Direction)
                                && !UtilsCurve.IsLineStraight(lcLine1, lcLine))
                            {
                                Solid solidTee1 = UtilsSolid.GetTotalSolid(tee1);
                                Solid solidTee = UtilsSolid.GetTotalSolid(tee);

                                List<Solid> soilds = new List<Solid>
                                {
                                    solidTee1,
                                    solidTee
                                };

                                try
                                {
                                    eleInterSectors = eleInterSectors.Where(x => !x.Id.ToString().Equals(tee.Id.ToString())).ToList();
                                    BoundingBoxXYZ boxSolid1 = UtilsSolid.GetTotalSolid(tee).GetBoundingBox();
                                    BoundingBoxXYZ boxFitting1 = GeometryUtils.GetBoudingBox(boxSolid1);
                                    List<MEPCurve> mepConnects1 = new List<MEPCurve>();
                                    if (boxFitting != null)
                                    {
                                        List<Element> eleInterSectors1 = GeometryUtils.FindPipeDuctNearestBox(_doc,
                                                                                                             raillingConverted.Where(x => x != null && x.IsValidObject
                                                                                                                                         && !x.Id.ToString().Equals(tee.Id.ToString())
                                                                                                                                         && !x.Id.ToString().Equals(tee1.Id.ToString()))
                                                                                                             .Select(x => x.Id)
                                                                                                             .ToList(), boxFitting1)
                                                                                                             .Where(x => x is Pipe || x is Duct)
                                                                                                             .ToList();

                                        eleInterSectors1.AddRange(eleInterSectors);

                                        if (eleInterSectors1?.Count == 3)
                                        {
                                            FamilyInstance instance = TeeRailingsData.CreateTeeFitting(_doc,
                                                                                                       eleInterSectors1.Cast<MEPCurve>().ToList(),
                                                                                                       null,
                                                                                                       null,
                                                                                                       soilds);

                                            if (instance?.IsValidObject == true)
                                            {
                                                listRemoves.Add(tee1);
                                                listRemoves.Add(tee);
                                                fittings.Add(instance);
                                            }
                                        }
                                    }
                                }
                                catch (Exception) { }
                            }
                        }
                    }
                }
            }
            if (listRemoves?.Count > 0)
            {
                using (Transaction tr = new Transaction(_doc, "update data"))
                {
                    tr.Start();
                    foreach (var instance in fittings)
                    {
                        RevitUtils.SetValueParamterConvert(App._UIDoc, instance, railingData.LinkEleData, railingData.ParameterData, true);
                    }
                    foreach (var instance in listRemoves)
                    {
                        var itemRemove = raillingConverted.FirstOrDefault(x => x.Id.Equals(instance.Id));
                        if (itemRemove != null)
                        {
                            raillingConverted.Remove(itemRemove);
                        }
                    }
                    _doc.Delete(listRemoves.Select(x => x.Id).ToList());
                    tr.Commit();
                }
            }

            return fittings;
        }

        private Dictionary<double, List<Element>> SortLength(Element elm, List<Element> elements)
        {
            Dictionary<double, List<Element>> result = new Dictionary<double, List<Element>>();
            List<Element> filterElem = elements.Where(x => !IsLocationLineStraight(x, elm)).ToList();
            foreach (Element element in filterElem)
            {
                if (element.Location is LocationCurve lcCurve
                     && lcCurve.Curve is Line lcLine)
                {
                    if (result.ContainsKey(lcLine.Length))
                        result[lcLine.Length].Add(element);
                    else
                        result.Add(lcLine.Length, new List<Element>() { element });
                }
            }

            return result.OrderBy(x => x.Key).ToDictionary(entry => (double)entry.Key,
                                                           entry => entry.Value);
        }

        private bool IsLocationLineStraight(Element elem1, Element elem2)
        {
            if (elem1.Location is LocationCurve lc1
                && lc1.Curve is Line line1
                && elem2.Location is LocationCurve lc2
                && lc2.Curve is Line line2
                && UtilsCurve.IsLineStraight(line1, line2))
                return true;
            return false;
        }

        public List<CableTray> FindNearestBox(Document doc, List<ElementId> cableTrayIds, BoundingBoxXYZ box, bool isCheckConnect = true)
        {
            List<CableTray> cableTrays = new List<CableTray>();
            if (cableTrayIds?.Count > 0 && box != null)
            {
                BoundingBoxIntersectsFilter boxFilter = GetBoxFilter(box);
                cableTrays = new FilteredElementCollector(doc, cableTrayIds).WherePasses(boxFilter)
                                                                             .Where(x => IsEndElementInBox(x, box))
                                                                             .Cast<CableTray>()
                                                                             .ToList();

                if (cableTrays.Count() > 1)
                {
                    CableTray cableTray = cableTrays.First();
                    cableTrays = cableTrays.Where(x => (!isCheckConnect || CommonDataPipeDuct.ValidateConnected(x, box))).ToList();
                }
            }
            return cableTrays;
        }

        public static BoundingBoxIntersectsFilter GetBoxFilter(BoundingBoxXYZ box)
        {
            Outline outline = new Outline(box.Min, box.Max);
            BoundingBoxIntersectsFilter boxFilter = new BoundingBoxIntersectsFilter(outline);
            return boxFilter;
        }

        public static bool IsEndElementInBox(Element element, BoundingBoxXYZ box)
        {
            bool isInBox = false;
            if (element != null && element.IsValidObject && element.Location is LocationCurve lcCurve && lcCurve.Curve is Line lcLine)
            {
                if (IsPointInBox(lcLine.GetEndPoint(0), box) &&
                    IsPointInBox(lcLine.GetEndPoint(1), box))
                    isInBox = false;
                else if (IsPointInBox(lcLine.GetEndPoint(0), box) ||
                     IsPointInBox(lcLine.GetEndPoint(1), box))
                {
                    isInBox = true;
                }
            }

            return isInBox;
        }

        public static bool IsPointInBox(XYZ point, BoundingBoxXYZ box)
        {
            bool isInBox = false;

            if (point != null && box != null)
            {
                XYZ min = box.Min;
                XYZ max = box.Max;

                if (point.X > min.X && point.X < max.X
                    && point.Y > min.Y && point.Y < max.Y
                    && point.Z > min.Z && point.Z < max.Z)
                {
                    isInBox = true;
                }
            }

            return isInBox;
        }

        #endregion MEP Method

        #region Structural Method

        /// <summary>
        /// Before Process IFC To Structural
        /// </summary>
        private void BeforeProcessIFCToStructural()
        {
            try
            {
                BeforeProcessIFCToStructural_StructuralColumn();
                BeforeProcessIFCToStructural_ArchitectureColumn();
                BeforeProcessIFCToStructural_Beam();
                BeforeProcessIFCToStructural_Floor();
                BeforeProcessIFCToStructural_Wall();
                BeforeProcessIFCToStructural_Celling();
                BeforeProcessIFCToStructural_Opening();
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Before Process IFC To Structural_StructuralColumn
        /// </summary>
        private void BeforeProcessIFCToStructural_StructuralColumn()
        {
            VMConvertIFCtoRevTargetObject flag = VMConvIFCMain.TargetObjsStructural.FirstOrDefault(item => string.Equals(Define.GetCategoryLabel(BuiltInCategory.OST_StructuralColumns).ToUpper(), item.Content));
            if (flag != null && flag.IsChecked == false)
                return;

            var temp = new List<LinkElementData>(IFCElements);
            VMSettingCategory vmCategory = VMSettingMain.SettingCategories.FirstOrDefault(item => item.ProcessBuiltInCategory == BuiltInCategory.OST_StructuralColumns);
            if (vmCategory != null)
            {
                string parameterName = vmCategory.IsCheckedGetParamByCategory ? vmCategory.NameParameterInRevit : string.Empty;
                ConvertParamData dataParam = new ConvertParamData(vmCategory.SelParaKey, parameterName, vmCategory.ValueParameter);

                if (vmCategory.SettingGrps?.Count > 0)
                {
                    foreach (var vMSetGrp in vmCategory.SettingGrps)
                    {
                        List<VMSettingIfc> settingObjs = new List<VMSettingIfc>();
                        List<LinkElementData> filteredElements = new List<LinkElementData>();

                        if (vMSetGrp is VMSettingGroupCondition)
                        {
                            settingObjs = vMSetGrp.SettingObjs.Where(item => !string.IsNullOrWhiteSpace(item.KeyValue)).ToList();

                            if (settingObjs.Count <= 0)
                                continue;

                            filteredElements = temp.Where(e => IsMatchIfcSettingCondition(e, settingObjs)).ToList();
                            ColumnDatasBeforeConvert.AddRange(filteredElements.Select(item => new ColumnData(App._UIDoc, item, vMSetGrp.SelType?.Id, LinkInstance, dataParam)).Where(x => x.ValidObject).ToList());
                            temp = temp.Except(filteredElements, new DefaultObjectComparer<LinkElementData>()).ToList();
                        }

                        if (vMSetGrp is VMSettingGroupSelection)
                        {
                            if (vMSetGrp.LstSelectElemId != null && vMSetGrp.LstSelectElemId.Count > 0)
                            {
                                foreach (var item in vMSetGrp.LstSelectElemId)
                                {
                                    filteredElements.AddRange(temp.Where(e => e.LinkElement.UniqueId.Equals(item)));
                                }

                                ColumnDatasBeforeConvert.AddRange(filteredElements.Select(item => new ColumnData(App._UIDoc, item, vMSetGrp.SelType?.Id, LinkInstance, dataParam)).Where(x => x.ValidObject).ToList());
                                temp = temp.Except(filteredElements, new DefaultObjectComparer<LinkElementData>()).ToList();
                            }
                        }
                    }
                }

                if (vmCategory.IsCheckedGetEleByCategory)
                {
                    var columnDatas = temp.Where(x => IsMatchCategory(x, BuiltInCategory.OST_StructuralColumns))
                                    .Select(x => new ColumnData(App._UIDoc, x, vmCategory.SelType?.Id, LinkInstance, dataParam)).Where(x => x.ValidObject).ToList();
                    ColumnDatasBeforeConvert.AddRange(columnDatas);
                }
            }
        }

        /// <summary>
        /// Before Process IFC To Structural_ArchitectureColumn
        /// </summary>
        private void BeforeProcessIFCToStructural_ArchitectureColumn()
        {
            VMConvertIFCtoRevTargetObject flag = VMConvIFCMain.TargetObjsStructural.FirstOrDefault(item => string.Equals(Define.GetCategoryLabel(BuiltInCategory.OST_Columns).ToUpper(), item.Content));
            if (flag != null && flag.IsChecked == false)
                return;

            var temp = new List<LinkElementData>(IFCElements);
            VMSettingCategory vmCategory = VMSettingMain.SettingCategories.FirstOrDefault(item => item.ProcessBuiltInCategory == BuiltInCategory.OST_Columns);
            if (vmCategory != null)
            {
                string parameterName = vmCategory.IsCheckedGetParamByCategory ? vmCategory.NameParameterInRevit : string.Empty;
                ConvertParamData dataParam = new ConvertParamData(vmCategory.SelParaKey, parameterName, vmCategory.ValueParameter);

                if (vmCategory.SettingGrps?.Count > 0)
                {
                    foreach (var vMSetGrp in vmCategory.SettingGrps)
                    {
                        List<VMSettingIfc> settingObjs = new List<VMSettingIfc>();
                        List<LinkElementData> filteredElements = new List<LinkElementData>();

                        if (vMSetGrp is VMSettingGroupCondition)
                        {
                            settingObjs = vMSetGrp.SettingObjs.Where(item => !string.IsNullOrWhiteSpace(item.KeyValue)).ToList();
                            if (settingObjs.Count <= 0)
                                continue;

                            filteredElements = temp.Where(e => IsMatchIfcSettingCondition(e, settingObjs)).ToList();
                            ArchiColumnDatasBeforeConvert.AddRange(filteredElements.Select(item => new ArchitectureColumnData(App._UIDoc, item, vMSetGrp.SelType?.Id, LinkInstance, dataParam)).Where(x => x.ValidObject).ToList());
                            temp = temp.Except(filteredElements, new DefaultObjectComparer<LinkElementData>()).ToList();
                        }
                        else
                        {
                            if (vMSetGrp.LstSelectElemId != null && vMSetGrp.LstSelectElemId.Count > 0)
                            {
                                foreach (var item in vMSetGrp.LstSelectElemId)
                                {
                                    filteredElements.AddRange(temp.Where(e => e.LinkElement.UniqueId.Equals(item)));
                                }

                                ArchiColumnDatasBeforeConvert.AddRange(filteredElements.Select(item => new ArchitectureColumnData(App._UIDoc, item, vMSetGrp.SelType?.Id, LinkInstance, dataParam)).Where(x => x.ValidObject).ToList());
                                temp = temp.Except(filteredElements, new DefaultObjectComparer<LinkElementData>()).ToList();
                            }
                        }
                    }
                }

                if (vmCategory.IsCheckedGetEleByCategory)
                {
                    var archColumnDatas = temp.Where(x => IsMatchCategory(x, BuiltInCategory.OST_Columns))
                                    .Select(x => new ArchitectureColumnData(App._UIDoc, x, vmCategory.SelType?.Id, LinkInstance, dataParam))
                                    .Where(x => x.ValidObject);
                    ArchiColumnDatasBeforeConvert.AddRange(archColumnDatas);
                }
            }
        }

        /// <summary>
        /// Before Process IFC ToStructural_Beam
        /// </summary>
        private void BeforeProcessIFCToStructural_Beam()
        {
            VMConvertIFCtoRevTargetObject flag = VMConvIFCMain.TargetObjsStructural.FirstOrDefault(item => string.Equals(Define.GetCategoryLabel(BuiltInCategory.OST_StructuralFraming).ToUpper(), item.Content));
            if (flag != null && flag.IsChecked == false)
                return;

            var temp = new List<LinkElementData>(IFCElements);
            VMSettingCategory vmCategory = VMSettingMain.SettingCategories.FirstOrDefault(item => item.ProcessBuiltInCategory == BuiltInCategory.OST_StructuralFraming);

            if (vmCategory != null)
            {
                string parameterName = vmCategory.IsCheckedGetParamByCategory ? vmCategory.NameParameterInRevit : string.Empty;
                ConvertParamData dataParam = new ConvertParamData(vmCategory.SelParaKey, parameterName, vmCategory.ValueParameter);

                if (vmCategory.SettingGrps?.Count > 0)
                {
                    foreach (var vMSetGrp in vmCategory.SettingGrps)
                    {
                        List<VMSettingIfc> settingObjs = new List<VMSettingIfc>();
                        List<LinkElementData> filteredElements = new List<LinkElementData>();

                        if (vMSetGrp is VMSettingGroupCondition)
                        {
                            settingObjs = vMSetGrp.SettingObjs.Where(item => !string.IsNullOrWhiteSpace(item.KeyValue)).ToList();
                            if (settingObjs.Count <= 0)
                                continue;

                            filteredElements = temp.Where(e => IsMatchIfcSettingCondition(e, settingObjs)).ToList();
                            BeamDatasBeforeConvert.AddRange(filteredElements.Select(item => new BeamData(App._UIDoc, item, vMSetGrp.SelType?.Id, LinkInstance, dataParam))
                                                                     .Where(x => x.ValidObject)
                                                                     .ToList());

                            temp = temp.Except(filteredElements, new DefaultObjectComparer<LinkElementData>()).ToList();
                        }
                        else
                        {
                            if (vMSetGrp.LstSelectElemId != null && vMSetGrp.LstSelectElemId.Count > 0)
                            {
                                foreach (var item in vMSetGrp.LstSelectElemId)
                                {
                                    filteredElements.AddRange(temp.Where(e => e.LinkElement.UniqueId.Equals(item)));
                                }

                                BeamDatasBeforeConvert.AddRange(filteredElements.Select(item => new BeamData(App._UIDoc, item, vMSetGrp.SelType?.Id, LinkInstance, dataParam))
                                                                     .Where(x => x.ValidObject)
                                                                     .ToList());

                                temp = temp.Except(filteredElements, new DefaultObjectComparer<LinkElementData>()).ToList();
                            }
                        }
                    }
                }

                if (vmCategory.IsCheckedGetEleByCategory)
                {
                    var beamDatas = temp.Where(x => IsMatchCategory(x, BuiltInCategory.OST_StructuralFraming))
                                    .Select(x => new BeamData(App._UIDoc, x, vmCategory.SelType?.Id, LinkInstance, dataParam)).Where(x => x.ValidObject);
                    BeamDatasBeforeConvert.AddRange(beamDatas);
                }
            }
        }

        /// <summary>
        /// Before Process IFC To Structural_Floor
        /// </summary>
        private void BeforeProcessIFCToStructural_Floor()
        {
            VMConvertIFCtoRevTargetObject flag = VMConvIFCMain.TargetObjsStructural.FirstOrDefault(item => string.Equals(Define.GetCategoryLabel(BuiltInCategory.OST_Floors).ToUpper(), item.Content));
            if (flag != null && flag.IsChecked == false)
                return;

            var temp = new List<LinkElementData>(IFCElements);
            VMSettingCategory vmCategory = VMSettingMain.SettingCategories.FirstOrDefault(item => item.ProcessBuiltInCategory == BuiltInCategory.OST_Floors);
            if (vmCategory != null)
            {
                string parameterName = vmCategory.IsCheckedGetParamByCategory ? vmCategory.NameParameterInRevit : string.Empty;
                ConvertParamData dataParam = new ConvertParamData(vmCategory.SelParaKey, parameterName, vmCategory.ValueParameter);

                if (vmCategory.SettingGrps?.Count > 0)
                {
                    foreach (var vMSetGrp in vmCategory.SettingGrps)
                    {
                        List<VMSettingIfc> settingObjs = new List<VMSettingIfc>();
                        List<LinkElementData> filteredElements = new List<LinkElementData>();

                        if (vMSetGrp is VMSettingGroupCondition)
                        {
                            settingObjs = vMSetGrp.SettingObjs.Where(item => !string.IsNullOrWhiteSpace(item.KeyValue)).ToList();
                            if (settingObjs.Count <= 0)
                                continue;

                            filteredElements = temp.Where(e => IsMatchIfcSettingCondition(e, settingObjs)).ToList();
                            FloorDatasBeforeConvert.AddRange(filteredElements.Select(item => new FloorData(App._UIDoc, item, vMSetGrp.SelType?.Id, LinkInstance, dataParam))
                                                                      .Where(x => x.ValidObject)
                                                                      .ToList());
                            temp = temp.Except(filteredElements, new DefaultObjectComparer<LinkElementData>()).ToList();
                        }
                        else
                        {
                            if (vMSetGrp.LstSelectElemId != null && vMSetGrp.LstSelectElemId.Count > 0)
                            {
                                foreach (var item in vMSetGrp.LstSelectElemId)
                                {
                                    filteredElements.AddRange(temp.Where(e => e.LinkElement.UniqueId.Equals(item)));
                                }

                                FloorDatasBeforeConvert.AddRange(filteredElements.Select(item => new FloorData(App._UIDoc, item, vMSetGrp.SelType?.Id, LinkInstance, dataParam))
                                                                       .Where(x => x.ValidObject)
                                                                       .ToList());
                                temp = temp.Except(filteredElements, new DefaultObjectComparer<LinkElementData>()).ToList();
                            }
                        }
                    }
                }

                if (vmCategory.IsCheckedGetEleByCategory)
                {
                    var floorDatas = temp.Where(x => IsMatchCategory(x, BuiltInCategory.OST_Floors))
                                    .Select(x => new FloorData(App._UIDoc, x, vmCategory.SelType?.Id, LinkInstance, dataParam)).Where(x => x.ValidObject);
                    FloorDatasBeforeConvert.AddRange(floorDatas);
                }
            }
        }

        /// <summary>
        /// Before Process IFC To Structural_Wall
        /// </summary>
        private void BeforeProcessIFCToStructural_Wall()
        {
            VMConvertIFCtoRevTargetObject flag = VMConvIFCMain.TargetObjsStructural.FirstOrDefault(item => string.Equals(Define.GetCategoryLabel(BuiltInCategory.OST_Walls).ToUpper(), item.Content));
            if (flag != null && flag.IsChecked == false)
                return;

            var temp = new List<LinkElementData>(IFCElements);
            VMSettingCategory vmCategory = VMSettingMain.SettingCategories.FirstOrDefault(item => item.ProcessBuiltInCategory == BuiltInCategory.OST_Walls);
            if (vmCategory != null)
            {
                string parameterName = vmCategory.IsCheckedGetParamByCategory ? vmCategory.NameParameterInRevit : string.Empty;
                ConvertParamData dataParam = new ConvertParamData(vmCategory.SelParaKey, parameterName, vmCategory.ValueParameter);

                if (vmCategory.SettingGrps?.Count > 0)
                {
                    foreach (var vMSetGrp in vmCategory.SettingGrps)
                    {
                        List<VMSettingIfc> settingObjs = new List<VMSettingIfc>();
                        List<LinkElementData> filteredElements = new List<LinkElementData>();

                        if (vMSetGrp is VMSettingGroupCondition)
                        {
                            settingObjs = vMSetGrp.SettingObjs.Where(item => !string.IsNullOrWhiteSpace(item.KeyValue)).ToList();
                            if (settingObjs.Count <= 0)
                                continue;

                            filteredElements = temp.Where(e => IsMatchIfcSettingCondition(e, settingObjs)).ToList();
                            WallDatasBeforeConvert.AddRange(filteredElements.Select(item => new WallData(App._UIDoc, item, vMSetGrp.SelType?.Id, LinkInstance, dataParam))
                                                                     .Where(x => x.ValidObject)
                                                                     .ToList());
                            temp = temp.Except(filteredElements, new DefaultObjectComparer<LinkElementData>()).ToList();
                        }
                        else
                        {
                            if (vMSetGrp.LstSelectElemId != null && vMSetGrp.LstSelectElemId.Count > 0)
                            {
                                foreach (var item in vMSetGrp.LstSelectElemId)
                                {
                                    filteredElements.AddRange(temp.Where(e => e.LinkElement.UniqueId.Equals(item)));
                                }

                                WallDatasBeforeConvert.AddRange(filteredElements.Select(item => new WallData(App._UIDoc, item, vMSetGrp.SelType?.Id, LinkInstance, dataParam))
                                                                     .Where(x => x.ValidObject)
                                                                     .ToList());
                                temp = temp.Except(filteredElements, new DefaultObjectComparer<LinkElementData>()).ToList();
                            }
                        }
                    }
                }

                if (vmCategory.IsCheckedGetEleByCategory)
                {
                    var wallDatas = temp.Where(x => IsMatchCategory(x, BuiltInCategory.OST_Walls))
                                    .Select(x => new WallData(App._UIDoc, x, vmCategory.SelType?.Id, LinkInstance, dataParam)).Where(x => x.ValidObject);
                    WallDatasBeforeConvert.AddRange(wallDatas);
                }
            }
        }

        /// <summary>
        /// Before Process IFC To Structural_Celling
        /// </summary>
        private void BeforeProcessIFCToStructural_Celling()
        {
            VMConvertIFCtoRevTargetObject flag = VMConvIFCMain.TargetObjsStructural.FirstOrDefault(item => string.Equals(Define.GetCategoryLabel(BuiltInCategory.OST_Ceilings).ToUpper(), item.Content));
            if (flag != null && flag.IsChecked == false)
                return;

            var temp = new List<LinkElementData>(IFCElements);
            VMSettingCategory vmCategory = VMSettingMain.SettingCategories.FirstOrDefault(item => item.ProcessBuiltInCategory == BuiltInCategory.OST_Ceilings);
            if (vmCategory != null)
            {
                string parameterName = vmCategory.IsCheckedGetParamByCategory ? vmCategory.NameParameterInRevit : string.Empty;
                ConvertParamData dataParam = new ConvertParamData(vmCategory.SelParaKey, parameterName, vmCategory.ValueParameter);

                if (vmCategory.SettingGrps?.Count > 0)
                {
                    foreach (var vMSetGrp in vmCategory.SettingGrps)
                    {
                        List<VMSettingIfc> settingObjs = new List<VMSettingIfc>();
                        List<LinkElementData> filteredElements = new List<LinkElementData>();

                        if (vMSetGrp is VMSettingGroupCondition)
                        {
                            settingObjs = vMSetGrp.SettingObjs.Where(item => !string.IsNullOrWhiteSpace(item.KeyValue)).ToList();
                            if (settingObjs.Count <= 0)
                                continue;

                            filteredElements = temp.Where(e => IsMatchIfcSettingCondition(e, settingObjs)).ToList();
                            CeilingDatasBeforeConvert.AddRange(filteredElements.Select(item => new CeilingData(App._UIDoc, item, vMSetGrp.SelType?.Id, LinkInstance, dataParam))
                                                                        .Where(x => x.ValidObject)
                                                                        .ToList());
                            temp = temp.Except(filteredElements, new DefaultObjectComparer<LinkElementData>()).ToList();
                        }
                        else
                        {
                            if (vMSetGrp.LstSelectElemId != null && vMSetGrp.LstSelectElemId.Count > 0)
                            {
                                foreach (var item in vMSetGrp.LstSelectElemId)
                                {
                                    filteredElements.AddRange(temp.Where(e => e.LinkElement.UniqueId.Equals(item)));
                                }

                                CeilingDatasBeforeConvert.AddRange(filteredElements.Select(item => new CeilingData(App._UIDoc, item, vMSetGrp.SelType?.Id, LinkInstance, dataParam))
                                                                        .Where(x => x.ValidObject)
                                                                        .ToList());
                                temp = temp.Except(filteredElements, new DefaultObjectComparer<LinkElementData>()).ToList();
                            }
                        }
                    }
                }

                if (vmCategory.IsCheckedGetEleByCategory)
                {
                    var ceilingDatas = temp.Where(x => IsMatchCategory(x, BuiltInCategory.OST_Ceilings))
                                   .Select(x => new CeilingData(App._UIDoc, x, vmCategory.SelType?.Id, LinkInstance, dataParam)).Where(x => x.ValidObject);
                    CeilingDatasBeforeConvert.AddRange(ceilingDatas);
                }
            }
        }

        /// <summary>
        /// Before Process IFC to opening
        /// </summary>
        private void BeforeProcessIFCToStructural_Opening()
        {
            string openingCateName = Define.GetCategoryLabel(BuiltInCategory.OST_ShaftOpening).ToUpper();
            VMConvertIFCtoRevTargetObject flag = VMConvIFCMain.TargetObjsMEP.FirstOrDefault(item => string.Equals(openingCateName, item.Content));
            if (flag != null && flag.IsChecked == false)
                return;

            var temp = new List<LinkElementData>(IFCElements);
            VMSettingCategory vmCategory = VMSettingMain.SettingCategories.FirstOrDefault(item => item.ProcessBuiltInCategory == BuiltInCategory.OST_ShaftOpening);
            if (vmCategory != null)
            {
                string parameterName = vmCategory.IsCheckedGetParamByCategory ? vmCategory.NameParameterInRevit : string.Empty;
                ConvertParamData dataParam = new ConvertParamData(vmCategory.SelParaKey, parameterName, vmCategory.ValueParameter);

                if (vmCategory.SettingGrps?.Count > 0)
                {
                    foreach (var vMSetGrp in vmCategory.SettingGrps)
                    {
                        List<VMSettingIfc> settingObjs = new List<VMSettingIfc>();
                        List<LinkElementData> filteredElements = new List<LinkElementData>();

                        if (vMSetGrp is VMSettingGroupCondition)
                        {
                            settingObjs = vMSetGrp.SettingObjs.Where(item => !string.IsNullOrWhiteSpace(item.KeyValue)).ToList();

                            if (settingObjs.Count <= 0)
                                continue;

                            filteredElements = temp.Where(e => IsMatchIfcSettingCondition(e, settingObjs)).ToList();
                            OpeningDatasBeforeConvert.AddRange(filteredElements.Select(item => new OpeningData(App._UIDoc, item, vMSetGrp.SelType?.Id, LinkInstance, dataParam)).ToList());
                            temp = temp.Except(filteredElements, new DefaultObjectComparer<LinkElementData>()).ToList();
                        }
                        else if (vMSetGrp is VMSettingGroupSelection)
                        {
                            if (vMSetGrp.LstSelectElemId != null && vMSetGrp.LstSelectElemId.Count > 0)
                            {
                                foreach (var elementIFC in vMSetGrp.LstSelectElemId)
                                {
                                    filteredElements.AddRange(temp.Where(e => e.LinkElement.UniqueId.Equals(elementIFC)));
                                }

                                OpeningDatasBeforeConvert.AddRange(filteredElements.Select(item => new OpeningData(App._UIDoc, item, vMSetGrp.SelType?.Id, LinkInstance, dataParam)).ToList());
                                temp = temp.Except(filteredElements, new DefaultObjectComparer<LinkElementData>()).ToList();
                            }
                        }
                    }
                }

                if (vmCategory.IsCheckedGetEleByCategory)
                {
                    var items = temp.Where(x => IsMatchCategory(x, BuiltInCategory.OST_ShaftOpening))
                                    .Select(x => new OpeningData(App._UIDoc, x, vmCategory.SelType?.Id, LinkInstance, dataParam));
                    OpeningDatasBeforeConvert.AddRange(items);
                }
            }
        }

        /// <summary>
        /// Process IFC To Structural
        /// </summary>
        private void ProcessIFCToStructural()
        {
            ProcessIFCToStructural_StructuralColumn();
            ProcessIFCToStructural_ArchitectureColumn();
            ProcessIFCToStructural_Floor();
            ProcessIFCToStructural_Beam();
            ProcessIFCToStructural_Wall();
            ProcessIFCToStructural_Ceiling();
            ProcessIFCToStructural_Opening();
        }

        private void AddShareParameter(VMSettingMain settingMain)
        {
            List<SharedParamData> paramDatas = GetSharedParamData(settingMain.SettingCategories);
            RevitUtils.AddShareParameter(App._UIDoc, paramDatas);
        }

        private List<SharedParamData> GetSharedParamData(IEnumerable<VMSettingCategory> settingCategories)
        {
            var paramDatas = new List<SharedParamData>();

            var nameParamVolume = RevitUtils.ChangeNameParam(App._UIDoc, "容積");
            var category = new List<BuiltInCategory> { BuiltInCategory.OST_StairsRailing, BuiltInCategory.OST_PipeCurves, BuiltInCategory.OST_DuctCurves };
#if DEBUG_2023 || RELEASE_2023
            var railingData = new SharedParamData(nameParamVolume, category, SpecTypeId.Volume);
#else
            var railingData = new SharedParamData(nameParamVolume, category, ParameterType.Volume);
#endif
            paramDatas.Add(railingData);

            foreach (var settingCategory in settingCategories)
            {
                if (!settingCategory.IsCheckedGetParamByCategory)
                {
                    continue;
                }

                List<BuiltInCategory> lsBuiltIn = new List<BuiltInCategory>();
                BuiltInCategory builtIn = settingCategory.ProcessBuiltInCategory;
                if (builtIn == BuiltInCategory.OST_Railings)
                {
                    builtIn = BuiltInCategory.OST_StairsRailing;
                    lsBuiltIn.Add(BuiltInCategory.OST_PipeCurves);
                    lsBuiltIn.Add(BuiltInCategory.OST_DuctCurves);
                }
                if (builtIn == BuiltInCategory.OST_ShaftOpening)
                {
                    lsBuiltIn.Add(BuiltInCategory.OST_GenericModel);
                }
#if DEBUG_2020 || RELEASE_2020
                if (builtIn == BuiltInCategory.OST_Ceilings)
                {
                    builtIn = BuiltInCategory.OST_Floors;
                }
#endif
                lsBuiltIn.Add(builtIn);

                var data = paramDatas.FirstOrDefault(x => x.ParamName.Equals(settingCategory.NameParameterInRevit));
                if (data == null)
                {
#if DEBUG_2023 || RELEASE_2023
                    var typeData = SpecTypeId.String.Text;
#else
                    var typeData = ParameterType.Text;
#endif

                    data = new SharedParamData(settingCategory.NameParameterInRevit, lsBuiltIn, typeData);
                    paramDatas.Add(data);
                }
                else
                {
                    data.Categories.AddRange(lsBuiltIn);
                }
            }

            return paramDatas;
        }

        /// <summary>
        /// Process IFC To Structural_StructuralColumn
        /// </summary>
        private void ProcessIFCToStructural_StructuralColumn()
        {
            if (ColumnDatasBeforeConvert.Count <= 0
                || FlagCancel == true
                || FlagSkip == true)
                return;

            foreach (var data in ColumnDatasBeforeConvert)
            {
                FlagCancel = _progressBar.IsCancel;
                FlagSkip = _progressBar.IsSkip;
                if (FlagCancel == true || FlagSkip == true)
                    break;

                _incrementValue++;
                string mess = string.Format("{0} / {1}{2}", _incrementValue, _sumObjectConvert, Define.MESS_PROGESSBAR_OBJECT_COVERT);
                _progressBar.SetMessage(mess);
                _progressBar.IncrementProgressBar();

                using (Transaction tr = new Transaction(_doc, "Convert"))
                {
                    try
                    {
                        FailureHandlingOptions fhOpts = tr.GetFailureHandlingOptions();
                        REVWarning1 supWarning = new REVWarning1(true);
                        fhOpts.SetFailuresPreprocessor(supWarning);
                        tr.SetFailureHandlingOptions(fhOpts);

                        REVWarning2 supWarning2 = new REVWarning2();
                        fhOpts.SetFailuresPreprocessor(supWarning2);
                        tr.SetFailureHandlingOptions(fhOpts);

                        tr.Start();
                        data.Initialize();
                        if (data.ConvertElem?.IsValidObject == true)
                        {
                            RevitUtils.SetValueParamterConvert(App._UIDoc, data.ConvertElem, data.LinkEleData, data.ParameterData);
                            tr.Commit();

                            double angle = RevitUtils.GetRotateFacingElement(data.ConvertElem, data.ColumnLine, data.IFCdata, LinkInstance);
                            if (!RevitUtils.IsEqual(angle, 0) && !RevitUtils.IsEqual(angle, Math.PI))
                            {
                                tr.Start();
                                ElementTransformUtils.RotateElement(_doc, data.ConvertElem.Id, data.ColumnLine, angle);
                                tr.Commit();
                            }
                        }
                        else
                        {
                            tr.RollBack();
                        }
                    }
                    catch (Exception)
                    {
                        if (tr.HasStarted())
                            tr.RollBack();
                    }
                }

                if (data.ConvertElem?.IsValidObject == true)
                {
                    ColumnDatasConverted.Add(data);
                }
                else
                {
                    ColumnDatasNotConverted.Add(data);
                }
            }
        }

        /// <summary>
        /// Process IFC To Structural_ArchitectureColumn
        /// </summary>
        private void ProcessIFCToStructural_ArchitectureColumn()
        {
            if (ArchiColumnDatasBeforeConvert.Count <= 0
                || FlagCancel == true
                || FlagSkip == true)
                return;

            foreach (var data in ArchiColumnDatasBeforeConvert)
            {
                FlagCancel = _progressBar.IsCancel;
                FlagSkip = _progressBar.IsSkip;
                if (FlagCancel == true || FlagSkip == true)
                    break;

                _incrementValue++;
                string mess = string.Format("{0} / {1}{2}", _incrementValue, _sumObjectConvert, Define.MESS_PROGESSBAR_OBJECT_COVERT);
                _progressBar.SetMessage(mess);
                _progressBar.IncrementProgressBar();

                using (Transaction tr = new Transaction(_doc, "Convert"))
                {
                    try
                    {
                        FailureHandlingOptions fhOpts = tr.GetFailureHandlingOptions();
                        REVWarning1 supWarning = new REVWarning1(true);
                        fhOpts.SetFailuresPreprocessor(supWarning);
                        tr.SetFailureHandlingOptions(fhOpts);

                        REVWarning2 supWarning2 = new REVWarning2();
                        fhOpts.SetFailuresPreprocessor(supWarning2);
                        tr.SetFailureHandlingOptions(fhOpts);

                        tr.Start();
                        data.Initialize();

                        if (data.ConvertElem?.IsValidObject == true)
                        {
                            RevitUtils.SetValueParamterConvert(App._UIDoc, data.ConvertElem, data.LinkEleData, data.ParameterData);
                            tr.Commit();

                            double angle = RevitUtils.GetRotateFacingElement(data.ConvertElem, data.ArchiColumnLine, data.IFCdata, LinkInstance);
                            if (!RevitUtils.IsEqual(angle, 0) && !RevitUtils.IsEqual(angle, Math.PI))
                            {
                                tr.Start();
                                ElementTransformUtils.RotateElement(_doc, data.ConvertElem.Id, data.ArchiColumnLine, angle);
                                tr.Commit(fhOpts);
                            }
                        }
                        else
                        {
                            tr.RollBack();
                        }
                    }
                    catch (Exception)
                    {
                        if (tr.HasStarted())
                            tr.RollBack();
                    }
                }

                if (data.ConvertElem?.IsValidObject == true)
                {
                    ArchiColumnDatasConverted.Add(data);
                }
                else
                {
                    ArchiColumnDatasNotConverted.Add(data);
                }
            }
        }

        /// <summary>
        /// Process IFC To Structural_Floor
        /// </summary>
        private void ProcessIFCToStructural_Floor()
        {
            if (FloorDatasBeforeConvert.Count <= 0
                || FlagCancel == true
                || FlagSkip == true)
                return;

            foreach (var data in FloorDatasBeforeConvert)
            {
                FlagCancel = _progressBar.IsCancel;
                FlagSkip = _progressBar.IsSkip;
                if (FlagCancel == true || FlagSkip == true)
                    break;

                _incrementValue++;
                string mess = string.Format("{0} / {1}{2}", _incrementValue, _sumObjectConvert, Define.MESS_PROGESSBAR_OBJECT_COVERT);
                _progressBar.SetMessage(mess);
                _progressBar.IncrementProgressBar();

                using (Transaction tr = new Transaction(_doc, "Convert"))
                {
                    try
                    {
                        FailureHandlingOptions fhOpts = tr.GetFailureHandlingOptions();
                        REVWarning1 supWarning = new REVWarning1(true);
                        fhOpts.SetFailuresPreprocessor(supWarning);
                        tr.SetFailureHandlingOptions(fhOpts);

                        REVWarning2 supWarning2 = new REVWarning2();
                        fhOpts.SetFailuresPreprocessor(supWarning2);
                        tr.SetFailureHandlingOptions(fhOpts);

                        tr.Start();
                        data.Initialize();

                        if (data.ConvertElem?.IsValidObject == true)
                        {
                            RevitUtils.SetValueParamterConvert(App._UIDoc, data.ConvertElem, data.LinkEleData, data.ParameterData);
                            tr.Commit();
                        }
                        else
                        {
                            tr.RollBack();
                        }
                    }
                    catch (Exception)
                    {
                        if (tr.HasStarted())
                            tr.RollBack();
                    }
                }

                if (data.ConvertElem?.IsValidObject == true)
                {
                    FloorDatasConverted.Add(data);
                }
                else
                {
                    FloorDatasNotConverted.Add(data);
                }
            }
        }

        /// <summary>
        /// Process IFC To Structural_Wall
        /// </summary>
        private void ProcessIFCToStructural_Wall()
        {
            if (WallDatasBeforeConvert.Count <= 0
                || FlagCancel == true
                || FlagSkip == true)
                return;

            foreach (var data in WallDatasBeforeConvert)
            {
                FlagCancel = _progressBar.IsCancel;
                FlagSkip = _progressBar.IsSkip;
                if (FlagCancel == true || FlagSkip == true)
                    break;

                _incrementValue++;
                string mess = string.Format("{0} / {1}{2}", _incrementValue, _sumObjectConvert, Define.MESS_PROGESSBAR_OBJECT_COVERT);
                _progressBar.SetMessage(mess);
                _progressBar.IncrementProgressBar();

                using (Transaction tr = new Transaction(_doc, "Convert"))
                {
                    try
                    {
                        FailureHandlingOptions fhOpts = tr.GetFailureHandlingOptions();
                        REVWarning1 supWarning = new REVWarning1(true);
                        fhOpts.SetFailuresPreprocessor(supWarning);
                        tr.SetFailureHandlingOptions(fhOpts);

                        REVWarning2 supWarning2 = new REVWarning2();
                        fhOpts.SetFailuresPreprocessor(supWarning2);
                        tr.SetFailureHandlingOptions(fhOpts);

                        tr.Start();
                        data.Initialize();

                        if (data.ConvertElem?.IsValidObject == true)
                        {
                            RevitUtils.SetValueParamterConvert(App._UIDoc, data.ConvertElem, data.LinkEleData, data.ParameterData);
                            tr.Commit();
                        }
                        else
                        {
                            tr.RollBack();
                        }
                    }
                    catch (Exception)
                    {
                        if (tr.HasStarted())
                            tr.RollBack();
                    }
                }

                if (data.ConvertElem?.IsValidObject == true)
                {
                    WallDatasConverted.Add(data);
                }
                else
                {
                    WallDatasNotConverted.Add(data);
                }
            }
        }

        /// <summary>
        /// Process IFC To Structural_Beam
        /// </summary>
        private void ProcessIFCToStructural_Beam()
        {
            if (BeamDatasBeforeConvert.Count <= 0
                || FlagCancel == true
                || FlagSkip == true)
                return;

            foreach (var data in BeamDatasBeforeConvert)
            {
                FlagCancel = _progressBar.IsCancel;
                FlagSkip = _progressBar.IsSkip;
                if (FlagCancel == true || FlagSkip == true)
                    break;

                _incrementValue++;
                string mess = string.Format("{0} / {1}{2}", _incrementValue, _sumObjectConvert, Define.MESS_PROGESSBAR_OBJECT_COVERT);
                _progressBar.SetMessage(mess);
                _progressBar.IncrementProgressBar();

                using (Transaction tr = new Transaction(_doc, "Convert"))
                {
                    try
                    {
                        FailureHandlingOptions fhOpts = tr.GetFailureHandlingOptions();
                        REVWarning1 supWarning = new REVWarning1(true);
                        fhOpts.SetFailuresPreprocessor(supWarning);
                        tr.SetFailureHandlingOptions(fhOpts);

                        REVWarning2 supWarning2 = new REVWarning2();
                        fhOpts.SetFailuresPreprocessor(supWarning2);
                        tr.SetFailureHandlingOptions(fhOpts);

                        tr.Start();
                        data.CreateBeam();

                        if (data.ConvertElem?.IsValidObject == true)
                        {
                            RevitUtils.SetValueParamterConvert(App._UIDoc, data.ConvertElem, data.LinkEleData, data.ParameterData);
                            tr.Commit();

                            double angle = RevitUtils.GetRotateFacingElement(data.ConvertElem, data.BeamLine, data.IFCdata, LinkInstance);
                            if (!RevitUtils.IsEqual(angle, 0) && !RevitUtils.IsEqual(angle, Math.PI))
                            {
                                tr.Start();
                                data.SetGeometryPositionOfBeam();
                                ElementTransformUtils.RotateElement(_doc, data.ConvertElem.Id, data.BeamLine, angle);
                                tr.Commit();
                            }
                        }
                        else
                        {
                            tr.RollBack();
                        }
                    }
                    catch (Exception)
                    {
                        if (tr.HasStarted())
                            tr.RollBack();
                    }
                }

                if (data.ConvertElem?.IsValidObject == true)
                {
                    BeamDatasConverted.Add(data);
                }
                else
                {
                    BeamDatasNotConverted.Add(data);
                }
            }
        }

        /// <summary>
        /// Process IFC To Structural_Ceiling
        /// </summary>
        private void ProcessIFCToStructural_Ceiling()
        {
            if (CeilingDatasBeforeConvert.Count <= 0
                || FlagCancel == true
                || FlagSkip == true)
                return;

            foreach (var data in CeilingDatasBeforeConvert)
            {
                FlagCancel = _progressBar.IsCancel;
                FlagSkip = _progressBar.IsSkip;
                if (FlagCancel == true || FlagSkip == true)
                    break;

                _incrementValue++;
                string mess = string.Format("{0} / {1}{2}", _incrementValue, _sumObjectConvert, Define.MESS_PROGESSBAR_OBJECT_COVERT);
                _progressBar.SetMessage(mess);
                _progressBar.IncrementProgressBar();

                using (Transaction tr = new Transaction(_doc, "Convert"))
                {
                    try
                    {
                        FailureHandlingOptions fhOpts = tr.GetFailureHandlingOptions();
                        REVWarning1 supWarning = new REVWarning1(true);
                        fhOpts.SetFailuresPreprocessor(supWarning);
                        tr.SetFailureHandlingOptions(fhOpts);

                        REVWarning2 supWarning2 = new REVWarning2();
                        fhOpts.SetFailuresPreprocessor(supWarning2);
                        tr.SetFailureHandlingOptions(fhOpts);

                        tr.Start();
                        data.Initialize();

                        if (data.ConvertElem?.IsValidObject == true)
                        {
                            RevitUtils.SetValueParamterConvert(App._UIDoc, data.ConvertElem, data.LinkEleData, data.ParameterData);
                            tr.Commit();
                        }
                        else
                        {
                            tr.RollBack();
                        }
                    }
                    catch (Exception)
                    {
                        if (tr.HasStarted())
                            tr.RollBack();
                    }
                }

                if (data.ConvertElem?.IsValidObject == true)
                {
                    CeilingDatasConverted.Add(data);
                }
                else
                {
                    CeilingDatasNotConverted.Add(data);
                }
            }
        }

        /// <summary>
        /// Process IFC To opening
        /// </summary>
        private void ProcessIFCToStructural_Opening()
        {
            if (OpeningDatasBeforeConvert.Count <= 0
               || FlagCancel == true
               || FlagSkip == true)
                return;

            foreach (var data in OpeningDatasBeforeConvert)
            {
                FlagCancel = _progressBar.IsCancel;
                FlagSkip = _progressBar.IsSkip;
                if (FlagCancel == true || FlagSkip == true)
                    break;

                _incrementValue++;
                string mess = string.Format("{0} / {1}{2}", _incrementValue, _sumObjectConvert, Define.MESS_PROGESSBAR_OBJECT_COVERT);
                _progressBar.SetMessage(mess);
                _progressBar.IncrementProgressBar();

                using (Transaction tr = new Transaction(_doc, "Convert"))
                {
                    try
                    {
                        FailureHandlingOptions fhOpts = tr.GetFailureHandlingOptions();
                        REVWarning1 supWarning = new REVWarning1(true);
                        fhOpts.SetFailuresPreprocessor(supWarning);
                        tr.SetFailureHandlingOptions(fhOpts);

                        REVWarning2 supWarning2 = new REVWarning2();
                        fhOpts.SetFailuresPreprocessor(supWarning2);
                        tr.SetFailureHandlingOptions(fhOpts);

                        tr.Start();
                        data.Initialize();
                        tr.Commit();

                        if (data.ConvertElem?.IsValidObject == true)
                        {
                            tr.Start();
                            data.ProcessCutElement();
                            tr.Commit();
                        }
                    }
                    catch (Exception)
                    {
                        if (tr.HasStarted())
                            tr.RollBack();
                    }
                }

                if (data.ConvertElem?.IsValidObject == true)
                {
                    OpeningDatasConverted.Add(data);
                }
                else
                {
                    OpeningDatasNotConverted.Add(data);
                }
            }
        }

        #endregion Structural Method

        #endregion Method
    }
}