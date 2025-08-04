using ShearWallCalculator.BuildingInfo;
using ShearWallCalculator.WindLoadCalculations.WindLoadCalculators;
using System;

namespace ShearWallCalculator.WindLoadCalculations
{
    public class WindLoadCalculator_MWFRS_ASCE7_16 : WindLoadCalculator_ASCE7_16_Base
    {
        // Figure 27.3.1 coefficients
        public double Cp_WW { get; set; } = 0.8;  // windward wall
        public double Cp_SW { get; set; } = -0.7; // sidewalls
        public double Cp_LW { get => GetLWCpValues(); } // leeward walls

        private double GetLWCpValues()
        {
            double x = buildingData.L_Over_B;

            if (x < 1) return -0.5;
            if (x >= 4) return -0.2;

            // Linear interpolation between (1, -0.5) and (4, -0.2)
            double x0 = 1.0, y0 = -0.5;
            double x1 = 4.0, y1 = -0.2;

            double interpolatedValue = y0 + (x - x0) * ((y1 - y0) / (x1 - x0));
            return interpolatedValue;
        }

        public WindLoadCalculator_MWFRS_ASCE7_16(WindParameters_Base p, BuildingData bldg_data)
        {
            Parameters = p;
            buildingData = bldg_data;

            CreateExtGcpCurves();
        }

        public void CreateExtGcpCurves()
        {
            switch (ASCEVersion)
            {
                case ASCE7_Versions.ASCE_VER_7_16:
                    extGCpCurve_Roof = Chapter27RoofFigureFactory_ASCE7_16.CreateRoofFigure_ASCE7_16(buildingData);
                    extGCpCurve_Wall = new Figure30_3_1_ASCE7_16();
                    break;
                case ASCE7_Versions.ASCE_VER_7_22:
                    extGCpCurve_Roof = Chapter27RoofFigureFactory_ASCE7_22.CreateRoofFigure_ASCE7_22(buildingData);
                    extGCpCurve_Wall = new Figure30_3_1_ASCE7_22();
                    break;
                default:
                    throw new Exception("ERROR: Invalid ASCE Version: " + ASCEVersion + " in WindLoadCalculator_Base constructor.");
            }
        }

        public override void CalculateExternalPressures()
        {

            //var kd = Parameters.Kd;


            //// roof pressure negative
            //foreach (var area in RoofAreaCalculator.effWindAreas)
            //{
            //    if (TryGetGCp_Neg_Roof_ByArea(area.Key, out var gcp))
            //    {
            //        double pressure = CalculateDynamicWindPressure(buildingData.MeanRoofHeight) * gcp;
            //        windPressureRoof_Neg_External.Add(area.Key, pressure);
            //    }
            //}

            //// BuildingWidth pressure positive
            //foreach (var area in WallAreaCalculator_BldgWidth.effWindAreas)
            //{
            //    if (TryGetGCp_Pos_BuildingWidthWall_ByArea(area.Key, out var gcp))
            //    {
            //        double pressure = CalculateDynamicWindPressure(buildingData.MeanRoofHeight) * gcp;
            //        windPressureBuildingWidthWall_Pos_External.Add(area.Key, pressure);
            //    }
            //}

            //// BuildingWidth pressure negative
            //foreach (var area in WallAreaCalculator_BldgWidth.effWindAreas)
            //{
            //    if (TryGetGCp_Neg_BuildingWidthWall_ByArea(area.Key, out var gcp))
            //    {
            //        double pressure = CalculateDynamicWindPressure(buildingData.MeanRoofHeight) * gcp;
            //        windPressureBuildingWidthWall_Neg_External.Add(area.Key, pressure);
            //    }
            //}

            //// BuildingLength pressure positive
            //foreach (var area in WallAreaCalculator_BldgLength.effWindAreas)
            //{
            //    if (TryGetGCp_Pos_BuildingLengthWall_ByArea(area.Key, out var gcp))
            //    {
            //        double pressure = CalculateDynamicWindPressure(buildingData.MeanRoofHeight) * gcp;
            //        windPressureBuildingLengthWall_Pos_External.Add(area.Key, pressure);
            //    }
            //}

            //// BuildingLength pressure negative
            //foreach (var area in WallAreaCalculator_BldgLength.effWindAreas)
            //{
            //    if (TryGetGCp_Neg_BuildingLengthWall_ByArea(area.Key, out var gcp))
            //    {
            //        double pressure = CalculateDynamicWindPressure(buildingData.MeanRoofHeight) * gcp;
            //        windPressureBuildingLengthWall_Neg_External.Add(area.Key, pressure);
            //    }
            //}
        }
    }
}
