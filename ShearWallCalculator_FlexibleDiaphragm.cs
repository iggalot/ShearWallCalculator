using calculator;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ShearWallCalculator
{
    public class ShearWallCalculator_FlexibleDiaphragm : ShearWallCalculatorBase
    {
        // dictionary containing the total shear acting on a wall -- resistance at nase pf diaphragm at top of walls
        [JsonIgnore]
        public Dictionary<int, double> TotalWallShear { get; set; } = new Dictionary<int, double>();
        // shear force from direct shear in X-direction -- resistance at base of diaphragm at top of walls

        [JsonIgnore]
        public Dictionary<BracedWallLine, double> DirectShear_X_Braced { get; set; } = new Dictionary<BracedWallLine, double>();

        [JsonIgnore]
        public Dictionary<BracedWallLine, List<double>> DirectShear_X_Groups { get; set; } = new Dictionary<BracedWallLine, List<double>>();

        [JsonIgnore]
        public Dictionary<int, double> DirectShear_X { get; set; } = new Dictionary<int, double>();

        // shear force from direct shear in Y-direction -- resistance at base of diaphragm at top of walls
        [JsonIgnore]
        public Dictionary<int, double> DirectShear_Y { get; set; } = new Dictionary<int, double>();

        public new string CalculatorType { get => "Flexible Diaphragm"; }

        public ShearWallCalculator_FlexibleDiaphragm()
        {
            // update calculations once data is loaded
            Update();
        }

        public ShearWallCalculator_FlexibleDiaphragm(WallSystem walls, DiaphragmSystem diaphragm, double v_x, double v_y) : base(walls, diaphragm, v_x, v_y)
        {
            // update the calculations
            Update();
        }

        /// <summary>
        /// copy constructor
        /// </summary>
        /// <param name="copy_calc"></param>
        public ShearWallCalculator_FlexibleDiaphragm(ShearWallCalculator_FlexibleDiaphragm copy_calc)
        {
            _diaphragm_system = copy_calc._diaphragm_system;
            _wall_system = copy_calc._wall_system;

            Update();

        }
        /// <summary>
        /// Function to update calculations.  Should be called everytime data is added, removed, or changed.
        /// </summary>
        public void Update() 
        {
            // check if we have data for a wall system and a diaphragm system
            if (_diaphragm_system == null || _wall_system == null)
            {
                //TODO:  Display warning if we don't have data.
                return;
            }

            // Update calculations if necessary for the diaphragm and the wall system            
            _diaphragm_system.Update();
            _wall_system.Update();

            // TODO:  CODE requires minimum of 5% of largest dimension of building as a minimum for the eccentricity
            Console.WriteLine("Center of Mass -- xr: " + _diaphragm_system.CtrMass.X + " ft.  yr: " + _diaphragm_system.CtrMass.Y + " ft.");
            Console.WriteLine("Center of Rigidity -- xr: " + _wall_system.CtrRigidity.X + " ft.  yr: " + _wall_system.CtrRigidity.Y + " ft.");

            base.Update();  // call the base class to update also.

            // check that our CoM and CoR values are valid and computed -- if not, the calculations don't work
            if (IsValidForCalculation is true)
            {
                // Perform component calculations to compute shear contributions
                ComputeDirectShear_X();  // horizontal walls
                ComputeDirectShear_Y();  // vertical walls

                // compute the total shear activing on the wall
                ComputeTotalShear();

                // display results
                Console.WriteLine(DisplayResults());
            }
        }


        public string DisplayResults()
        {
            string str = "";
            str += "\n  ID   | Rigidity | X-bar   |  Y-bar   | Vix (kips)   | Viy (k)   | V_tot (k)";
            str += "\n ------------------------------------------------------------------------------------------";
            foreach (var result in TotalWallShear)
            {
                int id = result.Key;
                str += "\n" + id + "   |   ";

                if (_wall_system.EW_Walls.ContainsKey(id))
                {
                    str += _wall_system.EW_Walls[id].WallRigidity.ToString("0.00");
                }
                else if (_wall_system.NS_Walls.ContainsKey(id))
                {
                    str += _wall_system.NS_Walls[id].WallRigidity.ToString("0.00");
                }
                else
                {
                    str += " ERROR ";
                }
                str += "     |   ";


                str += _wall_system.X_bar_walls[id].ToString("0.00") + "    |   " + _wall_system.Y_bar_walls[id].ToString("0.00") + "   |   ";

                if (DirectShear_X.ContainsKey(id))
                {
                    str += DirectShear_X[id].ToString("0.00");
                }
                else
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


        private void ComputeDirectShear_X()
        {
            double shear_x_plf = V_x / BoundingBoxWorld.Height;
            double beamStart = BoundingBoxWorld.Y;
            double beamEnd = BoundingBoxWorld.Y + BoundingBoxWorld.Height;

            var bracedWallLine = _wall_system.BWL_Manager.GetEastWestWallLines();

            //// Compute support center Y-coordinates
            //List<double> supports = bracedWallLine.groupedWalls
            //    .Where(g => g.Count > 0)
            //    .Select(group => group.Average(w => w.Center.Y))
            //    .ToList();

            //var loads = SupportLoadDistributor.DistributeLoad(supports, shear_x_plf, beamStart, beamEnd);

            //// Store individual group loads
            //var groupLoads = loads.Select(l => l.Load).ToList();
            //DirectShear_X_Groups[bracedWallLine] = groupLoads;

            //// Store total for verification
            //double totalShear = groupLoads.Sum();
            //DirectShear_X_Braced[bracedWallLine] = totalShear;

            // Output
            //for (int i = 0; i < groupLoads.Count; i++)
            //{
            //    Console.WriteLine($"Group {i} (Y = {supports[i]:F2}) carries {groupLoads[i]:F2} kN");
            //}
            //Console.WriteLine($"Total shear on braced wall line: {totalShear:F2} kN");
        }

        /// <summary>
        /// Compute the direct shear for loads acting in the Y-direction
        /// </summary>
        private void ComputeDirectShear_Y()
        {
            DirectShear_Y.Clear();
            int sign = -1;  // computing the resistance on the diaphragm
            foreach (var wall in _wall_system.NS_Walls)
            {
                DirectShear_Y.Add(wall.Key, sign * V_y * wall.Value.WallRigidity / _wall_system.TotalRigidity_Y);
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
                TotalWallShear.Add(id, result.Value);
            }
            foreach (var result in DirectShear_Y)
            {
                int id = result.Key;
                TotalWallShear.Add(id, result.Value);
            }
        }



        public class SupportLoadDistributor
        {
            public class Support
            {
                public int ID { get; set; } // the ID number of the braced wall line
                public double Location { get; set; }
                public double Load { get; set; }
            }

            /// <summary>
            /// Distributes a uniform load across supports using tributary areas.
            /// Includes beam overhangs if beamStart or beamEnd extend beyond supports.
            /// </summary>
            public static List<Support> DistributeLoad(
                List<double> supportPositions,
                double uniformLoad,
                double beamStart,
                double beamEnd)
            {
                if (supportPositions == null || supportPositions.Count < 2)
                    throw new ArgumentException("At least two supports are required.");

                if (beamEnd <= beamStart)
                    throw new ArgumentException("Beam end must be greater than beam start.");

                supportPositions = supportPositions.OrderBy(x => x).ToList();
                var supports = new List<Support>();

                for (int i = 0; i < supportPositions.Count; i++)
                {
                    double tributaryStart, tributaryEnd;

                    if (i == 0)
                    {
                        // First support
                        double nextMid = (supportPositions[i] + supportPositions[i + 1]) / 2.0;
                        tributaryStart = beamStart;
                        tributaryEnd = nextMid;
                    }
                    else if (i == supportPositions.Count - 1)
                    {
                        // Last support
                        double prevMid = (supportPositions[i] + supportPositions[i - 1]) / 2.0;
                        tributaryStart = prevMid;
                        tributaryEnd = beamEnd;
                    }
                    else
                    {
                        // Interior supports
                        double prevMid = (supportPositions[i] + supportPositions[i - 1]) / 2.0;
                        double nextMid = (supportPositions[i] + supportPositions[i + 1]) / 2.0;
                        tributaryStart = prevMid;
                        tributaryEnd = nextMid;
                    }

                    double width = tributaryEnd - tributaryStart;
                    if (width < 0)
                        throw new InvalidOperationException("Calculated tributary width is negative. Check support positions.");

                    supports.Add(new Support
                    {
                        Location = supportPositions[i],
                        Load = uniformLoad * width
                    });
                }

                return supports;
            }
        }
    }
}
