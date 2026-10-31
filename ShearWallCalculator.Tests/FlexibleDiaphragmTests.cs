using System;
using System.Linq;
using System.Windows;
using calculator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShearWallCalculator;

namespace ShearWallCalculator.Tests
{
    /// <summary>
    /// Flexible diaphragm against hand calculations: the load is a uniform line load across the plan, the diaphragm spans as
    /// simple beams between wall lines (tributary widths), and the walls on a line share its reaction by rigidity. Wall shears
    /// are the force on the diaphragm, opposite to the load, as in the rigid calculation.
    /// </summary>
    [TestClass]
    public class FlexibleDiaphragmTests
    {
        private const double H = 9.0;

        private static double Rigidity(double h, double L) => 1.0 / (0.4 * Math.Pow(h / L, 3) + 0.3 * (h / L));

        /// <summary>
        /// 40 x 20 ft plan. N-S lines at x = 0 (10 ft wall), x = 10 (10 ft wall) and x = 40 (a 10 ft and a 5 ft wall);
        /// E-W lines at y = 0 and y = 20.
        /// </summary>
        private static ShearWallCalculator_FlexibleDiaphragm ThreeLines()
        {
            var calc = new ShearWallCalculator_FlexibleDiaphragm();
            calc._wall_system.AddWall(new WallData(H, 0, 5, 0, 15));
            calc._wall_system.AddWall(new WallData(H, 10, 5, 10, 15));
            calc._wall_system.AddWall(new WallData(H, 40, 2, 40, 12));
            calc._wall_system.AddWall(new WallData(H, 40, 14, 40, 19));
            calc._wall_system.AddWall(new WallData(H, 15, 0, 25, 0));
            calc._wall_system.AddWall(new WallData(H, 15, 20, 25, 20));
            calc._diaphragm_system.AddDiaphragm(new DiaphragmData_Rectangular(new Point(0, 0), new Point(40, 20)));
            return calc;
        }

        private static int WallAt(ShearWallCalculatorBase calc, double x, double y) =>
            calc._wall_system._walls.Single(w => Math.Abs(w.Value.Center.X - x) < 1e-6 && Math.Abs(w.Value.Center.Y - y) < 1e-6).Key;

        [TestMethod]
        public void TributaryWidths_HandCalculation_AndSigns()
        {
            // Vy = 8 k over 40 ft: w = 0.2 k/ft. Line x = 0: 0 to 5 ft -> 1.0 k; x = 10: 5 to 25 ft -> 4.0 k; x = 40: 25 to 40 ft -> 3.0 k.
            var calc = ThreeLines();
            calc.AddLoads(0, 8);
            calc.PerformCalculations();
            Assert.AreEqual(0, calc.Warnings.Count, string.Join(" | ", calc.Warnings));

            Assert.AreEqual(-1.0, calc.TotalWallShear[WallAt(calc, 0, 10)], 1e-9);
            Assert.AreEqual(-4.0, calc.TotalWallShear[WallAt(calc, 10, 10)], 1e-9);
            double k10 = Rigidity(H, 10), k5 = Rigidity(H, 5);
            Assert.AreEqual(-3.0 * k10 / (k10 + k5), calc.TotalWallShear[WallAt(calc, 40, 7)], 1e-6);
            Assert.AreEqual(-3.0 * k5 / (k10 + k5), calc.TotalWallShear[WallAt(calc, 40, 16.5)], 1e-6);

            var c = calc.CaseResults.Single();
            CollectionAssert.AreEqual(new[] { 5.0, 20.0, 15.0 }, c.LinesY.Select(l => Math.Round(l.Width, 9)).ToArray());
            Assert.AreEqual(-8.0, c.LinesY.Sum(l => l.Reaction), 1e-9, "the lines react the whole load");
            Assert.AreEqual(-8.0, calc._wall_system.NS_Walls.Keys.Sum(id => calc.TotalWallShear[id]), 1e-9);
        }

        [TestMethod]
        public void XLoad_SplitBetweenTwoLines_OppositeToTheLoad()
        {
            var calc = ThreeLines();
            calc.SetLoadCases(new[] { new LateralLoadCase("+x", 10, 0), new LateralLoadCase("-x", -10, 0) });
            calc.PerformCalculations();
            foreach (var id in calc._wall_system.EW_Walls.Keys)
            {
                Assert.AreEqual(-5.0, calc.CaseResults[0].Total[id], 1e-9);
                Assert.AreEqual(5.0, calc.CaseResults[1].Total[id], 1e-9);
                Assert.AreEqual(5.0, calc.WallShearRange[id].MaxPositive, 1e-9);
                Assert.AreEqual(-5.0, calc.WallShearRange[id].MaxNegative, 1e-9);
                Assert.AreEqual(5.0, calc.WallShearRange[id].Design, 1e-9);
            }
        }

        /// <summary>One wall line in a direction cannot support a flexible diaphragm: said plainly, not an exception.</summary>
        [TestMethod]
        public void OneLineInADirection_WarnsInsteadOfThrowing()
        {
            var calc = new ShearWallCalculator_FlexibleDiaphragm();
            calc._wall_system.AddWall(new WallData(H, 0, 5, 0, 15));
            calc._wall_system.AddWall(new WallData(H, 40, 5, 40, 15));
            calc._wall_system.AddWall(new WallData(H, 15, 0, 25, 0));
            calc._diaphragm_system.AddDiaphragm(new DiaphragmData_Rectangular(new Point(0, 0), new Point(40, 20)));
            calc.AddLoads(10, 8);
            calc.PerformCalculations();
            Assert.IsTrue(calc.Warnings.Any(w => w.StartsWith("East-west")), string.Join(" | ", calc.Warnings));
            Assert.IsFalse(calc.TotalWallShear.ContainsKey(WallAt(calc, 20, 0)));
            Assert.AreEqual(-4.0, calc.TotalWallShear[WallAt(calc, 0, 10)], 1e-9);
        }

        [TestMethod]
        public void TorsionalMoment_IsReportedAsNotApplied()
        {
            var calc = ThreeLines();
            calc.SetLoadCases(new[] { new LateralLoadCase("with Mt", 0, 8, 50) });
            calc.PerformCalculations();
            Assert.IsTrue(calc.Warnings.Any(w => w.Contains("Mt")));
        }
    }
}
