using ShearWallCalculator.BuildingInfo;
using System;

namespace ShearWallCalculator.WindLoadCalculations
{
    public class HipRoofWindLoadParameters : WindLoadParameters_Base
    {
        public override void ComputeEffectiveWindAreas_Roof(BuildingData bldg_data)
        {
            if (bldg_data.RoofPitch < 7)
            {
                FlatRoofAreaCalculator.Compute(this, bldg_data);
            }
            else
            {
                HipRoofAreaCalculator.Compute(this, bldg_data);
            }
        }
    }
}
