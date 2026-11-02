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
                    Label(ctx, $"{wallId}  {a.Model ?? "no HD"}  {a.TensionAsd:N0} lb", new Point(p.X + 7, p.Y - 16), a.Model == null ? Controls.Ui.FailInk : a.Note != null ? Controls.Ui.CloseInk : LandText, dip);
                }
                return;
            }
            if (!landingsOnto.TryGetValue(currentLevel, out var onto)) return;
            // the beams that carry hold-downs: their loads and the reactions at the ends
            foreach (var br in loadPath?.Beams.Where(x => x.Beam.OnStory == currentLevel) ?? Enumerable.Empty<BeamReactions>())
            {
                var mid = WorldToScreen(new Point((br.Beam.Start.X + br.Beam.End.X) / 2, (br.Beam.Start.Y + br.Beam.End.Y) / 2), m_layers);
                Label(ctx, $"{br.Beam.Id}: {br.Loads.Count} hold-down{(br.Loads.Count == 1 ? "" : "s")}, R {br.StartAsd / 1000:0.0}k / {br.EndAsd / 1000:0.0}k", new Point(mid.X + 6, mid.Y + 6), BeamInk, dip);
            }
            foreach (var l in onto)
            {
                if (l.Kind != PathPointKind.WallHoldown)
                {
                    // a beam end or a column base: a new hold-down point, sized for its reaction
                    var q = WorldToScreen(l.At, m_layers);
                    var r = new Rect(q.X - 5, q.Y - 5, 10, 10);
                    ctx.DrawRectangle(l.Supported ? BeamInk : Controls.Ui.FailFill, l.Supported ? LandEdge : MissingEdge, r);
                    string t = l.Supported ? $"{l.From}: ↑ {l.TensionAsd / 1000:0.0}k → {l.SupportId}, {l.Model ?? "none strong enough"}"
                                           : $"{l.From}: ↑ {l.TensionAsd / 1000:0.0}k, nothing under it";
                    Label(ctx, t, new Point(q.X + 8, q.Y + 4), l.Supported && l.Choice != null ? (l.Note != null ? Controls.Ui.CloseInk : LandText) : Controls.Ui.FailInk, dip);
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
                string text = !l.Supported ? $"↓ {l.TensionAsd / 1000:0.0}k  nothing below: draw a beam (B) or a column (P)"
                            : l.Support == SupportKind.Wall ? $"↓ {l.TensionAsd / 1000:0.0}k" : $"↓ {l.TensionAsd / 1000:0.0}k on {l.SupportId}";
                if (l.Note != null) text += $"  · {l.Model ?? "none"} ({HoldownRules.Name(l.Family)} here)";
                Label(ctx, text, new Point(p.X + 8, p.Y - 20), !l.Supported ? Controls.Ui.FailInk : l.Note != null ? Controls.Ui.CloseInk : LandText, dip);
            }
        }

        private static void Label(DrawingContext ctx, string text, Point at, Brush ink, double dip)
        {
            var ft = new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), 10.5, ink, dip);
            ctx.DrawRectangle(LabelBack, null, new Rect(at.X - 2, at.Y, ft.Width + 4, ft.Height));
            ctx.DrawText(ft, at);
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
