using ShearWallCalculator.BuildingInfo;
using ShearWallCalculator.WindLoadCalculations.WindLoadCalculators;
using System;

namespace ShearWallCalculator.WindLoadCalculations
{
    public class WindLoadCalculator_MWFRS_ASCE7_16 : WindLoadCalculator_ASCE7_16_Base
    {
        // Figure 27.3.1 coefficients
        public double Cp_WW { get; set; } = 0.8;  // windward wall
        public double Cp_SW { get; set; } = -0.7; // sidewalls
        public double Cp_LW { get => GetLWCpValues(); } // leeward walls

        private double GetLWCpValues()
        {
            double x = buildingData.L_Over_B;

            if (x < 1) return -0.5;
            if (x >= 4) return -0.2;

            // Linear interpolation between (1, -0.5) and (4, -0.2)
            double x0 = 1.0, y0 = -0.5;
            double x1 = 4.0, y1 = -0.2;

            double interpolatedValue = y0 + (x - x0) * ((y1 - y0) / (x1 - x0));
            return interpolatedValue;
        }

        public WindLoadCalculator_MWFRS_ASCE7_16(WindParameters_Base p, BuildingData bldg_data)
        {
            Parameters = p;
            buildingData = bldg_data;
        }
    }
}
