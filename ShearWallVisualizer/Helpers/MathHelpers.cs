using ShearWallCalculator;
using System;
using System.Windows;
using System.Windows.Shapes;

namespace ShearWallVisualizer.Helpers
{
    public class MathHelpers
    {
        /// <summary>
        /// converts coordinates from the screen (canvas) to the world coordinates
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static Point ScreenCoord_ToWorld(double cnv_height, Point p, double SCALE_X, double SCALE_Y)
        {
            return new Point(p.X / SCALE_X, (cnv_height - p.Y) / SCALE_Y);
        }

        /// <summary>
        /// converts coordinates from the world to the screen coordinates (canvas) system
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static Point WorldCoord_ToScreen(double cnv_height, Point p, double SCALE_X, double SCALE_Y)
        {
            return new Point(p.X * (float)SCALE_X, cnv_height - (p.Y * SCALE_Y));
        }

        #region Helper and Utility Functions


        /// <summary>
        /// Function to determine if a line object is more horizontal than vertical by comparing the x and y distances between the start and end points
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public static bool LineIsHorizontal(Line line)
        {
            if (line == null) return false;

            return (Math.Abs(line.X1 - line.X2) > Math.Abs(line.Y1 - line.Y2));
        }

        public static double DistanceBetweenPoints(Point p1, Point p2)
        {
            double x1 = p1.X;
            double y1 = p1.Y;
            double x2 = p2.X;
            double y2 = p2.Y;
            return Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
        }

        public static bool PointIsWithinRange(Point pt1, Point pt2, float range)
        {
            if (DistanceBetweenPoints(pt1, pt2) <= range)
            {
                return true;
            }
            return false;
        }

        public static Point FindNearestSnapPoint(WallSystem wall_system, DiaphragmSystem diaphragm_system, Point src_pt, out bool result)
        {
            Point pt = new Point(float.MaxValue, float.MaxValue);
            result = false;
            double dist = float.MaxValue;

            bool wall_Result = false;
            bool diaphragm_Result = false;

            Point wall_pt = new Point(0, 0);
            if (wall_system != null)
            {
                wall_pt = FindNearestWallEndPoint(wall_system, src_pt, out wall_Result);
            }

            Point diaphragm_pt = new Point(0, 0);
            if (diaphragm_system != null)
            {
                diaphragm_pt = FindNearestDiaphragmCornerPoint(diaphragm_system, src_pt, out diaphragm_Result);
            }

            if (wall_Result == true && diaphragm_Result == true)
            {
                if (DistanceBetweenPoints(wall_pt, src_pt) <= DistanceBetweenPoints(diaphragm_pt, src_pt))
                {
                    dist = DistanceBetweenPoints(wall_pt, src_pt);
                    pt = wall_pt;
                    result = true;
                }
                else
                {
                    dist = DistanceBetweenPoints(diaphragm_pt, src_pt);
                    pt = diaphragm_pt;
                    result = true;
                }
            }
            else if (wall_Result == true)
            {
                dist = DistanceBetweenPoints(wall_pt, src_pt);
                pt = wall_pt;
                result = true;
            }
            else if (diaphragm_Result == true)
            {
                dist = DistanceBetweenPoints(diaphragm_pt, src_pt);
                pt = diaphragm_pt;
                result = true;
            }
            return pt;
        }

        private static Point FindNearestWallEndPoint(WallSystem wall_system, Point src_pt, out bool result)
        {
            Point pt = new Point(float.MaxValue, float.MaxValue);
            result = false;
            double dist = float.MaxValue;

            if (wall_system == null)
                return new Point(0, 0);

            foreach (var wall in wall_system._walls)
            {
                double p1_dist = DistanceBetweenPoints(wall.Value.Start, src_pt);
                double p2_dist = DistanceBetweenPoints(wall.Value.End, src_pt);

                if (p1_dist <= dist)
                {
                    dist = p1_dist;
                    pt = wall.Value.Start;
                    result = true;
                }
                if (p2_dist <= dist)
                {
                    dist = p2_dist;
                    pt = wall.Value.End;
                    result = true;
                }
            }
            return pt;
        }

        private static Point FindNearestDiaphragmCornerPoint(DiaphragmSystem diaphragm_system, Point src_pt, out bool result)
        {


            Point pt = new Point(0, 0);
            double dist = float.MaxValue;
            result = false;



            foreach (var diaphragm in diaphragm_system._diaphragms)
            {
                double p1_dist = DistanceBetweenPoints(diaphragm.Value.P1, src_pt);
                double p2_dist = DistanceBetweenPoints(diaphragm.Value.P2, src_pt);
                double p3_dist = DistanceBetweenPoints(diaphragm.Value.P3, src_pt);
                double p4_dist = DistanceBetweenPoints(diaphragm.Value.P4, src_pt);

                if (p1_dist <= dist)
                {
                    dist = p1_dist;
                    pt = diaphragm.Value.P1;
                    result = true;
                }
                if (p2_dist <= dist)
                {
                    dist = p2_dist;
                    pt = diaphragm.Value.P2;
                    result = true;

                }
                if (p3_dist <= dist)
                {
                    dist = p3_dist;
                    pt = diaphragm.Value.P3;
                    result = true;
                }
                if (p4_dist <= dist)
                {
                    dist = p4_dist;
                    pt = diaphragm.Value.P4;
                    result = true;
                }
            }

            return pt;
        }
        #endregion
    }
}
