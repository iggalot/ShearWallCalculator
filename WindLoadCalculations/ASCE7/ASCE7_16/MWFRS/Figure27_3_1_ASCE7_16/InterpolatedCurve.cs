using static ShearWallCalculator.WindLoadCalculations.Chapter27RoofFigureFactory_ASCE7_16;

namespace ShearWallCalculator.WindLoadCalculations
{
    public class InterpolatedCurve : Chapter27and30_GCpCurveBase
    {
        public override string ChartTitle { get; set; } = "ASCE 7-16 Figure 27.3-1";
        public override string ChartCriteria { get; set; }

        public InterpolatedCurve(double h_over_L, double slope, double low_val, double high_val, 
            Chapter27and30_GCpCurveBase low, Chapter27and30_GCpCurveBase high, double area = 50)
        {
            ChartCriteria = $"Slope = {slope:0.##} — interpolated h/L = {h_over_L:0.###}";

            // Roof zones
            RoofCurves_Pos["ZoneWW"] = GCpCurveInterpolator.Interpolate(
                low.RoofCurves_Pos["ZoneWW"],
                high.RoofCurves_Pos["ZoneWW"],
                h_over_L, low_val, high_val);

            RoofCurves_Neg["ZoneWW"] = GCpCurveInterpolator.Interpolate(
                low.RoofCurves_Neg["ZoneWW"],
                high.RoofCurves_Neg["ZoneWW"],
                h_over_L, low_val, high_val);

            RoofCurves_Neg["ZoneLW"] = GCpCurveInterpolator.Interpolate(
                low.RoofCurves_Neg["ZoneLW"],
                high.RoofCurves_Neg["ZoneLW"],
                h_over_L, low_val, high_val);
        }
    }

}
