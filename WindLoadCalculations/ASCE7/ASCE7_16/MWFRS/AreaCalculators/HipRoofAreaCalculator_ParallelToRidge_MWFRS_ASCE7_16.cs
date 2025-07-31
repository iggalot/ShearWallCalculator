using ShearWallCalculator.BuildingInfo;
using ShearWallCalculator.WindLoadCalculations.ASCE7.ASCE7_16;
using System;
using System.Collections.Generic;
using System.Windows;

namespace ShearWallCalculator.WindLoadCalculations
{
    public class HipRoofAreaCalculator_ParallelToRidge_MWFRS_ASCE7_16 : AreaCalculator_ASCE7_16_Base
    {
        public HipRoofAreaCalculator_ParallelToRidge_MWFRS_ASCE7_16(BuildingData bldg_data)
        {
            buildingData = bldg_data;
        }

        public override void ComputeEffectiveWindAreas()
        {
            //if (optionalParams == null) return;

            double building_length = buildingData.BuildingLength;
            double building_width = buildingData.BuildingWidth;
            double h = buildingData.MeanRoofHeight;

            // the divisions of the flat roof or parallel to ridge for theta <= 10 deg
            double offset1 = 0.5 * h;
            double offset2 = 1.0 * h;
            double offset3 = 2.0 * h;

            Point A = new Point(0, 0);
            Point B = new Point(building_length, 0);
            Point C = new Point(building_length, building_width);
            Point D = new Point(0, building_width);

            int number_of_zones = WindLoadCalculator_Base.GetNumberRoofZones_MWFRS(buildingData.MeanRoofHeight, buildingData.BuildingLength);

            switch (number_of_zones)
            {
                case 4: ComputeAreas_FourZones(); break;
                case 3: ComputeAreas_ThreeZones(); break;
                case 2: ComputeAreas_TwoZones(); break;
                case 1: ComputeAreas_OneZone(); break;
            }
            //// Define when wind is parallel to ridge (parallel to BuildingLength) dimension
            //// assumes wind is blowing from the left (west) to the right (east)
            //// D---------p21---p22---p23---------C
            //// | \ 4      |    |     |    1    / |
            //// |  \       | 3  |  2  |        /  |
            //// | 5 HPL == p11===p12===p13==HPR 5 |  Ridge "="
            //// |  /       | 3  |  2  |        \  |
            //// | / 4      |    |     |    1    \ |
            //// A---------p1----p2----p3----------B
            //Point A = new Point(0, 0);
            //Point B = new Point(building_length, 0);
            //Point C = new Point(building_length, building_width);
            //Point D = new Point(0, building_width);

            //Point p1, p2, p3, p10, p11, p12, p13, p14, p21, p22, p23, p31, p32;

            //Point hip_point_left;
            //Point hip_point_right;
            //Point hip_point_top;
            //Point hip_point_bottom;

            //double hip_offset = 0.5 * building_width;

            //// windward roof is triangle
            //hip_point_top = new Point(building_length * 0.5, building_width-hip_offset);
            //hip_point_bottom = new Point(building_length * 0.5, hip_offset);
            //hip_point_left = new Point(hip_offset, 0.5 * building_width);
            //hip_point_right = new Point(building_length - hip_offset, 0.5 * building_width);

            //// windward roof is a trapezoid
            //if(hip_point_left.X > hip_point_right.X)
            //{
            //    //// WW roof
            //    //effWindAreas.Add(0, new EffectiveWindArea("Zone5", 
            //    //    new List<Point> { A, hip_point_bottom, hip_point_top, D }, null));
            //    //// LW roof
            //    //effWindAreas.Add(9, new EffectiveWindArea("Zone5", 
            //    //    new List<Point> { B, C, hip_point_top, hip_point_bottom }, null));

            //}
            //// windward and leeward roof is triangle
            //else
            //{
            //    // WW roof
            //    effWindAreas.Add(0, new EffectiveWindArea("Zone5", new List<Point> { A, hip_point_left, D }, null));
            //    // LW roof
            //    effWindAreas.Add(9, new EffectiveWindArea("Zone5", new List<Point> { B, C, hip_point_right }, null));

            //    // Zone 4
            //    if (offset1 > hip_point_left.X)
            //    {
            //        p1 = new Point(offset1, 0);
            //        p11 = new Point(offset1, 0.5 * building_width);
            //        p21 = new Point(offset1, building_width);

            //        // does the zone fit in the building
            //        if (IsValidRectangle_BuildingLength(building_length, building_width, offset1))
            //        {
            //            // yes it fits 
            //            // WW roof 
            //            effWindAreas.Add(1, new EffectiveWindArea("Zone4", new List<Point> { A, p1, p11, hip_point_left }, null));
            //            // LW roof 
            //            effWindAreas.Add(10, new EffectiveWindArea("Zone4", new List<Point> { hip_point_left, p11, p21, C }, null));
            //        } else
            //        {
            //            // no it doesnt fit
            //            // WW roof 
            //            effWindAreas.Add(1, new EffectiveWindArea("Zone4", new List<Point> { A, B, hip_point_right, hip_point_left }, null));
            //            // LW roof 
            //            effWindAreas.Add(10, new EffectiveWindArea("Zone4", new List<Point> { hip_point_left, hip_point_right, C, D }, null));
            //            return;
            //        }
            //    }
            //    // else the ridge point is in zone 3
            //    else if (offset1 < hip_point_left.X && hip_point_left.X < offset2)
            //    {
            //        p1 = new Point(offset1, 0);
            //        p2 = new Point(offset2, 0);
            //        p11 = new Point(offset1, offset1);
            //        p12 = new Point(offset2, 0.5 * building_width);
            //        p21 = new Point(offset1, building_width - offset1);
            //        p31 = new Point(offset1, building_width);
            //        p32 = new Point(offset2, building_width);

            //        effWindAreas.Add(10, new EffectiveWindArea("Zone4", new List<Point> { A, p1, p11 }, null));
            //        effWindAreas.Add(11, new EffectiveWindArea("Zone4", new List<Point> { D, p21, p31 }, null));

            //        effWindAreas.Add(20, new EffectiveWindArea("Zone3", new List<Point> { p1, p2, p12, hip_point_left, p11 }, null));
            //        effWindAreas.Add(21, new EffectiveWindArea("Zone3", new List<Point> { p21, hip_point_left, p12, p32, p31 }, null));

            //        // hip point right is in zone 3
            //        if (offset2 < hip_point_right.X && hip_point_right.X < offset3)
            //        {

            //        } 
            //        // hip point right is in Zone 2
            //        else if (hip_point_right.X > offset3 && hip_point_right.X < building_length)
            //        {

            //        }
            //    }

            //    // else the ridge point is in zone 2
            //    else if (offset2 < hip_point_left.X && hip_point_left.X < offset3)
            //    {
            //        p1 = new Point(offset1, 0);
            //        p2 = new Point(offset2, 0);
            //        p11 = new Point(offset1, offset1);
            //        p12 = new Point(offset2, offset2);
            //        p21 = new Point(offset1, building_width - offset1);
            //        p22 = new Point(offset2, building_width - offset2);
            //        p31 = new Point(offset1, building_width);
            //        p32 = new Point(offset2, building_width);

            //        effWindAreas.Add(10, new EffectiveWindArea("Zone4", new List<Point> { A, p1, p11 }, null));
            //        effWindAreas.Add(11, new EffectiveWindArea("Zone4", new List<Point> { D, p21, p31 }, null));

            //        effWindAreas.Add(20, new EffectiveWindArea("Zone3", new List<Point> { p1, p2, p12, p11 }, null));
            //        effWindAreas.Add(21, new EffectiveWindArea("Zone3", new List<Point> { p21, p22, p32, p31 }, null));

            //    }

            //    // else the ridge point is in zone 1
            //    else if(hip_point_left.X > offset3)
            //    {

            //    }
            //}


        //        // Zone 5
        //        Point p10 = new Point(0.5 * Math.Min(building_length, building_width), 0.5 * building_width);
        //    Point p14 = new Point(building_length - 0.5 * Math.Min(building_length, building_width), 0.5 * building_width);
            
        //    // WW roof 
        //    effWindAreas.Add(0, new EffectiveWindArea("Zone5", new List<Point> { A, p10, D }, null));
        //    // LW roof 
        //    effWindAreas.Add(9, new EffectiveWindArea("Zone5", new List<Point> { B, C, p14 }, null));

        //    // Zone 4
        //    if (IsValidRectangle_BuildingLength(building_length, building_width, offset1))
        //    {
        //        p1 = new Point(offset1, 0);
        //        p11 = new Point(offset1, 0.5 * building_width);
        //        p21 = new Point(offset1, building_width);

        //        effWindAreas.Add(1, new EffectiveWindArea("Zone4", new List<Point> { A, p1, p11, p10 }, null));
        //        effWindAreas.Add(2, new EffectiveWindArea("Zone4", new List<Point> { p10, p11, p21, D }, null));
        //    }
        //    else
        //    {
        //        // doesn't fit in building
        //        effWindAreas.Add(1, new EffectiveWindArea("Zone4", new List<Point> { A, B, p14, p10 }, null));
        //        effWindAreas.Add(2, new EffectiveWindArea("Zone4", new List<Point> { p10, p14, C, D }, null));
        //        return;
        //    }

        //    // Zone 3
        //    if (IsValidRectangle_BuildingLength(building_length, building_width, offset2))
        //    {
        //        p2 = new Point(offset2, 0);
        //        p12 = new Point(offset2, 0.5 * building_width);
        //        p22 = new Point(offset2, building_width);

        //        effWindAreas.Add(3, new EffectiveWindArea("Zone3", new List<Point> { p1, p2, p12, p11 }, null));
        //        effWindAreas.Add(4, new EffectiveWindArea("Zone3", new List<Point> { p11, p12, p22, p21 }, null));
        //    }
        //    else
        //    {
        //        // doesn't fit in building -- extend end of zone to end of building
        //        effWindAreas.Add(3, new EffectiveWindArea("Zone3", new List<Point> { p1, B, p14, p11 }, null));
        //        effWindAreas.Add(4, new EffectiveWindArea("Zone3", new List<Point> { p11, p14, C, p21 }, null));
        //        return;
        //    }

        //    // Zone 2 and Zone 1
        //    if (IsValidRectangle_BuildingLength(building_length, building_width, offset3))
        //    {
        //        p3 = new Point(offset3, 0);
        //        p13 = new Point(offset3, 0.5 * building_width);
        //        p23 = new Point(offset3, building_width);

        //        // Zone 2
        //        effWindAreas.Add(5, new EffectiveWindArea("Zone2", new List<Point> { p2, p3, p13, p12 }, null));
        //        effWindAreas.Add(6, new EffectiveWindArea("Zone2", new List<Point> { p12, p13, p23, p22 }, null));

        //        // Zone 1
        //        var area = new EffectiveWindArea("Zone1", new List<Point> { p3, B, p14, p13 });
        //        effWindAreas.Add(7, new EffectiveWindArea("Zone1", new List<Point> { p3, B, p14, p13 }, null));
        //        effWindAreas.Add(8, new EffectiveWindArea("Zone1", new List<Point> { p13, p14, C, p23 }, null));
        //    }
        //    else
        //    {
        //        // Zone 2
        //        effWindAreas.Add(5, new EffectiveWindArea("Zone2", new List<Point> { p2, B, p14, p12 }, null));
        //        effWindAreas.Add(6, new EffectiveWindArea("Zone2", new List<Point> { p12, p14, C, p22 }, null));
        //        return;
        //    }
        }

