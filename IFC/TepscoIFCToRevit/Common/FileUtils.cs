using Autodesk.Revit.DB;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace TepscoIFCToRevit.Common
{
    public class FileUtils
    {
        public static void LoadResources()
        {
#if DEBUG_2020
            Assembly.LoadFrom("C:\\Users\\trong\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2020\\TepscoIFCToRevit\\MaterialDesignThemes.Wpf.dll");
            Assembly.LoadFrom("C:\\Users\\trong\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2020\\TepscoIFCToRevit\\MaterialDesignColors.dll");
            Assembly.LoadFrom("C:\\Users\\trong\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2020\\TepscoIFCToRevit\\RevitUtilities.dll");
#elif DEBUG_2023
            Assembly.LoadFrom("C:\\Users\\trong\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2023\\TepscoIFCToRevit\\MaterialDesignThemes.Wpf.dll");
            Assembly.LoadFrom("C:\\Users\\trong\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2023\\TepscoIFCToRevit\\MaterialDesignColors.dll");
            Assembly.LoadFrom("C:\\Users\\trong\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2023\\TepscoIFCToRevit\\RevitUtilities.dll");
#else
            Assembly.LoadFrom(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "MaterialDesignThemes.Wpf.dll"));
            Assembly.LoadFrom(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "MaterialDesignColors.dll"));
            Assembly.LoadFrom(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "RevitUtilities.dll"));
