using Autodesk.Revit.DB;

namespace TepscoIFCToRevit.UI.ViewModels.Interface
{
    internal class HandleWarning : IFailuresPreprocessor
    {
        public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
        {
            failuresAccessor.DeleteAllWarnings();
            return FailureProcessingResult.Continue;
        }
    }
}