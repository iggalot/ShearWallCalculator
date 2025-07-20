using ShearWallCalculator.BuildingInfo;
using ShearWallCalculator.WindLoadCalculations.Chapter30.AreaCalculator;
using System.Collections.Generic;

namespace ShearWallCalculator.WindLoadCalculations
{
    public class FlatRoofAreaCalculator_MWFRS_ASCE7_22 : AreaCalculator_Base
    {
        /// <summary>
        /// Effective wind areas for roof
        /// </summary>
        public override Dictionary<int, EffectiveWindArea> effWindAreas { get; set; } = new Dictionary<int, EffectiveWindArea>();

        public override void Compute(WindLoadParameters_Base p, BuildingData bldg_data, Dictionary<string, double> optionalParams = null)
        {
            // TODO: 
        }
    }

}
