using System;
using System.Windows;
using calculator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShearWallCalculator.Drawing;
using ShearWallCalculator.Interfaces;

namespace ShearWallCalculator.Tests
{
    /// <summary>Plan image placement: pixel ↔ world, two-point scale, straighten, move, drawing-scale import, save.</summary>
    [TestClass]
    public class ReferenceImageTests
    {
        private const double Tol = 1e-9;

        private static void Near(Point expected, Point actual, string what = null)
        {
            Assert.AreEqual(expected.X, actual.X, 1e-7, what);
            Assert.AreEqual(expected.Y, actual.Y, 1e-7, what);
        }

        [TestMethod]
        public void Imported_BottomLeftAtOrigin_PixelRowsGoDown()
        {
            // 1000 x 500 px at 0.05 ft/px: 50 ft x 25 ft; top-left pixel at (0, 25)
            var img = ReferenceImage.Imported("plan.png", 1000, 500, 0.05);
            Near(new Point(0, 25), img.PixelToWorld(new Point(0, 0)));
            Near(new Point(0, 0), img.PixelToWorld(new Point(0, 500)), "bottom-left");
            Near(new Point(50, 0), img.PixelToWorld(new Point(1000, 500)), "bottom-right");
            Near(new Point(250, 100), img.WorldToPixel(img.PixelToWorld(new Point(250, 100))), "round trip");
            Assert.AreEqual(new Rect(0, 0, 50, 25), img.WorldBounds());
        }

        [TestMethod]
        public void DrawingScale_QuarterInchAt150Dpi()
        {
            // 1/4" = 1'-0": 4 ft per paper inch; 150 px per inch → 4 / 150 ft per pixel (0.32 in)
            Assert.AreEqual(4.0 / 150.0, ReferenceImage.FeetPerPixelFromDrawingScale(4, 150), Tol);
            Assert.Throws<ArgumentException>(() => ReferenceImage.FeetPerPixelFromDrawingScale(4, 0));
        }

        [TestMethod]
        public void Calibrate_TwoPoints_FirstPointStays()
        {
            // a line drawn 10 ft long on the image is really 30 ft: everything triples about the first point
            var img = ReferenceImage.Imported("p.png", 400, 400, 0.1);
            Point a = new Point(5, 5), b = new Point(15, 5);
            Point pxA = img.WorldToPixel(a), pxB = img.WorldToPixel(b);
            img.Calibrate(a, b, 30);
            Assert.AreEqual(0.3, img.FeetPerPixel, Tol);
            Near(a, img.PixelToWorld(pxA), "the first point stays");
            Assert.AreEqual(30, (img.PixelToWorld(pxB) - a).Length, 1e-7);
            Assert.IsTrue(img.Calibrated);
            Assert.Throws<ArgumentException>(() => img.Calibrate(a, a, 10));
            Assert.Throws<ArgumentException>(() => img.Calibrate(a, b, 0));
        }

        [TestMethod]
        public void Straighten_ToNearestAxis_AboutFirstPoint()
        {
            // a wall line on the scan runs 3° off E-W: rotate by −3°
            var img = ReferenceImage.Imported("p.png", 400, 400, 0.1);
            double t = 3 * Math.PI / 180;
            Point a = new Point(10, 10), b = new Point(10 + 20 * Math.Cos(t), 10 + 20 * Math.Sin(t));
            Point pxB = img.WorldToPixel(b);
            Assert.AreEqual(-3, img.Straighten(a, b), 1e-9);
            Assert.AreEqual(-3, img.RotationDegrees, 1e-9);
            Point nb = img.PixelToWorld(pxB);
            Assert.AreEqual(10, nb.Y, 1e-7, "now level");
            Assert.AreEqual(30, nb.X, 1e-7);

            // 88° → 90° (N-S)
            var img2 = ReferenceImage.Imported("p.png", 400, 400, 0.1);
            double t2 = 88 * Math.PI / 180;
            Assert.AreEqual(2, img2.Straighten(new Point(0, 0), new Point(Math.Cos(t2), Math.Sin(t2))), 1e-9);
        }

