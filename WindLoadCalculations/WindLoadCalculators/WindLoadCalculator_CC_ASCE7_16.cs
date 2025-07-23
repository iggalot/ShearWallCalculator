using ShearWallCalculator.BuildingInfo;
using ShearWallCalculator.WindLoadCalculations.Chapter30;
using ShearWallCalculator.WindLoadCalculations.WindLoadCalculators;
using System;

namespace ShearWallCalculator.WindLoadCalculations
{
    /// <summary>
    /// Computes C&C pressures for ASCE 7-16 using the analytical method of Part 3 on page 350
    /// </summary>
    public class WindLoadCalculator_CC_ASCE7_16 : WindLoadCalculator_CC_Base
    {
        public override ASCE7_Versions ASCEVersion { get => ASCE7_Versions.ASCE_VER_7_16; }

        public WindLoadCalculator_CC_ASCE7_16(WindLoadParameters_Base p, BuildingData bldg_data) : base()
        {
            buildingData = bldg_data;
            Parameters = p;

            if (buildingData.MeanRoofHeight <= 60)
            {
                CreateExtGcpCurves();
            } else
            {
                throw new Exception("ERROR: Building max mean roof height has exceeded 60 ft -- " + buildingData.MeanRoofHeight + " ft.");
            }
        }
    }
}
