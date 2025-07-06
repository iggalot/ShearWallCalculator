namespace ShearWallCalculator.WindLoadCalculations.Chapter30
{
    /// <summary>
    /// ASCE7-16 Figure 30-3-2F
    /// Hip Roofs
    /// h/B <= 0.5
    /// h <= 60ft
    /// 7deg < slope <= 20deg
    /// </summary>
    public static class Figure30_3_2F_part2
    {
        /// <summary>
        /// Roof
        /// </summary>
        public static ExternalGCpCurve Zone3_neg_Roof { get; set; } = new ExternalGCpCurve(10, -2.9, 200, -1.6);
        public static ExternalGCpCurve Zone2e_neg_Roof { get; set; } = new ExternalGCpCurve(10, -2.5, 200, -2.1);
        public static ExternalGCpCurve Zone2r_neg_Roof { get; set; } = new ExternalGCpCurve(10, -2.9, 200, -2.3);
        public static ExternalGCpCurve Zone1_neg_Roof { get; set; } = new ExternalGCpCurve(10, -1.8, 100, -2.0);
    }
}
