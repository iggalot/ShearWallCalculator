using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using BuildingModel.Checks;
using BuildingModel.Edits;
using BuildingModel.Geometry;
using BuildingModel.Model;
using calculator;
using Newtonsoft.Json;
using ShearWallCalculator;
using ShearWallCalculator.Design;

namespace ShearWallCalculator.ModelAdapter
{
    /// <summary>
    /// One story of the designer: the model level on top of it, its calculator, and how the calculator's integer keys
    /// map to the model's IDs.
    /// </summary>
    public sealed class StoryMap
    {
        public Level Level { get; set; }
        public ShearWallCalculatorBase Calculator { get; set; }

        /// <summary>The story height under the level (ft).</summary>
        public double PlateHeight { get; set; }

        /// <summary>Calculator wall key → model wall ID (W12 → 12: the same key on every story the wall spans).</summary>
        public Dictionary<int, string> Walls { get; } = new Dictionary<int, string>();

        /// <summary>Calculator diaphragm key → model diaphragm ID (D3 → 3, or 301, 302... when openings split it).</summary>
        public Dictionary<int, string> Diaphragms { get; } = new Dictionary<int, string>();

        public string ModelId(string kind, int key) =>
            kind == "Wall" ? (Walls.TryGetValue(key, out var w) ? w : null) : kind == "Diaphragm" ? (Diaphragms.TryGetValue(key, out var d) ? d : null) : null;

        public int? Key(string modelId)
        {
            foreach (var kv in Walls) if (kv.Value == modelId) return kv.Key;
            foreach (var kv in Diaphragms) if (kv.Value == modelId) return kv.Key;
            return null;
        }
    }

    /// <summary>
    /// The building model is the source of truth for the Shear Wall Designer's geometry. Each level above the ground is
    /// one designer story: its net diaphragm (openings taken out, as rectangles) and the shear walls under it that its
    /// diaphragm reaches. The designer's own data rides in the model's data bags:
    /// <list type="bullet">
    /// <item>a wall's design inputs, per story, under <see cref="WallKey"/>;</item>
    /// <item>a diaphragm's type (rigid, flexible) under <see cref="DiaphragmKey"/>;</item>
    /// <item>the project's design settings on the building under <see cref="SettingsKey"/>.</item>
    /// </list>
    /// </summary>
    public static class ShearWallAdapter
    {
        public const string WallKey = "ShearWall";
        public const string DiaphragmKey = "ShearWall";
        public const string SettingsKey = "ShearWall.DesignSettings";

        private static readonly JsonSerializerSettings Json = new JsonSerializerSettings
        {
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            NullValueHandling = NullValueHandling.Ignore,
        };

        /// <summary>The designer's data about one wall: its design inputs on each story (by level ID) and its type.</summary>
        public sealed class WallExtras
        {
            public Dictionary<string, WallDesignInputs> Design { get; set; } = new Dictionary<string, WallDesignInputs>();
            public WallTypes WallType { get; set; } = WallTypes.Type_01;

            /// <summary>
            /// Per level: the wall still has the default it was given (true: the exterior one, false: the interior one), so it
            /// follows when the wall becomes exterior or interior. Not there once the user edits the design (null: none).
            /// </summary>
            public Dictionary<string, bool> AutoExterior { get; set; }

            /// <summary>
            /// The part of the wall declared a shear wall: from and to, in ft along the wall from its start. Its ends are where
            /// the hold-downs go. Null: the wall is not a shear wall (it carries only its gravity load).
            /// </summary>
            public ShearPart Shear { get; set; }
        }

        /// <summary>A stretch of a wall, in ft along it from its start (<see cref="Wall.Start"/>).</summary>
        public sealed class ShearPart
        {
            public double From { get; set; }
            public double To { get; set; }
            public double Length => To - From;
        }

        /// <summary>
        /// On the building once this app declares the shear walls itself (every project it has opened). Without it the project is
        /// from before: the walls the model marked (<see cref="Wall.IsShearWall"/>) are its shear walls, each over its whole length.
        /// </summary>
        public const string DeclaredKey = "ShearWall.Declared";

        /// <summary>The shortest part of a wall that can be declared (ft).</summary>
        public const double MinShearLength = 0.5;

