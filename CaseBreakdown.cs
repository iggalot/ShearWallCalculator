using calculator;
using System.Collections.Generic;
using System.Linq;

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
