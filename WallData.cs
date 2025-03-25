using System;
using System.IO;
using System.Numerics;

namespace calculator
{
    public enum WallTypes
    {
        PortalFrame = 0,
        Type_01 = 1,
        Type_02 = 2,
        Type_03 = 3,
        Type_04 = 4,
        Type_05 = 5,
        Type_06 = 6,
        Type_07 = 7,
        Type_08 = 8
    }

    public enum WallDirs
    {
        EastWest = 0,
        NorthSouth = 1
    }

    /// <summary>
    /// Base class for all wall objects
    /// </summary>
    public class WallData
    {
        public int Id {get; set;}
        public System.Windows.Point Start { get; set; } // start point of wall...bottommost point at one end
        public System.Windows.Point End { get; set; } // end point of wall...bottommost point at other end
        public System.Windows.Point Center { get => GetCenterPoint(); } // center point of wall
        public float WallLength { get => GetLength(); }  // length of the wall
        public float WallHeight { get; set; } = 9;  // height of wall feet.
        public float WallRigidity { get => GetRigidity(); } // rigidity in direction of the wall
        public WallTypes WallType { get; set; } = WallTypes.Type_01;

        public WallDirs WallDir { get; set; }
        public Vector2 Dir { get => GetUnitDirection(); }

        public float Rxr { get => ComputeFirstMomentOfRigidity_X(); }  // first moment of rigidity for horizontal walls
        public float Ryr { get => ComputeFirstMomentOfRigidity_Y(); }  // first moment of rigidity for vertical walls

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="id">identifier for the wall segment</param>
        /// <param name="ht">height of the wall segment</param>
        /// <param name="sx">x-coord of start pt</param>
        /// <param name="sy">y-coord of start pt</param>
        /// <param name="ex">x-coord of end pt</param>
        /// <param name="ey">y-coord of end pt</param>
        /// <exception cref="ArgumentException"></exception>
        public WallData(float ht, float sx, float sy, float ex, float ey, WallDirs wallDir)
        {
            // sort the points so START is always on the left of a horizontal line
            if(wallDir == WallDirs.EastWest)
            {
                if(sx < ex)
                {
                    Start = new System.Windows.Point(sx, sy);
                    End = new System.Windows.Point(ex, ey);
                } else
                {
                    Start = new System.Windows.Point(ex, ey);
                    End = new System.Windows.Point(sx, sy);
                }
            }

            if (wallDir == WallDirs.NorthSouth)
            {
                if (sy < ey)
                {
                    Start = new System.Windows.Point(sx, sy);
                    End = new System.Windows.Point(ex, ey);
                }
                else
                {
                    Start = new System.Windows.Point(ex, ey);
                    End = new System.Windows.Point(sx, sy);
                }
            }

            WallHeight = ht;
            WallDir = wallDir;

            //TODO:: resolve this exceptions so that they return null -- and then remember to handle this in the functions that created the object.
            if(Start == End)
            {
                throw new ArgumentException("Start and end points cannot be the same.");
            }
        }

        private System.Windows.Point GetCenterPoint()
        {
            return new System.Windows.Point((float)(Start.X + End.X) / 2, (float)(Start.Y + End.Y) / 2);
        }

        /// <summary>
        /// Compute the distance from the start point to the end point
        /// </summary>
        /// <returns></returns>
        private float GetLength()
        {
            return (float) Math.Sqrt((End.X - Start.X) * (End.X - Start.X) + (End.Y - Start.Y) * (End.Y - Start.Y));
        }

        /// <summary>
        /// computes the cartesian unit vector from the start point to the end point
        /// </summary>
        /// <returns></returns>
        private Vector2 GetUnitDirection()
        {
            if(WallLength == 0)
            {
                return new Vector2(0, 0);
            }
            return new Vector2((float)(End.X - Start.X) / WallLength, (float)(End.Y - Start.Y) / WallLength);
        }

        /// <summary>
        /// Computes the rigidity of the wall segment in the line of the wall
        /// Rigidity = 1 / (0.4*(h/L)^3+0.3*(h/ L)
        /// where h = wall height
        /// L = wall length
        /// </summary>
        /// <returns></returns>
        private float GetRigidity()
        {
            if (WallLength == 0)
            {
                return 0;
            }

            return 1.0f / (0.4f * (WallHeight / WallLength) * (WallHeight / WallLength) * (WallHeight / WallLength) + 0.3f * (WallHeight / WallLength)); ;
        }

        /// <summary>
        /// Compute first y-moment of rigidity about global origin (0,0)
        /// </summary>
        /// <returns></returns>
        private float ComputeFirstMomentOfRigidity_X()
        {
            return (float)(Start.X + End.X) / 2 * WallRigidity;
        }

        /// <summary>
        /// Compute first x-moment of rigidity about global origin (0,0)
        /// </summary>
        /// <returns></returns>
        private float ComputeFirstMomentOfRigidity_Y()
        {
            return (float)(Start.Y + End.Y) / 2 * WallRigidity;
        }

        /// <summary>
        /// Creates a string for displaying the info of the wall
        /// </summary>
        /// <returns></returns>
        public string DisplayInfo()
        {
            string str = "";
            str += "\nWall ID: " + Id + "   WallDir: " + WallDir;
            str += "\nStart: " + Start.X + ", " + Start.Y + "  End: " + End.X + ", " + End.Y + "   UnitVec: <" + Dir.X + ", " + Dir.Y +">";
            str += "\nLength: " + WallLength + "  Height: " + WallHeight + "  Rigidity: " + WallRigidity;
            str += "\nRxr: " + Rxr + "  Ryr: " + Ryr;
            return str;
        }

        public void WriteToFile(string filename)
        {
            string str = "";
            str += Id.ToString() + " " + Start.X.ToString() + " " + Start.Y.ToString() + " " + End.X.ToString() + " " + End.Y.ToString() + " " + WallHeight.ToString() + " " + WallDir.ToString();

            using (StreamWriter sw = File.AppendText(filename))
            {
                sw.WriteLine(str);
            }
        }
    }
}
