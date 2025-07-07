using System;
using System.Collections.Generic;
using System.Windows;

namespace ShearWallCalculator.WindLoadCalculations
{
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
        public WindLoadCalculationTypes AnalysisType { get; set; } = WindLoadCalculationTypes.COMPONENT_AND_CLADDING;

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
        public Dictionary<int, EffectiveWindArea_Roof> effWindAreas_Roof { get; set; } = new Dictionary<int, EffectiveWindArea_Roof>();
        public Dictionary<int, double> GCp_Values { get; set; } = new Dictionary<int, double>();
        
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

        public void ComputeEffectiveWindAreas_Roof()
        {
            // Figure 30.3-2A -- Flat roof and Gable / Hip with slope less than 7
            if (RoofType == RoofTypes.ROOF_TYPE_FLAT || RoofPitch < 7)
            {
                ComputeFlatRoofAreas();
                return;
            }

            if(RoofType == RoofTypes.ROOF_TYPE_GABLE)
            {
                ComputeGableRoofAreas();
            } 
            else if (RoofType == RoofTypes.ROOF_TYPE_HIP)
            {
                ComputeHipRoofAreas();
            } else
            {
                throw new NotImplementedException("Error in ComputeEffectiveWindAreas_Roof() -- Invalid roof type: " + RoofType);
            }
        }

