using static ShearWallCalculator.WindLoadCalculations.Chapter27RoofFigureFactory_ASCE7_16;

namespace ShearWallCalculator.WindLoadCalculations
{
    public class InterpolatedCpCurve : Chapter27and30_GCpCurveBase
    {
        public override string ChartTitle { get; set; } = "ASCE 7-16 Figure 27.3-1";
        public override string ChartCriteria { get; set; }

        public InterpolatedCpCurve(double h_over_L, double slope, double low_val, double high_val, 
            Chapter27and30_GCpCurveBase lower_curve, Chapter27and30_GCpCurveBase upper_curve, double area = 50)
        {
            ChartCriteria = $"Slope = {slope:0.##} — interpolated h/L = {h_over_L:0.###}";

            // Roof zones

            foreach (var key in lower_curve.RoofCurves_Pos.Keys)
            {
                RoofCurves_Pos[key] = GCpCurveInterpolator.Interpolate(
                lower_curve.RoofCurves_Pos[key],
                upper_curve.RoofCurves_Pos[key],
                h_over_L, low_val, high_val);
            }

            foreach (var key in lower_curve.RoofCurves_Neg.Keys)
            {
                RoofCurves_Neg[key] = GCpCurveInterpolator.Interpolate(
                lower_curve.RoofCurves_Neg[key],
                upper_curve.RoofCurves_Neg[key],
                h_over_L, low_val, high_val);
            }
        }
    }

}
