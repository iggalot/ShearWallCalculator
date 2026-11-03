namespace ShearWallCalculator.Design
{
    /// <summary>
    /// A shear wall's own framing rules (SDPWS-2021). The wall's framing itself (pieces, studs, sill bolts) is the
    /// WoodFraming library's, for every wall of the building model.
    /// </summary>
    public static class ShearWallFraming
    {
        public const string Citation =
            "SDPWS-2021 4.3.6.1: framing and blocking \"2\" nominal or greater\"; end posts carry the chord forces. 4.3.7.1 item 5: \"the width of the nailed face of a common framing member or blocking at adjoining panel edges shall be 3\" nominal or greater and nailing shall be staggered\" where nails are at 2\" o.c., or 10d at 3\" o.c. or less (or > 980 plf in SDC D–F); two 2x fastened per 4.3.6.1.1 may be used instead. Item 6: \"Maximum stud spacing shall be 24\" on center.\"";

        /// <summary>SDPWS-2021 4.3.7.1 item 6: studs at 24" on centre at most.</summary>
        public const double MaxStudSpacingIn = 24;

        /// <summary>
        /// SDPWS-2021 4.3.7.1 item 5 (wood structural panels): a 3x nominal (or two 2x fastened per 4.3.6.1.1) nailed face
        /// at adjoining panel edges, with staggered nailing, where nails are at 2" o.c., or 10d at 3" o.c. or less.
        /// </summary>
        public static bool WideFaceAtPanelJoints(int edgeSpacingIn, bool tenPenny) => edgeSpacingIn <= 2 || (tenPenny && edgeSpacingIn <= 3);
    }
}
