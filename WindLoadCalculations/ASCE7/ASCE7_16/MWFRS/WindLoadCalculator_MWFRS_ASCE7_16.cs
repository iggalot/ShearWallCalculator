using ShearWallCalculator.BuildingInfo;
using ShearWallCalculator.WindLoadCalculations.WindLoadCalculators;
using System;

namespace ShearWallCalculator.WindLoadCalculations
{
    public class WindLoadCalculator_MWFRS_ASCE7_16 : WindLoadCalculator_ASCE7_16_Base
    {
        public override ASCE7_Versions ASCEVersion { get => ASCE7_Versions.ASCE_VER_7_16; }

        public WindLoadCalculator_MWFRS_ASCE7_16(WindParameters_Base p, BuildingData bldg_data)
        {
            Parameters = p;
            buildingData = bldg_data;
        }
    }
}
