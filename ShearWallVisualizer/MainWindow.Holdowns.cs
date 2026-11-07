using BuildingModel.Edits;
using BuildingModel.Geometry;
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

        /// <summary>
        /// The foundation at each anchor point: the walls (and column bases) meeting there added case by case; the worst
        /// uplift and the worst overturning compression (wind alone, no dead load), each in its own case.
        /// </summary>
        private List<ShearWallCalculator.Design.FoundationPoint> foundationPoints = new List<ShearWallCalculator.Design.FoundationPoint>();

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
            foundationPoints = FoundationLoads();
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
                    // a wall's hold-down type that cannot anchor here: a valid one is picked automatically (shown on the plan
                    // and in the Design tab's Hold-down column; the wall's Hold-down type overrides it), an issue only when
                    // none of that type is strong enough
                    else if (l.Note != null && l.Choice == null)
                        list.Add(new ModelIssue { Severity = Severity.Error, Code = "HoldownTypeReplaced", LevelId = levelId, At = at, Message = $"{what} on {l.SupportId ?? levelName}: {l.Note}" });
                    else if (l.Choice == null && l.Kind != PathPointKind.WallHoldown)
                        list.Add(new ModelIssue
                        {
                            Severity = Severity.Error, Code = "HoldownTooWeak", LevelId = levelId, At = at,
                            Message = $"{what}: no {HoldownRules.Name(l.Family).Substring(HoldownRules.Name(l.Family).IndexOf(' ') + 1)} is strong enough for {l.TensionAsd:N0} lb on {l.SupportId ?? levelName}.",
                        });
                }
            }
        }

        private static readonly Brush LabelBack = Controls.Ui.Frozen("#E6FFFFFF");
        private static readonly Brush BeamInk = Controls.Ui.Frozen("#8B4A14");

        private static Pen MakePen(Brush b, double t) { var p = new Pen(b, t); p.Freeze(); return p; }

        /// <summary>Over the plan: the hold-downs from the story above (a level view), or the foundation anchors (the foundation view).</summary>
        private void DrawLandings(DrawingContext ctx)
        {
            // SHOW Hold-downs: the markers, what lands from above and the anchors; SHOW Reactions: a level's beam-end reactions
            if (!showHoldowns && (foundationView || !showReactions)) return;
            heldLabels = new List<(Rect, FormattedText)>(); // every leader first, then the labels over them
            try { DrawLandingMarks(ctx); }
            finally { FlushLabels(ctx); }
        }

        private void DrawLandingMarks(DrawingContext ctx)
        {
            double dip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            // the foundation plan's hold-downs are the model's: this app relays them (Sketch.Connections, the Lateral
            // section it saves) and the shared sketch draws them, as in every app
            if (foundationView) return;
            var onto = landingsOnto.TryGetValue(currentLevel, out var here) ? here : new List<HoldownLanding>();
            var outline = BuildingOutline(); // the labels go outside it, each with an arrow to its point
            // Each end of every beam on its own (SHOW Reactions): its reaction along the beam and its own connection, pointing
            // at that end; two beams on one column are two connections, never one label
            if (showReactions) DrawBeamEnds(ctx, onto.Where(l => l.Kind == PathPointKind.BeamEnd).ToList(), outline, dip);
            foreach (var ic in planIcons) PlaceIconLabel(ctx, ic, outline, dip); // each hold-down marker's label, an arrow to it
            // the hold-downs landing from above, the column bases and the beam ends carrying them on down: the model's, drawn
            // by the shared sketch from what this app relays (Sketch.Connections), the same on every level and in every app
        }

        /// <summary>
        /// Each end of every beam on the level: its uplift reaction "(2.7k)" along the beam just inside that end, and a small
        /// label of its own connection ("MSTI26") with an arrow at that end, outside the building when the beam is on its
        /// edge. An end with no uplift: "(0.0k)" and "None" (no hold-down needed; gravity is not calculated yet). Red: nothing
        /// under the end, or no connection strong enough; amber: a connection of another type than the wall's.
        /// </summary>
        private void DrawBeamEnds(DrawingContext ctx, List<HoldownLanding> ends, Rect? outline, double dip)
        {
            var lv = Sketch.ActiveLevel;
            if (building == null || lv == null) return;
            foreach (var bm in building.BeamsOn(lv))
            foreach (bool atStart in new[] { true, false })
            {
                string endName = $"beam {bm.Id} {(atStart ? "start" : "end")}";
                var l = ends.FirstOrDefault(x => x.From == endName); // null: no uplift at this end
                Point end = Sketch.ToScreen(atStart ? bm.Start : bm.End), other = Sketch.ToScreen(atStart ? bm.End : bm.Start);
                if ((other - end).Length < 20) continue;
                var ink = l == null ? BeamInk : l.Supported && l.Choice != null ? (l.Note != null ? Controls.Ui.CloseInk : BeamInk) : Controls.Ui.FailInk;

                // the reaction, along the beam just inside its end
                var rft = new FormattedText($"({(l?.TensionAsd ?? 0) / 1000:0.0}k)", CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), 9.5 * LabelScale, ink, dip);
                double ang = BuildingModel.Sketch.SketchCanvas.MemberLabelAngle(other - end);
                var rsize = new Size(rft.Width + 4, rft.Height);
                // just inside the end on the side away from its name; taken (another beam's end at the same column): the
                // name's side, then further in along the beam; with no clear spot, the one covering least
                var spots = new List<(Point c, Point tip, Rect bounds)>();
                foreach (double gap in new[] { 15.0, 15 + rsize.Width * 0.6, 15 + rsize.Width * 1.2 })
                    foreach (bool nameSide in new[] { false, true })
                    {
                        var (sc, st) = ShearWallCalculator.Drawing.PlanLabels.BeamEnd(end, other, rsize, gap, ang, nameSide);
                        var sb = new RotateTransform(ang, sc.X, sc.Y).TransformBounds(new Rect(sc.X - rsize.Width / 2, sc.Y - rsize.Height / 2, rsize.Width, rsize.Height));
                        spots.Add((sc, st, sb));
                    }
                var pick = spots.FirstOrDefault(s => !labelsTaken.Any(tk => Controls.DesignOverlay.Overlaps(tk, s.bounds)));
                if (pick.bounds.Width == 0) pick = spots.OrderBy(s => Controls.DesignOverlay.Covered(s.bounds, labelsTaken)).First(); // default: a zero box
                var c = pick.c;
                var tip = pick.tip;
                var rbox = new Rect(c.X - rsize.Width / 2, c.Y - rsize.Height / 2, rsize.Width, rsize.Height);
                var turn = new RotateTransform(ang, c.X, c.Y);
                ctx.PushTransform(turn);
                ctx.DrawRectangle(LabelBack, null, rbox);
                ctx.DrawText(rft, new Point(rbox.X + 2, rbox.Y));
                ctx.Pop();
                labelsTaken.Add(turn.TransformBounds(rbox));

                // its own connection: the marker at this end (DrawPlanHoldowns) and its label (PlaceIconLabel)
            }
        }

        /// <summary>A hold-down marker on the plan, drawn before the tags and labels, waiting for its own label.</summary>
        private readonly List<(Point icon, Vector inward, string text, Brush ink, double size)> planIcons = new List<(Point, Vector, string, Brush, double)>();

        /// <summary>
        /// The level plan's hold-down markers, by the shared rule (<see cref="BuildingModel.Sketch.HoldownMarks"/>, as on the
        /// building model's foundation plan): each shear wall's hold-downs exactly where they go (WallSchedule.HoldownPoints:
        /// on the inside face of the end post the hold-down needs, a post in from each end of the shear part), the marker
        /// as wide as the post where it shows; each beam end's connection just in from the end (SHOW Reactions; an open
        /// square, "None", where no hold-down is needed). Drawn and kept clear first, so the tags and labels keep off them;
        /// each gets its label with an arrow later (PlaceIconLabel), so two close together are told apart.
        /// </summary>
        private void DrawPlanHoldowns(DrawingContext ctx)
        {
            planIcons.Clear();
            double size = Controls.DesignOverlay.HoldownSize;
            void Mark(Point end, Point other, BuildingModel.Sketch.HoldownMark mark, string text, Brush ink, Point? at = null, double s = BuildingModel.Sketch.HoldownMarks.Size)
            {
                var u = other - end;
                if (u.Length < 20) return;
                u.Normalize();
                var c = at ?? BuildingModel.Sketch.HoldownMarks.IconAt(end, other, size);
                var box = BuildingModel.Sketch.HoldownMarks.DrawIcon(ctx, c, mark, s);
                box.Inflate(1, 1);
                labelsTaken.Add(box);
                planIcons.Add((c, u, text, ink, s));
            }
            // the walls' hold-downs, and the beam ends carrying hold-downs on down: the model's (the Lateral section this app
            // relays), drawn by the shared sketch on every level as in every app; here only the beam ends with no uplift ("None")
            var lv = Sketch.ActiveLevel;
            if (showHoldowns && building != null && lv != null)
            {
                var onto = landingsOnto.TryGetValue(currentLevel, out var here) ? here : new List<HoldownLanding>();
                foreach (var bm in building.BeamsOn(lv))
                foreach (bool atStart in new[] { true, false })
                {
                    string endName = $"beam {bm.Id} {(atStart ? "start" : "end")}";
                    if (onto.Any(x => x.Kind == PathPointKind.BeamEnd && x.From == endName)) continue; // the shared sketch's
                    Mark(Sketch.ToScreen(atStart ? bm.Start : bm.End), Sketch.ToScreen(atStart ? bm.End : bm.Start), BuildingModel.Sketch.HoldownMark.None, "None", BeamInk);
                }
            }
        }

        /// <summary>
        /// A hold-down marker's label, placed by the shared rule (<see cref="BuildingModel.Sketch.HoldownMarks.LabelBox"/>, the
        /// same in every app): beside its member a little in from the end, outside the building first when the member is on
        /// its edge, kept off the other labels; with an arrow to the marker.
        /// </summary>
        private void PlaceIconLabel(DrawingContext ctx, (Point icon, Vector inward, string text, Brush ink, double size) ic, Rect? outline, double dip)
        {
            var ft = new FormattedText(ic.text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), 9.5 * LabelScale, ic.ink, dip);
            var box = BuildingModel.Sketch.HoldownMarks.LabelBox(ic.icon, ic.inward, new Size(ft.Width + 4, ft.Height), outline, labelsTaken);
            labelReach.Union(box);
            BuildingModel.Sketch.HoldownMarks.Arrow(ctx, box, ic.icon, ic.ink, ic.size);
            if (heldLabels != null) heldLabels.Add((box, ft));
            else BuildingModel.Sketch.HoldownMarks.DrawLabel(ctx, box, ft);
        }

        /// <summary>The level's drawing on screen (as Zoom to the drawing frames it); null when there is none.</summary>
        private Rect? BuildingOutline()
        {
            var ext = BuildingEdits.Extents(Sketch.Building, Sketch.ActiveLevel) ?? BuildingEdits.Extents(Sketch.Building);
            if (ext == null) return null;
            var a = Sketch.ToScreen(new Point2(ext.Value.X0, ext.Value.Y0 + ext.Value.Height));
            var b = Sketch.ToScreen(new Point2(ext.Value.X0 + ext.Value.Width, ext.Value.Y0));
            return new Rect(a, b);
        }

        /// <summary>
        /// Marks the overlays' labels must keep off, drawn later or by the sketch: the center of mass and of rigidity (markers
        /// and captions), the sketch's wall, beam and column labels and its marks at the plan's edges.
        /// </summary>
        private void ReservePlanMarks()
        {
            if (Calculator?._wall_system == null) return;
            // the sketch's own marks at the plan's edges: the scale bar, the grid's numbers along the bottom and the left
            var bar = Sketch.ScaleBarArea();
            if (!bar.IsEmpty) labelsTaken.Add(bar);
            labelsTaken.Add(new Rect(0, Sketch.ActualHeight - 16, Sketch.ActualWidth, 16));
            labelsTaken.Add(new Rect(0, 0, 28, Sketch.ActualHeight));
            void Around(Point world, double w, double h)
            {
                if (double.IsNaN(world.X) || double.IsNaN(world.Y)) return;
                var p = WorldToScreen(world, m_layers);
                labelsTaken.Add(new Rect(p.X - 14, p.Y - 14, w, h));
            }
            Around(Calculator._diaphragm_system?.CtrMass ?? new Point(double.NaN, double.NaN), 60, 30);
            Around(Calculator._wall_system.CtrRigidity, 60, 30);
            // the sketch's wall, beam and column labels, where it drew them this frame (along their members; a column's set
            // off with a leader), and the column squares themselves
            labelsTaken.AddRange(Sketch.MemberLabelBoxes);
            var lv = Sketch.ActiveLevel;
            if (building != null && lv != null)
                foreach (var c in building.ColumnsUnder(lv))
                {
                    var p = WorldToScreen(new Point(c.At.X, c.At.Y), m_layers);
                    double half = Math.Max(4, 0.25 * Sketch.Zoom);
                    labelsTaken.Add(new Rect(p.X - half, p.Y - half, half * 2, half * 2));
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
        /// the schedule or another label. <paramref name="leader"/>: the point the label is about; a thin line runs from it
        /// to the label's nearest edge, so a label moved off its point still shows which one it is.
        /// </summary>
        private void Label(DrawingContext ctx, string text, Point at, Brush ink, double dip, bool above = false, Point? leader = null)
        {
            var ft = new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), 10.5 * LabelScale, ink, dip);
            if (above) at = new Point(at.X, at.Y - ft.Height);
            labelReach.Union(new Rect(at.X - 2, at.Y, ft.Width + 4, ft.Height)); // where it wants to be (before the canvas edge moves it)
            // kept on the canvas: a label by the right edge goes to the left of its point instead of off the plan
            // (with a leader: wholly to the left of its point, mirrored, so it does not sit beside another point)
            if (m_layers != null && at.X + ft.Width + 4 > m_layers.ActualWidth)
                at = new Point(Math.Max(2, leader is Point lp ? lp.X - (at.X - lp.X) - ft.Width : m_layers.ActualWidth - ft.Width - 6), at.Y);
            var box = new Rect(at.X - 2, at.Y, ft.Width + 4, ft.Height);
            double step = ft.Height + 2, dir = above ? -1 : 1;
            if (labelsTaken.Any(t => Controls.DesignOverlay.Overlaps(t, box)))
            {
                // away from the mark first, then the other way; with no clear spot, the one covering the least (not the first:
                // that can lie across a wall's tag)
                var tries = new List<Rect> { box };
                for (int k = 1; k <= 6; k++)
                {
                    double dy = ((k + 1) / 2) * step * (k % 2 == 1 ? dir : -dir);
                    tries.Add(new Rect(at.X - 2, at.Y + dy, ft.Width + 4, ft.Height));
                }
                var clear = tries.Where(x => !labelsTaken.Any(t => Controls.DesignOverlay.Overlaps(t, x))).ToList();
                box = clear.Count > 0 ? clear[0] : tries.OrderBy(x => Controls.DesignOverlay.Covered(x, labelsTaken)).First();
            }
            labelsTaken.Add(box);
            if (leader is Point from)
            {
                // to the nearest point of the label's edge; a short tick when the label sits right at its point
                var to = new Point(Math.Max(box.Left, Math.Min(box.Right, from.X)), Math.Max(box.Top, Math.Min(box.Bottom, from.Y)));
                if ((to - from).Length > 3)
                {
                    var pen = new Pen(ink, 0.9);
                    pen.Freeze();
                    ctx.DrawLine(pen, from, to);
                    ctx.DrawEllipse(ink, null, to, 1.6, 1.6);
                }
            }
            if (heldLabels != null) { heldLabels.Add((box, ft)); return; } // drawn over every leader (FlushLabels)
            ctx.DrawRectangle(LabelBack, null, box);
            ctx.DrawText(ft, new Point(box.X + 2, box.Y));
        }

        /// <summary>Every label of this frame, where each wants to be (screen pixels).</summary>
        private Rect labelReach = Rect.Empty;

        /// <summary>How far the labels reach past the drawing on each side (pixels): Zoom to the drawing leaves this room.</summary>
        private Thickness labelRoom;

        private void MeasureLabelRoom()
        {
            var outline = BuildingOutline(); // as ZoomExtents
            if (outline == null || labelReach.IsEmpty) { labelRoom = new Thickness(); return; }
            var d = outline.Value;
            const double gap = 12;
            labelRoom = new Thickness(Math.Max(0, d.Left - labelReach.Left) + gap, Math.Max(0, d.Top - labelReach.Top) + gap,
                                      Math.Max(0, labelReach.Right - d.Right) + gap, Math.Max(0, labelReach.Bottom - d.Bottom) + gap);
        }

        /// <summary>Labels held back while a set of leaders is drawn, so no leader runs over a label's text; null: drawn at once.</summary>
        private List<(Rect box, FormattedText ft)> heldLabels;

        private void FlushLabels(DrawingContext ctx)
        {
            if (heldLabels != null)
                foreach (var (box, ft) in heldLabels)
                {
                    ctx.DrawRectangle(LabelBack, null, box);
                    ctx.DrawText(ft, new Point(box.X + 2, box.Y));
                }
            heldLabels = null;
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
            // at one point, the walls meeting there added case by case (not each wall's own worst)
            double up = foundationPoints.Count == 0 ? 0 : foundationPoints.Max(p => p.UpliftAsd);
            double down = foundationPoints.Count == 0 ? 0 : foundationPoints.Max(p => p.BearingAsd);
            return "Foundation (wind only)" + (foundationPoints.Count > 0 ? $"  ·  max at a point +{down / 1000:0.0}k (C) / −{up / 1000:0.0}k (T) ASD" : "") +
                   (missing > 0 ? $"  ·  {missing} hold-down{(missing == 1 ? "" : "s")} too weak" : "");
        }

        /// <summary>
        /// What the first story puts on the foundation: both ends of every wall (its hold-down points), per load case, and
        /// the column bases' uplift; added where they meet (<see cref="ShearWallCalculator.Design.FoundationForces"/>).
        /// </summary>
        private List<ShearWallCalculator.Design.FoundationPoint> FoundationLoads()
        {
            var loads = new List<ShearWallCalculator.Design.FoundationLoad>();
            if (levels.Count == 0 || allDesignResults.Count == 0 || levels[0]?._wall_system == null) return new List<ShearWallCalculator.Design.FoundationPoint>();
            var design = allDesignResults[0];
            foreach (var kv in levels[0]._wall_system._walls)
            {
                if (design == null || !design.TryGetValue(kv.Key, out var r) || r?.EndForceByCase == null) continue;
                var w = kv.Value;
                var (s, e) = ShearWallCalculator.Design.WallSchedule.HoldownPoints(w, r.Holdown);
                bool lowIsStart = w.WallDir == calculator.WallDirs.EastWest ? w.Start.X <= w.End.X : w.Start.Y <= w.End.Y;
                string name = stories.Count > 0 && stories[0].Walls.TryGetValue(kv.Key, out var id) ? id : $"W{kv.Key}";
                loads.AddRange(ShearWallCalculator.Design.FoundationForces.OfWall(name, lowIsStart ? s : e, lowIsStart ? e : s, r.EndForceByCase));
            }
            foreach (var a in foundationAnchors.Where(a => a.Kind == PathPointKind.ColumnBase))
                loads.Add(new ShearWallCalculator.Design.FoundationLoad
                {
                    At = a.At, From = a.From, Envelope = a.TensionAsd,
                    ByCase = a.CaseAsd == null ? null : new Dictionary<string, double>(a.CaseAsd),
                });
            var points = ShearWallCalculator.Design.FoundationForces.Combine(loads);
            foreach (var p in points.Where(p => p.From.Count > 1))
                Log.Debug($"Foundation at ({p.At.X:0.##}, {p.At.Y:0.##}): {string.Join(" + ", p.From)}: uplift {p.UpliftAsd:0} lb ({p.UpliftCase}), bearing {p.BearingAsd:0} lb ({p.BearingCase}), case by case");
            return points;
        }

        /// <summary>
        /// The end of the viewport strip: what the plan shows, a word or two per overlay that is on ("Hold-downs · Wind
        /// reactions" on G), so the foundation's numbers are not read as everything it must carry (no gravity here).
        /// </summary>
        private void UpdateShowing()
        {
            if (tbShowing == null) return;
            var parts = new List<string>();
            if (foundationView)
            {
                // a word or two: these are the WIND forces only, not everything the foundation carries
                if (showHoldowns && showReactions) parts.Add("Wind only");
                else if (showHoldowns) parts.Add("Wind hold-downs");
                else if (showReactions) parts.Add("Wind reactions");
            }
            else
            {
                if (showDesign) parts.Add("Wall design");
                if (showHoldowns) parts.Add("Hold-downs");
                if (showReactions && building != null && Sketch.ActiveLevel != null && building.BeamsOn(Sketch.ActiveLevel).Any()) parts.Add("Beam reactions");
                if (showLoads) parts.Add("Wind loads");
            }
            string text = parts.Count == 0 ? "Plan only" : string.Join("  ·  ", parts);
            if (tbShowing.Text == text) return;
            tbShowing.Text = text;
            showingBadge.ToolTip = foundationView
                ? "The foundation's WIND forces from the first story's walls: hold-down uplift and the overturning compression, the wind alone (no dead load counted). " +
                  "Gravity loads are not included: the Load Manager's foundation view adds them."
                : "What the plan shows: the shear wall design (marks, hold-downs) and, when on, one wind load case.";
        }

        /// <summary>SHOW Reactions: on the foundation, what it takes at each point; on a level, each beam end's reaction and connection.</summary>
        private bool showReactions = true;

        private static readonly Brush ReactionInk = Controls.Ui.Frozen("#5B2C83");

        /// <summary>
        /// The foundation reactions, one short label per point, below right of it with a leader: "+6.8k (C) / −7.6k (T)",
        /// + compression, − tension (uplift), 0.6W alone (no dead load), kips ASD; the walls (and column bases)
        /// meeting at the point added case by case, each the worst in its own case. A column base: T only.
        /// </summary>
        private void DrawReactions(DrawingContext ctx)
        {
            if (!foundationView || !showReactions || foundationPoints.Count == 0) return;
            double dip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            var wallsAt = new HashSet<(long, long)>(foundationAnchors.Where(a => a.Kind == PathPointKind.WallHoldown)
                                                                       .Select(a => ShearWallCalculator.Design.FoundationForces.Key(a.At)));
            foreach (var fp in foundationPoints)
            {
                var p = WorldToScreen(fp.At, m_layers);
                bool walls = fp.From.Any(f => !f.StartsWith("column")) || wallsAt.Contains(ShearWallCalculator.Design.FoundationForces.Key(fp.At));
                string text = (walls ? $"+{fp.BearingAsd / 1000:0.0}k (C) / " : "") + $"−{fp.UpliftAsd / 1000:0.0}k (T)";
                Label(ctx, text, new Point(p.X + 10, p.Y + 8), ReactionInk, dip, above: false, leader: p);
            }
        }

        /// <summary>SHOW Hold-downs: the hold-down markers and their labels, what lands on the floor from above, the foundation's anchors.</summary>
        private bool showHoldowns = true;

        private void btnHoldowns_Click(object sender, RoutedEventArgs e)
        {
            showHoldowns = !showHoldowns;
            Sketch.ShowConnections = showHoldowns; // the foundation plan's, drawn by the shared sketch
            btnHoldowns.Tag = showHoldowns ? "on" : null;
            Log.Info($"Hold-downs overlay {(showHoldowns ? "on" : "off")}.");
            RedrawDrawing();
        }

        private void btnReactions_Click(object sender, RoutedEventArgs e)
        {
            showReactions = !showReactions;
            btnReactions.Tag = showReactions ? "on" : null;
            Log.Info($"Reactions overlay {(showReactions ? "on" : "off")}.");
            RedrawDrawing();
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
