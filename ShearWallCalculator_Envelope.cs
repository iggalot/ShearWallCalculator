using calculator;
using Newtonsoft.Json;
using ShearWallCalculator.Design;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ShearWallCalculator
{
    /// <summary>
    /// Rigid AND flexible: both calculations of the same walls, diaphragm and loads; each wall is designed for the larger of
    /// its two shears (<see cref="DiaphragmAssumption.Envelope"/>). The two share this calculator's wall and diaphragm
    /// systems. Multi-story, the rigid run of a level carries the rigid reactions of the level above and the flexible run the
    /// flexible ones (<see cref="StoryCascade"/>): two whole-building analyses, enveloped wall by wall.
    /// </summary>
    public class ShearWallCalculator_Envelope : ShearWallCalculatorBase
    {
        public const string RigidTag = "rigid", FlexibleTag = "flexible";

        [JsonIgnore]
        public ShearWallCalculator_RigidDiaphragm Rigid { get; } = new ShearWallCalculator_RigidDiaphragm();

        [JsonIgnore]
        public ShearWallCalculator_FlexibleDiaphragm Flexible { get; } = new ShearWallCalculator_FlexibleDiaphragm();

        /// <summary>The flexible run's loads from the level above (null: the same as <see cref="ShearWallCalculatorBase.LoadsFromAbove"/>, which the rigid run takes).</summary>
        [JsonIgnore]
        public Dictionary<string, List<PointLoad>> FlexibleLoadsFromAbove { get; set; }

        /// <summary>Which calculation gives each wall its design shear (<see cref="RigidTag"/> or <see cref="FlexibleTag"/>).</summary>
        [JsonIgnore]
        public Dictionary<int, string> Governs { get; } = new Dictionary<int, string>();

        public ShearWallCalculator_Envelope() : base() { }

        public ShearWallCalculator_Envelope(ShearWallCalculatorBase copy_calc) : base(copy_calc) { }

        /// <summary>The case name shown for a wall's governing case: "Case 1 (rigid)".</summary>
        public static string Tagged(string caseName, string tag) => $"{caseName} ({tag})";

        public override void PerformCalculations()
        {
            if (_diaphragm_system == null || _wall_system == null) return;
            base.Update();
            foreach (var c in new ShearWallCalculatorBase[] { Rigid, Flexible })
            {
                c._wall_system = _wall_system;
                c._diaphragm_system = _diaphragm_system;
                c.V_x = V_x;
                c.V_y = V_y;
                c.LoadCases = LoadCases;
                c.DesignSettings = DesignSettings;
            }
            Rigid.LoadsFromAbove = LoadsFromAbove ?? new Dictionary<string, List<PointLoad>>();
            Flexible.LoadsFromAbove = FlexibleLoadsFromAbove ?? Rigid.LoadsFromAbove;

            TotalWallShear.Clear();
            GoverningLoadCase.Clear();
            WallShearRange.Clear();
            Governs.Clear();
            if (!IsValidForCalculation) return;
            Rigid.PerformCalculations();
            Flexible.PerformCalculations();

            foreach (int id in Rigid.TotalWallShear.Keys.Union(Flexible.TotalWallShear.Keys))
            {
                bool hasR = Rigid.TotalWallShear.TryGetValue(id, out double r);
                bool hasF = Flexible.TotalWallShear.TryGetValue(id, out double f);
                bool rigidGoverns = hasR && (!hasF || Math.Abs(r) >= Math.Abs(f)); // a tie goes to rigid
                string tag = rigidGoverns ? RigidTag : FlexibleTag;
                var from = rigidGoverns ? (ShearWallCalculatorBase)Rigid : Flexible;
                TotalWallShear[id] = rigidGoverns ? r : f;
                Governs[id] = tag;
                if (from.GoverningLoadCase.TryGetValue(id, out var name)) GoverningLoadCase[id] = Tagged(name, tag);
            }

            // the range over both runs: the most positive and most negative of either, with the run named
            foreach (var (calc, tag) in new (ShearWallCalculatorBase, string)[] { (Rigid, RigidTag), (Flexible, FlexibleTag) })
                foreach (var kv in calc.WallShearRange)
                {
                    var s = kv.Value;
                    if (!WallShearRange.TryGetValue(kv.Key, out var e))
                    {
                        WallShearRange[kv.Key] = new ShearRange
                        {
                            MaxPositive = s.MaxPositive, MaxPositiveCase = Tagged(s.MaxPositiveCase, tag),
                            MaxNegative = s.MaxNegative, MaxNegativeCase = Tagged(s.MaxNegativeCase, tag),
                        };
                        continue;
                    }
                    if (s.MaxPositive > e.MaxPositive) { e.MaxPositive = s.MaxPositive; e.MaxPositiveCase = Tagged(s.MaxPositiveCase, tag); }
                    if (s.MaxNegative < e.MaxNegative) { e.MaxNegative = s.MaxNegative; e.MaxNegativeCase = Tagged(s.MaxNegativeCase, tag); }
                }
        }

        /// <summary>
        /// Per load case, each wall's shear of larger magnitude of the two runs (for a level below that is not an envelope;
        /// <see cref="StoryCascade"/> gives an envelope level below each run's own reactions instead).
        /// </summary>
        public override IEnumerable<KeyValuePair<string, Dictionary<int, double>>> CaseWallTotals()
        {
            var flex = Flexible.CaseWallTotals().ToDictionary(k => k.Key, k => k.Value);
            foreach (var r in Rigid.CaseWallTotals())
            {
                var merged = new Dictionary<int, double>(r.Value);
                if (flex.TryGetValue(r.Key, out var f))
                    foreach (var kv in f)
                        if (!merged.TryGetValue(kv.Key, out var v) || Math.Abs(kv.Value) > Math.Abs(v)) merged[kv.Key] = kv.Value;
                yield return new KeyValuePair<string, Dictionary<int, double>>(r.Key, merged);
            }
        }
    }

    /// <summary>Makes a level's calculator match the project's <see cref="DiaphragmAssumption"/>.</summary>
    public static class DiaphragmAnalysis
    {
        /// <summary>The assumption a calculator implements.</summary>
        public static DiaphragmAssumption KindOf(ShearWallCalculatorBase calc) =>
            calc is ShearWallCalculator_Envelope ? DiaphragmAssumption.Envelope
            : calc is ShearWallCalculator_FlexibleDiaphragm ? DiaphragmAssumption.Flexible
            : DiaphragmAssumption.Rigid;

        /// <summary>A new calculator of the kind (empty).</summary>
        public static ShearWallCalculatorBase Create(DiaphragmAssumption kind) =>
            kind == DiaphragmAssumption.Envelope ? new ShearWallCalculator_Envelope()
            : kind == DiaphragmAssumption.Flexible ? new ShearWallCalculator_FlexibleDiaphragm()
            : (ShearWallCalculatorBase)new ShearWallCalculator_RigidDiaphragm();

        /// <summary>
        /// <paramref name="calc"/> when it is already of the kind; else a calculator of the kind with the same walls, diaphragms,
        /// loads and settings (null stays null).
        /// </summary>
        public static ShearWallCalculatorBase Ensure(ShearWallCalculatorBase calc, DiaphragmAssumption kind)
        {
            if (calc == null || KindOf(calc) == kind) return calc;
            return kind == DiaphragmAssumption.Envelope ? new ShearWallCalculator_Envelope(calc)
                 : kind == DiaphragmAssumption.Flexible ? new ShearWallCalculator_FlexibleDiaphragm(calc)
                 : (ShearWallCalculatorBase)new ShearWallCalculator_RigidDiaphragm(calc);
        }

        /// <summary>Short name: "Rigid", "Flexible", "Envelope (rigid and flexible)".</summary>
        public static string Name(DiaphragmAssumption kind) =>
            kind == DiaphragmAssumption.Envelope ? "Envelope (rigid and flexible)" : kind == DiaphragmAssumption.Flexible ? "Flexible" : "Rigid";
    }
}
