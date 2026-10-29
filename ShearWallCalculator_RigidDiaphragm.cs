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

            //Console.WriteLine("Center of Mass -- xr: " + _diaphragm_system.CtrMass.X + " ft.  yr: " + _diaphragm_system.CtrMass.Y + " ft.");
            //Console.WriteLine("Center of Rigidity -- xr: " + _wall_system.CtrRigidity.X + " ft.  yr: " + _wall_system.CtrRigidity.Y + " ft.");

            // check that our CoM and CoR values are valid and computed -- if not, the calculations don't work
            if (IsValidForCalculation is true)
            {
                // Set ASCE 7 minimum for building eccentricity.
                // 5% of building width / Length or 10% of building height
                double min_ecc_x = Math.Max(0.05 * BoundingBoxWorld.Width, 0.1 * BuildingHeight);
                double min_ecc_y = Math.Max(0.05 * BoundingBoxWorld.Height, 0.1 * BuildingHeight);

                // Eccentricity between center of rigidity and center of mass, keeping its sign (the side
                // the torsion acts on) and raising its magnitude to the minimum.
                ecc_x = ApplyMinimumEccentricity(_wall_system.CtrRigidity.X - _diaphragm_system.CtrMass.X, min_ecc_x);
                ecc_y = ApplyMinimumEccentricity(_wall_system.CtrRigidity.Y - _diaphragm_system.CtrMass.Y, min_ecc_y);

                // torsional moment (kip-ft) from the load at the center of mass about the center of rigidity
                Mt_comb = (V_x * ecc_y) - (V_y * ecc_x);

                //// display results
                //Console.WriteLine(DisplayResults());
            }
            ComputeDirectShear_X();  // horizontal walls
            ComputeDirectShear_Y();  // vertical walls
            ComputeEccentricShear(); // shear in line of wall due to rotation eccentricty of structure

            // compute the total shear activing on the wall
            ComputeTotalShear();
        }

        /// <summary>
        /// Returns the eccentricity with its sign preserved and its magnitude at least <paramref name="minimum"/>.
        /// A zero eccentricity takes the positive minimum.
        /// </summary>
        public static double ApplyMinimumEccentricity(double actual, double minimum)
        {
            double magnitude = Math.Max(Math.Abs(actual), minimum);
            return actual < 0 ? -magnitude : magnitude;
        }

        /// <summary>Eccentricity used in the x direction (ft), after the minimum.</summary>
        public double Eccentricity_X => ecc_x;

        /// <summary>Eccentricity used in the y direction (ft), after the minimum.</summary>
        public double Eccentricity_Y => ecc_y;

        public string DisplayResults()
        {
            string Cell(Dictionary<int, double> values, int id) =>
                values.TryGetValue(id, out double v) ? v.ToString("0.00") : "-";

            var sb = new System.Text.StringBuilder();
            sb.Append("\n  ID   | Rigidity | X-bar   |  Y-bar   | Vix (kips)   | Viy (k)   | V_ecc (k)  | V_tot (k)");
            sb.Append("\n ------------------------------------------------------------------------------------------");
            foreach (var result in TotalWallShear)
            {
                int id = result.Key;
                string rigidity = _wall_system._walls.TryGetValue(id, out var wall) ? wall.WallRigidity.ToString("0.00") : "-";
                sb.Append('\n').Append(id).Append("   |   ").Append(rigidity)
                  .Append("     |   ").Append(Cell(_wall_system.X_bar_walls, id))
                  .Append("    |   ").Append(Cell(_wall_system.Y_bar_walls, id))
                  .Append("   |   ").Append(Cell(DirectShear_X, id))
                  .Append("     |   ").Append(Cell(DirectShear_Y, id))
                  .Append("      |   ").Append(Cell(EccentricShear, id))
                  .Append("    |   ").Append(Cell(TotalWallShear, id));
            }
            return sb.ToString();
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