        /// <summary>True when the shear walls are declared in this app (<see cref="DeclaredKey"/>).</summary>
        public static bool Declares(Building b) => b?.GetData(DeclaredKey) != null;

        /// <summary>
        /// The part of <paramref name="w"/> that is a shear wall, kept on the wall (null: not a shear wall). A project from before
        /// (<see cref="Declares"/> false): a wall the model marked, its whole length.
        /// </summary>
        public static ShearPart ShearPartOf(Building b, Wall w)
        {
            if (w == null) return null;
            double len = w.Length;
            var s = ReadWall(w).Shear;
            if (s != null)
            {
                double from = Math.Max(0, Math.Min(s.From, s.To)), to = Math.Min(len, Math.Max(s.From, s.To));
                return to - from >= MinShearLength - 1e-9 ? new ShearPart { From = from, To = to } : null;
            }
            return !Declares(b) && w.IsShearWall && len > 0 ? new ShearPart { From = 0, To = len } : null;
        }

        public static bool IsShearWall(Building b, Wall w) => ShearPartOf(b, w) != null;

        /// <summary>The shear walls of the building (declared, or from before: see <see cref="ShearPartOf"/>).</summary>
        public static List<Wall> ShearWalls(Building b) => b.Walls.Where(w => IsShearWall(b, w)).ToList();

        /// <summary>The two ends of a part of a wall, in plan.</summary>
        public static (Point2 a, Point2 e) Ends(Wall w, ShearPart p)
        {
            double len = w.Length;
            Point2 At(double t) => len > 0 ? new Point2(w.Start.X + (w.End.X - w.Start.X) * t / len, w.Start.Y + (w.End.Y - w.Start.Y) * t / len) : w.Start;
            return (At(p.From), At(p.To));
        }

        /// <summary>The levels whose diaphragms the wall can hold up: the stories it spans (above its base, up to its top).</summary>
        public static List<Level> StoriesOf(Building b, Wall w)
        {
            var lo = b.Level(w.BaseLevelId);
            var hi = b.Level(w.TopLevelId);
            if (lo == null || hi == null) return new List<Level>();
            return Stories(b).Where(l => l.Elevation > lo.Elevation && l.Elevation <= hi.Elevation).ToList();
        }

        /// <summary>
        /// Declares the part of <paramref name="w"/> from <paramref name="from"/> to <paramref name="to"/> (ft from its start, kept
        /// inside it) a shear wall. A story it spans that has no sheathing yet is given the project's default for where the wall is
        /// (exterior or interior) as the pick to start from; it is the wall's own, not followed when the wall moves. False when the
        /// part is shorter than <see cref="MinShearLength"/>.
        /// </summary>
        public static bool Declare(Building b, Wall w, double from, double to, DesignSettings settings)
        {
            double len = w.Length;
            double a = Math.Max(0, Math.Min(from, to)), e = Math.Min(len, Math.Max(from, to));
            if (e - a < MinShearLength - 1e-9) return false;
            var x = ReadWall(w);
            x.Shear = new ShearPart { From = Math.Round(a, 4), To = Math.Round(e, 4) };
            foreach (var l in StoriesOf(b, w))
                if (!x.Design.TryGetValue(l.Id, out var d) || d == null) x.Design[l.Id] = NewWallDesign(b, w, settings);
            x.AutoExterior = null;
            WriteWall(w, x);
            return true;
        }

        /// <summary>The wall is no longer a shear wall: this app's data on it is taken off (its sheathing too).</summary>
        public static void Undeclare(Wall w) => w.SetData(WallKey, null);

        /// <summary>
        /// On opening a project: from now on this app declares its shear walls (<see cref="DeclaredKey"/>). A project from before keeps
        /// its shear walls: each wall the model marked is declared over its whole length. Returns how many were declared so.
        /// </summary>
        public static int Adopt(Building b)
        {
            if (Declares(b)) return 0;
            int n = 0;
            foreach (var w in b.Walls)
            {
                var x = ReadWall(w);
                if (x.Shear != null || !w.IsShearWall || !(w.Length >= MinShearLength)) continue;
                x.Shear = new ShearPart { From = 0, To = Math.Round(w.Length, 4) };
                WriteWall(w, x);
                n++;
            }
            b.SetData(DeclaredKey, "1");
            return n;
        }

