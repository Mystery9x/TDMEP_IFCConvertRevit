using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using RevitUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using TepscoIFCToRevit.Common;

namespace TepscoIFCToRevit.Data.RailingsData
{
    public class CrossRailingData
    {
        public Transform Transform { get; set; }
        public MEP_CURVE_TYPE CrossroadType { get; set; }
        public XYZ Location { get; set; }
        public List<XYZ> Vectors { get; set; }

        public CrossRailingData(Element element,
                                MEP_CURVE_TYPE crossType,
                                Transform transform = null,
                                List<MEPCurve> pipes = null,
                                Solid solid = null)
        {
            CrossroadType = crossType;
            Transform = transform;
            GetInfoCrossroad(element, pipes, solid);
        }

        /// <summary>
        /// Get direction of 4 pipe connect to fitting
        /// </summary>
        private void GetInfoCrossroad(Element element, List<MEPCurve> pipes, Solid solid = null)
        {
            try
            {
                solid = solid != null ? solid : UtilsSolid.GetTotalSolid(element);
                List<XYZ> nodes = new List<XYZ>();
                if (pipes != null && pipes.Count == 4)
                {
                    //to create a cross family with 4 pipe have already existed
                    Line line0 = (pipes[0].Location as LocationCurve).Curve as Line;
                    Line line1 = (pipes[1].Location as LocationCurve).Curve as Line;
                    Line line2 = (pipes[2].Location as LocationCurve).Curve as Line;
                    Line line3 = (pipes[3].Location as LocationCurve).Curve as Line;
                    XYZ centroid = Transform == null ? solid.ComputeCentroid() : Transform.OfPoint(solid.ComputeCentroid());

                    ConnectorManager connectorManager0 = pipes[0].ConnectorManager;
                    ConnectorManager connectorManager1 = pipes[1].ConnectorManager;
                    ConnectorManager connectorManager2 = pipes[2].ConnectorManager;
                    ConnectorManager connectorManager3 = pipes[3].ConnectorManager;
                    XYZ node0 = FittingRallingData.GetNearestConnector(connectorManager0, centroid).Origin;
                    XYZ node1 = FittingRallingData.GetNearestConnector(connectorManager1, centroid).Origin;
                    XYZ node2 = FittingRallingData.GetNearestConnector(connectorManager2, centroid).Origin;
                    XYZ node3 = FittingRallingData.GetNearestConnector(connectorManager3, centroid).Origin;
                    nodes = new List<XYZ>() { node0, node1, node2, node3 };

                    foreach (Line line in new List<Line>() { line1, line2, line3 })
                    {
                        if (!RevitUtils.IsParallel(line.Direction, line0.Direction, 1e-1))
                        {
                            XYZ normal = line.Direction.CrossProduct(line0.Direction).Normalize();
                            Plane plane = Plane.CreateByNormalAndOrigin(normal, (node0 + node1 + node2 + node3) / 4);

                            XYZ projection0_Start = RevitUtils.ProjectOnto(plane, line0.GetEndPoint(0));
                            XYZ projection0_End = RevitUtils.ProjectOnto(plane, line0.GetEndPoint(1));
                            XYZ projection1_Start = RevitUtils.ProjectOnto(plane, line1.GetEndPoint(0));
                            XYZ projection1_End = RevitUtils.ProjectOnto(plane, line1.GetEndPoint(1));

                            Location = RevitUtils.GetUnBoundIntersection(Line.CreateBound(projection0_Start, projection0_End), Line.CreateBound(projection1_Start, projection1_End));
                            break;
                        }
                    }
                }

                Vectors = new List<XYZ>();
                Vectors.Add(nodes[0] - Location);
                Vectors.Add(nodes[1] - Location);
                Vectors.Add(nodes[2] - Location);
                Vectors.Add(nodes[3] - Location);

                ApplyTransform();
            }
            catch (Exception) { }
        }

        private void ApplyTransform()
        {
            if (Transform != null)
            {
                // Location = Transform.OfPoint(Location);
                Vectors = Vectors.ConvertAll(x => Transform.OfVector(x));
            }
        }

