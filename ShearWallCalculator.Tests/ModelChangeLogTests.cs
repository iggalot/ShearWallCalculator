using BuildingModel.Diagnostics;
using System.Linq;
using System.Windows;
using calculator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShearWallCalculator.Design;
using ShearWallCalculator.Diagnostics;
using ShearWallCalculator.Drawing;
using ShearWallCalculator.NDSProvisions;

namespace ShearWallCalculator.Tests
{
    /// <summary>The log names every model change: geometry, sheathing and nailing, design settings, loads, the image.</summary>
    [TestClass]
    public class ModelChangeLogTests
    {
        private static readonly ShearWallDesigner Designer = new ShearWallDesigner();

        private static ShearWallCalculator_RigidDiaphragm Model()
        {
            var calc = new ShearWallCalculator_RigidDiaphragm();
            calc._wall_system.AddWall(new WallData(9, 0, 0, 20, 0));
            calc._wall_system.AddWall(new WallData(9, 0, 0, 0, 15));
            calc._diaphragm_system.AddDiaphragm(new DiaphragmData_Rectangular(new Point(0, 0), new Point(40, 30)));
            return calc;
        }

        private static System.Collections.Generic.List<string> Diff(ShearWallCalculator_RigidDiaphragm calc, System.Action<ShearWallCalculator_RigidDiaphragm> change)
        {
            var before = ModelSnapshot.Take(calc, Designer);
            change(calc);
            return ModelChangeLog.Differences(before, ModelSnapshot.Take(calc, Designer));
        }

        [TestMethod]
        public void NoChange_NothingLogged()
        {
            var calc = Model();
            Assert.AreEqual(0, Diff(calc, c => { }).Count);
        }

        [TestMethod]
        public void WallAddedDeletedAndResized()
        {
            var calc = Model();
            int first = calc._wall_system._walls.Keys.Min();
            var lines = Diff(calc, c =>
            {
                c._wall_system.AddWall(new WallData(10, 40, 0, 40, 30));
                c._wall_system._walls[first].End = new Point(25, 0);
            });
            Assert.IsTrue(lines.Any(l => l.Contains("added") && l.Contains("(40, 0) to (40, 30)") && l.Contains("h = 10 ft")), string.Join("\n", lines));
            Assert.IsTrue(lines.Any(l => l.StartsWith($"Wall {first} geometry") && l.Contains("L = 20 ft") && l.Contains("L = 25 ft")), string.Join("\n", lines));

            var removed = Diff(calc, c => c._wall_system._walls.Remove(first));
            Assert.AreEqual(1, removed.Count);
            StringAssert.Contains(removed[0], $"Wall {first} deleted");
        }

        [TestMethod]
        public void SheathingNailingFramingDeadLoadHoldown()
        {
            var calc = Model();
            int id = calc._wall_system._walls.Keys.Min();
            var gyp = Designer.SheathingOptions.First(o => o.Table == SheathingTable.GypsumAndPlaster && o.Row == 4);
            var lines = Diff(calc, c =>
            {
                var d = new WallDesignInputs { Species = WoodTypes.WOODTYPE_SPF_HF, DeadLoad = 150, Holdown = HoldownFamilies.HTT_ToFoundation };
                var f1 = d.EffectiveFace1().Clone();
                f1.EdgeSpacing = 4;
                d.SetFaces(f1, gyp.ToFace());
                c._wall_system._walls[id].Design = d;
            });
            string all = string.Join("\n", lines);
            Assert.IsTrue(lines.Any(l => l.StartsWith($"Wall {id} face 1:") && l.Contains("edge nailing 6\"") && l.Contains("edge nailing 4\"")), all);
            Assert.IsTrue(lines.Any(l => l.StartsWith($"Wall {id} face 2: none →") && l.Contains(gyp.Description)), all);
            Assert.IsTrue(lines.Any(l => l == $"Wall {id} framing: DF-L/SP → SPF/HF"), all);
            Assert.IsTrue(lines.Any(l => l == $"Wall {id} dead load: 0 plf → 150 plf"), all);
            Assert.IsTrue(lines.Any(l => l.StartsWith($"Wall {id} hold-down type:")), all);
        }

        [TestMethod]
        public void DiaphragmSettingsLoadsAndImage()
        {
            var calc = Model();
            int did = calc._diaphragm_system._diaphragms.Keys.Single();
            var lines = Diff(calc, c =>
            {
                c._diaphragm_system._diaphragms[did].P3 = new Point(50, 30);
                c.DesignSettings.Method = DesignMethods.LRFD;
                c.SetLoadCases(new[] { new LateralLoadCase("Case 1", 12.5, 0) });
                c.Image = ReferenceImage.Imported(@"C:\plans\a.png", 1000, 800, 0.05);
            });
            string all = string.Join("\n", lines);
            Assert.IsTrue(lines.Any(l => l.StartsWith($"Diaphragm {did}:") && l.Contains("40 × 30") && l.Contains("50 × 30")), all);
            Assert.IsTrue(lines.Contains("Design setting design method: ASD → LRFD"), all);
            Assert.IsTrue(lines.Any(l => l.StartsWith("Loads changed: Case 1: Vx = 12.5 k")), all);
            Assert.IsTrue(lines.Any(l => l.StartsWith(@"Plan image added: C:\plans\a.png")), all);

            var img = Diff(calc, c => { c.Image.Calibrate(new Point(0, 0), new Point(10, 0), 20); c.Image.Locked = true; });
            Assert.IsTrue(img.Any(l => l.StartsWith("Plan image scale:")), string.Join("\n", img));
            Assert.IsTrue(img.Contains("Plan image locked: no → yes"), string.Join("\n", img));
        }
    }
}
