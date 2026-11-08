using calculator;
using ShearWallCalculator.Design;
using ShearWallCalculator.NDSProvisions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace ShearWallCalculator
{
    /// <summary>What a hold-down, beam end or column base anchors to.</summary>
    public enum SupportKind { None, Foundation, Wall, Column, Beam }

    /// <summary>What a load-path point is: a wall's hold-down, the end of a beam that carries one, or the base of a column.</summary>
    public enum PathPointKind { WallHoldown, BeamEnd, ColumnBase }

    /// <summary>
    /// Which hold-down family can anchor to what. The rules:
    /// <list type="bullet">
    /// <item>LSTHD / STHD strap-tie hold-downs are cast into the concrete: the FOUNDATION only, never floor to floor.</item>
    /// <item>Straps (CS coil straps, MSTC / MST straps) need wood on both sides: to the end post of a WALL directly below,
    /// to a COLUMN below, or wrapped around a BEAM below. Not to the foundation (use an STHD or an anchored hold-down there).</item>
    /// <item>HDUE / DTT (and the discontinued HDU) hold-downs anchor with a bolt or rod: to the FOUNDATION, or floor to floor
    /// with a rod through the floor into a hold-down on the post of a WALL or a COLUMN directly below. A rod cannot anchor
    /// into the side of a beam.</item>
    /// <item>LTT / HTT tension ties: taken like HDUE (the foundation, or a rod to a post directly below). To be confirmed
    /// against the catalog's floor-to-floor installations.</item>
    /// </list>
    /// </summary>
    public static class HoldownRules
    {
        public static bool CanAnchorTo(HoldownFamilies family, SupportKind support)
        {
            if (support == SupportKind.None) return false;
            switch (family)
            {
                case HoldownFamilies.STHD_ToFoundation: return support == SupportKind.Foundation;
                case HoldownFamilies.Strap_ToWallBelow: return support != SupportKind.Foundation;
                default: return support != SupportKind.Beam; // HDUE / DTT / HDU / HTT
            }
        }

        /// <summary>Why <paramref name="family"/> cannot anchor to <paramref name="support"/> (null when it can).</summary>
        public static string WhyNot(HoldownFamilies family, SupportKind support)
        {
            if (CanAnchorTo(family, support)) return null;
            if (support == SupportKind.None) return "there is nothing under it";
            switch (family)
            {
                case HoldownFamilies.STHD_ToFoundation: return "an STHD is cast into a concrete footing: it cannot connect floor to floor";
                case HoldownFamilies.Strap_ToWallBelow: return "a strap needs wood below it: at the foundation use an STHD or an anchored hold-down";
                default: return $"{Name(family)} anchors with a rod into a post directly below: it cannot anchor into a beam (strap around the beam instead)";
            }
        }

        /// <summary>
        /// The family used at a support: the one chosen when it can anchor there; otherwise straps onto wood (a wall, a column,
        /// a beam) and an HDUE / DTT at the foundation.
        /// </summary>
        public static HoldownFamilies For(SupportKind support, HoldownFamilies chosen) =>
            CanAnchorTo(chosen, support) ? chosen : support == SupportKind.Foundation ? HoldownFamilies.HDU_ToFoundation : HoldownFamilies.Strap_ToWallBelow;

        /// <summary>The family for a new connection (a beam end or a column base) at a support: a strap onto wood, an HDUE / DTT at the foundation.</summary>
        public static HoldownFamilies NewConnection(SupportKind support, HoldownFamilies chosen) =>
            support == SupportKind.Foundation ? (CanAnchorTo(chosen, support) ? chosen : HoldownFamilies.HDU_ToFoundation) : HoldownFamilies.Strap_ToWallBelow;

        /// <summary>The family in a word or two: "strap", "HDUE / DTT", "LTT / HTT", "STHD", "HDU".</summary>
        public static string ShortName(HoldownFamilies f) =>
            f == HoldownFamilies.HDU_ToFoundation ? "HDUE / DTT" : f == HoldownFamilies.HTT_ToFoundation ? "LTT / HTT" :
            f == HoldownFamilies.STHD_ToFoundation ? "STHD" : f == HoldownFamilies.Strap_ToWallBelow ? "strap" : "HDU";

        public static string Name(HoldownFamilies f) =>
            f == HoldownFamilies.HDU_ToFoundation ? "an HDUE / DTT" : f == HoldownFamilies.HTT_ToFoundation ? "an LTT / HTT" :
            f == HoldownFamilies.STHD_ToFoundation ? "an STHD" : f == HoldownFamilies.Strap_ToWallBelow ? "a strap" : "an HDU";
    }

    /// <summary>A beam in the framing of the level on top of story <see cref="OnStory"/> (0 = the floor over the first story).</summary>
    public sealed class FramingBeam
    {
        public string Id { get; set; }
        public Point Start { get; set; }
        public Point End { get; set; }
        public int OnStory { get; set; }
        public double Length => (End - Start).Length;
    }

    /// <summary>A column standing in stories <see cref="BaseStory"/> to <see cref="TopStory"/> (base story 0: on the foundation).</summary>
    public sealed class FramingColumn
    {
        public string Id { get; set; }
        public Point At { get; set; }
        public int BaseStory { get; set; }
        public int TopStory { get; set; }
    }

    /// <summary>One load on a beam and the beam's end reactions for it (simple span).</summary>
    public sealed class BeamLoad
    {
        public string From { get; set; }
        /// <summary>ft from the beam's start.</summary>
        public double A { get; set; }
        public double Asd { get; set; }
        public double Lrfd { get; set; }
        public bool LowEndUp { get; set; }
        public double StartAsd { get; set; }
        public double EndAsd { get; set; }
    }

    /// <summary>A beam that carries hold-downs: its loads and the reactions at its ends (for each wind direction, the worse taken).</summary>
    public sealed class BeamReactions
    {
        public FramingBeam Beam { get; set; }
        public List<BeamLoad> Loads { get; } = new List<BeamLoad>();
        public double StartAsd { get; set; }
        public double EndAsd { get; set; }
        public double StartLrfd { get; set; }
        public double EndLrfd { get; set; }
    }

    /// <summary>
    /// The vertical load path of the hold-downs, top down. Every hold-down point is traced to what carries it:
    /// <list type="bullet">
    /// <item>a WALL of the story below (on the same line for a wall's hold-down; any wall through the point for a beam end
    /// or a column base): its load goes into that wall's overturning (a force from above), and the wall's own hold-downs
    /// take it down;</item>
    /// <item>a COLUMN under that level: it carries the load to its base, which is traced again (to the foundation for a
    /// column standing on the ground);</item>
    /// <item>a BEAM on that level: the beam's end reactions (simple span: R_start = Σ T (L − a) / L, R_end = Σ T a / L, each
    /// wind direction apart, the worse taken) are new hold-down points, traced to the walls or columns under the ends;</item>
    /// <item>the FOUNDATION under the first story;</item>
    /// <item>or nothing: an error (draw a beam or a column).</item>
    /// </list>
    /// Each connection is checked against <see cref="HoldownRules"/>; where the wall's hold-down family cannot anchor to what
    /// is under it, the connection is sized as the family that can, and flagged. New connections (beam ends, column bases)
    /// are sized for their reaction: a strap onto wood, the chosen anchored hold-down at the foundation.
    /// </summary>
    public sealed class LoadPathTrace
    {
        /// <summary>A point on a beam, a column or a wall: this close in plan (ft).</summary>
        public const double Tolerance = StoryCascade.LineTolerance;

        private readonly List<FramingBeam> beams;
        private readonly List<FramingColumn> columns;
        private readonly Func<HoldownFamilies, WoodTypes, double, HoldownChoice> size;

        /// <summary>Loads arriving on the level on top of story k (k = −1: the foundation), waiting for that level to be traced.</summary>
        private readonly Dictionary<int, List<Arrival>> pending = new Dictionary<int, List<Arrival>>();
        private readonly Dictionary<int, List<StackedForce>> extra = new Dictionary<int, List<StackedForce>>();

        /// <summary>Every point traced, by the level it lands on (k = the level on top of story k; −1 = the foundation).</summary>
        public Dictionary<int, List<HoldownLanding>> ByLevel { get; } = new Dictionary<int, List<HoldownLanding>>();

        /// <summary>The beams that carry something, with their reactions.</summary>
        public List<BeamReactions> Beams { get; } = new List<BeamReactions>();

        /// <param name="size">Sizes a hold-down: family, species, required tension (lb ASD) → the lightest that works (null: none).</param>
        public LoadPathTrace(IEnumerable<FramingBeam> beams, IEnumerable<FramingColumn> columns, Func<HoldownFamilies, WoodTypes, double, HoldownChoice> size)
        {
            this.beams = beams?.ToList() ?? new List<FramingBeam>();
            this.columns = columns?.ToList() ?? new List<FramingColumn>();
            this.size = size ?? ((f, s, t) => null);
        }

        private sealed class Arrival
        {
            public Point At;
            public double Asd, Lrfd;
            public bool? LowEndUp;          // null: both directions (a reaction from a beam or a column)
            public PathPointKind Kind;
            public string From;
            public int UpperStory;          // story of the wall the load comes from (1 = the first)
            public int WallId;
            public WallDirs? Dir;           // a wall's hold-down: the wall's direction (lands on a wall on the same line)
            public HoldownFamilies Family;  // the family chosen for the wall the load comes from
            public WoodTypes Species;
            public HoldownChoice Chosen;    // a wall's hold-down: the one its design chose
            public WallDesignResult Result; // a wall's hold-down: its design (told where it is anchored with another type)
            /// <summary>The tension in each load case (lb ASD / LRFD); null when not known (the envelope is used).</summary>
            public Dictionary<string, double> CaseAsd, CaseLrfd;
            /// <summary>The force on what is under it in each load case (lb ASD, + up: tension / − down: compression), both ends of every couple; null when not known.</summary>
            public Dictionary<string, double> CaseUp;
        }

        /// <summary>Σ case by case of loads that act together; null when any of them has no cases (then the envelope rules apply).</summary>
        public static Dictionary<string, double> SumByCase(IEnumerable<Dictionary<string, double>> loads, IEnumerable<double> factors = null)
        {
            var list = loads.ToList();
            if (list.Count == 0 || list.Any(x => x == null)) return null;
            var f = factors?.ToList();
            var sum = new Dictionary<string, double>();
            for (int i = 0; i < list.Count; i++)
                foreach (var kv in list[i])
                    sum[kv.Key] = (sum.TryGetValue(kv.Key, out var s) ? s : 0) + kv.Value * (f == null ? 1 : f[i]);
            return sum;
        }

        private static double Max(Dictionary<string, double> byCase) => byCase.Count == 0 ? 0 : Math.Max(0, byCase.Values.Max());

        /// <summary>The extra forces from above on story <paramref name="story"/>'s walls (beam ends and column bases on them), by wall.</summary>
        public Dictionary<int, List<StackedForce>> ExtraOn(int story)
        {
            var d = new Dictionary<int, List<StackedForce>>();
            if (!extra.TryGetValue(story, out var list)) return d;
            foreach (var f in list)
            {
                int id = int.Parse(f.From.Split('|')[0]);
                if (!d.TryGetValue(id, out var l)) d[id] = l = new List<StackedForce>();
                l.Add(new StackedForce { Along = f.Along, Asd = f.Asd, Lrfd = f.Lrfd, LowEndUp = f.LowEndUp, CaseUp = f.CaseUp, From = f.From.Substring(f.From.IndexOf('|') + 1) });
            }
            return d;
        }

        /// <summary>
        /// After story <paramref name="story"/> (0 = the first) is designed: its walls' hold-downs and the columns ending on the
        /// level under it land on that level (the level on top of story − 1, or the foundation for the first story).
        /// </summary>
        public void Land(int story, IDictionary<int, WallData> walls, IReadOnlyDictionary<int, WallDesignResult> design, IDictionary<int, WallData> wallsBelow)
        {
            int level = story - 1;
            var arriving = new List<Arrival>();
            if (pending.TryGetValue(level, out var p))
                // one column base for everything a column carries: the worse wind direction
                foreach (var g in p.GroupBy(x => x.From))
                {
                    var f = g.First();
                    double up = g.Where(x => x.LowEndUp != false).Sum(x => x.Asd), dn = g.Where(x => x.LowEndUp != true).Sum(x => x.Asd);
                    double upL = g.Where(x => x.LowEndUp != false).Sum(x => x.Lrfd), dnL = g.Where(x => x.LowEndUp != true).Sum(x => x.Lrfd);
                    // case by case when every load knows its cases: the worst case of the sum, not the sum of the worst
                    var cAsd = SumByCase(g.Select(x => x.CaseAsd));
                    var cLrfd = SumByCase(g.Select(x => x.CaseLrfd));
                    f.Asd = cAsd != null ? Max(cAsd) : Math.Max(up, dn);
                    f.Lrfd = cLrfd != null ? Max(cLrfd) : Math.Max(upL, dnL);
                    f.CaseAsd = cAsd;
                    f.CaseLrfd = cLrfd;
                    f.CaseUp = SumByCase(g.Select(x => x.CaseUp));
                    f.LowEndUp = g.All(x => x.LowEndUp == true) ? true : g.All(x => x.LowEndUp == false) ? (bool?)false : null;
                    arriving.Add(f);
                }
            pending.Remove(level);
            if (walls != null && design != null)
                foreach (var kv in walls.OrderBy(k => k.Key))
                {
                    if (!design.TryGetValue(kv.Key, out var r) || !r.HoldownNeeded) continue;
                    var w = kv.Value;
                    var d = w.Design ?? new WallDesignInputs();
                    var (p1, p2) = WallSchedule.HoldownPoints(w, r.Holdown);
                    bool ew = w.WallDir == WallDirs.EastWest;
                    r.HoldownElsewhere.Clear();
                    foreach (var at in new[] { p1, p2 })
                    {
                        bool atLow = ew ? at.X < (w.Start.X + w.End.X) / 2 : at.Y < (w.Start.Y + w.End.Y) / 2;
                        arriving.Add(new Arrival
                        {
                            At = at, Asd = r.TensionAsd, Lrfd = r.TensionLrfd, LowEndUp = atLow, Kind = PathPointKind.WallHoldown,
                            From = $"wall {kv.Key}", UpperStory = story + 1, WallId = kv.Key, Dir = w.WallDir,
                            Family = d.Holdown, Species = d.Species, Chosen = r.Holdown, Result = r,
                            CaseAsd = r.TensionByCase?.ToDictionary(c => c.Key, c => c.Value[atLow ? 0 : 1]),
                            CaseLrfd = r.TensionByCase?.ToDictionary(c => c.Key, c => c.Value[atLow ? 2 : 3]),
                            CaseUp = r.EndForceByCase?.ToDictionary(c => c.Key, c => c.Value[atLow ? 0 : 1]),
                        });
                    }
                }
            if (!ByLevel.ContainsKey(level)) ByLevel[level] = new List<HoldownLanding>();
            var beamLoads = new Dictionary<FramingBeam, List<Arrival>>();
            foreach (var a in arriving) Place(a, level, wallsBelow, beamLoads);

            // the beams: their reactions are new hold-down points at their ends, on the walls or columns under them
            foreach (var kv in beamLoads)
            {
                var bm = kv.Key;
                double L = bm.Length;
                var reactions = new BeamReactions { Beam = bm };
                double s0 = 0, e0 = 0, s1 = 0, e1 = 0, sl0 = 0, el0 = 0, sl1 = 0, el1 = 0;
                foreach (var a in kv.Value)
                {
                    double x = L <= 0 ? 0 : Math.Max(0, Math.Min(L, Vector.Multiply(a.At - bm.Start, (bm.End - bm.Start) / L)));
                    var load = new BeamLoad { From = a.From, A = x, Asd = a.Asd, Lrfd = a.Lrfd, LowEndUp = a.LowEndUp ?? true,
                                              StartAsd = L > 0 ? a.Asd * (L - x) / L : a.Asd / 2, EndAsd = L > 0 ? a.Asd * x / L : a.Asd / 2 };
                    reactions.Loads.Add(load);
                    double ls = L > 0 ? a.Lrfd * (L - x) / L : a.Lrfd / 2, le = L > 0 ? a.Lrfd * x / L : a.Lrfd / 2;
                    // a load with no wind direction (from a beam or column above) goes with both
                    if (a.LowEndUp != false) { s0 += load.StartAsd; e0 += load.EndAsd; sl0 += ls; el0 += le; }
                    if (a.LowEndUp != true) { s1 += load.StartAsd; e1 += load.EndAsd; sl1 += ls; el1 += le; }
                }
                reactions.StartAsd = Math.Max(s0, s1);
                reactions.EndAsd = Math.Max(e0, e1);
                reactions.StartLrfd = Math.Max(sl0, sl1);
                reactions.EndLrfd = Math.Max(el0, el1);
                // case by case when every load knows its cases: each end's worst case of the sum
                var xs = kv.Value.Select(a => L <= 0 ? 0 : Math.Max(0, Math.Min(L, Vector.Multiply(a.At - bm.Start, (bm.End - bm.Start) / L)))).ToList();
                var toStart = xs.Select(x => L > 0 ? (L - x) / L : 0.5).ToList();
                var toEnd = xs.Select(x => L > 0 ? x / L : 0.5).ToList();
                var sAsd = SumByCase(kv.Value.Select(a => a.CaseAsd), toStart);
                var eAsd = SumByCase(kv.Value.Select(a => a.CaseAsd), toEnd);
                var sLrfd = SumByCase(kv.Value.Select(a => a.CaseLrfd), toStart);
                var eLrfd = SumByCase(kv.Value.Select(a => a.CaseLrfd), toEnd);
                // the compression too: a wall end pressing on the beam is a load on it as much as one lifting it
                var sUp = SumByCase(kv.Value.Select(a => a.CaseUp), toStart);
                var eUp = SumByCase(kv.Value.Select(a => a.CaseUp), toEnd);
                if (sAsd != null && eAsd != null && sLrfd != null && eLrfd != null)
                {
                    reactions.StartAsd = Max(sAsd);
                    reactions.EndAsd = Max(eAsd);
                    reactions.StartLrfd = Max(sLrfd);
                    reactions.EndLrfd = Max(eLrfd);
                }
                Beams.Add(reactions);
                var first = kv.Value[0];
                foreach (var (end, asd, lrfd, which, cA, cL, cU) in new[] { (bm.Start, reactions.StartAsd, reactions.StartLrfd, "start", sAsd, sLrfd, sUp), (bm.End, reactions.EndAsd, reactions.EndLrfd, "end", eAsd, eLrfd, eUp) })
                {
                    // an end that only presses down still lands on what is under it (in bearing: no hold-down to size)
                    if (!(asd > 0) && (cU == null || cU.Values.All(v => Math.Abs(v) < 0.05))) continue;
                    Place(new Arrival
                    {
                        At = end, Asd = asd, Lrfd = lrfd, LowEndUp = null, Kind = PathPointKind.BeamEnd, From = $"beam {bm.Id} {which}",
                        UpperStory = first.UpperStory, WallId = first.WallId, Family = first.Family, Species = first.Species,
                        CaseAsd = cA, CaseLrfd = cL, CaseUp = cU,
                    }, level, wallsBelow, null);
                }
            }
        }

        /// <summary>What carries one point at <paramref name="level"/>, and what that does.</summary>
        private void Place(Arrival a, int level, IDictionary<int, WallData> wallsBelow, Dictionary<FramingBeam, List<Arrival>> beamLoads)
        {
            var l = new HoldownLanding
            {
                UpperStory = a.UpperStory, WallId = a.WallId, At = a.At, TensionAsd = a.Asd, TensionLrfd = a.Lrfd, Kind = a.Kind, From = a.From,
                LowEndUp = a.LowEndUp, CaseAsd = a.CaseAsd, CaseUp = a.CaseUp,
            };
            ByLevel[level].Add(l);

            // what is under the point
            int? wallId = null;
            FramingColumn col = null;
            FramingBeam beam = null;
            if (level < 0) { l.Support = SupportKind.Foundation; l.OnFoundation = true; }
            else
            {
                wallId = wallsBelow == null ? null
                       : a.Dir != null ? StoryCascade.WallUnder(a.At, a.Dir.Value, wallsBelow)
                       : StoryCascade.WallUnder(a.At, WallDirs.EastWest, wallsBelow) ?? StoryCascade.WallUnder(a.At, WallDirs.NorthSouth, wallsBelow);
                if (wallId != null) { l.Support = SupportKind.Wall; l.LowerWallId = wallId; l.SupportId = $"wall {wallId}"; }
                else if ((col = columns.FirstOrDefault(c => c.BaseStory <= level && c.TopStory >= level && (c.At - a.At).Length <= Tolerance)) != null)
                { l.Support = SupportKind.Column; l.SupportId = $"column {col.Id}"; }
                else if (beamLoads != null && (beam = beams.FirstOrDefault(b => b.OnStory == level && OnSegment(a.At, b.Start, b.End))) != null)
                { l.Support = SupportKind.Beam; l.SupportId = $"beam {beam.Id}"; }
                else l.Support = SupportKind.None;
            }

            // the connection: the wall's own hold-down when its family can anchor there; else the family that can, sized
            if (l.Support != SupportKind.None)
            {
                var fam = a.Kind == PathPointKind.WallHoldown ? HoldownRules.For(l.Support, a.Family) : HoldownRules.NewConnection(l.Support, a.Family);
                l.Family = fam;
                if (a.Kind == PathPointKind.WallHoldown && fam == a.Family) l.Choice = a.Chosen;
                else if (a.Asd > 0) l.Choice = size(fam, a.Species, a.Asd);
                if (a.Kind == PathPointKind.WallHoldown && fam != a.Family)
                {
                    // a valid type is picked here automatically; the wall's Hold-down type (Design tab) overrides it
                    l.Note = $"{HoldownRules.WhyNot(a.Family, l.Support)}: {HoldownRules.Name(fam)} ({l.Choice?.Model ?? "none strong enough"}) is used here instead, picked automatically.";
                    string where = $"{l.Choice?.Model ?? "none strong enough"} {HoldownRules.ShortName(fam)} on {l.SupportId}";
                    if (a.Result != null && !a.Result.HoldownElsewhere.Contains(where)) a.Result.HoldownElsewhere.Add(where);
                }
            }
            l.Model = l.Choice?.Model;

            switch (l.Support)
            {
                case SupportKind.Wall:
                    // a wall's hold-down on a wall on the same line is already in that wall's design (StoryCascade.HoldownForcesOn);
                    // a beam end or a column base on a wall is a force from above, for both wind directions
                    if (a.Kind != PathPointKind.WallHoldown)
                    {
                        var w = wallsBelow[wallId.Value];
                        bool ew = w.WallDir == WallDirs.EastWest;
                        double along = ew ? a.At.X - Math.Min(w.Start.X, w.End.X) : a.At.Y - Math.Min(w.Start.Y, w.End.Y);
                        if (!extra.TryGetValue(level, out var list)) extra[level] = list = new List<StackedForce>();
                        foreach (bool up in new[] { true, false })
                            if (a.LowEndUp == null || a.LowEndUp == up)
                                list.Add(new StackedForce { Along = along, Asd = a.Asd, Lrfd = a.Lrfd, LowEndUp = up, CaseUp = a.CaseUp, From = $"{wallId}|{a.From} (story {a.UpperStory} wall {a.WallId})" });
                    }
                    break;
                case SupportKind.Column:
                    // down the column to its base: the level under its base story (the foundation for the first story)
                    int baseLevel = col.BaseStory - 1;
                    if (!pending.TryGetValue(baseLevel, out var down)) pending[baseLevel] = down = new List<Arrival>();
                    down.Add(new Arrival
                    {
                        At = col.At, Asd = a.Asd, Lrfd = a.Lrfd, LowEndUp = a.LowEndUp, Kind = PathPointKind.ColumnBase, From = $"column {col.Id} base",
                        UpperStory = a.UpperStory, WallId = a.WallId, Family = a.Family, Species = a.Species, CaseAsd = a.CaseAsd, CaseLrfd = a.CaseLrfd, CaseUp = a.CaseUp,
                    });
                    break;
                case SupportKind.Beam:
                    if (!beamLoads.TryGetValue(beam, out var on)) beamLoads[beam] = on = new List<Arrival>();
                    on.Add(a);
                    break;
            }
        }

        private static bool OnSegment(Point p, Point a, Point b)
        {
            var ab = b - a;
            double L2 = ab.LengthSquared;
            if (L2 <= 0) return (p - a).Length <= Tolerance;
            double t = Math.Max(0, Math.Min(1, Vector.Multiply(p - a, ab) / L2));
            return (p - (a + t * ab)).Length <= Tolerance;
        }
    }
}
