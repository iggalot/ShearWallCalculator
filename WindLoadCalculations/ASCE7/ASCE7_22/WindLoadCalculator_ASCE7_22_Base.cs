using ShearWallCalculator.BuildingInfo;
using ShearWallCalculator.WindLoadCalculations.Chapter30.AreaCalculator;
using System;

namespace ShearWallCalculator.WindLoadCalculations
{
    /// <summary>
    ///  In ASCE7-16, the dynamic wind pressure coefficient includes Kd in the dynamic wind calculation.  
    ///  In ASCE7_22 it does not -- instead applying Kd to the external and net pressure calculations separately.  
    ///  In the end the calculations are the same.  This now better matches these codes.  
    ///  But dyanmic wind pressure qz and qh will be drastically different.
    /// </summary>
    public class WindLoadCalculator_ASCE7_22_Base : WindLoadCalculator_Base
    {
        public override ASCE7_Versions ASCEVersion => ASCE7_Versions.ASCE_VER_7_22;

        public override WindParameters_Base Parameters { get; set; }
        public override BuildingData buildingData { get; set; }
        public override AreaCalculator_Base RoofAreaCalculator { get; set; }
        public override AreaCalculator_Base WallAreaCalculator_BldgLength { get; set; }
        public override AreaCalculator_Base WallAreaCalculator_BldgWidth { get; set; }
        public override Chapter27and30_GCpCurveBase extGCpCurve_Roof { get; set; }
        public override Chapter27and30_GCpCurveBase extGCpCurve_Wall { get; set; }

        public override double CalculateDynamicWindPressure(double z)
        {
            if (Parameters == null)
                return -1000;

            WindParameters_Base p = Parameters;

            double V = p.WindSpeed;
            double Kzt = p.Kzt;
            double I = p.ImportanceFactor;
            double Kz = GetKz(z, p.ExposureCategory);
            double qz = 0.00256 * Kz * Kzt * V * V * I;
            return qz;
        }

        /// <summary>
        /// Calculates the qh * GCP for external pressures
        /// </summary>
        public override void CalculateExternalPressures()
        {

        }

        /// <summary>
        /// Calculates the qh * GCP for external pressures minus the internal pressure qh * GCpi
        /// </summary>
        public override void CalculateNetPressures()
        {
            var kd = Parameters.Kd;
            // roof pressure positive
            foreach (var items in windPressureRoof_Pos_External)
            {
                double int_pressure = CalculateDynamicWindPressure(buildingData.MeanRoofHeight) * GetGCpi() * kd;
                windPressureRoof_Pos_Net.Add(items.Key, items.Value + int_pressure);
            }

            // roof pressure negative
            foreach (var items in windPressureRoof_Neg_External)
            {
                double int_pressure = CalculateDynamicWindPressure(buildingData.MeanRoofHeight) * GetGCpi() * kd;
                windPressureRoof_Neg_Net.Add(items.Key, items.Value - int_pressure);
            }

            // BuildingWidth wall pressure positive
            foreach (var items in windPressureBuildingWidthWall_Pos_External)
            {
                double pressure = CalculateDynamicWindPressure(buildingData.MeanRoofHeight) * GetGCpi() * kd;
                windPressureBuildingWidthWall_Pos_Net.Add(items.Key, items.Value + pressure);
            }

            // BuildingWidth wall pressure negative
            foreach (var items in windPressureBuildingWidthWall_Neg_External)
            {
                double pressure = CalculateDynamicWindPressure(buildingData.MeanRoofHeight) * GetGCpi() * kd;
                windPressureBuildingWidthWall_Neg_Net.Add(items.Key, items.Value - pressure);
            }

            // BuildingLength pressure positive
            foreach (var items in windPressureBuildingLengthWall_Pos_External)
            {
                double pressure = CalculateDynamicWindPressure(buildingData.MeanRoofHeight) * GetGCpi() * kd;
                windPressureBuildingLength_Pos_Net.Add(items.Key, items.Value + pressure);
            }

            // Building Length pressure negative
            foreach (var items in windPressureBuildingLengthWall_Neg_External)
            {
                double pressure = CalculateDynamicWindPressure(buildingData.MeanRoofHeight) * GetGCpi() * kd;
                windPressureBuildingLengthWall_Neg_Net.Add(items.Key, items.Value - pressure);
            }
        }

        // Get Kz approximation based on building height and exposure category
        public override double GetKz(double z, WindExposureCategories exposure)
        {
            double zg, alpha;

            switch (exposure)
            {
                case WindExposureCategories.WIND_EXP_CAT_B:
                    zg = 1200.0;
                    alpha = 7.0;
                    break;
                case WindExposureCategories.WIND_EXP_CAT_C:
                    zg = 900.0;
                    alpha = 9.5;
                    break;
                case WindExposureCategories.WIND_EXP_CAT_D:
                    zg = 700.0;
                    alpha = 11.5;
                    break;
                default:
                    zg = 900.0;
                    alpha = 9.5;
                    break;
            }

            z = Math.Max(z, 15); // Minimum height for Kz is 15 ft
            return 2.01 * Math.Pow(z / zg, 2.0 / alpha);
        }

        /// <summary>
        /// Compute Kd coefficient for the specific version of ASCE7
        /// </summary>
        /// <param name="calc_type"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public override double GetKd(WindLoadCalculationTypes calc_type)
        {
            switch (calc_type)
            {
                case WindLoadCalculationTypes.COMPONENT_AND_CLADDING:
                    return 0.85;
                case WindLoadCalculationTypes.MWFRS:
                    return 0.85;
                default:
                    throw new NotImplementedException("ERROR:  " + calc_type + " not supported. ");
            }
        }

        /// <summary>
        /// The +/- coefficient for internal pressure coefficient GCpi from ASCE7_16 & ASCE7-22 Table 26.13-1
        /// </summary>
        /// <returns></returns>
        public override double GetGCpi()
        {
            switch (buildingData.EnclosureType)
            {
                case BuildingEnclosures.BLDG_ENCLOSED: return 0.18;
                case BuildingEnclosures.BLDG_PARTIALLY_ENCLOSED: return 0.55;
                case BuildingEnclosures.BLDG_PARTIALLY_OPEN: return 0.18;
                case BuildingEnclosures.BLDG_OPEN: return 0.0;
                default: throw new Exception("ERROR: Invalid enclosure type: " + buildingData.EnclosureType + " in WindLoadCalculator_MWFRS_ASCE7_22 constructor.");
            }
        }
    }
}
