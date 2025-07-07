namespace ShearWallCalculator.WindLoadCalculations.Chapter30
{
    /// <summary>
    /// ASCE7-16 Figure 30-3-2E
    /// Hip Roofs
    /// h/B >= 0.8
    /// h <= 60ft
    /// 7deg < slope <= 20deg
    /// </summary>
    public class Figure30_3_2E_2F:Chapter30_BaseFigure
    {
        /// <summary>
        /// ASCE7-16 Figure 30-3-2E (Roof) and 2F (Overhang)
        /// </summary>
        /// <param name="h">mean roof height</param>
        /// <param name="B">width of building -- normal to wind</param>
        public Figure30_3_2E_2F(double h, double B)
        {
            /// <summary>
            /// Roof
            /// </summary>
            if (h / B >= 0.8)
            {
                // Figure30_3_2E
                /// <summary>
                /// Roof
                /// </summary>
                Zone3_neg_Roof = new ExternalGCpCurve(10, -2.6, 200, -1.4);
                Zone2e_neg_Roof = new ExternalGCpCurve(10, -2.6, 200, -1.4);
                Zone2r_neg_Roof = new ExternalGCpCurve(10, -2.4, 200, -1.3);
                Zone1_neg_Roof = new ExternalGCpCurve(20, -1.8, 100, -1.0);

                Zone3_pos_Roof = new ExternalGCpCurve(10, 0.7, 100, 0.3);
                Zone2r_pos_Roof = new ExternalGCpCurve(10, 0.7, 100, 0.3);
                Zone2e_pos_Roof = new ExternalGCpCurve(10, 0.7, 100, 0.3);
                Zone1_pos_Roof = new ExternalGCpCurve(10, 0.7, 100, 0.3);

                // Figur30_3_2F
                /// <summary>
                /// Overhang
                /// </summary>
                Zone3_Overhang = new ExternalGCpCurve(10, -2.0, 200, -1.0);
                Zone2e_Overhang = new ExternalGCpCurve(10, -2.0, 200, -1.0);
                Zone2r_Overhang = new ExternalGCpCurve(10, -2.0, 200, -1.0);
                Zone1_Overhang = new ExternalGCpCurve(10, -1.4, 100, -0.8);


            }
            else if (h / B <= 0.5)
            {
                /// <summary>
                /// Roof
                /// </summary>
                Zone3_neg_Roof = new ExternalGCpCurve(10, -1.8, 200, -1.1);
                Zone2e_neg_Roof = new ExternalGCpCurve(10, -1.8, 200, -1.1);
                Zone2r_neg_Roof = new ExternalGCpCurve(10, -2.4, 200, -1.3);
                Zone1_neg_Roof = new ExternalGCpCurve(20, -1.3, 100, -1.0);

                Zone3_pos_Roof = new ExternalGCpCurve(10, 0.7, 100, 0.3);
                Zone2r_pos_Roof = new ExternalGCpCurve(10, 0.7, 100, 0.3);
                Zone2e_pos_Roof = new ExternalGCpCurve(10, 0.7, 100, 0.3);
                Zone1_pos_Roof = new ExternalGCpCurve(10, 0.7, 100, 0.3);

                /// <summary>
                /// Overhang
                /// </summary>
                Zone3_Overhang = new ExternalGCpCurve(10, 0.7, 100, 0.3);
                Zone2r_Overhang = new ExternalGCpCurve(10, 0.7, 100, 0.3);
                Zone2e_Overhang = new ExternalGCpCurve(10, 0.7, 100, 0.3);
                Zone1_Overhang = new ExternalGCpCurve(10, 0.7, 100, 0.3);
            }
        }
    }
}
