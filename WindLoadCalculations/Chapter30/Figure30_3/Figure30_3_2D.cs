namespace ShearWallCalculator.WindLoadCalculations.Chapter30
{
    /// <summary>
    /// ASCE7-16 Figure 30-3-2D
    /// Gable Roofs
    /// h <= 60ft
    /// 27deg < slope <= 45deg
    /// </summary>
    public class Figure30_3_2D : Chapter30_BaseFigure
    {
        public Figure30_3_2D()
        {
            // ---- Roof: Negative Pressure Zones ----
            RoofCurves_Neg["Zone3r"] = new ExternalGCpCurve(10, -2.0, 200, -1.0);
            RoofCurves_Neg["Zone3e"] = new ExternalGCpCurve(2, -3.2, 300, -1.0);
            RoofCurves_Neg["Zone2n"] = new ExternalGCpCurve(10, -2.0, 200, -1.0);
            RoofCurves_Neg["Zone2r"] = new ExternalGCpCurve(10, -1.8, 100, -0.8);
            RoofCurves_Neg["Zone2e"] = new ExternalGCpCurve(10, -1.8, 100, -0.8);
            RoofCurves_Neg["Zone1"] = new ExternalGCpCurve(10, -1.8, 100, -0.8);

            // ---- Roof: Positive Pressure Zones ----
            RoofCurves_Pos["Zone3r"] = new ExternalGCpCurve(10, 0.9, 100, 0.5);
            RoofCurves_Pos["Zone3e"] = new ExternalGCpCurve(10, 0.9, 100, 0.5);
            RoofCurves_Pos["Zone2n"] = new ExternalGCpCurve(10, 0.9, 100, 0.5);
            RoofCurves_Pos["Zone2r"] = new ExternalGCpCurve(10, 0.9, 100, 0.5);
            RoofCurves_Pos["Zone2e"] = new ExternalGCpCurve(10, 0.9, 100, 0.5);
            RoofCurves_Pos["Zone1"] = new ExternalGCpCurve(10, 0.9, 100, 0.5);

            // ---- Overhang: Negative Pressure Zones ----
            OverhangCurves["Zone3r"] = new ExternalGCpCurve(10, -2.8, 200, -1.8);
            OverhangCurves["Zone3e"] = new ExternalGCpCurve(2, -4.0, 300, -1.8);
            OverhangCurves["Zone2n"] = new ExternalGCpCurve(10, -2.8, 200, -1.8);
            OverhangCurves["Zone2r"] = new ExternalGCpCurve(10, -3.0, 150, -2.2);
            OverhangCurves["Zone2e"] = new ExternalGCpCurve(10, -2.6, 100, -1.6);
            OverhangCurves["Zone1"] = new ExternalGCpCurve(10, -2.6, 100, -1.6);

            ///// <summary>
            ///// Roof
            ///// </summary>
            //Zone3r_neg_Roof = new ExternalGCpCurve(10, -2.0, 200, -1.0);
            //Zone3e_neg_Roof = new ExternalGCpCurve(2, -3.2, 300, -1.0);
            //Zone2n_neg_Roof = new ExternalGCpCurve(10, -2.0, 200, -1.0);
            //Zone2r_neg_Roof = new ExternalGCpCurve(10, -1.8, 100, -0.8);
            //Zone2e_neg_Roof = new ExternalGCpCurve(10, -1.8, 100, -0.8);
            //Zone1_neg_Roof = new ExternalGCpCurve(10, -1.8, 100, -0.8);

            //Zone3r_pos_Roof = new ExternalGCpCurve(10, 0.9, 100, 0.5);
            //Zone3e_pos_Roof = new ExternalGCpCurve(10, 0.9, 100, 0.5);
            //Zone2n_pos_Roof = new ExternalGCpCurve(10, 0.9, 100, 0.5);
            //Zone2r_pos_Roof = new ExternalGCpCurve(10, 0.9, 100, 0.5);
            //Zone2e_pos_Roof = new ExternalGCpCurve(10, 0.9, 100, 0.5);
            //Zone1_pos_Roof = new ExternalGCpCurve(10, 0.9, 100, 0.5);

            ///// <summary>
            ///// Overhang
            ///// </summary>
            //Zone3r_Overhang = new ExternalGCpCurve(10, -2.8, 200, -1.8);
            //Zone3e_Overhang = new ExternalGCpCurve(2, -4.0, 300, -1.8);
            //Zone2n_Overhang = new ExternalGCpCurve(10, -2.8, 200, -1.8);
            //Zone2r_Overhang = new ExternalGCpCurve(10, -3.0, 150, -2.2);
            //Zone2e_Overhang = new ExternalGCpCurve(10, -2.6, 100, -1.6);
            //Zone1_Overhang = new ExternalGCpCurve(10, -2.6, 100, -1.6);
        }
    }
}
