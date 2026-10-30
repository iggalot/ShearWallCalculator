using System;

namespace ShearWallCalculator.NDSProvisions
{
    public enum SdpwsEditions
    {
        SDPWS_2015 = 0,   // referenced by IBC 2018 and IBC 2021 (pairs with ASCE 7-16)
        SDPWS_2021 = 1,   // referenced by IBC 2024 (pairs with ASCE 7-22)
    }

    public enum DesignMethods
    {
        ASD = 0,
        LRFD = 1,
    }

    /// <summary>
    /// Where the SDPWS tables in this folder came from, and the edition-dependent factors applied to them.
    ///
    /// Source of the typed tables (deduced 2026-10-30; see docs/SDPWS_EDITION.md):
    /// - Tables 4.2A-C and 4.3A-C have separate seismic (v_s, with G_a) and wind (v_w) columns, with
    ///   v_w = 1.4 v_s. That is the SDPWS 2015 (and 2008) layout. SDPWS 2021 lists a single nominal value v_n.
    /// - The wind column equals the 2021 v_n. For example, Table 4.3A 15/32" sheathing-grade panel, 8d @ 2" o.c.:
    ///   v_w = 1790 plf here, and v_n = 1790 plf in a published SDPWS 2021 example (WoodWorks).
    /// So the tables were most likely typed from SDPWS 2015. For WIND design the wind column can be used
    /// for either edition; only the factors below change.
    /// </summary>
    public static class SdpwsSource
    {
        public const string TableSource =
            "SDPWS tables typed from the 2015 edition (separate seismic/wind columns). " +
            "The wind column equals the SDPWS 2021 nominal value v_n, so it serves both editions for wind design.";

        public static string EditionName(SdpwsEditions edition) =>
            edition == SdpwsEditions.SDPWS_2021 ? "AWC SDPWS-2021" : "AWC SDPWS-2015";

        /// <summary>The SDPWS edition referenced by the building code that also references the ASCE 7 edition.</summary>
        public static SdpwsEditions DefaultFor(bool asce7_22) =>
            asce7_22 ? SdpwsEditions.SDPWS_2021 : SdpwsEditions.SDPWS_2015;

        /// <summary>ASD reduction factor for wind, Omega_D = 2.0 (SDPWS 2015 4.3.3; SDPWS 2021 4.1.4).</summary>
        public const double OmegaWind = 2.0;

        /// <summary>LRFD resistance factor for wind, phi_D = 0.80 (SDPWS 2015 4.3.3; SDPWS 2021 4.1.4).</summary>
        public const double PhiWind = 0.80;

        /// <summary>Design unit shear for wind from a tabulated nominal (wind) value, plf.</summary>
        public static double DesignUnitShearWind(double nominal, DesignMethods method) =>
            method == DesignMethods.ASD ? nominal / OmegaWind : nominal * PhiWind;

        /// <summary>
        /// Strength aspect-ratio factor for wood structural panel shear walls with h/b over 2:1 (1.0 at 2:1 or less):
        /// 1.25 - 0.125 h/b. It came in with SDPWS 2015 (2008 used 2b/h) and is kept in 2021; 0.81 at 3.5:1.
        /// Walls over the Table 4.3.4 maximum (3.5:1 blocked) are not permitted; that is checked separately.
        /// </summary>
        public static double AspectRatioFactor(double height, double length)
        {
            double ratio = Ratio(height, length);
            return ratio <= 2.0 ? 1.0 : 1.25 - 0.125 * ratio;
        }

        /// <summary>
        /// 2b/h: the deflection-compatibility factor used when segments of different aspect ratios in one line are
        /// combined (SDPWS 2015/2021 4.3.3.4.1). It is an alternative to the strength factor, never applied with it.
        /// </summary>
        public static double CompatibilityFactor(double height, double length)
        {
            double ratio = Ratio(height, length);
            return ratio <= 2.0 ? 1.0 : 2.0 / ratio;
        }

        /// <summary>
        /// SDPWS 2021 Table 4.3A footnote 10: 0.92 on the nominal value where 10d common nails are used and the
        /// hold-down is on the inside face of the end post. Not in SDPWS 2015.
        /// </summary>
        public static double InsideFaceHoldownFactor(SdpwsEditions edition, bool tenPennyNails, bool holdownOnInsideFace) =>
            edition == SdpwsEditions.SDPWS_2021 && tenPennyNails && holdownOnInsideFace ? 0.92 : 1.0;

        private static double Ratio(double height, double length)
        {
            if (height <= 0 || length <= 0) throw new ArgumentException("Wall height and length must be positive.");
            return height / length;
        }
    }
}
