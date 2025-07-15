namespace ShearWallCalculator.WindLoadCalculations.Chapter30
{
    /// <summary>
    /// ASCE 7-16 Figure 30-3-2A
    /// Gable Roofs
    /// h <= 60ft
    /// slope <= 7deg
    /// </summary>I'm lost
    public class Figure30_3_2A_ASCE7_16 : Chapter30_BaseFigure
    {
        public Figure30_3_2A_ASCE7_16()
        {
            ChartTitle = "ASCE 7-16 Figure 30-3-2A";
            ChartCriteria = "h <= 60ft, slope <= 7deg";

            // Positive Pressure Roof Zones
            RoofCurves_Pos["Zone3"] = new ExternalGCpCurve(10, 0.3, 100, 0.2);
            RoofCurves_Pos["Zone2"] = new ExternalGCpCurve(10, 0.3, 100, 0.2);
            RoofCurves_Pos["Zone1"] = new ExternalGCpCurve(10, 0.3, 100, 0.2);
            RoofCurves_Pos["Zone1'"] = new ExternalGCpCurve(10, 0.3, 100, 0.2);

            // Negative Pressure Roof Zones
            RoofCurves_Neg["Zone3"] = new ExternalGCpCurve(10, -3.2, 500, -1.4);
            RoofCurves_Neg["Zone2"] = new ExternalGCpCurve(10, -2.3, 500, -1.4);
            RoofCurves_Neg["Zone1"] = new ExternalGCpCurve(10, -1.7, 500, -1.0);
            RoofCurves_Neg["Zone1'"] = new ExternalGCpCurve(100, -0.9, 1000, -0.4);


            // Overhang Curves
            OverhangCurves["Zone3"] = new ExternalGCpCurve(10, -3.2, 500, -1.1);
            OverhangCurves["Zone2"] = new ExternalGCpCurve(10, -2.3, 500, -1.1);
            // TODO: THIS IS FOUR SEGMENT GRAPH -- Update to correct values for all overhang
            OverhangCurves["Zone1"] = new ExternalGCpCurve(100, -1.7, 500, -1.0);
            OverhangCurves["Zone1'"] = new ExternalGCpCurve(100, -1.7, 500, -1.0);

        }
        // Overhang zones

    }
}
