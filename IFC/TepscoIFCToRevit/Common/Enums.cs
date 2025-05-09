namespace TepscoIFCToRevit.Common
{
    public enum MEP_CURVE_TYPE
    {
        None = 0,
        PIPE = 1,
        OVAL_DUCT = 2,
        RECTANGULAR_DUCT = 3,
        ROUND_DUCT = 4,
        DUCT = 5,
    }

    public enum Orders
    {
        Undefine,
        WallFirst,
        BeamFIrst,
    }

    public enum ObjectIFCType
    {
        Pipe,
        Duct,
        ConduitTerminal,
        Beam,
        Column,
        Wall,
        Floor,
        PipeSP,
        CableTray,
        Opening
    }

    public enum Proptotypes
    {
        None,
        SlantedColumnDimension,
        BarShapeCenterLine,
        CylinderCenterLine,
        DrawMesh,
        DecimateMesh,
    }

    public enum PipingSuportPlacements
    {
        Auto_I,
        Auto_T,
        Manual,
    }

    public enum RaillingType
    {
        Auto,
        Pipe,
        Duct,
        ModelInPlace
    }
}