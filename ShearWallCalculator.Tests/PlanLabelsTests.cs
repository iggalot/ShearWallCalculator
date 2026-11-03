using System.Windows;
using ShearWallCalculator.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ShearWallCalculator.Tests
{
    /// <summary>The plan's landing labels: short, and outside the building past its nearest side.</summary>
    [TestClass]
    public class PlanLabelsTests
    {
        [TestMethod]
        public void ShortFrom_DropsTheWords()
        {
            Assert.AreEqual("BM2", PlanLabels.ShortFrom("beam BM2 end"));
            Assert.AreEqual("BM2", PlanLabels.ShortFrom("beam BM2 start"));
            Assert.AreEqual("C3", PlanLabels.ShortFrom("column C3 base"));
            Assert.AreEqual("", PlanLabels.ShortFrom(null));
        }

        // a building 0..600 × 0..480 px; a label 100 × 14, 16 px off the side
        private static readonly Rect B = new Rect(0, 0, 600, 480);
        private static readonly Size S = new Size(100, 14);
        private static readonly Rect Wide = new Rect(-500, -500, 1600, 1500);

        [TestMethod]
        public void OutsideSpot_PastTheNearestSide_CentredOnThePoint()
        {
            var (top, up) = PlanLabels.OutsideSpot(new Point(560, 160), B, S, 16, Wide); // nearest the right side (40 px)
            Assert.AreEqual(new Rect(616, 153, 100, 14), top);
            Assert.AreEqual(new Vector(1, 0), up);

            var (bot, down) = PlanLabels.OutsideSpot(new Point(500, 470), B, S, 16, Wide); // BM2's start, near the bottom
            Assert.AreEqual(new Rect(450, 496, 100, 14), bot);
            Assert.AreEqual(new Vector(0, 1), down);

            var (l, left) = PlanLabels.OutsideSpot(new Point(20, 240), B, S, 16, Wide);
            Assert.AreEqual(new Rect(-116, 233, 100, 14), l);
            Assert.AreEqual(new Vector(-1, 0), left);

            var (t, upw) = PlanLabels.OutsideSpot(new Point(300, 30), B, S, 16, Wide);
            Assert.AreEqual(new Rect(250, -30, 100, 14), t);
            Assert.AreEqual(new Vector(0, -1), upw);
        }

        [TestMethod]
        public void OutsideSpot_NoRoomBeside_GoesBelow_KeptOnTheCanvas()
        {
            // the plan panel barely wider than the building: a label by the right side goes past the bottom instead,
            // slid left to stay on the canvas (BM2's strap, 130 px up from the bottom)
            var canvas = new Rect(-20, -60, 640, 600);
            var (box, outward) = PlanLabels.OutsideSpot(new Point(600, 350), B, new Size(250, 14), 16, canvas);
            Assert.AreEqual(new Vector(0, 1), outward);
            Assert.AreEqual(new Rect(370, 496, 250, 14), box);
        }

        [TestMethod]
        public void Leader_StraightWhenPastTheNearestSide_ElseOutAndAround()
        {
            // a label below a point near the bottom: straight to it
            var straight = PlanLabels.Leader(new Point(500, 470), B, new Rect(450, 496, 100, 14), 16);
            CollectionAssert.AreEqual(new[] { new Point(500, 496), new Point(500, 470) }, straight);
            // a point on the right side with its label above the building: out past the right side first, then up to the label
            var around = PlanLabels.Leader(new Point(600, 240), B, new Rect(520, -30, 100, 14), 16);
            CollectionAssert.AreEqual(new[] { new Point(616, -16), new Point(616, 240), new Point(600, 240) }, around);
            // a point on the right side with its label below the building, off to the left (BM2's strap on junk): out past
            // the right side, down round the corner, then along under the building, never across it
            var corner = PlanLabels.Leader(new Point(600, 350), B, new Rect(200, 496, 250, 14), 16);
            CollectionAssert.AreEqual(new[] { new Point(450, 496), new Point(616, 496), new Point(616, 350), new Point(600, 350) }, corner);
        }

        [TestMethod]
        public void OutsideSpot_ACorner_GoesAboveOrBelow()
        {
            Assert.AreEqual(new Vector(0, -1), PlanLabels.OutsideSpot(new Point(0, 0), B, S, 16, Wide).outward);
            Assert.AreEqual(new Vector(0, 1), PlanLabels.OutsideSpot(new Point(600, 480), B, S, 16, Wide).outward);
        }
    }
}
