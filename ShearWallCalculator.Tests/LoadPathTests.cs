using System.Collections.Generic;
using System.Linq;
using System.Windows;
using calculator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShearWallCalculator.Design;

namespace ShearWallCalculator.Tests
{
    /// <summary>
    /// The hold-down load path: what each family can anchor to, beams carrying hold-downs to their ends (simple-span
    /// reactions), columns carrying them to their base, and the foundation.
    /// </summary>
    [TestClass]
    public class LoadPathTests
    {
        /// <summary>A sizer that names what it was asked for: "family:lb".</summary>
        private static HoldownChoice Size(HoldownFamilies f, WoodTypes s, double t) => new HoldownChoice { Model = $"{f}:{t:0}", Allowable = t };

        [TestMethod]
        public void Rules_WhatEachFamilyCanAnchorTo()
        {
            // STHD: cast into concrete, the foundation only
            Assert.IsTrue(HoldownRules.CanAnchorTo(HoldownFamilies.STHD_ToFoundation, SupportKind.Foundation));
            Assert.IsFalse(HoldownRules.CanAnchorTo(HoldownFamilies.STHD_ToFoundation, SupportKind.Wall));
            Assert.IsFalse(HoldownRules.CanAnchorTo(HoldownFamilies.STHD_ToFoundation, SupportKind.Beam));
            // straps: wood below (a wall, a column, wrapped around a beam), not concrete
            Assert.IsTrue(HoldownRules.CanAnchorTo(HoldownFamilies.Strap_ToWallBelow, SupportKind.Wall));
            Assert.IsTrue(HoldownRules.CanAnchorTo(HoldownFamilies.Strap_ToWallBelow, SupportKind.Column));
            Assert.IsTrue(HoldownRules.CanAnchorTo(HoldownFamilies.Strap_ToWallBelow, SupportKind.Beam));
            Assert.IsFalse(HoldownRules.CanAnchorTo(HoldownFamilies.Strap_ToWallBelow, SupportKind.Foundation));
            // HDUE: the foundation, or a rod into a post directly below; never into a beam
            Assert.IsTrue(HoldownRules.CanAnchorTo(HoldownFamilies.HDU_ToFoundation, SupportKind.Foundation));
            Assert.IsTrue(HoldownRules.CanAnchorTo(HoldownFamilies.HDU_ToFoundation, SupportKind.Wall));
            Assert.IsTrue(HoldownRules.CanAnchorTo(HoldownFamilies.HDU_ToFoundation, SupportKind.Column));
            Assert.IsFalse(HoldownRules.CanAnchorTo(HoldownFamilies.HDU_ToFoundation, SupportKind.Beam));
            foreach (HoldownFamilies f in System.Enum.GetValues(typeof(HoldownFamilies)))
                Assert.IsFalse(HoldownRules.CanAnchorTo(f, SupportKind.None));
            Assert.AreEqual(HoldownFamilies.Strap_ToWallBelow, HoldownRules.For(SupportKind.Beam, HoldownFamilies.HDU_ToFoundation));
            Assert.AreEqual(HoldownFamilies.HDU_ToFoundation, HoldownRules.For(SupportKind.Foundation, HoldownFamilies.Strap_ToWallBelow));
        }

