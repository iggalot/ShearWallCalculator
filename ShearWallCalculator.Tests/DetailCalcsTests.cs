using System.Collections.Generic;
using System.Linq;
using System.Windows;
using calculator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShearWallCalculator.Design;

namespace ShearWallCalculator.Tests
{
    /// <summary>
    /// The detail sheets restate the design: each line's numbers are the ones <see cref="ShearWallDesigner.Check"/> and
    /// <see cref="LoadPathTrace"/> used, checked here against the formulas (T = 0.6 V h / d − 0.6 D / 2, d = L − post;
    /// simple-span reactions).
    /// </summary>
    [TestClass]
    public class DetailCalcsTests
    {
        private static readonly ShearWallDesigner Designer = new ShearWallDesigner();

        private static CalcLine Line(List<CalcLine> lines, string item) => lines.Single(l => l.Item == item);

        [TestMethod]
        public void WallSheet_RestatesTheOverturning()
        {
            var wall = new WallData(9, 0, 0, 10, 0);
            var r = Designer.Check(1, wall, 2.0, "Case 1", new DesignSettings(), new WallDesignInputs { DeadLoad = 100 });
            var lines = DetailCalcs.Wall(wall, r, null);

            double M = 2.0 * 1000 * 9, d = 10 - r.PostAlongWall / 12, D = 100 * 10;
            Assert.AreEqual(d, r.HoldownArm, 1e-9);
            Assert.AreEqual(0.6 * M / d - 0.6 * D / 2, r.OwnTensionAsd, 1e-6, "the source formula");
            Assert.AreEqual($"{M:0} lb-ft", Line(lines, "Overturning").Result);
            Assert.AreEqual($"{d:0.##} ft", Line(lines, "Lever arm").Result);
            Assert.AreEqual($"{D:0} lb", Line(lines, "Dead load").Result);
            Assert.AreEqual($"{r.OwnTensionAsd:0} lb (ASD)", Line(lines, "Own uplift").Result);
            StringAssert.Contains(Line(lines, "Own uplift").Calculation, $"0.6 × {M:0} / {d:0.##} − 0.6 × {D:0} / 2");
            Assert.AreEqual($"{0.6 * 2000 / 10:0} plf", Line(lines, "Unit shear").Result);
            Assert.AreEqual($"T = {r.TensionAsd:0} lb (ASD)", Line(lines, "Hold-down tension").Result);
            if (r.Holdown != null) StringAssert.Contains(Line(lines, "Hold-down").Calculation, r.Holdown.Model);
        }

        [TestMethod]
        public void WallSheet_ListsTheForcesFromAbove_AndWhereEachHoldownAnchors()
        {
            var wall = new WallData(9, 0, 0, 20, 0);
            var forces = new List<StackedForce> { new StackedForce { Along = 0.2, Asd = 1500, Lrfd = 2500, LowEndUp = true, From = "level 3, wall 7" } };
            var r = Designer.Check(1, wall, 1.0, null, new DesignSettings(), null, forces);
            var (p1, p2) = WallSchedule.HoldownPoints(wall, r.Holdown);
            var own = new List<HoldownLanding>
            {
                new HoldownLanding { At = p1, Support = SupportKind.Foundation, OnFoundation = true, Family = HoldownFamilies.HDU_ToFoundation, Model = "HDU2", Kind = PathPointKind.WallHoldown },
                new HoldownLanding { At = p2, Support = SupportKind.None, Kind = PathPointKind.WallHoldown },
            };
            var lines = DetailCalcs.Wall(wall, r, own);
            var above = Line(lines, "From above");
            StringAssert.Contains(above.Calculation, "level 3, wall 7");
            Assert.AreEqual("1500 lb", above.Result);
            Assert.AreEqual($"{r.TensionFromAboveAsd:0} lb", Line(lines, "Added by them").Result);
            Assert.AreEqual("HDU2", Line(lines, "West end").Result);
            StringAssert.Contains(Line(lines, "West end").Calculation, "the foundation");
            Assert.IsTrue(Line(lines, "East end").Fails, "nothing under the east hold-down");
        }

        [TestMethod]
        public void BeamSheet_ReactionsAddUpPerWindDirection()
        {
            var bm = new FramingBeam { Id = "BM1", Start = new Point(0, 0), End = new Point(20, 0) };
            var br = new BeamReactions { Beam = bm, StartAsd = 900, EndAsd = 700 };
            br.Loads.Add(new BeamLoad { From = "wall 1", A = 5, Asd = 1000, LowEndUp = true, StartAsd = 750, EndAsd = 250 });
            br.Loads.Add(new BeamLoad { From = "wall 2", A = 16, Asd = 500, LowEndUp = true, StartAsd = 100, EndAsd = 400 });
            var lines = DetailCalcs.Beam(br, new[]
            {
                new HoldownLanding { From = "beam BM1 start", Support = SupportKind.Wall, LowerWallId = 3, SupportId = "wall 3", Family = HoldownFamilies.Strap_ToWallBelow, Model = "CS16", TensionAsd = 900 },
                new HoldownLanding { From = "beam BM1 end", Support = SupportKind.None, TensionAsd = 700 },
            });
            Assert.AreEqual("L = 20 ft", Line(lines, "Span").Result);
            Assert.AreEqual("850 / 650 lb", Line(lines, "  Sum").Result, "Σ of the shares in that direction");
            Assert.AreEqual("start 900 lb, end 700 lb", Line(lines, "Reactions").Result);
            Assert.AreEqual("CS16", Line(lines, "Start support").Result);
            Assert.IsTrue(Line(lines, "End support").Fails);
        }

        [TestMethod]
        public void ColumnSheet_WhatLandsAndItsBase()
        {
            var c = new FramingColumn { Id = "C1", At = new Point(5, 5), BaseStory = 0, TopStory = 1 };
            var on = new[] { new HoldownLanding { From = "wall 4", UpperStory = 3, WallId = 4, TensionAsd = 2400, Family = HoldownFamilies.HDU_ToFoundation, Model = "HDU5" } };
            var bottom = new HoldownLanding { From = "column C1 base", Kind = PathPointKind.ColumnBase, Support = SupportKind.Foundation, OnFoundation = true, TensionAsd = 2400, Family = HoldownFamilies.HDU_ToFoundation, Model = "HDU5" };
            var lines = DetailCalcs.Column(c, "from the foundation up to Level 3", on, bottom);
            Assert.AreEqual("2400 lb", Line(lines, "Lands on it").Result);
            Assert.AreEqual("2400 lb (ASD)", Line(lines, "Down the column").Result);
            Assert.AreEqual("HDU5", Line(lines, "Base").Result);
            StringAssert.Contains(Line(lines, "Base").Calculation, "the foundation");
        }
    }
}
