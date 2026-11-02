using System;
using System.Collections.Generic;
using System.Linq;

namespace ShearWallCalculator
{
    /// <summary>
    /// Adding and deleting whole floors (levels) of a 1-3 story building: the levels (bottom first) and the story plate
    /// heights change together, so the floor elevations and the eave height (their sum) follow. Deleting a floor in the
    /// middle moves the floors above it down onto the floor below it: <see cref="DeleteConsequences"/> says what that does
    /// to the load path before the user confirms.
    /// </summary>
    public static class FloorEdits
    {
        public const int MaxFloors = 3;

        /// <summary>The plate heights with a new top story of <paramref name="plateHeight"/> (ft).</summary>
        public static List<double> PlatesAfterAdd(IList<double> plates, double plateHeight)
        {
            var list = plates?.ToList() ?? new List<double>();
            list.Add(plateHeight);
            return list;
        }

        /// <summary>The plate heights without story <paramref name="index"/> (0 = the first story); the stories above keep theirs.</summary>
        public static List<double> PlatesAfterDelete(IList<double> plates, int index)
        {
            var list = plates?.ToList() ?? new List<double>();
            if (index >= 0 && index < list.Count) list.RemoveAt(index);
            return list;
        }

        /// <summary>
        /// Removes level <paramref name="index"/> (0 = level 1); the levels above move down one. Refuses to remove the only
        /// level (it is cleared instead; see <see cref="Clear"/>).
        /// </summary>
        public static void Delete(IList<ShearWallCalculatorBase> levels, int index)
        {
            if (levels == null) throw new ArgumentNullException(nameof(levels));
            if (levels.Count <= 1) throw new InvalidOperationException("A building keeps at least one floor: clear it instead.");
            if (index < 0 || index >= levels.Count) throw new ArgumentOutOfRangeException(nameof(index));
            levels.RemoveAt(index);
        }

        /// <summary>Deletes every wall and diaphragm of a level. Returns how many.</summary>
        public static int Clear(ShearWallCalculatorBase level)
        {
            if (level == null) return 0;
            int n = level._wall_system._walls.Count + level._diaphragm_system._diaphragms.Count;
            level._wall_system._walls.Clear();
            level._diaphragm_system._diaphragms.Clear();
            return n;
        }

        /// <summary>What deleting level <paramref name="index"/> does, one line each, for the confirmation.</summary>
        public static List<string> DeleteConsequences(IList<ShearWallCalculatorBase> levels, int index, IList<double> plates)
        {
            var lines = new List<string>();
            if (levels == null || index < 0 || index >= levels.Count) return lines;
            int n = levels.Count;
            var level = levels[index];
            int walls = level._wall_system._walls.Count, diaphragms = level._diaphragm_system._diaphragms.Count;
            lines.Add($"Level {index + 1} has {walls} wall{(walls == 1 ? "" : "s")} and {diaphragms} diaphragm{(diaphragms == 1 ? "" : "s")}; they are deleted.");
            if (plates != null && index < plates.Count)
                lines.Add($"Its {plates[index]:0.##} ft story comes out: the eave drops from {plates.Sum():0.##} to {plates.Sum() - plates[index]:0.##} ft and the wind is shared again.");
            if (index == n - 1)
            {
                if (n > 1) lines.Add($"Level {index} becomes the top level: its diaphragm becomes the roof.");
                return lines;
            }
            // the levels above move down one
            for (int k = index + 1; k < n; k++)
                lines.Add($"Level {k + 1} becomes level {k}.");
            var upper = levels[index + 1];
            if (index == 0)
                lines.Add("The new level 1 stands on the foundation: its hold-downs anchor to the foundation.");
            else
            {
                var unsupported = StoryCascade.Unsupported(upper, levels[index - 1], index + 1);
                lines.Add(unsupported.Count == 0
                    ? $"Every wall of level {index + 2} stands on a wall of level {index}: the load path stays."
                    : $"{unsupported.Count} wall{(unsupported.Count == 1 ? "" : "s")} of level {index + 2} will have no wall under {(unsupported.Count == 1 ? "it" : "them")} on level {index} (check the Calculations tab afterwards).");
            }
            return lines;
        }
    }
}
