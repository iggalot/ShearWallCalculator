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
        public void Schedule_OneMarkPerConstruction_WeakestFirst()
        {
            var design = new Dictionary<int, WallDesignResult>
            {
                [0] = R(0, "Structural I 15/32\", 8d @ 4\"", 602.5),
                [1] = R(1, "Gyp. wallboard 1/2\", both faces", 200),
                [2] = R(2, "Structural I 15/32\", 8d @ 4\"", 560),   // tall wall: aspect factor lowers it
                [3] = R(3, "Structural I 15/32\", 8d @ 6\"", 392.5),
            };
            var s = WallSchedule.Build(design);
            CollectionAssert.AreEqual(new[] { "SW1", "SW2", "SW3" }, s.Select(e => e.Mark).ToList());
            Assert.AreEqual("Gyp. wallboard 1/2\", both faces", s[0].Sheathing);
            Assert.AreEqual("Structural I 15/32\", 8d @ 6\"", s[1].Sheathing);
            CollectionAssert.AreEqual(new[] { 0, 2 }, s[2].WallIds);
            Assert.AreEqual(560, s[2].Capacity, Tol, "the smallest capacity of the group");

            var marks = WallSchedule.MarksByWall(s);
            Assert.AreEqual("SW3", marks[2]);
            Assert.AreEqual("SW1", marks[1]);
        }

        [TestMethod]
        public void Schedule_EmptyOrMissing()
        {
            Assert.AreEqual(0, WallSchedule.Build(null).Count);
            Assert.AreEqual(0, WallSchedule.Build(new Dictionary<int, WallDesignResult>()).Count);
        }

        [TestMethod]
        public void HoldownPoints_InsetHalfThePost_BothEnds()
        {
            // E-W wall from (10, 5) to (30, 5); HDU with a 3.5 x 3.5 minimum post: inset 1.75 in = 0.145833 ft
            var wall = new WallData(9, 10, 5, 30, 5);
            var h = new HoldownChoice { Model = "HDU2", WoodMinHeight = 3.5, WoodMinWidth = 3.5 };
            var (a, b) = WallSchedule.HoldownPoints(wall, h);
            Assert.AreEqual(10 + 1.75 / 12, a.X, Tol);
            Assert.AreEqual(5, a.Y, Tol);
            Assert.AreEqual(30 - 1.75 / 12, b.X, Tol);

            // N-S wall drawn top to bottom (WallData puts Start at the bottom), no member size given: two 2x studs, 3 in → 1.5 in inset
            var ns = new WallData(9, 0, 20, 0, 8);
            var (c, d) = WallSchedule.HoldownPoints(ns, null);
            Assert.AreEqual(8 + 1.5 / 12, c.Y, Tol);
            Assert.AreEqual(20 - 1.5 / 12, d.Y, Tol);
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
