using ShearWallCalculator.BuildingInfo;
using ShearWallCalculator.WindLoadCalculations.ASCE7.ASCE7_16;

namespace ShearWallCalculator.WindLoadCalculations
{
    public class GableRoofAreaCalculator_MWFRS_ASCE7_16 : AreaCalculator_ASCE7_16_Base
    {
        public GableRoofAreaCalculator_MWFRS_ASCE7_16(BuildingData bldg_data)
        {
            buildingData = bldg_data;
        }
    }
}
