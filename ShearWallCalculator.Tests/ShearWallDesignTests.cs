using System;
using calculator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShearWallCalculator.Design;
using ShearWallCalculator.NDSProvisions;

namespace ShearWallCalculator.Tests
{
    /// <summary>
    /// Wood structural panel shear-wall checks for wind, by hand (SDPWS Table 4.3A, 4.3.3/4.1.4, 4.3.4; hold-down
    /// T = V h / d, d = L - post, at ASD against the Simpson allowables in this repository).
    /// </summary>
    [TestClass]
    public class ShearWallDesignTests
    {
        private static readonly ShearWallDesigner Designer = new ShearWallDesigner();

        private static WallData Wall(double length, double height = 9) => new WallData(height, 0, 0, length, 0);

        private static DesignSettings Asd => new DesignSettings { Method = DesignMethods.ASD, Edition = SdpwsEditions.SDPWS_2021 };
        private static DesignSettings Lrfd => new DesignSettings { Method = DesignMethods.LRFD, Edition = SdpwsEditions.SDPWS_2021 };

        [TestMethod]
        public void Asd_HandCalculation_15_32StructuralI_8dAt6()
        {
            // L = 10 ft, h = 9 ft, V = 5 k (strength). v = 0.6 x 5000 / 10 = 300 plf.
            // v_w = 785 plf (Table 4.3A); ASD 785 / 2.0 = 392.5 plf; ratio 0.764.
            // Hold-down arm d = L - post = 10 - 3/12 = 9.75 ft (HDU2: min. 3 x 3.5 member, 3 in along the wall).
            // T = 0.6 x 5000 x 9 / 9.75 = 2769.2 lb -> HDU2-SDS2.5 (3075 lb DF-SP), the lightest at or above it.
            var r = Designer.Check(1, Wall(10), 5.0, "Case 1", Asd);
            Assert.AreEqual(0.6, r.LoadFactor);
            Assert.AreEqual(300.0, r.Demand, 1e-9);
            Assert.AreEqual(785.0, r.Nominal);
            Assert.AreEqual(1.0, r.AspectFactor);
            Assert.AreEqual(392.5, r.Capacity, 1e-9);
            Assert.AreEqual(300.0 / 392.5, r.ShearRatio, 1e-9);
            Assert.AreEqual(3.0, r.PostAlongWall, 1e-12);
            Assert.AreEqual(9.75, r.HoldownArm, 1e-12);
            Assert.AreEqual(27000 / 9.75, r.TensionAsd, 1e-9);
            Assert.AreEqual(45000 / 9.75, r.TensionLrfd, 1e-9, "reported for reference: 1.0W");
            Assert.AreEqual("HDU2-SDS2.5", r.Holdown.Model);
            Assert.AreEqual(3075.0, r.Holdown.Allowable);
            Assert.IsTrue(r.Pass);
            Assert.AreEqual("OK", r.Status);
        }

        [TestMethod]
        public void Lrfd_UsesFullWindAndPhi0_8_HoldownStillAsd()
        {
            // v = 1.0 x 5000 / 10 = 500 plf; capacity 0.80 x 785 = 628 plf.
            var r = Designer.Check(1, Wall(10), 5.0, null, Lrfd);
            Assert.AreEqual(500.0, r.Demand, 1e-9);
            Assert.AreEqual(628.0, r.Capacity, 1e-9);
            Assert.AreEqual(27000 / 9.75, r.TensionAsd, 1e-9, "hold-downs are always checked at ASD (0.6W)");
            Assert.AreEqual("HDU2-SDS2.5", r.Holdown.Model);
        }

        [TestMethod]
        public void NarrowWall_AspectFactorAndLimit()
        {
            // h = 9, L = 3: h/b = 3.0 -> factor 1.25 - 0.375 = 0.875. L = 2.5: 3.6 > 3.5 not allowed.
            var r = Designer.Check(1, Wall(3), 1.0, null, Asd);
            Assert.AreEqual(3.0, r.AspectRatio, 1e-12);
            Assert.AreEqual(0.875, r.AspectFactor, 1e-12);
            Assert.AreEqual(785 * 0.875 / 2.0, r.Capacity, 1e-9);
            Assert.IsTrue(r.AspectOk);

            var tooNarrow = Designer.Check(1, Wall(2.5), 0.5, null, Asd);
            Assert.IsFalse(tooNarrow.AspectOk);
            Assert.IsFalse(tooNarrow.Pass);
            StringAssert.Contains(tooNarrow.Status, "not allowed");
        }

