namespace ShearWallCalculator.WindLoadCalculations.Chapter30
{
    /// <summary>
    /// ASCE7-22 Figure 30-3-2B
    /// Gable Roofs
    /// h <= 60ft
    /// 7deg < slope <= 20deg
    /// </summary>
    public class Figure30_3_2B_ASCE7_22 : Chapter30_BaseFigure
    {
        public Figure30_3_2B_ASCE7_22()
        {
            ChartTitle = "ASCE 7-22 Figure 30-3-2B - Gable Roofs";
            ChartCriteria = "h <= 60ft, 7deg < slope <= 20deg";

            // ---- Roof: Negative Pressure Zones ----
            RoofCurves_Neg["Zone3"] = new ExternalGCpCurve(10, -3.6, 100, -1.8);
            RoofCurves_Neg["Zone2"] = new ExternalGCpCurve(10, -2.7, 200, -1.0);
            RoofCurves_Neg["Zone1"] = new ExternalGCpCurve(10, -2.0, 300, -0.5);

            // ---- Roof: Positive Pressure Zones ----
            RoofCurves_Pos["Zone3"] = new ExternalGCpCurve(2, 0.6, 100, 0.3);
            RoofCurves_Pos["Zone2"] = new ExternalGCpCurve(2, 0.6, 100, 0.3);
            RoofCurves_Pos["Zone1"] = new ExternalGCpCurve(2, 0.6, 100, 0.3);
        }
    }
}

