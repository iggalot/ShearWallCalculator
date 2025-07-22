using ShearWallCalculator.BuildingInfo;
using ShearWallCalculator.WindLoadCalculations.Chapter30;
using ShearWallCalculator.WindLoadCalculations.Chapter30.Figure30_3;
using System;
using System.ComponentModel;

namespace ShearWallCalculator.WindLoadCalculations
{
    public enum RoofTypes
    {
        ROOF_TYPE_FLAT = 0,
        ROOF_TYPE_GABLE = 1,
        ROOF_TYPE_HIP = 2
    }
    public enum ASCE7_Versions
    {
        //ASCE_VER_7_10 = 0,
        ASCE_VER_7_16 = 1,
        ASCE_VER_7_22 = 2
    }

    public enum WindLoadCalculationTypes
    {
        COMPONENT_AND_CLADDING = 0,  // Components and cladding  -- Chapter 30
        MWFRS = 1 // MWFRS
    }

    public enum WindExposureCategories
    {
        WIND_EXP_CAT_B = 1,
        WIND_EXP_CAT_C = 2,
        WIND_EXP_CAT_D = 3
    }

    public enum WindZones_CC
    {
        CC_1 = 1,       // Roof flat zone
        CC_2 = 2,       // Roof edge zone
        CC_3 = 3,       // Roof corner zone
        CC_4 = 4,       // Wall flat zone
        CC_5 = 5        // Wall edge zone (width 'a')
    }

    public enum WindLoadCases
    {
        WLC_BaseA,
        WLC_BaseB,
        WLC_Balloon1,
        WLC_Balloon2,
        WLC_Suction1,
        WLC_Suction2
    }

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

    public abstract class WindLoadCalculator_Base
    {
        public virtual ASCE7_Versions ASCEVersion { get; }
        public WindLoadParameters_Base Parameters { get; set; }
        public BuildingData buildingData { get; set; }

        // Which figure of Ch30_3_2A thru I to use for CC roof
        public Chapter30_BaseFigure extGCpCurve_Roof { get; set; }

        // The curve of Ch30_3_1 to use for CC walls
        public Chapter30_BaseFigure extGCpCurve_Wall { get; set; }

        public void CreateExtGcpCurves()
        {
            switch (ASCEVersion)
            {
                case ASCE7_Versions.ASCE_VER_7_16:
                    extGCpCurve_Roof = Chapter30RoofFigureFactory_ASCE7_16.CreateRoofFigure_ASCE7_16(
                         buildingData.RoofType, buildingData.MeanRoofHeight, buildingData.BuildingWidth, buildingData.RoofPitch);
                    extGCpCurve_Wall = new Figure30_3_1_ASCE7_16();
                    break;
                case ASCE7_Versions.ASCE_VER_7_22:
                    extGCpCurve_Roof = Chapter30RoofFigureFactory_ASCE7_22.CreateRoofFigure_ASCE7_22(
                        buildingData.RoofType, buildingData.MeanRoofHeight, buildingData.BuildingWidth, buildingData.RoofPitch);
                    extGCpCurve_Wall = new Figure30_3_1_ASCE7_22();
                    break;
                default:
                    throw new Exception("ERROR: Invalid ASCE Version: " + ASCEVersion + " in WindLoadCalculator_Base constructor.");
            }
        }

        /// <summary>
        /// Calculates the dyanmic wind pressure q at a specified height z per ASCE7_16 and ASCE7_22
        /// </summary>
        /// <param name="p"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public virtual double CalculateDynamicWindPressure(double z)
        {
            if (Parameters == null)
                return -1000;

            WindLoadParameters_Base p = Parameters;

            double V = p.WindSpeed;
            double Kd = p.Kd;
            double Kzt = p.Kzt;
            double I = p.ImportanceFactor;
            double Kz = GetKz(z, p.ExposureCategory);
            double qz = 0.00256 * Kz * Kzt * Kd * V * V * I;
            return qz;
        }


        // Get Kz approximation based on building height and exposure category
        public virtual double GetKz(double z, WindExposureCategories exposure)
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
        public virtual double GetKd(WindLoadCalculationTypes calc_type)
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
        public virtual double GetGCpi()
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
