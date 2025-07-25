using ShearWallCalculator.BuildingInfo;
using ShearWallCalculator.WindLoadCalculations.ASCE7.ASCE7_16.CC.AreaCalculators;
using System.Collections.Generic;
using System.Windows;

namespace ShearWallCalculator.WindLoadCalculations
{
    public class GableRoofAreaCalculator_CC_ASCE7_22 : AreaCalculator_CC_ASCE7_22_Base
    {
        public override BuildingData buildingData { get; set; }
        public GableRoofAreaCalculator_CC_ASCE7_22(BuildingData bldg_data)
        {
            buildingData = bldg_data;
        }

        public override void ComputeEffectiveWindAreas(WindParameters_Base parameters, BuildingData bldg_data, bool windIsParallelToRidge = false, Dictionary<string, double> optionalParams = null)
        {
            // Gable logic
            if (bldg_data.BuildingLength < bldg_data.BuildingWidth)
            {
                Point p1 = new Point(0, 0);
                Point p3 = new Point(0.5 * bldg_data.BuildingLength - CritDim_a, 0);
                Point p4 = new Point(0.5 * bldg_data.BuildingLength, 0);
                Point p5 = new Point(0.5 * bldg_data.BuildingLength + CritDim_a, 0);
                Point p7 = new Point(bldg_data.BuildingLength, 0);

                Point p11 = new Point(0, CritDim_a);
                Point p13 = new Point(0.5 * bldg_data.BuildingLength - CritDim_a, CritDim_a);
                Point p14 = new Point(0.5 * bldg_data.BuildingLength, CritDim_a);
                Point p15 = new Point(0.5 * bldg_data.BuildingLength + CritDim_a, CritDim_a);
                Point p17 = new Point(bldg_data.BuildingLength, CritDim_a);

                Point p21 = new Point(0, bldg_data.BuildingWidth - CritDim_a);
                Point p23 = new Point(0.5 * bldg_data.BuildingLength - CritDim_a, bldg_data.BuildingWidth - CritDim_a);
                Point p24 = new Point(0.5 * bldg_data.BuildingLength, bldg_data.BuildingWidth - CritDim_a);
                Point p25 = new Point(0.5 * bldg_data.BuildingLength + CritDim_a, bldg_data.BuildingWidth - CritDim_a);
                Point p27 = new Point(bldg_data.BuildingLength, bldg_data.BuildingWidth - CritDim_a);

                Point p31 = new Point(0, bldg_data.BuildingWidth);
                Point p33 = new Point(0.5 * bldg_data.BuildingLength - CritDim_a, bldg_data.BuildingWidth);
                Point p34 = new Point(0.5 * bldg_data.BuildingLength, bldg_data.BuildingWidth);
                Point p35 = new Point(0.5 * bldg_data.BuildingLength + CritDim_a, bldg_data.BuildingWidth);
                Point p37 = new Point(bldg_data.BuildingLength, bldg_data.BuildingWidth);

                // Bottom row of rectangles
                effWindAreas.Add(2, new EffectiveWindArea("Zone2", new List<Point> { p1, p3, p13, p11 }, null));
                effWindAreas.Add(3, new EffectiveWindArea("Zone3", new List<Point> { p3, p4, p14, p13 }, null));
                effWindAreas.Add(4, new EffectiveWindArea("Zone3", new List<Point> { p4, p5, p15, p14 }, null));
                effWindAreas.Add(5, new EffectiveWindArea("Zone2", new List<Point> { p5, p7, p17, p15 }, null));

                // Middle row of rectangles
                effWindAreas.Add(8, new EffectiveWindArea("Zone1", new List<Point> { p11, p13, p23, p21 }, null));
                effWindAreas.Add(9, new EffectiveWindArea("Zone2", new List<Point> { p13, p14, p24, p23 }, null));
                effWindAreas.Add(10, new EffectiveWindArea("Zone2", new List<Point> { p14, p15, p25, p24 }, null));
                effWindAreas.Add(11, new EffectiveWindArea("Zone1", new List<Point> { p15, p17, p27, p25 }, null));

                // Top row of rectangles
                effWindAreas.Add(14, new EffectiveWindArea("Zone2", new List<Point> { p21, p23, p33, p31 }, null));
                effWindAreas.Add(15, new EffectiveWindArea("Zone3", new List<Point> { p23, p24, p34, p33 }, null));
                effWindAreas.Add(16, new EffectiveWindArea("Zone3", new List<Point> { p24, p25, p35, p34 }, null));
                effWindAreas.Add(17, new EffectiveWindArea("Zone2", new List<Point> { p25, p27, p37, p35 }, null));

                return;
            }
            else if (bldg_data.BuildingLength > bldg_data.BuildingWidth)
            {
                // Figure 30.3-2B / 2C / 2D -- Flat roof and Gable with slope greater than 7

                Point p1 = new Point(0, 0);
                Point p2 = new Point(CritDim_a, 0);
                Point p3 = new Point(bldg_data.BuildingLength - CritDim_a, 0);
                Point p4 = new Point(bldg_data.BuildingLength, 0);

                Point p21 = new Point(0, 0.5 * bldg_data.BuildingWidth - CritDim_a);
                Point p22 = new Point(CritDim_a, 0.5 * bldg_data.BuildingWidth - CritDim_a);
                Point p23 = new Point(bldg_data.BuildingLength - CritDim_a, 0.5 * bldg_data.BuildingWidth - CritDim_a);
                Point p24 = new Point(bldg_data.BuildingLength, 0.5 * bldg_data.BuildingWidth - CritDim_a);

                Point p31 = new Point(0, 0.5 * bldg_data.BuildingWidth);
                Point p32 = new Point(CritDim_a, 0.5 * bldg_data.BuildingWidth);
                Point p33 = new Point(bldg_data.BuildingLength - CritDim_a, 0.5 * bldg_data.BuildingWidth);
                Point p34 = new Point(bldg_data.BuildingLength, 0.5 * bldg_data.BuildingWidth);

                Point p41 = new Point(0, 0.5 * bldg_data.BuildingWidth + CritDim_a);
                Point p42 = new Point(CritDim_a, 0.5 * bldg_data.BuildingWidth + CritDim_a);
                Point p43 = new Point(bldg_data.BuildingLength - CritDim_a, 0.5 * bldg_data.BuildingWidth + CritDim_a);
                Point p44 = new Point(bldg_data.BuildingLength, 0.5 * bldg_data.BuildingWidth + CritDim_a);

                Point p51 = new Point(0, bldg_data.BuildingWidth);
                Point p52 = new Point(CritDim_a, bldg_data.BuildingWidth);
                Point p53 = new Point(bldg_data.BuildingLength - CritDim_a, bldg_data.BuildingWidth);
                Point p54 = new Point(bldg_data.BuildingLength, bldg_data.BuildingWidth);

                // Bottom row of rectangles
                effWindAreas.Add(1, new EffectiveWindArea("Zone2", new List<Point> { p1, p2, p22, p21 }, null));
                effWindAreas.Add(2, new EffectiveWindArea("Zone1", new List<Point> { p2, p3, p23, p22 }, null));
                effWindAreas.Add(3, new EffectiveWindArea("Zone2", new List<Point> { p3, p4, p24, p23 }, null));

                //// Top row of rectangles
                effWindAreas.Add(7, new EffectiveWindArea("Zone3", new List<Point> { p21, p22, p32, p31 }, null));
                effWindAreas.Add(8, new EffectiveWindArea("Zone2", new List<Point> { p22, p23, p33, p32 }, null));
                effWindAreas.Add(9, new EffectiveWindArea("Zone3", new List<Point> { p23, p24, p34, p33 }, null));

                // ------------------ RIDGE ---------------- //

                // Bottom row of rectangles
                effWindAreas.Add(10, new EffectiveWindArea("Zone3", new List<Point> { p31, p32, p42, p41 }, null));
                effWindAreas.Add(11, new EffectiveWindArea("Zone2", new List<Point> { p32, p33, p43, p42 }, null));
                effWindAreas.Add(12, new EffectiveWindArea("Zone3", new List<Point> { p33, p34, p44, p43 }, null));

                // Middle row of rectangles
                effWindAreas.Add(13, new EffectiveWindArea("Zone2", new List<Point> { p41, p42, p52, p51 }, null));
                effWindAreas.Add(14, new EffectiveWindArea("Zone1", new List<Point> { p42, p43, p53, p52 }, null));
                effWindAreas.Add(15, new EffectiveWindArea("Zone2", new List<Point> { p43, p44, p54, p53 }, null));

                // Top row of rectangles

                return;
            }

            // Building length and width are equal, so we will choose the vertical direction as the ridge direction
            // (this is an abitrary selection)
            else
            {
                Point p1 = new Point(0, 0);
                Point p3 = new Point(0.5 * bldg_data.BuildingLength - CritDim_a, 0);
                Point p4 = new Point(0.5 * bldg_data.BuildingLength, 0);
                Point p5 = new Point(0.5 * bldg_data.BuildingLength + CritDim_a, 0);
                Point p7 = new Point(bldg_data.BuildingLength, 0);

                Point p11 = new Point(0, CritDim_a);
                Point p13 = new Point(0.5 * bldg_data.BuildingLength - CritDim_a, CritDim_a);
                Point p14 = new Point(0.5 * bldg_data.BuildingLength, CritDim_a);
                Point p15 = new Point(0.5 * bldg_data.BuildingLength + CritDim_a, CritDim_a);
                Point p17 = new Point(bldg_data.BuildingLength, CritDim_a);

                Point p21 = new Point(0, bldg_data.BuildingWidth - CritDim_a);
                Point p23 = new Point(0.5 * bldg_data.BuildingLength - CritDim_a, bldg_data.BuildingWidth - CritDim_a);
                Point p24 = new Point(0.5 * bldg_data.BuildingLength, bldg_data.BuildingWidth - CritDim_a);
                Point p25 = new Point(0.5 * bldg_data.BuildingLength + CritDim_a, bldg_data.BuildingWidth - CritDim_a);
                Point p27 = new Point(bldg_data.BuildingLength, bldg_data.BuildingWidth - CritDim_a);

                Point p31 = new Point(0, bldg_data.BuildingWidth);
                Point p33 = new Point(0.5 * bldg_data.BuildingLength - CritDim_a, bldg_data.BuildingWidth);
                Point p34 = new Point(0.5 * bldg_data.BuildingLength, bldg_data.BuildingWidth);
                Point p35 = new Point(0.5 * bldg_data.BuildingLength + CritDim_a, bldg_data.BuildingWidth);
                Point p37 = new Point(bldg_data.BuildingLength, bldg_data.BuildingWidth);

                // Bottom row of rectangles
                effWindAreas.Add(2, new EffectiveWindArea("Zone2", new List<Point> { p1, p3, p13, p11 }, null));
                effWindAreas.Add(3, new EffectiveWindArea("Zone3", new List<Point> { p3, p4, p14, p13 }, null));
                effWindAreas.Add(4, new EffectiveWindArea("Zone3", new List<Point> { p4, p5, p15, p14 }, null));
                effWindAreas.Add(5, new EffectiveWindArea("Zone2", new List<Point> { p5, p7, p17, p15 }, null));

                // Middle row of rectangles
                effWindAreas.Add(8, new EffectiveWindArea("Zone1", new List<Point> { p11, p13, p23, p21 }, null));
                effWindAreas.Add(9, new EffectiveWindArea("Zone2", new List<Point> { p13, p14, p24, p23 }, null));
                effWindAreas.Add(10, new EffectiveWindArea("Zone2", new List<Point> { p14, p15, p25, p24 }, null));
                effWindAreas.Add(11, new EffectiveWindArea("Zone1", new List<Point> { p15, p17, p27, p25 }, null));

                // Top row of rectangles
                effWindAreas.Add(14, new EffectiveWindArea("Zone2", new List<Point> { p21, p23, p33, p31 }, null));
                effWindAreas.Add(15, new EffectiveWindArea("Zone3", new List<Point> { p23, p24, p34, p33 }, null));
                effWindAreas.Add(16, new EffectiveWindArea("Zone3", new List<Point> { p24, p25, p35, p34 }, null));
                effWindAreas.Add(17, new EffectiveWindArea("Zone2", new List<Point> { p25, p27, p37, p35 }, null));
                return;
            }
        }
    }
}
