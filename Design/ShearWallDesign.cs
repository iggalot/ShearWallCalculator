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
        HDU_ToFoundation = 0,     // HDUE / DTT hold-downs anchored to the foundation (HDU in catalogs before C-C-2026)
        HTT_ToFoundation = 1,     // LTT / HTT / HTTH tension ties anchored to the foundation
        Strap_ToWallBelow = 2,    // strap ties to the wall below
        STHD_ToFoundation = 3,    // LSTHD / STHD strap-tie hold-downs cast into the foundation
        HDU_Legacy_ToFoundation = 4, // discontinued HDU-SDS2.5 (C-C-2021 p. 53; Simpson letter L-C-HDUECONV25), only when chosen
    }

    /// <summary>
    /// The foundation conditions that set an LSTHD / STHD allowable load (Simpson C-C-2026 p. 67, "Tension Loads for STHD
    /// Installations"). One set for the project, saved with the design settings. The defaults are the conservative columns:
    /// cracked concrete, endwall, a 6" stemwall; the "Wind and SDC A&amp;B" row because this program designs for wind.
    /// </summary>
    public class StrapHoldownConditions
    {
        /// <summary>The stemwall width (in.): options listed for a minimum stemwall up to this are used (6 or 8; slab on grade: 8, table footnote 11).</summary>
        public double Stemwall { get; set; } = 6;
        public bool CrackedConcrete { get; set; } = true;
        public SthdPositions Position { get; set; } = SthdPositions.Endwall;
        public SthdSeismic Seismic { get; set; } = SthdSeismic.WindAndSdcAB;

        public StrapHoldownConditions Clone() => (StrapHoldownConditions)MemberwiseClone();

        /// <summary>e.g. "6\" stemwall, cracked, endwall, Wind and SDC A&amp;B".</summary>
        public string Describe() =>
            $"{Stemwall:0.#}\" stemwall, {(CrackedConcrete ? "cracked" : "uncracked")} concrete, {Position.ToString().ToLowerInvariant()}, " +
            (Seismic == SthdSeismic.WindAndSdcAB ? "Wind and SDC A&B" : "SDC C–F");
    }

    /// <summary>
    /// How the diaphragms carry the load to the shear walls (one choice for the whole project). The engineer decides which
    /// idealization applies (ASCE 7 and SDPWS give the conditions); the program does not classify the diaphragm. Envelope
    /// designs each wall for the worse of the two, a common choice when the diaphragm is neither clearly rigid nor clearly
    /// flexible.
    /// </summary>
    public enum DiaphragmAssumption
    {
        Rigid = 0,     // load shared by wall rigidity, plus torsion about the center of rigidity
        Flexible = 1,  // each wall line takes its tributary load (simple spans), plus torsion by equivalent tributary weights
        Envelope = 2,  // both; each wall takes the larger shear
    }

    /// <summary>Settings for the whole project.</summary>
    public class DesignSettings
    {
        /// <summary>Rigid, flexible or the envelope of both: the envelope by default (a project keeps the one it saved). Chosen on the Calculations tab.</summary>
        public DiaphragmAssumption Diaphragm { get; set; } = DiaphragmAssumption.Envelope;

        public DesignMethods Method { get; set; } = DesignMethods.ASD;
        public SdpwsEditions Edition { get; set; } = SdpwsEditions.SDPWS_2021;

        /// <summary>Hold-downs drawn only from the company standard list (kept by the app) instead of the full catalog.</summary>
        public bool HoldownsFromStandardList { get; set; }

        /// <summary>The hold-downs and list version when the project was last saved (null in older projects).</summary>
        public HoldownListRecord HoldownList { get; set; }

        /// <summary>The foundation conditions for LSTHD / STHD hold-downs (null in older projects: the defaults).</summary>
        public StrapHoldownConditions StrapHoldowns { get; set; }

        /// <summary>The STHD conditions in use: the project's, else the defaults.</summary>
        public StrapHoldownConditions StrapHoldownsOrDefault() => StrapHoldowns ?? new StrapHoldownConditions();

        /// <summary>
        /// The sheathing, nailing and hold-down type given to a newly drawn EXTERIOR wall (null: the program default,
        /// <see cref="WallDesignInputs.ExteriorDefault"/>). Projects saved before interior defaults used it for every new wall.
        /// </summary>
        public WallDesignInputs NewWallDesign { get; set; }

        /// <summary>The design given to a newly drawn INTERIOR wall (null: the program default, <see cref="WallDesignInputs.InteriorDefault"/>).</summary>
        public WallDesignInputs NewInteriorWallDesign { get; set; }

        /// <summary>
        /// A new wall's design (a copy): the project's exterior or interior default, else the program's. An exterior wall is
        /// one with any part of its length on the building's outside edge.
        /// </summary>
        public WallDesignInputs NewWallDesignFor(bool exterior) =>
            (exterior ? NewWallDesign ?? WallDesignInputs.ExteriorDefault() : NewInteriorWallDesign ?? WallDesignInputs.InteriorDefault()).Clone();
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

        /// <summary>Face 1 sheathing (any table). Null in projects saved before faces: then the panel fields above are face 1.</summary>
        public FaceSheathing Face1 { get; set; }

        /// <summary>Face 2 sheathing; null = one face. (Older projects: Sides = 2 meant the same panel on both faces.)</summary>
        public FaceSheathing Face2 { get; set; }

        /// <summary>Face 1 as used by the design (from the panel fields in older projects).</summary>
        public FaceSheathing EffectiveFace1() => Face1 ?? new FaceSheathing
        {
            Table = SheathingTable.WoodPanels, Material = Sheathing, Thickness = Thickness, Nail = Nail, EdgeSpacing = EdgeSpacing,
        };

        /// <summary>Face 2 as used by the design: null when there is one face.</summary>
        public FaceSheathing EffectiveFace2() => Face1 != null ? Face2 : (Sides == 2 ? EffectiveFace1() : null);

        /// <summary>Sets both faces and keeps the older fields (panel, edge spacing, sides) in step for older readers.</summary>
        public void SetFaces(FaceSheathing face1, FaceSheathing face2)
        {
            Face1 = face1?.Clone() ?? throw new ArgumentNullException(nameof(face1));
            Face2 = face2?.Clone();
            if (Face1.Table == SheathingTable.WoodPanels)
            {
                Sheathing = Face1.Material; Thickness = Face1.Thickness; Nail = Face1.Nail; EdgeSpacing = Face1.EdgeSpacing;
            }
            Sides = Face2 == null ? 1 : 2;
        }

        /// <summary>SDPWS Table 4.3C row of the interior default: 1/2" gypsum wallboard, 7" edge fastening, studs at 16" o.c., blocked (v_w 250 plf).</summary>
        public const int InteriorGypsumRow = 6;

        /// <summary>The program's default for an exterior wall: 15/32" Structural I, 8d common at 6" edges, one face.</summary>
        public static WallDesignInputs ExteriorDefault()
        {
            var d = new WallDesignInputs();
            d.SetFaces(new FaceSheathing(), null);
            return d;
        }

        /// <summary>
        /// The program's default for an interior wall: 1/2" gypsum wallboard on both faces (SDPWS Table 4.3C row
        /// <see cref="InteriorGypsumRow"/>), the same species and hold-down type as the exterior default.
        /// </summary>
        public static WallDesignInputs InteriorDefault()
        {
            var gyp = new FaceSheathing
            {
                Table = SheathingTable.GypsumAndPlaster, Material = SheathingMaterials.SHEATH_MAT_GYPSUM_WALLBOARD,
                Thickness = NominalPanelThicknesses.THICKNESS_1_2IN, Row = InteriorGypsumRow, EdgeSpacing = 0,
            };
            var d = new WallDesignInputs();
            d.SetFaces(gyp, gyp);
            return d;
        }

        public WallDesignInputs Clone()
        {
            var c = (WallDesignInputs)MemberwiseClone();
            c.Face1 = Face1?.Clone();
            c.Face2 = Face2?.Clone();
            return c;
        }
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
        /// <summary>Stable key "Model#n" (n-th fastener option of the model, 1-based).</summary>
        public string Key { get; set; }
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
        /// <summary>Where the values are tabulated, e.g. "C-C-2026 p. 61" (<see cref="SimpsonCatalog.Edition"/>).</summary>
        public string Source { get; set; }
        public string Fasteners { get; set; }
        public double FastenerLength { get; set; }
        public double? WoodMinHeight { get; set; }
        public double? WoodMinWidth { get; set; }
        public double? AllowableDF_SP { get; set; }
        public double? AllowableSPF_HF { get; set; }
        public double? DeflectionAtAllowable { get; set; }
        /// <summary>LSTHD / STHD: the minimum stemwall (in.) of this option; null for the other families.</summary>
        public double? MinStemwall { get; set; }
        /// <summary>LSTHD / STHD: the conditions its allowable load is for (e.g. "8\" stemwall, cracked concrete, endwall, ...").</summary>
        public string Conditions { get; set; }
    }

    public sealed class HoldownChoice
    {
        /// <summary>Stable key of the catalog option, "Model#n" (see <see cref="ShearWallDesigner.OptionKey"/>).</summary>
        public string Key { get; set; }
        public string Model { get; set; }
        public string Fasteners { get; set; }
        public double Allowable { get; set; }
        public double? WoodMinHeight { get; set; }
        public double? WoodMinWidth { get; set; }
        /// <summary>LSTHD / STHD: the stemwall and conditions the allowable load is for; null for the other families.</summary>
        public string Conditions { get; set; }

        /// <summary>e.g. "HDU14-SDS2.5 (36 1/4 SDS, min. 3.5 × 7.25)": the minimum member tells apart options with the same fasteners.</summary>
        public override string ToString()
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(Fasteners)) parts.Add(Fasteners);
            if (WoodMinHeight.HasValue && WoodMinWidth.HasValue) parts.Add($"min. {WoodMinHeight.Value:0.##} × {WoodMinWidth.Value:0.##}");
            return parts.Count == 0 ? Model : $"{Model} ({string.Join(", ", parts)})";
        }
    }

    /// <summary>
    /// The company's standard hold-downs: catalog option keys ("Model#n") chosen by the engineer. Kept by the app, not by a
    /// project; the design can be limited to these (<see cref="DesignSettings.HoldownsFromStandardList"/>).
    /// The company default ships with the app (<see cref="PackagedPath"/>, with a version number). Each user's own copy
    /// (<see cref="DefaultPath"/>) records which company version it came from and whether the user has changed it, so a
    /// newer company version is taken automatically unless the user has edits (see <see cref="Startup"/>).
    /// </summary>
    public sealed class HoldownStandardList
    {
        private readonly HashSet<string> keys = new HashSet<string>(StringComparer.Ordinal);

        public IReadOnlyCollection<string> Keys => keys;
        public int Count => keys.Count;
        public bool Contains(string key) => key != null && keys.Contains(key);

        /// <summary>Company default version: of the file itself (company file) or the one this copy last took or was offered (user file). 0 = none.</summary>
        public int CompanyVersion { get; set; }

        /// <summary>Date of that company version (yyyy-MM-dd), or null.</summary>
        public string CompanyDate { get; set; }

        /// <summary>True once the user has changed this copy (it no longer simply follows the company default).</summary>
        public bool Customized { get; private set; }

        /// <summary>Raised after the list changes (the host saves it and re-runs the design).</summary>
        public event EventHandler Changed;

        /// <summary>The user ticks or unticks one option: the list becomes the user's own.</summary>
        public void Set(string key, bool included)
        {
            if (string.IsNullOrEmpty(key)) return;
            bool changed = included ? keys.Add(key) : keys.Remove(key);
            if (!changed) return;
            Customized = true;
            Changed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>Replaces this list by the company default (keys and version); it is no longer customized.</summary>
        public void RestoreFrom(HoldownStandardList company)
        {
            keys.Clear();
            foreach (var k in company?.keys ?? Enumerable.Empty<string>()) keys.Add(k);
            CompanyVersion = company?.CompanyVersion ?? 0;
            CompanyDate = company?.CompanyDate;
            Customized = false;
            Changed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>Records that the user saw company version <paramref name="company"/> and kept their own list.</summary>
        public void KeepMineOver(HoldownStandardList company)
        {
            CompanyVersion = company.CompanyVersion;
            CompanyDate = company.CompanyDate;
            Changed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>This list becomes company default <paramref name="version"/> (after "Save as company default").</summary>
        public void MarkAsCompanyVersion(int version, string date)
        {
            CompanyVersion = version;
            CompanyDate = date;
            Customized = false;
            Changed?.Invoke(this, EventArgs.Empty);
        }

        public bool SameKeysAs(HoldownStandardList other) => other != null && keys.SetEquals(other.keys);

        /// <summary>File name of the company default shipped next to the program.</summary>
        public const string PackagedFileName = "HoldownStandardList.default.json";

        /// <summary>The company default shipped with the app (next to the program).</summary>
        public static string PackagedPath => System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, PackagedFileName);

        /// <summary>Each user's own copy: %AppData%\ShearWallCalculator\HoldownStandardList.json.</summary>
        public static string DefaultPath =>
            System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ShearWallCalculator", "HoldownStandardList.json");

        public void Save(string path)
        {
            var dir = System.IO.Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) System.IO.Directory.CreateDirectory(dir);
            var data = new
            {
                Description = "Standard hold-downs: catalog option keys (Model#n = n-th fastener option of the model). " +
                              "Version/Date: the company default version. Customized: changed by the user since.",
                Version = CompanyVersion,
                Date = CompanyDate,
                Customized,
                Keys = keys.OrderBy(k => k, StringComparer.Ordinal).ToList(),
            };
            System.IO.File.WriteAllText(path, Newtonsoft.Json.JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented));
        }

        /// <summary>Reads a list; a missing file gives an empty list (version 0).</summary>
        public static HoldownStandardList Load(string path)
        {
            var list = new HoldownStandardList();
            if (!System.IO.File.Exists(path)) return list;
            var data = Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(path));
            foreach (var k in data["Keys"]?.Values<string>() ?? Enumerable.Empty<string>())
                if (!string.IsNullOrWhiteSpace(k)) list.keys.Add(k.Trim());
            list.CompanyVersion = (int?)data["Version"] ?? 0;
            list.CompanyDate = (string)data["Date"];
            // A list saved before versions existed counts as the user's own if it has anything in it.
            list.Customized = (bool?)data["Customized"] ?? list.keys.Count > 0;
            return list;
        }

        /// <summary>What <see cref="Startup"/> did with the company default.</summary>
        public enum CompanyUpdate
        {
            None,           // the user's list is up to date with the company default
            Seeded,         // first run: the user's list was created from the company default
            Updated,        // a newer company default replaced a list the user had not changed
            NeedsChoice,    // a newer company default exists but the user has edits: ask (RestoreFrom or KeepMineOver)
        }

        /// <summary>
        /// At start-up: reads the company default (<paramref name="companyPath"/>) and the user's list (<paramref name="userPath"/>).
        /// No user list yet: it is created from the company default. A newer company version replaces a list the user has not
        /// changed (or that already matches it); if the user has edits, the caller asks. The user file is saved when changed.
        /// </summary>
        public static (HoldownStandardList user, HoldownStandardList company, CompanyUpdate update) Startup(string userPath, string companyPath)
        {
            var company = Load(companyPath);
            company.Customized = false;
            if (!System.IO.File.Exists(userPath))
            {
                var seeded = new HoldownStandardList();
                seeded.RestoreFrom(company);
                seeded.Save(userPath);
                return (seeded, company, CompanyUpdate.Seeded);
            }
            var user = Load(userPath);
            if (company.CompanyVersion <= user.CompanyVersion) return (user, company, CompanyUpdate.None);
            if (!user.Customized || user.SameKeysAs(company))
            {
                user.RestoreFrom(company);
                user.Save(userPath);
                return (user, company, CompanyUpdate.Updated);
            }
            return (user, company, CompanyUpdate.NeedsChoice);
        }
    }

    /// <summary>
    /// What the hold-down selection used when the project was saved: the standard list's company version, whether it was the
    /// user's own edit, and each wall's hold-down (catalog key, "" for none). On opening, <see cref="Differences"/> says which
    /// walls would now get a different hold-down.
    /// </summary>
    public sealed class HoldownListRecord
    {
        public bool FromStandardList { get; set; }
        public int CompanyVersion { get; set; }
        public string CompanyDate { get; set; }
        public bool Customized { get; set; }
        public Dictionary<int, string> WallHoldowns { get; set; } = new Dictionary<int, string>();

        public static HoldownListRecord Make(IReadOnlyDictionary<int, WallDesignResult> design, HoldownStandardList list, bool fromStandardList) =>
            new HoldownListRecord
            {
                FromStandardList = fromStandardList,
                CompanyVersion = list?.CompanyVersion ?? 0,
                CompanyDate = list?.CompanyDate,
                Customized = list?.Customized ?? false,
                WallHoldowns = (design ?? new Dictionary<int, WallDesignResult>()).ToDictionary(kv => kv.Key, kv => kv.Value.Holdown?.Key ?? ""),
            };

        /// <summary>"Wall 3: HDU5-SDS2.5#1 → HDU8-SDS2.5#1" for each saved wall whose hold-down is now different.</summary>
        public List<string> Differences(IReadOnlyDictionary<int, WallDesignResult> design)
        {
            var lines = new List<string>();
            foreach (var kv in WallHoldowns.OrderBy(k => k.Key))
            {
                if (design == null || !design.TryGetValue(kv.Key, out var r)) continue;
                string now = r.Holdown?.Key ?? "";
                if (now != kv.Value) lines.Add($"Wall {kv.Key}: {Name(kv.Value)} → {Name(now)}");
            }
            return lines;
        }

        /// <summary>"company list v3 (2026-11-15)", plus ", with the user's own changes".</summary>
        public static string Describe(int version, string date, bool customized) =>
            (version == 0 ? "no company list version" : $"company list v{version}" + (string.IsNullOrEmpty(date) ? "" : $" ({date})")) +
            (customized ? ", with the user's own changes" : "");

        private static string Name(string key) => string.IsNullOrEmpty(key) ? "none" : key;
    }

    /// <summary>
    /// A hold-down force of a wall above that stands on this wall: the upper wall's overturning, passed down at its
    /// hold-down post. For one wind direction the upper wall lifts at one end (+T) and bears down at the other (−T).
    /// </summary>
    public sealed class StackedForce
    {
        /// <summary>ft from this wall's low end (smaller x for an east-west wall, smaller y for a north-south wall).</summary>
        public double Along { get; set; }
        /// <summary>lb, + uplift, − bearing down (ASD, 0.6W).</summary>
        public double Asd { get; set; }
        /// <summary>lb, the same at LRFD (1.0W), for reference.</summary>
        public double Lrfd { get; set; }
        /// <summary>The wind direction this force goes with: true when the low end of the walls is in tension.</summary>
        public bool LowEndUp { get; set; }
        public string From { get; set; }
    }

    /// <summary>How a wall's check reads at a glance.</summary>
    public enum DesignState { Pass, Close, Fail }

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
        /// <summary>Each sheathed face with its table value and factors (one or two).</summary>
        public List<FaceResult> Faces { get; set; } = new List<FaceResult>();
        /// <summary>Nominal unit shear of the wall from its faces (plf), before Omega or phi.</summary>
        public double CombinedNominal { get; set; }
        /// <summary>How the faces were combined.</summary>
        public string CombineRule { get; set; }
        public double ShearRatio => Capacity > 0 ? Demand / Capacity : double.PositiveInfinity;

        /// <summary>Post dimension along the wall at each hold-down (in.), from the hold-down's minimum member.</summary>
        public double PostAlongWall { get; set; }
        /// <summary>Hold-down lever arm d = L - post (ft): between the hold-down centerlines.</summary>
        public double HoldownArm { get; set; }
        public double TensionAsd { get; set; }      // lb: 0.6 V h / d - 0.6 D L / 2, d = L - post
        public double TensionLrfd { get; set; }     // lb: 1.0 V h / d - 0.9 D L / 2 (for reference)
        public HoldownChoice Holdown { get; set; }  // null if none in the pool is strong enough

        /// <summary>The hold-down forces of the walls above standing on this one (empty on the top level or a single story).</summary>
        public List<StackedForce> FromAbove { get; set; } = new List<StackedForce>();
        /// <summary>This wall's own tension (its shear and dead load only), lb ASD / LRFD; <see cref="TensionAsd"/> adds the walls above.</summary>
        public double OwnTensionAsd { get; set; }
        public double OwnTensionLrfd { get; set; }
        /// <summary>The tension added by the walls above (lb, ASD).</summary>
        public double TensionFromAboveAsd => Math.Max(0, TensionAsd - OwnTensionAsd);
        /// <summary>The walls above's share of T at the design shear (lb, ASD / LRFD: their moment over the arm).</summary>
        public double AboveTensionAsd { get; set; }
        public double AboveTensionLrfd { get; set; }
        /// <summary>The dead load on the wall resisting overturning (lb).</summary>
        public double DeadLoadLb { get; set; }

        /// <summary>
        /// The hold-down tension in each load case, at the end that lifts in it: [low-end ASD, high-end ASD, low-end LRFD,
        /// high-end LRFD] (lb; "low" = the smaller x of an E-W wall, the smaller y of a N-S wall). Null until
        /// <see cref="SetCaseTensions"/>. The load path adds these case by case where hold-downs share a column or a beam:
        /// two walls' worst cases are not at the same time.
        /// </summary>
        public Dictionary<string, double[]> TensionByCase { get; set; }

        /// <summary>
        /// The tension of each case from the wall's force in that case (kips on the diaphragm, building axes; the top of the
        /// wall is pushed the other way, so a negative force lifts the low end). Same formula as <see cref="TensionAsd"/>,
        /// at |V_case|; the walls above are scaled by |V_case| / V (they are stacked on the same line, so they follow the
        /// same cases), which gives exactly <see cref="TensionAsd"/> in the governing case.
        /// </summary>
        public void SetCaseTensions(IEnumerable<KeyValuePair<string, double>> forceByCase)
        {
            TensionByCase = new Dictionary<string, double[]>();
            EndForceByCase = new Dictionary<string, double[]>();
            double arm = HoldownArm, V = Math.Abs(this.V);
            if (!(arm > 0) || double.IsInfinity(arm)) return;
            foreach (var kv in forceByCase)
            {
                double vc = Math.Abs(kv.Value), share = V > 1e-12 ? vc / V : 0, M = vc * 1000.0 * Height;
                double asd = Math.Max(0, 0.6 * M / arm - 0.6 * DeadLoadLb / 2 + AboveTensionAsd * share);
                double lrfd = Math.Max(0, 1.0 * M / arm - 0.9 * DeadLoadLb / 2 + AboveTensionLrfd * share);
                bool lowLifts = kv.Value < 0;
                TensionByCase[kv.Key] = lowLifts ? new[] { asd, 0, lrfd, 0 } : new[] { 0, asd, 0, lrfd };

                // both ends on the foundation, + up / − down (ASD): the overturning couple ± and the dead load down, with
                // 0.6D (uplift, 0.6D + 0.6W) and with 1.0D (bearing, D + 0.6W). The walls above add their couple at both
                // ends (taken equal at the compression end: a wall stacked end to end)
                double couple = 0.6 * M / arm + AboveTensionAsd * share;
                double up06 = couple - 0.6 * DeadLoadLb / 2, down06 = -(couple + 0.6 * DeadLoadLb / 2);
                double up10 = couple - DeadLoadLb / 2, down10 = -(couple + DeadLoadLb / 2);
                EndForceByCase[kv.Key] = lowLifts ? new[] { up06, down06, up10, down10 } : new[] { down06, up06, down10, up10 };
            }
        }

        /// <summary>
        /// The force each end puts on what is under it in each load case, lb ASD, + up (uplift) / − down (bearing):
        /// [low end with 0.6D, high end with 0.6D, low end with 1.0D, high end with 1.0D]. The foundation adds these where
        /// walls meet (<see cref="FoundationForces"/>): uplift from the 0.6D values, bearing from the 1.0D values.
        /// </summary>
        public Dictionary<string, double[]> EndForceByCase { get; set; }

        /// <summary>Where the load path anchors this wall's hold-down with another type than its own (e.g. "ST9 strap on beam BM2"): set by the load path.</summary>
        public List<string> HoldownElsewhere { get; } = new List<string>();

        /// <summary>True when the hold-down was drawn from the company standard list only.</summary>
        public bool HoldownFromStandardList { get; set; }
        public bool HoldownNeeded => TensionAsd > 0;
        public double HoldownRatio => !HoldownNeeded ? 0 : Holdown == null ? double.PositiveInfinity : TensionAsd / Holdown.Allowable;

        public bool ShearOk => !double.IsNaN(Capacity) && ShearRatio <= 1.0 + 1e-9;
        public bool HoldownOk => !HoldownNeeded || Holdown != null;
        public bool Pass => AspectOk && ShearOk && HoldownOk;

        /// <summary>A passing wall at or above this shear ratio is shown as close to its limit.</summary>
        public const double CloseRatio = 0.90;

        /// <summary>Pass, close (passes at a shear ratio of <see cref="CloseRatio"/> or more) or fail: the colour and mark the wall is shown with.</summary>
        public DesignState State => !Pass ? DesignState.Fail : ShearRatio >= CloseRatio - 1e-9 ? DesignState.Close : DesignState.Pass;

        /// <summary>Short reason when the wall does not pass.</summary>
        public string Status =>
            !AspectOk ? $"h/b {AspectRatio:0.00} > {MaxAspectRatio:0.0} not allowed"
            : double.IsNaN(Capacity) ? "no table value for this nailing"
            : !ShearOk ? $"shear {ShearRatio:0.00} > 1"
            : !HoldownOk ? (HoldownFromStandardList ? "no standard-list hold-down strong enough" : "no hold-down strong enough")
            : "OK";
    }

    /// <summary>
    /// Wood structural panel shear-wall checks for wind (SDPWS), per wall segment (each drawn wall is one
    /// full-height segment):
    /// v = factor V / L; capacity = v_w x species x footnote-10 x aspect factor x sides / Omega (ASD) or x phi (LRFD);
    /// hold-down tension T = M / d, d = L - post (less dead load), always at ASD (0.6W) against Simpson allowables.
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

        private List<SheathingOption> sheathingOptions;

        /// <summary>Every face choice: Tables 4.3A, 4.3B and 4.3C.</summary>
        public IReadOnlyList<SheathingOption> SheathingOptions => sheathingOptions ?? (sheathingOptions = SheathingRules.AllOptions());

        public SheathingOption FindOption(FaceSheathing f) => f == null ? null : SheathingOptions.FirstOrDefault(o => o.Matches(f));

        /// <summary>One face: table value, species factor (Tables 4.3A/B), footnote 10 (Table 4.3A, 2021), aspect factor; 0 if over its h/b limit.</summary>
        public FaceResult CheckFace(FaceSheathing f, WallDesignInputs d, DesignSettings settings, double h, double L)
        {
            var o = FindOption(f);
            var fr = new FaceResult { Face = f, Option = o };
            if (o == null)
            {
                fr.Tabulated = fr.Nominal = double.NaN;
                fr.Note = "not in the tables";
                return fr;
            }
            fr.Tabulated = o.NominalWind(f.EdgeSpacing);
            fr.SpeciesFactor = o.Table == SheathingTable.GypsumAndPlaster ? 1.0 : SpeciesFactor(d.Species);
            fr.InsideFaceFactor = o.Table == SheathingTable.WoodPanels
                ? SdpwsSource.InsideFaceHoldownFactor(settings.Edition, f.Nail == CommonNailSizes.NAILSIZE_10D, d.HoldownOnInsideFace) : 1.0;
            fr.MaxAspectRatio = SheathingRules.MaxAspectRatio(o);
            fr.AspectOk = L > 0 && h / L <= fr.MaxAspectRatio + 1e-9;
            fr.AspectFactor = SheathingRules.AspectFactor(o, h, L);
            if (!fr.AspectOk)
            {
                fr.Nominal = 0;
                fr.Note = $"h/b over {fr.MaxAspectRatio:0.0}: this face does not count";
            }
            else fr.Nominal = fr.Tabulated * fr.SpeciesFactor * fr.InsideFaceFactor * fr.AspectFactor;
            return fr;
        }

        /// <summary>The sheathing as one line: "A", "A, both faces" or "A + B".</summary>
        public string SheathingName(WallDesignInputs d)
        {
            var f1 = d.EffectiveFace1();
            var f2 = d.EffectiveFace2();
            string n1 = FindOption(f1)?.Name ?? "(not in the tables)";
            string Sp(FaceSheathing f) => FindOption(f)?.UsesEdgeSpacing == true ? $" @ {f.EdgeSpacing}\"" : "";
            if (f2 == null) return n1 + Sp(f1);
            if (f1.SameConstructionAs(f2)) return n1 + Sp(f1) + ", both faces";
            return n1 + Sp(f1) + " + " + (FindOption(f2)?.Name ?? "(not in the tables)") + Sp(f2);
        }

        public static readonly int[] EdgeSpacings = { 6, 4, 3, 2 };

        public PanelOption FindPanel(WallDesignInputs d) =>
            panels.FirstOrDefault(p => p.Sheathing == d.Sheathing && p.Thickness == d.Thickness && p.Nail == d.Nail);

        /// <summary>Table 4.3A footnote: framing other than Douglas fir-larch or southern pine, 1 - (0.5 - G); SPF G = 0.42.</summary>
        public static double SpeciesFactor(WoodTypes species) => species == WoodTypes.WOODTYPE_SPF_HF ? 1 - (0.5 - 0.42) : 1.0;

        /// <param name="above">The hold-down forces of the walls of the story above standing on this wall (multi-story): they add their
        /// moment about this wall's compression post, for the wind direction that lifts the same end.</param>
        public WallDesignResult Check(int wallId, WallData wall, double vKips, string governingCase, DesignSettings settings, WallDesignInputs d = null,
                                      IReadOnlyList<StackedForce> above = null)
        {
            d = d ?? wall.Design ?? new WallDesignInputs();
            settings = settings ?? new DesignSettings();
            double L = wall.WallLength, h = wall.WallHeight, V = Math.Abs(vKips);
            double factor = settings.Method == DesignMethods.ASD ? 0.6 : 1.0;

            var r = new WallDesignResult
            {
                WallId = wallId,
                GoverningCase = governingCase,
                Method = settings.Method,
                Edition = settings.Edition,
                Inputs = d.Clone(),
                Length = L,
                Height = h,
                V = V,
                LoadFactor = factor,
                AspectRatio = L > 0 ? h / L : double.PositiveInfinity,
            };
            r.Demand = L > 0 ? factor * V * 1000.0 / L : double.PositiveInfinity;

            var face1 = d.EffectiveFace1();
            var face2 = d.EffectiveFace2();
            var fr1 = CheckFace(face1, d, settings, h, L);
            var fr2 = face2 == null ? null : CheckFace(face2, d, settings, h, L);
            r.Faces.Add(fr1);
            if (fr2 != null) r.Faces.Add(fr2);
            r.Panel = SheathingName(d);
            r.Nominal = fr1.Tabulated;
            r.SpeciesFactor = fr1.SpeciesFactor;
            r.InsideFaceFactor = fr1.InsideFaceFactor;
            r.AspectFactor = fr1.AspectFactor;
            r.MaxAspectRatio = r.Faces.Max(f => f.MaxAspectRatio); // the wall counts while any face is within its limit
            var (nominal, rule) = SheathingRules.Combine(fr1, fr2, face2 != null && face1.SameConstructionAs(face2));
            r.CombinedNominal = nominal;
            r.CombineRule = rule;
            r.Capacity = SdpwsSource.DesignUnitShearWind(nominal, settings.Method);

            // Hold-down: overturning moment about the compression post over the lever arm d = L - post (hold-down
            // centerlines at half a post in from each end), less dead load. Dead load acts at L/2, d/2 from the post
            // centerline, so its share is D d/2 / d = D/2 whatever the post. The post comes from the hold-down chosen,
            // which depends on T: start with two 2x studs and repeat until the choice settles.
            double M = V * 1000.0 * h;                      // lb-ft
            double D = Math.Max(0, d.DeadLoad) * L;         // lb on the wall
            r.HoldownFromStandardList = settings.HoldownsFromStandardList;
            r.FromAbove = above?.ToList() ?? new List<StackedForce>();
            double post = WallSchedule.DefaultPostAlongWall;
            for (int pass = 0; pass < 5; pass++)
            {
                r.PostAlongWall = post;
                r.HoldownArm = L - post / 12.0;
                double arm = r.HoldownArm;
                // The walls above: their moment about the compression post at the far end, for the worse wind direction
                // (both ends get the same hold-down). A wall stacked end to end adds its own T; the dead load of this
                // wall resists the whole stack.
                var (aboveAsd, aboveLrfd) = MomentFromAbove(r.FromAbove, L, post / 24.0);
                double ownAsd = 0.6 * M / arm - 0.6 * D / 2, ownLrfd = 1.0 * M / arm - 0.9 * D / 2;
                r.OwnTensionAsd = arm > 0 ? Math.Max(0, ownAsd) : double.PositiveInfinity;
                r.OwnTensionLrfd = arm > 0 ? Math.Max(0, ownLrfd) : double.PositiveInfinity;
                r.TensionAsd = arm > 0 ? Math.Max(0, ownAsd + aboveAsd / arm) : double.PositiveInfinity;
                r.TensionLrfd = arm > 0 ? Math.Max(0, ownLrfd + aboveLrfd / arm) : double.PositiveInfinity;
                r.AboveTensionAsd = arm > 0 ? aboveAsd / arm : 0;
                r.AboveTensionLrfd = arm > 0 ? aboveLrfd / arm : 0;
                r.DeadLoadLb = D;
                r.Holdown = r.HoldownNeeded ? SelectHoldown(d.Holdown, d.Species, r.TensionAsd, settings.HoldownsFromStandardList, settings.StrapHoldownsOrDefault()) : null;
                double next = WallSchedule.PostAlongWall(r.Holdown);
                if (r.Holdown == null || Math.Abs(next - post) < 1e-9) break;
                post = next;
            }
            return r;
        }

        /// <summary>
        /// The overturning moment (lb-ft, ASD and LRFD) the forces from above put on a wall of length <paramref name="L"/>
        /// about its compression post (centerline <paramref name="halfPost"/> ft in from the end): for each wind direction
        /// Σ F × (distance from the post at the far end), and the larger of the two directions.
        /// </summary>
        public static (double asd, double lrfd) MomentFromAbove(IEnumerable<StackedForce> forces, double L, double halfPost)
        {
            double lowAsd = 0, lowLrfd = 0, highAsd = 0, highLrfd = 0;
            foreach (var f in forces ?? Enumerable.Empty<StackedForce>())
            {
                double p = Math.Max(0, Math.Min(L, f.Along));
                if (f.LowEndUp)
                {
                    double lever = Math.Max(0, L - halfPost - p); // compression post at the high end
                    lowAsd += f.Asd * lever;
                    lowLrfd += f.Lrfd * lever;
                }
                else
                {
                    double lever = Math.Max(0, p - halfPost);     // compression post at the low end
                    highAsd += f.Asd * lever;
                    highLrfd += f.Lrfd * lever;
                }
            }
            return lowAsd >= highAsd ? (lowAsd, lowLrfd) : (highAsd, highLrfd);
        }

        /// <summary>The company standard hold-downs (kept by the app). Empty unless the host sets it.</summary>
        public HoldownStandardList StandardList { get; set; } = new HoldownStandardList();

        /// <summary>The company default shipped with the app (for "Restore company default" and the catalog's Company column).</summary>
        public HoldownStandardList CompanyList { get; set; } = new HoldownStandardList();

        /// <summary>Stable key of a catalog option: the model and the option's position (1-based) in its fastener options.</summary>
        public static string OptionKey(string model, int optionIndex) => $"{model}#{optionIndex + 1}";

        /// <summary>The catalog edition and page a family's values come from, e.g. "C-C-2026 p. 61".</summary>
        public static string CatalogSource(HoldownFamilies family) =>
            family == HoldownFamilies.HDU_Legacy_ToFoundation
                ? $"{SimpsonCatalog.LegacyHduEdition} {SimpsonCatalog.PagesLegacyHdu} (discontinued, {SimpsonCatalog.LegacyHduLetter})"
                : SimpsonCatalog.Edition + " " + (family == HoldownFamilies.HDU_ToFoundation ? SimpsonCatalog.PagesHduDtt
                                          : family == HoldownFamilies.HTT_ToFoundation ? SimpsonCatalog.PagesHtt
                                          : family == HoldownFamilies.STHD_ToFoundation ? SimpsonCatalog.PagesSthd : SimpsonCatalog.PagesStraps);

        /// <summary>The footnote for the design page and the report: the catalog edition every hold-down load comes from.</summary>
        public static string CatalogFootnote =>
            $"Hold-down allowable tension loads (ASD): {SimpsonCatalog.Citation} — HDUE/DTT {SimpsonCatalog.PagesHduDtt}, LTT/HTT/HTTH {SimpsonCatalog.PagesHtt}, " +
            $"LSTHD/STHD {SimpsonCatalog.PagesSthd}, strap ties {SimpsonCatalog.PagesStraps}. Discontinued HDU (only where chosen): {SimpsonCatalog.LegacyHduEdition} {SimpsonCatalog.PagesLegacyHdu}. " +
            "Verify against the current catalog and the evaluation reports.";

        /// <summary>
        /// The lightest option (smallest allowable tension) that is at least the required tension, from the full catalog or,
        /// with <paramref name="standardOnly"/>, from the company standard list only (null when none there is strong enough).
        /// </summary>
        /// <param name="sthd">The foundation conditions for LSTHD / STHD (null: the defaults); ignored by the other families.</param>
        public HoldownChoice SelectHoldown(HoldownFamilies family, WoodTypes species, double requiredLb, bool standardOnly = false, StrapHoldownConditions sthd = null)
        {
            string key = species == WoodTypes.WOODTYPE_SPF_HF ? "SPF_HF" : "DF_SP";
            return HoldownOptions(family, key, sthd)
                .Where(o => !standardOnly || StandardList.Contains(o.Key))
                .Where(o => o.Allowable >= requiredLb)
                .OrderBy(o => o.Allowable)
                .FirstOrDefault();
        }

        /// <summary>Standard-list keys that are not options in the catalog (e.g. after a catalog edit).</summary>
        public List<string> StandardKeysNotInCatalog()
        {
            var all = new HashSet<string>(CatalogRows().Select(r => r.Key));
            return StandardList.Keys.Where(k => !all.Contains(k)).OrderBy(k => k).ToList();
        }

        /// <summary>
        /// Every hold-down in the catalog this designer selects from, one row per model and fastener option, with all the
        /// stored data. It reads the same managers as <see cref="SelectHoldown"/>, so the values shown are the values used.
        /// </summary>
        /// <param name="sthd">The foundation conditions the LSTHD / STHD allowable loads are given for (null: the defaults).</param>
        public List<HoldownSpecRow> CatalogRows(StrapHoldownConditions sthd = null)
        {
            sthd = sthd ?? new StrapHoldownConditions();
            var rows = new List<HoldownSpecRow>();
            void Add(HoldownFamilies family, string model, int gauge, double w, double hl, double? b, double? cl, double? so, double? bolt, string code,
                     int qty, string type, string size, double length, double? minH, double? minW, IDictionary<string, double> loads, double? defl, int index)
            {
                rows.Add(new HoldownSpecRow
                {
                    Key = OptionKey(model, index), Family = family, Model = model, FirstOfModel = index == 0, Gauge = gauge, W = w, HeightOrLength = hl, B = b, CL = cl, SO = so,
                    AnchorBoltDia = bolt, CodeRef = code, Source = CatalogSource(family), Fasteners = Fasteners(qty, type, size), FastenerLength = length,
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
                        f.Screws_Quantity, f.Screws_Type, f.ScrewSize, f.Length, f.WoodMinHeight, f.WoodMinWidth, f.AllowableTensionLoads, f.DeflectionAtAllowableLoad, i);
                }
            // discontinued HDU-SDS2.5: their own family, so they are only selected for a wall set to it
            foreach (var m in catalog.LegacyHDUManager.HDU_Dict.Values)
                for (int i = 0; i < m.FastenerOptions.Count; i++)
                {
                    var f = m.FastenerOptions[i];
                    Add(HoldownFamilies.HDU_Legacy_ToFoundation, m.Model, m.Gauge, m.W, m.H, m.B, m.CL, m.SO, m.AnchorBoltDia, m.CodeRef,
                        f.Screws_Quantity, f.Screws_Type, f.ScrewSize, f.Length, f.WoodMinHeight, f.WoodMinWidth, f.AllowableTensionLoads, f.DeflectionAtAllowableLoad, i);
                }
            foreach (var m in catalog.HTTManager.HTT_Dict.Values)
                for (int i = 0; i < m.FastenerOptions.Count; i++)
                {
                    var f = m.FastenerOptions[i];
                    Add(HoldownFamilies.HTT_ToFoundation, m.Model, m.Gauge, m.W, m.L, m.B, m.CL, m.SO, m.AnchorBoltDia, m.CodeRef,
                        f.Screws_Quantity, f.Screws_Type, f.ScrewSize, f.Length, f.WoodMinHeight, f.WoodMinWidth, f.AllowableTensionLoads, f.DeflectionAtAllowableLoad, i);
                }
            foreach (var m in catalog.StrapTiesManager.StrapTies_Dict.Values)
                for (int i = 0; i < m.FastenerOptions.Count; i++)
                {
                    var f = m.FastenerOptions[i];
                    Add(HoldownFamilies.Strap_ToWallBelow, m.Model, m.Gauge, m.W, m.L, null, null, null, null, m.CodeRef,
                        f.Screws_Quantity, f.Screws_Type, f.ScrewSize, f.Length, null, null, f.AllowableTensionLoads, null, i);
                }
            // LSTHD / STHD: one option per minimum stemwall ("#1" 6", "#2" 8"); the nails and the allowable load are the
            // table's for the project's seismic row, concrete and position. The loads are the same for every species.
            string conditions = sthd.Describe();
            foreach (var m in catalog.STHDManager.STHD_Dict.Values)
                for (int i = 0; i < STHD_Manager.Stemwalls.Length; i++)
                {
                    var inst = m.Installations.FirstOrDefault(x => x.MinStemwall == STHD_Manager.Stemwalls[i] && x.Seismic == sthd.Seismic);
                    if (inst == null) continue;
                    double a = inst.Allowable(sthd.CrackedConcrete, sthd.Position);
                    var loads = new Dictionary<string, double> { { "DF_SP", a }, { "SPF_HF", a } };
                    Add(HoldownFamilies.STHD_ToFoundation, m.Model, m.Gauge, m.W, m.L, null, null, null, null, m.CodeRef,
                        inst.Nails_Quantity, "", inst.NailSize, inst.NailLength, m.WoodMinHeight, m.WoodMinWidth, loads, m.DeflectionOnFraming, i);
                    rows[rows.Count - 1].MinStemwall = inst.MinStemwall;
                    rows[rows.Count - 1].Conditions = $"min. {inst.MinStemwall:0.#}\" stemwall, le = {m.Embedment:0.#}\"; {conditions}";
                }
            return rows;
        }

        /// <summary>The options <see cref="SelectHoldown"/> chooses from (for checks against <see cref="CatalogRows"/>).</summary>
        public IEnumerable<HoldownChoice> Options(HoldownFamilies family, WoodTypes species, StrapHoldownConditions sthd = null) =>
            HoldownOptions(family, species == WoodTypes.WOODTYPE_SPF_HF ? "SPF_HF" : "DF_SP", sthd);

        /// <summary>
        /// The selectable options of one family for one species, built from <see cref="CatalogRows"/> (one source of data).
        /// LSTHD / STHD options need a stemwall at least their minimum.
        /// </summary>
        private IEnumerable<HoldownChoice> HoldownOptions(HoldownFamilies family, string species, StrapHoldownConditions sthd)
        {
            sthd = sthd ?? new StrapHoldownConditions();
            foreach (var r in CatalogRows(sthd).Where(r => r.Family == family && (r.MinStemwall == null || r.MinStemwall <= sthd.Stemwall + 1e-9)))
            {
                double? a = species == "SPF_HF" ? r.AllowableSPF_HF : r.AllowableDF_SP;
                if (a.HasValue)
                    yield return new HoldownChoice
                    {
                        Key = r.Key, Model = r.Model, Fasteners = r.Fasteners, Allowable = a.Value,
                        WoodMinHeight = r.WoodMinHeight, WoodMinWidth = r.WoodMinWidth, Conditions = r.Conditions,
                    };
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
            var f1 = start.EffectiveFace1();
            var f2 = start.EffectiveFace2();
            var trials = new List<(FaceSheathing a, FaceSheathing b)>();
            FaceSheathing At(FaceSheathing f, int s) { var c = f.Clone(); if (FindOption(f)?.UsesEdgeSpacing == true) c.EdgeSpacing = s; return c; }
            if (f2 == null)
            {
                foreach (int s in EdgeSpacings) trials.Add((At(f1, s), null));
                foreach (int s in EdgeSpacings) trials.Add((At(f1, s), At(f1, s)));   // then the same on both faces
            }
            else
                foreach (int s in EdgeSpacings) trials.Add((At(f1, s), At(f2, s)));

            WallDesignInputs last = start;
            foreach (var (a, b) in trials)
            {
                var trial = start.Clone();
                if (start.Face1 == null && (b == null || a.SameConstructionAs(b)))
                {
                    trial.EdgeSpacing = a.EdgeSpacing;   // older project: stay on the panel fields
                    trial.Sides = b == null ? 1 : 2;
                }
                else trial.SetFaces(a, b);
                var r = Check(0, wall, vKips, null, settings, trial);
                if (double.IsNaN(r.Capacity)) continue;
                last = trial;
                if (r.AspectOk && r.ShearOk) return trial;
            }
            return last;
        }

        // ------------------------------------------------------------------ names

        public static string MaterialName(SheathingMaterials m)
        {
            switch (m)
            {
                case SheathingMaterials.SHEATH_MAT_STRUCTURAL_1: return "Structural I";
                case SheathingMaterials.SHEATH_MAT_SHEATHING_AND_SINGLE_FLOOR: return "Sheathing";
                case SheathingMaterials.SHEATH_MAT_PLYWOOD: return "Plywood panel siding";
                case SheathingMaterials.SHEATH_MAT_PARTICLEBOARD: return "Particleboard";
                case SheathingMaterials.SHEATH_MAT_FIBERBOARD: return "Structural fiberboard";
                case SheathingMaterials.SHEATH_MAT_GYPSUM_WALLBOARD: return "Gypsum wallboard";
                case SheathingMaterials.SHEATH_MAT_GYPSUM_SHEATHING_BOARD: return "Gypsum sheathing";
                case SheathingMaterials.SHEATH_MAT_GYPSUM_LATH_PLASTER: return "Gypsum lath and plaster";
                case SheathingMaterials.SHEATH_MAT_CEMENT_PLASTER_OVER_METAL: return "Portland cement plaster on metal lath";
                default: return m.ToString();
            }
        }

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
