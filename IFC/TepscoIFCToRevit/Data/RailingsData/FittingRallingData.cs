using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Linq;
using TepscoIFCToRevit.Common;
using TepscoIFCToRevit.Common.ComparerUtils;

namespace TepscoIFCToRevit.Data.RailingsData
{
    public class FittingRallingData
    {
        #region Common

        public static bool IsRectangle(PlanarFace face)
        {
            List<Line> lines = new List<Line>();
            foreach (EdgeArray er in face.EdgeLoops)
                foreach (Edge edge in er)
                    if (edge.AsCurve() is Line line)
                        lines.Add(line);
                    else
                        return false;
            if (lines.Count != 4)
                return false;
            Line lineCheck = lines.FirstOrDefault();
            return lines.Skip(1).Any(x => RevitUtils.IsParallel(x.Direction, lineCheck.Direction) && x.ApproximateLength == lineCheck.ApproximateLength) &&
                    lines.Skip(1).Any(x => x.Direction.DotProduct(lineCheck.Direction) == 0);
        }

        /// <summary>
        /// Get common perpendicular of 2 lines
        /// </summary>
        public static XYZ GetCommonPerpendicular(Line line1, Line line2)
        {
            XYZ normalFace = line1.Direction.CrossProduct(line2.Direction);
            XYZ pointFace = line1.Origin;
            Plane function2Face = Plane.CreateByNormalAndOrigin(normalFace, pointFace);

            XYZ sPoint = RevitUtils.ProjectOnto(function2Face, line2.Origin);
            XYZ ePoint = RevitUtils.ProjectOnto(function2Face, line2.Origin + line2.Direction * 10);

            Line lineProjectionLine2 = Line.CreateBound(sPoint, ePoint);
            XYZ foot1 = RevitUtils.GetUnBoundIntersection(lineProjectionLine2, line1);
            Line heightLine = Line.CreateUnbound(foot1, normalFace);
            XYZ foot2 = RevitUtils.GetUnBoundIntersection(heightLine, line2);

            return (foot1 + foot2) / 2;
        }

        /// <summary>
        /// get all vectices of planarface
        /// </summary>
        public static List<XYZ> GetVertices(PlanarFace planarFace)
        {
            List<XYZ> vertices = new List<XYZ>();
            foreach (EdgeArray edgeloop in planarFace.EdgeLoops)
                foreach (Edge edge in edgeloop)
                {
                    vertices.Add(edge.AsCurve().GetEndPoint(0));
                    if (edge.AsCurve() is Arc)
                        vertices.Add(edge.AsCurve().Evaluate(0.5, true));
                }
            return vertices;
        }

        /// <summary>
        /// calculate center of polygon
        /// </summary>
        public static XYZ GetCenterOfPolygon(List<XYZ> points)
        {
            if (points.Count == 0)
                return null;

            int numPoints = points.Count;
            double sumX = 0, sumY = 0, sumZ = 0;

            foreach (XYZ point in points)
            {
                sumX += point.X;
                sumY += point.Y;
                sumZ += point.Z;
            }

            double centerX = sumX / numPoints;
            double centerY = sumY / numPoints;
            double centerZ = sumZ / numPoints;

            return new XYZ(centerX, centerY, centerZ);
        }

        /// <summary>
        /// count all edges of planarface
        /// </summary>
        public static int CountEdge(PlanarFace planarFace)
        {
            int count = 0;
            foreach (EdgeArray edgeloop in planarFace.EdgeLoops)
                foreach (Edge edge in edgeloop)
                    count++;
            return count;
        }

        /// <summary>
        /// mapping connector
        /// </summary>
        public static void MappingConnector(Document doc, FamilyInstance fitting, List<MEPCurve> pipes)
        {
            if (fitting != null)
            {
                XYZ location = (fitting.Location as LocationPoint).Point;
                List<Connector> connectors = new List<Connector>();
                foreach (MEPCurve pipe in pipes)
                    connectors.Add(GetNearestConnector(pipe.ConnectorManager, location));

                foreach (var connector in connectors)
                {
                    Connector con = GetNearestConnector(fitting.MEPModel.ConnectorManager, connector.Origin);

                    connector.ConnectTo(con);
                }
            }

            doc.Regenerate();
        }

        /// <summary>
        /// Get the neaest conector of connector manager to the given point
        /// </summary>
        public static Connector GetNearestConnector(ConnectorManager connectorManager, XYZ point)
        {
            List<Connector> connectors = new List<Connector>();
            foreach (Connector connector in connectorManager.Connectors)
                connectors.Add(connector);
            return connectors.OrderBy(x => x.Origin.DistanceTo(point)).FirstOrDefault();
        }

        /// <summary>
        /// Get total area of a list of face
        /// </summary>
        public static double SumArea(IEnumerable<double> area)
        {
            double sum = 0;
            area.ForEach(x => sum += x);
            return sum;
        }

        /// <summary>
        /// Get center coplanar surfaces (have tolerance)
        /// </summary>
        public static XYZ GetCenter(List<PlanarFace> faces)
        {
            List<XYZ> points = new List<XYZ>();
            List<double> areas = new List<double>();
            foreach (PlanarFace face in faces)
                points.AddRange(FittingRallingData.GetVertices(face));

            points = points.Distinct(new PointEqualityComparer(1e-5)).ToList();
            XYZ center = new XYZ(0, 0, 0);
            points.ForEach(x => center += x);
            return points.Count > 0 ? center / points.Count : center;
        }

        #endregion Common
    }
}