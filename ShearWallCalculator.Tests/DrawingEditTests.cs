using System.Linq;
using System.Windows;
using calculator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShearWallCalculator.Drawing;

namespace ShearWallCalculator.Tests
{
    /// <summary>Undo / redo of the drawing, and the drafting edits (delete, move, copy, extents, ortho).</summary>
    [TestClass]
    public class DrawingEditTests
    {
        private static ShearWallCalculator_RigidDiaphragm Building()
        {
            var calc = new ShearWallCalculator_RigidDiaphragm();
            calc._diaphragm_system.AddDiaphragm(new DiaphragmData_Rectangular(new Point(0, 0), new Point(40, 30)));
            calc._wall_system.AddWall(new WallData(9, 0, 0, 20, 0));   // wall 0: E-W, 20 ft
            calc._wall_system.AddWall(new WallData(9, 0, 0, 0, 30));   // wall 1: N-S, 30 ft
            return calc;
        }

        [TestMethod]
        public void UndoRedo_AddDeleteAndDesignEdits()
        {
            var calc = Building();
            var h = new DrawingHistory();
            h.Reset(calc);
            Assert.IsFalse(h.CanUndo);
            Assert.IsFalse(h.Record(calc), "no change, no step");

            calc._wall_system.AddWall(new WallData(9, 40, 0, 40, 30)); // wall 2
            Assert.IsTrue(h.Record(calc));
            calc._wall_system._walls[0].Design.EdgeSpacing = 2;
            Assert.IsTrue(h.Record(calc));
            DrawingEdits.Delete(calc, "Wall", 1);
            Assert.IsTrue(h.Record(calc));
            Assert.AreEqual(3, h.UndoSteps);

            Assert.AreEqual("wall 1 deleted", h.Undo(calc)); // the step undone; the wall comes back
            Assert.AreEqual(3, calc._wall_system._walls.Count);
            Assert.AreEqual(1, calc._wall_system.NS_Walls.Count(w => w.Key == 1), "sorted into N-S again");
            Assert.AreEqual(30, calc._wall_system._walls[1].WallLength, 1e-9);

            Assert.AreEqual("wall 0 changed", h.Undo(calc));
            Assert.AreEqual(6, calc._wall_system._walls[0].Design.EdgeSpacing);
            Assert.AreEqual("wall 2 added", h.Undo(calc));
            Assert.AreEqual(2, calc._wall_system._walls.Count);
            Assert.IsFalse(h.CanUndo);
            Assert.IsNull(h.Undo(calc));

            Assert.AreEqual("wall 2 added", h.Redo(calc));
            Assert.AreEqual(3, calc._wall_system._walls.Count);
            Assert.AreEqual(2, h.RedoSteps);

            // a new change drops what was undone
            calc._diaphragm_system._diaphragms.Clear();
            Assert.IsTrue(h.Record(calc));
            Assert.IsFalse(h.CanRedo);
            Assert.AreEqual("diaphragm 0 deleted", h.Undo(calc));
            Assert.AreEqual(1200, calc._diaphragm_system._diaphragms[0].Area, 1e-6);
        }

        [TestMethod]
        public void UndoRestoresTheCalculatedResults()
        {
            var calc = Building();
            calc._wall_system.AddWall(new WallData(9, 40, 0, 40, 30));
            calc._wall_system.AddWall(new WallData(9, 0, 30, 40, 30));
            calc.PerformCalculations();
            var cr = calc._wall_system.CtrRigidity;
            var h = new DrawingHistory();
            h.Reset(calc);

            DrawingEdits.Move(calc, "Wall", 2, new Vector(-20, 0));
            h.Record(calc);
            calc.PerformCalculations();
            Assert.AreNotEqual(cr.X, calc._wall_system.CtrRigidity.X, 1e-6);

            h.Undo(calc);
            calc.PerformCalculations();
            Assert.AreEqual(cr.X, calc._wall_system.CtrRigidity.X, 1e-9);
            Assert.AreEqual(cr.Y, calc._wall_system.CtrRigidity.Y, 1e-9);
        }

