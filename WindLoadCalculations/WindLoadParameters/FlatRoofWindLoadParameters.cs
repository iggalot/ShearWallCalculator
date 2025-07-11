using ShearWallCalculator.BuildingInfo;
using ShearWallCalculator.WindLoadCalculations.Chapter30.AreaCalculator;

namespace ShearWallCalculator.WindLoadCalculations
{
    public class FlatRoofWindLoadParameters : WindLoadParameters_Base
    {
        public override RoofAreaCalculator_Base RoofAreaCalculator { get; set; }

        public override void ComputeEffectiveWindAreas_Roof(BuildingData bldg_data)
        {
            RoofAreaCalculator = RoofAreaCalculatorFactory.Create(bldg_data, this);
            RoofAreaCalculator.Compute(this, bldg_data);
        }
    }
}
