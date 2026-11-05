using System.Linq;
using System.Windows;
using BuildingModel.Edits;
using BuildingModel.Geometry;
using BuildingModel.Model;
using BuildingModel.Serialization;
using calculator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShearWallCalculator.Drawing;
using ShearWallCalculator.ModelAdapter;

namespace ShearWallCalculator.Tests
{
    /// <summary>
    /// The project is the building model file: the designer's section (manual loads, the loads when saved) and the plan
    /// images go into the copy that is saved and come out of the building on opening; a model from the editor has none.
    /// </summary>
    [TestClass]
    public class ShearWallProjectTests
    {
        private static Building TwoStories()
        {
            var b = Building.New("Job");
            var l2 = BuildingEdits.AddStory(b, 10);
            var roof = BuildingEdits.AddStory(b, 9);
            b.AddDiaphragm(l2, new Rect2(0, 0, 40, 30));
            b.AddDiaphragm(roof, new Rect2(0, 0, 40, 30));
            return b;
        }

        private static Building ThroughTheFile(Building b) => ModelFile.FromJson(ModelFile.ToJson(new ModelDocument { Building = b })).Building;

        [TestMethod]
        public void ManualLoads_PerStory_ThroughTheFile_ThePlansStayInTheBuilding()
        {
            var b = TwoStories();
            var stories = ShearWallAdapter.ToCalculators(b);
            stories[0].Calculator.AddLoads(12.5, 8);
            stories[1].Calculator.AddLoads(6, 4.25);
            // a plan under the roof level, as the editor stores it, and one in the format older projects used
            var roofLevel = b.Level(stories[1].Level.Id);
            roofLevel.SetData("Sketch.Plan", "{\"File\":\"plans\\\\roof.png\"}");
            b.Level(stories[0].Level.Id).SetData("ShearWall.Image", "{\"Path\":\"C:\\\\plans\\\\l2.png\"}");

            var saved = ModelFile.Clone(b);
            ShearWallProject.Save(saved, stories, manualLoads: true);
            Assert.IsNull(b.GetData(ShearWallProject.Key), "the building being edited is not changed by a save");

            var opened = ThroughTheFile(saved);
            var calcs = ShearWallProject.Open(opened, manualLoads: true, out var section);
            Assert.IsNotNull(section);
            Assert.AreEqual(2, calcs.Count);
            Assert.AreEqual(12.5, calcs[0].V_x, 1e-12);
            Assert.AreEqual(8, calcs[0].V_y, 1e-12);
            Assert.AreEqual(4.25, calcs[1].V_y, 1e-12);

            Assert.IsNull(opened.GetData(ShearWallProject.Key), "taken out of the building while it is edited");
            Assert.IsNotNull(opened.Level(stories[1].Level.Id).GetData("Sketch.Plan"), "the plans are the building's: kept, not taken out");
            Assert.IsNotNull(opened.Level(stories[0].Level.Id).GetData("ShearWall.Image"), "an older plan is left for the sketch to convert");
        }

        [TestMethod]
        public void WithWind_NoManualLoads_ButTheLoadsWhenSaved()
        {
            var b = TwoStories();
            var stories = ShearWallAdapter.ToCalculators(b);
            stories[0].Calculator.SetLoadCases(new[] { new LateralLoadCase("W1", 10, 0, 0) });
            var saved = ModelFile.Clone(b);
            var p = ShearWallProject.Save(saved, stories, manualLoads: false);
            Assert.AreEqual(0, p.ManualLoads.Count);
            Assert.AreEqual(10, p.LoadsWhenSaved[stories[0].Level.Id].Vx, 1e-12);

            var calcs = ShearWallProject.Open(ThroughTheFile(saved), manualLoads: false, out var section);
            Assert.AreEqual(0, calcs[0].V_x, "the wind gives the loads again");
            Assert.AreEqual(10, section.LoadsWhenSaved[stories[0].Level.Id].Vx, 1e-12);
        }

        [TestMethod]
        public void AModelFromTheEditor_HasNoSection()
        {
            var calcs = ShearWallProject.Open(ThroughTheFile(TwoStories()), manualLoads: true, out var section);
            Assert.IsNull(section, "not designed yet");
            Assert.AreEqual(2, calcs.Count);
            Assert.IsTrue(calcs.All(c => c.V_x == 0));
        }

        [TestMethod]
        public void Save_WritesTheDesignIntoTheCopy()
        {
            var b = TwoStories();
            b.AddWall(b.Ground, b.LevelsBottomUp[1], 0, 0, 40, 0, shearWall: true);
            var stories = ShearWallAdapter.ToCalculators(b);
            var wall = stories[0].Calculator._wall_system._walls.Values.Single();
            wall.Design.EdgeSpacing = 2;
            var saved = ModelFile.Clone(b);
            ShearWallProject.Save(saved, stories, manualLoads: true);
            var back = ShearWallAdapter.ToCalculators(ThroughTheFile(saved));
            Assert.AreEqual(2, back[0].Calculator._wall_system._walls.Values.Single().Design.EdgeSpacing);
        }
    }
}
