using Newtonsoft.Json;
using ShearWallCalculator;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

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

        /// <summary>Every load case with its subcalculations, in the order analysed (for the calculation sheets).</summary>
        [JsonIgnore]
        public List<RigidCaseResult> CaseResults { get; } = new List<RigidCaseResult>();

        public override IEnumerable<KeyValuePair<string, Dictionary<int, double>>> CaseWallTotals() =>
            CaseResults.Select(c => new KeyValuePair<string, Dictionary<int, double>>(c.Name, c.Total));

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

            // Each load case is analysed in turn; every wall keeps its largest shear (the envelope) and
            // the components (direct, torsional) from that same case, so the columns still add up.
            var cases = CasesToAnalyse();
            double savedVx = V_x, savedVy = V_y;
            var best = new Dictionary<int, (double total, string name, int caseIndex)>();
            var perCase = new List<(Dictionary<int, double> dx, Dictionary<int, double> dy, Dictionary<int, double> ecc, double mt, double ex, double ey)>();
            CaseResults.Clear();
            WallShearRange.Clear();

            for (int i = 0; i < cases.Count; i++)
            {
                var c = cases[i];
                V_x = c.Vx;
                V_y = c.Vy;
                AnalyseOneCase(c);
                perCase.Add((new Dictionary<int, double>(DirectShear_X), new Dictionary<int, double>(DirectShear_Y),
                             new Dictionary<int, double>(EccentricShear), Mt_comb, ecc_x, ecc_y));
                var direct = new Dictionary<int, double>(DirectShear_X);
                foreach (var kv in DirectShear_Y) direct[kv.Key] = kv.Value;
                CaseResults.Add(new RigidCaseResult
                {
                    Name = c.Name, Vx = c.Vx, Vy = c.Vy, Mt = c.Mt, EccX = ecc_x, EccY = ecc_y, MtComb = Mt_comb,
                    Direct = direct, Torsional = new Dictionary<int, double>(EccentricShear), Total = new Dictionary<int, double>(TotalWallShear),
                });
                TrackRange(TotalWallShear, c.Name);
                KeepGoverning(TotalWallShear, c.Name, best, i);
            }
            V_x = savedVx;
            V_y = savedVy;

            DirectShear_X.Clear(); DirectShear_Y.Clear(); EccentricShear.Clear(); TotalWallShear.Clear(); GoverningLoadCase.Clear();
            double governingMt = 0;
            foreach (var kv in best)
            {
                int id = kv.Key;
                var pc = perCase[kv.Value.caseIndex];
                if (pc.dx.TryGetValue(id, out var dx)) DirectShear_X[id] = dx;
                if (pc.dy.TryGetValue(id, out var dy)) DirectShear_Y[id] = dy;
                if (pc.ecc.TryGetValue(id, out var e)) EccentricShear[id] = e;
                TotalWallShear[id] = kv.Value.total;
                GoverningLoadCase[id] = kv.Value.name;
                if (Math.Abs(pc.mt) > Math.Abs(governingMt)) { governingMt = pc.mt; ecc_x = pc.ex; ecc_y = pc.ey; }
            }
            if (cases.Count == 1 && perCase.Count == 1) { governingMt = perCase[0].mt; ecc_x = perCase[0].ex; ecc_y = perCase[0].ey; }
            Mt_comb = governingMt;
        }

        /// <summary>Direct and torsional shears for one load case (uses V_x, V_y set by the caller).</summary>
        private void AnalyseOneCase(LateralLoadCase loadCase)
        {
            // check that our CoM and CoR values are valid and computed -- if not, the calculations don't work
            if (IsValidForCalculation is true)
            {
                double actual_x = _wall_system.CtrRigidity.X - _diaphragm_system.CtrMass.X;
                double actual_y = _wall_system.CtrRigidity.Y - _diaphragm_system.CtrMass.Y;

                if (loadCase.ApplyMinimumEccentricity)
                {
                    // Minimum eccentricity for manual loads: 5% of the plan dimension or 10% of the building
                    // height, keeping the sign (the side the torsion acts on).
                    double min_ecc_x = Math.Max(0.05 * BoundingBoxWorld.Width, 0.1 * BuildingHeight);
                    double min_ecc_y = Math.Max(0.05 * BoundingBoxWorld.Height, 0.1 * BuildingHeight);
                    ecc_x = ApplyMinimumEccentricity(actual_x, min_ecc_x);
                    ecc_y = ApplyMinimumEccentricity(actual_y, min_ecc_y);
                }
                else
                {
                    // Wind load cases bring their own torsion (Fig. 27.3-8); only the real offset applies.
                    ecc_x = actual_x;
                    ecc_y = actual_y;
                }

                // torsional moment (kip-ft) from the load at the center of mass about the center of rigidity,
                // plus any torsion the load case itself carries
                Mt_comb = (V_x * ecc_y) - (V_y * ecc_x) + loadCase.Mt;
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

        /// <summary>
        /// Torsional shear in each wall, as resistance on the diaphragm (same sense as the direct shear).
        /// Mt_comb (CCW +) turns the diaphragm by θ = Mt/J about the CR; a point at (x̄, ȳ) moves (−θȳ, +θx̄),
        /// so each wall pushes back with −k·(that movement). Signed, that is
        ///   EW walls: Fx = +k·Mt·ȳ/J      NS walls: Fy = −k·Mt·x̄/J
        /// and their moment about the CR, Σ(x̄·Fy − ȳ·Fx) = −Mt·(Σk x̄² + Σk ȳ²)/J = −Mt, balances the torsion.
        /// </summary>
        private void ComputeEccentricShear()
        {
            EccentricShear.Clear();
            double J = _wall_system.InertiaPolar;

            foreach (var wall in _wall_system.EW_Walls)
                EccentricShear.Add(wall.Key, wall.Value.WallRigidity * Mt_comb * _wall_system.Y_bar_walls[wall.Key] / J);

            foreach (var wall in _wall_system.NS_Walls)
                EccentricShear.Add(wall.Key, -wall.Value.WallRigidity * Mt_comb * _wall_system.X_bar_walls[wall.Key] / J);
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

    /// <summary>
    /// One load case through the rigid-diaphragm distribution. Building axes: x right, y up, moments CCW positive.
    /// Vx, Vy act at the center of mass; Mt is the torsion the case itself carries. ecc = CR − CM;
    /// Mt_comb = Vx·ecc_y − Vy·ecc_x + Mt is the moment about the center of rigidity. Wall shears are the force each wall
    /// exerts on the diaphragm (opposite to the load): direct = −k·V/Σk, torsional = +k·Mt_comb·ȳ/J (E-W walls, along x)
    /// or −k·Mt_comb·x̄/J (N-S walls, along y), with x̄, ȳ measured from the center of rigidity.
    /// </summary>
    public sealed class RigidCaseResult
    {
        public string Name { get; set; }
        public double Vx { get; set; }
        public double Vy { get; set; }
        public double Mt { get; set; }
        public double EccX { get; set; }
        public double EccY { get; set; }
        public double MtComb { get; set; }
        public Dictionary<int, double> Direct { get; set; }
        public Dictionary<int, double> Torsional { get; set; }
        public Dictionary<int, double> Total { get; set; }

        /// <summary>The Mt_comb terms: Vx·ecc_y and −Vy·ecc_x (kip-ft).</summary>
        public double MomentFromVx => Vx * EccY;
        public double MomentFromVy => -Vy * EccX;

        /// <summary>
        /// Statics checks on the wall forces (forces on the diaphragm, E-W walls along x, N-S walls along y):
        /// ΣFx = −Vx and ΣFy = −Vy (the walls react the load); the torsional shears alone sum to zero in x and in y (a
        /// couple); and the moment of the wall forces about the center of rigidity, Σ(x̄·Fy − ȳ·Fx), is −Mt_comb.
        /// </summary>
        public (double sumFx, double sumFy, double torsionalFx, double torsionalFy, double momentAboutCR) Checks(WallSystem walls)
        {
            double fx = walls.EW_Walls.Keys.Where(Total.ContainsKey).Sum(id => Total[id]);
            double fy = walls.NS_Walls.Keys.Where(Total.ContainsKey).Sum(id => Total[id]);
            double tx = walls.EW_Walls.Keys.Where(Torsional.ContainsKey).Sum(id => Torsional[id]);
            double ty = walls.NS_Walls.Keys.Where(Torsional.ContainsKey).Sum(id => Torsional[id]);
            double m = walls.NS_Walls.Keys.Where(Total.ContainsKey).Sum(id => walls.X_bar_walls[id] * Total[id])
                     - walls.EW_Walls.Keys.Where(Total.ContainsKey).Sum(id => walls.Y_bar_walls[id] * Total[id]);
            return (fx, fy, tx, ty, m);
        }
    }
}
