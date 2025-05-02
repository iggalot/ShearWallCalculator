using Newtonsoft.Json;
using ShearWallCalculator;
using System;
using System.Drawing;
using System.IO;
using System.Numerics;
using System.Windows.Media;

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
    [JsonObject(MemberSerialization.OptIn)]
    public class WallData : DrawableObject
    {
        public override int ID { get; set; }
        public override string Type => "Wall";
        [JsonProperty]
        public System.Windows.Point Start { get; set; } // start point of wall...bottommost point at one end
        [JsonProperty]
        public System.Windows.Point End { get; set; } // end point of wall...bottommost point at other end
        public System.Windows.Point Center { get => GetCenterPoint(); } // center point of wall
        public double WallLength { get => GetLength(); }  // length of the wall
        [JsonProperty]
        public double WallHeight { get; set; } = 9;  // height of wall feet.
        public double WallRigidity { get => GetRigidity(); } // rigidity in direction of the wall
        [JsonProperty]
        public WallTypes WallType { get; set; } = WallTypes.Type_01;
        [JsonProperty]
        public WallDirs WallDir { get; set; }
        public Vector2 Dir { get => GetUnitDirection(); }

        public double Rxr { get => ComputeFirstMomentOfRigidity_X(); }  // first moment of rigidity for horizontal walls
        public double Ryr { get => ComputeFirstMomentOfRigidity_Y(); }  // first moment of rigidity for vertical walls

        public WallData()
        {
                
        }
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
        public WallData(double ht, double sx, double sy, double ex, double ey)
        {
            if (ht <= 0)
            {
                throw new ArgumentException("Height must be greater than 0.");
            }



            // sort the points so START is always on the left of a horizontal line
            if (sy == ey)
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

            // else we are looking at a vertical line so make the START the bottom
            else
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

            //TODO:: resolve this exceptions so that they return null -- and then remember to handle this in the functions that created the object.
            if(Start == End)
            {
                throw new ArgumentException("Start and end points cannot be the same.");
            }

            // these have to be after the START and END are set
            WallHeight = ht;
            WallDir = GetWallDir();
        }

        /// <summary>
        /// Constructor that takes input data as Point to create the object
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        public WallData(double ht, Point start, Point end) : this(ht, start.X, start.Y, end.X, end.Y) { }


        private WallDirs GetWallDir()
        {
            if (Start.X == End.X)
            {
                return WallDirs.NorthSouth;
            }
            else
            {
                return WallDirs.EastWest;
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
        private double GetLength()
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
            return( new Vector2((float)((End.X - Start.X) / WallLength), (float)((End.Y - Start.Y) / WallLength)));
        }

        /// <summary>
        /// Computes the rigidity of the wall segment in the line of the wall
        /// Rigidity = 1 / (0.4*(h/L)^3+0.3*(h/ L)
        /// where h = wall height
        /// L = wall length
        /// </summary>
        /// <returns></returns>
        private double GetRigidity()
        {
            if (WallLength == 0)
            {
                return 0;
            }

            return 1.0f / (0.4f * (WallHeight / WallLength) * (WallHeight / WallLength) * (WallHeight / WallLength) + 0.3f * (WallHeight / WallLength)); ;
        }

        public void Update()
        {
            // Update stuff for walls here.
        }

        /// <summary>
        /// Compute first y-moment of rigidity about global origin (0,0)
        /// </summary>
        /// <returns></returns>
        private double ComputeFirstMomentOfRigidity_X()
        {
            return (Start.X + End.X) / 2 * WallRigidity;
        }

        /// <summary>
        /// Compute first x-moment of rigidity about global origin (0,0)
        /// </summary>
        /// <returns></returns>
        private double ComputeFirstMomentOfRigidity_Y()
        {
            return (Start.Y + End.Y) / 2 * WallRigidity;
        }

        /// <summary>
        /// Creates a string for displaying the info of the wall
        /// </summary>
        /// <returns></returns>
        public string DisplayInfo()
        {
            string str = "";
            str += "\nWallDir: " + WallDir;
            str += "\nStart: " + Start.X + ", " + Start.Y + "  End: " + End.X + ", " + End.Y + "   UnitVec: <" + Dir.X + ", " + Dir.Y +">";
            str += "\nLength: " + WallLength + "  Height: " + WallHeight + "  Rigidity: " + WallRigidity;
            str += "\nRxr: " + Rxr + "  Ryr: " + Ryr;
            return str;
        }
    }
}
