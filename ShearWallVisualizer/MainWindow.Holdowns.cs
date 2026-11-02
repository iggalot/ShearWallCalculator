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
            if (levels.Count == 0 || allDesignResults.Count != levels.Count) return;
            for (int i = 1; i < levels.Count; i++)
                if (levels[i]?._wall_system != null && levels[i - 1]?._wall_system != null)
                    landingsOnto[i - 1] = StoryCascade.Landings(levels[i]._wall_system._walls, allDesignResults[i], levels[i - 1]._wall_system._walls, i + 1);
            if (levels[0]?._wall_system != null)
                foundationAnchors = StoryCascade.Landings(levels[0]._wall_system._walls, allDesignResults[0], null, 1);
            int open = landingsOnto.Values.Sum(l => l.Count(x => !x.Supported));
            if (open > 0) Log.Info("Hold-downs", $"{open} hold-down{(open == 1 ? "" : "s")} from an upper story land where there is no wall below.");
        }

        /// <summary>The issues: an upper hold-down with no wall under it, on the level where the support is missing.</summary>
        private void AddLandingIssues(List<ModelIssue> list)
        {
            foreach (var kv in landingsOnto)
            {
                if (kv.Key >= stories.Count) continue;
                var lower = stories[kv.Key];
                var upper = kv.Key + 1 < stories.Count ? stories[kv.Key + 1] : null;
                foreach (var l in kv.Value.Where(x => !x.Supported))
                {
                    string wallId = upper != null && upper.Walls.TryGetValue(l.WallId, out var id) ? id : $"wall {l.WallId}";
                    list.Add(new ModelIssue
                    {
                        Severity = Severity.Error, Code = "HoldownUnsupported", LevelId = lower.Level.Id,
                        At = new BuildingModel.Geometry.Point2(l.At.X, l.At.Y),
                        Message = $"A hold-down of {wallId} ({upper?.Level.Name}, {l.Model ?? "no model"}, {l.TensionAsd:N0} lb) lands on {lower.Level.Name} " +
                                  $"at ({l.At.X:0.##}, {l.At.Y:0.##}) with no wall under it: add a beam or a post to carry it down.",
                    });
                }
            }
        }

        private static readonly Brush LandFill = Controls.Ui.Frozen("#1F2933"), LandText = Controls.Ui.InkSecondary, LabelBack = Controls.Ui.Frozen("#E6FFFFFF");
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
                    string wallId = stories.Count > 0 && stories[0].Walls.TryGetValue(a.WallId, out var id) ? id : $"W{a.WallId}";
                    Label(ctx, $"{wallId}  {a.Model ?? "no HD"}  {a.TensionAsd:N0} lb", new Point(p.X + 7, p.Y - 16), a.Model != null ? LandText : Controls.Ui.FailInk, dip);
                }
                return;
            }
            if (!landingsOnto.TryGetValue(currentLevel, out var onto)) return;
            foreach (var l in onto)
            {
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
                string text = l.Supported ? $"↓ {l.TensionAsd / 1000:0.0}k" : $"↓ {l.TensionAsd / 1000:0.0}k  no wall below: beam or post";
                Label(ctx, text, new Point(p.X + 8, p.Y - 20), l.Supported ? LandText : Controls.Ui.FailInk, dip);
            }
        }

        private static void Label(DrawingContext ctx, string text, Point at, Brush ink, double dip)
        {
            var ft = new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), 10.5, ink, dip);
            ctx.DrawRectangle(LabelBack, null, new Rect(at.X - 2, at.Y, ft.Width + 4, ft.Height));
            ctx.DrawText(ft, at);
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
