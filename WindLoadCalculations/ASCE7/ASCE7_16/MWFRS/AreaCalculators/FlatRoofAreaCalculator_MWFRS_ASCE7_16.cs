using ShearWallCalculator.BuildingInfo;
using ShearWallCalculator.WindLoadCalculations.ASCE7.ASCE7_16;
using System;
using System.Collections.Generic;
using System.Windows;

namespace ShearWallCalculator.WindLoadCalculations
{
    public class FlatRoofAreaCalculator_MWFRS_ASCE7_16 : AreaCalculator_ASCE7_16_Base
    {
        public override BuildingData buildingData { get; set; }

        public FlatRoofAreaCalculator_MWFRS_ASCE7_16(BuildingData bldg_data)
        {
            buildingData = bldg_data;
        }

        public static bool IsValidRectangle_BuildingLength(double L, double B, double offset)
        {
            return offset > 0 && offset < L;
        }
        public static bool IsValidRectangle_BuildingWidth(double L, double B, double offset)
        {
            return offset > 0 && offset < B;
        }

        public override void ComputeEffectiveWindAreas(WindParameters_Base parameters, BuildingData bldg_data, bool windIsParallelToRidge = false, Dictionary<string, double> optionalParams = null)
        {
            //if (optionalParams == null) return;

            double building_length = bldg_data.BuildingLength;
            double building_width = bldg_data.BuildingWidth;

            //if (optionalParams.ContainsKey("BuildingLength") && optionalParams.ContainsKey("BuildingWidth"))
            //{
            //    building_length = optionalParams["BuildingLength"];
            //    building_width = optionalParams["BuildingWidth"];
            //}
            //else
            //{
            //    throw new Exception("ERROR: Building length and width are required in FlatRoofAreaCalculator_MWFRS_ASCE7_16 constructor.");
            //}

            double h = bldg_data.MeanRoofHeight;

            var zones = new Dictionary<int, EffectiveWindArea>();



            double offset1 = 0.5 * h;
            double offset2 = 1.0 * h;
            double offset3 = 2.0 * h;


            if (building_length > building_width)
            {
                if (windIsParallelToRidge)
                {
                    // Define when wind is parallel to ridge (parallel to BuildingLength) dimension
                    // D---p21---p22---p23---C
                    // | 4  | 3  |  2   |  1 |
                    // E===p11===p12===p13===F  Ridge "="
                    // | 4  | 3  |  2   |  1 |
                    // A---p1----p2----p3----B
                    Point A = new Point(0, 0);
                    Point B = new Point(buildingData.BuildingLength, 0);
                    Point C = new Point(buildingData.BuildingWidth, buildingData.BuildingLength);
                    Point D = new Point(0, buildingData.BuildingWidth);
                    Point E = new Point(0, 0.5 * buildingData.BuildingWidth);
                    Point F = new Point(buildingData.BuildingLength, 0.5 * buildingData.BuildingWidth);

                    Point p1, p2, p3, p11, p12, p13, p21, p22, p23;

                    // Zone 4
                    if (IsValidRectangle_BuildingLength(building_length, building_width, offset1))
                    {
                        p1 = new Point(offset1, 0);
                        p11 = new Point(offset1, 0.5 * building_width);
                        p21 = new Point(offset1, building_width);

                        effWindAreas.Add(1, new EffectiveWindArea("Zone4", new List<Point> { A, p1, p11, D }, null));
                        effWindAreas.Add(2, new EffectiveWindArea("Zone4", new List<Point> { E, p11, p21, D }, null));
                    }
                    else
                    {
                        // doesn't fit in building
                        effWindAreas.Add(1, new EffectiveWindArea("Zone4", new List<Point> { A, B, F, E }, null));
                        effWindAreas.Add(2, new EffectiveWindArea("Zone4", new List<Point> { E, F, C, D }, null));
                        return;
                    }

                    // Zone 3
                    if (IsValidRectangle_BuildingLength(building_length, building_width, offset2))
                    {
                        p2 = new Point(offset2, 0);
                        p12 = new Point(offset2, 0.5 * building_width);
                        p22 = new Point(offset2, building_width);

                        effWindAreas.Add(3, new EffectiveWindArea("Zone3", new List<Point> { p1, p2, p12, p11 }, null));
                        effWindAreas.Add(4, new EffectiveWindArea("Zone3", new List<Point> { p11, p12, p22, p21 }, null));
                    }
                    else
                    {
                        // doesn't fit in building -- extend end of zone to end of building
                        effWindAreas.Add(3, new EffectiveWindArea("Zone3", new List<Point> { p1, B, F, p11 }, null));
                        effWindAreas.Add(4, new EffectiveWindArea("Zone3", new List<Point> { p11, F, C, p21 }, null));
                        return;
                    }

                    // Zone 2 and Zone 1
                    if (IsValidRectangle_BuildingLength(building_length, building_width, offset3))
                    {
                        p3 = new Point(offset3, 0);
                        p13 = new Point(offset3, 0.5 * building_width);
                        p23 = new Point(offset3, building_width);

                        // Zone 2
                        effWindAreas.Add(5, new EffectiveWindArea("Zone2", new List<Point> { p2, p3, p13, p12 }, null));
                        effWindAreas.Add(6, new EffectiveWindArea("Zone2", new List<Point> { p12, p13, p23, p22 }, null));

                        // Zone 1
                        effWindAreas.Add(7, new EffectiveWindArea("Zone1", new List<Point> { p3, B, F, p13 }, null));
                        effWindAreas.Add(8, new EffectiveWindArea("Zone1", new List<Point> { p13, F, C, p23 }, null));
                    }
                    else
                    {
                        // Zone 2
                        effWindAreas.Add(5, new EffectiveWindArea("Zone2", new List<Point> { p2, B, F, p12 }, null));
                        effWindAreas.Add(6, new EffectiveWindArea("Zone2", new List<Point> { p12, F, C, p22 }, null));
                        return;
                    }
                }
                // Define when wind is perpendicular to ridge (parallel to BuildingWidth) dimension
                // D----------------C
                // |  1             |
                // p21-------------p22
                // |      2b        |   
                // R_L==============R_R    Ridge"="
                // |      2a        |    
                // p11--------------p12   
                // |       3        |    
                // p1---------------p2
                // |       4        |    
                // A----------------B
                //
                // Note: Ridge can be in any of the four zones -- but Flat roofs don't care about the ridge
                else
                {
                    building_length = buildingData.BuildingLength;
                    building_width = buildingData.BuildingWidth;
                    double ridge_offset = 0.5 * building_width;


                    Point A = new Point(0, 0);
                    Point B = new Point(buildingData.BuildingLength, 0);
                    Point C = new Point(buildingData.BuildingWidth, buildingData.BuildingLength);
                    Point D = new Point(0, buildingData.BuildingWidth);
                    Point R_L = new Point(0, 0.5 * buildingData.BuildingWidth);
                    Point R_R = new Point(buildingData.BuildingLength, 0.5 * buildingData.BuildingWidth);

                    Point p1, p2, p11, p12, p21, p22;

                    // Zone 4
                    // First check if zone 4 fits entirely on the roof
                    if (IsValidRectangle_BuildingWidth(building_length, building_width, offset1))
                    {
                        p1 = new Point(0, offset1);
                        p2 = new Point(building_length, offset1);
                        effWindAreas.Add(1, new EffectiveWindArea("Zone4", new List<Point> { A, B, p1, p2 }, null));
                    }
                    else
                    {
                        // doesn't fit in building
                        effWindAreas.Add(1, new EffectiveWindArea("Zone4", new List<Point> { A, B, C, D }, null));
                        return;
                    }

                    // Zone 3
                    if (IsValidRectangle_BuildingWidth(building_length, building_width, offset2))
                    {
                        p11 = new Point(0, offset2);
                        p12 = new Point(building_length, offset2);

                        effWindAreas.Add(2, new EffectiveWindArea("Zone3", new List<Point> { p1, p2, p12, p11 }, null));
                    }
                    else
                    {
                        // doesn't fit in building
                        effWindAreas.Add(2, new EffectiveWindArea("Zone3", new List<Point> { p1, p2, C, D }, null));
                        return;
                    }

                    // Zone 2 and Zone 1
                    if (IsValidRectangle_BuildingLength(building_length, building_width, offset3))
                    {
                        p21 = new Point(0, offset3);
                        p22 = new Point(building_length, offset3);

                        effWindAreas.Add(3, new EffectiveWindArea("Zone2", new List<Point> { p11, p12, p22, p21 }, null));
                        effWindAreas.Add(4, new EffectiveWindArea("Zone1", new List<Point> { p21, p22, C, D }, null));

                    }
                    else
                    {
                        // doesn't fit in building
                        effWindAreas.Add(3, new EffectiveWindArea("Zone2", new List<Point> { p11, p12, C, D }, null));
                        return;
                    }
                }
            }


            // Defined when building width is larger than building length
            else if (building_width > building_length)
            {
                if (windIsParallelToRidge)
                {
                    // Define when wind is parallel to ridge (parallel to BuildingLength) dimension
                    // D---------------R2--------------C
                    // |        1      ||       1      |
                    // p21------------p22--------------p23
                    // |        2      ||       2      |
                    // p11------------p12--------------p13
                    // |        3       ||      3      |
                    // p1--------------p2--------------p3
                    // |        4       ||      4      |
                    // A---------------R1--------------B
                    //  
                    //  Ridge = "="
                    Point A = new Point(0, 0);
                    Point B = new Point(buildingData.BuildingLength, 0);
                    Point C = new Point(buildingData.BuildingWidth, buildingData.BuildingLength);
                    Point D = new Point(0, buildingData.BuildingWidth);
                    Point R1 = new Point(0.5 * building_length, 0);
                    Point R2 = new Point(0.5 * building_length, building_width);

                    double ridge_offset = 0.5 * building_length;

                    Point p1, p2, p3, p11, p12, p13, p21, p22, p23;

                    // Zone 4
                    // is Zone4 fully in the building?
                    if (IsValidRectangle_BuildingWidth(building_length, building_width, offset1))
                    {
                        // yes
                        p1 = new Point(0, offset1);
                        p2 = new Point(ridge_offset, offset1);
                        p3 = new Point(building_length, offset1);

                        p11 = new Point(ridge_offset, offset1);
                        p12 = new Point(ridge_offset, offset1);
                        p13 = new Point(building_length, offset1);

                        effWindAreas.Add(1, new EffectiveWindArea("Zone4", new List<Point> { A, R1, p2, p1 }, null));
                        effWindAreas.Add(2, new EffectiveWindArea("Zone4", new List<Point> { R1, B, p3, p2 }, null));
                    }
                    else
                    {
                        // doesn't fit in building
                        effWindAreas.Add(1, new EffectiveWindArea("Zone4", new List<Point> { A, R1, R2, D }, null));
                        effWindAreas.Add(2, new EffectiveWindArea("Zone4", new List<Point> { R1, B, C, R2 }, null));
                        return;
                    }

                    // Zone 3
                    if (IsValidRectangle_BuildingWidth(building_length, building_width, offset2))
                    {
                        p11 = new Point(0, offset2);
                        p12 = new Point(ridge_offset, offset2);
                        p13 = new Point(building_length, offset2);

                        effWindAreas.Add(3, new EffectiveWindArea("Zone3", new List<Point> { p1, p2, p12, p11 }, null));
                        effWindAreas.Add(4, new EffectiveWindArea("Zone3", new List<Point> { p2, p3, p13, p12 }, null));
                    }
                    else
                    {
                        // doesn't fit in building -- extend end of zone to end of building
                        effWindAreas.Add(3, new EffectiveWindArea("Zone3", new List<Point> { p1, p2, R2, D }, null));
                        effWindAreas.Add(4, new EffectiveWindArea("Zone3", new List<Point> { p2, p3, C, R2 }, null));
                        return;
                    }

                    // Zone 2 and Zone 1
                    if (IsValidRectangle_BuildingWidth(building_length, building_width, offset3))
                    {
                        p21 = new Point(0, offset3);
                        p22 = new Point(ridge_offset, offset3);
                        p23 = new Point(building_length, offset3);

                        // Zone 2
                        effWindAreas.Add(5, new EffectiveWindArea("Zone2", new List<Point> { p11, p12, p22, p21 }, null));
                        effWindAreas.Add(6, new EffectiveWindArea("Zone1", new List<Point> { p12, p13, p23, p22 }, null));

                        // Zone 1
                        effWindAreas.Add(7, new EffectiveWindArea("Zone2", new List<Point> { p21, p22, R2, D }, null));
                        effWindAreas.Add(8, new EffectiveWindArea("Zone1", new List<Point> { p22, p23, C, R2 }, null));
                    }
                    else
                    {
                        // Zone 2
                        effWindAreas.Add(5, new EffectiveWindArea("Zone2", new List<Point> { p2, p3, p13, p12 }, null));
                        effWindAreas.Add(6, new EffectiveWindArea("Zone1", new List<Point> { p3, B, C, p13 }, null));
                        return;
                    }
                }

                else
                {
                    
                    // Define when wind is parallel to ridge (parallel to BuildingLength) dimension
                    // D---p11---p12-- R1-----p13----C
                    // |    |    |     ||      |     |
                    // |    |    |     ||      |     |
                    // |    |    |     ||      |     |
                    // | 4  | 3  |  2a ||  2b  |  1  |
                    // A---p1----p2---R2------p2-----B
                    //  
                    //  Ridge = "="
                    Point A = new Point(0, 0);
                    Point B = new Point(buildingData.BuildingLength, 0);
                    Point C = new Point(buildingData.BuildingWidth, buildingData.BuildingLength);
                    Point D = new Point(0, buildingData.BuildingWidth);
                    Point R1 = new Point(0.5 * building_length, 0);
                    Point R2 = new Point(0.5 * building_length, building_width);

                    double ridge_offset = 0.5 * building_length;

                    Point p1, p2, p3, p11, p12, p13, p21, p22, p23;

                    // Zone 4
                    // is Zone4 fully in the building?
                    if (IsValidRectangle_BuildingLength(building_length, building_width, offset1))
                    {
                        // yes
                        p1 = new Point(offset1, 0);
                        p11 = new Point(offset1, building_width);

                        effWindAreas.Add(1, new EffectiveWindArea("Zone4", new List<Point> { A, p1, p11, D }, null));
                    }
                    else
                    {
                        // doesn't fit in building
                        effWindAreas.Add(1, new EffectiveWindArea("Zone4", new List<Point> { A, B, C, D }, null));
                        return;
                    }

                    // Zone 3
                    if (IsValidRectangle_BuildingLength(building_length, building_width, offset2))
                    {
                        p2 = new Point(offset2, 0);
                        p12 = new Point(offset2, building_width);

                        effWindAreas.Add(2, new EffectiveWindArea("Zone3", new List<Point> { p1, p2, p12, p11 }, null));
                    }
                    else
                    {
                        // doesn't fit in building -- extend end of zone to end of building
                        effWindAreas.Add(2, new EffectiveWindArea("Zone3", new List<Point> { p1, B, C, p11 }, null));
                        return;
                    }

                    // Zone 2 and Zone 1
                    if (IsValidRectangle_BuildingLength(building_length, building_width, offset3))
                    {
                        p3 = new Point(offset3, 0);
                        p13 = new Point(offset3, building_width);

                        // Zone 2
                        effWindAreas.Add(5, new EffectiveWindArea("Zone2", new List<Point> { p2, p3, p13, p12 }, null));
                        effWindAreas.Add(6, new EffectiveWindArea("Zone1", new List<Point> { p3, B, C, p13 }, null));
                    }
                    else
                    {
                        // Zone 2
                        effWindAreas.Add(5, new EffectiveWindArea("Zone2", new List<Point> { p2, B, C, p12 }, null));
                        return;
                    }
                }
            }
        }
    }
}
