namespace ShearWallCalculator.WindLoadCalculations.Chapter30
{
    /// <summary>
    /// ASCE7-16 Figure 30-3-2G
    /// Hip Roofs
    /// h <= 60ft
    /// slope = 45 deg
    /// </summary>
    public class Figure30_3_2F_2G_INTERPOLATED_ASCE7_22 : Chapter30_BaseFigure
    {
        public Figure30_3_2F_2G_INTERPOLATED_ASCE7_22(double slope)
        {
            ChartTitle = "ASCE 7-22 Figure 30-3-2G - Hip Roofs";
            ChartCriteria = "h <= 60ft, 27deg < slope < 45deg";

            Figure30_3_2F_ASCE7_22 f30_3_2F = new Figure30_3_2F_ASCE7_22();
            Figure30_3_2G_ASCE7_22 f30_3_2G = new Figure30_3_2G_ASCE7_22();

            /// NEegative pressures are interpolated from Table2F and 2G for the specified slope value
            // Zone 3 negative
            var curve3f = f30_3_2F.RoofCurves_Neg["Zone3"];
            var curve3g = f30_3_2G.RoofCurves_Neg["Zone3"];
            var interp_curve3_neg_y1 = (curve3f.Y1 - curve3f.Y1) * (slope - 27) / (45 - 27) + curve3f.Y1;
            var interp_curve3_neg_y2 = (curve3g.Y2 - curve3g.Y2) * (slope - 27) / (45 - 27) + curve3g.Y2;

            // Zone 2 negative
            var curve2f = f30_3_2F.RoofCurves_Neg["Zone2"];
            var curve2g = f30_3_2G.RoofCurves_Neg["Zone2"];
            var interp_curve2_neg_y1 = (curve2f.Y1 - curve2f.Y1) * (slope - 27) / (45 - 27) + curve2f.Y1;
            var interp_curve2_neg_y2 = (curve2g.Y2 - curve2g.Y2) * (slope - 27) / (45 - 27) + curve2g.Y2;

            // Zone 1 negative
            var curve1f = f30_3_2F.RoofCurves_Neg["Zone1"];
            var curve1g = f30_3_2G.RoofCurves_Neg["Zone1"];
            var interp_curve1_neg_y1 = (curve1f.Y1 - curve1f.Y1) * (slope - 27) / (45 - 27) + curve1f.Y1;
            var interp_curve1_neg_y2 = (curve1g.Y2 - curve1g.Y2) * (slope - 27) / (45 - 27) + curve1g.Y2;
            
            // Roof Positive Pressure Zones
            /// These are the same for 2F and 2G
            RoofCurves_Pos["Zone3"] = new ExternalGCpCurve(10, 0.7, 100, 0.3);
            RoofCurves_Pos["Zone2"] = new ExternalGCpCurve(10, 0.7, 100, 0.3);
            RoofCurves_Pos["Zone1"] = new ExternalGCpCurve(10, 0.7, 100, 0.3);
        } 
    }
}
