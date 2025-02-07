using System;
using System.Collections.Generic;
using System.Numerics;

namespace calculator
{
    public enum WallDirs
    {
        EastWest = 0,
        NorthSouth = 1
    }
    public class Point
    {
        public float X;
        public float Y;
        public Point(float x, float y)
        {
            X = x;
            Y = y;
        }
    }

    public class Wall
    {
        public int Id {get; set;}
        public Point Start { get; set; } // start point of wall...bottommost point at one end
        public Point End { get; set; } // end point of wall...bottommost point at other end
        public Point Center { get => GetCenterPoint(); } // center point of wall
        public float WallLength { get => GetLength(); }  // length of the wall
        public float WallHeight { get; set; } = 9;  // height of wall feet.
        public float WallRigidity { get => GetRigidity(); } // rigidity in direction of the wall

      

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
        public Wall(int id, float ht, float sx, float sy, float ex, float ey, WallDirs wallDir)
        {
            Id = id;
            WallHeight = ht;
            Start = new Point(sx, sy);
            End = new Point(ex, ey);
            WallDir = wallDir;
        }


        private Point GetCenterPoint()
        {
            return new Point((Start.X + End.X) / 2, (Start.Y + End.Y) / 2);
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
            return new Vector2((End.X - Start.X) / WallLength, (End.Y - Start.Y) / WallLength);
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
            return (Start.X + End.X) / 2 * WallRigidity;
        }

