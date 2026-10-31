using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace ShearWallCalculator.Drawing
{
    /// <summary>The object snaps (like AutoCAD OSNAP). Several can be on at once.</summary>
    [Flags]
    public enum SnapModes
    {
        None = 0,
        Endpoint = 1,
        Midpoint = 2,
        Intersection = 4,
        Perpendicular = 8,
        Nearest = 16,
        Default = Endpoint | Midpoint | Intersection | Perpendicular,
    }

    /// <summary>The snap found under the cursor.</summary>
    public struct SnapResult
    {
        public SnapModes Kind;   // one mode, or None when nothing is in reach
        public Point Point;      // the snapped point (the cursor itself when Kind is None)

        public bool Found => Kind != SnapModes.None;

        public static string Describe(SnapModes kind)
        {
            switch (kind)
            {
                case SnapModes.Endpoint: return "Endpoint";
                case SnapModes.Midpoint: return "Midpoint";
                case SnapModes.Intersection: return "Intersection";
                case SnapModes.Perpendicular: return "Perpendicular";
                case SnapModes.Nearest: return "Nearest";
                default: return "";
            }
        }
    }

    /// <summary>A straight piece of the drawing that can be snapped to: a wall, or one edge of a diaphragm.</summary>
    public struct SnapSegment
    {
        public Point A, B;
        public SnapSegment(Point a, Point b) { A = a; B = b; }
    }

    /// <summary>
    /// Finds the object snap nearest the cursor. Within the tolerance (world units), the kinds rank: endpoint,
    /// intersection, midpoint, perpendicular (needs the point the line starts from), nearest. Among candidates of the
    /// same kind the closest wins.
    /// </summary>
    public static class Snapper
    {
        public static SnapResult Find(Point cursor, double tolerance, IList<SnapSegment> segments, SnapModes modes, Point? from = null)
        {
            if (modes == SnapModes.None || segments == null || segments.Count == 0 || !(tolerance > 0))
                return new SnapResult { Kind = SnapModes.None, Point = cursor };

            foreach (var kind in new[] { SnapModes.Endpoint, SnapModes.Intersection, SnapModes.Midpoint, SnapModes.Perpendicular, SnapModes.Nearest })
            {
                if ((modes & kind) == 0) continue;
                Point? best = null;
                double bestD = tolerance;
                foreach (var p in Candidates(kind, cursor, segments, from))
                {
                    double d = (p - cursor).Length;
                    if (d <= bestD) { bestD = d; best = p; }
                }
                if (best.HasValue) return new SnapResult { Kind = kind, Point = best.Value };
            }
            return new SnapResult { Kind = SnapModes.None, Point = cursor };
        }

        private static IEnumerable<Point> Candidates(SnapModes kind, Point cursor, IList<SnapSegment> segments, Point? from)
        {
            switch (kind)
            {
                case SnapModes.Endpoint:
                    foreach (var s in segments) { yield return s.A; yield return s.B; }
                    break;
                case SnapModes.Midpoint:
                    foreach (var s in segments) yield return new Point((s.A.X + s.B.X) / 2, (s.A.Y + s.B.Y) / 2);
                    break;
                case SnapModes.Intersection:
                    for (int i = 0; i < segments.Count; i++)
                        for (int j = i + 1; j < segments.Count; j++)
                            if (Intersect(segments[i], segments[j], out var x)) yield return x;
                    break;
                case SnapModes.Perpendicular:
                    if (from.HasValue)
                        foreach (var s in segments)
                            if (FootOnSegment(from.Value, s, out var f)) yield return f;
                    break;
                case SnapModes.Nearest:
                    foreach (var s in segments) yield return ClosestOnSegment(cursor, s);
                    break;
            }
        }

        /// <summary>The crossing point of two segments (ends included). Parallel or overlapping segments have none.</summary>
        public static bool Intersect(SnapSegment s, SnapSegment t, out Point p)
        {
            p = default(Point);
            Vector r = s.B - s.A, q = t.B - t.A;
            double den = Vector.CrossProduct(r, q);
            if (Math.Abs(den) < 1e-12) return false;
            Vector w = t.A - s.A;
            double u = Vector.CrossProduct(w, q) / den;   // along s
            double v = Vector.CrossProduct(w, r) / den;   // along t
            const double e = 1e-9;
            if (u < -e || u > 1 + e || v < -e || v > 1 + e) return false;
            p = s.A + u * r;
            return true;
        }

        /// <summary>The foot of the perpendicular from <paramref name="p"/> to the segment, when it falls on the segment.</summary>
        public static bool FootOnSegment(Point p, SnapSegment s, out Point foot)
        {
            Vector d = s.B - s.A;
            double len2 = d.LengthSquared;
            foot = s.A;
            if (len2 < 1e-18) return false;
            double u = Vector.Multiply(p - s.A, d) / len2;
            if (u < 0 || u > 1) return false;
            foot = s.A + u * d;
            return true;
        }

        public static Point ClosestOnSegment(Point p, SnapSegment s)
        {
            Vector d = s.B - s.A;
            double len2 = d.LengthSquared;
            if (len2 < 1e-18) return s.A;
            double u = Math.Max(0, Math.Min(1, Vector.Multiply(p - s.A, d) / len2));
            return s.A + u * d;
        }

        /// <summary>Distance from a point to a segment.</summary>
        public static double DistanceToSegment(Point p, SnapSegment s) => (p - ClosestOnSegment(p, s)).Length;

        /// <summary>
        /// What a click picks: the closest wall within the tolerance, else the smallest diaphragm containing the point.
        /// Returns ("Wall" / "Diaphragm", id), or (null, -1).
        /// </summary>
        public static (string kind, int id) Pick(ShearWallCalculatorBase calc, Point p, double tolerance)
        {
            if (calc?._wall_system?._walls != null)
            {
                var wall = calc._wall_system._walls
                    .Select(kv => (kv.Key, d: DistanceToSegment(p, new SnapSegment(kv.Value.Start, kv.Value.End))))
                    .Where(x => x.d <= tolerance)
                    .OrderBy(x => x.d).ThenBy(x => x.Key)
                    .Select(x => (int?)x.Key).FirstOrDefault();
                if (wall.HasValue) return ("Wall", wall.Value);
            }
            if (calc?._diaphragm_system?._diaphragms != null)
            {
                var dia = calc._diaphragm_system._diaphragms
                    .Where(kv => p.X >= Math.Min(kv.Value.P1.X, kv.Value.P3.X) && p.X <= Math.Max(kv.Value.P1.X, kv.Value.P3.X) &&
                                 p.Y >= Math.Min(kv.Value.P1.Y, kv.Value.P3.Y) && p.Y <= Math.Max(kv.Value.P1.Y, kv.Value.P3.Y))
                    .OrderBy(kv => kv.Value.Area).ThenBy(kv => kv.Key)
                    .Select(kv => (int?)kv.Key).FirstOrDefault();
                if (dia.HasValue) return ("Diaphragm", dia.Value);
            }
            return (null, -1);
        }

        /// <summary>The snap segments of a model: every wall, and the four edges of every diaphragm.</summary>
        public static List<SnapSegment> SegmentsOf(ShearWallCalculatorBase calc)
        {
            var list = new List<SnapSegment>();
            if (calc?._wall_system?._walls != null)
                list.AddRange(calc._wall_system._walls.Values.Select(w => new SnapSegment(w.Start, w.End)));
            if (calc?._diaphragm_system?._diaphragms != null)
                foreach (var d in calc._diaphragm_system._diaphragms.Values)
                {
                    list.Add(new SnapSegment(d.P1, d.P2));
                    list.Add(new SnapSegment(d.P2, d.P3));
                    list.Add(new SnapSegment(d.P3, d.P4));
                    list.Add(new SnapSegment(d.P4, d.P1));
                }
            return list;
        }
    }
}
