using ShearWallCalculator.BuildingInfo;
using ShearWallCalculator.WindLoadCalculations.Chapter30;
using System;

namespace ShearWallCalculator.WindLoadCalculations
{
    /// <summary>
    /// Computes C&C pressures for ASCE 7-16 using the analytical method of Part 3 on page 350
    /// </summary>
    public class WindLoadCalculator_CC_ASCE7_16: WindLoadCalculator_Base
    {
        public override ASCE7_Versions ASCEVersion { get => ASCE7_Versions.ASCE_VER_7_16; }

        // Which figure of Ch30_3_2A thru I to use for CC
        public Chapter30_BaseFigure extGCpCurve { get; set; }

        public WindLoadCalculator_CC_ASCE7_16(WindLoadParameters_Base p, BuildingData bldg_data)
        {
            buildingData = bldg_data;
            Parameters = p;

            if (buildingData.MeanRoofHeight <= 60)
            {

                switch (buildingData.RoofType)
                {
                    case RoofTypes.ROOF_TYPE_FLAT:
                        GetExtGCpCurve_FlatRoof(p);
                        break;
                    case RoofTypes.ROOF_TYPE_GABLE:
                        GetExtGCpCurve_GableRoof(p);
                        break;
                    case RoofTypes.ROOF_TYPE_HIP:
                        GetExtGCpCurve_HipRoof(p);
                        break;
                    default:
                        throw new Exception("ERROR: Invalid roof type: " + buildingData.RoofType + " in WindLoadCalculator_CC_ASCE7_16 constructor.");
                }
            } 
        }

        private void GetExtGCpCurve_FlatRoof(WindLoadParameters_Base p)
        {
            extGCpCurve = new Figure30_3_2A_ASCE7_16();
        }

        private void GetExtGCpCurve_GableRoof(WindLoadParameters_Base p)
        {
            if (buildingData.RoofPitch < 0)
            {
                throw new NotImplementedException("Error: Roof slope is less than 0 degrees. No table defined in GetExtGCpCuve_GableRoof().");
            }
            else if (buildingData.RoofPitch <= 7)
            {
                extGCpCurve = new Figure30_3_2A_ASCE7_16();
            }
            else if (buildingData.RoofPitch <= 20)
            {
                extGCpCurve = new Figure30_3_2B_ASCE7_16();
            }
            else if (buildingData.RoofPitch <= 27)
            {
                extGCpCurve = new Figure30_3_2C_ASCE7_16();
            }
            else if (buildingData.RoofPitch <= 45)
            {
                extGCpCurve = new Figure30_3_2D_ASCE7_16();
            }
            else
            {
                throw new NotImplementedException("Error: Roof slope is greater than 45 degrees. No table defined in GetExtGCpCuve_GableRoof().");
            }
        }

        private void GetExtGCpCurve_HipRoof(WindLoadParameters_Base p)
        {
            if (buildingData.RoofPitch < 0)
            {
                throw new NotImplementedException("Error: Roof slope is less than 0 degrees. No table defined in GetExtGCpCuve_HipRoof().");
            }
            else if (buildingData.RoofPitch <= 7)
            {
                extGCpCurve = new Figure30_3_2A_ASCE7_16();
            }
            else if (buildingData.RoofPitch <= 20)
            {
                extGCpCurve = new Figure30_3_2E_2F_ASCE7_16(buildingData.MeanRoofHeight, buildingData.BuildingWidth);
            }
            else if (buildingData.RoofPitch <= 27)
            {
                extGCpCurve = new Figure30_3_2G_ASCE7_16(buildingData.MeanRoofHeight, buildingData.BuildingWidth);
            }
            else if (buildingData.RoofPitch <= 45)
            {
                extGCpCurve = new Figure30_3_2H_2I_ASCE7_16(buildingData.MeanRoofHeight, buildingData.BuildingWidth, buildingData.RoofPitch);
            }
            else
            {
                throw new NotImplementedException("Error: Roof slope is greater than 45 degrees. No table defined in GetExtGCpCuve_HipRoof().");
            }
        }
    }
}
