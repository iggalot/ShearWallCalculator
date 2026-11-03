using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using calculator;
using ShearWallCalculator.Design;

namespace ShearWallVisualizer.Controls
{
    /// <summary>
    /// The design drawn over the plan, as on a drawing set: a schedule mark on each wall (SW1, SW2, ... per sheathing
    /// construction, with the wall's hold-down model), the hold-downs at both wall ends on the post centerlines, and the
    /// shear wall schedule in the lower-left corner. Pass/fail is the wall color, drawn with the walls.
    /// </summary>
    public static class DesignOverlay
    {
        private static readonly Typeface Face = new Typeface("Segoe UI");
        private static readonly Typeface FaceBold = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);
        private static readonly Brush TagFill = Ui.Frozen("#FFFFFF"), LegendFill = Ui.Frozen("#F2FFFFFF");
        private static readonly Brush HoldownFill = Ui.Frozen("#1F2933");
        private static readonly Pen TagPass = Frozen(Ui.PassInk, 1.2), TagFail = Dashed(Ui.FailStripe, 1.8), TagClose = Frozen(Ui.CloseStripe, 1.6), HairlinePen = Frozen(Ui.Hairline, 1),
                                    HoldownEdge = Frozen(Brushes.White, 1), HoldownMissing = Frozen(Ui.FailInk, 2);

        private const double TagOffset = 15;   // px from the wall line to the tag center
        private const double HoldownSize = 8;  // px, the hold-down square
        private const int LegendRows = 8;

        private static Pen Frozen(Brush b, double t)
        {
            var p = new Pen(b, t);
            p.Freeze();
            return p;
        }

        private static Pen Dashed(Brush b, double t)
        {
            var p = new Pen(b, t) { DashStyle = new DashStyle(new double[] { 2.5, 1.5 }, 0) };
            p.Freeze();
            return p;
        }

        /// <summary>Draws the overlay. <paramref name="toScreen"/> maps world ft to canvas pixels.</summary>
        public static void Draw(DrawingContext ctx, IEnumerable<KeyValuePair<int, WallData>> walls, IReadOnlyDictionary<int, WallDesignResult> design,
                                Func<Point, Point> toScreen, Size view, double pixelsPerDip, double labelScale = 1, List<Rect> taken = null,
                                bool schedule = true, string markLevel = null)
        {
            taken = taken ?? new List<Rect>();
            scale = labelScale > 0 ? labelScale : 1; // the plan's text size (the sketch's LabelScale)
            if (walls == null || design == null || design.Count == 0) return;
            var entries = WallSchedule.Build(design, markLevel);
            var marks = WallSchedule.MarksByWall(entries);
            var bounds = new Rect(view);
            Rect drawn = Rect.Empty; // screen extent of the walls and their tags, kept clear by the schedule

            foreach (var kv in walls)
            {
                if (!design.TryGetValue(kv.Key, out var r) || !marks.TryGetValue(kv.Key, out string mark)) continue;
                var w = kv.Value;
                Point a = toScreen(w.Start), b = toScreen(w.End);
                Vector along = b - a;
                if (along.Length < 1) continue;
                drawn.Union(a);
                drawn.Union(b);

                if (r.HoldownNeeded)
                {
                    var (h1, h2) = WallSchedule.HoldownPoints(w, r.Holdown);
                    foreach (var p in new[] { toScreen(h1), toScreen(h2) })
                        if (bounds.Contains(p)) DrawHoldown(ctx, p, r.Holdown != null);
                }

                // Tag on the side away from the wall number (drawn below/right of the midpoint): above an E-W wall, left of a
                // N-S wall; on the other side when that would leave the canvas.
                var n = new Vector(-along.Y, along.X);
                n.Normalize();
                if (n.Y > 1e-9 || (Math.Abs(n.Y) <= 1e-9 && n.X > 0)) n = -n;
                string hd = !r.HoldownNeeded ? null : r.Holdown?.Model ?? "no HD";
                var tag = Tag(mark, hd, pixelsPerDip);
                bool vertical = Math.Abs(along.X) < Math.Abs(along.Y);
                Point mid = a + along / 2;
                // the preferred side, the other side, then slid along the wall both ways: the first spot on the canvas that
                // touches no tag already drawn (walls close together put their tags on top of each other otherwise)
                var tries = new List<Rect>();
                Vector slide = vertical ? new Vector(0, tag.size.Height + 3) : new Vector(tag.size.Width * 0.6, 0);
                for (int k = 0; k <= 4; k++)
                    foreach (int sgn in k == 0 ? new[] { 1 } : new[] { 1, -1 })
                        foreach (var side in new[] { n, -n })
                        {
                            var bx = TagBox(mid, side, tag.size, vertical);
                            bx.Offset(slide * k * sgn);
                            tries.Add(bx);
                        }
                var inView = tries.Where(x => bounds.Contains(x)).ToList();
                if (inView.Count == 0) continue;
                Rect box = inView.FirstOrDefault(x => !taken.Any(t => t.IntersectsWith(x)));
                if (box.IsEmpty) box = inView[0];
                DrawTag(ctx, box, tag, r.State);
                taken.Add(box);
                drawn.Union(box);
            }

            // the schedule on the plan, unless the window shows it beside the plan (the drawing key)
            if (schedule) DrawLegend(ctx, entries, view, drawn, pixelsPerDip, taken);
        }

