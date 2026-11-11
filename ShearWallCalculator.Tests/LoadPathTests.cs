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

        /// <summary>
        /// A beam (y = 10, x 0 to 12) from the W wall into a girder (x = 12, y 0 to 20) on the S and N walls, a wall on the
        /// beam (x 2 to 10). The beam's end lands on the girder 10 ft along its 20 ft: a strap onto the girder, and from the
        /// girder's own free body R (20 − 10) / 20 to its start and R × 10 / 20 to its end, on the walls (off-centre: AGirder_TakesEachBeamAtItsPlace).
        /// </summary>
        [TestMethod]
        public void ABeamOnAGirder_ItsEndGoesOnDownTheGirder()
        {
            var lower = new Dictionary<int, WallData>
            {
                [11] = new WallData(9, 0, 0, 0, 20), [13] = new WallData(9, 0, 0, 20, 0), [14] = new WallData(9, 0, 20, 20, 20),
            };
            var upper = new Dictionary<int, WallData> { [1] = new WallData(9, 2, 10, 10, 10) };
            var design = new Dictionary<int, WallDesignResult> { [1] = new WallDesignResult { TensionAsd = 1000, TensionLrfd = 1667 } };
            var beam = new FramingBeam { Id = "BM1", Start = new Point(0, 10), End = new Point(12, 10), OnStory = 0 };
            var girder = new FramingBeam { Id = "BM2", Start = new Point(12, 0), End = new Point(12, 20), OnStory = 0 };
            var trace = new LoadPathTrace(new[] { beam, girder }, null, Size);
            trace.Land(1, upper, design, lower);

            var (p1, p2) = WallSchedule.HoldownPoints(upper[1], null);
            double rEnd = System.Math.Max(1000 * p1.X / 12, 1000 * p2.X / 12);
            var rb = trace.Beams.Single(x => x.Beam == beam);
            Assert.AreEqual(rEnd, rb.EndAsd, 1e-6);
            var on = trace.ByLevel[0];
            var end = on.Single(x => x.From == "beam BM1 end");
            Assert.AreEqual(SupportKind.Beam, end.Support, "onto the girder, not nothing");
            Assert.AreEqual("beam BM2", end.SupportId);
            Assert.AreEqual(HoldownFamilies.Strap_ToWallBelow, end.Family);
            var rg = trace.Beams.Single(x => x.Beam == girder);
            Assert.AreEqual(rEnd * (20 - 10) / 20, rg.StartAsd, 1e-6);
            Assert.AreEqual(rEnd * 10 / 20, rg.EndAsd, 1e-6);
            Assert.AreEqual(13, on.Single(x => x.From == "beam BM2 start").LowerWallId);
            Assert.AreEqual(14, on.Single(x => x.From == "beam BM2 end").LowerWallId);
            Assert.IsFalse(on.Any(x => x.Support == SupportKind.None), "everything lands on something");
        }

        /// <summary>
        /// The girder's reactions from its own free body, each load at its place: two beams landing on a 20 ft girder at 6 ft
        /// and at 15 ft from its start, each with a wall on it. A beam's end reaction R lands at a: (20 − a) / 20 of it to the
        /// girder's start, a / 20 to its end; the two summed (each beam end acts in both wind directions, so both go in
        /// together). Not half to each end.
        /// </summary>
        [TestMethod]
        public void AGirder_TakesEachBeamAtItsPlace()
        {
            var lower = new Dictionary<int, WallData>
            {
                [11] = new WallData(9, 0, 0, 0, 20), [13] = new WallData(9, 0, 0, 20, 0), [14] = new WallData(9, 0, 20, 20, 20),
            };
            var upper = new Dictionary<int, WallData> { [1] = new WallData(9, 2, 6, 10, 6), [2] = new WallData(9, 3, 15, 9, 15) };
            var design = new Dictionary<int, WallDesignResult>
            {
                [1] = new WallDesignResult { TensionAsd = 1000, TensionLrfd = 1667 },
                [2] = new WallDesignResult { TensionAsd = 700, TensionLrfd = 1167 },
            };
            var b1 = new FramingBeam { Id = "BM1", Start = new Point(0, 6), End = new Point(12, 6), OnStory = 0 };
            var b2 = new FramingBeam { Id = "BM2", Start = new Point(0, 15), End = new Point(12, 15), OnStory = 0 };
            var girder = new FramingBeam { Id = "G", Start = new Point(12, 0), End = new Point(12, 20), OnStory = 0 };
            var trace = new LoadPathTrace(new[] { b1, b2, girder }, null, Size);
            trace.Land(1, upper, design, lower);

            double R1 = trace.Beams.Single(x => x.Beam == b1).EndAsd, R2 = trace.Beams.Single(x => x.Beam == b2).EndAsd;
            var (q1, q2) = WallSchedule.HoldownPoints(upper[1], null);
            Assert.AreEqual(System.Math.Max(1000 * q1.X / 12, 1000 * q2.X / 12), R1, 1e-6, "BM1's end, from its own free body");
            var rg = trace.Beams.Single(x => x.Beam == girder);
            Assert.AreEqual(R1 * 14 / 20 + R2 * 5 / 20, rg.StartAsd, 1e-6, "the girder's start: each at its place");
            Assert.AreEqual(R1 * 6 / 20 + R2 * 15 / 20, rg.EndAsd, 1e-6, "the girder's end");
            Assert.AreNotEqual((R1 + R2) / 2, rg.StartAsd, 1.0, "not half to each end");
            Assert.AreEqual(6, rg.Loads.Single(l => l.From == "beam BM1 end").A, 1e-9);
            Assert.AreEqual(15, rg.Loads.Single(l => l.From == "beam BM2 end").A, 1e-9);
        }

        /// <summary>
        /// Reciprocal framing: four beams round a square, pinwheel-fashion, each from a wall to the middle of the next, the last
        /// onto the first (A on B on C on D on A); a wall on A (x = 10, y 2 to 8). A's far end x passes half to each beam in
        /// turn and an eighth comes back to A's middle: x = T a / 20 + x / 16, so x = (16 / 15) T a / 20 (a: the hold-down
        /// further along, the worse direction). Solved together, and everything lands on a wall.
        /// </summary>
        [TestMethod]
        public void BeamsRoundALoop_SolvedTogether()
        {
            var lower = new Dictionary<int, WallData>
            {
                [11] = new WallData(9, 0, 0, 0, 30), [12] = new WallData(9, 30, 0, 30, 30),
                [13] = new WallData(9, 0, 0, 30, 0), [14] = new WallData(9, 0, 30, 30, 30),
            };
            var upper = new Dictionary<int, WallData> { [1] = new WallData(9, 10, 2, 10, 8) };
            var design = new Dictionary<int, WallDesignResult> { [1] = new WallDesignResult { TensionAsd = 1000, TensionLrfd = 1667 } };
            var a = new FramingBeam { Id = "A", Start = new Point(10, 0), End = new Point(10, 20), OnStory = 0 };
            var b = new FramingBeam { Id = "B", Start = new Point(0, 20), End = new Point(20, 20), OnStory = 0 };
            var c = new FramingBeam { Id = "C", Start = new Point(20, 30), End = new Point(20, 10), OnStory = 0 };
            var d = new FramingBeam { Id = "D", Start = new Point(30, 10), End = new Point(10, 10), OnStory = 0 };
            var trace = new LoadPathTrace(new[] { a, b, c, d }, null, Size);
            trace.Land(1, upper, design, lower);

            var (p1, p2) = WallSchedule.HoldownPoints(upper[1], null);
            double far = System.Math.Max(p1.Y, p2.Y), near = System.Math.Min(p1.Y, p2.Y);
            double x = 16.0 / 15 * 1000 * far / 20;
            Assert.AreEqual(x, trace.Beams.Single(r => r.Beam == a).EndAsd, 1e-6);
            Assert.AreEqual(x / 2, trace.Beams.Single(r => r.Beam == b).EndAsd, 1e-6);
            Assert.AreEqual(x / 4, trace.Beams.Single(r => r.Beam == c).EndAsd, 1e-6);
            Assert.AreEqual(x / 8, trace.Beams.Single(r => r.Beam == d).EndAsd, 1e-6);
            Assert.AreEqual(1000 * (20 - near) / 20 + x / 16, trace.Beams.Single(r => r.Beam == a).StartAsd, 1e-6);
            var on = trace.ByLevel[0];
            Assert.AreEqual("beam B", on.Single(l => l.From == "beam A end").SupportId);
            Assert.AreEqual("beam A", on.Single(l => l.From == "beam D end").SupportId);
            Assert.IsFalse(on.Any(l => l.Support == SupportKind.None), "everything lands on something");
            Assert.AreEqual(4, on.Count(l => l.Kind == PathPointKind.BeamEnd && l.LowerWallId != null), "each beam's start on a wall");
        }

        /// <summary>Two beams meeting end to end over nothing, a wall on one: a mechanism, their ends on nothing (an error).</summary>
        [TestMethod]
        public void TwoBeamsOnEachOthersEnds_OverNothing_AnError()
        {
            var lower = new Dictionary<int, WallData> { [11] = new WallData(9, 0, 0, 0, 20), [12] = new WallData(9, 30, 0, 30, 20) };
            var upper = new Dictionary<int, WallData> { [1] = new WallData(9, 2, 10, 8, 10) };
            var design = new Dictionary<int, WallDesignResult> { [1] = new WallDesignResult { TensionAsd = 1000, TensionLrfd = 1667 } };
            var a = new FramingBeam { Id = "A", Start = new Point(0, 10), End = new Point(15, 10), OnStory = 0 };
            var c = new FramingBeam { Id = "C", Start = new Point(15, 10), End = new Point(30, 10), OnStory = 0 };
            var trace = new LoadPathTrace(new[] { a, c }, null, Size);
            trace.Land(1, upper, design, lower);
            var on = trace.ByLevel[0];
            Assert.AreEqual(SupportKind.None, on.Single(l => l.From == "beam A end").Support);
            Assert.AreEqual(11, on.Single(l => l.From == "beam A start").LowerWallId);
        }

        /// <summary>
        /// A cantilever (owner 2026-11-10): a 14 ft beam on a wall at its start and a wall crossing under it at 8 ft, a wall
        /// standing on its overhang (10 to 14 ft). By statics, ΣM about the start: a hold-down's T at a gives T a / 8 at the
        /// back of the overhang and T (1 − a / 8) at the start, less than nothing: the start is pressed down (no hold-down).
        /// Nothing at the tip.
        /// </summary>
        [TestMethod]
        public void ACantilever_ByStatics_TheBackSupportTakesMoreThanT()
        {
            var lower = new Dictionary<int, WallData> { [11] = new WallData(9, 0, 0, 0, 20), [12] = new WallData(9, 8, 0, 8, 20) };
            var upper = new Dictionary<int, WallData> { [1] = new WallData(9, 10, 10, 14, 10) };
            var design = new Dictionary<int, WallDesignResult> { [1] = new WallDesignResult { TensionAsd = 1000, TensionLrfd = 1667 } };
            var bm = new FramingBeam { Id = "BM1", Start = new Point(0, 10), End = new Point(14, 10), OnStory = 0 };
            var trace = new LoadPathTrace(new[] { bm }, null, Size);
            trace.Land(1, upper, design, lower);

            var rb = trace.Beams.Single();
            Assert.IsTrue(rb.Stands);
            CollectionAssert.AreEqual(new[] { "start", "at 8 ft" }, rb.Supports.Select(s => s.Name).ToArray());
            var (p1, p2) = WallSchedule.HoldownPoints(upper[1], null);
            double a = System.Math.Max(p1.X, p2.X);
            Assert.AreEqual(1000 * a / 8, rb.Supports[1].Asd, 1e-6, "T a / 8: more than T");
            Assert.IsTrue(rb.Supports[0].Asd < 0, "the start is pressed down");
            foreach (var ld in rb.Loads) Assert.AreEqual(ld.Asd, ld.ToSupports.Sum(), 1e-6, "ΣF");
            var on = trace.ByLevel[0];
            var back = on.Single(l => l.From == "beam BM1 at 8 ft");
            Assert.AreEqual(12, back.LowerWallId);
            Assert.AreEqual(8, back.At.X, 1e-9);
            Assert.IsFalse(on.Any(l => l.From == "beam BM1 start" || l.From == "beam BM1 end"), "no hold-down at the start (pressed) nor at the tip (nothing there)");
            Assert.IsFalse(on.Any(l => l.Support == SupportKind.None));
        }

        /// <summary>
        /// A beam continuous over a wall at its middle (two 10 ft spans, EI the same along it), a wall standing on its first
        /// span. A unit load at a in the first span: the middle support's moment m = a (l² − a²) / (4 l²), so the far end
        /// takes −m / l (pressed down), the start 1 − a / l − m / l, the middle a / l + 2 m / l.
        /// </summary>
        [TestMethod]
        public void AContinuousBeam_EIConstant_TheTextbook()
        {
            var lower = new Dictionary<int, WallData>
            {
                [11] = new WallData(9, 0, 0, 0, 20), [12] = new WallData(9, 10, 0, 10, 20), [13] = new WallData(9, 20, 0, 20, 20),
            };
            var upper = new Dictionary<int, WallData> { [1] = new WallData(9, 2, 10, 8, 10) };
            var design = new Dictionary<int, WallDesignResult> { [1] = new WallDesignResult { TensionAsd = 1000, TensionLrfd = 1667 } };
            var bm = new FramingBeam { Id = "BM1", Start = new Point(0, 10), End = new Point(20, 10), OnStory = 0 };
            var trace = new LoadPathTrace(new[] { bm }, null, Size);
            trace.Land(1, upper, design, lower);

            var rb = trace.Beams.Single();
            CollectionAssert.AreEqual(new[] { "start", "at 10 ft", "end" }, rb.Supports.Select(s => s.Name).ToArray());
            const double l = 10;
            double K(double a) => a * (l * l - a * a) / (4 * l * l * l);
            var (p1, p2) = WallSchedule.HoldownPoints(upper[1], null);
            double lo = System.Math.Min(p1.X, p2.X), hi = System.Math.Max(p1.X, p2.X);
            Assert.AreEqual(1000 * System.Math.Max(1 - lo / l - K(lo), 1 - hi / l - K(hi)), rb.Supports[0].Asd, 1e-6);
            Assert.AreEqual(1000 * System.Math.Max(lo / l + 2 * K(lo), hi / l + 2 * K(hi)), rb.Supports[1].Asd, 1e-6);
            Assert.AreEqual(1000 * System.Math.Max(-K(lo), -K(hi)), rb.Supports[2].Asd, 1e-6, "the far end pressed down");
            var on = trace.ByLevel[0];
            Assert.AreEqual(11, on.Single(x => x.From == "beam BM1 start").LowerWallId);
            Assert.AreEqual(12, on.Single(x => x.From == "beam BM1 at 10 ft").LowerWallId);
            Assert.IsFalse(on.Any(x => x.From == "beam BM1 end"), "pressed down only: no hold-down");
        }

        /// <summary>A beam on one wall only, at its start: it cannot stand. Its end goes on nothing: an error.</summary>
        [TestMethod]
        public void ABeamOnOneSupport_CannotStand_AnError()
        {
            var lower = new Dictionary<int, WallData> { [11] = new WallData(9, 0, 0, 0, 20) };
            var upper = new Dictionary<int, WallData> { [1] = new WallData(9, 2, 10, 8, 10) };
            var design = new Dictionary<int, WallDesignResult> { [1] = new WallDesignResult { TensionAsd = 1000, TensionLrfd = 1667 } };
            var bm = new FramingBeam { Id = "BM1", Start = new Point(0, 10), End = new Point(12, 10), OnStory = 0 };
            var trace = new LoadPathTrace(new[] { bm }, null, Size);
            trace.Land(1, upper, design, lower);
            Assert.IsFalse(trace.Beams.Single().Stands);
            Assert.AreEqual(SupportKind.None, trace.ByLevel[0].Single(x => x.From == "beam BM1 end").Support);
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

        /// <summary>
        /// The tutorial's stair trimmer: a beam (x = 12, y 0 to 20) on the S and N walls, which are shear walls only from x = 0
        /// to 6 (their shear parts), with a wall on the beam. Its ends are on the plain parts of the walls: each goes down a
        /// post in the wall (owner 2026-11-10: as a column) to the foundation, anchored there for its reaction. Nothing is
        /// "on nothing".
        /// </summary>
        [TestMethod]
        public void ABeamEnd_OnAPlainWall_GoesDownAPostInIt_ToTheFoundation()
        {
            var shearParts = new Dictionary<int, WallData> { [13] = new WallData(9, 0, 0, 6, 0), [14] = new WallData(9, 0, 20, 6, 20) };
            var walls = new[]
            {
                new FramingWall { Id = "W1", Start = new Point(0, 0), End = new Point(20, 0), Story = 0 },
                new FramingWall { Id = "W2", Start = new Point(0, 20), End = new Point(20, 20), Story = 0 },
            };
            var upper = new Dictionary<int, WallData> { [1] = new WallData(9, 12, 4, 12, 14) }; // N-S, along the beam
            var design = new Dictionary<int, WallDesignResult> { [1] = new WallDesignResult { TensionAsd = 1000, TensionLrfd = 1667 } };
            var beam = new FramingBeam { Id = "BM3", Start = new Point(12, 0), End = new Point(12, 20), OnStory = 0 };
            var trace = new LoadPathTrace(new[] { beam }, null, Size, walls);
            trace.Land(1, upper, design, shearParts);

            var r = trace.Beams.Single();
            Assert.IsTrue(r.Stands, "held by the two walls at its ends, plain parts though they are");
            var on = trace.ByLevel[0];
            Assert.IsFalse(on.Any(x => !x.Supported), "everything lands on something");
            var start = on.Single(x => x.From == "beam BM3 start");
            Assert.AreEqual(SupportKind.Wall, start.Support);
            Assert.AreEqual("wall W1", start.SupportId);
            Assert.IsNull(start.LowerWallId, "not a shear wall's shear part: no force on a shear wall's overturning");
            Assert.AreEqual(0, trace.ExtraOn(0).Count);

            trace.Land(0, new Dictionary<int, WallData>(), new Dictionary<int, WallDesignResult>(), null);
            var posts = trace.ByLevel[-1].Where(x => x.Kind == PathPointKind.WallPost).OrderBy(x => x.At.Y).ToList();
            Assert.AreEqual(2, posts.Count);
            Assert.AreEqual("wall W1 post", posts[0].From);
            Assert.AreEqual("wall W2 post", posts[1].From);
            Assert.IsTrue(posts.All(x => x.OnFoundation && x.Support == SupportKind.Foundation));
            Assert.AreEqual(r.StartAsd, posts[0].TensionAsd, 1e-9, "the post carries the beam end's reaction down");
            Assert.AreEqual(r.EndAsd, posts[1].TensionAsd, 1e-9);
            Assert.IsNotNull(posts[0].Model, "anchored at the foundation, sized for it");
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
