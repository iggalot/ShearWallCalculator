using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using calculator;

namespace ShearWallCalculator.Design
{
    /// <summary>One line of the shear wall schedule: a mark (SW2-1, SW2-2, ...) for each wall, its own design.</summary>
    public sealed class ScheduleEntry
    {
        public string Mark { get; set; }

        /// <summary>The sheathing, as in the Design tab (e.g. "Structural I 15/32", 8d @ 4", both faces").</summary>
        public string Sheathing { get; set; }

        /// <summary>Design unit shear capacity of the wall, plf (its aspect factor included).</summary>
        public double Capacity { get; set; }

        /// <summary>The wall's hold-down model (null: none).</summary>
        public string Holdown { get; set; }

        public List<int> WallIds { get; } = new List<int>();
    }

    /// <summary>
    /// What the drawing shows of the design: the shear wall schedule (one mark per wall, its own design: SW{level}-{wall})
    /// and where the hold-downs sit at each wall end.
    /// </summary>
    public static class WallSchedule
    {
        /// <summary>
        /// Post size along the wall when the hold-down does not give one (in.): two 2x studs.
        /// </summary>
        public const double DefaultPostAlongWall = 3.0;

        /// <summary>
        /// One mark per wall, its own design (sheathing, nailing, hold-down), made of the wall's level and its number: wall 9
        /// drawn on the Level 2 plan is SW2-9 (unique across the project; it says which plan to look on and which wall).
        /// <paramref name="level"/>: the level part ("2" for the Level 2 plan; <see cref="LevelTag"/>). Null: SW9.
        /// Walls with no result are left out; in wall-number order.
        /// </summary>
        public static List<ScheduleEntry> Build(IReadOnlyDictionary<int, WallDesignResult> design, string level = null)
        {
            if (design == null) return new List<ScheduleEntry>();
            var list = new List<ScheduleEntry>();
            foreach (var r in design.Values.Where(r => r != null).OrderBy(r => r.WallId))
            {
                var e = new ScheduleEntry
                {
                    Mark = Mark(level, r.WallId),
                    Sheathing = r.Panel ?? "",
                    Capacity = double.IsNaN(r.Capacity) ? 0 : r.Capacity,
                    Holdown = r.Holdown?.Model,
                };
                e.WallIds.Add(r.WallId);
                list.Add(e);
            }
            return list;
        }

        /// <summary>
        /// The level part of a mark (SW2-1 is mark 1 on the Level 2 plan): the number ending the level's name, so the mark says
        /// which plan to look on; a level named otherwise gets its initial ("Roof": SWR-1), not its story number, which could
        /// be another level's name number. <paramref name="story"/> (0 = the first story) only when the name is empty.
        /// </summary>
        public static string LevelTag(string levelName, int story)
        {
            string name = (levelName ?? "").Trim();
            var m = System.Text.RegularExpressions.Regex.Match(name, @"(\d+)\s*$");
            if (m.Success) return m.Groups[1].Value;
            char initial = name.FirstOrDefault(char.IsLetterOrDigit);
            return initial != default(char) ? char.ToUpperInvariant(initial).ToString() : (story + 1).ToString();
        }

        /// <summary>A wall's mark: SW{level}-{wall number} (SW2-9: wall 9 on the Level 2 plan); no level: SW9.</summary>
        public static string Mark(string level, int wallId) => string.IsNullOrEmpty(level) ? $"SW{wallId}" : $"SW{level}-{wallId}";

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
