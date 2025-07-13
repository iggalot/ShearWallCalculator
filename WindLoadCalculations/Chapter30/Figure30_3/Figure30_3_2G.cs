namespace ShearWallCalculator.WindLoadCalculations.Chapter30
{
    /// <summary>
    /// ASCE7-16 Figure 30-3-2G
    /// Hip Roofs
    /// h <= 60ft
    /// 20deg < slope <= 27deg
    /// </summary>
    public class Figure30_3_2G : Chapter30_BaseFigure
    {
        public Figure30_3_2G(double h, double b)
        {
            ChartTitle = "ASCE 7-16 Figure 30-3-2G - Hip Roofs";
            ChartCriteria = "h <= 60ft, 20deg < slope <= 27deg";

            // Roof Negative Pressure Zones
            RoofCurves_Neg["Zone3"] = new ExternalGCpCurve(10, -2.0, 200, -1.0);
            RoofCurves_Neg["Zone2e"] = new ExternalGCpCurve(10, -2.0, 200, -1.0);
            RoofCurves_Neg["Zone2r"] = new ExternalGCpCurve(10, -2.0, 200, -1.0);
            RoofCurves_Neg["Zone1"] = new ExternalGCpCurve(10, -1.4, 100, -0.8);

            // Roof Positive Pressure Zones
            RoofCurves_Pos["Zone3"] = new ExternalGCpCurve(10, 0.7, 100, 0.3);
            RoofCurves_Pos["Zone2r"] = new ExternalGCpCurve(10, 0.7, 100, 0.3);
            RoofCurves_Pos["Zone2e"] = new ExternalGCpCurve(10, 0.7, 100, 0.3);
            RoofCurves_Pos["Zone1"] = new ExternalGCpCurve(10, 0.7, 100, 0.3);

            // Overhang Zones
            OverhangCurves["Zone3"] = new ExternalGCpCurve(7, -3.1, 200, -1.5);
            OverhangCurves["Zone2r"] = new ExternalGCpCurve(7, -2.5, 200, -2.0);
            OverhangCurves["Zone2e"] = new ExternalGCpCurve(7, -2.5, 200, -2.0);
            OverhangCurves["Zone1"] = new ExternalGCpCurve(7, -1.9, 200, -1.8);
        } 
    }
}
