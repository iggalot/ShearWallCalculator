namespace ShearWallCalculator.WindLoadCalculations.Chapter30
{
    /// <summary>
    /// ASCE7-16 Figure 30-3-2B
    /// Gable Roofs
    /// h <= 60ft
    /// 7deg < slope <= 20deg
    /// </summary>
    public class Figure30_3_2B_ASCE7_22 : Chapter30_BaseFigure
    {
        public Figure30_3_2B_ASCE7_22()
        {
            ChartTitle = "ASCE 7-16 Figure 30-3-2B - Gable Roofs";
            ChartCriteria = "h <= 60ft, 7deg < slope <= 20deg";

            // ---- Roof: Negative Pressure Zones ----
            RoofCurves_Neg["Zone3r"] = new ExternalGCpCurve(10, -3.6, 100, -1.8);
            RoofCurves_Neg["Zone3e"] = new ExternalGCpCurve(10, -3.0, 250, -1.0);
            RoofCurves_Neg["Zone2n"] = new ExternalGCpCurve(10, -3.0, 250, -1.0);
            RoofCurves_Neg["Zone2r"] = new ExternalGCpCurve(10, -3.0, 250, -1.0);
            RoofCurves_Neg["Zone2e"] = new ExternalGCpCurve(20, -2.0, 100, -0.5);
            RoofCurves_Neg["Zone1"] = new ExternalGCpCurve(20, -2.0, 100, -0.5);

            // ---- Roof: Positive Pressure Zones ----
            RoofCurves_Pos["Zone3r"] = new ExternalGCpCurve(2, 0.7, 100, 0.3);
            RoofCurves_Pos["Zone3e"] = new ExternalGCpCurve(2, 0.7, 100, 0.3);
            RoofCurves_Pos["Zone2n"] = new ExternalGCpCurve(2, 0.7, 100, 0.3);
            RoofCurves_Pos["Zone2r"] = new ExternalGCpCurve(2, 0.7, 100, 0.3);
            RoofCurves_Pos["Zone2e"] = new ExternalGCpCurve(2, 0.7, 100, 0.3);
            RoofCurves_Pos["Zone1"] = new ExternalGCpCurve(2, 0.7, 100, 0.3);

            // ---- Overhang: Negative Pressure Zones ----
            OverhangCurves["Zone3r"] = new ExternalGCpCurve(10, -4.7, 100, -2.3);
            OverhangCurves["Zone3e"] = new ExternalGCpCurve(10, -4.1, 250, -1.5);
            OverhangCurves["Zone2n"] = new ExternalGCpCurve(10, -3.5, 250, -2.0);
            OverhangCurves["Zone2r"] = new ExternalGCpCurve(10, -3.5, 250, -2.0);
            OverhangCurves["Zone2e"] = new ExternalGCpCurve(20, -2.5, 100, -1.5);
            OverhangCurves["Zone1"] = new ExternalGCpCurve(10, -2.5, 100, -1.5);
        }
    }
}

