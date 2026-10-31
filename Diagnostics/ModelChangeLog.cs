using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using ShearWallCalculator.Design;
using ShearWallCalculator.NDSProvisions;

namespace ShearWallCalculator.Diagnostics
{
    /// <summary>
    /// What the model looked like at one moment, in words: each wall's geometry and design inputs, each diaphragm,
    /// the design settings, the loads and the plan image. Two snapshots are compared to log every change, whichever
    /// part of the program made it.
    /// </summary>
    public sealed class ModelSnapshot
    {
        public Dictionary<int, string> WallGeometry { get; } = new Dictionary<int, string>();
        public Dictionary<int, Dictionary<string, string>> WallDesign { get; } = new Dictionary<int, Dictionary<string, string>>();
        public Dictionary<int, string> Diaphragms { get; } = new Dictionary<int, string>();
        public Dictionary<string, string> Settings { get; } = new Dictionary<string, string>();
        public List<string> Loads { get; } = new List<string>();
        public Dictionary<string, string> Image { get; } = new Dictionary<string, string>();

        private static string F(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);
        private static string P(Point p) => $"({F(p.X)}, {F(p.Y)})";

        /// <summary>Takes the snapshot. <paramref name="designer"/> names the sheathing; without it the raw values are used.</summary>
        public static ModelSnapshot Take(ShearWallCalculatorBase calc, ShearWallDesigner designer = null)
        {
            var s = new ModelSnapshot();
            if (calc == null) return s;

            if (calc._wall_system?._walls != null)
                foreach (var kv in calc._wall_system._walls)
                {
                    var w = kv.Value;
                    s.WallGeometry[kv.Key] = $"{P(w.Start)} to {P(w.End)}, L = {F(w.WallLength)} ft, h = {F(w.WallHeight)} ft";
                    s.WallDesign[kv.Key] = Describe(w.Design ?? new WallDesignInputs(), designer);
                }

            if (calc._diaphragm_system?._diaphragms != null)
                foreach (var kv in calc._diaphragm_system._diaphragms)
                {
                    var d = kv.Value;
                    double x = Math.Min(d.P1.X, d.P3.X), y = Math.Min(d.P1.Y, d.P3.Y);
                    s.Diaphragms[kv.Key] = $"corner ({F(x)}, {F(y)}), {F(Math.Abs(d.P3.X - d.P1.X))} × {F(Math.Abs(d.P3.Y - d.P1.Y))} ft";
                }

            var ds = calc.DesignSettings ?? new DesignSettings();
            s.Settings["design method"] = ds.Method.ToString();
            s.Settings["SDPWS edition"] = SdpwsSource.EditionName(ds.Edition);
            s.Settings["hold-down pool"] = ds.HoldownsFromStandardList ? "company standard list" : "full catalog";
            s.Settings["sheathing for new walls"] = ds.NewWallDesign == null ? "default" : string.Join("; ", Describe(ds.NewWallDesign, designer).Select(kv => kv.Key + " " + kv.Value));

            if (calc.LoadCases != null && calc.LoadCases.Count > 0)
                foreach (var c in calc.LoadCases)
                    s.Loads.Add($"{c.Name}: Vx = {F(c.Vx)} k, Vy = {F(c.Vy)} k, Mt = {F(c.Mt)} k-ft");
            else
                s.Loads.Add($"manual: Vx = {F(calc.V_x)} k, Vy = {F(calc.V_y)} k");

            var img = calc.Image;
            if (img != null)
            {
                s.Image["file"] = img.Path ?? "";
                s.Image["stored in project"] = img.Data == null ? "no" : $"yes, {img.Data.Length / 1024.0:0} kB";
                s.Image["scale"] = $"{(img.FeetPerPixel * 12).ToString("0.####", CultureInfo.InvariantCulture)} in per pixel" + (img.Calibrated ? "" : " (not set)");
                s.Image["rotation"] = F(img.RotationDegrees) + "°";
                s.Image["position"] = "top-left at " + P(img.Origin);
                s.Image["opacity"] = (img.Opacity * 100).ToString("0", CultureInfo.InvariantCulture) + " %";
                s.Image["locked"] = img.Locked ? "yes" : "no";
                s.Image["shown"] = img.Visible ? "yes" : "no";
            }
            return s;
        }

