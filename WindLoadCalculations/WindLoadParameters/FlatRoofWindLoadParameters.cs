using ShearWallCalculator.BuildingInfo;

namespace ShearWallCalculator.WindLoadCalculations
{
    public class FlatRoofWindLoadParameters : WindLoadParameters_Base
    {
        public override void ComputeEffectiveWindAreas_Roof(BuildingData bldg_data)
        {
            FlatRoofAreaCalculator.Compute(this, bldg_data);
        }
    }
}
