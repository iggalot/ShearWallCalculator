using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShearWallCalculator.Design;

namespace ShearWallCalculator.Tests
{
    /// <summary>
    /// The hold-down catalog against its source, Simpson Strong-Tie Wood Construction Connectors C-C-2021: the LSTHD / STHD
    /// table (p. 59) in full, and the values corrected when the HDU/DTT (p. 53), LTT/HTT (p. 55) and strap (p. 269) tables
    /// were checked. The expected numbers are typed here from the catalog, not taken from the code.
    /// </summary>
    [TestClass]
    public class SimpsonCatalogTests
    {
        // C-C-2021 p. 59, "Tension Loads for STHD Installations": stemwall, model, nails, uncracked mid/corner/end, cracked mid/corner/end.
        private static readonly (SthdSeismic row, double stem, string model, int nails, double[] unc, double[] cr)[] Table =
        {
            (SthdSeismic.WindAndSdcAB, 6, "LSTHD8", 20, new double[] { 2985, 2590, 1620 }, new double[] { 2565, 2225, 1395 }),
            (SthdSeismic.WindAndSdcAB, 6, "STHD10", 24, new double[] { 3535, 3535, 1960 }, new double[] { 2910, 2910, 1635 }),
            (SthdSeismic.WindAndSdcAB, 6, "STHD14", 30, new double[] { 4935, 4935, 3065 }, new double[] { 4935, 4935, 3065 }),
            (SthdSeismic.WindAndSdcAB, 8, "LSTHD8", 20, new double[] { 2985, 2590, 2135 }, new double[] { 2565, 2225, 1835 }),
            (SthdSeismic.WindAndSdcAB, 8, "STHD10", 28, new double[] { 4755, 4075, 3015 }, new double[] { 4020, 3350, 2480 }),
            (SthdSeismic.WindAndSdcAB, 8, "STHD14", 30, new double[] { 5285, 5285, 4410 }, new double[] { 5285, 5285, 4410 }),
            (SthdSeismic.SdcCtoF, 6, "LSTHD8", 16, new double[] { 2270, 2090, 1220 }, new double[] { 2250, 1950, 1220 }),
            (SthdSeismic.SdcCtoF, 6, "STHD10", 18, new double[] { 2750, 2750, 1615 }, new double[] { 2550, 2550, 1435 }),
            (SthdSeismic.SdcCtoF, 6, "STHD14", 22, new double[] { 3695, 3695, 2685 }, new double[] { 3695, 3695, 2685 }),
            (SthdSeismic.SdcCtoF, 8, "LSTHD8", 16, new double[] { 2615, 2125, 1635 }, new double[] { 2250, 1950, 1610 }),
            (SthdSeismic.SdcCtoF, 8, "STHD10", 20, new double[] { 3400, 2940, 2295 }, new double[] { 3400, 2940, 2175 }),
            (SthdSeismic.SdcCtoF, 8, "STHD14", 24, new double[] { 3815, 3815, 3500 }, new double[] { 3815, 3815, 3500 }),
        };

        [TestMethod]
        public void Sthd_EveryTableValue_ForEveryCondition_StandardAndRimJoist()
        {
            var d = new ShearWallDesigner();
            int checkedValues = 0;
            foreach (SthdSeismic seismic in new[] { SthdSeismic.WindAndSdcAB, SthdSeismic.SdcCtoF })
                foreach (bool cracked in new[] { false, true })
                    foreach (SthdPositions pos in new[] { SthdPositions.Midwall, SthdPositions.Corner, SthdPositions.Endwall })
                    {
                        var c = new StrapHoldownConditions { Seismic = seismic, CrackedConcrete = cracked, Position = pos, Stemwall = 8 };
                        var rows = d.CatalogRows(c).Where(r => r.Family == HoldownFamilies.STHD_ToFoundation).ToList();
                        Assert.AreEqual(12, rows.Count, "6 models (3 standard, 3 RJ) x 2 stemwalls");
                        foreach (var t in Table.Where(t => t.row == seismic))
                            foreach (string model in new[] { t.model, t.model + "RJ" })
                            {
                                var r = rows.Single(x => x.Model == model && x.MinStemwall == t.stem);
                                double expected = (cracked ? t.cr : t.unc)[(int)pos];
                                Assert.AreEqual(expected, r.AllowableDF_SP.Value, 0.0, $"{model} {t.stem}\" {seismic} {(cracked ? "cracked" : "uncracked")} {pos}");
                                Assert.AreEqual(expected, r.AllowableSPF_HF.Value, 0.0, "the same for every species");
                                Assert.AreEqual($"{t.nails} 0.148", r.Fasteners);
                                Assert.AreEqual(3.25, r.FastenerLength, 0.0);
                                Assert.AreEqual(t.stem == 6 ? model + "#1" : model + "#2", r.Key);
                                checkedValues++;
                            }
                    }
            Assert.AreEqual(2 * 2 * 3 * 6 * 2, checkedValues);
        }

