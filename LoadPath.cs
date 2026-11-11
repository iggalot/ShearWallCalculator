using calculator;
using ShearWallCalculator.Design;
using ShearWallCalculator.NDSProvisions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using BuildingModel.Statics;

namespace ShearWallCalculator
{
    /// <summary>What a hold-down, beam end or column base anchors to.</summary>
    public enum SupportKind { None, Foundation, Wall, Column, Beam }

    /// <summary>What a load-path point is: a wall's hold-down, the end of a beam that carries one, or the base of a column.</summary>
    /// <summary>
    /// A wall's hold-down, a beam's end, a column's base, or a post in a plain wall (or a shear wall past its shear part)
    /// taking what lands on the wall down to what is under it, as a column does (owner 2026-11-10).
    /// </summary>
    public enum PathPointKind { WallHoldown, BeamEnd, ColumnBase, WallPost }

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

    /// <summary>
    /// A wall of the model, its whole length, standing in story <see cref="Story"/> (0 = the first). Any wall holds up what
    /// lands on it: a shear wall's shear part takes it into its overturning; anywhere else (a plain wall, a shear wall past
    /// its shear part) a post or strap in the wall takes it down to what is under the wall, as a column (owner 2026-11-10).
    /// </summary>
    public sealed class FramingWall
    {
        public string Id { get; set; }
        public Point Start { get; set; }
        public Point End { get; set; }
        public int Story { get; set; }
    }

    /// <summary>One load on a beam and what it passes to each of the beam's supports (<see cref="BeamReactions.Supports"/>, in order).</summary>
    public sealed class BeamLoad
    {
        public string From { get; set; }
        /// <summary>ft from the beam's start.</summary>
        public double A { get; set; }
        public double Asd { get; set; }
        public double Lrfd { get; set; }
        public bool LowEndUp { get; set; }
        /// <summary>Its tension passed to each support (lb ASD, + up; a cantilever's back support is pushed down, −).</summary>
        public List<double> ToSupports { get; } = new List<double>();
    }

    /// <summary>One support of a beam carrying hold-downs, and its reaction (the worse wind direction).</summary>
    public sealed class BeamSupportReaction
    {
        /// <summary>"start", "end", or "at 12.5 ft" (from its start): its hold-down is "beam BM1 start", "beam BM1 at 12.5 ft".</summary>
        public string Name { get; set; }
        /// <summary>ft from the beam's start.</summary>
        public double At { get; set; }
        public Point Where { get; set; }
        public double Asd { get; set; }
        public double Lrfd { get; set; }
    }

    /// <summary>
    /// A beam that carries hold-downs: its loads and the reactions at its supports (for each wind direction, the worse taken):
    /// the walls and columns under it anywhere along it and, at an end with neither, a beam (owner 2026-11-10).
    /// </summary>
    public sealed class BeamReactions
    {
        public FramingBeam Beam { get; set; }
        public List<BeamLoad> Loads { get; } = new List<BeamLoad>();
        public List<BeamSupportReaction> Supports { get; } = new List<BeamSupportReaction>();
        /// <summary>False: fewer than two supports, it cannot stand; its ends are taken as a simple span, an end on nothing an error.</summary>
        public bool Stands { get; set; } = true;
        /// <summary>Two supports by statics (an overhang past them too), else continuous (EI the same along it).</summary>
        public string Method => !Stands ? "it cannot stand (fewer than two supports): taken as a simple span"
                              : Supports.Count == 2 ? "statics, ΣM about each support" : $"continuous over {Supports.Count} supports, EI constant";
        private BeamSupportReaction Named(string n) => Supports.FirstOrDefault(x => x.Name == n);
        public double StartAsd => Named("start")?.Asd ?? 0;
        public double EndAsd => Named("end")?.Asd ?? 0;
        public double StartLrfd => Named("start")?.Lrfd ?? 0;
        public double EndLrfd => Named("end")?.Lrfd ?? 0;
    }

