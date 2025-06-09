using calculator;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ShearWallCalculator
{
    public class ShearWallCalculator_FlexibleDiaphragm : ShearWallCalculatorBase
    {
        [JsonIgnore]
        public Dictionary<BracedWallLine, double> DirectShear_X_BracedLine { get; set; } = new Dictionary<BracedWallLine, double>();
        
        [JsonIgnore]
        public Dictionary<BracedWallLine, double> DirectShear_Y_BracedLine { get; set; } = new Dictionary<BracedWallLine, double>();

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

        public ShearWallCalculator_FlexibleDiaphragm(WallSystem wall_system, DiaphragmSystem diaphragm, double v_x, double v_y) : base(wall_system, diaphragm, v_x, v_y)
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

                // display results
                Console.WriteLine(DisplayResults());
            }
        }


        public string DisplayResults()
        {
            string str = "";
        //    str += "\n  ID   | Rigidity | X-bar   |  Y-bar   | Vix (kips)   | Viy (k)   | V_tot (k)";
        //    str += "\n ------------------------------------------------------------------------------------------";
        //    foreach (var result in TotalWallShear)
        //    {
        //        int id = result.Key;
        //        str += "\n" + id + "   |   ";

        //        if (_wall_system.EW_Walls.ContainsKey(id))
        //        {
        //            str += _wall_system.EW_Walls[id].WallRigidity.ToString("0.00");
        //        }
        //        else if (_wall_system.NS_Walls.ContainsKey(id))
        //        {
        //            str += _wall_system.NS_Walls[id].WallRigidity.ToString("0.00");
        //        }
        //        else
        //        {
        //            str += " ERROR ";
        //        }
        //        str += "     |   ";


        //        str += _wall_system.X_bar_walls[id].ToString("0.00") + "    |   " + _wall_system.Y_bar_walls[id].ToString("0.00") + "   |   ";

        //        if (DirectShear_X.ContainsKey(id))
        //        {
        //            str += DirectShear_X[id].ToString("0.00");
        //        }
        //        else
        //        {
        //            str += "----";
        //        }
        //        str += "     |   ";
        //        if (DirectShear_Y.ContainsKey(id))
        //        {
        //            str += DirectShear_Y[id].ToString("0.00");
        //        }
        //        else
        //        {
        //            str += "----";
        //        }
        //        str += "      |   ";

        //        if (TotalWallShear.ContainsKey(id))
        //        {
        //            str += TotalWallShear[id].ToString("0.00");
        //        }
        //        else
        //        {
        //            str += "----";
        //        }
        //    }
            return str;
        }


        private void ComputeDirectShear_X()
        {
            double shear_x_plf = V_x / BoundingBoxWorld.Height;
            double beamStart = BoundingBoxWorld.Y;
            double beamEnd = BoundingBoxWorld.Y + BoundingBoxWorld.Height;

            // Step 1: Collect supports from all East-West braced wall lines
            List<SupportLoadDistributor.Support> supports = new List<SupportLoadDistributor.Support>();
            foreach (var bwl in _wall_system.BWL_Manager.BracedWallLines)
            {
                if (bwl.WallDir == WallDirs.EastWest)
                {
                    supports.Add(new SupportLoadDistributor.Support
                    {
                        ID = bwl.GroupNumber,
                        Location = bwl.Center.Y
                    });
                }
            }

            // Debugging: Print the number of east-west braced wall lines found
            Console.WriteLine($"Found {supports.Count} east-west braced wall lines.");

            // Step 2: Sort supports by Y-location
            supports.Sort((a, b) => a.Location.CompareTo(b.Location));

            // Step 3: Distribute the total shear to the supports
            List<SupportLoadDistributor.Support> loads = SupportLoadDistributor.DistributeLoad(supports, shear_x_plf, beamStart, beamEnd);

            // Step 4: Initialize result dictionaries
            DirectShear_X_BracedLine.Clear();
            DirectShear_X.Clear();

            // Step 5: Build a lookup of distributed shear loads by group ID
            Dictionary<int, double> groupLoadMap = loads.ToDictionary(l => l.ID, l => l.Load);

            // Debugging: Print the total shear for each braced wall line
            foreach (var load in loads)
            {
                Console.WriteLine($"Group {load.ID} has {load.Load:F2} kN of shear.");
            }

            // Step 6: Accumulate shear load to each wall based on rigidity
            foreach (var bwl in _wall_system.BWL_Manager.BracedWallLines)
            {
                if (bwl.WallDir != WallDirs.EastWest)
                    continue;

                // Get total load for this braced wall line from the lookup table
                if (!groupLoadMap.TryGetValue(bwl.GroupNumber, out double totalShear))
                    continue;

                // Store total shear for this BWL
                DirectShear_X_BracedLine[bwl] = totalShear;

                // Debugging: Log the total shear for the current BWL
                Console.WriteLine($"Braced Wall Line {bwl.GroupNumber} has a total shear of {totalShear:F2} kN.");

                // Calculate total rigidity of all walls in this BWL
                double sum_rigid = bwl.WallReferences.Sum(w => w.WallRigidity);
                if (sum_rigid == 0)
                    continue;

                // Distribute the shear load to individual wall segments
                foreach (var wall in bwl.WallReferences)
                {
                    // Make sure wall ID is correctly populated
                    if (wall.ID == 0)
                    {
                        Console.WriteLine($"Warning: Wall ID is 0 for Wall in BWL {bwl.GroupNumber}. Check initialization.");
                    }

                    double wallShear = totalShear * (wall.WallRigidity / sum_rigid);

                    // Check if the wall ID already exists in DirectShear_X and accumulate the shear value
                    if (DirectShear_X.ContainsKey(wall.ID))
                    {
                        DirectShear_X[wall.ID] += wallShear; // Accumulate shear for duplicate wall ID
                    }
                    else
                    {
                        DirectShear_X.Add(wall.ID, wallShear); // Add new shear for a unique wall ID
                    }

                    // Debugging: Print the shear assigned to this wall
                    Console.WriteLine($"Wall ID {wall.ID} receives {wallShear:F2} kN of shear.");
                }
            }

            // Debugging: Print the final shear for each wall
            foreach (var entry in DirectShear_X)
            {
                Console.WriteLine($"Wall ID: {entry.Key}, Total Shear: {entry.Value:F2} kN");
            }
        }


        /// <summary>
        /// Compute the direct shear for loads acting in the Y-direction
        /// </summary>
        private void ComputeDirectShear_Y()
        {
            double shear_x_plf = V_y / BoundingBoxWorld.Height;
            double beamStart = BoundingBoxWorld.Y;
            double beamEnd = BoundingBoxWorld.Y + BoundingBoxWorld.Width;

            // Step 1: Collect supports from all East-West braced wall lines
            List<SupportLoadDistributor.Support> supports = new List<SupportLoadDistributor.Support>();
            foreach (var bwl in _wall_system.BWL_Manager.BracedWallLines)
            {
                if (bwl.WallDir == WallDirs.NorthSouth)
                {
                    supports.Add(new SupportLoadDistributor.Support
                    {
                        ID = bwl.GroupNumber,
                        Location = bwl.Center.Y
                    });
                }
            }

            // Debugging: Print the number of east-west braced wall lines found
            Console.WriteLine($"Found {supports.Count} east-west braced wall lines.");

            // Step 2: Sort supports by Y-location
            supports.Sort((a, b) => a.Location.CompareTo(b.Location));

            // Step 3: Distribute the total shear to the supports
            List<SupportLoadDistributor.Support> loads = SupportLoadDistributor.DistributeLoad(supports, shear_x_plf, beamStart, beamEnd);

            // Step 4: Initialize result dictionaries
            DirectShear_Y_BracedLine.Clear();
            DirectShear_Y.Clear();

            // Step 5: Build a lookup of distributed shear loads by group ID
            Dictionary<int, double> groupLoadMap = loads.ToDictionary(l => l.ID, l => l.Load);

            // Debugging: Print the total shear for each braced wall line
            foreach (var load in loads)
            {
                Console.WriteLine($"Group {load.ID} has {load.Load:F2} kN of shear.");
            }

            // Step 6: Accumulate shear load to each wall based on rigidity
            foreach (var bwl in _wall_system.BWL_Manager.BracedWallLines)
            {
                if (bwl.WallDir != WallDirs.NorthSouth)
                    continue;

                // Get total load for this braced wall line from the lookup table
                if (!groupLoadMap.TryGetValue(bwl.GroupNumber, out double totalShear))
                    continue;

                // Store total shear for this BWL
                DirectShear_Y_BracedLine[bwl] = totalShear;

                // Debugging: Log the total shear for the current BWL
                Console.WriteLine($"Braced Wall Line {bwl.GroupNumber} has a total shear of {totalShear:F2} kN.");

                // Calculate total rigidity of all walls in this BWL
                double sum_rigid = bwl.WallReferences.Sum(w => w.WallRigidity);
                if (sum_rigid == 0)
                    continue;

                // Distribute the shear load to individual wall segments
                foreach (var wall in bwl.WallReferences)
                {
                    // Make sure wall ID is correctly populated
                    if (wall.ID == 0)
                    {
                        Console.WriteLine($"Warning: Wall ID is 0 for Wall in BWL {bwl.GroupNumber}. Check initialization.");
                    }

                    double wallShear = totalShear * (wall.WallRigidity / sum_rigid);

                    // Check if the wall ID already exists in DirectShear_X and accumulate the shear value
                    if (DirectShear_Y.ContainsKey(wall.ID))
                    {
                        DirectShear_Y[wall.ID] += wallShear; // Accumulate shear for duplicate wall ID
                    }
                    else
                    {
                        DirectShear_Y.Add(wall.ID, wallShear); // Add new shear for a unique wall ID
                    }

                    // Debugging: Print the shear assigned to this wall
                    Console.WriteLine($"Wall ID {wall.ID} receives {wallShear:F2} kN of shear.");
                }
            }

            // Debugging: Print the final shear for each wall
            foreach (var entry in DirectShear_Y)
            {
                Console.WriteLine($"Wall ID: {entry.Key}, Total Shear: {entry.Value:F2} kN");
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
                List<Support> supports_unsorted,
                double uniformLoad,
                double beamStart,
                double beamEnd)
            {
                if (supports_unsorted == null || supports_unsorted.Count < 2)
                    throw new ArgumentException("At least two supports are required.");

                if (beamEnd <= beamStart)
                    throw new ArgumentException("Beam end must be greater than beam start.");

                
                // sort the support locations in order
                List<Support> supports = supports_unsorted.OrderBy(x => x.Location).ToList();

                // create a temporary results list
                List<Support> results = new List<Support>();

                for (int i = 0; i < supports.Count; i++)
                {
                    double tributaryStart, tributaryEnd;

                    if (i == 0)
                    {
                        // First support
                        double nextMid = (supports[i].Location + supports[i + 1].Location) / 2.0;
                        tributaryStart = beamStart;
                        tributaryEnd = nextMid;
                    }
                    else if (i == supports.Count - 1)
                    {
                        // Last support
                        double prevMid = (supports[i].Location + supports[i - 1].Location) / 2.0;
                        tributaryStart = prevMid;
                        tributaryEnd = beamEnd;
                    }
                    else
                    {
                        // Interior supports
                        double prevMid = (supports[i].Location + supports[i - 1].Location) / 2.0;
                        double nextMid = (supports[i].Location + supports[i + 1].Location) / 2.0;
                        tributaryStart = prevMid;
                        tributaryEnd = nextMid;
                    }

                    double width = tributaryEnd - tributaryStart;
                    if (width < 0)
                        throw new InvalidOperationException("Calculated tributary width is negative. Check support positions.");

                    results.Add(new Support
                    {
                        ID = supports[i].ID,
                        Location = supports[i].Location,
                        Load = uniformLoad * width
                    });
                }

                return results;
            }
        }
    }
}
