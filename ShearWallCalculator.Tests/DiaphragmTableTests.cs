using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShearWallCalculator.NDSProvisions;

namespace ShearWallCalculator.Tests
{
    /// <summary>
    /// SDPWS-2015 Tables 4.2A (blocked) and 4.2C (unblocked), Column B (wind), every row against the printed tables (AWC
    /// SDPWS-2015 Chapter 4, pages 21 and 23, read from the page images).
    /// </summary>
    [TestClass]
    public class DiaphragmTableTests
    {
        private const SheathingMaterials S1 = SheathingMaterials.SHEATH_MAT_STRUCTURAL_1, SF = SheathingMaterials.SHEATH_MAT_SHEATHING_AND_SINGLE_FLOOR;
        private const CommonNailSizes D6 = CommonNailSizes.NAILSIZE_6D, D8 = CommonNailSizes.NAILSIZE_8D, D10 = CommonNailSizes.NAILSIZE_10D;
        private const NominalPanelThicknesses T5_16 = NominalPanelThicknesses.THICKNESS_5_16IN, T3_8 = NominalPanelThicknesses.THICKNESS_3_8IN,
            T7_16 = NominalPanelThicknesses.THICKNESS_7_16IN, T15_32 = NominalPanelThicknesses.THICKNESS_15_32IN, T19_32 = NominalPanelThicknesses.THICKNESS_19_32IN;

        // grade, nail, penetration, thickness, width; 4.2A wind at 6/6, 4/6, 2-1/2/4, 2/3; 4.2C wind Case 1, Cases 2-6
        private static readonly (SheathingMaterials g, CommonNailSizes n, double p, NominalPanelThicknesses t, double w, double[] blocked, double[] unblocked)[] Printed =
        {
            (S1, D6, 1.25, T5_16, 2, new double[] { 520, 700, 1050, 1175 }, new double[] { 460, 350 }),
            (S1, D6, 1.25, T5_16, 3, new double[] { 590, 785, 1175, 1330 }, new double[] { 520, 390 }),
            (S1, D8, 1.375, T3_8, 2, new double[] { 755, 1010, 1485, 1680 }, new double[] { 670, 505 }),
            (S1, D8, 1.375, T3_8, 3, new double[] { 840, 1120, 1680, 1890 }, new double[] { 740, 560 }),
            (S1, D10, 1.5, T15_32, 2, new double[] { 895, 1190, 1790, 2045 }, new double[] { 800, 600 }),
            (S1, D10, 1.5, T15_32, 3, new double[] { 1010, 1345, 2015, 2295 }, new double[] { 895, 670 }),
            (SF, D6, 1.25, T5_16, 2, new double[] { 475, 630, 940, 1065 }, new double[] { 420, 310 }),
            (SF, D6, 1.25, T5_16, 3, new double[] { 530, 700, 1065, 1205 }, new double[] { 475, 350 }),
            (SF, D6, 1.25, T3_8, 2, new double[] { 520, 700, 1050, 1175 }, new double[] { 460, 350 }),
            (SF, D6, 1.25, T3_8, 3, new double[] { 590, 785, 1175, 1330 }, new double[] { 520, 390 }),
            (SF, D8, 1.375, T3_8, 2, new double[] { 670, 895, 1345, 1525 }, new double[] { 600, 450 }),
            (SF, D8, 1.375, T3_8, 3, new double[] { 755, 1010, 1510, 1710 }, new double[] { 670, 505 }),
            (SF, D8, 1.375, T7_16, 2, new double[] { 715, 950, 1415, 1610 }, new double[] { 645, 475 }),
            (SF, D8, 1.375, T7_16, 3, new double[] { 800, 1065, 1595, 1805 }, new double[] { 715, 530 }),
            (SF, D8, 1.375, T15_32, 2, new double[] { 755, 1010, 1485, 1680 }, new double[] { 670, 505 }),
            (SF, D8, 1.375, T15_32, 3, new double[] { 840, 1120, 1680, 1890 }, new double[] { 740, 560 }),
            (SF, D10, 1.5, T15_32, 2, new double[] { 810, 1080, 1610, 1835 }, new double[] { 715, 530 }),
            (SF, D10, 1.5, T15_32, 3, new double[] { 910, 1205, 1820, 2060 }, new double[] { 810, 600 }),
            (SF, D10, 1.5, T19_32, 2, new double[] { 895, 1190, 1790, 2045 }, new double[] { 800, 600 }),
            (SF, D10, 1.5, T19_32, 3, new double[] { 1010, 1345, 2015, 2295 }, new double[] { 895, 670 }),
        };

        [TestMethod]
        public void Table4_2A_WindColumn_AsPrinted()
        {
            var rows = new Table4_2Manager().Table4_2ANominalUnitShearCapacities.Values.ToList();
            Assert.AreEqual(Printed.Length, rows.Count, "every row of the printed table, no more");
            foreach (var e in Printed)
            {
                string at = $"{e.g} {e.n} {e.t} {e.w}\"";
                var r = rows.Single(x => x.SheathingGrade == e.g && x.CommonNailSize == e.n && x.NominalPanelThickness == e.t && x.NominalWidthOfNailedFaceAtBoundaries == e.w);
                Assert.AreEqual(e.p, r.MinimumFastenerPenetration, 1e-9, at);
                var w = new[] { r.Wind6_6, r.Wind4_6, r.Wind2_5_4, r.Wind2_3 };
                for (int i = 0; i < 4; i++) Assert.AreEqual(e.blocked[i], w[i].v_s, $"{at} column {i}");
                CollectionAssert.AreEqual(new[] { 6.0, 4, 2.5, 2 }, w.Select(x => x.NailSpacing_Boundary).ToArray(), at);
                CollectionAssert.AreEqual(new[] { 6.0, 6, 4, 3 }, w.Select(x => x.NailSpacing_OtherPanelEdges).ToArray(), at);
            }
        }

        [TestMethod]
        public void Table4_2C_WindColumn_AsPrinted()
        {
            var rows = new Table4_2CManager().Table4_2CNominalUnitShearCapacities.Values.ToList();
            Assert.AreEqual(Printed.Length, rows.Count, "every row of the printed table, no more");
            foreach (var e in Printed)
            {
                string at = $"{e.g} {e.n} {e.t} {e.w}\"";
                var r = rows.Single(x => x.SheathingGrade == e.g && x.CommonNailSize == e.n && x.NominalPanelThickness == e.t && x.NominalWidthOfNailedFaceAtBoundaries == e.w);
                Assert.AreEqual(e.p, r.MinimumFastenerPenetration, 1e-9, at);
                Assert.AreEqual(e.unblocked[0], r.Wind6_Case1.v_s, at + " Case 1");
                Assert.AreEqual(e.unblocked[1], r.Wind6_OtherCases.v_s, at + " Cases 2-6");
            }
        }
    }
}
