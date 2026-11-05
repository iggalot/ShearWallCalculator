using calculator;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace ShearWallCalculator
{
    /// <summary>
    /// One wall in one load case, as calculated: the case's own load (this level's wind), the part from the walls of the level
    /// above, the eccentricities and Mt,comb (rigid), and the wall's direct, torsional and total shear. Every case is its own
    /// simultaneous Vx, Vy, Mt; a wall is designed for the worst of them (<see cref="Governs"/>).
    /// </summary>
    public sealed class WallCaseRow
    {
        public int Wall { get; set; }
        /// <summary>The case name as listed (an envelope's carry the run: "Case 1, ... (rigid)").</summary>
        public string Case { get; set; }
        /// <summary>"rigid" or "flexible".</summary>
        public string Run { get; set; }
        /// <summary>This level's own load of the case (kips, kip-ft about the center of mass).</summary>
        public double OwnVx { get; set; }
        public double OwnVy { get; set; }
        public double OwnMt { get; set; }
        /// <summary>From the walls of the level above, standing on this diaphragm (Mt about the center of mass).</summary>
        public double AboveVx { get; set; }
        public double AboveVy { get; set; }
        public double AboveMt { get; set; }
        /// <summary>Rigid: ecc = CR − CM (ft) and Mt,comb = Vx·ey − Vy·ex + Mt (kip-ft); null for the flexible run.</summary>
        public double? EccX { get; set; }
        public double? EccY { get; set; }
        public double? MtComb { get; set; }
        /// <summary>The wall's force on the diaphragm (kips, building axes): direct + torsion = total.</summary>
        public double Direct { get; set; }
        public double Torsion { get; set; }
        public double Total { get; set; }
        /// <summary>This case gives the wall its design shear.</summary>
        public bool Governs { get; set; }
    }

    /// <summary>
    /// Every wall in every load case of a level (any diaphragm: rigid, flexible or the envelope of both), and one case's loads
    /// from the level above: what the Calculations tab lists wall by wall and the plan's Loads overlay draws.
    /// </summary>
    public static class CaseBreakdown
    {
        private static IEnumerable<(ShearWallCalculatorBase run, string tag)> Runs(ShearWallCalculatorBase calc)
        {
            if (calc is ShearWallCalculator_Envelope env)
            {
                yield return (env.Rigid, ShearWallCalculator_Envelope.RigidTag);
                yield return (env.Flexible, ShearWallCalculator_Envelope.FlexibleTag);
            }
            else if (calc is ShearWallCalculator_RigidDiaphragm) yield return (calc, ShearWallCalculator_Envelope.RigidTag);
            else if (calc is ShearWallCalculator_FlexibleDiaphragm) yield return (calc, ShearWallCalculator_Envelope.FlexibleTag);
        }

        private static string Listed(ShearWallCalculatorBase calc, string name, string tag) =>
            calc is ShearWallCalculator_Envelope ? ShearWallCalculator_Envelope.Tagged(name, tag) : name;

        /// <summary>The case names as listed, in calculation order.</summary>
        public static List<string> CaseNames(ShearWallCalculatorBase calc)
        {
            var names = new List<string>();
            foreach (var (run, tag) in Runs(calc))
            {
                if (run is ShearWallCalculator_RigidDiaphragm r) names.AddRange(r.CaseResults.Select(c => Listed(calc, c.Name, tag)));
                else if (run is ShearWallCalculator_FlexibleDiaphragm f) names.AddRange(f.CaseResults.Select(c => Listed(calc, c.Name, tag)));
            }
            return names;
        }

        /// <summary>The direction the case's wind blows (this level's own wind; the total when it has none), a unit plan vector; null: no load.</summary>
        public static Vector? WindDir(LateralLoadCase lc)
        {
            if (lc == null) return null;
            var v = new Vector(lc.Vx - lc.AboveVx, lc.Vy - lc.AboveVy);
            if (v.Length < 1e-6) v = new Vector(lc.Vx, lc.Vy);
            if (v.Length < 1e-6) return null;
            v.Normalize();
            return v;
        }

        /// <summary>Where the case's wind comes from: +x blows from the west, +y from the south; the oblique cases both ("SW").</summary>
        public static string WindFrom(LateralLoadCase lc)
        {
            if (!(WindDir(lc) is Vector d)) return null;
            string ns = d.Y > 1e-6 ? "S" : d.Y < -1e-6 ? "N" : "";
            string ew = d.X > 1e-6 ? "W" : d.X < -1e-6 ? "E" : "";
            return ns + ew;
        }

        /// <summary>The directions the wind comes from, in the order the lists go round: W, SW, S, SE, E, NE, N, NW.</summary>
        public static readonly string[] Directions = { "W", "SW", "S", "SE", "E", "NE", "N", "NW" };

        /// <summary>One case as listed for the designer: its name (the key), its direction group and a label that starts with it.</summary>
        public sealed class CaseItem
        {
            /// <summary>The listed name (<see cref="CaseNames"/>): what the tables and the overlay key on.</summary>
            public string Name { get; set; }
            /// <summary>"W", "SW", ... (where the wind comes from); "" for a case with no load.</summary>
            public string From { get; set; }
            /// <summary>"SW · Case 4, torsion +267.6 k·ft CCW" (an envelope adds the run).</summary>
            public string Label { get; set; }
            public int CaseNumber { get; set; }
        }

        /// <summary>
        /// The cases grouped by where the wind comes from (<see cref="Directions"/> order), then by case number, then by the
        /// torsion (+ before −); each labelled with its direction first, so a case is found by the side it blows on.
        /// </summary>
        public static List<CaseItem> Listing(ShearWallCalculatorBase calc)
        {
            var items = new List<(CaseItem item, int dir, double mt, int run, int order)>();
            int order = 0, runIndex = 0;
            foreach (var (run, tag) in Runs(calc))
            {
                var names = run is ShearWallCalculator_RigidDiaphragm r ? r.CaseResults.Select(c => c.Name)
                          : run is ShearWallCalculator_FlexibleDiaphragm f ? f.CaseResults.Select(c => c.Name) : Enumerable.Empty<string>();
                var cases = run.CasesToAnalyse().GroupBy(c => c.Name).ToDictionary(g => g.Key, g => g.First());
                foreach (var name in names)
                {
                    cases.TryGetValue(name, out var lc);
                    string from = WindFrom(lc) ?? "";
                    var m = System.Text.RegularExpressions.Regex.Match(name, @"^Case (\d+)");
                    int n = m.Success ? int.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) : 0;
                    double mt = lc == null ? 0 : lc.Mt - lc.AboveMt;
                    // the wind calculator's own name, less the direction (it leads the label): "Case 1, roof Case A, +GCpi"
                    string what = System.Text.RegularExpressions.Regex.Replace(name, @", from [NSEW]{1,2}\b", "");
                    if (m.Success && System.Math.Abs(mt) > 1e-6) what = System.Text.RegularExpressions.Regex.Replace(what, @", torsion [+\-−]+$", ""); // the value follows
                    if (m.Success && System.Math.Abs(mt) > 1e-6)
                        what += $", torsion {(mt > 0 ? "+" : "−")}{System.Math.Abs(mt).ToString("0.0", System.Globalization.CultureInfo.CurrentCulture)} k·ft {(mt > 0 ? "CCW" : "CW")}";
                    if (calc is ShearWallCalculator_Envelope) what += $" ({tag})";
                    int dir = System.Array.IndexOf(Directions, from);
                    items.Add((new CaseItem { Name = Listed(calc, name, tag), From = from, Label = (from.Length > 0 ? from + " · " : "") + what, CaseNumber = n },
                               dir < 0 ? Directions.Length : dir, mt, runIndex, order++));
                }
                runIndex++;
            }
            return items.OrderBy(x => x.dir).ThenBy(x => x.item.CaseNumber == 0 ? int.MaxValue : x.item.CaseNumber)
                        .ThenByDescending(x => x.mt).ThenBy(x => x.run).ThenBy(x => x.order).Select(x => x.item).ToList();
        }

        /// <summary>Every wall in every case (wall order, then case order).</summary>
        public static List<WallCaseRow> Rows(ShearWallCalculatorBase calc)
        {
            var rows = new List<WallCaseRow>();
            if (calc?._wall_system?._walls == null) return rows;
            foreach (var (run, tag) in Runs(calc))
            {
                var cases = run.CasesToAnalyse().GroupBy(c => c.Name).ToDictionary(g => g.Key, g => g.First());
                void Add(string name, int id, double total, double torsion, double? ex, double? ey, double? mtc)
                {
                    cases.TryGetValue(name, out var lc);
                    string listed = Listed(calc, name, tag);
                    rows.Add(new WallCaseRow
                    {
                        Wall = id, Case = listed, Run = tag,
                        AboveVx = lc?.AboveVx ?? 0, AboveVy = lc?.AboveVy ?? 0, AboveMt = lc?.AboveMt ?? 0,
                        OwnVx = (lc?.Vx ?? 0) - (lc?.AboveVx ?? 0), OwnVy = (lc?.Vy ?? 0) - (lc?.AboveVy ?? 0), OwnMt = (lc?.Mt ?? 0) - (lc?.AboveMt ?? 0),
                        EccX = ex, EccY = ey, MtComb = mtc,
                        Direct = total - torsion, Torsion = torsion, Total = total,
                        Governs = calc.GoverningLoadCase.TryGetValue(id, out var g) && g == listed,
                    });
                }
                if (run is ShearWallCalculator_RigidDiaphragm r)
                    foreach (var c in r.CaseResults)
                        foreach (var kv in c.Total ?? new Dictionary<int, double>())
                            Add(c.Name, kv.Key, kv.Value, c.Torsional != null && c.Torsional.TryGetValue(kv.Key, out var t) ? t : 0, c.EccX, c.EccY, c.MtComb);
                else if (run is ShearWallCalculator_FlexibleDiaphragm f)
                    foreach (var c in f.CaseResults)
                        foreach (var kv in c.Total ?? new Dictionary<int, double>())
                            Add(c.Name, kv.Key, kv.Value, c.Torsional != null && c.Torsional.TryGetValue(kv.Key, out var t) ? t : 0, null, null, null);
            }
            return rows.OrderBy(x => x.Wall).ToList(); // stable: case order kept within a wall
        }

        /// <summary>
        /// One listed case: the loads the walls of the level above put on this diaphragm (each at the upper wall's middle) and
        /// this level's walls' shears (their force on the diaphragm; the top of each wall receives the opposite). Null when the
        /// case is not one of this level's.
        /// </summary>
        public static (List<PointLoad> fromAbove, Dictionary<int, double> wallTotals, LateralLoadCase loadCase)? Case(ShearWallCalculatorBase calc, string listed)
        {
            if (calc == null || listed == null) return null;
            foreach (var (run, tag) in Runs(calc))
            {
                foreach (var kv in run.CaseWallTotals())
                {
                    if (Listed(calc, kv.Key, tag) != listed) continue;
                    var above = run.LoadsFromAbove; // an envelope gives each run its own (the flexible run the flexible reactions)
                    var loads = above != null && above.TryGetValue(kv.Key, out var l) ? l : new List<PointLoad>();
                    var lc = run.CasesToAnalyse().FirstOrDefault(c => c.Name == kv.Key);
                    return (loads, kv.Value ?? new Dictionary<int, double>(), lc);
                }
            }
            return null;
        }
    }
}
