using Autodesk.Revit.DB;
using System.Collections.Generic;

namespace TepscoIFCToRevit
{
    public class Define
    {
        #region Ribbon

        public static readonly string TEPSCO_IFC_LICENSE_PANELNAME = "TEPSCO ライセンス";
        public static readonly string TEPSCO_IFC_TO_REV_TABNAME = "TEPSCO IFC 変換";
        public static readonly string TEPSCO_IFC_TO_REV_TABNAMEJOIN = "TEPSCO IFC 加入";

        #endregion Ribbon

        #region Command Path

        public static readonly string TEPSCO_IFC_CMD_BEGIN = "TepscoIFCToRevit.Command.BeginCommand";
        public static readonly string TEPSCO_IFC_CMD_MAPPING_SETTING = "TepscoIFCToRevit.Command.MappingSettingCommand";
        public static readonly string TEPSCO_IFC_CMD_CONVERT_IFC_TO_MEP = "TepscoIFCToRevit.Command.ConvertIFCMEPToRevCommand";
        public static readonly string TEPSCO_IFC_CMD_CONVERT_IFC_TO_STRUCTURAL = "TepscoIFCToRevit.Command.ConverIFCStructuralToRevCommand";
        public static readonly string TEPSCO_IFC_CMD_CONVERT_IFC = "TepscoIFCToRevit.Command.IFCConvertCommand";
        public static readonly string TEPSCO_IFC_CMD_JOIN_IFC = "TepscoIFCToRevit.Command.JoinGeometryCommand";
        public static readonly string TEPSCO_IFC_CMD_OBJECT_LIST_IFC = "TepscoIFCToRevit.Command.ObjectListCommand";
        public static readonly string TEPSCO_IFC_CMD_LICENSE = "TepscoIFCToRevit.Command.LoginLicenseCommand";

        #endregion Command Path

        public static readonly string TEPSCO_MESS_HAS_NOT_LINK_FILE = "プロジェクトにリンク IFC ファイルが無い !";
        public static readonly string TEPSCO_MESS_SAVED_SETTING_SUCCESS = "設定情報の保存済み！";
        public static readonly string TEPSCO_MESS_SAVED_SETTING_FAILED = "設定情報が保存できません！";
        public static readonly string TEPSCO_HEADER_GET_ELEMENT_BY_CATEGORY = "カテゴリから要素を読み出す";
        public static readonly string TEPSCO_HEADER_GET_PARAM_BY_CATEGORY = "マテリアルパラメータマッピング ";

        #region Mess Main

        public static readonly string MESS_CONVERT = "変換情報";

        public static readonly string MESS_PIPE_CONVERT = "パイプ変換済み";
        public static readonly string MESS_PIPE_NOT_CONVERT = "配管変換できない";

        public static readonly string MESS_DUCT_CONVERT = "ダクト変換済み";
        public static readonly string MESS_DUCT_NOT_CONVERT = "ダクト変換できない";

        public static readonly string MESS_SUPPORT_CONVERT = "配管サポートを変換済み";
        public static readonly string MESS_SUPPORT_NOT_CONVERT = "配管サポート変換できない";

        public static readonly string MESS_RAILINGS_CONVERT = "手すりが変換されました。";
        public static readonly string MESS_RAILINGS_NOT_CONVERT = "手すりが変換されませんでした。";

        public static readonly string MESS_FITTING_CONVERT = "フィッティング変換済み";
        public static readonly string MESS_FITTING_NOT_CONVERT = "フィッティング変換できない";

        public static readonly string MESS_CONDUITTMNBOX_CONVERT = "電気設備 変換済み";
        public static readonly string MESS_CONDUITTMNBOX_NOT_CONVERT = "電気設備 変換できない";

        public static readonly string MESS_STRUCTURE_COLUMN_CONVERT = "柱（構造）変換済み";
        public static readonly string MESS_STRUCTURE_COLUMN_NOT_CONVERT = "柱 (構造) 変換できない";

        public static readonly string MESS_ARCHITECTURE_COLUMN_CONVERT = "柱（意匠）変換済み";
        public static readonly string MESS_ARCHITECTURE_COLUMN_NOT_CONVERT = "柱 (意匠) 変換できない";

        public static readonly string MESS_FLOOR_CONVERT = "床変換済み";
        public static readonly string MESS_FLOOR_NOT_CONVERT = "床変換できない";

        public static readonly string MESS_CELLING_CONVERT = "天井変換済み";
        public static readonly string MESS_CELLING_NOT_CONVERT = "天井変換できない";

        public static readonly string MESS_BEAM_CONVERT = "梁変換済み";
        public static readonly string MESS_BEAM_NOT_CONVERT = "梁変換できない";

        public static readonly string MESS_WALL_CONVERT = "壁変換済み";
        public static readonly string MESS_WALL_NOT_CONVERT = "壁変換できない";

