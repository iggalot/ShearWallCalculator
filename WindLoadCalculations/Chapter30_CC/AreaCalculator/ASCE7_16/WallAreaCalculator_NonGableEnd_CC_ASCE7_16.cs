using ShearWallCalculator.BuildingInfo;
using ShearWallCalculator.WindLoadCalculations.Chapter30.AreaCalculator;
using System;
using System.Collections.Generic;

namespace ShearWallCalculator.WindLoadCalculations.Chapter30_CC.AreaCalculator.ASCE7_22
{
    public class WallAreaCalculator_NonGableEnd_CC_ASCE7_16 : AreaCalculator_Base
    {
        public override Dictionary<int, EffectiveWindArea> effWindAreas { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        public override double CritDim_a { get; set; }
        public override bool HasCritDim { get; set; }

        public override void Compute(WindLoadParameters_Base p, BuildingData bldg_data, Dictionary<string, double> optionalParams = null)
        {
            /// <summary>
            /// The critical width dimenstion "a" used throughout chapter 30
            /// -- minimum of 0.4 * building height and 0.1 * min(building Length, building width)
            /// but not less than 4% of smallest dimension or 3 ft.
            /// </summary>
            CritDim_a = Math.Max(
                Math.Min(0.4 * bldg_data.MeanRoofHeight, 0.1 * Math.Min(bldg_data.BuildingLength, bldg_data.BuildingWidth)),
                Math.Max(0.04 * Math.Min(bldg_data.BuildingLength, bldg_data.BuildingWidth),
                3)
                );
            HasCritDim = true;
        }
    }
}