        private void ComputeAreas_FourZones()
        {
            throw new NotImplementedException();
        }

        private void ComputeAreas_ThreeZones()
        {
            throw new NotImplementedException();

            //double building_length = buildingData.BuildingLength;
            //double building_width = buildingData.BuildingWidth;
            //double h = buildingData.MeanRoofHeight;
            //double ridge_offset = Math.Min(building_length, building_width) / 2.0;
            //Point left_ridge_pt = new Point(ridge_offset, 0.5 * building_width);
            //Point right_ridge_pt = new Point(building_length - ridge_offset, 0.5 * building_width);

            //int left_ridge_zone = WindLoadCalculator_Base.GetZoneNumber(left_ridge_pt, buildingData.MeanRoofHeight, buildingData.BuildingLength);
            //int right_ridge_zone = WindLoadCalculator_Base.GetZoneNumber(right_ridge_pt, buildingData.MeanRoofHeight, buildingData.BuildingLength);

            //double offset1 = 0.5 * buildingData.MeanRoofHeight;
            //double offset2 = 1.0 * buildingData.MeanRoofHeight;
            //double offset3 = 2.0 * buildingData.MeanRoofHeight;

            //Point A = new Point(0, 0);
            //Point B = new Point(building_length, 0);
            //Point C = new Point(building_length, building_width);
            //Point D = new Point(0, building_width);

            //if (left_ridge_pt.X > right_ridge_pt.X)
            //{
            //    // TODO should we crash here, or switch to a vertical direction?
            //    throw new NotImplementedException("ERROR:  In ComputeAreas_OneZone()Left ridge point cannot be to the right of right ridge point");
            //}

            //// Zone 5
            //effWindAreas.Add(1, new EffectiveWindArea("Zone5", new List<Point> { A, left_ridge_pt, D }, null));
            //effWindAreas.Add(2, new EffectiveWindArea("Zone5", new List<Point> { B, C, right_ridge_pt }, null));

            //if(left_ridge_zone == 4 && right_ridge_zone == 2)
            //{

            //}

            //// Zone 4
            //if (left_ridge_zone == right_ridge_zone)
            //{
            //    Point p1 = new Point(offset1, 0);
            //    Point p11 = new Point(offset1, 0.5 * building_width - (building_length - offset1));
            //    Point p21 = new Point(offset1, building_width - (building_length - offset1));
            //    Point p31 = new Point(offset1, building_width);

            //    var area = new EffectiveWindArea("Zone4", new List<Point> { A, p1, left_ridge_pt }, null);
            //    EffectiveWindArea.RemoveConsecutiveDuplicates(area.OuterBoundary);
            //    effWindAreas.Add(10, area);
            //    area = new EffectiveWindArea("Zone4", new List<Point> { D, left_ridge_pt, p31 }, null);
            //    EffectiveWindArea.RemoveConsecutiveDuplicates(area.OuterBoundary);
            //    effWindAreas.Add(11, area);

            //    if (left_ridge_pt == right_ridge_pt) // triangular region
            //    {


            //        if (p11 == left_ridge_pt)
            //        {

            //        }
            //        else
            //        {
            //            var area = new EffectiveWindArea("Zone4", new List<Point> { A, p1, p11, left_ridge_pt }, null);
            //            EffectiveWindArea.RemoveConsecutiveDuplicates(area.OuterBoundary);
            //            effWindAreas.Add(10, area);
            //            effWindAreas.Add(10, new EffectiveWindArea("Zone4", new List<Point> { A, p1, p11, left_ridge_pt }, null));
            //            effWindAreas.Add(11, new EffectiveWindArea("Zone4", new List<Point> { D, left_ridge_pt, p11, p31 }, null));
            //        }
            //    }
            //    else // trapezoidal region
            //    {
            //        effWindAreas.Add(10, new EffectiveWindArea("Zone4", new List<Point> { A, p1, p11, right_ridge_pt, left_ridge_pt }, null));
            //        effWindAreas.Add(11, new EffectiveWindArea("Zone4", new List<Point> { D, left_ridge_pt, right_ridge_pt, p21, p31 }, null));
            //    }

            //    effWindAreas.Add(20, new EffectiveWindArea("Zone3", new List<Point> { p1, B, p11 }, null));
            //    effWindAreas.Add(21, new EffectiveWindArea("Zone3", new List<Point> { p21, C, p31 }, null));
            //}
            //else
            //{
            //    Point p1 = new Point(offset1, 0);
            //    Point p11 = new Point(offset1, 0.5 * building_width);
            //    Point p31 = new Point(offset1, building_width);

            //    effWindAreas.Add(10, new EffectiveWindArea("Zone4", new List<Point> { A, p1, p11, left_ridge_pt, left_ridge_pt }, null));
            //    effWindAreas.Add(11, new EffectiveWindArea("Zone4", new List<Point> { D, left_ridge_pt, p11, p31 }, null));

            //    effWindAreas.Add(20, new EffectiveWindArea("Zone3", new List<Point> { p1, B, right_ridge_pt, p11 }, null));
            //    effWindAreas.Add(21, new EffectiveWindArea("Zone3", new List<Point> { p11, right_ridge_pt, C, p31 }, null));
            //}
        }

