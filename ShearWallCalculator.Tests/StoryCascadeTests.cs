using System.Collections.Generic;
using System.Linq;
using System.Windows;
using calculator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ShearWallCalculator.Tests
{
    /// <summary>Multi-story, top story first: the walls above load the floor below with minus their shear, at their position.</summary>
    [TestClass]
    public class StoryCascadeTests
    {
        private const double Tol = 1e-9;

        /// <summary>A 40 × 30 ft rigid level with the given walls ((x1, y1, x2, y2), 9 ft high).</summary>
        private static ShearWallCalculator_RigidDiaphragm Level(params (double, double, double, double)[] walls)
        {
            var c = new ShearWallCalculator_RigidDiaphragm();
            c._diaphragm_system.AddDiaphragm(new DiaphragmData_Rectangular(new Point(0, 0), new Point(40, 30)));
            foreach (var w in walls) c._wall_system.AddWall(new WallData(9, w.Item1, w.Item2, w.Item3, w.Item4));
            return c;
        }

        private static readonly (double, double, double, double)[] Box =
            { (0, 0, 40, 0), (0, 30, 40, 30), (0, 0, 0, 30), (40, 0, 40, 30) };

        private static double Sum(ShearWallCalculator_RigidDiaphragm c, string name, WallDirs dir) =>
            c.CaseResults.Single(r => r.Name == name).Total.Where(kv => c._wall_system._walls[kv.Key].WallDir == dir).Sum(kv => kv.Value);

        /// <summary>Stacked symmetric boxes: 10 k on the roof, 8 k on the floor. Roof walls 5 k each; the floor walls 9 k each.</summary>
        [TestMethod]
        public void StackedWalls_StoryShearAddsUp()
        {
            var floor = Level(Box);
            var roof = Level(Box);
            roof.SetLoadCases(new[] { new LateralLoadCase("W", 10, 0) });
            floor.SetLoadCases(new[] { new LateralLoadCase("W", 8, 0) });
            var notes = StoryCascade.Analyze(new List<ShearWallCalculatorBase> { floor, roof });
            Assert.AreEqual(0, notes.Count, string.Join("\n", notes));

            var top = roof.CaseResults.Single();
            Assert.AreEqual(-5, top.Total[0], Tol);
            Assert.AreEqual(-5, top.Total[1], Tol);
            var bottom = floor.CaseResults.Single();
            Assert.AreEqual(18, bottom.Vx, Tol, "own 8 k + 10 k from the walls above");
            Assert.AreEqual(-9, bottom.Total[0], Tol);
            Assert.AreEqual(-9, bottom.Total[1], Tol);
            Assert.AreEqual(0, bottom.Total[2], Tol, "N-S walls take no x load (no torsion: symmetric)");

            var loads = floor.LoadsFromAbove["W"];
            Assert.AreEqual(4, loads.Count);
            var fromWall0 = loads.Single(p => p.From == "level 2, wall 0");
            Assert.AreEqual(new Point(20, 0), new Point(fromWall0.X, fromWall0.Y));
            Assert.AreEqual(5, fromWall0.Fx, Tol, "minus the wall's shear: it pushes the floor downwind");
        }

        /// <summary>
        /// Equilibrium of the story above: its walls' reactions add up to its load, acting at its center of mass, with its case
        /// torsion. The upper walls are uneven (center of rigidity off center), so the individual shears include torsion, yet
        /// the moment of the reactions about the (same) center of mass below must be exactly the upper case Mt.
        /// </summary>
        [TestMethod]
        public void UpperReactions_ActAtTheUpperCenterOfMass()
        {
            var roof = Level((0, 0, 40, 0), (0, 30, 12, 30), (0, 0, 0, 30), (40, 0, 40, 8));
            var floor = Level(Box);
            roof.SetLoadCases(new[] { new LateralLoadCase("A", 6, -4, 25), new LateralLoadCase("B", -3, 7, -40) });
            floor.SetLoadCases(new[] { new LateralLoadCase("A", 2, 1), new LateralLoadCase("B", 1, 2) });
            StoryCascade.Analyze(new List<ShearWallCalculatorBase> { floor, roof });

            foreach (var (name, vx, vy, mt, ownX, ownY) in new[] { ("A", 6.0, -4.0, 25.0, 2.0, 1.0), ("B", -3.0, 7.0, -40.0, 1.0, 2.0) })
            {
                var c = floor.CaseResults.Single(r => r.Name == name);
                Assert.AreEqual(ownX + vx, c.Vx, 1e-5, name);
                Assert.AreEqual(ownY + vy, c.Vy, 1e-5, name);
                Assert.AreEqual(mt, c.Mt, 1e-5, name + ": the reactions' moment about the CM is the upper case torsion");
                // the floor walls resist the whole story shear (lever arms are stored as float: 1e-5)
                Assert.AreEqual(-(ownX + vx), Sum(floor, name, WallDirs.EastWest), 1e-5);
                Assert.AreEqual(-(ownY + vy), Sum(floor, name, WallDirs.NorthSouth), 1e-5);
            }
        }

        [TestMethod]
        public void DiscontinuousWall_Flagged_AndTheFloorSpreadsItsShear()
        {
            var roof = Level(Box);
            var floor = Level((0, 0, 40, 0), (0, 30, 40, 30), (0, 0, 0, 30), (30, 0, 30, 30)); // N-S wall moved in from x = 40 to x = 30
            roof.SetLoadCases(new[] { new LateralLoadCase("Y", 0, 10) });
            floor.SetLoadCases(new[] { new LateralLoadCase("Y", 0, 0) });
            var notes = StoryCascade.Analyze(new List<ShearWallCalculatorBase> { floor, roof });
            Assert.AreEqual(1, notes.Count);
            StringAssert.Contains(notes[0], "Level 2, wall 3: no wall on level 1");

            // roof: symmetric, 5 k in each N-S wall; on the floor the 10 k act at x = 20 (resultant), CR at x = 15
            var c = floor.CaseResults.Single();
            Assert.AreEqual(10, c.Vy, Tol);
            Assert.AreEqual(0, c.Mt, Tol);
            Assert.AreEqual(-5, c.EccX, 1e-6, "ecc_x = x_CR - x_CM = 15 - 20");
            Assert.AreEqual(-10, Sum(floor, "Y", WallDirs.NorthSouth), 1e-6);
            Assert.IsTrue(c.Total[3] < c.Total[2], "the wall nearer the load (x = 30) takes more");
        }

        [TestMethod]
        public void ManualLoadBelow_GoesWithEveryCaseAbove()
        {
            var own = new List<LateralLoadCase> { new LateralLoadCase("Manual load", 3, 4, 0, applyMinimumEccentricity: true) };
            var above = new Dictionary<string, List<PointLoad>>
            {
                ["Case 1"] = new List<PointLoad> { new PointLoad { X = 10, Y = 15, Fx = 2 } },
                ["Case 2"] = new List<PointLoad> { new PointLoad { X = 20, Y = 25, Fy = -1 } },
            };
            var r = StoryCascade.WithLoadsFromAbove(own, above, new Point(20, 15));
            Assert.AreEqual(2, r.Count);
            Assert.AreEqual(5, r[0].Vx, Tol);
            Assert.AreEqual(4, r[0].Vy, Tol);
            Assert.AreEqual(0, r[0].Mt, Tol, "on the CM's line");
            Assert.AreEqual(3, r[1].Vy, Tol);
            Assert.AreEqual(0, r[1].Mt, Tol);
            Assert.IsTrue(r.All(c => c.ApplyMinimumEccentricity));

            // off the CM: Fx = 2 at y = 25 (10 ft above it) turns clockwise: −10 × 2
            var m = StoryCascade.WithLoadsFromAbove(new List<LateralLoadCase> { new LateralLoadCase("K", 0, 0) },
                new Dictionary<string, List<PointLoad>> { ["K"] = new List<PointLoad> { new PointLoad { X = 0, Y = 25, Fx = 2 } } }, new Point(20, 15));
            Assert.AreEqual(-20, m.Single().Mt, Tol);
            Assert.AreEqual(-20, m.Single().AboveMt, Tol);
            Assert.AreEqual(own, StoryCascade.WithLoadsFromAbove(own, new Dictionary<string, List<PointLoad>>(), new Point()), "nothing above: unchanged");
        }

        [TestMethod]
        public void SupportedLength_UnionOfWallsOnTheSameLine()
        {
            var w = new WallData(9, 0, 10, 20, 10);
            var below = new[]
            {
                new WallData(9, -5, 10.1, 6, 10.1),   // 0-6 on the line (within 0.25 ft)
                new WallData(9, 4, 10, 9, 10),        // 4-9: overlaps the first
                new WallData(9, 12, 11, 20, 11),      // 1 ft off the line: not under it
                new WallData(9, 15, 0, 15, 20),       // crosses it: N-S, not under it
            };
            Assert.AreEqual(9, StoryCascade.SupportedLength(w, below), 1e-9);
        }

        [TestMethod]
        public void CopyFloor_CopiesWallsWithDesignAndDiaphragms()
        {
            var floor = Level(Box);
            floor._wall_system._walls[1].Design.EdgeSpacing = 3;
            var roof = new ShearWallCalculator_RigidDiaphragm();
            Assert.AreEqual(5, ShearWallCalculator.Drawing.DrawingEdits.CopyFloor(floor, roof));
            Assert.AreEqual(4, roof._wall_system._walls.Count);
            Assert.AreEqual(1, roof._diaphragm_system._diaphragms.Count);
            Assert.AreEqual(3, roof._wall_system._walls[1].Design.EdgeSpacing);
            Assert.AreNotSame(floor._wall_system._walls[1], roof._wall_system._walls[1], "copies, not shared");
            Assert.AreEqual(30, StoryCascade.SupportedLength(roof._wall_system._walls[3], floor._wall_system._walls.Values), 1e-9, "stacked: fully supported");
        }

        [TestMethod]
        public void OneLevel_SameAsBefore()
        {
            var a = Level(Box); var b = Level(Box);
            a.SetLoadCases(new[] { new LateralLoadCase("W", 7, 3, 12) });
            b.SetLoadCases(new[] { new LateralLoadCase("W", 7, 3, 12) });
            b.PerformCalculations();
            StoryCascade.Analyze(new List<ShearWallCalculatorBase> { a });
            foreach (var kv in b.TotalWallShear) Assert.AreEqual(kv.Value, a.TotalWallShear[kv.Key], 1e-12);
        }

        /// <summary>End to end: two-story wind, split by strip, cascaded down. Level 1's walls carry the whole building's base shear in every case.</summary>
        [TestMethod]
        public void TwoStoryWind_Level1WallsCarryTheBaseShear()
        {
            var b = new ASCE7WindLoadCalculator.BuildingData
            {
                BuildingLength = 40, BuildingWidth = 30, BuildingHeight = 20, NumberOfStories = 2,
                RoofType = ASCE7WindLoadCalculator.RoofTypes.ROOF_TYPE_FLAT, RidgeDirection = ASCE7WindLoadCalculator.RidgeDirections.RIDGE_DIR_NONE,
                EnclosureType = ASCE7WindLoadCalculator.BuildingEnclosures.BLDG_ENCLOSED,
            };
            var a = ASCE7WindLoadCalculator.WindAnalysis.Run(ASCE7WindLoadCalculator.ASCE7_Versions.ASCE_VER_7_22,
                new ASCE7WindLoadCalculator.WindParameters_Base { WindSpeed = 115, ExposureCategory = ASCE7WindLoadCalculator.WindExposureCategories.WIND_EXP_CAT_C }, b);
            var r = ASCE7WindLoadCalculator.StoryWindLoads.Compute(a, false, b.NumberOfStories);
            var floor = Level(Box);
            var roof = Level((0, 0, 40, 0), (0, 30, 20, 30), (0, 0, 0, 30), (40, 0, 40, 30)); // uneven above: torsion goes down too
            floor.SetLoadCases(r.Levels[0].LoadCases.Select(c => new LateralLoadCase(c.Name, c.Vx, c.Vy, c.Mt)));
            roof.SetLoadCases(r.Levels[1].LoadCases.Select(c => new LateralLoadCase(c.Name, c.Vx, c.Vy, c.Mt)));
            var notes = StoryCascade.Analyze(new List<ShearWallCalculatorBase> { floor, roof });
            Assert.AreEqual(0, notes.Count, string.Join("; ", notes));
            Assert.IsTrue(r.Building.LoadCases.Count >= 4);
            foreach (var wc in r.Building.LoadCases)
            {
                Assert.AreEqual(-wc.Vx, Sum(floor, wc.Name, WallDirs.EastWest), 1e-4, wc.Name);
                Assert.AreEqual(-wc.Vy, Sum(floor, wc.Name, WallDirs.NorthSouth), 1e-4, wc.Name);
                var top = r.Levels[1].LoadCases.Single(c => c.Name == wc.Name);
                Assert.AreEqual(-top.Vx, Sum(roof, wc.Name, WallDirs.EastWest), 1e-4, wc.Name);
                Assert.AreEqual(wc.Mt, floor.CaseResults.Single(c => c.Name == wc.Name).Mt, 1e-4, "same CM on both levels: the torsions add up");
            }
        }

        [TestMethod]
        public void ProjectFile_KeepsTheUpperLevels()
        {
            var floor = Level(Box);
            var roof = Level((0, 0, 40, 0), (0, 30, 40, 30), (0, 0, 0, 30), (40, 0, 40, 30), (20, 0, 20, 30));
            var json = ShearWallCalculator.Interfaces.ProjectSerializer.ToJson(floor, null, true, new[] { roof });
            StringAssert.Contains(json, "\"FormatVersion\": 3");
            var read = ShearWallCalculator.Interfaces.ProjectSerializer.Read(json);
            Assert.AreEqual(4, read.Calculator._wall_system._walls.Count);
            Assert.AreEqual(1, read.UpperLevels.Count);
            Assert.AreEqual(5, read.UpperLevels[0]._wall_system._walls.Count);
            Assert.AreEqual(1, read.UpperLevels[0]._diaphragm_system._diaphragms.Count);
            Assert.AreEqual(0, ShearWallCalculator.Interfaces.ProjectSerializer.Read(ShearWallCalculator.Interfaces.ProjectSerializer.ToJson(floor, null, true)).UpperLevels.Count, "one story: no upper levels");
        }

        [TestMethod]
        public void EmptyOrUnfinishedUpperLevel_LevelOneAsBefore()
        {
            foreach (var roof in new[] { new ShearWallCalculator_RigidDiaphragm(), Level((0, 0, 40, 0)) })
            {
                var a = Level(Box); var b = Level(Box);
                a.SetLoadCases(new[] { new LateralLoadCase("W", 7, 3, 12) });
                b.SetLoadCases(new[] { new LateralLoadCase("W", 7, 3, 12) });
                roof.SetLoadCases(new[] { new LateralLoadCase("W", 5, 0) });
                b.PerformCalculations();
                var notes = StoryCascade.Analyze(new List<ShearWallCalculatorBase> { a, roof });
                foreach (var kv in b.TotalWallShear) Assert.AreEqual(kv.Value, a.TotalWallShear[kv.Key], 1e-12);
                Assert.AreEqual(roof._wall_system._walls.Count == 0 ? 0 : 1, notes.Count(n => n.Contains("cannot be calculated yet")));
            }
        }
    }
}
