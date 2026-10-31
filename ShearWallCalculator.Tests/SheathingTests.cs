using System.Linq;
using calculator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShearWallCalculator.Design;
using ShearWallCalculator.NDSProvisions;

namespace ShearWallCalculator.Tests
{
    /// <summary>
    /// Sheathing on one or two faces from SDPWS Tables 4.3A, 4.3B and 4.3C (2015 wind columns), by hand:
    /// ASD capacity = nominal / 2.0. Two faces: same construction 2 × one face (2021 4.3.5.4.1); different materials the
    /// larger of the stronger face and 2 × the weaker (4.3.5.4.2); for wind, wood panel + gypsum wallboard added when the
    /// project allows it. A face over its Table 4.3.4 h/b limit does not count.
    /// </summary>
    [TestClass]
    public class SheathingTests
    {
        private const double Tol = 1e-6;
        private static readonly ShearWallDesigner Designer = new ShearWallDesigner();

        private static FaceSheathing Wsp(SheathingMaterials m, NominalPanelThicknesses t, CommonNailSizes n, int spacing) =>
            new FaceSheathing { Table = SheathingTable.WoodPanels, Material = m, Thickness = t, Nail = n, EdgeSpacing = spacing };

        private static readonly FaceSheathing SI_15_32_8d_6 = Wsp(SheathingMaterials.SHEATH_MAT_STRUCTURAL_1, NominalPanelThicknesses.THICKNESS_15_32IN, CommonNailSizes.NAILSIZE_8D, 6); // 785
        private static readonly FaceSheathing Sht_7_16_8d_6 = Wsp(SheathingMaterials.SHEATH_MAT_SHEATHING_AND_SINGLE_FLOOR, NominalPanelThicknesses.THICKNESS_7_16IN, CommonNailSizes.NAILSIZE_8D, 6); // 670

        private static FaceSheathing Gyp(int row)
        {
            var o = Designer.SheathingOptions.Single(x => x.Table == SheathingTable.GypsumAndPlaster && x.Row == row);
            return o.ToFace();
        }

        private static WallDesignResult Check(double h, double L, FaceSheathing f1, FaceSheathing f2, bool sum = true, WoodTypes species = WoodTypes.WOODTYPE_DF_SP)
        {
            var d = new WallDesignInputs { Species = species, HoldownOnInsideFace = false };
            d.SetFaces(f1, f2);
            var wall = new WallData(h, 0, 0, L, 0);
            var settings = new DesignSettings { Method = DesignMethods.ASD, Edition = SdpwsEditions.SDPWS_2021, SumWindFaces = sum };
            return Designer.Check(0, wall, 1.0, "test", settings, d);
        }

        [TestMethod]
        public void Table4_3C_RowsUsed_HaveTheExpectedValues()
        {
            // 1/2" gypsum wallboard: row 1 7"/24" unblocked 150; row 4 7"/16" unblocked 200; 5/8" row 16 4"/16" blocked 350; cement plaster row 26 360
            Assert.AreEqual(150, Designer.FindOption(Gyp(1)).NominalWind(0), Tol);
            Assert.AreEqual(200, Designer.FindOption(Gyp(4)).NominalWind(0), Tol);
            Assert.AreEqual(350, Designer.FindOption(Gyp(16)).NominalWind(0), Tol);
            Assert.AreEqual(360, Designer.FindOption(Gyp(26)).NominalWind(0), Tol);
            Assert.IsTrue(Designer.FindOption(Gyp(16)).Blocked);
            Assert.IsFalse(Designer.FindOption(Gyp(4)).Blocked);
        }

        [TestMethod]
        public void Gypsum_OneFace_And_BothFaces()
        {
            var one = Check(8, 8, Gyp(4), null);
            Assert.AreEqual(200, one.CombinedNominal, Tol);
            Assert.AreEqual(100, one.Capacity, Tol, "ASD 200 / 2.0");
            Assert.AreEqual(1.0, one.Faces[0].SpeciesFactor, "no species factor in Table 4.3C");

            var two = Check(8, 8, Gyp(4), Gyp(4));
            Assert.AreEqual(400, two.CombinedNominal, Tol);
            StringAssert.Contains(two.CombineRule, "same material");
        }

        [TestMethod]
        public void WoodPanelPlusGypsum_AddedForWind_WhenAllowed()
        {
            var r = Check(8, 8, SI_15_32_8d_6, Gyp(4), sum: true);
            Assert.AreEqual(785 + 200, r.CombinedNominal, Tol);
            Assert.AreEqual(492.5, r.Capacity, Tol);
            StringAssert.Contains(r.CombineRule, "added for wind");
        }