        /// <summary>A wall's design inputs as labelled values: face 1, face 2, framing, dead load, hold-down.</summary>
        public static Dictionary<string, string> Describe(WallDesignInputs d, ShearWallDesigner designer)
        {
            var f1 = d.EffectiveFace1();
            var f2 = d.EffectiveFace2();
            return new Dictionary<string, string>
            {
                ["face 1"] = Face(f1, designer),
                ["face 2"] = f2 == null ? "none" : f1.SameConstructionAs(f2) ? "same as face 1" : Face(f2, designer),
                ["framing"] = d.Species == WoodTypes.WOODTYPE_SPF_HF ? "SPF/HF" : "DF-L/SP",
                ["dead load"] = F(d.DeadLoad) + " plf",
                ["hold-down type"] = d.Holdown.ToString(),
                ["hold-down on inside face"] = d.HoldownOnInsideFace ? "yes" : "no",
            };
        }

        private static string Face(FaceSheathing f, ShearWallDesigner designer)
        {
            var o = designer?.FindOption(f);
            string name = o?.Description ?? $"{f.Table} {f.Material} {f.Thickness} {f.Nail}" + (f.Row > 0 ? $" row {f.Row}" : "");
            bool spacing = o == null ? f.Table != SheathingTable.GypsumAndPlaster : o.UsesEdgeSpacing;
            return spacing ? $"{name}, edge nailing {f.EdgeSpacing}\"" : name;
        }
    }

    /// <summary>The differences between two snapshots, one line each, for the log.</summary>
    public static class ModelChangeLog
    {
        public static List<string> Differences(ModelSnapshot before, ModelSnapshot after)
        {
            var lines = new List<string>();
            if (before == null || after == null) return lines;

            foreach (int id in after.WallGeometry.Keys.Except(before.WallGeometry.Keys).OrderBy(i => i))
                lines.Add($"Wall {id} added: {after.WallGeometry[id]}");
            foreach (int id in before.WallGeometry.Keys.Except(after.WallGeometry.Keys).OrderBy(i => i))
                lines.Add($"Wall {id} deleted (was {before.WallGeometry[id]})");
            foreach (int id in after.WallGeometry.Keys.Intersect(before.WallGeometry.Keys).OrderBy(i => i))
            {
                if (before.WallGeometry[id] != after.WallGeometry[id])
                    lines.Add($"Wall {id} geometry: {before.WallGeometry[id]} → {after.WallGeometry[id]}");
                Compare($"Wall {id}", before.WallDesign[id], after.WallDesign[id], lines);
            }

            foreach (int id in after.Diaphragms.Keys.Except(before.Diaphragms.Keys).OrderBy(i => i))
                lines.Add($"Diaphragm {id} added: {after.Diaphragms[id]}");
            foreach (int id in before.Diaphragms.Keys.Except(after.Diaphragms.Keys).OrderBy(i => i))
                lines.Add($"Diaphragm {id} deleted (was {before.Diaphragms[id]})");
            foreach (int id in after.Diaphragms.Keys.Intersect(before.Diaphragms.Keys).OrderBy(i => i))
                if (before.Diaphragms[id] != after.Diaphragms[id])
                    lines.Add($"Diaphragm {id}: {before.Diaphragms[id]} → {after.Diaphragms[id]}");

            Compare("Design setting", before.Settings, after.Settings, lines);

            if (!before.Loads.SequenceEqual(after.Loads))
                lines.Add("Loads changed: " + (after.Loads.Count == 0 ? "none" : string.Join("; ", after.Loads)) +
                          "  (were " + (before.Loads.Count == 0 ? "none" : string.Join("; ", before.Loads)) + ")");

            if (before.Image.Count == 0 && after.Image.Count > 0)
                lines.Add($"Plan image added: {after.Image["file"]}, {after.Image["scale"]}");
            else if (before.Image.Count > 0 && after.Image.Count == 0)
                lines.Add($"Plan image removed: {before.Image["file"]}");
            else
                Compare("Plan image", before.Image, after.Image, lines);
            return lines;
        }

        private static void Compare(string what, Dictionary<string, string> before, Dictionary<string, string> after, List<string> lines)
        {
            foreach (var kv in after)
                if (before.TryGetValue(kv.Key, out string old) && old != kv.Value)
                    lines.Add($"{what} {kv.Key}: {old} → {kv.Value}");
        }
    }
}
