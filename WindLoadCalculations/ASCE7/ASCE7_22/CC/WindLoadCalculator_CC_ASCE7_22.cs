using ShearWallCalculator.BuildingInfo;
using ShearWallCalculator.WindLoadCalculations.Chapter30;
using ShearWallCalculator.WindLoadCalculations.Chapter30.Figure30_3;
using System;

namespace ShearWallCalculator.WindLoadCalculations.WindLoadCalculators
{
    /// <summary>
    /// Computes C&C pressures for ASCE 7-16 using the analytical method of Part 3 on page 350
    /// </summary>
    public class WindLoadCalculator_CC_ASCE7_22: WindLoadCalculator_ASCE7_22_Base
    {
        public override ASCE7_Versions ASCEVersion { get => ASCE7_Versions.ASCE_VER_7_22; }

        public WindLoadCalculator_CC_ASCE7_22(WindParameters_Base p, BuildingData bldg_data)
        {
            buildingData = bldg_data;
            Parameters = p;

            if (buildingData.MeanRoofHeight <= 60)
            {
                CreateExtGcpCurves();
            }
        }

        public void CreateExtGcpCurves()
        {
            switch (ASCEVersion)
            {
                case ASCE7_Versions.ASCE_VER_7_16:
                    extGCpCurve_Roof = Chapter30RoofFigureFactory_ASCE7_16.CreateRoofFigure_ASCE7_16(
                         buildingData.RoofType, buildingData.MeanRoofHeight, buildingData.BuildingWidth, buildingData.RoofPitch);
                    extGCpCurve_Wall = new Figure30_3_1_ASCE7_16();
                    break;
                case ASCE7_Versions.ASCE_VER_7_22:
                    extGCpCurve_Roof = Chapter30RoofFigureFactory_ASCE7_22.CreateRoofFigure_ASCE7_22(
                        buildingData.RoofType, buildingData.MeanRoofHeight, buildingData.BuildingWidth, buildingData.RoofPitch);
                    extGCpCurve_Wall = new Figure30_3_1_ASCE7_22();
                    break;
                default:
                    throw new Exception("ERROR: Invalid ASCE Version: " + ASCEVersion + " in WindLoadCalculator_Base constructor.");
            }
        }

    }
}
