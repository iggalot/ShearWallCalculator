using ShearWallCalculator.BuildingInfo;
using ShearWallCalculator.WindLoadCalculations.WindLoadCalculators;
using System;

namespace ShearWallCalculator.WindLoadCalculations
{
    public class WindLoadCalculator_MWFRS_ASCE7_22 : WindLoadCalculator_ASCE7_22_Base
    {
        public override ASCE7_Versions ASCEVersion { get => ASCE7_Versions.ASCE_VER_7_22; }

        public WindLoadCalculator_MWFRS_ASCE7_22(WindLoadParameters_Base p, BuildingData bldg_data)
        {
            Parameters = p;
            buildingData = bldg_data;
        }

        public override double CalculateDynamicWindPressure(double z)
        {
            if (Parameters == null)
                return -1000;

            WindLoadParameters_Base p = Parameters;

            double V = p.WindSpeed;
            double Kzt = p.Kzt;
            double I = p.ImportanceFactor;
            double Kz = GetKz(z, p.ExposureCategory);
            double qz = 0.00256 * Kz * Kzt * V * V * I;
            return qz;
        }
    }
}
