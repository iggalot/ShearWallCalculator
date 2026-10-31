using calculator;
using ShearWallCalculator.NDSProvisions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ShearWallCalculator.Design
{
    /// <summary>What anchors the end of the wall against overturning.</summary>
    public enum HoldownFamilies
    {
        HDU_ToFoundation = 0,     // HDU / DTT hold-downs anchored to the foundation
        HTT_ToFoundation = 1,     // HTT tension ties anchored to the foundation
        Strap_ToWallBelow = 2,    // strap ties to the wall below
    }

    /// <summary>Settings for the whole project.</summary>
    public class DesignSettings
    {
        public DesignMethods Method { get; set; } = DesignMethods.ASD;
        public SdpwsEditions Edition { get; set; } = SdpwsEditions.SDPWS_2021;
    }

    /// <summary>The design choices for one wall (saved with the wall). Defaults: 15/32" Structural I, 8d at 6" o.c., one side.</summary>
    public class WallDesignInputs
    {
        public SheathingMaterials Sheathing { get; set; } = SheathingMaterials.SHEATH_MAT_STRUCTURAL_1;
        public NominalPanelThicknesses Thickness { get; set; } = NominalPanelThicknesses.THICKNESS_15_32IN;
        public CommonNailSizes Nail { get; set; } = CommonNailSizes.NAILSIZE_8D;

        /// <summary>Panel edge nail spacing (in.): 6, 4, 3 or 2.</summary>
        public int EdgeSpacing { get; set; } = 6;

        /// <summary>1 or 2 sheathed faces (same panel and nailing on both).</summary>
        public int Sides { get; set; } = 1;

        public WoodTypes Species { get; set; } = WoodTypes.WOODTYPE_DF_SP;

        /// <summary>Dead load on top of the wall (plf) that resists overturning. 0 = none counted.</summary>
        public double DeadLoad { get; set; }

        public HoldownFamilies Holdown { get; set; } = HoldownFamilies.HDU_ToFoundation;

        /// <summary>Hold-down on the inside face of the end post (SDPWS 2021 Table 4.3A footnote 10, with 10d nails).</summary>
        public bool HoldownOnInsideFace { get; set; } = true;

        public WallDesignInputs Clone() => (WallDesignInputs)MemberwiseClone();
    }

    /// <summary>One wood structural panel choice from SDPWS Table 4.3A.</summary>
    public sealed class PanelOption
    {
        public PanelOption(Table4_3A row) { Row = row; }

        public Table4_3A Row { get; }
        public SheathingMaterials Sheathing => Row.SheathingType;
        public NominalPanelThicknesses Thickness => Row.NominalPanelThickness;
        public CommonNailSizes Nail => Row.CommonNailSize;

        public string Name => $"{ShearWallDesigner.MaterialName(Sheathing)} {ShearWallDesigner.ThicknessName(Thickness)}, {ShearWallDesigner.NailName(Nail)}";

        /// <summary>Tabulated nominal wind capacity v_w (plf) at this edge spacing; NaN if the table has none.</summary>
        public double NominalWind(int edgeSpacing)
        {
            Table4_3_WindEntry e;
            switch (edgeSpacing)
            {
                case 6: e = Row.Wind6; break;
                case 4: e = Row.Wind4; break;
                case 3: e = Row.Wind3; break;
                case 2: e = Row.Wind2; break;
                default: e = null; break;
            }
            return e != null && e.v_s > 0 ? e.v_s : double.NaN;
        }

        public override string ToString() => Name;
    }

    /// <summary>The selected hold-down: model, fastener schedule and allowable tension (lb, ASD).</summary>
    /// <summary>One hold-down model with one fastener option, as stored in the catalog (inches, pounds; ASD allowable tension).</summary>
    public sealed class HoldownSpecRow
    {
        public HoldownFamilies Family { get; set; }
        public string Model { get; set; }
        public bool FirstOfModel { get; set; }
        public int Gauge { get; set; }
        public double W { get; set; }
        /// <summary>H for HDU/DTT, L for HTT and straps.</summary>
        public double HeightOrLength { get; set; }
        public double? B { get; set; }
        public double? CL { get; set; }
        public double? SO { get; set; }
        public double? AnchorBoltDia { get; set; }
        public string CodeRef { get; set; }
        public string Fasteners { get; set; }
        public double FastenerLength { get; set; }
        public double? WoodMinHeight { get; set; }
        public double? WoodMinWidth { get; set; }
        public double? AllowableDF_SP { get; set; }
        public double? AllowableSPF_HF { get; set; }
        public double? DeflectionAtAllowable { get; set; }
    }

    public sealed class HoldownChoice
    {
        public string Model { get; set; }
        public string Fasteners { get; set; }
        public double Allowable { get; set; }
        public override string ToString() => string.IsNullOrEmpty(Fasteners) ? Model : $"{Model} ({Fasteners})";
    }

    /// <summary>All the numbers for one wall, with the code reference for each step.</summary>
    public class WallDesignResult
    {
        public int WallId { get; set; }
        public string GoverningCase { get; set; }
        public DesignMethods Method { get; set; }
        public SdpwsEditions Edition { get; set; }
        public WallDesignInputs Inputs { get; set; }
        public string Panel { get; set; }

        public double Length { get; set; }          // ft
        public double Height { get; set; }          // ft
        public double V { get; set; }               // strength-level wind shear on the wall, kips
        public double LoadFactor { get; set; }      // 0.6 (ASD) or 1.0 (LRFD)

        public double AspectRatio { get; set; }     // h/b
        public double MaxAspectRatio { get; set; }  // Table 4.3.4
        public bool AspectOk => AspectRatio <= MaxAspectRatio + 1e-9;
        public double AspectFactor { get; set; }    // 1.25 - 0.125 h/b above 2:1

        public double Demand { get; set; }          // v = factor x V / L, plf
        public double Nominal { get; set; }         // tabulated v_w for one side, plf
        public double SpeciesFactor { get; set; }   // Table 4.3A footnote: 1 - (0.5 - G)
        public double InsideFaceFactor { get; set; }// 2021 footnote 10
        public double Capacity { get; set; }        // design capacity, plf (all sides)
        public double ShearRatio => Capacity > 0 ? Demand / Capacity : double.PositiveInfinity;

        public double TensionAsd { get; set; }      // lb: 0.6 V h / L - 0.6 D L / 2
        public double TensionLrfd { get; set; }     // lb: 1.0 V h / L - 0.9 D L / 2 (for reference)
        public HoldownChoice Holdown { get; set; }  // null if none in the catalog is strong enough
        public bool HoldownNeeded => TensionAsd > 0;
        public double HoldownRatio => !HoldownNeeded ? 0 : Holdown == null ? double.PositiveInfinity : TensionAsd / Holdown.Allowable;

        public bool ShearOk => !double.IsNaN(Capacity) && ShearRatio <= 1.0 + 1e-9;
        public bool HoldownOk => !HoldownNeeded || Holdown != null;
        public bool Pass => AspectOk && ShearOk && HoldownOk;

        /// <summary>Short reason when the wall does not pass.</summary>
        public string Status =>
            !AspectOk ? $"h/b {AspectRatio:0.00} > {MaxAspectRatio:0.0} not allowed"
            : double.IsNaN(Capacity) ? "no table value for this nailing"
            : !ShearOk ? $"shear {ShearRatio:0.00} > 1"
            : !HoldownOk ? "no hold-down strong enough"
            : "OK";
    }

    /// <summary>
    /// Wood structural panel shear-wall checks for wind (SDPWS), per wall segment (each drawn wall is one
    /// full-height segment):
    /// v = factor V / L; capacity = v_w x species x footnote-10 x aspect factor x sides / Omega (ASD) or x phi (LRFD);
    /// hold-down tension T = M / L = V h / L (less dead load), always at ASD (0.6W) against Simpson allowables.
    /// </summary>
    public class ShearWallDesigner
    {
        private readonly List<PanelOption> panels;
        private readonly SimpsonCatalog catalog;

        public ShearWallDesigner(Table4_3Manager table = null, SimpsonCatalog catalog = null)
        {
            table = table ?? new Table4_3Manager();
            this.catalog = catalog ?? new SimpsonCatalog();
            panels = table.Table4_3ANominalUnitShearCapacities.Values
                .Where(r => r.SheathingType == SheathingMaterials.SHEATH_MAT_STRUCTURAL_1 ||
                            r.SheathingType == SheathingMaterials.SHEATH_MAT_SHEATHING_AND_SINGLE_FLOOR)
                .Select(r => new PanelOption(r))
                .ToList();
        }

        /// <summary>The wood structural panel rows of Table 4.3A offered for design.</summary>
        public IReadOnlyList<PanelOption> Panels => panels;

        public static readonly int[] EdgeSpacings = { 6, 4, 3, 2 };

        public PanelOption FindPanel(WallDesignInputs d) =>
            panels.FirstOrDefault(p => p.Sheathing == d.Sheathing && p.Thickness == d.Thickness && p.Nail == d.Nail);

        /// <summary>Table 4.3A footnote: framing other than Douglas fir-larch or southern pine, 1 - (0.5 - G); SPF G = 0.42.</summary>
        public static double SpeciesFactor(WoodTypes species) => species == WoodTypes.WOODTYPE_SPF_HF ? 1 - (0.5 - 0.42) : 1.0;

        public WallDesignResult Check(int wallId, WallData wall, double vKips, string governingCase, DesignSettings settings, WallDesignInputs d = null)
        {
            d = d ?? wall.Design ?? new WallDesignInputs();
            settings = settings ?? new DesignSettings();
            var panel = FindPanel(d);
            double L = wall.WallLength, h = wall.WallHeight, V = Math.Abs(vKips);
            double factor = settings.Method == DesignMethods.ASD ? 0.6 : 1.0;

            var r = new WallDesignResult
            {
                WallId = wallId,
                GoverningCase = governingCase,
                Method = settings.Method,
                Edition = settings.Edition,
                Inputs = d.Clone(),
                Panel = panel?.Name ?? "(not in Table 4.3A)",
                Length = L,
                Height = h,
                V = V,
                LoadFactor = factor,
                AspectRatio = L > 0 ? h / L : double.PositiveInfinity,
                MaxAspectRatio = SDPWS_Table4_3_4.GetMaxAspectRatio(ShearWallSheathingTypes.SheathingTypes_WOOD_BLOCKED),
            };
            r.AspectFactor = L > 0 && h > 0 ? SdpwsSource.AspectRatioFactor(h, L) : 0;
            r.Demand = L > 0 ? factor * V * 1000.0 / L : double.PositiveInfinity;

            r.Nominal = panel?.NominalWind(d.EdgeSpacing) ?? double.NaN;
            r.SpeciesFactor = SpeciesFactor(d.Species);
            r.InsideFaceFactor = SdpwsSource.InsideFaceHoldownFactor(settings.Edition, d.Nail == CommonNailSizes.NAILSIZE_10D, d.HoldownOnInsideFace);
            int sides = d.Sides == 2 ? 2 : 1;
            r.Capacity = SdpwsSource.DesignUnitShearWind(r.Nominal * r.SpeciesFactor * r.InsideFaceFactor * r.AspectFactor * sides, settings.Method);

            // Hold-down: the moment of the shear at the top of the wall over the wall length (M / L), less dead load.
            double M = V * 1000.0 * h;                      // lb-ft
            double D = Math.Max(0, d.DeadLoad) * L;         // lb on the wall
            r.TensionAsd = L > 0 ? Math.Max(0, 0.6 * M / L - 0.6 * D / 2) : double.PositiveInfinity;
            r.TensionLrfd = L > 0 ? Math.Max(0, 1.0 * M / L - 0.9 * D / 2) : double.PositiveInfinity;
            r.Holdown = r.HoldownNeeded ? SelectHoldown(d.Holdown, d.Species, r.TensionAsd) : null;
            return r;
        }

        /// <summary>The lightest catalog option (smallest allowable tension) that is at least the required tension.</summary>
        public HoldownChoice SelectHoldown(HoldownFamilies family, WoodTypes species, double requiredLb)
        {
            string key = species == WoodTypes.WOODTYPE_SPF_HF ? "SPF_HF" : "DF_SP";
            return HoldownOptions(family, key)
                .Where(o => o.Allowable >= requiredLb)
                .OrderBy(o => o.Allowable)
                .FirstOrDefault();
        }

        /// <summary>
        /// Every hold-down in the catalog this designer selects from, one row per model and fastener option, with all the
        /// stored data. It reads the same managers as <see cref="SelectHoldown"/>, so the values shown are the values used.
        /// </summary>
        public List<HoldownSpecRow> CatalogRows()
        {
            var rows = new List<HoldownSpecRow>();
            void Add(HoldownFamilies family, string model, int gauge, double w, double hl, double? b, double? cl, double? so, double? bolt, string code,
                     int qty, string type, string size, double length, double? minH, double? minW, IDictionary<string, double> loads, double? defl, bool first)
            {
                rows.Add(new HoldownSpecRow
                {
                    Family = family, Model = model, FirstOfModel = first, Gauge = gauge, W = w, HeightOrLength = hl, B = b, CL = cl, SO = so,
                    AnchorBoltDia = bolt, CodeRef = code, Fasteners = Fasteners(qty, type, size), FastenerLength = length,
                    WoodMinHeight = minH, WoodMinWidth = minW, DeflectionAtAllowable = defl,
                    AllowableDF_SP = loads.TryGetValue("DF_SP", out var df) ? df : (double?)null,
                    AllowableSPF_HF = loads.TryGetValue("SPF_HF", out var spf) ? spf : (double?)null,
                });
            }
            foreach (var m in catalog.HDUManager.HDU_Dict.Values)
                for (int i = 0; i < m.FastenerOptions.Count; i++)
                {
                    var f = m.FastenerOptions[i];
                    Add(HoldownFamilies.HDU_ToFoundation, m.Model, m.Gauge, m.W, m.H, m.B, m.CL, m.SO, m.AnchorBoltDia, m.CodeRef,
                        f.Screws_Quantity, f.Screws_Type, f.ScrewSize, f.Length, f.WoodMinHeight, f.WoodMinWidth, f.AllowableTensionLoads, f.DeflectionAtAllowableLoad, i == 0);
                }
            foreach (var m in catalog.HTTManager.HTT_Dict.Values)
                for (int i = 0; i < m.FastenerOptions.Count; i++)
                {
                    var f = m.FastenerOptions[i];
                    Add(HoldownFamilies.HTT_ToFoundation, m.Model, m.Gauge, m.W, m.L, m.B, m.CL, m.SO, m.AnchorBoltDia, m.CodeRef,
                        f.Screws_Quantity, f.Screws_Type, f.ScrewSize, f.Length, f.WoodMinHeight, f.WoodMinWidth, f.AllowableTensionLoads, f.DeflectionAtAllowableLoad, i == 0);
                }
            foreach (var m in catalog.StrapTiesManager.StrapTies_Dict.Values)
                for (int i = 0; i < m.FastenerOptions.Count; i++)
                {
                    var f = m.FastenerOptions[i];
                    Add(HoldownFamilies.Strap_ToWallBelow, m.Model, m.Gauge, m.W, m.L, null, null, null, null, m.CodeRef,
                        f.Screws_Quantity, f.Screws_Type, f.ScrewSize, f.Length, null, null, f.AllowableTensionLoads, null, i == 0);
                }
            return rows;
        }

        /// <summary>The options <see cref="SelectHoldown"/> chooses from (for checks against <see cref="CatalogRows"/>).</summary>
        public IEnumerable<HoldownChoice> Options(HoldownFamilies family, WoodTypes species) =>
            HoldownOptions(family, species == WoodTypes.WOODTYPE_SPF_HF ? "SPF_HF" : "DF_SP");

        private IEnumerable<HoldownChoice> HoldownOptions(HoldownFamilies family, string species)
        {
            switch (family)
            {
                case HoldownFamilies.HDU_ToFoundation:
                    foreach (var m in catalog.HDUManager.HDU_Dict.Values)
                        foreach (var f in m.FastenerOptions)
                            if (f.AllowableTensionLoads.TryGetValue(species, out var a))
                                yield return new HoldownChoice { Model = m.Model, Fasteners = Fasteners(f.Screws_Quantity, f.Screws_Type, f.ScrewSize), Allowable = a };
                    break;
                case HoldownFamilies.HTT_ToFoundation:
                    foreach (var m in catalog.HTTManager.HTT_Dict.Values)
                        foreach (var f in m.FastenerOptions)
                            if (f.AllowableTensionLoads.TryGetValue(species, out var a))
                                yield return new HoldownChoice { Model = m.Model, Fasteners = Fasteners(f.Screws_Quantity, f.Screws_Type, f.ScrewSize), Allowable = a };
                    break;
                default:
                    foreach (var m in catalog.StrapTiesManager.StrapTies_Dict.Values)
                        foreach (var f in m.FastenerOptions)
                            if (f.AllowableTensionLoads.TryGetValue(species, out var a))
                                yield return new HoldownChoice { Model = m.Model, Fasteners = Fasteners(f.Screws_Quantity, f.Screws_Type, f.ScrewSize), Allowable = a };
                    break;
            }
        }

        private static string Fasteners(int qty, string type, string size)
        {
            string what = string.Join(" ", new[] { size, type }.Where(s => !string.IsNullOrWhiteSpace(s)));
            return qty > 0 ? $"{qty} {what}".Trim() : what;
        }

        /// <summary>
        /// The lightest passing nailing for the wall's panel: 6" then 4", 3", 2" edge spacing on one side, then the
        /// same on both sides. If none passes, the strongest is kept (the result then shows why it fails).
        /// </summary>
        public WallDesignInputs AutoSelect(WallData wall, double vKips, DesignSettings settings)
        {
            var start = (wall.Design ?? new WallDesignInputs()).Clone();
            WallDesignInputs last = start;
            foreach (int sides in new[] { 1, 2 })
                foreach (int s in EdgeSpacings)
                {
                    var trial = start.Clone();
                    trial.Sides = sides;
                    trial.EdgeSpacing = s;
                    if (double.IsNaN(FindPanel(trial)?.NominalWind(s) ?? double.NaN)) continue;
                    last = trial;
                    var r = Check(0, wall, vKips, null, settings, trial);
                    if (r.AspectOk && r.ShearOk) return trial;
                }
            return last;
        }

        // ------------------------------------------------------------------ names

        public static string MaterialName(SheathingMaterials m) =>
            m == SheathingMaterials.SHEATH_MAT_STRUCTURAL_1 ? "Structural I" :
            m == SheathingMaterials.SHEATH_MAT_SHEATHING_AND_SINGLE_FLOOR ? "Sheathing" : m.ToString();

        public static string ThicknessName(NominalPanelThicknesses t)
        {
            switch (t)
            {
                case NominalPanelThicknesses.THICKNESS_5_16IN: return "5/16\"";
                case NominalPanelThicknesses.THICKNESS_3_8IN: return "3/8\"";
                case NominalPanelThicknesses.THICKNESS_7_16IN: return "7/16\"";
                case NominalPanelThicknesses.THICKNESS_1_2IN: return "1/2\"";
                case NominalPanelThicknesses.THICKNESS_15_32IN: return "15/32\"";
                case NominalPanelThicknesses.THICKNESS_19_32IN: return "19/32\"";
                case NominalPanelThicknesses.THICKNESS_5_8IN: return "5/8\"";
                case NominalPanelThicknesses.THICKNESS_23_32IN: return "23/32\"";
                default: return t.ToString();
            }
        }

        public static string NailName(CommonNailSizes n)
        {
            switch (n)
            {
                case CommonNailSizes.NAILSIZE_6D: return "6d";
                case CommonNailSizes.NAILSIZE_8D: return "8d";
                case CommonNailSizes.NAILSIZE_10D: return "10d";
                default: return n.ToString();
            }
        }
    }
}
