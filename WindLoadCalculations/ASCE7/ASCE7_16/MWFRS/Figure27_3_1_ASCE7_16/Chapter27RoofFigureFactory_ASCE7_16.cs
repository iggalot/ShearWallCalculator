using ShearWallCalculator.BuildingInfo;
using System;

namespace ShearWallCalculator.WindLoadCalculations
{
    public static class Chapter27RoofFigureFactory_ASCE7_16
    {
        public static Chapter27and30_GCpCurveBase CreateRoofFigure_ASCE7_16(BuildingData buildingData, double area)
        {

            RidgeDirections ridgeDirection = buildingData.RidgeDirection;
            RoofTypes roofType = buildingData.RoofType;
            double roofSlope = buildingData.RoofPitch;
            double h_over_L = buildingData.h_Over_L;

            switch (ridgeDirection)
            {
                case RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH: // parallel to ridge case

                    break;
                case RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH:  // normal to ridge case
                    if (roofSlope >= 10)
                    {

                    }
                    else
                    {
                        if (h_over_L <= 0.5) 
                        {
                            return new ParallelToRidge_LowSlope_Roof_Low();
                        } 
                        else if (h_over_L >= 1.0)
                        {
                            //return new ParallelToRidge_LowSlope_Roof_High();
                        }
                    }
                    break;
                case RidgeDirections.RIDGE_DIR_NONE:
                    break;

                default:
                    throw new ArgumentException("ERROR: Invalid roof type" + roofType + " in Chapter30FigureFactory");
            }

            return null;
        }
    }

}
