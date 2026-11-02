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
        /// <summary>
        /// Multi-story: the sections of every level, roof first (the order of the analysis), each titled with its level, with
        /// the loads each level takes from the walls above it. One level gives the same sections as <see cref="Sections"/>.
        /// </summary>
        public static IEnumerable<ReportSection> Levels(IList<ShearWallCalculatorBase> levels, bool linkedToWind,
            Func<ShearWallCalculatorBase, IReadOnlyDictionary<int, WallDesignResult>> design, ShearWallDesigner designer, IList<string> notes)
        {
            if (levels == null || levels.Count <= 1)
                return Sections(levels?.FirstOrDefault(), linkedToWind, design?.Invoke(levels?.FirstOrDefault()), designer);
            var all = new List<ReportSection>();
            for (int i = levels.Count - 1; i >= 0; i--)
            {
                string name = i == levels.Count - 1 ? $"Level {i + 1} (roof)" : $"Level {i + 1}";
                var calc = levels[i];
                var sections = Sections(calc, linkedToWind, design?.Invoke(calc), designer).ToList();
                var intro = new List<FrameworkElement> { WindReport.Heading(name) };
                intro.Add(WindReport.Para(i == levels.Count - 1
                    ? $"The top story and the roof. Analysed first: its walls stand on level {i}, which carries their shears."
                    : $"Story {i + 1} and the floor over it. Its diaphragm takes its own wind plus the walls of level {i + 2} standing on it: " +
                      "each of those walls pushes on this floor with minus its shear, at the wall's middle. They are added at the center of mass " +
                      "with their moment about it (below), so a wall that stacks passes its shear straight down and one that does not is spread by this floor."));
                var above = calc?.IsValidForCalculation == true ? calc.CasesToAnalyse().Where(c => c.AboveVx != 0 || c.AboveVy != 0 || c.AboveMt != 0).ToList() : new List<LateralLoadCase>();
                if (above.Count > 0)
                    intro.AddRange(WindReport.Tables(new[] { "Case", "From above Vx (k)", "Vy (k)", "Mt about CM (k-ft)", "Total Vx (k)", "Vy (k)", "Mt (k-ft)" },
                        above.Select(c => new[] { c.Name, c.AboveVx.ToString("0.00"), c.AboveVy.ToString("0.00"), c.AboveMt.ToString("0.0"),
                                                  c.Vx.ToString("0.00"), c.Vy.ToString("0.00"), c.Mt.ToString("0.0") }).ToList()));
                foreach (var n in (notes ?? new List<string>()).Where(n => n.StartsWith($"Level {i + 2}, wall") || n.StartsWith($"Level {i + 1} ")))
                    intro.Add(WindReport.Para("Check: " + n));
                for (int k = 0; k < sections.Count; k++)
                {
                    var blocks = k == 0 ? intro.Concat(sections[k].Blocks).ToList() : sections[k].Blocks;
                    all.Add(new ReportSection($"{name}: {sections[k].Title}", blocks));
                }
            }
            return all;
        }

        public static IEnumerable<ReportSection> Sections(ShearWallCalculatorBase calc, bool linkedToWind,
            IReadOnlyDictionary<int, WallDesignResult> design = null, ShearWallDesigner designer = null)
        {
            var blocks = new List<FrameworkElement> { WindReport.Heading("Shear walls") };
            if (calc == null || !calc.IsValidForCalculation)
            {
                blocks.Add(WindReport.Para("No shear-wall results: draw a diaphragm and walls in both directions first."));
                return new[] { new ReportSection("Shear walls", blocks) };
            }

            calc.PerformCalculations();
            bool rigid = calc is ShearWallCalculator_RigidDiaphragm;
            var env = calc as ShearWallCalculator_Envelope;
            blocks.Add(WindReport.Para(
                (env != null ? "Envelope of a rigid and a flexible diaphragm (the engineer's choice for this project): both are calculated, and each wall is " +
                               "designed for the larger of its two shears. Rigid: the load is shared by wall rigidity, plus torsion about the centre of rigidity. " +
                               "Flexible: each wall line takes the load tributary to it, plus torsion by equivalent tributary weights. Each case below is marked (rigid) or (flexible). "
                 : rigid ? "Rigid diaphragm: the load is shared by wall rigidity, plus torsion from the eccentricity between the centre of mass and the centre of rigidity. "
                       : "Flexible diaphragm: each wall line takes the load tributary to it. ") +
                (linkedToWind
                    ? "Loads: the ASCE 7 wind load cases from the wind pages (Ch. 27 Fig. 27.3-8 or Ch. 28 Fig. 28.3-1, the wind from every side), strength level."
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
                if (!calc.WallShearRange.TryGetValue(kv.Key, out var range))
                {
                    rows.Add(new[] { kv.Key.ToString(), Dir(wall), wall.WallLength.ToString("0.00"), wall.WallHeight.ToString("0.0"), "-", "-", "-", "-", "-", "-" });
                    continue;
                }
                double v = range.Design;
                rows.Add(new[]
                {
                    kv.Key.ToString(), Dir(wall), wall.WallLength.ToString("0.00"), wall.WallHeight.ToString("0.0"),
                    range.MaxPositive.ToString("0.000"), range.MaxPositiveCase ?? "-",
                    range.MaxNegative.ToString("0.000"), range.MaxNegativeCase ?? "-",
                    v.ToString("0.000"),
                    wall.WallLength <= 0 ? "-" : (1000 * v / wall.WallLength).ToString("0"),
                });
            }
            blocks.Add(WindReport.Heading("Wall shears"));
            blocks.Add(WindReport.Para(
                "Each wall's shear over all load cases (strength level, kips). The signed values are the force the wall exerts on the " +
                "diaphragm in the building axes (+x right, +y up), so they oppose the load. The most positive and most negative values " +
                "come from the wind blowing from opposite sides. Design shear V = max(|most positive|, |most negative|); v = V / L. " +
                "Hold-downs come in pairs, one at each end of the wall, both sized for the tension from V."));
            blocks.AddRange(WindReport.Tables(new[] { "Wall", "Dir.", "L (ft)", "h (ft)", "Most + (k)", "Case", "Most − (k)", "Case", "V (k)", "v (plf)" }, rows));

            if (env != null)
            {
                blocks.Add(WindReport.Heading("Rigid and flexible"));
                blocks.Add(WindReport.Para("Each wall's design shear by each calculation (kips, max |V| over its cases); the larger is used."));
                blocks.AddRange(WindReport.Tables(new[] { "Wall", "Rigid V (k)", "Flexible V (k)", "Governs" },
                    calc._wall_system._walls.Keys.OrderBy(k => k).Select(id => new[]
                    {
                        id.ToString(),
                        env.Rigid.WallShearRange.TryGetValue(id, out var r) ? r.Design.ToString("0.000") : "-",
                        env.Flexible.WallShearRange.TryGetValue(id, out var f) ? f.Design.ToString("0.000") : "-",
                        env.Governs.TryGetValue(id, out var g) ? g : "-",
                    }).ToList()));
            }

            var sections = new List<ReportSection> { new ReportSection("Shear walls", blocks) };
            var rigidRun = calc as ShearWallCalculator_RigidDiaphragm ?? env?.Rigid;
            var flexRun = calc as ShearWallCalculator_FlexibleDiaphragm ?? env?.Flexible;
            if (rigidRun != null && rigidRun.CaseResults.Count > 0)
                sections.Add(RigidSheet(rigidRun));
            if (flexRun != null && flexRun.CaseResults.Count > 0)
                sections.Add(FlexibleSheet(flexRun));
            if (design != null && design.Count > 0) sections.Add(DesignSection(calc, design, designer));
            return sections;
        }

        private static string Dir(WallData wall) => wall.WallDir == WallDirs.EastWest ? "E-W" : "N-S";

        /// <summary>
        /// The rigid-diaphragm calculation sheet: sign convention and formulas, wall properties, then every load case with its
        /// Mt_comb terms, each wall's direct, torsional and total shear, and the statics checks.
        /// </summary>
        private static ReportSection RigidSheet(ShearWallCalculator_RigidDiaphragm calc)
        {
            var ws = calc._wall_system;
            var cm = calc._diaphragm_system.CtrMass;
            var cr = ws.CtrRigidity;
            double sumKx = ws.EW_Walls.Values.Sum(w => w.WallRigidity), sumKy = ws.NS_Walls.Values.Sum(w => w.WallRigidity);
            var blocks = new List<FrameworkElement>
            {
                WindReport.Heading("Rigid diaphragm calculation sheet"),
                WindReport.Para(
                    "Sign convention. Building axes: x to the right, y up, origin at the bottom-left of the plan; moments counterclockwise " +
                    "(seen from above) positive, M = (r × F)z = rx Fy − ry Fx (right-hand rule). Each case's Vx and Vy act at the center of " +
                    "mass (CM) and Mt is the torsion the case itself carries about the CM. E-W walls carry forces along x, N-S walls along y. " +
                    "Wall shears are the force each wall exerts on the diaphragm: they resist the load, so they are opposite in sign to it."),
                WindReport.Para(
                    "Formulas. k = 1 / (0.4 (h/L)³ + 0.3 (h/L)) (cantilever pier, relative rigidity). Center of rigidity (CR): " +
                    "x_CR = Σk x / Σk over the N-S walls, y_CR = Σk y / Σk over the E-W walls. x̄ = x − x_CR, ȳ = y − y_CR. " +
                    "J = Σ(E-W) k ȳ² + Σ(N-S) k x̄². Eccentricity ecc = CR − CM. Torsional moment about the CR: " +
                    "Mt_comb = Vx·ecc_y − Vy·ecc_x + Mt. Direct shear: −k·Vx / Σk (E-W), −k·Vy / Σk (N-S). " +
                    "Torsional shear (the wall's share of the force couple equal to Mt_comb): +k·Mt_comb·ȳ / J (E-W), −k·Mt_comb·x̄ / J (N-S). " +
                    "Total = direct + torsional."),
                WindReport.Para(
                    "Checks for every case: ΣFx = −Vx and ΣFy = −Vy (the walls react the load); the torsional shears alone sum to zero in x " +
                    "and in y (a couple, no net force); the moment of the wall shears about the CR, Σ(x̄·Fy − ȳ·Fx), equals −Mt_comb " +
                    "(the walls resist the torsion: the diaphragm's forces on the walls produce +Mt_comb)."),
            };

            var props = ws._walls.OrderBy(w => w.Key).Select(kv =>
            {
                var w = kv.Value;
                bool ew = w.WallDir == WallDirs.EastWest;
                double xb = ws.X_bar_walls.TryGetValue(kv.Key, out var a) ? a : double.NaN, yb = ws.Y_bar_walls.TryGetValue(kv.Key, out var b) ? b : double.NaN;
                double arm = ew ? yb : xb;
                return new[]
                {
                    kv.Key.ToString(), Dir(w), w.Center.X.ToString("0.00"), w.Center.Y.ToString("0.00"), w.WallLength.ToString("0.00"),
                    w.WallHeight.ToString("0.00"), w.WallRigidity.ToString("0.0000"), xb.ToString("0.000"), yb.ToString("0.000"),
                    (w.WallRigidity * arm * arm).ToString("0.000"),
                };
            }).ToList();
            blocks.Add(WindReport.Heading("Walls"));
            blocks.AddRange(WindReport.Tables(new[] { "Wall", "Dir.", "x (ft)", "y (ft)", "L (ft)", "h (ft)", "k", "x̄ (ft)", "ȳ (ft)", "k·arm² (ft²)" }, props));
            blocks.Add(WindReport.Para(
                $"Σk (E-W) = {sumKx:0.0000}, Σk (N-S) = {sumKy:0.0000}. CR = ({cr.X:0.000}, {cr.Y:0.000}) ft; CM = ({cm.X:0.000}, {cm.Y:0.000}) ft. " +
                $"J = {ws.InertiaXX:0.000} (E-W) + {ws.InertiaYY:0.000} (N-S) = {ws.InertiaPolar:0.000} ft². " +
                "The arm is ȳ for E-W walls and x̄ for N-S walls."));

            foreach (var c in calc.CaseResults)
            {
                var (fx, fy, tx, ty, m) = c.Checks(ws);
                blocks.Add(WindReport.Heading(c.Name));
                blocks.Add(WindReport.Para(
                    $"Vx = {c.Vx:0.000} k, Vy = {c.Vy:0.000} k, Mt = {c.Mt:0.00} k-ft; ecc_x = {c.EccX:0.000} ft, ecc_y = {c.EccY:0.000} ft. " +
                    $"Mt_comb = Vx·ecc_y − Vy·ecc_x + Mt = ({c.Vx:0.000})({c.EccY:0.000}) − ({c.Vy:0.000})({c.EccX:0.000}) + ({c.Mt:0.00}) = " +
                    $"{c.MomentFromVx:0.00} + {c.MomentFromVy:0.00} + {c.Mt:0.00} = {c.MtComb:0.00} k-ft ({(c.MtComb >= 0 ? "CCW" : "CW")})."));
                var rows = ws._walls.Keys.OrderBy(k => k).Where(c.Total.ContainsKey).Select(id => new[]
                {
                    id.ToString(), Dir(ws._walls[id]),
                    c.Direct.TryGetValue(id, out var d) ? d.ToString("0.000") : "-",
                    c.Torsional.TryGetValue(id, out var t) ? t.ToString("0.000") : "-",
                    c.Total[id].ToString("0.000"),
                }).ToList();
                blocks.AddRange(WindReport.Tables(new[] { "Wall", "Dir.", "Direct (k)", "Torsional (k)", "Total (k)" }, rows));
                blocks.Add(WindReport.Para(
                    $"Checks: ΣFx = {fx:0.000} (−Vx = {-c.Vx:0.000}); ΣFy = {fy:0.000} (−Vy = {-c.Vy:0.000}); torsional ΣFx = {tx:0.000}, ΣFy = {ty:0.000}; " +
                    $"Σ(x̄·Fy − ȳ·Fx) = {m:0.00} (−Mt_comb = {-c.MtComb:0.00})."));
            }
            return new ReportSection("Rigid diaphragm calculation sheet", blocks);
        }

        /// <summary>The flexible-diaphragm calculation sheet: method, warnings, then every case's line reactions and wall shares.</summary>
        private static ReportSection FlexibleSheet(ShearWallCalculator_FlexibleDiaphragm calc)
        {
            var ws = calc._wall_system;
            var box = calc.BoundingBoxWorld;
            var blocks = new List<FrameworkElement>
            {
                WindReport.Heading("Flexible diaphragm calculation sheet"),
                WindReport.Para(
                    "Sign convention. Building axes: x to the right, y up. E-W wall lines take the x loads, N-S wall lines the y loads. " +
                    "Line reactions and wall shears are the force on the diaphragm, opposite in sign to the load (as in the rigid calculation)."),
                WindReport.Para(
                    $"Method. Each load V is a uniform line load across the plan, w = V / {box.Height:0.##} ft for x loads (plan depth) and " +
                    $"w = V / {box.Width:0.##} ft for y loads (plan width). The diaphragm spans as simple beams between wall lines: each line " +
                    "takes w × its tributary width, from half way to the neighbouring line (or the plan edge for an outer line), so the " +
                    "reactions add up to −V. The walls on a line share its reaction by rigidity: share = reaction × k / Σk(line). " +
                    "An overhang beyond the outer line goes to that line as a force only (its small moment is not redistributed)."),
                WindReport.Para(
                    "Torsion (equivalent weights, WoodWorks \"Torsional analysis for flexible diaphragms\"). The rigid-diaphragm stiffness " +
                    "distribution is not used. Each line gets the weight K = its tributary share of the direct load (tributary width / plan " +
                    "dimension, so Σ K = 1 in each direction). Center of the weights: x_c = Σ K x (N-S lines), y_c = Σ K y (E-W lines); " +
                    "arms x̄ = x − x_c, ȳ = y − y_c; J = Σ K ȳ² (E-W) + Σ K x̄² (N-S), ft². Torsional force on the diaphragm for the case's " +
                    "torsion Mt (CCW +): E-W lines +K·Mt·ȳ / J, N-S lines −K·Mt·x̄ / J; shared by the walls on the line by rigidity, and added " +
                    "to the direct shear. These forces sum to zero in x and in y (a couple) and their moment about the center is −Mt. " +
                    "Assumption: the weights are normalized to 1 in each direction separately, which sets how Mt is shared between the x " +
                    "and y lines; a semi-rigid analysis would be needed to do better. The direct loads are uniform over the plan, so they " +
                    "add no moment about the center of a rectangular plan."),
            };
            foreach (var w in calc.Warnings) blocks.Add(WindReport.Para("Note: " + w));

            foreach (var c in calc.CaseResults)
            {
                blocks.Add(WindReport.Heading(c.Name));
                string torsion = Math.Abs(c.Mt) < 1e-9 ? "Mt = 0." :
                    c.TorsionApplied ? $"Mt = {c.Mt:0.00} k-ft; center ({c.CenterX:0.00}, {c.CenterY:0.00}) ft, J = {c.J:0.000} ft²." :
                    $"Mt = {c.Mt:0.00} k-ft, NOT applied (see the note).";
                blocks.Add(WindReport.Para($"Vx = {c.Vx:0.000} k, Vy = {c.Vy:0.000} k; " + torsion));
                foreach (var (lines, axis, v) in new[] { (c.LinesX, "y", c.Vx), (c.LinesY, "x", c.Vy) })
                {
                    if (lines == null || lines.Count == 0) continue;
                    var rows = new List<string[]>();
                    foreach (var l in lines)
                        foreach (var id in l.WallIds)
                        {
                            var wall = ws._walls[id];
                            rows.Add(new[]
                            {
                                l.Line.ToString(), $"{axis} = {l.Location:0.00}", $"{l.From:0.00} to {l.To:0.00}", l.Width.ToString("0.00"),
                                l.Reaction.ToString("0.000"), l.K.ToString("0.0000"), l.Arm.ToString("0.00"), l.Torsional.ToString("0.000"),
                                id.ToString(), wall.WallRigidity.ToString("0.0000"), l.SumRigidity.ToString("0.0000"),
                                c.Total.TryGetValue(id, out var s) ? s.ToString("0.000") : "-",
                            });
                        }
                    string bar = axis == "y" ? "ȳ" : "x̄";
                    blocks.AddRange(WindReport.Tables(new[] { "Line", "At (ft)", "Tributary (ft)", "Width (ft)", "Direct (k)", "K", $"{bar} (ft)", "Torsional (k)", "Wall", "k", "Σk line", "Wall shear (k)" }, rows));
                    blocks.Add(WindReport.Para($"w = {(lines.Count > 0 ? lines[0].W : 0):0.0000} k/ft. Check: Σ direct = {lines.Sum(l => l.Reaction):0.000} k (−V{(axis == "y" ? "x" : "y")} = {-v:0.000} k); " +
                                               $"Σ torsional = {lines.Sum(l => l.Torsional):0.000} k (0)."));
                }
                if (c.TorsionApplied && Math.Abs(c.Mt) > 1e-9)
                {
                    double m = c.LinesY.Sum(l => l.Arm * l.Torsional) - c.LinesX.Sum(l => l.Arm * l.Torsional);
                    blocks.Add(WindReport.Para($"Check: moment of the torsional forces about the center = Σ x̄·Fy − Σ ȳ·Fx = {m:0.000} k-ft (−Mt = {-c.Mt:0.000} k-ft)."));
                }
            }
            return new ReportSection("Flexible diaphragm calculation sheet", blocks);
        }

        /// <summary>The design checks: settings, a summary table, then every step for each wall.</summary>
        private static ReportSection DesignSection(ShearWallCalculatorBase calc, IReadOnlyDictionary<int, WallDesignResult> design, ShearWallDesigner designer)
        {
            var s = calc.DesignSettings ?? new DesignSettings();
            var blocks = new List<FrameworkElement>
            {
                WindReport.Heading("Shear wall design"),
                WindReport.Para($"{(s.Method == DesignMethods.ASD ? "ASD: walls 0.6W, capacity = nominal / 2.0" : "LRFD: walls 1.0W, capacity = 0.80 x nominal")} " +
                                $"({SdpwsSource.EditionName(s.Edition)}). Hold-downs are checked at ASD (0.6W) against Simpson allowable loads. " +
                                "Sheathing from SDPWS Tables 4.3A (wood structural panels), 4.3B (wood over gypsum) and 4.3C (gypsum, plaster), wind columns; " +
                                "each drawn wall is one full-height segment. " + SdpwsSource.TableSource),
                WindReport.Para(SheathingRules.TwoFaceRuleNote),
            };
            if (s.HoldownsFromStandardList)
            {
                var keys = designer?.StandardList.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList() ?? new List<string>();
                string version = designer == null ? "" : " — " + HoldownListRecord.Describe(designer.StandardList.CompanyVersion, designer.StandardList.CompanyDate, designer.StandardList.Customized);
                blocks.Add(WindReport.Para("Hold-downs selected from the standard list only" + version + " (the lightest adequate option of the wall's type; " +
                                           "no fall-back to the full catalog). Options in the list (Model#n = n-th fastener option of the model): " +
                                           (keys.Count == 0 ? "none." : string.Join(", ", keys) + ".")));
            }
            else
                blocks.Add(WindReport.Para("Hold-downs selected from the full catalog (the lightest adequate option of the wall's type)."));
            blocks.Add(WindReport.Para(ShearWallDesigner.CatalogFootnote));
            if (design.Values.Any(r => r.Holdown?.Conditions != null))
                blocks.Add(WindReport.Para("LSTHD / STHD strap-tie hold-downs: allowable loads from Simpson Strong-Tie " + ShearWallDesigner.CatalogSource(HoldownFamilies.STHD_ToFoundation) + ", \"Tension Loads " +
                                           "for STHD Installations\", for " + s.StrapHoldownsOrDefault().Describe() + " (f'c ≥ 2,500 psi; minimum spacing " +
                                           "3 × le; the LSTHD8 spall, two-pour and brick-ledge provisions are not applied)."));
            var rows = design.Keys.OrderBy(k => k).Select(k => design[k]).Select(r => new[]
            {
                r.WallId.ToString(), r.Panel, ShortRule(r.CombineRule),
                double.IsInfinity(r.Demand) ? "-" : r.Demand.ToString("0"), double.IsNaN(r.Capacity) ? "-" : r.Capacity.ToString("0"),
                double.IsNaN(r.Capacity) ? "-" : r.ShearRatio.ToString("0.00"), r.TensionAsd.ToString("0"),
                !r.HoldownNeeded ? "none" : r.Holdown?.ToString() ?? "none strong enough", r.Pass ? "OK" : r.Status,
            }).ToList();
            blocks.AddRange(WindReport.Tables(new[] { "Wall", "Sheathing", "Faces", "v (plf)", "Allowed", "Ratio", "T (lb)", "Hold-down", "Result" }, rows));
            foreach (var r in design.Keys.OrderBy(k => k).Select(k => design[k]))
                blocks.Add(WindReport.Para(string.Join(Environment.NewLine, WallDesignPanel.Explain(r))));
            return new ReportSection("Shear wall design", blocks);
        }

        /// <summary>The two-face rule in a few words for the summary table; the full rule is in each wall's steps.</summary>
        private static string ShortRule(string rule)
        {
            if (string.IsNullOrEmpty(rule)) return "-";
            if (rule.StartsWith("one face")) return "1";
            if (rule.StartsWith("same material")) return "2 × one";
            if (rule.EndsWith("stronger face")) return "stronger";
            if (rule.EndsWith("2 × weaker face")) return "2 × weaker";
            return rule;
        }
    }
}
