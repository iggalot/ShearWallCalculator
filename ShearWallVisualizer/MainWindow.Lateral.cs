using System;
using System.Collections.Generic;
using System.Linq;
using ShearWallCalculator;
using ShearWallCalculator.Design;

namespace ShearWallVisualizer
{
    public partial class MainWindow
    {
        /// <summary>
        /// What the lateral design puts on the foundation, for the load manager (its "Lateral" section of the project file, written
        /// on every save): each first-story shear wall's two ends, per wind case, the overturning couple alone (its hold-down's
        /// uplift at one end, the same pressing down at the other) at the wind's own level (the ASD couple ÷ 0.6), with no dead load (this app counts
        /// none): the load manager adds the dead load from its own loads and combines. Column bases carrying
        /// hold-downs from above are added as this app's ASD force ÷ 0.6. Null when there is no design yet.
        /// </summary>
        private LoadManager.LateralSection LateralForFoundation(int revision)
        {
            if (!windLoadsLinked || levels.Count == 0 || allDesignResults.Count == 0 || levels[0]?._wall_system == null) return null;
            var design = allDesignResults[0];
            if (design == null) return null;
            var s = new LoadManager.LateralSection
            {
                Source = "Shear Wall Calculator",
                Saved = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'"),
                ModelRevision = revision,
                Note = "Each shear wall end's overturning couple by wind case, W at strength level (this app's ASD couple ÷ 0.6), the walls above " +
                       "included as designed; no dead load (the load manager's is added).",
            };
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
                string modelId = stories.Count > 0 && stories[0].Walls.TryGetValue(kv.Key, out var id) ? id : null;
                string name = modelId ?? $"W{kv.Key}";
                var upLow = new Dictionary<string, double>();
                var upHigh = new Dictionary<string, double>();
                foreach (var c in r.EndForceByCase)
                {
                    // [low, high]: the couple alone, + up at the low end (no dead load in this app)
                    double couple = c.Value[0] / 0.6;
                    upLow[c.Key] = Math.Round(couple, 1);
                    upHigh[c.Key] = Math.Round(-couple, 1);
                    Case(c.Key);
                }
                s.Points.Add(new LoadManager.LateralPoint { From = $"{name} {(ew ? "west" : "south")} end", WallId = modelId, X = low.X, Y = low.Y, Up = upLow });
                s.Points.Add(new LoadManager.LateralPoint { From = $"{name} {(ew ? "east" : "north")} end", WallId = modelId, X = high.X, Y = high.Y, Up = upHigh });
            }
            foreach (var a in foundationAnchors.Where(a => a.Kind == PathPointKind.ColumnBase && a.CaseAsd != null && a.CaseAsd.Count > 0))
            {
                s.Points.Add(new LoadManager.LateralPoint
                {
                    From = a.From, X = a.At.X, Y = a.At.Y,
                    Up = a.CaseAsd.ToDictionary(kv => kv.Key, kv => Math.Round(kv.Value / 0.6, 1)),
                });
                foreach (var c in a.CaseAsd.Keys) Case(c);
            }
            s.Cases = cases;
            return s.IsEmpty ? null : s;
        }
    }
}