        /// <summary>
        /// The shear wall checks, on the declared parts (the model's own checks know no shear walls): for each level, a shear wall
        /// both ways reaching its diaphragm; a shear wall no diaphragm reaches; two shear walls of one story over each other.
        /// </summary>
        public static List<ModelIssue> Checks(Building b)
        {
            var list = new List<ModelIssue>();
            var shear = b.Walls.Select(w => (w, p: ShearPartOf(b, w))).Where(t => t.p != null && t.w.Direction != WallDirection.NotOrthogonal
                                                                              && b.Level(t.w.BaseLevelId) != null && b.Level(t.w.TopLevelId) != null).ToList();
            foreach (var l in Stories(b))
            {
                var net = b.NetDiaphragm(l);
                if (net.IsEmpty) continue;
                foreach (var dir in new[] { WallDirection.WestEast, WallDirection.SouthNorth })
                {
                    bool any = shear.Any(t => t.w.Direction == dir && StoriesOf(b, t.w).Contains(l) && Reaches(net, t.w, t.p));
                    if (!any)
                        list.Add(new ModelIssue
                        {
                            Severity = Severity.Warning, Code = "NoLateralSupport", LevelId = l.Id, Ids = { l.Id },
                            Message = $"{l.Name}: no shear wall {(dir == WallDirection.WestEast ? "running W-E" : "running S-N")} is declared under it. " +
                                      "Pick a wall on the plan and make it a shear wall.",
                        });
                }
            }
            foreach (var (w, p) in shear)
            {
                var top = b.Level(w.TopLevelId);
                var stories = StoriesOf(b, w);
                if (stories.Count > 0 && !stories.Any(l => Reaches(b.NetDiaphragm(l), w, p)))
                    list.Add(new ModelIssue
                    {
                        Severity = Severity.Warning, Code = "ShearWallNotReached", LevelId = top.Id, Ids = { w.Id }, At = Mid(w, p),
                        Message = $"Shear wall {w.Id}: no diaphragm reaches its shear part, so it takes no load.",
                    });
            }
            for (int i = 0; i < shear.Count; i++)
                for (int j = i + 1; j < shear.Count; j++)
                {
                    var (a, pa) = shear[i];
                    var (c, pc) = shear[j];
                    if (a.Direction != c.Direction) continue;
                    bool we = a.Direction == WallDirection.WestEast;
                    if (Math.Abs(we ? a.Start.Y - c.Start.Y : a.Start.X - c.Start.X) > 1e-6) continue;
                    var ea = Ends(a, pa); var ec = Ends(c, pc);
                    double Lo((Point2 a, Point2 e) x) => we ? Math.Min(x.a.X, x.e.X) : Math.Min(x.a.Y, x.e.Y);
                    double Hi((Point2 a, Point2 e) x) => we ? Math.Max(x.a.X, x.e.X) : Math.Max(x.a.Y, x.e.Y);
                    double lo = Math.Max(Lo(ea), Lo(ec)), hi = Math.Min(Hi(ea), Hi(ec));
                    if (hi - lo <= 1e-6) continue;
                    if (!StoriesOf(b, a).Intersect(StoriesOf(b, c)).Any()) continue;
                    list.Add(new ModelIssue
                    {
                        Severity = Severity.Error, Code = "WallOverlap", LevelId = a.TopLevelId, Ids = { a.Id, c.Id }, At = Mid(a, pa),
                        Message = $"Shear walls {a.Id} and {c.Id} overlap over {hi - lo:0.##} ft: the same wall is counted twice. Shorten one's shear part, or make one not a shear wall.",
                    });
                }
            return list;
        }

        private static bool Reaches(Region net, Wall w, ShearPart p)
        {
            var (a, e) = Ends(w, p);
            return net.LengthAlong(a, e, BuildingQueries.WallTolerance) > 0;
        }

        private static Point2 Mid(Wall w, ShearPart p)
        {
            var (a, e) = Ends(w, p);
            return new Point2((a.X + e.X) / 2, (a.Y + e.Y) / 2);
        }

        public sealed class DiaphragmExtras
        {
            public DiaphragmTypes Type { get; set; } = DiaphragmTypes.DIAPHRAGM_RIGID;
        }

