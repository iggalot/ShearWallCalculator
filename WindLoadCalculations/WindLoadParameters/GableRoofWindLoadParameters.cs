using System;

namespace ShearWallCalculator.WindLoadCalculations
{
    public class GableRoofWindLoadParameters : WindLoadParameters_Base
    {
        public override double MeanRoofHeight
        {
            get
            {
                double h1 = Math.Tan(RoofPitch * Math.PI / 180.0) * BuildingLength / 2.0;
                double h2 = Math.Tan(RoofPitch * Math.PI / 180.0) * BuildingWidth / 2.0;
                return BuildingHeight + Math.Min(h1, h2);
            }
        }

        public override void ComputeEffectiveWindAreas_Roof()
        {
            if (RoofPitch < 7)
            {
                FlatRoofAreaCalculator.Compute(this);
            }
            else
            {
                GableRoofAreaCalculator.Compute(this);
            }
        }
    }
}
