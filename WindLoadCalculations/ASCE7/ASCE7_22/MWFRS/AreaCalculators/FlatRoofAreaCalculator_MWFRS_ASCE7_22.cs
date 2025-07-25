using ShearWallCalculator.BuildingInfo;
using ShearWallCalculator.WindLoadCalculations.ASCE7.ASCE7_16.CC.AreaCalculators;

namespace ShearWallCalculator.WindLoadCalculations
{
    public class FlatRoofAreaCalculator_MWFRS_ASCE7_22 : AreaCalculator_MWFRS_ASCE7_22_Base
    {
        public override BuildingData buildingData { get; set; }
        public FlatRoofAreaCalculator_MWFRS_ASCE7_22(BuildingData bldg_data)
        {
            buildingData = bldg_data;
        }
    }

}
