using System.Linq;
using calculator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShearWallCalculator.Design;
using ShearWallCalculator.NDSProvisions;

namespace ShearWallCalculator.Tests
{
    /// <summary>
    /// The engineer's own hold-down for a wall (WallDesignInputs.HoldownKey) instead of the lightest that works. By hand:
    /// L = 10 ft, h = 9 ft, V = 5 k, T = 0.6 × 5000 × 9 / d, d = L − post / 12 (post from the hold-down's minimum member).
    /// </summary>
    [TestClass]
    public class HoldownChoiceTests
    {
        private static readonly ShearWallDesigner Designer = new ShearWallDesigner();
        private static WallData Wall() => new WallData(9, 0, 0, 10, 0);
        private static DesignSettings Asd => new DesignSettings { Method = DesignMethods.ASD, Edition = SdpwsEditions.SDPWS_2021 };

        private static string Key(string model) =>
            Designer.Options(HoldownFamilies.HDU_ToFoundation, WoodTypes.WOODTYPE_DF_SP).First(o => o.Model == model).Key;

        [TestMethod]
        public void Auto_IsTheLightestThatWorks()
        {
            var r = Designer.Check(1, Wall(), 5.0, null, Asd);
            Assert.AreEqual("HDUE3-SDS3", r.Holdown.Model);
            Assert.IsFalse(r.HoldownChosen);
        }

        [TestMethod]
        public void SizeUp_IsUsed_ArmFromItsPost_Passes()
        {
            var d = new WallDesignInputs { HoldownKey = Key("HDUE5-SDS3") };
            var r = Designer.Check(1, Wall(), 5.0, null, Asd, d);
            Assert.AreEqual("HDUE5-SDS3", r.Holdown.Model);
            Assert.IsTrue(r.HoldownChosen);
            Assert.AreEqual(10 - 1.5 * WallSchedule.PostAlongWall(r.Holdown) / 12.0, r.HoldownArm, 1e-12);
            Assert.AreEqual(27000 / r.HoldownArm, r.TensionAsd, 1e-9);
            Assert.IsTrue(r.Holdown.Allowable > 3790, "larger than the HDUE3 auto pick");
            Assert.IsTrue(r.HoldownOk);
            Assert.IsTrue(r.Pass);
        }

        [TestMethod]
        public void TooWeak_FailsTheWall_WithTheReason()
        {
            // DTT2Z, first fastener option: 1825 lb DF/SP < T ≈ 2769 lb
            var d = new WallDesignInputs { HoldownKey = Key("DTT2Z") };
            var r = Designer.Check(1, Wall(), 5.0, null, Asd, d);
            Assert.AreEqual("DTT2Z", r.Holdown.Model);
            Assert.AreEqual(1825.0, r.Holdown.Allowable);
            Assert.IsTrue(r.TensionAsd > 1825);
            Assert.IsFalse(r.HoldownOk);
            Assert.IsFalse(r.Pass);
            Assert.AreEqual(DesignState.Fail, r.State);
            StringAssert.Contains(r.Status, "DTT2Z 1825 lb < T");
        }

        [TestMethod]
        public void ChoiceNotOfTheWallsType_IsIgnored_AutoUsed()
        {
            var d = new WallDesignInputs { Holdown = HoldownFamilies.HTT_ToFoundation, HoldownKey = Key("HDUE5-SDS3") };
            var r = Designer.Check(1, Wall(), 5.0, null, Asd, d);
            Assert.IsFalse(r.HoldownChosen);
            Assert.AreEqual(d.HoldownKey, r.HoldownChoiceIgnored);
            Assert.AreEqual(Designer.SelectHoldown(HoldownFamilies.HTT_ToFoundation, WoodTypes.WOODTYPE_DF_SP, r.TensionAsd).Key, r.Holdown.Key);
        }

        [TestMethod]
        public void Choice_IsUsedEvenWhenNotOnTheStandardList()
        {
            var s = Asd;
            s.HoldownsFromStandardList = true; // the designer's standard list is empty here
            var r = Designer.Check(1, Wall(), 5.0, null, s, new WallDesignInputs { HoldownKey = Key("HDUE5-SDS3") });
            Assert.AreEqual("HDUE5-SDS3", r.Holdown.Model);
            Assert.IsTrue(r.Pass);
        }

        [TestMethod]
        public void Clone_KeepsTheChoice()
        {
            var d = new WallDesignInputs { HoldownKey = "HDUE5-SDS3#1" };
            Assert.AreEqual("HDUE5-SDS3#1", d.Clone().HoldownKey);
        }
    }
}
