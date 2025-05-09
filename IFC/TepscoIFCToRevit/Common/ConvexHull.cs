using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Linq;

namespace TepscoIFCToRevit.Common
{
    public class ConvexHull
    {
        public static double Cross(XYZ O, XYZ A, XYZ B)
        {
            return (A.X - O.X) * (B.Y - O.Y) - (A.Y - O.Y) * (B.X - O.X);
        }

        public static List<XYZ> GetConvexHull(List<XYZ> lstXYZ)
        {
            if (lstXYZ == null)
                return null;

            if (lstXYZ.Count() <= 1)
                return lstXYZ;

            int n = lstXYZ.Count(), k = 0;
            List<XYZ> H = new List<XYZ>(new XYZ[2 * n]);

            lstXYZ.Sort((a, b) =>
                 a.X == b.X ? a.Y.CompareTo(b.Y) : a.X.CompareTo(b.X));

            // Build lower hull
            for (int i = 0; i < n; ++i)
            {
                while (k >= 2 && Cross(H[k - 2], H[k - 1], lstXYZ[i]) <= 0)
                    k--;
                H[k++] = lstXYZ[i];
            }

            // Build upper hull
            for (int i = n - 2, t = k + 1; i >= 0; i--)
            {
                while (k >= t && Cross(H[k - 2], H[k - 1], lstXYZ[i]) <= 0)
                    k--;
                H[k++] = lstXYZ[i];
            }

            return H.Take(k - 1).ToList();
        }
    }
}