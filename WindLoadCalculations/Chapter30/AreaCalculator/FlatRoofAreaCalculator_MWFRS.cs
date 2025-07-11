using ShearWallCalculator.BuildingInfo;
using ShearWallCalculator.WindLoadCalculations.Chapter30.AreaCalculator;
using System;
using System.Collections.Generic;

namespace ShearWallCalculator.WindLoadCalculations
{
    public class FlatRoofAreaCalculator_MWFRS : RoofAreaCalculator_Base
    {
        /// <summary>
        /// Effective wind areas for roof
        /// </summary>
        public override Dictionary<int, EffectiveWindArea_Roof> effWindAreas_Roof { get; set; } = new Dictionary<int, EffectiveWindArea_Roof>();

        public override void Compute(WindLoadParameters_Base p, BuildingData bldg_data)
        {
            // TODO: 
        }
    }

}
