using System.Collections.Generic;

namespace ShearWallCalculator.WindLoadCalculations.Chapter30
{
    public abstract class Chapter30_BaseFigure
    {
        public Dictionary<string, ExternalGCpCurve> RoofCurves_Pos { get; private set; }
        public Dictionary<string, ExternalGCpCurve> RoofCurves_Neg { get; private set; }
        public Dictionary<string, ExternalGCpCurve> OverhangCurves { get; private set; }

        protected Chapter30_BaseFigure()
        {
            RoofCurves_Pos = new Dictionary<string, ExternalGCpCurve>();
            RoofCurves_Neg = new Dictionary<string, ExternalGCpCurve>();
            OverhangCurves = new Dictionary<string, ExternalGCpCurve>();
        }


        ///// <summary>
        ///// Roof
        ///// </summary>
        //public static ExternalGCpCurve Zone3_neg_Roof { get; set; } = null;
        //public static ExternalGCpCurve Zone3r_neg_Roof { get; set; } = null;
        //public static ExternalGCpCurve Zone3e_neg_Roof { get; set; } = null;

        //public static ExternalGCpCurve Zone2_neg_Roof { get; set; } = null;
        //public static ExternalGCpCurve Zone2n_neg_Roof { get; set; } = null;
        //public static ExternalGCpCurve Zone2r_neg_Roof { get; set; } = null;
        //public static ExternalGCpCurve Zone2e_neg_Roof { get; set; } = null;
        //public static ExternalGCpCurve Zone1_neg_Roof { get; set; } = null;
        //public static ExternalGCpCurve Zone1_prime_neg_Roof { get; set; } = null;

        //public static ExternalGCpCurve Zone3_pos_Roof { get; set; } = null;
        //public static ExternalGCpCurve Zone3r_pos_Roof { get; set; } = null;
        //public static ExternalGCpCurve Zone3e_pos_Roof { get; set; } = null;
        //public static ExternalGCpCurve Zone2_pos_Roof { get; set; } = null;

        //public static ExternalGCpCurve Zone2n_pos_Roof { get; set; } = null;
        //public static ExternalGCpCurve Zone2r_pos_Roof { get; set; } = null;
        //public static ExternalGCpCurve Zone2e_pos_Roof { get; set; } = null;
        //public static ExternalGCpCurve Zone1_pos_Roof { get; set; } = null;
        //public static ExternalGCpCurve Zone1_prime_pos_Roof { get; set; } = null;

        ///// <summary>
        ///// Overhang
        ///// </summary>
        //public static ExternalGCpCurve Zone3_Overhang { get; set; } = null;
        //public static ExternalGCpCurve Zone3r_Overhang { get; set; } = null;
        //public static ExternalGCpCurve Zone3e_Overhang { get; set; } = null;
        //public static ExternalGCpCurve Zone2_Overhang { get; set; } = null;
        //public static ExternalGCpCurve Zone2n_Overhang { get; set; } = null;
        //public static ExternalGCpCurve Zone2r_Overhang { get; set; } = null;
        //public static ExternalGCpCurve Zone2e_Overhang { get; set; } = null;
        //public static ExternalGCpCurve Zone1_Overhang { get; set; } = null;
        //public static ExternalGCpCurve Zone1_prime_Overhang { get; set; } = null;
    }
}