        public static readonly string MESS_LIST_OBJECT = "オブジェクトリスト";

        public static readonly string MESS_CREATE_TYPE_FLOOR_CHANGE_TYPE_CELLING = "2020は天井の作成をサポートしていないため、ツールは天井の代わりに床を使って、作成します。";

        public static readonly string MESS_LOAD_FAMILY_PIPE_SUPPORT_BEFORE_MAPPING = "画面上でマッピング を実行する前に、配管サポートファミリをロードしてください。";

        public static readonly string MESS_HAS_BEEN_LOAD_FAMILY = "全ての配管サポートファミリがロードされました。";

        public static readonly string MESS_OVERWRITE_FAMILY = "ファミリとタイプを上書きしますが、よろしいですか？";

        #endregion Mess Main

        #region Lable UI

        public static readonly string TITLE_SETTING = "設定";

        // select category to place object
        public static readonly string COLSE_CONTENT = "閉じる";

        public static readonly string CANCEL_CONTENT = "キャンセル";

        public static readonly string Apply_CONTENT = "申し込み";

        public static readonly string TILLE_CONVERT_IFC_TO_REV = "IFCを Revitへ変換";

        public static readonly string HEADER_DATAGRID_SELECT_FILE_IFC = "IFC 名をリンク";
        public static readonly string SELECT_CATEGORY_MEP = "MEPオブジェクトを選択する";
        public static readonly string SELECT_CATEGORY_STRUCTURE = "構造オブジェクトを選択する";

        // string join geomertry

        public static readonly string TILLE_UI_JOINT_GEOMERTRY = "ジオメトリ結合";

        public static readonly string UI_CATERGORY1 = " カテゴリ1 :";
        public static readonly string UI_CATERGORY2 = " カテゴリ2 :";
        public static readonly string UI_CATERGORY3 = " カテゴリ3 :";
        public static readonly string UI_CATERGORY4 = " カテゴリ4 :";

        public static readonly string WARNING_SELECT_CATEGORY_DUPLICATE = "選択したカテゴリが重複しています。再度確認してください。";
        public static readonly string PLEASE_SELECT_OPTIONS = "全てのフィールドを選択してください。";

        public static readonly string CONTENT_COMMAND_LOAD_FAMILY = "ファミリをロード";

        public static readonly string LABLE_AUTO_CREATE_PIPE_SUPPORT = "自動で配管サポートを作成します。";
        public static readonly string LABLE_MANUALLY_CREATE_PIPE_SUPPORT = "手動で配管サポートを作成します。";

        public static readonly string LABLE_CONTENT_SELECT_TYPE = "選択する タイプ";

        public static readonly string LABLE_CONTENT_SELECT_PARAM = "パラメータ選択";

        #endregion Lable UI

        #region NameFamilyPipeSupport

        public List<string> NAME_FAMILY_PIPE_SUPPORT = new List<string>()
        {
        "ケーブルトレイサポート_H形鋼",
        "ケーブルトレイサポート_不等辺山形鋼",
        "ケーブルトレイサポート_溝形鋼",
        "ケーブルトレイサポート_等辺山形鋼",
        "ケーブルトレイサポート_角形鋼管(正方形)",
        };

        #endregion NameFamilyPipeSupport

        #region Tab Headers

        public const string TAB_HEADER_PIPE = "PIPE";
        public const string TAB_HEADER_DUCT = "DUCT";
        public const string TAB_HEADER_PIPINGSUPPORT = "PIPINGSUPPORT";
        public const string TAB_HEADER_ELECTRICALEQUIPMENT = "ELECTRICALEQUIPMENT";

        public static string GetCategoryLabel(BuiltInCategory builtInCategory)
        {
            switch (builtInCategory)
            {
                case BuiltInCategory.INVALID: return string.Empty;
                case BuiltInCategory.OST_PipeCurves: return Define.TAB_HEADER_PIPE;
                case BuiltInCategory.OST_DuctCurves: return Define.TAB_HEADER_DUCT;
                case BuiltInCategory.OST_GenericModel: return Define.TAB_HEADER_PIPINGSUPPORT;
                case BuiltInCategory.OST_ElectricalEquipment: return Define.TAB_HEADER_ELECTRICALEQUIPMENT;
                case BuiltInCategory.OST_CableTray: return Define.TAB_HEADER_CABLETRAY;
                default: return string.Empty;
            }
        }

        #endregion Tab Headers

        #region Progessbar convert

        public static readonly string LABLE_PROCESS = "プロセス";

        public static readonly string MESS_PROGESSBAR_PROCESS_TITLE = "ファミリをロード";

        public static readonly string MESS_PROGESSBAR_PROCESS_LOAD_SETTING = "負荷設定";

        public static readonly string MESS_PROGESSBAR_PROCESS_OBJECT_COVERT = "プロセスオブジェクト変換済み";

