using ShearWallCalculator.BuildingInfo;
using ShearWallCalculator.WindLoadCalculations.Chapter30.AreaCalculator;
using System;
using System.Collections.Generic;

namespace ShearWallCalculator.WindLoadCalculations
{
    public class GableRoofAreaCalculator_MWFRS_ASCE7_22 : AreaCalculator_Base
    {
        public override Dictionary<int, EffectiveWindArea> effWindAreas { get; set; } = new Dictionary<int, EffectiveWindArea>();

        public override void Compute(WindParameters_Base p, BuildingData bldg_data, Dictionary<string, double> optionalParams = null)
        {
            // TODO
        }
    }
}