        private void ComputeHipRoofAreas()
        {
            // Figure 30.3-2E / 2F / 2G / 2H / 2I -- Flat roof and Gable with slope greater than 7
            // Map if Length is less than width -- ridge is vertical on map
            //  E-----F
            //  | \ / |
            //  |  D  |
            //  |  |  |
            //  |  C  |
            //  | / \ |
            //  A=----B 
            if (BuildingLength < BuildingWidth)
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
            }
            else if (BuildingLength > BuildingWidth)
            {
                // Figure 30.3-2E / 2F / 2G / 2H / 2I -- Flat roof and Gable with slope greater than 7
                // Map if Length is less than width -- ridge is vertical on map
                //  E--------F
                //  |\      /|
                //  | C----D |
                //  |/      \|
                //  A--------B
                //
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

            // Building length and width are equal, so all zone 1s are triangles
            else
            {
                // Figure 30.3-2E / 2F / 2G / 2H / 2I -- Flat roof and Gable with slope greater than 7
                // Map if Length is less than width -- ridge is vertical on map
                //  D---E
                //  |\ /|
                //  | C |
                //  |/ \|
                //  A---B
                //

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
        }
        private void ComputeGableRoofAreas()
        {
            if (BuildingLength < BuildingWidth)
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
            else if (BuildingLength > BuildingWidth)
            {
                // Figure 30.3-2B / 2C / 2D -- Flat roof and Gable with slope greater than 7

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

            // Building length and width are equal, so all zone 1s are triangles
            else
            {
                // Figure 30.3-2B / 2C / 2D -- Flat roof and Gable with slope greater than 7
                if (RoofType == RoofTypes.ROOF_TYPE_GABLE)
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
            }
        }
        private void ComputeFlatRoofAreas()
        {
            effWindAreas_Roof.Clear();

            double halfMinDim = Math.Min(BuildingLength, BuildingWidth) / 2.0;

            // Compute zone offsets based on MRH, clamped to avoid overlap
            double d12 = Math.Min(1.2 * MeanRoofHeight, halfMinDim - 0.01);
            double d06 = Math.Min(0.6 * MeanRoofHeight, halfMinDim - 0.01);
            double d02 = Math.Min(0.2 * MeanRoofHeight, d06 - 0.01);

            int id = 1;

            // === Zone 1′ (interior zone)
            if (d12 < halfMinDim)
            {
                Point p1 = new Point(d12, d12);
                Point p2 = new Point(BuildingLength - d12, d12);
                Point p3 = new Point(BuildingLength - d12, BuildingWidth - d12);
                Point p4 = new Point(d12, BuildingWidth - d12);

                var zone1p = new EffectiveWindArea_Roof("Zone_1p",
                    new List<Point> { p1, p2, p3, p4 }, null);

                effWindAreas_Roof.Add(id++, zone1p);
            }

            // === Zone 1 (middle band)
            if (d06 < halfMinDim)
            {
                Point p5 = new Point(d06, d06);
                Point p6 = new Point(BuildingLength - d06, d06);
                Point p7 = new Point(BuildingLength - d06, BuildingWidth - d06);
                Point p8 = new Point(d06, BuildingWidth - d06);

                List<Point> outer1 = new List<Point> { p5, p6, p7, p8 };
                List<Point> inner1 = null;

                if (d12 < halfMinDim)
                {
                    Point p1 = new Point(d12, d12);
                    Point p2 = new Point(BuildingLength - d12, d12);
                    Point p3 = new Point(BuildingLength - d12, BuildingWidth - d12);
                    Point p4 = new Point(d12, BuildingWidth - d12);
                    inner1 = new List<Point> { p4, p3, p2, p1 }; // CW hole
                }

                var zone1 = new EffectiveWindArea_Roof("Zone_1", outer1, inner1 != null ? new[] { inner1 } : null);
                effWindAreas_Roof.Add(id++, zone1);

                // === Zone 2 (outer band around zone 1)
                Point p9 = new Point(0, 0);
                Point p10 = new Point(BuildingLength, 0);
                Point p11 = new Point(BuildingLength, BuildingWidth);
                Point p12 = new Point(0, BuildingWidth);

                var zone2 = new EffectiveWindArea_Roof("Zone_2",
                    new List<Point> { p9, p10, p11, p12 },
                    new[] { new List<Point> { p8, p7, p6, p5 } }); // CW hole

                effWindAreas_Roof.Add(id++, zone2);
            }

            // === Zone 3 (corner zones), only if d02 < d06
            if (d02 < d06 - 0.01)
            {
                // Lower-left corner
                var zone3_1 = new EffectiveWindArea_Roof("Zone_3_LL",
                    new List<Point>
                    {
                new Point(0, 0),
                new Point(d06, 0),
                new Point(d06, d02),
                new Point(d02, d02),
                new Point(d02, d06),
                new Point(0, d06)
                    },
                    null);
                effWindAreas_Roof.Add(id++, zone3_1);

                // Lower-right corner
                var zone3_2 = new EffectiveWindArea_Roof("Zone_3_LR",
                    new List<Point>
                    {
                new Point(BuildingLength, 0),
                new Point(BuildingLength, d06),
                new Point(BuildingLength - d02, d06),
                new Point(BuildingLength - d02, d02),
                new Point(BuildingLength - d06, d02),
                new Point(BuildingLength - d06, 0)
                    },
                    null);
                effWindAreas_Roof.Add(id++, zone3_2);

                // Upper-right corner
                var zone3_3 = new EffectiveWindArea_Roof("Zone_3_UR",
                    new List<Point>
                    {
                new Point(BuildingLength - d06, BuildingWidth),
                new Point(BuildingLength - d06, BuildingWidth - d02),
                new Point(BuildingLength - d02, BuildingWidth - d02),
                new Point(BuildingLength - d02, BuildingWidth - d06),
                new Point(BuildingLength, BuildingWidth - d06),
                new Point(BuildingLength, BuildingWidth)
                    },
                    null);
                effWindAreas_Roof.Add(id++, zone3_3);

                // Upper-left corner
                var zone3_4 = new EffectiveWindArea_Roof("Zone_3_UL",
                    new List<Point>
                    {
                new Point(0, BuildingWidth),
                new Point(0, BuildingWidth - d06),
                new Point(d02, BuildingWidth - d06),
                new Point(d02, BuildingWidth - d02),
                new Point(d06, BuildingWidth - d02),
                new Point(d06, BuildingWidth)
                    },
                    null);
                effWindAreas_Roof.Add(id++, zone3_4);
            }

            return;
        }

        //private void ComputeFlatRoofAreas()
        //{
        //    effWindAreas_Roof.Clear(); // Optional: clear any existing zones

        //    double d12 = 1.2 * MeanRoofHeight;
        //    double d06 = 0.6 * MeanRoofHeight;
        //    double d02 = 0.2 * MeanRoofHeight;

        //    double maxOffset = Math.Min(BuildingLength, BuildingWidth) / 2.0;

        //    // Basic validation to prevent invalid geometry
        //    if (d12 >= maxOffset)
        //        throw new InvalidOperationException($"MeanRoofHeight is too large: 1.2 * H = {d12:F2} exceeds half of the shortest building dimension ({maxOffset:F2}).");

        //    if (d06 >= maxOffset)
        //        throw new InvalidOperationException($"MeanRoofHeight is too large: 0.6 * H = {d06:F2} exceeds half of the shortest building dimension ({maxOffset:F2}).");

        //    if (d02 >= d06)
        //        throw new InvalidOperationException($"MeanRoofHeight is too large: 0.2 * H = {d02:F2} is greater than or equal to 0.6 * H = {d06:F2}, causing invalid zone 3 corner geometry.");

        //    // === Proceed with safe calculations ===

        //    Point p1 = new Point(d12, d12);
        //    Point p2 = new Point(BuildingLength - d12, d12);
        //    Point p3 = new Point(BuildingLength - d12, BuildingWidth - d12);
        //    Point p4 = new Point(d12, BuildingWidth - d12);

        //    var roof_area_1_prime = new EffectiveWindArea_Roof(
        //        "Zone_1p",
        //        new List<Point> { p1, p2, p3, p4 },
        //        null);

        //    Point p5 = new Point(d06, d06);
        //    Point p6 = new Point(BuildingLength - d06, d06);
        //    Point p7 = new Point(BuildingLength - d06, BuildingWidth - d06);
        //    Point p8 = new Point(d06, BuildingWidth - d06);

        //    var roof_area_1 = new EffectiveWindArea_Roof(
        //        "Zone_1",
        //        new List<Point> { p5, p6, p7, p8 },
        //        new[] { new List<Point> { p4, p3, p2, p1 } }
        //    );

        //    Point p9 = new Point(0, 0);
        //    Point p10 = new Point(BuildingLength, 0);
        //    Point p11 = new Point(BuildingLength, BuildingWidth);
        //    Point p12 = new Point(0, BuildingWidth);

        //    var roof_area_2 = new EffectiveWindArea_Roof(
        //        "Zone_2",
        //        new List<Point> { p9, p10, p11, p12 },
        //        new[] { new List<Point> { p8, p7, p6, p5 } }
        //    );

        //    // Region 3 zones (only show one for brevity; copy for others)
        //    Point p13 = new Point(0, 0);
        //    Point p14 = new Point(d06, 0);
        //    Point p15 = new Point(d06, d02);
        //    Point p16 = new Point(d02, d02);
        //    Point p17 = new Point(d02, d06);
        //    Point p18 = new Point(0, d06);

        //    var roof_area_3_1 = new EffectiveWindArea_Roof(
        //        "Zone_3_LL",
        //        new List<Point> { p13, p14, p15, p16, p17, p18 },
        //        null);

        //    // TODO: Repeat for 3_2, 3_3, 3_4

        //    effWindAreas_Roof.Add(1, roof_area_1_prime);
        //    effWindAreas_Roof.Add(2, roof_area_1);
        //    effWindAreas_Roof.Add(3, roof_area_2);
        //    effWindAreas_Roof.Add(4, roof_area_3_1);
        //    // effWindAreas_Roof.Add(5, ...);

        //    return;
        //}



        //private void ComputeFlatRoofAreas()
        //{
        //    // central flat region 1' outer perimeter in CCW
        //    Point p1 = new Point(1.2 * MeanRoofHeight, 1.2 * MeanRoofHeight);
        //    Point p2 = new Point(BuildingLength - (1.2 * MeanRoofHeight), 1.2 * MeanRoofHeight);
        //    Point p3 = new Point(BuildingLength - (1.2 * MeanRoofHeight), BuildingWidth - (1.2 * MeanRoofHeight));
        //    Point p4 = new Point(1.2 * MeanRoofHeight, BuildingWidth - (1.2 * MeanRoofHeight));

        //    var roof_area_1_prime = new EffectiveWindArea_Roof(
        //        "1'",
        //        new List<Point> { p1, p2, p3, p4 },
        //        null);

        //    //  region 1 middle band in CCW -- holes are CW p4,p3, p2, p1
        //    Point p5 = new Point(0.6 * MeanRoofHeight, 0.6 * MeanRoofHeight);
        //    Point p6 = new Point(BuildingLength - 0.6 * MeanRoofHeight, 0.6 * MeanRoofHeight);
        //    Point p7 = new Point(BuildingLength - 0.6 * MeanRoofHeight, BuildingWidth - 0.6 * MeanRoofHeight);
        //    Point p8 = new Point(0.6 * MeanRoofHeight, BuildingWidth - 0.6 * MeanRoofHeight);

        //    var roof_area_1 = new EffectiveWindArea_Roof(
        //        "1",
        //        new List<Point> { p5, p6, p7, p8 },
        //        new[] { new List<Point> { p4, p3, p2, p1 } }
        //        );

        //    // region 2 outer band in CCW -- holes are CW p8, p7, p6, p5
        //    Point p9 = new Point(0, 0);
        //    Point p10 = new Point(BuildingLength, 0);
        //    Point p11 = new Point(BuildingLength, BuildingWidth);
        //    Point p12 = new Point(0, BuildingWidth);

        //    var roof_area_2 = new EffectiveWindArea_Roof(
        //        "2",
        //        new List<Point> { p9, p10, p11, p12 },
        //        new[] { new List<Point> { p8, p7, p6, p5 } }
        //        );

        //    // region 3 corner zones
        //    // lower left
        //    Point p13 = new Point(0, 0);
        //    Point p14 = new Point(0.6 * MeanRoofHeight, 0);
        //    Point p15 = new Point(0.6 * MeanRoofHeight, 0.2 * MeanRoofHeight);
        //    Point p16 = new Point(0.2 * MeanRoofHeight, 0.2 * MeanRoofHeight);
        //    Point p17 = new Point(0.2 * MeanRoofHeight, 0.6 * MeanRoofHeight);
        //    Point p18 = new Point(0, 0.6 * MeanRoofHeight);

        //    var roof_area_3_1 = new EffectiveWindArea_Roof(
        //        "3_1",
        //        new List<Point> { p13, p14, p15, p16, p17, p18 },
        //        null
        //        );

        //    // lower right
        //    Point p19 = new Point(BuildingLength, 0);
        //    Point p20 = new Point(BuildingLength, 0.6 * MeanRoofHeight);
        //    Point p21 = new Point(BuildingLength - 0.2 * MeanRoofHeight, 0.6 * MeanRoofHeight);
        //    Point p22 = new Point(BuildingLength - 0.2 * MeanRoofHeight, 0.2 * MeanRoofHeight);
        //    Point p23 = new Point(BuildingLength - 0.6 * MeanRoofHeight, 0.2 * MeanRoofHeight);
        //    Point p24 = new Point(BuildingLength - 0.6 * MeanRoofHeight, 0);


        //    var roof_area_3_2 = new EffectiveWindArea_Roof(
        //        "3_2",
        //        new List<Point> { p19, p20, p21, p22, p23, p24 },
        //        null
        //        );

        //    // upper right
        //    Point p25 = new Point(BuildingLength - 0.6 * MeanRoofHeight, BuildingWidth);
        //    Point p26 = new Point(BuildingLength - 0.6 * MeanRoofHeight, BuildingWidth - 0.2 * MeanRoofHeight);
        //    Point p27 = new Point(BuildingLength - 0.2 * MeanRoofHeight, BuildingWidth - 0.2 * MeanRoofHeight);
        //    Point p28 = new Point(BuildingLength - 0.2 * MeanRoofHeight, BuildingWidth - 0.6 * MeanRoofHeight);
        //    Point p29 = new Point(BuildingLength, BuildingWidth - 0.6 * MeanRoofHeight);
        //    Point p30 = new Point(BuildingLength, BuildingWidth);

        //    var roof_area_3_3 = new EffectiveWindArea_Roof(
        //        "3_3",
        //        new List<Point> { p25, p26, p27, p28, p29, p30 },
        //        null
        //        );

        //    // upper left
        //    Point p31 = new Point(0, BuildingWidth);
        //    Point p32 = new Point(0, BuildingWidth - 0.6 * MeanRoofHeight);
        //    Point p33 = new Point(0.2 * MeanRoofHeight, BuildingWidth - 0.6 * MeanRoofHeight);
        //    Point p34 = new Point(0.2 * MeanRoofHeight, BuildingWidth - 0.2 * MeanRoofHeight);
        //    Point p35 = new Point(0.6 * MeanRoofHeight, BuildingWidth - 0.2 * MeanRoofHeight);
        //    Point p36 = new Point(0.6 * MeanRoofHeight, BuildingWidth);

        //    var roof_area_3_4 = new EffectiveWindArea_Roof(
        //        "3_4",
        //        new List<Point> { p31, p32, p33, p34, p35, p36 },
        //        null
        //        );

        //    effWindAreas_Roof.Add(1, roof_area_1_prime); //z1_prime
        //    effWindAreas_Roof.Add(2, roof_area_1);  //z1
        //    effWindAreas_Roof.Add(3, roof_area_2);  //z2
        //    effWindAreas_Roof.Add(4, roof_area_3_1);  //z3
        //    effWindAreas_Roof.Add(5, roof_area_3_2);  //z3
        //    effWindAreas_Roof.Add(6, roof_area_3_3);  //z3
        //    effWindAreas_Roof.Add(7, roof_area_3_4);  //z3

        //    return;
        //}
    }
}
