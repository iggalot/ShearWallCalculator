namespace ShearWallCalculator.WindLoadCalculations
{
    public class FlatRoofWindLoadParameters : WindLoadParameters_Base
    {
        public override double MeanRoofHeight
        {
            get { return BuildingHeight; }
        }

        public override void ComputeEffectiveWindAreas_Roof()
        {
            FlatRoofAreaCalculator.Compute(this);
        }
    }
}
