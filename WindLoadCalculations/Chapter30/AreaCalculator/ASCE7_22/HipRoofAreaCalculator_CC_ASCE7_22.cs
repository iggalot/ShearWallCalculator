using ShearWallCalculator.BuildingInfo;
using ShearWallCalculator.WindLoadCalculations.Chapter30.AreaCalculator;
using System;
using System.Collections.Generic;
using System.Windows;

namespace ShearWallCalculator.WindLoadCalculations
{
    public class HipRoofAreaCalculator_CC_ASCE7_22 : RoofAreaCalculator_Base
    {
        /// <summary>
        /// Effective wind areas for roof
        /// </summary>
        public override Dictionary<int, EffectiveWindArea_Roof> effWindAreas_Roof { get; set; } = new Dictionary<int, EffectiveWindArea_Roof>();

        public static double CritDim_a { get; set; }

        public override void Compute(WindLoadParameters_Base parameters, BuildingData bldg_data)
        {
            /// <summary>
            /// The critical width dimenstion "a" used throughout chapter 30
            /// -- minimum of 0.4 * building height and 0.1 * min(building Length, building width)
            /// </summary>
            CritDim_a = Math.Min(0.4 * bldg_data.MeanRoofHeight, 0.1 * Math.Min(bldg_data.BuildingLength, bldg_data.BuildingWidth));
            
            // for finding the inset points
            double inset_dist = 1.414 * CritDim_a;

            // Hip logic
            // Figure 30.3-2E / 2F / 2G / 2H / 2I -- Flat roof and Gable with slope greater than 7
            // Map if Length is less than width -- ridge is vertical on map
            //  E-----F
            //  | \ / |
            //  |  D  |
            //  |  |  |
            //  |  C  |
            //  | / \ |
            //  A=----B 
            if (bldg_data.BuildingLength < bldg_data.BuildingWidth)
            {
                // Corners of the hip roof planes
                Point A = new Point(0, 0);
                Point B = new Point(bldg_data.BuildingLength, 0);
                Point C = new Point(0.5 * bldg_data.BuildingLength, 0.5 * bldg_data.BuildingLength);
                Point D = new Point(0.5 * bldg_data.BuildingLength, bldg_data.BuildingWidth - 0.5 * bldg_data.BuildingLength);
                Point E = new Point(0, bldg_data.BuildingWidth);
                Point F = new Point(bldg_data.BuildingLength, bldg_data.BuildingWidth);

                Point p1 = new Point(CritDim_a, CritDim_a); // lower left corner
                Point p2 = new Point(CritDim_a + inset_dist, CritDim_a); // lower left 1st intermediate
                Point p3 = new Point(bldg_data.BuildingLength - CritDim_a - inset_dist, CritDim_a); // lower right 1st intermediate
                Point p4 = new Point(bldg_data.BuildingLength - CritDim_a, CritDim_a); // lower right
                Point p5 = new Point(C.X, C.Y - inset_dist); // upper corner of lower triangle

                // lower triangle
                effWindAreas_Roof.Add(1, new EffectiveWindArea_Roof("Zone3", new List<Point> { A, B, p4, p1 }, null));
                effWindAreas_Roof.Add(2, new EffectiveWindArea_Roof("Zone2", new List<Point> { p1, p2, p5, p3, p4, C }, null));
                effWindAreas_Roof.Add(3, new EffectiveWindArea_Roof("Zone1", new List<Point> { p2, p3, p5 }, null));

                // left trapezoid
                Point p6 = new Point(p1.X, p1.Y + inset_dist);
                Point p7 = new Point(p1.X, bldg_data.BuildingWidth - CritDim_a - inset_dist);
                Point p8 = new Point(p1.X, bldg_data.BuildingWidth - CritDim_a);

                Point p9 = new Point(C.X - CritDim_a, C.Y + (inset_dist - CritDim_a));
                Point p10 = new Point(D.X - CritDim_a, D.Y - (inset_dist - CritDim_a));

                effWindAreas_Roof.Add(4, new EffectiveWindArea_Roof("Zone3", new List<Point> { A, p1, p8, E }, null));
                effWindAreas_Roof.Add(5, new EffectiveWindArea_Roof("Zone2", new List<Point> { p1, C, D, p8, p7, p10, p9, p6 }, null));
                effWindAreas_Roof.Add(6, new EffectiveWindArea_Roof("Zone1", new List<Point> { p6, p9, p10, p7 }, null));

                Point p20 = new Point(p4.X, p4.Y + inset_dist);
                Point p21 = new Point(p4.X, bldg_data.BuildingWidth - CritDim_a - inset_dist);
                Point p22 = new Point(p4.X, bldg_data.BuildingWidth - CritDim_a);

                Point p23 = new Point(C.X + CritDim_a, C.Y + (inset_dist - CritDim_a));
                Point p24 = new Point(D.X + CritDim_a, D.Y - (inset_dist - CritDim_a));

                // right trapezoid
                effWindAreas_Roof.Add(7, new EffectiveWindArea_Roof("Zone3", new List<Point> { B, F, p22, p4 }, null));
                effWindAreas_Roof.Add(8, new EffectiveWindArea_Roof("Zone2", new List<Point> { p4, p20, p23, p24, p21, p22, D, C }, null));
                effWindAreas_Roof.Add(9, new EffectiveWindArea_Roof("Zone1", new List<Point> { p20, p21, p24, p23 }, null));
                 
                // top triangle
                Point p31 = new Point(D.X, D.Y + inset_dist);
                Point p32 = new Point(p8.X + inset_dist, p8.Y);
                Point p33 = new Point(p22.X - inset_dist, p22.Y);
                effWindAreas_Roof.Add(10, new EffectiveWindArea_Roof("Zone3", new List<Point> { E, p8, p22, F }, null));
                effWindAreas_Roof.Add(11, new EffectiveWindArea_Roof("Zone2", new List<Point> { p8, D, p22, p33, p31, p32, p22 }, null));
                effWindAreas_Roof.Add(12, new EffectiveWindArea_Roof("Zone1", new List<Point> { p32, p31, p33 }, null));
            }
            else if (bldg_data.BuildingLength > bldg_data.BuildingWidth)
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
                Point B = new Point(bldg_data.BuildingLength, 0);
                Point C = new Point(0.5 * bldg_data.BuildingWidth, 0.5 * bldg_data.BuildingWidth);
                Point D = new Point(bldg_data.BuildingLength - 0.5 * bldg_data.BuildingWidth, 0.5 * bldg_data.BuildingWidth);
                Point E = new Point(0, bldg_data.BuildingWidth);
                Point F = new Point(bldg_data.BuildingLength, bldg_data.BuildingWidth);

                // bottom trapezoid
                Point p1 = new Point(0, 0);
                Point p2 = new Point(CritDim_a, CritDim_a);
                Point p3 = new Point(CritDim_a + inset_dist, CritDim_a);

                Point p4 = new Point(bldg_data.BuildingLength - inset_dist - CritDim_a, CritDim_a);
                Point p5 = new Point(bldg_data.BuildingLength - inset_dist, CritDim_a);
                Point p6 = new Point(C.X + inset_dist - CritDim_a, C.Y - CritDim_a);
                Point p7 = new Point(D.X - inset_dist + CritDim_a, D.Y - CritDim_a);

                effWindAreas_Roof.Add(1, new EffectiveWindArea_Roof("Zone3", new List<Point> { A, B, p5, p2 }, null));
                effWindAreas_Roof.Add(2, new EffectiveWindArea_Roof("Zone2", new List<Point> { p2, p3, p6, p7, p4, p5, D, C }, null));
                effWindAreas_Roof.Add(3, new EffectiveWindArea_Roof("Zone1", new List<Point> { p3, p4, p7, p6 }, null));





                //Point p11 = new Point(0, CritDim_a);
                //Point p12 = new Point(CritDim_a, CritDim_a);
                //Point p13 = new Point(bldg_data.BuildingLength - CritDim_a, CritDim_a);
                //Point p14 = new Point(bldg_data.BuildingLength, CritDim_a);

                //Point p21 = new Point(0, bldg_data.BuildingWidth - CritDim_a);
                //Point p22 = new Point(CritDim_a, bldg_data.BuildingWidth - CritDim_a);
                //Point p23 = new Point(bldg_data.BuildingLength - CritDim_a, bldg_data.BuildingWidth - CritDim_a);
                //Point p24 = new Point(bldg_data.BuildingLength, bldg_data.BuildingWidth - CritDim_a);

                //Point p31 = new Point(0, bldg_data.BuildingWidth);
                //Point p32 = new Point(CritDim_a, bldg_data.BuildingWidth);
                //Point p33 = new Point(bldg_data.BuildingLength - CritDim_a, bldg_data.BuildingWidth);
                //Point p34 = new Point(bldg_data.BuildingLength, bldg_data.BuildingWidth);

                //effWindAreas_Roof.Add(1, new EffectiveWindArea_Roof("Zone3", new List<Point> { p1, p2, p12, p11 }, null));
                //effWindAreas_Roof.Add(2, new EffectiveWindArea_Roof("Zone2e", new List<Point> { p2, p3, p13, p12 }, null));
                //effWindAreas_Roof.Add(3, new EffectiveWindArea_Roof("Zone3", new List<Point> { p3, p4, p14, p13 }, null));
                //effWindAreas_Roof.Add(4, new EffectiveWindArea_Roof("Zone2e", new List<Point> { p11, p12, p22, p21 }, null));
                //effWindAreas_Roof.Add(5, new EffectiveWindArea_Roof("Zone2e", new List<Point> { p13, p14, p24, p23 }, null));
                //effWindAreas_Roof.Add(6, new EffectiveWindArea_Roof("Zone3", new List<Point> { p21, p22, p32, p31 }, null));
                //effWindAreas_Roof.Add(7, new EffectiveWindArea_Roof("Zone2e", new List<Point> { p22, p23, p33, p32 }, null));
                //effWindAreas_Roof.Add(8, new EffectiveWindArea_Roof("Zone3", new List<Point> { p23, p24, p34, p33 }, null));

                //// left triangle
                //Point p40 = new Point(p12.X, p12.Y + inset_dist);
                //Point p41 = new Point(C.X - inset_dist, C.Y);
                //Point p42 = new Point(p22.X, p22.Y - inset_dist);
                //effWindAreas_Roof.Add(9, new EffectiveWindArea_Roof("Zone1", new List<Point> { p40, p41, p42 }, null));
                //effWindAreas_Roof.Add(10, new EffectiveWindArea_Roof("Zone2r", new List<Point> { p12, C, p22, p42, p41, p40 }, null));

                //// lower trapezoid
                //Point p50 = new Point(p12.X + inset_dist, p12.Y);
                //Point p51 = new Point(p13.X - inset_dist, p13.Y);
                //Point p52 = new Point(D.X - (inset_dist - CritDim_a), D.Y - CritDim_a);
                //Point p53 = new Point(C.X + (inset_dist - CritDim_a), C.Y - CritDim_a);
                //effWindAreas_Roof.Add(11, new EffectiveWindArea_Roof("Zone1", new List<Point> { p50, p51, p52, p53 }, null));
                //effWindAreas_Roof.Add(12, new EffectiveWindArea_Roof("Zone2r", new List<Point> { p12, p50, p53, p52, p51, p13, D, C }, null));

                //// upper trapezoid
                //Point p60 = new Point(p22.X + inset_dist, p22.Y);
                //Point p61 = new Point(C.X + (inset_dist - CritDim_a), C.Y + CritDim_a);
                //Point p62 = new Point(D.X - (inset_dist - CritDim_a), D.Y + CritDim_a);
                //Point p63 = new Point(p23.X - inset_dist, p23.Y);

                //effWindAreas_Roof.Add(13, new EffectiveWindArea_Roof("Zone1", new List<Point> { p60, p61, p62, p63 }, null));
                //effWindAreas_Roof.Add(14, new EffectiveWindArea_Roof("Zone2r", new List<Point> { p22, C, D, p23, p63, p62, p61, p60 }, null));

                //// right triangle
                //Point p70 = new Point(p13.X, p13.Y + inset_dist);
                //Point p71 = new Point(p23.X, p23.Y - inset_dist);
                //Point p72 = new Point(D.X + inset_dist, D.Y);
                //effWindAreas_Roof.Add(15, new EffectiveWindArea_Roof("Zone1", new List<Point> { p70, p71, p72 }, null));
                //effWindAreas_Roof.Add(16, new EffectiveWindArea_Roof("Zone2r", new List<Point> { p13, p70, p72, p71, p23, D }, null));
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

                //// Corners of the hip roof planes
                //Point A = new Point(0, 0);
                //Point B = new Point(bldg_data.BuildingLength, 0);
                //Point C = new Point(0.5 * bldg_data.BuildingLength, 0.5 * bldg_data.BuildingWidth);
                //Point E = new Point(0, bldg_data.BuildingWidth);
                //Point F = new Point(bldg_data.BuildingLength, bldg_data.BuildingWidth);

                //Point p1 = new Point(0, 0);
                //Point p2 = new Point(CritDim_a, 0);
                //Point p3 = new Point(bldg_data.BuildingLength - CritDim_a, 0);
                //Point p4 = new Point(bldg_data.BuildingLength, 0);

                //Point p11 = new Point(0, CritDim_a);
                //Point p12 = new Point(CritDim_a, CritDim_a);
                //Point p13 = new Point(bldg_data.BuildingLength - CritDim_a, CritDim_a);
                //Point p14 = new Point(bldg_data.BuildingLength, CritDim_a);

                //Point p21 = new Point(0, bldg_data.BuildingWidth - CritDim_a);
                //Point p22 = new Point(CritDim_a, bldg_data.BuildingWidth - CritDim_a);
                //Point p23 = new Point(bldg_data.BuildingLength - CritDim_a, bldg_data.BuildingWidth - CritDim_a);
                //Point p24 = new Point(bldg_data.BuildingLength, bldg_data.BuildingWidth - CritDim_a);

                //Point p31 = new Point(0, bldg_data.BuildingWidth);
                //Point p32 = new Point(CritDim_a, bldg_data.BuildingWidth);
                //Point p33 = new Point(bldg_data.BuildingLength - CritDim_a, bldg_data.BuildingWidth);
                //Point p34 = new Point(bldg_data.BuildingLength, bldg_data.BuildingWidth);

                //effWindAreas_Roof.Add(1, new EffectiveWindArea_Roof("Zone3", new List<Point> { p1, p2, p12, p11 }, null));
                //effWindAreas_Roof.Add(2, new EffectiveWindArea_Roof("Zone2e", new List<Point> { p2, p3, p13, p12 }, null));
                //effWindAreas_Roof.Add(3, new EffectiveWindArea_Roof("Zone3", new List<Point> { p3, p4, p14, p13 }, null));
                //effWindAreas_Roof.Add(4, new EffectiveWindArea_Roof("Zone2e", new List<Point> { p11, p12, p22, p21 }, null));
                //effWindAreas_Roof.Add(5, new EffectiveWindArea_Roof("Zone2e", new List<Point> { p13, p14, p24, p23 }, null));
                //effWindAreas_Roof.Add(6, new EffectiveWindArea_Roof("Zone3", new List<Point> { p21, p22, p32, p31 }, null));
                //effWindAreas_Roof.Add(7, new EffectiveWindArea_Roof("Zone2e", new List<Point> { p22, p23, p33, p32 }, null));
                //effWindAreas_Roof.Add(8, new EffectiveWindArea_Roof("Zone3", new List<Point> { p23, p24, p34, p33 }, null));

                //// for finding the inset points
                //var inset_dist = 1.414 * CritDim_a;

                //// left triangle
                //Point p40 = new Point(p12.X, p12.Y + inset_dist);
                //Point p41 = new Point(C.X - inset_dist, C.Y);
                //Point p42 = new Point(p22.X, p22.Y - inset_dist);
                //effWindAreas_Roof.Add(9, new EffectiveWindArea_Roof("Zone1", new List<Point> { p40, p41, p42 }, null));
                //effWindAreas_Roof.Add(10, new EffectiveWindArea_Roof("Zone2r", new List<Point> { p12, C, p22, p42, p41, p40 }, null));

                //// lower triangle
                //Point p50 = new Point(p12.X + inset_dist, p12.Y);
                //Point p51 = new Point(p13.X - inset_dist, p13.Y);
                //Point p52 = new Point(C.X, C.Y - inset_dist);
                //effWindAreas_Roof.Add(11, new EffectiveWindArea_Roof("Zone1", new List<Point> { p50, p51, p52 }, null));
                //effWindAreas_Roof.Add(12, new EffectiveWindArea_Roof("Zone2r", new List<Point> { p12, p50, p52, p51, p13, C }, null));

                //// upper triangle
                //Point p60 = new Point(p22.X + inset_dist, p22.Y);
                //Point p61 = new Point(C.X, C.Y + inset_dist);
                //Point p62 = new Point(p23.X - inset_dist, p23.Y);

                //effWindAreas_Roof.Add(13, new EffectiveWindArea_Roof("Zone1", new List<Point> { p60, p61, p62 }, null));
                //effWindAreas_Roof.Add(14, new EffectiveWindArea_Roof("Zone2r", new List<Point> { p22, C, p23, p62, p61, p60 }, null));

                //// right triangle
                //Point p70 = new Point(p13.X, p13.Y + inset_dist);
                //Point p71 = new Point(p23.X, p23.Y - inset_dist);
                //Point p72 = new Point(C.X + inset_dist, C.Y);
                //effWindAreas_Roof.Add(15, new EffectiveWindArea_Roof("Zone1", new List<Point> { p70, p71, p72 }, null));
                //effWindAreas_Roof.Add(16, new EffectiveWindArea_Roof("Zone2r", new List<Point> { p13, p70, p72, p71, p23, C }, null));
            }
        }
    }
}
