using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using ASCE7WindLoadCalculator;
using ASCE7WindLoadCalculator.ModelAdapter;
using BuildingModel.Checks;
using BuildingModel.Model;
using BuildingModel.Serialization;
using ShearWallCalculator.Design;

namespace ShearWallCalculator.ModelAdapter
{
    /// <summary>A wall, hold-down or connection the check finds failing.</summary>
    public sealed class CheckFailure
    {
        /// <summary>The model's wall (or column, beam) it is about; null when it has none.</summary>
        public string Id { get; set; }
        public string LevelId { get; set; }
        public string LevelName { get; set; }
        /// <summary>"W4 fails on Level 2: shear 412 plf over the capacity 380 plf."</summary>
        public string Message { get; set; }
    }

    /// <summary>What <see cref="ShearWallCheck.Run"/> found.</summary>
    public sealed class ShearWallCheckResult
    {
        /// <summary>Why nothing was checked (no shear walls, model errors, no wind); null when it was.</summary>
        public string NotChecked { get; set; }
        public List<StoryMap> Stories { get; set; } = new List<StoryMap>();
        public List<Dictionary<int, WallDesignResult>> ByStory { get; set; } = new List<Dictionary<int, WallDesignResult>>();
        public LoadPathTrace LoadPath { get; set; }
        public List<CheckFailure> Failures { get; } = new List<CheckFailure>();
        public int WallsChecked => ByStory.Sum(s => s.Count);
        /// <summary>The foundation's hold-down points as this design gives them (what the shear wall app saves).</summary>
        public LateralSection Lateral { get; set; }
        public bool Passes => NotChecked == null && Failures.Count == 0;
    }

    /// <summary>
    /// The shear wall design run on the project as it is, with no window: the same calculators, wind cases, design pass
    /// (<see cref="DesignPass"/>) and load path as the Shear Wall Calculator, from the model, the wind section and the walls'
    /// design inputs in the file. Reads only: the hub runs it to tell the user when a change elsewhere (the building, the
    /// wind) makes a wall or a hold-down fail, before anyone opens the shear wall app.
    /// </summary>
    public static class ShearWallCheck
    {
        /// <summary>The parts of the project the design is based on (<see cref="ModelFingerprint"/>).</summary>
        public const string WindPart = "wind", DesignPart = "shear walls";

        /// <summary>The fingerprints of what the shear wall design uses, as the project is now.</summary>
        public static Dictionary<string, string> InputsNow(Building b) => new Dictionary<string, string>
        {
            [ModelFingerprint.GeometryPart] = ModelFingerprint.Geometry(b),
            [WindPart] = ModelFingerprint.Data(b, WindSection.Key),
            [DesignPart] = ModelFingerprint.AppData(b, ShearWallAdapter.WallKey, ShearWallAdapter.SettingsKey, ShearWallAdapter.DeclaredKey),
        };

        /// <summary>
        /// A designer as the shear wall app makes one: the Simpson catalog and the user's standard hold-down list (read
        /// only; the company default next to the program when the user has none).
        /// </summary>
        public static ShearWallDesigner NewDesigner()
        {
            var designer = new ShearWallDesigner(null, new SimpsonCatalog());
            try
            {
                var list = HoldownStandardList.Load(HoldownStandardList.DefaultPath);
                if (list.Count == 0) list = HoldownStandardList.Load(HoldownStandardList.PackagedPath);
                designer.StandardList = list;
            }
            catch (Exception ex) when (ex is System.IO.IOException || ex is UnauthorizedAccessException || ex is Newtonsoft.Json.JsonException) { }
            return designer;
        }

        /// <summary>The model's beams and columns by story (for the load path and the details).</summary>
        public static (List<FramingBeam> beams, List<FramingColumn> columns) Framing(Building b, IList<StoryMap> stories)
        {
            var beams = new List<FramingBeam>();
            var columns = new List<FramingColumn>();
            if (b == null || stories == null || stories.Count == 0) return (beams, columns);
            int StoryOf(string levelId) => stories.ToList().FindIndex(s => s.Level.Id == levelId);
            foreach (var bm in b.Beams)
            {
                int i = StoryOf(bm.LevelId);
                if (i >= 0) beams.Add(new FramingBeam { Id = bm.Id, Start = new Point(bm.Start.X, bm.Start.Y), End = new Point(bm.End.X, bm.End.Y), OnStory = i });
            }
            foreach (var c in b.Columns)
            {
                int top = StoryOf(c.TopLevelId);
                int below = StoryOf(c.BaseLevelId);
                int bas = b.Ground != null && c.BaseLevelId == b.Ground.Id ? 0 : below < 0 ? -1 : below + 1;
                if (top >= 0 && bas >= 0 && bas <= top) columns.Add(new FramingColumn { Id = c.Id, At = new Point(c.At.X, c.At.Y), BaseStory = bas, TopStory = top });
            }
            return (beams, columns);
        }

