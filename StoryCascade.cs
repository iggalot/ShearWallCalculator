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

        /// <summary>Calculates every level, top first. <paramref name="levels"/> runs bottom to top. Returns what to check (per level).</summary>
        public static List<string> Analyze(IList<ShearWallCalculatorBase> levels)
        {
            var notes = new List<string>();
            if (levels == null || levels.Count == 0) return notes;
            int n = levels.Count;
            for (int i = n - 1; i >= 0; i--)
            {
                var calc = levels[i];
                if (calc == null) continue;
                calc.LoadsFromAbove = new Dictionary<string, List<PointLoad>>();
                if (i < n - 1 && levels[i + 1] != null)
                {
                    var upper = levels[i + 1];
                    bool upperHasWalls = upper._wall_system?._walls.Count > 0;
                    if (upperHasWalls && !upper.IsValidForCalculation)
                        notes.Add($"Level {i + 2} cannot be calculated yet (it needs a diaphragm and walls both ways), so its wind is not carried down to level {i + 1}.");
                    else
                        calc.LoadsFromAbove = Reactions(upper, i + 2);
                    if (calc is ShearWallCalculator_FlexibleDiaphragm && calc.LoadsFromAbove.Count > 0)
                        notes.Add($"Level {i + 1} is a flexible diaphragm: the walls above are spread as a uniform load plus torsion (an approximation).");
                    notes.AddRange(Unsupported(upper, calc, i + 2));
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

        /// <summary>Upper walls with no wall under (most of) their length on the level below: their hold-downs and shear need a load path (beam, post).</summary>
        public static List<string> Unsupported(ShearWallCalculatorBase upper, ShearWallCalculatorBase lower, int upperLevel)
        {
            var notes = new List<string>();
            var below = lower?._wall_system?._walls.Values.ToList() ?? new List<WallData>();
            foreach (var kv in upper?._wall_system?._walls ?? new Dictionary<int, WallData>())
            {
                double under = SupportedLength(kv.Value, below);
                if (under < kv.Value.WallLength - 0.1)
                    notes.Add($"Level {upperLevel}, wall {kv.Key}: {(under < 0.1 ? "no wall" : $"a wall under only {under:0.#} of {kv.Value.WallLength:0.#} ft")} on level {upperLevel - 1}. " +
                              "Its shear goes into the floor below; its hold-downs need a post, beam or blocking under them.");
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

        /// <summary>The length of <paramref name="w"/> that has a wall of <paramref name="below"/> on the same line under it (ft).</summary>
        public static double SupportedLength(WallData w, IEnumerable<WallData> below)
        {
            bool ew = w.WallDir == WallDirs.EastWest;
            double a0 = ew ? Math.Min(w.Start.X, w.End.X) : Math.Min(w.Start.Y, w.End.Y);
            double a1 = ew ? Math.Max(w.Start.X, w.End.X) : Math.Max(w.Start.Y, w.End.Y);
            double line = ew ? w.Start.Y : w.Start.X;
            var pieces = new List<(double, double)>();
            foreach (var b in below)
            {
                if (b.WallDir != w.WallDir) continue;
                if (Math.Abs((ew ? b.Start.Y : b.Start.X) - line) > LineTolerance) continue;
                double b0 = Math.Max(a0, ew ? Math.Min(b.Start.X, b.End.X) : Math.Min(b.Start.Y, b.End.Y));
                double b1 = Math.Min(a1, ew ? Math.Max(b.Start.X, b.End.X) : Math.Max(b.Start.Y, b.End.Y));
                if (b1 > b0) pieces.Add((b0, b1));
            }
            // union of the overlaps
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
