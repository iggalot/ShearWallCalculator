using ShearWallCalculator.BuildingInfo;
using ShearWallCalculator.WindLoadCalculations.Chapter30.AreaCalculator;
using System.Collections.Generic;

namespace ShearWallCalculator.WindLoadCalculations
{
    public class HipRoofAreaCalculator_MWFRS_ASCE7_16 : AreaCalculator_Base
    {
        public static double CritDim_a { get; set; }
        /// <summary>
        /// Effective wind areas for roof
        /// </summary>
        public override Dictionary<int, EffectiveWindArea> effWindAreas { get; set; } = new Dictionary<int, EffectiveWindArea>();

        public override void Compute(WindParameters_Base p, BuildingData bldg_data, Dictionary<string, double> optionalParams = null)
        {
            // TODO
        }
    }
}
