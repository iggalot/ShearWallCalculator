using ShearWallCalculator.BuildingInfo;
using ShearWallCalculator.WindLoadCalculations.Chapter30.AreaCalculator;

namespace ShearWallCalculator.WindLoadCalculations
{
    public class GableRoofWindLoadParameters : WindLoadParameters_Base
    {
        public override RoofAreaCalculator_Base RoofAreaCalculator { get; set; }

        public override void ComputeEffectiveWindAreas_Roof(BuildingData bldg_data, ASCE7_Versions version)
        {
            RoofAreaCalculator = RoofAreaCalculatorFactory.Create(bldg_data, this, version);
            RoofAreaCalculator.Compute(this, bldg_data);
        }
    }
}
