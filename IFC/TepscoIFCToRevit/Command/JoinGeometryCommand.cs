using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.IO;
using System.Windows;
using TepscoIFCToRevit.Common;
using TepscoIFCToRevit.UI.Views;
using Application = Autodesk.Revit.ApplicationServices.Application;

namespace TepscoIFCToRevit.Command
{
    [Transaction(TransactionMode.Manual)]
    internal class JoinGeometryCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            //if (!CheckLicenseUltils.CheckLicense())
            //{
            //    return Result.Cancelled;
            //}

            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;

            JoinGeometryUI wpfForm = new JoinGeometryUI(doc);
            wpfForm.ShowDialog();

            return Result.Succeeded;
        }
    }
}