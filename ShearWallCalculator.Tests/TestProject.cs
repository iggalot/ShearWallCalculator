using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using BuildingModel.Serialization;
using calculator;
using ShearWallCalculator.ModelAdapter;

namespace ShearWallCalculator.Tests
{
    /// <summary>
    /// Saves calculators as a project and opens it again the way the app does: the walls and diaphragms in a building model
    /// (their design in its bags), the designer's section and plan images (<see cref="ShearWallProject.Save"/>), the model
    /// file's JSON, then <see cref="ShearWallProject.Open"/> and <see cref="ShearWallAdapter.ToCalculators"/>.
    /// </summary>
    internal static class TestProject
    {
        public static ShearWallCalculatorBase RoundTrip(ShearWallCalculatorBase calc, bool manualLoads = true, Func<string, string> editSettingsJson = null) =>
            RoundTrip(new[] { calc }, manualLoads, editSettingsJson)[0];

        /// <param name="editSettingsJson">Changes the stored design settings before they are read back (an older file's settings).</param>
        public static List<ShearWallCalculatorBase> RoundTrip(IList<ShearWallCalculatorBase> calcs, bool manualLoads = true, Func<string, string> editSettingsJson = null)
        {
            foreach (var c in calcs) GiveADiaphragm(c);
            var b = ShearWallAdapter.FromCalculators(calcs);
            var stories = ShearWallAdapter.ToCalculators(b, calcs);
            ShearWallProject.Save(b, stories, manualLoads);
            if (editSettingsJson != null) b.SetData(ShearWallAdapter.SettingsKey, editSettingsJson(b.GetData(ShearWallAdapter.SettingsKey)));

            var back = ModelFile.FromJson(ModelFile.ToJson(new ModelDocument { Building = b })).Building;
            var opened = ShearWallProject.Open(back, manualLoads, out _);
            return ShearWallAdapter.ToCalculators(back, opened).Select(s => s.Calculator).ToList();
        }

        /// <summary>The model takes the walls a diaphragm reaches: walls drawn without one get one around them.</summary>
        private static void GiveADiaphragm(ShearWallCalculatorBase c)
        {
            var walls = c._wall_system._walls.Values.ToList();
            if (walls.Count == 0 || c._diaphragm_system._diaphragms.Count > 0) return;
            double x0 = walls.Min(w => Math.Min(w.Start.X, w.End.X)) - 1, y0 = walls.Min(w => Math.Min(w.Start.Y, w.End.Y)) - 1;
            double x1 = walls.Max(w => Math.Max(w.Start.X, w.End.X)) + 1, y1 = walls.Max(w => Math.Max(w.Start.Y, w.End.Y)) + 1;
            c._diaphragm_system._diaphragms[1] = new DiaphragmData_Rectangular(new Point(x0, y0), new Point(x1, y1)) { ID = 1 };
            c.Update();
        }
    }
}
