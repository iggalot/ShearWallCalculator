using System;
using System.Linq;
using System.Windows;
using calculator;
using Newtonsoft.Json;

namespace ShearWallCalculator.Drawing
{
    /// <summary>
    /// Drafting edits on the drawing: delete, move and copy a wall or diaphragm, the extents of the drawing, and the
    /// horizontal / vertical (ortho) constraint. <c>kind</c> is "Wall" or "Diaphragm", as in the selection.
    /// </summary>
    public static class DrawingEdits
    {
        /// <summary>Deletes the wall or diaphragm. Returns false when it does not exist.</summary>
        public static bool Delete(ShearWallCalculatorBase calc, string kind, int id)
        {
            if (kind == "Wall" && calc?._wall_system?._walls.ContainsKey(id) == true) { calc._wall_system.DeleteWall(id); return true; }
            if (kind == "Diaphragm" && calc?._diaphragm_system?._diaphragms.ContainsKey(id) == true) { calc._diaphragm_system.DeleteDiaphragm(id); return true; }
            return false;
        }

        /// <summary>Moves the wall or diaphragm by <paramref name="by"/> (ft). Its id and design stay. Returns false when it does not exist.</summary>
        public static bool Move(ShearWallCalculatorBase calc, string kind, int id, Vector by)
        {
            if (kind == "Wall" && calc?._wall_system?._walls.TryGetValue(id, out var w) == true)
            {
                w.Start += by;
                w.End += by;
                w.Update();
                calc._wall_system.Update();
                return true;
            }
            if (kind == "Diaphragm" && calc?._diaphragm_system?._diaphragms.TryGetValue(id, out var d) == true)
            {
                d.P1 += by; d.P2 += by; d.P3 += by; d.P4 += by;
                d.Update();
                calc._diaphragm_system.Update();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Adds a copy of the wall or diaphragm moved by <paramref name="by"/> (ft): same height, type and design (a wall),
        /// same type (a diaphragm). Returns the new id, or -1 when the original does not exist.
        /// </summary>
        public static int Copy(ShearWallCalculatorBase calc, string kind, int id, Vector by)
        {
            if (kind == "Wall" && calc?._wall_system?._walls.TryGetValue(id, out var w) == true)
            {
                var copy = Clone(w);
                copy.Start = w.Start + by;
                copy.End = w.End + by;
                calc._wall_system.AddWall(copy);
                return copy.ID;
            }
            if (kind == "Diaphragm" && calc?._diaphragm_system?._diaphragms.TryGetValue(id, out var d) == true)
            {
                var copy = Clone(d);
                copy.P1 = d.P1 + by; copy.P2 = d.P2 + by; copy.P3 = d.P3 + by; copy.P4 = d.P4 + by;
                calc._diaphragm_system.AddDiaphragm(copy);
                return copy.ID;
            }
            return -1;
        }

        /// <summary>The base point offered for a move or copy: a wall's start, a diaphragm's lower-left corner.</summary>
        public static Point? BasePoint(ShearWallCalculatorBase calc, string kind, int id)
        {
            if (kind == "Wall" && calc?._wall_system?._walls.TryGetValue(id, out var w) == true) return w.Start;
            if (kind == "Diaphragm" && calc?._diaphragm_system?._diaphragms.TryGetValue(id, out var d) == true) return d.P1;
            return null;
        }

        /// <summary>The middle of a wall, the centroid of a diaphragm; null when it does not exist.</summary>
        public static Point? Center(ShearWallCalculatorBase calc, string kind, int id)
        {
            if (kind == "Wall" && calc?._wall_system?._walls.TryGetValue(id, out var w) == true) return new Point((w.Start.X + w.End.X) / 2, (w.Start.Y + w.End.Y) / 2);
            if (kind == "Diaphragm" && calc?._diaphragm_system?._diaphragms.TryGetValue(id, out var d) == true) return new Point((d.P1.X + d.P3.X) / 2, (d.P1.Y + d.P3.Y) / 2);
            return null;
        }

        /// <summary>The box around every wall and diaphragm (and the plan, when given and shown); <see cref="Rect.Empty"/> when there is nothing.</summary>
        public static Rect Extents(ShearWallCalculatorBase calc, bool includePlan)
        {
            Rect r = Rect.Empty;
            foreach (var w in calc?._wall_system?._walls.Values ?? Enumerable.Empty<WallData>())
            {
                r.Union(w.Start);
                r.Union(w.End);
            }
            foreach (var d in calc?._diaphragm_system?._diaphragms.Values ?? Enumerable.Empty<DiaphragmData_Rectangular>())
            {
                r.Union(d.P1);
                r.Union(d.P3);
            }
            var img = calc?.Image;
            if (includePlan && img != null && img.Visible)
            {
                var b = img.WorldBounds();
                if (!b.IsEmpty) r.Union(b);
            }
            return r;
        }

        /// <summary>The point squared to <paramref name="from"/>: straight across or straight up, whichever is closer.</summary>
        public static Point Ortho(Point p, Point from) =>
            Math.Abs(p.X - from.X) >= Math.Abs(p.Y - from.Y) ? new Point(p.X, from.Y) : new Point(from.X, p.Y);

        private static T Clone<T>(T item) => JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(item),
            new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace });
    }
}