        [TestMethod]
        public void Rotated_PixelWorldRoundTrip_AndMatrixAgrees()
        {
            var img = new ReferenceImage { FeetPerPixel = 0.02, RotationDegrees = 30, Origin = new Point(3, 40), PixelWidth = 800, PixelHeight = 600 };
            var px = new Point(123, 456);
            Near(px, img.WorldToPixel(img.PixelToWorld(px)));
            Near(img.PixelToWorld(px), img.PixelToWorldMatrix().Transform(px), "drawing matrix");
            // +u runs at 30° CCW from +x
            Vector u = img.PixelToWorld(new Point(100, 0)) - img.Origin;
            Assert.AreEqual(30, Math.Atan2(u.Y, u.X) * 180 / Math.PI, 1e-9);
        }

        [TestMethod]
        public void Move_BasePointToDestination()
        {
            var img = ReferenceImage.Imported("p.png", 100, 100, 0.1);
            Point px = img.WorldToPixel(new Point(2, 3));
            img.Move(new Point(2, 3), new Point(12, -4));
            Near(new Point(12, -4), img.PixelToWorld(px));
        }

        [TestMethod]
        public void ProjectFile_KeepsTheImagePlacement()
        {
            var calc = new ShearWallCalculator_RigidDiaphragm
            {
                Image = new ReferenceImage
                {
                    Path = @"C:\plans\first floor.png", FeetPerPixel = 0.0266, RotationDegrees = -2.5, Origin = new Point(-3.25, 61.5),
                    Opacity = 0.4, Locked = true, Visible = false, PixelWidth = 3300, PixelHeight = 2550, Calibrated = true,
                },
            };
            var read = TestProject.RoundTrip(calc).Image;
            Assert.AreEqual(calc.Image.Path, read.Path);
            Assert.AreEqual(0.0266, read.FeetPerPixel, Tol);
            Assert.AreEqual(-2.5, read.RotationDegrees, Tol);
            Near(new Point(-3.25, 61.5), read.Origin);
            Assert.AreEqual(0.4, read.Opacity, Tol);
            Assert.IsTrue(read.Locked);
            Assert.IsFalse(read.Visible);
            Assert.AreEqual(3300, read.PixelWidth);
            Assert.IsTrue(read.Calibrated);
        }

        [TestMethod]
        public void Resolve_FindsTheImageBesideAMovedProject()
        {
            string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "swc-img-" + Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(dir);
            try
            {
                string img = System.IO.Path.Combine(dir, "plan.png");
                System.IO.File.WriteAllText(img, "x");
                var r = new ReferenceImage { Path = @"Z:\gone\plan.png" };
                Assert.AreEqual(img, r.Resolve(System.IO.Path.Combine(dir, "job.swc.json")));
                Assert.IsNull(new ReferenceImage { Path = @"Z:\gone\other.png" }.Resolve(System.IO.Path.Combine(dir, "job.swc.json")));
            }
            finally { System.IO.Directory.Delete(dir, true); }
        }

        [TestMethod]
        public void ProjectFile_StoresTheImageItself_ReopensAtItsScaleWithoutTheOriginalFile()
        {
            string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "swc-img-" + Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(dir);
            try
            {
                string file = System.IO.Path.Combine(dir, "plan.png");
                var bytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 1, 2, 3, 4, 5 };
                System.IO.File.WriteAllBytes(file, bytes);
                var img = ReferenceImage.Imported(file, 1000, 800, 0.05);
                img.Calibrate(new Point(0, 0), new Point(10, 0), 20);
                img.RotateAbout(new Point(0, 0), 3);
                img.Visible = false;
                Assert.IsTrue(img.Embed(file));

                var calc = new ShearWallCalculator_RigidDiaphragm { Image = img };
                System.IO.File.Delete(file); // the original is gone: the project still has the image
                var read = TestProject.RoundTrip(calc).Image;

                CollectionAssert.AreEqual(bytes, read.Data);
                Assert.AreEqual(0.1, read.FeetPerPixel, Tol, "the final (calibrated) scale");
                Assert.AreEqual(3, read.RotationDegrees, Tol);
                Assert.AreEqual(img.Origin.X, read.Origin.X, 1e-9);
                Assert.AreEqual(img.Origin.Y, read.Origin.Y, 1e-9);
                Assert.IsTrue(read.Calibrated);
                Assert.IsFalse(read.Visible, "the layer's on/off state is saved too");
                Assert.IsFalse(new ReferenceImage().Embed(file), "a missing file is reported, not thrown");
            }
            finally { System.IO.Directory.Delete(dir, true); }
        }
    }
}
