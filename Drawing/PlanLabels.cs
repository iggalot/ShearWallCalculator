using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace ShearWallCalculator.Drawing
{
    /// <summary>Where the plan's hold-down landing labels go, and what they say (pure: unit-tested).</summary>
    public static class PlanLabels
    {
        /// <summary>"beam BM2 end" → "BM2", "column C3 base" → "C3": what a beam end or column base is, without the words.</summary>
        public static string ShortFrom(string from)
        {
            string s = from ?? "";
            foreach (var w in new[] { "beam ", "column " }) if (s.StartsWith(w)) s = s.Substring(w.Length);
            foreach (var w in new[] { " end", " start", " base" }) if (s.EndsWith(w)) s = s.Substring(0, s.Length - w.Length);
            return s;
        }

        /// <summary>
        /// Where a label of <paramref name="size"/> for <paramref name="p"/> goes outside <paramref name="outline"/>: past a
        /// side, by <paramref name="gap"/>, centred on the point along it and slid along it to stay on <paramref name="canvas"/>.
        /// The side nearest the point when the label fits there, else the next nearest (a corner: above or below, where a wide
        /// label has room); with no side fitting, the nearest. Returns the box and the outward direction.
        /// </summary>
        public static (Rect box, Vector outward) OutsideSpot(Point p, Rect outline, Size size, double gap, Rect canvas)
        {
            var sides = new List<(double dist, int order, Rect box, Vector outward)>
            {
                (p.Y - outline.Top, 0, new Rect(p.X - size.Width / 2, outline.Top - gap - size.Height, size.Width, size.Height), new Vector(0, -1)),
                (outline.Bottom - p.Y, 1, new Rect(p.X - size.Width / 2, outline.Bottom + gap, size.Width, size.Height), new Vector(0, 1)),
                (p.X - outline.Left, 2, new Rect(outline.Left - gap - size.Width, p.Y - size.Height / 2, size.Width, size.Height), new Vector(-1, 0)),
                (outline.Right - p.X, 3, new Rect(outline.Right + gap, p.Y - size.Height / 2, size.Width, size.Height), new Vector(1, 0)),
            };
            var ordered = sides.OrderBy(s => Math.Round(s.dist * 2) / 2).ThenBy(s => s.order).ToList();
            foreach (var s in ordered)
            {
                var b = s.box;
                if (s.outward.Y != 0) b.X = Clamp(b.X, canvas.Left, canvas.Right - b.Width);
                else b.Y = Clamp(b.Y, canvas.Top, canvas.Bottom - b.Height);
                if (canvas.Contains(b)) return (b, s.outward);
            }
            return (ordered[0].box, ordered[0].outward);
        }

        /// <summary>
        /// The leader from the label to <paramref name="p"/>, as points from the label's edge to the point. A label past the
        /// point's nearest side: straight. Otherwise it first steps out past that side by <paramref name="off"/> and runs
        /// outside the building (round its corner to a label past the next side), so it never lies along a wall or beam on
        /// the outline nor crosses the building.
        /// </summary>
        public static Point[] Leader(Point p, Rect outline, Rect box, double off)
        {
            double dl = p.X - outline.Left, dr = outline.Right - p.X, dt = p.Y - outline.Top, db = outline.Bottom - p.Y;
            double min = Math.Min(Math.Min(dt, db), Math.Min(dl, dr));
            Point elbow; bool past, upright; // upright: the point's side runs up and down (left or right)
            if (dt <= min + 0.5) { elbow = new Point(p.X, outline.Top - off); past = box.Bottom <= outline.Top; upright = false; }
            else if (db <= min + 0.5) { elbow = new Point(p.X, outline.Bottom + off); past = box.Top >= outline.Bottom; upright = false; }
            else if (dl <= min + 0.5) { elbow = new Point(outline.Left - off, p.Y); past = box.Right <= outline.Left; upright = true; }
            else { elbow = new Point(outline.Right + off, p.Y); past = box.Left >= outline.Right; upright = true; }
            Point Near(Point q) => new Point(Clamp(q.X, box.Left, box.Right), Clamp(q.Y, box.Top, box.Bottom));
            if (past) return new[] { Near(p), p };
            // the label past a side next to the point's: round the corner between them, outside
            Point? corner = null;
            if (upright && box.Top >= outline.Bottom) corner = new Point(elbow.X, outline.Bottom + off);
            else if (upright && box.Bottom <= outline.Top) corner = new Point(elbow.X, outline.Top - off);
            else if (!upright && box.Left >= outline.Right) corner = new Point(outline.Right + off, elbow.Y);
            else if (!upright && box.Right <= outline.Left) corner = new Point(outline.Left - off, elbow.Y);
            if (corner is Point c)
            {
                var start = Near(c);
                return (start - c).Length < 0.5 ? new[] { c, elbow, p } : new[] { start, c, elbow, p };
            }
            return new[] { Near(elbow), elbow, p };
        }

        /// <summary>
        /// A beam end's reaction label ("(2.7k)"), turned along the beam by <paramref name="angle"/> degrees (the beam's own
        /// label angle): its middle just inside the end at <paramref name="end"/> (screen), <paramref name="gap"/> px in from
        /// it, on the side away from the beam's name. Also the point a connection label's arrow ends at: on the beam, just
        /// inside this end, so two beams meeting at one column each point at their own end.
        /// </summary>
        public static (Point center, Point tip) BeamEnd(Point end, Point other, Size text, double gap, double angle, bool nameSide = false)
        {
            var u = other - end;
            if (u.Length < 1e-9) return (end, end);
            u.Normalize();
            double t = angle * Math.PI / 180;
            var up = new Vector(Math.Sin(t), -Math.Cos(t)); // above a level beam, left of an upright one (its name is on the other side)
            if (nameSide) up = -up; // the other side, when this one is taken
            var center = end + u * (gap + text.Width / 2) + up * (text.Height / 2 + 3);
            return (center, end + u * 6);
        }

        private static double Clamp(double v, double lo, double hi) => hi < lo ? lo : Math.Max(lo, Math.Min(hi, v));
    }
}