        public static WallExtras ReadWall(Wall w) =>
            Parse<WallExtras>(w.GetData(WallKey)) ?? new WallExtras();

        public static void WriteWall(Wall w, WallExtras x) => w.SetData(WallKey, JsonConvert.SerializeObject(x, Json));

        public static DiaphragmExtras ReadDiaphragm(Diaphragm d) => Parse<DiaphragmExtras>(d.GetData(DiaphragmKey)) ?? new DiaphragmExtras();

        private static T Parse<T>(string s) where T : class
        {
            if (string.IsNullOrEmpty(s)) return null;
            try { return JsonConvert.DeserializeObject<T>(s, Json); }
            catch (JsonException) { return null; }
        }

        /// <summary>The numeric part of an ID (W12 → 12); null when it has none.</summary>
        public static int? Number(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            int i = 0;
            while (i < id.Length && !char.IsDigit(id[i])) i++;
            return int.TryParse(id.Substring(i), NumberStyles.None, CultureInfo.InvariantCulture, out int n) ? n : (int?)null;
        }

        /// <summary>The designer's stories: every level above the ground, bottom up.</summary>
        public static List<Level> Stories(Building b) => b.LevelsBottomUp.Where(l => l.Elevation > 0).ToList();

        /// <summary>
        /// Puts the model into the designer's calculators, one per story (reusing <paramref name="existing"/> in order,
        /// so their loads stay; each is made rigid, flexible or the envelope as the project settings say, DesignSettings.Diaphragm). The walls and diaphragms of
        /// each are replaced from the model. Returns the stories, bottom up.
        /// </summary>
        public static List<StoryMap> ToCalculators(Building b, IList<ShearWallCalculatorBase> existing = null)
        {
            var settings = Parse<DesignSettings>(b.GetData(SettingsKey)) ?? existing?.FirstOrDefault()?.DesignSettings ?? new DesignSettings();
            var stories = new List<StoryMap>();
            var levels = Stories(b);
            for (int i = 0; i < levels.Count; i++)
            {
                var level = levels[i];
                var calc = DiaphragmAnalysis.Ensure(existing != null && i < existing.Count && existing[i] != null ? existing[i] : DiaphragmAnalysis.Create(settings.Diaphragm), settings.Diaphragm);
                calc.DesignSettings = settings;
                var map = new StoryMap { Level = level, Calculator = calc, PlateHeight = b.StoryHeight(level) };
                Fill(b, map);
                stories.Add(map);
            }
            return stories;
        }