        /// <summary>
        /// Compute first x-moment of rigidity about global origin (0,0)
        /// </summary>
        /// <returns></returns>
        private float ComputeFirstMomentOfRigidity_Y()
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
            str += "\nWall ID: " + Id + "   WallDir: " + WallDir;
            str += "\nStart: " + Start.X + ", " + Start.Y + "  End: " + End.X + ", " + End.Y + "   UnitVec: <" + Dir.X + ", " + Dir.Y +">";
            str += "\nLength: " + WallLength + "  Height: " + WallHeight + "  Rigidity: " + WallRigidity;
            str += "\nRxr: " + Rxr + "  Ryr: " + Ryr;
            return str;
        }
    }

    /// <summary>
    /// Origin = Bottom left corner of building
    /// +x direction is to the right
    /// +y direction is up
    /// </summary>
    public class ShearWallCalculator
    {
        /// <summary>
        /// Loads and eccentricty values 
        /// Uses Cartesian coordinate and right-hand rule -- x+ right, y+ up, rot+ = CCW
        /// </summary>
        public float V_x { get; set; } = 40; // x direction load (kips) acting at center of mass
        public float V_y { get; set; } = 0;  // y direction load (kips) acting at center of mass

        /// <summary>
        /// eccentricities
        /// TODO:  CODE requires minimum of 5% of largest dimension of building as a minimum for the eccentricity
        /// </summary>
        private float ecc_x = 0; // eccentricity in x direction (feet) measured from center of rigidity to center of mass
        private float ecc_y = 0; // eccentricity in y direction (feet) measured from center of rigidity to center of mass

        public float Mt_comb { get; set; } = 0; // moment due to eccentric loading  "+ = CCW, "-" = CW

        // collection of walls in East West direction (horizontal on screen)
        public Dictionary<int, Wall> EW_Walls { get; set; } = new Dictionary<int, Wall>();

        // collection of walls in North South direction (vertical on screen)
        public Dictionary<int, Wall> NS_Walls { get; set; } = new Dictionary<int, Wall>();

        // distance from center of wall to center of rigidity in y-direction
        public Dictionary<int, float> Y_bar_walls { get; set; } = new Dictionary<int, float>();

        // distance from center of wall to center of rigidity in x-direction
        public Dictionary<int, float> X_bar_walls { get; set; } = new Dictionary<int, float>();

        // shear force from direct shear in X-direction -- resistance at base of diaphragm at top of walls
        public Dictionary<int, float> DirectShear_X { get; set; } = new Dictionary<int, float>();

        // shear force from direct shear in Y-direction -- resistance at base of diaphragm at top of walls
        public Dictionary<int, float> DirectShear_Y { get; set; } = new Dictionary<int, float>();

        // shear force in line of wall from eccentric loading, Mr -- resistance at base of diaphragm at top of walls
        public Dictionary<int, float> EccentricShear { get; set; } = new Dictionary<int, float>();

        // dictionary containing the total shear acting on a wall -- resistance at nase pf diaphragm at top of walls
        public Dictionary<int, float> TotalWallShear { get; set; } = new Dictionary<int, float>();

        private float TotalRigidity_X { get; set; } = 0;  // total rigidity in x-direction
        private float TotalRigidity_Y { get; set; } = 0;  // total rigidity in y-direction

        // center or rigidity
        public Point CtrRigidity { get; set; } = new Point(0, 0);
        // center of mass
        public Point CtrMass { get; set; } = new Point(0, 0);

        // moments of inertia
        public float InertiaXX { get; set; } // inertia of horizontal walls about center of rigidity
        public float InertiaYY { get; set; } // inertia of vertical walls about center of rigidity
        public float InertiaPolar { get; set; } // polar moment of all walls about center of rigidity

        /// <summary>
        /// default constructor
        /// </summary>
        public ShearWallCalculator()
        {
            LoadTestWallData();
            //LoadTestWallData2();

            //update calculations once data is loaded
            Update();
        }

        /// <summary>
        /// Function to update calculations.  Should be called everytime data is added, removed, or changed.
        /// </summary>
        private void Update()
        {
            // find center of rigidity
            ComputeCenterOfRigidity();  // compute the center of rigidty for the arrangement of shear walls
            Console.WriteLine("Center of Rigidity -- xr: " + CtrRigidity.X + " ft.  yr: " + CtrRigidity.Y + " ft.");

            // update eccentricty values between center of mass and center of Rigidity
            ecc_x = CtrRigidity.X - CtrMass.X;
            ecc_y = CtrRigidity.Y - CtrMass.Y;
            Console.WriteLine("ecc_x: " + ecc_x + " ft.  ecc_y: " + ecc_y + " ft.");

            Mt_comb = (V_x * ecc_y) - (V_y * ecc_x);
            Console.WriteLine("M_comb: " + Mt_comb + " kips-m");

            // update x_bar and y_bar value for each wall (distance from center of wall to center of rigidity)
            X_bar_walls.Clear();
            Y_bar_walls.Clear();

            TotalRigidity_X = 0;  // clear previous value
            foreach (var wall in EW_Walls)
            {
                X_bar_walls.Add(wall.Key, (wall.Value.Center.X - CtrRigidity.X));
                Y_bar_walls.Add(wall.Key, (wall.Value.Center.Y - CtrRigidity.Y));
                TotalRigidity_X += wall.Value.WallRigidity;
                Console.WriteLine(wall.Value.DisplayInfo());
                Console.WriteLine("x_bar: " + X_bar_walls[wall.Key] + " ft.  y_bar: " + Y_bar_walls[wall.Key] + " ft.");

            }

            TotalRigidity_Y = 0;  // clear previous value
            foreach (var wall in NS_Walls)
            {
                X_bar_walls.Add(wall.Key, (wall.Value.Center.X - CtrRigidity.X));
                Y_bar_walls.Add(wall.Key, (wall.Value.Center.Y - CtrRigidity.Y));
                TotalRigidity_Y += wall.Value.WallRigidity;

                Console.WriteLine(wall.Value.DisplayInfo());
                Console.WriteLine("x_bar: " + X_bar_walls[wall.Key] + " ft.  y_bar: " + Y_bar_walls[wall.Key] + " ft.");
            }
            Console.WriteLine("Total Rigidity -- X: " + TotalRigidity_X + " ft.  Y: " + TotalRigidity_Y + " ft.");

            // compute inertia of walls
            ComputeInertia();
            Console.WriteLine("Inertia -- XX: " + InertiaXX + " ft.  YY: " + InertiaYY + " ft.   J = " + InertiaPolar + " ft^4");

            // compute shear contributions
            ComputeDirectShear_X();  // horizontal walls
            ComputeDirectShear_Y();  // vertical walls
            ComputeEccentricShear(); // shear in line of wall due to rotation eccentricty of structure

            // compute the total shear activing on the wall
            ComputeTotalShear();

            // display results
            Console.WriteLine(DisplayResults());
        }

        public string DisplayResults()
        {
            string str = "";
            str += "\n  ID   | Rigidity | X-bar   |  Y-bar   | Vix (kips)   | Viy (k)   | V_ecc (k)  | V_tot (k)";
            str += "\n ------------------------------------------------------------------------------------------";
            foreach (var result in TotalWallShear)
            {
                int id = result.Key;
                str += "\n" + id + "   |   ";

                if (EW_Walls.ContainsKey(id))
                {
                    str += EW_Walls[id].WallRigidity.ToString("0.00");
                }
                else if (NS_Walls.ContainsKey(id))
                {
                    str += NS_Walls[id].WallRigidity.ToString("0.00");
                }
                else
                {
                    str += " ERROR ";
                }
                str += "     |   ";


                str += X_bar_walls[id].ToString("0.00") + "    |   " + Y_bar_walls[id].ToString("0.00") + "   |   ";

                if (DirectShear_X.ContainsKey(id))
                {
                    str += DirectShear_X[id].ToString("0.00");
                } else
                {
                    str += "----";
                }
                str += "     |   ";
                if (DirectShear_Y.ContainsKey(id))
                {
                    str += DirectShear_Y[id].ToString("0.00");
                }
                else
                {
                    str += "----";
                }
                str += "      |   ";

                if (EccentricShear.ContainsKey(id))
                {
                    str += EccentricShear[id].ToString("0.00");
                }
                else
                {
                    str += "----";
                }
                str += "    |   ";

                if (TotalWallShear.ContainsKey(id))
                {
                    str += TotalWallShear[id].ToString("0.00");
                }
                else
                {
                    str += "----";
                }
            }
            return str;
        }

        /// <summary>
        /// Compute the direct shear for loads acting in the X-direction
        /// </summary>
        private void ComputeDirectShear_X()
        {
            int sign = -1; // computing the resistance on the diaphragm
            foreach (var wall in EW_Walls)
            {
                DirectShear_X.Add(wall.Key, sign * V_x * wall.Value.WallRigidity / TotalRigidity_X);
            }
        }

        /// <summary>
        /// Compute the direct shear for loads acting in the Y-direction
        /// </summary>
        private void ComputeDirectShear_Y()
        {
            int sign = -1;  // computing the resistance on the diaphragm
            foreach (var wall in NS_Walls)
            {
                DirectShear_Y.Add(wall.Key, sign * V_y * wall.Value.WallRigidity / TotalRigidity_Y);
            }
        }

        private void ComputeEccentricShear()
        {
            foreach (var wall in EW_Walls)
            {
                int sign = 1; // computing the resistance on the diaphragm
                if(Mt_comb < 0)
                {
                    if (wall.Value.Center.Y < CtrRigidity.Y)
                    {
                        sign = +1;
                    } else if (wall.Value.Center.Y > CtrRigidity.Y)
                    {
                        sign = -1;
                    } else
                    {
                        sign = 0;
                    } 
                } else if (Mt_comb > 0)
                {
                    if (wall.Value.Center.Y > CtrRigidity.Y)
                    {
                        sign = -1;
                    }
                    else if (wall.Value.Center.Y < CtrRigidity.Y)
                    {
                        sign = +1;
                    }
                    else
                    {
                        sign = 0;
                    }
                }

                float moment = Math.Abs(Mt_comb * Y_bar_walls[wall.Key]);
                EccentricShear.Add(wall.Key, (sign * moment * wall.Value.WallRigidity) / InertiaPolar);
            }

            foreach (var wall in NS_Walls)
            {
                int sign = 1;
                if (Mt_comb > 0)
                {
                    if (wall.Value.Center.X > CtrRigidity.X)
                    {
                        sign = +1;
                    }
                    else if (wall.Value.Center.X < CtrRigidity.X)
                    {
                        sign = -1;
                    }
                    else
                    {
                        sign = 0;
                    }
                }
                else if (Mt_comb < 0)
                {
                    if (wall.Value.Center.X > CtrRigidity.X)
                    {
                        sign = -1;
                    }
                    else if (wall.Value.Center.X < CtrRigidity.X)
                    {
                        sign = +1;
                    }
                    else
                    {
                        sign = 0;
                    }
                }

                float moment = Math.Abs(Mt_comb * X_bar_walls[wall.Key]);
                EccentricShear.Add(wall.Key, (sign * moment * wall.Value.WallRigidity) / InertiaPolar);
            }
        }

        /// <summary>
        /// Tallies the total shear acting on a wall as the sum of direct shear and eccentric shear
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        private void ComputeTotalShear()
        {
            TotalWallShear.Clear();
            foreach (var result in DirectShear_X)
            {
                int id = result.Key;
                TotalWallShear.Add(id, result.Value + EccentricShear[id]);
            }
            foreach (var result in DirectShear_Y)
            {
                int id = result.Key;
                TotalWallShear.Add(id, result.Value + EccentricShear[id]);
            }
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
                sum_Ixx += wall.Value.WallRigidity * (CtrRigidity.Y - wall.Value.Center.Y) * (CtrRigidity.Y - wall.Value.Center.Y);
                //Console.WriteLine(wall.Value.DisplayInfo());
            }
            InertiaXX = sum_Ixx;

            // North / South walls
            float sum_Iyy = 0;
            foreach (var wall in NS_Walls)
            {
                sum_Iyy += wall.Value.WallRigidity * (CtrRigidity.X - wall.Value.Center.X) * (CtrRigidity.X - wall.Value.Center.X);
            }
            InertiaYY = sum_Iyy;

            // Computes polar moment of inertia
            InertiaPolar = InertiaXX + InertiaYY;
        }

        /// <summary>
        /// Primary function to compute the center of rigidity of a system of walls.  Works only for horizontal and vertical wall segments
        /// </summary>
        private void ComputeCenterOfRigidity()
        {
            // Display the wall info
            float horiz_sum = 0;
            float horiz_rigidity_sum = 0;
            float Ryr_sum = 0;
            //Console.WriteLine("\nHorizontal Walls");
            foreach (var wall in EW_Walls)
            {
                horiz_sum += wall.Value.WallLength;
                horiz_rigidity_sum += wall.Value.WallRigidity;
                Ryr_sum += wall.Value.Ryr;
                //Console.WriteLine(wall.Value.DisplayInfo());
            }
            //Console.WriteLine("Sum Ry: " + horiz_rigidity_sum + "   Sum Ryr: " + Ryr_sum);

            float vert_rigidity_sum = 0;
            float vert_sum = 0;
            float Rxr_sum = 0;
            //Console.WriteLine("\nVertical Walls");
            foreach (var wall in NS_Walls)
            {
                vert_sum += wall.Value.WallLength;
                vert_rigidity_sum += wall.Value.WallRigidity;
                Rxr_sum += wall.Value.Rxr;
                //Console.WriteLine(wall.Value.DisplayInfo());
            }
            //Console.WriteLine("\nSum Rx: " + vert_rigidity_sum + "   Sum Rxyr: " + Rxr_sum);

            CtrRigidity = new Point(Rxr_sum / vert_rigidity_sum, Ryr_sum / horiz_rigidity_sum);
        }

        /// <summary>
        /// Prepares a set of test data.  Based on Tonatiuh Rodriquez Niki video 01 on YouTube.
        /// https://www.youtube.com/watch?v=Ljp5M0CTOwA&list=PLOnJNeyZggWT5z5PoXbNfec9nlfH6AczC&index=1
        /// </summary>
        private void LoadTestWallData()
        {
            // Center of mass
            CtrMass = new Point(7.58f, 37.5f);

            // East / West Wall Segments
            Wall wall1 = new Wall(1, 9, 0, 0, 20, 0, WallDirs.EastWest);
            EW_Walls.Add(1, wall1);
            Wall wall2 = new Wall(2, 9, 20, 30, 30, 30, WallDirs.EastWest);
            EW_Walls.Add(2, wall2);
            Wall wall3 = new Wall(3, 9,20, 45, 30, 45, WallDirs.EastWest);
            EW_Walls.Add(3, wall3);
            Wall wall4 = new Wall(4, 9, 0, 75, 20, 75, WallDirs.EastWest);
            EW_Walls.Add(4, wall4);

            // North / South Wall Segments
            Wall wall5 = new Wall(5, 9, 0, 27.5f, 0, 47.5f, WallDirs.NorthSouth);
            NS_Walls.Add(5, wall5);
            Wall wall6 = new Wall(6, 9, 20, 20, 20, 30, WallDirs.NorthSouth);
            NS_Walls.Add(6, wall6);
            Wall wall7 = new Wall(7, 9, 20, 45, 20, 55, WallDirs.NorthSouth);
            NS_Walls.Add(7, wall7);
        }

        /// <summary>
        /// Prepares a set of test data.  Based on Kestava - Rigid Diaphragm Example on YouTube.
        /// https://www.youtube.com/watch?v=f5xqVsTlpnY
        /// </summary>
        private void LoadTestWallData2()
        {
            // Building 40Wx80L
            // R1 = R2 = R3 = R
            // R4 = 3 x R
            CtrMass = new Point(20, 40);

            // East / West Wall Segments
            Wall wall2 = new Wall(2, 9, 15, 80, 25, 80, WallDirs.EastWest);
            EW_Walls.Add(wall2.Id, wall2);
            Wall wall4 = new Wall(4, 9, 10.5875f, 0, 29.4125f, 0, WallDirs.EastWest); // this wall is 3x rigid as others
            //Wall wall4 = new Wall(4, 9, 15, 0, 25, 0, WallDirs.EastWest); // this wall is 3x rigid as others
            EW_Walls.Add(wall4.Id, wall4);

            // North / South Wall Segments
            Wall wall1 = new Wall(1, 9, 0, 5, 0, 15, WallDirs.NorthSouth);
            NS_Walls.Add(wall1.Id, wall1);
            Wall wall3 = new Wall(3, 9, 40, 5, 40, 15, WallDirs.NorthSouth);
            NS_Walls.Add(wall3.Id, wall3);
        }
    }
}
