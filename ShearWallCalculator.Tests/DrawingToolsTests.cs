using System.Collections.Generic;
using System.Windows;
using calculator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShearWallCalculator.Drawing;

namespace ShearWallCalculator.Tests
{
    /// <summary>Object snaps, typed coordinates and picking a wall or diaphragm on the drawing.</summary>
    [TestClass]
    public class DrawingToolsTests
    {
        private const double Tol = 1e-9;

        // a 20 ft E-W wall on y = 0 and a 10 ft N-S wall on x = 8 crossing it from y = -4 to y = 6
        private static readonly List<SnapSegment> Cross = new List<SnapSegment>
        {
            new SnapSegment(new Point(0, 0), new Point(20, 0)),
            new SnapSegment(new Point(8, -4), new Point(8, 6)),
        };

        private static void AreEqual(Point expected, Point actual)
        {
            Assert.AreEqual(expected.X, actual.X, Tol, "x");
            Assert.AreEqual(expected.Y, actual.Y, Tol, "y");
        }

        [TestMethod]
        public void Endpoint_WinsWhenInReach()
        {
            var r = Snapper.Find(new Point(19.6, 0.3), 1.0, Cross, SnapModes.Default);
            Assert.AreEqual(SnapModes.Endpoint, r.Kind);
            AreEqual(new Point(20, 0), r.Point);
        }

        [TestMethod]
        public void Midpoint_OfTheWall()
        {
            var r = Snapper.Find(new Point(10.4, 0.2), 1.0, Cross, SnapModes.Default);
            Assert.AreEqual(SnapModes.Midpoint, r.Kind);
            AreEqual(new Point(10, 0), r.Point);
        }

        [TestMethod]
        public void Intersection_OfTheTwoWalls_RanksAboveMidpoint()
        {
            // (8, 0) is the crossing; the N-S wall's midpoint (8, 1) is also in reach
            var r = Snapper.Find(new Point(8.3, 0.5), 1.0, Cross, SnapModes.Default);
            Assert.AreEqual(SnapModes.Intersection, r.Kind);
            AreEqual(new Point(8, 0), r.Point);
        }

        [TestMethod]
        public void Perpendicular_FromTheStartPoint()
        {
            // drawing from (14, 9): the foot on the E-W wall is (14, 0)
            var r = Snapper.Find(new Point(14.3, 0.4), 1.0, Cross, SnapModes.Perpendicular, from: new Point(14, 9));
            Assert.AreEqual(SnapModes.Perpendicular, r.Kind);
            AreEqual(new Point(14, 0), r.Point);
        }

        [TestMethod]
        public void Perpendicular_NeedsAStartPoint()
        {
            var r = Snapper.Find(new Point(14.3, 0.4), 1.0, Cross, SnapModes.Perpendicular);
            Assert.IsFalse(r.Found);
            AreEqual(new Point(14.3, 0.4), r.Point);
        }

        [TestMethod]
        public void Nearest_IsTheClosestPointOnTheWall()
        {
            var r = Snapper.Find(new Point(15.2, 0.6), 1.0, Cross, SnapModes.Nearest);
            Assert.AreEqual(SnapModes.Nearest, r.Kind);
            AreEqual(new Point(15.2, 0), r.Point);
        }

        [TestMethod]
        public void OutOfReach_OrSnapsOff_GivesTheCursor()
        {
            Assert.IsFalse(Snapper.Find(new Point(15, 5), 1.0, Cross, SnapModes.Default).Found);
            Assert.IsFalse(Snapper.Find(new Point(20, 0), 1.0, Cross, SnapModes.None).Found);
        }

        [TestMethod]
        public void ParallelWalls_DoNotIntersect()
        {
            Assert.IsFalse(Snapper.Intersect(new SnapSegment(new Point(0, 0), new Point(10, 0)), new SnapSegment(new Point(0, 2), new Point(10, 2)), out _));
            Assert.IsFalse(Snapper.Intersect(new SnapSegment(new Point(0, 0), new Point(10, 0)), new SnapSegment(new Point(12, -1), new Point(12, 1)), out _),
                "lines cross beyond the end of a segment");
        }

