using calculator;
using ShearWallCalculator.NDSProvisions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ShearWallCalculator.Design
{
    /// <summary>The SDPWS table a sheathing choice comes from.</summary>
    public enum SheathingTable
    {
        None = 0,
        WoodPanels = 1,       // Table 4.3A: wood structural panels, plywood siding, particleboard, structural fiberboard
        WoodOverGypsum = 2,   // Table 4.3B: wood structural panels applied over 1/2" or 5/8" gypsum
        GypsumAndPlaster = 3, // Table 4.3C: gypsum wallboard and sheathing, gypsum lath and plaster, cement plaster
    }

    /// <summary>
    /// One face of a wall: the table row and, for Tables 4.3A and 4.3B, the panel edge fastener spacing. Saved with the
    /// wall. Table 4.3A and 4.3B rows are found by material, thickness and nail; Table 4.3C rows by their number.
    /// </summary>
    public sealed class FaceSheathing
    {
        public SheathingTable Table { get; set; } = SheathingTable.WoodPanels;
        public SheathingMaterials Material { get; set; } = SheathingMaterials.SHEATH_MAT_STRUCTURAL_1;
        public NominalPanelThicknesses Thickness { get; set; } = NominalPanelThicknesses.THICKNESS_15_32IN;
        public CommonNailSizes Nail { get; set; } = CommonNailSizes.NAILSIZE_8D;

        /// <summary>Panel edge fastener spacing (in.), Tables 4.3A and 4.3B: 6, 4, 3 or 2.</summary>
        public int EdgeSpacing { get; set; } = 6;

        /// <summary>Table 4.3C row number (the fastener spacing, stud spacing and blocking are part of the row).</summary>
        public int Row { get; set; }

        public FaceSheathing Clone() => (FaceSheathing)MemberwiseClone();

        /// <summary>The same table row and nailing (SDPWS 4.3.5.4.1: same material and construction on both faces).</summary>
        public bool SameConstructionAs(FaceSheathing o) =>
            o != null && Table == o.Table && Material == o.Material && Thickness == o.Thickness && Nail == o.Nail &&
            (Table == SheathingTable.GypsumAndPlaster ? Row == o.Row : EdgeSpacing == o.EdgeSpacing);
    }

    /// <summary>A sheathing choice offered for a face: one row of Table 4.3A, 4.3B or 4.3C.</summary>
    public sealed class SheathingOption
    {
        public SheathingTable Table { get; set; }
        public SheathingMaterials Material { get; set; }
        public NominalPanelThicknesses Thickness { get; set; }
        public CommonNailSizes Nail { get; set; }
        public int Row { get; set; }                 // Table 4.3C row number; 0 otherwise
        public string Name { get; set; }             // short, for the table
        public string Description { get; set; }     // full, for tooltips and the report
        public string Fasteners { get; set; }

        /// <summary>Wind nominal values (plf) by edge spacing 6/4/3/2 (Tables 4.3A/B), or the single value (Table 4.3C, key 0).</summary>
        public Dictionary<int, double> Wind { get; set; } = new Dictionary<int, double>();

        /// <summary>For Table 4.3C the row's own fastener spacing (in.), as printed.</summary>
        public string RowSpacing { get; set; }

        public bool UsesEdgeSpacing => Table == SheathingTable.WoodPanels || Table == SheathingTable.WoodOverGypsum;

        /// <summary>Tabulated nominal wind value v_w (plf); NaN when the table has none for this spacing.</summary>
        public double NominalWind(int edgeSpacing)
        {
            int key = UsesEdgeSpacing ? edgeSpacing : 0;
            return Wind.TryGetValue(key, out double v) && v > 0 ? v : double.NaN;
        }

        public bool Matches(FaceSheathing f) =>
            f != null && f.Table == Table && f.Material == Material && f.Thickness == Thickness &&
            (Table == SheathingTable.GypsumAndPlaster ? f.Row == Row : f.Nail == Nail);

        public FaceSheathing ToFace(int edgeSpacing = 6) => new FaceSheathing
        {
            Table = Table, Material = Material, Thickness = Thickness, Nail = Nail, Row = Row, EdgeSpacing = UsesEdgeSpacing ? edgeSpacing : 0,
        };

        /// <summary>Wood structural panels (Structural I, sheathing grade, plywood siding), in Table 4.3A or 4.3B.</summary>
        public bool IsWoodStructuralPanel =>
            (Table == SheathingTable.WoodPanels || Table == SheathingTable.WoodOverGypsum) &&
            (Material == SheathingMaterials.SHEATH_MAT_STRUCTURAL_1 || Material == SheathingMaterials.SHEATH_MAT_SHEATHING_AND_SINGLE_FLOOR ||
             Material == SheathingMaterials.SHEATH_MAT_PLYWOOD);

        public bool IsStructuralFiberboard => Table == SheathingTable.WoodPanels && Material == SheathingMaterials.SHEATH_MAT_FIBERBOARD;

        public bool IsGypsumWallboard => Table == SheathingTable.GypsumAndPlaster && Material == SheathingMaterials.SHEATH_MAT_GYPSUM_WALLBOARD;

        /// <summary>Table 4.3C rows marked blocked (unblocked gypsum has its own row values).</summary>
        public bool Blocked { get; set; } = true;

        public override string ToString() => Name;
    }

    /// <summary>One face's share of the wall capacity (for the details and the report).</summary>
    public sealed class FaceResult
    {
        public SheathingOption Option { get; set; }
        public FaceSheathing Face { get; set; }
        public double Tabulated { get; set; }        // v_w from the table, plf
        public double SpeciesFactor { get; set; } = 1;
        public double InsideFaceFactor { get; set; } = 1;
        public double AspectFactor { get; set; } = 1;
        public double MaxAspectRatio { get; set; }
        public bool AspectOk { get; set; } = true;   // h/b within this material's Table 4.3.4 limit
        /// <summary>Nominal value used for this face: tabulated × factors, 0 when the face cannot count.</summary>
        public double Nominal { get; set; }
        public string Note { get; set; }
    }

    /// <summary>
    /// The sheathing rows offered for design and the rules for one or two faces (SDPWS 2015/2021):
    /// each face's nominal value from its table with its factors; a face whose material is over its Table 4.3.4 aspect
    /// ratio does not count; two faces of the same construction give twice one face (2021 4.3.5.4.1); two different
    /// materials give the larger of the stronger face and twice the weaker (2021 4.3.5.4.2), except that for wind a wood
    /// structural panel or structural fiberboard face may be added to a gypsum wallboard face when the project allows it.
    /// </summary>
    public static class SheathingRules
    {
        /// <summary>Every row of Tables 4.3A, 4.3B and 4.3C as a face choice (duplicate 4.3C rows listed once).</summary>
        public static List<SheathingOption> AllOptions(Table4_3Manager a = null, Table4_3BManager b = null, Table4_3CManager c = null)
        {
            a = a ?? new Table4_3Manager();
            b = b ?? new Table4_3BManager();
            c = c ?? new Table4_3CManager();
            var list = new List<SheathingOption>();

            foreach (var r in a.Table4_3ANominalUnitShearCapacities.Values)
            {
                var o = new SheathingOption
                {
                    Table = SheathingTable.WoodPanels, Material = r.SheathingType, Thickness = r.NominalPanelThickness, Nail = r.CommonNailSize,
                    Fasteners = ShearWallDesigner.NailName(r.CommonNailSize),
                };
                Put(o.Wind, 6, r.Wind6?.v_s); Put(o.Wind, 4, r.Wind4?.v_s); Put(o.Wind, 3, r.Wind3?.v_s); Put(o.Wind, 2, r.Wind2?.v_s);
                o.Name = $"{ShortMaterial(o.Material)} {ShearWallDesigner.ThicknessName(o.Thickness)}, {Nails(o.Nail)}";
                o.Description = $"{ShearWallDesigner.MaterialName(o.Material)} {ShearWallDesigner.ThicknessName(o.Thickness)}, {o.Fasteners} (Table 4.3A)";
                list.Add(o);
            }

            foreach (var r in b.Table4_3BNominalUnitShearCapacities.Values)
            {
                // the plywood panel siding rows are typed as sheathing grade with the casing nail in the note
                bool siding = !string.IsNullOrWhiteSpace(r.Note);
                var o = new SheathingOption
                {
                    Table = SheathingTable.WoodOverGypsum, Material = siding ? SheathingMaterials.SHEATH_MAT_PLYWOOD : r.SheathingType,
                    Thickness = r.NominalPanelThickness, Nail = r.CommonNailSize,
                    Fasteners = ShearWallDesigner.NailName(r.CommonNailSize) + (siding ? " galvanized casing" : ""),
                };
                Put(o.Wind, 6, r.Wind6?.v_s); Put(o.Wind, 4, r.Wind4?.v_s); Put(o.Wind, 3, r.Wind3?.v_s); Put(o.Wind, 2, r.Wind2?.v_s);
                o.Name = $"{ShortMaterial(o.Material)} {ShearWallDesigner.ThicknessName(o.Thickness)}, {Nails(o.Nail)} over gyp.";
                o.Description = $"{ShearWallDesigner.MaterialName(o.Material)} {ShearWallDesigner.ThicknessName(o.Thickness)}, {o.Fasteners}, over 1/2\" or 5/8\" gypsum (Table 4.3B)";
                list.Add(o);
            }

            var seen = new HashSet<string>();
            foreach (var kv in c.Table4_3CNominalUnitShearCapacities.OrderBy(k => k.Key))
            {
                var r = kv.Value;
                string spacing = (r.MaxFastenerEdgeSpacing ?? "").Trim();
                bool blocked = (r.Note ?? "").Trim().Equals("blocked", StringComparison.OrdinalIgnoreCase);
                string fast = string.Join("; ", new[] { r.FastenerTypeAndSize1, r.FastenerTypeAndSize2, r.FastenerTypeAndSize3 }.Where(s => !string.IsNullOrWhiteSpace(s)));
                string sig = $"{r.SheathingType}|{r.NominalPanelThickness}|{spacing}|{r.MaxStudSpacing}|{blocked}|{r.Wind?.v_s}|{fast}";
                if (!seen.Add(sig)) continue; // the table has a repeated row
                var o = new SheathingOption
                {
                    Table = SheathingTable.GypsumAndPlaster, Material = r.SheathingType, Thickness = r.NominalPanelThickness, Row = kv.Key,
                    RowSpacing = spacing, Blocked = blocked, Fasteners = fast,
                };
                Put(o.Wind, 0, r.Wind?.v_s);
                string thick = r.NominalPanelThickness == NominalPanelThicknesses.THICKNESS_5_8IN_2PLY ? "5/8\" 2-ply" : ShearWallDesigner.ThicknessName(r.NominalPanelThickness);
                o.Name = $"{ShortMaterial(o.Material)} {thick}, {SpacingText(spacing)}/{r.MaxStudSpacing:0} {(blocked ? "blk" : "unblk")}";
                o.Description = $"{ShearWallDesigner.MaterialName(o.Material)} {thick}, fasteners at {SpacingText(spacing)} edge, studs {r.MaxStudSpacing:0}\" o.c., " +
                                $"{(blocked ? "blocked" : "unblocked")} ({fast}) (Table 4.3C row {kv.Key})";
                list.Add(o);
            }
            return list;
        }

        private static void Put(Dictionary<int, double> d, int key, double? v) { if (v.HasValue && v.Value > 0) d[key] = v.Value; }

        private static string SpacingText(string s)
        {
            if (string.IsNullOrEmpty(s)) return "?";
            if (s.All(ch => char.IsDigit(ch) || ch == '.')) return s + "\"";
            return s.Replace("\n", " ");
        }

        private static string Nails(CommonNailSizes n)
        {
            string s = ShearWallDesigner.NailName(n);
            return s.Length > 14 ? "11 ga" : s;
        }

        public static string ShortMaterial(SheathingMaterials m)
        {
            switch (m)
            {
                case SheathingMaterials.SHEATH_MAT_STRUCTURAL_1: return "Structural I";
                case SheathingMaterials.SHEATH_MAT_SHEATHING_AND_SINGLE_FLOOR: return "Sheathing";
                case SheathingMaterials.SHEATH_MAT_PLYWOOD: return "Plywood siding";
                case SheathingMaterials.SHEATH_MAT_PARTICLEBOARD: return "Particleboard";
                case SheathingMaterials.SHEATH_MAT_FIBERBOARD: return "Fiberboard";
                case SheathingMaterials.SHEATH_MAT_GYPSUM_WALLBOARD: return "Gyp. wallboard";
                case SheathingMaterials.SHEATH_MAT_GYPSUM_SHEATHING_BOARD: return "Gyp. sheathing";
                case SheathingMaterials.SHEATH_MAT_GYPSUM_LATH_PLASTER: return "Gyp. lath + plaster";
                case SheathingMaterials.SHEATH_MAT_CEMENT_PLASTER_OVER_METAL: return "Cement plaster";
                default: return m.ToString();
            }
        }

        /// <summary>Table 4.3.4 maximum h/b for a face's material.</summary>
        public static double MaxAspectRatio(SheathingOption o)
        {
            if (o.IsWoodStructuralPanel) return SDPWS_Table4_3_4.GetMaxAspectRatio(ShearWallSheathingTypes.SheathingTypes_WOOD_BLOCKED);
            if (o.IsStructuralFiberboard) return SDPWS_Table4_3_4.GetMaxAspectRatio(ShearWallSheathingTypes.SheathingTypes_STRUCTURAL_FIBERBOARD);
            if (o.Material == SheathingMaterials.SHEATH_MAT_PARTICLEBOARD) return SDPWS_Table4_3_4.GetMaxAspectRatio(ShearWallSheathingTypes.SheathingTypes_PARTICLEBOARD);
            if (o.Material == SheathingMaterials.SHEATH_MAT_CEMENT_PLASTER_OVER_METAL) return SDPWS_Table4_3_4.GetMaxAspectRatio(ShearWallSheathingTypes.SheathingTypes_PORTLAND_CEMENT_PLASTER);
            return SDPWS_Table4_3_4.GetMaxAspectRatio(ShearWallSheathingTypes.SheathingTypes_GYPSUM_WALLBOARD);
        }

        /// <summary>
        /// Strength aspect-ratio factor of a face: wood structural panels 1.25 − 0.125 h/b over 2:1; structural fiberboard
        /// 1.09 − 0.09 h/b over 1:1 (SDPWS 2015/2021 4.3.4); 1.0 for the others (their limit is 2:1).
        /// </summary>
        public static double AspectFactor(SheathingOption o, double h, double L)
        {
            if (!(h > 0) || !(L > 0)) return 0;
            double ratio = h / L;
            if (o.IsWoodStructuralPanel) return SdpwsSource.AspectRatioFactor(h, L);
            if (o.IsStructuralFiberboard) return ratio <= 1.0 ? 1.0 : 1.09 - 0.09 * ratio;
            return 1.0;
        }

        /// <summary>Wind exception: WSP or structural fiberboard on one face, gypsum wallboard on the other.</summary>
        public static bool MaySum(SheathingOption a, SheathingOption b) =>
            (a.IsWoodStructuralPanel || a.IsStructuralFiberboard) && b.IsGypsumWallboard ||
            (b.IsWoodStructuralPanel || b.IsStructuralFiberboard) && a.IsGypsumWallboard;

        /// <summary>
        /// The wall's nominal unit shear from one or two faces, and the rule used. <paramref name="sameConstruction"/>:
        /// both faces are the same row and nailing.
        /// </summary>
        public static (double nominal, string rule) Combine(FaceResult f1, FaceResult f2, bool sameConstruction, bool sumForWind)
        {
            if (f2 == null) return (f1?.Nominal ?? double.NaN, "one face");
            if (f1 == null) return (f2.Nominal, "one face");
            double a = f1.Nominal, b = f2.Nominal;
            if (double.IsNaN(a) || double.IsNaN(b)) return (double.NaN, "no table value");
            if (sameConstruction) return (a + b, "same material both faces: 2 × one face (SDPWS 2021 4.3.5.4.1)");
            if (sumForWind && MaySum(f1.Option, f2.Option) && f1.Nominal > 0 && f2.Nominal > 0)
                return (a + b, "wood panel + gypsum wallboard, added for wind (SDPWS 4.3.5.4.2 exception; project setting)");
            double strong = Math.Max(a, b), weak = Math.Min(a, b);
            return strong >= 2 * weak
                ? (strong, "different materials: larger of the stronger face and 2 × the weaker (SDPWS 2021 4.3.5.4.2) → stronger face")
                : (2 * weak, "different materials: larger of the stronger face and 2 × the weaker (SDPWS 2021 4.3.5.4.2) → 2 × weaker face");
        }
    }
}
