using System.ComponentModel;

namespace ShearWallCalculator.WindLoadCalculations
{
    public class WindLoadCalculator_Base
    {
        public enum WindZones_Walls_MWFRS
        {
            [Description("Windward Wall - z=0ft")]
            MWFRS_WW_0 = 0,
            [Description("Windward Wall - z=15ft")]
            MWFRS_WW_15 = 1,
            [Description("Windward Wall - z=h")]
            MWFRS_WW_h = 2,
            [Description("Leeward Wall")]
            MWFRS_LW_h = 3,
            [Description("Sidewall")]
            MWFRS_SW_h = 4
        }

        public enum WindZones_Roof_MWFRS
        {
            [Description("Windward Roof 0->h/2")]
            MWFRS_WR_0_h2 = 0,
            [Description("Windward Roof h/2->h")]
            MWFRS_WR_h2_h = 1,
            [Description("Windward Roof h->2h")]
            MWFRS_WR_h_2h = 2,
            [Description("Windward Roof > 2h")]
            MWFRS_WR_2h_L = 3,
            [Description("Windward Roof Full")]
            MWFRS_WR_Full = 4,
            [Description("Leeward Roof Full")]
            MWFRS_LR_Full = 5
        }

        public enum WindZones_CC
        {
            CC_1 = 0,       // Roof flat zone
            CC_2 = 1,       // Roof edge zone
            CC_3 = 2,       // Roof corner zone
            CC_4 = 3,       // Wall flat zone
            CC_5 = 4        // Wall edge zone (width 'a')
        }

        // Wind Load Parameters class
        public class WindLoadParameters
        {
            public string RiskCategory { get; set; }
            public double WindSpeed { get; set; }
            public string ExposureCategory { get; set; }
            public double BuildingHeight { get; set; }
            public string EnclosureClassification { get; set; }
            public double Kd { get; set; }
            public double Kzt { get; set; }
            public double GustFactor { get; set; } = 0.85;
            public double ImportanceFactor { get; set; }
            public double BuildingLength { get; set; }
            public double BuildingWidth { get; set; }
            public double RoofPitch { get; set; }
            public string RidgeDirection { get; set; }
        }

    }
}
