namespace ShearWallCalculator.WindLoadCalculations.Chapter30
{
    /// <summary>
    /// ASCE7-16 Figure 30-3-2C
    /// Gable Roofs
    /// h <= 60ft
    /// 20deg < slope <= 27deg
    /// </summary>
    public class Figure30_3_2C : Chapter30_BaseFigure
    {
        public Figure30_3_2C()
        {
            /// <summary>
            /// Roof
            /// </summary>
            Zone3r_neg_Roof = new ExternalGCpCurve(4, -3.6, 50, -1.8);
            Zone3e_neg_Roof = new ExternalGCpCurve(10, -2.5, 150, -1.2);
            Zone2n_neg_Roof = new ExternalGCpCurve(10, -2.5, 250, -1.0);
            Zone2r_neg_Roof = new ExternalGCpCurve(10, -2.5, 250, -1.0);
            Zone2e_neg_Roof = new ExternalGCpCurve(20, -1.5, 300, -0.8);
            Zone1_neg_Roof = new ExternalGCpCurve(20, -1.5, 300, -0.8);

            Zone3r_pos_Roof = new ExternalGCpCurve(2, 0.7, 100, 0.3);
            Zone3e_pos_Roof = new ExternalGCpCurve(2, 0.7, 100, 0.3);
            Zone2n_pos_Roof = new ExternalGCpCurve(2, 0.7, 100, 0.3);
            Zone2r_pos_Roof = new ExternalGCpCurve(2, 0.7, 100, 0.3);
            Zone2e_pos_Roof = new ExternalGCpCurve(2, 0.7, 100, 0.3);
            Zone1_pos_Roof = new ExternalGCpCurve(2, 0.7, 100, 0.3);

            /// <summary>
            /// Overhang
            /// </summary>
            Zone3r_Overhang = new ExternalGCpCurve(4, -4.7, 50, -2.4);
            Zone3e_Overhang = new ExternalGCpCurve(10, -3.6, 150, -1.7);
            Zone2n_Overhang = new ExternalGCpCurve(10, -3.0, 150, -2.2);
            Zone2r_Overhang = new ExternalGCpCurve(10, -3.0, 150, -2.2);
            Zone2e_Overhang = new ExternalGCpCurve(20, -2.0, 350, -1.8);
            Zone1_Overhang = new ExternalGCpCurve(20, -2.0, 350, -1.8);
        }
    }
}