        /// <summary>
        /// The wind calculator's own wind cases on every story (each story its strip of the building), as the shear wall
        /// app takes them. Returns the per-story result (null with no levels).
        /// </summary>
        public static StoryWindCaseResult SetWindLoads(IList<ShearWallCalculatorBase> levels, WindAnalysisResult analysis, bool onlyCases1And3)
        {
            if (levels == null || levels.Count == 0) return null;
            int n = Math.Max(1, Math.Min(levels.Count, BuildingData.MaxStories));
            var r = StoryWindLoads.ComputeCases(analysis, onlyCases1And3, n);
            for (int i = 0; i < levels.Count; i++)
                levels[i].SetLoadCases(i < r.Levels.Count
                    ? r.Levels[i].Select(c => new LateralLoadCase(c.Name, c.Vx, c.Vy, c.Mt, applyMinimumEccentricity: false))
                    : Enumerable.Empty<LateralLoadCase>());
            return r;
        }

        /// <summary>
        /// Runs the design on <paramref name="building"/> as it is (a copy: the building is not changed). Nothing is checked
        /// without declared shear walls, with model errors (no design, as in the app) or without a wind section.
        /// </summary>
        public static ShearWallCheckResult Run(Building building, ShearWallDesigner designer = null)
        {
            var result = new ShearWallCheckResult();
            if (building == null) { result.NotChecked = "no project"; return result; }
            var b = ModelFile.Clone(building);
            ShearWallAdapter.Adopt(b); // an older project's shear walls, as the app takes them
            if (ShearWallAdapter.ShearWalls(b).Count == 0) { result.NotChecked = "no shear walls declared"; return result; }
            int errors = ModelChecks.Run(b).Concat(ShearWallAdapter.Checks(b)).Count(i => i.Severity == Severity.Error);
            if (errors > 0) { result.NotChecked = $"the building has {errors} error{(errors == 1 ? "" : "s")}, so the shear walls are not designed"; return result; }
            var ws = WindSection.Read(b);
            if (ws == null) { result.NotChecked = "no wind set up"; return result; }

            var stories = ShearWallAdapter.ToCalculators(b);
            result.Stories = stories;
            var levels = stories.Select(s => s.Calculator).ToList();
            try
            {
                var input = ws.BuildingFor(b);
                SetWindLoads(levels, WindAnalysis.Run(ws.Edition, ws.ToParameters(), input.Building), ws.OnlyCases1And3);
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentException || ex is NullReferenceException || ex is IndexOutOfRangeException)
            {
                result.NotChecked = "the wind could not be calculated for this building: " + ex.Message;
                return result;
            }
            var (beams, columns) = Framing(b, stories);
            StoryCascade.Analyze(levels, i => beams.Where(x => x.OnStory == i).Select(x => (x.Start, x.End)), i => i >= 0 && i < stories.Count ? stories[i].Level.Name : null);
            var (byStory, trace) = DesignPass.All(levels, designer ?? NewDesigner(), beams, columns);
            result.ByStory = byStory;
            result.LoadPath = trace;

            for (int i = 0; i < stories.Count; i++)
            {
                var s = stories[i];
                foreach (var kv in byStory[i].Where(kv => !kv.Value.Pass))
                {
                    string id = s.Walls.TryGetValue(kv.Key, out var m) ? m : $"W{kv.Key}";
                    result.Failures.Add(new CheckFailure { Id = id, LevelId = s.Level.Id, LevelName = s.Level.Name, Message = $"{id} fails on {s.Level.Name}: {kv.Value.FailureReason}." });
                }
            }
            foreach (var kv in trace.ByLevel)
            {
                var lvl = kv.Key >= 0 && kv.Key < stories.Count ? stories[kv.Key].Level : b.Ground;
                string where = kv.Key >= 0 ? lvl?.Name : "the foundation";
                foreach (var l in kv.Value)
                {
                    var from = l.UpperStory - 1 >= 0 && l.UpperStory - 1 < stories.Count ? stories[l.UpperStory - 1] : null;
                    string wallId = from != null && from.Walls.TryGetValue(l.WallId, out var id) ? id : $"wall {l.WallId}";
                    string what = l.Kind == PathPointKind.WallHoldown ? $"A hold-down of {wallId}" : $"The {l.From} (carrying {wallId}'s hold-down)";
                    if (!l.Supported)
                        result.Failures.Add(new CheckFailure { Id = from != null && from.Walls.TryGetValue(l.WallId, out var w1) ? w1 : null, LevelId = lvl?.Id, LevelName = where, Message = $"{what} lands on {where} with nothing under it." });
                    else if (l.Choice == null && l.Kind != PathPointKind.WallHoldown)
                        result.Failures.Add(new CheckFailure { Id = null, LevelId = lvl?.Id, LevelName = where, Message = $"{what}: no connection strong enough for {l.TensionAsd:N0} lb on {l.SupportId ?? where}." });
                }
            }
            var anchors = trace.ByLevel.TryGetValue(-1, out var a) ? a : new List<HoldownLanding>();
            result.Lateral = Lateral(levels, stories, byStory, anchors, 0);
            if (result.Lateral != null) result.Lateral.BasedOn = InputsNow(building);
            return result;
        }

