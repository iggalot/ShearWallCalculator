namespace ShearWallCalculator.WindLoadCalculations.Chapter30
{
    /// <summary>
    /// ASCE7-22 Figure 30-3-2C
    /// Gable Roofs
    /// h <= 60ft
    /// 20deg < slope <= 27deg
    /// </summary>
    public class Figure30_3_2C_ASCE7_22 : Chapter30_BaseFigure
    {
        public Figure30_3_2C_ASCE7_22()
        {
            ChartTitle = "ASCE 7-22 Figure 30-3-2C - Gable Roofs";
            ChartCriteria = "h <= 60ft, 20deg < slope <= 27deg";

            // ---- Roof: Negative Pressure Zones ----
            RoofCurves_Neg["Zone3"] = new ExternalGCpCurve(10, -3.0, 100, -1.4);
            RoofCurves_Neg["Zone2"] = new ExternalGCpCurve(10, -2.5, 100, -1.2);
            RoofCurves_Neg["Zone1"] = new ExternalGCpCurve(10, -1.5, 200, -0.8);

            // ---- Roof: Positive Pressure Zones ----
            RoofCurves_Pos["Zone3"] = new ExternalGCpCurve(2, 0.6, 200, 0.3);
            RoofCurves_Pos["Zone2"] = new ExternalGCpCurve(2, 0.6, 200, 0.3);
            RoofCurves_Pos["Zone1"] = new ExternalGCpCurve(2, 0.6, 200, 0.3);
        }
    }
}
