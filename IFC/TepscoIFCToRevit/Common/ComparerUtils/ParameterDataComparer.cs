using System.Collections.Generic;
using TepscoIFCToRevit.Data;

namespace TepscoIFCToRevit.Common.ComparerUtils
{
    public class ParameterDataComparer : IEqualityComparer<ParameterData>
    {
        public bool Equals(ParameterData data1, ParameterData data2)
        {
            // So sánh theo trục FaceNormal

            if (data1?.ProcessParameter != null
                && data2?.ProcessParameter != null
                && !string.IsNullOrWhiteSpace(data1.Name)
                && !string.IsNullOrWhiteSpace(data2.Name))
            {
                return data1.Name.Equals(data2.Name);
            }
            return false;
        }

        public int GetHashCode(ParameterData obj)
        {
            return 0;
        }
    }
}