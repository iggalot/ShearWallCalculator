using ShearWallCalculator.BuildingInfo;
using System;
using System.Collections.Generic;

namespace ShearWallCalculator.WindLoadCalculations
{
    public static class Chapter27RoofFigureFactory_ASCE7_22
    {
        public static Chapter27and30_GCpCurveBase CreateRoofFigure_ASCE7_22(BuildingData buildingData, double area=50)
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
                        if(h_over_L <= 0.25)
                        {
                            return new NormToRidge_LargeSlope_Roof_Low(roofSlope);
                        } else if (h_over_L >= 1.0)
                        {
                            var high = new NormToRidge_LargeSlope_Roof_High(roofSlope);
                        } else if (h_over_L == 0.5)
                        {
                            
                        }
                        else if (h_over_L > 0.25 && h_over_L < 0.5)
                        {
                            // interpolate
                            var low = new NormToRidge_LargeSlope_Roof_Low(roofSlope);
                            var mid = new NormToRidge_LargeSlope_Roof_Mid(roofSlope);

                            return new InterpolatedCurve(h_over_L, roofSlope, 0.25, 0.5, low, mid);

                        } 
                        else if (h_over_L > 0.5 && h_over_L < 1.0)
                        {
                            var mid = new NormToRidge_LargeSlope_Roof_Mid(roofSlope);
                            var high = new NormToRidge_LargeSlope_Roof_High(roofSlope);

                            return new InterpolatedCurve(h_over_L, roofSlope, 0.5, 1.0, mid, high);
                        }
                    }
                    else
                    {
                        if (h_over_L <= 0.5) 
                        {
                            return new ParallelToRidge_LowSlope_Roof_Low();
                        } 
                        else if (h_over_L >= 1.0)
                        {
                            return new ParallelToRidge_LowSlope_Roof_High();
                        } else if (h_over_L > 0.5 && h_over_L < 1.0)
                        {
                            var low = new ParallelToRidge_LowSlope_Roof_Low();
                            var high = new ParallelToRidge_LowSlope_Roof_High();

                            return new InterpolatedCurve(h_over_L, roofSlope, 0.5, 1.0, low, high);
                        }
                    }
                    break;
                case RidgeDirections.RIDGE_DIR_NONE:
                    {
                        if (h_over_L <= 0.5)
                        {
                            return new ParallelToRidge_LowSlope_Roof_Low();
                        }
                        else if (h_over_L >= 1.0)
                        {
                            return new ParallelToRidge_LowSlope_Roof_High();
                        }
                        else if (h_over_L > 0.5 && h_over_L < 1.0)
                        {
                            var low = new ParallelToRidge_LowSlope_Roof_Low();
                            var high = new ParallelToRidge_LowSlope_Roof_High();

                            return new InterpolatedCurve(h_over_L, roofSlope, 0.5, 1.0, low, high);
                        }
                    }
                    break;

                default:
                    throw new ArgumentException("ERROR: Invalid roof type" + roofType + " in Chapter30FigureFactory");
            }

            return null;
        }

        public static class GCpCurveInterpolator
        {
            public static ExternalGCpCurve Interpolate(
                ExternalGCpCurve lowCurve,
                ExternalGCpCurve highCurve,
                double inputValue,
                double lowPoint,
                double highPoint)
            {
                if (inputValue < lowPoint || inputValue > highPoint)
                    throw new ArgumentOutOfRangeException(nameof(inputValue), $"Value must be between {lowPoint} and {highPoint}.");

                double t = (inputValue - lowPoint) / (highPoint - lowPoint);

                var pLow = lowCurve.Points;
                var pHigh = highCurve.Points;

                if (pLow.Length != pHigh.Length)
                    throw new InvalidOperationException("Curves must have the same number of points.");

                var interpolated = new List<(double X, double Y)>();

                for (int i = 0; i < pLow.Length; i++)
                {
                    if (pLow[i].X != pHigh[i].X)
                        throw new InvalidOperationException("Curves must have matching X coordinates.");

                    double x = pLow[i].X;
                    double y = pLow[i].Y + (pHigh[i].Y - pLow[i].Y) * t;

                    interpolated.Add((x, y));
                }

                return new ExternalGCpCurve(interpolated.ToArray());
            }
        }
    }

}