#endif
        }

        public static string GetAddinFolder()
        {
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        public static string GetFamilyFolder()
        {
#if DEBUG_2020
            return "C:\\Users\\trong\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2020\\TepscoIFCToRevit\\Family";
#elif DEBUG_2023
            return "C:\\Users\\trong\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2023\\TepscoIFCToRevit\\Family";
#else
            return Path.Combine(GetAddinFolder(), "Family");

#endif
        }

        public static string GetIconsFolder()
        {
            return Path.Combine(GetAddinFolder(), "Icons");
        }

        public static string GetIconsRevitFolder()
        {
            return Path.Combine(GetIconsFolder(), "IconRevit");
        }

        public static string GetPipingSupportFamilyFolder()
        {
#if DEBUG_2020
            return "C:\\Users\\trong\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2020\\TepscoIFCToRevit\\Family\\PipingSupport";
#elif DEBUG_2023
            return "C:\\Users\\trong\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2023\\TepscoIFCToRevit\\Family\\PipingSupport";
#else
            return Path.Combine(GetFamilyFolder(), "PipingSupport");
#endif
        }

        public static string GetOpeningFamilyFolder()
        {
#if DEBUG_2020
            return "C:\\Users\\trong\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2020\\TepscoIFCToRevit\\Family\\Opening";
#elif DEBUG_2023
            return "C:\\Users\\trong\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2023\\TepscoIFCToRevit\\Family\\Opening";
#else
            return Path.Combine(GetFamilyFolder(), "Opening");
#endif
        }

        public static string GetElectricalEquipmentFamilyFolder()
        {
#if DEBUG_2020
            return "C:\\Users\\trong\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2020\\TepscoIFCToRevit\\Family\\ElectricalEquipment";
#elif DEBUG_2023
            return "C:\\Users\\trong\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2023\\TepscoIFCToRevit\\Family\\ElectricalEquipment";
#else
            return Path.Combine(GetFamilyFolder(), "ElectricalEquipment");
#endif
        }

        public static string GetRailingsFamilyFolder()
        {
#if DEBUG_2020
            return "C:\\Users\\trong\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2020\\TepscoIFCToRevit\\Family\\Pipe-Duct-Fitting";
#elif DEBUG_2023
            return "C:\\Users\\trong\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2023\\TepscoIFCToRevit\\Family\\Pipe-Duct-Fitting";
#else
            return Path.Combine(GetFamilyFolder(), "Pipe-Duct-Fitting");
#endif
        }

        public static string GetFileShareParamFolder()
        {
#if DEBUG_2020
            return "C:\\Users\\trong\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2020\\TepscoIFCToRevit\\IFCShareParameter.txt";
#elif DEBUG_2023
            return "C:\\Users\\trong\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2023\\TepscoIFCToRevit\\IFCShareParameter.txt";
#else
            return Path.Combine(GetAddinFolder(), "IFCShareParameter.txt");
#endif
        }

        public static string GetFileShareParamVolumeFolder()
        {
#if DEBUG_2020
            return "C:\\Users\\trong\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2020\\TepscoIFCToRevit\\IFCShareParameterVolume.txt";
#elif DEBUG_2023
            return "C:\\Users\\trong\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2023\\TepscoIFCToRevit\\IFCShareParameterVolume.txt";
#else
            return Path.Combine(GetAddinFolder(), "IFCShareParameterVolume.txt");
#endif
        }

        public static string GetFileIconDownFolder()
        {
#if DEBUG_2020
            return "C:\\Users\\trong\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2020\\TepscoIFCToRevit\\Icons\\img\\down.png";
#elif DEBUG_2023
            return "C:\\Users\\trong\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2023\\TepscoIFCToRevit\\Icons\\img\\down.png";
#else
            return Path.Combine(GetIconsFolder(), "down.png");
#endif
        }

        public static string GetFileIconRevitFolder(BuiltInCategory builtInCategory)
        {
#if DEBUG_2020
            if (builtInCategory == BuiltInCategory.OST_PipeCurves)
                return "C:\\Users\\trong\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2020\\TepscoIFCToRevit\\Icons\\IconRevit\\Pipe.png";
            else if (builtInCategory == BuiltInCategory.OST_DuctCurves)
                return "C:\\Users\\trong\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2020\\TepscoIFCToRevit\\Icons\\IconRevit\\Duct.png";
            else if (builtInCategory == BuiltInCategory.OST_CableTray)
                return "C:\\Users\\trong\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2020\\TepscoIFCToRevit\\Icons\\IconRevit\\CableTray.png";
            else if (builtInCategory == BuiltInCategory.OST_GenericModel)
                return "C:\\Users\\trong\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2020\\TepscoIFCToRevit\\Icons\\IconRevit\\PipingSupport.png";
            else if (builtInCategory == BuiltInCategory.OST_Columns)
                return "C:\\Users\\trong\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2020\\TepscoIFCToRevit\\Icons\\IconRevit\\Column.png";
            else if (builtInCategory == BuiltInCategory.OST_ElectricalEquipment)
                return "C:\\Users\\trong\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2020\\TepscoIFCToRevit\\Icons\\IconRevit\\Electric.png";
            else if (builtInCategory == BuiltInCategory.OST_StructuralFraming)
                return "C:\\Users\\trong\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2020\\TepscoIFCToRevit\\Icons\\IconRevit\\Beam.png";
            else if (builtInCategory == BuiltInCategory.OST_Floors)
                return "C:\\Users\\trong\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2020\\TepscoIFCToRevit\\Icons\\IconRevit\\Floor.png";
            else if (builtInCategory == BuiltInCategory.OST_Ceilings)
                return "C:\\Users\\trong\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2020\\TepscoIFCToRevit\\Icons\\IconRevit\\Ceiling.png";
            else if (builtInCategory == BuiltInCategory.OST_StructuralColumns)
                return "C:\\Users\\trong\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2020\\TepscoIFCToRevit\\Icons\\IconRevit\\StructureColumn.png";
            else if (builtInCategory == BuiltInCategory.OST_Walls)
                return "C:\\Users\\trong\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2020\\TepscoIFCToRevit\\Icons\\IconRevit\\Wall.png";
            else if (builtInCategory == BuiltInCategory.OST_Railings)
                return "C:\\Users\\trong\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2020\\TepscoIFCToRevit\\Icons\\IconRevit\\Railing.png";
            else if (builtInCategory == BuiltInCategory.OST_PipeAccessory)
                return "C:\\Users\\trong\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2020\\TepscoIFCToRevit\\Icons\\IconRevit\\Accessory.png";
            else
                return "C:\\Users\\trong\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2020\\TepscoIFCToRevit\\Icons\\IconRevit\\Opening.png";
#elif DEBUG_2023
            if (builtInCategory == BuiltInCategory.OST_PipeCurves)
                return "C:\\Users\\trong\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2023\\TepscoIFCToRevit\\Icons\\IconRevit\\Pipe.png";
            else if (builtInCategory == BuiltInCategory.OST_DuctCurves)
                return "C:\\Users\\trong\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2023\\TepscoIFCToRevit\\Icons\\IconRevit\\Duct.png";
            else if (builtInCategory == BuiltInCategory.OST_CableTray)
                return "C:\\Users\\trong\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2023\\TepscoIFCToRevit\\Icons\\IconRevit\\CableTray.png";
            else if (builtInCategory == BuiltInCategory.OST_GenericModel)
                return "C:\\Users\\trong\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2023\\TepscoIFCToRevit\\Icons\\IconRevit\\PipingSupport.png";
            else if (builtInCategory == BuiltInCategory.OST_Columns)
                return "C:\\Users\\trong\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2023\\TepscoIFCToRevit\\Icons\\IconRevit\\Column.png";
            else if (builtInCategory == BuiltInCategory.OST_ElectricalEquipment)
                return "C:\\Users\\trong\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2023\\TepscoIFCToRevit\\Icons\\IconRevit\\Electric.png";
            else if (builtInCategory == BuiltInCategory.OST_StructuralFraming)
                return "C:\\Users\\trong\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2023\\TepscoIFCToRevit\\Icons\\IconRevit\\Beam.png";
            else if (builtInCategory == BuiltInCategory.OST_Floors)
                return "C:\\Users\\trong\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2023\\TepscoIFCToRevit\\Icons\\IconRevit\\Floor.png";
            else if (builtInCategory == BuiltInCategory.OST_Ceilings)
                return "C:\\Users\\trong\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2023\\TepscoIFCToRevit\\Icons\\IconRevit\\Ceiling.png";
            else if (builtInCategory == BuiltInCategory.OST_StructuralColumns)
                return "C:\\Users\\trong\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2023\\TepscoIFCToRevit\\Icons\\IconRevit\\StructureColumn.png";
            else if (builtInCategory == BuiltInCategory.OST_Walls)
                return "C:\\Users\\trong\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2023\\TepscoIFCToRevit\\Icons\\IconRevit\\Wall.png";
            else if (builtInCategory == BuiltInCategory.OST_Railings)
                return "C:\\Users\\trong\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2023\\TepscoIFCToRevit\\Icons\\IconRevit\\Railing.png";
            else if (builtInCategory == BuiltInCategory.OST_PipeAccessory)
                return "C:\\Users\\trong\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2023\\TepscoIFCToRevit\\Icons\\IconRevit\\Accessory.png";
            else
                return "C:\\Users\\trong\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2023\\TepscoIFCToRevit\\Icons\\IconRevit\\Opening.png";
#else
            if (builtInCategory == BuiltInCategory.OST_PipeCurves)
                return Path.Combine(GetIconsRevitFolder(), "Pipe.png");
            else if (builtInCategory == BuiltInCategory.OST_DuctCurves)
                return Path.Combine(GetIconsRevitFolder(), "Duct.png");
            else if (builtInCategory == BuiltInCategory.OST_CableTray)
                return Path.Combine(GetIconsRevitFolder(), "CableTray.png");
            else if (builtInCategory == BuiltInCategory.OST_GenericModel)
                return Path.Combine(GetIconsRevitFolder(), "PipingSupport.png");
            else if (builtInCategory == BuiltInCategory.OST_Columns)
                return Path.Combine(GetIconsRevitFolder(), "Column.png");
            else if (builtInCategory == BuiltInCategory.OST_ElectricalEquipment)
                return Path.Combine(GetIconsRevitFolder(), "Electric.png");
            else if (builtInCategory == BuiltInCategory.OST_StructuralFraming)
                return Path.Combine(GetIconsRevitFolder(), "Beam.png");
            else if (builtInCategory == BuiltInCategory.OST_Floors)
                return Path.Combine(GetIconsRevitFolder(), "Floor.png");
            else if (builtInCategory == BuiltInCategory.OST_Ceilings)
                return Path.Combine(GetIconsRevitFolder(), "Ceiling.png");
            else if (builtInCategory == BuiltInCategory.OST_StructuralColumns)
                return Path.Combine(GetIconsRevitFolder(), "StructureColumn.png");
            else if (builtInCategory == BuiltInCategory.OST_Walls)
                return Path.Combine(GetIconsRevitFolder(), "Wall.png");
            else if (builtInCategory == BuiltInCategory.OST_Railings)
                return Path.Combine(GetIconsRevitFolder(), "Railing.png");
            else if (builtInCategory == BuiltInCategory.OST_PipeAccessory)
                return Path.Combine(GetIconsRevitFolder(), "Accessory.png");
            else
                return Path.Combine(GetIconsRevitFolder(), "Opening.png");
#endif
        }

        public static void ReadJSONCad2DData(string cadDataPath, out Dictionary<string, List<string>> scaffoldData)
        {
            if (File.Exists(cadDataPath))
            {
                string fileText = File.ReadAllText(cadDataPath);
                scaffoldData = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(fileText);

                if (scaffoldData == null)
                    scaffoldData = new Dictionary<string, List<string>>();
            }
            else
                scaffoldData = new Dictionary<string, List<string>>();
        }
    }
}