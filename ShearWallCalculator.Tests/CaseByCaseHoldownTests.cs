using System.Collections.Generic;
using System.Linq;
using System.Windows;
using calculator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShearWallCalculator.Design;

namespace ShearWallCalculator.Tests
{
    /// <summary>
    /// Hold-down tensions case by case. Each wall's design hold-down is its worst case, but two walls' worst cases are not at
    /// the same time (an E-W wall peaks in a W or E case, a N-S wall in a N or S case): where their hold-downs share a column
    /// or a beam, the support takes the worst case of their SUM, not the sum of their worst cases.
    /// </summary>
    [TestClass]
    public class CaseByCaseHoldownTests
    {
        private static HoldownChoice Size(HoldownFamilies f, WoodTypes s, double t) => new HoldownChoice { Model = $"{f}:{t:0}", Allowable = t };

        private static Dictionary<string, double[]> Low(params (string c, double asd)[] cases) =>
            cases.ToDictionary(x => x.c, x => new[] { x.asd, 0, x.asd * 1.6, 0 });

        [TestMethod]
        public void CaseTension_ByHand_OnTheEndThatLifts()
        {
            // V = 10 k, h = 10 ft, d = 9.75 ft, D = 2000 lb, walls above 500 lb (ASD) / 800 lb (LRFD) at the design shear
            var r = new WallDesignResult { V = 10, Height = 10, HoldownArm = 9.75, DeadLoadLb = 2000, AboveTensionAsd = 500, AboveTensionLrfd = 800 };
            r.SetCaseTensions(new[]
            {
                new KeyValuePair<string, double>("A", -10), // force on the diaphragm −x: the top is pushed +x, the low (west) end lifts
                new KeyValuePair<string, double>("B", 4),   // +x: the high end lifts, at 40 % of the design shear
            });
            // A: 0.6 × 10 k × 1000 × 10 ft / 9.75 ft − 0.6 × 2000 / 2 + 500 = 6153.85 − 600 + 500
            Assert.AreEqual(6053.846, r.TensionByCase["A"][0], 1e-3);
            Assert.AreEqual(0, r.TensionByCase["A"][1]);
            // LRFD: 1.0 × 100000 / 9.75 − 0.9 × 2000 / 2 + 800
            Assert.AreEqual(10156.410, r.TensionByCase["A"][2], 1e-3);
            // B: 0.6 × 4 × 1000 × 10 / 9.75 − 600 + 500 × 0.4
            Assert.AreEqual(0, r.TensionByCase["B"][0]);
            Assert.AreEqual(2061.538, r.TensionByCase["B"][1], 1e-3);
        }

        [TestMethod]
        public void CaseTension_TheGoverningCase_IsTheDesignTension()
        {
            var r = new WallDesignResult { V = 7.5, Height = 9, HoldownArm = 11.5, DeadLoadLb = 1200, AboveTensionAsd = 300, AboveTensionLrfd = 450 };
            r.TensionAsd = 0.6 * 7.5 * 1000 * 9 / 11.5 - 0.6 * 1200 / 2 + 300; // the design's own formula (ShearWallDesigner.Check)
            r.SetCaseTensions(new[] { new KeyValuePair<string, double>("gov", 7.5), new KeyValuePair<string, double>("other", -3) });
            Assert.AreEqual(r.TensionAsd, r.TensionByCase["gov"][1], 1e-9);
        }

        [TestMethod]
        public void ACornerColumn_TakesTheWorstCaseOfTheSum_NotTheSumOfTheWorst()
        {
            // wall 1 E-W along y = 0 and wall 2 N-S, their low hold-downs at one point, on column C1 to the foundation
            var w1 = new WallData(9, 0, 0, 10, 0);
            var (p, _) = WallSchedule.HoldownPoints(w1, null);
            var w2 = new WallData(9, p.X, p.Y - (p.X - 0), p.X, 10);
            Assert.AreEqual(p.Y, WallSchedule.HoldownPoints(w2, null).Item1.Y, 1e-9, "the test's walls share the point");
            var upper = new Dictionary<int, WallData> { [1] = w1, [2] = w2 };
            var design = new Dictionary<int, WallDesignResult>
            {
                // W case: wall 1 at its worst; S case: wall 2 at its worst; Q (quartering): both at about 75 %
                [1] = new WallDesignResult { TensionAsd = 1000, TensionLrfd = 1600, TensionByCase = Low(("W", 1000), ("S", 0), ("Q", 750)) },
                [2] = new WallDesignResult { TensionAsd = 800, TensionLrfd = 1280, TensionByCase = Low(("W", 0), ("S", 800), ("Q", 600)) },
            };
            var col = new FramingColumn { Id = "C1", At = p, BaseStory = 0, TopStory = 0 };
            var trace = new LoadPathTrace(null, new[] { col }, Size);
            trace.Land(1, upper, design, new Dictionary<int, WallData>());
            var onColumn = trace.ByLevel[0].Where(x => x.Support == SupportKind.Column).ToList();
            Assert.AreEqual(2, onColumn.Count);
            Assert.AreEqual(1000, onColumn.Single(x => x.WallId == 1).TensionAsd, 1e-9, "each wall's own hold-down is still its worst case");

            trace.Land(0, new Dictionary<int, WallData>(), new Dictionary<int, WallDesignResult>(), null);
            var b = trace.ByLevel[-1].Single(x => x.Kind == PathPointKind.ColumnBase);
            // max(W: 1000 + 0, S: 0 + 800, Q: 750 + 600) = 1350, not 1000 + 800 = 1800
            Assert.AreEqual(1350, b.TensionAsd, 1e-9);
            Assert.AreEqual("HDU_ToFoundation:1350", b.Model);
            Assert.AreEqual(1350 * 1.6, trace.ByLevel[-1].Single(x => x.Kind == PathPointKind.ColumnBase).TensionLrfd, 1e-6);
        }

