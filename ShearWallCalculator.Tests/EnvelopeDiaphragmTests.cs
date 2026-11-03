using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using BuildingModel.Geometry;
using BuildingModel.Model;
using BuildingModel.Serialization;
using calculator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShearWallCalculator.Design;
using ShearWallCalculator.ModelAdapter;
using ShearWallCalculator.NDSProvisions;

namespace ShearWallCalculator.Tests
{
    /// <summary>
    /// The project's diaphragm setting: rigid, flexible or the envelope of both. The envelope is checked against the rigid and
    /// flexible calculations run on their own (each of those is checked against hand calculations in its own tests).
    /// </summary>
    [TestClass]
    public class EnvelopeDiaphragmTests
    {
        /// <summary>
        /// 40 × 20 ft; N-S walls at x = 0, 10 and 40 (two there), E-W walls at y = 0 and 20 (the flexible tests' plan). The
        /// N-S lines are unevenly spaced, so the rigid and flexible shares differ and each governs some wall.
        /// </summary>
        private static T Plan<T>(T calc) where T : ShearWallCalculatorBase
        {
            calc._wall_system.AddWall(new WallData(9, 0, 5, 0, 15));
            calc._wall_system.AddWall(new WallData(9, 10, 5, 10, 15));
            calc._wall_system.AddWall(new WallData(9, 40, 2, 40, 12));
            calc._wall_system.AddWall(new WallData(9, 40, 14, 40, 19));
            calc._wall_system.AddWall(new WallData(9, 15, 0, 25, 0));
            calc._wall_system.AddWall(new WallData(9, 15, 20, 25, 20));
            calc._diaphragm_system.AddDiaphragm(new DiaphragmData_Rectangular(new Point(0, 0), new Point(40, 20)));
            return calc;
        }

        private static readonly LateralLoadCase[] Cases =
        {
            new LateralLoadCase("+y", 0, 8, 0, applyMinimumEccentricity: true), new LateralLoadCase("-y", 0, -8, 0, applyMinimumEccentricity: true),
            new LateralLoadCase("+x", 6, 0, 0, applyMinimumEccentricity: true), new LateralLoadCase("-x", -6, 0, 0, applyMinimumEccentricity: true),
        };

        [TestMethod]
        public void Envelope_EachWallTakesTheLargerOfRigidAndFlexible()
        {
            var rigid = Plan(new ShearWallCalculator_RigidDiaphragm());
            var flex = Plan(new ShearWallCalculator_FlexibleDiaphragm());
            var env = Plan(new ShearWallCalculator_Envelope());
            foreach (var c in new ShearWallCalculatorBase[] { rigid, flex, env })
            {
                c.SetLoadCases(Cases);
                c.PerformCalculations();
            }

            Assert.AreEqual(6, env.TotalWallShear.Count);
            foreach (int id in env._wall_system._walls.Keys)
            {
                double r = rigid.TotalWallShear[id], f = flex.TotalWallShear[id];
                bool rigidGoverns = Math.Abs(r) >= Math.Abs(f);
                Assert.AreEqual(rigidGoverns ? r : f, env.TotalWallShear[id], 1e-9, $"wall {id}");
                Assert.AreEqual(rigidGoverns ? "rigid" : "flexible", env.Governs[id]);
                Assert.AreEqual(Math.Max(rigid.WallShearRange[id].Design, flex.WallShearRange[id].Design), env.WallShearRange[id].Design, 1e-9);
                Assert.AreEqual(Math.Max(rigid.WallShearRange[id].MaxPositive, flex.WallShearRange[id].MaxPositive), env.WallShearRange[id].MaxPositive, 1e-9);
                Assert.AreEqual(Math.Min(rigid.WallShearRange[id].MaxNegative, flex.WallShearRange[id].MaxNegative), env.WallShearRange[id].MaxNegative, 1e-9);
                string run = rigidGoverns ? "rigid" : "flexible";
                string name = (rigidGoverns ? (ShearWallCalculatorBase)rigid : flex).GoverningLoadCase[id];
                Assert.AreEqual($"{name} ({run})", env.GoverningLoadCase[id]);
            }
            Assert.IsTrue(env.Governs.Values.Contains("rigid") && env.Governs.Values.Contains("flexible"),
                "on this plan each calculation governs some wall: " + string.Join(", ", env.Governs.Select(g => $"{g.Key}:{g.Value}")));

            // the two runs are the stand-alone calculations
            foreach (int id in env._wall_system._walls.Keys)
            {
                Assert.AreEqual(rigid.TotalWallShear[id], env.Rigid.TotalWallShear[id], 1e-9);
                Assert.AreEqual(flex.TotalWallShear[id], env.Flexible.TotalWallShear[id], 1e-9);
            }
        }