    /// <summary>
    /// The vertical load path of the hold-downs, top down. Every hold-down point is traced to what carries it:
    /// <list type="bullet">
    /// <item>a WALL of the story below (on the same line for a wall's hold-down; any wall through the point for a beam end
    /// or a column base): its load goes into that wall's overturning (a force from above), and the wall's own hold-downs
    /// take it down;</item>
    /// <item>a COLUMN under that level: it carries the load to its base, which is traced again (to the foundation for a
    /// column standing on the ground);</item>
    /// <item>a plain WALL of the story below (or a shear wall past its shear part): a post or strap in the wall takes it down
    /// to what is under the wall, traced again, as a column ("wall W2 post"; owner 2026-11-10);</item>
    /// <item>a BEAM on that level: the beam's reactions at its supports (every wall and column under it anywhere along it, a
    /// beam at an end with neither; by its own free body, <see cref="BeamStatics"/>: two supports by statics, R_start = Σ T
    /// (L − a) / L on a simple span, an overhang past them too; three or more continuous, EI the same along it; each wind
    /// direction apart, the worse taken) are new hold-down points ("beam BM1 start", "beam BM1 at 12.5 ft"), traced to what
    /// is under each;</item>
    /// <item>the FOUNDATION under the first story;</item>
    /// <item>or nothing: an error (draw a beam or a column).</item>
    /// </list>
    /// Each connection is checked against <see cref="HoldownRules"/>; where the wall's hold-down family cannot anchor to what
    /// is under it, the connection is sized as the family that can, and flagged. New connections (beam ends, column bases)
    /// are sized for their reaction: a strap onto wood, the chosen anchored hold-down at the foundation. CONSERVATIVE (owner,
    /// 2026-11-07): a beam end's or column's connection is sized for the hold-downs' tension alone, no dead load counted
    /// against it; the Load Manager finds the net (<c>UpperUplift</c>: 0.6D and the roof's wind with it), often far less.
    /// Kept for now; to change if it becomes an issue (ROADMAP, step 5).
    /// </summary>
    public sealed class LoadPathTrace
    {
        /// <summary>A point on a beam, a column or a wall: this close in plan (ft).</summary>
        public const double Tolerance = StoryCascade.LineTolerance;

        private readonly List<FramingBeam> beams;
        private readonly List<FramingColumn> columns;
        private readonly List<FramingWall> walls;
        private readonly Func<HoldownFamilies, WoodTypes, double, HoldownChoice> size;

        /// <summary>Loads arriving on the level on top of story k (k = −1: the foundation), waiting for that level to be traced.</summary>
        private readonly Dictionary<int, List<Arrival>> pending = new Dictionary<int, List<Arrival>>();
        private readonly Dictionary<int, List<StackedForce>> extra = new Dictionary<int, List<StackedForce>>();

        /// <summary>Every point traced, by the level it lands on (k = the level on top of story k; −1 = the foundation).</summary>
        public Dictionary<int, List<HoldownLanding>> ByLevel { get; } = new Dictionary<int, List<HoldownLanding>>();

        /// <summary>The beams that carry something, with their reactions.</summary>
        public List<BeamReactions> Beams { get; } = new List<BeamReactions>();

