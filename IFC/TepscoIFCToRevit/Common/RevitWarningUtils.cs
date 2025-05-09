using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Linq;

namespace TepscoIFCToRevit.Common
{
    public class REVWarning1 : IFailuresPreprocessor
    {
        public static bool _isApply;

        public REVWarning1(bool isApply)
        {
            _isApply = isApply;
        }

        public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
        {
            if (_isApply)
            {
                var messages = failuresAccessor.GetFailureMessages();
                if (messages.Count() > 0)
                {
                    foreach (FailureMessageAccessor message in messages)
                    {
                        var lstId = message.GetFailingElementIds();
                        failuresAccessor.DeleteWarning(message);
                    }
                }
            }

            return FailureProcessingResult.Continue;
        }
    }

    public class REVWarning2 : IFailuresPreprocessor
    {
        public bool _hasError = false;

        public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
        {
            string transactionName = failuresAccessor.GetTransactionName();

            IList<FailureMessageAccessor> fmas = failuresAccessor.GetFailureMessages();

            if (fmas.Count == 0)
            {
                return FailureProcessingResult.Continue;
            }

            bool isResolved = false;

            foreach (FailureMessageAccessor fma in fmas)
            {
                if (fma.HasResolutions())
                {
                    failuresAccessor.ResolveFailure(fma);
                    isResolved = true;
                }

                if (fma.GetSeverity() == FailureSeverity.Error ||
                    fma.GetSeverity() == FailureSeverity.DocumentCorruption)
                    _hasError = true;
            }

            failuresAccessor.DeleteAllWarnings();

            if (isResolved)
            {
                return FailureProcessingResult.ProceedWithCommit;
            }

            return FailureProcessingResult.Continue;
        }
    }

    public class REVWarning3 : IFailuresPreprocessor
    {
        public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
        {
            IList<FailureMessageAccessor> failList = failuresAccessor.GetFailureMessages();
            if (failList.Count > 0)
            {
                failuresAccessor.DeleteAllWarnings();
                if (failuresAccessor.GetFailureMessages().Count > 0)
                {
                    return FailureProcessingResult.WaitForUserInput;
                }
                else
                {
                    return FailureProcessingResult.ProceedWithCommit;
                }
            }

            return FailureProcessingResult.Continue;
        }
    }
}