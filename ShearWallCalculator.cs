using System;
using System.Collections.Generic;
using System.Data;

namespace calculator
{
    /// <summary>
    /// Origin = Bottom left corner of building
    /// +x direction is to the right
    /// +y direction is up
    /// </summary>
    public class ShearWallCalculator
    {
        public const string FILENAME = "results.txt";

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
        public Dictionary<int, WallData> EW_Walls { get; set; } = new Dictionary<int, WallData>();

        // collection of walls in North South direction (vertical on screen)
        public Dictionary<int, WallData> NS_Walls { get; set; } = new Dictionary<int, WallData>();

        public List<System.Windows.Point> DiaphragmPoints { get; set; } = new List<System.Windows.Point>();

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
            //// Sample data for testing
            //LoadTestWallData();
            //LoadTestWallData2();

            //update calculations once data is loaded
            Update();           
        }

        public ShearWallCalculator(Dictionary<int, WallData> walls, List<System.Windows.Point> diaphagm_pts)
        {

            // clear the current calculator
            EW_Walls.Clear();
            NS_Walls.Clear();
            DiaphragmPoints.Clear();

            // sort through the list of walls and assign to appropriate dictionary
            foreach (var wall in walls)
            {
                if(wall.Value.WallDir == WallDirs.EastWest)
                {
                    EW_Walls.Add(wall.Key, wall.Value);
                } else
                {
                    NS_Walls.Add(wall.Key, wall.Value);
                }
            }

            DiaphragmPoints = diaphagm_pts;

            // update the calculations
            Update();
        }

        /// <summary>
        /// Function to update calculations.  Should be called everytime data is added, removed, or changed.
        /// </summary>
        private void Update()
        {
            // find the center of mass
            ComputeCenterOfMass();
            Console.WriteLine("Center of Mass -- xr: " + CtrMass.X + " ft.  yr: " + CtrMass.Y + " ft.");
            

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

        private void ComputeCenterOfMass()
        {
            if(DiaphragmPoints.Count == 0)
            {
                CtrMass.X = 0;
                CtrMass.Y = 0;
            } else
            {
                int num_pts = DiaphragmPoints.Count;
                // find coords
                float sum_x = 0;
                float sum_y = 0;
                for (int i = 0; i < num_pts; i++)
                {
                    sum_x += (float)DiaphragmPoints[i].X;
                    sum_y += (float)DiaphragmPoints[i].Y;
                }
                CtrMass.X = sum_x / num_pts;
                CtrMass.Y = sum_y / num_pts;


                
            }
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
            DirectShear_X.Clear();
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
            DirectShear_Y.Clear();
            int sign = -1;  // computing the resistance on the diaphragm
            foreach (var wall in NS_Walls)
            {
                DirectShear_Y.Add(wall.Key, sign * V_y * wall.Value.WallRigidity / TotalRigidity_Y);
            }
        }

        private void ComputeEccentricShear()
        {
            EccentricShear.Clear();

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
            WallData wall1 = new WallData(1, 9, 0, 0, 20, 0, WallDirs.EastWest);
            EW_Walls.Add(1, wall1);
            WallData wall2 = new WallData(2, 9, 20, 30, 30, 30, WallDirs.EastWest);
            EW_Walls.Add(2, wall2);
            WallData wall3 = new WallData(3, 9,20, 45, 30, 45, WallDirs.EastWest);
            EW_Walls.Add(3, wall3);
            WallData wall4 = new WallData(4, 9, 0, 75, 20, 75, WallDirs.EastWest);
            EW_Walls.Add(4, wall4);

            // North / South Wall Segments
            WallData wall5 = new WallData(5, 9, 0, 27.5f, 0, 47.5f, WallDirs.NorthSouth);
            NS_Walls.Add(5, wall5);
            WallData wall6 = new WallData(6, 9, 20, 20, 20, 30, WallDirs.NorthSouth);
            NS_Walls.Add(6, wall6);
            WallData wall7 = new WallData(7, 9, 20, 45, 20, 55, WallDirs.NorthSouth);
            NS_Walls.Add(7, wall7);
        }

        /// <summary>
        /// Routine to write out wall data to the file
        /// </summary>
        /// <param name="filename"></param>
        public void WriteToFile(string filename)
        {
            foreach (var wall in EW_Walls)
            {
                wall.Value.WriteToFile(FILENAME);
            }
            foreach (var wall in NS_Walls)
            {
                wall.Value.WriteToFile(FILENAME);
            }
        }

        public void ReadFromFile(string filename)
        {
            throw new NotImplementedException();
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
            WallData wall2 = new WallData(2, 9, 15, 80, 25, 80, WallDirs.EastWest);
            EW_Walls.Add(wall2.Id, wall2);
            WallData wall4 = new WallData(4, 9, 10.5875f, 0, 29.4125f, 0, WallDirs.EastWest); // this wall is 3x rigid as others
            //Wall wall4 = new Wall(4, 9, 15, 0, 25, 0, WallDirs.EastWest); // this wall is 3x rigid as others
            EW_Walls.Add(wall4.Id, wall4);

            // North / South Wall Segments
            WallData wall1 = new WallData(1, 9, 0, 5, 0, 15, WallDirs.NorthSouth);
            NS_Walls.Add(wall1.Id, wall1);
            WallData wall3 = new WallData(3, 9, 40, 5, 40, 15, WallDirs.NorthSouth);
            NS_Walls.Add(wall3.Id, wall3);
        }

        public void DeleteWall(int id)
        {
            Console.WriteLine("\nDeleting wall #" + id.ToString());
            if (EW_Walls.ContainsKey(id))
            {
                EW_Walls.Remove(id);
            }
            if (NS_Walls.ContainsKey(id))
            {
                NS_Walls.Remove(id);
            }

            Update();
        }
    }
}
