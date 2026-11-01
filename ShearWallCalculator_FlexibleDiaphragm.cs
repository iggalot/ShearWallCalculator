using calculator;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ShearWallCalculator
{
    public class ShearWallCalculator_FlexibleDiaphragm : ShearWallCalculatorBase
    {
        private WallSystem wall_system;
        private DiaphragmSystem diaphragm_system;
        private double currentMagX1;
        private double currentMagX2;

        [JsonIgnore]
        public Dictionary<BracedWallLine, double> DirectShear_X_BracedLine { get; set; } = new Dictionary<BracedWallLine, double>();
        
        [JsonIgnore]
        public Dictionary<BracedWallLine, double> DirectShear_Y_BracedLine { get; set; } = new Dictionary<BracedWallLine, double>();

        [JsonIgnore]
        public Dictionary<int, double> DirectShear_X { get; set; } = new Dictionary<int, double>();

        // shear force from direct shear in Y-direction -- resistance at base of diaphragm at top of walls
        [JsonIgnore]
        public Dictionary<int, double> DirectShear_Y { get; set; } = new Dictionary<int, double>();

        /// <summary>Torsional shear of each wall for the case being analysed (equivalent-weight method, see <see cref="ComputeTorsion"/>).</summary>
        [JsonIgnore]
        public Dictionary<int, double> TorsionalShear { get; set; } = new Dictionary<int, double>();

        /// <summary>Center of the equivalent weights (ft) and their polar sum J = Σ K d² (ft²) for the torsion.</summary>
        [JsonIgnore]
        public double TorsionCenterX { get; private set; }
        [JsonIgnore]
        public double TorsionCenterY { get; private set; }
        [JsonIgnore]
        public double TorsionJ { get; private set; }

        /// <summary>Line reactions of the case being analysed (x loads to the E-W lines, y loads to the N-S lines).</summary>
        [JsonIgnore]
        public List<LineReaction> LinesX { get; } = new List<LineReaction>();
        [JsonIgnore]
        public List<LineReaction> LinesY { get; } = new List<LineReaction>();

        /// <summary>Every load case with its line reactions and wall shears (for the calculation sheet).</summary>
        [JsonIgnore]
        public List<FlexibleCaseResult> CaseResults { get; } = new List<FlexibleCaseResult>();

        public override IEnumerable<KeyValuePair<string, Dictionary<int, double>>> CaseWallTotals() =>
            CaseResults.Select(c => new KeyValuePair<string, Dictionary<int, double>>(c.Name, c.Total));

        /// <summary>What the flexible distribution could not do for these walls and loads (shown on the sheet).</summary>
        [JsonIgnore]
        public List<string> Warnings { get; } = new List<string>();


        public new string CalculatorType { get => "Flexible Diaphragm"; }

        public ShearWallCalculator_FlexibleDiaphragm() : base()
        {
        }

        /// <summary>
        /// copy constructor
        /// </summary>
        /// <param name="copy_calc"></param>
        public ShearWallCalculator_FlexibleDiaphragm(ShearWallCalculatorBase copy_calc) : base(copy_calc)
        {

        }

        /// <summary>
        /// Function to update calculations.  Should be called everytime data is added, removed, or changed.
        /// </summary>
        public override void PerformCalculations() 
        {
            base.Update(); // update necessary system parts

            // check if we have data for a wall system and a diaphragm system
            if (_diaphragm_system == null || _wall_system == null)
            {
                return;
            }

            //// TODO:  CODE requires minimum of 5% of largest dimension of building as a minimum for the eccentricity
            //Console.WriteLine("Center of Mass -- xr: " + _diaphragm_system.CtrMass.X + " ft.  yr: " + _diaphragm_system.CtrMass.Y + " ft.");
            //Console.WriteLine("Center of Rigidity -- xr: " + _wall_system.CtrRigidity.X + " ft.  yr: " + _wall_system.CtrRigidity.Y + " ft.");

            // check that our CoM and CoR values are valid and computed -- if not, the calculations don't work
            if (IsValidForCalculation is true)
            {
                // Flexible diaphragm: each load case's V goes to the wall lines by tributary width (direct), and its torsion
                // Mt by the equivalent-weight method (ComputeTorsion). Walls keep the range over all cases and the largest.
                var cases = CasesToAnalyse();
                double savedVx = V_x, savedVy = V_y;
                var best = new Dictionary<int, (double total, string name, int caseIndex)>();
                var perCase = new List<(Dictionary<int, double> dx, Dictionary<int, double> dy, Dictionary<int, double> t)>();
                WallShearRange.Clear();
                CaseResults.Clear();
                Warnings.Clear();
                for (int i = 0; i < cases.Count; i++)
                {
                    V_x = cases[i].Vx;
                    V_y = cases[i].Vy;
                    ComputeDirectShear_X();  // horizontal walls
                    ComputeDirectShear_Y();  // vertical walls
                    bool torsionApplied = ComputeTorsion(cases[i].Mt);
                    ComputeTotalShear();
                    perCase.Add((new Dictionary<int, double>(DirectShear_X), new Dictionary<int, double>(DirectShear_Y), new Dictionary<int, double>(TorsionalShear)));
                    CaseResults.Add(new FlexibleCaseResult
                    {
                        Name = cases[i].Name, Vx = cases[i].Vx, Vy = cases[i].Vy, Mt = cases[i].Mt,
                        LinesX = LinesX.ToList(), LinesY = LinesY.ToList(), Total = new Dictionary<int, double>(TotalWallShear),
                        TorsionApplied = torsionApplied, CenterX = TorsionCenterX, CenterY = TorsionCenterY, J = TorsionJ,
                        Torsional = new Dictionary<int, double>(TorsionalShear),
                    });
                    TrackRange(TotalWallShear, cases[i].Name);
                    KeepGoverning(TotalWallShear, cases[i].Name, best, i);
                }
                V_x = savedVx;
                V_y = savedVy;
                var distinct = Warnings.Distinct().ToList();     // one line per direction, not per case
                Warnings.Clear();
                Warnings.AddRange(distinct);

                DirectShear_X.Clear(); DirectShear_Y.Clear(); TorsionalShear.Clear(); TotalWallShear.Clear(); GoverningLoadCase.Clear();
                foreach (var kv in best)
                {
                    var pc = perCase[kv.Value.caseIndex];
                    if (pc.dx.TryGetValue(kv.Key, out var dx)) DirectShear_X[kv.Key] = dx;
                    if (pc.dy.TryGetValue(kv.Key, out var dy)) DirectShear_Y[kv.Key] = dy;
                    if (pc.t.TryGetValue(kv.Key, out var t)) TorsionalShear[kv.Key] = t;
                    TotalWallShear[kv.Key] = kv.Value.total;
                    GoverningLoadCase[kv.Key] = kv.Value.name;
                }
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

        /// <summary>X loads go to the east-west wall lines, spread over the plan depth (y).</summary>
        private void ComputeDirectShear_X()
        {
            Distribute(WallDirs.EastWest, V_x, BoundingBoxWorld.Y, BoundingBoxWorld.Y + BoundingBoxWorld.Height, b => b.Center.Y,
                       DirectShear_X, DirectShear_X_BracedLine, LinesX);
        }

        /// <summary>Y loads go to the north-south wall lines, spread over the plan width (x).</summary>
        private void ComputeDirectShear_Y()
        {
            Distribute(WallDirs.NorthSouth, V_y, BoundingBoxWorld.X, BoundingBoxWorld.X + BoundingBoxWorld.Width, b => b.Center.X,
                       DirectShear_Y, DirectShear_Y_BracedLine, LinesY);
        }

        /// <summary>
        /// Flexible diaphragm: the load V is a uniform line load w = V / (end − start) across the plan; the diaphragm spans as
        /// simple beams between the wall lines, so each line takes w × its tributary width (half way to each neighbour, to the
        /// plan edge for the outer lines). The walls on a line share its reaction by rigidity. Wall shears are the force each
        /// wall exerts on the diaphragm, opposite in sign to the load (the same convention as the rigid calculation).
        /// </summary>
        private void Distribute(WallDirs dir, double V, double start, double end, Func<BracedWallLine, double> location,
            Dictionary<int, double> wallShears, Dictionary<BracedWallLine, double> lineShears, List<LineReaction> lines)
        {
            wallShears.Clear();
            lineShears.Clear();
            lines.Clear();
            var bwls = _wall_system.BWL_Manager.BracedWallLines.Where(b => b.WallDir == dir).ToList();
            if (bwls.Count < 2 || end <= start)
            {
                Warnings.Add($"{(dir == WallDirs.EastWest ? "East-west" : "North-south")} walls: a flexible diaphragm needs at least two wall lines " +
                             $"in this direction (found {bwls.Count}); these walls get no shear.");
                return;
            }

            double w = V / (end - start);
            var supports = bwls.Select(b => new SupportLoadDistributor.Support { ID = b.GroupNumber, Location = location(b) }).ToList();
            var loads = SupportLoadDistributor.DistributeLoad(supports, w, start, end).ToDictionary(l => l.ID);

            foreach (var bwl in bwls.OrderBy(location))
            {
                var s = loads[bwl.GroupNumber];
                double reaction = -s.Load;                        // the line's force on the diaphragm
                lineShears[bwl] = reaction;
                double sumK = bwl.WallReferences.Sum(x => x.WallRigidity);
                lines.Add(new LineReaction
                {
                    Line = bwl.GroupNumber, Location = s.Location, From = s.TributaryStart, To = s.TributaryEnd, W = w, Reaction = reaction,
                    SumRigidity = sumK, WallIds = bwl.WallReferences.Select(x => x.ID).ToList(),
                });
                if (sumK == 0) continue;
                foreach (var wall in bwl.WallReferences)
                {
                    double share = reaction * wall.WallRigidity / sumK;
                    wallShears[wall.ID] = wallShears.TryGetValue(wall.ID, out var v) ? v + share : share;
                }
            }
        }

        /// <summary>
        /// Torsion with a flexible diaphragm, equivalent-weight method (WoodWorks, "Torsional analysis for flexible diaphragms"):
        /// each line's weight K = its tributary share of the direct load (tributary width / plan dimension; Σ K = 1 in each
        /// direction). Center x_c = Σ K x over the N-S lines, y_c = Σ K y over the E-W lines; J = Σ K ȳ² (E-W) + Σ K x̄² (N-S).
        /// Line force on the diaphragm, CCW-positive torsion T: E-W lines +K T ȳ / J, N-S lines −K T x̄ / J (same signs as the
        /// rigid calculation). They sum to zero in x and in y and their moment about the center is −T. Each line's force is
        /// shared by its walls by rigidity. Needs at least two lines in each direction; returns false when it cannot be applied.
        /// </summary>
        private bool ComputeTorsion(double T)
        {
            TorsionalShear.Clear();
            TorsionCenterX = TorsionCenterY = TorsionJ = 0;
            foreach (var l in LinesX.Concat(LinesY)) { l.K = l.Arm = l.Torsional = 0; }
            if (LinesX.Count < 2 || LinesY.Count < 2)
            {
                if (Math.Abs(T) > 1e-9)
                    Warnings.Add("Torsion Mt is not applied: the flexible torsion needs at least two wall lines in each direction.");
                return false;
            }

            void Weights(List<LineReaction> lines)
            {
                double span = lines.Max(l => l.To) - lines.Min(l => l.From);
                foreach (var l in lines) l.K = l.Width / span;
            }
            Weights(LinesX);
            Weights(LinesY);
            TorsionCenterY = LinesX.Sum(l => l.K * l.Location);
            TorsionCenterX = LinesY.Sum(l => l.K * l.Location);
            foreach (var l in LinesX) l.Arm = l.Location - TorsionCenterY;   // ȳ
            foreach (var l in LinesY) l.Arm = l.Location - TorsionCenterX;   // x̄
            TorsionJ = LinesX.Concat(LinesY).Sum(l => l.K * l.Arm * l.Arm);
            if (TorsionJ <= 0) return false;

            foreach (var l in LinesX) l.Torsional = l.K * T * l.Arm / TorsionJ;
            foreach (var l in LinesY) l.Torsional = -l.K * T * l.Arm / TorsionJ;
            foreach (var l in LinesX.Concat(LinesY))
            {
                if (l.SumRigidity == 0) continue;
                foreach (var wall in _wall_system.BWL_Manager.BracedWallLines.First(b => b.GroupNumber == l.Line && b.WallDir == (LinesX.Contains(l) ? WallDirs.EastWest : WallDirs.NorthSouth)).WallReferences)
                {
                    double share = l.Torsional * wall.WallRigidity / l.SumRigidity;
                    TorsionalShear[wall.ID] = TorsionalShear.TryGetValue(wall.ID, out var v) ? v + share : share;
                }
            }
            return true;
        }

        /// <summary>Total = direct (tributary) + torsional, per wall.</summary>
        public void ComputeTotalShear()
        {
            TotalWallShear.Clear();
            foreach (var result in DirectShear_X.Concat(DirectShear_Y))
                TotalWallShear[result.Key] = (TotalWallShear.TryGetValue(result.Key, out var v) ? v : 0) + result.Value;
            foreach (var result in TorsionalShear)
                TotalWallShear[result.Key] = (TotalWallShear.TryGetValue(result.Key, out var v) ? v : 0) + result.Value;
        }

        public class SupportLoadDistributor
        {
            public class Support
            {
                public int ID { get; set; } // the ID number of the braced wall line
                public double Location { get; set; }
                public double Load { get; set; }
                public double TributaryStart { get; set; }
                public double TributaryEnd { get; set; }
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
                        Load = uniformLoad * width,
                        TributaryStart = tributaryStart,
                        TributaryEnd = tributaryEnd,
                    });
                }

                return results;
            }
        }
    }

    /// <summary>One wall line's share of a load in a flexible diaphragm (tributary width).</summary>
    public sealed class LineReaction
    {
        public int Line { get; set; }

        /// <summary>Position of the line across the load (ft): y for E-W lines, x for N-S lines.</summary>
        public double Location { get; set; }

        /// <summary>Tributary extent (ft): half way to each neighbouring line, to the plan edge for the outer lines.</summary>
        public double From { get; set; }
        public double To { get; set; }
        public double Width => To - From;

        /// <summary>Uniform line load w = V / plan dimension (k/ft).</summary>
        public double W { get; set; }

        /// <summary>The line's force on the diaphragm, −w × width (kips).</summary>
        public double Reaction { get; set; }

        public double SumRigidity { get; set; }

        /// <summary>Torsion: equivalent weight K = tributary share (Σ K = 1 per direction), arm from the center (ȳ or x̄, ft), and the line's torsional force (kips).</summary>
        public double K { get; set; }
        public double Arm { get; set; }
        public double Torsional { get; set; }
        public List<int> WallIds { get; set; }
    }

    /// <summary>One load case through the flexible distribution.</summary>
    public sealed class FlexibleCaseResult
    {
        public string Name { get; set; }
        public double Vx { get; set; }
        public double Vy { get; set; }
        public double Mt { get; set; }
        public List<LineReaction> LinesX { get; set; }
        public List<LineReaction> LinesY { get; set; }
        public Dictionary<int, double> Total { get; set; }

        /// <summary>Torsion of this case: applied (two or more lines in each direction), center of the weights, J = Σ K d² (ft²), wall torsional shears.</summary>
        public bool TorsionApplied { get; set; }
        public double CenterX { get; set; }
        public double CenterY { get; set; }
        public double J { get; set; }
        public Dictionary<int, double> Torsional { get; set; }
    }
}