        [TestMethod]
        public void Sthd_StrapData_Gauge_Length_Embedment_Deflection()
        {
            var m = new STHD_Manager().STHD_Dict;
            // C-C-2021 p. 58 "Material", p. 59 strap length / le columns, footnote 5 deflections (on framing, over sheathing)
            var expected = new (string model, int ga, double L, double le, double onFraming, double overSheathing)[]
            {
                ("LSTHD8", 14, 18.625, 8, 0.089, 0.114), ("LSTHD8RJ", 14, 32.125, 8, 0.089, 0.114),
                ("STHD10", 12, 24.625, 10, 0.117, 0.146), ("STHD10RJ", 12, 38.125, 10, 0.117, 0.146),
                ("STHD14", 12, 26.125, 14, 0.118, 0.164), ("STHD14RJ", 12, 39.625, 14, 0.118, 0.164),
            };
            Assert.AreEqual(expected.Length, m.Count);
            foreach (var e in expected)
            {
                var s = m[e.model];
                Assert.AreEqual(e.ga, s.Gauge, e.model);
                Assert.AreEqual(e.L, s.L, 0.0, e.model);
                Assert.AreEqual(e.le, s.Embedment, 0.0, e.model);
                Assert.AreEqual(e.onFraming, s.DeflectionOnFraming, 0.0, e.model);
                Assert.AreEqual(e.overSheathing, s.DeflectionOverSheathing, 0.0, e.model);
                Assert.AreEqual(e.model.EndsWith("RJ"), s.RimJoist);
            }
        }

        [TestMethod]
        public void Sthd_Selection_UsesTheProjectConditions_AndTheStemwall()
        {
            var d = new ShearWallDesigner();
            var df = WoodTypes.WOODTYPE_DF_SP;
            // defaults: 6" stemwall, cracked, endwall, wind -> LSTHD8 1395, STHD10 1635, STHD14 3065
            Assert.AreEqual("LSTHD8#1", d.SelectHoldown(HoldownFamilies.STHD_ToFoundation, df, 1395).Key);
            Assert.AreEqual("STHD10#1", d.SelectHoldown(HoldownFamilies.STHD_ToFoundation, df, 1396).Key);
            Assert.AreEqual("STHD14#1", d.SelectHoldown(HoldownFamilies.STHD_ToFoundation, df, 3000).Key);
            Assert.IsNull(d.SelectHoldown(HoldownFamilies.STHD_ToFoundation, df, 3066), "an 8\" option needs an 8\" stemwall");

            var eight = new StrapHoldownConditions { Stemwall = 8 };
            Assert.AreEqual("STHD14#2", d.SelectHoldown(HoldownFamilies.STHD_ToFoundation, df, 3066, sthd: eight).Key);
            Assert.AreEqual(4410, d.SelectHoldown(HoldownFamilies.STHD_ToFoundation, df, 3066, sthd: eight).Allowable, 0.0);

            var mid = new StrapHoldownConditions { Stemwall = 8, CrackedConcrete = false, Position = SthdPositions.Midwall };
            var c = d.SelectHoldown(HoldownFamilies.STHD_ToFoundation, df, 4700, sthd: mid);
            Assert.AreEqual("STHD10#2", c.Key, "uncracked midwall, 8\": STHD10 4,755 lb");
            Assert.AreEqual("28 0.148", c.Fasteners);
            Assert.AreEqual(3.0, c.WoodMinHeight.Value, 0.0, "double 2x studs");
            StringAssert.Contains(c.Conditions, "uncracked concrete, midwall");

            // a standard-list STHD: RJ models are chosen only from the list (the standard strap wins a tie otherwise)
            Assert.AreEqual("STHD10#1", d.SelectHoldown(HoldownFamilies.STHD_ToFoundation, df, 1500).Key);
            d.StandardList.Set("STHD10RJ#1", true);
            Assert.AreEqual("STHD10RJ#1", d.SelectHoldown(HoldownFamilies.STHD_ToFoundation, df, 1500, standardOnly: true).Key);
            Assert.AreEqual(0, d.StandardKeysNotInCatalog().Count);
        }

        [TestMethod]
        public void Sthd_TheWallCheck_ReadsTheConditionsFromTheDesignSettings()
        {
            var d = new ShearWallDesigner();
            var wall = new calculator.WallData(8, 0, 0, 4, 0); // 8 ft high, 4 ft long
            var inputs = new WallDesignInputs { Holdown = HoldownFamilies.STHD_ToFoundation };
            // T = 0.6 x 2.2 k x 8 ft / (4 - 0.25) ft ~ 2816 lb (8 ft wall height, 3" post)
            var defaults = d.Check(1, wall, 2.2, null, new DesignSettings(), inputs);
            Assert.AreEqual("STHD14#1", defaults.Holdown.Key, "6\" cracked endwall: STHD10 1,635 is too small");
            var mid = d.Check(1, wall, 2.2, null, new DesignSettings { StrapHoldowns = new StrapHoldownConditions { Position = SthdPositions.Midwall } }, inputs);
            Assert.AreEqual("STHD10#1", mid.Holdown.Key, "6\" cracked midwall: STHD10 2,910");
        }