        [TestMethod]
        public void WoodPanelPlusGypsum_LargerOfStrongerAndTwiceWeaker_WhenNotAllowed()
        {
            var r = Check(8, 8, SI_15_32_8d_6, Gyp(4), sum: false);
            Assert.AreEqual(785, r.CombinedNominal, Tol, "max(785, 2 × 200)");
            StringAssert.Contains(r.CombineRule, "stronger face");

            var r2 = Check(8, 8, Sht_7_16_8d_6, Gyp(16), sum: false);
            Assert.AreEqual(700, r2.CombinedNominal, Tol, "max(670, 2 × 350)");
            StringAssert.Contains(r2.CombineRule, "2 × weaker");
        }

        [TestMethod]
        public void GypsumPlusCementPlaster_IsNotASummingPair()
        {
            var r = Check(8, 8, Gyp(1), Gyp(26), sum: true);
            Assert.AreEqual(360, r.CombinedNominal, Tol, "max(360, 2 × 150)");
            Assert.IsFalse(SheathingRules.MaySum(Designer.FindOption(Gyp(1)), Designer.FindOption(Gyp(26))));
        }

        [TestMethod]
        public void TallNarrowWall_GypsumFaceDoesNotCount_WoodFaceTakesTheAspectFactor()
        {
            // h = 9, L = 3.5: h/b = 2.571 > 2.0 for gypsum; wood panels 1.25 − 0.125 × 2.571 = 0.92857
            var r = Check(9, 3.5, SI_15_32_8d_6, Gyp(4), sum: true);
            double af = 1.25 - 0.125 * 9 / 3.5;
            Assert.AreEqual(af, r.Faces[0].AspectFactor, Tol);
            Assert.AreEqual(0, r.Faces[1].Nominal, Tol);
            StringAssert.Contains(r.Faces[1].Note, "does not count");
            Assert.AreEqual(785 * af, r.CombinedNominal, Tol);
            Assert.IsTrue(r.AspectOk, "the wood face is within 3.5:1");

            var gypOnly = Check(9, 3.5, Gyp(4), null);
            Assert.IsFalse(gypOnly.AspectOk);
            Assert.AreEqual(2.0, gypOnly.MaxAspectRatio, Tol);
        }

        [TestMethod]
        public void SpeciesFactor_WoodTablesOnly()
        {
            var r = Check(8, 8, SI_15_32_8d_6, Gyp(4), sum: true, species: WoodTypes.WOODTYPE_SPF_HF);
            Assert.AreEqual(0.92, r.Faces[0].SpeciesFactor, Tol);
            Assert.AreEqual(1.0, r.Faces[1].SpeciesFactor, Tol);
            Assert.AreEqual(785 * 0.92 + 200, r.CombinedNominal, Tol);
        }

        [TestMethod]
        public void StructuralFiberboard_AspectFactorOver1to1()
        {
            // 1/2" fiberboard, 4" edge: 475; h/b = 9/6 = 1.5 → 1.09 − 0.09 × 1.5 = 0.955
            var fb = Designer.SheathingOptions.First(o => o.IsStructuralFiberboard && o.Thickness == NominalPanelThicknesses.THICKNESS_1_2IN).ToFace(4);
            var r = Check(9, 6, fb, null);
            Assert.AreEqual(475 * 0.955, r.CombinedNominal, Tol);
            Assert.IsTrue(double.IsNaN(Designer.FindOption(fb).NominalWind(6)), "no 6\" column for fiberboard");
        }

        [TestMethod]
        public void Table4_3B_WoodOverGypsum()
        {
            var o = Designer.SheathingOptions.First(x => x.Table == SheathingTable.WoodOverGypsum && x.Material == SheathingMaterials.SHEATH_MAT_STRUCTURAL_1 &&
                                                         x.Thickness == NominalPanelThicknesses.THICKNESS_15_32IN);
            var r = Check(8, 8, o.ToFace(4), null);
            Assert.AreEqual(1205, r.CombinedNominal, Tol);
            Assert.AreEqual(602.5, r.Capacity, Tol);
        }

