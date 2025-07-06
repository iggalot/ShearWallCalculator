namespace ShearWallCalculator.WindLoadCalculations.Chapter30
{
    /// <summary>
    /// ASCE7-16 Figure 30-3-2D
    /// Gable Roofs
    /// h <= 60ft
    /// 27deg < slope <= 45deg
    /// </summary>
    public static class Figure30_3_2D
    {
        /// <summary>
        /// Roof
        /// </summary>
        public static ExternalGCpCurve Zone3r_neg_Roof { get; set; } = new ExternalGCpCurve(10, -2.0, 200, -1.0);
        public static ExternalGCpCurve Zone3e_neg_Roof { get; set; } = new ExternalGCpCurve(2, -3.2, 300, -1.0);
        public static ExternalGCpCurve Zone2n_neg_Roof { get; set; } = new ExternalGCpCurve(10, -2.0, 200, -1.0);
        public static ExternalGCpCurve Zone2r_neg_Roof { get; set; } = new ExternalGCpCurve(10, -1.8, 100, -0.8);
        public static ExternalGCpCurve Zone2e_neg_Roof { get; set; } = new ExternalGCpCurve(10, -1.8, 100, -0.8);
        public static ExternalGCpCurve Zone1_neg_Roof { get; set; } = new ExternalGCpCurve(10, -1.8, 100, -0.8);

        public static ExternalGCpCurve Zone3r_pos_Roof { get; set; } = new ExternalGCpCurve(10, 0.9, 100, 0.5);
        public static ExternalGCpCurve Zone3e_pos_Roof { get; set; } = new ExternalGCpCurve(10, 0.9, 100, 0.5);
        public static ExternalGCpCurve Zone2n_pos_Roof { get; set; } = new ExternalGCpCurve(10, 0.9, 100, 0.5);
        public static ExternalGCpCurve Zone2r_pos_Roof { get; set; } = new ExternalGCpCurve(10, 0.9, 100, 0.5);
        public static ExternalGCpCurve Zone2e_pos_Roof { get; set; } = new ExternalGCpCurve(10, 0.9, 100, 0.5);
        public static ExternalGCpCurve Zone1_pos_Roof { get; set; }  = new ExternalGCpCurve(10, 0.9, 100, 0.5);


        /// <summary>
        /// Overhang
        /// </summary>
        public static ExternalGCpCurve Zone3r_Overhang { get; set; } = new ExternalGCpCurve(10, -2.8, 200,-1.8);
        public static ExternalGCpCurve Zone3e_Overhang { get; set; } = new ExternalGCpCurve(2, -4.0, 300,-1.8);
        public static ExternalGCpCurve Zone2n_Overhang { get; set; } = new ExternalGCpCurve(10, -2.8, 200,-1.8);
        public static ExternalGCpCurve Zone2r_Overhang { get; set; } = new ExternalGCpCurve(10, -3.0, 150, -2.2);
        public static ExternalGCpCurve Zone2e_Overhang { get; set; } = new ExternalGCpCurve(10, -2.6, 100, -1.6);
        public static ExternalGCpCurve Zone1_Overhang { get; set; }  = new ExternalGCpCurve(10, -2.6, 100, -1.6);

    }
}
