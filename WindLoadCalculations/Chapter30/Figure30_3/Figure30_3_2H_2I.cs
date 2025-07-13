namespace ShearWallCalculator.WindLoadCalculations.Chapter30
{
    public class Figure30_3_2H_2I : Chapter30_BaseFigure
    {
        public Figure30_3_2H_2I(double h, double b, double slope)
        {
            ChartTitle = "ASCE 7-16 Figure 30-3-2H and 2I - Hip Roofs";
            ChartCriteria = "h <= 60ft, 27deg < slope <= 45deg";

            // Calculate parameters
            double a = -0.6175 - 0.02 * slope;    // Zone1_neg_Roof upper Y1
            double b1 = -0.0950 - 0.0135 * slope; // Zone1_neg_Roof lower Y2
            double c = 0.2 - 0.067 * slope;       // Zone2e_neg_Roof Y1
            double d = 1.0 - 0.082 * slope;       // Zone2r_neg_Roof Y1

            double amin = 9 - 0.135 * slope;
            double amax = 280 - 5.0 * slope;

            // Roof Negative Curves
            RoofCurves_Neg["Zone1"] = new ExternalGCpCurve(10, a, 200, b1);
            RoofCurves_Neg["Zone2e"] = new ExternalGCpCurve(3, c, 50, -0.8, 1, amax);
            RoofCurves_Neg["Zone2r"] = new ExternalGCpCurve(7, d, 200, -1.0, 1, amax);
            RoofCurves_Neg["Zone3"] = new ExternalGCpCurve(10, -1.4, 100, -0.8, amin, 1000);

            // Roof Positive Curves (fixed values)
            RoofCurves_Pos["Zone3"] = new ExternalGCpCurve(3, 0.9, 100, 0.3);
            RoofCurves_Pos["Zone2r"] = new ExternalGCpCurve(3, 0.9, 100, 0.3);
            RoofCurves_Pos["Zone2e"] = new ExternalGCpCurve(3, 0.9, 100, 0.3);
            RoofCurves_Pos["Zone1"] = new ExternalGCpCurve(3, 0.9, 100, 0.3);

            // Overhang negative parameters
            double e = -1.4175 - 0.02 * slope;
            double f = -0.895 - 0.0135 * slope;
            double g = -0.6 - 0.067 * slope;
            double i = 0.2 - 0.082 * slope;
            double j = 0.45 - 0.108 * slope;

            // Overhang Curves
            OverhangCurves["Zone3"] = new ExternalGCpCurve(5, j, 50, -1.8, amin, 1000);
            OverhangCurves["Zone2r"] = new ExternalGCpCurve(7, i, 100, -1.8, 1, amax);
            OverhangCurves["Zone2e"] = new ExternalGCpCurve(3, e, 50, -1.6, 1, amax);
            OverhangCurves["Zone1"] = new ExternalGCpCurve(10, e, 200, f);
        }
    }
}
