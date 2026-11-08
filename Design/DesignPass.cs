using System.Collections.Generic;
using System.Linq;

namespace ShearWallCalculator.Design
{
    /// <summary>
    /// The shear wall design of every story, roof down, so each hold-down carries the walls stacked on it, and the trace of
    /// where every hold-down lands (a wall below, a beam, a column, the foundation). The one design pass: the Shear Wall
    /// Calculator's window and the headless check (<c>ShearWallCheck</c>, the hub's re-check) both run this, so they never
    /// disagree. No UI.
    /// </summary>
    public static class DesignPass
    {
        /// <summary>
        /// Checks the stories from the top down to <paramref name="down"/> into <paramref name="all"/> (index = story; those
        /// under <paramref name="down"/> are left as they are). The calculators must be analysed first
        /// (<see cref="StoryCascade.Analyze"/>). Returns the load-path trace.
        /// </summary>
        public static LoadPathTrace TopDown(IList<ShearWallCalculatorBase> calcs, int down, Dictionary<int, WallDesignResult>[] all,
                                            ShearWallDesigner designer, List<FramingBeam> beams, List<FramingColumn> columns)
        {
            var trace = NewLoadPath(calcs, designer, beams, columns);
            Dictionary<int, WallDesignResult> above = null;
            for (int k = calcs.Count - 1; k >= down; k--)
            {
                all[k] = above = CheckLevel(calcs[k], k + 1 < calcs.Count ? calcs[k + 1] : null, above, k + 2, designer, trace.ExtraOn(k));
                trace.Land(k, calcs[k]?._wall_system?._walls, above, k > 0 ? calcs[k - 1]?._wall_system?._walls : null);
            }
            return trace;
        }

        /// <summary>Every story's checks (index = story) and the trace.</summary>
        public static (List<Dictionary<int, WallDesignResult>> byStory, LoadPathTrace loadPath) All(IList<ShearWallCalculatorBase> calcs,
            ShearWallDesigner designer, List<FramingBeam> beams, List<FramingColumn> columns)
        {
            var all = new Dictionary<int, WallDesignResult>[calcs.Count];
            var trace = TopDown(calcs, 0, all, designer, beams, columns);
            return (all.Select(x => x ?? new Dictionary<int, WallDesignResult>()).ToList(), trace);
        }

        /// <summary>A trace with the model's beams and columns by story, sizing new connections from the project's catalog.</summary>
        public static LoadPathTrace NewLoadPath(IList<ShearWallCalculatorBase> calcs, ShearWallDesigner designer, List<FramingBeam> beams, List<FramingColumn> columns)
        {
            var settings = calcs.FirstOrDefault(c => c != null)?.DesignSettings ?? new DesignSettings();
            return new LoadPathTrace(beams ?? new List<FramingBeam>(), columns ?? new List<FramingColumn>(), (f, species, t) =>
                designer?.SelectHoldown(f, species, t, settings.HoldownsFromStandardList, settings.StrapHoldownsOrDefault()));
        }

        /// <summary>
        /// The check of each wall of one story for its governing shear (empty when the story cannot be calculated), with the
        /// hold-down forces of the story above (<paramref name="upper"/>, <paramref name="upperDesign"/>) and what the load path
        /// adds (<paramref name="extra"/>: beam ends and column bases standing on it).
        /// </summary>
        public static Dictionary<int, WallDesignResult> CheckLevel(ShearWallCalculatorBase calc, ShearWallCalculatorBase upper,
            Dictionary<int, WallDesignResult> upperDesign, int upperLevel, ShearWallDesigner designer, Dictionary<int, List<StackedForce>> extra = null)
        {
            var results = new Dictionary<int, WallDesignResult>();
            if (calc == null || designer == null || !calc.IsValidForCalculation) return results;
            // every wall's force in every case: its hold-down tension case by case (the load path adds those that share a support)
            var byWall = CaseBreakdown.Rows(calc).GroupBy(x => x.Wall).ToDictionary(g => g.Key, g => g.ToList());
            foreach (var kv in calc._wall_system._walls)
            {
                if (!calc.TotalWallShear.TryGetValue(kv.Key, out double v)) continue;
                calc.GoverningLoadCase.TryGetValue(kv.Key, out string governing);
                var above = upper == null ? null : StoryCascade.HoldownForcesOn(kv.Value, upper._wall_system._walls, upperDesign, upperLevel);
                if (extra != null && extra.TryGetValue(kv.Key, out var more)) (above = above ?? new List<StackedForce>()).AddRange(more);
                results[kv.Key] = designer.Check(kv.Key, kv.Value, v, governing, calc.DesignSettings, null, above);
                if (byWall.TryGetValue(kv.Key, out var rows))
                {
                    var r = results[kv.Key];
                    var byCase = rows.GroupBy(x => x.Case).Select(g => new KeyValuePair<string, double>(g.Key, g.First().Total)).ToList();
                    var fromAbove = CaseForcesOn(kv.Key, kv.Value, calc._wall_system._walls, upper, upperDesign, extra);
                    r.SetCaseTensions(byCase, fromAbove);
                    AtLeastTheWorstCase(r, kv.Value, calc.DesignSettings, designer, byCase, fromAbove);
                }
            }
            return results;
        }

