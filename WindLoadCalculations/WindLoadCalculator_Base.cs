using System;
using System.ComponentModel;
using System.Security.Cryptography;

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
        ASCE_VER_7_05 = 0,
        ASCE_VER_7_10 = 1,
        ASCE_VER_7_16 = 2,
        ASCE_VER_7_22 = 3
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

    // Wind Load Parameters class
    public class WindLoadParameters
    {
        public string RiskCategory { get; set; }
        public double WindSpeed { get; set; }
        public WindExposureCategories ExposureCategory { get; set; }
        public double BuildingHeight { get; set; }
        public string EnclosureClassification { get; set; }
        public double Kd { get; set; }
        public double Kzt { get; set; } = 1.0;
        public double GustFactor { get; set; } = 0.85;
        public double ImportanceFactor { get; set; }
        public double BuildingLength { get; set; }
        public double BuildingWidth { get; set; }
        public double RoofPitch { get; set; }
        public string RidgeDirection { get; set; }
        public RoofTypes RoofType { get; set; } = RoofTypes.ROOF_TYPE_GABLE;

        /// <summary>
        /// The mean roof height of the building, h per ASCE7
        /// </summary>
        public double MeanRoofHeight { get => GetMeanRoofHeight(); }

        /// <summary>
        /// The critical width dimenstion "a" used throughout chapter 30
        /// -- minimum of 0.4 * building height and 0.1 * min(building Length, building width)
        /// </summary>
        public double CritDim_a { get => GetCritDim_a(); }

        /// <summary>
        /// Zone areas
        /// </summary>
        public double A1 { get; set; } = -1.0;  // zone 1
        public double A1_prime { get; set; } = -1.0; // zone 1' -- Fig 30.3-2A
        public double A2 { get; set; } = -1.0;  // zone 2
        public double A2_n { get; set; } = -1.0;  // zone 2n -- Fig 30.3-2B // normal to ridge at edge of roof
        public double A2_e { get; set; } = -1.0;  // zone 2e -- Fig 30.3-2B // parallel to ridge at edge of roof
        public double A2_r { get; set; } = -1.0;  // zone 2r -- Fig 30.3-2B // parallel to ridge at peak
        public double A3 { get; set; } = -1.0;  // zone 3
        public double A3_e { get; set; } = -1.0;  // zone 3e -- Fig 30.3-2B
        public double A3_r { get; set; } = -1.0;  // zone 3r -- Fig 30.3-2B
        public double A4_1 { get; set; } = -1.0;  // zone 4 -- area of Zone 4 on end wall
        public double A4_2 { get; set; } = -1.0;  // zone 4 -- area of Zone 4 on side wall
        public double A5 { get; set; } = -1.0;  // zone 5

        private double GetCritDim_a()
        {
            return Math.Min(0.4 * MeanRoofHeight, 0.1 * Math.Min(BuildingLength, BuildingWidth));
        }

        /// <summary>
        /// Determines the mean roof height 
        /// </summary>
        /// <returns></returns>
        public double GetMeanRoofHeight()
        {   if(RoofType == RoofTypes.ROOF_TYPE_FLAT)
                return BuildingHeight;
            if (RoofType == RoofTypes.ROOF_TYPE_GABLE || RoofType == RoofTypes.ROOF_TYPE_HIP)
            {
                double h1 = Math.Tan(RoofPitch * Math.PI / 180.0) * BuildingLength / 2.0;
                double h2 = Math.Tan(RoofPitch * Math.PI / 180.0) * BuildingWidth / 2.0;

                // choose the smalles
                return BuildingHeight + Math.Min(h1, h2);

            }
            else
                return BuildingHeight / 2.0;
        }

        public void ComputeEffectiveWindAreas()
        {
            // Compute Wall Effective Areas
            A4_1 = BuildingHeight * (BuildingWidth - 2.0 * CritDim_a);
            A4_2 = BuildingHeight * (BuildingLength - 2.0 * CritDim_a);

            A5 = BuildingHeight * CritDim_a;

            // Figure 30.3-2A -- Flat roof and Gable / Hip with slope less than 7
            if(RoofType == RoofTypes.ROOF_TYPE_FLAT ||
                (RoofType == RoofTypes.ROOF_TYPE_GABLE && RoofPitch < 7) ||
                (RoofType == RoofTypes.ROOF_TYPE_HIP && RoofPitch < 7)){

                // central flat region
                A1_prime = BuildingLength * BuildingWidth - (BuildingLength - 1.2 * MeanRoofHeight)*(BuildingWidth - 1.2 * MeanRoofHeight);
                // outer ring around building
                A1 = ((BuildingLength-0.6*MeanRoofHeight) * (BuildingWidth-0.6*MeanRoofHeight)) -A1_prime;
                A2 = (BuildingLength * BuildingWidth) - (BuildingLength - 0.6 * MeanRoofHeight) * (BuildingWidth - 0.6 * MeanRoofHeight) - A1_prime;
                A3 = 2.0 * (0.6 * MeanRoofHeight) * (0.2 * MeanRoofHeight);
                return;
            }

            // Figure 30.3-2B / 2C / 2D -- Flat roof and Gable with slope greater than 7
            if  (RoofType == RoofTypes.ROOF_TYPE_GABLE && RoofPitch > 7) 
            {
                A3_e = CritDim_a * CritDim_a;
                A3_r = CritDim_a * CritDim_a;

                // TODO:  Fix these so that A2_e is parallel to ridge
                // Building length assumed parallel to ridge
                A2_e = (BuildingLength - 2.0 * CritDim_a) * CritDim_a;
                A2_n = CritDim_a * BuildingWidth - 2.0 * A3_e - 2.0 * A3_r;
                A2_r = A2_e;

                // central flat region
                A1 = 0.5 * ((BuildingLength - CritDim_a) * (BuildingWidth - CritDim_a) - 2.0*A2_r);
                return;
            }

            // Figure 30.3-2E / 2F / 2G / 2H / 2I -- Flat roof and Gable with slope greater than 7
            if (RoofType == RoofTypes.ROOF_TYPE_HIP && RoofPitch > 7)
            {
                A3 = CritDim_a * CritDim_a;

                // TODO:  Fix these so that A2_e is parallel to ridge
                // Building length assumed parallel to ridge
                A2_e = (BuildingLength - 2.0 * CritDim_a) * CritDim_a;
                A2_n = CritDim_a * BuildingWidth - 2.0 * A3;  // this is an A2_e also on the chart

                // TODO:  Calculate A1 values
                // A1 on short side (perp to ridge) is a triangle
                // A1 on long side (parallel to ridge) is a rectangle


                throw new NotImplementedException("TODO:  Implement Hip with slope > 7");
                //A3_e = CritDim_a * CritDim_a;
                //A3_r = CritDim_a * CritDim_a;

                //// TODO:  Fix these so that A2_e is parallel to ridge
                //A2_e = (BuildingLength - 2.0 * CritDim_a) * CritDim_a;
                //A2_n = CritDim_a * BuildingWidth - 2.0 * A3_e - 2.0 * A3_r;
                //A2_r = A2_e;

                //// central flat region
                //A1 = 0.5 * ((BuildingLength - CritDim_a) * (BuildingWidth - CritDim_a) - 2.0 * A2_r);
                //return;
            }
        }

    }

    public class WindLoadCalculator_Base
    {
        public virtual ASCE7_Versions ASCEVersion { get; }
        public WindLoadParameters Parameters { get; set; }

        /// <summary>
        /// Calculates the dyanmic wind pressure q at a specified height z
        /// </summary>
        /// <param name="p"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public static double CalculateDynamicWindPressure(WindLoadParameters p, double z)
        {
            double V = p.WindSpeed;
            double Kd = p.Kd;
            double Kzt = p.Kzt;
            double I = p.ImportanceFactor;
            double Kz = GetKz(z, p.ExposureCategory);
            double qz = 0.00256 * Kz * Kzt * Kd * V * V * I;
            return qz;
        }

        // Get Kz approximation based on building height and exposure category
        public static double GetKz(double z, WindExposureCategories exposure)
        {
            double zg, alpha;

            switch (exposure)
            {
                case WindExposureCategories.WIND_EXP_CAT_B:
                    zg = 1200;
                    alpha = 7.0;
                    break;
                case WindExposureCategories.WIND_EXP_CAT_C:
                    zg = 900;
                    alpha = 9.5;
                    break;
                case WindExposureCategories.WIND_EXP_CAT_D:
                    zg = 700;
                    alpha = 11.5;
                    break;
                default:
                    zg = 900;
                    alpha = 9.5;
                    break;
            }

            z = Math.Max(z, 15); // Minimum height for Kz is 15 ft
            return 2.01 * Math.Pow(z / zg, 2.0 / alpha);
        } 
    }
}
