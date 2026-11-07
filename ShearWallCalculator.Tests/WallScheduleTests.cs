using System.Collections.Generic;
using System.Linq;
using calculator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShearWallCalculator.Design;

namespace ShearWallCalculator.Tests
{
    /// <summary>The design on the drawing: schedule marks per construction, and hold-downs on the post centerlines.</summary>
    [TestClass]
    public class WallScheduleTests
    {
        private const double Tol = 1e-9;

        private static WallDesignResult R(int id, string panel, double capacity) =>
            new WallDesignResult { WallId = id, Panel = panel, Capacity = capacity };

        [TestMethod]
        public void Schedule_OneMarkPerWall_ItsLevelAndNumber()
        {
            var design = new Dictionary<int, WallDesignResult>
            {
                [9] = R(9, "Structural I 15/32\", 8d @ 6\"", 392.5),
                [2] = R(2, "Structural I 15/32\", 8d @ 6\"", 392.5), // the same construction: still its own mark
                [4] = R(4, "Gyp. wallboard 1/2\", both faces", 200),
            };
            var s = WallSchedule.Build(design, "2");
            // wall 9 on the Level 2 plan is SW2-9; in wall-number order
            CollectionAssert.AreEqual(new[] { "SW2-2", "SW2-4", "SW2-9" }, s.Select(e => e.Mark).ToList());
            Assert.IsTrue(s.All(e => e.WallIds.Count == 1));
            Assert.AreEqual("Gyp. wallboard 1/2\", both faces", s[1].Sheathing);
            Assert.AreEqual(200, s[1].Capacity, Tol);
            var marks = WallSchedule.MarksByWall(s);
            Assert.AreEqual("SW2-9", marks[9]);
            Assert.AreEqual("SW3-9", WallSchedule.Mark("3", 9));
            Assert.AreEqual("SW9", WallSchedule.Build(design).Last().Mark, "no level: SW and the wall number");
        }

        [TestMethod]
        public void Schedule_LevelTag_FromTheLevelName()
        {
            Assert.AreEqual("2", WallSchedule.LevelTag("Level 2", 0));
            Assert.AreEqual("12", WallSchedule.LevelTag("Floor 12", 10));
            Assert.AreEqual("R", WallSchedule.LevelTag("Roof", 1), "no number: the initial, not the story number");
            Assert.AreEqual("2", WallSchedule.LevelTag("", 1), "no name: the story number");
        }

        [TestMethod]
        public void Schedule_EmptyOrMissing()
        {
            Assert.AreEqual(0, WallSchedule.Build(null).Count);
            Assert.AreEqual(0, WallSchedule.Build(new Dictionary<int, WallDesignResult>()).Count);
        }

        [TestMethod]
        public void HoldownPoints_AtThePostsInsideFace_AFullPostIn_BothEnds()
        {
            // E-W wall from (10, 5) to (30, 5); HDU with a 3.5 x 3.5 minimum post: on its inside face, 3.5 in from the end
            var wall = new WallData(9, 10, 5, 30, 5);
            var h = new HoldownChoice { Model = "HDU2", WoodMinHeight = 3.5, WoodMinWidth = 3.5 };
            var (a, b) = WallSchedule.HoldownPoints(wall, h);
            Assert.AreEqual(10 + 3.5 / 12, a.X, Tol);
            Assert.AreEqual(5, a.Y, Tol);
            Assert.AreEqual(30 - 3.5 / 12, b.X, Tol);

            // N-S wall drawn top to bottom (WallData puts Start at the bottom), no member size given: two 2x studs, 3 in
            var ns = new WallData(9, 0, 20, 0, 8);
            var (c, d) = WallSchedule.HoldownPoints(ns, null);
            Assert.AreEqual(8 + 3.0 / 12, c.Y, Tol);
            Assert.AreEqual(20 - 3.0 / 12, d.Y, Tol);
            Assert.AreEqual(0, c.X, Tol);
        }

        [TestMethod]
        public void PostAlongWall_FirstMemberSize_OrTwoStuds()
        {
            Assert.AreEqual(3.0, WallSchedule.PostAlongWall(new HoldownChoice { WoodMinHeight = 3, WoodMinWidth = 3.5 }), Tol);
            Assert.AreEqual(5.5, WallSchedule.PostAlongWall(new HoldownChoice { WoodMinHeight = 5.5, WoodMinWidth = 3.5 }), Tol);
            Assert.AreEqual(WallSchedule.DefaultPostAlongWall, WallSchedule.PostAlongWall(new HoldownChoice()), Tol);
        }
    }
}
