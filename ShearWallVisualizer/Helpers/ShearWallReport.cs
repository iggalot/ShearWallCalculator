using ASCE7WindLoadCalculator;
using calculator;
using ShearWallCalculator;
using ShearWallCalculator.Design;
using ShearWallCalculator.NDSProvisions;
using ShearWallVisualizer.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace ShearWallVisualizer.Helpers
{
    /// <summary>The shear-wall part of the printed report (added after the wind pages).</summary>
    public static class ShearWallReport
    {
        public static IEnumerable<ReportSection> Sections(ShearWallCalculatorBase calc, bool linkedToWind,
            IReadOnlyDictionary<int, WallDesignResult> design = null)
        {
            var blocks = new List<FrameworkElement> { WindReport.Heading("Shear walls") };
            if (calc == null || !calc.IsValidForCalculation)
            {
                blocks.Add(WindReport.Para("No shear-wall results: draw a diaphragm and walls in both directions first."));
                return new[] { new ReportSection("Shear walls", blocks) };
            }

            calc.PerformCalculations();
            bool rigid = calc is ShearWallCalculator_RigidDiaphragm;
            blocks.Add(WindReport.Para(
                (rigid ? "Rigid diaphragm: the load is shared by wall rigidity, plus torsion from the eccentricity between the centre of mass and the centre of rigidity. "
                       : "Flexible diaphragm: each wall line takes the load tributary to it. ") +
                (linkedToWind
                    ? "Loads: the ASCE 7 wind load cases (Fig. 27.3-8) from the wind pages, strength level; each wall shows its governing case."
                    : "Loads: MANUAL loads entered by the user, not the wind calculation.")));

            if (calc.LoadCases != null && calc.LoadCases.Count > 0)
            {
                blocks.Add(WindReport.Para("Load cases analysed (kips, kip-ft):"));
                blocks.AddRange(WindReport.Tables(new[] { "Case", "Vx (k)", "Vy (k)", "Mt (k-ft)" },
                    calc.LoadCases.Select(c => new[] { c.Name, c.Vx.ToString("0.00"), c.Vy.ToString("0.00"), c.Mt.ToString("0.0") }).ToList()));
            }
            else
            {
                blocks.Add(WindReport.Para($"Manual load: Vx = {calc.V_x:0.00} k, Vy = {calc.V_y:0.00} k, with the minimum eccentricity."));
            }

            var rows = new List<string[]>();
            foreach (var kv in calc._wall_system._walls.OrderBy(w => w.Key))
            {
                var wall = kv.Value;
                double v = calc.TotalWallShear.TryGetValue(kv.Key, out var s) ? s : double.NaN;
                calc.GoverningLoadCase.TryGetValue(kv.Key, out var governing);
                rows.Add(new[]
                {
                    kv.Key.ToString(),
                    wall.WallDir == WallDirs.EastWest ? "E-W" : "N-S",
                    wall.WallLength.ToString("0.00"),
                    wall.WallHeight.ToString("0.0"),
                    double.IsNaN(v) ? "-" : Math.Abs(v).ToString("0.00"),
                    double.IsNaN(v) || wall.WallLength <= 0 ? "-" : (1000 * Math.Abs(v) / wall.WallLength).ToString("0"),
                    governing ?? "-",
                });
            }
            blocks.Add(WindReport.Heading("Wall shears"));
            blocks.Add(WindReport.Para("V is the largest shear over all load cases (strength level); v = V / L is the unit shear along the wall."));
            blocks.AddRange(WindReport.Tables(new[] { "Wall", "Direction", "L (ft)", "h (ft)", "V (k)", "v (plf)", "Governing case" }, rows));

            var sections = new List<ReportSection> { new ReportSection("Shear walls", blocks) };
            if (design != null && design.Count > 0) sections.Add(DesignSection(calc, design));
            return sections;
        }

        /// <summary>The design checks: settings, a summary table, then every step for each wall.</summary>
        private static ReportSection DesignSection(ShearWallCalculatorBase calc, IReadOnlyDictionary<int, WallDesignResult> design)
        {
            var s = calc.DesignSettings ?? new DesignSettings();
            var blocks = new List<FrameworkElement>
            {
                WindReport.Heading("Shear wall design"),
                WindReport.Para($"{(s.Method == DesignMethods.ASD ? "ASD: walls 0.6W, capacity = nominal / 2.0" : "LRFD: walls 1.0W, capacity = 0.80 x nominal")} " +
                                $"({SdpwsSource.EditionName(s.Edition)}). Hold-downs are checked at ASD (0.6W) against Simpson allowable loads. " +
                                "Blocked wood structural panels, Table 4.3A; each drawn wall is one full-height segment. " + SdpwsSource.TableSource),
            };
            var rows = design.Keys.OrderBy(k => k).Select(k => design[k]).Select(r => new[]
            {
                r.WallId.ToString(), r.Panel, $"{r.Inputs.EdgeSpacing}\" / {(r.Inputs.Sides == 2 ? 2 : 1)}",
                double.IsInfinity(r.Demand) ? "-" : r.Demand.ToString("0"), double.IsNaN(r.Capacity) ? "-" : r.Capacity.ToString("0"),
                double.IsNaN(r.Capacity) ? "-" : r.ShearRatio.ToString("0.00"), r.TensionAsd.ToString("0"),
                !r.HoldownNeeded ? "none" : r.Holdown?.ToString() ?? "none strong enough", r.Pass ? "OK" : r.Status,
            }).ToList();
            blocks.AddRange(WindReport.Tables(new[] { "Wall", "Sheathing", "Nails / sides", "v (plf)", "Allowed", "Ratio", "T (lb)", "Hold-down", "Result" }, rows));
            foreach (var r in design.Keys.OrderBy(k => k).Select(k => design[k]))
                blocks.Add(WindReport.Para(string.Join(Environment.NewLine, WallDesignPanel.Explain(r))));
            return new ReportSection("Shear wall design", blocks);
        }
    }
}
