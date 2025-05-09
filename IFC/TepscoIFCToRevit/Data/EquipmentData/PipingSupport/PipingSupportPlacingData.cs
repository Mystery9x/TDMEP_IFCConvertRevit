using Autodesk.Revit.DB;
using TepscoIFCToRevit.Common;

namespace TepscoIFCToRevit.Data.EquipmentData.PipingSupport
{
    public class PipingSupportPlacingData
    {
        public PlanarFace FacePerpendicularWithLocation { get; set; }

        public PlanarFace FacePlace { get; set; }
        public Line MainLine { get; set; }

        public XYZ Direction { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }

        public Line LWith { get; set; }

        public Line LHeight { get; set; }

        public double Length { get; set; }
        public double WingLength { get; set; }
        public ElementId TypeId { get; set; }

        public XYZ LocationPlace { get; set; }

        public PipingSuportPlacements Placement { get; set; }

        public bool IsValid()
        {
            bool isGeneralValid = FacePerpendicularWithLocation != null
                                && MainLine != null
                                && !double.IsNaN(Width)
                                && !double.IsNaN(Height)
                                && !double.IsNaN(Length);

            bool isPlacementValid = true;
            if (Placement == PipingSuportPlacements.Auto_T)
                isPlacementValid = !double.IsNaN(WingLength) && double.IsNaN(Height);

            return isGeneralValid && isPlacementValid;
        }
    }
}