        public static FamilyInstance CreateCrossRoad(Document _doc, Element element, List<MEPCurve> pipes, Transform transform, Solid solid = null)
        {
            FamilyInstance fitting = null;

            try
            {
                MEP_CURVE_TYPE crossType = pipes.FirstOrDefault() is Pipe ? MEP_CURVE_TYPE.PIPE :
                                                                      ((pipes[0] as Duct).DuctType.Shape == ConnectorProfileType.Rectangular ? MEP_CURVE_TYPE.DUCT : MEP_CURVE_TYPE.ROUND_DUCT);
                CrossRailingData crossroad = new CrossRailingData(element, crossType, transform, pipes, solid);
                FamilySymbol familySymbol = new FilteredElementCollector(_doc).OfClass(typeof(FamilySymbol))
                                                                              .WhereElementIsElementType()
                                                                              .Cast<FamilySymbol>()
                                                                              .FirstOrDefault(x => x.Name == (crossroad.CrossroadType == MEP_CURVE_TYPE.PIPE ? "Family-PipeFitting-Crossroads" :
                                                                                                        crossroad.CrossroadType == MEP_CURVE_TYPE.DUCT ? "DuctFitting_Rectangle_Cross" : "DuctFitting_Round_Cross"));
                using (Transaction tr = new Transaction(_doc))
                {
                    tr.Start("Create cross");
                    try
                    {
                        if (!familySymbol.IsActive) familySymbol.Activate();
                        fitting = _doc.Create.NewFamilyInstance(crossroad.Location, familySymbol, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);

                        //rotate main direciotn
                        XYZ mainDir = crossroad.Vectors.FirstOrDefault().Normalize();
                        XYZ normal0 = fitting.HandOrientation.CrossProduct(mainDir).Normalize();
                        double angle0 = fitting.HandOrientation.AngleTo(mainDir);
                        if (!normal0.IsZeroLength())
                            ElementTransformUtils.RotateElement(_doc, fitting.Id, Line.CreateUnbound(crossroad.Location, normal0), angle0);
                        else if (mainDir.IsAlmostEqualTo(fitting.HandOrientation.Negate(), 1e-5))
                        {
                            normal0 = fitting.HandOrientation.CrossProduct(fitting.FacingOrientation).Normalize();
                            ElementTransformUtils.RotateElement(_doc, fitting.Id, Line.CreateUnbound(crossroad.Location, normal0), Math.PI);
                        }

                        //rotate branch direction
                        _doc.Regenerate();
                        XYZ branchDir = crossroad.Vectors.FirstOrDefault(x => !RevitUtils.IsParallel(x, mainDir)).Normalize();
                        Plane plane = Plane.CreateByNormalAndOrigin(fitting.HandOrientation, crossroad.Location);
                        XYZ org = RevitUtils.ProjectOnto(plane, crossroad.Location + branchDir);
                        XYZ current = RevitUtils.ProjectOnto(plane, crossroad.Location + fitting.FacingOrientation);
                        double angle1 = (crossroad.Location - current).AngleTo(crossroad.Location - org);
                        if (Math.Round(angle1, 5) != 0 && Math.Round(angle1, 5) != Math.Round(Math.PI, 5))
                        {
                            XYZ normal1 = (crossroad.Location - current).Normalize().CrossProduct((crossroad.Location - org).Normalize());
                            ElementTransformUtils.RotateElement(_doc, fitting.Id, Line.CreateUnbound(crossroad.Location, normal1), angle1);
                        }
                        _doc.Regenerate();
                        if (branchDir.AngleTo(fitting.FacingOrientation) > Math.PI / 2)
                            branchDir = branchDir.Negate();

                        _doc.Regenerate();
                        SetParameter(crossroad, fitting, mainDir, branchDir, pipes[0]);
                        _doc.Regenerate();
                        FittingRallingData.MappingConnector(_doc, fitting, pipes);

                        tr.Commit();
                    }
                    catch (Exception)
                    {
                        tr.RollBack();
                    }
                }
            }
            catch (Exception)
            {
            }

            return fitting != null && fitting.IsValidObject ? fitting : null;
        }

        private static void SetParameter(CrossRailingData crossroad, Element fitting, XYZ mainDir, XYZ branchDir, MEPCurve pipe)
        {
            double angle = branchDir.AngleTo(mainDir);
            if (crossroad.CrossroadType == MEP_CURVE_TYPE.PIPE ||
                crossroad.CrossroadType == MEP_CURVE_TYPE.ROUND_DUCT)
            {
                fitting.LookupParameter("Main Diameter").Set(pipe.Diameter);
                fitting.LookupParameter("Branch Diameter").Set(pipe.Diameter);
            }
            else
            {
                fitting.LookupParameter("Width").Set(pipe.Width);
                fitting.LookupParameter("Height").Set(pipe.Height);
            }
            double tolerance = Math.PI / 180;
            fitting.LookupParameter("Branch pipe length 1").Set(crossroad.Vectors.FirstOrDefault(x => x.Normalize().AngleTo(branchDir) <= tolerance).GetLength());
            fitting.LookupParameter("Branch pipe length 2").Set(crossroad.Vectors.FirstOrDefault(x => x.Normalize().AngleTo(branchDir.Negate()) <= tolerance).GetLength());
            fitting.LookupParameter("Intersection to the leftside").Set(crossroad.Vectors.FirstOrDefault(x => x.Normalize().AngleTo(mainDir.Negate()) <= tolerance).GetLength());
            fitting.LookupParameter("Intersection to the rightside").Set(crossroad.Vectors.FirstOrDefault(x => x.Normalize().AngleTo(mainDir) <= tolerance).GetLength());
            fitting.LookupParameter("Angle").Set(angle);
        }
    }
}