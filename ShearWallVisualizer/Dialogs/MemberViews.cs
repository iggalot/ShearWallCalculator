using ShearWallCalculator;
using ShearWallCalculator.Design;
using ShearWallVisualizer.Controls;
using WoodFraming;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace ShearWallVisualizer.Dialogs
{
    /// <summary>What the construction window shows for a beam or a column.</summary>
    public sealed class MemberView
    {
        public string WindowTitle { get; set; }
        public string Title { get; set; }
        public string Subtitle { get; set; }
        /// <summary>The member's id: the 3D view keeps the user's turn while the same member is shown again.</summary>
        public string Key { get; set; }
        public Func<FrameworkElement> Elevation { get; set; }
        public Func<Model3DGroup> Model { get; set; }
        public Func<FrameworkElement> Pieces { get; set; }
        public List<CalcLine> Lines { get; set; } = new List<CalcLine>();
        public string Footnote { get; set; }
    }

    /// <summary>One end of a beam: its uplift reaction, its connection (the traced landing, null with no uplift) and what is under it.</summary>
    public sealed class BeamEndInfo
    {
        public string Which { get; set; }        // "start", "end"
        public double Reaction { get; set; }     // lb ASD, uplift
        public HoldownLanding Landing { get; set; }
        /// <summary>What it bears on: "column C3", "wall 4", "nothing found".</summary>
        public string Under { get; set; }
        public bool OnColumn => Under != null && Under.StartsWith("column");
        public string Connection => Landing == null ? "None" : !Landing.Supported ? "nothing under it" : Landing.Model ?? "none strong enough";
        public Brush Ink => Landing == null ? DetailInk.Timber : !Landing.Supported || Landing.Model == null ? Ui.FailInk : Landing.Note != null ? Ui.CloseInk : DetailInk.Timber;
    }

    /// <summary>
    /// The beams' and columns' views in the construction window: an elevation to scale with the loads, connections and
    /// supports, a 3D model and the list of pieces. Neither is sized yet: a beam is drawn 3 1/2 × 11 1/4 in., a column
    /// 3 1/2 × 3 1/2 in., and said so.
    /// </summary>
    public static class MemberViews
    {
        public const double BeamWidth = 3.5, BeamDepth = 11.25, PostSize = 3.5;
        private static readonly Brush AboveInk = Ui.Frozen("#C2410C");

        // ------------------------------------------------------------------ beam

        public static MemberView Beam(string id, string levelName, BeamReactions br, BeamEndInfo start, BeamEndInfo end, List<CalcLine> lines, string footnote)
        {
            double L = (br?.Beam?.Length ?? 0) * 12;
            return new MemberView
            {
                Key = "beam " + id,
                WindowTitle = $"Beam {id}: elevation, 3D and pieces",
                Title = $"Beam {id} · {levelName}",
                Subtitle = $"In the framing of {levelName}, {FtIn.Of(L)} span, start on the left. " +
                           (br == null || br.Loads.Count == 0 ? "No hold-downs stand on it. " : $"{br.Loads.Count} hold-down{(br.Loads.Count == 1 ? "" : "s")} of the story above stand on it, ") +
                           (br == null || br.Loads.Count == 0 ? "" : $"held down at its ends ({start.Reaction:N0} and {end.Reaction:N0} lb, ASD). ") +
                           "Not sized here: drawn 3 1/2 × 11 1/4 in.",
                Elevation = () => new BeamElevation(id, br, start, end),
                Model = () => BeamModel(br, start, end),
                Pieces = () => BeamPieces(id, br, start, end),
                Lines = lines ?? new List<CalcLine>(),
                Footnote = footnote,
            };
        }

        private static Model3DGroup BeamModel(BeamReactions br, BeamEndInfo start, BeamEndInfo end)
        {
            var g = new Model3DGroup();
            double L = (br?.Beam?.Length ?? 0) * 12;
            if (L <= 0) return g;
            g.Children.Add(Framing3DView.BoxModel(0, 0, 0, L, BeamDepth, BeamWidth, Framing3DView.PlateMaterial));
            foreach (var (e, x, inward) in new[] { (start, 0.0, 1.0), (end, L, -1.0) })
            {
                // what it bears on, and the connection on its face
                if (e.OnColumn) g.Children.Add(Framing3DView.BoxModel(x - PostSize / 2, -60, 0, x + PostSize / 2, -0.05, PostSize, Framing3DView.PostMaterial));
                else g.Children.Add(Framing3DView.BoxModel(x - 6, -60, -1, x + 6, -0.05, BeamWidth + 1, Framing3DView.StudMaterial));
                if (e.Landing != null)
                {
                    double sx = x + inward * 3;
                    g.Children.Add(Framing3DView.BoxModel(Math.Min(sx, sx + inward * 1.25), -20, BeamWidth + 0.02, Math.Max(sx, sx + inward * 1.25), BeamDepth - 1, BeamWidth + 0.12, Framing3DView.SteelMaterial));
                }
            }
            foreach (var ld in br.Loads)
            {
                double a = ld.A * 12;
                g.Children.Add(Framing3DView.BoxModel(a - 0.625, BeamDepth - 2, BeamWidth + 0.02, a + 0.625, BeamDepth + 20, BeamWidth + 0.12, Framing3DView.StrapMaterial));
            }
            return g;
        }

        private static FrameworkElement BeamPieces(string id, BeamReactions br, BeamEndInfo start, BeamEndInfo end)
        {
            var p = new StackPanel { Margin = new Thickness(4, 4, 16, 16) };
            double L = (br?.Beam?.Length ?? 0) * 12;
            WallBuildWindow.Section(p, "Beam", "");
            p.Children.Add(WallBuildWindow.Table(new[] { "Piece", "Size", "Qty", "Length", "Notes" }, new List<string[]>
            {
                new[] { $"Beam {id}", "not sized (drawn 3 1/2 × 11 1/4)", "1", FtIn.Of(L), "Its uplift is found here; size it for its gravity loads too, with bearing at each end." },
            }));
            WallBuildWindow.Section(p, "End connections", "Each end held down to what it bears on, for that end's uplift reaction (wind, ASD).");
            p.Children.Add(WallBuildWindow.Table(new[] { "End", "Uplift (lb)", "Connection", "Fasteners", "Allowable (lb)", "Bears on", "Notes" },
                new[] { start, end }.Select(e => new[]
                {
                    e.Which == "start" ? "Start (left)" : "End (right)", $"{e.Reaction:N0}", e.Connection, WallBuildWindow.Fast(e.Landing?.Choice?.Fasteners),
                    e.Landing?.Choice?.Allowable > 0 ? $"{e.Landing.Choice.Allowable:N0}" : "", e.Under ?? "",
                    e.Landing == null ? "No uplift at this end (gravity is not calculated)." : e.Landing.Note ?? "",
                }).ToList()));
            WallBuildWindow.Section(p, "Hold-downs on it from above", br == null || br.Loads.Count == 0 ? "None." : "Each pulls up on the beam at its distance a from the start.");
            if (br != null && br.Loads.Count > 0)
                p.Children.Add(WallBuildWindow.Table(new[] { "a (from the start)", "From", "Uplift (lb, ASD)", "To the start / end (lb)" },
                    br.Loads.OrderBy(x => x.A).Select(x => new[] { FtIn.Of(x.A * 12), x.From ?? "", $"{x.Asd:N0}", $"{x.StartAsd:N0} / {x.EndAsd:N0}" }).ToList()));
            Assumed(p, "The beam is a simple span between its supports; the connections are sized for uplift only.",
                       "Beam size, bearing and gravity loads are not designed here.");
            return p;
        }

        // ------------------------------------------------------------------ column

        public static MemberView Column(string id, double baseElevation, double topElevation, IList<(string name, double elevation)> levels,
                                        List<HoldownLanding> onIt, HoldownLanding bottom, string span, List<CalcLine> lines, string footnote)
        {
            double H = Math.Max(0, topElevation - baseElevation) * 12;
            return new MemberView
            {
                Key = "column " + id,
                WindowTitle = $"Column {id}: elevation, 3D and pieces",
                Title = $"Column {id}",
                Subtitle = $"{span[0].ToString().ToUpperInvariant()}{span.Substring(1)}, {FtIn.Of(H)}. " +
                           (onIt.Count == 0 ? "Nothing lands on its top. " : $"{onIt.Count} {(onIt.Count == 1 ? "connection lands" : "connections land")} on its top. ") +
                           (bottom == null ? "No uplift at its base. " : $"Its base holds {bottom.TensionAsd:N0} lb up (ASD). ") + "Not sized here: drawn 3 1/2 × 3 1/2 in.",
                Elevation = () => new ColumnElevation(id, baseElevation, topElevation, levels, onIt, bottom),
                Model = () => ColumnModel(H, onIt, bottom),
                Pieces = () => ColumnPieces(id, H, onIt, bottom),
                Lines = lines ?? new List<CalcLine>(),
                Footnote = footnote,
            };
        }

        private static Model3DGroup ColumnModel(double H, List<HoldownLanding> onIt, HoldownLanding bottom)
        {
            var g = new Model3DGroup();
            if (H <= 0) return g;
            g.Children.Add(Framing3DView.BoxModel(0, 0, 0, PostSize, H, PostSize, Framing3DView.PostMaterial));
            if (bottom != null)
            {
                g.Children.Add(Framing3DView.BoxModel(PostSize + 0.02, 1.5, 0.3, PostSize + 2.9, 13.5, PostSize - 0.3, Framing3DView.SteelMaterial));
                g.Children.Add(Framing3DView.BoxModel(PostSize + 1.15, -10, PostSize / 2 - 0.3, PostSize + 1.75, 4, PostSize / 2 + 0.3, Framing3DView.SteelMaterial));
            }
            if (bottom == null || bottom.Support == SupportKind.Foundation) g.Children.Add(Framing3DView.BoxModel(-12, -16, -12, PostSize + 12, -0.05, PostSize + 12, Framing3DView.ConcreteMaterial));
            else g.Children.Add(Framing3DView.BoxModel(-8, -12, -2, PostSize + 8, -0.05, PostSize + 2, Framing3DView.StudMaterial));
            int k = 0;
            foreach (var l in onIt)
            {
                double z = 0.4 + 1.1 * (k++ % 3);
                g.Children.Add(Framing3DView.BoxModel(PostSize + 0.02, H - 14, z, PostSize + 0.12, H + 18, z + 0.9, Framing3DView.StrapMaterial));
            }
            return g;
        }

        private static FrameworkElement ColumnPieces(string id, double H, List<HoldownLanding> onIt, HoldownLanding bottom)
        {
            var p = new StackPanel { Margin = new Thickness(4, 4, 16, 16) };
            WallBuildWindow.Section(p, "Column", "");
            p.Children.Add(WallBuildWindow.Table(new[] { "Piece", "Size", "Qty", "Length", "Notes" }, new List<string[]>
            {
                new[] { $"Column {id}", "not sized (drawn 3 1/2 × 3 1/2)", "1", FtIn.Of(H), "Size it for its gravity loads; it carries the uplift below down to its base." },
            }));
            WallBuildWindow.Section(p, "Connections at its top", onIt.Count == 0 ? "None." : "What lands on it from above, each with its own connection.");
            if (onIt.Count > 0)
                p.Children.Add(WallBuildWindow.Table(new[] { "From", "Uplift (lb, ASD)", "Connection", "Fasteners", "Allowable (lb)" },
                    onIt.Select(l => new[] { ShortFrom(l), $"{l.TensionAsd:N0}", l.Model ?? "none strong enough", WallBuildWindow.Fast(l.Choice?.Fasteners), l.Choice?.Allowable > 0 ? $"{l.Choice.Allowable:N0}" : "" }).ToList()));
            WallBuildWindow.Section(p, "Base connection", bottom == null ? "No uplift at its base." : $"Its base holds {bottom.TensionAsd:N0} lb up.");
            if (bottom != null)
                p.Children.Add(WallBuildWindow.Table(new[] { "Connection", "Fasteners", "Allowable (lb)", "To", "Notes" }, new List<string[]>
                {
                    new[] { bottom.Supported ? bottom.Model ?? "none strong enough" : "nothing under it", WallBuildWindow.Fast(bottom.Choice?.Fasteners), bottom.Choice?.Allowable > 0 ? $"{bottom.Choice.Allowable:N0}" : "",
                            DetailCalcs.Support(bottom), bottom.Support == SupportKind.Foundation ? "Anchor into the foundation (not designed here)." : bottom.Note ?? "" },
                }));
            Assumed(p, "The column carries uplift straight down from its top to its base.", "Column size, its gravity loads and the foundation are not designed here.");
            return p;
        }

        internal static string ShortFrom(HoldownLanding l)
        {
            string s = ShearWallCalculator.Drawing.PlanLabels.ShortFrom(l.From);
            return l.Kind == PathPointKind.WallHoldown ? $"wall {l.WallId} (story {l.UpperStory})" : s;
        }

        private static void Assumed(Panel p, params string[] lines)
        {
            WallBuildWindow.Section(p, "Assumed (confirm)", "");
            foreach (var s in lines) p.Children.Add(new TextBlock { Text = "•  " + s, TextWrapping = TextWrapping.Wrap, Foreground = Ui.InkSecondary, Margin = new Thickness(6, 1, 0, 1) });
        }

        // ------------------------------------------------------------------ drawings

        /// <summary>A beam in elevation to scale (start on the left): what it bears on at each end, each end's connection with its
        /// uplift, the hold-downs from above at their distances, and the dimensions.</summary>
        private sealed class BeamElevation : FrameworkElement
        {
            private readonly string id;
            private readonly BeamReactions br;
            private readonly BeamEndInfo start, end;

            public BeamElevation(string id, BeamReactions br, BeamEndInfo start, BeamEndInfo end)
            {
                this.id = id;
                this.br = br;
                this.start = start;
                this.end = end;
            }

            protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo) { base.OnRenderSizeChanged(sizeInfo); InvalidateVisual(); }

            protected override void OnRender(DrawingContext dc)
            {
                double W = ActualWidth, Hh = ActualHeight;
                dc.DrawRectangle(DetailInk.Paper, null, new Rect(0, 0, W, Hh));
                double L = (br?.Beam?.Length ?? 0) * 12;
                if (L <= 0 || W < 300) return;
                double left = 130, right = 130;
                double s = (W - left - right) / L;
                double dpx = Math.Max(20, BeamDepth * s);
                double yt = Math.Max(170, Hh * 0.38), yb = yt + dpx;
                double X(double x) => left + x * s;

                dc.DrawRectangle(DetailInk.Post, DetailInk.P(DetailInk.Timber, 1.2), new Rect(X(0), yt, L * s, dpx));
                DetailInk.Label(dc, this, $"Beam {id}  (not sized: drawn 3 1/2 × 11 1/4)", new Point(X(L / 2), yt + dpx / 2), DetailInk.Timber, 11, bold: true, centre: true);

                foreach (var (e, x, inward) in new[] { (start, 0.0, 1.0), (end, L, -1.0) })
                {
                    double px = X(x);
                    // what it bears on
                    if (e.OnColumn) dc.DrawRectangle(DetailInk.Post, DetailInk.P(DetailInk.Timber, 1), new Rect(px - Math.Max(10, PostSize * s) / 2, yb, Math.Max(10, PostSize * s), 80));
                    else if (e.Under != null && !e.Under.StartsWith("nothing")) DetailInk.Hatch(dc, new Rect(px - 22, yb, 44, 14), DetailInk.Joint);
                    DetailInk.Label(dc, this, e.Under ?? "?", new Point(px + inward * 14, yb + 58), DetailInk.Dim, 10, rightAligned: inward < 0);
                    // the connection: a strap down the beam's face into what is under it
                    if (e.Landing != null) dc.DrawRectangle(DetailInk.Hardware, null, new Rect(px + inward * 6 - 3, yt + 3, 6, dpx + 34));
                    bool ra = inward < 0;
                    double ty = yb + 96;
                    DetailInk.Label(dc, this, $"({e.Reaction / 1000:0.0}k)", inward > 0 ? new Point(X(0), ty) : new Point(X(L), ty), e.Ink, 12, bold: true, rightAligned: ra);
                    DetailInk.Label(dc, this, e.Connection, inward > 0 ? new Point(X(0), ty + 17) : new Point(X(L), ty + 17), e.Ink, 12, bold: true, rightAligned: ra);
                    string fast = WallBuildWindow.Fast(e.Landing?.Choice?.Fasteners);
                    if (!string.IsNullOrEmpty(fast)) DetailInk.Label(dc, this, "fasteners " + fast, inward > 0 ? new Point(X(0), ty + 34) : new Point(X(L), ty + 34), e.Ink, 10.5, rightAligned: ra);
                    if (e.Landing != null && e.Landing.Supported) DetailInk.Label(dc, this, $"→ {DetailCalcs.Support(e.Landing)}", inward > 0 ? new Point(X(0), ty + 49) : new Point(X(L), ty + 49), e.Ink, 10.5, rightAligned: ra);
                    DetailInk.Label(dc, this, e.Which == "start" ? "Start" : "End", new Point(px, yt - 16), DetailInk.Dim, 10, bold: true, centre: true);
                }

                // the hold-downs from above, at a
                int k = 0;
                double yd = yb + 170;
                foreach (var ld in (br?.Loads ?? new List<BeamLoad>()).OrderBy(x => x.A))
                {
                    double x = X(ld.A * 12);
                    DetailInk.Arrow(dc, DetailInk.P(AboveInk, 1.8), new Point(x, yt), new Point(x, yt - 44));
                    double ly = yt - 64 - 30 * (k % 3);
                    DetailInk.Label(dc, this, $"↑ {ld.Asd / 1000:0.0}k", new Point(x, ly), AboveInk, 11, bold: true, centre: true);
                    DetailInk.Label(dc, this, ld.From ?? "", new Point(x, ly + 15), AboveInk, 10, centre: true);
                    DetailInk.Dimension(dc, this, new Point(X(0), yd + 16 * (k + 1)), new Point(x, yd + 16 * (k + 1)), $"a = {FtIn.Of(ld.A * 12)}");
                    k++;
                }
                DetailInk.Dimension(dc, this, new Point(X(0), yd), new Point(X(L), yd), FtIn.Of(L));
            }
        }

        /// <summary>A column in elevation to scale: the levels it passes, what lands on its top, its base connection.</summary>
        private sealed class ColumnElevation : FrameworkElement
        {
            private readonly string id;
            private readonly double baseE, topE;
            private readonly IList<(string name, double elevation)> levels;
            private readonly List<HoldownLanding> onIt;
            private readonly HoldownLanding bottom;

            public ColumnElevation(string id, double baseE, double topE, IList<(string, double)> levels, List<HoldownLanding> onIt, HoldownLanding bottom)
            {
                this.id = id;
                this.baseE = baseE;
                this.topE = topE;
                this.levels = levels ?? new List<(string, double)>();
                this.onIt = onIt ?? new List<HoldownLanding>();
                this.bottom = bottom;
            }

            protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo) { base.OnRenderSizeChanged(sizeInfo); InvalidateVisual(); }

            protected override void OnRender(DrawingContext dc)
            {
                double W = ActualWidth, Hh = ActualHeight;
                dc.DrawRectangle(DetailInk.Paper, null, new Rect(0, 0, W, Hh));
                double H = (topE - baseE) * 12;
                if (H <= 0 || Hh < 300) return;
                double top = 150, bot = 150;
                double s = (Hh - top - bot) / H;
                double cx = W * 0.42, cw = Math.Max(16, PostSize * s);
                double Y(double inches) => Hh - bot - inches * s;

                foreach (var (name, e) in levels.Where(l => l.elevation >= baseE - 1e-9 && l.elevation <= topE + 1e-9))
                {
                    double y = Y((e - baseE) * 12);
                    dc.DrawLine(DetailInk.P(DetailInk.Dim, 0.8, 4, 3), new Point(40, y), new Point(W - 40, y));
                    DetailInk.Label(dc, this, $"{name}  {e:0.##} ft", new Point(44, y - 16), DetailInk.Dim, 10);
                }
                dc.DrawRectangle(DetailInk.Post, DetailInk.P(DetailInk.Timber, 1.2), new Rect(cx - cw / 2, Y(H), cw, H * s));
                DetailInk.Label(dc, this, $"Column {id}", new Point(cx + cw / 2 + 10, Y(H * 0.55)), DetailInk.Timber, 12, bold: true);
                DetailInk.Label(dc, this, "not sized: drawn 3 1/2 × 3 1/2", new Point(cx + cw / 2 + 10, Y(H * 0.55) + 17), DetailInk.Dim, 10);
                DetailInk.Dimension(dc, this, new Point(cx - cw / 2 - 60, Y(H)), new Point(cx - cw / 2 - 60, Y(0)), FtIn.Of(H), vertical: true);

                // what lands on its top
                int k = 0;
                foreach (var l in onIt)
                {
                    double x = cx - cw / 2 + (k + 0.5) * cw / Math.Max(1, onIt.Count);
                    DetailInk.Arrow(dc, DetailInk.P(AboveInk, 1.8), new Point(x, Y(H)), new Point(x, Y(H) - 40));
                    var ink = !l.Supported || l.Model == null ? Ui.FailInk : AboveInk;
                    DetailInk.Label(dc, this, $"↑ {l.TensionAsd / 1000:0.0}k  {l.Model ?? "none strong enough"}  from {ShortFrom(l)}", new Point(cx + cw / 2 + 14, Y(H) - 60 - 18 * k), ink, 10.5, bold: true);
                    k++;
                }

                // its base
                double yb = Y(0);
                if (bottom == null || bottom.Support == SupportKind.Foundation) DetailInk.Hatch(dc, new Rect(cx - 70, yb, 140, 20), DetailInk.Concrete);
                else if (bottom.Supported) DetailInk.Hatch(dc, new Rect(cx - 50, yb, 100, 12), DetailInk.Joint);
                if (bottom != null)
                {
                    double hx = cx + cw / 2;
                    dc.DrawRectangle(DetailInk.Hardware, null, new Rect(hx, yb - Math.Max(18, 12 * s), Math.Max(6, 2.9 * s), Math.Max(18, 12 * s)));
                    dc.DrawLine(DetailInk.P(DetailInk.Hardware, 2.4), new Point(hx + Math.Max(3, 1.45 * s), yb), new Point(hx + Math.Max(3, 1.45 * s), yb + 18));
                    var ink = !bottom.Supported || bottom.Model == null ? Ui.FailInk : Ui.Ink;
                    DetailInk.Label(dc, this, $"Base: ↑ {bottom.TensionAsd / 1000:0.0}k", new Point(cx + 70, yb + 26), ink, 12, bold: true);
                    DetailInk.Label(dc, this, bottom.Supported ? $"{bottom.Model ?? "none strong enough"} → {DetailCalcs.Support(bottom)}" : "nothing under it", new Point(cx + 70, yb + 43), ink, 11);
                    if (!string.IsNullOrEmpty(bottom.Choice?.Fasteners)) DetailInk.Label(dc, this, "fasteners " + WallBuildWindow.Fast(bottom.Choice.Fasteners), new Point(cx + 70, yb + 59), ink, 10.5);
                }
                else DetailInk.Label(dc, this, "Base: no uplift", new Point(cx + 70, yb + 26), DetailInk.Dim, 11);
            }
        }
    }
}
