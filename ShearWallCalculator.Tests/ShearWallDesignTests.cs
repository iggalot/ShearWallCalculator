using System;
using calculator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShearWallCalculator.Design;
using ShearWallCalculator.NDSProvisions;

namespace ShearWallCalculator.Tests
{
    /// <summary>
    /// Wood structural panel shear-wall checks for wind, by hand (SDPWS Table 4.3A, 4.3.3/4.1.4, 4.3.4; hold-down
    /// T = V h / L at ASD against the Simpson allowables in this repository).
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
            // T = 0.6 x 5000 x 9 / 10 = 2700 lb -> HDU2-SDS2.5 (3075 lb DF-SP), the lightest at or above 2700.
            var r = Designer.Check(1, Wall(10), 5.0, "Case 1", Asd);
            Assert.AreEqual(0.6, r.LoadFactor);
            Assert.AreEqual(300.0, r.Demand, 1e-9);
            Assert.AreEqual(785.0, r.Nominal);
            Assert.AreEqual(1.0, r.AspectFactor);
            Assert.AreEqual(392.5, r.Capacity, 1e-9);
            Assert.AreEqual(300.0 / 392.5, r.ShearRatio, 1e-9);
            Assert.AreEqual(2700.0, r.TensionAsd, 1e-9);
            Assert.AreEqual(4500.0, r.TensionLrfd, 1e-9, "reported for reference: 1.0W");
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
            Assert.AreEqual(2700.0, r.TensionAsd, 1e-9, "hold-downs are always checked at ASD (0.6W)");
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
            // T = 0.6 x 5000 x 9 / 10 - 0.6 x (200 plf x 10 ft) / 2 = 2700 - 600 = 2100 lb -> DTT2Z (2145).
            var d = new WallDesignInputs { DeadLoad = 200 };
            var r = Designer.Check(1, Wall(10), 5.0, null, Asd, d);
            Assert.AreEqual(2100.0, r.TensionAsd, 1e-9);
            Assert.AreEqual(4500 - 0.9 * 2000 / 2, r.TensionLrfd, 1e-9);
            Assert.AreEqual("DTT2Z", r.Holdown.Model);

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
            var read = Interfaces.ProjectSerializer.Read(Interfaces.ProjectSerializer.ToJson(calc, null, false)).Calculator;
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
                Assert.AreEqual(2700.0, r.TensionAsd, 1e-6);
                Assert.AreEqual("HDU2-SDS2.5", r.Holdown.Model);
                Assert.IsTrue(r.Pass);
            }
            foreach (var id in calc._wall_system.NS_Walls.Keys)
            {
                var r = Designer.Check(id, calc._wall_system._walls[id], calc.TotalWallShear[id], null, Asd);
                Assert.AreEqual(180.0, r.Demand, 1e-6);
                Assert.AreEqual(1620.0, r.TensionAsd, 1e-6);
                Assert.IsTrue(r.Holdown.Allowable >= 1620.0);
                Assert.IsTrue(r.Pass);
            }
        }
    }
}
