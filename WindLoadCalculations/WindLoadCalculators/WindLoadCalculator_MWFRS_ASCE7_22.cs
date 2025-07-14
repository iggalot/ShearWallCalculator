using ShearWallCalculator.BuildingInfo;

namespace ShearWallCalculator.WindLoadCalculations
{
    public class WindLoadCalculator_MWFRS_ASCE7_22 : WindLoadCalculator_Base
    {
        public override ASCE7_Versions ASCEVersion { get => ASCE7_Versions.ASCE_VER_7_22; }

        public WindLoadCalculator_MWFRS_ASCE7_22(WindLoadParameters_Base p, BuildingData bldg_data)
        {
            Parameters = p;
            buildingData = bldg_data;
        }
    }
}
