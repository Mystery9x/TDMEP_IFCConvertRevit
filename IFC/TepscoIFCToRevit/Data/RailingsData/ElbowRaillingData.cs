using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using RevitUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using TepscoIFCToRevit.Common;
using TepscoIFCToRevit.Common.ComparerUtils;

namespace TepscoIFCToRevit.Data.RailingsData
{
    public class ElbowRaillingData
    {
        public Transform Transform { get; set; }
        public MEP_CURVE_TYPE ElbowType { get; set; }
        public XYZ Centroid { get; set; }
        public double Angle { get; set; }
        public double Radius { get; set; }
        public XYZ Vector0 { get; set; }
        public XYZ Center0 { get; set; }
        public XYZ Vector1 { get; set; }
        public XYZ Center1 { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public ElbowRaillingData(Element element, MEP_CURVE_TYPE elbowType, Transform transform = null, List<MEPCurve> pipes = null, Solid solid = null)
        {
            ElbowType = elbowType;
            Transform = transform;
            GetInfoElbow(element, pipes, solid);
        }

        public static FamilyInstance CreateElbow(Document _doc, List<MEPCurve> pipes, Element elementFitting, Transform transform, Solid solid = null)
        {
            FamilyInstance result = null;

            try
            {
                MEP_CURVE_TYPE elbowType = pipes.FirstOrDefault() is Pipe ? MEP_CURVE_TYPE.PIPE : ((pipes[0] as Duct).DuctType.Shape == ConnectorProfileType.Round ? MEP_CURVE_TYPE.ROUND_DUCT : MEP_CURVE_TYPE.DUCT);
                ElbowRaillingData elbow = new ElbowRaillingData(elementFitting, elbowType, transform, pipes, solid);

                using (Transaction tran = new Transaction(_doc))
                {
                    try
                    {
                        tran.Start("elbow");
                        FamilySymbol familySymbol = new FilteredElementCollector(_doc).OfClass(typeof(FamilySymbol))
                                                                                  .WhereElementIsElementType()
                                                                                  .Cast<FamilySymbol>()
                                                                                  .FirstOrDefault(x => x.Name == (elbow.ElbowType == MEP_CURVE_TYPE.PIPE ? "Family-PipeFitting-Elbow" :
                                                                                                                elbow.ElbowType == MEP_CURVE_TYPE.ROUND_DUCT ? "DuctFitting_Round_Elbow" : "DuctFitting_Rectangle_Elbow"));
                        if (!familySymbol.IsActive) familySymbol.Activate();

                        #region create and set value to parameters

                        FamilyInstance fitting = _doc.Create.NewFamilyInstance(elbow.Centroid, familySymbol, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
                        _doc.Regenerate();
                        SetValueToParameter(fitting, elbow, pipes[0]);

                        _doc.Regenerate();

                        #endregion create and set value to parameters

                        if (fitting != null)//fitting == null when parameter are NOT satify, error or warning show up
                        {
                            #region move center0 to the correct position

                            //move center0 to the correct position
                            ElbowRaillingData newElbow = new ElbowRaillingData(fitting, elbowType);
                            ElementTransformUtils.MoveElement(_doc, fitting.Id, elbow.Center0 - newElbow.Center0);
                            _doc.Regenerate();

                            #endregion move center0 to the correct position

                            #region rotate vector0 to the correct position

                            //rotate vector0 to the correct position
                            XYZ normal0 = newElbow.Vector0.CrossProduct(elbow.Vector0).Normalize();
                            if (!normal0.IsZeroLength())
                            {
                                double rotation0 = newElbow.Vector0.AngleTo(elbow.Vector0);
                                ElementTransformUtils.RotateElement(_doc, fitting.Id, Line.CreateUnbound(elbow.Center0, normal0), rotation0);
                            }
                            else if (newElbow.Vector0.IsAlmostEqualTo(elbow.Vector0.Negate(), 1e-5))
                            {
                                normal0 = elbow.Vector0.CrossProduct(elbow.Vector1).Normalize();
                                if (normal0.IsZeroLength())
                                    normal0 = elbow.Vector0.CrossProduct(new XYZ(elbow.Vector0.X, elbow.Vector0.Y, elbow.Vector0.Z + 1)).Normalize();// just make sure normal is not zero length
                                ElementTransformUtils.RotateElement(_doc, fitting.Id, Line.CreateUnbound(elbow.Center0, normal0), Math.PI);
                            }
                            _doc.Regenerate();
                            newElbow = new ElbowRaillingData(fitting, elbowType);

                            #endregion rotate vector0 to the correct position

                            #region rotate vector1 to the correct position

                            //projection of center1, rotate with axis vector0
                            Plane plane = Plane.CreateByNormalAndOrigin(elbow.Vector0, elbow.Center0);
                            XYZ projectOrg = RevitUtils.ProjectOnto(plane, elbow.Center1);
                            XYZ vectorOrg = (projectOrg - elbow.Center0).Normalize();
                            XYZ projectNew = RevitUtils.ProjectOnto(plane, newElbow.Center1);
                            XYZ vectorNew = (projectNew - elbow.Center0).Normalize();
                            double rotation1 = vectorNew.AngleTo(vectorOrg);
                            XYZ normal1 = vectorNew.CrossProduct(vectorOrg).Normalize();
                            if (!normal1.IsZeroLength())
                                ElementTransformUtils.RotateElement(_doc, fitting.Id, Line.CreateUnbound(elbow.Center0, normal1), rotation1);
                            else if (vectorNew.IsAlmostEqualTo(vectorOrg.Negate(), 1e-5))
                                ElementTransformUtils.RotateElement(_doc, fitting.Id, Line.CreateUnbound(elbow.Center0, elbow.Vector0), Math.PI);

                            #endregion rotate vector1 to the correct position

                            _doc.Regenerate();
                            FittingRallingData.MappingConnector(_doc, fitting, pipes);
                        }
                        result = fitting.IsValidObject ? fitting : null;
                        tran.Commit();
                    }
                    catch (Exception)
                    {
                        tran.RollBack();
                    }
                }
            }
            catch (Exception) { }

            return result != null && result.IsValidObject ? result : null;
        }

        /// <summary>
        /// Set value to parameter
        /// </summary>
        /// <param name="fitting"></param>
        /// <param name="elbow"></param>
        /// <param name="pipe"></param>
        private static void SetValueToParameter(Element fitting, ElbowRaillingData elbow, MEPCurve pipe)
        {
            fitting.LookupParameter("Angle").Set(elbow.Angle);
            fitting.LookupParameter("Radius").Set(elbow.Radius);
            if (elbow.ElbowType == MEP_CURVE_TYPE.PIPE ||
                elbow.ElbowType == MEP_CURVE_TYPE.ROUND_DUCT)
            {
                fitting.LookupParameter("Diameter").Set(pipe.Diameter);
            }
            else
            {
                fitting.LookupParameter("Width").Set(pipe.Width);
                fitting.LookupParameter("Height").Set(pipe.Height);
            }
        }

        /// <summary>
        ///
        /// </summary>
        private void GetInfoElbow(Element element, List<MEPCurve> pipes = null, Solid solid = null)
        {
            try
            {
                solid = solid ?? UtilsSolid.GetTotalSolid(element);
                List<PlanarFace> faces = new List<PlanarFace>();
                foreach (Face face in solid.Faces)
                    if (face is PlanarFace pl) faces.Add(pl);

                var order = (ElbowType == MEP_CURVE_TYPE.PIPE || ElbowType == MEP_CURVE_TYPE.ROUND_DUCT) ?
                                        faces.OrderByDescending(x => FittingRallingData.CountEdge(x)).ToList() :
                                        faces.Where(x => FittingRallingData.IsRectangle(x)).ToList();

                //_______________ Get directions of the elbow ______________________________
                Vector0 = order[0].FaceNormal;
                Vector1 = order[1].FaceNormal;

                Center0 = FittingRallingData.GetCenterOfPolygon(FittingRallingData.GetVertices(order[0]));
                Center1 = FittingRallingData.GetCenterOfPolygon(FittingRallingData.GetVertices(order[1]));

                /*there are a few cases that the elbow was exported to a shape look like mesh.
                Then the face in order to connect to the pipe is not a single face.
                We have to join the faces that are coplanar together */
                if (ElbowType == MEP_CURVE_TYPE.PIPE || ElbowType == MEP_CURVE_TYPE.ROUND_DUCT)
                {
                    var groupFaces = faces.GroupBy(x => x.FaceNormal, new VectorEqualityComparer(0, 0.1)).ToList();
                    if (pipes?.Count >= 2
                       && pipes[0].Location is LocationCurve lc1
                        && lc1.Curve is Line linePipe1
                        && pipes[1].Location is LocationCurve lc2
                        && lc2.Curve is Line linePipe2)
                    {
                        XYZ pipe0Dir = linePipe1.Direction;
                        XYZ pipe1Dir = linePipe2.Direction;

                        groupFaces = groupFaces.Where(x => RevitUtils.IsParallel(x.Key, pipe0Dir, 0.01)
                                                      || RevitUtils.IsParallel(x.Key, pipe1Dir, 0.01)).ToList();
                    }

                    groupFaces = groupFaces.OrderByDescending(x => FittingRallingData.SumArea(x.Select(y => y.Area)))
                                .ToList();

                    double area0 = 0;
                    double area1 = 0;

                    if (groupFaces.Count >= 1 && groupFaces[0].Count() > 0)
                    {
                        Vector0 = new XYZ();
                        foreach (var face in groupFaces[0])
                        {
                            Vector0 = (face.FaceNormal + Vector0).Normalize();
                            area0 += face.Area;
                        }
                        Center0 = FittingRallingData.GetCenter(groupFaces[0].ToList());
                    }
                    if (groupFaces.Count >= 2 && groupFaces[1].Count() > 0)
                    {
                        Vector1 = new XYZ();
                        foreach (var face in groupFaces[1])
                        {
                            Vector1 = (face.FaceNormal + Vector1).Normalize();
                            area1 += face.Area;
                        }
                        Center1 = FittingRallingData.GetCenter(groupFaces[1].ToList());
                    }

                    /*there is a case that elbow hase angle 180. Vector0 and Vector1 are equal beacuse they was joined in a same group.
                     we have to split this group to 2 face different*/
                    if (Math.Round(area0, 4) != Math.Round(area1, 4) || Vector0.IsAlmostEqualTo(Vector1, 1e-5))
                        CaseElbowIsAHalfOfCircle(groupFaces[0].ToList());
                }

                // ExchangeInfor(pipe0);
                // ExchangeInfor(pipe1);

                //_______________ Get information Centroid, Radius, Angle ______________________________

                Line line0 = Line.CreateUnbound(Center0, Vector0);
                Line line1 = Line.CreateUnbound(Center1, Vector1);
                XYZ center = RevitUtils.GetUnBoundIntersection(line0, line1);//appear tolerance, return null
                if (center == null && RevitUtils.IsParallel(Vector0, Vector1))// case: pipe parallel, elbow is a half circle
                {
                    center = (Center0 + Center1) / 2;
                    Centroid = center;
                    Radius = (center.DistanceTo(Center0) + center.DistanceTo(Center1)) / 2;// average tolerance
                }
                else
                {
                    if (center == null)// case: Extremely small tolerance, lines don't intersect
                        center = FittingRallingData.GetCommonPerpendicular(line0, line1);
                    Centroid = center;

                    XYZ normal = Vector0.CrossProduct(Vector1).Normalize();
                    XYZ dir0 = normal.CrossProduct(Vector0).Normalize();
                    XYZ dir1 = normal.CrossProduct(Vector1).Normalize();

                    Line l0 = Line.CreateUnbound(Center0, dir0);
                    Line l1 = Line.CreateUnbound(Center1, dir1);
                    center = RevitUtils.GetUnBoundIntersection(l0, l1);//appear tolerance, return null

                    if (center == null) // case: Extremely small tolerance, lines don't intersect
                        center = FittingRallingData.GetCommonPerpendicular(l0, l1);

                    Radius = (center.DistanceTo(Center0) + center.DistanceTo(Center1)) / 2;// average tolerance
                }

                Angle = Math.PI - Vector0.AngleTo(Vector1);
                if (Angle == 0) Angle = Math.PI;

                ApplyTransform();
            }
            catch (Exception) { }
        }

        private void CaseElbowIsAHalfOfCircle(List<PlanarFace> faces)
        {
            List<PlanarFace> list0 = GetFaceAdjacent(ref faces);
            List<PlanarFace> list1 = GetFaceAdjacent(ref faces);

            Vector0 = new XYZ();
            foreach (var face in list0)
                Vector0 = (face.FaceNormal + Vector0).Normalize();
            Center0 = FittingRallingData.GetCenter(list0.ToList());

            Vector1 = new XYZ();
            foreach (var face in list1)
                Vector1 = (face.FaceNormal + Vector1).Normalize();
            Center1 = FittingRallingData.GetCenter(list1.ToList());
        }

        private List<PlanarFace> GetFaceAdjacent(ref List<PlanarFace> faces)
        {
            List<PlanarFace> ret = new List<PlanarFace>();
            List<XYZ> points = new List<XYZ>();
            for (int i = faces.Count - 1; i > -1; i--)
            {
                List<XYZ> vertices = FittingRallingData.GetVertices(faces[i]);

                if (ret.Count == 0)
                {
                    ret.Add(faces[i]);
                    points.AddRange(vertices);
                    faces.RemoveAt(i);
                }
                else if (points.Any(x => vertices.Any(y => x.IsAlmostEqualTo(y, 1e-5))))
                {
                    ret.Add(faces[i]);
                    points.AddRange(vertices);
                    points = points.Distinct(new PointEqualityComparer()).ToList();
                    faces.RemoveAt(i);
                }
            }

            return ret;
        }

        private void ApplyTransform()
        {
            if (Transform != null)
            {
                Centroid = Transform.OfPoint(Centroid);
                Center1 = Transform.OfPoint(Center1);
                Center0 = Transform.OfPoint(Center0);
                Vector0 = Transform.OfVector(Vector0);
                Vector1 = Transform.OfVector(Vector1);
            }
        }

        /// <summary>
        /// Only Get information form pipe input
        /// </summary>
        /// <param name="pipe"></param>
        private void ExchangeInfor(MEPCurve pipe)
        {
            if (pipe != null)
            {
                XYZ point;
                XYZ vector;
                Line line = (pipe.Location as LocationCurve).Curve as Line;
                if (line.GetEndPoint(0).DistanceTo((Center0 + Center1) / 2) > line.GetEndPoint(1).DistanceTo((Center0 + Center1) / 2))
                {
                    point = line.GetEndPoint(1);
                    vector = line.Direction.Negate();
                }
                else
                {
                    point = line.GetEndPoint(0);
                    vector = line.Direction;
                }

                if (Center0.DistanceTo(point) < Center1.DistanceTo(point))
                {
                    Center0 = point;
                    Vector0 = vector;
                }
                else
                {
                    Center1 = point;
                    Vector1 = vector;
                }
            }
        }
    }
}