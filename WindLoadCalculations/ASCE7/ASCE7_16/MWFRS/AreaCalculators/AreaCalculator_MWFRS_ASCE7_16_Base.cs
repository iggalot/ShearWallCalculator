using ShearWallCalculator.BuildingInfo;
using System;
using System.Collections.Generic;

namespace ShearWallCalculator.WindLoadCalculations.ASCE7.ASCE7_16.CC.AreaCalculators
{
    public class AreaCalculator_MWFRS_ASCE7_16_Base : AreaCalculator_ASCE7_16_Base
    {
        public override void ComputeEffectiveWindAreas(WindParameters_Base p, BuildingData bldg_data, bool windIsParallelToRidge = true, Dictionary<string, double> optionalDimension = null)
        {
            throw new System.NotImplementedException();
        }
    }
}