        [TestMethod]
        public void Ensure_ChangesTheKind_KeepsWallsDiaphragmsLoadsAndSettings()
        {
            var rigid = Plan(new ShearWallCalculator_RigidDiaphragm());
            rigid.SetLoadCases(Cases);
            rigid.DesignSettings.Method = DesignMethods.LRFD;
            Assert.AreSame(rigid, DiaphragmAnalysis.Ensure(rigid, DiaphragmAssumption.Rigid));

            foreach (var kind in new[] { DiaphragmAssumption.Flexible, DiaphragmAssumption.Envelope })
            {
                var other = DiaphragmAnalysis.Ensure(rigid, kind);
                Assert.AreEqual(kind, DiaphragmAnalysis.KindOf(other));
                Assert.AreSame(rigid._wall_system, other._wall_system);
                Assert.AreSame(rigid._diaphragm_system, other._diaphragm_system);
                Assert.AreEqual(4, other.LoadCases.Count);
                Assert.AreEqual(8, other.V_y, 1e-12);
                Assert.AreSame(rigid.DesignSettings, other.DesignSettings);
            }
            Assert.IsNull(DiaphragmAnalysis.Ensure(null, DiaphragmAssumption.Envelope));
        }

        /// <summary>Two stories: the envelope's rigid run is the all-rigid building, its flexible run the all-flexible one.</summary>
        [TestMethod]
        public void TwoStories_EachRunCarriesItsOwnReactionsDown()
        {
            List<ShearWallCalculatorBase> Run(DiaphragmAssumption kind)
            {
                var floor = Plan(DiaphragmAnalysis.Create(kind));
                var roof = Plan(DiaphragmAnalysis.Create(kind));
                roof.SetLoadCases(Cases);
                floor.SetLoadCases(Cases.Select(c => new LateralLoadCase(c.Name, c.Vx * 1.5, c.Vy * 1.5, 0, applyMinimumEccentricity: true)));
                var levels = new List<ShearWallCalculatorBase> { floor, roof };
                StoryCascade.Analyze(levels);
                return levels;
            }
            var rigid = Run(DiaphragmAssumption.Rigid);
            var flex = Run(DiaphragmAssumption.Flexible);
            var env = Run(DiaphragmAssumption.Envelope);
            var lower = (ShearWallCalculator_Envelope)env[0];
            Assert.IsTrue(lower.LoadsFromAbove.Count > 0 && lower.FlexibleLoadsFromAbove.Count > 0);
            foreach (int id in lower._wall_system._walls.Keys)
            {
                Assert.AreEqual(rigid[0].TotalWallShear[id], lower.Rigid.TotalWallShear[id], 1e-9, $"rigid, wall {id}");
                Assert.AreEqual(flex[0].TotalWallShear[id], lower.Flexible.TotalWallShear[id], 1e-9, $"flexible, wall {id}");
                Assert.AreEqual(Math.Max(Math.Abs(rigid[0].TotalWallShear[id]), Math.Abs(flex[0].TotalWallShear[id])), Math.Abs(lower.TotalWallShear[id]), 1e-9);
            }
        }

        /// <summary>The setting is saved with the project's design settings and gives every story a calculator of that kind.</summary>
        [TestMethod]
        public void Setting_SavedInTheModel_EveryStoryGetsThatKind()
        {
            var b = Building.New();
            var l2 = b.AddLevel("Level 2", 12);
            var roof = b.AddLevel("Roof", 22);
            var lv = new[] { b.Ground, l2, roof };
            for (int i = 1; i < lv.Length; i++)
            {
                b.AddDiaphragm(lv[i], new Rect2(0, 0, 60, 40));
                b.AddWall(lv[i - 1], lv[i], 0, 0, 60, 0);
                b.AddWall(lv[i - 1], lv[i], 0, 40, 60, 40);
                b.AddWall(lv[i - 1], lv[i], 0, 0, 0, 40);
                b.AddWall(lv[i - 1], lv[i], 60, 0, 60, 40);
            }

            var s = ShearWallAdapter.ToCalculators(b);
            Assert.IsTrue(s.All(x => x.Calculator is ShearWallCalculator_Envelope), "the default: the envelope of rigid and flexible");
            s[0].Calculator.V_x = 7;
            s[0].Calculator.DesignSettings.Diaphragm = DiaphragmAssumption.Rigid;
            Assert.IsTrue(ShearWallAdapter.StoreDesign(b, s));

            var reused = ShearWallAdapter.ToCalculators(b, s.Select(x => x.Calculator).ToList());
            Assert.IsTrue(reused.All(x => x.Calculator is ShearWallCalculator_RigidDiaphragm));
            Assert.AreEqual(7, reused[0].Calculator.V_x, 1e-12, "the level's load stays");

            var reopened = ShearWallAdapter.ToCalculators(ModelFile.Clone(b));
            Assert.AreEqual(DiaphragmAssumption.Rigid, reopened[0].Calculator.DesignSettings.Diaphragm, "a project keeps the one it saved");
            Assert.IsTrue(reopened.All(x => x.Calculator is ShearWallCalculator_RigidDiaphragm));

            reopened[0].Calculator.DesignSettings.Diaphragm = DiaphragmAssumption.Flexible;
            ShearWallAdapter.StoreDesign(b, reopened);
            Assert.IsTrue(ShearWallAdapter.ToCalculators(ModelFile.Clone(b)).All(x => x.Calculator is ShearWallCalculator_FlexibleDiaphragm));
        }
    }
}
