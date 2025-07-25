using ShearWallCalculator.BuildingInfo;
using ShearWallCalculator.WindLoadCalculations.Chapter30.AreaCalculator;
using ShearWallCalculator.WindLoadCalculations.Chapter30_CC.AreaCalculator.ASCE7_22;
using System;
using System.Collections.Generic;

namespace ShearWallCalculator.WindLoadCalculations
{
    public class HipRoofWindLoadParameters : WindParameters_Base
    {
        public override AreaCalculator_Base RoofAreaCalculator { get; set; }
        public override AreaCalculator_Base WallAreaCalculator_BldgLength {get; set; }
        public override AreaCalculator_Base WallAreaCalculator_BldgWidth { get; set; }

        public override void ComputeEffectiveWindAreas(BuildingData bldg_data, ASCE7_Versions version)
        {
            RoofAreaCalculator = RoofAreaCalculatorFactory.Create(bldg_data, this, version);
            RoofAreaCalculator.Compute(this, bldg_data);

            double len = bldg_data.BuildingLength;
            double wid = bldg_data.BuildingWidth;

            var lenParams = new Dictionary<string, double> { { "WallLength", len } };
            var widParams = new Dictionary<string, double> { { "WallLength", wid } };

            //SideWall
            WallAreaCalculator_BldgLength = CreateWallCalculator(version, "NonGable");
            WallAreaCalculator_BldgLength.Compute(this, bldg_data, lenParams);

            WallAreaCalculator_BldgWidth = CreateWallCalculator(version, "NonGable");
            WallAreaCalculator_BldgWidth.Compute(this, bldg_data, widParams);
        }

        // pseudo factory method here
        private AreaCalculator_Base CreateWallCalculator(ASCE7_Versions version, string wall_type)
        {
            if (version == ASCE7_Versions.ASCE_VER_7_16)
            {
                return new WallAreaCalculator_NonGableEnd_CC_ASCE7_16(wall_type);
            }
            else if (version == ASCE7_Versions.ASCE_VER_7_22)
            {
                return new WallAreaCalculator_NonGableEnd_CC_ASCE7_22(wall_type);
            }
            else
            {
                throw new NotImplementedException("Wall calculator not implemented for version " + version);
            }
        }
    }
}