        [TestMethod]
        public void SpeciesBothSidesAndFootnote10()
        {
            var d = new WallDesignInputs { Species = WoodTypes.WOODTYPE_SPF_HF, Sides = 2, EdgeSpacing = 4 };
            var r = Designer.Check(1, Wall(10), 5.0, null, Asd, d);
            Assert.AreEqual(0.92, r.SpeciesFactor, 1e-12, "1 - (0.5 - 0.42)");
            Assert.AreEqual(1205 * 0.92 * 2 / 2.0, r.Capacity, 1e-9);

            var tenD = new WallDesignInputs { Nail = CommonNailSizes.NAILSIZE_10D, HoldownOnInsideFace = true };
            var r21 = Designer.Check(1, Wall(10), 5.0, null, Asd, tenD);
            Assert.AreEqual(950.0, r21.Nominal);
            Assert.AreEqual(950 * 0.92 / 2.0, r21.Capacity, 1e-9, "SDPWS 2021 footnote 10");
            var r15 = Designer.Check(1, Wall(10), 5.0, null, new DesignSettings { Edition = SdpwsEditions.SDPWS_2015 }, tenD);
            Assert.AreEqual(950 / 2.0, r15.Capacity, 1e-9, "not in SDPWS 2015");
        }

        [TestMethod]
        public void DeadLoad_ReducesHoldownTension()
        {
            // T = 0.6 x 5000 x 9 / d - 0.6 x (200 plf x 10 ft) / 2, d = 10 - post/12: dead load at L/2 is d/2 from the
            // compression post, so its share D d/2 / d = D/2 does not depend on the post.
            var d = new WallDesignInputs { DeadLoad = 200 };
            var r = Designer.Check(1, Wall(10), 5.0, null, Asd, d);
            Assert.AreEqual(27000 / r.HoldownArm - 600, r.TensionAsd, 1e-9);
            Assert.AreEqual(45000 / r.HoldownArm - 0.9 * 2000 / 2, r.TensionLrfd, 1e-9);
            Assert.AreEqual(WallSchedule.PostAlongWall(r.Holdown), r.PostAlongWall, 1e-12, "the arm uses the chosen hold-down's post");
            Assert.IsTrue(r.Holdown.Allowable >= r.TensionAsd);

            var heavy = Designer.Check(1, Wall(10), 1.0, null, Asd, new WallDesignInputs { DeadLoad = 2000 });
            Assert.AreEqual(0.0, heavy.TensionAsd, "no uplift");
            Assert.IsFalse(heavy.HoldownNeeded);
            Assert.IsNull(heavy.Holdown);
            Assert.IsTrue(heavy.HoldownOk);
        }

        [TestMethod]
        public void HoldownSelection_Boundaries()
        {
            Assert.AreEqual("DTT2Z", Designer.SelectHoldown(HoldownFamilies.HDU_ToFoundation, WoodTypes.WOODTYPE_DF_SP, 2145).Model);
            Assert.AreEqual("HDU2-SDS2.5", Designer.SelectHoldown(HoldownFamilies.HDU_ToFoundation, WoodTypes.WOODTYPE_DF_SP, 2146).Model);
            Assert.AreEqual("HDU14-SDS2.5", Designer.SelectHoldown(HoldownFamilies.HDU_ToFoundation, WoodTypes.WOODTYPE_DF_SP, 14400).Model);
            Assert.IsNull(Designer.SelectHoldown(HoldownFamilies.HDU_ToFoundation, WoodTypes.WOODTYPE_DF_SP, 20000), "beyond the catalog");
            Assert.IsNotNull(Designer.SelectHoldown(HoldownFamilies.HTT_ToFoundation, WoodTypes.WOODTYPE_DF_SP, 1000));
            Assert.IsNotNull(Designer.SelectHoldown(HoldownFamilies.Strap_ToWallBelow, WoodTypes.WOODTYPE_DF_SP, 500));

            var big = Designer.Check(1, Wall(4, 10), 10.0, null, Asd); // T = 0.6 x 10000 x 10 / 4 = 15000 lb
            Assert.IsNull(big.Holdown);
            Assert.IsFalse(big.Pass);
        }

