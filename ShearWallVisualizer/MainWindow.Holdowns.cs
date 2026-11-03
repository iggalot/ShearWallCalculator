using BuildingModel.Checks;
using BuildingModel.Diagnostics;
using BuildingModel.Model;
using ShearWallCalculator;
using ShearWallCalculator.Design;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace ShearWallVisualizer
{
    /// <summary>
    /// Where the hold-downs go down the building:
    /// <list type="bullet">
    /// <item>on each level's plan, the hold-downs of the walls of the story ABOVE, where they land on this story's walls
    /// (a dark triangle with the uplift), or where there is no wall under them (a red triangle and an issue: a beam or post
    /// is needed);</item>
    /// <item>the FOUNDATION view (the Foundation button): the first story's walls over the ground and every hold-down
    /// anchor with its wall, model and uplift, for the foundation plan.</item>
    /// </list>
    /// </summary>
    public partial class MainWindow
    {
        /// <summary>The hold-downs of story i+1 landing on story i, by i (0 = the first story).</summary>
        private Dictionary<int, List<HoldownLanding>> landingsOnto = new Dictionary<int, List<HoldownLanding>>();

        /// <summary>The first story's hold-downs: the foundation anchors.</summary>
        private List<HoldownLanding> foundationAnchors = new List<HoldownLanding>();

        /// <summary>True while the plan shows the ground (the foundation view).</summary>
        private bool foundationView;

        /// <summary>After the design: where every hold-down lands (called at the end of UpdateDesign).</summary>
        private void ComputeLandings()
        {
            landingsOnto = new Dictionary<int, List<HoldownLanding>>();
            foundationAnchors = new List<HoldownLanding>();
            if (levels.Count == 0 || allDesignResults.Count != levels.Count || loadPath == null) return;
            foreach (var kv in loadPath.ByLevel)
                if (kv.Key < 0) foundationAnchors = kv.Value;
                else landingsOnto[kv.Key] = kv.Value;
            RefreshDetail(); // an open detail follows the design
            int open = landingsOnto.Values.Sum(l => l.Count(x => !x.Supported));
            if (open > 0) Log.Info("Hold-downs", $"{open} hold-down{(open == 1 ? "" : "s")} from an upper story land where there is no wall below.");
        }

        /// <summary>The issues: an upper hold-down with no wall under it, on the level where the support is missing.</summary>
        private void AddLandingIssues(List<ModelIssue> list)
        {
            var all = landingsOnto.Select(kv => (level: kv.Key, list: kv.Value)).ToList();
            all.Add((-1, foundationAnchors));
            foreach (var (k, landings) in all)
            {
                if (k >= stories.Count) continue;
                string levelId = k < 0 ? building?.Ground?.Id : stories[k].Level.Id;
                string levelName = k < 0 ? "the foundation" : stories[k].Level.Name;
                foreach (var l in landings)
                {
                    var from = l.UpperStory - 1 >= 0 && l.UpperStory - 1 < stories.Count ? stories[l.UpperStory - 1] : null;
                    string wallId = from != null && from.Walls.TryGetValue(l.WallId, out var id) ? id : $"wall {l.WallId}";
                    string what = l.Kind == PathPointKind.WallHoldown ? $"A hold-down of {wallId} ({from?.Level.Name})"
                                : l.Kind == PathPointKind.BeamEnd ? $"The {l.From} (carrying {wallId}'s hold-down)" : $"The {l.From} (carrying {wallId}'s hold-down)";
                    var at = new BuildingModel.Geometry.Point2(l.At.X, l.At.Y);
                    if (!l.Supported)
                        list.Add(new ModelIssue
                        {
                            Severity = Severity.Error, Code = "HoldownUnsupported", LevelId = levelId, At = at,
                            Message = $"{what}, {l.TensionAsd:N0} lb up, lands on {levelName} at ({l.At.X:0.##}, {l.At.Y:0.##}) with nothing under it: " +
                                      (l.Kind == PathPointKind.BeamEnd ? "the beam end needs a wall or a column under it." : "draw a beam (B) or a column (P) to carry it down."),
                        });
                    else if (l.Note != null)
                        list.Add(new ModelIssue { Severity = Severity.Warning, Code = "HoldownTypeReplaced", LevelId = levelId, At = at, Message = $"{what} on {l.SupportId ?? levelName}: {l.Note}" });
                    else if (l.Choice == null && l.Kind != PathPointKind.WallHoldown)
                        list.Add(new ModelIssue
                        {
                            Severity = Severity.Error, Code = "HoldownTooWeak", LevelId = levelId, At = at,
                            Message = $"{what}: no {HoldownRules.Name(l.Family).Substring(HoldownRules.Name(l.Family).IndexOf(' ') + 1)} is strong enough for {l.TensionAsd:N0} lb on {l.SupportId ?? levelName}.",
                        });
                }
            }
        }

        private static readonly Brush LandFill = Controls.Ui.Frozen("#1F2933"), LandText = Controls.Ui.InkSecondary, LabelBack = Controls.Ui.Frozen("#E6FFFFFF");
        private static readonly Brush BeamInk = Controls.Ui.Frozen("#8B4A14");
        private static readonly Pen LandEdge = FrozenPen(Colors.White, 1), MissingEdge = MakePen(Controls.Ui.FailInk, 2);

        private static Pen MakePen(Brush b, double t) { var p = new Pen(b, t); p.Freeze(); return p; }

        /// <summary>Over the plan: the hold-downs from the story above (a level view), or the foundation anchors (the foundation view).</summary>
        private void DrawLandings(DrawingContext ctx)
        {
            if (!showDesign) return;
            double dip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            if (foundationView)
            {
                foreach (var a in foundationAnchors)
                {
                    var p = WorldToScreen(a.At, m_layers);
                    var box = new Rect(p.X - 4.5, p.Y - 4.5, 9, 9);
                    ctx.DrawRectangle(a.Model != null ? LandFill : Brushes.White, a.Model != null ? LandEdge : MissingEdge, box);
                    string wallId = a.Kind != PathPointKind.WallHoldown ? a.From
                                  : stories.Count > 0 && stories[0].Walls.TryGetValue(a.WallId, out var id) ? id : $"W{a.WallId}";
                    Label(ctx, $"{wallId}  {a.Model ?? "no HD"}  {a.TensionAsd:N0} lb", new Point(p.X + 7, p.Y - 5), a.Model == null ? Controls.Ui.FailInk : a.Note != null ? Controls.Ui.CloseInk : LandText, dip, above: true);
                }
                return;
            }
            if (!landingsOnto.TryGetValue(currentLevel, out var onto)) return;
            // Everything at one point gets one label: the hold-downs landing there from above ("↓ 1.0k + 0.6k on column C2") and
            // the beam ends or column bases there with their connection ("BM2 end ↑ 0.4k HRS6"). The beam's own summary is on the
            // Members tab and its selection card, not on the plan.
            var atPoint = new Dictionary<(long, long), (Point p, List<string> downs, List<string> ups, Brush ink)>();
            Brush Worse(Brush a, Brush b) => ReferenceEquals(a, Controls.Ui.FailInk) || ReferenceEquals(b, Controls.Ui.FailInk) ? Controls.Ui.FailInk
                                           : ReferenceEquals(a, Controls.Ui.CloseInk) || ReferenceEquals(b, Controls.Ui.CloseInk) ? Controls.Ui.CloseInk : LandText;
            void Add(Point world, Point screen, string down, string up, Brush ink)
            {
                var k = ((long)Math.Round(world.X * 4), (long)Math.Round(world.Y * 4)); // within 3 in
                if (!atPoint.TryGetValue(k, out var g0)) atPoint[k] = g0 = (screen, new List<string>(), new List<string>(), ink);
                if (down != null) g0.downs.Add(down);
                if (up != null) g0.ups.Add(up);
                atPoint[k] = (g0.p, g0.downs, g0.ups, Worse(g0.ink, ink));
            }
            foreach (var l in onto)
            {
                if (l.Kind != PathPointKind.WallHoldown)
                {
                    // a beam end or a column base: a new hold-down point, sized for its reaction
                    var q = WorldToScreen(l.At, m_layers);
                    var r = new Rect(q.X - 5, q.Y - 5, 10, 10);
                    ctx.DrawRectangle(l.Supported ? BeamInk : Controls.Ui.FailFill, l.Supported ? LandEdge : MissingEdge, r);
                    string who = l.From.StartsWith("beam ") ? l.From.Substring(5) : l.From;
                    string up = l.Supported ? $"{who} ↑ {l.TensionAsd / 1000:0.0}k {l.Model ?? "none strong enough"}|{l.SupportId}"
                                            : $"{who} ↑ {l.TensionAsd / 1000:0.0}k, nothing under it|";
                    Add(l.At, q, null, up, l.Supported && l.Choice != null ? (l.Note != null ? Controls.Ui.CloseInk : LandText) : Controls.Ui.FailInk);
                    continue;
                }
                var p = WorldToScreen(l.At, m_layers);
                // a triangle pointing down: a load coming down onto this story
                var tri = new StreamGeometry();
                using (var g = tri.Open())
                {
                    g.BeginFigure(new Point(p.X - 6, p.Y - 9), true, true);
                    g.PolyLineTo(new[] { new Point(p.X + 6, p.Y - 9), new Point(p.X, p.Y) }, true, false);
                }
                tri.Freeze();
                ctx.DrawGeometry(l.Supported ? LandFill : Controls.Ui.FailFill, l.Supported ? LandEdge : MissingEdge, tri);
                // "load|what it lands on": loads landing on the same thing at one point are listed once ("↓ 1.0k + 0.6k on column C2")
                string rest = !l.Supported ? "  nothing below: draw a beam (B) or a column (P)" : l.Support == SupportKind.Wall ? "" : $" on {l.SupportId}";
                if (l.Note != null) rest += $"  · {l.Model ?? "none"} ({HoldownRules.Name(l.Family)} here)";
                Add(l.At, p, $"{l.TensionAsd / 1000:0.0}k|{rest}", null, !l.Supported ? Controls.Ui.FailInk : l.Note != null ? Controls.Ui.CloseInk : LandText);
            }
            foreach (var g in atPoint.Values)
            {
                var parts = g.downs.Select(t => t.Split('|')).GroupBy(t => t[1]).Select(x => "↓ " + string.Join(" + ", x.Select(t => t[0])) + x.Key).ToList();
                // a beam end's support is named only when the loads from above have not already named it
                string said = string.Join(" ", parts);
                parts.AddRange(g.ups.Select(t => t.Split('|')).Select(t => t[1].Length == 0 || said.Contains(t[1]) ? t[0] : $"{t[0]} on {t[1]}"));
                Label(ctx, string.Join("   ", parts), new Point(g.p.X + 8, g.p.Y - 10), g.ink, dip, above: true);
            }
        }

        /// <summary>
        /// Marks the overlays' labels must keep off, drawn later or by the sketch: the center of mass and of rigidity (markers
        /// and captions) and the sketch's wall labels ("W9  39'", beside a N-S wall, under an E-W wall).
        /// </summary>
        private void ReservePlanMarks()
        {
            if (Calculator?._wall_system == null) return;
            double s = LabelScale;
            void Around(Point world, double w, double h)
            {
                if (double.IsNaN(world.X) || double.IsNaN(world.Y)) return;
                var p = WorldToScreen(world, m_layers);
                labelsTaken.Add(new Rect(p.X - 14, p.Y - 14, w, h));
            }
            Around(Calculator._diaphragm_system?.CtrMass ?? new Point(double.NaN, double.NaN), 60, 30);
            Around(Calculator._wall_system.CtrRigidity, 60, 30);
            // the sketch's beam labels (at the middle, below right) and column labels (above right), on this level
            var lv = Sketch.ActiveLevel;
            if (building != null && lv != null)
            {
                foreach (var bm in building.BeamsOn(lv))
                {
                    var m = WorldToScreen(new Point((bm.Start.X + bm.End.X) / 2, (bm.Start.Y + bm.End.Y) / 2), m_layers);
                    labelsTaken.Add(new Rect(m.X + 2, m.Y + 1, 12 + 8 * s * bm.Id.Length, 15 * s));
                }
                foreach (var c in building.ColumnsUnder(lv))
                {
                    var p = WorldToScreen(new Point(c.At.X, c.At.Y), m_layers);
                    double half = Math.Max(4, 0.25 * Sketch.Zoom);
                    labelsTaken.Add(new Rect(p.X - half - 2, p.Y - half - 4 - 13 * s, half * 2 + 10 + 8 * s * c.Id.Length, half * 2 + 6 + 13 * s));
                }
            }
            foreach (var w in Calculator._wall_system._walls.Values)
            {
                var m = WorldToScreen(new Point((w.Start.X + w.End.X) / 2, (w.Start.Y + w.End.Y) / 2), m_layers);
                labelsTaken.Add(w.WallDir == calculator.WallDirs.NorthSouth
                    ? new Rect(m.X + 4, m.Y - 7 * s - 2, 58 * s, 15 * s)
                    : new Rect(m.X - 16, m.Y + 3, 58 * s, 15 * s));
            }
        }

        /// <summary>The plan's text size against the base sizes: the sketch's, so the overlays match the plan's own labels.</summary>
        private double LabelScale => Sketch?.LabelScale ?? 1;

        /// <summary>
        /// What the overlays have drawn so far this frame (wall tags, the schedule, labels): a label that would land on one
        /// is moved off it. Cleared at the start of each drawing of the plan.
        /// </summary>
        private readonly List<Rect> labelsTaken = new List<Rect>();

        /// <summary>
        /// A label on a light back, top-left at <paramref name="at"/>; <paramref name="above"/>: bottom-left there (it grows
        /// upward, off the mark under it). Moved by its own height down or up (the nearest first) when it would cover a tag,
        /// the schedule or another label.
        /// </summary>
        private void Label(DrawingContext ctx, string text, Point at, Brush ink, double dip, bool above = false)
        {
            var ft = new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), 10.5 * LabelScale, ink, dip);
            if (above) at = new Point(at.X, at.Y - ft.Height);
            // kept on the canvas: a label by the right edge goes to the left of its point instead of off the plan
            if (m_layers != null && at.X + ft.Width + 4 > m_layers.ActualWidth) at = new Point(Math.Max(2, m_layers.ActualWidth - ft.Width - 6), at.Y);
            var box = new Rect(at.X - 2, at.Y, ft.Width + 4, ft.Height);
            double step = ft.Height + 2, dir = above ? -1 : 1;
            for (int k = 1; k <= 6 && labelsTaken.Any(t => t.IntersectsWith(box)); k++)
            {
                double dy = ((k + 1) / 2) * step * (k % 2 == 1 ? dir : -dir); // away from the mark first, then the other way
                var tryBox = new Rect(at.X - 2, at.Y + dy, ft.Width + 4, ft.Height);
                if (!labelsTaken.Any(t => t.IntersectsWith(tryBox))) { box = tryBox; break; }
            }
            labelsTaken.Add(box);
            ctx.DrawRectangle(LabelBack, null, box);
            ctx.DrawText(ft, new Point(box.X + 2, box.Y));
        }

        /// <summary>
        /// The hold-down points to snap to on the level shown (a column under one, a beam end on one): those landing here from the
        /// story above, with the beam ends and column bases, and this story's own hold-downs; in the foundation view, the anchors.
        /// </summary>
        private IEnumerable<BuildingModel.Edits.SnapPoint> HoldownSnapPoints()
        {
            var list = new List<BuildingModel.Edits.SnapPoint>();
            BuildingModel.Edits.SnapPoint P(HoldownLanding l)
            {
                var from = l.UpperStory - 1 >= 0 && l.UpperStory - 1 < stories.Count ? stories[l.UpperStory - 1] : null;
                string wall = from != null && from.Walls.TryGetValue(l.WallId, out var id) ? id : $"W{l.WallId}";
                string label = l.Kind == PathPointKind.WallHoldown ? $"Hold-down {wall}" : l.From;
                return new BuildingModel.Edits.SnapPoint(new BuildingModel.Geometry.Point2(l.At.X, l.At.Y), label);
            }
            if (foundationView) { list.AddRange(foundationAnchors.Select(P)); return list; }
            if (landingsOnto.TryGetValue(currentLevel, out var onto)) list.AddRange(onto.Select(P));
            var own = currentLevel == 0 ? foundationAnchors : landingsOnto.TryGetValue(currentLevel - 1, out var below) ? below : null;
            if (own != null) list.AddRange(own.Where(l => l.Kind == PathPointKind.WallHoldown && l.UpperStory == currentLevel + 1).Select(P));
            return list;
        }

        /// <summary>The caption of the foundation view.</summary>
        private string FoundationCaption()
        {
            int missing = foundationAnchors.Count(a => a.Model == null);
            double max = foundationAnchors.Count == 0 ? 0 : foundationAnchors.Max(a => a.TensionAsd);
            return $"Foundation  ·  {foundationAnchors.Count} hold-down anchor{(foundationAnchors.Count == 1 ? "" : "s")} of {LevelName(0)}'s walls" +
                   (foundationAnchors.Count > 0 ? $", largest uplift {max:N0} lb (ASD)" : "") + (missing > 0 ? $"  ·  {missing} without a hold-down strong enough" : "");
        }

        /// <summary>FOUNDATION: the ground plan with the first story's hold-down anchors.</summary>
        private void Foundation_Click(object sender, RoutedEventArgs e)
        {
            var ground = building?.Ground;
            if (ground == null || stories.Count == 0) return;
            Sketch.SetTool(BuildingModel.Sketch.SketchTool.Select);
            Sketch.ShowLevel(ground.Id); // ActiveLevelChanged turns the foundation view on
        }
    }
}
