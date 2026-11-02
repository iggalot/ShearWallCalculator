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

        /// <summary>
        /// An upper wall over a beam is carried (no note); an upper wall past the edge of the floor below is flagged, beam or
        /// not, since a beam takes the wall's weight and uplift but not its shear (the junk project: the floor stops at x = 46,
        /// the upper walls run to 59.5 over beams).
        /// </summary>
        [TestMethod]
        public void UpperWall_OverABeam_IsCarried_ButPastTheFloorEdge_IsFlagged()
        {
            var roof = new ShearWallCalculator_RigidDiaphragm();
            roof._diaphragm_system.AddDiaphragm(new DiaphragmData_Rectangular(new Point(0, 0), new Point(60, 30)));
            foreach (var w in new[] { (0.0, 0.0, 60.0, 0.0), (0.0, 30.0, 60.0, 30.0), (0.0, 0.0, 0.0, 30.0), (60.0, 0.0, 60.0, 30.0) })
                roof._wall_system.AddWall(new WallData(9, w.Item1, w.Item2, w.Item3, w.Item4));
            var floor = new ShearWallCalculator_RigidDiaphragm();
            floor._diaphragm_system.AddDiaphragm(new DiaphragmData_Rectangular(new Point(0, 0), new Point(46, 30)));
            foreach (var w in new[] { (0.0, 0.0, 46.0, 0.0), (0.0, 30.0, 46.0, 30.0), (0.0, 0.0, 0.0, 30.0), (46.0, 0.0, 46.0, 30.0) })
                floor._wall_system.AddWall(new WallData(9, w.Item1, w.Item2, w.Item3, w.Item4));
            // the beams under the overhang, drawn a little off the wall lines as in the project
            var beams = new List<(Point, Point)> { (new Point(46, 29.94), new Point(60, 29.94)), (new Point(60, 29.94), new Point(60, 0.06)), (new Point(60, 0.06), new Point(45.9, 0)) };

            var bare = StoryCascade.Unsupported(roof, floor, 3);
            Assert.AreEqual(3, bare.Count(n => n.Contains("hold-downs need")), "without the beams: walls 1, 2 (14 of 60 ft) and 4 (all) over nothing");
            var withBeams = StoryCascade.Unsupported(roof, floor, 3, beams);
            Assert.AreEqual(0, withBeams.Count(n => n.Contains("hold-downs need")), "the beams carry them");

            int Key(double x1, double y1, double x2, double y2) =>
                roof._wall_system._walls.Single(kv => kv.Value.Start == new Point(x1, y1) && kv.Value.End == new Point(x2, y2)).Key;
            int south = Key(0, 0, 60, 0), north = Key(0, 30, 60, 30), west = Key(0, 0, 0, 30), east = Key(60, 0, 60, 30);
            var off = StoryCascade.OffFloor(roof, floor);
            Assert.AreEqual(3, off.Count);
            Assert.AreEqual(14, off[south], 1e-9, "the E-W walls: 46 to 60 is past the floor");
            Assert.AreEqual(14, off[north], 1e-9);
            Assert.AreEqual(30, off[east], 1e-9, "the N-S wall at x = 60: all of it");
            Assert.IsFalse(off.ContainsKey(west), "the wall at x = 0 is on the floor's edge");
            StringAssert.Contains(withBeams.Single(n => n.Contains($"wall {east}:")), "all of it is beyond the edge of the level 2 floor");
            StringAssert.Contains(withBeams.Single(n => n.Contains($"wall {south}:")), "14 of its 60 ft");

            // the floor extended under them: nothing to say
            floor._diaphragm_system.AddDiaphragm(new DiaphragmData_Rectangular(new Point(46, 0), new Point(60, 30)));
            Assert.AreEqual(0, StoryCascade.OffFloor(roof, floor).Count, "two diaphragms side by side cover the walls");
            Assert.AreEqual(0, StoryCascade.Unsupported(roof, floor, 3, beams).Count);
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
            StringAssert.Contains(notes[0], "Level 2, wall 3: no wall or beam on level 1");

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
            var read = TestProject.RoundTrip(new ShearWallCalculatorBase[] { floor, roof });
            Assert.AreEqual(2, read.Count);
            Assert.AreEqual(4, read[0]._wall_system._walls.Count);
            Assert.AreEqual(5, read[1]._wall_system._walls.Count);
            Assert.AreEqual(1, read[1]._diaphragm_system._diaphragms.Count);
            Assert.AreEqual(1, TestProject.RoundTrip(new ShearWallCalculatorBase[] { Level(Box) }).Count, "one story: no upper levels");
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

        // ------------------------------------------------------------------ hold-down tension through the floors

        private static readonly Design.ShearWallDesigner Designer = new Design.ShearWallDesigner();

        /// <summary>A couple at the posts of a wall the same length: the moment is T × d, so the tension below grows by T.</summary>
        [TestMethod]
        public void MomentFromAbove_CoupleEndToEnd_IsTd()
        {
            double L = 10, half = 0.125, d = L - 2 * half;
            var forces = new[]
            {
                new Design.StackedForce { Along = half, Asd = 1000, Lrfd = 1600, LowEndUp = true },
                new Design.StackedForce { Along = L - half, Asd = -1000, Lrfd = -1600, LowEndUp = true },
                new Design.StackedForce { Along = L - half, Asd = 1000, Lrfd = 1600, LowEndUp = false },
                new Design.StackedForce { Along = half, Asd = -1000, Lrfd = -1600, LowEndUp = false },
            };
            var (asd, lrfd) = Design.ShearWallDesigner.MomentFromAbove(forces, L, half);
            Assert.AreEqual(1000 * d, asd, Tol);
            Assert.AreEqual(1600 * d, lrfd, Tol);
        }

        /// <summary>A shorter wall above in the middle of a long one: its couple T × d_upper spreads over the longer arm below.</summary>
        [TestMethod]
        public void MomentFromAbove_ShortWallInTheMiddle()
        {
            var forces = new[]
            {
                new Design.StackedForce { Along = 6, Asd = 2000, LowEndUp = true },
                new Design.StackedForce { Along = 14, Asd = -2000, LowEndUp = true },
            };
            var (asd, _) = Design.ShearWallDesigner.MomentFromAbove(forces, 20, 0.125);
            Assert.AreEqual(2000 * 8, asd, Tol, "a couple: its moment does not depend on where it sits");
        }

        /// <summary>Upper walls on the same line give an uplift and a bearing force per direction; one off the line gives none.</summary>
        [TestMethod]
        public void HoldownForcesOn_FindsTheWallsAbove()
        {
            var lower = new WallData(9, 0, 0, 20, 0);
            var upper = new Dictionary<int, WallData>
            {
                [1] = new WallData(9, 0, 0, 10, 0),      // on it, from the left end
                [2] = new WallData(9, 16, 0.1, 26, 0.1), // hangs off the right end: only the uplift at x = 16.125 lands
                [3] = new WallData(9, 0, 5, 10, 5),      // another line
            };
            var design = upper.ToDictionary(kv => kv.Key, kv => new Design.WallDesignResult { TensionAsd = 1000, TensionLrfd = 1500, PostAlongWall = 3 });
            var f = StoryCascade.HoldownForcesOn(lower, upper, design, 2);
            Assert.AreEqual(4, f.Count(x => x.From == "level 2, wall 1"));
            Assert.AreEqual(1, f.Count(x => x.From == "level 2, wall 2"), "the bearing end is off the wall; the uplift at the other end lands only when lifting the low end");
            var w2 = f.Single(x => x.From == "level 2, wall 2");
            Assert.IsTrue(w2.LowEndUp);
            Assert.AreEqual(16.125, w2.Along, Tol);
            Assert.AreEqual(1000, w2.Asd, Tol);
            Assert.IsFalse(f.Any(x => x.From == "level 2, wall 3"));
        }

        /// <summary>
        /// Where the hold-downs land: each wall that needs them has two, on its post centerlines; on a wall of the story below
        /// (same line, between its ends), on nothing (a beam or post is needed), or, for the first story, on the foundation.
        /// </summary>
        [TestMethod]
        public void Landings_OnAWallBelow_OnNothing_OrOnTheFoundation()
        {
            var upper = new Dictionary<int, WallData>
            {
                [1] = new WallData(9, 0, 0, 10, 0),   // over the lower wall along y = 0
                [2] = new WallData(9, 14, 0, 24, 0),  // its east end hangs past the lower wall (ends at x = 20)
                [3] = new WallData(9, 30, 5, 30, 15), // N-S, nothing under it
                [4] = new WallData(9, 0, 30, 10, 30), // no hold-downs needed
            };
            var lower = new Dictionary<int, WallData> { [11] = new WallData(9, 0, 0, 20, 0) };
            var design = new Dictionary<int, Design.WallDesignResult>
            {
                [1] = new Design.WallDesignResult { TensionAsd = 1200 },
                [2] = new Design.WallDesignResult { TensionAsd = 900 },
                [3] = new Design.WallDesignResult { TensionAsd = 500 },
                [4] = new Design.WallDesignResult { TensionAsd = 0 },
            };
            var l = StoryCascade.Landings(upper, design, lower, 2);
            Assert.AreEqual(6, l.Count, "two per wall that needs hold-downs; wall 4 needs none");
            Assert.IsTrue(l.Where(x => x.WallId == 1).All(x => x.LowerWallId == 11 && x.Supported));
            var w2 = l.Where(x => x.WallId == 2).OrderBy(x => x.At.X).ToList();
            Assert.AreEqual(11, w2[0].LowerWallId, "the west post is over the lower wall");
            Assert.IsNull(w2[1].LowerWallId, "the east post (x near 24) is past its end");
            Assert.IsFalse(w2[1].Supported);
            Assert.AreEqual(900, w2[1].TensionAsd);
            Assert.IsTrue(l.Where(x => x.WallId == 3).All(x => !x.Supported), "a N-S wall with no N-S wall under it");
            Assert.IsTrue(l.All(x => x.UpperStory == 2));

            var anchors = StoryCascade.Landings(lower, new Dictionary<int, Design.WallDesignResult> { [11] = new Design.WallDesignResult { TensionAsd = 2000 } }, null, 1);
            Assert.AreEqual(2, anchors.Count);
            Assert.IsTrue(anchors.All(a => a.OnFoundation && a.Supported && a.LowerWallId == null));
            Assert.IsTrue(anchors.Min(a => a.At.X) > 0 && anchors.Max(a => a.At.X) < 20, "on the post centerlines, inside the wall ends");
        }

        /// <summary>Walls stacked end to end: the hold-down below carries its own tension plus the one above (cumulative tension).</summary>
        [TestMethod]
        public void Check_StackedWall_TensionAccumulates()
        {
            var wall = new WallData(9, 0, 0, 10, 0);
            var upperWalls = new Dictionary<int, WallData> { [7] = new WallData(9, 0, 0, 10, 0) };
            var upperResult = Designer.Check(7, upperWalls[7], 2.0, null, new Design.DesignSettings());
            var forces = StoryCascade.HoldownForcesOn(wall, upperWalls, new Dictionary<int, Design.WallDesignResult> { [7] = upperResult }, 2);

            var alone = Designer.Check(1, wall, 3.0, null, new Design.DesignSettings());
            var stacked = Designer.Check(1, wall, 3.0, null, new Design.DesignSettings(), null, forces);
            Assert.AreEqual(0.6 * 3000 * 9 / stacked.HoldownArm, stacked.OwnTensionAsd, 1e-6, "its own share: 0.6 V h / d (d from the post of the heavier hold-down)");
            double half = stacked.PostAlongWall / 24.0, upHalf = upperResult.PostAlongWall / 24.0;
            double expected = stacked.OwnTensionAsd + upperResult.TensionAsd * (10 - half - upHalf) / stacked.HoldownArm
                              - upperResult.TensionAsd * System.Math.Max(0, upHalf - half) / stacked.HoldownArm;
            Assert.AreEqual(expected, stacked.TensionAsd, 1e-6);
            Assert.IsTrue(stacked.TensionAsd > alone.TensionAsd + 0.9 * upperResult.TensionAsd, "about the upper T added");
            Assert.IsTrue(stacked.Holdown == null || stacked.Holdown.Allowable >= stacked.TensionAsd, "the hold-down is chosen for the total");
        }

        /// <summary>The dead load of the lower wall resists the whole stack: 0.6 D L / 2 comes off the total, not just its own share.</summary>
        [TestMethod]
        public void Check_DeadLoadBelowOffsetsTheWallsAbove()
        {
            var wall = new WallData(9, 0, 0, 10, 0);
            var forces = new List<Design.StackedForce>
            {
                new Design.StackedForce { Along = 0.125, Asd = 1000, Lrfd = 1000, LowEndUp = true },
                new Design.StackedForce { Along = 9.875, Asd = -1000, Lrfd = -1000, LowEndUp = true },
            };
            var r = Designer.Check(1, wall, 0.0, null, new Design.DesignSettings(), new Design.WallDesignInputs { DeadLoad = 100 }, forces);
            Assert.AreEqual(0.0, r.OwnTensionAsd, Tol, "no shear of its own");
            Assert.AreEqual(System.Math.Max(0, (1000 * (10 - r.PostAlongWall / 24 - 0.125) - 1000 * System.Math.Max(0, 10 - r.PostAlongWall / 24 - 9.875)) / r.HoldownArm - 0.6 * 100 * 10 / 2), r.TensionAsd, 1e-6);
        }

        // ------------------------------------------------------------------ adding and deleting floors

        [TestMethod]
        public void FloorEdits_PlateHeights()
        {
            CollectionAssert.AreEqual(new[] { 10.0, 9.0, 8.0 }, FloorEdits.PlatesAfterAdd(new[] { 10.0, 9.0 }, 8));
            CollectionAssert.AreEqual(new[] { 10.0, 8.0 }, FloorEdits.PlatesAfterDelete(new[] { 10.0, 9.0, 8.0 }, 1), "the stories above keep their heights");
            CollectionAssert.AreEqual(new[] { 10.0 }, FloorEdits.PlatesAfterDelete(new[] { 10.0 }, 4), "out of range: unchanged");
        }

        /// <summary>Deleting the middle floor of three: level 3 becomes level 2 and stands on level 1; the cascade still runs.</summary>
        [TestMethod]
        public void FloorEdits_DeleteMiddleFloor_UpperMovesDown()
        {
            var l1 = Level(Box); var l2 = Level(Box); var l3 = Level((0, 0, 20, 0), (0, 30, 40, 30), (0, 0, 0, 30), (40, 0, 40, 30));
            var levels = new List<ShearWallCalculatorBase> { l1, l2, l3 };
            var lines = FloorEdits.DeleteConsequences(levels, 1, new[] { 10.0, 9.0, 8.0 });
            Assert.IsTrue(lines.Any(l => l.Contains("Level 3 becomes level 2")));
            Assert.IsTrue(lines.Any(l => l.Contains("eave drops from 27 to 18 ft")));
            Assert.IsTrue(lines.Any(l => l.Contains("stands on a wall of level 1")), string.Join("; ", lines));
            FloorEdits.Delete(levels, 1);
            Assert.AreEqual(2, levels.Count);
            Assert.AreSame(l3, levels[1]);
            l3.SetLoadCases(new[] { new LateralLoadCase("W", 10, 0) });
            l1.SetLoadCases(new[] { new LateralLoadCase("W", 8, 0) });
            StoryCascade.Analyze(levels);
            Assert.AreEqual(18, l1.CaseResults.Single().Vx, 1e-6, "level 1 carries the moved-down level");
        }

        /// <summary>Deleting level 1 under an upper floor: it lands on the foundation; deleting the top: the floor below becomes the roof.</summary>
        [TestMethod]
        public void FloorEdits_DeleteBottomOrTop_SaysWhatHappens()
        {
            var levels = new List<ShearWallCalculatorBase> { Level(Box), Level((5, 0, 15, 0)) };
            Assert.IsTrue(FloorEdits.DeleteConsequences(levels, 0, null).Any(l => l.Contains("foundation")));
            Assert.IsTrue(FloorEdits.DeleteConsequences(levels, 1, null).Any(l => l.Contains("becomes the roof")));
            var three = new List<ShearWallCalculatorBase> { Level(Box), Level((5, 0, 15, 0)), Level((5, 10, 15, 10)) };
            Assert.IsTrue(FloorEdits.DeleteConsequences(three, 1, null).Any(l => l.Contains("1 wall of level 3 will have no wall under it")));
        }

        [TestMethod]
        public void FloorEdits_TheOnlyFloorIsClearedNotDeleted()
        {
            var only = new List<ShearWallCalculatorBase> { Level(Box) };
            Assert.Throws<System.InvalidOperationException>(() => FloorEdits.Delete(only, 0));
            Assert.AreEqual(5, FloorEdits.Clear(only[0]));
            Assert.AreEqual(0, only[0]._wall_system._walls.Count);
        }
    }
}
