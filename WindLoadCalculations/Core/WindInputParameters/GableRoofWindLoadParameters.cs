using ShearWallCalculator.BuildingInfo;
using ShearWallCalculator.WindLoadCalculations.Chapter30.AreaCalculator;
using ShearWallCalculator.WindLoadCalculations.Chapter30_CC.AreaCalculator.ASCE7_22;
using System;
using System.Collections.Generic;

namespace ShearWallCalculator.WindLoadCalculations
{
    public class GableRoofWindLoadParameters : WindParameters_Base
    {
        public override AreaCalculator_Base RoofAreaCalculator { get; set; }
        public override AreaCalculator_Base WallAreaCalculator_BldgLength { get; set; }
        public override AreaCalculator_Base WallAreaCalculator_BldgWidth { get; set; }

        public override void ComputeEffectiveWindAreas(BuildingData bldg_data, ASCE7_Versions version)
        {
            // Step 1: Compute roof
            RoofAreaCalculator = RoofAreaCalculatorFactory.Create(bldg_data, this, version);
            RoofAreaCalculator.Compute(this, bldg_data);

            double len = bldg_data.BuildingLength;
            double wid = bldg_data.BuildingWidth;

            // Step 2: Determine gable sides
            bool widthIsGable = bldg_data.BuildingWidth <= bldg_data.BuildingLength;
            bool lengthIsGable = bldg_data.BuildingLength <= bldg_data.BuildingWidth;

            var lenParams = new Dictionary<string, double> { { "WallLength", bldg_data.BuildingLength } };
            var widParams = new Dictionary<string, double> { { "WallLength", bldg_data.BuildingWidth } };

            // Step 3: Assign wall calculators using C# 7.3-compatible helper

            if(len < wid)
            {
                // Gable End wall -- BuildingLength side
                WallAreaCalculator_BldgLength = CreateWallCalculator(version, "GableEnd");
                WallAreaCalculator_BldgLength.Compute(this, bldg_data, lenParams);

                // Side wall -- BuildingWidth side
                WallAreaCalculator_BldgWidth = CreateWallCalculator(version, "NonGableEnd");
                WallAreaCalculator_BldgWidth.Compute(this, bldg_data, widParams);
            } else if (wid > len)
            {
                // Gable End wall -- BuildingWidth side
                WallAreaCalculator_BldgWidth = CreateWallCalculator(version, "GableEnd");
                WallAreaCalculator_BldgWidth.Compute(this, bldg_data, widParams);

                // Side wall -- BuildingLength side
                WallAreaCalculator_BldgLength = CreateWallCalculator(version, "NonGableEnd");
                WallAreaCalculator_BldgLength.Compute(this, bldg_data, lenParams);
            } else
            {
                // if dimensions are the same, make the BuildingWidth the gable end
                // Gable end wall -- BuildingWidth side
                WallAreaCalculator_BldgWidth = CreateWallCalculator(version, "GableEnd");
                WallAreaCalculator_BldgWidth.Compute(this, bldg_data, widParams);

                // Side wall -- BuildingLength side
                WallAreaCalculator_BldgLength = CreateWallCalculator(version, "NonGableEnd");
                WallAreaCalculator_BldgLength.Compute(this, bldg_data, lenParams);
            }
        }

        // pseudo factory method here.
        private AreaCalculator_Base CreateWallCalculator(ASCE7_Versions version, string wall_type)
        {
            if (version == ASCE7_Versions.ASCE_VER_7_16)
            {
                if(wall_type == "GableEnd")
                {
                    return new WallAreaCalculator_GableEnd_CC_ASCE7_16(wall_type);
                } else
                {
                    return new WallAreaCalculator_NonGableEnd_CC_ASCE7_16(wall_type);
                }
            }
            else if (version == ASCE7_Versions.ASCE_VER_7_22)
            {
                if (wall_type == "GableEnd")
                {
                    return new WallAreaCalculator_GableEnd_CC_ASCE7_22(wall_type);
                }
                else
                {
                    return new WallAreaCalculator_NonGableEnd_CC_ASCE7_22(wall_type);
                }
            }
            else
            {
                throw new NotImplementedException("Error: Version " + version + " not supported.");
            }
        }
    }
}
