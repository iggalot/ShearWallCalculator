using ShearWallCalculator.BuildingInfo;
using ShearWallCalculator.WindLoadCalculations.WindLoadCalculators;
using System;

namespace ShearWallCalculator.WindLoadCalculations
{
    public class WindLoadCalculator_MWFRS_ASCE7_16 : WindLoadCalculator_ASCE7_16_Base
    {
        public WindLoadCalculator_MWFRS_ASCE7_16(WindParameters_Base p, BuildingData bldg_data)
        {
            Parameters = p;
            buildingData = bldg_data;
        }
    }
}
