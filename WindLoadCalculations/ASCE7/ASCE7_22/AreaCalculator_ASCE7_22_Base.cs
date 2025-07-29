using ShearWallCalculator.BuildingInfo;
using ShearWallCalculator.WindLoadCalculations.Chapter30.AreaCalculator;
using System;

namespace ShearWallCalculator.WindLoadCalculations.ASCE7.ASCE7_22
{
    public class AreaCalculator_ASCE7_22_Base : AreaCalculator_Base
    {
        public override BuildingData buildingData { get; set; }
        public override bool HasCritDim { get; set; } = false;
        public override double CritDim_a { get; set; } = 0;
        public override void ComputeEffectiveWindAreas()
        {
            throw new NotImplementedException();
        }
    }
}
