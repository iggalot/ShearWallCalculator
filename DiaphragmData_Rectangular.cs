using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Principal;

namespace ShearWallCalculator
{
    public enum DiaphragmTypes
    {
        DIAPHRAGM_UNKNOWN = 0,
        DIAPHRAGM_RIGID = 1,
        DIAPHRAGM_FLEXIBLE = 2,
    }
    /// <summary>
    /// Diaphragms are retangular areas defined by four points that contain a mass element in the structure
    /// </summary>
    public class DiaphragmData_Rectangular
    {
        public DiaphragmTypes DiaphragmType = DiaphragmTypes.DIAPHRAGM_RIGID;

        public float Area { get; set; } = 0.0f;


        /// Rectangular region defined by P1, P2, P3, P4
        /// 
        /// P4 --- P3
        /// |       |
        /// P1 --- P2 

        public System.Windows.Point P1 { get; set; } = new System.Windows.Point();
        public System.Windows.Point P2 { get; set; } = new System.Windows.Point();
        public System.Windows.Point P3 { get; set; } = new System.Windows.Point();
        public System.Windows.Point P4 { get; set; } = new System.Windows.Point();


        public System.Windows.Point Centroid { get => ComputeCentroid(); }

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="p1">first point of rectangle</param>
        /// <param name="p2">second point of rectangle</param>
        public DiaphragmData_Rectangular(System.Windows.Point p1, System.Windows.Point p2)
        {
            if (p1 == null || p2 == null)
            {
                return;
            }
            if (p1 == p2)
            {
                throw new System.Exception("Diaphram points must be different");
            }
            if (p1.X == p2.X || p1.Y == p2.Y)
            {
                throw new System.Exception("Unable to draw rectangular region");
            }

            System.Windows.Point _p1, _p2, _p3, _p4;

            /// Sort points so our region is a rectangle defined as
            /// 
            /// P4 --- P3
            /// |       |
            /// P1 --- P2 
            if (p1.X < p2.X)
            {
                if (p1.Y < p2.Y)
                {
                    P1 = p1;
                    P2 = new System.Windows.Point(p2.X, p1.Y);
                    P3 = p2;
                    P4 = new System.Windows.Point(p1.X, p2.Y);
                }
                else
                {
                    P1 = new System.Windows.Point(p1.X, p2.Y);
                    P2 = p2;
                    P4 = new System.Windows.Point(p2.X, p1.Y);
                    P4 = p1;
                }
            }
            else
            {
                if (p1.Y < p2.Y)
                {
                    P1 = new System.Windows.Point(p2.X, p1.Y);
                    P2 = p1;
                    P3 = new System.Windows.Point(p1.X, p2.Y);
                    P4 = p2;
                }
                else
                {
                    P1 = p2;
                    P2 = new System.Windows.Point(p1.X, p2.Y);
                    P3 = p1;
                    P4 = new System.Windows.Point(p2.X, p1.Y);
                }
            }
        }

        public void Update()
        {
            // compute area of region
            ComputeArea();

            // compute centroid of region
            ComputeCentroid();
        }

        /// <summary>
        /// Returns the area for the simple region bounded by the four points of the diaphragm
        /// -- should work for non-rectangular regions
        /// </summary>
        /// <returns></returns>
        private float ComputeArea()
        {
            return (float)(0.5 * ((P1.X*P2.Y - P2.X*P1.Y) + (P2.X*P3.Y - P3.X*P2.Y) + (P3.X*P4.Y - P4.X*P3.Y) + (P4.X*P1.Y - P1.X*P4.Y)));
        }

        /// <summary>
        /// Computes the centroid for the simple region bounded by the four points of the diaphragm
        /// -- should work for non-rectangular regions
        /// </summary>
        /// <returns></returns>
        private System.Windows.Point ComputeCentroid()
        {
            // Make an array of our points
            System.Windows.Point[] DiaphragmPoints = new System.Windows.Point[] { P1, P2, P3, P4 };

            // Generic algorithm for computing a centroid
            int num_pts = DiaphragmPoints.Length;

            // find coords
            float sum_x = 0;
            float sum_y = 0;

            for (int i = 0; i < 4; i++)
            {
                sum_x += (float)DiaphragmPoints[i].X;
                sum_y += (float)DiaphragmPoints[i].Y;
            }
            
            return new System.Windows.Point(sum_x / num_pts, sum_y / num_pts);
        }
    }
}
