namespace ShearWallCalculator.WindLoadCalculations.Chapter30
{
    public class Figure30_3_2H_2I : Chapter30_BaseFigure
    {
        public Figure30_3_2H_2I(double h, double b, double slope)
        {
            var a = -0.6175 - 0.02 * slope;  // z1 neg upper
            var b1 = -0.0950 - 0.0135 * slope; // z1 neg lower
            var c = 0.2 - 0.067 * slope; // 2e neg
            var d = 1.0 - 0.082 * slope; // 2r neg

            var amin = 9 - 0.135 * slope;
            var amax = 280 - 5.0 * slope;

            // Figure30_3_2H
            /// <summary>
            /// Roof
            /// </summary>
            Zone1_neg_Roof = new ExternalGCpCurve(10, a, 200, b1);
            Zone2e_neg_Roof = new ExternalGCpCurve(3, c, 50, -0.8, 1, amax);
            Zone2r_neg_Roof = new ExternalGCpCurve(7, d, 200, -1.0, 1, amax);
            Zone3_neg_Roof = new ExternalGCpCurve(10, -1.4, 100, -0.8, amin, 1000);


            Zone3_pos_Roof = new ExternalGCpCurve(3, 0.9, 100, 0.3);
            Zone2r_pos_Roof = new ExternalGCpCurve(3, 0.9, 100, 0.3);
            Zone2e_pos_Roof = new ExternalGCpCurve(3, 0.9, 100, 0.3);
            Zone1_pos_Roof = new ExternalGCpCurve(3, 0.9, 100, 0.3);

            // Figure30_3_2I
            /// <summary>
            /// Overhang
            /// </summary>
            var e = -1.4175 - 0.02 * slope;  // z1 neg upper
            var f = -0.895 - 0.0135 * slope; // z1 neg lower
            var g = -0.6 - 0.067 * slope; // 2e neg
            var i = 0.2 - 0.082 * slope; // 2r neg
            var j = 0.45 - 0.108 * slope; // 3
            Zone3_Overhang = new ExternalGCpCurve(5, j, 50, -1.8, amin, 1000);
            Zone2r_Overhang = new ExternalGCpCurve(7, i, 100, -1.8, 1, amax);
            Zone2e_Overhang = new ExternalGCpCurve(3, e, 50, -1.6, 1, amax);
            Zone1_Overhang = new ExternalGCpCurve(10, e, 200, f);
        }
    }
}
