using ShearWallCalculator.BuildingInfo;
using ShearWallCalculator.WindLoadCalculations.ASCE7.ASCE7_22;

namespace ShearWallCalculator.WindLoadCalculations
{
    public class HipRoofAreaCalculator_MWFRS_ASCE7_22 : AreaCalculator_ASCE7_22_Base
    {
        public HipRoofAreaCalculator_MWFRS_ASCE7_22(BuildingData bldg_data)
        {
            buildingData = bldg_data;
        }
    }
}
