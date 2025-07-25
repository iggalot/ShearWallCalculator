using ShearWallCalculator.BuildingInfo;
using ShearWallCalculator.WindLoadCalculations.Chapter30.AreaCalculator;
using System;

namespace ShearWallCalculator.WindLoadCalculations.ASCE7.ASCE7_22
{
    public class AreaCalculator_ASCE7_22_Base : AreaCalculator_Base
    {
        public override BuildingData buildingData { get; set; }

        public override double CritDim_a { get => ComputeCritDim_a(); }
        public override bool HasCritDim { get; set; } = true;

        public double ComputeCritDim_a()
        {
            return Math.Max(
                Math.Min(0.4 * buildingData.MeanRoofHeight, 0.1 * Math.Min(buildingData.BuildingLength, buildingData.BuildingWidth)),
                Math.Max(0.04 * Math.Min(buildingData.BuildingLength, buildingData.BuildingWidth),
                3)
                );
        }
    }
}
