using Autodesk.Revit.DB;

namespace TepscoIFCToRevit.Data.EquipmentData.Electrical
{
    public class ConduitTerminalPlacingData
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

        public bool IsValid()
        {
            bool isGeneralValid = FacePerpendicularWithLocation != null
                                && MainLine != null
                                && !double.IsNaN(Width)
                                && !double.IsNaN(Height)
                                && !double.IsNaN(Length);

            bool isPlacementValid = true;

            return isGeneralValid && isPlacementValid;
        }
    }
}