        // ------------------------------------------------------------------ typed coordinates

        private static Point Parse(string text, Point? last = null, Point toward = default(Point))
        {
            Assert.IsTrue(CoordinateInput.TryParse(text, last, toward, out var p, out var error), error);
            return p;
        }

        [TestMethod]
        public void Absolute_XY() => AreEqual(new Point(12, 30), Parse("12,30"));

        [TestMethod]
        public void Relative_XY() => AreEqual(new Point(15, 28.5), Parse("@3,-1.5", new Point(12, 30)));

        [TestMethod]
        public void Polar_LengthAndAngle()
        {
            AreEqual(new Point(10, 25), Parse("@15<90", new Point(10, 10)));
            AreEqual(new Point(-10, 10), Parse("@20<180", new Point(10, 10)));
            var p = Parse("@10<30", new Point(0, 0));
            AreEqual(new Point(8.660254038, 5), p);
        }

        [TestMethod]
        public void BareLength_GoesTowardTheCursor()
        {
            AreEqual(new Point(10, 22), Parse("12", new Point(10, 10), toward: new Point(10, 40)));
            AreEqual(new Point(-2, 10), Parse("12", new Point(10, 10), toward: new Point(3, 10)));
        }

        [TestMethod]
        public void FeetAndInches()
        {
            void Len(string s, double ft)
            {
                Assert.IsTrue(CoordinateInput.TryLength(s, out double v), s);
                Assert.AreEqual(ft, v, Tol, s);
            }
            Len("12'6\"", 12.5);
            Len("12'-6\"", 12.5);
            Len("12' 6", 12.5);
            Len("12'", 12);
            Len("6\"", 0.5);
            Len("-3'", -3);
            Len("7.25", 7.25);
            AreEqual(new Point(20.5, 8.25), Parse("20'6\",8'3\""));
        }

        [TestMethod]
        public void BadInput_ExplainsWhy()
        {
            Assert.IsFalse(CoordinateInput.TryParse("@5,0", null, default(Point), out _, out var e1));
            StringAssert.Contains(e1, "first point");
            Assert.IsFalse(CoordinateInput.TryParse("abc", new Point(0, 0), new Point(1, 0), out _, out var e2));
            StringAssert.Contains(e2, "abc");
            Assert.IsFalse(CoordinateInput.TryParse("20<90", new Point(0, 0), new Point(1, 0), out _, out var e3));
            StringAssert.Contains(e3, "@");
            Assert.IsFalse(CoordinateInput.TryParse("5", new Point(0, 0), new Point(0, 0), out _, out _), "no direction for a bare length");
            Assert.IsFalse(CoordinateInput.TryLength("12'x", out _));
        }

        // ------------------------------------------------------------------ picking

        [TestMethod]
        public void Pick_WallFirst_ThenSmallestDiaphragm()
        {
            var calc = new ShearWallCalculator_RigidDiaphragm();
            calc._diaphragm_system.AddDiaphragm(new DiaphragmData_Rectangular(new Point(0, 0), new Point(40, 30)));   // 0
            calc._diaphragm_system.AddDiaphragm(new DiaphragmData_Rectangular(new Point(10, 10), new Point(20, 20))); // 1, inside 0
            calc._wall_system.AddWall(new WallData(9, 0, 0, 20, 0));                                                // 0
            calc._wall_system.AddWall(new WallData(9, 20, 10, 20, 30));                                             // 1

            Assert.AreEqual(("Wall", 0), Snapper.Pick(calc, new Point(5, 0.3), 0.5));
            Assert.AreEqual(("Wall", 1), Snapper.Pick(calc, new Point(19.8, 15), 0.5), "a wall on top of a diaphragm wins");
            Assert.AreEqual(("Diaphragm", 1), Snapper.Pick(calc, new Point(15, 15), 0.5), "the inner diaphragm");
            Assert.AreEqual(("Diaphragm", 0), Snapper.Pick(calc, new Point(30, 5), 0.5));
            Assert.AreEqual(((string)null, -1), Snapper.Pick(calc, new Point(50, 50), 0.5));
        }
    }
}
