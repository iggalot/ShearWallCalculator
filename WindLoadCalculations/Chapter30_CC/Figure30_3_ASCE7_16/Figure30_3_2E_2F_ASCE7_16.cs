using System.Collections.Generic;

namespace ShearWallCalculator.WindLoadCalculations.Chapter30
{
    /// <summary>
    /// ASCE7-16 Figure 30-3-2E
    /// Hip Roofs
    /// h/B >= 0.8
    /// h <= 60ft
    /// 7deg < slope <= 20deg
    /// </summary>
    public class Figure30_3_2E_2F_ASCE7_16:Chapter30_BaseFigure
    {
        /// <summary>
        /// ASCE7-16 Figure 30-3-2E (Roof) and 2F (Overhang)
        /// </summary>
        /// <param name="h">mean roof height</param>
        /// <param name="B">width of building -- normal to wind</param>
        public Figure30_3_2E_2F_ASCE7_16(double h, double B)
        {
            ChartTitle = "ASCE 7-16 Figure 30-3-2E and 2F - Hip Roofs";
            ChartCriteria = "h <= 60ft, 7deg < slope <= 20deg";

            double ratio = h / B;

            var low = new FigureDataSet();
            var high = new FigureDataSet();

            // Low h/B <= 0.5
            low.RoofNeg["Zone3"] = new ExternalGCpCurve(10, -1.8, 200, -1.1);
            low.RoofNeg["Zone2e"] = new ExternalGCpCurve(10, -1.8, 200, -1.1);
            low.RoofNeg["Zone2r"] = new ExternalGCpCurve(10, -2.4, 200, -1.3);
            low.RoofNeg["Zone1"] = new ExternalGCpCurve(20, -1.3, 100, -1.0);

            low.RoofPos["Zone3"] = new ExternalGCpCurve(10, 0.7, 100, 0.3);
            low.RoofPos["Zone2r"] = new ExternalGCpCurve(10, 0.7, 100, 0.3);
            low.RoofPos["Zone2e"] = new ExternalGCpCurve(10, 0.7, 100, 0.3);
            low.RoofPos["Zone1"] = new ExternalGCpCurve(10, 0.7, 100, 0.3);

            low.Overhang["Zone3"] = new ExternalGCpCurve(10, -2.9, 200, -1.6);
            low.Overhang["Zone2r"] = new ExternalGCpCurve(10, -2.9, 200, -2.3);
            low.Overhang["Zone2e"] = new ExternalGCpCurve(10, -2.3, 200, -2.1);
            low.Overhang["Zone1"] = new ExternalGCpCurve(10, -1.8, 100, -2.0);

            // High h/B >= 0.8
            high.RoofNeg["Zone3"] = new ExternalGCpCurve(10, -2.6, 200, -1.4);
            high.RoofNeg["Zone2e"] = new ExternalGCpCurve(10, -2.6, 200, -1.4);
            high.RoofNeg["Zone2r"] = new ExternalGCpCurve(10, -2.4, 200, -1.3);
            high.RoofNeg["Zone1"] = new ExternalGCpCurve(20, -1.8, 100, -1.0);

            high.RoofPos["Zone3"] = new ExternalGCpCurve(10, 0.7, 100, 0.3);
            high.RoofPos["Zone2r"] = new ExternalGCpCurve(10, 0.7, 100, 0.3);
            high.RoofPos["Zone2e"] = new ExternalGCpCurve(10, 0.7, 100, 0.3);
            high.RoofPos["Zone1"] = new ExternalGCpCurve(10, 0.7, 100, 0.3);

            high.Overhang["Zone3"] = new ExternalGCpCurve(10, -3.7, 200, -1.9);
            high.Overhang["Zone2e"] = new ExternalGCpCurve(10, -3.1, 200, -2.4);
            high.Overhang["Zone2r"] = new ExternalGCpCurve(10, -2.9, 200, -2.3);
            high.Overhang["Zone1"] = new ExternalGCpCurve(20, -2.3, 100, -2.0);

            if (ratio <= 0.5)
            {
                CopyDataset(low);
            }
            else if (ratio >= 0.8)
            {
                CopyDataset(high);
            }
            else
            {
                double t = (ratio - 0.5) / (0.8 - 0.5);
                CopyDataset(InterpolateDataSet(low, high, t));
            }
        }

        private void CopyDataset(FigureDataSet source)
        {
            foreach (var kv in source.RoofNeg) RoofCurves_Neg[kv.Key] = kv.Value;
            foreach (var kv in source.RoofPos) RoofCurves_Pos[kv.Key] = kv.Value;
            foreach (var kv in source.Overhang) OverhangCurves[kv.Key] = kv.Value;
        }

        private FigureDataSet InterpolateDataSet(FigureDataSet low, FigureDataSet high, double t)
        {
            var result = new FigureDataSet();

            foreach (var key in low.RoofNeg.Keys)
                result.RoofNeg[key] = InterpolateCurve(low.RoofNeg[key], high.RoofNeg[key], t);

            foreach (var key in low.RoofPos.Keys)
                result.RoofPos[key] = InterpolateCurve(low.RoofPos[key], high.RoofPos[key], t);

            foreach (var key in low.Overhang.Keys)
                result.Overhang[key] = InterpolateCurve(low.Overhang[key], high.Overhang[key], t);

            return result;
        }

        private ExternalGCpCurve InterpolateCurve(ExternalGCpCurve a, ExternalGCpCurve b, double t)
        {
            return new ExternalGCpCurve(
                Lerp(a.X1, b.X1, t),
                Lerp(a.Y1, b.Y1, t),
                Lerp(a.X2, b.X2, t),
                Lerp(a.Y2, b.Y2, t)
            );
        }

        private double Lerp(double a, double b, double t)
        {
            return a + (b - a) * t;
        }

        private class FigureDataSet
        {
            public Dictionary<string, ExternalGCpCurve> RoofNeg = new Dictionary<string, ExternalGCpCurve>();
            public Dictionary<string, ExternalGCpCurve> RoofPos = new Dictionary<string, ExternalGCpCurve>();
            public Dictionary<string, ExternalGCpCurve> Overhang = new Dictionary<string, ExternalGCpCurve>();
        }
    }
}