        [TestMethod]
        public void CheckedAgainstCC2021_TheCorrectedValues()
        {
            var cat = new SimpsonCatalog();
            // p. 269: MSTI36 is 36" long (was 26); MSTC52 SPF/HF 4,735 lb (was 4,315, the MSTC40 value); LSTI listed IBC, FL, LA
            Assert.AreEqual(36, cat.StrapTiesManager.StrapTies_Dict["MSTI36"].L, 0.0);
            Assert.AreEqual(4735, cat.StrapTiesManager.StrapTies_Dict["MSTC52"].FastenerOptions[0].AllowableTensionLoads["SPF_HF"], 0.0);
            Assert.AreEqual(4315, cat.StrapTiesManager.StrapTies_Dict["MSTC40"].FastenerOptions[0].AllowableTensionLoads["SPF_HF"], 0.0);
            Assert.AreEqual("IBC / FL / LA", cat.StrapTiesManager.StrapTies_Dict["LSTI49"].CodeRef);
            Assert.AreEqual("IBC / FL / LA", cat.StrapTiesManager.StrapTies_Dict["LSTI73"].CodeRef);
            // p. 53: DTT1Z minimum wood member 1-1/2 x 5-1/2 for each fastener option
            Assert.IsTrue(cat.HDUManager.HDU_Dict["DTT1Z"].FastenerOptions.All(o => o.WoodMinHeight == 1.5 && o.WoodMinWidth == 5.5));
            // p. 55: LTTP2 deflections 0.104, 0.112, 0.136, 0.128 in. (were ten times smaller)
            CollectionAssert.AreEqual(new[] { 0.104, 0.112, 0.136, 0.128 },
                cat.HTTManager.HTT_Dict["LTTP2"].FastenerOptions.Select(o => o.DeflectionAtAllowableLoad).ToArray());
        }

        [TestMethod]
        public void StrapTies_AllLoads_MatchCC2021Page269()
        {
            // C-C-2021 p. 269: model, nails, DF/SP, SPF/HF
            var expected = new Dictionary<string, (int n, double df, double spf)>
            {
                ["ST2115"] = (10, 660, 660), ["LSTA9"] = (8, 740, 635), ["LSTA12"] = (10, 925, 795), ["LSTA15"] = (12, 1110, 955),
                ["LSTA18"] = (14, 1235, 1115), ["LSTA21"] = (16, 1235, 1235), ["LSTA24"] = (18, 1235, 1235), ["LSTA30"] = (22, 1640, 1640),
                ["LSTA36"] = (24, 1640, 1640), ["MSTA9"] = (8, 750, 650), ["MSTA12"] = (10, 940, 810), ["MSTA15"] = (12, 1130, 970),
                ["MSTA18"] = (14, 1315, 1135), ["MSTA21"] = (16, 1505, 1295), ["MSTA24"] = (18, 1640, 1460), ["MSTA30"] = (22, 2050, 1825),
                ["MSTA36"] = (26, 2050, 2050), ["MSTA49"] = (26, 2020, 2020), ["ST9"] = (8, 885, 765), ["ST12"] = (10, 1105, 955),
                ["ST18"] = (14, 1420, 1335), ["ST22"] = (18, 1420, 1420), ["HRS6"] = (6, 605, 530), ["HRS8"] = (10, 1010, 880),
                ["HRS12"] = (14, 1415, 1230), ["ST292"] = (12, 1260, 1120), ["ST2122"] = (16, 1530, 1510), ["ST2215"] = (20, 1875, 1875),
                ["ST6215"] = (20, 2090, 1910), ["ST6224"] = (28, 2535, 2535), ["ST6236"] = (40, 3845, 3845), ["MSTI26"] = (26, 2745, 2380),
                ["MSTI36"] = (36, 3800, 3295), ["MSTI48"] = (48, 5070, 4390), ["MSTI60"] = (60, 5070, 5070), ["MSTI72"] = (72, 5070, 5070),
                ["HTP37Z"] = (20, 900, 690), ["MSTC28"] = (36, 3460, 2990), ["MSTC40"] = (52, 4735, 4315), ["MSTC52"] = (62, 4735, 4735),
                ["MSTC66"] = (76, 5850, 5850), ["MSTC78"] = (76, 5850, 5850), ["HRS416Z"] = (16, 2835, 2305), ["LSTI49"] = (32, 2970, 2560),
                ["LSTI73"] = (48, 4205, 3840),
            };
            var dict = new StrapTies_Manager().StrapTies_Dict;
            CollectionAssert.AreEquivalent(expected.Keys.ToList(), dict.Keys.ToList());
            foreach (var kv in expected)
            {
                var o = dict[kv.Key].FastenerOptions.Single();
                Assert.AreEqual(kv.Value.n, o.Screws_Quantity, kv.Key);
                Assert.AreEqual(kv.Value.df, o.AllowableTensionLoads["DF_SP"], 0.0, kv.Key);
                Assert.AreEqual(kv.Value.spf, o.AllowableTensionLoads["SPF_HF"], 0.0, kv.Key);
            }
        }
    }
}
