using System.Linq;
using calculator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShearWallCalculator.Design;
using ShearWallCalculator.NDSProvisions;

namespace ShearWallCalculator.Tests
{
    /// <summary>
    /// The design edits shared by the Design table and the plan's wall card (WallDesignEdits), checked against the design by
    /// hand: L = 10 ft, h = 9 ft, V = 5 k at ASD; 15/32" Structural I 8d: 785 plf at 6", 1205 plf at 4" (SDPWS Table 4.3A).
    /// </summary>
    [TestClass]
    public class WallDesignEditsTests
    {
        private static readonly ShearWallDesigner Designer = new ShearWallDesigner();
        private static WallData Wall() => new WallData(9, 0, 0, 10, 0);
        private static DesignSettings Asd => new DesignSettings { Method = DesignMethods.ASD, Edition = SdpwsEditions.SDPWS_2021 };
        private static SheathingOption StructI => Designer.FindOption(new WallDesignInputs().EffectiveFace1());
        private static SheathingOption Gypsum => Designer.SheathingOptions.First(o => o.Table == SheathingTable.GypsumAndPlaster);

        [TestMethod]
        public void Face1Spacing_4in_Capacity1205Over2()
        {
            var d = new WallDesignInputs();
            WallDesignEdits.SetFace1Spacing(d, 4);
            Assert.AreEqual(4, d.EffectiveFace1().EdgeSpacing);
            Assert.AreEqual(1205 / 2.0, Designer.Check(1, Wall(), 5.0, null, Asd, d).Capacity, 1e-9);
        }

        [TestMethod]
        public void SecondFaceSame_TwiceTheCapacity_ThenNone_Once()
        {
            var d = new WallDesignInputs();
            WallDesignEdits.SetFace2Same(d);
            Assert.IsTrue(WallDesignEdits.SecondIsSame(d));
            Assert.AreEqual(2 * 785 / 2.0, Designer.Check(1, Wall(), 5.0, null, Asd, d).Capacity, 1e-9, "two equal faces add");

            // a "same" face 2 follows face 1's nailing
            WallDesignEdits.SetFace1Spacing(d, 4);
            Assert.AreEqual(4, d.EffectiveFace2().EdgeSpacing);
            Assert.AreEqual(2 * 1205 / 2.0, Designer.Check(1, Wall(), 5.0, null, Asd, d).Capacity, 1e-9);

            WallDesignEdits.SetNoFace2(d);
            Assert.IsNull(d.EffectiveFace2());
            Assert.AreEqual(1205 / 2.0, Designer.Check(1, Wall(), 5.0, null, Asd, d).Capacity, 1e-9);
        }

        [TestMethod]
        public void OwnSecondFace_KeepsFace1_AndHasItsOwnNailing()
        {
            var d = new WallDesignInputs();
            WallDesignEdits.SetFace2(d, StructI);
            WallDesignEdits.SetFace2Spacing(d, 4);
            Assert.AreEqual(6, d.EffectiveFace1().EdgeSpacing);
            Assert.AreEqual(4, d.EffectiveFace2().EdgeSpacing);
            Assert.IsFalse(WallDesignEdits.SecondIsSame(d));
        }

        [TestMethod]
        public void Face1ToGypsum_NoEdgeSpacing_SameFace2Follows()
        {
            var d = new WallDesignInputs();
            WallDesignEdits.SetFace2Same(d);
            WallDesignEdits.SetFace1(d, Gypsum);
            Assert.AreEqual(SheathingTable.GypsumAndPlaster, d.EffectiveFace1().Table);
            Assert.AreEqual(0, d.EffectiveFace1().EdgeSpacing, "a Table 4.3C row has its own fastener spacing");
            Assert.AreEqual(SheathingTable.GypsumAndPlaster, d.EffectiveFace2().Table);
        }

        [TestMethod]
        public void HoldownType_ResetsTheChosenSize_SameTypeKeepsIt()
        {
            var d = new WallDesignInputs { HoldownKey = "HDUE9-SDS3.5#1" };
            WallDesignEdits.SetHoldownType(d, HoldownFamilies.HDU_ToFoundation);
            Assert.AreEqual("HDUE9-SDS3.5#1", d.HoldownKey, "same type: kept");
            WallDesignEdits.SetHoldownType(d, HoldownFamilies.HTT_ToFoundation);
            Assert.AreEqual(HoldownFamilies.HTT_ToFoundation, d.Holdown);
            Assert.IsNull(d.HoldownKey, "a new type: Auto");
        }
    }
}
