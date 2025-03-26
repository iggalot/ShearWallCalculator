using System;

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
            /// 
            /// P4 --- P3
            /// |       |
            /// P1 --- P2 
            
            // first point is either P1 or P4
            if (first_pt.X < second_pt.X)
            {
                // Cases:
                // (A) first point is P1 and second point is P3
                if (first_pt.Y < second_pt.Y)
                {
                    P1 = first_pt;
                    P3 = second_pt;

                    P2 = new System.Windows.Point(P3.X, P1.Y);
                    P4 = new System.Windows.Point(P1.X, P3.Y);
                }
                // (B) first point is P4 and second point is P2
                else
                {
                    P2 = second_pt;
                    P4 = first_pt;

                    P1 = new System.Windows.Point(P2.X, P4.Y);
                    P3 = new System.Windows.Point(P4.X, P2.Y);
                }
            }

            // first point is either P2 or P3
            else
            {
                // Cases:
                // (A) first point is P2 and second point is P4
                if (first_pt.Y < second_pt.Y)
                {
                    P2 = first_pt;
                    P4 = second_pt;

                    P1 = new System.Windows.Point(P4.X, P2.Y);
                    P3 = new System.Windows.Point(P2.X, P4.Y);
                }
                // (B) first point is P3 and second point is P1
                else
                {
                    P1 = second_pt;
                    P3 = first_pt;

                    P2 = new System.Windows.Point(P3.X, P1.Y);
                    P4 = new System.Windows.Point(P1.X, P3.Y);
                }
            }

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
