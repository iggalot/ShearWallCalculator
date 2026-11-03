using System.Collections.Generic;
using System.Linq;
using System.Windows;
using calculator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ShearWallCalculator.Tests
{
    /// <summary>
    /// The wall-by-case breakdown (Calculations tab, plan Loads overlay) against the calculation it reads: every case its own
    /// Vx, Vy, Mt; own + above = the case's load; direct + torsion = total; the walls react the whole case; the governing case
    /// is the one with the design shear.
    /// </summary>
    [TestClass]
    public class CaseBreakdownTests
    {
        private const double Tol = 1e-5;

        private static T Level<T>(T c, params (double, double, double, double)[] walls) where T : ShearWallCalculatorBase
        {
            c._diaphragm_system.AddDiaphragm(new DiaphragmData_Rectangular(new Point(0, 0), new Point(40, 30)));
            foreach (var w in walls) c._wall_system.AddWall(new WallData(9, w.Item1, w.Item2, w.Item3, w.Item4));
            return c;
        }

        private static readonly (double, double, double, double)[] Box =
            { (0, 0, 40, 0), (0, 30, 40, 30), (0, 0, 0, 30), (40, 0, 40, 30) };

        /// <summary>Uneven roof walls (torsion), a box floor, two cases that would cancel if mixed.</summary>
        private static (ShearWallCalculatorBase floor, ShearWallCalculatorBase roof) TwoStory<T>(System.Func<T> make) where T : ShearWallCalculatorBase
        {
            var roof = Level(make(), (0, 0, 40, 0), (0, 30, 12, 30), (0, 0, 0, 30), (40, 0, 40, 8));
            var floor = Level(make(), Box);
            roof.SetLoadCases(new[] { new LateralLoadCase("A", 6, 0, 25), new LateralLoadCase("B", 0, -7, -40) });
            floor.SetLoadCases(new[] { new LateralLoadCase("A", 2, 0), new LateralLoadCase("B", 0, -3) });
            StoryCascade.Analyze(new List<ShearWallCalculatorBase> { floor, roof });
            return (floor, roof);
        }

        [TestMethod]
        public void Rigid_EveryWallEveryCase_AddsUpToTheCase()
        {
            var (floor, _) = TwoStory(() => new ShearWallCalculator_RigidDiaphragm());
            var rows = CaseBreakdown.Rows(floor);
            CollectionAssert.AreEqual(new[] { "A", "B" }, CaseBreakdown.CaseNames(floor));
            Assert.AreEqual(4 * 2, rows.Count, "4 walls × 2 cases");

            var rigid = (ShearWallCalculator_RigidDiaphragm)floor;
            foreach (var r in rows)
            {
                var c = rigid.CaseResults.Single(x => x.Name == r.Case);
                Assert.AreEqual(c.Total[r.Wall], r.Total, Tol);
                Assert.AreEqual(r.Total, r.Direct + r.Torsion, Tol);
                Assert.AreEqual(c.Vx, r.OwnVx + r.AboveVx, Tol, "own + above = the case's Vx");
                Assert.AreEqual(c.Vy, r.OwnVy + r.AboveVy, Tol);
                Assert.AreEqual(c.MtComb, r.MtComb.Value, Tol);
            }
            // own loads are this level's, unmixed: case A has no Vy of its own, case B no Vx
            var a = rows.First(r => r.Case == "A");
            var b = rows.First(r => r.Case == "B");
            Assert.AreEqual(2, a.OwnVx, Tol); Assert.AreEqual(0, a.OwnVy, Tol);
            Assert.AreEqual(0, b.OwnVx, Tol); Assert.AreEqual(-3, b.OwnVy, Tol);
            Assert.AreEqual(6, a.AboveVx, Tol); Assert.AreEqual(0, a.AboveVy, Tol, "case A from above is the roof's case A only");
            Assert.AreEqual(-7, b.AboveVy, Tol);

            // the walls react each case on its own
            foreach (var name in new[] { "A", "B" })
            {
                var c = rigid.CaseResults.Single(x => x.Name == name);
                Assert.AreEqual(-c.Vx, rows.Where(r => r.Case == name && floor._wall_system._walls[r.Wall].WallDir == WallDirs.EastWest).Sum(r => r.Total), Tol);
                Assert.AreEqual(-c.Vy, rows.Where(r => r.Case == name && floor._wall_system._walls[r.Wall].WallDir == WallDirs.NorthSouth).Sum(r => r.Total), Tol);
            }

            // one governing case per wall: the one with the design shear
            foreach (var g in rows.GroupBy(r => r.Wall))
            {
                Assert.AreEqual(1, g.Count(r => r.Governs), $"wall {g.Key}");
                Assert.AreEqual(floor.WallShearRange[g.Key].Design, System.Math.Abs(g.Single(r => r.Governs).Total), Tol);
            }
        }

        [TestMethod]
        public void Case_FromAbove_IsTheUpperWallsOfThatCase()
        {
            var (floor, roof) = TwoStory(() => new ShearWallCalculator_RigidDiaphragm());
            var b = CaseBreakdown.Case(floor, "B").Value;
            var upper = ((ShearWallCalculator_RigidDiaphragm)roof).CaseResults.Single(c => c.Name == "B");
            Assert.AreEqual(upper.Total.Count, b.fromAbove.Count);
            Assert.AreEqual(-7, b.fromAbove.Sum(p => p.Fy), Tol, "the roof's case B, nothing of case A");
            Assert.AreEqual(0, b.fromAbove.Sum(p => p.Fx), Tol);
            Assert.AreEqual(b.loadCase.Vy, -b.wallTotals.Where(kv => floor._wall_system._walls[kv.Key].WallDir == WallDirs.NorthSouth).Sum(kv => kv.Value), Tol);
            Assert.IsNull(CaseBreakdown.Case(floor, "nope"));
        }

        /// <summary>+x is wind from the west, +y from the south; the own wind decides, not what comes from above.</summary>
        [TestMethod]
        public void WindFrom_TheCaseDirection()
        {
            Assert.AreEqual("W", CaseBreakdown.WindFrom(new LateralLoadCase("a", 10, 0)));
            Assert.AreEqual("E", CaseBreakdown.WindFrom(new LateralLoadCase("a", -10, 0)));
            Assert.AreEqual("S", CaseBreakdown.WindFrom(new LateralLoadCase("a", 0, 8)));
            Assert.AreEqual("N", CaseBreakdown.WindFrom(new LateralLoadCase("a", 0, -8)));
            Assert.AreEqual("SW", CaseBreakdown.WindFrom(new LateralLoadCase("a", 5.6, 4.5, 30)));
            Assert.AreEqual("NE", CaseBreakdown.WindFrom(new LateralLoadCase("a", -5.6, -4.5)));
            Assert.AreEqual("W", CaseBreakdown.WindFrom(new LateralLoadCase("a", 12, 3) { AboveVx = 2, AboveVy = 3 }), "own: 10, 0");
            Assert.IsNull(CaseBreakdown.WindFrom(new LateralLoadCase("a", 0, 0)));
        }

        [TestMethod]
        public void Envelope_ListsBothRuns_Tagged()
        {
            var (floor, _) = TwoStory(() => new ShearWallCalculator_Envelope());
            CollectionAssert.AreEqual(new[] { "A (rigid)", "B (rigid)", "A (flexible)", "B (flexible)" }, CaseBreakdown.CaseNames(floor));
            var rows = CaseBreakdown.Rows(floor);
            Assert.AreEqual(4 * 4, rows.Count);
            Assert.IsTrue(rows.Where(r => r.Run == "flexible").All(r => r.MtComb == null));
            foreach (var g in rows.GroupBy(r => r.Wall))
                Assert.AreEqual(1, g.Count(r => r.Governs), $"wall {g.Key}");
            Assert.AreEqual(-7, CaseBreakdown.Case(floor, "B (flexible)").Value.fromAbove.Sum(p => p.Fy), Tol);
        }
    }
}
