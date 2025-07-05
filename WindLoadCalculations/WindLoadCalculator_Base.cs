using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;

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
        public RoofTypes RoofType { get; set; } = RoofTypes.ROOF_TYPE_HIP;

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
        /// Effective wind areas for roof
        /// </summary>
        public Dictionary<int, EffectiveWindArea_Roof> effWindAreas_Roof { get; set; }
        //public double A1 { get; set; } = -1.0;  // zone 1
        //public double A1_prime { get; set; } = -1.0; // zone 1' -- Fig 30.3-2A
        //public double A2 { get; set; } = -1.0;  // zone 2
        //public double A2_n { get; set; } = -1.0;  // zone 2n -- Fig 30.3-2B // normal to ridge at edge of roof
        //public double A2_e { get; set; } = -1.0;  // zone 2e -- Fig 30.3-2B // parallel to ridge at edge of roof
        //public double A2_r { get; set; } = -1.0;  // zone 2r -- Fig 30.3-2B // parallel to ridge at peak
        //public double A3 { get; set; } = -1.0;  // zone 3
        //public double A3_e { get; set; } = -1.0;  // zone 3e -- Fig 30.3-2B
        //public double A3_r { get; set; } = -1.0;  // zone 3r -- Fig 30.3-2B
        
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
            effWindAreas_Roof = new Dictionary<int, EffectiveWindArea_Roof>();

            // Compute Wall Effective Areas
            A4_1 = BuildingHeight * (BuildingWidth - 2.0 * CritDim_a);
            A4_2 = BuildingHeight * (BuildingLength - 2.0 * CritDim_a);

            A5 = BuildingHeight * CritDim_a;



            if (BuildingLength < BuildingWidth)
            {
                // Figure 30.3-2A -- Flat roof and Gable / Hip with slope less than 7
                if (RoofType == RoofTypes.ROOF_TYPE_FLAT ||
                    (RoofType == RoofTypes.ROOF_TYPE_GABLE && RoofPitch < 7) ||
                    (RoofType == RoofTypes.ROOF_TYPE_HIP && RoofPitch < 7))
                {

                    // central flat region 1' out perimieter in CCW
                    Point p1 = new Point(1.2 * MeanRoofHeight, 1.2 * MeanRoofHeight);
                    Point p2 = new Point(BuildingLength - (1.2 * MeanRoofHeight), 1.2 * MeanRoofHeight);
                    Point p3 = new Point(BuildingLength - (1.2 * MeanRoofHeight), BuildingWidth - (1.2 * MeanRoofHeight));
                    Point p4 = new Point(1.2 * MeanRoofHeight, BuildingWidth - (1.2 * MeanRoofHeight));

                    var roof_area_1_prime = new EffectiveWindArea_Roof(
                        "1'",
                        new List<Point> { p1, p2, p3, p4 },
                        null);

                    //  region 1 middle band in CCW -- holes are CW p4,p3, p2, p1
                    Point p5 = new Point(0.6 * MeanRoofHeight, 0.6 * MeanRoofHeight);
                    Point p6 = new Point(BuildingLength - 0.6 * MeanRoofHeight, 0.6 * MeanRoofHeight);
                    Point p7 = new Point(BuildingLength - 0.6 * MeanRoofHeight, BuildingWidth - 0.6 * MeanRoofHeight);
                    Point p8 = new Point(0.6 * MeanRoofHeight, BuildingWidth - 0.6 * MeanRoofHeight);

                    var roof_area_1 = new EffectiveWindArea_Roof(
                        "1'",
                        new List<Point> { p5, p6, p7, p8 },
                        new[] { new List<Point> { p4, p3, p2, p1 } }
                        );

                    // region 2 outer band in CCW -- holes are CW p8, p7, p6, p5
                    Point p9 = new Point(0, 0);
                    Point p10 = new Point(BuildingLength, 0);
                    Point p11 = new Point(BuildingLength, BuildingWidth);
                    Point p12 = new Point(0, BuildingWidth);

                    var roof_area_2 = new EffectiveWindArea_Roof(
                        "2",
                        new List<Point> { p9, p10, p11, p12 },
                        new[] { new List<Point> { p8, p7, p6, p5 } }
                        );

                    // region 3 corner zones
                    // lower left
                    Point p13 = new Point(0, 0);
                    Point p14 = new Point(0.6 * MeanRoofHeight, 0);
                    Point p15 = new Point(0.6 * MeanRoofHeight, 0.2 * MeanRoofHeight);
                    Point p16 = new Point(0.2 * MeanRoofHeight, 0.2 * MeanRoofHeight);
                    Point p17 = new Point(0.2 * MeanRoofHeight, 0.6 * MeanRoofHeight);
                    Point p18 = new Point(0, 0.6 * MeanRoofHeight);

                    var roof_area_3_1 = new EffectiveWindArea_Roof(
                        "3_1",
                        new List<Point> { p13, p14, p15, p16, p17, p18 },
                        null
                        );

                    // lower right
                    Point p19 = new Point(BuildingLength, 0);
                    Point p20 = new Point(BuildingLength, 0.6 * MeanRoofHeight);
                    Point p21 = new Point(BuildingLength - 0.2 * MeanRoofHeight, 0.6 * MeanRoofHeight);
                    Point p22 = new Point(BuildingLength - 0.2 * MeanRoofHeight, 0.2 * MeanRoofHeight);
                    Point p23 = new Point(BuildingLength - 0.6 * MeanRoofHeight, 0.2 * MeanRoofHeight);
                    Point p24 = new Point(BuildingLength - 0.6 * MeanRoofHeight, 0);


                    var roof_area_3_2 = new EffectiveWindArea_Roof(
                        "3_2",
                        new List<Point> { p19, p20, p21, p22, p23, p24 },
                        null
                        );

                    // upper right
                    Point p25 = new Point(BuildingLength - 0.6 * MeanRoofHeight, BuildingWidth - 0.6 * MeanRoofHeight);
                    Point p26 = new Point(BuildingLength, BuildingWidth - 0.6 * MeanRoofHeight);
                    Point p27 = new Point(BuildingLength, BuildingWidth);
                    Point p28 = new Point(BuildingLength - 0.6 * MeanRoofHeight, BuildingWidth);
                    Point p29 = new Point(BuildingLength - 0.6 * MeanRoofHeight, BuildingWidth - 0.2 * MeanRoofHeight);
                    Point p30 = new Point(BuildingLength - 0.2 * MeanRoofHeight, BuildingWidth - 0.2 * MeanRoofHeight);

                    var roof_area_3_3 = new EffectiveWindArea_Roof(
                        "3_3",
                        new List<Point> { p25, p26, p27, p28, p29, p30 },
                        null
                        );

                    // upper left
                    Point p31 = new Point(0, BuildingWidth);
                    Point p32 = new Point(0, BuildingWidth - 0.6 * MeanRoofHeight);
                    Point p33 = new Point(0.6 * MeanRoofHeight, BuildingWidth - 0.6 * MeanRoofHeight);
                    Point p34 = new Point(0.6 * MeanRoofHeight, BuildingWidth - 0.2 * MeanRoofHeight);
                    Point p35 = new Point(0.2 * MeanRoofHeight, BuildingWidth - 0.2 * MeanRoofHeight);
                    Point p36 = new Point(0.6 * MeanRoofHeight, BuildingWidth);

                    var roof_area_3_4 = new EffectiveWindArea_Roof(
                        "3_4",
                        new List<Point> { p31, p32, p33, p34, p35, p36 },
                        null
                        );

                    effWindAreas_Roof.Add(1, roof_area_1_prime); //z1_prime
                    effWindAreas_Roof.Add(2, roof_area_1);  //z1
                    effWindAreas_Roof.Add(3, roof_area_2);  //z2
                    effWindAreas_Roof.Add(4, roof_area_3_1);  //z3
                    effWindAreas_Roof.Add(5, roof_area_3_2);  //z3
                    effWindAreas_Roof.Add(6, roof_area_3_3);  //z3
                    effWindAreas_Roof.Add(7, roof_area_3_4);  //z3

                    return;
                }

                // Figure 30.3-2B / 2C / 2D -- Flat roof and Gable with slope greater than 7
                else if (RoofType == RoofTypes.ROOF_TYPE_GABLE && RoofPitch > 7)
                {
                    Point p1 = new Point(0, 0);
                    Point p2 = new Point(CritDim_a, 0);
                    Point p3 = new Point(0.5 * BuildingLength - CritDim_a, 0);
                    Point p4 = new Point(0.5 * BuildingLength, 0);
                    Point p5 = new Point(0.5 * BuildingLength + CritDim_a, 0);
                    Point p6 = new Point(BuildingLength - CritDim_a, 0);
                    Point p7 = new Point(BuildingLength, 0);

                    Point p11 = new Point(0, CritDim_a);
                    Point p12 = new Point(CritDim_a, CritDim_a);
                    Point p13 = new Point(0.5 * BuildingLength - CritDim_a, CritDim_a);
                    Point p14 = new Point(0.5 * BuildingLength, CritDim_a);
                    Point p15 = new Point(0.5 * BuildingLength + CritDim_a, CritDim_a);
                    Point p16 = new Point(BuildingLength - CritDim_a, CritDim_a);
                    Point p17 = new Point(BuildingLength, CritDim_a);

                    Point p21 = new Point(0, BuildingWidth - CritDim_a);
                    Point p22 = new Point(CritDim_a, BuildingWidth - CritDim_a);
                    Point p23 = new Point(0.5 * BuildingLength - CritDim_a, BuildingWidth - CritDim_a);
                    Point p24 = new Point(0.5 * BuildingLength, BuildingWidth - CritDim_a);
                    Point p25 = new Point(0.5 * BuildingLength + CritDim_a, BuildingWidth - CritDim_a);
                    Point p26 = new Point(BuildingLength - CritDim_a, BuildingWidth - CritDim_a);
                    Point p27 = new Point(BuildingLength, BuildingWidth - CritDim_a);

                    Point p31 = new Point(0, BuildingWidth);
                    Point p32 = new Point(CritDim_a, BuildingWidth);
                    Point p33 = new Point(0.5 * BuildingLength - CritDim_a, BuildingWidth);
                    Point p34 = new Point(0.5 * BuildingLength, BuildingWidth);
                    Point p35 = new Point(0.5 * BuildingLength + CritDim_a, BuildingWidth);
                    Point p36 = new Point(BuildingLength - CritDim_a, BuildingWidth);
                    Point p37 = new Point(BuildingLength, BuildingWidth);

                    // Bottom row of rectangles
                    effWindAreas_Roof.Add(1, new EffectiveWindArea_Roof("3e", new List<Point> { p1, p2, p12, p11 }, null));
                    effWindAreas_Roof.Add(2, new EffectiveWindArea_Roof("2n", new List<Point> { p2, p3, p13, p12 }, null));
                    effWindAreas_Roof.Add(3, new EffectiveWindArea_Roof("3r", new List<Point> { p3, p4, p14, p13 }, null));
                    effWindAreas_Roof.Add(4, new EffectiveWindArea_Roof("3r", new List<Point> { p4, p5, p15, p14 }, null));
                    effWindAreas_Roof.Add(5, new EffectiveWindArea_Roof("2n", new List<Point> { p5, p6, p16, p15 }, null));
                    effWindAreas_Roof.Add(6, new EffectiveWindArea_Roof("3e", new List<Point> { p6, p7, p17, p16 }, null));

                    // Middle row of rectangles
                    effWindAreas_Roof.Add(7, new EffectiveWindArea_Roof("2e", new List<Point> { p11, p12, p22, p21 }, null));
                    effWindAreas_Roof.Add(8, new EffectiveWindArea_Roof("1", new List<Point> { p12, p13, p23, p22 }, null));
                    effWindAreas_Roof.Add(9, new EffectiveWindArea_Roof("2r", new List<Point> { p13, p14, p24, p23 }, null));
                    effWindAreas_Roof.Add(10, new EffectiveWindArea_Roof("2r", new List<Point> { p14, p15, p25, p24 }, null));
                    effWindAreas_Roof.Add(11, new EffectiveWindArea_Roof("1", new List<Point> { p15, p16, p26, p25 }, null));
                    effWindAreas_Roof.Add(12, new EffectiveWindArea_Roof("2e", new List<Point> { p16, p17, p27, p26 }, null));

                    // Top row of rectangles
                    effWindAreas_Roof.Add(13, new EffectiveWindArea_Roof("3e", new List<Point> { p21, p22, p32, p31 }, null));
                    effWindAreas_Roof.Add(14, new EffectiveWindArea_Roof("2n", new List<Point> { p22, p23, p33, p32 }, null));
                    effWindAreas_Roof.Add(15, new EffectiveWindArea_Roof("3r", new List<Point> { p23, p24, p34, p33 }, null));
                    effWindAreas_Roof.Add(16, new EffectiveWindArea_Roof("3r", new List<Point> { p24, p25, p35, p34 }, null));
                    effWindAreas_Roof.Add(17, new EffectiveWindArea_Roof("2n", new List<Point> { p25, p26, p36, p35 }, null));
                    effWindAreas_Roof.Add(18, new EffectiveWindArea_Roof("3e", new List<Point> { p26, p27, p37, p36 }, null));

                    return;
                }

                // Figure 30.3-2E / 2F / 2G / 2H / 2I -- Flat roof and Gable with slope greater than 7
                // Map if Length is less than width -- ridge is vertical on map
                //  E-----F
                //  |\   /|
                //  | \ / |
                //  |  D  |
                //  |  |  |
                //  |  |  |
                //  |  C  |
                //  | / \ |
                //  |/   \|
                //  A=----B 

                else if (RoofType == RoofTypes.ROOF_TYPE_HIP && RoofPitch > 7)
                {
                    // Corners of the hip roof planes
                    Point A = new Point(0, 0);
                    Point B = new Point(BuildingLength, 0);
                    Point C = new Point(0.5 * BuildingLength, 0.5 * BuildingLength);
                    Point D = new Point(0.5 * BuildingLength, BuildingWidth - 0.5 * BuildingLength);
                    Point E = new Point(0, BuildingWidth);
                    Point F = new Point(BuildingLength, BuildingWidth);

                    Point p1 = new Point(0, 0);
                    Point p2 = new Point(CritDim_a, 0);
                    Point p3 = new Point(BuildingLength - CritDim_a, 0);
                    Point p4 = new Point(BuildingLength, 0);

                    Point p11 = new Point(0, CritDim_a);
                    Point p12 = new Point(CritDim_a, CritDim_a);
                    Point p13 = new Point(BuildingLength - CritDim_a, CritDim_a);
                    Point p14 = new Point(BuildingLength, CritDim_a);

                    Point p21 = new Point(0, BuildingWidth - CritDim_a);
                    Point p22 = new Point(CritDim_a, BuildingWidth - CritDim_a);
                    Point p23 = new Point(BuildingLength - CritDim_a, BuildingWidth - CritDim_a);
                    Point p24 = new Point(BuildingLength, BuildingWidth - CritDim_a);

                    Point p31 = new Point(0, BuildingWidth);
                    Point p32 = new Point(CritDim_a, BuildingWidth);
                    Point p33 = new Point(BuildingLength - CritDim_a, BuildingWidth);
                    Point p34 = new Point(BuildingLength, BuildingWidth);

                    effWindAreas_Roof.Add(1, new EffectiveWindArea_Roof("3", new List<Point> { p1, p2, p12, p11 }, null));
                    effWindAreas_Roof.Add(2, new EffectiveWindArea_Roof("2e", new List<Point> { p2, p3, p13, p12 }, null));
                    effWindAreas_Roof.Add(3, new EffectiveWindArea_Roof("3", new List<Point> { p3, p4, p14, p13 }, null));
                    effWindAreas_Roof.Add(4, new EffectiveWindArea_Roof("2e", new List<Point> { p11, p12, p22, p21 }, null));
                    effWindAreas_Roof.Add(5, new EffectiveWindArea_Roof("2e", new List<Point> { p13, p14, p24, p23 }, null));
                    effWindAreas_Roof.Add(6, new EffectiveWindArea_Roof("3", new List<Point> { p21, p22, p32, p31 }, null));
                    effWindAreas_Roof.Add(7, new EffectiveWindArea_Roof("2e", new List<Point> { p22, p23, p33, p32 }, null));
                    effWindAreas_Roof.Add(8, new EffectiveWindArea_Roof("3", new List<Point> { p23, p24, p34, p33 }, null));

                    // for finding the inset points
                    var inset_dist = 1.414 * CritDim_a;

                    // lower triangle
                    Point p40 = new Point(p12.X + inset_dist, p12.Y);
                    Point p41 = new Point(p13.X - inset_dist, p13.Y);
                    Point p42 = new Point(C.X, C.Y - inset_dist);
                    effWindAreas_Roof.Add(9, new EffectiveWindArea_Roof("1", new List<Point> { p40, p41, p42 }, null));
                    effWindAreas_Roof.Add(10, new EffectiveWindArea_Roof("2r", new List<Point> { p12, p40, p42, p41, p13, C }, null));

                    // left trapezoid
                    Point p50 = new Point(p12.X, p12.Y + inset_dist);
                    Point p51 = new Point(C.X - CritDim_a, C.Y + (inset_dist - CritDim_a));
                    Point p52 = new Point(D.X - CritDim_a, D.Y - (inset_dist - CritDim_a));
                    Point p53 = new Point(p22.X, p22.Y - inset_dist);
                    effWindAreas_Roof.Add(11, new EffectiveWindArea_Roof("1", new List<Point> { p50, p51, p52, p53 }, null));
                    effWindAreas_Roof.Add(12, new EffectiveWindArea_Roof("2r", new List<Point> { p12, C, D, p22, p53, p52, p51, p50 }, null));

                    // left trapezoid
                    Point p60 = new Point(p13.X, p13.Y + inset_dist);
                    Point p61 = new Point(p23.X, p23.Y - inset_dist);
                    Point p62 = new Point(D.X + CritDim_a, D.Y - (inset_dist - CritDim_a));
                    Point p63 = new Point(C.X + CritDim_a, C.Y + (inset_dist - CritDim_a));
                    effWindAreas_Roof.Add(13, new EffectiveWindArea_Roof("1", new List<Point> { p60, p61, p62, p63 }, null));
                    effWindAreas_Roof.Add(14, new EffectiveWindArea_Roof("2r", new List<Point> { p13, p60, p63, p62, p61, p23, D, C }, null));

                    // top triangle
                    Point p70 = new Point(p22.X + inset_dist, p22.Y);
                    Point p71 = new Point(p23.X - inset_dist, p23.Y);
                    Point p72 = new Point(D.X, D.Y + inset_dist);
                    effWindAreas_Roof.Add(15, new EffectiveWindArea_Roof("1", new List<Point> { p70, p72, p71 }, null));
                    effWindAreas_Roof.Add(16, new EffectiveWindArea_Roof("2r", new List<Point> { p22, D, p23, p71, p72, p70 }, null));

                } else
                {
                    throw new Exception("ERROR:  In ComputeEffectiveWindAreas() -- Invalid building dimensions detected. L:  " + BuildingLength + " W: " + BuildingWidth + " RoofType: " + RoofType + "  theta: " + RoofPitch);
                }
            } 
            else if (BuildingLength > BuildingWidth)
            {
                // Figure 30.3-2A -- Flat roof and Gable / Hip with slope less than 7
                if (RoofType == RoofTypes.ROOF_TYPE_FLAT ||
                    (RoofType == RoofTypes.ROOF_TYPE_GABLE && RoofPitch < 7) ||
                    (RoofType == RoofTypes.ROOF_TYPE_HIP && RoofPitch < 7))
                {

                    // central flat region 1' out perimieter in CCW
                    Point p1 = new Point(1.2 * MeanRoofHeight, 1.2 * MeanRoofHeight);
                    Point p2 = new Point(BuildingLength - (1.2 * MeanRoofHeight), 1.2 * MeanRoofHeight);
                    Point p3 = new Point(BuildingLength - (1.2 * MeanRoofHeight), BuildingWidth - (1.2 * MeanRoofHeight));
                    Point p4 = new Point(1.2 * MeanRoofHeight, BuildingWidth - (1.2 * MeanRoofHeight));

                    var roof_area_1_prime = new EffectiveWindArea_Roof(
                        "1'",
                        new List<Point> { p1, p2, p3, p4 },
                        null);

                    //  region 1 middle band in CCW -- holes are CW p4,p3, p2, p1
                    Point p5 = new Point(0.6 * MeanRoofHeight, 0.6 * MeanRoofHeight);
                    Point p6 = new Point(BuildingLength - 0.6 * MeanRoofHeight, 0.6 * MeanRoofHeight);
                    Point p7 = new Point(BuildingLength - 0.6 * MeanRoofHeight, BuildingWidth - 0.6 * MeanRoofHeight);
                    Point p8 = new Point(0.6 * MeanRoofHeight, BuildingWidth - 0.6 * MeanRoofHeight);

                    var roof_area_1 = new EffectiveWindArea_Roof(
                        "1'",
                        new List<Point> { p5, p6, p7, p8 },
                        new[] { new List<Point> { p4, p3, p2, p1 } }
                        );

                    // region 2 outer band in CCW -- holes are CW p8, p7, p6, p5
                    Point p9 = new Point(0, 0);
                    Point p10 = new Point(BuildingLength, 0);
                    Point p11 = new Point(BuildingLength, BuildingWidth);
                    Point p12 = new Point(0, BuildingWidth);

                    var roof_area_2 = new EffectiveWindArea_Roof(
                        "2",
                        new List<Point> { p9, p10, p11, p12 },
                        new[] { new List<Point> { p8, p7, p6, p5 } }
                        );

                    // region 3 corner zones
                    // lower left
                    Point p13 = new Point(0, 0);
                    Point p14 = new Point(0.6 * MeanRoofHeight, 0);
                    Point p15 = new Point(0.6 * MeanRoofHeight, 0.2 * MeanRoofHeight);
                    Point p16 = new Point(0.2 * MeanRoofHeight, 0.2 * MeanRoofHeight);
                    Point p17 = new Point(0.2 * MeanRoofHeight, 0.6 * MeanRoofHeight);
                    Point p18 = new Point(0, 0.6 * MeanRoofHeight);

                    var roof_area_3_1 = new EffectiveWindArea_Roof(
                        "3_1",
                        new List<Point> { p13, p14, p15, p16, p17, p18 },
                        null
                        );

                    // lower right
                    Point p19 = new Point(BuildingLength, 0);
                    Point p20 = new Point(BuildingLength, 0.6 * MeanRoofHeight);
                    Point p21 = new Point(BuildingLength - 0.2 * MeanRoofHeight, 0.6 * MeanRoofHeight);
                    Point p22 = new Point(BuildingLength - 0.2 * MeanRoofHeight, 0.2 * MeanRoofHeight);
                    Point p23 = new Point(BuildingLength - 0.6 * MeanRoofHeight, 0.2 * MeanRoofHeight);
                    Point p24 = new Point(BuildingLength - 0.6 * MeanRoofHeight, 0);


                    var roof_area_3_2 = new EffectiveWindArea_Roof(
                        "3_2",
                        new List<Point> { p19, p20, p21, p22, p23, p24 },
                        null
                        );

                    // upper right
                    Point p25 = new Point(BuildingLength - 0.6 * MeanRoofHeight, BuildingWidth - 0.6 * MeanRoofHeight);
                    Point p26 = new Point(BuildingLength, BuildingWidth - 0.6 * MeanRoofHeight);
                    Point p27 = new Point(BuildingLength, BuildingWidth);
                    Point p28 = new Point(BuildingLength - 0.6 * MeanRoofHeight, BuildingWidth);
                    Point p29 = new Point(BuildingLength - 0.6 * MeanRoofHeight, BuildingWidth - 0.2 * MeanRoofHeight);
                    Point p30 = new Point(BuildingLength - 0.2 * MeanRoofHeight, BuildingWidth - 0.2 * MeanRoofHeight);

                    var roof_area_3_3 = new EffectiveWindArea_Roof(
                        "3_3",
                        new List<Point> { p25, p26, p27, p28, p29, p30 },
                        null
                        );

                    // upper left
                    Point p31 = new Point(0, BuildingWidth);
                    Point p32 = new Point(0, BuildingWidth - 0.6 * MeanRoofHeight);
                    Point p33 = new Point(0.6 * MeanRoofHeight, BuildingWidth - 0.6 * MeanRoofHeight);
                    Point p34 = new Point(0.6 * MeanRoofHeight, BuildingWidth - 0.2 * MeanRoofHeight);
                    Point p35 = new Point(0.2 * MeanRoofHeight, BuildingWidth - 0.2 * MeanRoofHeight);
                    Point p36 = new Point(0.6 * MeanRoofHeight, BuildingWidth);

                    var roof_area_3_4 = new EffectiveWindArea_Roof(
                        "3_4",
                        new List<Point> { p31, p32, p33, p34, p35, p36 },
                        null
                        );

                    effWindAreas_Roof.Add(1, roof_area_1_prime); //z1_prime
                    effWindAreas_Roof.Add(2, roof_area_1);  //z1
                    effWindAreas_Roof.Add(3, roof_area_2);  //z2
                    effWindAreas_Roof.Add(4, roof_area_3_1);  //z3
                    effWindAreas_Roof.Add(5, roof_area_3_2);  //z3
                    effWindAreas_Roof.Add(6, roof_area_3_3);  //z3
                    effWindAreas_Roof.Add(7, roof_area_3_4);  //z3

                    return;
                }

                // Figure 30.3-2B / 2C / 2D -- Flat roof and Gable with slope greater than 7
                else if (RoofType == RoofTypes.ROOF_TYPE_GABLE && RoofPitch > 7)
                {
                    Point p1 = new Point(0, 0);
                    Point p2 = new Point(CritDim_a, 0);
                    Point p3 = new Point(BuildingLength - CritDim_a, 0);
                    Point p4 = new Point(BuildingLength, 0);

                    Point p11 = new Point(0, CritDim_a);
                    Point p12 = new Point(CritDim_a, CritDim_a);
                    Point p13 = new Point(BuildingLength - CritDim_a, CritDim_a);
                    Point p14 = new Point(BuildingLength, CritDim_a);

                    Point p21 = new Point(0, 0.5 * BuildingWidth - CritDim_a);
                    Point p22 = new Point(CritDim_a, 0.5 * BuildingWidth - CritDim_a);
                    Point p23 = new Point(BuildingLength - CritDim_a, 0.5 * BuildingWidth - CritDim_a);
                    Point p24 = new Point(BuildingLength, 0.5 * BuildingWidth - CritDim_a);

                    Point p31 = new Point(0, 0.5 * BuildingWidth);
                    Point p32 = new Point(CritDim_a, 0.5 * BuildingWidth);
                    Point p33 = new Point(BuildingLength - CritDim_a, 0.5 * BuildingWidth);
                    Point p34 = new Point(BuildingLength, 0.5 * BuildingWidth);

                    Point p41 = new Point(0, 0.5 * BuildingWidth + CritDim_a);
                    Point p42 = new Point(CritDim_a, 0.5 * BuildingWidth + CritDim_a);
                    Point p43 = new Point(BuildingLength - CritDim_a, 0.5 * BuildingWidth + CritDim_a);
                    Point p44 = new Point(BuildingLength, 0.5 * BuildingWidth + CritDim_a);

                    Point p51 = new Point(0, BuildingWidth - CritDim_a);
                    Point p52 = new Point(CritDim_a, BuildingWidth - CritDim_a);
                    Point p53 = new Point(BuildingLength - CritDim_a, BuildingWidth - CritDim_a);
                    Point p54 = new Point(BuildingLength, BuildingWidth - CritDim_a);

                    Point p61 = new Point(0, BuildingWidth);
                    Point p62 = new Point(CritDim_a, BuildingWidth);
                    Point p63 = new Point(BuildingLength - CritDim_a, BuildingWidth);
                    Point p64 = new Point(BuildingLength, BuildingWidth);


                    // Bottom row of rectangles
                    effWindAreas_Roof.Add(1, new EffectiveWindArea_Roof("3e", new List<Point> { p1, p2, p12, p11 }, null));
                    effWindAreas_Roof.Add(2, new EffectiveWindArea_Roof("2e", new List<Point> { p2, p3, p13, p12 }, null));
                    effWindAreas_Roof.Add(3, new EffectiveWindArea_Roof("3e", new List<Point> { p3, p4, p14, p13 }, null));

                    // Middle row of rectangles
                    effWindAreas_Roof.Add(4, new EffectiveWindArea_Roof("2n", new List<Point> { p11, p12, p22, p21 }, null));
                    effWindAreas_Roof.Add(5, new EffectiveWindArea_Roof("1", new List<Point> { p12, p13, p23, p22 }, null));
                    effWindAreas_Roof.Add(6, new EffectiveWindArea_Roof("2n", new List<Point> { p13, p14, p24, p23 }, null));

                    //// Top row of rectangles
                    effWindAreas_Roof.Add(7, new EffectiveWindArea_Roof("3r", new List<Point> { p21, p22, p32, p31 }, null));
                    effWindAreas_Roof.Add(8, new EffectiveWindArea_Roof("2r", new List<Point> { p22, p23, p33, p32 }, null));
                    effWindAreas_Roof.Add(9, new EffectiveWindArea_Roof("3r", new List<Point> { p23, p24, p34, p33 }, null));

                    // ------------------ RIDGE ---------------- //

                    // Bottom row of rectangles
                    effWindAreas_Roof.Add(10, new EffectiveWindArea_Roof("3r", new List<Point> { p31, p32, p42, p41 }, null));
                    effWindAreas_Roof.Add(11, new EffectiveWindArea_Roof("2r", new List<Point> { p32, p33, p43, p42 }, null));
                    effWindAreas_Roof.Add(12, new EffectiveWindArea_Roof("3r", new List<Point> { p33, p34, p44, p43 }, null));

                    // Middle row of rectangles
                    effWindAreas_Roof.Add(13, new EffectiveWindArea_Roof("2n", new List<Point> { p41, p42, p52, p51 }, null));
                    effWindAreas_Roof.Add(14, new EffectiveWindArea_Roof("1", new List<Point> { p42, p43, p53, p52 }, null));
                    effWindAreas_Roof.Add(15, new EffectiveWindArea_Roof("2n", new List<Point> { p43, p44, p54, p53 }, null));

                    // Top row of rectangles
                    effWindAreas_Roof.Add(16, new EffectiveWindArea_Roof("3e", new List<Point> { p51, p52, p62, p61 }, null));
                    effWindAreas_Roof.Add(17, new EffectiveWindArea_Roof("2e", new List<Point> { p52, p53, p63, p62 }, null));
                    effWindAreas_Roof.Add(18, new EffectiveWindArea_Roof("3e", new List<Point> { p53, p54, p64, p63 }, null));

                    return;
                }

                // Figure 30.3-2E / 2F / 2G / 2H / 2I -- Flat roof and Gable with slope greater than 7
                // Map if Length is less than width -- ridge is vertical on map
                //  E--------F
                //  |\      /|
                //  | C----D |
                //  |/      \|
                //  A--------B
                //

                else if (RoofType == RoofTypes.ROOF_TYPE_HIP && RoofPitch > 7)
                {
                    // Corners of the hip roof planes
                    Point A = new Point(0, 0);
                    Point B = new Point(BuildingLength, 0);
                    Point C = new Point(0.5 * BuildingWidth, 0.5 * BuildingWidth);
                    Point D = new Point(BuildingLength - 0.5 * BuildingWidth, 0.5 * BuildingWidth);
                    Point E = new Point(0, BuildingWidth);
                    Point F = new Point(BuildingLength, BuildingWidth);

                    Point p1 = new Point(0, 0);
                    Point p2 = new Point(CritDim_a, 0);
                    Point p3 = new Point(BuildingLength - CritDim_a, 0);
                    Point p4 = new Point(BuildingLength, 0);

                    Point p11 = new Point(0, CritDim_a);
                    Point p12 = new Point(CritDim_a, CritDim_a);
                    Point p13 = new Point(BuildingLength - CritDim_a, CritDim_a);
                    Point p14 = new Point(BuildingLength, CritDim_a);

                    Point p21 = new Point(0, BuildingWidth - CritDim_a);
                    Point p22 = new Point(CritDim_a, BuildingWidth - CritDim_a);
                    Point p23 = new Point(BuildingLength - CritDim_a, BuildingWidth - CritDim_a);
                    Point p24 = new Point(BuildingLength, BuildingWidth - CritDim_a);

                    Point p31 = new Point(0, BuildingWidth);
                    Point p32 = new Point(CritDim_a, BuildingWidth);
                    Point p33 = new Point(BuildingLength - CritDim_a, BuildingWidth);
                    Point p34 = new Point(BuildingLength, BuildingWidth);

                    effWindAreas_Roof.Add(1, new EffectiveWindArea_Roof("3", new List<Point> { p1, p2, p12, p11 }, null));
                    effWindAreas_Roof.Add(2, new EffectiveWindArea_Roof("2e", new List<Point> { p2, p3, p13, p12 }, null));
                    effWindAreas_Roof.Add(3, new EffectiveWindArea_Roof("3", new List<Point> { p3, p4, p14, p13 }, null));
                    effWindAreas_Roof.Add(4, new EffectiveWindArea_Roof("2e", new List<Point> { p11, p12, p22, p21 }, null));
                    effWindAreas_Roof.Add(5, new EffectiveWindArea_Roof("2e", new List<Point> { p13, p14, p24, p23 }, null));
                    effWindAreas_Roof.Add(6, new EffectiveWindArea_Roof("3", new List<Point> { p21, p22, p32, p31 }, null));
                    effWindAreas_Roof.Add(7, new EffectiveWindArea_Roof("2e", new List<Point> { p22, p23, p33, p32 }, null));
                    effWindAreas_Roof.Add(8, new EffectiveWindArea_Roof("3", new List<Point> { p23, p24, p34, p33 }, null));

                    // for finding the inset points
                    var inset_dist = 1.414 * CritDim_a;

                    // left triangle
                    Point p40 = new Point(p12.X, p12.Y + inset_dist);
                    Point p41 = new Point(C.X - inset_dist, C.Y);
                    Point p42 = new Point(p22.X, p22.Y - inset_dist);
                    effWindAreas_Roof.Add(9, new EffectiveWindArea_Roof("1", new List<Point> { p40, p41, p42 }, null));
                    effWindAreas_Roof.Add(10, new EffectiveWindArea_Roof("2r", new List<Point> { p12, C, p22, p42, p41, p40 }, null));

                    // lower trapezoid
                    Point p50 = new Point(p12.X + inset_dist, p12.Y);
                    Point p51 = new Point(p13.X - inset_dist, p13.Y);
                    Point p52 = new Point(D.X - (inset_dist - CritDim_a), D.Y - CritDim_a);
                    Point p53 = new Point(C.X + (inset_dist - CritDim_a), C.Y - CritDim_a);
                    effWindAreas_Roof.Add(11, new EffectiveWindArea_Roof("1", new List<Point> { p50, p51, p52, p53 }, null));
                    effWindAreas_Roof.Add(12, new EffectiveWindArea_Roof("2r", new List<Point> { p12, p50, p53, p52, p51, p13, D, C }, null));

                    // upper trapezoid
                    Point p60 = new Point(p22.X + inset_dist, p22.Y);
                    Point p61 = new Point(C.X + (inset_dist - CritDim_a), C.Y + CritDim_a);
                    Point p62 = new Point(D.X - (inset_dist - CritDim_a), D.Y + CritDim_a);
                    Point p63 = new Point(p23.X - inset_dist, p23.Y);

                    effWindAreas_Roof.Add(13, new EffectiveWindArea_Roof("1", new List<Point> { p60, p61, p62, p63 }, null));
                    effWindAreas_Roof.Add(14, new EffectiveWindArea_Roof("2r", new List<Point> { p22, C, D, p23, p63, p62, p61, p60 }, null));

                    // right triangle
                    Point p70 = new Point(p13.X, p13.Y + inset_dist);
                    Point p71 = new Point(p23.X, p23.Y - inset_dist);
                    Point p72 = new Point(D.X + inset_dist, D.Y);
                    effWindAreas_Roof.Add(15, new EffectiveWindArea_Roof("1", new List<Point> { p70, p71, p72 }, null));
                    effWindAreas_Roof.Add(16, new EffectiveWindArea_Roof("2r", new List<Point> { p13, p70, p72, p71, p23, D }, null));
                }
                else
                {
                    throw new Exception("ERROR:  In ComputeEffectiveWindAreas() -- Invalid building dimensions detected. L:  " + BuildingLength + " W: " + BuildingWidth + " RoofType: " + RoofType + "  theta: " + RoofPitch);
                }
            }
            // Building length and width are equal, so all zone 1s are triangles
            else
            {
                // Figure 30.3-2A -- Flat roof and Gable / Hip with slope less than 7
                if (RoofType == RoofTypes.ROOF_TYPE_FLAT ||
                    (RoofType == RoofTypes.ROOF_TYPE_GABLE && RoofPitch < 7) ||
                    (RoofType == RoofTypes.ROOF_TYPE_HIP && RoofPitch < 7))
                {

                    // central flat region 1' out perimeter in CCW
                    Point p1 = new Point(1.2 * MeanRoofHeight, 1.2 * MeanRoofHeight);
                    Point p2 = new Point(BuildingLength - (1.2 * MeanRoofHeight), 1.2 * MeanRoofHeight);
                    Point p3 = new Point(BuildingLength - (1.2 * MeanRoofHeight), BuildingWidth - (1.2 * MeanRoofHeight));
                    Point p4 = new Point(1.2 * MeanRoofHeight, BuildingWidth - (1.2 * MeanRoofHeight));

                    var roof_area_1_prime = new EffectiveWindArea_Roof(
                        "1'",
                        new List<Point> { p1, p2, p3, p4 },
                        null);

                    //  region 1 middle band in CCW -- holes are CW p4,p3, p2, p1
                    Point p5 = new Point(0.6 * MeanRoofHeight, 0.6 * MeanRoofHeight);
                    Point p6 = new Point(BuildingLength - 0.6 * MeanRoofHeight, 0.6 * MeanRoofHeight);
                    Point p7 = new Point(BuildingLength - 0.6 * MeanRoofHeight, BuildingWidth - 0.6 * MeanRoofHeight);
                    Point p8 = new Point(0.6 * MeanRoofHeight, BuildingWidth - 0.6 * MeanRoofHeight);

                    var roof_area_1 = new EffectiveWindArea_Roof(
                        "1'",
                        new List<Point> { p5, p6, p7, p8 },
                        new[] { new List<Point> { p4, p3, p2, p1 } }
                        );

                    // region 2 outer band in CCW -- holes are CW p8, p7, p6, p5
                    Point p9 = new Point(0, 0);
                    Point p10 = new Point(BuildingLength, 0);
                    Point p11 = new Point(BuildingLength, BuildingWidth);
                    Point p12 = new Point(0, BuildingWidth);

                    var roof_area_2 = new EffectiveWindArea_Roof(
                        "2",
                        new List<Point> { p9, p10, p11, p12 },
                        new[] { new List<Point> { p8, p7, p6, p5 } }
                        );

                    // region 3 corner zones
                    // lower left
                    Point p13 = new Point(0, 0);
                    Point p14 = new Point(0.6 * MeanRoofHeight, 0);
                    Point p15 = new Point(0.6 * MeanRoofHeight, 0.2 * MeanRoofHeight);
                    Point p16 = new Point(0.2 * MeanRoofHeight, 0.2 * MeanRoofHeight);
                    Point p17 = new Point(0.2 * MeanRoofHeight, 0.6 * MeanRoofHeight);
                    Point p18 = new Point(0, 0.6 * MeanRoofHeight);

                    var roof_area_3_1 = new EffectiveWindArea_Roof(
                        "3_1",
                        new List<Point> { p13, p14, p15, p16, p17, p18 },
                        null
                        );

                    // lower right
                    Point p19 = new Point(BuildingLength, 0);
                    Point p20 = new Point(BuildingLength, 0.6 * MeanRoofHeight);
                    Point p21 = new Point(BuildingLength - 0.2 * MeanRoofHeight, 0.6 * MeanRoofHeight);
                    Point p22 = new Point(BuildingLength - 0.2 * MeanRoofHeight, 0.2 * MeanRoofHeight);
                    Point p23 = new Point(BuildingLength - 0.6 * MeanRoofHeight, 0.2 * MeanRoofHeight);
                    Point p24 = new Point(BuildingLength - 0.6 * MeanRoofHeight, 0);


                    var roof_area_3_2 = new EffectiveWindArea_Roof(
                        "3_2",
                        new List<Point> { p19, p20, p21, p22, p23, p24 },
                        null
                        );

                    // upper right
                    Point p25 = new Point(BuildingLength - 0.6 * MeanRoofHeight, BuildingWidth - 0.6 * MeanRoofHeight);
                    Point p26 = new Point(BuildingLength, BuildingWidth - 0.6 * MeanRoofHeight);
                    Point p27 = new Point(BuildingLength, BuildingWidth);
                    Point p28 = new Point(BuildingLength - 0.6 * MeanRoofHeight, BuildingWidth);
                    Point p29 = new Point(BuildingLength - 0.6 * MeanRoofHeight, BuildingWidth - 0.2 * MeanRoofHeight);
                    Point p30 = new Point(BuildingLength - 0.2 * MeanRoofHeight, BuildingWidth - 0.2 * MeanRoofHeight);

                    var roof_area_3_3 = new EffectiveWindArea_Roof(
                        "3_3",
                        new List<Point> { p25, p26, p27, p28, p29, p30 },
                        null
                        );

                    // upper left
                    Point p31 = new Point(0, BuildingWidth);
                    Point p32 = new Point(0, BuildingWidth - 0.6 * MeanRoofHeight);
                    Point p33 = new Point(0.6 * MeanRoofHeight, BuildingWidth - 0.6 * MeanRoofHeight);
                    Point p34 = new Point(0.6 * MeanRoofHeight, BuildingWidth - 0.2 * MeanRoofHeight);
                    Point p35 = new Point(0.2 * MeanRoofHeight, BuildingWidth - 0.2 * MeanRoofHeight);
                    Point p36 = new Point(0.6 * MeanRoofHeight, BuildingWidth);

                    var roof_area_3_4 = new EffectiveWindArea_Roof(
                        "3_4",
                        new List<Point> { p31, p32, p33, p34, p35, p36 },
                        null
                        );

                    effWindAreas_Roof.Add(1, roof_area_1_prime); //z1_prime
                    effWindAreas_Roof.Add(2, roof_area_1);  //z1
                    effWindAreas_Roof.Add(3, roof_area_2);  //z2
                    effWindAreas_Roof.Add(4, roof_area_3_1);  //z3
                    effWindAreas_Roof.Add(5, roof_area_3_2);  //z3
                    effWindAreas_Roof.Add(6, roof_area_3_3);  //z3
                    effWindAreas_Roof.Add(7, roof_area_3_4);  //z3

                    return;
                }

                // Figure 30.3-2B / 2C / 2D -- Flat roof and Gable with slope greater than 7
                else if (RoofType == RoofTypes.ROOF_TYPE_GABLE && RoofPitch > 7)
                {
                    Point p1 = new Point(0, 0);
                    Point p2 = new Point(CritDim_a, 0);
                    Point p3 = new Point(BuildingLength - CritDim_a, 0);
                    Point p4 = new Point(BuildingLength, 0);

                    Point p11 = new Point(0, CritDim_a);
                    Point p12 = new Point(CritDim_a, CritDim_a);
                    Point p13 = new Point(BuildingLength - CritDim_a, CritDim_a);
                    Point p14 = new Point(BuildingLength, CritDim_a);

                    Point p21 = new Point(0, 0.5 * BuildingWidth - CritDim_a);
                    Point p22 = new Point(CritDim_a, 0.5 * BuildingWidth - CritDim_a);
                    Point p23 = new Point(BuildingLength - CritDim_a, 0.5 * BuildingWidth - CritDim_a);
                    Point p24 = new Point(BuildingLength, 0.5 * BuildingWidth - CritDim_a);

                    Point p31 = new Point(0, 0.5 * BuildingWidth);
                    Point p32 = new Point(CritDim_a, 0.5 * BuildingWidth);
                    Point p33 = new Point(BuildingLength - CritDim_a, 0.5 * BuildingWidth);
                    Point p34 = new Point(BuildingLength, 0.5 * BuildingWidth);

                    Point p41 = new Point(0, 0.5 * BuildingWidth + CritDim_a);
                    Point p42 = new Point(CritDim_a, 0.5 * BuildingWidth + CritDim_a);
                    Point p43 = new Point(BuildingLength - CritDim_a, 0.5 * BuildingWidth + CritDim_a);
                    Point p44 = new Point(BuildingLength, 0.5 * BuildingWidth + CritDim_a);

                    Point p51 = new Point(0, BuildingWidth - CritDim_a);
                    Point p52 = new Point(CritDim_a, BuildingWidth - CritDim_a);
                    Point p53 = new Point(BuildingLength - CritDim_a, BuildingWidth - CritDim_a);
                    Point p54 = new Point(BuildingLength, BuildingWidth - CritDim_a);

                    Point p61 = new Point(0, BuildingWidth);
                    Point p62 = new Point(CritDim_a, BuildingWidth);
                    Point p63 = new Point(BuildingLength - CritDim_a, BuildingWidth);
                    Point p64 = new Point(BuildingLength, BuildingWidth);


                    // Bottom row of rectangles
                    effWindAreas_Roof.Add(1, new EffectiveWindArea_Roof("3e", new List<Point> { p1, p2, p12, p11 }, null));
                    effWindAreas_Roof.Add(2, new EffectiveWindArea_Roof("2e", new List<Point> { p2, p3, p13, p12 }, null));
                    effWindAreas_Roof.Add(3, new EffectiveWindArea_Roof("3e", new List<Point> { p3, p4, p14, p13 }, null));

                    // Middle row of rectangles
                    effWindAreas_Roof.Add(4, new EffectiveWindArea_Roof("2n", new List<Point> { p11, p12, p22, p21 }, null));
                    effWindAreas_Roof.Add(5, new EffectiveWindArea_Roof("1", new List<Point> { p12, p13, p23, p22 }, null));
                    effWindAreas_Roof.Add(6, new EffectiveWindArea_Roof("2n", new List<Point> { p13, p14, p24, p23 }, null));

                    //// Top row of rectangles
                    effWindAreas_Roof.Add(7, new EffectiveWindArea_Roof("3r", new List<Point> { p21, p22, p32, p31 }, null));
                    effWindAreas_Roof.Add(8, new EffectiveWindArea_Roof("2r", new List<Point> { p22, p23, p33, p32 }, null));
                    effWindAreas_Roof.Add(9, new EffectiveWindArea_Roof("3r", new List<Point> { p23, p24, p34, p33 }, null));

                    // ------------------ RIDGE ---------------- //

                    // Bottom row of rectangles
                    effWindAreas_Roof.Add(10, new EffectiveWindArea_Roof("3r", new List<Point> { p31, p32, p42, p41 }, null));
                    effWindAreas_Roof.Add(11, new EffectiveWindArea_Roof("2r", new List<Point> { p32, p33, p43, p42 }, null));
                    effWindAreas_Roof.Add(12, new EffectiveWindArea_Roof("3r", new List<Point> { p33, p34, p44, p43 }, null));

                    // Middle row of rectangles
                    effWindAreas_Roof.Add(13, new EffectiveWindArea_Roof("2n", new List<Point> { p41, p42, p52, p51 }, null));
                    effWindAreas_Roof.Add(14, new EffectiveWindArea_Roof("1", new List<Point> { p42, p43, p53, p52 }, null));
                    effWindAreas_Roof.Add(15, new EffectiveWindArea_Roof("2n", new List<Point> { p43, p44, p54, p53 }, null));

                    // Top row of rectangles
                    effWindAreas_Roof.Add(16, new EffectiveWindArea_Roof("3e", new List<Point> { p51, p52, p62, p61 }, null));
                    effWindAreas_Roof.Add(17, new EffectiveWindArea_Roof("2e", new List<Point> { p52, p53, p63, p62 }, null));
                    effWindAreas_Roof.Add(18, new EffectiveWindArea_Roof("3e", new List<Point> { p53, p54, p64, p63 }, null));

                    return;
                }

                // Figure 30.3-2E / 2F / 2G / 2H / 2I -- Flat roof and Gable with slope greater than 7
                // Map if Length is less than width -- ridge is vertical on map
                //  D---E
                //  |\ /|
                //  | C |
                //  |/ \|
                //  A---B
                //

                else if (RoofType == RoofTypes.ROOF_TYPE_HIP && RoofPitch > 7)
                {
                    // Corners of the hip roof planes
                    Point A = new Point(0, 0);
                    Point B = new Point(BuildingLength, 0);
                    Point C = new Point(0.5 * BuildingLength, 0.5 * BuildingWidth);
                    Point E = new Point(0, BuildingWidth);
                    Point F = new Point(BuildingLength, BuildingWidth);

                    Point p1 = new Point(0, 0);
                    Point p2 = new Point(CritDim_a, 0);
                    Point p3 = new Point(BuildingLength - CritDim_a, 0);
                    Point p4 = new Point(BuildingLength, 0);

                    Point p11 = new Point(0, CritDim_a);
                    Point p12 = new Point(CritDim_a, CritDim_a);
                    Point p13 = new Point(BuildingLength - CritDim_a, CritDim_a);
                    Point p14 = new Point(BuildingLength, CritDim_a);

                    Point p21 = new Point(0, BuildingWidth - CritDim_a);
                    Point p22 = new Point(CritDim_a, BuildingWidth - CritDim_a);
                    Point p23 = new Point(BuildingLength - CritDim_a, BuildingWidth - CritDim_a);
                    Point p24 = new Point(BuildingLength, BuildingWidth - CritDim_a);

                    Point p31 = new Point(0, BuildingWidth);
                    Point p32 = new Point(CritDim_a, BuildingWidth);
                    Point p33 = new Point(BuildingLength - CritDim_a, BuildingWidth);
                    Point p34 = new Point(BuildingLength, BuildingWidth);

                    effWindAreas_Roof.Add(1, new EffectiveWindArea_Roof("3", new List<Point> { p1, p2, p12, p11 }, null));
                    effWindAreas_Roof.Add(2, new EffectiveWindArea_Roof("2e", new List<Point> { p2, p3, p13, p12 }, null));
                    effWindAreas_Roof.Add(3, new EffectiveWindArea_Roof("3", new List<Point> { p3, p4, p14, p13 }, null));
                    effWindAreas_Roof.Add(4, new EffectiveWindArea_Roof("2e", new List<Point> { p11, p12, p22, p21 }, null));
                    effWindAreas_Roof.Add(5, new EffectiveWindArea_Roof("2e", new List<Point> { p13, p14, p24, p23 }, null));
                    effWindAreas_Roof.Add(6, new EffectiveWindArea_Roof("3", new List<Point> { p21, p22, p32, p31 }, null));
                    effWindAreas_Roof.Add(7, new EffectiveWindArea_Roof("2e", new List<Point> { p22, p23, p33, p32 }, null));
                    effWindAreas_Roof.Add(8, new EffectiveWindArea_Roof("3", new List<Point> { p23, p24, p34, p33 }, null));

                    // for finding the inset points
                    var inset_dist = 1.414 * CritDim_a;

                    // left triangle
                    Point p40 = new Point(p12.X, p12.Y + inset_dist);
                    Point p41 = new Point(C.X - inset_dist, C.Y);
                    Point p42 = new Point(p22.X, p22.Y - inset_dist);
                    effWindAreas_Roof.Add(9, new EffectiveWindArea_Roof("1", new List<Point> { p40, p41, p42 }, null));
                    effWindAreas_Roof.Add(10, new EffectiveWindArea_Roof("2r", new List<Point> { p12, C, p22, p42, p41, p40 }, null));

                    // lower triangle
                    Point p50 = new Point(p12.X + inset_dist, p12.Y);
                    Point p51 = new Point(p13.X - inset_dist, p13.Y);
                    Point p52 = new Point(C.X, C.Y - inset_dist);
                    effWindAreas_Roof.Add(11, new EffectiveWindArea_Roof("1", new List<Point> { p50, p51, p52 }, null));
                    effWindAreas_Roof.Add(12, new EffectiveWindArea_Roof("2r", new List<Point> { p12, p50, p52, p51, p13, C }, null));

                    // upper triangle
                    Point p60 = new Point(p22.X + inset_dist, p22.Y);
                    Point p61 = new Point(C.X, C.Y + inset_dist);
                    Point p62 = new Point(p23.X - inset_dist, p23.Y);

                    effWindAreas_Roof.Add(13, new EffectiveWindArea_Roof("1", new List<Point> { p60, p61, p62 }, null));
                    effWindAreas_Roof.Add(14, new EffectiveWindArea_Roof("2r", new List<Point> { p22, C, p23, p62, p61, p60 }, null));

                    // right triangle
                    Point p70 = new Point(p13.X, p13.Y + inset_dist);
                    Point p71 = new Point(p23.X, p23.Y - inset_dist);
                    Point p72 = new Point(C.X + inset_dist, C.Y);
                    effWindAreas_Roof.Add(15, new EffectiveWindArea_Roof("1", new List<Point> { p70, p71, p72 }, null));
                    effWindAreas_Roof.Add(16, new EffectiveWindArea_Roof("2r", new List<Point> { p13, p70, p72, p71, p23, C }, null));
                }
                else
                {
                    throw new Exception("ERROR:  In ComputeEffectiveWindAreas() -- Invalid building dimensions detected. L:  " + BuildingLength + " W: " + BuildingWidth + " RoofType: " + RoofType + "  theta: " + RoofPitch);
                }
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