        [TestMethod]
        public void Options_RepeatedTableRowListedOnce_SidingRowsNamed()
        {
            int rows43C = new Table4_3CManager().Table4_3CNominalUnitShearCapacities.Count;
            int opts43C = Designer.SheathingOptions.Count(o => o.Table == SheathingTable.GypsumAndPlaster);
            Assert.AreEqual(rows43C - 1, opts43C, "rows 1 and 2 of the typed Table 4.3C are the same");
            Assert.AreEqual(2, Designer.SheathingOptions.Count(o => o.Table == SheathingTable.WoodOverGypsum && o.Material == SheathingMaterials.SHEATH_MAT_PLYWOOD));
            Assert.AreEqual(Designer.SheathingOptions.Count, Designer.SheathingOptions.Select(o => o.Description).Distinct().Count(), "every choice reads differently");
        }

        [TestMethod]
        public void OlderProject_TwoSides_IsStillTwiceOneFace()
        {
            var d = new WallDesignInputs { Sides = 2, EdgeSpacing = 4, HoldownOnInsideFace = false }; // no faces: Structural I 15/32 8d
            var r = Designer.Check(0, new WallData(8, 0, 0, 8, 0), 1.0, "t", new DesignSettings(), d);
            Assert.AreEqual(2 * 1205, r.CombinedNominal, Tol);
            StringAssert.Contains(r.Panel, "both faces");
        }

        [TestMethod]
        public void SetFaces_KeepsOlderFieldsInStep_AndCloneIsDeep()
        {
            var d = new WallDesignInputs();
            d.SetFaces(Sht_7_16_8d_6, Gyp(4));
            Assert.AreEqual(SheathingMaterials.SHEATH_MAT_SHEATHING_AND_SINGLE_FLOOR, d.Sheathing);
            Assert.AreEqual(2, d.Sides);
            var c = d.Clone();
            c.Face1.EdgeSpacing = 2;
            Assert.AreEqual(6, d.Face1.EdgeSpacing);
        }

        [TestMethod]
        public void ProjectFile_KeepsBothFaces_TheSummingSetting_AndTheNewWallDefault()
        {
            var calc = new ShearWallCalculator_RigidDiaphragm();
            var wall = new WallData(8, 0, 0, 8, 0) { Design = new WallDesignInputs() };
            wall.Design.SetFaces(SI_15_32_8d_6, Gyp(16));
            calc._wall_system.AddWall(wall);
            calc.DesignSettings.SumWindFaces = false;
            calc.DesignSettings.NewWallDesign = new WallDesignInputs();
            calc.DesignSettings.NewWallDesign.SetFaces(Gyp(4), Gyp(4));

            var read = ShearWallCalculator.Interfaces.ProjectSerializer.Read(
                ShearWallCalculator.Interfaces.ProjectSerializer.ToJson(calc, null, linkedToWind: false)).Calculator;
            var d = read._wall_system._walls.Values.Single().Design;
            Assert.IsTrue(d.EffectiveFace1().SameConstructionAs(SI_15_32_8d_6));
            Assert.IsTrue(d.EffectiveFace2().SameConstructionAs(Gyp(16)));
            Assert.IsFalse(read.DesignSettings.SumWindFaces);
            Assert.IsTrue(read.DesignSettings.NewWallDesign.EffectiveFace2().SameConstructionAs(Gyp(4)));
        }

        [TestMethod]
        public void NewProject_AddsWoodAndGypsumForWind_ByDefault()
        {
            Assert.IsTrue(new DesignSettings().SumWindFaces);
            Assert.IsTrue(new ShearWallCalculator_RigidDiaphragm().DesignSettings.SumWindFaces);
        }

        [TestMethod]
        public void AutoSelect_TightensTheWoodFaceNailing_KeepsTheGypsumFace()
        {
            // V = 4 k on 8 ft, ASD: v = 0.6 × 4000 / 8 = 300 plf; needs nominal ≥ 600. 785 + 200 at 6" passes already;
            // with V = 8 k (600 plf, nominal ≥ 1200): 1205 + 200 = 1405 at 4" passes.
            var wall = new WallData(8, 0, 0, 8, 0) { Design = new WallDesignInputs { HoldownOnInsideFace = false } };
            wall.Design.SetFaces(SI_15_32_8d_6, Gyp(4));
            var settings = new DesignSettings { SumWindFaces = true };
            var pick = Designer.AutoSelect(wall, 8.0, settings);
            Assert.AreEqual(4, pick.Face1.EdgeSpacing);
            Assert.AreEqual(SheathingTable.GypsumAndPlaster, pick.Face2.Table);
            Assert.IsTrue(Designer.Check(0, wall, 8.0, "t", settings, pick).ShearOk);
        }
    }
}
