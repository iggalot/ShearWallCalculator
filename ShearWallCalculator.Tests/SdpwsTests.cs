using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShearWallCalculator.NDSProvisions;

namespace ShearWallCalculator.Tests
{
    /// <summary>SDPWS table source and the factors applied to the tabulated nominal values (docs/SDPWS_EDITION.md).</summary>
    [TestClass]
    public class SdpwsTests
    {
        private static Table4_3A Row(SheathingMaterials material, CommonNailSizes nail, NominalPanelThicknesses t) =>
            new Table4_3Manager().Table4_3ANominalUnitShearCapacities.Values
                .Single(r => r.SheathingType == material && r.CommonNailSize == nail && r.NominalPanelThickness == t);

        [TestMethod]
        public void Table4_3A_WindColumn_Is1_4TimesSeismic_The2015Layout()
        {
            foreach (var r in new Table4_3Manager().Table4_3ANominalUnitShearCapacities.Values)
            {
                var pairs = new[] { (r.Seismic6, r.Wind6), (r.Seismic4, r.Wind4), (r.Seismic3, r.Wind3), (r.Seismic2, r.Wind2) };
                foreach (var (s, w) in pairs.Where(p => p.Item1 != null && p.Item2 != null && p.Item1.v_s > 0))
                    Assert.AreEqual(1.4 * s.v_s, w.v_s, 0.011 * w.v_s, $"{r.SheathingType} {r.CommonNailSize} {r.NominalPanelThickness}");
            }
        }

        [TestMethod]
        public void Table4_3A_SheathingGrade15_32_8d_At2in_Matches2021Example()
        {
            // WoodWorks SDPWS-2021 example: 15/32" WSP, 8d common @ 2" o.c., v_n = 1790 plf.
            var r = Row(SheathingMaterials.SHEATH_MAT_SHEATHING_AND_SINGLE_FLOOR, CommonNailSizes.NAILSIZE_8D, NominalPanelThicknesses.THICKNESS_15_32IN);
            Assert.AreEqual(1790, r.Wind2.v_s);
            Assert.AreEqual(1280, r.Seismic2.v_s);
        }

        [TestMethod]
        public void DesignUnitShear_AsdDividesBy2_LrfdTimes0_8()
        {
            Assert.AreEqual(895, SdpwsSource.DesignUnitShearWind(1790, DesignMethods.ASD), 1e-9);
            Assert.AreEqual(1432, SdpwsSource.DesignUnitShearWind(1790, DesignMethods.LRFD), 1e-9);
        }

        [TestMethod]
        public void AspectRatioFactor_OneUpTo2to1_Then1_25Minus0_125hb()
        {
            Assert.AreEqual(1.0, SdpwsSource.AspectRatioFactor(8, 4), 1e-12);      // 2:1
            Assert.AreEqual(0.875, SdpwsSource.AspectRatioFactor(9, 3), 1e-12);    // 3:1
            Assert.AreEqual(0.8125, SdpwsSource.AspectRatioFactor(7, 2), 1e-12);   // 3.5:1 -> 0.81
            Assert.AreEqual(2.0 / 3.0, SdpwsSource.CompatibilityFactor(9, 3), 1e-12);
            Assert.Throws<ArgumentException>(() => SdpwsSource.AspectRatioFactor(9, 0));
        }

        [TestMethod]
        public void Footnote10_OnlyIn2021With10dAndInsideFaceHoldown()
        {
            Assert.AreEqual(0.92, SdpwsSource.InsideFaceHoldownFactor(SdpwsEditions.SDPWS_2021, true, true));
            Assert.AreEqual(1.0, SdpwsSource.InsideFaceHoldownFactor(SdpwsEditions.SDPWS_2015, true, true));
            Assert.AreEqual(1.0, SdpwsSource.InsideFaceHoldownFactor(SdpwsEditions.SDPWS_2021, false, true));
            Assert.AreEqual(SdpwsEditions.SDPWS_2021, SdpwsSource.DefaultFor(asce7_22: true));
            Assert.AreEqual(SdpwsEditions.SDPWS_2015, SdpwsSource.DefaultFor(asce7_22: false));
        }
    }
}
