using Autodesk.Revit.DB;
using System.Collections.Generic;
using TepscoIFCToRevit.Data.MEPData;

namespace TepscoIFCToRevit.Common.ComparerUtils
{
    public class PipeEqualityComparer : IEqualityComparer<PipeData>
    {
        public bool Equals(PipeData x, PipeData y)
        {
            if (x == null || y == null)
                return false;

            int xTypeId = x.ProcessPipeType.Id.IntegerValue;
            int yTypeId = y.ProcessPipeType.Id.IntegerValue;

            double xDiameter = x.DiameterPipe;
            double yDiameter = y.DiameterPipe;

            XYZ xStartPoint = x.StartPoint;
            XYZ yStartPoint = y.StartPoint;

            XYZ xEndPoint = x.EndPoint;
            XYZ yEndPoint = y.EndPoint;

            return xTypeId == yTypeId
                   && RevitUtilities.Common.IsEqual(xDiameter, yDiameter)
                   && RevitUtilities.Common.IsEqual(xStartPoint, yStartPoint)
                   && RevitUtilities.Common.IsEqual(xEndPoint, yEndPoint);
        }

        public int GetHashCode(PipeData obj)
        {
            return 0;
        }
    }
}