        /// <summary>
        /// The hold-down sized for the worst case of the wall's own statics when that is more than the design's tension (the
        /// design takes the walls above at their worst, scaled by this wall's share of its own case; the worst of a wall
        /// above can be in another case): the tension raised to it and, unless the engineer chose one, the lightest hold-down
        /// strong enough, the cases found again with its post.
        /// </summary>
        private static void AtLeastTheWorstCase(WallDesignResult r, calculator.WallData wall, DesignSettings settings, ShearWallDesigner designer,
            List<KeyValuePair<string, double>> byCase, List<CaseForceFromAbove> fromAbove)
        {
            for (int pass = 0; pass < 3; pass++)
            {
                double worst = r.TensionByCase == null || r.TensionByCase.Count == 0 ? 0 : r.TensionByCase.Values.Max(v => System.Math.Max(v[0], v[1]));
                if (!(worst > r.TensionAsd + 0.05) || double.IsInfinity(r.TensionAsd)) return;
                r.TensionAsd = worst;
                r.TensionLrfd = worst / 0.6;
                if (r.HoldownChosen) return; // the engineer's: checked against the higher tension (HoldownRatio)
                var d = wall.Design ?? new WallDesignInputs();
                double post = WallSchedule.PostAlongWall(r.Holdown);
                r.Holdown = designer.SelectHoldown(d.Holdown, d.Species, worst, settings?.HoldownsFromStandardList ?? false, (settings ?? new DesignSettings()).StrapHoldownsOrDefault());
                double next = WallSchedule.PostAlongWall(r.Holdown);
                if (r.Holdown == null || System.Math.Abs(next - post) < 1e-9) return;
                // a bigger post: a shorter arm, the cases again
                r.HoldownArm += 1.5 * (post - next) / 12.0;
                r.PostAlongWall = next;
                r.SetCaseTensions(byCase, fromAbove);
            }
        }

        /// <summary>
        /// The forces from above on one wall, case by case, the compression as well as the tension: the ends of the walls
        /// above whose hold-downs the load path lands on it (<see cref="StoryCascade.WallUnder"/>, as <see cref="LoadPathTrace"/>
        /// does), at the hold-down when they pull and at the end post's centreline when they press, and the beam ends and
        /// column bases on it (<paramref name="extra"/>). Every force on what is under it is on the wall in turn, so its ends
        /// balance them (<see cref="WallDesignResult.SetCaseTensions(IEnumerable{KeyValuePair{string, double}}, IList{CaseForceFromAbove})"/>).
        /// </summary>
        public static List<CaseForceFromAbove> CaseForcesOn(int key, calculator.WallData wall, IDictionary<int, calculator.WallData> story,
            ShearWallCalculatorBase upper, Dictionary<int, WallDesignResult> upperDesign, Dictionary<int, List<StackedForce>> extra)
        {
            var list = new List<CaseForceFromAbove>();
            bool ew = wall.WallDir == calculator.WallDirs.EastWest;
            double lo = ew ? System.Math.Min(wall.Start.X, wall.End.X) : System.Math.Min(wall.Start.Y, wall.End.Y);
            double Along(System.Windows.Point p) => (ew ? p.X : p.Y) - lo;
            if (upper?._wall_system?._walls != null && upperDesign != null)
                foreach (var u in upper._wall_system._walls.OrderBy(x => x.Key))
                {
                    if (!upperDesign.TryGetValue(u.Key, out var r) || !r.HoldownNeeded || r.EndForceByCase == null) continue;
                    var w = u.Value;
                    bool uew = w.WallDir == calculator.WallDirs.EastWest;
                    var (p1, p2) = WallSchedule.HoldownPoints(w, r.Holdown);
                    double half = WallSchedule.PostAlongWall(r.Holdown) / 24.0;
                    foreach (var p in new[] { p1, p2 })
                    {
                        if (StoryCascade.WallUnder(p, w.WallDir, story) != key) continue;
                        bool atLow = uew ? p.X < (w.Start.X + w.End.X) / 2 : p.Y < (w.Start.Y + w.End.Y) / 2;
                        // where it presses: its end post's centreline, half a post nearer its end (ShearWallCheck.CompressionAt)
                        var end = (p - w.Start).Length <= (p - w.End).Length ? w.Start : w.End;
                        var toEnd = end - p;
                        var press = toEnd.Length < 1e-9 ? p : p + toEnd / toEnd.Length * half;
                        list.Add(new CaseForceFromAbove
                        {
                            AtPull = Along(p), AtPress = Along(press), From = $"wall {u.Key}",
                            Up = r.EndForceByCase.ToDictionary(c => c.Key, c => c.Value[atLow ? 0 : 1]),
                        });
                    }
                }
            if (extra != null && extra.TryGetValue(key, out var more))
                foreach (var f in more.Where(f => f.CaseUp != null).GroupBy(f => (f.From, f.Along)).Select(g => g.First()))
                    list.Add(new CaseForceFromAbove { AtPull = f.Along, AtPress = f.Along, From = f.From, Up = f.CaseUp });
            return list;
        }
    }
}
