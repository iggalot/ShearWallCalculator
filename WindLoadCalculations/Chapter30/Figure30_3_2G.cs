namespace ShearWallCalculator.WindLoadCalculations.Chapter30
{
    /// <summary>
    /// ASCE7-16 Figure 30-3-2G
    /// Hip Roofs
    /// h <= 60ft
    /// 20deg < slope <= 27deg
    /// </summary>
    public class Figure30_3_2G : Chapter30_BaseFigure
    {
        public Figure30_3_2G(double h, double b)
        {
            Zone3_neg_Roof = new ExternalGCpCurve(10, -2.0, 200, -1.0);
            Zone2e_neg_Roof = new ExternalGCpCurve(10, -2.0, 200, -1.0);
            Zone2r_neg_Roof = new ExternalGCpCurve(10, -2.0, 200, -1.0);
            Zone1_neg_Roof = new ExternalGCpCurve(10, -1.4, 100, -0.8);
            /// <summary>
            /// Roof
            /// </summary>
            Zone3_pos_Roof = new ExternalGCpCurve(10, 0.7, 100, 0.3);
            Zone2r_pos_Roof = new ExternalGCpCurve(10, 0.7, 100, 0.3);
            Zone2e_pos_Roof = new ExternalGCpCurve(10, 0.7, 100, 0.3);
            Zone1_pos_Roof = new ExternalGCpCurve(10, 0.7, 100, 0.3);

            /// <summary>
            /// Overhang
            /// </summary>
            Zone3_Overhang = new ExternalGCpCurve(7, -3.1, 200, -1.5);
            Zone2r_Overhang = new ExternalGCpCurve(7, -2.7, 200, -2.0);
            Zone2e_Overhang = new ExternalGCpCurve(7, -2.7, 200, -2.0);
            Zone1_Overhang = new ExternalGCpCurve(10, -2.6, 200, -1.8);
        } 
    }
}