        [TestMethod]
        public void ABeam_CarriesTheHoldowns_ToItsEnds_OnTheWallsBelow()
        {
            var lower = new Dictionary<int, WallData> { [11] = new WallData(9, 0, 0, 0, 20), [12] = new WallData(9, 20, 0, 20, 20) };
            var upper = new Dictionary<int, WallData> { [1] = new WallData(9, 5, 10, 15, 10) }; // E-W, no E-W wall under it
            var design = new Dictionary<int, WallDesignResult> { [1] = new WallDesignResult { TensionAsd = 1000, TensionLrfd = 1667 } };
            var beam = new FramingBeam { Id = "BM1", Start = new Point(0, 10), End = new Point(20, 10), OnStory = 0 };
            var trace = new LoadPathTrace(new[] { beam }, null, Size);
            trace.Land(1, upper, design, lower);

            var on = trace.ByLevel[0];
            var holdowns = on.Where(x => x.Kind == PathPointKind.WallHoldown).ToList();
            Assert.AreEqual(2, holdowns.Count);
            Assert.IsTrue(holdowns.All(x => x.Support == SupportKind.Beam && x.SupportId == "beam BM1"));
            Assert.IsTrue(holdowns.All(x => x.Family == HoldownFamilies.Strap_ToWallBelow && x.Note != null), "an HDUE cannot anchor into a beam: a strap around it");

            var (p1, p2) = WallSchedule.HoldownPoints(upper[1], null);
            double a1 = p1.X, a2 = p2.X, L = 20;
            var r = trace.Beams.Single();
            // each wind direction lifts one end of the wall: the worse reaction at each beam end
            Assert.AreEqual(System.Math.Max(1000 * (L - a1) / L, 1000 * (L - a2) / L), r.StartAsd, 1e-6);
            Assert.AreEqual(System.Math.Max(1000 * a1 / L, 1000 * a2 / L), r.EndAsd, 1e-6);

            var ends = on.Where(x => x.Kind == PathPointKind.BeamEnd).OrderBy(x => x.At.X).ToList();
            Assert.AreEqual(2, ends.Count);
            Assert.AreEqual(11, ends[0].LowerWallId, "the start bears on the N-S wall at x = 0");
            Assert.AreEqual(12, ends[1].LowerWallId);
            Assert.IsTrue(ends.All(x => x.Family == HoldownFamilies.Strap_ToWallBelow && x.Choice != null), "a new strap sized for each reaction");
            Assert.AreEqual($"Strap_ToWallBelow:{r.StartAsd:0}", ends[0].Model);

            var extra = trace.ExtraOn(0);
            Assert.AreEqual(2, extra[11].Count, "the reaction is a force from above on the wall, for both wind directions");
            Assert.AreEqual(10, extra[11][0].Along, 1e-9, "10 ft from the wall's low end");
            Assert.AreEqual(r.StartAsd, extra[11][0].Asd, 1e-9);
        }

        [TestMethod]
        public void AColumn_CarriesAHoldown_ToTheFoundation_AndNothingIsAnError()
        {
            var upper = new Dictionary<int, WallData> { [1] = new WallData(9, 5, 30, 15, 30) };
            var (p1, _) = WallSchedule.HoldownPoints(upper[1], null);
            var design = new Dictionary<int, WallDesignResult> { [1] = new WallDesignResult { TensionAsd = 2400, TensionLrfd = 4000 } };
            var col = new FramingColumn { Id = "C1", At = p1, BaseStory = 0, TopStory = 0 };
            var trace = new LoadPathTrace(null, new[] { col }, Size);
            trace.Land(1, upper, design, new Dictionary<int, WallData>());
            var on = trace.ByLevel[0].OrderBy(x => x.At.X).ToList();
            Assert.AreEqual(SupportKind.Column, on[0].Support);
            Assert.AreEqual(HoldownFamilies.HDU_ToFoundation, on[0].Family, "an HDUE with a rod into the column is fine");
            Assert.IsNull(on[0].Note);
            Assert.IsFalse(on[1].Supported, "the east hold-down has nothing under it");

            trace.Land(0, new Dictionary<int, WallData>(), new Dictionary<int, WallDesignResult>(), null);
            var b = trace.ByLevel[-1].Single();
            Assert.AreEqual(PathPointKind.ColumnBase, b.Kind);
            Assert.AreEqual(SupportKind.Foundation, b.Support);
            Assert.IsTrue(b.OnFoundation);
            Assert.AreEqual(2400, b.TensionAsd, 1e-9);
            Assert.AreEqual("HDU_ToFoundation:2400", b.Model, "the column base anchor sized for what the column carries");
        }

        [TestMethod]
        public void AnSthd_CannotConnectFloorToFloor()
        {
            var lower = new Dictionary<int, WallData> { [11] = new WallData(9, 0, 0, 20, 0) };
            var wall = new WallData(9, 0, 0, 10, 0) { Design = new WallDesignInputs { Holdown = HoldownFamilies.STHD_ToFoundation } };
            var upper = new Dictionary<int, WallData> { [1] = wall };
            var design = new Dictionary<int, WallDesignResult> { [1] = new WallDesignResult { TensionAsd = 1500 } };
            var trace = new LoadPathTrace(null, null, Size);
            trace.Land(1, upper, design, lower);
            var on = trace.ByLevel[0];
            Assert.IsTrue(on.All(x => x.Support == SupportKind.Wall && x.Family == HoldownFamilies.Strap_ToWallBelow));
            StringAssert.Contains(on[0].Note, "concrete");
            Assert.AreEqual(0, trace.ExtraOn(0).Count, "a wall on a wall on the same line is already in the lower wall's design");

            // the same wall on the first story: an STHD into the foundation is right
            var t2 = new LoadPathTrace(null, null, Size);
            t2.Land(0, upper, design, null);
            Assert.IsTrue(t2.ByLevel[-1].All(x => x.Support == SupportKind.Foundation && x.Family == HoldownFamilies.STHD_ToFoundation && x.Note == null));
        }
    }
}
