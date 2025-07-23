namespace ShearWallCalculator.WindLoadCalculations.Chapter30
{
    /// <summary>
    /// ASCE 7-22 Figure 30-3-2A
    /// Gable Roofs
    /// h <= 60ft
    /// slope <= 7deg
    /// </summary>
    public class Figure30_3_2A_ASCE7_22 : Chapter30_BaseFigure
    {
        public Figure30_3_2A_ASCE7_22()
        {
            ChartTitle = "ASCE 7-22 Figure 30-3-2A";
            ChartCriteria = "h <= 60ft, slope <= 7deg";

            // Positive Pressure Roof Zones
            RoofCurves_Pos["Zone3"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, 0.3),
                (10,  0.3),
                (20, 0.25),
                (50, 0.22),
                (100, 0.2),
                (200, 0.2),
                (500, 0.2),
                (1000, 0.2)
            });

            RoofCurves_Pos["Zone2"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, 0.3),
                (10,  0.3),
                (20, 0.25),
                (50, 0.22),
                (100, 0.2),
                (200, 0.2),
                (500, 0.2),
                (1000, 0.2)
            });
            RoofCurves_Pos["Zone1"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, 0.3),
                (10,  0.3),
                (20, 0.25),
                (50, 0.22),
                (100, 0.2),
                (200, 0.2),
                (500, 0.2),
                (1000, 0.2)
            });
            RoofCurves_Pos["Zone1'"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, 0.3),
                (10,  0.3),
                (20, 0.25),
                (50, 0.22),
                (100, 0.2),
                (200, 0.2),
                (500, 0.2),
                (1000, 0.2)
            });

            // Negative Pressure Roof Zones
            RoofCurves_Neg["Zone3"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -3.2),
                (10,  -3.2),
                (20, -2.9),
                (50, -2.45),
                (100, -2.15),
                (200, -1.83),
                (500, -1.4),
                (1000, -1.4)
            });
            RoofCurves_Neg["Zone2"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -2.3),
                (10,  -2.3),
                (20, -2.13),
                (50, -1.92),
                (100, -1.78),
                (200, -1.62),
                (500, -1.4),
                (1000, -1.4)
            });
            RoofCurves_Neg["Zone1"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -1.7),
                (10,  -1.7),
                (20, -1.59),
                (50, -1.4),
                (100, -1.3),
                (200, -1.15),
                (500, -1.0),
                (1000, -1.0)
            });
            RoofCurves_Neg["Zone1'"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -0.9),
                (10,  -0.9),
                (20, -0.9),
                (50, -0.9),
                (100, -0.9),
                (200, -0.75),
                (500, -0.55),
                (1000, -0.4),
                (1500, -0.4),
            });

            // Overhang Curves
            OverhangCurves["Zone3"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -3.2),
                (10,  -3.2),
                (20, -2.85),
                (50, -2.35),
                (100, -1.97),
                (200, -1.6),
                (500, -1.1),
                (1000, -1.1),
            });
            OverhangCurves["Zone2"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -2.3),
                (10,  -2.3),
                (20, -2.1),
                (50, -1.81),
                (100, -1.6),
                (200, -1.38),
                (500, -1.1),
                (1000, -1.1),
            });
            OverhangCurves["Zone1"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -1.7),
                (10,  -1.7),
                (20, -1.66),
                (50, -1.62),
                (100, -1.6),
                (200, -1.35),
                (500, -1.0),
                (1000, -1.0),
            });
            OverhangCurves["Zone1'"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -1.7),
                (10,  -1.7),
                (20, -1.66),
                (50, -1.62),
                (100, -1.6),
                (200, -1.35),
                (500, -1.0),
                (1000, -1.0),
            });

        }
    }
}
