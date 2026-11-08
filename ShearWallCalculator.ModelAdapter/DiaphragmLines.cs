using System;
using System.Collections.Generic;
using System.Linq;
using BuildingModel.Geometry;
using BuildingModel.Model;
using NDS_WoodLibrary.NDS2018;
using ShearWallCalculator.Design;

namespace ShearWallCalculator.ModelAdapter
{
    /// <summary>
    /// Each diaphragm's lines of shear walls (<see cref="DiaphragmLine"/>, owner 2026-11-07: the line shear and the
    /// collectors). The walls on one line in one level's diaphragm (the tops of the story under it, the sills of the story
    /// standing on it: their <see cref="ShearPairs"/>) take the line's force R from the diaphragm in each case. The diaphragm
    /// gives it along its whole depth on the line, v = R / depth, and the boundary nailing along the line is sized for the
    /// worst v (ASD, 0.6 W; <see cref="DiaphragmNailing"/>). The top plates gather it into the walls' shear parts: the collector
    /// force at a point is what the diaphragm has delivered up to it less what the walls have taken
    /// (<see cref="TopPlateCollector"/>).
    /// </summary>
    public static class DiaphragmLines
    {
        /// <summary>Parallel walls this close across (ft) are on one line: the app's, for walls drawn a little off a common line.</summary>
        public const double LineTolerance = 0.5;

        public static List<DiaphragmLine> Design(Building b, LateralSection s, ModelDefaults defaults = null)
        {
            var list = new List<DiaphragmLine>();
            if (b == null || s?.Walls == null || s.Walls.Count == 0) return list;
            var def = defaults ?? b.Defaults ?? new ModelDefaults();
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
                    foreach (var c in clusters) list.Add(Line(level, levels.IndexOf(level), level == roof, ew, c, cells, s.Cases, def));
                }
            }
            return list;
        }

        private static double Across(Point2 p, bool ew) => ew ? p.Y : p.X;
        private static double Along(Point2 p, bool ew) => ew ? p.X : p.Y;

        private static DiaphragmLine Line(Level level, int index, bool isRoof, bool ew, List<ShearPair> pairs, List<Rect2> cells, IList<string> cases, ModelDefaults def)
        {
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

            double worst = 0, worstN = 0, worstAt = start;
            foreach (var k in cases ?? new List<string>())
            {
                double r = pairs.Sum(p => p.OnOtherIn(k));
                double v = r / depth;
                line.Shear[k] = Math.Round(v, 1);
                worst = Math.Max(worst, Math.Abs(v));
                // the collector: what the diaphragm has delivered (−R spread on its spans) and the walls have taken, up to t
                var loads = merged.Select(m => (a: m.lo, b: m.hi, f: -r * (m.hi - m.lo) / depth))
                                  .Concat(pairs.Select(p => (a: Math.Min(Along(p.Start, ew), Along(p.End, ew)), b: Math.Max(Along(p.Start, ew), Along(p.End, ew)), f: p.OnOtherIn(k)))).ToList();
                foreach (double t in loads.SelectMany(l => new[] { l.a, l.b }).Distinct())
                {
                    double n = loads.Sum(l => l.b - l.a < 1e-9 ? (t >= l.a - 1e-9 ? l.f : 0) : l.f * Math.Max(0, Math.Min(t, l.b) - l.a) / (l.b - l.a));
                    if (Math.Abs(n) > worstN) { worstN = Math.Abs(n); worstAt = t; }
                }
            }
            line.ShearAsdPlf = Math.Round(0.6 * worst, 1);

            // the boundary nailing (the roof's or the floors' sheathing; the framing's species for footnote 2)
            var species = Enum.TryParse(def.StudSpecies, out Species sp1) ? sp1 : Species.DouglasFirLarch;
            var grade = Enum.TryParse(def.StudGrade, out Grade gr1) ? gr1 : Grade.No2;
            double sg = DiaphragmNailing.SpecificGravityFactor(SpeciesInfo.SpecificGravity(species), species == Species.DouglasFirLarch || species == Species.SouthernPine);
            var nailing = DiaphragmNailing.Design(line.ShearAsdPlf, def.DiaphragmGrade == "StructuralI", isRoof ? def.RoofNail : def.FloorNail,
                isRoof ? def.RoofSheathing : def.FloorSheathing, def.DiaphragmFramingWidth, isRoof ? def.RoofBlocked : def.FloorBlocked, sg);
            line.Sheathing = nailing.Row ?? $"{nailing.GradeName}, {nailing.Nail}, {(isRoof ? def.RoofSheathing : def.FloorSheathing):0.###}\"";
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
            if (line.CollectorLb < 1) line.Collector = "No collector force: the walls' shear parts take the diaphragm's shear where it arrives.";
            else if (!pairs.Any(p => p.AtTop)) line.Collector = $"The {level.Name} floor's rim or blocking along the line (the walls standing on it, none under it): not designed.";
            else
            {
                string nominal = index <= 1 ? def.StudSizeFirstStory : def.StudSizeAbove;
                var c = WoodFraming.TopPlateCollector.Design(line.CollectorLb, species, grade, nominal, def.SpliceNail);
                line.Collector = c.Fails != null && c.Z <= 0 ? c.Fails :
                    $"{c.Lumber} double top plate: {c.NailsEachSide} {c.Nail} each side of a splice (Z' = {c.ZPrime:0} lb); one ply {c.TensionLb:0} lb in tension, {c.CompressionLb:0} lb in compression.";
                if (c.Fails != null) line.Fails = line.Fails == null ? c.Fails : $"{line.Fails} {c.Fails}";
            }
            return line;
        }
    }
}
