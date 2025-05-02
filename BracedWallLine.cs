using calculator;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ShearWallCalculator
{
    public class BracedWallLine
    {
        private List<WallData> walls;
        private double tolerance;
        public List<List<WallData>> groupedWalls;

        public WallDirs WallDir { get; }

        public BracedWallLine(double tolerance, WallDirs direction)
        {
            this.walls = new List<WallData>();
            this.tolerance = tolerance;
            this.groupedWalls = new List<List<WallData>>();
            this.WallDir = direction;
        }

        public void AddWall(WallData wall)
        {
            walls.Add(wall);
            RecomputeGroups();
        }

        public void RemoveWall(WallData wall)
        {
            walls.Remove(wall);
            RecomputeGroups();
        }

        public void Clear()
        {
            walls.Clear();
            groupedWalls.Clear();
        }

        public void Update()
        {
            RecomputeGroups();
        }

        public List<List<WallData>> GetGroups()
        {
            return groupedWalls;
        }

        public void PrintGroups()
        {
            int groupIndex = 1;
            foreach (var group in groupedWalls)
            {
                Console.WriteLine($"Group {groupIndex++}:");
                foreach (var wall in group)
                {
                    Console.WriteLine($"ID: {wall.ID}    Center: ({wall.Center.X:F2}, {wall.Center.Y:F2})");
                }
            }
        }


        /// <summary>
        /// Retrieves the group (list of WallData) that contains the specified wall ID.
        /// </summary>
        public List<WallData> GetGroupByWallId(int wallId)
        {
            return groupedWalls.FirstOrDefault(group => group.Any(w => w.ID == wallId));
        }

        /// <summary>
        /// Retrieves the index of the group in groupedWalls that contains the specified wall ID.
        /// </summary>
        public int GetGroupIndexByWallId(int wallId)
        {
            for (int i = 0; i < groupedWalls.Count; i++)
            {
                if (groupedWalls[i].Any(w => w.ID == wallId))
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Retrieves both the group and its index that contains the specified wall ID (C# 7.3 compatible).
        /// </summary>
        public Tuple<List<WallData>, int> GetGroupAndIndexByWallId(int wallId)
        {
            for (int i = 0; i < groupedWalls.Count; i++)
            {
                var group = groupedWalls[i];
                if (group.Any(w => w.ID == wallId))
                    return Tuple.Create(group, i);
            }
            return Tuple.Create<List<WallData>, int>(null, -1);
        }

        private void RecomputeGroups()
        {
            if (walls.Count == 0)
            {
                groupedWalls.Clear();
                return;
            }

            groupedWalls.Clear();

            // Sort by the coordinate of interest
            var sorted = walls.OrderBy(w => WallDir == WallDirs.EastWest ? w.Center.Y : w.Center.X).ToList();

            List<WallData> currentGroup = new List<WallData> { sorted[0] };
            double groupCoord = WallDir == WallDirs.EastWest ? sorted[0].Center.Y : sorted[0].Center.X;

            for (int i = 1; i < sorted.Count; i++)
            {
                double currentCoord = WallDir == WallDirs.EastWest ? sorted[i].Center.Y : sorted[i].Center.X;

                if (Math.Abs(currentCoord - groupCoord) <= tolerance)
                {
                    currentGroup.Add(sorted[i]);
                }
                else
                {
                    groupedWalls.Add(new List<WallData>(currentGroup));
                    currentGroup.Clear();
                    currentGroup.Add(sorted[i]);
                    groupCoord = currentCoord;
                }
            }

            if (currentGroup.Count > 0)
                groupedWalls.Add(currentGroup);
        }
    }
}
