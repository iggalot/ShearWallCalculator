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

    //public class BracedWallLine
    //{
    //    private List<double> values; // Stores the wall positions
    //    private double tolerance; // Max allowed grouping distance
    //    public List<List<double>> groupedValues; // Stores computed groups

    //    //public List<List<WallData>> walls;
    //    //public List<List<WallData>> groupedWalls;

    //    public BracedWallLine(double tolerance)
    //    {
    //        this.values = new List<double>();
    //        this.tolerance = tolerance;
    //        this.groupedValues = new List<List<double>>();
    //    }

    //    // Add a value and recompute groups
    //    public void AddValue(double value)
    //    {
    //        values.Add(value);
    //        RecomputeGroups();
    //    }

    //    // Remove a value and recompute groups
    //    public void RemoveValue(double value)
    //    {
    //        values.Remove(value);
    //        RecomputeGroups();
    //    }

    //    // Clears all the list values for this braced wall line group
    //    public void Clear()
    //    {
    //        values.Clear();
    //        groupedValues.Clear();
    //    }

    //    public void Update()
    //    {
    //        RecomputeGroups();
    //    }

    //    // Recomputes the grouping of wall positions
    //    private void RecomputeGroups()
    //    {
    //        if (values.Count == 0)
    //        {
    //            groupedValues.Clear();
    //            return;
    //        }

    //        values.Sort(); // Ensure sorted order
    //        groupedValues.Clear();
    //        List<double> currentGroup = new List<double> { values[0] };
    //        double groupStart = values[0]; // First element of the group

    //        for (int i = 1; i < values.Count; i++)
    //        {
    //            if (Math.Abs(values[i] - groupStart) <= tolerance)
    //            {
    //                currentGroup.Add(values[i]);
    //            }
    //            else
    //            {
    //                groupedValues.Add(new List<double>(currentGroup));
    //                currentGroup.Clear();
    //                currentGroup.Add(values[i]);
    //                groupStart = values[i];
    //            }
    //        }

    //        if (currentGroup.Count > 0)
    //            groupedValues.Add(currentGroup);
    //    }

    //    // Get the current groups
    //    public List<List<double>> GetGroups()
    //    {
    //        return groupedValues;
    //    }

    //    // Display the groups
    //    public void PrintGroups()
    //    {
    //        int groupIndex = 1;
    //        foreach (var group in groupedValues)
    //        {
    //            Console.WriteLine($"Group {groupIndex++}: {string.Join(", ", group)}");
    //        }
    //    }

    //    //public void RunTestCase()
    //    //{
    //    //    BracedWallLine bracedWallLine = new BracedWallLine(5.0); // Set tolerance

    //    //    // Initial values with modifications
    //    //    bracedWallLine.AddValue(10);
    //    //    bracedWallLine.AddValue(13);
    //    //    bracedWallLine.AddValue(15);  // Changed from 17
    //    //    bracedWallLine.AddValue(22);  // Changed from 18
    //    //    bracedWallLine.AddValue(27);
    //    //    bracedWallLine.AddValue(35);
    //    //    bracedWallLine.AddValue(37);
    //    //    bracedWallLine.AddValue(45);

    //    //    // Print results
    //    //    Console.WriteLine("After Changing 17 → 15 and 18 → 22:");
    //    //    bracedWallLine.PrintGroups();
    //    //}
    //}
}
