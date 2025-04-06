using calculator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ShearWallCalculator
{
    /// <summary>
    /// Contains data for a single wall system of multiple walls
    /// </summary>
    public class WallSystem
    {
        private float DEFAULT_ALLOWABLE_BRACEDLINE_OFFSET = 8.0f;  // the minimum offset allowed for groupings of braced wall lines IRC requires 4ft max.

        // A collection of all walls defined in the system
        public Dictionary<int, WallData> _walls { get; set; } = new Dictionary<int, WallData>(); // collection of walls>


        // sorted collection of walls in East West direction (horizontal on screen)
        public Dictionary<int, WallData> EW_Walls { get; set; } = new Dictionary<int, WallData>();

        // sorted collection of walls in North South direction (vertical on screen)
        public Dictionary<int, WallData> NS_Walls { get; set; } = new Dictionary<int, WallData>();

        // braced wall line groups
        public BracedWallLine BracedWallGroups_EW { get; set; }
        public BracedWallLine BracedWallGroups_NS { get; set; }

        // distance from center of wall to center of rigidity in y-direction
        public Dictionary<int, double> Y_bar_walls { get; set; } = new Dictionary<int, double>();

        // distance from center of wall to center of rigidity in x-direction
        public Dictionary<int, double> X_bar_walls { get; set; } = new Dictionary<int, double>();


        public double TotalRigidity_X { get; set; } = 0;  // total rigidity in x-direction
        public double TotalRigidity_Y { get; set; } = 0;  // total rigidity in y-direction

        // center or rigidity
        public System.Windows.Point CtrRigidity { get; set; } = new System.Windows.Point(0, 0);

        // moments of inertia for the shear wall groups
        public double InertiaXX { get; set; } // inertia of horizontal walls about center of rigidity
        public double InertiaYY { get; set; } // inertia of vertical walls about center of rigidity
        public double InertiaPolar { get; set; } // polar moment of all walls about center of rigidity


        /// <summary>
        /// Returns the minimum (leftmost) X value of all wall points
        /// </summary>
        public System.Windows.Point X_MIN
        {
            get
            {
                if (_walls.Count == 0) return new System.Windows.Point(1000000, 1000000);

                System.Windows.Point minpt = new System.Windows.Point(1000000, 1000000);

                foreach (WallData item in _walls.Values)
                {
                    System.Windows.Point start = item.Start;
                    System.Windows.Point end = item.End;

                    if(start.X < minpt.X) minpt = start;
                    if(end.X < minpt.X) minpt = end;
                }
                return minpt;
            }
        }
        /// <summary>
        /// Returns a point with the maximum (rightmost) X value of all diaphragm points
        /// </summary>
        public System.Windows.Point X_MAX
        {
            get
            {
                if (_walls.Count == 0) return new System.Windows.Point(-1000000, -1000000);

                System.Windows.Point maxpt = new System.Windows.Point(-1000000, -1000000);

                foreach (WallData item in _walls.Values)
                {
                    System.Windows.Point start = item.Start;
                    System.Windows.Point end = item.End;

                    if (start.X > maxpt.X) maxpt = start;
                    if (end.X > maxpt.X) maxpt = end;
                }
                return maxpt;
            }
        }
        /// <summary>
        /// Returns the minimum (bottom) Y value of all wall points
        /// </summary>
        public System.Windows.Point Y_MIN
        {
            get
            {
                if (_walls.Count == 0) return new System.Windows.Point(1000000, 1000000);

                System.Windows.Point minpt = new System.Windows.Point(1000000, 1000000);

                foreach (WallData item in _walls.Values)
                {
                    System.Windows.Point start = item.Start;
                    System.Windows.Point end = item.End;

                    if (start.Y < minpt.Y) minpt = start;
                    if (end.Y < minpt.Y) minpt = end;
                }
                return minpt;
            }
        }
        /// <summary>
        /// Returns the maximum (top) Y value of all diaphragm points
        /// </summary>
        public System.Windows.Point Y_MAX
        {
            get
            {
                if (_walls.Count == 0) return new System.Windows.Point(-1000000, -1000000);

                System.Windows.Point maxpt = new System.Windows.Point(-1000000, -1000000);

                foreach (WallData item in _walls.Values)
                {
                    System.Windows.Point start = item.Start;
                    System.Windows.Point end = item.End;

                    if (start.Y > maxpt.Y) maxpt = start;
                    if (end.Y > maxpt.Y) maxpt = end;
                }
                return maxpt;
            }
        }

        /// <summary>
        /// Constructor that takes a collection of wall data and creates a single wall system
        /// </summary>
        /// <param name="walls"></param>
        public WallSystem()
        {
            // Create the braced wall line groups for this wall system
            BracedWallGroups_EW = new BracedWallLine(DEFAULT_ALLOWABLE_BRACEDLINE_OFFSET);
            BracedWallGroups_NS = new BracedWallLine(DEFAULT_ALLOWABLE_BRACEDLINE_OFFSET);
        }

        public void Update()
        {
            // clear the current calculator
            EW_Walls.Clear();
            NS_Walls.Clear();
            BracedWallGroups_EW.Clear();
            BracedWallGroups_NS.Clear();

            // sort through the list of walls and assign to appropriate dictionary
            foreach (var wall in _walls)
            {
                if (wall.Value.WallDir == WallDirs.EastWest)
                {
                    EW_Walls.Add(wall.Key, wall.Value);
                    BracedWallGroups_EW.AddValue(wall.Value.Center.Y); // add horizontal walls to the braced wall lines groups
                }
                else
                {
                    NS_Walls.Add(wall.Key, wall.Value);
                    BracedWallGroups_NS.AddValue(wall.Value.Center.X); // add vertical walls to the braced wall lines
                }
            }

            // update braced wall line calculations
            BracedWallGroups_EW.Update(); 
            BracedWallGroups_NS.Update();

            Console.WriteLine("\nEW Wall Groups: ");
            BracedWallGroups_EW.PrintGroups();

            Console.WriteLine("\nNS Wall Groups: ");
            BracedWallGroups_NS.PrintGroups();

            // recompute the center of rigidity for the system
            ComputeCenterOfRigidity();

            // clear centroid calculations and update x_bar and y_bar value for each wall (distance from center of wall to center of rigidity)
            X_bar_walls.Clear();
            Y_bar_walls.Clear();

            TotalRigidity_X = 0;  // clear previous value
            foreach (var wall in EW_Walls)
            {
                X_bar_walls.Add(wall.Key, (float)(wall.Value.Center.X - CtrRigidity.X));
                Y_bar_walls.Add(wall.Key, (float)(wall.Value.Center.Y - CtrRigidity.Y));
                TotalRigidity_X += wall.Value.WallRigidity;
                Console.WriteLine(wall.Value.DisplayInfo());
                Console.WriteLine("x_bar: " + X_bar_walls[wall.Key] + " ft.  y_bar: " + Y_bar_walls[wall.Key] + " ft.");

            }

            TotalRigidity_Y = 0;  // clear previous value
            foreach (var wall in NS_Walls)
            {
                X_bar_walls.Add(wall.Key, (float)(wall.Value.Center.X - CtrRigidity.X));
                Y_bar_walls.Add(wall.Key, (float)(wall.Value.Center.Y - CtrRigidity.Y));
                TotalRigidity_Y += wall.Value.WallRigidity;

                Console.WriteLine(wall.Value.DisplayInfo());
                Console.WriteLine("x_bar: " + X_bar_walls[wall.Key] + " ft.  y_bar: " + Y_bar_walls[wall.Key] + " ft.");
            }
            Console.WriteLine("Total Rigidity -- X: " + TotalRigidity_X + " ft.  Y: " + TotalRigidity_Y + " ft.");

            // compute inertia of walls
            ComputeInertia();
            Console.WriteLine("Inertia -- XX: " + InertiaXX + " ft.  YY: " + InertiaYY + " ft.   J = " + InertiaPolar + " ft^4");


        }

        /// <summary>
        /// Computes inertia values Ixx, Iyy, and Ipolar
        /// </summary>
        private void ComputeInertia()
        {
            // East / West walls
            float sum_Ixx = 0;
            foreach (var wall in EW_Walls)
            {
                sum_Ixx += (float)(wall.Value.WallRigidity * (CtrRigidity.Y - wall.Value.Center.Y) * (CtrRigidity.Y - wall.Value.Center.Y));
                //Console.WriteLine(wall.Value.DisplayInfo());
            }
            InertiaXX = sum_Ixx;

            // North / South walls
            float sum_Iyy = 0;
            foreach (var wall in NS_Walls)
            {
                sum_Iyy += (float)(wall.Value.WallRigidity * (CtrRigidity.X - wall.Value.Center.X) * (CtrRigidity.X - wall.Value.Center.X));
            }
            InertiaYY = sum_Iyy;

            // Computes polar moment of inertia
            InertiaPolar = InertiaXX + InertiaYY;
        }


        /// <summary>
        /// Primary function to compute the center of rigidity of a system of walls.  Works only for horizontal and vertical wall segments
        /// </summary>
        public void ComputeCenterOfRigidity()
        {
            // Display the wall info
            double horiz_sum = 0;
            double horiz_rigidity_sum = 0;
            double Ryr_sum = 0;
            //Console.WriteLine("\nHorizontal Walls");
            foreach (var wall in EW_Walls)
            {
                horiz_sum += wall.Value.WallLength;
                horiz_rigidity_sum += wall.Value.WallRigidity;
                Ryr_sum += wall.Value.Ryr;
                //Console.WriteLine(wall.Value.DisplayInfo());
            }
            //Console.WriteLine("Sum Ry: " + horiz_rigidity_sum + "   Sum Ryr: " + Ryr_sum);

            double vert_rigidity_sum = 0;
            double vert_sum = 0;
            double Rxr_sum = 0;
            //Console.WriteLine("\nVertical Walls");
            foreach (var wall in NS_Walls)
            {
                vert_sum += wall.Value.WallLength;
                vert_rigidity_sum += wall.Value.WallRigidity;
                Rxr_sum += wall.Value.Rxr;
                //Console.WriteLine(wall.Value.DisplayInfo());
            }
            //Console.WriteLine("\nSum Rx: " + vert_rigidity_sum + "   Sum Rxyr: " + Rxr_sum);

            CtrRigidity = new System.Windows.Point(Rxr_sum / vert_rigidity_sum, Ryr_sum / horiz_rigidity_sum);
        }


        /// <summary>
        /// Adds a wall to the wall system
        /// </summary>
        /// <param name="id"></param>
        /// <param name="wall"></param>
        public void AddWall(WallData wall)
        {
            int id = GetNextWallID();
            _walls.Add(id, wall);
        }

        /// <summary>
        /// Removes a wall from the wall system
        /// </summary>
        /// <param name="id"></param>
        public void DeleteWall(int id)
        {
            bool exists = false;
            if (_walls.ContainsKey(id))
            { 
                exists = true;
                _walls.Remove(id);
            }

            if (EW_Walls.ContainsKey(id))
            {
                exists = true;
                EW_Walls.Remove(id);
            }
            if (NS_Walls.ContainsKey(id))
            {
                exists = true;
                NS_Walls.Remove(id);
            }

            if (exists == false)
            {
                Console.WriteLine("\nWall #" + id.ToString() + " does not exist.");
                return;
            } else
            {
                Console.WriteLine("\nDeleting wall #" + id.ToString());
                Update();
            }
        }

        /// <summary>
        /// Scans the list of walls currently stored to find the next available wall id.  This should ensure a unique identifier
        /// </summary>
        /// <returns></returns>
        private int GetNextWallID()
        {
            int i = 0;
            while (true)
            {
                if (!_walls.ContainsKey(i))
                {
                    return i;
                }
                else
                {
                    i++;
                }
            }
        }

        public void Draw()
        {

        }
    }
}
