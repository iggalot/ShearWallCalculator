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

        /// <summary>Line reactions of the case being analysed (x loads to the E-W lines, y loads to the N-S lines).</summary>
        [JsonIgnore]
        public List<LineReaction> LinesX { get; } = new List<LineReaction>();
        [JsonIgnore]
        public List<LineReaction> LinesY { get; } = new List<LineReaction>();

        /// <summary>Every load case with its line reactions and wall shears (for the calculation sheet).</summary>
        [JsonIgnore]
        public List<FlexibleCaseResult> CaseResults { get; } = new List<FlexibleCaseResult>();

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
                // A flexible diaphragm distributes by tributary width and cannot transmit torsion, so each
                // load case contributes only its direct shears; walls keep their largest shear (envelope).
                var cases = CasesToAnalyse();
                double savedVx = V_x, savedVy = V_y;
                var best = new Dictionary<int, (double total, string name, int caseIndex)>();
                var perCase = new List<(Dictionary<int, double> dx, Dictionary<int, double> dy)>();
                WallShearRange.Clear();
                CaseResults.Clear();
                Warnings.Clear();
                for (int i = 0; i < cases.Count; i++)
                {
                    V_x = cases[i].Vx;
                    V_y = cases[i].Vy;
                    ComputeDirectShear_X();  // horizontal walls
                    ComputeDirectShear_Y();  // vertical walls
                    ComputeTotalShear();
                    perCase.Add((new Dictionary<int, double>(DirectShear_X), new Dictionary<int, double>(DirectShear_Y)));
                    CaseResults.Add(new FlexibleCaseResult
                    {
                        Name = cases[i].Name, Vx = cases[i].Vx, Vy = cases[i].Vy, Mt = cases[i].Mt,
                        LinesX = LinesX.ToList(), LinesY = LinesY.ToList(), Total = new Dictionary<int, double>(TotalWallShear),
                    });
                    TrackRange(TotalWallShear, cases[i].Name);
                    KeepGoverning(TotalWallShear, cases[i].Name, best, i);
                }
                V_x = savedVx;
                V_y = savedVy;
                var distinct = Warnings.Distinct().ToList();     // one line per direction, not per case
                Warnings.Clear();
                Warnings.AddRange(distinct);
                if (cases.Any(c => Math.Abs(c.Mt) > 1e-9))
                    Warnings.Add("Some load cases carry a torsional moment Mt. The flexible distribution spreads each load uniformly " +
                                 "and does not apply Mt; how Mt should become a non-uniform load along the diaphragm is not decided yet.");

                DirectShear_X.Clear(); DirectShear_Y.Clear(); TotalWallShear.Clear(); GoverningLoadCase.Clear();
                foreach (var kv in best)
                {
                    var pc = perCase[kv.Value.caseIndex];
                    if (pc.dx.TryGetValue(kv.Key, out var dx)) DirectShear_X[kv.Key] = dx;
                    if (pc.dy.TryGetValue(kv.Key, out var dy)) DirectShear_Y[kv.Key] = dy;
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

        public void ComputeTotalShear()
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
    }
}
