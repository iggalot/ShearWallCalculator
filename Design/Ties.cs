using System;
using System.Collections.Generic;
using System.Linq;

namespace ShearWallCalculator.Design
{
    /// <summary>
    /// A strap across a butt splice of a rim or a line of roof blocking (a collector or a chord): the lightest strap tie of the
    /// catalog (C-C-2026 p. 291, the (160) loads) whose allowable tension takes the whole force and that fits the member's
    /// face (its width at most the member's depth less 1", for the nails' edge distance). Its fasteners are the catalog's
    /// total, half on each side of the splice.
    /// </summary>
    public sealed class SpliceStrap
    {
        public string Model { get; set; }
        public int Gauge { get; set; }
        public double WidthIn { get; set; }
        public double LengthIn { get; set; }
        public int Nails { get; set; }
        public string NailSize { get; set; }
        public double AllowableLb { get; set; }
        public double ForceLb { get; set; }
        public string Fails { get; set; }

        public static readonly string[] Citations =
        {
            $"Simpson Strong-Tie C-C-2026, {SimpsonCatalog.PagesStraps}, \"HRS/HTP/LSTA/LSTI/MST/MSTA/MSTC/MSTI/ST Strap Ties\", allowable tension (160) for DF/SP or SPF/HF; the fasteners are the total, half each side of the splice. The lightest that takes the force and fits the member's face is picked.",
        };

        private static readonly Lazy<SimpsonCatalog> Catalog = new Lazy<SimpsonCatalog>(() => new SimpsonCatalog());

        public string Describe() => Fails ?? $"{Model} ({Gauge} ga, {WidthIn:0.###}\" x {LengthIn:0.###}\"), ({Nails}) {NailSize} nails total, half each side: {AllowableLb:0} lb ≥ {ForceLb:0} lb";

        public static SpliceStrap Pick(double forceLb, bool spf, double memberDepthIn)
        {
            string key = spf ? "SPF_HF" : "DF_SP";
            double force = Math.Abs(forceLb);
            var options = Catalog.Value.StrapTiesManager.StrapTies_Dict.Values
                .SelectMany(m => m.FastenerOptions.Select(f => (m, f, a: f.AllowableTensionLoads != null && f.AllowableTensionLoads.TryGetValue(key, out var v) ? v : 0)))
                .Where(o => o.a > 0 && o.m.W <= memberDepthIn - 1 + 1e-9).ToList();
            var pick = options.Where(o => o.a >= force - 1e-9).OrderBy(o => o.a).ThenBy(o => o.m.L).FirstOrDefault();
            if (pick.m == null)
            {
                double most = options.Count == 0 ? 0 : options.Max(o => o.a);
                return new SpliceStrap { ForceLb = force, Fails = $"No strap tie of the catalog ({SimpsonCatalog.PagesStraps}) fitting a {memberDepthIn:0.##}\" face takes {force:0} lb (the most: {most:0} lb): two straps, a coiled strap, or a stronger splice." };
            }
            return new SpliceStrap
            {
                Model = pick.m.Model, Gauge = pick.m.Gauge, WidthIn = pick.m.W, LengthIn = pick.m.L, Nails = pick.f.Screws_Quantity,
                NailSize = $"{pick.f.ScrewSize}\" x {pick.f.Length:0.##}\"", AllowableLb = pick.a, ForceLb = force,
            };
        }
    }

    /// <summary>
    /// Framing plates tying the rim (or the roof's blocking) to a wall's top plates where toe-nails would need closer than the
    /// app's least spacing: LTP4 (C-C-2026 p. 310, configuration 7, load direction G along the plates, (160)), spaced for the
    /// wall's shear, s = 12 A / v, to the inch below, at most 24". Placed on a face with no wood structural panel over the
    /// plates (gypsum goes on after) with the tabulated 0.131" x 1-1/2" nails; on a wall sheathed with wood panels on both
    /// faces, over the thinner, which footnote 3 allows up to 1/2" with 0.131" x 2-1/2" nails at the full load.
    /// </summary>
    public sealed class TopClip
    {
        public const string Model = "LTP4";
        public const double MaxSpacingIn = 24, MinSpacingIn = 6;
        public double ShearAsdPlf { get; set; }
        public double AllowableLb { get; set; }
        public string Fastener { get; set; }
        public string Placement { get; set; }
        public double SpacingIn { get; set; }
        public string Fails { get; set; }
        public double CapacityPlf => SpacingIn > 0 ? AllowableLb * 12 / SpacingIn : 0;

        public static IEnumerable<string> Citations => FramingPlates_Manager.Citations;

        public string Describe() => Fails ?? $"{Model} at {SpacingIn:0}\" o.c. ({Fastener}; {Placement}): {AllowableLb:0} lb each, {CapacityPlf:0} plf ≥ {ShearAsdPlf:0} plf";

        /// <param name="panelFacesIn">The wall's wood structural panel faces' thicknesses (in); a face with none (bare or gypsum) is not listed.</param>
        public static TopClip Design(double vAsdPlf, bool spf, IList<double> panelFacesIn)
        {
            var row = new FramingPlates_Manager().Of(Model, "0.131 x 1-1/2 nails", "G");
            var c = new TopClip { ShearAsdPlf = Math.Abs(vAsdPlf), AllowableLb = row.Allowable160[spf ? "SPF_HF" : "DF_SP"] };
            var panels = panelFacesIn ?? new List<double>();
            if (panels.Count < 2)
            {
                c.Fastener = "(12) 0.131\" x 1-1/2\" nails";
                c.Placement = panels.Count == 0 ? "either face" : "on the face with no wood panel sheathing";
            }
            else
            {
                double t = panels.Min();
                c.Placement = $"over the {t:0.###}\" wood panel sheathing (C-C-2026 p. 310 footnote 3)";
                c.Fastener = "(12) 0.131\" x 2-1/2\" nails";
                if (t > 0.5 + 1e-9) { c.Fails = $"Both faces have wood panels, the thinner {t:0.###}\": the catalog gives the LTP4 over 1/2\" at most (p. 310 footnote 3): clip the plates before sheathing, or another tie."; return c; }
            }
            if (c.ShearAsdPlf <= 1e-9) { c.SpacingIn = MaxSpacingIn; return c; }
            c.SpacingIn = Math.Min(MaxSpacingIn, Math.Floor(c.AllowableLb * 12 / c.ShearAsdPlf + 1e-9));
            if (c.SpacingIn < MinSpacingIn - 1e-9)
                c.Fails = $"{Model}s would need {c.AllowableLb * 12 / c.ShearAsdPlf:0.#}\" o.c. for {c.ShearAsdPlf:0} plf, closer than {MinSpacingIn:0}\": a stronger tie (blocking with A35s, or a continuous strap).";
            return c;
        }
    }
}
