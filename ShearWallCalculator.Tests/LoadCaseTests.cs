using System;
using System.Linq;
using System.Windows;
using calculator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShearWallCalculator;

namespace ShearWallCalculator.Tests
{
    /// <summary>Several design load cases (e.g. ASCE 7 Fig. 27.3-8): each wall reports its governing case.</summary>
    [TestClass]
    public class LoadCaseTests
    {
        private const double H = 9.0;

        private static T Box<T>(double west, double east) where T : ShearWallCalculatorBase, new()
        {
            var calc = new T();
            calc._wall_system.AddWall(new WallData(H, 0, 5, 0, 5 + west));
            calc._wall_system.AddWall(new WallData(H, 40, 5, 40, 5 + east));
            calc._wall_system.AddWall(new WallData(H, 15, 0, 25, 0));
            calc._wall_system.AddWall(new WallData(H, 15, 20, 25, 20));
            calc._diaphragm_system.AddDiaphragm(new DiaphragmData_Rectangular(new Point(0, 0), new Point(40, 20)));
            return calc;
        }

        [TestMethod]
        public void Rigid_EachWallKeepsItsLargestShear()
        {
            var calc = Box<ShearWallCalculator_RigidDiaphragm>(10, 10);
            calc.SetLoadCases(new[]
            {
                new LateralLoadCase("X only", 10, 0),
                new LateralLoadCase("Y only", 0, 6),
                new LateralLoadCase("Both 75%", 7.5, 4.5),
            });
            calc.PerformCalculations();

            foreach (var id in calc._wall_system.EW_Walls.Keys)
            {
                Assert.AreEqual(5.0, Math.Abs(calc.TotalWallShear[id]), 1e-9, "east-west walls: half of 10 k");
                Assert.AreEqual("X only", calc.GoverningLoadCase[id]);
            }
            foreach (var id in calc._wall_system.NS_Walls.Keys)
            {
                Assert.AreEqual(3.0, Math.Abs(calc.TotalWallShear[id]), 1e-9, "north-south walls: half of 6 k");
                Assert.AreEqual("Y only", calc.GoverningLoadCase[id]);
            }
            Assert.AreEqual(10, calc.V_x, 1e-12);
            Assert.AreEqual(6, calc.V_y, 1e-12);
        }

        /// <summary>Wind cases carry their own torsion and do not add the manual minimum eccentricity.</summary>
        [TestMethod]
        public void Rigid_WindCaseTorsion_IsResisted_NoMinimumEccentricity()
        {
            var calc = Box<ShearWallCalculator_RigidDiaphragm>(10, 10); // symmetric: CR at CM
            calc.SetLoadCases(new[] { new LateralLoadCase("Torsion only", 0, 0, mt: 12) });
            calc.PerformCalculations();
            var ws = calc._wall_system;
            double resisted = ws.EW_Walls.Keys.Sum(id => Math.Abs(calc.EccentricShear[id] * ws.Y_bar_walls[id]))
                            + ws.NS_Walls.Keys.Sum(id => Math.Abs(calc.EccentricShear[id] * ws.X_bar_walls[id]));
            Assert.AreEqual(12, resisted, 1e-6);

            calc.SetLoadCases(new[] { new LateralLoadCase("Wind X", 10, 0) });
            calc.PerformCalculations();
            Assert.IsTrue(calc.EccentricShear.Values.All(v => Math.Abs(v) < 1e-9), "symmetric building, no case torsion: no torsional shear");
        }

        [TestMethod]
        public void Rigid_ManualLoad_UnchangedBehavior()
        {
            var a = Box<ShearWallCalculator_RigidDiaphragm>(12, 6);
            a.AddLoads(4, 9);
            a.PerformCalculations();

            var b = Box<ShearWallCalculator_RigidDiaphragm>(12, 6);
            b.SetLoadCases(new[] { new LateralLoadCase("same", 4, 9, 0, applyMinimumEccentricity: true) });
            b.PerformCalculations();

            foreach (var id in a.TotalWallShear.Keys)
                Assert.AreEqual(a.TotalWallShear[id], b.TotalWallShear[id], 1e-12);
            Assert.AreEqual(0, a.LoadCases.Count, "AddLoads replaces design cases with the single manual load");
        }

        /// <summary>
        /// Flexible diaphragm, tributary widths: two north-south lines at x = 0 and x = 40 each take half of
        /// a Y load; two east-west lines at y = 0 and y = 20 each take half of an X load. The Y direction used
        /// to mix plan axes and doubled the load.
        /// </summary>
        [TestMethod]
        public void Flexible_TributaryDistribution_BothDirections()
        {
            var calc = Box<ShearWallCalculator_FlexibleDiaphragm>(10, 10);
            calc.AddLoads(10, 6);
            calc.PerformCalculations();
            foreach (var id in calc._wall_system.NS_Walls.Keys) Assert.AreEqual(3.0, Math.Abs(calc.TotalWallShear[id]), 1e-6);
            foreach (var id in calc._wall_system.EW_Walls.Keys) Assert.AreEqual(5.0, Math.Abs(calc.TotalWallShear[id]), 1e-6);
        }

        [TestMethod]
        public void Flexible_EnvelopeOfCases()
        {
            var calc = Box<ShearWallCalculator_FlexibleDiaphragm>(10, 10);
            calc.SetLoadCases(new[] { new LateralLoadCase("X", 10, 0), new LateralLoadCase("75% both", 7.5, 4.5), new LateralLoadCase("Y", 0, 6) });
            calc.PerformCalculations();
            Assert.AreEqual(10.0, calc._wall_system.EW_Walls.Keys.Sum(id => Math.Abs(calc.TotalWallShear[id])), 1e-6);
            Assert.AreEqual(6.0, calc._wall_system.NS_Walls.Keys.Sum(id => Math.Abs(calc.TotalWallShear[id])), 1e-6);
            Assert.IsTrue(calc.GoverningLoadCase.Values.All(n => n == "X" || n == "Y"));
        }
    }
}
