namespace ShearWallCalculator.WindLoadCalculations.Chapter30
{
    /// <summary>
    /// ASCE7-16 Figure 30-3-2E
    /// Hip Roofs
    /// h/B <= 0.5
    /// h <= 60ft
    /// 7deg < slope <= 20deg
    /// </summary>
    public static class Figure30_3_2E_part2
    {
        /// <summary>
        /// Roof
        /// </summary>
        public static ExternalGCpCurve Zone3_neg_Roof { get; set; } = new ExternalGCpCurve(10, -1.8, 200, -1.1);
        public static ExternalGCpCurve Zone2e_neg_Roof { get; set; } = new ExternalGCpCurve(10, -1.8, 200, -1.1);
        public static ExternalGCpCurve Zone2r_neg_Roof { get; set; } = new ExternalGCpCurve(10, -2.4, 200, -1.3);
        public static ExternalGCpCurve Zone1_neg_Roof { get; set; } = new ExternalGCpCurve(20, -1.3, 100, -1.0);

        public static ExternalGCpCurve Zone3_pos_Roof { get; set; }  = new ExternalGCpCurve(10, 0.7, 100, 0.3);
        public static ExternalGCpCurve Zone2r_pos_Roof { get; set; } = new ExternalGCpCurve(10, 0.7, 100, 0.3);
        public static ExternalGCpCurve Zone2e_pos_Roof { get; set; } = new ExternalGCpCurve(10, 0.7, 100, 0.3);
        public static ExternalGCpCurve Zone1_pos_Roof { get; set; }  = new ExternalGCpCurve(10, 0.7, 100, 0.3);
    }
}