        [TestMethod]
        public void AutoSelect_PicksTheLightestPassingNailing()
        {
            // 10 ft wall, V = 9 k: v = 540 plf. 6" -> 392.5 (no), 4" -> 602.5 (yes).
            var wall = Wall(10);
            var d = Designer.AutoSelect(wall, 9.0, Asd);
            Assert.AreEqual(4, d.EdgeSpacing);
            Assert.AreEqual(1, d.Sides);

            // V = 20 k: v = 1200 plf; one side at 2" = 1022.5 (no); both sides at 4" = 1205 (yes).
            var both = Designer.AutoSelect(wall, 20.0, Asd);
            Assert.AreEqual(2, both.Sides);
            Assert.AreEqual(4, both.EdgeSpacing);
        }

        [TestMethod]
        public void Panels_AreTheWoodStructuralPanelRows()
        {
            Assert.IsTrue(Designer.Panels.Count >= 8);
            foreach (var p in Designer.Panels)
                Assert.IsTrue(p.Sheathing == SheathingMaterials.SHEATH_MAT_STRUCTURAL_1 || p.Sheathing == SheathingMaterials.SHEATH_MAT_SHEATHING_AND_SINGLE_FLOOR);
            Assert.IsNotNull(Designer.FindPanel(new WallDesignInputs()), "the default panel is in the table");
        }

        [TestMethod]
        public void OldSelector_NowUsesTension()
        {
            // 10 ft wall, 9 ft high, V = 2 k: T = 1800 lb. With V (2 lb) every strap passed; now only those >= 1800.
            var sel = new ShearWallSelector(2.0, Wall(10), new SimpsonCatalog(), ConnectorTypes.CONNECTOR_HDU);
            Assert.AreEqual(1.8, sel.ComputeHolddownForces(), 1e-12);
            Assert.IsTrue(sel.selectedConnectors.Count > 0);
            Assert.IsFalse(sel.selectedConnectors.Exists(c => c.Model == "DTT1Z"), "DTT1Z (910 lb max) is too weak for 1800 lb");
        }

        [TestMethod]
        public void DesignChoices_SaveWithTheWall()
        {
            var calc = new ShearWallCalculator_RigidDiaphragm();
            var w = Wall(10);
            w.Design.EdgeSpacing = 3;
            w.Design.Holdown = HoldownFamilies.Strap_ToWallBelow;
            calc._wall_system.AddWall(w);
            calc.DesignSettings.Method = DesignMethods.LRFD;
            var read = TestProject.RoundTrip(calc);
            foreach (var wall in read._wall_system._walls.Values)
            {
                Assert.AreEqual(3, wall.Design.EdgeSpacing);
                Assert.AreEqual(HoldownFamilies.Strap_ToWallBelow, wall.Design.Holdown);
            }
            Assert.AreEqual(DesignMethods.LRFD, read.DesignSettings.Method);
        }

        [TestMethod]
        public void FlexibleDiaphragm_TributaryShearsDesignLikeRigid()
        {
            // 40 x 20 ft flexible diaphragm, 10 ft walls on all four sides, Vx = 10 k, Vy = 6 k.
            // Tributary: each E-W wall line takes 10 / 2 = 5 k, each N-S line 6 / 2 = 3 k (one wall per line).
            // E-W wall: v = 0.6 x 5000 / 10 = 300 plf vs 392.5 plf; T = 0.6 x 5000 x 9 / 10 = 2700 lb -> HDU2-SDS2.5.
            // N-S wall: v = 0.6 x 3000 / 10 = 180 plf; T = 0.6 x 3000 x 9 / 10 = 1620 lb.
            var calc = new ShearWallCalculator_FlexibleDiaphragm();
            calc._wall_system.AddWall(new WallData(9, 0, 5, 0, 15));
            calc._wall_system.AddWall(new WallData(9, 40, 5, 40, 15));
            calc._wall_system.AddWall(new WallData(9, 15, 0, 25, 0));
            calc._wall_system.AddWall(new WallData(9, 15, 20, 25, 20));
            calc._diaphragm_system.AddDiaphragm(new DiaphragmData_Rectangular(new System.Windows.Point(0, 0), new System.Windows.Point(40, 20)));
            calc.AddLoads(10, 6);
            calc.PerformCalculations();

            foreach (var id in calc._wall_system.EW_Walls.Keys)
            {
                var r = Designer.Check(id, calc._wall_system._walls[id], calc.TotalWallShear[id], null, Asd);
                Assert.AreEqual(300.0, r.Demand, 1e-6);
                Assert.AreEqual(392.5, r.Capacity, 1e-9);
                Assert.AreEqual(27000 / 9.75, r.TensionAsd, 1e-6, "d = 10 - 3/12 ft");
                Assert.AreEqual("HDU2-SDS2.5", r.Holdown.Model);
                Assert.IsTrue(r.Pass);
            }
            foreach (var id in calc._wall_system.NS_Walls.Keys)
            {
                var r = Designer.Check(id, calc._wall_system._walls[id], calc.TotalWallShear[id], null, Asd);
                Assert.AreEqual(180.0, r.Demand, 1e-6);
                Assert.AreEqual(1800.0 * 9 / r.HoldownArm, r.TensionAsd, 1e-6, "0.6V = 180 plf x 10 ft; d = L - post");
                Assert.AreEqual(10 - r.PostAlongWall / 12, r.HoldownArm, 1e-12);
                Assert.IsTrue(r.Holdown.Allowable >= r.TensionAsd);
                Assert.IsTrue(r.Pass);
            }
        }