        private static void DrawHoldown(DrawingContext ctx, Point p, bool found)
        {
            var box = new Rect(p.X - HoldownSize / 2, p.Y - HoldownSize / 2, HoldownSize, HoldownSize);
            if (found)
                ctx.DrawRectangle(HoldownFill, HoldownEdge, box);
            else
            {
                ctx.DrawRectangle(TagFill, HoldownMissing, box);   // no hold-down strong enough: open red square
                ctx.DrawLine(HoldownMissing, box.TopLeft, box.BottomRight);
            }
        }

        private static FormattedText Text(string s, double size, Brush ink, double dip, bool bold = false) =>
            new FormattedText(s, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, bold ? FaceBold : Face, size * scale, ink, dip);

        /// <summary>The text size against the base sizes, for the drawing under way (set by <see cref="Draw"/>; the UI thread only).</summary>
        private static double scale = 1;

        private sealed class TagText
        {
            public FormattedText Mark, Holdown;
            public Size size;
            public double Gap;
        }

        private static TagText Tag(string mark, string holdown, double dip)
        {
            var t = new TagText
            {
                Mark = Text(mark, 11, Ui.Ink, dip, bold: true),
                Holdown = holdown == null ? null : Text(holdown, 10, holdown == "no HD" ? Ui.FailInk : Ui.InkSecondary, dip),
            };
            t.Gap = t.Holdown == null ? 0 : 5;
            t.size = new Size(t.Mark.Width + t.Gap + (t.Holdown?.Width ?? 0) + 10, Math.Max(t.Mark.Height, t.Holdown?.Height ?? 0) + 2);
            return t;
        }

        /// <summary>The tag beside the wall midpoint on side <paramref name="n"/>: centered over/under an E-W wall, beside a N-S wall.</summary>
        private static Rect TagBox(Point mid, Vector n, Size s, bool vertical)
        {
            if (vertical)
                return new Rect(n.X < 0 ? mid.X - TagOffset / 2 - s.Width : mid.X + TagOffset / 2, mid.Y - s.Height / 2, s.Width, s.Height);
            return new Rect(mid.X - s.Width / 2, mid.Y + n.Y * TagOffset - s.Height / 2, s.Width, s.Height);
        }

        /// <summary>"SW2 | HDU2" in a rounded box, outlined slate blue (passes), yellow (close to the limit) or dashed crimson (fails).</summary>
        private static void DrawTag(DrawingContext ctx, Rect box, TagText t, ShearWallCalculator.Design.DesignState state)
        {
            ctx.DrawRoundedRectangle(TagFill, state == ShearWallCalculator.Design.DesignState.Fail ? TagFail : state == ShearWallCalculator.Design.DesignState.Close ? TagClose : TagPass, box, 4, 4);
            ctx.DrawText(t.Mark, new Point(box.X + 5, box.Y + (box.Height - t.Mark.Height) / 2));
            if (t.Holdown != null)
            {
                double sx = box.X + 5 + t.Mark.Width + t.Gap / 2;
                ctx.DrawLine(HairlinePen, new Point(sx, box.Y + 3), new Point(sx, box.Bottom - 3));
                ctx.DrawText(t.Holdown, new Point(box.X + 5 + t.Mark.Width + t.Gap, box.Y + (box.Height - t.Holdown.Height) / 2));
            }
        }

