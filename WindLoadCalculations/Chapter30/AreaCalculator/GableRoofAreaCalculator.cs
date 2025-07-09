using System.Collections.Generic;
using System.Windows;

namespace ShearWallCalculator.WindLoadCalculations
{
    public static class GableRoofAreaCalculator : RoofAreaCalculator_Base
    {
        public static void Compute(WindLoadParameters_Base parameters)
        {
            // Gable logic
            if (parameters.BuildingLength < parameters.BuildingWidth)
            {
                Point p1 = new Point(0, 0);
                Point p2 = new Point(parameters.CritDim_a, 0);
                Point p3 = new Point(0.5 * parameters.BuildingLength - parameters.CritDim_a, 0);
                Point p4 = new Point(0.5 * parameters.BuildingLength, 0);
                Point p5 = new Point(0.5 * parameters.BuildingLength + parameters.CritDim_a, 0);
                Point p6 = new Point(parameters.BuildingLength - parameters.CritDim_a, 0);
                Point p7 = new Point(parameters.BuildingLength, 0);

                Point p11 = new Point(0, parameters.CritDim_a);
                Point p12 = new Point(parameters.CritDim_a, parameters.CritDim_a);
                Point p13 = new Point(0.5 * parameters.BuildingLength - parameters.CritDim_a, parameters.CritDim_a);
                Point p14 = new Point(0.5 * parameters.BuildingLength, parameters.CritDim_a);
                Point p15 = new Point(0.5 * parameters.BuildingLength + parameters.CritDim_a, parameters.CritDim_a);
                Point p16 = new Point(parameters.BuildingLength - parameters.CritDim_a, parameters.CritDim_a);
                Point p17 = new Point(parameters.BuildingLength, parameters.CritDim_a);

                Point p21 = new Point(0, parameters.BuildingWidth - parameters.CritDim_a);
                Point p22 = new Point(parameters.CritDim_a, parameters.BuildingWidth - parameters.CritDim_a);
                Point p23 = new Point(0.5 * parameters.BuildingLength - parameters.CritDim_a, parameters.BuildingWidth - parameters.CritDim_a);
                Point p24 = new Point(0.5 * parameters.BuildingLength, parameters.BuildingWidth - parameters.CritDim_a);
                Point p25 = new Point(0.5 * parameters.BuildingLength + parameters.CritDim_a, parameters.BuildingWidth - parameters.CritDim_a);
                Point p26 = new Point(parameters.BuildingLength - parameters.CritDim_a, parameters.BuildingWidth - parameters.CritDim_a);
                Point p27 = new Point(parameters.BuildingLength, parameters.BuildingWidth - parameters.CritDim_a);

                Point p31 = new Point(0, parameters.BuildingWidth);
                Point p32 = new Point(parameters.CritDim_a, parameters.BuildingWidth);
                Point p33 = new Point(0.5 * parameters.BuildingLength - parameters.CritDim_a, parameters.BuildingWidth);
                Point p34 = new Point(0.5 * parameters.BuildingLength, parameters.BuildingWidth);
                Point p35 = new Point(0.5 * parameters.BuildingLength + parameters.CritDim_a, parameters.BuildingWidth);
                Point p36 = new Point(parameters.BuildingLength - parameters.CritDim_a, parameters.BuildingWidth);
                Point p37 = new Point(parameters.BuildingLength, parameters.BuildingWidth);

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
            else if (parameters.BuildingLength > parameters.BuildingWidth)
            {
                // Figure 30.3-2B / 2C / 2D -- Flat roof and Gable with slope greater than 7

                Point p1 = new Point(0, 0);
                Point p2 = new Point(parameters.CritDim_a, 0);
                Point p3 = new Point(parameters.BuildingLength - parameters.CritDim_a, 0);
                Point p4 = new Point(parameters.BuildingLength, 0);

                Point p11 = new Point(0, parameters.CritDim_a);
                Point p12 = new Point(parameters.CritDim_a, parameters.CritDim_a);
                Point p13 = new Point(parameters.BuildingLength - parameters.CritDim_a, parameters.CritDim_a);
                Point p14 = new Point(parameters.BuildingLength, parameters.CritDim_a);

                Point p21 = new Point(0, 0.5 * parameters.BuildingWidth - parameters.CritDim_a);
                Point p22 = new Point(parameters.CritDim_a, 0.5 * parameters.BuildingWidth - parameters.CritDim_a);
                Point p23 = new Point(parameters.BuildingLength - parameters.CritDim_a, 0.5 * parameters.BuildingWidth - parameters.CritDim_a);
                Point p24 = new Point(parameters.BuildingLength, 0.5 * parameters.BuildingWidth - parameters.CritDim_a);

                Point p31 = new Point(0, 0.5 * parameters.BuildingWidth);
                Point p32 = new Point(parameters.CritDim_a, 0.5 * parameters.BuildingWidth);
                Point p33 = new Point(parameters.BuildingLength - parameters.CritDim_a, 0.5 * parameters.BuildingWidth);
                Point p34 = new Point(parameters.BuildingLength, 0.5 * parameters.BuildingWidth);

                Point p41 = new Point(0, 0.5 * parameters.BuildingWidth + parameters.CritDim_a);
                Point p42 = new Point(parameters.CritDim_a, 0.5 * parameters.BuildingWidth + parameters.CritDim_a);
                Point p43 = new Point(parameters.BuildingLength - parameters.CritDim_a, 0.5 * parameters.BuildingWidth + parameters.CritDim_a);
                Point p44 = new Point(parameters.BuildingLength, 0.5 * parameters.BuildingWidth + parameters.CritDim_a);

                Point p51 = new Point(0, parameters.BuildingWidth - parameters.CritDim_a);
                Point p52 = new Point(parameters.CritDim_a, parameters.BuildingWidth - parameters.CritDim_a);
                Point p53 = new Point(parameters.BuildingLength - parameters.CritDim_a, parameters.BuildingWidth - parameters.CritDim_a);
                Point p54 = new Point(parameters.BuildingLength, parameters.BuildingWidth - parameters.CritDim_a);

                Point p61 = new Point(0, parameters.BuildingWidth);
                Point p62 = new Point(parameters.CritDim_a, parameters.BuildingWidth);
                Point p63 = new Point(parameters.BuildingLength - parameters.CritDim_a, parameters.BuildingWidth);
                Point p64 = new Point(parameters.BuildingLength, parameters.BuildingWidth);


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
                Point p1 = new Point(0, 0);
                Point p2 = new Point(parameters.CritDim_a, 0);
                Point p3 = new Point(parameters.BuildingLength - parameters.CritDim_a, 0);
                Point p4 = new Point(parameters.BuildingLength, 0);

                Point p11 = new Point(0, parameters.CritDim_a);
                Point p12 = new Point(parameters.CritDim_a, parameters.CritDim_a);
                Point p13 = new Point(parameters.BuildingLength - parameters.CritDim_a, parameters.CritDim_a);
                Point p14 = new Point(parameters.BuildingLength, parameters.CritDim_a);

                Point p21 = new Point(0, 0.5 * parameters.BuildingWidth - parameters.CritDim_a);
                Point p22 = new Point(parameters.CritDim_a, 0.5 * parameters.BuildingWidth - parameters.CritDim_a);
                Point p23 = new Point(parameters.BuildingLength - parameters.CritDim_a, 0.5 * parameters.BuildingWidth - parameters.CritDim_a);
                Point p24 = new Point(parameters.BuildingLength, 0.5 * parameters.BuildingWidth - parameters.CritDim_a);

                Point p31 = new Point(0, 0.5 * parameters.BuildingWidth);
                Point p32 = new Point(parameters.CritDim_a, 0.5 * parameters.BuildingWidth);
                Point p33 = new Point(parameters.BuildingLength - parameters.CritDim_a, 0.5 * parameters.BuildingWidth);
                Point p34 = new Point(parameters.BuildingLength, 0.5 * parameters.BuildingWidth);

                Point p41 = new Point(0, 0.5 * parameters.BuildingWidth + parameters.CritDim_a);
                Point p42 = new Point(parameters.CritDim_a, 0.5 * parameters.BuildingWidth + parameters.CritDim_a);
                Point p43 = new Point(parameters.BuildingLength - parameters.CritDim_a, 0.5 * parameters.BuildingWidth + parameters.CritDim_a);
                Point p44 = new Point(parameters.BuildingLength, 0.5 * parameters.BuildingWidth + parameters.CritDim_a);

                Point p51 = new Point(0, parameters.BuildingWidth - parameters.CritDim_a);
                Point p52 = new Point(parameters.CritDim_a, parameters.BuildingWidth - parameters.CritDim_a);
                Point p53 = new Point(parameters.BuildingLength - parameters.CritDim_a, parameters.BuildingWidth - parameters.CritDim_a);
                Point p54 = new Point(parameters.BuildingLength, parameters.BuildingWidth - parameters.CritDim_a);

                Point p61 = new Point(0, parameters.BuildingWidth);
                Point p62 = new Point(parameters.CritDim_a, parameters.BuildingWidth);
                Point p63 = new Point(parameters.BuildingLength - parameters.CritDim_a, parameters.BuildingWidth);
                Point p64 = new Point(parameters.BuildingLength, parameters.BuildingWidth);


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
}
