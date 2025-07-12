using System;

namespace ShearWallCalculator.WindLoadCalculations.Chapter30.Figure30_3
{
    public static class Chapter30FigureFactory
    {
        public static Chapter30_BaseFigure CreateFigure(RoofTypes roofType, double h, double b, double slope)
        {
            switch (roofType)
            {
                case RoofTypes.ROOF_TYPE_FLAT:
                    // return flat roof figure instance(s), maybe based on slope or h, b?
                    return new Figure30_3_2A(); // example class

                case RoofTypes.ROOF_TYPE_GABLE:
                    if (slope <= 7)
                        return new Figure30_3_2A();
                    else if (slope <= 20)
                        return new Figure30_3_2B();
                    else if (slope <= 27)
                        return new Figure30_3_2C();
                    else
                        return new Figure30_3_2D();

                case RoofTypes.ROOF_TYPE_HIP:
                    if (slope <= 20)
                        return new Figure30_3_2E_2F(h, b);
                    else if (slope <= 27)
                        return new Figure30_3_2G(h, b);
                    else
                        return new Figure30_3_2H_2I(h, b, slope);

                default:
                    throw new ArgumentException("ERROR: Invalid roof type" + roofType + " in Chapter30FigureFactory");
            }
        }
    }

}
