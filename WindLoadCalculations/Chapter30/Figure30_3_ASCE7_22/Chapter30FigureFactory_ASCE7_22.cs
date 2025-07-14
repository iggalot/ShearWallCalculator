using System;

namespace ShearWallCalculator.WindLoadCalculations.Chapter30.Figure30_3
{
    public static class Chapter30FigureFactory_ASCE7_22
    {
        public static Chapter30_BaseFigure CreateFigure_ASCE7_22(RoofTypes roofType, double h, double b, double slope)
        {
            switch (roofType)
            {
                case RoofTypes.ROOF_TYPE_FLAT:
                    // return flat roof figure instance(s), maybe based on slope or h, b?
                    return new Figure30_3_2A_ASCE7_22(); // example class

                case RoofTypes.ROOF_TYPE_GABLE:
                    if (slope <= 7)
                        return new Figure30_3_2A_ASCE7_22();
                    else if (slope <= 20)
                        return new Figure30_3_2B_ASCE7_22();
                    else if (slope <= 27)
                        return new Figure30_3_2C_ASCE7_22();
                    else
                        return new Figure30_3_2D_ASCE7_22();

                case RoofTypes.ROOF_TYPE_HIP:
                    if (slope <= 20)
                        return new Figure30_3_2E_ASCE7_22();
                    else if (slope <= 27)
                        return new Figure30_3_2F_ASCE7_22();
                    else if (slope == 45)
                        return new Figure30_3_2G_ASCE7_22();
                    else
                        return new Figure30_3_2F_2G_INTERPOLATED_ASCE7_22(slope);

                default:
                    throw new ArgumentException("ERROR: Invalid roof type" + roofType + " in Chapter30FigureFactory");
            }
        }
    }

}
