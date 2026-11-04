using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace ShearWallCalculator.Design
{
    /// <summary>What one thing puts on the foundation at a point, per load case (lb ASD, + up / − down): wind only, no dead load.</summary>
    public sealed class FoundationLoad
    {
        public Point At { get; set; }

        /// <summary>Who it is ("W1", "column C1").</summary>
        public string From { get; set; }

        /// <summary>Per case (0.6W, the overturning alone). Null: <see cref="Envelope"/> in every case.</summary>
        public Dictionary<string, double> ByCase { get; set; }

        /// <summary>Used in every case when there are no per-case values (lb, + up).</summary>
        public double Envelope { get; set; }
    }

    /// <summary>The foundation's wind forces at one point: the most uplift and the most compression, each in its own case.</summary>
    public sealed class FoundationPoint
    {
        public Point At { get; set; }
        public List<string> From { get; } = new List<string>();
        public double UpliftAsd { get; set; }
        public string UpliftCase { get; set; }
        /// <summary>The most overturning compression (lb ASD, positive), the wind alone.</summary>
        public double BearingAsd { get; set; }
        public string BearingCase { get; set; }
    }

    /// <summary>
    /// The foundation at each anchor point, from the wind alone (this app counts no dead load: the load manager adds the
    /// gravity loads at the foundation). Walls (and columns) meeting at a point act on it together: the foundation takes the
    /// SUM of their forces, case by case, the worst uplift and, separately, the worst compression, which are in different
    /// cases. A wall's lifting end in one case is its compression end in the reverse.
    /// </summary>
    public static class FoundationForces
    {
        /// <summary>Points closer than this are one point (ft): 3 in, as the plan groups them.</summary>
        public const double Snap = 0.25;

        public static (long, long) Key(Point p) => ((long)Math.Round(p.X / Snap), (long)Math.Round(p.Y / Snap));

        public static List<FoundationPoint> Combine(IEnumerable<FoundationLoad> loads)
        {
            var result = new List<FoundationPoint>();
            foreach (var g in (loads ?? Enumerable.Empty<FoundationLoad>()).GroupBy(l => Key(l.At)))
            {
                var list = g.ToList();
                var cases = list.SelectMany(l => l.ByCase?.Keys ?? Enumerable.Empty<string>()).Distinct().ToList();
                if (cases.Count == 0) cases.Add("(envelope)");
                double Value(FoundationLoad l, string c) => l.ByCase == null ? l.Envelope : l.ByCase.TryGetValue(c, out var v) ? v : 0;

                var p = new FoundationPoint { At = list[0].At };
                p.From.AddRange(list.Select(l => l.From).Where(s => s != null).Distinct());
                foreach (var c in cases)
                {
                    double sum = list.Sum(l => Value(l, c));
                    if (sum > p.UpliftAsd) { p.UpliftAsd = sum; p.UpliftCase = c; }
                    if (-sum > p.BearingAsd) { p.BearingAsd = -sum; p.BearingCase = c; }
                }
                result.Add(p);
            }
            return result;
        }

        /// <summary>A wall's two ends on the foundation (its hold-down points), from its per-case end forces [low, high].</summary>
        public static IEnumerable<FoundationLoad> OfWall(string name, Point low, Point high, Dictionary<string, double[]> endForceByCase)
        {
            if (endForceByCase == null) yield break;
            Dictionary<string, double> Pick(int i) => endForceByCase.ToDictionary(kv => kv.Key, kv => kv.Value[i]);
            yield return new FoundationLoad { At = low, From = name, ByCase = Pick(0) };
            yield return new FoundationLoad { At = high, From = name, ByCase = Pick(1) };
        }
    }
}
