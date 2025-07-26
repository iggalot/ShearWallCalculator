using ShearWallCalculator.BuildingInfo;
using ShearWallCalculator.WindLoadCalculations.Chapter30.AreaCalculator;
using System;
using System.Collections.Generic;

namespace ShearWallCalculator.WindLoadCalculations.ASCE7.ASCE7_22
{
    public class AreaCalculator_ASCE7_22_Base : AreaCalculator_Base
    {
        public override BuildingData buildingData { get; set; }

        public override void ComputeEffectiveWindAreas(WindParameters_Base p, BuildingData bldg_data, bool windIsParallelToRidge = true, Dictionary<string, double> optionalDimension = null)
        {
            throw new NotImplementedException();
        }
    }
}