        /// <summary>
        /// The shear wall schedule: mark, sheathing, capacity and walls; then the hold-down symbol. In the canvas corner
        /// that covers the least of the drawing (lower-left first).
        /// </summary>
        private static void DrawLegend(DrawingContext ctx, List<ScheduleEntry> schedule, Size view, Rect drawn, double dip, List<Rect> taken)
        {
            if (schedule.Count == 0) return;
            var lines = new List<(FormattedText mark, FormattedText text)>();
            foreach (var e in schedule.Take(LegendRows))
            {
                string walls = e.Holdown ?? "no hold-down"; // the mark names the wall; its hold-down completes its design
                string cap = e.Capacity > 0 ? $"{e.Capacity:0} plf" : "no capacity";
                lines.Add((Text(e.Mark, 11, Ui.Ink, dip, bold: true), Text($"{e.Sheathing}  ·  {cap}  ·  {walls}", 11, Ui.InkSecondary, dip)));
            }
            if (schedule.Count > LegendRows)
                lines.Add((Text("", 11, Ui.Ink, dip), Text($"+ {schedule.Count - LegendRows} more (Design tab)", 11, Ui.InkMuted, dip)));

            var title = Text("SHEAR WALL SCHEDULE", 10, Ui.InkMuted, dip, bold: true);
            var key = Text("hold-down at the post centerline (both ends)", 10, Ui.InkMuted, dip);
            double markW = lines.Max(l => l.mark.Width) + 8;
            // rows as tall as their text (the text size follows the plan's label scale)
            double rowH = lines.Max(l => Math.Max(l.mark.Height, l.text.Height)) + 1, pad = 8;
            double width = Math.Max(Math.Max(title.Width, key.Width + HoldownSize + 6), markW + lines.Max(l => l.text.Width)) + 2 * pad;
            width = Math.Min(width, Math.Max(120, view.Width - 20));
            double height = pad + title.Height + 2 + lines.Count * rowH + 4 + key.Height + pad;
            var box = LegendPlace(new Size(width, height), view, drawn);
            taken.Add(box);

            ctx.PushClip(new RectangleGeometry(box));
            ctx.DrawRoundedRectangle(LegendFill, HairlinePen, box, 4, 4);
            double y = box.Y + pad;
            ctx.DrawText(title, new Point(box.X + pad, y));
            y += title.Height + 2;
            foreach (var (mark, text) in lines)
            {
                ctx.DrawText(mark, new Point(box.X + pad, y));
                ctx.DrawText(text, new Point(box.X + pad + markW, y));
                y += rowH;
            }
            y += 4;
            DrawHoldown(ctx, new Point(box.X + pad + HoldownSize / 2, y + key.Height / 2), true);
            ctx.DrawText(key, new Point(box.X + pad + HoldownSize + 6, y));
            ctx.Pop();
        }

        /// <summary>
        /// The canvas corner where the schedule covers the least of <paramref name="drawn"/>; lower-left on a tie. Kept in
        /// from the edges past the plan's grid numbers (left and bottom) and the scale bar (bottom left).
        /// </summary>
        internal static Rect LegendPlace(Size s, Size view, Rect drawn)
        {
            const double m = 10, left = 34, bottom = 40, top = 40;
            var corners = new[]
            {
                new Rect(left, view.Height - s.Height - bottom, s.Width, s.Height),                // lower-left
                new Rect(left, top, s.Width, s.Height),                                            // upper-left (below the level caption)
                new Rect(view.Width - s.Width - m, top, s.Width, s.Height),                        // upper-right (below the level caption, which can be wide)
                new Rect(view.Width - s.Width - m, view.Height - s.Height - bottom, s.Width, s.Height), // lower-right
            };
            if (drawn.IsEmpty) return corners[0];
            double Covered(Rect r)
            {
                var i = Rect.Intersect(r, drawn);
                return i.IsEmpty ? 0 : i.Width * i.Height;
            }
            return corners.OrderBy(Covered).First(); // OrderBy is stable: lower-left wins ties
        }
    }
}
