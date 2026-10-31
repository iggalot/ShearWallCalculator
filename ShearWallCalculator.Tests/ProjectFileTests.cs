using System.IO;
using System.Linq;
using System.Windows;
using ASCE7WindLoadCalculator;
using calculator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShearWallCalculator.Interfaces;

namespace ShearWallCalculator.Tests
{
    /// <summary>One project file holds the wind inputs, the drawing and the loads (format 2); older drawing files still open.</summary>
    [TestClass]
    public class ProjectFileTests
    {
        private static ShearWallCalculator_RigidDiaphragm Drawing()
        {
            var calc = new ShearWallCalculator_RigidDiaphragm();
            calc._wall_system.AddWall(new WallData(9, 0, 5, 0, 15));
            calc._wall_system.AddWall(new WallData(9, 40, 5, 40, 15));
            calc._wall_system.AddWall(new WallData(10, 15, 0, 25, 0));
            calc._wall_system.AddWall(new WallData(9, 15, 20, 25, 20));
            calc._diaphragm_system.AddDiaphragm(new DiaphragmData_Rectangular(new Point(0, 0), new Point(40, 20)));
            calc.SetLoadCases(new[] { new LateralLoadCase("Case 1", 12.5, 0), new LateralLoadCase("Case 1 (S)", 0, 20.25, 3.5) });
            return calc;
        }

        private static WindInputs Wind()
        {
            var p = new WindParameters_Base
            {
                RiskCategory = "III", WindSpeed = 142, ExposureCategory = WindExposureCategories.WIND_EXP_CAT_D, Kzt = 1.23,
                RoofComponentArea = 14 * 14 / 3.0, WallComponentArea = 21,
                LateralProcedure = LateralProcedure.Envelope,
            };
            var b = new BuildingData
            {
                BuildingLength = 72, BuildingWidth = 36, BuildingHeight = 18, RoofPitch = 22.6, OverhangWidth = 2.5, ParapetHeight = 3, ParapetEnclosure = BuildingEnclosures.BLDG_OPEN,
                RoofType = RoofTypes.ROOF_TYPE_GABLE, RidgeDirection = RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH,
                EnclosureType = BuildingEnclosures.BLDG_PARTIALLY_ENCLOSED,
            };
            return WindInputs.From(ASCE7_Versions.ASCE_VER_7_16, p, b, onlyCases1And3: true,
                new ComponentChoice(ComponentChoice.Framing, 14), new ComponentChoice(ComponentChoice.Area, 21));
        }

        [TestMethod]
        public void RoundTrip_KeepsEveryWindInput()
        {
            var read = ProjectSerializer.Read(ProjectSerializer.ToJson(Drawing(), Wind(), linkedToWind: true));
            Assert.IsFalse(read.IsOlderDrawingFile);
            var w = read.Wind;
            Assert.AreEqual(ASCE7_Versions.ASCE_VER_7_16, w.Edition);
            Assert.IsTrue(w.OnlyCases1And3);

            var p = w.ToParameters();
            Assert.AreEqual("III", p.RiskCategory);
            Assert.AreEqual(142, p.WindSpeed);
            Assert.AreEqual(LateralProcedure.Envelope, p.LateralProcedure);
            Assert.AreEqual(WindExposureCategories.WIND_EXP_CAT_D, p.ExposureCategory);
            Assert.AreEqual(1.23, p.Kzt);
            Assert.AreEqual(14 * 14 / 3.0, p.RoofComponentArea, 1e-12);
            Assert.AreEqual(21, p.WallComponentArea);

            var b = w.ToBuilding();
            Assert.AreEqual(72, b.BuildingLength);
            Assert.AreEqual(36, b.BuildingWidth);
            Assert.AreEqual(18, b.BuildingHeight);
            Assert.AreEqual(22.6, b.RoofPitch);
            Assert.AreEqual(2.5, b.OverhangWidth);
            Assert.AreEqual(3, b.ParapetHeight);
            Assert.AreEqual(BuildingEnclosures.BLDG_OPEN, b.ParapetEnclosure);
            Assert.AreEqual(RoofTypes.ROOF_TYPE_GABLE, b.RoofType);
            Assert.AreEqual(RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH, b.RidgeDirection);
            Assert.AreEqual(BuildingEnclosures.BLDG_PARTIALLY_ENCLOSED, b.EnclosureType);

            Assert.AreEqual(ComponentChoice.Framing, w.RoofComponent.Kind);
            Assert.AreEqual(14, w.RoofComponent.Entry);
            Assert.AreEqual(ComponentChoice.Area, w.WallComponent.Kind);
            Assert.AreEqual(21, w.WallComponent.Entry);
        }

