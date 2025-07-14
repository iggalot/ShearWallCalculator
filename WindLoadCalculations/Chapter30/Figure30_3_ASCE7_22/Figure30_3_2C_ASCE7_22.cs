namespace ShearWallCalculator.WindLoadCalculations.Chapter30
{
    /// <summary>
    /// ASCE7-16 Figure 30-3-2C
    /// Gable Roofs
    /// h <= 60ft
    /// 20deg < slope <= 27deg
    /// </summary>
    public class Figure30_3_2C_ASCE7_22 : Chapter30_BaseFigure
    {
        public Figure30_3_2C_ASCE7_22()
        {
            ChartTitle = "ASCE 7-16 Figure 30-3-2C - Gable Roofs";
            ChartCriteria = "h <= 60ft, 20deg < slope <= 27deg";

            // ---- Roof: Negative Pressure Zones ----
            RoofCurves_Neg["Zone3r"] = new ExternalGCpCurve(4, -3.6, 50, -1.8);
            RoofCurves_Neg["Zone3e"] = new ExternalGCpCurve(10, -2.5, 150, -1.2);
            RoofCurves_Neg["Zone2n"] = new ExternalGCpCurve(10, -2.5, 150, -1.2);
            RoofCurves_Neg["Zone2r"] = new ExternalGCpCurve(10, -2.5, 150, -1.2);
            RoofCurves_Neg["Zone2e"] = new ExternalGCpCurve(20, -1.5, 300, -0.8);
            RoofCurves_Neg["Zone1"] = new ExternalGCpCurve(20, -1.5, 300, -0.8);

            // ---- Roof: Positive Pressure Zones ----
            RoofCurves_Pos["Zone3r"] = new ExternalGCpCurve(2, 0.7, 100, 0.3);
            RoofCurves_Pos["Zone3e"] = new ExternalGCpCurve(2, 0.7, 100, 0.3);
            RoofCurves_Pos["Zone2n"] = new ExternalGCpCurve(2, 0.7, 100, 0.3);
            RoofCurves_Pos["Zone2r"] = new ExternalGCpCurve(2, 0.7, 100, 0.3);
            RoofCurves_Pos["Zone2e"] = new ExternalGCpCurve(2, 0.7, 100, 0.3);
            RoofCurves_Pos["Zone1"] = new ExternalGCpCurve(2, 0.7, 100, 0.3);

            // ---- Overhang: Negative Pressure Zones ----
            OverhangCurves["Zone3r"] = new ExternalGCpCurve(4, -4.7, 50, -2.4);
            OverhangCurves["Zone3e"] = new ExternalGCpCurve(10, -3.6, 150, -1.7);
            OverhangCurves["Zone2n"] = new ExternalGCpCurve(10, -3.0, 150, -2.2);
            OverhangCurves["Zone2r"] = new ExternalGCpCurve(10, -3.0, 150, -2.2);
            OverhangCurves["Zone2e"] = new ExternalGCpCurve(20, -2.0, 350, -1.8);
            OverhangCurves["Zone1"] = new ExternalGCpCurve(20, -2.0, 350, -1.8);
        }
    }
}
