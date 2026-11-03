using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using calculator;

namespace ShearWallCalculator.Design
{
    /// <summary>One line of the shear wall schedule: a mark (SW1, SW2, ...) for each different construction.</summary>
    public sealed class ScheduleEntry
    {
        public string Mark { get; set; }

        /// <summary>The sheathing, as in the Design tab (e.g. "Structural I 15/32", 8d @ 4", both faces").</summary>
        public string Sheathing { get; set; }

        /// <summary>Design capacity of this construction, plf (the smallest over its walls: the aspect factor can lower it).</summary>
        public double Capacity { get; set; }

        public List<int> WallIds { get; } = new List<int>();
    }

    /// <summary>
    /// What the drawing shows of the design: the shear wall schedule (one mark per sheathing construction, weakest
    /// first, as on a drawing set) and where the hold-downs sit at each wall end.
    /// </summary>
    public static class WallSchedule
    {
        /// <summary>
        /// Post size along the wall when the hold-down does not give one (in.): two 2x studs.
        /// </summary>
        public const double DefaultPostAlongWall = 3.0;

        /// <summary>
        /// Groups the walls by sheathing construction and numbers the groups SW1, SW2, ... from the weakest (lowest
        /// design capacity of any of its walls) up; ties by name. Walls with no result are left out.
        /// <paramref name="level"/>: the level the walls are drawn on ("2" for the Level 2 plan); the marks are then
        /// SW2-1, SW2-2, ..., unique across the project and saying which plan to look on. Null: SW1, SW2, ...
        /// </summary>
        public static List<ScheduleEntry> Build(IReadOnlyDictionary<int, WallDesignResult> design, string level = null)
        {
            if (design == null) return new List<ScheduleEntry>();
            var groups = design.Values
                .Where(r => r != null)
                .GroupBy(r => r.Panel ?? "")
                .Select(g => new
                {
                    Sheathing = g.Key,
                    Capacity = g.Min(r => double.IsNaN(r.Capacity) ? 0 : r.Capacity),
                    Ids = g.Select(r => r.WallId).OrderBy(i => i).ToList(),
                })
                .OrderBy(g => g.Capacity).ThenBy(g => g.Sheathing, StringComparer.Ordinal)
                .ToList();

            var list = new List<ScheduleEntry>();
            int n = 0;
            foreach (var g in groups)
            {
                var e = new ScheduleEntry { Mark = string.IsNullOrEmpty(level) ? "SW" + (++n) : $"SW{level}-{++n}", Sheathing = g.Sheathing, Capacity = g.Capacity };
                e.WallIds.AddRange(g.Ids);
                list.Add(e);
            }
            return list;
        }

        /// <summary>The schedule mark of each wall.</summary>
        public static Dictionary<int, string> MarksByWall(IEnumerable<ScheduleEntry> schedule)
        {
            var marks = new Dictionary<int, string>();
            foreach (var e in schedule)
                foreach (int id in e.WallIds) marks[id] = e.Mark;
            return marks;
        }

        /// <summary>
        /// Post dimension along the wall (in.): the first of the hold-down's minimum member sizes (Simpson lists the
        /// member as "along the wall × wall depth", e.g. 3 × 3.5 for two 2x studs in a 2x4 wall); two 2x studs otherwise.
        /// </summary>
        public static double PostAlongWall(HoldownChoice h) =>
            h?.WoodMinHeight is double d && d > 0 ? d : DefaultPostAlongWall;

        /// <summary>
        /// Hold-down locations (world ft) at both ends of the wall, each inset from the wall end by half the post, so the
        /// marks sit on the post centerlines. Wind reverses, so both ends get one.
        /// </summary>
        public static (Point start, Point end) HoldownPoints(WallData wall, HoldownChoice h)
        {
            double L = wall.WallLength;
            if (L <= 0) return (wall.Start, wall.End);
            double inset = Math.Min(PostAlongWall(h) / 12.0 / 2.0, L / 2.0);
            var u = new Vector((wall.End.X - wall.Start.X) / L, (wall.End.Y - wall.Start.Y) / L);
            return (wall.Start + u * inset, wall.End - u * inset);
        }
    }
}