        private void ComputeAreas_TwoZones()
        {
            double building_length = buildingData.BuildingLength;
            double building_width = buildingData.BuildingWidth;
            double h = buildingData.MeanRoofHeight;
            double ridge_offset = Math.Min(building_length, building_width) / 2.0;
            Point left_ridge_pt = new Point(ridge_offset, 0.5 * building_width);
            Point right_ridge_pt = new Point(building_length - ridge_offset, 0.5 * building_width);

            int left_ridge_zone = WindLoadCalculator_Base.GetZoneNumber(left_ridge_pt, buildingData.MeanRoofHeight, buildingData.BuildingLength);
            int right_ridge_zone = WindLoadCalculator_Base.GetZoneNumber(right_ridge_pt, buildingData.MeanRoofHeight, buildingData.BuildingLength);

            double offset1 = 0.5 * buildingData.MeanRoofHeight;
            double offset2 = 1.0 * buildingData.MeanRoofHeight;
            double offset3 = 2.0 * buildingData.MeanRoofHeight;

            Point A = new Point(0, 0);
            Point B = new Point(building_length, 0);
            Point C = new Point(building_length, building_width);
            Point D = new Point(0, building_width);

            if (left_ridge_pt.X > right_ridge_pt.X)
            {
                // TODO should we crash here, or switch to a vertical direction?
                throw new NotImplementedException("ERROR:  In ComputeAreas_OneZone()Left ridge point cannot be to the right of right ridge point");
            }

            // Zone 5
            effWindAreas.Add(1, new EffectiveWindArea("Zone5", new List<Point> { A, left_ridge_pt, D }, null));
            effWindAreas.Add(2, new EffectiveWindArea("Zone5", new List<Point> { B, C, right_ridge_pt }, null));

            // Zone 4
            if (left_ridge_zone == right_ridge_zone)
            {
                Point p1 = new Point(offset1, 0);
                Point p11 = new Point(offset1, 0.5 * building_width - (building_length - offset1));
                Point p21 = new Point(offset1, building_width - (building_length - offset1));
                Point p31 = new Point(offset1, building_width);

                var area = new EffectiveWindArea("Zone4", new List<Point> { A, p1, p11, right_ridge_pt, left_ridge_pt }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area.OuterBoundary);
                effWindAreas.Add(10, area);

                area = new EffectiveWindArea("Zone4", new List<Point> { D, left_ridge_pt, right_ridge_pt, p21, p31 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area.OuterBoundary);
                effWindAreas.Add(11, area);

                effWindAreas.Add(20, new EffectiveWindArea("Zone3", new List<Point> { p1, B, p11 }, null));
                effWindAreas.Add(21, new EffectiveWindArea("Zone3", new List<Point> { p21, C, p31 }, null));
            }
            else
            {
                Point p1 = new Point(offset1, 0);
                Point p11 = new Point(offset1, 0.5 * building_width);
                Point p31 = new Point(offset1, building_width);

                var area = new EffectiveWindArea("Zone4", new List<Point> { A, p1, p11, left_ridge_pt }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area.OuterBoundary);
                effWindAreas.Add(10, area);

                area = new EffectiveWindArea("Zone4", new List<Point> { D, left_ridge_pt, p11, p31 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area.OuterBoundary);
                effWindAreas.Add(11, area);


                area = new EffectiveWindArea("Zone3", new List<Point> { p1, B, right_ridge_pt, p11 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area.OuterBoundary);
                effWindAreas.Add(20, area);

                area = new EffectiveWindArea("Zone3", new List<Point> { p11, right_ridge_pt, C, p31 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area.OuterBoundary);
                effWindAreas.Add(21, area);
            }
        }

        private void ComputeAreas_OneZone()
        {
            double building_length = buildingData.BuildingLength;
            double building_width = buildingData.BuildingWidth;
            double h = buildingData.MeanRoofHeight;
            double ridge_offset = Math.Min(building_length, building_width) / 2.0;
            Point left_ridge_pt = new Point(ridge_offset, 0.5 * building_width);
            Point right_ridge_pt = new Point(building_length - ridge_offset, 0.5 * building_width);

            int left_ridge_zone = WindLoadCalculator_Base.GetZoneNumber(left_ridge_pt, buildingData.MeanRoofHeight, buildingData.BuildingLength);
            int right_ridge_zone = WindLoadCalculator_Base.GetZoneNumber(right_ridge_pt, buildingData.MeanRoofHeight, buildingData.BuildingLength);

            Point A = new Point(0, 0);
            Point B = new Point(building_length, 0);
            Point C = new Point(building_length, building_width);
            Point D = new Point(0, building_width);

            if (left_ridge_pt.X > right_ridge_pt.X)
            {
                // TODO should we crash here, or switch to a vertical direction?
                throw new NotImplementedException("ERROR:  In ComputeAreas_OneZone()Left ridge point cannot be to the right of right ridge point");
            }

            // Zone 5
            effWindAreas.Add(1, new EffectiveWindArea("Zone5", new List<Point> { A, left_ridge_pt, D }, null));
            effWindAreas.Add(2, new EffectiveWindArea("Zone5", new List<Point> { B, C, right_ridge_pt }, null));

            var area = new EffectiveWindArea("Zone4", new List<Point> { A, B, left_ridge_pt }, null);
            EffectiveWindArea.RemoveConsecutiveDuplicates(area.OuterBoundary);
            effWindAreas.Add(10, area);

            area = new EffectiveWindArea("Zone4", new List<Point> { D, left_ridge_pt, right_ridge_pt, C }, null);
            EffectiveWindArea.RemoveConsecutiveDuplicates(area.OuterBoundary);
            effWindAreas.Add(11, area);
        }
    }
}