        public static readonly string MESS_PROGESSBAR_CREATE_FETTING = "T形フィッティングを作成する";

        public static readonly string MESS_PROGESSBAR_CREATE_TRANSITION_FETTING = "Transition形フィッティングを作成する";

        public static readonly string MESS_PROGESSBAR_OBJECT_COVERT = "オブジェクト変換済み";

        #endregion Progessbar convert

        #region KEY

        public static readonly string TEPSCO_IFC_MAPPING_KEY_PARAMETER_CONTAIN = "Ifc";
        public static readonly string TEPSCO_IFC_REQUEST_HANDLER_KEY = "TEPSCO_IFC_REVIT";

        #endregion KEY

        #region phase 5

        public static readonly string GROUP_SHARE_PARAMETER = "IFCパラメータ";
        public static readonly string TEPSCO_MESS_PICK_OBJECT = "オブジェクトピックエラー";
        public const string TAB_HEADER_CABLETRAY = "ケーブル ラック";
        public static readonly string LABLE_SELECTION_ELEMENT = "選択";
        public static readonly string LABLE_PARAM_IFC = "IFCパラメータ";
        public static readonly string LABLE_CONTENT_SELECT_TYPE_IS_RAILING = "このカテゴリは";
        public static readonly string FileImportSuccess = "ファイルがインプットされました。";
        public static readonly string StatusAlert = "警告";
        public static readonly string FileImportFail = "ファイルの拡張子が正しくありません。";
        public static readonly string StatusErr = "エラー";
        public static readonly string SelectFileImport = "インプットするJSONファイルの選択";
        public static readonly string SelectPathExport = "エクスポートするパスの選択";
        public static readonly string ExportSaveSuccess = "ファイルが保存されました。";
        public static readonly string ExportSaveFail = "ファイルの保存にエラーが発生されました。";
        public static readonly string ImportExcelFile = "エクセルファイルインプット";
        public static readonly string RuleIsNull = "ルールがありません。";
        public static readonly string RuleSelectedIsNull = "ルールが選択されていません。";
        public static readonly string ElementNotExist = "エレメントがありません。";
        public static readonly string FileNotFound = "ファイルがありません。";
        public static readonly string FileNotSupport = "ファイルはオブジェクトリストの正しい形式ではありません。";
        public static readonly string ElementId = "エレメントID";
        public static readonly string ElementType = "タイプ";
        public static readonly string ElementStatus = "ステータス";
        public static readonly string ElementRevitLnkIns = "Revitリンクインスタンス";

        public static readonly string LoadFamilyComplete = "ファミリロード成功";
        public static readonly string btnImportSetting = "設定インポート";
        public static readonly string btnExportSetting = "設定エクスポート";
        public static readonly string RibbonObjectList = "エクセルデータインポート";
        public static readonly string ExportXMLContent = "エクスポート";
        public static readonly string ImportXMLContent = "インポート";

        public static readonly string StatusSuccess = "成功";
        public static readonly string StatusFailed = "失敗";
        public static readonly string RevLnkName = "Revit リンク名";
        public static readonly string RevType = "タイプ";
        public static readonly string RevStatus = "ステータス";
        public static readonly string RevElemId = "エレメントID";
        public static readonly string CloseXMLBeforeSave = "ファイルが開いています。エクスポートする前にファイルを閉じてください。";

        public static readonly string LabelStructure = "構造";
        public static readonly string LabelArchitecture = "建築";
        public static readonly string LabelSystem = "設備";
        //public static readonly string LabelEtc = "...";

        public static readonly string NameMaterialRailings = "E613_手摺り";
        public static readonly string NameSystemType = "手摺";

        public static readonly string FileShareDoesNotExist = "共有パラメータファイルが存在しません";
        public static readonly string LABLE_COUNT_ELEMENT_SELECTED = "選択された要素: ";

        #endregion phase 5

        public static readonly string LOGIN_FAILED = "ログインできませんでした。";
        public static readonly string LOGOUT_FAILED = "ログアウトできませんでした。";
        public static readonly string NAME_FILE_LICENSE = "License.txt";
        public static readonly string LICENSE_INVALID = "ライセンスキーが無効です。";
        public static readonly string LICENSE_EXPIRED = "ライセンスキーの有効期限が切れています。";
        public static readonly string LICENSE_SUCCESS = "ライセンスキーは有効です。";
        public static readonly string LICENSE_LOGOUT = "ライセンスキーがログアウトされています。";
        public static readonly string LICENSE_NULL = "ライセンスキーデータがありません";
        public static readonly string LICENSE_DATA_NULL = "ライセンスデータなし";
        public static readonly string TIME_DATA = "dd/MM/yyyy";
        public static readonly string Warning = "警告";
        public static readonly string Information = "情報";
        public static readonly string Error = "エラー";
        public static readonly string ButtonLogin = "ライセンスキー";
        public static readonly string ButtonLicense = "ライセンス";
    }
}