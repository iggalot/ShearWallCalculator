using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShearWallCalculator.Design;

namespace ShearWallCalculator.Tests
{
    /// <summary>A diaphragm's nailing for a wind unit shear (SDPWS-2015 Tables 4.2A and 4.2C, ASD: the nominal value ÷ 2.0), by hand.</summary>
    [TestClass]
    public class DiaphragmNailingTests
    {
        private const double Floor = 23.0 / 32.0, Roof = 7.0 / 16.0;

        /// <summary>
        /// The floors' defaults: Sheathing and Single-Floor, 10d, 23/32" (the 19/32" row, the table's thickest), 2" framing,
        /// blocked. 300 plf: 895 / 2 = 447.5 at 6"/6". 500: 1190 / 2 = 595 at 4"/6". 700: past 4"/6", 2-1/2" needs 3" framing (4.2.7.1.1
        /// item 3): the 3" row, 2015 / 2 = 1007.5 at 2-1/2"/4". 1200: over 2295 / 2 = 1147.5 at 2"/3": fails.
        /// </summary>
        [TestMethod]
        public void Blocked_Floor_HandCalc()
        {
            var a = DiaphragmNailing.Design(300, false, "10d", Floor, 2, true);
            Assert.IsNull(a.Fails, a.Fails);
            Assert.AreEqual(6, a.BoundaryIn); Assert.AreEqual(6, a.EdgesIn); Assert.AreEqual(447.5, a.AllowablePlf, 1e-9);
            StringAssert.Contains(a.Row, "19/32");
            Assert.IsNull(a.Note);

            var b = DiaphragmNailing.Design(500, false, "10d", Floor, 2, true);
            Assert.AreEqual(4, b.BoundaryIn); Assert.AreEqual(6, b.EdgesIn); Assert.AreEqual(595, b.AllowablePlf, 1e-9);

            var c = DiaphragmNailing.Design(700, false, "10d", Floor, 2, true);
            Assert.IsNull(c.Fails, c.Fails);
            Assert.AreEqual(2.5, c.BoundaryIn); Assert.AreEqual(4, c.EdgesIn); Assert.AreEqual(1007.5, c.AllowablePlf, 1e-9);
            StringAssert.Contains(c.Note, "3\" nominal framing");

            var d = DiaphragmNailing.Design(1200, false, "10d", Floor, 2, true);
            StringAssert.Contains(d.Fails, "Table 4.2B");
        }

        /// <summary>
        /// The roof's defaults: Sheathing and Single-Floor, 8d, 7/16", 2" framing, unblocked: Table 4.2C Cases 2-6, 475 / 2 =
        /// 237.5 plf at 6". 300 plf fails; blocked, Table 4.2A 8d 7/16" 2": 715 / 2 = 357.5 at 6"/6" would take it.
        /// </summary>
        [TestMethod]
        public void Unblocked_Roof_HandCalc()
        {
            var a = DiaphragmNailing.Design(200, false, "8d", Roof, 2, false);
            Assert.IsNull(a.Fails, a.Fails);
            Assert.AreEqual(6, a.BoundaryIn); Assert.AreEqual(6, a.EdgesIn); Assert.AreEqual(237.5, a.AllowablePlf, 1e-9);

            var b = DiaphragmNailing.Design(300, false, "8d", Roof, 2, false);
            StringAssert.Contains(b.Fails, "238 plf");
            StringAssert.Contains(b.Fails, "8d at 6\" o.c. at the boundaries");
            StringAssert.Contains(b.Fails, "358 plf");
        }

        /// <summary>Footnote 2: Spruce-Pine-Fir (G 0.42): 1 − (0.5 − 0.42) = 0.92; 895 × 0.92 / 2 = 411.7. DF-L and SP: 1.0.</summary>
        [TestMethod]
        public void SpecificGravityFactor_Footnote2()
        {
            Assert.AreEqual(0.92, DiaphragmNailing.SpecificGravityFactor(0.42, false), 1e-9);
            Assert.AreEqual(1.0, DiaphragmNailing.SpecificGravityFactor(0.55, false), 1e-9, "not over 1");
            Assert.AreEqual(1.0, DiaphragmNailing.SpecificGravityFactor(0.42, true), 1e-9);
            Assert.AreEqual(411.7, DiaphragmNailing.Design(300, false, "10d", Floor, 2, true, 0.92).AllowablePlf, 0.01);
        }

        [TestMethod]
        public void NoRow_OrNoNail_Fails()
        {
            StringAssert.Contains(DiaphragmNailing.Design(100, true, "8d", 0.25, 2, true).Fails, "No row");
            StringAssert.Contains(DiaphragmNailing.Design(100, false, "16d", Floor, 2, true).Fails, "not a nail");
        }
    }
}
