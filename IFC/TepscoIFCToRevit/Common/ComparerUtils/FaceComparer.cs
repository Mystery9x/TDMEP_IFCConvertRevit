using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;

namespace TepscoIFCToRevit.Common.ComparerUtils
{
    public class FaceComparer : IComparer<PlanarFace>
    {
        public int Compare(PlanarFace face1, PlanarFace face2)
        {
            // So sánh theo trục FaceNormal

            double dotProduct = 0;
            try
            {
                dotProduct = face1.FaceNormal.DotProduct(face2.FaceNormal);
            }
            catch (Exception)
            { }

            if (dotProduct == 1.0)
            {
                // Nếu trục FaceNormal giống nhau, thì so sánh theo khoảng cách giữa các face
                double distance1 = face1.Origin.DistanceTo(face2.Origin);
                double distance2 = face2.Origin.DistanceTo(face1.Origin);
                return distance1.CompareTo(distance2);
            }
            else
            {
                // Nếu trục FaceNormal khác nhau, thì so sánh bằng phương thức DotProduct() của lớp Vector
                return dotProduct.CompareTo(1.0);
            }
        }
    }
}