        /// <summary>
        /// The hold-down catalog screen lists exactly what the design selects from: every option the selector can choose
        /// (each family, each species) appears with the same model, fasteners and allowable tension, and nothing extra.
        /// </summary>
        [TestMethod]
        public void HoldownCatalogRows_AreTheValuesTheDesignUses()
        {
            var d = new ShearWallDesigner();
            var rows = d.CatalogRows();
            Assert.IsTrue(rows.Count > 0);
            foreach (HoldownFamilies family in Enum.GetValues(typeof(HoldownFamilies)))
                foreach (var species in new[] { WoodTypes.WOODTYPE_DF_SP, WoodTypes.WOODTYPE_SPF_HF })
                {
                    var options = System.Linq.Enumerable.ToList(d.Options(family, species));
                    var shown = System.Linq.Enumerable.ToList(System.Linq.Enumerable.Where(rows, r => r.Family == family &&
                        (species == WoodTypes.WOODTYPE_SPF_HF ? r.AllowableSPF_HF : r.AllowableDF_SP).HasValue));
                    Assert.AreEqual(options.Count, shown.Count, $"{family} {species}");
                    for (int i = 0; i < options.Count; i++)
                    {
                        Assert.AreEqual(options[i].Model, shown[i].Model);
                        Assert.AreEqual(options[i].Fasteners, shown[i].Fasteners);
                        Assert.AreEqual(options[i].Allowable, (species == WoodTypes.WOODTYPE_SPF_HF ? shown[i].AllowableSPF_HF : shown[i].AllowableDF_SP).Value, 0.0);
                    }
                }

            // stored values come through verbatim: DTT1Z, 6 #9 SD screws, 840 lb DF/SP and SPF/HF, 0.17 in.
            var dtt = System.Linq.Enumerable.First(rows, r => r.Model == "DTT1Z");
            Assert.IsTrue(dtt.FirstOfModel);
            Assert.AreEqual(14, dtt.Gauge);
            Assert.AreEqual(7.125, dtt.HeightOrLength, 0.0);
            Assert.AreEqual("6 #9 SD", dtt.Fasteners);
            Assert.AreEqual(840, dtt.AllowableDF_SP.Value, 0.0);
            Assert.AreEqual(840, dtt.AllowableSPF_HF.Value, 0.0);
            Assert.AreEqual(0.17, dtt.DeflectionAtAllowable.Value, 0.0);
        }