        /// <param name="size">Sizes a hold-down: family, species, required tension (lb ASD) → the lightest that works (null: none).</param>
        /// <param name="walls">Every wall of the model (the shear walls' shear parts come with each story's <c>Land</c>).</param>
        public LoadPathTrace(IEnumerable<FramingBeam> beams, IEnumerable<FramingColumn> columns, Func<HoldownFamilies, WoodTypes, double, HoldownChoice> size,
                             IEnumerable<FramingWall> walls = null)
        {
            this.beams = beams?.ToList() ?? new List<FramingBeam>();
            this.columns = columns?.ToList() ?? new List<FramingColumn>();
            this.walls = walls?.ToList() ?? new List<FramingWall>();
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
            /// <summary>A wall's end: where it presses in the cases it does not lift (its end post's centreline); null: at <see cref="At"/>.</summary>
            public Point? PressAt;
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
                // one column base for everything a column carries: the worse wind direction (a wall's posts: one at each place)
                foreach (var g in p.GroupBy(x => (x.From, Math.Round(x.At.X, 4), Math.Round(x.At.Y, 4))))
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
                            PressAt = PressPoint(w, at, r.Holdown),
                        });
                    }
                }
            if (!ByLevel.ContainsKey(level)) ByLevel[level] = new List<HoldownLanding>();
            var beamLoads = new Dictionary<FramingBeam, List<Arrival>>();
            foreach (var a in arriving) Place(a, level, wallsBelow, beamLoads);

            // the beams: their reactions are new hold-down points at their ends, on the walls, columns or beams under them. A
            // beam end on another beam is a load on it, so the level's beams are solved together: going round until nothing
            // changes (each a simple span, linear), which settles a chain in a pass or two and beams bearing on each other
            // round a loop (reciprocal framing) to the exact answer. A loop resting only on each other's ends never settles:
            // a mechanism, its ends left on nothing (an error: draw a column or a post).
            var onBeams = new Dictionary<(FramingBeam bm, string support), (FramingBeam onto, Arrival a)>();
            var solved = new Dictionary<FramingBeam, (BeamReactions r, List<(string name, Arrival a)> at)>();
            var cut = new HashSet<(FramingBeam, string)>();
            var last = new Dictionary<(FramingBeam, string), double>(); // each support's force at the last pass
            var supportsOf = new Dictionary<FramingBeam, List<BeamSupportReaction>>();
            List<BeamSupportReaction> Supports(FramingBeam bm) =>
                supportsOf.TryGetValue(bm, out var s) ? s : supportsOf[bm] = SupportsOf(bm, level, wallsBelow);
            for (int round = 0; round < 2; round++)
            {
                bool settled = false;
                for (int pass = 0; pass < 5000 && !settled; pass++)
                {
                    double change = 0, scale = 0;
                    var carrying = beamLoads.Keys.Concat(onBeams.Values.Select(v => v.onto)).Distinct().ToList();
                    foreach (var bm in carrying)
                    {
                        var loads = (beamLoads.TryGetValue(bm, out var own) ? own : new List<Arrival>())
                                    .Concat(onBeams.Where(o => o.Value.onto == bm).Select(o => o.Value.a)).ToList();
                        if (loads.Count == 0) continue;
                        var s = React(bm, loads, Supports(bm));
                        solved[bm] = s;
                        foreach (var (name, a) in s.at)
                        {
                            var key = (bm, name);
                            var onto = a == null || cut.Contains(key) ? null : BeamUnder(a, level, wallsBelow, bm);
                            last.TryGetValue(key, out var was);
                            double size = a == null ? 0 : Size(a);
                            change += Math.Abs(size - was);
                            last[key] = size;
                            scale += size;
                            if (onto == null) onBeams.Remove(key);
                            else onBeams[key] = (onto, a);
                        }
                    }
                    settled = pass > 0 && change <= 1e-9 * (1 + scale);
                }
                if (settled) break;
                // a mechanism: the ends resting on one another's ends with nothing under them carry nothing on
                bool BackTo(FramingBeam from, FramingBeam to)
                {
                    var seen = new HashSet<FramingBeam>();
                    var next = new Stack<FramingBeam>(new[] { from });
                    while (next.Count > 0)
                    {
                        var cur = next.Pop();
                        foreach (var o in onBeams.Where(x => x.Key.bm == cur).Select(x => x.Value.onto).ToList())
                        {
                            if (o == to) return true;
                            if (seen.Add(o)) next.Push(o);
                        }
                    }
                    return false;
                }
                foreach (var kv in onBeams.ToList().Where(kv => BackTo(kv.Value.onto, kv.Key.bm)).ToList()) cut.Add(kv.Key);
                foreach (var k in cut) onBeams.Remove(k);
                solved.Clear();
                last.Clear();
            }
            foreach (var kv in solved)
            {
                Beams.Add(kv.Value.r);
                foreach (var (name, a) in kv.Value.at)
                    if (a != null) Place(a, level, wallsBelow, cut.Contains((kv.Key, name)) ? null : new Dictionary<FramingBeam, List<Arrival>>(), kv.Key);
            }
        }

        /// <summary>
        /// What holds a beam up, in order along it: the walls of the story below crossing it, ending under it or under one of
        /// its ends (a wall along it: where they overlap ends), the columns under it, and at an end with neither, a beam (as the
        /// gravity load path, <c>GravityLoadPath.SupportsOf</c>). Two within <see cref="Tolerance"/> are one; one within it of an
        /// end is that end.
        /// </summary>
        private List<BeamSupportReaction> SupportsOf(FramingBeam bm, int level, IDictionary<int, WallData> wallsBelow)
        {
            double L = bm.Length;
            var list = new List<BeamSupportReaction>();
            if (level < 0) return list;
            var u = L > 0 ? (bm.End - bm.Start) / L : new Vector();
            double Along(Point p) => L <= 0 ? 0 : Math.Max(0, Math.Min(L, Vector.Multiply(p - bm.Start, u)));
            bool Under(Point p) => WallOrColumnUnder(new Arrival { At = p }, level, wallsBelow, out _, out _);
            var xs = new List<double>();
            if (Under(bm.Start) || OnBeam(bm.Start, level, bm) != null) xs.Add(0);
            if (Under(bm.End) || OnBeam(bm.End, level, bm) != null) xs.Add(L);
            var segs = (wallsBelow?.Values ?? Enumerable.Empty<WallData>()).Select(w => (w.Start, w.End))
                       .Concat(walls.Where(w => w.Story == level).Select(w => (w.Start, w.End))); // every wall: a plain one holds it up too
            foreach (var (ws, we) in segs)
            {
                foreach (var p in new[] { ws, we }) if (OnSegment(p, bm.Start, bm.End)) xs.Add(Along(p));
                if (Crossing(bm.Start, bm.End, ws, we, out double t)) xs.Add(t * L);
            }
            foreach (var c in columns.Where(c => c.BaseStory <= level && c.TopStory >= level && OnSegment(c.At, bm.Start, bm.End))) xs.Add(Along(c.At));
            foreach (var x0 in xs.Select(x => x <= Tolerance ? 0 : x >= L - Tolerance ? L : x).OrderBy(x => x))
            {
                if (list.Any(s => Math.Abs(s.At - x0) <= Tolerance)) continue;
                string name = x0 <= 0 ? "start" : x0 >= L ? "end" : $"at {x0:0.##} ft";
                list.Add(new BeamSupportReaction { Name = name, At = x0, Where = name == "start" ? bm.Start : name == "end" ? bm.End : bm.Start + u * x0 });
            }
            return list;
        }

        /// <summary>Where two segments cross, as a share of the first (0 to 1); false when they do not, or run alongside.</summary>
        private static bool Crossing(Point a, Point b, Point c, Point d, out double t)
        {
            t = 0;
            Vector r = b - a, s = d - c, q = c - a;
            double den = Vector.CrossProduct(r, s);
            if (Math.Abs(den) < 1e-12) return false;
            t = Vector.CrossProduct(q, s) / den;
            double v = Vector.CrossProduct(q, r) / den;
            return t >= -1e-9 && t <= 1 + 1e-9 && v >= -1e-9 && v <= 1 + 1e-9;
        }

        /// <summary>The size of a beam end's force, to tell when the beams have settled.</summary>
        private static double Size(Arrival a) =>
            Math.Abs(a.Asd) + Math.Abs(a.Lrfd) + (a.CaseAsd?.Values.Sum(Math.Abs) ?? 0) + (a.CaseLrfd?.Values.Sum(Math.Abs) ?? 0) + (a.CaseUp?.Values.Sum(Math.Abs) ?? 0);

        /// <summary>
        /// A beam's reactions for the loads on it, by its own free body (<see cref="BeamStatics"/>: two supports by statics,
        /// an overhang past them too; three or more continuous, EI the same along it; owner 2026-11-10), and the new points at
        /// its supports (null: one with nothing to pass on). Each load's share of each support is its influence there: a unit
        /// load at its place. With fewer than two supports it cannot stand: it is taken as a simple span on its ends, and an end
        /// with nothing under it goes on nothing (an error: draw a wall, a column or a beam).
        /// </summary>
        private (BeamReactions r, List<(string name, Arrival a)> at) React(FramingBeam bm, List<Arrival> loads, List<BeamSupportReaction> supports)
        {
            double L = bm.Length;
            var reactions = new BeamReactions { Beam = bm, Stands = supports.Count >= 2 && L > 0 };
            var sup = reactions.Stands ? supports
                    : new List<BeamSupportReaction> { new BeamSupportReaction { Name = "start", At = 0, Where = bm.Start }, new BeamSupportReaction { Name = "end", At = L, Where = bm.End } };
            foreach (var s in sup) reactions.Supports.Add(new BeamSupportReaction { Name = s.Name, At = s.At, Where = s.Where });
            int n = sup.Count;
            double X(Point p) => L <= 0 ? 0 : Math.Max(0, Math.Min(L, Vector.Multiply(p - bm.Start, (bm.End - bm.Start) / L)));

            // each place a load pulls or presses: what a unit load there gives each support
            var places = new List<double>();
            foreach (var a in loads)
            {
                places.Add(X(a.At));
                if (a.PressAt != null) places.Add(X(a.PressAt.Value));
            }
            places = places.Distinct().ToList();
            double[][] infl = null;
            if (L > 0)
                infl = BeamStatics.Reactions(L, sup.Select(s => s.At).ToList(),
                    places.Select(x => { var u = new BeamLoads(); u.Points.Add((x, 1.0)); return u; }).ToList());
            double[] Share(double x)
            {
                int i = places.IndexOf(x);
                if (infl != null && i >= 0) return infl[i];
                return Enumerable.Repeat(1.0 / n, n).ToArray(); // a beam of no length: shared
            }

            var dir = new double[2, n];
            var dirL = new double[2, n];
            foreach (var a in loads)
            {
                double x = X(a.At);
                var k = Share(x);
                var load = new BeamLoad { From = a.From, A = x, Asd = a.Asd, Lrfd = a.Lrfd, LowEndUp = a.LowEndUp ?? true };
                for (int i = 0; i < n; i++)
                {
                    load.ToSupports.Add(a.Asd * k[i]);
                    // a load with no wind direction (from a beam or column above) goes with both
                    if (a.LowEndUp != false) { dir[0, i] += a.Asd * k[i]; dirL[0, i] += a.Lrfd * k[i]; }
                    if (a.LowEndUp != true) { dir[1, i] += a.Asd * k[i]; dirL[1, i] += a.Lrfd * k[i]; }
                }
                reactions.Loads.Add(load);
            }
            var first = loads.FirstOrDefault(a => a.Kind == PathPointKind.WallHoldown) ?? loads[0];
            var at = new List<(string, Arrival)>();
            for (int i = 0; i < n; i++)
            {
                var r = reactions.Supports[i];
                r.Asd = Math.Max(dir[0, i], dir[1, i]);
                r.Lrfd = Math.Max(dirL[0, i], dirL[1, i]);
                // case by case when every load knows its cases: each support's worst case of the sum
                int j = i;
                var shares = loads.Select(a => Share(X(a.At))[j]).ToList();
                var cA = SumByCase(loads.Select(a => a.CaseAsd), shares);
                var cL = SumByCase(loads.Select(a => a.CaseLrfd), shares);
                if (cA != null && cL != null) { r.Asd = Max(cA); r.Lrfd = Max(cL); }
                // the compression too: a wall end pressing on the beam is a load on it as much as one lifting it, where it
                // presses (its end post's centreline: as the Load Manager's free bodies take it), the pull at the hold-down
                Dictionary<string, double> cU = null;
                if (loads.All(a => a.CaseUp != null))
                {
                    cU = new Dictionary<string, double>();
                    foreach (var a in loads)
                    {
                        double kPull = Share(X(a.At))[j], kPress = a.PressAt == null ? kPull : Share(X(a.PressAt.Value))[j];
                        foreach (var c in a.CaseUp)
                            cU[c.Key] = (cU.TryGetValue(c.Key, out var v) ? v : 0) + c.Value * (c.Value < 0 ? kPress : kPull);
                    }
                }
                // a support that only presses down still lands on what is under it (in bearing: no hold-down to size)
                if (!(r.Asd > 0) && (cU == null || cU.Values.All(v => Math.Abs(v) < 0.05))) { at.Add((r.Name, null)); continue; }
                at.Add((r.Name, new Arrival
                {
                    At = r.Where, Asd = r.Asd, Lrfd = r.Lrfd, LowEndUp = null, Kind = PathPointKind.BeamEnd, From = $"beam {bm.Id} {r.Name}",
                    UpperStory = first.UpperStory, WallId = first.WallId, Family = first.Family, Species = first.Species,
                    CaseAsd = cA, CaseLrfd = cL, CaseUp = cU,
                }));
            }
            return (reactions, at);
        }

        /// <summary>
        /// The beam a beam end lands on when no wall or column is under it: one it lands on partway along, else one whose end
        /// is there (as the gravity load path). Null: a wall or a column carries it, or nothing does.
        /// </summary>
        private FramingBeam BeamUnder(Arrival a, int level, IDictionary<int, WallData> wallsBelow, FramingBeam except)
        {
            if (level < 0 || WallOrColumnUnder(a, level, wallsBelow, out _, out _)) return null;
            return OnBeam(a.At, level, except);
        }

        /// <summary>
        /// Where a wall's hold-down end presses down: its end post's centreline, half a post nearer the wall's end than the
        /// hold-down (as <c>DesignPass.CaseForcesOn</c> and <c>ShearWallCheck.CompressionAt</c>).
        /// </summary>
        private static Point PressPoint(WallData w, Point at, HoldownChoice holdown)
        {
            double half = WallSchedule.PostAlongWall(holdown) / 24.0;
            var end = (at - w.Start).Length <= (at - w.End).Length ? w.Start : w.End;
            var toEnd = end - at;
            return toEnd.Length < 1e-9 ? at : at + toEnd / toEnd.Length * half;
        }

        private FramingBeam OnBeam(Point at, int level, FramingBeam except)
        {
            var on = beams.Where(b => b != except && b.OnStory == level && OnSegment(at, b.Start, b.End)).ToList();
            return on.FirstOrDefault(b => (at - b.Start).Length > Tolerance && (at - b.End).Length > Tolerance) ?? on.FirstOrDefault();
        }

        /// <summary>A wall of the story below through the point (on the same line for a wall's hold-down), else a column under it.</summary>
        private bool WallOrColumnUnder(Arrival a, int level, IDictionary<int, WallData> wallsBelow, out int? wallId, out FramingColumn col) =>
            WallOrColumnUnder(a, level, wallsBelow, out wallId, out col, out _);

        /// <summary>
        /// What stands under a point at <paramref name="level"/>: a shear wall's shear part (<paramref name="wallId"/>; on the
        /// same line for a wall's hold-down), else a column, else any other wall of the story below through the point
        /// (<paramref name="plain"/>: a plain wall, or a shear wall past its shear part).
        /// </summary>
        private bool WallOrColumnUnder(Arrival a, int level, IDictionary<int, WallData> wallsBelow, out int? wallId, out FramingColumn col, out FramingWall plain)
        {
            wallId = wallsBelow == null ? null
                   : a.Dir != null ? StoryCascade.WallUnder(a.At, a.Dir.Value, wallsBelow)
                   : StoryCascade.WallUnder(a.At, WallDirs.EastWest, wallsBelow) ?? StoryCascade.WallUnder(a.At, WallDirs.NorthSouth, wallsBelow);
            col = null;
            plain = null;
            if (wallId != null) return true;
            var at = a.At;
            col = columns.FirstOrDefault(c => c.BaseStory <= level && c.TopStory >= level && (c.At - at).Length <= Tolerance);
            if (col != null) return true;
            if (level < 0) return false;
            plain = walls.FirstOrDefault(w => w.Story == level && OnSegment(at, w.Start, w.End));
            return plain != null;
        }

        /// <summary>
        /// What carries one point at <paramref name="level"/>, and what that does. <paramref name="beamLoads"/> null: no beam
        /// is looked for (an end left on nothing); <paramref name="except"/>: the beam whose end the point is.
        /// </summary>
        private void Place(Arrival a, int level, IDictionary<int, WallData> wallsBelow, Dictionary<FramingBeam, List<Arrival>> beamLoads, FramingBeam except = null)
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
            FramingWall plain = null;
            if (level < 0) { l.Support = SupportKind.Foundation; l.OnFoundation = true; }
            else
            {
                WallOrColumnUnder(a, level, wallsBelow, out wallId, out col, out plain);
                if (wallId != null) { l.Support = SupportKind.Wall; l.LowerWallId = wallId; l.SupportId = $"wall {wallId}"; }
                else if (col != null) { l.Support = SupportKind.Column; l.SupportId = $"column {col.Id}"; }
                else if (plain != null) { l.Support = SupportKind.Wall; l.SupportId = $"wall {plain.Id}"; }
                else if (beamLoads != null && (beam = OnBeam(a.At, level, except)) != null)
                { l.Support = SupportKind.Beam; l.SupportId = $"beam {beam.Id}"; }
                else l.Support = SupportKind.None;
            }

            // the connection: the wall's own hold-down when its family can anchor there; else the family that can, sized
            if (l.Support != SupportKind.None)
            {
                var fam = a.Kind == PathPointKind.WallHoldown ? HoldownRules.For(l.Support, a.Family) : HoldownRules.NewConnection(l.Support, a.Family);
                l.Family = fam;
                if (a.Kind == PathPointKind.WallHoldown && fam == a.Family) l.Choice = a.Chosen;
                // conservative (owner): the hold-downs' tension alone, no dead load against it (see the class's notes)
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
                case SupportKind.Wall when plain != null:
                    // a plain wall (or a shear wall past its shear part): a post or strap in it takes the load down to what is
                    // under the wall, the level under its story (the foundation for the first story), as a column (owner 2026-11-10)
                    int under = plain.Story - 1;
                    if (!pending.TryGetValue(under, out var post)) pending[under] = post = new List<Arrival>();
                    post.Add(new Arrival
                    {
                        At = a.At, Asd = a.Asd, Lrfd = a.Lrfd, LowEndUp = a.LowEndUp, Kind = PathPointKind.WallPost, From = $"wall {plain.Id} post",
                        UpperStory = a.UpperStory, WallId = a.WallId, Family = a.Family, Species = a.Species, CaseAsd = a.CaseAsd, CaseLrfd = a.CaseLrfd, CaseUp = a.CaseUp,
                    });
                    break;
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
