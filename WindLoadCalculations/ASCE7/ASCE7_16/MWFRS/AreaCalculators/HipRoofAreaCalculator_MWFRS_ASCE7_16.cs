using ShearWallCalculator.BuildingInfo;
using ShearWallCalculator.WindLoadCalculations.ASCE7.ASCE7_16;

namespace ShearWallCalculator.WindLoadCalculations
{
    public class HipRoofAreaCalculator_MWFRS_ASCE7_16 : AreaCalculator_ASCE7_16_Base
    {
        public override BuildingData buildingData { get; set; }

        public HipRoofAreaCalculator_MWFRS_ASCE7_16(BuildingData bldg_data)
        {
            buildingData = bldg_data;
        }
    }
}