        /// <summary>
        /// What the lateral design puts on the foundation (the model's "Lateral" section, written on every save of the shear
        /// wall app): each first-story shear wall's two ends, per wind case, the overturning couple alone (its hold-down's
        /// uplift at one end, the same pressing down at the other) at the wind's own level (the ASD couple ÷ 0.6), with no
        /// dead load (the load manager adds its own), and each end's hold-down. Column bases carrying hold-downs from above
        /// are added as the ASD force ÷ 0.6. Null when there is no design yet.
        /// </summary>
        public static LateralSection Lateral(IList<ShearWallCalculatorBase> levels, IList<StoryMap> stories,
            IList<Dictionary<int, WallDesignResult>> byStory, IEnumerable<HoldownLanding> foundationAnchors, int revision)
        {
            if (levels == null || levels.Count == 0 || byStory == null || byStory.Count == 0 || levels[0]?._wall_system == null) return null;
            var design = byStory[0];
            if (design == null) return null;
            var s = new LateralSection
            {
                Source = "Shear Wall Calculator",
                Saved = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'"),
                ModelRevision = revision,
                Note = "Each shear wall end's overturning couple by wind case, W at strength level (this app's ASD couple ÷ 0.6), the walls above " +
                       "included as designed; no dead load (the load manager's is added).",
            };
            // one entry per wind case of the wind calculator (its own name). The wind is the same whether the diaphragm is taken rigid or
            // flexible (the pressures come from the geometry); the two are only two ways of sharing it among the walls, so each
            // wall end takes the governing of the two for that wind ("Case 1, ... (rigid)" and "(flexible)" become "Case 1, ...")
            string Bare(string key)
            {
                foreach (var tag in new[] { ShearWallCalculator_Envelope.RigidTag, ShearWallCalculator_Envelope.FlexibleTag })
                {
                    string tail = $" ({tag})";
                    if (key.EndsWith(tail, StringComparison.Ordinal)) return key.Substring(0, key.Length - tail.Length);
                }
                return key;
            }
            void Govern(Dictionary<string, double> d, string key, double v)
            {
                if (!d.TryGetValue(key, out var was) || Math.Abs(v) > Math.Abs(was)) d[key] = v;
            }
            var cases = new List<string>();
            void Case(string c) { if (!cases.Contains(c)) cases.Add(c); }
            foreach (var kv in levels[0]._wall_system._walls)
            {
                if (!design.TryGetValue(kv.Key, out var r) || r?.EndForceByCase == null || r.EndForceByCase.Count == 0) continue;
                var w = kv.Value;
                var (start, end) = WallSchedule.HoldownPoints(w, r.Holdown);
                bool ew = w.WallDir == calculator.WallDirs.EastWest;
                bool lowIsStart = ew ? w.Start.X <= w.End.X : w.Start.Y <= w.End.Y;
                var low = lowIsStart ? start : end;
                var high = lowIsStart ? end : start;
                string modelId = stories != null && stories.Count > 0 && stories[0].Walls.TryGetValue(kv.Key, out var id) ? id : null;
                string name = modelId ?? $"W{kv.Key}";
                var upLow = new Dictionary<string, double>();
                var upHigh = new Dictionary<string, double>();
                foreach (var c in r.EndForceByCase)
                {
                    // [low, high]: the couple alone, + up at the low end (no dead load in this app)
                    double couple = Math.Round(c.Value[0] / 0.6, 1);
                    string k = Bare(c.Key);
                    Govern(upLow, k, couple);
                    upHigh[k] = -upLow[k];
                    Case(k);
                }
                s.Points.Add(new LateralPoint { From = $"{name} {(ew ? "west" : "south")} end", WallId = modelId, X = low.X, Y = low.Y, Up = upLow, Connector = r.Holdown?.Model });
                s.Points.Add(new LateralPoint { From = $"{name} {(ew ? "east" : "north")} end", WallId = modelId, X = high.X, Y = high.Y, Up = upHigh, Connector = r.Holdown?.Model });
            }
            foreach (var a in (foundationAnchors ?? Enumerable.Empty<HoldownLanding>()).Where(a => a.Kind == PathPointKind.ColumnBase && a.CaseAsd != null && a.CaseAsd.Count > 0))
            {
                var up = new Dictionary<string, double>();
                foreach (var kv in a.CaseAsd) { string k = Bare(kv.Key); Govern(up, k, Math.Round(kv.Value / 0.6, 1)); Case(k); }
                s.Points.Add(new LateralPoint { From = a.From, X = a.At.X, Y = a.At.Y, Up = up, Connector = a.Model });
            }
            // the wind calculator's case names, nothing else about the wind: the load manager takes each case (its direction,
            // its roof pressures) from the wind calculator itself
            s.Cases = cases;
            return s.IsEmpty ? null : s;
        }
    }
}
