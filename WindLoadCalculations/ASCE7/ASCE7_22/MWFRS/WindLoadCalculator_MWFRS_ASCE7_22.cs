using ShearWallCalculator.BuildingInfo;
using ShearWallCalculator.WindLoadCalculations.WindLoadCalculators;
using System;

namespace ShearWallCalculator.WindLoadCalculations
{
    public class WindLoadCalculator_MWFRS_ASCE7_22 : WindLoadCalculator_ASCE7_22_Base
    {
        public override ASCE7_Versions ASCEVersion { get => ASCE7_Versions.ASCE_VER_7_22; }

        public WindLoadCalculator_MWFRS_ASCE7_22(WindParameters_Base p, BuildingData bldg_data)
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
    }
}
