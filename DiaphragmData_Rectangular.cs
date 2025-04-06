using System;
using System.Windows;

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

        /// Rectangular region defined by P1, P2, P3, P4
        /// 
        /// P4 --- P3
        /// |       |
        /// P1 --- P2 

        public System.Windows.Point P1 { get; set; } = new System.Windows.Point();
        public System.Windows.Point P2 { get; set; } = new System.Windows.Point();
        public System.Windows.Point P3 { get; set; } = new System.Windows.Point();
        public System.Windows.Point P4 { get; set; } = new System.Windows.Point();


        // Area of the diaphragm
        public float Area { get; set; } = 0.0f;
        
        // Centroid of the diaphragm
        public System.Windows.Point Centroid { get; set; } = new System.Windows.Point();
        
        // Horizontal dimension of the diaphragm
        public float HorizDim_X { get; set; } = 0.0f;
        // Vertical dimension of the diaphragm
        public float HorizDim_Y { get; set; } = 0.0f;

        /// <summary>
        /// Default constructor
        ///              
        /// P4 --- P3
        /// |       |
        /// P1 --- P2 
        /// 
        /// </summary>
        /// <param name="first_pt">first point of rectangle</param>
        /// <param name="second_pt">second point of rectangle</param>
        public DiaphragmData_Rectangular(System.Windows.Point first_pt, System.Windows.Point second_pt)
        {
            if (first_pt == null || second_pt == null)
            {
                return;
            }

            //TODO:: resolve these two exceptions so that they return null -- and then remember to handle this in the functions that created the object.
            if (first_pt == second_pt)
            {
                throw new System.Exception("Diaphram points must be different");
            }
            if (first_pt.X == second_pt.X || first_pt.Y == second_pt.Y)
            {
                throw new System.Exception("Unable to draw rectangular region");
            }

            /// Sort points so our region is a rectangle defined as
            /// P4 --- P3
            /// |       |
            /// P1 --- P2 
            double minX = Math.Min(first_pt.X, second_pt.X);
            double maxX = Math.Max(first_pt.X, second_pt.X);
            double minY = Math.Min(first_pt.Y, second_pt.Y);
            double maxY = Math.Max(first_pt.Y, second_pt.Y);

            P1 = new Point(minX, minY); // Lower Left
            P2 = new Point(maxX, minY); // Lower Right
            P3 = new Point(maxX, maxY); // Upper Right
            P4 = new Point(minX, maxY); // Upper Left

            Update();  // Update the calculations
        }

        public void Update()
        {
            // Compute the dimensions of diaphragm
            HorizDim_X = Math.Abs((float)(P3.X - P1.X));
            HorizDim_Y = Math.Abs((float)(P3.Y - P1.Y));

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
        private void ComputeArea()
        {
            Area = (float)Math.Abs(0.5 * ((P1.X*P2.Y - P2.X*P1.Y) + (P2.X*P3.Y - P3.X*P2.Y) + (P3.X*P4.Y - P4.X*P3.Y) + (P4.X*P1.Y - P1.X*P4.Y)));
            return;
        }

        /// <summary>
        /// Computes the centroid for the simple region bounded by the four points of the diaphragm
        /// - for the rectangular region it should be the midpoint between P1 and P3
        /// </summary>
        /// <returns></returns>
        private void ComputeCentroid()
        {
            float y_c = (float)(0.5 * (P1.Y + P3.Y));
            float x_c = (float)(0.5 * (P1.X + P3.X));
            
            Centroid = new System.Windows.Point(x_c, y_c);
            return;
        }
    }
}
