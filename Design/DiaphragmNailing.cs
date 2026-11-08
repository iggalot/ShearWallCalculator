using ShearWallCalculator.NDSProvisions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ShearWallCalculator.Design
{
    /// <summary>
    /// A wood structural panel diaphragm's nailing for a wind unit shear (ASD, plf): blocked by SDPWS-2015 Table 4.2A, unblocked
    /// by Table 4.2C, Column B (wind; the 2015 wind column is the 2021 v_n). The ASD allowable is the nominal value, times the
    /// specific gravity factor of footnote 2, over the ASD reduction factor 2.0 (4.2.3).
    ///
    /// The row: the sheathing grade and the nail, the thickest "minimum nominal panel thickness" not over the panel's own (a
    /// thicker panel takes the row it meets: 23/32" takes 19/32"), and the nailed face's width (2" or 3" nominal).
    /// Blocked: the widest boundary spacing whose allowable takes the shear (6, 4, 2-1/2, 2 at the boundaries; 6, 6, 4, 3 at
    /// the other panel edges); at 2-1/2" or less, 3" nominal framing at the adjoining panel edges and staggered nailing
    /// (4.2.7.1.1 item 3), so the 3" row. Unblocked: 6" at the boundaries and supported edges, the lower Cases 2-6 value (the
    /// panel layout is not in the model: Case 1 is not assumed).
    /// </summary>
    public sealed class DiaphragmNailing
    {
        public bool StructuralI { get; set; }
        public string Nail { get; set; }
        public double ThicknessIn { get; set; }
        public double FramingWidthIn { get; set; }
        public bool Blocked { get; set; }
        /// <summary>Footnote 2's specific gravity factor, 1.0 for Douglas Fir-Larch and Southern Pine.</summary>
        public double SgFactor { get; set; } = 1;
        public double ShearAsdPlf { get; set; }
        /// <summary>The table row used ("Sheathing and Single-Floor, 10d, 19/32\" min., 2\" framing").</summary>
        public string Row { get; set; }
        public double BoundaryIn { get; set; }
        /// <summary>At the other panel edges (blocked); the supported edges (unblocked).</summary>
        public double EdgesIn { get; set; }
        public double NominalPlf { get; set; }
        public double AllowablePlf => SdpwsSource.DesignUnitShearWind(NominalPlf * SgFactor, DesignMethods.ASD);
        /// <summary>A construction requirement that comes with the spacing (3" framing, staggered nailing), else null.</summary>
        public string Note { get; set; }
        /// <summary>What does not work, in a sentence (null: it works).</summary>
        public string Fails { get; set; }

        public string GradeName => StructuralI ? "Structural I" : "Sheathing and Single-Floor";

        public static readonly string[] Citations =
        {
            "AWC SDPWS-2015 4.2.3: \"The ASD allowable unit shear capacity shall be determined by dividing the tabulated nominal unit shear capacity, modified by applicable footnotes, by the ASD reduction factor of 2.0.\"",
            "AWC SDPWS-2015 Table 4.2A (blocked) and Table 4.2C (unblocked), Column B, wind; footnote 2: for framing other than Douglas-Fir-Larch or Southern Pine, × [1 − (0.5 − G)], not over 1.",
            "AWC SDPWS-2015 4.2.7.1.1 item 3: 3\" nominal framing at adjoining panel edges and staggered nailing where the spacing is 2-1/2\" o.c. or less at adjoining panel edges, or 10d nails penetrating more than 1-1/2\" at 3\" o.c. or less.",
            "AWC SDPWS-2015 4.2.7.1.3: unblocked diaphragms as 4.2.7.1.1, without blocking at panel edges; their fasteners as Table 4.2C (6\" at the boundaries and supported edges).",
            "AWC SDPWS-2015 4.1.4: boundary elements (chords, collectors) transfer the design tension and compression; the sheathing does not splice them.",
        };

        /// <summary>The nailing for <paramref name="vAsdPlf"/> (ASD, plf) of a diaphragm of this sheathing.</summary>
        public static DiaphragmNailing Design(double vAsdPlf, bool structuralI, string nail, double thicknessIn, double framingWidthIn, bool blocked, double sgFactor = 1)
        {
            var n = new DiaphragmNailing
            {
                ShearAsdPlf = Math.Abs(vAsdPlf), StructuralI = structuralI, Nail = (nail ?? "").Trim().ToLowerInvariant(), ThicknessIn = thicknessIn,
                FramingWidthIn = framingWidthIn, Blocked = blocked, SgFactor = Math.Min(1, sgFactor > 0 ? sgFactor : 1),
            };
            var size = NailSize(n.Nail);
            var grade = structuralI ? SheathingMaterials.SHEATH_MAT_STRUCTURAL_1 : SheathingMaterials.SHEATH_MAT_SHEATHING_AND_SINGLE_FLOOR;
            double width = framingWidthIn >= 3 - 1e-9 ? 3 : 2;
            if (size == null) { n.Fails = $"\"{nail}\" is not a nail of SDPWS Tables 4.2A and 4.2C (6d, 8d, 10d common)."; return n; }

            if (blocked)
            {
                var rows = new Table4_2Manager().Table4_2ANominalUnitShearCapacities.Values.Where(r => r.SheathingGrade == grade && r.CommonNailSize == size.Value).ToList();
                var t = Thickest(rows.Select(r => r.NominalPanelThickness), thicknessIn);
                if (t == null) { n.Fails = NoRow(n, "4.2A"); return n; }
                Table4_2A Row(double w) => rows.FirstOrDefault(r => r.NominalPanelThickness == t.Value && Math.Abs(r.NominalWidthOfNailedFaceAtBoundaries - w) < 1e-9);
                Table4_2_WindEntry[] Columns(Table4_2A r) => new[] { r.Wind6_6, r.Wind4_6, r.Wind2_5_4, r.Wind2_3 };
                for (int i = 0; i < 4; i++)
                {
                    bool needs3 = i >= 2; // 2-1/2" or less at adjoining panel edges (4.2.7.1.1 3a)
                    var r = Row(needs3 ? 3 : width);
                    if (r == null) continue;
                    var c = Columns(r)[i];
                    n.Set(r.NominalPanelThickness, r.NominalWidthOfNailedFaceAtBoundaries, c.NailSpacing_Boundary, c.NailSpacing_OtherPanelEdges, c.v_s);
                    n.Note = needs3 ? "3\" nominal framing at the adjoining panel edges and boundaries, the nails staggered (SDPWS 4.2.7.1.1 item 3)." : null;
                    if (n.AllowablePlf >= n.ShearAsdPlf - 1e-9) return n;
                }
                n.Fails = $"{n.ShearAsdPlf:0} plf is over Table 4.2A's most for this sheathing ({n.AllowablePlf:0} plf at {Inches(n.BoundaryIn)} o.c.): a high-load diaphragm (Table 4.2B) or a stronger sheathing.";
                return n;
            }

            var urows = new Table4_2CManager().Table4_2CNominalUnitShearCapacities.Values.Where(r => r.SheathingGrade == grade && r.CommonNailSize == size.Value).ToList();
            var ut = Thickest(urows.Select(r => r.NominalPanelThickness), thicknessIn);
            var ur = ut == null ? null : urows.FirstOrDefault(r => r.NominalPanelThickness == ut.Value && Math.Abs(r.NominalWidthOfNailedFaceAtBoundaries - width) < 1e-9);
            if (ur == null) { n.Fails = NoRow(n, "4.2C"); return n; }
            n.Set(ur.NominalPanelThickness, ur.NominalWidthOfNailedFaceAtBoundaries, 6, 6, ur.Wind6_OtherCases.v_s);
            if (n.AllowablePlf < n.ShearAsdPlf - 1e-9)
            {
                var b = Design(vAsdPlf, structuralI, nail, thicknessIn, framingWidthIn, true, sgFactor);
                n.Fails = $"{n.ShearAsdPlf:0} plf is over the unblocked {n.AllowablePlf:0} plf (Table 4.2C, Cases 2-6): blocked, " +
                          (b.Fails == null ? $"{b.Nail} at {Inches(b.BoundaryIn)} o.c. at the boundaries, {Inches(b.EdgesIn)} at the other panel edges, would take it ({b.AllowablePlf:0} plf)." : "and even blocked it does not work.");
            }
            return n;
        }

        private void Set(NominalPanelThicknesses t, double width, double boundary, double edges, double nominal)
        {
            Row = $"{GradeName}, {Nail}, {ShearWallDesigner.ThicknessName(t)} min., {width:0}\" framing";
            FramingWidthIn = Math.Max(FramingWidthIn, width);
            BoundaryIn = boundary;
            EdgesIn = edges;
            NominalPlf = nominal;
        }

        private static string NoRow(DiaphragmNailing n, string table) =>
            $"No row of SDPWS Table {table} for {n.GradeName} with {n.Nail} nails and a {n.ThicknessIn:0.###}\" panel.";

        /// <summary>The thickest of <paramref name="ts"/> not over <paramref name="inches"/> (to 1/64"), else null.</summary>
        private static NominalPanelThicknesses? Thickest(IEnumerable<NominalPanelThicknesses> ts, double inches) =>
            ts.Distinct().Where(t => InchesOf(t) <= inches + 1.0 / 64).OrderByDescending(InchesOf).Cast<NominalPanelThicknesses?>().FirstOrDefault();

        /// <summary>A nominal panel thickness in inches ("19/32\"" → 0.59375).</summary>
        public static double InchesOf(NominalPanelThicknesses t)
        {
            var p = ShearWallDesigner.ThicknessName(t).Replace("\"", "").Split('/');
            return p.Length == 2 ? double.Parse(p[0], CultureInfo.InvariantCulture) / double.Parse(p[1], CultureInfo.InvariantCulture) : 0;
        }

        private static CommonNailSizes? NailSize(string nail) =>
            nail == "6d" ? CommonNailSizes.NAILSIZE_6D : nail == "8d" ? CommonNailSizes.NAILSIZE_8D : nail == "10d" ? CommonNailSizes.NAILSIZE_10D : (CommonNailSizes?)null;

        private static string Inches(double v) => v.ToString("0.#", CultureInfo.InvariantCulture) + "\"";

        /// <summary>SDPWS Table 4.2A/4.2C footnote 2: 1 − (0.5 − G), not over 1; 1.0 for Douglas Fir-Larch and Southern Pine.</summary>
        public static double SpecificGravityFactor(double g, bool dflOrSouthernPine) => dflOrSouthernPine ? 1 : Math.Min(1, 1 - (0.5 - g));
    }
}
