using calculator;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ShearWallCalculator.Design
{
    /// <summary>One line of a member's calculation: what, how (the formula with the numbers in it), and the result.</summary>
    public sealed class CalcLine
    {
        public string Item { get; set; }
        public string Calculation { get; set; }
        public string Result { get; set; }
        /// <summary>True for a line that fails a check (shown in the fail colour).</summary>
        public bool Fails { get; set; }
        /// <summary>True for a line that needs a look (a replaced hold-down type).</summary>
        public bool Flag { get; set; }

        public CalcLine(string item, string calculation, string result, bool fails = false, bool flag = false)
        {
            Item = item; Calculation = calculation; Result = result; Fails = fails; Flag = flag;
        }

        public override string ToString() => $"{Item}: {Calculation} = {Result}";
    }

    /// <summary>
    /// The calculation sheets of the detail views and the report: a wall (shear, overturning, the forces from above, its
    /// hold-downs and what they anchor to), a beam carrying hold-downs (its loads and simple-span reactions) and a column
    /// (what lands on it, and its base). They restate the numbers of <see cref="ShearWallDesigner.Check"/> and
    /// <see cref="LoadPathTrace"/>; they compute nothing new.
    /// </summary>
    public static class DetailCalcs
    {
        public static string Support(HoldownLanding l) =>
            l == null ? "?" : l.Support == SupportKind.Foundation ? "the foundation" : l.Support == SupportKind.None ? "nothing under it" : l.SupportId;

        public static string FamilyName(HoldownFamilies f) =>
            f == HoldownFamilies.HDU_ToFoundation ? "HDUE / DTT" : f == HoldownFamilies.HTT_ToFoundation ? "LTT / HTT" :
            f == HoldownFamilies.STHD_ToFoundation ? "STHD" : f == HoldownFamilies.Strap_ToWallBelow ? "strap" : "HDU (discontinued)";

        /// <summary>
        /// A wall's sheet. <paramref name="own"/>: its two hold-downs as traced (what each anchors to), may be empty.
        /// </summary>
        public static List<CalcLine> Wall(WallData w, WallDesignResult r, IEnumerable<HoldownLanding> own)
        {
            var lines = new List<CalcLine>();
            if (w == null || r == null) return lines;
            double L = r.Length, h = r.Height, f = r.LoadFactor;
            string fac = f.ToString("0.0#");
            lines.Add(new CalcLine("Design shear", $"governing case {r.GoverningCase ?? "-"} (strength level)", $"V = {r.V:0.00} k"));
            lines.Add(new CalcLine("Unit shear", $"v = {fac} V / L = {fac} × {r.V * 1000:0} / {L:0.##}", $"{r.Demand:0} plf"));
            lines.Add(new CalcLine("Capacity", r.Panel ?? "", double.IsNaN(r.Capacity) ? "no table value" : $"{r.Capacity:0} plf, ratio {r.ShearRatio:0.00}", fails: !r.ShearOk));
            lines.Add(new CalcLine("Aspect ratio", $"h / b = {h:0.##} / {L:0.##}", $"{r.AspectRatio:0.00} (max {r.MaxAspectRatio:0.0})", fails: !r.AspectOk));

            double M = r.V * 1000 * h;
            lines.Add(new CalcLine("Overturning", $"M = V h = {r.V * 1000:0} × {h:0.##}", $"{M:0} lb-ft"));
            lines.Add(new CalcLine("Lever arm", $"d = L − 1.5 post = {L:0.##} − 1.5 × {r.PostAlongWall:0.##}/12 (hold-down on its post's inside face, to the far post's centerline)", $"{r.HoldownArm:0.##} ft"));
            lines.Add(new CalcLine("Own uplift", $"T = 0.6 M / d = 0.6 × {M:0} / {r.HoldownArm:0.##} (no dead load counted)", $"{r.OwnTensionAsd:0} lb (ASD)"));
            foreach (var s in r.FromAbove ?? new List<StackedForce>())
                lines.Add(new CalcLine("From above", $"{s.From}: {(s.Asd >= 0 ? "uplift" : "bearing")} at {s.Along:0.##} ft from the {(w.WallDir == WallDirs.EastWest ? "west" : "south")} end, " +
                                                     $"wind lifting the {(s.LowEndUp ? (w.WallDir == WallDirs.EastWest ? "west" : "south") : (w.WallDir == WallDirs.EastWest ? "east" : "north"))} end",
                                       $"{s.Asd:0} lb"));
            if (r.FromAbove != null && r.FromAbove.Count > 0)
                lines.Add(new CalcLine("Added by them", "Σ F × (distance to the compression post) / d, the worse wind direction", $"{r.TensionFromAboveAsd:0} lb"));
            lines.Add(new CalcLine("Hold-down tension", r.FromAbove != null && r.FromAbove.Count > 0 ? "own + from above" : "own", $"T = {r.TensionAsd:0} lb (ASD)"));
            if (r.HoldownNeeded)
                lines.Add(r.Holdown != null
                    ? new CalcLine("Hold-down", $"{r.Holdown.Model}{(r.HoldownFromStandardList ? " (standard list)" : "")}, allowable {r.Holdown.Allowable:0} lb",
                                   $"ratio {r.HoldownRatio:0.00}")
                    : new CalcLine("Hold-down", "none in the catalog is strong enough", "FAIL", fails: true));
            foreach (var l in (own ?? Enumerable.Empty<HoldownLanding>()).OrderBy(x => x.At.X + x.At.Y))
            {
                string end = EndName(w, l.At);
                if (!l.Supported)
                    lines.Add(new CalcLine($"{end} end", "anchors to nothing: no wall, column or beam under it", "draw a beam or a column", fails: true));
                else
                    lines.Add(new CalcLine($"{end} end", $"{FamilyName(l.Family)} to {Support(l)}" + (l.Note != null ? $": {l.Note}" : ""),
                                           l.Model ?? "none strong enough", fails: l.Model == null, flag: l.Note != null));
            }
            return lines;
        }

        /// <summary>"West", "East", "South" or "North": the end of the wall nearer the point.</summary>
        public static string EndName(WallData w, System.Windows.Point p)
        {
            bool ew = w.WallDir == WallDirs.EastWest;
            double mid = ew ? (w.Start.X + w.End.X) / 2 : (w.Start.Y + w.End.Y) / 2;
            double at = ew ? p.X : p.Y;
            return ew ? (at < mid ? "West" : "East") : (at < mid ? "South" : "North");
        }

        /// <summary>A beam's sheet: each load and its share at the two ends, the reactions, and what each end anchors to.</summary>
        public static List<CalcLine> Beam(BeamReactions br, IEnumerable<HoldownLanding> ends)
        {
            var lines = new List<CalcLine>();
            if (br == null) return lines;
            double L = br.Beam.Length;
            lines.Add(new CalcLine("Span", $"from ({br.Beam.Start.X:0.##}, {br.Beam.Start.Y:0.##}) to ({br.Beam.End.X:0.##}, {br.Beam.End.Y:0.##})", $"L = {L:0.##} ft"));
            var sup = br.Supports;
            lines.Add(new CalcLine("Supports", br.Method, string.Join(", ", sup.Select(s => $"{s.Name} ({s.At:0.##} ft)"))));
            if (sup.Count > 0 && sup.Min(s => s.At) > 0.01) lines.Add(new CalcLine("Cantilever", "its start runs past its first support", $"{sup.Min(s => s.At):0.##} ft"));
            if (sup.Count > 0 && L - sup.Max(s => s.At) > 0.01) lines.Add(new CalcLine("Cantilever", "its end runs past its last support", $"{L - sup.Max(s => s.At):0.##} ft"));
            string simple = sup.Count == 2 && sup[0].At <= 0 && sup[1].At >= L ? "R start = T (L − a) / L, R end = T a / L" : "each support's share from the beam's free body";
            string Each(IEnumerable<double> v) => string.Join(" / ", v.Select(x => $"{x:0}")) + " lb";
            foreach (var g in br.Loads.GroupBy(x => x.LowEndUp).OrderByDescending(x => x.Key))
            {
                lines.Add(new CalcLine("Wind", $"the direction lifting the {(g.Key ? "low" : "high")} ends of the walls", ""));
                foreach (var ld in g)
                    lines.Add(new CalcLine($"  {ld.From}", $"T = {ld.Asd:0} lb at a = {ld.A:0.##} ft: {simple}", Each(ld.ToSupports)));
                lines.Add(new CalcLine("  Sum", "", Each(Enumerable.Range(0, sup.Count).Select(i => g.Sum(x => i < x.ToSupports.Count ? x.ToSupports[i] : 0)))));
            }
            lines.Add(new CalcLine("Reactions", "the worse wind direction at each support (uplift + , ASD)", string.Join(", ", sup.Select(s => $"{s.Name} {s.Asd:0} lb"))));
            foreach (var l in ends ?? Enumerable.Empty<HoldownLanding>())
            {
                string which = SupportName(l.From);
                which = char.ToUpper(which[0]) + which.Substring(1);
                lines.Add(!l.Supported
                    ? new CalcLine($"{which} support", "nothing under it", "add a wall or a column", fails: true)
                    : new CalcLine($"{which} support", $"{FamilyName(l.Family)} to {Support(l)}, sized for {l.TensionAsd:0} lb", l.Model ?? "none strong enough", fails: l.Model == null));
            }
            return lines;
        }

        /// <summary>A beam support's name from its hold-down's: "beam BM1 at 12.5 ft" → "at 12.5 ft", "beam BM1 start" → "start".</summary>
        public static string SupportName(string from)
        {
            var w = (from ?? "").Split(' ');
            return w.Length >= 3 ? string.Join(" ", w.Skip(2)) : from ?? "";
        }

        /// <summary>A column's sheet: what lands on it, what goes down it, and its base connection.</summary>
        public static List<CalcLine> Column(FramingColumn c, string levels, IEnumerable<HoldownLanding> onIt, HoldownLanding bottom)
        {
            var lines = new List<CalcLine>();
            if (c == null) return lines;
            lines.Add(new CalcLine("Column", $"at ({c.At.X:0.##}, {c.At.Y:0.##})", levels));
            var on = (onIt ?? Enumerable.Empty<HoldownLanding>()).ToList();
            foreach (var l in on)
                lines.Add(new CalcLine("Lands on it", $"{l.From}" + (l.UpperStory > 0 ? $" (story {l.UpperStory}, wall {l.WallId})" : "") + $": {FamilyName(l.Family)} {l.Model ?? ""}", $"{l.TensionAsd:0} lb"));
            if (on.Count == 0) lines.Add(new CalcLine("Lands on it", "no hold-down or beam end", "0 lb"));
            if (bottom != null)
            {
                lines.Add(new CalcLine("Down the column", "the worse wind direction", $"{bottom.TensionAsd:0} lb (ASD)"));
                lines.Add(!bottom.Supported
                    ? new CalcLine("Base", "nothing under the base", "add a wall, a beam or a column", fails: true)
                    : new CalcLine("Base", $"{FamilyName(bottom.Family)} to {Support(bottom)}", bottom.Model ?? "none strong enough", fails: bottom.Model == null));
            }
            return lines;
        }
    }
}
