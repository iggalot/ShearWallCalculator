using calculator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace ShearWallCalculator
{
    /// <summary>
    /// Multi-story shear walls, analysed from the top story down. Each level is one calculator: the walls of that story and
    /// the diaphragm on top of them (level 1 = the first story and the floor over it; the top level = the top story and the
    /// roof). A level's diaphragm takes its own wind (its strip of the walls) plus the reactions of the walls of the story
    /// above, which stand on it: each upper wall pushes on this floor with minus its shear, at its own position. Walls that
    /// stack pass their shear straight down; a wall with no wall under it has its shear spread by the diaphragm below, which
    /// is the real load path.
    /// <para>Rigid diaphragm: the reactions add to the level's forces at the center of mass, plus their moment about it
    /// (Σ (x − x_m) F_y − (y − y_m) F_x), so the torsion about the center of rigidity comes out exactly. Flexible diaphragm:
    /// the same resultant and moment are used, which spreads the point loads like a uniform load plus torsion; an
    /// approximation, flagged on the level (point loads by simple-span statics are a later step).</para>
    /// </summary>
    public static class StoryCascade
    {
        /// <summary>Two walls are on the same line when they are this close across it (ft).</summary>
        public const double LineTolerance = 0.25;

        /// <summary>
        /// Starts a note that is information (an approximation the method makes), not something to fix: shown as a note, not a
        /// warning. <see cref="IsInfo"/> and <see cref="Text"/> read it.
        /// </summary>
        public const string InfoMark = "ⓘ ";
        public static bool IsInfo(string note) => note != null && note.StartsWith(InfoMark);
        public static string Text(string note) => IsInfo(note) ? note.Substring(InfoMark.Length) : note;

        /// <summary>Calculates every level, top first. <paramref name="levels"/> runs bottom to top. Returns what to check (per level).</summary>
        /// <param name="beamsOn">The beams in the framing on top of story i (under the walls of story i + 1), as segments; null: none.</param>
        /// <param name="nameOf">
        /// The name of story i's level: the level on top of it, whose plan its walls are drawn on ("Level 3"). Null: "level
        /// i + 1" (the first story is level 1).
        /// </param>
        public static List<string> Analyze(IList<ShearWallCalculatorBase> levels, Func<int, IEnumerable<(Point a, Point b)>> beamsOn = null,
            Func<int, string> nameOf = null)
        {
            var notes = new List<string>();
            if (levels == null || levels.Count == 0) return notes;
            string Name(int story) => nameOf?.Invoke(story) ?? $"level {story + 1}";
            string Cap(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1);
            int n = levels.Count;
            for (int i = n - 1; i >= 0; i--)
            {
                var calc = levels[i];
                if (calc == null) continue;
                calc.LoadsFromAbove = new Dictionary<string, List<PointLoad>>();
                var env = calc as ShearWallCalculator_Envelope;
                if (env != null) env.FlexibleLoadsFromAbove = null;
                if (i < n - 1 && levels[i + 1] != null)
                {
                    var upper = levels[i + 1];
                    bool upperHasWalls = upper._wall_system?._walls.Count > 0;
                    if (upperHasWalls && !upper.IsValidForCalculation)
                        notes.Add($"{Cap(Name(i + 1))} cannot be calculated yet (it needs a diaphragm and walls both ways), so its wind is not carried down to {Name(i)}.");
                    else if (env != null && upper is ShearWallCalculator_Envelope upperEnv)
                    {
                        // two whole-building runs: rigid reactions to the rigid run below, flexible to the flexible
                        calc.LoadsFromAbove = Reactions(upperEnv.Rigid, i + 2);
                        env.FlexibleLoadsFromAbove = Reactions(upperEnv.Flexible, i + 2);
                    }
                    else
                        calc.LoadsFromAbove = Reactions(upper, i + 2);
                    if ((calc is ShearWallCalculator_FlexibleDiaphragm || env != null) && calc.LoadsFromAbove.Count > 0)
                        notes.Add(InfoMark + $"{Cap(Name(i))}{(env != null ? ", flexible run of the envelope" : " is a flexible diaphragm")}: the forces of the walls above are spread over " +
                                  "this floor as a uniform load plus their torsion, not applied where each wall stands (an approximation of the flexible method; the rigid run applies them at the walls).");
                    notes.AddRange(Unsupported(upper, calc, i + 2, beamsOn?.Invoke(i), Cap(Name(i + 1)), Name(i)));
                }
                calc.PerformCalculations();
            }
            return notes;
        }

        /// <summary>The upper level's walls as loads on the diaphragm below, per load case: minus each wall's shear at the wall's middle.</summary>
        public static Dictionary<string, List<PointLoad>> Reactions(ShearWallCalculatorBase upper, int upperLevel)
        {
            var result = new Dictionary<string, List<PointLoad>>();
            var walls = upper?._wall_system?._walls;
            if (walls == null) return result;
            foreach (var c in upper.CaseWallTotals())
            {
                var list = new List<PointLoad>();
                foreach (var kv in c.Value ?? new Dictionary<int, double>())
                {
                    if (!walls.TryGetValue(kv.Key, out var w)) continue;
                    bool ew = w.WallDir == WallDirs.EastWest;
                    list.Add(new PointLoad
                    {
                        X = (w.Start.X + w.End.X) / 2, Y = (w.Start.Y + w.End.Y) / 2,
                        Fx = ew ? -kv.Value : 0, Fy = ew ? 0 : -kv.Value,
                        From = $"level {upperLevel}, wall {kv.Key}",
                    });
                }
                result[c.Key] = list;
            }
            return result;
        }

        /// <summary>
        /// This level's cases with the loads from above added. The reactions of an upper case go with the case of the same
        /// name; when no names match and this level has one case (a manual load), that load goes with every upper case.
        /// </summary>
        public static List<LateralLoadCase> WithLoadsFromAbove(List<LateralLoadCase> own, Dictionary<string, List<PointLoad>> above, Point centerOfMass)
        {
            if (above == null || above.Count == 0) return own;
            var result = new List<LateralLoadCase>();
            bool anyMatch = own.Any(c => above.ContainsKey(c.Name));
            if (!anyMatch && own.Count == 1)
            {
                foreach (var kv in above) result.Add(Merge(own[0], kv.Key, kv.Value, centerOfMass));
                return result;
            }
            foreach (var c in own)
                result.Add(Merge(c, c.Name, above.TryGetValue(c.Name, out var loads) ? loads : null, centerOfMass));
            foreach (var kv in above.Where(kv => !own.Any(c => c.Name == kv.Key)))
                result.Add(Merge(new LateralLoadCase(kv.Key, 0, 0), kv.Key, kv.Value, centerOfMass));
            return result;
        }

        private static LateralLoadCase Merge(LateralLoadCase c, string name, List<PointLoad> loads, Point cm)
        {
            double fx = 0, fy = 0, m = 0;
            foreach (var p in loads ?? new List<PointLoad>())
            {
                fx += p.Fx;
                fy += p.Fy;
                if (!double.IsNaN(cm.X) && !double.IsNaN(cm.Y)) m += (p.X - cm.X) * p.Fy - (p.Y - cm.Y) * p.Fx;
            }
            return new LateralLoadCase(name, c.Vx + fx, c.Vy + fy, c.Mt + m, c.ApplyMinimumEccentricity) { AboveVx = fx, AboveVy = fy, AboveMt = m };
        }

        /// <summary>
        /// Upper walls with part of their length over nothing on the level below (no wall and no beam of
        /// <paramref name="beams"/> on the same line): their weight and hold-downs need a load path; and upper walls that run
        /// past the edge of the floor below (<see cref="OffFloor"/>): that part's shear has nowhere to go.
        /// </summary>
        /// <param name="upperName">The upper walls' level, as named in the model (null: "Level <paramref name="upperLevel"/>").</param>
        /// <param name="lowerName">The level under them (the floor they stand on; null: "level <paramref name="upperLevel"/> − 1").</param>
        public static List<string> Unsupported(ShearWallCalculatorBase upper, ShearWallCalculatorBase lower, int upperLevel,
            IEnumerable<(Point a, Point b)> beams = null, string upperName = null, string lowerName = null)
        {
            var notes = new List<string>();
            upperName = upperName ?? $"Level {upperLevel}";
            lowerName = lowerName ?? $"level {upperLevel - 1}";
            var below = (lower?._wall_system?._walls.Values ?? Enumerable.Empty<WallData>()).Select(b => (b.Start, b.End))
                        .Concat(beams ?? Enumerable.Empty<(Point, Point)>()).ToList();
            var off = OffFloor(upper, lower);
            foreach (var kv in (upper?._wall_system?._walls ?? new Dictionary<int, WallData>()).OrderBy(k => k.Key))
            {
                double L = kv.Value.WallLength, under = SupportedLength(kv.Value, below);
                if (under < L - 0.1)
                    notes.Add($"{upperName}, wall {kv.Key}: {(under < 0.1 ? "no wall or beam" : $"a wall or beam under only {under:0.#} of {L:0.#} ft")} on {lowerName}. " +
                              "Its hold-downs need a post, beam or blocking under them.");
                if (off.TryGetValue(kv.Key, out double beyond))
                    notes.Add($"{upperName}, wall {kv.Key}: {(beyond >= L - 0.1 ? "all of it is" : $"{beyond:0.#} of its {L:0.#} ft is")} beyond the edge of the {lowerName} floor, " +
                              "so its shear has no diaphragm to go into. Extend the floor's diaphragm under it.");
            }
            return notes;
        }

        /// <summary>
        /// The hold-down forces of the upper level's walls that stand on <paramref name="lower"/> (same line, within
        /// <see cref="LineTolerance"/>): each upper wall with uplift lifts at one hold-down post (+T) and bears down at the
        /// other (−T), for each wind direction. The uplift counts when its post is over the lower wall; the bearing only
        /// when the uplift does too (a couple), so an upper wall hanging off the end never reduces the tension.
        /// </summary>
        public static List<Design.StackedForce> HoldownForcesOn(WallData lower, IDictionary<int, WallData> upperWalls,
            IReadOnlyDictionary<int, Design.WallDesignResult> upperDesign, int upperLevel)
        {
            var forces = new List<Design.StackedForce>();
            if (lower == null || upperWalls == null || upperDesign == null) return forces;
            bool ew = lower.WallDir == WallDirs.EastWest;
            double Lo(WallData w) => ew ? Math.Min(w.Start.X, w.End.X) : Math.Min(w.Start.Y, w.End.Y);
            double Hi(WallData w) => ew ? Math.Max(w.Start.X, w.End.X) : Math.Max(w.Start.Y, w.End.Y);
            double Line(WallData w) => ew ? w.Start.Y : w.Start.X;
            double lo = Lo(lower), hi = Hi(lower);
            bool On(double p) => p >= lo - LineTolerance && p <= hi + LineTolerance;
            foreach (var kv in upperWalls.OrderBy(k => k.Key))
            {
                var u = kv.Value;
                if (u.WallDir != lower.WallDir || Math.Abs(Line(u) - Line(lower)) > LineTolerance) continue;
                if (!upperDesign.TryGetValue(kv.Key, out var r) || !(r.TensionAsd > 0) || double.IsInfinity(r.TensionAsd)) continue;
                double half = r.PostAlongWall / 24.0;
                double a = Lo(u) + half, b = Hi(u) - half; // the upper wall's hold-down posts
                void Add(double up, double down, bool lowUp)
                {
                    if (!On(up)) return;
                    string from = $"level {upperLevel}, wall {kv.Key}";
                    forces.Add(new Design.StackedForce { Along = up - lo, Asd = r.TensionAsd, Lrfd = r.TensionLrfd, LowEndUp = lowUp, From = from });
                    if (On(down))
                        forces.Add(new Design.StackedForce { Along = down - lo, Asd = -r.TensionAsd, Lrfd = -r.TensionLrfd, LowEndUp = lowUp, From = from });
                }
                Add(a, b, true);  // wind lifting the low ends
                Add(b, a, false); // and the other way
            }
            return forces;
        }

        /// <summary>
        /// Where each hold-down of a story's walls lands: the post centerline at each end of every wall that needs hold-downs
        /// (<see cref="Design.WallSchedule.HoldownPoints"/>), and what is under it. With <paramref name="lowerWalls"/> null the
        /// story is the first one and every hold-down anchors to the FOUNDATION; otherwise it lands on a wall of the story below
        /// when one is on the same line and reaches the post (within <see cref="LineTolerance"/>), and has NO SUPPORT when
        /// none does (a beam, post or blocking is needed under it).
        /// </summary>
        public static List<HoldownLanding> Landings(IDictionary<int, WallData> upperWalls, IReadOnlyDictionary<int, Design.WallDesignResult> upperDesign,
                                                    IDictionary<int, WallData> lowerWalls, int upperStory)
        {
            var list = new List<HoldownLanding>();
            if (upperWalls == null || upperDesign == null) return list;
            foreach (var kv in upperWalls.OrderBy(k => k.Key))
            {
                if (!upperDesign.TryGetValue(kv.Key, out var r) || !r.HoldownNeeded) continue;
                var w = kv.Value;
                var (p1, p2) = Design.WallSchedule.HoldownPoints(w, r.Holdown);
                foreach (var p in new[] { p1, p2 })
                {
                    var l = new HoldownLanding
                    {
                        UpperStory = upperStory, WallId = kv.Key, At = p, TensionAsd = r.TensionAsd, Model = r.Holdown?.Model,
                        OnFoundation = lowerWalls == null,
                    };
                    if (lowerWalls != null) l.LowerWallId = WallUnder(p, w.WallDir, lowerWalls);
                    list.Add(l);
                }
            }
            return list;
        }

        /// <summary>The wall of <paramref name="walls"/> in the direction given that runs under the point (same line, between its ends), or null.</summary>
        public static int? WallUnder(Point p, WallDirs dir, IDictionary<int, WallData> walls)
        {
            foreach (var kv in walls.OrderBy(k => k.Key))
            {
                var b = kv.Value;
                if (b.WallDir != dir) continue;
                bool ew = dir == WallDirs.EastWest;
                if (Math.Abs((ew ? b.Start.Y : b.Start.X) - (ew ? p.Y : p.X)) > LineTolerance) continue;
                double lo = ew ? Math.Min(b.Start.X, b.End.X) : Math.Min(b.Start.Y, b.End.Y);
                double hi = ew ? Math.Max(b.Start.X, b.End.X) : Math.Max(b.Start.Y, b.End.Y);
                double at = ew ? p.X : p.Y;
                if (at >= lo - LineTolerance && at <= hi + LineTolerance) return kv.Key;
            }
            return null;
        }

        /// <summary>The length of <paramref name="w"/> that has a wall of <paramref name="below"/> on the same line under it (ft).</summary>
        public static double SupportedLength(WallData w, IEnumerable<WallData> below) =>
            SupportedLength(w, below.Where(b => b.WallDir == w.WallDir).Select(b => (b.Start, b.End)));

        /// <summary>
        /// The length of <paramref name="w"/> with something under it on the same line (ft): walls or beams of the level
        /// below, as segments; a segment counts when both its ends are within <see cref="LineTolerance"/> of the wall's line.
        /// </summary>
        public static double SupportedLength(WallData w, IEnumerable<(Point a, Point b)> below)
        {
            bool ew = w.WallDir == WallDirs.EastWest;
            double a0 = ew ? Math.Min(w.Start.X, w.End.X) : Math.Min(w.Start.Y, w.End.Y);
            double a1 = ew ? Math.Max(w.Start.X, w.End.X) : Math.Max(w.Start.Y, w.End.Y);
            double line = ew ? w.Start.Y : w.Start.X;
            var pieces = new List<(double, double)>();
            foreach (var (p, q) in below)
            {
                if (Math.Abs((ew ? p.Y : p.X) - line) > LineTolerance || Math.Abs((ew ? q.Y : q.X) - line) > LineTolerance) continue;
                double b0 = Math.Max(a0, ew ? Math.Min(p.X, q.X) : Math.Min(p.Y, q.Y));
                double b1 = Math.Min(a1, ew ? Math.Max(p.X, q.X) : Math.Max(p.Y, q.Y));
                // an end within the tolerance of the wall's end (a beam framing into a corner post) reaches it
                if (b0 - a0 <= LineTolerance) b0 = a0;
                if (a1 - b1 <= LineTolerance) b1 = a1;
                if (b1 > b0) pieces.Add((b0, b1));
            }
            return Union(pieces);
        }

        /// <summary>
        /// The length of <paramref name="w"/> over a diaphragm of <paramref name="floor"/> (ft): the part whose shear the floor
        /// under it can take (its edges count, within <see cref="LineTolerance"/>).
        /// </summary>
        public static double OnFloorLength(WallData w, IEnumerable<DiaphragmData_Rectangular> floor)
        {
            bool ew = w.WallDir == WallDirs.EastWest;
            double a0 = ew ? Math.Min(w.Start.X, w.End.X) : Math.Min(w.Start.Y, w.End.Y);
            double a1 = ew ? Math.Max(w.Start.X, w.End.X) : Math.Max(w.Start.Y, w.End.Y);
            double line = ew ? w.Start.Y : w.Start.X;
            var pieces = new List<(double, double)>();
            foreach (var d in floor)
            {
                double x0 = Math.Min(d.P1.X, d.P3.X), x1 = Math.Max(d.P1.X, d.P3.X), y0 = Math.Min(d.P1.Y, d.P3.Y), y1 = Math.Max(d.P1.Y, d.P3.Y);
                double c0 = ew ? y0 : x0, c1 = ew ? y1 : x1;
                if (line < c0 - LineTolerance || line > c1 + LineTolerance) continue;
                double b0 = Math.Max(a0, ew ? x0 : y0), b1 = Math.Min(a1, ew ? x1 : y1);
                if (b0 - a0 <= LineTolerance) b0 = a0; // a floor edge within the tolerance of the wall's end reaches it
                if (a1 - b1 <= LineTolerance) b1 = a1;
                if (b1 > b0) pieces.Add((b0, b1));
            }
            return Union(pieces);
        }

        /// <summary>
        /// The upper level's walls that run past the edge of the floor under them (key → ft beyond it): that part's shear has no
        /// diaphragm to go into. A beam or column under a wall carries its weight and uplift, not its shear.
        /// </summary>
        public static Dictionary<int, double> OffFloor(ShearWallCalculatorBase upper, ShearWallCalculatorBase lower)
        {
            var off = new Dictionary<int, double>();
            var floor = lower?._diaphragm_system?._diaphragms.Values.ToList();
            if (floor == null || floor.Count == 0) return off; // no floor at all: the level cannot be calculated, said elsewhere
            foreach (var kv in upper?._wall_system?._walls ?? new Dictionary<int, WallData>())
            {
                double beyond = kv.Value.WallLength - OnFloorLength(kv.Value, floor);
                if (beyond > 0.1) off[kv.Key] = beyond;
            }
            return off;
        }

        /// <summary>The total length of the union of the intervals.</summary>
        private static double Union(List<(double, double)> pieces)
        {
            double total = 0, end = double.NegativeInfinity;
            foreach (var (s, e) in pieces.OrderBy(p => p.Item1))
            {
                double from = Math.Max(s, end);
                if (e > from) total += e - from;
                end = Math.Max(end, e);
            }
            return total;
        }
    }
}

namespace ShearWallCalculator
{
    /// <summary>One hold-down of a wall and what it lands on: a wall of the story below, the foundation, or nothing (needs a beam or post).</summary>
    public sealed class HoldownLanding
    {
        /// <summary>The story (1 = the first) of the wall whose hold-down this is.</summary>
        public int UpperStory { get; set; }
        public int WallId { get; set; }

        /// <summary>The post centerline in plan (ft).</summary>
        public System.Windows.Point At { get; set; }

        /// <summary>The hold-down tension (lb, ASD) and the model chosen (null: none strong enough).</summary>
        public double TensionAsd { get; set; }
        public string Model { get; set; }

        /// <summary>The wall of the story below it lands on (null: none, or the foundation).</summary>
        public int? LowerWallId { get; set; }

        /// <summary>A first-story hold-down: it anchors to the foundation.</summary>
        public bool OnFoundation { get; set; }

        public bool Supported => OnFoundation || LowerWallId != null || Support != SupportKind.None;

        // ---- from the load-path trace (LoadPathTrace)

        public double TensionLrfd { get; set; }
        public PathPointKind Kind { get; set; }

        /// <summary>"wall 3", "beam BM1 end", "column C2 base".</summary>
        public string From { get; set; }

        /// <summary>The wind direction (true: the low ends of the walls lift); null: both (a beam end or column base).</summary>
        public bool? LowEndUp { get; set; }

        /// <summary>The tension in each load case (lb, ASD); null when not known. Loads at one point add case by case.</summary>
        public Dictionary<string, double> CaseAsd { get; set; }

        /// <summary>What it anchors to, and its id ("wall 2", "beam BM1", "column C1").</summary>
        public SupportKind Support { get; set; }
        public string SupportId { get; set; }

        /// <summary>The family of the connection (the wall's own, or the one that can anchor there) and the option sized.</summary>
        public Design.HoldownFamilies Family { get; set; }
        public Design.HoldownChoice Choice { get; set; }

        /// <summary>Why the wall's hold-down type was replaced here (null: it was not).</summary>
        public string Note { get; set; }
    }
}
