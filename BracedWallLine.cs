using calculator;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace ShearWallCalculator
{
    /// <summary>
    /// Class to manager the braced wall groups.  
    /// </summary>
    public class BracedWallLineGroupManager
    {
        private const float DEFAULT_ALLOWABLE_BRACEDLINE_OFFSET = 8.0f;  // the minimum offset allowed for groupings of braced wall lines IRC requires 4ft max.
        private int _nextGroupNumber = 1;
        private float tolerance;
        


        public List<BracedWallLine> BracedWallLines { get; private set; } 
        public int GetNextGroupNumber() => _nextGroupNumber++;

        public BracedWallLineGroupManager(Dictionary<int, WallData> walls, float tolerance = DEFAULT_ALLOWABLE_BRACEDLINE_OFFSET)
        {
            this.tolerance = tolerance;
            BracedWallLines = new List<BracedWallLine>();
            RecomputeGroups(walls);
        }

        public List<BracedWallLine> GetEastWestWallLines()
        {
            List<BracedWallLine> result = new List<BracedWallLine>();

            foreach (BracedWallLine line in BracedWallLines)
            {
                if (line.WallDir == WallDirs.EastWest)
                {
                    result.Add(line);
                }
            }

            return result;
        }


        public List<BracedWallLine> GetNorthSouthWallLines()
        {
            List<BracedWallLine> result = new List<BracedWallLine>();

            foreach (BracedWallLine line in BracedWallLines)
            {
                if (line.WallDir == WallDirs.NorthSouth)
                {
                    result.Add(line);
                }
            }

            return result;
        }

        /// <summary>
        /// function to create gouprs of walls into defined braced wall lines
        /// </summary>
        /// <param name="walls"></param>
        private void RecomputeGroups(Dictionary<int, WallData> walls)
        { 
            BracedWallLines.Clear();

            if (walls == null || (walls.Count == 0))
            {
                return;
            }

            List<WallData> EW_Walls = new List<WallData>();
            List<WallData> NS_Walls = new List<WallData>();

            foreach (KeyValuePair<int, WallData> kvp in walls)
            {
                WallData wall = kvp.Value;
                if (wall.WallDir == WallDirs.EastWest)
                    EW_Walls.Add(wall);
                else if (wall.WallDir == WallDirs.NorthSouth)
                    NS_Walls.Add(wall);
            }

            // sort the EW walls
            var sorted_ew = EW_Walls.OrderBy(w => w.Center.Y).ToList();
            if (sorted_ew.Count > 0)
            {
                BracedWallLine currentLine = new BracedWallLine(GetNextGroupNumber(), WallDirs.EastWest);
                double groupCoord = sorted_ew[0].Center.Y;
                currentLine.AddWall(sorted_ew[0]);

                for (int i = 1; i < sorted_ew.Count; i++)
                {
                    double currentCoord = sorted_ew[i].Center.Y;

                    if (Math.Abs(currentCoord - groupCoord) <= tolerance)
                    {
                        currentLine.AddWall(sorted_ew[i]);
                    }
                    else
                    {
                        BracedWallLines.Add(currentLine);
                        groupCoord = currentCoord;

                        currentLine = new BracedWallLine(GetNextGroupNumber(), WallDirs.EastWest);
                        currentLine.AddWall(sorted_ew[i]);
                    }
                }

                if (currentLine.WallReferences.Count > 0)
                    BracedWallLines.Add(currentLine);
            }



            //// sort the NS walls
            List<WallData> sorted_ns = NS_Walls.OrderBy(w => w.Center.X).ToList();
            if (sorted_ns.Count > 0)
            {
                BracedWallLine currentLine = new BracedWallLine(GetNextGroupNumber(), WallDirs.NorthSouth);
                double groupCoord = sorted_ns[0].Center.X;
                currentLine.AddWall(sorted_ns[0]);

                for (int i = 1; i < sorted_ns.Count; i++)
                {
                    double currentCoord = sorted_ns[i].Center.X;

                    if (Math.Abs(currentCoord - groupCoord) <= tolerance)
                    {
                        currentLine.AddWall(sorted_ns[i]);
                    }
                    else
                    {
                        BracedWallLines.Add(currentLine);
                        groupCoord = currentCoord;

                        currentLine = new BracedWallLine(GetNextGroupNumber(), WallDirs.NorthSouth);
                        currentLine.AddWall(sorted_ns[i]);
                    }
                }

                if (currentLine.WallReferences.Count > 0)
                    BracedWallLines.Add(currentLine);
            }
        }
    }

    public class BracedWallLine
    {
        [JsonIgnore]
        public int GroupNumber { get; set; }   // unique group ID

        [JsonIgnore]
        public List<WallData> WallReferences { get; set; } = new List<WallData>();  // contains the ids for the WallData walls on this braced line

        [JsonIgnore]
        public WallDirs WallDir { get; }

        [JsonIgnore]
        public Point Center { get; set; }

        public BracedWallLine(int id, WallDirs direction)
        {
            GroupNumber = id;
            this.WallDir = direction;
            Update();
        }

        public void Update()
        {
            // find the center point of all the wall references on this braced line
            int count = WallReferences.Count;
            double x = 0;
            double y = 0;
            for (int i = 0; i < count; i++)
            {
                x += WallReferences[i].Center.X;
                y += WallReferences[i].Center.Y;
            }
            x /= count;
            y /= count;
            this.Center = new Point(x, y);
        }

        public void AddWall(WallData wall)
        {
            WallReferences.Add(wall);
            Update();
        }

        public void RemoveWall(WallData wall)
        {
            WallReferences.Remove(wall);
            Update();
        }

        //public void Clear()
        //{
        //    walls.Clear();
        //    groupedWalls.Clear();
        //}

        //public void Update()
        //{
        //    RecomputeGroups();
        //}

        //public List<List<WallData>> GetGroups()
        //{
        //    return groupedWalls;
        //}

        //public void PrintGroups()
        //{
        //    int groupIndex = 1;
        //    foreach (var group in groupedWalls)
        //    {
        //        Console.WriteLine($"Group {groupIndex++}:");
        //        foreach (var wall in group)
        //        {
        //            Console.WriteLine($"ID: {wall.ID}    Center: ({wall.Center.X:F2}, {wall.Center.Y:F2})");
        //        }
        //    }
        //}


        ///// <summary>
        ///// Retrieves the group (list of WallData) that contains the specified wall ID.
        ///// </summary>
        //public List<WallData> GetGroupByWallId(int wallId)
        //{
        //    return groupedWalls.FirstOrDefault(group => group.Any(w => w.ID == wallId));
        //}

        ///// <summary>
        ///// Retrieves the index of the group in groupedWalls that contains the specified wall ID.
        ///// </summary>
        //public int GetGroupIndexByWallId(int wallId)
        //{
        //    for (int i = 0; i < groupedWalls.Count; i++)
        //    {
        //        if (groupedWalls[i].Any(w => w.ID == wallId))
        //            return i;
        //    }
        //    return -1;
        //}

        ///// <summary>
        ///// Retrieves both the group and its index that contains the specified wall ID (C# 7.3 compatible).
        ///// </summary>
        //public Tuple<List<WallData>, int> GetGroupAndIndexByWallId(int wallId)
        //{
        //    for (int i = 0; i < groupedWalls.Count; i++)
        //    {
        //        var group = groupedWalls[i];
        //        if (group.Any(w => w.ID == wallId))
        //            return Tuple.Create(group, i);
        //    }
        //    return Tuple.Create<List<WallData>, int>(null, -1);
        //}

        //private void RecomputeGroups()
        //{
        //    if (walls.Count == 0)
        //    {
        //        groupedWalls.Clear();
        //        return;
        //    }

        //    groupedWalls.Clear();

        //    // Sort by the coordinate of interest
        //    var sorted = walls.OrderBy(w => WallDir == WallDirs.EastWest ? w.Center.Y : w.Center.X).ToList();

        //    List<WallData> currentGroup = new List<WallData> { sorted[0] };
        //    double groupCoord = WallDir == WallDirs.EastWest ? sorted[0].Center.Y : sorted[0].Center.X;

        //    BracedWallLineGroupManager manager = new BracedWallLineGroupManager();

        //    for (int i = 1; i < sorted.Count; i++)
        //    {
        //        double currentCoord = WallDir == WallDirs.EastWest ? sorted[i].Center.Y : sorted[i].Center.X;

        //        if (Math.Abs(currentCoord - groupCoord) <= tolerance)
        //        {
        //            currentGroup.Add(sorted[i]);
        //        }
        //        else
        //        {
        //            groupedWalls.Add(new List<WallData>(currentGroup));
        //            currentGroup.Clear();
        //            currentGroup.Add(sorted[i]);
        //            groupCoord = currentCoord;
        //        }
        //    }

        //    if (currentGroup.Count > 0)
        //        groupedWalls.Add(currentGroup);
        //}
    }
}
