using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using TepscoIFCToRevit.UI.Views.LicenseUI;

namespace TepscoIFCToRevit.Command
{
    [Transaction(TransactionMode.Manual)]
    public class LoginLicenseCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            LicenseUI infoForm = new LicenseUI();
            infoForm.ShowDialog();

            return Result.Succeeded;
        }
    }
}