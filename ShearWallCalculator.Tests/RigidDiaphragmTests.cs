using System;
using System.Linq;
using System.Windows;
using calculator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShearWallCalculator;

[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]

namespace ShearWallCalculator.Tests
{
    /// <summary>
    /// Rigid-diaphragm distribution of lateral load to shear walls: relative rigidity, center of rigidity,
    /// direct shear in proportion to rigidity, torsion about the center of rigidity, minimum eccentricity.
    /// </summary>
    [TestClass]
    public class RigidDiaphragmTests
    {
        private const double H = 9.0; // wall height (ft)

        /// <summary>
        /// 40 x 20 ft diaphragm. North-south walls at x = 0 (length westLength) and x = 40 (length eastLength);
        /// east-west walls of 10 ft at y = 0 and y = 20.
        /// </summary>
        private static ShearWallCalculator_RigidDiaphragm Box(double westLength, double eastLength, double vx, double vy)
        {
            var calc = new ShearWallCalculator_RigidDiaphragm();
            calc._wall_system.AddWall(new WallData(H, 0, 5, 0, 5 + westLength));
            calc._wall_system.AddWall(new WallData(H, 40, 5, 40, 5 + eastLength));
            calc._wall_system.AddWall(new WallData(H, 15, 0, 25, 0));
            calc._wall_system.AddWall(new WallData(H, 15, 20, 25, 20));
            calc._diaphragm_system.AddDiaphragm(new DiaphragmData_Rectangular(new Point(0, 0), new Point(40, 20)));
            calc.AddLoads(vx, vy);
            calc.PerformCalculations();
            return calc;
        }

        private static double Rigidity(double h, double L) => 1.0 / (0.4 * Math.Pow(h / L, 3) + 0.3 * (h / L));

        [DataTestMethod]
        [DataRow(9.0, 9.0)]
        [DataRow(9.0, 4.0)]
        [DataRow(9.0, 20.0)]
        public void WallRigidity_CantileverPier(double h, double L)
        {
            // Relative rigidity of a cantilevered pier (flexural + shear deflection), constant E*t.
            var wall = new WallData(h, 0, 0, L, 0);
            Assert.AreEqual(Rigidity(h, L), wall.WallRigidity, 1e-5 * Rigidity(h, L));
        }

        [TestMethod]
        public void CenterOfRigidity_IsRigidityWeightedCentroid()
        {
            var calc = Box(westLength: 10, eastLength: 6, vx: 0, vy: 10);
            double kw = Rigidity(H, 10), ke = Rigidity(H, 6);
            Assert.AreEqual((kw * 0 + ke * 40) / (kw + ke), calc._wall_system.CtrRigidity.X, 1e-3);
            Assert.AreEqual(10.0, calc._wall_system.CtrRigidity.Y, 1e-6);
        }

        [TestMethod]
        public void CenterOfMass_IsDiaphragmCentroid()
        {
            var calc = Box(10, 10, 0, 10);
            Assert.AreEqual(20.0, calc._diaphragm_system.CtrMass.X, 1e-9);
            Assert.AreEqual(10.0, calc._diaphragm_system.CtrMass.Y, 1e-9);
        }

        /// <summary>Direct shear is shared in proportion to rigidity and sums to the applied load (reacting it).</summary>
        [TestMethod]
        public void DirectShear_ProportionalToRigidity_AndInEquilibrium()
        {
            var calc = Box(westLength: 10, eastLength: 6, vx: 8, vy: 12);
            Assert.AreEqual(-12.0, calc.DirectShear_Y.Values.Sum(), 1e-9);
            Assert.AreEqual(-8.0, calc.DirectShear_X.Values.Sum(), 1e-9);

            double kw = Rigidity(H, 10), ke = Rigidity(H, 6);
            var ns = calc._wall_system.NS_Walls.OrderBy(w => w.Value.Center.X).Select(w => calc.DirectShear_Y[w.Key]).ToArray();
            Assert.AreEqual(kw / ke, ns[0] / ns[1], 1e-6);
        }

        [DataTestMethod]
        [DataRow(-10.0, 2.0, -10.0)]
        [DataRow(10.0, 2.0, 10.0)]
        [DataRow(-0.5, 2.0, -2.0)]
        [DataRow(0.5, 2.0, 2.0)]
        [DataRow(0.0, 2.0, 2.0)]
        public void MinimumEccentricity_KeepsSign(double actual, double minimum, double expected)
        {
            Assert.AreEqual(expected, ShearWallCalculator_RigidDiaphragm.ApplyMinimumEccentricity(actual, minimum), 1e-12);
        }

        /// <summary>
        /// Stiffer wall on the west side puts the center of rigidity west of the center of mass: the
        /// eccentricity (CR - CM) must be negative, not replaced by the positive minimum.
        /// </summary>
        [TestMethod]
        public void Eccentricity_StiffWestWall_IsNegative()
        {
            var calc = Box(westLength: 16, eastLength: 4, vx: 0, vy: 10);
            double actual = calc._wall_system.CtrRigidity.X - calc._diaphragm_system.CtrMass.X;
            Assert.IsTrue(actual < -2.0, $"test setup: CR should be well west of CM (got {actual:0.00})");
            Assert.AreEqual(actual, calc.Eccentricity_X, 1e-6);
        }

