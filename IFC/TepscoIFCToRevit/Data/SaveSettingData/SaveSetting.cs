using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using RevitUtilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using TepscoIFCToRevit.UI.ViewModels;
using TepscoIFCToRevit.UI.ViewModels.VMMappingSetting;

namespace TepscoIFCToRevit.Data.SaveSettingData
{
    public class SaveSetting
    {
        [JsonProperty("SAVE_MAPPING_SETTING", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public virtual List<SaveMappingSetting> SaveMappingSetting { get; set; }

        public static SaveSetting FromJson(string json) => JsonConvert.DeserializeObject<SaveSetting>(json, Converter.Settings);

        public static string ToJson(SaveSetting self) => JsonConvert.SerializeObject(self, Converter.Settings);

        #region Get setting from json

        public static VMSettingMain GetMainSettings(List<RevitLinkInstance> revLnkInss)
        {
            string szFromJsonSetting = string.Empty;
            try
            {
                szFromJsonSetting = Properties.Settings.Default.SettingMapping;
            }
            catch (Exception) { }

            // try to restore saved settings from previous sesion
            SaveSetting saveSetting = string.IsNullOrWhiteSpace(szFromJsonSetting)
                                    ? null
                                    : FromJson(szFromJsonSetting);

            if (saveSetting?.SaveMappingSetting != null)
                return DeserializeObjectFromJsonSetting(App._UIDoc, revLnkInss, App.Global_IFCObjectMergeData, saveSetting);
            else
                return SetDefaultSettingMain(App._UIDoc, revLnkInss, App.Global_IFCObjectMergeData);
        }

        public static VMSettingMain ImportMainSetting(List<RevitLinkInstance> revLnkIns, string szFromJsonSetting)
        {
            // try to restore saved settings from previous sesion
            SaveSetting saveSetting = string.IsNullOrWhiteSpace(szFromJsonSetting)
                                    ? null
                                    : FromJson(szFromJsonSetting);

            if (saveSetting?.SaveMappingSetting != null)
                return DeserializeObjectFromJsonSetting(App._UIDoc, revLnkIns, App.Global_IFCObjectMergeData, saveSetting);
            else
                return SetDefaultSettingMain(App._UIDoc, revLnkIns, App.Global_IFCObjectMergeData);
        }

        /// <summary>
        /// Deserialize Object From Json Setting
        /// </summary>
        /// <param name="uIDocument"></param>
        /// <param name="revLnkIns"></param>
        /// <param name="iFCObjectMergeData"></param>
        /// <param name="fromJson"></param>
        /// <returns></returns>
        public static VMSettingMain DeserializeObjectFromJsonSetting(UIDocument uIDocument,
                                                              List<RevitLinkInstance> revLnkIns,
                                                              IFCObjectData iFCObjectMergeData,
                                                              SaveSetting saveSetting)
        {
            try
            {
                List<VMSettingCategory> categories = new List<VMSettingCategory>();

                foreach (var saveCat in saveSetting.SaveMappingSetting)
                {
                    int builtIn = saveCat.ProcessBuiltInCategory;

                    VMSettingCategory vMSetCat = new VMSettingCategory(uIDocument, saveCat)
                    {
                        IsCheckedGetEleByCategory = saveCat.IsCheckedGetEleByCategory,
                        IsCheckedGetParamByCategory = saveCat.IsCheckedGetParamByCategory
                    };

                    var settingTypes = vMSetCat.SettingType;
                    if (settingTypes?.Count > 0)
                        vMSetCat.SelType = settingTypes.FirstOrDefault(item => item.Id.IntegerValue == saveCat.SelTypeCaseGetByCategory);
                    vMSetCat.BeginSelTypeCaseGetByCategoryId = saveCat.SelTypeCaseGetByCategory;

                    vMSetCat.ToggleGroupSelection = saveCat.ToggelBtnGrp;

                    var groups = new List<VMSettingGroup>();
                    foreach (var saveGrp in saveCat.SettingGrps)
                    {
                        VMSettingGroup vMSetGrp;
                        if (!saveGrp.IsGroupSelection)
                        {
                            vMSetGrp = new VMSettingGroupCondition(vMSetCat, saveGrp);
                            (vMSetGrp as VMSettingGroupCondition).ContentGroup = vMSetGrp;

                            foreach (var saveObj in saveGrp.SettingObjs)
                            {
                                IFCObjectData source = new IFCObjectData(uIDocument, revLnkIns, iFCObjectMergeData)
                                {
                                    KeyFormat_Contain = saveObj.FlagContain,
                                    KeyFormat_Equal = saveObj.FlagEqual,
                                    KeyValue = saveObj.KeyValue
                                };

                                VMSettingIfc vMSetObj = new VMSettingIfc(source, vMSetGrp);

                                if (source.KeyParameters.Count > 0)
                                    vMSetObj.SelParaKey = source.KeyParameters.FirstOrDefault(item => item.Name == saveObj.SelParaKey);

                                vMSetObj.BeginSelParaKey = saveObj.SelParaKey;
                                vMSetGrp.SettingObjs.Add(vMSetObj);
                            }
                        }
                        else
                        {
                            vMSetGrp = new VMSettingGroupSelection(vMSetCat, saveGrp);

                            foreach (var saveObj in saveGrp.SettingObjs)
                            {
                                IFCObjectData source = new IFCObjectData(uIDocument, revLnkIns, iFCObjectMergeData)
                                {
                                    KeyFormat_Contain = saveObj.FlagContain,
                                    KeyFormat_Equal = saveObj.FlagEqual,
                                    KeyValue = saveObj.KeyValue
                                };

                                VMSettingIfc vMSetObj = new VMSettingIfc(source, vMSetGrp);

                                if (source.KeyParameters.Count > 0)
                                    vMSetObj.SelParaKey = source.KeyParameters.FirstOrDefault(item => item.Name == saveObj.SelParaKey);

                                vMSetObj.BeginSelParaKey = saveObj.SelParaKey;

                                (vMSetGrp as VMSettingGroupSelection).ContentGroup = vMSetGrp as VMSettingGroupSelection;
                                (vMSetGrp as VMSettingGroupSelection).IsGroupSelection = saveGrp.IsGroupSelection;
                                (vMSetGrp as VMSettingGroupSelection).LstSelectElemId = saveObj.LstElementIdSel;
                                if (saveObj.LstElementIdSel?.Count > 0)
                                {
                                    vMSetObj.CountElementSelected = Define.LABLE_COUNT_ELEMENT_SELECTED + (saveObj.LstElementIdSel.Count);
                                }

                                vMSetGrp.SettingObjs.Add(vMSetObj);
                            }
                        }

                        var setTypeIteams = vMSetGrp.SettingTypeItems;
                        if (saveGrp.Type.Count > 0
                            && saveGrp.Type[0].SelType != int.MinValue
                            && setTypeIteams.Count > 0
                            && setTypeIteams[0].SettingSymbolObjs.Count > 0)
                        {
                            setTypeIteams[0].SelectedSymbol = setTypeIteams[0].SettingSymbolObjs
                                                                              .FirstOrDefault(item => item.Id.IntegerValue == saveGrp.Type[0].SelType);
                            vMSetGrp.BeginSelTypeId = saveGrp.Type[0].SelType;

                            if (setTypeIteams[0].SelectedSymbol == null)
                                setTypeIteams[0].SelectedSymbol = setTypeIteams[0].SettingSymbolObjs.FirstOrDefault();

                            if (vMSetCat.ProcessBuiltInCategory == BuiltInCategory.OST_Railings)
                            {
                                setTypeIteams[0].SelectedSymbol = setTypeIteams[0].SettingSymbolObjs
                                                                              .FirstOrDefault(item => item.Name == saveGrp.Type[0].NameType);
                            }

                            if (vMSetCat.ProcessBuiltInCategory == BuiltInCategory.OST_ElectricalEquipment)
                            {
                                setTypeIteams[0].SelectedSymbol = setTypeIteams[0].SettingSymbolObjs
                                                                              .FirstOrDefault(item => item.Name == saveGrp.Type[0].NameType);
                            }

                            setTypeIteams[0].SelectedFamily = setTypeIteams[0].Families?
                                                                          .FirstOrDefault(item => item.Name == saveGrp.Type[0].NameFamily);
                        }

                        vMSetCat.SettingGrps.Add(vMSetGrp);
                    }
                    categories.Add(vMSetCat);
                }

                return SetDefaultSettingMain(App._UIDoc, revLnkIns, App.Global_IFCObjectMergeData, categories);
            }
            catch (Exception e)
            {
                IO.ShowException(e);
            }
            return null;
        }

        /// <summary>
        /// Set Default Setting Main
        /// </summary>
        /// <param name="uIDocument"></param>
        /// <param name="revLnkInss"></param>
        /// <param name="iFCObjectMergeData"></param>
        /// <returns></returns>
        public static VMSettingMain SetDefaultSettingMain(UIDocument uIDocument,
                                                          List<RevitLinkInstance> revLnkInss,
                                                          IFCObjectData iFCObjectMergeData,
                                                          List<VMSettingCategory> settings = null)
        {
            try
            {
                if (uIDocument != null || revLnkInss?.Count > 0)
                {
                    VMSettingMain result = settings?.Count > 0 ? new VMSettingMain(settings) : new VMSettingMain(new List<VMSettingCategory>());
                    List<BuiltInCategory> cats = new List<BuiltInCategory>()
                        {
                             BuiltInCategory.OST_PipeCurves,
                             BuiltInCategory.OST_DuctCurves,
                             BuiltInCategory.OST_StructuralColumns,
                             BuiltInCategory.OST_Columns,
                             BuiltInCategory.OST_Floors,
                             BuiltInCategory.OST_Walls,
                             BuiltInCategory.OST_StructuralFraming,

                             BuiltInCategory.OST_Ceilings,
                             BuiltInCategory.OST_GenericModel,
                             BuiltInCategory.OST_ElectricalEquipment,
                             BuiltInCategory.OST_Railings,
                             BuiltInCategory.OST_CableTray,
                             BuiltInCategory.OST_PipeAccessory,
                             BuiltInCategory.OST_ShaftOpening
                        };

                    foreach (var cat in cats)
                    {
                        if (result.SettingCategories.Any(x => x.ProcessBuiltInCategory.Equals(cat)))
                        {
                            continue;
                        }

                        VMSettingCategory catViewModel = new VMSettingCategory(uIDocument, (int)cat, true);
                        VMSettingGroup groupViewModel = SetDefaultSettingGroup(uIDocument, catViewModel, revLnkInss, iFCObjectMergeData);
                        catViewModel.SettingGrps.Add(groupViewModel);

                        result.SettingCategories.Add(catViewModel);
                    }

                    return result;
                }
            }
            catch (Exception) { }
            return null;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="uIDocument"></param>
        /// <param name="vMSetCat"></param>
        /// <param name="revLnkInss"></param>
        /// <param name="iFCObjectMergeData"></param>
        /// <returns></returns>
        public static VMSettingGroup SetDefaultSettingGroup(UIDocument uIDocument,
                                                            VMSettingCategory vMSetCat,
                                                            List<RevitLinkInstance> revLnkInss,
                                                            IFCObjectData iFCObjectMergeData)
        {
            try
            {
                VMSettingGroup result = new VMSettingGroup(vMSetCat);
                IFCObjectData mappingData = new IFCObjectData(uIDocument, revLnkInss, iFCObjectMergeData);
                VMSettingIfc vMSetObjDefault = new VMSettingIfc(mappingData, result);
                result.SettingObjs.Add(vMSetObjDefault);
                return result;
            }
            catch (Exception)
            { }
            return null;
        }

        #endregion Get setting from json
    }

    public static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters =
            {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }
}