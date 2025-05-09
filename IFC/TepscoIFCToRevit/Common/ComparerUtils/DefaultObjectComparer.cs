using System.Collections.Generic;

namespace TepscoIFCToRevit.Common.ComparerUtils
{
    public class DefaultObjectComparer<T> : IEqualityComparer<T> where T : class
    {
        public bool Equals(T x, T y)
        {
            return x.GetHashCode() == y.GetHashCode();
        }

        public int GetHashCode(T obj)
        {
            return base.GetHashCode();
        }
    }
}