namespace ShearWallCalculator.WindLoadCalculations.Chapter30
{
    /// <summary>
    /// ASCE7-16 Figure 30-3-2F
    /// Hip Roofs
    /// h/B >= 0.8
    /// h <= 60ft
    /// 7deg < slope <= 20deg
    /// </summary>
    public static class Figure30_3_2F_part1
    {
        /// <summary>
        /// Roof
        /// </summary>
        public static ExternalGCpCurve Zone3_neg_Roof { get; set; } = new ExternalGCpCurve(10, -3.7, 200, -1.9);
        public static ExternalGCpCurve Zone2e_neg_Roof { get; set; } = new ExternalGCpCurve(10, -3.1, 200, -2.4);
        public static ExternalGCpCurve Zone2r_neg_Roof { get; set; } = new ExternalGCpCurve(10, -2.9, 200, -2.3);
        public static ExternalGCpCurve Zone1_neg_Roof { get; set; } = new ExternalGCpCurve(20, -2.3, 100, -2.0);
    }
}
