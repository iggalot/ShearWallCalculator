namespace ShearWallCalculator.WindLoadCalculations.Chapter30
{
    /// <summary>
    /// ASCE7-22 Figure 30-3-2D
    /// Gable Roofs
    /// h <= 60ft
    /// 27deg < slope <= 45deg
    /// </summary>
    public class Figure30_3_2D_ASCE7_22 : Chapter30_BaseFigure
    {
        public Figure30_3_2D_ASCE7_22()
        {
            ChartTitle = "ASCE 7-22 Figure 30-3-2D - Gable Roofs";
            ChartCriteria = "h <= 60ft, 27deg < slope <= 45deg";

            // ---- Roof: Negative Pressure Zones ----
            RoofCurves_Neg["Zone3"] = new ExternalGCpCurve(10, -2.5, 200, -1.0);
            RoofCurves_Neg["Zone2"] = new ExternalGCpCurve(10, -2.0, 200, -1.0);
            RoofCurves_Neg["Zone1"] = new ExternalGCpCurve(10, -1.8, 100, -0.8);

            // ---- Roof: Positive Pressure Zones ----
            RoofCurves_Pos["Zone3"] = new ExternalGCpCurve(10, 0.9, 200, 0.5);
            RoofCurves_Pos["Zone2"] = new ExternalGCpCurve(10, 0.9, 200, 0.5);
            RoofCurves_Pos["Zone1"] = new ExternalGCpCurve(10, 0.9, 200, 0.5);
        }
    }
}
