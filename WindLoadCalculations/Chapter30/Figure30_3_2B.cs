namespace ShearWallCalculator.WindLoadCalculations.Chapter30
{
    /// <summary>
    /// ASCE7-16 Figure 30-3-2B
    /// Gable Roofs
    /// h <= 60ft
    /// 7deg < slope <= 20deg
    /// </summary>
    public static class Figure30_3_2B
    {
        /// <summary>
        /// Roof
        /// </summary>
        public static ExternalGCpCurve Zone3r_neg_Roof { get; set; } = new ExternalGCpCurve(10, -3.6, 100, -1.8);
        public static ExternalGCpCurve Zone3e_neg_Roof { get; set; } = new ExternalGCpCurve(10, -3.0, 250, -1.0);
        public static ExternalGCpCurve Zone2n_neg_Roof { get; set; } = new ExternalGCpCurve(10, -3.0, 250, -1.0);
        public static ExternalGCpCurve Zone2r_neg_Roof { get; set; } = new ExternalGCpCurve(10, -3.0, 250, -1.0);
        public static ExternalGCpCurve Zone2e_neg_Roof { get; set; } = new ExternalGCpCurve(20, -2.0, 100, -0.5);
        public static ExternalGCpCurve  Zone1_neg_Roof { get; set; } = new ExternalGCpCurve(20, -2.0, 100, -0.5);

        public static ExternalGCpCurve Zone3r_pos_Roof { get; set; } = new ExternalGCpCurve(2, 0.7, 100, 0.3);
        public static ExternalGCpCurve Zone3e_pos_Roof { get; set; } = new ExternalGCpCurve(2, 0.7, 100, 0.3);
        public static ExternalGCpCurve Zone2n_pos_Roof { get; set; } = new ExternalGCpCurve(2, 0.7, 100, 0.3);
        public static ExternalGCpCurve Zone2r_pos_Roof { get; set; } = new ExternalGCpCurve(2, 0.7, 100, 0.3);
        public static ExternalGCpCurve Zone2e_pos_Roof { get; set; } = new ExternalGCpCurve(2, 0.7, 100, 0.3);
        public static ExternalGCpCurve  Zone1_pos_Roof { get; set; } = new ExternalGCpCurve(2, 0.7, 100, 0.3);


        /// <summary>
        /// Overhang
        /// </summary>
        public static ExternalGCpCurve Zone3r_Overhang { get; set; } = new ExternalGCpCurve(10, -4.7, 100, -2.3);
        public static ExternalGCpCurve Zone3e_Overhang { get; set; } = new ExternalGCpCurve(10, -4.1, 250, -1.5);
        public static ExternalGCpCurve Zone2n_Overhang { get; set; } = new ExternalGCpCurve(10, -3.5, 250, -2.0);
        public static ExternalGCpCurve Zone2r_Overhang { get; set; } = new ExternalGCpCurve(10, -3.5, 250, -2.0);
        public static ExternalGCpCurve Zone2e_Overhang { get; set; } = new ExternalGCpCurve(20, -2.5, 100, -1.5);
        public static ExternalGCpCurve  Zone1_Overhang { get; set; } = new ExternalGCpCurve(10, -2.5, 100, -1.5);

    }
}
