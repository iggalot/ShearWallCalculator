using ShearWallCalculator.BuildingInfo;
using ShearWallCalculator.WindLoadCalculations.Chapter30.AreaCalculator;
using System.Collections.Generic;

namespace ShearWallCalculator.WindLoadCalculations.ASCE7.ASCE7_16
{
    public class AreaCalculator_ASCE7_16_Base : AreaCalculator_Base
    {
        public override BuildingData buildingData { get; set; }

        public override void ComputeEffectiveWindAreas(WindParameters_Base p, BuildingData bldg_data, bool windIsParallelToRidge = true, Dictionary<string, double> optionalDimension = null)
        {
            throw new System.NotImplementedException();
        }
    }
}
