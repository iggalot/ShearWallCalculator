namespace ShearWallCalculator.WindLoadCalculations.Chapter30
{
    /// <summary>
    /// ASCE 7-22 Figure 30-3-2A
    /// Gable Roofs
    /// h <= 60ft
    /// slope <= 7deg
    /// </summary>
    public class Figure30_3_1_ASCE7_22 : Chapter30_BaseFigure
    {
        public Figure30_3_1_ASCE7_22()
        {
            ChartTitle = "ASCE 7-22 Figure 30-3-1";
            ChartCriteria = "h <= 60ft, Walls";

            // Positive Pressure Wall Zones
            WallCurves_Pos["Zone5"] = new ExternalGCpCurve(10, 1.0, 500, 0.7);
            WallCurves_Pos["Zone4"] = new ExternalGCpCurve(10, 1.0, 500, 0.7);

            // Negative Pressure Wall Zones
            WallCurves_Neg["Zone5"] = new ExternalGCpCurve(10, -1.4, 500, -0.8);
            WallCurves_Neg["Zone4"] = new ExternalGCpCurve(10, -1.1, 500, -0.8);
        }
    }
}
