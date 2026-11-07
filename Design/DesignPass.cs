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
                    results[kv.Key].SetCaseTensions(rows.GroupBy(x => x.Case).Select(g => new KeyValuePair<string, double>(g.Key, g.First().Total)));
            }
            return results;
        }
    }
}
