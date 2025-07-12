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
    public class Figure30_3_2E_2F:Chapter30_BaseFigure
    {
        /// <summary>
        /// ASCE7-16 Figure 30-3-2E (Roof) and 2F (Overhang)
        /// </summary>
        /// <param name="h">mean roof height</param>
        /// <param name="B">width of building -- normal to wind</param>
        public Figure30_3_2E_2F(double h, double B)
        {
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

            low.Overhang["Zone3"] = new ExternalGCpCurve(10, 0.7, 100, 0.3);
            low.Overhang["Zone2r"] = new ExternalGCpCurve(10, 0.7, 100, 0.3);
            low.Overhang["Zone2e"] = new ExternalGCpCurve(10, 0.7, 100, 0.3);
            low.Overhang["Zone1"] = new ExternalGCpCurve(10, 0.7, 100, 0.3);

            // High h/B >= 0.8
            high.RoofNeg["Zone3"] = new ExternalGCpCurve(10, -2.6, 200, -1.4);
            high.RoofNeg["Zone2e"] = new ExternalGCpCurve(10, -2.6, 200, -1.4);
            high.RoofNeg["Zone2r"] = new ExternalGCpCurve(10, -2.4, 200, -1.3);
            high.RoofNeg["Zone1"] = new ExternalGCpCurve(20, -1.8, 100, -1.0);

            high.RoofPos["Zone3"] = new ExternalGCpCurve(10, 0.7, 100, 0.3);
            high.RoofPos["Zone2r"] = new ExternalGCpCurve(10, 0.7, 100, 0.3);
            high.RoofPos["Zone2e"] = new ExternalGCpCurve(10, 0.7, 100, 0.3);
            high.RoofPos["Zone1"] = new ExternalGCpCurve(10, 0.7, 100, 0.3);

            high.Overhang["Zone3"] = new ExternalGCpCurve(10, -2.0, 200, -1.0);
            high.Overhang["Zone2e"] = new ExternalGCpCurve(10, -2.0, 200, -1.0);
            high.Overhang["Zone2r"] = new ExternalGCpCurve(10, -2.0, 200, -1.0);
            high.Overhang["Zone1"] = new ExternalGCpCurve(10, -1.4, 100, -0.8);

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

            // Optional: Handle intermediate values (e.g. 0.5 < h/B < 0.8)
            // You can interpolate or throw depending on your design

            ///// <summary>
            ///// Roof
            ///// </summary>
            //if (h / B >= 0.8)
            //{
            //    // Figure30_3_2E
            //    /// <summary>
            //    /// Roof
            //    /// </summary>
            //    Zone3_neg_Roof = new ExternalGCpCurve(10, -2.6, 200, -1.4);
            //    Zone2e_neg_Roof = new ExternalGCpCurve(10, -2.6, 200, -1.4);
            //    Zone2r_neg_Roof = new ExternalGCpCurve(10, -2.4, 200, -1.3);
            //    Zone1_neg_Roof = new ExternalGCpCurve(20, -1.8, 100, -1.0);

            //    Zone3_pos_Roof = new ExternalGCpCurve(10, 0.7, 100, 0.3);
            //    Zone2r_pos_Roof = new ExternalGCpCurve(10, 0.7, 100, 0.3);
            //    Zone2e_pos_Roof = new ExternalGCpCurve(10, 0.7, 100, 0.3);
            //    Zone1_pos_Roof = new ExternalGCpCurve(10, 0.7, 100, 0.3);

            //    // Figur30_3_2F
            //    /// <summary>
            //    /// Overhang
            //    /// </summary>
            //    Zone3_Overhang = new ExternalGCpCurve(10, -2.0, 200, -1.0);
            //    Zone2e_Overhang = new ExternalGCpCurve(10, -2.0, 200, -1.0);
            //    Zone2r_Overhang = new ExternalGCpCurve(10, -2.0, 200, -1.0);
            //    Zone1_Overhang = new ExternalGCpCurve(10, -1.4, 100, -0.8);


            //}
            //else if (h / B <= 0.5)
            //{
            //    /// <summary>
            //    /// Roof
            //    /// </summary>
            //    Zone3_neg_Roof = new ExternalGCpCurve(10, -1.8, 200, -1.1);
            //    Zone2e_neg_Roof = new ExternalGCpCurve(10, -1.8, 200, -1.1);
            //    Zone2r_neg_Roof = new ExternalGCpCurve(10, -2.4, 200, -1.3);
            //    Zone1_neg_Roof = new ExternalGCpCurve(20, -1.3, 100, -1.0);

            //    Zone3_pos_Roof = new ExternalGCpCurve(10, 0.7, 100, 0.3);
            //    Zone2r_pos_Roof = new ExternalGCpCurve(10, 0.7, 100, 0.3);
            //    Zone2e_pos_Roof = new ExternalGCpCurve(10, 0.7, 100, 0.3);
            //    Zone1_pos_Roof = new ExternalGCpCurve(10, 0.7, 100, 0.3);

            //    /// <summary>
            //    /// Overhang
            //    /// </summary>
            //    Zone3_Overhang = new ExternalGCpCurve(10, 0.7, 100, 0.3);
            //    Zone2r_Overhang = new ExternalGCpCurve(10, 0.7, 100, 0.3);
            //    Zone2e_Overhang = new ExternalGCpCurve(10, 0.7, 100, 0.3);
            //    Zone1_Overhang = new ExternalGCpCurve(10, 0.7, 100, 0.3);
            //}
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
