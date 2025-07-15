namespace ShearWallCalculator.WindLoadCalculations.Chapter30
{
    /// <summary>
    /// ASCE7-16 Figure 30-3-2G
    /// Hip Roofs
    /// h <= 60ft
    /// slope = 45 deg
    /// </summary>
    public class Figure30_3_2G_ASCE7_22 : Chapter30_BaseFigure
    {
        public Figure30_3_2G_ASCE7_22()
        {
            ChartTitle = "ASCE 7-22 Figure 30-3-2G - Hip Roofs";
            ChartCriteria = "h <= 60ft, slope = 45deg";

            // Roof Negative Pressure Zones
            RoofCurves_Neg["Zone3"] = new ExternalGCpCurve(10, -2.4, 100, -1.0);
            RoofCurves_Neg["Zone2"] = new ExternalGCpCurve(10, -1.8, 100, -0.8);
            RoofCurves_Neg["Zone1"] = new ExternalGCpCurve(10, -1.5, 100, -0.7);

            // Roof Positive Pressure Zones
            RoofCurves_Pos["Zone3"] = new ExternalGCpCurve(10, 0.7, 100, 0.3);
            RoofCurves_Pos["Zone2"] = new ExternalGCpCurve(10, 0.7, 100, 0.3);
            RoofCurves_Pos["Zone1"] = new ExternalGCpCurve(10, 0.7, 100, 0.3);
        } 
    }
}
