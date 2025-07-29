using ShearWallCalculator.BuildingInfo;
using ShearWallCalculator.WindLoadCalculations;
using System;

namespace ShearWallCalculator.WindLoadCalculations
{
    public static class Chapter30RoofFigureFactory_ASCE7_16
    {
        public static Chapter30_BaseFigure CreateRoofFigure_ASCE7_16(RoofTypes roofType, double h, double b, double slope)
        {
            switch (roofType)
            {
                case RoofTypes.ROOF_TYPE_FLAT:
                    // return flat roof figure instance(s), maybe based on slope or h, b?
                    return new Figure30_3_2A_ASCE7_16(); // example class

                case RoofTypes.ROOF_TYPE_GABLE:
                    if (slope <= 7)
                        return new Figure30_3_2A_ASCE7_16();
                    else if (slope <= 20)
                        return new Figure30_3_2B_ASCE7_16();
                    else if (slope <= 27)
                        return new Figure30_3_2C_ASCE7_16();
                    else
                        return new Figure30_3_2D_ASCE7_16();

                case RoofTypes.ROOF_TYPE_HIP:
                    if (slope <= 20)
                        return new Figure30_3_2E_2F_ASCE7_16(h, b);
                    else if (slope <= 27)
                        return new Figure30_3_2G_ASCE7_16(h, b);
                    else
                        return new Figure30_3_2H_2I_ASCE7_16(h, b, slope);

                default:
                    throw new ArgumentException("ERROR: Invalid roof type" + roofType + " in Chapter30FigureFactory");
            }
        }
    }

}
