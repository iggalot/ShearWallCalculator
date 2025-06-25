using Newtonsoft.Json;
using ShearWallCalculator;
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
    
    public class ShearWallCalculator_RigidDiaphragm : ShearWallCalculatorBase
    {
        public new string CalculatorType { get => "Rigid Diaphragm"; }

        /// <summary>
        /// eccentricities
        /// TODO:  CODE requires minimum of 5% of largest dimension of building as a minimum for the eccentricity
        /// </summary>
        [JsonIgnore]
        private double ecc_x = 0; // eccentricity in x direction (feet) measured from center of rigidity to center of mass
        [JsonIgnore]
        private double ecc_y = 0; // eccentricity in y direction (feet) measured from center of rigidity to center of mass

        [JsonIgnore]
        public double Mt_comb { get; set; } = 0; // moment due to eccentric loading  "+ = CCW, "-" = CW

        // shear force from direct shear in X-direction -- resistance at base of diaphragm at top of walls
        [JsonIgnore]
        public Dictionary<int, double> DirectShear_X { get; set; } = new Dictionary<int, double>();

        // shear force from direct shear in Y-direction -- resistance at base of diaphragm at top of walls
        [JsonIgnore]
        public Dictionary<int, double> DirectShear_Y { get; set; } = new Dictionary<int, double>();

        // shear force in line of wall from eccentric loading, Mr -- resistance at base of diaphragm at top of walls
        [JsonIgnore]
        public Dictionary<int, double> EccentricShear { get; set; } = new Dictionary<int, double>();

        /// <summary>
        /// default constructor
        /// </summary>
        public ShearWallCalculator_RigidDiaphragm() : base()
        {

        }

        /// <summary>
        /// copy constructor
        /// </summary>
        /// <param name="copy_calc"></param>
        public ShearWallCalculator_RigidDiaphragm(ShearWallCalculatorBase copy_calc) : base(copy_calc)
        {

        }

        /// <summary>
        /// Function to update calculations.  Should be called everytime data is added, removed, or changed.
        /// </summary>
        public override void PerformCalculations()
        {
            // check if we have data for a wall system and a diaphragm system
            if (_diaphragm_system == null || _wall_system == null)
            {
                //TODO:  Display warning if we don't have data.
                return;
            }

            base.Update();

            Console.WriteLine("Center of Mass -- xr: " + _diaphragm_system.CtrMass.X + " ft.  yr: " + _diaphragm_system.CtrMass.Y + " ft.");
            Console.WriteLine("Center of Rigidity -- xr: " + _wall_system.CtrRigidity.X + " ft.  yr: " + _wall_system.CtrRigidity.Y + " ft.");

            // check that our CoM and CoR values are valid and computed -- if not, the calculations don't work
            if (IsValidForCalculation is true)
            {
                // Set ASCE 7 minimum for building eccentricity.
                // 5% of building width / Length or 10% of building height
                double min_ecc_x = Math.Max(0.05 * BoundingBoxWorld.Width, 0.1 * BuildingHeight);
                double min_ecc_y = Math.Max(0.05 * BoundingBoxWorld.Height, 0.1 * BuildingHeight);

                // update eccentricty values between center of mass and center of Rigidity
                // TODO:  this function isnt very efficient.  Can we do better?  Should we signal that the eccentricity is being overridden by the minimum code value?
                ecc_x = Math.Max(min_ecc_x, (float)(_wall_system.CtrRigidity.X - _diaphragm_system.CtrMass.X));
                ecc_y = Math.Max(min_ecc_y, (float)(_wall_system.CtrRigidity.Y - _diaphragm_system.CtrMass.Y));

                Console.WriteLine("ecc_x: " + ecc_x + " ft.  ecc_y: " + ecc_y + " ft.");

                // compute moment due to eccentric loading between center of mass and center of rigidity
                Mt_comb = (V_x * ecc_y) - (V_y * ecc_x);
                Console.WriteLine("M_comb: " + Mt_comb + " kips-m");

                //// display results
                //Console.WriteLine(DisplayResults());
            }
            ComputeDirectShear_X();  // horizontal walls
            ComputeDirectShear_Y();  // vertical walls
            ComputeEccentricShear(); // shear in line of wall due to rotation eccentricty of structure

            // compute the total shear activing on the wall
            ComputeTotalShear();
        }

        public string DisplayResults()
        {
            string str = "";
            str += "\n  ID   | Rigidity | X-bar   |  Y-bar   | Vix (kips)   | Viy (k)   | V_ecc (k)  | V_tot (k)";
            str += "\n ------------------------------------------------------------------------------------------";
            foreach (var result in TotalWallShear)
            {
                int id = result.Key;

                // rigidity values
                if (_wall_system._walls.ContainsKey(id) is false)
                {

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
                        str += " R. ERROR ";
                    }
                    str += "     |   ";
                }

                // x-bar and y-bar values
                if (_wall_system.X_bar_walls.ContainsKey(id) is false)
                {
                    str += _wall_system.X_bar_walls[id].ToString("0.00") + "    |   ";
                }
                else
                {
                    str += " X_BAR ERROR" + "    |   ";
                }

                if (_wall_system.Y_bar_walls.ContainsKey(id) is false)
                {
                    str += _wall_system.Y_bar_walls[id].ToString("0.00") + "   |   ";
                }
                else
                {
                    str += "Y_BAR ERROR" + "    |   ";
                }

                // direct shear in X-direction calcs
                if (DirectShear_X.ContainsKey(id))
                {
                    str += DirectShear_X[id].ToString("0.00");
                }
                else
                {
                    str += "VX_ERROR";
                }
                str += "     |   ";

                // direct shear in Y-direction calcs
                if (DirectShear_Y.ContainsKey(id))
                {
                    str += DirectShear_Y[id].ToString("0.00");
                }
                else
                {
                    str += "VY_ERROR";
                }
                str += "      |   ";

                if (EccentricShear.ContainsKey(id))
                {
                    str += EccentricShear[id].ToString("0.00");
                }
                else
                {
                    str += "ECC. V_ERROR";
                }
                str += "    |   ";

                // Total shear values
                if (TotalWallShear.ContainsKey(id))
                {
                    str += TotalWallShear[id].ToString("0.00");
                }
                else
                {
                    str += "TOTAL V_ERR";
                }
                str += "\n";

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
            foreach (var wall in _wall_system.EW_Walls)
            {
                DirectShear_X.Add(wall.Key, sign * V_x * wall.Value.WallRigidity / _wall_system.TotalRigidity_X);
            }
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

        private void ComputeEccentricShear()
        {
            EccentricShear.Clear();

            foreach (var wall in _wall_system.EW_Walls)
            {
                int sign = 1; // computing the resistance on the diaphragm
                if(Mt_comb < 0)
                {
                    if (wall.Value.Center.Y < _wall_system.CtrRigidity.Y)
                    {
                        sign = +1;
                    } else if (wall.Value.Center.Y > _wall_system.CtrRigidity.Y)
                    {
                        sign = -1;
                    } else
                    {
                        sign = 0;
                    } 
                } else if (Mt_comb > 0)
                {
                    if (wall.Value.Center.Y > _wall_system.CtrRigidity.Y)
                    {
                        sign = -1;
                    }
                    else if (wall.Value.Center.Y < _wall_system.CtrRigidity.Y)
                    {
                        sign = +1;
                    }
                    else
                    {
                        sign = 0;
                    }
                }

                double moment = Math.Abs(Mt_comb * _wall_system.Y_bar_walls[wall.Key]);
                EccentricShear.Add(wall.Key, (sign * moment * wall.Value.WallRigidity) / _wall_system.InertiaPolar);
            }

            foreach (var wall in _wall_system.NS_Walls)
            {
                int sign = 1;
                if (Mt_comb > 0)
                {
                    if (wall.Value.Center.X > _wall_system.CtrRigidity.X)
                    {
                        sign = +1;
                    }
                    else if (wall.Value.Center.X < _wall_system.CtrRigidity.X)
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
                    if (wall.Value.Center.X > _wall_system.CtrRigidity.X)
                    {
                        sign = -1;
                    }
                    else if (wall.Value.Center.X < _wall_system.CtrRigidity.X)
                    {
                        sign = +1;
                    }
                    else
                    {
                        sign = 0;
                    }
                }

                double moment = Math.Abs(Mt_comb * _wall_system.X_bar_walls[wall.Key]);
                EccentricShear.Add(wall.Key, (sign * moment * wall.Value.WallRigidity) / _wall_system.InertiaPolar);
            }
        }

        /// <summary>
        /// Tallies the total shear acting on a wall as the sum of direct shear and eccentric shear
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public void ComputeTotalShear()
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
    }
}
