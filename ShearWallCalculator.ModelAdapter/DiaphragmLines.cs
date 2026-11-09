using System;
using System.Collections.Generic;
using System.Linq;
using BuildingModel.Geometry;
using BuildingModel.Model;
using WoodFraming.Connections;
using NDS_WoodLibrary.NDS2018;
using ShearWallCalculator.Design;

namespace ShearWallCalculator.ModelAdapter
{
    /// <summary>
    /// Each diaphragm's lines of shear walls (<see cref="DiaphragmLine"/>, owner 2026-11-07: the line shear and the
    /// collectors). The walls on one line in one level's diaphragm (the tops of the story under it, the sills of the story
    /// standing on it: their <see cref="ShearPairs"/>) take the line's force R from the diaphragm in each case. The diaphragm
    /// gives it along its whole depth on the line, v = R / depth, and the boundary nailing along the line is sized for the
    /// worst v (ASD, 0.6 W; <see cref="DiaphragmNailing"/>). The collector gathers it into the walls' shear parts: its force at
    /// a point is what the diaphragm has delivered up to it less what the walls have taken. Over the walls under the line it
    /// is their top plates (<see cref="WoodFraming.TopPlateCollector"/>); where no wall is under it (none at all, or past the
    /// lower walls' ends) the floor's rim (<see cref="WoodFraming.RimCollector"/>, owner 2026-11-08) or the roof's continuous
    /// blocking, strapped across every joint (owner 2026-11-09).
    /// </summary>
    public static class DiaphragmLines
    {
        /// <summary>Parallel walls this close across (ft) are on one line: the app's, for walls drawn a little off a common line.</summary>
        public const double LineTolerance = 0.5;

