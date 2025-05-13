namespace ShearWallCalculator.NDSProvisions
{
    public enum ShearWallSheathingTypes
    {
        SheathingTypes_UNDEFINED = -1,
        SheathingTypes_WOOD_UNBLOCKED = 0,
        SheathingTypes_WOOD_BLOCKED = 1,
        SheathingTypes_PARTICLEBOARD = 2,
        SheathingTypes_DIAGONALSHEATHING_CONVENTIONAL = 3,
        SheathingTypes_GYPSUM_WALLBOARD = 4,
        SheathingTypes_PORTLAND_CEMENT_PLASTER = 5,
        SheathingTypes_STRUCTURAL_FIBERBOARD = 6
    }
    public class SDPWS_Table4_3_4
    {
        public static double GetMaxAspectRatio(ShearWallSheathingTypes sheathingType)
        {
            switch (sheathingType)
            {
                case ShearWallSheathingTypes.SheathingTypes_WOOD_UNBLOCKED:  return 2.0;
                case ShearWallSheathingTypes.SheathingTypes_WOOD_BLOCKED: return 3.5;
                case ShearWallSheathingTypes.SheathingTypes_PARTICLEBOARD: return 2;
                case ShearWallSheathingTypes.SheathingTypes_DIAGONALSHEATHING_CONVENTIONAL: return 2;
                case ShearWallSheathingTypes.SheathingTypes_GYPSUM_WALLBOARD: return 2;
                case ShearWallSheathingTypes.SheathingTypes_PORTLAND_CEMENT_PLASTER: return 2;
                case ShearWallSheathingTypes.SheathingTypes_STRUCTURAL_FIBERBOARD: return 3.5;
                default: return 0.0;
            }
        }
    }
}
