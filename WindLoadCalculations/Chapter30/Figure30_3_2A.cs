namespace ShearWallCalculator.WindLoadCalculations.Chapter30
{
    /// <summary>
    /// ASCE 7-16 Figure 30-3-2A
    /// Gable Roofs
    /// h <= 60ft
    /// slope <= 7deg
    /// </summary>
    public static class Figure30_3_2A
    {
        /// <summary>
        /// Roof
        /// </summary>
        public static ExternalGCpCurve Zone3_pos_Roof { get; set; } = new ExternalGCpCurve(10, 0.3, 100, 0.2);
        public static ExternalGCpCurve Zone2_pos_Roof { get; set; } = new ExternalGCpCurve(10, 0.3, 100, 0.2);
        public static ExternalGCpCurve Zone1_pos_Roof { get; set; } = new ExternalGCpCurve(10, 0.3, 100, 0.2);
        public static ExternalGCpCurve Zone1_prime_pos_Roof { get; set; } = new ExternalGCpCurve(10, 0.3, 100, 0.2);
        public static ExternalGCpCurve Zone3_neg_Roof { get; set; } = new ExternalGCpCurve(10, -3.2, 500, -1.4);
        public static ExternalGCpCurve Zone2_neg_Roof { get; set; } = new ExternalGCpCurve(10, -2.4, 500, -1.4);
        public static ExternalGCpCurve Zone1_neg_Roof { get; set; } = new ExternalGCpCurve(10, -1.7, 500, -1.0);
        public static ExternalGCpCurve Zone1_prime_neg_Roof { get; set; } = new ExternalGCpCurve(100, -0.9, 1000, -0.4);

        /// <summary>
        /// Overhang
        /// </summary>
        public static ExternalGCpCurve Zone3_Overhang { get; set; } = new ExternalGCpCurve(10, -3.2, 500, -1.1);
        public static ExternalGCpCurve Zone2_Overhang { get; set; } = new ExternalGCpCurve(10, -2.3, 500, -1.1);
        public static ExternalGCpCurve Zone1_Overhang { get; set; } = new ExternalGCpCurve(10, -1.7, 100, -1.6);
        public static ExternalGCpCurve Zone1_prime_Overhang { get; set; } = new ExternalGCpCurve(10, -1.7, 100, -1.6);
    }
}