        /// <summary>Torsional shears resist the whole torsional moment: sum(|V_i d_i|) = |Mt|.</summary>
        [DataTestMethod]
        [DataRow(10.0, 6.0, 0.0, 10.0)]
        [DataRow(16.0, 4.0, 5.0, 10.0)]
        [DataRow(10.0, 10.0, 7.0, 3.0)]
        public void TorsionalShears_ResistTheTorsionalMoment(double west, double east, double vx, double vy)
        {
            var calc = Box(west, east, vx, vy);
            var ws = calc._wall_system;
            double resisted =
                ws.EW_Walls.Keys.Sum(id => Math.Abs(calc.EccentricShear[id] * ws.Y_bar_walls[id])) +
                ws.NS_Walls.Keys.Sum(id => Math.Abs(calc.EccentricShear[id] * ws.X_bar_walls[id]));
            Assert.AreEqual(Math.Abs(calc.Mt_comb), resisted, 1e-3 * Math.Max(1, Math.Abs(calc.Mt_comb)));
        }

        /// <summary>
        /// Signed moment equilibrium about the center of rigidity, CCW positive (Mz = r x F = x̄·Fy − ȳ·Fx).
        /// The wall shears are the resistance on the diaphragm, so their moment must cancel the applied
        /// torsion: Σ(x̄·Fy − ȳ·Fx) + Mt = 0. Direct shears give no moment about the CR by its definition.
        /// </summary>
        [DataTestMethod]
        [DataRow(10.0, 6.0, 0.0, 10.0)]
        [DataRow(16.0, 4.0, 5.0, 10.0)]
        [DataRow(4.0, 16.0, -5.0, 10.0)]
        [DataRow(10.0, 10.0, 7.0, -3.0)]
        public void WallShears_BalanceTheTorsion_Signed(double west, double east, double vx, double vy)
        {
            var calc = Box(west, east, vx, vy);
            var ws = calc._wall_system;
            double resisting =
                ws.NS_Walls.Keys.Sum(id => ws.X_bar_walls[id] * calc.TotalWallShear[id]) -
                ws.EW_Walls.Keys.Sum(id => ws.Y_bar_walls[id] * calc.TotalWallShear[id]);
            Assert.AreEqual(0.0, resisting + calc.Mt_comb, 1e-3 * Math.Max(1, Math.Abs(calc.Mt_comb)));
        }

        /// <summary>
        /// Hand check: stiff west wall puts the CR west of the CM, so a +y load at the CM turns the diaphragm
        /// CCW about the CR. The east wall, on the far side, moves further in +y and must pick up torsional
        /// shear (more than its direct share); the west wall is relieved.
        /// </summary>
        [TestMethod]
        public void Torsion_LoadsTheWallAwayFromTheCenterOfRigidity()
        {
            var calc = Box(westLength: 16, eastLength: 4, vx: 0, vy: 10);
            Assert.IsTrue(calc.Mt_comb > 0, "load east of the CR turns the diaphragm CCW");
            var ns = calc._wall_system.NS_Walls.OrderBy(w => w.Value.Center.X).Select(w => w.Key).ToArray();
            int west = ns[0], east = ns[1];
            Assert.IsTrue(Math.Abs(calc.TotalWallShear[east]) > Math.Abs(calc.DirectShear_Y[east]), "east wall gains");
            Assert.IsTrue(Math.Abs(calc.TotalWallShear[west]) < Math.Abs(calc.DirectShear_Y[west]), "west wall is relieved");
            Assert.AreEqual(-10.0, calc.TotalWallShear[west] + calc.TotalWallShear[east], 1e-6, "still reacts Vy");
        }

        [TestMethod]
        public void TotalShear_IsDirectPlusTorsional()
        {
            var calc = Box(12, 6, 4, 9);
            foreach (var id in calc.TotalWallShear.Keys)
            {
                double direct = calc.DirectShear_X.TryGetValue(id, out var dx) ? dx : calc.DirectShear_Y[id];
                Assert.AreEqual(direct + calc.EccentricShear[id], calc.TotalWallShear[id], 1e-12);
            }
        }

        [TestMethod]
        public void DisplayResults_ListsEveryWall()
        {
            var calc = Box(12, 6, 4, 9);
            string table = calc.DisplayResults();
            foreach (var id in calc.TotalWallShear.Keys)
                StringAssert.Contains(table, "\n" + id + "   |");
        }

        [TestMethod]
        public void Constructor_WithSystemsAndLoads_KeepsThem()
        {
            var walls = new WallSystem();
            var diaphragms = new DiaphragmSystem();
            var copy = new TestableCalculator(walls, diaphragms, 3.5, 7.25);
            Assert.AreSame(walls, copy._wall_system);
            Assert.AreSame(diaphragms, copy._diaphragm_system);
            Assert.AreEqual(3.5, copy.V_x);
            Assert.AreEqual(7.25, copy.V_y);
        }

        private sealed class TestableCalculator : ShearWallCalculatorBase
        {
            public TestableCalculator(WallSystem w, DiaphragmSystem d, double vx, double vy) : base(w, d, vx, vy) { }
            public override void PerformCalculations() { }
        }
    }
}
