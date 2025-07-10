using ShearWallCalculator.BuildingInfo;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace ShearWallCalculator.WindLoadCalculations
{
    public class WindLoadCalculator_MWFRS_ASCE7_16 : WindLoadCalculator_Base
    {
        public override ASCE7_Versions ASCEVersion { get => ASCE7_Versions.ASCE_VER_7_16; }

        public WindLoadCalculator_MWFRS_ASCE7_16(WindLoadParameters_Base p, BuildingData bldg_data)
        {
            Parameters = p;
            buildingData = bldg_data;
        }
    }
}
