using calculator;
using ShearWallCalculator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ShearWallVisualizer.Controls
{
    /// <summary>
    /// The Calculations tab: how the lateral load is shared among the walls. A summary strip (loads, centers of mass and
    /// rigidity, J), then either the envelope over all load cases (the design shear of each wall) or one load case with its
    /// direct, torsional and total shears; for a flexible diaphragm also the wall lines with their tributary widths.
    /// The full step-by-step sheets are in File &gt; Print report.
    /// </summary>
    public class ShearResultsPanel : UserControl
    {
        /// <summary>A wall row was clicked (wall id).</summary>
        public event Action<int> WallSelected;

        /// <summary>The × on a wall row (wall id); the host deletes it.</summary>
        public event Action<int> WallDeleteRequested;

        private readonly SortState envelopeSort = new SortState(), caseSort = new SortState(), flexSort = new SortState(), linesSort = new SortState();

        private readonly StackPanel top = new StackPanel();
        private readonly ComboBox cbCase = new ComboBox { Width = 260, Margin = new Thickness(0, 0, 12, 0) };
        private readonly TextBlock tbCaseInfo = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Foreground = Ui.InkSecondary, TextWrapping = TextWrapping.Wrap };
        private readonly Grid tables = new Grid();
        private readonly DockPanel caseBar = new DockPanel { Margin = new Thickness(0, 10, 0, 8) };
        private ShearWallCalculatorBase calc;
        private int selectedWall = -1;
        private bool loading;
        private UiTable wallTable;

        public ShearResultsPanel()
        {
            FontFamily = new FontFamily("Segoe UI");
            FontSize = 12;
            Background = Ui.Surface;

            var label = new TextBlock { Text = "Show:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 6, 0) };
            DockPanel.SetDock(label, Dock.Left);
            DockPanel.SetDock(cbCase, Dock.Left);
            caseBar.Children.Add(label);
            caseBar.Children.Add(cbCase);
            caseBar.Children.Add(tbCaseInfo);
            cbCase.SelectionChanged += (s, e) => { if (!loading) BuildTables(); };

            var root = new DockPanel { Margin = new Thickness(12, 10, 12, 10) };
            DockPanel.SetDock(top, Dock.Top);
            DockPanel.SetDock(caseBar, Dock.Top);
            root.Children.Add(top);
            root.Children.Add(caseBar);
            root.Children.Add(tables);
            Content = root;
        }

        /// <summary>Multi-story: what to check about the levels (unsupported walls above, approximations), shown under the summary.</summary>
        public IList<string> Notes { get; set; }

        /// <summary>Shows the results. <paramref name="todo"/> (what to draw next) replaces them when the model cannot be calculated.</summary>
        public void Show(ShearWallCalculatorBase calculator, string todo, int selectedWallId)
        {
            calc = calculator;
            selectedWall = selectedWallId;
            top.Children.Clear();
            tables.Children.Clear();

            if (!string.IsNullOrEmpty(todo) || calc == null)
            {
                top.Children.Add(Ui.Banner(string.IsNullOrEmpty(todo) ? "Nothing to calculate yet." : todo, warning: false));
                caseBar.Visibility = Visibility.Collapsed;
                return;
            }
            caseBar.Visibility = Visibility.Visible;

            BuildSummary();

            string keep = cbCase.SelectedItem as string;
            loading = true;
            cbCase.Items.Clear();
            cbCase.Items.Add(EnvelopeName);
            foreach (var name in CaseNames()) cbCase.Items.Add(name);
            cbCase.SelectedItem = keep != null && cbCase.Items.Contains(keep) ? keep : EnvelopeName;
            loading = false;
            BuildTables();
        }

        private const string EnvelopeName = "All load cases (design shear)";

        private bool Rigid => calc is ShearWallCalculator_RigidDiaphragm;

        /// <summary>Envelope: both runs; their cases are listed as "Case (rigid)" and "Case (flexible)".</summary>
        private ShearWallCalculator_Envelope Env => calc as ShearWallCalculator_Envelope;

        private ShearWallCalculator_RigidDiaphragm RigidRun => calc as ShearWallCalculator_RigidDiaphragm ?? Env?.Rigid;
        private ShearWallCalculator_FlexibleDiaphragm FlexibleRun => calc as ShearWallCalculator_FlexibleDiaphragm ?? Env?.Flexible;

        private IEnumerable<string> CaseNames() =>
            Env != null ? Env.Rigid.CaseResults.Select(c => ShearWallCalculator_Envelope.Tagged(c.Name, ShearWallCalculator_Envelope.RigidTag))
                             .Concat(Env.Flexible.CaseResults.Select(c => ShearWallCalculator_Envelope.Tagged(c.Name, ShearWallCalculator_Envelope.FlexibleTag)))
            : calc is ShearWallCalculator_RigidDiaphragm r ? r.CaseResults.Select(c => c.Name)
            : calc is ShearWallCalculator_FlexibleDiaphragm f ? f.CaseResults.Select(c => c.Name)
            : Enumerable.Empty<string>();

        // ------------------------------------------------------------------ summary

        private void BuildSummary()
        {
            var strip = new WrapPanel();
            var ws = calc._wall_system;
            var ds = calc._diaphragm_system;
            int cases = Env != null ? Env.Rigid.CaseResults.Count : CaseNames().Count();
            strip.Children.Add(Ui.Figure("Diaphragm", Env != null ? "Envelope" : Rigid ? "Rigid" : "Flexible",
                Env != null ? $"Rigid and flexible both calculated; each wall takes the larger shear (rigid governs {Env.Governs.Count(g => g.Value == ShearWallCalculator_Envelope.RigidTag)} walls, flexible {Env.Governs.Count(g => g.Value == ShearWallCalculator_Envelope.FlexibleTag)})"
                : Rigid ? "Load shared by wall rigidity, plus torsion about the center of rigidity" : "Each wall line takes its tributary load, plus torsion by equivalent tributary weights"));
            strip.Children.Add(Ui.Figure("Largest load", $"Vx {Ui.Num(calc.V_x, "0.0")} k,  Vy {Ui.Num(calc.V_y, "0.0")} k",
                $"{cases} load case{(cases == 1 ? "" : "s")}; strength level"));
            strip.Children.Add(Ui.Figure("Center of mass", $"({Ui.Num(ds.CtrMass.X, "0.0")}, {Ui.Num(ds.CtrMass.Y, "0.0")}) ft", "Centroid of the diaphragm areas; the load acts here"));
            if (RigidRun != null)
            {
                strip.Children.Add(Ui.Figure("Center of rigidity", $"({Ui.Num(ws.CtrRigidity.X, "0.0")}, {Ui.Num(ws.CtrRigidity.Y, "0.0")}) ft", "Σ k·x / Σ k of the N-S walls, Σ k·y / Σ k of the E-W walls"));
                strip.Children.Add(Ui.Figure("Σ k (E-W, N-S)", $"{Ui.Num(ws.TotalRigidity_X, "0.00")},  {Ui.Num(ws.TotalRigidity_Y, "0.00")}", "Total rigidity of the walls in each direction"));
                strip.Children.Add(Ui.Figure("J", $"{Ui.Num(ws.InertiaPolar, "0")} ft²", "Polar moment of the wall rigidities about the center of rigidity, Σ k·d²"));
            }
            if (FlexibleRun != null)
            {
                var f = FlexibleRun;
                strip.Children.Add(Ui.Figure("Wall lines (E-W, N-S)", $"{f.LinesX.Count},  {f.LinesY.Count}"));
            }
            top.Children.Add(Ui.Card(strip, new Thickness(0, 0, 0, 0)));

            if (FlexibleRun is ShearWallCalculator_FlexibleDiaphragm flex)
                foreach (var w in flex.Warnings.Distinct())
                    top.Children.Add(new Border { Margin = new Thickness(0, 8, 0, 0), Child = Ui.Banner(w, warning: true) });
            foreach (var n in Notes ?? new List<string>())
                top.Children.Add(new Border { Margin = new Thickness(0, 8, 0, 0), Child = Ui.Banner(n, warning: true) });
        }

        // ------------------------------------------------------------------ tables

        private void BuildTables()
        {
            tables.Children.Clear();
            tables.RowDefinitions.Clear();
            string name = cbCase.SelectedItem as string;
            if (name == null || name == EnvelopeName) { Envelope(); return; }
            if (Env != null)
            {
                var rc = Env.Rigid.CaseResults.FirstOrDefault(c => ShearWallCalculator_Envelope.Tagged(c.Name, ShearWallCalculator_Envelope.RigidTag) == name);
                if (rc != null) { RigidCase(rc); return; }
                var fc = Env.Flexible.CaseResults.FirstOrDefault(c => ShearWallCalculator_Envelope.Tagged(c.Name, ShearWallCalculator_Envelope.FlexibleTag) == name);
                if (fc != null) FlexibleCase(fc);
            }
            else if (calc is ShearWallCalculator_RigidDiaphragm r) RigidCase(r.CaseResults.First(c => c.Name == name));
            else if (calc is ShearWallCalculator_FlexibleDiaphragm f) FlexibleCase(f.CaseResults.First(c => c.Name == name));
        }

        /// <summary>A wall table: click a row to select the wall, × to delete it (the last column is added here).</summary>
        private UiTable NewWallTable(SortState sort, params (string, double, string)[] columns)
        {
            var t = new UiTable(sort, columns.Concat(new[] { ("", 30.0, "") }).ToArray());
            t.RowClicked += id =>
            {
                selectedWall = id;
                t.Select(id);
                WallSelected?.Invoke(id);
            };
            return t;
        }

        private void AddDelete(UiTable t, int row, int col, int id) =>
            t.Put(row, col, Ui.DeleteButton(() => Dispatcher.BeginInvoke(new Action(() => WallDeleteRequested?.Invoke(id)),
                System.Windows.Threading.DispatcherPriority.Background), $"Delete wall {id}"));

        private IEnumerable<KeyValuePair<int, WallData>> Walls() => calc._wall_system._walls.OrderBy(k => k.Key);

        private static string Dir(WallData w) => w.WallDir == WallDirs.EastWest ? "E-W" : "N-S";

        private void Envelope()
        {
            var env = Env;
            tbCaseInfo.Text = "Per wall, the most positive and most negative shear over every case. The design shear is the larger magnitude. " +
                              (env != null ? "Envelope: over the cases of both the rigid and the flexible calculation; each case says which. " : "") +
                              "Signs: the wall's force on the diaphragm (x right, y up).";
            var columns = new List<(string, double, string)>
            {
                ("Wall", 44, ""), ("Dir", 40, ""), ("L (ft)", 52, "Wall length"), ("k", 54, "Relative rigidity"),
                ("Most + (k)", 70, "Most positive shear over all cases"), ("Case", 150, ""),
                ("Most − (k)", 70, "Most negative shear over all cases"), ("Case", 150, ""),
            };
            if (env != null)
            {
                columns.Add(("Rigid V (k)", 70, "The rigid-diaphragm design shear, max |V| over its cases"));
                columns.Add(("Flexible V (k)", 80, "The flexible-diaphragm design shear, max |V| over its cases"));
            }
            columns.Add(("Design V (k)", 80, "max(|most +|, |most −|): goes to the Design tab"));
            wallTable = NewWallTable(envelopeSort, columns.ToArray());
            foreach (var kv in Walls())
            {
                int id = kv.Key;
                var w = kv.Value;
                calc.WallShearRange.TryGetValue(id, out var range);
                int r = wallTable.AddRow(id, id == selectedWall);
                int c = 0;
                wallTable.Text(r, c++, id.ToString(), id, bold: true, right: false);
                wallTable.Text(r, c++, Dir(w), id, Ui.InkSecondary, right: false);
                wallTable.Text(r, c++, Ui.Num(w.WallLength), id);
                wallTable.Text(r, c++, Ui.Num(w.WallRigidity, "0.000"), id);
                wallTable.Text(r, c++, range == null ? "–" : Ui.Num(range.MaxPositive), id);
                wallTable.Text(r, c++, range?.MaxPositiveCase ?? "", id, Ui.InkSecondary, right: false, tip: range?.MaxPositiveCase);
                wallTable.Text(r, c++, range == null ? "–" : Ui.Num(range.MaxNegative), id);
                wallTable.Text(r, c++, range?.MaxNegativeCase ?? "", id, Ui.InkSecondary, right: false, tip: range?.MaxNegativeCase);
                if (env != null)
                {
                    string Run(ShearWallCalculatorBase run) => run.WallShearRange.TryGetValue(id, out var x) ? Ui.Num(x.Design) : "–";
                    bool rigidGoverns = env.Governs.TryGetValue(id, out var g) && g == ShearWallCalculator_Envelope.RigidTag;
                    wallTable.Text(r, c++, Run(env.Rigid), id, bold: rigidGoverns);
                    wallTable.Text(r, c++, Run(env.Flexible), id, bold: !rigidGoverns && g != null);
                }
                wallTable.Text(r, c++, range == null ? "–" : Ui.Num(range.Design), id, bold: true,
                    tip: range == null ? null : "Governing case: " + range.DesignCase);
                AddDelete(wallTable, r, c, id);
            }
            Place(wallTable.InFrame(), 0);
        }

        /// <summary>Multi-story: the part of a case that comes from the walls of the level above (empty when none).</summary>
        private string AboveText(string caseName, ShearWallCalculatorBase run)
        {
            var c = (run ?? calc)?.CasesToAnalyse().FirstOrDefault(x => x.Name == caseName);
            if (c == null || (c.AboveVx == 0 && c.AboveVy == 0 && c.AboveMt == 0)) return "";
            return $".   From the level above (its walls standing on this floor): Vx {Ui.Num(c.AboveVx)} k,  Vy {Ui.Num(c.AboveVy)} k,  Mt {Ui.Num(c.AboveMt, "0.0")} k·ft about the center of mass";
        }

        private void RigidCase(RigidCaseResult rc)
        {
            tbCaseInfo.Text = $"Vx {Ui.Num(rc.Vx)} k,  Vy {Ui.Num(rc.Vy)} k,  Mt {Ui.Num(rc.Mt, "0.0")} k·ft,  e = ({Ui.Num(rc.EccX)}, {Ui.Num(rc.EccY)}) ft,  " +
                              $"Mt,comb = Vx·ey − Vy·ex + Mt = {Ui.Num(rc.MtComb, "0.0")} k·ft" + AboveText(rc.Name, RigidRun);
            var ws = calc._wall_system;
            wallTable = NewWallTable(caseSort,
                ("Wall", 44, ""), ("Dir", 40, ""), ("k", 54, "Relative rigidity"),
                ("x̄ (ft)", 58, "Wall center x − center of rigidity x"), ("ȳ (ft)", 58, "Wall center y − center of rigidity y"),
                ("Direct (k)", 70, "−k·V / Σk"), ("Torsion (k)", 74, "E-W: +k·Mt,comb·ȳ / J;  N-S: −k·Mt,comb·x̄ / J"), ("Total (k)", 70, ""));
            foreach (var kv in Walls())
            {
                int id = kv.Key;
                int r = wallTable.AddRow(id, id == selectedWall);
                int c = 0;
                wallTable.Text(r, c++, id.ToString(), id, bold: true, right: false);
                wallTable.Text(r, c++, Dir(kv.Value), id, Ui.InkSecondary, right: false);
                wallTable.Text(r, c++, Ui.Num(kv.Value.WallRigidity, "0.000"), id);
                wallTable.Text(r, c++, ws.X_bar_walls.TryGetValue(id, out var xb) ? Ui.Num(xb) : "–", id);
                wallTable.Text(r, c++, ws.Y_bar_walls.TryGetValue(id, out var yb) ? Ui.Num(yb) : "–", id);
                wallTable.Text(r, c++, rc.Direct != null && rc.Direct.TryGetValue(id, out var d) ? Ui.Num(d) : "–", id);
                wallTable.Text(r, c++, rc.Torsional != null && rc.Torsional.TryGetValue(id, out var t) ? Ui.Num(t) : "–", id);
                wallTable.Text(r, c++, rc.Total != null && rc.Total.TryGetValue(id, out var tot) ? Ui.Num(tot) : "–", id, bold: true);
                AddDelete(wallTable, r, c, id);
            }
            Place(wallTable.InFrame(), 0);
        }

        private void FlexibleCase(FlexibleCaseResult fc)
        {
            tbCaseInfo.Text = $"Vx {Ui.Num(fc.Vx)} k,  Vy {Ui.Num(fc.Vy)} k,  Mt {Ui.Num(fc.Mt, "0.0")} k·ft" +
                              (fc.TorsionApplied ? $",  torsion center ({Ui.Num(fc.CenterX, "0.0")}, {Ui.Num(fc.CenterY, "0.0")}) ft,  J = Σ K·d² = {Ui.Num(fc.J, "0.0")} ft²" : "") + AboveText(fc.Name, FlexibleRun);

            var lines = new UiTable(linesSort,
                ("Line", 40, ""), ("Dir", 40, ""), ("At (ft)", 56, "Line position across the load: y for E-W lines, x for N-S lines"),
                ("Tributary (ft)", 104, "Half way to each neighbouring line"), ("w (k/ft)", 62, "V / plan dimension"),
                ("Share (k)", 66, "−w × tributary width"), ("K", 50, "Tributary share (Σ K = 1 per direction)"),
                ("Arm (ft)", 60, "From the torsion center"), ("Torsion (k)", 74, ""), ("Walls", 90, ""));
            void AddLines(IEnumerable<LineReaction> list, string dir)
            {
                foreach (var l in list ?? Enumerable.Empty<LineReaction>())
                {
                    int r = lines.AddRow(l.Line, false);
                    int c = 0;
                    lines.Text(r, c++, l.Line.ToString(), l.Line, bold: true, right: false);
                    lines.Text(r, c++, dir, l.Line, Ui.InkSecondary, right: false);
                    lines.Text(r, c++, Ui.Num(l.Location, "0.0"), l.Line);
                    lines.Text(r, c++, $"{Ui.Num(l.From, "0.0")} – {Ui.Num(l.To, "0.0")}", l.Line);
                    lines.Text(r, c++, Ui.Num(l.W, "0.000"), l.Line);
                    lines.Text(r, c++, Ui.Num(l.Reaction), l.Line);
                    lines.Text(r, c++, Ui.Num(l.K, "0.000"), l.Line);
                    lines.Text(r, c++, Ui.Num(l.Arm, "0.0"), l.Line);
                    lines.Text(r, c++, Ui.Num(l.Torsional), l.Line);
                    lines.Text(r, c++, string.Join(", ", l.WallIds ?? new List<int>()), l.Line, Ui.InkSecondary, right: false);
                }
            }
            AddLines(fc.LinesX, "E-W");
            AddLines(fc.LinesY, "N-S");

            wallTable = NewWallTable(flexSort,
                ("Wall", 44, ""), ("Dir", 40, ""), ("k", 54, "Relative rigidity (splits a line's force among its walls)"),
                ("Direct (k)", 70, "The line's share × k / Σk of the line"), ("Torsion (k)", 74, ""), ("Total (k)", 70, ""));
            foreach (var kv in Walls())
            {
                int id = kv.Key;
                double total = fc.Total != null && fc.Total.TryGetValue(id, out var t) ? t : double.NaN;
                double tors = fc.Torsional != null && fc.Torsional.TryGetValue(id, out var q) ? q : 0;
                int r = wallTable.AddRow(id, id == selectedWall);
                int c = 0;
                wallTable.Text(r, c++, id.ToString(), id, bold: true, right: false);
                wallTable.Text(r, c++, Dir(kv.Value), id, Ui.InkSecondary, right: false);
                wallTable.Text(r, c++, Ui.Num(kv.Value.WallRigidity, "0.000"), id);
                wallTable.Text(r, c++, Ui.Num(total - tors), id);
                wallTable.Text(r, c++, fc.TorsionApplied ? Ui.Num(tors) : "–", id);
                wallTable.Text(r, c++, Ui.Num(total), id, bold: true);
                AddDelete(wallTable, r, c, id);
            }

            var linesBlock = new DockPanel { Margin = new Thickness(0, 0, 0, 10) };
            var lt = Ui.Heading("Wall lines");
            DockPanel.SetDock(lt, Dock.Top);
            linesBlock.Children.Add(lt);
            linesBlock.Children.Add(lines.InFrame());
            var wallsBlock = new DockPanel();
            var wt = Ui.Heading("Walls");
            DockPanel.SetDock(wt, Dock.Top);
            wallsBlock.Children.Add(wt);
            wallsBlock.Children.Add(wallTable.InFrame());
            Place(linesBlock, 0, auto: true);
            Place(wallsBlock, 1);
        }

        private void Place(UIElement e, int row, bool auto = false)
        {
            while (tables.RowDefinitions.Count <= row)
                tables.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            if (auto) tables.RowDefinitions[row].Height = new GridLength(1, GridUnitType.Auto);
            if (auto && e is FrameworkElement fe) fe.MaxHeight = 300;
            Grid.SetRow(e, row);
            tables.Children.Add(e);
        }
    }
}