        /// <summary>
        /// Company standard list: with the toggle on, the lightest adequate option from the list only; none strong enough
        /// means no hold-down (and the status says it was the standard list), never a silent fall-back to the full catalog.
        /// </summary>
        [TestMethod]
        public void StandardList_LimitsTheSelection_AndSaysWhenNoneFits()
        {
            var d = new ShearWallDesigner();
            // full catalog: 2146 lb -> HDU2-SDS2.5 (3075 lb DF/SP)
            Assert.AreEqual("HDU2-SDS2.5", d.SelectHoldown(HoldownFamilies.HDU_ToFoundation, WoodTypes.WOODTYPE_DF_SP, 2146).Model);

            d.StandardList.Set(ShearWallDesigner.OptionKey("HDU5-SDS2.5", 0), true);
            d.StandardList.Set(ShearWallDesigner.OptionKey("HDU8-SDS2.5", 0), true);
            var c = d.SelectHoldown(HoldownFamilies.HDU_ToFoundation, WoodTypes.WOODTYPE_DF_SP, 2146, standardOnly: true);
            Assert.AreEqual("HDU5-SDS2.5", c.Model);
            Assert.AreEqual("HDU5-SDS2.5#1", c.Key);
            Assert.AreEqual(5645, c.Allowable, 0.0);
            Assert.AreEqual("HDU8-SDS2.5", d.SelectHoldown(HoldownFamilies.HDU_ToFoundation, WoodTypes.WOODTYPE_DF_SP, 6000, true).Model);
            Assert.IsNull(d.SelectHoldown(HoldownFamilies.HDU_ToFoundation, WoodTypes.WOODTYPE_DF_SP, 7000, true), "HDU8 #1 is 6765 lb");
            Assert.IsNotNull(d.SelectHoldown(HoldownFamilies.HDU_ToFoundation, WoodTypes.WOODTYPE_DF_SP, 7000, false));

            var settings = new DesignSettings { Method = DesignMethods.ASD, HoldownsFromStandardList = true };
            var small = new ShearWallDesigner();
            small.StandardList.Set(ShearWallDesigner.OptionKey("HDU2-SDS2.5", 0), true);
            var r = small.Check(1, Wall(4, 10), 2.2, null, settings);   // v = 0.6 x 2200 / 4 = 330 plf; T = 0.6 x 2200 x 10 / 4 = 3300 lb > HDU2 3075 lb
            Assert.IsNull(r.Holdown);
            Assert.IsTrue(r.HoldownFromStandardList);
            Assert.AreEqual("no standard-list hold-down strong enough", r.Status);
        }

        [TestMethod]
        public void StandardList_KeysAreStable_TextNamesThePostSize_AndUnknownKeysAreReported()
        {
            var d = new ShearWallDesigner();
            var rows = d.CatalogRows();
            Assert.AreEqual(rows.Count, System.Linq.Enumerable.Count(System.Linq.Enumerable.Distinct(System.Linq.Enumerable.Select(rows, r => r.Key))), "keys are unique");
            // HDU14-SDS2.5 options 1 and 3 share fasteners and minimum wood text but are different options
            var hdu14 = System.Linq.Enumerable.ToList(System.Linq.Enumerable.Where(rows, r => r.Model == "HDU14-SDS2.5"));
            CollectionAssert.AreEqual(new[] { "HDU14-SDS2.5#1", "HDU14-SDS2.5#2", "HDU14-SDS2.5#3" }, System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(hdu14, r => r.Key)));
            var choice = d.SelectHoldown(HoldownFamilies.HDU_ToFoundation, WoodTypes.WOODTYPE_DF_SP, 14400);
            Assert.AreEqual("HDU14-SDS2.5 (36 1/4 SDS, min. 3.5 × 5.5)", choice.ToString());

            d.StandardList.Set("NOT-A-MODEL#1", true);
            CollectionAssert.AreEqual(new[] { "NOT-A-MODEL#1" }, d.StandardKeysNotInCatalog());
        }

        [TestMethod]
        public void StandardList_SavesAndLoads_AndRaisesChanged()
        {
            string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "swc-test-" + Guid.NewGuid().ToString("N"), "HoldownStandardList.json");
            try
            {
                Assert.AreEqual(0, HoldownStandardList.Load(path).Count, "a missing file is an empty list");
                var list = new HoldownStandardList();
                int changed = 0;
                list.Changed += (s, e) => changed++;
                list.Set("HDU5-SDS2.5#1", true);
                list.Set("HDU5-SDS2.5#1", true);        // already there: no change
                list.Set("LTTP2#2", true);
                Assert.AreEqual(2, changed);
                list.Save(path);
                var back = HoldownStandardList.Load(path);
                CollectionAssert.AreEquivalent(new[] { "HDU5-SDS2.5#1", "LTTP2#2" }, System.Linq.Enumerable.ToArray(back.Keys));
            }
            finally
            {
                var dir = System.IO.Path.GetDirectoryName(path);
                if (System.IO.Directory.Exists(dir)) System.IO.Directory.Delete(dir, true);
            }
        }

        [TestMethod]
        public void StandardListToggle_SavesWithTheProject()
        {
            var calc = new ShearWallCalculator_RigidDiaphragm();
            calc.DesignSettings.HoldownsFromStandardList = true;
            var read = TestProject.RoundTrip(calc);
            Assert.IsTrue(read.DesignSettings.HoldownsFromStandardList);
        }
    }
}