        private static void Fill(Building b, StoryMap map)
        {
            var calc = map.Calculator;
            var level = map.Level;
            if (calc._wall_system == null) calc._wall_system = new WallSystem();
            if (calc._diaphragm_system == null) calc._diaphragm_system = new DiaphragmSystem();
            calc._wall_system._walls.Clear();
            calc._wall_system.EW_Walls.Clear();
            calc._wall_system.NS_Walls.Clear();
            calc._diaphragm_system._diaphragms.Clear();

            // the net diaphragm as rectangles, each given to the model diaphragm that holds its centre
            var mine = b.DiaphragmsOn(level).ToList();
            var cells = b.NetDiaphragm(level).Cells().ToList();
            var byDiaphragm = mine.ToDictionary(d => d, d => new List<Rect2>());
            foreach (var c in cells)
            {
                double cx = (c.X0 + c.X1) / 2, cy = (c.Y0 + c.Y1) / 2;
                var owner = mine.FirstOrDefault(d => d.Pieces.Any(p => p.Contains(cx, cy))) ?? mine.FirstOrDefault();
                if (owner != null) byDiaphragm[owner].Add(c);
            }
            var usedKeys = new HashSet<int>();
            foreach (var kv in byDiaphragm)
            {
                int n = Number(kv.Key.Id) ?? 0;
                var type = ReadDiaphragm(kv.Key).Type;
                for (int k = 0; k < kv.Value.Count; k++)
                {
                    int key = kv.Value.Count == 1 ? n : n * 100 + k + 1;
                    while (usedKeys.Contains(key)) key += 10000;
                    usedKeys.Add(key);
                    var r = kv.Value[k];
                    var d = new DiaphragmData_Rectangular(new Point(r.X0, r.Y0), new Point(r.X1, r.Y1)) { ID = key, DiaphragmType = type };
                    calc._diaphragm_system._diaphragms[key] = d;
                    map.Diaphragms[key] = kv.Key.Id;
                }
            }

            // the shear walls under the level that its diaphragm reaches: each its declared part (its ends are the hold-downs')
            var net = b.NetDiaphragm(level);
            foreach (var w in b.WallsUnder(level).Where(w => w.Direction != WallDirection.NotOrthogonal))
            {
                var part = ShearPartOf(b, w);
                if (part == null) continue; // not a shear wall: it carries only its gravity load
                var (p0, p1) = Ends(w, part);
                if (!(net.LengthAlong(p0, p1, BuildingQueries.WallTolerance) > 0)) continue;
                int key = Number(w.Id) ?? 0;
                if (calc._wall_system._walls.ContainsKey(key)) continue;
                double height = HeightAt(b, w, level);
                if (!(height > 0)) continue;
                var extras = ReadWall(w);
                var data = new WallData(height, p0.X, p0.Y, p1.X, p1.Y) { ID = key, WallType = extras.WallType };
                if (extras.Design.TryGetValue(level.Id, out var design) && design != null)
                {
                    data.Design = design;
                    // an untouched default follows the wall: it moved onto or off the outside edge
                    if (extras.AutoExterior != null && extras.AutoExterior.TryGetValue(level.Id, out bool wasExterior) && wasExterior != b.IsExterior(w))
                    {
                        data.Design = NewWallDesign(b, w, calc.DesignSettings);
                        DefaultNote(w, level, b.IsExterior(w), data.Design, switched: true);
                    }
                }
                else
                {
                    data.Design = NewWallDesign(b, w, calc.DesignSettings); // a new wall: the project's exterior or interior default
                    DefaultNote(w, level, b.IsExterior(w), data.Design, switched: false);
                }
                calc._wall_system._walls[key] = data;
                map.Walls[key] = w.Id;
            }
            calc._wall_system.Update();
            calc._diaphragm_system.Update();
        }

        private static readonly HashSet<string> defaultNotes = new HashSet<string>();

        /// <summary>
        /// Diagnostics: a wall given the exterior or interior default (new, or its untouched default following it on or off the
        /// outside edge, e.g. when a floor grows past it). Logged once per wall, level and kind: why its sheathing changed
        /// without being picked, and that the interior default's gypsum has no edge-nailing choice.
        /// </summary>
        private static void DefaultNote(Wall w, Level level, bool exterior, WallDesignInputs design, bool switched)
        {
            if (!defaultNotes.Add($"{w.Id}|{level.Id}|{exterior}|{switched}")) return;
            var f1 = design?.EffectiveFace1();
            var f2 = design?.EffectiveFace2();
            string face = f1 == null ? "none" : $"{f1.Material} {f1.Thickness}".Replace("SHEATH_MAT_", "").Replace("THICKNESS_", "") +
                          (f1.Table != SheathingTable.WoodPanels ? $" (Table 4.3C row {f1.Row}: its fastener spacing is fixed, no edge-nailing choice)" : $", edge nailing {f1.EdgeSpacing}\"") +
                          (f2 != null ? ", both faces" : ", one face");
            string kind = exterior ? "exterior" : "interior";
            BuildingModel.Diagnostics.Log.Info("Design", switched
                ? $"{w.Id} ({level.Name}) is now an {kind} wall ({(exterior ? "it reaches" : "it no longer reaches")} the building's outside edge) and its sheathing was " +
                  $"still the {(exterior ? "interior" : "exterior")} default, so it follows: now the {kind} default, {face}. Pick a sheathing on the Design tab to keep one."
                : $"{w.Id} ({level.Name}) starts with the {kind} default: {face}.");
        }

        /// <summary>
        /// The design a wall starts with: Structural I (or the project's exterior default) when any part of its length is on
        /// the building's outside edge, gypsum wallboard on both faces (or the project's interior default) otherwise.
        /// </summary>
        public static WallDesignInputs NewWallDesign(Building b, Wall w, DesignSettings settings) =>
            (settings ?? new DesignSettings()).NewWallDesignFor(b.IsExterior(w));

