using ShearWallCalculator.BuildingInfo;
using ShearWallCalculator.WindLoadCalculations.Chapter30.AreaCalculator;
using ShearWallCalculator.WindLoadCalculations.Chapter30_CC.AreaCalculator.ASCE7_22;
using System;
using System.Collections.Generic;

namespace ShearWallCalculator.WindLoadCalculations
{
    public class GableRoofWindLoadParameters : WindLoadParameters_Base
    {
        public override AreaCalculator_Base RoofAreaCalculator { get; set; }
        public override AreaCalculator_Base WallAreaCalculator_BldgLength { get; set; }
        public override AreaCalculator_Base WallAreaCalculator_BldgWidth { get; set; }

        public override void ComputeEffectiveWindAreas(BuildingData bldg_data, ASCE7_Versions version)
        {
            // Step 1: Compute roof
            RoofAreaCalculator = RoofAreaCalculatorFactory.Create(bldg_data, this, version);
            RoofAreaCalculator.Compute(this, bldg_data);

            // Step 2: Determine gable sides
            bool widthIsGable = bldg_data.BuildingWidth <= bldg_data.BuildingLength;
            bool lengthIsGable = bldg_data.BuildingLength <= bldg_data.BuildingWidth;

            var lenParams = new Dictionary<string, double> { { "WallLength", bldg_data.BuildingLength } };
            var widParams = new Dictionary<string, double> { { "WallLength", bldg_data.BuildingWidth } };

            // Step 3: Assign wall calculators using C# 7.3-compatible helper
            WallAreaCalculator_BldgLength = CreateWallCalculator(version, lengthIsGable);
            WallAreaCalculator_BldgLength.Compute(this, bldg_data, lenParams);

            WallAreaCalculator_BldgWidth = CreateWallCalculator(version, widthIsGable);
            WallAreaCalculator_BldgWidth.Compute(this, bldg_data, widParams);
        }

        // pseudo factory method here.
        private AreaCalculator_Base CreateWallCalculator(ASCE7_Versions version, bool isGable)
        {
            if (version == ASCE7_Versions.ASCE_VER_7_16)
            {
                return isGable
                    ? (AreaCalculator_Base)new WallAreaCalculator_GableEnd_CC_ASCE7_16()
                    : new WallAreaCalculator_NonGableEnd_CC_ASCE7_16();
            }
            else if (version == ASCE7_Versions.ASCE_VER_7_22)
            {
                return isGable
                    ? (AreaCalculator_Base)new WallAreaCalculator_GableEnd_CC_ASCE7_22()
                    : new WallAreaCalculator_NonGableEnd_CC_ASCE7_22();
            }
            else
            {
                throw new NotImplementedException("Error: Version " + version + " not supported.");
            }
        }
    }
}