        [TestMethod]
        public void History_KeepsAtMostTheLimit()
        {
            var calc = Building();
            var h = new DrawingHistory();
            h.Reset(calc);
            for (int i = 0; i < DrawingHistory.Limit + 20; i++)
            {
                calc._wall_system._walls[0].WallHeight = 10 + i;
                h.Record(calc);
            }
            Assert.AreEqual(DrawingHistory.Limit - 1, h.UndoSteps);
        }

        [TestMethod]
        public void Move_KeepsIdAndDesign()
        {
            var calc = Building();
            calc._wall_system._walls[0].Design.EdgeSpacing = 3;
            Assert.IsTrue(DrawingEdits.Move(calc, "Wall", 0, new Vector(5, 10)));
            var w = calc._wall_system._walls[0];
            Assert.AreEqual(new Point(5, 10), w.Start);
            Assert.AreEqual(new Point(25, 10), w.End);
            Assert.AreEqual(3, w.Design.EdgeSpacing);
            Assert.IsTrue(calc._wall_system.EW_Walls.ContainsKey(0));

            Assert.IsTrue(DrawingEdits.Move(calc, "Diaphragm", 0, new Vector(-1, 2)));
            var d = calc._diaphragm_system._diaphragms[0];
            Assert.AreEqual(new Point(-1, 2), d.P1);
            Assert.AreEqual(new Point(39, 32), d.P3);
            Assert.AreEqual(new Point(19, 17), d.Centroid);
            Assert.AreEqual(1200, d.Area, 1e-6);
            Assert.AreEqual(new Point(19, 17), calc._diaphragm_system.CtrMass);

            Assert.IsFalse(DrawingEdits.Move(calc, "Wall", 99, new Vector(1, 1)));
        }

        [TestMethod]
        public void Copy_IsIndependentWithANewId()
        {
            var calc = Building();
            calc._wall_system._walls[1].Design.EdgeSpacing = 3;
            calc._wall_system._walls[1].WallHeight = 10;
            int id = DrawingEdits.Copy(calc, "Wall", 1, new Vector(40, 0));
            Assert.AreEqual(2, id);
            var c = calc._wall_system._walls[id];
            Assert.AreEqual(new Point(40, 0), c.Start);
            Assert.AreEqual(new Point(40, 30), c.End);
            Assert.AreEqual(10, c.WallHeight);
            Assert.AreEqual(3, c.Design.EdgeSpacing);
            Assert.IsTrue(calc._wall_system.NS_Walls.ContainsKey(id));
            c.Design.EdgeSpacing = 4;
            Assert.AreEqual(3, calc._wall_system._walls[1].Design.EdgeSpacing, "the copy's design is its own");
            Assert.AreEqual(new Point(0, 0), calc._wall_system._walls[1].Start, "the original stays");

            int did = DrawingEdits.Copy(calc, "Diaphragm", 0, new Vector(0, 30));
            Assert.AreEqual(1, did);
            Assert.AreEqual(2400, calc._diaphragm_system.TotalArea, 1e-6);
            Assert.AreEqual(new Point(20, 30), calc._diaphragm_system.CtrMass);
            Assert.AreEqual(-1, DrawingEdits.Copy(calc, "Diaphragm", 7, new Vector()));
        }

        [TestMethod]
        public void Delete_Extents_Ortho()
        {
            var calc = Building();
            calc._wall_system.AddWall(new WallData(9, 50, -5, 50, 10));
            var r = DrawingEdits.Extents(calc, includePlan: false);
            Assert.AreEqual(new Rect(0, -5, 50, 35), r);
            Assert.IsTrue(DrawingEdits.Delete(calc, "Wall", 2));
            Assert.IsFalse(DrawingEdits.Delete(calc, "Wall", 2));
            Assert.AreEqual(new Rect(0, 0, 40, 30), DrawingEdits.Extents(calc, false));
            Assert.IsTrue(DrawingEdits.Extents(new ShearWallCalculator_RigidDiaphragm(), true).IsEmpty);

            Assert.AreEqual(new Point(10, 2), DrawingEdits.Ortho(new Point(10, 3), new Point(0, 2)));
            Assert.AreEqual(new Point(0, -8), DrawingEdits.Ortho(new Point(1, -8), new Point(0, 2)));
            Assert.AreEqual(new Point(new Vector(3, 4).Length, 0).X, 5, 1e-12);
        }
    }
}
