using ShearWallCalculator.BuildingInfo;
using System;

namespace ShearWallCalculator.WindLoadCalculations
{
    public class GableRoofWindLoadParameters : WindLoadParameters_Base
    {
        public override void ComputeEffectiveWindAreas_Roof(BuildingData bldg_data)
        {
            if (bldg_data.RoofPitch < 7)
            {
                FlatRoofAreaCalculator.Compute(this, bldg_data);
            }
            else
            {
                GableRoofAreaCalculator.Compute(this, bldg_data);
            }
        }
    }
}