        /// <summary>
        /// A wall's height for the diaphragm of <paramref name="level"/>: from the level below that braces it (or its base)
        /// up to this level.
        /// </summary>
        public static double HeightAt(Building b, Wall w, Level level)
        {
            var lo = b.Level(w.BaseLevelId);
            if (lo == null) return 0;
            double from = lo.Elevation;
            foreach (var br in b.BracedAt(w))
                if (br.Level.Elevation < level.Elevation && br.Level.Elevation > from) from = br.Level.Elevation;
            return level.Elevation - from;
        }

        /// <summary>
        /// Writes what the designer changed in its calculators back into the model's bags: each wall's design inputs on
        /// that story, its type, and the design settings. Geometry is not written back (it is edited in the sketch).
        /// Returns true when a bag changed.
        /// </summary>
        public static bool StoreDesign(Building b, IEnumerable<StoryMap> stories)
        {
            bool changed = false;
            ShearWallCalculatorBase any = null;
            foreach (var s in stories)
            {
                any = any ?? s.Calculator;
                foreach (var kv in s.Walls)
                {
                    if (!(b.Find(kv.Value) is Wall w) || !s.Calculator._wall_system._walls.TryGetValue(kv.Key, out var data)) continue;
                    var x = ReadWall(w);
                    string before = w.GetData(WallKey);
                    TrackDefault(b, w, s, x, data.Design);
                    x.Design[s.Level.Id] = data.Design;
                    x.WallType = data.WallType;
                    WriteWall(w, x);
                    changed |= before != w.GetData(WallKey);
                }
                foreach (var kv in s.Diaphragms)
                {
                    if (!(b.Find(kv.Value) is Diaphragm d) || !s.Calculator._diaphragm_system._diaphragms.TryGetValue(kv.Key, out var data)) continue;
                    string before = d.GetData(DiaphragmKey);
                    d.SetData(DiaphragmKey, data.DiaphragmType == DiaphragmTypes.DIAPHRAGM_RIGID ? null : JsonConvert.SerializeObject(new DiaphragmExtras { Type = data.DiaphragmType }, Json));
                    changed |= before != d.GetData(DiaphragmKey);
                }
            }
            if (any?.DesignSettings != null)
            {
                string before = b.GetData(SettingsKey);
                b.SetData(SettingsKey, JsonConvert.SerializeObject(any.DesignSettings, Json));
                changed |= before != b.GetData(SettingsKey);
            }
            return changed;
        }

        /// <summary>
        /// Keeps <see cref="WallExtras.AutoExterior"/> for one wall and story: still the default for where the wall is (a new
        /// wall, or one that followed a move) → marked; changed from what was stored (the user edited it) → no longer marked.
        /// </summary>
        private static void TrackDefault(Building b, Wall w, StoryMap s, WallExtras x, WallDesignInputs now)
        {
            string id = s.Level.Id;
            bool exterior = b.IsExterior(w);
            bool stored = x.Design.TryGetValue(id, out var before) && before != null;
            bool isDefault = now != null && Same(now, NewWallDesign(b, w, s.Calculator.DesignSettings));
            bool wasAuto = x.AutoExterior != null && x.AutoExterior.ContainsKey(id);
            if (isDefault && (!stored || wasAuto))
            {
                if (x.AutoExterior == null) x.AutoExterior = new Dictionary<string, bool>();
                x.AutoExterior[id] = exterior;
            }
            else if (wasAuto && !(stored && Same(now, before)))
            {
                x.AutoExterior.Remove(id);
                if (x.AutoExterior.Count == 0) x.AutoExterior = null;
            }
        }

        private static bool Same(WallDesignInputs a, WallDesignInputs b) =>
            JsonConvert.SerializeObject(a, Json) == JsonConvert.SerializeObject(b, Json);

        // ------------------------------------------------------------------ stories

        /// <summary>A new building with one story per plate height (bottom first; the default story height when none or not more than 0), named by <see cref="BuildingEdits.NameStories"/>.</summary>
        public static Building NewBuilding(IList<double> plateHeights, double? fallbackHeight = null)
        {
            var b = Building.New();
            double fallback = fallbackHeight ?? b.Defaults.StoryHeight;
            var heights = plateHeights != null && plateHeights.Count > 0 ? plateHeights : new[] { fallback };
            foreach (double h in heights) BuildingEdits.AddStory(b, h > 0 ? h : fallback);
            BuildingEdits.NameStories(b);
            return b;
        }


