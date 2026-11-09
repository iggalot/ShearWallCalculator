using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShearWallCalculator.Design;

namespace ShearWallCalculator.Tests
{
    /// <summary>
    /// The ties picked from the catalog (C-C-2026): a splice strap from the strap ties of p. 291, the LTP4 framing plate of
    /// p. 310. The expected numbers are typed here from the catalog's pages, not taken from the code.
    /// </summary>
    [TestClass]
    public class TiesTests
    {
        [DataTestMethod]
        // the lightest that takes it, DF/SP (160): 1,662 lb → ST2215 1,875; 301 lb → HRS6 605
        [DataRow(1662, 9.25, "ST2215", 1875, 20)]
        [DataRow(301, 5.5, "HRS6", 605, 6)]
        // a 2x4's face (3.5": at most 2.5" wide): MSTC28 (3" wide, 3,460) does not fit, MSTI36 (2-1/16", 3,800) does
        [DataRow(3000, 3.5, "MSTI36", 3800, 36)]
        [DataRow(3000, 9.25, "MSTC28", 3460, 36)]
        public void SpliceStrap_TheLightestThatTakesItAndFits(double force, double depth, string model, double allowable, int nails)
        {
            var s = SpliceStrap.Pick(force, false, depth);
            Assert.IsNull(s.Fails, s.Fails);
            Assert.AreEqual(model, s.Model);
            Assert.AreEqual(allowable, s.AllowableLb);
            Assert.AreEqual(nails, s.Nails);
            StringAssert.Contains(s.Describe(), $"{allowable:0} lb ≥ {force:0} lb");
        }

        [TestMethod]
        public void SpliceStrap_OverTheStrongest_Fails()
        {
            var s = SpliceStrap.Pick(6000, false, 9.25);
            Assert.IsNull(s.Model);
            StringAssert.Contains(s.Fails, "the most: 5850 lb"); // MSTC66/78, DF/SP (160)
        }

        [DataTestMethod]
        // LTP4, configuration 7, G, (160): DF/SP 715 lb, SPF/HF 615; s = 12 A / v to the inch below, at most 24"
        [DataRow(200, false, 24.0)]    // 42.9" → 24"
        [DataRow(500, false, 17.0)]    // 17.16"
        [DataRow(500, true, 14.0)]     // 615 × 12 / 500 = 14.76"
        public void TopClip_SpacedForTheShear(double v, bool spf, double spacing)
        {
            var c = TopClip.Design(v, spf, new List<double> { 0.4375 });
            Assert.IsNull(c.Fails, c.Fails);
            Assert.AreEqual(spf ? 615 : 715, c.AllowableLb);
            Assert.AreEqual(spacing, c.SpacingIn);
            Assert.AreEqual("(12) 0.131\" x 1-1/2\" nails", c.Fastener, "on the face with no wood panel");
        }

        [TestMethod]
        public void TopClip_OverSheathing_LongerNails_UpToHalfInch()
        {
            var c = TopClip.Design(500, false, new List<double> { 0.5, 0.4375 });
            Assert.IsNull(c.Fails, c.Fails);
            Assert.AreEqual("(12) 0.131\" x 2-1/2\" nails", c.Fastener, "p. 310 footnote 3: the full load over 3/8\" or 1/2\"");
            StringAssert.Contains(c.Placement, "0.438\" wood panel");
            StringAssert.Contains(TopClip.Design(500, false, new List<double> { 0.59375, 0.75 }).Fails, "over 1/2\" at most");
            StringAssert.Contains(TopClip.Design(1500, false, null).Fails, "closer than 6\""); // 5.7"
        }
    }
}
