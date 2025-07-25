using ShearWallCalculator.BuildingInfo;
using ShearWallCalculator.WindLoadCalculations.Chapter30.AreaCalculator;

namespace ShearWallCalculator.WindLoadCalculations.ASCE7.ASCE7_16
{
    public class AreaCalculator_ASCE7_16_Base : AreaCalculator_Base
    {
        public override BuildingData buildingData { get; set; }
    }
}