        [TestMethod]
        public void RoundTrip_KeepsDrawingLoadCasesAndLink()
        {
            var original = Drawing();
            var read = ProjectSerializer.Read(ProjectSerializer.ToJson(original, Wind(), linkedToWind: true));

            Assert.IsInstanceOfType(read.Calculator, typeof(ShearWallCalculator_RigidDiaphragm));
            Assert.AreEqual(4, read.Calculator._wall_system._walls.Count);
            Assert.AreEqual(10, read.Calculator._wall_system._walls.Values.Max(wl => wl.WallHeight));
            Assert.AreEqual(1, read.Calculator._diaphragm_system._diaphragms.Count);
            CollectionAssert.AreEqual(original.LoadCases.Select(c => c.Name).ToList(), read.Calculator.LoadCases.Select(c => c.Name).ToList());
            Assert.AreEqual(3.5, read.Calculator.LoadCases[1].Mt);
            Assert.IsTrue(read.Loads.LinkedToWind);

            read.Calculator.PerformCalculations();
            original.PerformCalculations();
            foreach (var id in original.TotalWallShear.Keys)
                Assert.AreEqual(original.TotalWallShear[id], read.Calculator.TotalWallShear[id], 1e-9);
        }

        [TestMethod]
        public void SameProject_GivesSameText_SoUnsavedChangesCanBeDetected()
        {
            Assert.AreEqual(ProjectSerializer.ToJson(Drawing(), Wind(), true), ProjectSerializer.ToJson(Drawing(), Wind(), true));
            var changed = Wind();
            changed.Site.WindSpeed = 143;
            Assert.AreNotEqual(ProjectSerializer.ToJson(Drawing(), Wind(), true), ProjectSerializer.ToJson(Drawing(), changed, true));
        }

        [TestMethod]
        public void OlderDrawingFile_StillOpens_WithoutWindInputs()
        {
            var older = new JsonDrawingSerializer();
            string path = Path.Combine(Path.GetTempPath(), "swc_older_" + System.Guid.NewGuid() + ".json");
            try
            {
                older.Save(path, Drawing());
                var read = ProjectSerializer.Load(path);
                Assert.IsTrue(read.IsOlderDrawingFile);
                Assert.IsNull(read.Loads);
                Assert.AreEqual(4, read.Calculator._wall_system._walls.Count);
                Assert.AreEqual(2, read.Calculator.LoadCases.Count);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [TestMethod]
        public void NotAProjectFile_SaysSo()
        {
            Assert.Throws<InvalidDataException>(() => ProjectSerializer.Read("not json"));
            Assert.Throws<InvalidDataException>(() => ProjectSerializer.Read("{ \"something\": 1 }"));
            Assert.Throws<InvalidDataException>(() => ProjectSerializer.Read("{ \"FormatVersion\": 99 }"));
        }

        [TestMethod]
        public void File_IsReadableText_WithNamedEnums()
        {
            string json = ProjectSerializer.ToJson(Drawing(), Wind(), true);
            StringAssert.Contains(json, "\"FormatVersion\": 2");
            StringAssert.Contains(json, "\"ASCE_VER_7_16\"");
            StringAssert.Contains(json, "\"WIND_EXP_CAT_D\"");
        }
    }
}