        [TestMethod]
        public void ABeam_EndReactions_CaseByCase()
        {
            var lower = new Dictionary<int, WallData> { [11] = new WallData(9, 0, 0, 0, 20), [12] = new WallData(9, 20, 0, 20, 20) };
            var a = new WallData(9, 5, 10, 15, 10);                     // E-W on the beam: both hold-downs on it
            var h = WallSchedule.HoldownPoints(a, null).Item1.X - 5;    // half a post, ft
            var bw = new WallData(9, 12, 10 - h, 12, 20);               // N-S: its low hold-down at (12, 10) on the beam
            var upper = new Dictionary<int, WallData> { [1] = a, [2] = bw };
            var design = new Dictionary<int, WallDesignResult>
            {
                [1] = new WallDesignResult
                {
                    TensionAsd = 1000, TensionLrfd = 1600,
                    TensionByCase = new Dictionary<string, double[]>
                    {
                        ["W"] = new double[] { 1000, 0, 1600, 0 }, ["E"] = new double[] { 0, 1000, 0, 1600 },
                        ["S"] = new double[] { 0, 0, 0, 0 }, ["Q"] = new double[] { 750, 0, 1200, 0 },
                    },
                },
                [2] = new WallDesignResult { TensionAsd = 800, TensionLrfd = 1280, TensionByCase = Low(("W", 0), ("E", 0), ("S", 800), ("Q", 600)) },
            };
            var beam = new FramingBeam { Id = "BM1", Start = new Point(0, 10), End = new Point(20, 10), OnStory = 0 };
            var trace = new LoadPathTrace(new[] { beam }, null, Size);
            trace.Land(1, upper, design, lower);

            double L = 20, a1 = 5 + h, a2 = 15 - h, xb = 12;
            double S(double x) => (L - x) / L;
            double E(double x) => x / L;
            double start = new[] { 1000 * S(a1), 1000 * S(a2), 800 * S(xb), 750 * S(a1) + 600 * S(xb) }.Max();
            double end = new[] { 1000 * E(a1), 1000 * E(a2), 800 * E(xb), 750 * E(a1) + 600 * E(xb) }.Max();
            var r = trace.Beams.Single();
            Assert.AreEqual(start, r.StartAsd, 1e-6);
            Assert.AreEqual(end, r.EndAsd, 1e-6);
            // the envelope rule (each wind direction, every wall at its worst) gives more at the start
            Assert.IsTrue(r.StartAsd < 1000 * S(a1) + 800 * S(xb) - 1);
            var ends = trace.ByLevel[0].Where(x => x.Kind == PathPointKind.BeamEnd).OrderBy(x => x.At.X).ToList();
            Assert.AreEqual(start, ends[0].TensionAsd, 1e-6);
            Assert.AreEqual(end, ends[1].TensionAsd, 1e-6);
        }

        [TestMethod]
        public void AHoldownTypeThatCannotAnchor_IsReplaced_AndTheWallIsTold()
        {
            var a = new WallData(9, 5, 10, 15, 10) { Design = new WallDesignInputs { Holdown = HoldownFamilies.HDU_ToFoundation } };
            var design = new Dictionary<int, WallDesignResult> { [1] = new WallDesignResult { TensionAsd = 900 } };
            var beam = new FramingBeam { Id = "BM1", Start = new Point(0, 10), End = new Point(20, 10), OnStory = 0 };
            var lower = new Dictionary<int, WallData> { [11] = new WallData(9, 0, 0, 0, 20), [12] = new WallData(9, 20, 0, 20, 20) };
            var trace = new LoadPathTrace(new[] { beam }, null, Size);
            trace.Land(1, new Dictionary<int, WallData> { [1] = a }, design, lower);
            Assert.IsTrue(trace.ByLevel[0].Where(x => x.Kind == PathPointKind.WallHoldown).All(x => x.Family == HoldownFamilies.Strap_ToWallBelow && x.Choice != null));
            CollectionAssert.AreEqual(new[] { "Strap_ToWallBelow:900 strap on beam BM1" }, design[1].HoldownElsewhere);
        }
    }
}
