using calculator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShearWallCalculator.Design;
using ShearWallCalculator.NDSProvisions;

namespace ShearWallCalculator.Tests
{
    /// <summary>
    /// How a wall's check reads at a glance (pass, close to the limit, fail), and the sheathing a new wall starts with:
    /// Structural I on an exterior wall, 1/2" gypsum wallboard on both faces of an interior wall (SDPWS Tables 4.3A, 4.3C).
    /// </summary>
    [TestClass]
    public class DesignStateAndDefaultsTests
    {
        private const double Tol = 1e-6;
        private static readonly ShearWallDesigner Designer = new ShearWallDesigner();

        /// <summary>A result with only what the state reads: shear demand and capacity, a short wall, no uplift.</summary>
        private static WallDesignResult Result(double demand, double capacity) => new WallDesignResult
        {
            Demand = demand, Capacity = capacity, AspectRatio = 1, MaxAspectRatio = 3.5, TensionAsd = 0,
        };

        [TestMethod]
        public void State_PassBelow90Percent_CloseFrom90To100_FailAbove()
        {
            Assert.AreEqual(DesignState.Pass, Result(85, 100).State);
            Assert.AreEqual(DesignState.Close, Result(90, 100).State, "0.90 exactly is close");
            Assert.AreEqual(DesignState.Close, Result(100, 100).State, "1.00 exactly still passes");
            Assert.AreEqual(DesignState.Fail, Result(101, 100).State);
        }

        [TestMethod]
        public void State_FailsOnAspectOrHoldown_EvenAtALowShearRatio()
        {
            var slender = Result(10, 100);
            slender.AspectRatio = 4;
            Assert.AreEqual(DesignState.Fail, slender.State);
            var uplift = Result(10, 100);
            uplift.TensionAsd = 5000; // no hold-down chosen
            Assert.AreEqual(DesignState.Fail, uplift.State);
        }

        /// <summary>Exterior default: 15/32" Structural I, 8d common at 6" edges, one face (SDPWS Table 4.3A).</summary>
        [TestMethod]
        public void ExteriorDefault_StructuralI_OneFace()
        {
            var d = WallDesignInputs.ExteriorDefault();
            var f = d.EffectiveFace1();
            Assert.AreEqual(SheathingTable.WoodPanels, f.Table);
            Assert.AreEqual(SheathingMaterials.SHEATH_MAT_STRUCTURAL_1, f.Material);
            Assert.AreEqual(NominalPanelThicknesses.THICKNESS_15_32IN, f.Thickness);
            Assert.AreEqual(CommonNailSizes.NAILSIZE_8D, f.Nail);
            Assert.AreEqual(6, f.EdgeSpacing);
            Assert.IsNull(d.EffectiveFace2());
            Assert.IsNotNull(Designer.FindOption(f), "a row of the table");
        }

        /// <summary>
        /// Interior default: Table 4.3C row 6, 1/2" gypsum wallboard, 7" edges, studs at 16" o.c., blocked, v_w = 250 plf
        /// a face. Same construction on both faces: 2 × 250 = 500 plf nominal; ASD / 2.0 = 250 plf.
        /// </summary>
        [TestMethod]
        public void InteriorDefault_GypsumBothFaces_Row6_250PlfAFace()
        {
            var d = WallDesignInputs.InteriorDefault();
            var o = Designer.FindOption(d.EffectiveFace1());
            Assert.IsNotNull(o);
            Assert.AreEqual(SheathingMaterials.SHEATH_MAT_GYPSUM_WALLBOARD, o.Material);
            Assert.AreEqual(NominalPanelThicknesses.THICKNESS_1_2IN, o.Thickness);
            Assert.IsTrue(o.Blocked);
            Assert.AreEqual(250, o.NominalWind(0), Tol);
            Assert.IsTrue(d.EffectiveFace1().SameConstructionAs(d.EffectiveFace2()));

            d.HoldownOnInsideFace = false;
            var settings = new DesignSettings { Method = DesignMethods.ASD, Edition = SdpwsEditions.SDPWS_2021 };
            var r = Designer.Check(1, new WallData(8, 0, 0, 10, 0), 1.0, "test", settings, d); // h/b 0.8
            Assert.AreEqual(500, r.CombinedNominal, Tol);
            Assert.AreEqual(250, r.Capacity, Tol);
        }

        [TestMethod]
        public void NewWallDesignFor_ProjectDefaultsFirst_ThenTheProgramOnes_AlwaysACopy()
        {
            var s = new DesignSettings();
            Assert.AreEqual(SheathingTable.WoodPanels, s.NewWallDesignFor(true).EffectiveFace1().Table);
            Assert.AreEqual(SheathingTable.GypsumAndPlaster, s.NewWallDesignFor(false).EffectiveFace1().Table);

            s.NewWallDesign = new WallDesignInputs { EdgeSpacing = 3 };
            s.NewInteriorWallDesign = new WallDesignInputs { EdgeSpacing = 2 };
            Assert.AreEqual(3, s.NewWallDesignFor(true).EdgeSpacing);
            Assert.AreEqual(2, s.NewWallDesignFor(false).EdgeSpacing);
            Assert.AreNotSame(s.NewWallDesign, s.NewWallDesignFor(true));
        }
    }
}