        public static List<DiaphragmLine> Design(Building b, LateralSection s, ConnectionDefaults connections = null)
        {
            var list = new List<DiaphragmLine>();
            if (b == null || s?.Walls == null || s.Walls.Count == 0) return list;
            var def = b.Defaults ?? new ModelDefaults();
            var cd = connections ?? ConnectionDefaults.Read(b);
            var levels = b.LevelsBottomUp;
            var roof = levels.LastOrDefault();
            var pairs = ShearPairs.Of(b, s).Where(p => !p.OnFoundation).ToList();
            foreach (var g in pairs.GroupBy(p => p.LevelId))
            {
                var level = b.Level(g.Key);
                if (level == null) continue;
                var cells = b.NetDiaphragm(level).Cells().ToList();
                foreach (bool ew in new[] { true, false })
                {
                    var clusters = new List<List<ShearPair>>();
                    foreach (var p in g.Where(p => (Math.Abs(p.Dir.X) > 0.5) == ew).OrderBy(p => Across(p.Middle, ew)))
                    {
                        if (clusters.Count > 0 && Math.Abs(Across(p.Middle, ew) - Across(clusters.Last()[0].Middle, ew)) <= LineTolerance) clusters.Last().Add(p);
                        else clusters.Add(new List<ShearPair> { p });
                    }
                    foreach (var c in clusters)
                    {
                        // the walls under the line, whole (their plates run their full length, past their shear parts)
                        var plates = c.Where(p => p.AtTop).Select(p => b.Find(p.WallId) as Wall).Where(w => w != null).Distinct()
                                      .Select(w => (lo: Math.Min(Along(w.Start, ew), Along(w.End, ew)), hi: Math.Max(Along(w.Start, ew), Along(w.End, ew)))).ToList();
                        list.Add(Line(level, levels.IndexOf(level), level == roof, ew, c, cells, s.Cases, def, cd, plates));
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// Each diaphragm's chords (owner 2026-11-08): for wind along x the diaphragm is a beam spanning in y between its
        /// east-west lines of shear walls (<paramref name="lines"/>), loaded by its own wind spread over its length,
        /// w = F / length (the worst case). Each span between two lines is simple, M = w L² / 8 at mid-span; past the end
        /// lines a cantilever, M = w a² / 2 at the line. The chords are at the diaphragm's edges there, T = C = M / d (ASD,
        /// 0.6 W), d its depth across. A chord is the double top plate of the walls under the edge whose length covers the
        /// point (spliced as a collector is), else on a floor the rim (a strap at each splice), on the roof its continuous
        /// blocking (a strap across every joint; owner 2026-11-09). Wind along y the same, x for y.
        /// </summary>
        public static List<DiaphragmChord> Chords(Building b, LateralSection s, List<DiaphragmLine> lines, ConnectionDefaults connections = null)
        {
            var list = new List<DiaphragmChord>();
            if (b == null || s?.Diaphragms == null || lines == null) return list;
            var def = b.Defaults ?? new ModelDefaults();
            var cd = connections ?? ConnectionDefaults.Read(b);
            var species = Enum.TryParse(def.StudSpecies, out Species sp1) ? sp1 : Species.DouglasFirLarch;
            var grade = Enum.TryParse(def.StudGrade, out Grade gr1) ? gr1 : Grade.No2;
            var roof = b.LevelsBottomUp.LastOrDefault();
            foreach (var dw in s.Diaphragms)
            {
                var level = b.Level(dw.LevelId);
                if (level == null) continue;
                var cells = b.NetDiaphragm(level).Cells().ToList();
                if (cells.Count == 0) continue;
                bool isRoof = level == roof;
                // the walls of the story under it, topped here: their double top plates
                var walls = b.WallsUnder(level).Where(w => w.TopLevelId == level.Id).ToList();
                // the lightest nailing of its sheathing (SDPWS Table 4.2A's 6" row, or 4.2C's one): away from the lines (owner 2026-11-09)
                double sg = DiaphragmNailing.SpecificGravityFactor(SpeciesInfo.SpecificGravity(species), species == Species.DouglasFirLarch || species == Species.SouthernPine);
                var light = DiaphragmNailing.Design(0, cd.StructuralI, isRoof ? cd.RoofNail : cd.FloorNail, isRoof ? cd.RoofSheathing : cd.FloorSheathing,
                                                    cd.DiaphragmFramingWidth, isRoof ? cd.RoofBlocked : cd.FloorBlocked, sg);
                foreach (bool alongX in new[] { true, false })
                {
                    // wind along x: the lines running east-west (at y), the span in y, the depth (and the chords' edges) in x
                    var on = lines.Where(l => l.LevelId == level.Id && l.DepthFt > 1e-6 &&
                                              (alongX ? Math.Abs(l.Y1 - l.Y0) < 1e-9 && Math.Abs(l.X1 - l.X0) > 1e-9 : Math.Abs(l.X1 - l.X0) < 1e-9 && Math.Abs(l.Y1 - l.Y0) > 1e-9))
                                  .OrderBy(l => alongX ? l.Y0 : l.X0).ToList();
                    if (on.Count == 0) continue;
                    var at = on.Select(l => alongX ? l.Y0 : l.X0).ToList();
                    DiaphragmLine LineAt(double v) => on.FirstOrDefault(l => Math.Abs((alongX ? l.Y0 : l.X0) - v) < 1e-9);
                    double f = (s.Cases ?? new List<string>()).Select(k => Math.Abs(alongX ? dw.FxOf(k) : dw.FyOf(k))).DefaultIfEmpty(0).Max();
                    if (f <= 1e-6) continue;
                    double lo = cells.Min(c => alongX ? c.Y0 : c.X0), hi = cells.Max(c => alongX ? c.Y1 : c.X1);
                    if (hi - lo <= 1e-6) continue;
                    double w = f / (hi - lo);
                    var spans = new List<(double from, double to, bool cant, double m, double t)>();
                    if (at[0] - lo > LineTolerance) spans.Add((lo, at[0], true, w * Math.Pow(at[0] - lo, 2) / 2, at[0]));
                    for (int i = 0; i + 1 < at.Count; i++)
                        if (at[i + 1] - at[i] > LineTolerance) spans.Add((at[i], at[i + 1], false, w * Math.Pow(at[i + 1] - at[i], 2) / 8, (at[i] + at[i + 1]) / 2));
                    if (hi - at.Last() > LineTolerance) spans.Add((at.Last(), hi, true, w * Math.Pow(hi - at.Last(), 2) / 2, at.Last()));
                    foreach (var sp in spans)
                    {
                        var c = Chord(b, level, isRoof, alongX, sp, w, cells, walls, def, cd, species, grade);
                        Zones(c, w, LineAt(sp.from), LineAt(sp.to), light);
                        list.Add(c);
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// The span's two nailing zones (owner 2026-11-09): the shear falls linearly from each line to nothing at mid-span (a
        /// cantilever's at its free end), from the larger of the line's own v and the span's, 0.6 w (L / 2) / d. The line's
        /// nailing runs out from it until v is under the lightest row's (rounded up to the 2 ft), the lightest row beyond; a
        /// line nailed at the lightest already has no zone.
        /// </summary>
        private static void Zones(DiaphragmChord c, double w, DiaphragmLine from, DiaphragmLine to, DiaphragmNailing light)
        {
            if (light == null || light.Fails != null || light.AllowablePlf <= 0) return;
            c.LightNailing = $"{light.Nail} at {light.BoundaryIn:0.#}\" o.c. at the boundaries and {light.EdgesIn:0.#}\" at the " + (light.Blocked ? "other panel edges (blocked)" : "supported edges (unblocked)");
            c.LightAllowablePlf = Math.Round(light.AllowablePlf, 1);
            double reach = c.Cantilever ? c.ToFt - c.FromFt : (c.ToFt - c.FromFt) / 2;
            double Heavy(DiaphragmLine l)
            {
                if (l == null || l.DepthFt <= 1e-6) return 0;
                if (l.BoundaryIn >= light.BoundaryIn - 1e-9 && l.EdgesIn >= light.EdgesIn - 1e-9) return 0;
                double vEnd = Math.Max(l.ShearAsdPlf, 0.6 * w * reach / l.DepthFt);
                if (vEnd <= light.AllowablePlf + 1e-9) return 0;
                double x = reach * (1 - light.AllowablePlf / vEnd);
                return Math.Min(reach, Math.Ceiling(x / 2 - 1e-9) * 2);
            }
            c.HeavyFromFt = Math.Round(Heavy(from), 2);
            c.HeavyToFt = Math.Round(Heavy(to), 2);
        }

        private static DiaphragmChord Chord(Building b, Level level, bool isRoof, bool alongX, (double from, double to, bool cant, double m, double t) sp, double w,
                                            List<Rect2> cells, List<Wall> walls, ModelDefaults def, ConnectionDefaults cd, Species species, Grade grade)
        {
            // the diaphragm across the span at the worst moment: the cells it crosses, merged; its edges the chords'
            var across = cells.Where(c => alongX ? c.Y0 - 1e-9 <= sp.t && sp.t <= c.Y1 + 1e-9 : c.X0 - 1e-9 <= sp.t && sp.t <= c.X1 + 1e-9)
                              .Select(c => alongX ? (lo: c.X0, hi: c.X1) : (lo: c.Y0, hi: c.Y1)).OrderBy(x => x.lo).ToList();
            var merged = new List<(double lo, double hi)>();
            foreach (var a in across)
            {
                if (merged.Count > 0 && a.lo <= merged.Last().hi + 1e-6) merged[merged.Count - 1] = (merged.Last().lo, Math.Max(merged.Last().hi, a.hi));
                else merged.Add(a);
            }
            double depth = merged.Sum(m => m.hi - m.lo);
            var chord = new DiaphragmChord
            {
                LevelId = level.Id, Roof = isRoof, WindAlongX = alongX, FromFt = Math.Round(sp.from, 4), ToFt = Math.Round(sp.to, 4), Cantilever = sp.cant,
                WPlf = Math.Round(w, 1), MomentLbFt = Math.Round(0.6 * sp.m, 1), AtFt = Math.Round(sp.t, 4), DepthFt = Math.Round(depth, 4),
            };
            if (depth <= 1e-6) { chord.Fails = $"The {level.Name} diaphragm has no depth across this span."; return chord; }
            double edgeLo = merged[0].lo, edgeHi = merged.Last().hi;
            chord.EdgeLoFt = Math.Round(edgeLo, 4);
            chord.EdgeHiFt = Math.Round(edgeHi, 4);
            double t = 0.6 * sp.m / depth;
            chord.ForceLb = Math.Round(t, 1);
            if (chord.ForceLb < 1) { chord.Chord = "No chord force."; return chord; }

            // the walls under an edge (running along the span, on the edge) whose length covers the point: their top plates
            bool AlongSpan(Wall x) => alongX ? Math.Abs(x.End.X - x.Start.X) < Math.Abs(x.End.Y - x.Start.Y) : Math.Abs(x.End.X - x.Start.X) > Math.Abs(x.End.Y - x.Start.Y);
            double AcrossOf(Wall x) => alongX ? (x.Start.X + x.End.X) / 2 : (x.Start.Y + x.End.Y) / 2;
            bool Covers(Wall x)
            {
                double a = alongX ? x.Start.Y : x.Start.X, c = alongX ? x.End.Y : x.End.X;
                return sp.t >= Math.Min(a, c) - 1e-9 && sp.t <= Math.Max(a, c) + 1e-9;
            }
            var parts = new List<string>();
            void Fail(string f) { if (f != null) chord.Fails = chord.Fails == null ? f : $"{chord.Fails} {f}"; }
            string axis = alongX ? "x" : "y";
            foreach (double edge in new[] { edgeLo, edgeHi })
            {
                var under = walls.Where(x => AlongSpan(x) && Math.Abs(AcrossOf(x) - edge) <= LineTolerance && Covers(x)).ToList();
                string where = $"{axis} = {edge:0.##} ft";
                if (under.Count > 0)
                {
                    chord.WallIds.AddRange(under.Select(x => x.Id).Where(id => !chord.WallIds.Contains(id)));
                    var c = WoodFraming.TopPlateCollector.Design(t, species, grade, b.StudSizeOf(under[0], level.Id), cd.SpliceNail);
                    parts.Add(c.Fails != null && c.Z <= 0 ? c.Fails :
                        $"At {where}: the {c.Lumber} double top plate, {c.NailsEachSide} {c.Nail} each side of a splice (Z' = {c.ZPrime:0} lb); one ply {c.TensionLb:0} lb in tension, {c.CompressionLb:0} lb in compression.");
                    Fail(c.Fails);
                }
                else
                {
                    var c = NoWallUnder(t, isRoof, species, grade, def);
                    var strap = Strap(c, species);
                    parts.Add(c.Fails != null && c.AreaIn2 <= 0 ? c.Fails :
                        $"At {where} (no wall under the edge): {MemberWords(c, level, isRoof)}, {c.TensionLb:0} lb in tension, {c.CompressionLb:0} lb in compression; " +
                        $"a strap across {(isRoof ? "every joint" : "each splice")} for {c.StrapLb:0} lb (ASD): {strap?.Describe() ?? "none"}.");
                    chord.StrapLb = Math.Max(chord.StrapLb, Math.Round(c.StrapLb, 1));
                    if (strap != null) { chord.Strap = strap.Describe(); Fail(strap.Fails); }
                    Fail(c.Fails);
                }
            }
            chord.Chord = string.Join(" ", parts);
            return chord;
        }

        /// <summary>Where no wall is under a line or an edge: the floor's rim (the joists' size), or the roof's continuous blocking (its own size).</summary>
        private static WoodFraming.RimCollector NoWallUnder(double forceLb, bool isRoof, Species species, Grade grade, ModelDefaults def) =>
            WoodFraming.RimCollector.Design(forceLb, species, grade, isRoof ? def.RoofBlockingSize : def.FloorJoistSize, isRoof ? "blocking" : "rim");

        /// <summary>The strap across a splice or joint of the rim or blocking, from the catalog (null: the member does not work).</summary>
        private static SpliceStrap Strap(WoodFraming.RimCollector c, Species species) =>
            c == null || c.DepthIn <= 0 ? null : SpliceStrap.Pick(c.StrapLb, species == Species.SprucePineFir || species == Species.HemFir, c.DepthIn);

        private static string MemberWords(WoodFraming.RimCollector c, Level level, bool isRoof) =>
            isRoof ? $"the roof's {c.Lumber} blocking (continuous, between the rafters)" : $"the {level.Name} floor's {c.Lumber} rim";

        private static string Cap(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1);

        private static double Across(Point2 p, bool ew) => ew ? p.Y : p.X;
        private static double Along(Point2 p, bool ew) => ew ? p.X : p.Y;

        private static DiaphragmLine Line(Level level, int index, bool isRoof, bool ew, List<ShearPair> pairs, List<Rect2> cells, IList<string> cases, ModelDefaults def, ConnectionDefaults cd,
                                          List<(double lo, double hi)> plates)
        {
            // a point within the length of a wall under the line has its top plates (none past its end: owner 2026-11-09);
            // elsewhere the floor's rim is the collector (owner 2026-11-08), or on the roof its continuous blocking (owner 2026-11-09)
            bool OverPlates(double t) => plates.Any(w => t >= w.lo - 1e-9 && t <= w.hi + 1e-9);
            double at = pairs.Average(p => Across(p.Middle, ew));
            // the diaphragm on the line: the cells it crosses or runs along, merged
            var spans = cells.Where(c => ew ? c.Y0 - LineTolerance <= at && at <= c.Y1 + LineTolerance : c.X0 - LineTolerance <= at && at <= c.X1 + LineTolerance)
                             .Select(c => ew ? (lo: c.X0, hi: c.X1) : (lo: c.Y0, hi: c.Y1)).OrderBy(x => x.lo).ToList();
            var merged = new List<(double lo, double hi)>();
            foreach (var sp in spans)
            {
                if (merged.Count > 0 && sp.lo <= merged.Last().hi + 1e-6) merged[merged.Count - 1] = (merged.Last().lo, Math.Max(merged.Last().hi, sp.hi));
                else merged.Add(sp);
            }
            double depth = merged.Sum(m => m.hi - m.lo);
            double start = merged.Count > 0 ? merged[0].lo : pairs.Min(p => Math.Min(Along(p.Start, ew), Along(p.End, ew)));
            double end = merged.Count > 0 ? merged.Last().hi : pairs.Max(p => Math.Max(Along(p.Start, ew), Along(p.End, ew)));
            var line = new DiaphragmLine
            {
                LevelId = level.Id, Roof = isRoof, DepthFt = Math.Round(depth, 4),
                X0 = ew ? start : at, Y0 = ew ? at : start, X1 = ew ? end : at, Y1 = ew ? at : end,
                WallIds = pairs.Select(p => p.WallId).Distinct().ToList(),
            };
            if (depth <= 1e-6) { line.Fails = $"The {level.Name} diaphragm does not reach this line of shear walls: no diaphragm to deliver their shear."; return line; }

            double worst = 0, worstN = 0, worstAt = start, platesN = 0, rimN = 0, rimAt = start;
            foreach (var k in cases ?? new List<string>())
            {
                double r = pairs.Sum(p => p.OnOtherIn(k));
                double v = r / depth;
                line.Shear[k] = Math.Round(v, 1);
                worst = Math.Max(worst, Math.Abs(v));
                // the collector: what the diaphragm has delivered (−R spread on its spans) and the walls have taken, up to t
                var loads = merged.Select(m => (a: m.lo, b: m.hi, f: -r * (m.hi - m.lo) / depth))
                                  .Concat(pairs.Select(p => (a: Math.Min(Along(p.Start, ew), Along(p.End, ew)), b: Math.Max(Along(p.Start, ew), Along(p.End, ew)), f: p.OnOtherIn(k)))).ToList();
                // N(t) is piecewise linear: its worst on each stretch is at a load's end or where the plates start or stop
                var ts = loads.SelectMany(l => new[] { l.a, l.b })
                              .Concat(plates.SelectMany(w => new[] { w.lo, w.hi }).Where(t => t > start && t < end)).Distinct();
                foreach (double t in ts)
                {
                    double n = loads.Sum(l => l.b - l.a < 1e-9 ? (t >= l.a - 1e-9 ? l.f : 0) : l.f * Math.Max(0, Math.Min(t, l.b) - l.a) / (l.b - l.a));
                    if (Math.Abs(n) > worstN) { worstN = Math.Abs(n); worstAt = t; }
                    // where the plates stop the force passes between them and the rim: both carry it there
                    bool edge = plates.Any(w => Math.Abs(t - w.lo) < 1e-9 || Math.Abs(t - w.hi) < 1e-9);
                    if (OverPlates(t)) platesN = Math.Max(platesN, Math.Abs(n));
                    if ((!OverPlates(t) || edge) && Math.Abs(n) > rimN) { rimN = Math.Abs(n); rimAt = t; }
                }
            }
            line.ShearAsdPlf = Math.Round(0.6 * worst, 1);

            // the boundary nailing (the roof's or the floors' sheathing; the framing's species for footnote 2)
            var species = Enum.TryParse(def.StudSpecies, out Species sp1) ? sp1 : Species.DouglasFirLarch;
            var grade = Enum.TryParse(def.StudGrade, out Grade gr1) ? gr1 : Grade.No2;
            double sg = DiaphragmNailing.SpecificGravityFactor(SpeciesInfo.SpecificGravity(species), species == Species.DouglasFirLarch || species == Species.SouthernPine);
            var nailing = DiaphragmNailing.Design(line.ShearAsdPlf, cd.StructuralI, isRoof ? cd.RoofNail : cd.FloorNail,
                isRoof ? cd.RoofSheathing : cd.FloorSheathing, cd.DiaphragmFramingWidth, isRoof ? cd.RoofBlocked : cd.FloorBlocked, sg);
            line.Sheathing = nailing.Row ?? $"{nailing.GradeName}, {nailing.Nail}, {(isRoof ? cd.RoofSheathing : cd.FloorSheathing):0.###}\"";
            line.Blocked = nailing.Blocked;
            line.Nail = nailing.Nail;
            line.BoundaryIn = nailing.BoundaryIn;
            line.EdgesIn = nailing.EdgesIn;
            line.AllowablePlf = Math.Round(nailing.AllowablePlf, 1);
            line.Note = nailing.Note;
            line.Fails = nailing.Fails;

            // the collector: the double top plate of the story under it (a wall's own framing defaults: the first story's
            // studs on the foundation, the size above higher up)
            line.CollectorLb = Math.Round(0.6 * worstN, 1);
            line.CollectorAtFt = Math.Round(worstAt - start, 2);
            void Fail(string f) { if (f != null) line.Fails = line.Fails == null ? f : $"{line.Fails} {f}"; }
            double platesLb = Math.Round(0.6 * platesN, 1), rimLb = Math.Round(0.6 * rimN, 1);
            if (line.CollectorLb < 1) { line.Collector = "No collector force: the walls' shear parts take the diaphragm's shear where it arrives."; return line; }
            var parts = new List<string>();
            if (platesLb >= 1)
            {
                // over the walls under the line: their double top plate (a wall's own framing defaults: the first story's
                // studs on the foundation, the size above higher up)
                string nominal = index <= 1 ? def.StudSizeFirstStory : def.StudSizeAbove;
                var c = WoodFraming.TopPlateCollector.Design(platesLb, species, grade, nominal, cd.SpliceNail);
                parts.Add(c.Fails != null && c.Z <= 0 ? c.Fails :
                    $"{c.Lumber} double top plate{(rimLb >= 1 ? $" ({platesLb:0} lb over the walls under the line)" : "")}: {c.NailsEachSide} {c.Nail} each side of a splice (Z' = {c.ZPrime:0} lb); one ply {c.TensionLb:0} lb in tension, {c.CompressionLb:0} lb in compression.");
                Fail(c.Fails);
            }
            if (rimLb >= 1)
            {
                // where no wall is under the line (none at all, or past the lower walls' ends): the floor's rim, one ply of the
                // joists' depth in the framing's lumber, a strap across each of its butt splices (owner 2026-11-08); on the roof
                // its continuous blocking, a strap across every joint (owner 2026-11-09)
                var c = NoWallUnder(rimLb, isRoof, species, grade, def);
                var strap = Strap(c, species);
                string where = plates.Count == 0 ? "no wall under the line" : $"past the walls under the line: {rimLb:0} lb at {rimAt - start:0.##} ft";
                parts.Add(c.Fails != null && c.AreaIn2 <= 0 ? c.Fails :
                    $"{Cap(MemberWords(c, level, isRoof))} ({where}): {c.TensionLb:0} lb in tension, {c.CompressionLb:0} lb in compression; " +
                    $"a strap across {(isRoof ? "every joint" : "each splice")} for {c.StrapLb:0} lb (ASD): {strap?.Describe() ?? "none"}.");
                line.StrapLb = Math.Round(c.StrapLb, 1);
                if (strap != null) { line.Strap = strap.Describe(); Fail(strap.Fails); }
                Fail(c.Fails);
            }
            line.Collector = string.Join(" ", parts);
            return line;
        }
    }
}
