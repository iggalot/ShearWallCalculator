using calculator;
using ShearWallCalculator;
using ShearWallCalculator.Design;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ShearWallVisualizer.Controls
{
    /// <summary>Shared ink and text for the detail drawings (the same on screen and in the report).</summary>
    internal static class DetailInk
    {
        public static readonly Brush Paper = Brushes.White;
        public static readonly Brush Sheathing = Ui.Frozen("#EEF3FA"), Joint = Ui.Frozen("#C9D6E8"), Post = Ui.Frozen("#D9B98C"),
                                     Hardware = Ui.Frozen("#1F2933"), Timber = Ui.Frozen("#8B4A14"), Concrete = Ui.Frozen("#D5D8DC"),
                                     DirA = Ui.Frozen("#1D4ED8"), DirB = Ui.Frozen("#C2410C"), Dim = Ui.Frozen("#52514E");
        public static Pen P(Brush b, double t, params double[] dash)
        {
            var p = new Pen(b, t);
            if (dash.Length > 0) p.DashStyle = new DashStyle(dash, 0);
            p.Freeze();
            return p;
        }

        public static FormattedText Text(Visual v, string s, double size, Brush ink, bool bold = false) =>
            new FormattedText(s ?? "", CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, bold ? FontWeights.SemiBold : FontWeights.Normal, FontStretches.Normal),
                size, ink, VisualTreeHelper.GetDpi(v).PixelsPerDip);

        /// <summary>Text with its top-left (or its centre when <paramref name="centre"/>) at a point, on a white back.</summary>
        public static void Label(DrawingContext dc, Visual v, string s, Point at, Brush ink, double size = 11, bool bold = false, bool centre = false, bool rightAligned = false)
        {
            var t = Text(v, s, size, ink, bold);
            var p = centre ? new Point(at.X - t.Width / 2, at.Y - t.Height / 2) : rightAligned ? new Point(at.X - t.Width, at.Y) : at;
            dc.DrawRectangle(Ui.Frozen("#E6FFFFFF"), null, new Rect(p.X - 1, p.Y, t.Width + 2, t.Height));
            dc.DrawText(t, p);
        }

        public static void Arrow(DrawingContext dc, Pen pen, Point from, Point to)
        {
            dc.DrawLine(pen, from, to);
            var d = to - from;
            if (d.Length < 1) return;
            d.Normalize();
            var n = new Vector(-d.Y, d.X);
            var g = new StreamGeometry();
            using (var c = g.Open())
            {
                c.BeginFigure(to, true, true);
                c.PolyLineTo(new[] { to - d * 8 + n * 4, to - d * 8 - n * 4 }, true, false);
            }
            g.Freeze();
            dc.DrawGeometry(pen.Brush, null, g);
        }

        /// <summary>A dimension line with end ticks and its text in the middle.</summary>
        public static void Dimension(DrawingContext dc, Visual v, Point a, Point b, string text, bool vertical = false)
        {
            var pen = P(Dim, 1);
            dc.DrawLine(pen, a, b);
            var t = vertical ? new Vector(4, 0) : new Vector(0, 4);
            dc.DrawLine(pen, a - t, a + t);
            dc.DrawLine(pen, b - t, b + t);
            Label(dc, v, text, new Point((a.X + b.X) / 2, (a.Y + b.Y) / 2), Dim, 10.5, centre: true);
        }

        public static void Hatch(DrawingContext dc, Rect r, Brush fill)
        {
            dc.DrawRectangle(fill, P(Dim, 0.8), r);
            dc.PushClip(new RectangleGeometry(r));
            var pen = P(Dim, 0.6);
            for (double x = r.Left - r.Height; x < r.Right; x += 7) dc.DrawLine(pen, new Point(x, r.Bottom), new Point(x + r.Height, r.Top));
            dc.Pop();
        }

        public static Brush ForDirection(bool? lowEndUp) => lowEndUp == false ? DirB : DirA;
    }

    /// <summary>
    /// A wall in elevation, to scale: its sheathing and end posts, the hold-down at each post with its model, tension and what
    /// it anchors to, the forces from the walls, beams and columns above at their positions, the shear, and the length, height
    /// and lever arm. The low end (west or south) is on the left.
    /// </summary>
    public sealed class WallElevation : FrameworkElement
    {
        private readonly WallData w;
        private readonly WallDesignResult r;
        private readonly List<HoldownLanding> own;

        public WallElevation(WallData wall, WallDesignResult result, IEnumerable<HoldownLanding> own, double width = 680, double height = 400)
        {
            w = wall;
            r = result;
            this.own = own?.ToList() ?? new List<HoldownLanding>();
            Width = width;
            Height = height;
        }

        protected override void OnRender(DrawingContext dc)
        {
            double W = Width, H = Height;
            dc.DrawRectangle(DetailInk.Paper, null, new Rect(0, 0, W, H));
            if (w == null || r == null || !(r.Length > 0) || !(r.Height > 0)) return;
            double L = r.Length, h = r.Height;
            bool ew = w.WallDir == WallDirs.EastWest;
            double left = 70, right = 40, top = 96, bottom = 132;
            double s = Math.Min((W - left - right) / L, (H - top - bottom) / h);
            double x0 = left + ((W - left - right) - L * s) / 2, yt = top, yb = top + h * s, xr = x0 + L * s;
            double X(double along) => x0 + Math.Max(0, Math.Min(L, along)) * s;

            // sheathing with its 4 ft joints, posts, plates
            dc.DrawRectangle(DetailInk.Sheathing, DetailInk.P(Ui.Ink, 1.5), new Rect(x0, yt, L * s, h * s));
            for (double j = 4; j < L - 0.01; j += 4) dc.DrawLine(DetailInk.P(DetailInk.Joint, 1), new Point(X(j), yt), new Point(X(j), yb));
            double pw = Math.Max(3, r.PostAlongWall / 12.0 * s);
            dc.DrawRectangle(DetailInk.Post, DetailInk.P(Ui.Ink, 1), new Rect(x0, yt, pw, h * s));
            dc.DrawRectangle(DetailInk.Post, DetailInk.P(Ui.Ink, 1), new Rect(xr - pw, yt, pw, h * s));

            // what it stands on: one band when both ends anchor to the same thing
            var lowEnd = own.FirstOrDefault(l => DetailCalcs.EndName(w, l.At) == (ew ? "West" : "South"));
            var highEnd = own.FirstOrDefault(l => l != lowEnd);
            string sup = lowEnd != null && highEnd != null && DetailCalcs.Support(lowEnd) == DetailCalcs.Support(highEnd) ? DetailCalcs.Support(lowEnd) : null;
            if (sup == "the foundation") DetailInk.Hatch(dc, new Rect(x0 - 20, yb, L * s + 40, 14), DetailInk.Concrete);
            else if (sup != null && sup != "nothing under it") dc.DrawRectangle(DetailInk.Joint, DetailInk.P(DetailInk.Dim, 0.8), new Rect(x0 - 10, yb, L * s + 20, 8));
            if (sup != null) DetailInk.Label(dc, this, $"on {sup}", new Point(x0 + L * s / 2, yb + 22), sup == "nothing under it" ? Ui.FailInk : DetailInk.Dim, 10.5, centre: true);

            // the hold-downs at the posts
            void Holdown(bool atLeft, HoldownLanding l)
            {
                double hx = atLeft ? x0 + pw : xr - pw - 9;
                dc.DrawRectangle(DetailInk.Hardware, null, new Rect(hx, yb - 26, 9, 26));
                var ink = l == null ? DetailInk.Dim : !l.Supported || l.Model == null ? Ui.FailInk : l.Note != null ? Ui.CloseInk : Ui.Ink;
                string model = l?.Model ?? r.Holdown?.Model ?? (r.HoldownNeeded ? "none strong enough" : "no uplift");
                string to = l == null ? "" : !l.Supported ? "→ nothing under it" : $"→ {DetailCalcs.FamilyName(l.Family)} to {DetailCalcs.Support(l)}";
                // the low end's text runs right from under its post, the high end's runs left to under its post
                double tx = atLeft ? x0 : xr;
                bool ra = !atLeft;
                DetailInk.Label(dc, this, model, new Point(tx, yb + 36), ink, 11, bold: true, rightAligned: ra);
                DetailInk.Label(dc, this, $"T = {r.TensionAsd:N0} lb", new Point(tx, yb + 51), ink, 10.5, rightAligned: ra);
                if (to != "") DetailInk.Label(dc, this, to, new Point(atLeft ? x0 - 30 : xr + 30, yb + 65 + (atLeft ? 0 : 14)), ink, 10.5, rightAligned: ra);
            }
            if (r.HoldownNeeded)
            {
                Holdown(true, lowEnd);
                Holdown(false, highEnd);
            }

            // the shear along the top plate, either way (inside the wall, clear of the forces from above)
            double ya = yt + 16;
            DetailInk.Arrow(dc, DetailInk.P(Ui.Ink, 1.6), new Point(X(L * 0.5), ya), new Point(X(L * 0.9), ya));
            DetailInk.Arrow(dc, DetailInk.P(Ui.Ink, 1.6), new Point(X(L * 0.5), ya), new Point(X(L * 0.1), ya));
            DetailInk.Label(dc, this, $"V = {r.V:0.00} k  (v = {r.Demand:0} plf)", new Point(X(L * 0.5), ya + 14), Ui.Ink, 11, centre: true);

            // the forces from above, at their positions (blue: wind lifting the low ends; orange: the high ends)
            int k = 0;
            foreach (var f in (r.FromAbove ?? new List<StackedForce>()).OrderBy(x => x.Along))
            {
                double x = X(f.Along);
                var pen = DetailInk.P(DetailInk.ForDirection(f.LowEndUp), 1.6);
                if (f.Asd >= 0) DetailInk.Arrow(dc, pen, new Point(x, yt), new Point(x, yt - 34));
                else DetailInk.Arrow(dc, pen, new Point(x, yt - 34), new Point(x, yt));
                DetailInk.Label(dc, this, $"{Math.Abs(f.Asd) / 1000:0.0}k {(f.Asd >= 0 ? "↑" : "↓")}", new Point(x + 3, yt - 48 - 12 * (k++ % 3)), DetailInk.ForDirection(f.LowEndUp), 10);
            }
            if (r.FromAbove != null && r.FromAbove.Count > 0)
            {
                string a = ew ? "west" : "south", b = ew ? "east" : "north";
                DetailInk.Label(dc, this, $"From above (ASD): blue = wind lifting the {a} ends, orange = the {b} ends", new Point(8, 6), DetailInk.Dim, 10);
            }

            // dimensions: the lever arm between the hold-downs, the length, the height
            if (r.HoldownNeeded) DetailInk.Dimension(dc, this, new Point(x0 + pw / 2, yb + 100), new Point(xr - pw / 2, yb + 100), $"d = {r.HoldownArm:0.##} ft");
            DetailInk.Dimension(dc, this, new Point(x0, yb + 116), new Point(xr, yb + 116), $"L = {L:0.##} ft");
            DetailInk.Dimension(dc, this, new Point(x0 - 40, yt), new Point(x0 - 40, yb), $"h = {h:0.##} ft", vertical: true);
            DetailInk.Label(dc, this, ew ? "W" : "S", new Point(x0 - 14, yt + 4), DetailInk.Dim, 10, bold: true);
            DetailInk.Label(dc, this, ew ? "E" : "N", new Point(xr + 6, yt + 4), DetailInk.Dim, 10, bold: true);
        }
    }

    /// <summary>
    /// A beam carrying hold-downs: to scale along its span, the uplift of each hold-down on it at its distance a, the reaction
    /// at each end (held down by the connection to what is under it) and what each end bears on.
    /// </summary>
    public sealed class BeamDiagram : FrameworkElement
    {
        private readonly BeamReactions br;
        private readonly List<HoldownLanding> ends;

        public BeamDiagram(BeamReactions reactions, IEnumerable<HoldownLanding> ends, double width = 680, double height = 280)
        {
            br = reactions;
            this.ends = ends?.ToList() ?? new List<HoldownLanding>();
            Width = width;
            Height = height;
        }

        protected override void OnRender(DrawingContext dc)
        {
            double W = Width, H = Height;
            dc.DrawRectangle(DetailInk.Paper, null, new Rect(0, 0, W, H));
            if (br?.Beam == null || !(br.Beam.Length > 0)) return;
            double L = br.Beam.Length, xs = 90, xe = W - 90, y = 150;
            double X(double a) => xs + Math.Max(0, Math.Min(L, a)) / L * (xe - xs);
            dc.DrawRectangle(DetailInk.Timber, null, new Rect(xs, y - 7, xe - xs, 14));
            DetailInk.Label(dc, this, br.Beam.Id, new Point((xs + xe) / 2, y + 26), DetailInk.Timber, 11, bold: true, centre: true);

            // the loads: uplift at a
            int k = 0;
            foreach (var ld in br.Loads.OrderBy(x => x.A))
            {
                double x = X(ld.A);
                var ink = DetailInk.ForDirection(ld.LowEndUp);
                DetailInk.Arrow(dc, DetailInk.P(ink, 1.8), new Point(x, y - 8), new Point(x, y - 56));
                DetailInk.Label(dc, this, $"{ld.From} {ld.Asd:N0} lb", new Point(x + 4, y - 72 - 14 * (k % 3)), ink, 10.5);
                DetailInk.Label(dc, this, $"a = {ld.A:0.##}", new Point(x, y + 44 + 13 * (k % 2)), DetailInk.Dim, 10, centre: true);
                k++;
            }

            // each end: the reaction holding it down, and what it bears on
            foreach (var (x, R, which) in new[] { (xs, br.StartAsd, "start"), (xe, br.EndAsd, "end") })
            {
                var l = ends.FirstOrDefault(e => e.From.EndsWith(which));
                DetailInk.Arrow(dc, DetailInk.P(Ui.Ink, 1.8), new Point(x, y - 60), new Point(x, y - 9));
                DetailInk.Label(dc, this, $"R = {R:N0} lb", new Point(x, y - 70), Ui.Ink, 11, bold: true, centre: true);
                var under = new Rect(x - 14, y + 8, 28, 26);
                if (l == null || !l.Supported) DetailInk.Label(dc, this, "✕ nothing under it", new Point(x, y + 20), Ui.FailInk, 10.5, true, centre: true);
                else if (l.Support == SupportKind.Column) dc.DrawRectangle(DetailInk.Timber, null, new Rect(x - 5, y + 8, 10, 40));
                else DetailInk.Hatch(dc, under, DetailInk.Joint);
                if (l != null && l.Supported)
                    DetailInk.Label(dc, this, $"{DetailCalcs.Support(l)}: {DetailCalcs.FamilyName(l.Family)} {l.Model ?? "none strong enough"}",
                                    new Point(x, y + 82), l.Model == null ? Ui.FailInk : Ui.Ink, 10.5, centre: true);
            }
            DetailInk.Dimension(dc, this, new Point(xs, H - 24), new Point(xe, H - 24), $"L = {L:0.##} ft");
            DetailInk.Label(dc, this, "Uplift (ASD): blue = wind lifting the low ends of the walls, orange = the high ends", new Point(8, 6), DetailInk.Dim, 10);
        }
    }

    /// <summary>A column in elevation: the levels it passes, what lands on its top, what goes down it, and its base connection.</summary>
    public sealed class ColumnDiagram : FrameworkElement
    {
        private readonly List<(string name, double elevation)> levels;
        private readonly double baseElevation, topElevation;
        private readonly List<HoldownLanding> onIt;
        private readonly HoldownLanding bottom;
        private readonly string id;

        public ColumnDiagram(string id, double baseElevation, double topElevation, IEnumerable<(string, double)> levels, IEnumerable<HoldownLanding> onIt,
                             HoldownLanding bottom, double width = 420, double height = 380)
        {
            this.id = id;
            this.baseElevation = baseElevation;
            this.topElevation = topElevation;
            this.levels = levels?.ToList() ?? new List<(string, double)>();
            this.onIt = onIt?.ToList() ?? new List<HoldownLanding>();
            this.bottom = bottom;
            Width = width;
            Height = height;
        }

        protected override void OnRender(DrawingContext dc)
        {
            double W = Width, H = Height;
            dc.DrawRectangle(DetailInk.Paper, null, new Rect(0, 0, W, H));
            double span = topElevation - baseElevation;
            if (!(span > 0)) return;
            double yTop = 80, yBase = H - 70, x = W / 2 - 40;
            double Y(double e) => yBase - (e - baseElevation) / span * (yBase - yTop);
            foreach (var (name, e) in levels.Where(l => l.elevation >= baseElevation - 1e-9 && l.elevation <= topElevation + 1e-9))
            {
                dc.DrawLine(DetailInk.P(DetailInk.Dim, 0.8, 4, 3), new Point(20, Y(e)), new Point(W - 20, Y(e)));
                DetailInk.Label(dc, this, $"{name}  {e:0.##} ft", new Point(22, Y(e) - 15), DetailInk.Dim, 10);
            }
            dc.DrawRectangle(DetailInk.Timber, null, new Rect(x - 6, Y(topElevation), 12, Y(baseElevation) - Y(topElevation)));
            DetailInk.Label(dc, this, id, new Point(x + 12, (Y(topElevation) + Y(baseElevation)) / 2), DetailInk.Timber, 11, bold: true);

            int k = 0;
            foreach (var l in onIt)
            {
                var ink = DetailInk.ForDirection(l.LowEndUp);
                DetailInk.Arrow(dc, DetailInk.P(ink, 1.8), new Point(x - 10 + 8 * k, Y(topElevation)), new Point(x - 10 + 8 * k, Y(topElevation) - 40));
                DetailInk.Label(dc, this, $"{l.From} {l.TensionAsd:N0} lb ({DetailCalcs.FamilyName(l.Family)} {l.Model ?? ""})", new Point(x + 10, Y(topElevation) - 56 - 14 * k), ink, 10.5);
                k++;
            }
            if (bottom != null)
            {
                if (bottom.Support == SupportKind.Foundation) DetailInk.Hatch(dc, new Rect(x - 40, Y(baseElevation), 80, 16), DetailInk.Concrete);
                else if (bottom.Supported) DetailInk.Hatch(dc, new Rect(x - 30, Y(baseElevation), 60, 10), DetailInk.Joint);
                dc.DrawRectangle(DetailInk.Hardware, null, new Rect(x + 6, Y(baseElevation) - 22, 8, 22));
                var ink = !bottom.Supported || bottom.Model == null ? Ui.FailInk : Ui.Ink;
                DetailInk.Label(dc, this, $"Base: {bottom.TensionAsd:N0} lb up", new Point(x + 22, Y(baseElevation) - 30), ink, 11, bold: true);
                DetailInk.Label(dc, this, bottom.Supported ? $"{DetailCalcs.FamilyName(bottom.Family)} {bottom.Model ?? "none strong enough"} to {DetailCalcs.Support(bottom)}" : "nothing under it",
                                new Point(x + 22, Y(baseElevation) + 20), ink, 10.5);
            }
        }
    }

    /// <summary>What a detail shows: a title, the drawing and the calculation lines.</summary>
    public sealed class DetailContent
    {
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public Func<FrameworkElement> Drawing { get; set; }
        public List<CalcLine> Lines { get; set; } = new List<CalcLine>();
        public string Footnote { get; set; }
    }

    /// <summary>The calculation lines as a three-column table (failing lines in the fail colour, flagged ones in yellow ink).</summary>
    public static class CalcTable
    {
        public static FrameworkElement Build(IList<CalcLine> lines)
        {
            var g = new Grid { Margin = new Thickness(0, 6, 0, 0) };
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            for (int i = 0; i < lines.Count; i++)
            {
                var l = lines[i];
                g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                var ink = l.Fails ? Ui.FailInk : l.Flag ? Ui.CloseInk : Ui.Ink;
                if (i % 2 == 1)
                {
                    var band = new Border { Background = Ui.Frozen("#F6F7F9") };
                    Grid.SetRow(band, i);
                    Grid.SetColumnSpan(band, 3);
                    g.Children.Add(band);
                }
                void Cell(string text, int col, bool bold = false, Brush b = null)
                {
                    var t = new TextBlock { Text = text, Foreground = b ?? ink, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(4, 2, 8, 2), FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal };
                    Grid.SetRow(t, i);
                    Grid.SetColumn(t, col);
                    g.Children.Add(t);
                }
                Cell(l.Item, 0, false, l.Fails || l.Flag ? ink : Ui.InkSecondary);
                Cell(l.Calculation, 1);
                Cell(l.Result, 2, true);
            }
            return g;
        }
    }

    /// <summary>
    /// The detail of the selected wall, beam or column: its drawing and its calculation. It stays open and follows the
    /// design as it changes (and the selection, when another wall, beam or column is picked).
    /// </summary>
    public sealed class MemberDetailWindow : Window
    {
        private readonly StackPanel body = new StackPanel { Margin = new Thickness(16, 12, 16, 16) };

        public MemberDetailWindow()
        {
            Title = "Detail";
            Width = 760;
            Height = 820;
            FontFamily = new FontFamily("Segoe UI");
            FontSize = 12;
            Background = Brushes.White;
            ShowInTaskbar = false;
            Content = new ScrollViewer { Content = body, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        }

        public void Show(DetailContent c)
        {
            body.Children.Clear();
            if (c == null)
            {
                Title = "Detail";
                body.Children.Add(new TextBlock { Text = "Select a wall, beam or column on the plan.", Foreground = Ui.InkMuted });
                return;
            }
            Title = "Detail: " + c.Title;
            body.Children.Add(new TextBlock { Text = c.Title, FontSize = 18, FontWeight = FontWeights.SemiBold });
            if (!string.IsNullOrEmpty(c.Subtitle)) body.Children.Add(new TextBlock { Text = c.Subtitle, Foreground = Ui.InkSecondary, Margin = new Thickness(0, 2, 0, 8), TextWrapping = TextWrapping.Wrap });
            var drawing = c.Drawing?.Invoke();
            if (drawing != null) body.Children.Add(new Border { Child = drawing, BorderBrush = Ui.Hairline, BorderThickness = new Thickness(1), HorizontalAlignment = HorizontalAlignment.Left });
            body.Children.Add(CalcTable.Build(c.Lines));
            if (!string.IsNullOrEmpty(c.Footnote)) body.Children.Add(new TextBlock { Text = c.Footnote, Foreground = Ui.InkMuted, FontSize = 11, Margin = new Thickness(0, 10, 0, 0), TextWrapping = TextWrapping.Wrap });
        }
    }
}