        /// <summary>
        /// After <see cref="BuildingEdits.CopyStory"/>: a copied wall's design was kept for the level it came from; it becomes
        /// its design on the new level too. Returns how many walls got one.
        /// </summary>
        public static int CopyDesignUp(Building b, Level from, Level to)
        {
            int n = 0;
            foreach (var w in b.Walls.Where(w => w.TopLevelId == to.Id))
            {
                var x = ReadWall(w);
                if (x.Design.ContainsKey(to.Id) || !x.Design.TryGetValue(from.Id, out var d) || d == null) continue;
                x.Design[to.Id] = d.Clone();
                WriteWall(w, x);
                n++;
            }
            return n;
        }

        /// <summary>
        /// An older designer project (calculators only) as a building model: level n at the sum of the plate heights
        /// (or each story's tallest wall, else 9 ft), each diaphragm rectangle a diaphragm, each wall a shear wall of its
        /// story with its design, declared over its whole length. Keys become IDs where they are free (W3 for wall 3 of the first story).
        /// </summary>
        public static Building FromCalculators(IList<ShearWallCalculatorBase> calcs, IList<double> plateHeights = null, string name = "")
        {
            var b = Building.New(name);
            b.SetData(DeclaredKey, "1"); // its shear walls are declared (below)
            var below = b.Ground;
            double z = 0;
            for (int i = 0; i < calcs.Count; i++)
            {
                var c = calcs[i];
                var walls = c?._wall_system?._walls ?? new Dictionary<int, WallData>();
                double h = plateHeights != null && i < plateHeights.Count && plateHeights[i] > 0 ? plateHeights[i]
                         : walls.Count > 0 ? walls.Values.Max(w => w.WallHeight) : 9;
                z += h;
                var level = b.AddLevel(i == calcs.Count - 1 && calcs.Count > 1 ? "Roof" : "Level " + (i + 2), z);
                foreach (var kv in (c?._diaphragm_system?._diaphragms ?? new Dictionary<int, DiaphragmData_Rectangular>()).OrderBy(kv => kv.Key))
                {
                    var d = new Diaphragm { Id = Free(b, "D", kv.Key), LevelId = level.Id, Pieces = { new Rect2(kv.Value.P1.X, kv.Value.P1.Y, kv.Value.P3.X, kv.Value.P3.Y) } };
                    if (kv.Value.DiaphragmType != DiaphragmTypes.DIAPHRAGM_RIGID)
                        d.SetData(DiaphragmKey, JsonConvert.SerializeObject(new DiaphragmExtras { Type = kv.Value.DiaphragmType }, Json));
                    b.Diaphragms.Add(d);
                }
                foreach (var kv in walls.OrderBy(kv => kv.Key))
                {
                    var w = new Wall
                    {
                        Id = Free(b, "W", kv.Key),
                        Start = new Point2(kv.Value.Start.X, kv.Value.Start.Y),
                        End = new Point2(kv.Value.End.X, kv.Value.End.Y),
                        BaseLevelId = below.Id,
                        TopLevelId = level.Id,
                    };
                    var x = new WallExtras { WallType = kv.Value.WallType };
                    x.Shear = new ShearPart { From = 0, To = Math.Round(w.Length, 4) }; // each wall of the older project was a shear wall: declared, whole
                    // an older wall without a design used the program default (Structural I): kept as it was, not a "new" wall
                    x.Design[level.Id] = kv.Value.Design ?? new WallDesignInputs();
                    WriteWall(w, x);
                    b.Walls.Add(w);
                }
                below = level;
            }
            var settings = calcs.FirstOrDefault(c => c != null)?.DesignSettings;
            if (settings != null) b.SetData(SettingsKey, JsonConvert.SerializeObject(settings, Json));
            return b;
        }

        private static string Free(Building b, string prefix, int key)
        {
            string id = prefix + key;
            return key > 0 && b.Find(id) == null ? id : b.NewId(prefix);
        }
    }
}
