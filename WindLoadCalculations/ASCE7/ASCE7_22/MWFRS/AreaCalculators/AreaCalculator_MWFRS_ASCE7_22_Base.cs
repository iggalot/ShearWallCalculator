using ShearWallCalculator.WindLoadCalculations.ASCE7.ASCE7_22;
using System;

namespace ShearWallCalculator.WindLoadCalculations.ASCE7.ASCE7_16.CC.AreaCalculators
{
    public class AreaCalculator_MWFRS_ASCE7_22_Base : AreaCalculator_ASCE7_22_Base
    {
        public double CritDim_a { get=> ComputeCritDim_a(); }
        public bool HasCritDim { get; set; } = true;

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
