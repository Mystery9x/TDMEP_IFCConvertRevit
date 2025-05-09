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
    public class TeeRailingsData
    {
        public Transform Transform { get; set; }
        public MEP_CURVE_TYPE TeeType { get; set; }
        public double LengthMain { get; set; }
        public double LengthBranch { get; set; }
        public double Angle { get; set; }
        public XYZ Location { get; set; }
        public XYZ CenterBranch { get; set; }
        public XYZ MainDirection { get; set; }
        public XYZ BranchDirection { get; set; }

        public TeeRailingsData(Element element, MEP_CURVE_TYPE teeType, Transform transform = null, List<Solid> solids = null)
        {
            TeeType = teeType;
            Transform = transform;
            GetInfor(element, solids);
        }

        private void GetInfor(Element element, List<Solid> solids = null)
        {
            solids = solids != null ? solids : UtilsSolid.GetAllSolids(element);
            List<PlanarFace> faces = new List<PlanarFace>();
            foreach (Solid solid in solids)
                foreach (Face face in solid.Faces)
                    if (face is PlanarFace pl) faces.Add(pl);

            if (TeeType == MEP_CURVE_TYPE.PIPE ||
                TeeType == MEP_CURVE_TYPE.ROUND_DUCT)
                InforPipeTeeFitting(faces);
            else
                InforDuctTeeFitting(faces);

            /*Reverse maindirection base on branch direction
             The direction of the pipes are always chosen according to a certain rule
             -> Caculate angle between main direction and branch direction*/
            Line lineMain = Line.CreateUnbound(Location, MainDirection);
            XYZ projection = lineMain.Project(CenterBranch).XYZPoint;
            if (projection != null && !projection.IsAlmostEqualTo(Location))
                MainDirection = (projection - Location).Normalize();//direction from location to projection
            BranchDirection = (CenterBranch - Location).Normalize();
            Angle = MainDirection.AngleTo(BranchDirection);

            ApplyTransform();
        }

        /// <summary>
        /// Get information about duct direction, duct center point, length of branches and location
        /// </summary>
        /// <param name="faces"></param>
        private void InforDuctTeeFitting(List<PlanarFace> faces)
        {
            //get rectangle face
            List<PlanarFace> recs = faces.Where(x => FittingRallingData.CountEdge(x) == 4).ToList();
            //<face0, face1, distance of 2 center, coordinate midpoint of 2 center>
            Tuple<PlanarFace, PlanarFace, double, XYZ> pair = null;
            int countLoop = 0;
            while (recs.Count > 1 && countLoop <= faces.Count)
            {
                PlanarFace face = recs.FirstOrDefault();
                XYZ centerFace = FittingRallingData.GetCenterOfPolygon(FittingRallingData.GetVertices(face));
                for (int i = 1; i < recs.Count; i++)
                {
                    PlanarFace rec = recs[i];
                    XYZ centerRec = FittingRallingData.GetCenterOfPolygon(FittingRallingData.GetVertices(rec));

                    Plane plane = Plane.CreateByNormalAndOrigin(rec.FaceNormal, centerRec);
                    if (Math.Round(rec.Area, 5) == Math.Round(face.Area, 5) &&
                        RevitUtils.ProjectOnto(plane, centerFace).IsAlmostEqualTo(centerRec, 1e-5))
                    {
                        double distance = centerFace.DistanceTo(centerRec);
                        if (pair == null || pair.Item3 < distance)
                        {
                            pair = new Tuple<PlanarFace, PlanarFace, double, XYZ>(rec, face, distance, (centerFace + centerRec) / 2);
                            recs.Remove(rec);
                            break;
                        }
                    }
                }
                recs.Remove(face);
                countLoop++;
            }

            MainDirection = pair.Item1.FaceNormal;
            Location = pair.Item4;
            PlanarFace branchFace = FindBranchFace(faces);
            BranchDirection = branchFace.FaceNormal;
            CenterBranch = FittingRallingData.GetCenterOfPolygon(FittingRallingData.GetVertices(branchFace));
            LengthMain = pair.Item3;
            LengthBranch = Location.DistanceTo(CenterBranch);
        }

        /// <summary>
        /// Get information about pipe direction, pipe center point, length of branches and location
        /// </summary>
        /// <param name="faces"></param>
        private void InforPipeTeeFitting(List<PlanarFace> faces)
        {
            //----
            //this codes handle the case mesh faces were exported
            List<IGrouping<XYZ, PlanarFace>> group = faces.GroupBy(x => x.FaceNormal, new VectorEqualityComparer(0, 0.1)).ToList();
            var pairs = group.GroupBy(x => x.Key, new VectorEqualityComparer(180, 0.1)).Where(x => x.Count() == 2);
            var mainPair = pairs.OrderByDescending(x => FittingRallingData.GetCenter(x.ToList()[0].ToList()).DistanceTo(FittingRallingData.GetCenter(x.ToList()[1].ToList()))).FirstOrDefault();

            XYZ Vector0 = new XYZ();
            XYZ Vector1 = new XYZ();

            foreach (var face in mainPair.FirstOrDefault())
                Vector0 = (face.FaceNormal + Vector0).Normalize();
            foreach (var face in mainPair.LastOrDefault())
                Vector1 = (face.FaceNormal + Vector1).Normalize();
            XYZ Center0 = FittingRallingData.GetCenter(mainPair.FirstOrDefault().ToList());
            XYZ Center1 = FittingRallingData.GetCenter(mainPair.LastOrDefault().ToList());
            //----

            if (RevitUtils.IsParallel(Vector0, Vector1))
            {
                LengthMain = Center0.DistanceTo(Center1);
                Location = (Center0 + Center1) / 2;
                MainDirection = Vector0;
            }

            var groupBranch = group.Where(x => IsBranchFace(x))
                                  .OrderByDescending(x => FittingRallingData.GetCenter(x.ToList()).DistanceTo(Location))
                                  .FirstOrDefault();
            BranchDirection = new XYZ();
            CenterBranch = FittingRallingData.GetCenter(groupBranch.ToList());
            foreach (var face in groupBranch)
                BranchDirection = (face.FaceNormal + BranchDirection).Normalize();
            LengthBranch = Location.DistanceTo(CenterBranch);
        }

        private bool IsBranchFace(IGrouping<XYZ, PlanarFace> input)
        {
            bool isParallel = !RevitUtils.IsParallel(input.Key, MainDirection, 1e-4);

            Plane plane = Plane.CreateByNormalAndOrigin(input.Key, Location);
            XYZ center = FittingRallingData.GetCenter(input.ToList());
            XYZ projection = RevitUtils.ProjectOnto(plane, center);
            bool isCenterDuplicate = Location.IsAlmostEqualTo(projection, 1e-4);
            return isParallel && isCenterDuplicate;
        }

        /// <summary>
        /// Find the branch face
        /// The branch face is the futhest face from its center to the Location,
        /// and the projection from its center  to the plane that is create by normal: MainDirection, origin: Location is duplicate with the Location point.
        /// </summary>
        /// <param name="faces"></param>
        /// <returns></returns>
        private PlanarFace FindBranchFace(List<PlanarFace> faces)
        {
            PlanarFace branchFace = null;
            double maxDistance = 0;
            foreach (PlanarFace face in faces)
            {
                Plane plane = Plane.CreateByNormalAndOrigin(face.FaceNormal, face.Origin);
                XYZ projecttion = RevitUtils.ProjectOnto(plane, Location);
                XYZ centerOfFace = FittingRallingData.GetCenterOfPolygon(FittingRallingData.GetVertices(face));
                double distance = centerOfFace.DistanceTo(Location);
                if (!RevitUtils.IsParallel(MainDirection, face.FaceNormal, 1e-5) &&
                        projecttion.IsAlmostEqualTo(centerOfFace, 1e-5) &&
                        (branchFace == null || branchFace != null && distance > maxDistance))
                {
                    branchFace = face;
                    maxDistance = distance;
                }
            }
            return branchFace;
        }

        private void ApplyTransform()
        {
            if (Transform != null)
            {
                Location = Transform.OfPoint(Location);
                CenterBranch = Transform.OfPoint(CenterBranch);
                MainDirection = Transform.OfVector(MainDirection);
                BranchDirection = Transform.OfVector(BranchDirection);
            }
        }

        public static FamilyInstance CreateTeeFitting(Document doc, List<MEPCurve> pipes, Element elementFitting, Transform transform, List<Solid> solids = null)
        {
            FamilyInstance fitting = null;
            try
            {
                MEP_CURVE_TYPE teeType = pipes.FirstOrDefault() is Pipe ? MEP_CURVE_TYPE.PIPE : ((pipes[0] as Duct).DuctType.Shape == ConnectorProfileType.Round ? MEP_CURVE_TYPE.ROUND_DUCT : MEP_CURVE_TYPE.DUCT);
                TeeRailingsData tee = new TeeRailingsData(elementFitting, teeType, transform, solids);

                using (Transaction tran = new Transaction(doc))
                {
                    tran.Start("tee fitting");
                    try
                    {
                        FamilySymbol familySymbol = new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol))
                                                                                      .WhereElementIsElementType()
                                                                                      .Cast<FamilySymbol>()
                                                                                      .FirstOrDefault(x => x.Name == (tee.TeeType == MEP_CURVE_TYPE.PIPE ?
                                                                                                             "Family-Pipe-Fitting-Tee" :
                                                                                                            tee.TeeType == MEP_CURVE_TYPE.ROUND_DUCT ? "DuctFitting_Round_Wye" : "DuctFitting_Rectangle_Tee"));
                        if (!familySymbol.IsActive) familySymbol.Activate();
                        fitting = doc.Create.NewFamilyInstance(tee.Location, familySymbol, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
                        doc.Regenerate();
                        SetParameter(fitting, pipes, tee);
                        doc.Regenerate();

                        if (fitting != null && fitting.IsValidObject)//fitting == null when parameter are NOT satify, error or warning show up
                        {
                            #region rotate main direction to the correct position

                            //rotate main direction to the correct position
                            doc.Regenerate();
                            TeeRailingsData newTee = new TeeRailingsData(fitting, teeType);
                            XYZ normal = newTee.MainDirection.CrossProduct(tee.MainDirection).Normalize();
                            if (!normal.IsZeroLength())
                            {
                                double rotation = newTee.MainDirection.AngleTo(tee.MainDirection);
                                ElementTransformUtils.RotateElement(doc, fitting.Id, Line.CreateUnbound(newTee.Location, normal), rotation);
                                doc.Regenerate();
                            }
                            else if (newTee.MainDirection.IsAlmostEqualTo(tee.MainDirection.Negate(), 1e-5))
                                ElementTransformUtils.RotateElement(doc, fitting.Id, Line.CreateUnbound(newTee.Location, newTee.MainDirection.CrossProduct(newTee.BranchDirection).Normalize()), Math.PI);

                            #endregion rotate main direction to the correct position

                            #region rotate branch direction to the correct position

                            //rotate branch direction to the correct position
                            doc.Regenerate();
                            newTee = new TeeRailingsData(fitting, teeType);
                            Plane plane = Plane.CreateByNormalAndOrigin(tee.MainDirection, tee.Location);
                            XYZ projectOrg = RevitUtils.ProjectOnto(plane, tee.CenterBranch);
                            XYZ vectorOrg = (projectOrg - tee.Location).Normalize();

                            XYZ projectNew = RevitUtils.ProjectOnto(plane, newTee.CenterBranch);
                            XYZ vectorNew = (projectNew - tee.Location).Normalize();
                            double rotation1 = vectorOrg.AngleTo(vectorNew);
                            XYZ normal1 = vectorOrg.CrossProduct(vectorNew);
                            if (!normal1.IsZeroLength())
                                ElementTransformUtils.RotateElement(doc, fitting.Id, Line.CreateUnbound(tee.Location, normal1), -rotation1);
                            else if (vectorOrg.IsAlmostEqualTo(vectorNew.Negate(), 1e-5))
                                ElementTransformUtils.RotateElement(doc, fitting.Id, Line.CreateUnbound(tee.Location, tee.MainDirection), Math.PI);

                            #endregion rotate branch direction to the correct position

                            doc.Regenerate();
                            FittingRallingData.MappingConnector(doc, fitting, pipes);
                        }
                        tran.Commit();
                    }
                    catch (Exception)
                    {
                        tran.RollBack();
                    }
                }
            }
            catch (Exception) { }
            return fitting != null && fitting.IsValidObject ? fitting : null;
        }

        private static void SetParameter(Element fitting, List<MEPCurve> pipes, TeeRailingsData tee)
        {
            if (fitting != null && fitting.IsValidObject)
            {
                try
                {
                    fitting.LookupParameter("Angle").Set(tee.Angle);
                    fitting.LookupParameter("Length Main").Set(tee.LengthMain);
                    fitting.LookupParameter("Length Branch").Set(tee.LengthBranch);

                    var order = pipes.OrderBy(x => tee.TeeType == MEP_CURVE_TYPE.DUCT ? (x.Width * x.Height) : x.Diameter);
                    MEPCurve branchPipe = order.FirstOrDefault();
                    MEPCurve mainPipe = order.LastOrDefault();
                    //note(*)
                    //if size mainPipe and branchPipe is duplicate with a small tolerance =>choose only a pipe size to set parameter
                    //if this tolerance is retained, it may cause geometric calculation errors for downstream processing.
                    //(function UtilsSolid.GetTotalSolid() return wrong, solids can not be merged)
                    if (tee.TeeType == MEP_CURVE_TYPE.DUCT)
                    {
                        //(*)
                        if (Math.Round(mainPipe.Width, 4) == Math.Round(branchPipe.Width, 4) &&
                            Math.Round(mainPipe.Height, 4) == Math.Round(branchPipe.Height, 4))
                            branchPipe = mainPipe;
                        fitting.LookupParameter("Width Main").Set(mainPipe.Width);
                        fitting.LookupParameter("Height Main").Set(mainPipe.Height);
                        fitting.LookupParameter("Width Branch").Set(branchPipe.Width);
                        fitting.LookupParameter("Height Branch").Set(branchPipe.Height);
                    }
                    else
                    {
                        //(*)
                        if (Math.Round(mainPipe.Diameter, 4) == Math.Round(branchPipe.Diameter, 4))
                            branchPipe = mainPipe;
                        fitting.LookupParameter("Diameter Main").Set(mainPipe.Diameter);
                        fitting.LookupParameter("Diameter Branch").Set(branchPipe.Diameter);
                    }
                }
                catch (Exception) { }
            }
        }
    }
}