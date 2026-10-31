using calculator;
using ShearWallCalculator;
using ShearWallCalculator.Design;
using ShearWallCalculator.NDSProvisions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace ShearWallVisualizer.Controls
{
    /// <summary>
    /// The Design tab: project settings (ASD/LRFD, SDPWS edition), one row per wall with its sheathing, nailing and
    /// hold-down choices and the results, and every equation for the selected wall. Changing a choice raises
    /// <see cref="DesignChanged"/>; the host recalculates and calls <see cref="Show"/>.
    /// </summary>
    public class WallDesignPanel : UserControl
    {
        public event EventHandler DesignChanged;
        public event EventHandler AutoSelectRequested;

        private static readonly Brush Ink = Frozen("#0B0B0B"), InkSecondary = Frozen("#52514E"), Hairline = Frozen("#D5DAE1"),
                                      HeaderFill = Frozen("#EEF1F5"), PassInk = Frozen("#1E6B33"), FailInk = Frozen("#B3261E"),
                                      PassFill = Frozen("#E7F4EA"), FailFill = Frozen("#FCE8E6"), SelectedFill = Frozen("#E4EDFA"),
                                      Surface = Frozen("#F7F8FA");

        private readonly RadioButton rbAsd = new RadioButton { Content = "ASD (0.6W)", Margin = new Thickness(0, 0, 12, 0), VerticalAlignment = VerticalAlignment.Center };
        private readonly RadioButton rbLrfd = new RadioButton { Content = "LRFD (1.0W)", Margin = new Thickness(0, 0, 20, 0), VerticalAlignment = VerticalAlignment.Center };
        private readonly ComboBox cbEdition = new ComboBox { Width = 130, Margin = new Thickness(0, 0, 20, 0) };
        private readonly Button btnAuto = new Button { Content = "Auto-select nailing (all walls)", Padding = new Thickness(10, 3, 10, 3) };
        private readonly ComboBox cbPool = new ComboBox { Width = 150, Margin = new Thickness(0, 0, 20, 0), ToolTip = "Standard list: only the hold-downs checked as company standard in View > Hold-down catalog. Full catalog: every option." };
        private readonly CheckBox cbSumFaces = new CheckBox
        {
            Content = "Add wood + gypsum faces (wind)", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 16, 0),
            ToolTip = "SDPWS 4.3.5.4.2 exception, wind only: a wood structural panel or structural fiberboard face plus a gypsum wallboard face may be added.\n" +
                      "Off: the larger of the stronger face and twice the weaker. Same material on both faces is always 2 × one face.",
        };
        private readonly TextBlock tbSummary = new TextBlock { FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(16, 0, 0, 0) };
        private readonly TextBlock tbNote = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = InkSecondary, Margin = new Thickness(0, 6, 0, 8) };
        private readonly Grid table = new Grid();
        private readonly StackPanel details = new StackPanel();
        private readonly Border detailsCard;

        private ShearWallCalculatorBase calc;
        private IReadOnlyDictionary<int, WallDesignResult> results = new Dictionary<int, WallDesignResult>();
        private ShearWallDesigner designer;
        private int selectedWall = -1;
        private bool loading;

        public WallDesignPanel()
        {
            FontFamily = new FontFamily("Segoe UI");
            FontSize = 12;
            Background = Surface;

            string group = "method" + Guid.NewGuid().ToString("N"); // unique per instance (window-scoped groups)
            rbAsd.GroupName = rbLrfd.GroupName = group;
            cbEdition.Items.Add(new ComboBoxItem { Content = "SDPWS 2021", Tag = SdpwsEditions.SDPWS_2021 });
            cbEdition.Items.Add(new ComboBoxItem { Content = "SDPWS 2015", Tag = SdpwsEditions.SDPWS_2015 });
            rbAsd.Checked += (s, e) => SettingsChanged();
            rbLrfd.Checked += (s, e) => SettingsChanged();
            cbEdition.SelectionChanged += (s, e) => SettingsChanged();
            cbPool.Items.Add("Full catalog");
            cbPool.Items.Add("Standard list");
            cbPool.SelectionChanged += (s, e) => SettingsChanged();
            cbSumFaces.Checked += (s, e) => SettingsChanged();
            cbSumFaces.Unchecked += (s, e) => SettingsChanged();
            btnAuto.Click += (s, e) => AutoSelectRequested?.Invoke(this, EventArgs.Empty);

            var bar = new WrapPanel { Margin = new Thickness(0, 0, 0, 2) };
            bar.Children.Add(Label("Design method:"));
            bar.Children.Add(rbAsd);
            bar.Children.Add(rbLrfd);
            bar.Children.Add(Label("Wood code:"));
            bar.Children.Add(cbEdition);
            bar.Children.Add(Label("Hold-downs:"));
            bar.Children.Add(cbPool);
            bar.Children.Add(cbSumFaces);
            bar.Children.Add(btnAuto);
            bar.Children.Add(tbSummary);

            detailsCard = new Border
            {
                Background = Brushes.White, BorderBrush = Hairline, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 8, 12, 10), Margin = new Thickness(0, 10, 0, 0), Child = details,
            };

            var root = new DockPanel { Margin = new Thickness(12, 10, 12, 10), LastChildFill = true };
            DockPanel.SetDock(bar, Dock.Top);
            DockPanel.SetDock(tbNote, Dock.Top);
            DockPanel.SetDock(detailsCard, Dock.Bottom);
            root.Children.Add(bar);
            root.Children.Add(tbNote);
            root.Children.Add(detailsCard);
            root.Children.Add(new ScrollViewer
            {
                Content = new Border { Background = Brushes.White, BorderBrush = Hairline, BorderThickness = new Thickness(1), Child = table, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top },
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            });
            Content = root;
        }

        /// <summary>Shows the walls and results. <paramref name="message"/> replaces the table when there is nothing to design.</summary>
        public void Show(ShearWallCalculatorBase calculator, IReadOnlyDictionary<int, WallDesignResult> designResults, ShearWallDesigner shearWallDesigner,
                         bool loadsFromWind, string message)
        {
            calc = calculator;
            results = designResults ?? new Dictionary<int, WallDesignResult>();
            designer = shearWallDesigner;

            loading = true;
            var settings = calc?.DesignSettings ?? new DesignSettings();
            (settings.Method == DesignMethods.LRFD ? rbLrfd : rbAsd).IsChecked = true;
            cbEdition.SelectedItem = cbEdition.Items.OfType<ComboBoxItem>().First(i => (SdpwsEditions)i.Tag == settings.Edition);
            int listed = designer?.StandardList.Count ?? 0;
            cbPool.Items[1] = $"Standard list ({listed})";
            cbPool.SelectedIndex = settings.HoldownsFromStandardList ? 1 : 0;
            cbSumFaces.IsChecked = settings.SumWindFaces;
            loading = false;

            tbNote.Text = (loadsFromWind ? "Wall shears from the ASCE 7 wind load cases (strength level). " : "Wall shears from MANUAL loads, taken as strength-level wind. ") +
                          (settings.Method == DesignMethods.ASD ? "ASD: walls get 0.6W, capacity = nominal / 2.0. " : "LRFD: walls get 1.0W, capacity = 0.80 × nominal. ") +
                          "Hold-downs are always checked at ASD (0.6W) against Simpson allowable loads. Sheathing from SDPWS Tables 4.3A, 4.3B and 4.3C, " +
                          "one or two faces; each drawn wall is one full-height segment. " +
                          (calculator is ShearWallCalculator_FlexibleDiaphragm
                              ? "Flexible diaphragm: each wall line takes its tributary load, plus the case torsion by equivalent tributary weights, split among its walls by rigidity. "
                              : "Rigid diaphragm: load shared by wall rigidity, plus torsion. ") + SdpwsSource.TableSource;

            table.Children.Clear();
            table.RowDefinitions.Clear();
            table.ColumnDefinitions.Clear();
            details.Children.Clear();

            if (message != null || calc == null || designer == null)
            {
                tbSummary.Text = string.Empty;
                btnAuto.IsEnabled = false;
                table.Children.Add(new TextBlock { Text = message ?? "Nothing to design yet.", Margin = new Thickness(12), TextWrapping = TextWrapping.Wrap, MaxWidth = 700 });
                detailsCard.Visibility = Visibility.Collapsed;
                return;
            }

            btnAuto.IsEnabled = true;
            int pass = results.Values.Count(r => r.Pass);
            tbSummary.Text = $"{pass} of {results.Count} walls pass";
            tbSummary.Foreground = pass == results.Count ? PassInk : FailInk;
            BuildTable();
            if (!results.ContainsKey(selectedWall) && results.Count > 0) // start on the first wall that fails
            {
                var failing = results.Keys.OrderBy(k => k).Where(k => !results[k].Pass).ToList();
                selectedWall = failing.Count > 0 ? failing[0] : results.Keys.Min();
            }
            ShowDetails();
        }

        // ------------------------------------------------------------------ table

        private static readonly (string header, double width, string tip)[] Columns =
        {
            ("Wall", 50, "Wall number and direction"),
            ("L (ft)", 42, "Wall length"),
            ("V (k)", 44, "Largest shear on the wall over all load cases, strength level"),
            ("Face 1", 134, "Sheathing on one face: any row of SDPWS Table 4.3A (wood panels), 4.3B (wood over gypsum) or 4.3C (gypsum, plaster)"),
            ("Nails", 46, "Edge fastener spacing (Tables 4.3A/B); Table 4.3C rows carry their own spacing"),
            ("Face 2", 134, "The other face: none, the same as face 1, or another material (two-face rule in the details below)"),
            ("Nails", 46, "Edge fastener spacing of face 2"),
            ("Framing", 64, "Framing species group (Table 4.3A footnote)"),
            ("Dead (plf)", 46, "Dead load on the wall that resists overturning (0 = none)"),
            ("Hold-down type", 70, "HDU or HTT anchors the wall end to the foundation; a strap ties it to the wall below"),
            ("v (plf)", 48, "Unit shear demand"),
            ("Allowed", 56, "Design unit shear capacity"),
            ("Ratio", 44, "Demand / capacity"),
            ("T (lb)", 50, "Hold-down tension at ASD"),
            ("Hold-down", 84, "Lightest Simpson model at or above T"),
            ("Status", 80, ""),
        };

        private void BuildTable()
        {
            foreach (var c in Columns) table.ColumnDefinitions.Add(c.header == "Status"
                ? new ColumnDefinition { Width = GridLength.Auto, MinWidth = c.width }   // the status text is never cut off
                : new ColumnDefinition { Width = new GridLength(c.width) });
            table.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            for (int c = 0; c < Columns.Length; c++)
            {
                var h = new Border
                {
                    Background = HeaderFill, BorderBrush = Hairline, BorderThickness = new Thickness(0, 0, 0, 1), Padding = new Thickness(4, 5, 4, 5),
                    Child = new TextBlock { Text = Columns[c].header, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap },
                    ToolTip = string.IsNullOrEmpty(Columns[c].tip) ? null : Columns[c].tip,
                };
                Grid.SetColumn(h, c);
                table.Children.Add(h);
            }

            int row = 1;
            foreach (var id in results.Keys.OrderBy(k => k))
            {
                var r = results[id];
                var wall = calc._wall_system._walls[id];
                table.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var back = new Border { Background = id == selectedWall ? SelectedFill : Brushes.Transparent, BorderBrush = Hairline, BorderThickness = new Thickness(0, 0, 0, 1), Tag = id };
                Grid.SetRow(back, row);
                Grid.SetColumnSpan(back, Columns.Length);
                back.MouseLeftButtonDown += (s, e) => Select((int)((Border)s).Tag);
                table.Children.Add(back);

                int col = 0;
                Cell(row, col++, $"{id} ({(wall.WallDir == WallDirs.EastWest ? "E-W" : "N-S")})", id);
                Cell(row, col++, r.Length.ToString("0.00"), id);
                Cell(row, col++, r.V.ToString("0.00"), id);

                var d = wall.Design ?? (wall.Design = new WallDesignInputs());
                var f1 = d.EffectiveFace1();
                var f2 = d.EffectiveFace2();
                bool same = f2 != null && f1.SameConstructionAs(f2);
                var o1 = designer.FindOption(f1);
                Put(row, col++, FaceCombo(designer.SheathingOptions.Cast<object>(), o1, o =>
                {
                    var nf1 = ((SheathingOption)o).ToFace(f1.EdgeSpacing > 0 ? f1.EdgeSpacing : 6);
                    if (!((SheathingOption)o).UsesEdgeSpacing) nf1.EdgeSpacing = 0;
                    else if (double.IsNaN(((SheathingOption)o).NominalWind(nf1.EdgeSpacing))) nf1.EdgeSpacing = FirstSpacing((SheathingOption)o);
                    d.SetFaces(nf1, same ? nf1 : f2);
                }, id));
                Put(row, col++, SpacingCell(o1, f1, s => { var n = f1.Clone(); n.EdgeSpacing = s; d.SetFaces(n, same ? n : f2); }, id));

                var face2Items = new List<object> { NoFace, SameFace };
                face2Items.AddRange(designer.SheathingOptions);
                var o2 = f2 == null ? null : designer.FindOption(f2);
                Put(row, col++, FaceCombo(face2Items, f2 == null ? (object)NoFace : same ? SameFace : (object)o2, o =>
                {
                    if (Equals(o, NoFace)) d.SetFaces(f1, null);
                    else if (Equals(o, SameFace)) d.SetFaces(f1, f1);
                    else
                    {
                        var opt = (SheathingOption)o;
                        var nf2 = opt.ToFace(opt.UsesEdgeSpacing ? (double.IsNaN(opt.NominalWind(6)) ? FirstSpacing(opt) : 6) : 0);
                        d.SetFaces(f1, nf2);
                    }
                }, id));
                if (f2 == null || same) Cell(row, col++, same ? "same" : "", id, InkSecondary);
                else Put(row, col++, SpacingCell(o2, f2, s => { var n = f2.Clone(); n.EdgeSpacing = s; d.SetFaces(f1, n); }, id));
                Put(row, col++, Combo(new object[] { "DF / SP", "SPF / HF" }, d.Species == WoodTypes.WOODTYPE_SPF_HF ? "SPF / HF" : "DF / SP",
                    o => d.Species = (string)o == "SPF / HF" ? WoodTypes.WOODTYPE_SPF_HF : WoodTypes.WOODTYPE_DF_SP, id));
                Put(row, col++, DeadLoadBox(d, id));
                Put(row, col++, Combo(new object[] { ShortHoldownName(HoldownFamilies.HDU_ToFoundation), ShortHoldownName(HoldownFamilies.HTT_ToFoundation), ShortHoldownName(HoldownFamilies.Strap_ToWallBelow) },
                    ShortHoldownName(d.Holdown), o => d.Holdown = Enum.GetValues(typeof(HoldownFamilies)).Cast<HoldownFamilies>().First(f => ShortHoldownName(f) == (string)o), id));

                Cell(row, col++, double.IsInfinity(r.Demand) ? "-" : r.Demand.ToString("0"), id);
                Cell(row, col++, double.IsNaN(r.Capacity) ? "-" : r.Capacity.ToString("0"), id);
                Cell(row, col++, double.IsNaN(r.Capacity) || double.IsInfinity(r.ShearRatio) ? "-" : r.ShearRatio.ToString("0.00"), id, r.ShearOk ? Ink : FailInk, bold: !r.ShearOk);
                Cell(row, col++, r.TensionAsd.ToString("0"), id);
                Cell(row, col++, !r.HoldownNeeded ? "no uplift" : r.Holdown?.Model ?? "none fits", id, r.HoldownOk ? Ink : FailInk);
                var status = new Border
                {
                    Background = r.Pass ? PassFill : FailFill, CornerRadius = new CornerRadius(4), Margin = new Thickness(3, 3, 3, 3), Padding = new Thickness(6, 2, 6, 2),
                    Child = new TextBlock { Text = (r.Pass ? "✓ " : "✗ ") + r.Status, Foreground = r.Pass ? PassInk : FailInk, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis },
                    ToolTip = r.Status, Tag = id,
                };
                status.MouseLeftButtonDown += (s, e) => Select((int)((Border)s).Tag);
                Put(row, col++, status);
                row++;
            }
        }

        /// <summary>Selects a wall from outside (the drawing's card); ignored if the wall has no result.</summary>
        public void SelectWall(int id)
        {
            if (results.ContainsKey(id)) Select(id);
        }

        private void Select(int id)
        {
            if (selectedWall == id) return;
            selectedWall = id;
            foreach (var b in table.Children.OfType<Border>().Where(b => Grid.GetColumnSpan(b) == Columns.Length && b.Tag is int))
                b.Background = (int)b.Tag == id ? SelectedFill : Brushes.Transparent;
            ShowDetails();
        }

        private void Cell(int row, int col, string text, int id, Brush brush = null, bool bold = false)
        {
            var t = new TextBlock
            {
                Text = text, Foreground = brush ?? Ink, FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal, Margin = new Thickness(4, 0, 4, 0),
                VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis, Tag = id, Background = Brushes.Transparent,
            };
            t.MouseLeftButtonDown += (s, e) => Select(id);
            Put(row, col, t);
        }

        private void Put(int row, int col, UIElement e)
        {
            Grid.SetRow(e, row);
            Grid.SetColumn(e, col);
            table.Children.Add(e);
        }

        private ComboBox Combo(IEnumerable<object> items, object selected, Action<object> apply, int id)
        {
            var cb = new ComboBox { Margin = new Thickness(2, 3, 2, 3), FontSize = 12, Padding = new Thickness(4, 1, 2, 1) };
            foreach (var i in items) cb.Items.Add(i);
            cb.SelectedItem = cb.Items.Cast<object>().FirstOrDefault(i => Equals(i, selected) || (i is PanelOption p && selected is PanelOption q && p.Name == q.Name));
            cb.GotFocus += (s, e) => Select(id);
            cb.SelectionChanged += (s, e) =>
            {
                if (loading || cb.SelectedItem == null) return;
                apply(cb.SelectedItem);
                selectedWall = id;
                DesignChanged?.Invoke(this, EventArgs.Empty);
            };
            return cb;
        }

        private const string NoFace = "None";
        private const string SameFace = "Same as face 1";

        /// <summary>A face choice: the short name in the box, the full description as its tooltip.</summary>
        private ComboBox FaceCombo(IEnumerable<object> items, object selected, Action<object> apply, int id)
        {
            var cb = Combo(items, selected, apply, id);
            cb.ToolTip = (selected as SheathingOption)?.Description ?? selected?.ToString();
            cb.MaxDropDownHeight = 420;
            return cb;
        }

        private static int FirstSpacing(SheathingOption o) =>
            ShearWallDesigner.EdgeSpacings.Where(s => !double.IsNaN(o.NominalWind(s))).DefaultIfEmpty(6).First();

        /// <summary>Edge spacing for a Table 4.3A/B face (only the spacings the table has); the row's own spacing for Table 4.3C.</summary>
        private UIElement SpacingCell(SheathingOption o, FaceSheathing f, Action<int> apply, int id)
        {
            if (o == null) return new TextBlock { Text = "-", Margin = new Thickness(4, 0, 4, 0), VerticalAlignment = VerticalAlignment.Center };
            if (!o.UsesEdgeSpacing)
                return new TextBlock
                {
                    Text = string.IsNullOrEmpty(o.RowSpacing) ? "-" : o.RowSpacing.Split(' ', '\n')[0] + (o.RowSpacing.All(ch => char.IsDigit(ch) || ch == '.') ? "\"" : ""),
                    Foreground = InkSecondary, Margin = new Thickness(4, 0, 4, 0), VerticalAlignment = VerticalAlignment.Center,
                    ToolTip = "Fastener spacing of this Table 4.3C row: " + o.RowSpacing,
                };
            var spacings = ShearWallDesigner.EdgeSpacings.Where(s => !double.IsNaN(o.NominalWind(s))).Select(s => (object)$"{s}\"");
            return Combo(spacings, $"{f.EdgeSpacing}\"", v => apply(int.Parse(((string)v).TrimEnd('"'), CultureInfo.InvariantCulture)), id);
        }

        private TextBox DeadLoadBox(WallDesignInputs d, int id)
        {
            var tb = new TextBox { Text = d.DeadLoad.ToString("0.#"), Margin = new Thickness(2, 3, 2, 3), VerticalContentAlignment = VerticalAlignment.Center, TextAlignment = TextAlignment.Right };
            void Commit()
            {
                if (!double.TryParse(tb.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out double v) || v < 0)
                {
                    tb.Text = d.DeadLoad.ToString("0.#"); // keep the last good value
                    return;
                }
                if (Math.Abs(v - d.DeadLoad) < 1e-9) return;
                d.DeadLoad = v;
                selectedWall = id;
                DesignChanged?.Invoke(this, EventArgs.Empty);
            }
            tb.GotFocus += (s, e) => { Select(id); tb.SelectAll(); };
            tb.LostFocus += (s, e) => Commit();
            tb.KeyDown += (s, e) => { if (e.Key == Key.Enter) Commit(); };
            return tb;
        }

        private void SettingsChanged()
        {
            if (loading || calc == null) return;
            calc.DesignSettings = calc.DesignSettings ?? new DesignSettings();
            calc.DesignSettings.Method = rbLrfd.IsChecked == true ? DesignMethods.LRFD : DesignMethods.ASD;
            if (cbEdition.SelectedItem is ComboBoxItem item) calc.DesignSettings.Edition = (SdpwsEditions)item.Tag;
            calc.DesignSettings.HoldownsFromStandardList = cbPool.SelectedIndex == 1;
            calc.DesignSettings.SumWindFaces = cbSumFaces.IsChecked == true;
            DesignChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>The table's short label (the column tooltip says where each one anchors).</summary>
        public static string ShortHoldownName(HoldownFamilies f) =>
            f == HoldownFamilies.HDU_ToFoundation ? "HDU" : f == HoldownFamilies.HTT_ToFoundation ? "HTT" : "Strap";

        public static string HoldownName(HoldownFamilies f) =>
            f == HoldownFamilies.HDU_ToFoundation ? "HDU to foundation" :
            f == HoldownFamilies.HTT_ToFoundation ? "HTT to foundation" : "Strap to wall below";

        // ------------------------------------------------------------------ details

        private void ShowDetails()
        {
            details.Children.Clear();
            if (!results.TryGetValue(selectedWall, out var r))
            {
                detailsCard.Visibility = Visibility.Collapsed;
                return;
            }
            detailsCard.Visibility = Visibility.Visible;
            var tools = new WrapPanel { Margin = new Thickness(0, 0, 0, 4) };
            var useForNew = new Button { Content = "Use this sheathing for new walls", Padding = new Thickness(8, 2, 8, 2), Margin = new Thickness(0, 0, 6, 0),
                                         ToolTip = "Walls drawn from now on start with this wall's faces, nailing, framing and hold-down type (saved with the project)." };
            useForNew.Click += (s, e) =>
            {
                calc.DesignSettings.NewWallDesign = r.Inputs.Clone();
                calc.DesignSettings.NewWallDesign.DeadLoad = 0;
                DesignChanged?.Invoke(this, EventArgs.Empty);
            };
            var applyAll = new Button { Content = "Same sheathing on every wall", Padding = new Thickness(8, 2, 8, 2),
                                        ToolTip = "Copies this wall's faces, nailing and framing to every wall (dead loads and hold-down types stay)." };
            applyAll.Click += (s, e) =>
            {
                if (MessageBox.Show(Window.GetWindow(this), $"Give every wall the sheathing of wall {r.WallId}?\n{r.Panel}", "Same sheathing on every wall",
                                    MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes) return;
                foreach (var w in calc._wall_system._walls.Values)
                {
                    w.Design = w.Design ?? new WallDesignInputs();
                    w.Design.SetFaces(r.Inputs.EffectiveFace1(), r.Inputs.EffectiveFace2());
                    w.Design.Species = r.Inputs.Species;
                }
                DesignChanged?.Invoke(this, EventArgs.Empty);
            };
            tools.Children.Add(useForNew);
            tools.Children.Add(applyAll);
            details.Children.Add(tools);
            foreach (var line in Explain(r))
                details.Children.Add(new TextBlock { Text = line, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 1, 0, 1), FontWeight = line.StartsWith("Wall ") ? FontWeights.SemiBold : FontWeights.Normal });
        }

        /// <summary>Every step for one wall, with the code reference (also used in the printed report).</summary>
        public static IEnumerable<string> Explain(WallDesignResult r)
        {
            var d = r.Inputs;
            string method = r.Method == DesignMethods.ASD ? "ASD" : "LRFD";
            string mark(bool ok) => ok ? "✓" : "✗";
            yield return $"Wall {r.WallId}: L = {r.Length:0.00} ft, h = {r.Height:0.0} ft, V = {r.V:0.00} k (strength level" +
                         (string.IsNullOrEmpty(r.GoverningCase) ? ")" : $", {r.GoverningCase})") + $" — {(r.Pass ? "passes" : "does not pass: " + r.Status)}";
            yield return $"Unit shear ({method}): v = {r.LoadFactor:0.0} × {r.V:0.00} k × 1000 / {r.Length:0.00} ft = {r.Demand:0} plf";
            yield return $"Aspect ratio h/b = {r.AspectRatio:0.00} " + (r.AspectOk ? "≤" : ">") + $" {r.MaxAspectRatio:0.0} (SDPWS Table 4.3.4) {mark(r.AspectOk)}; " +
                         (r.AspectRatio > 2 ? $"factor 1.25 − 0.125 × {r.AspectRatio:0.00} = {r.AspectFactor:0.000} (SDPWS 4.3.4)" : "factor 1.0 (h/b ≤ 2)");
            string divisor = r.Method == DesignMethods.ASD ? $"/ {SdpwsSource.OmegaWind:0.0}" : $"× {SdpwsSource.PhiWind:0.00}";
            int n = 0;
            foreach (var f in r.Faces)
            {
                n++;
                string what = f.Option?.Description ?? "(not in the tables)";
                string spacing = f.Option?.UsesEdgeSpacing == true ? $", {f.Face.EdgeSpacing}\" edge" : "";
                yield return $"Face {n}: {what}{spacing}: v_w = {(double.IsNaN(f.Tabulated) ? "-" : f.Tabulated.ToString("0"))} plf" +
                             (f.SpeciesFactor != 1 ? $" × {f.SpeciesFactor:0.00} species" : "") +
                             (f.InsideFaceFactor < 1 ? $" × {f.InsideFaceFactor:0.00} (footnote 10)" : "") +
                             (f.AspectFactor != 1 ? $" × {f.AspectFactor:0.000} aspect" : "") +
                             (f.AspectOk ? $" = {(double.IsNaN(f.Nominal) ? "-" : f.Nominal.ToString("0"))} plf" : $" — {f.Note}");
            }
            if (r.Faces.Count > 1)
                yield return $"Two faces: {r.CombineRule} = {(double.IsNaN(r.CombinedNominal) ? "-" : r.CombinedNominal.ToString("0"))} plf";
            yield return $"Capacity ({method}, {SdpwsSource.EditionName(r.Edition)}): {(double.IsNaN(r.CombinedNominal) ? "-" : r.CombinedNominal.ToString("0"))} {divisor} = " +
                         $"{(double.IsNaN(r.Capacity) ? "-" : r.Capacity.ToString("0"))} plf → ratio {(double.IsNaN(r.Capacity) ? "-" : r.ShearRatio.ToString("0.00"))} {mark(r.ShearOk)}";
            yield return $"Hold-down (always ASD): T = M / L = 0.6 × {r.V:0.00} k × 1000 × {r.Height:0.0} ft / {r.Length:0.00} ft" +
                         (d.DeadLoad > 0 ? $" − 0.6 × {d.DeadLoad:0} plf × {r.Length:0.00} ft / 2" : "") + $" = {r.TensionAsd:0} lb" +
                         $"  (LRFD 1.0W" + (d.DeadLoad > 0 ? " − 0.9D" : "") + $": {r.TensionLrfd:0} lb, for reference)";
            yield return !r.HoldownNeeded ? "No uplift: no hold-down needed for overturning."
                : r.Holdown == null ? $"✗ No {HoldownName(d.Holdown)} in the {(r.HoldownFromStandardList ? "company standard list" : "catalog")} reaches {r.TensionAsd:0} lb: lengthen the wall, add dead load or use another type" +
                                      (r.HoldownFromStandardList ? " (or add a stronger option to the standard list, or switch to the full catalog)." : ".")
                : $"{HoldownName(d.Holdown)}{(r.HoldownFromStandardList ? " (standard list)" : "")}: {r.Holdown} — allowable {r.Holdown.Allowable:0} lb ({(d.Species == WoodTypes.WOODTYPE_SPF_HF ? "SPF/HF" : "DF/SP")}) → ratio {r.HoldownRatio:0.00} {mark(true)}";
        }

        private static TextBlock Label(string text) =>
            new TextBlock { Text = text, Margin = new Thickness(0, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center, Foreground = InkSecondary };

        private static Brush Frozen(string hex)
        {
            var b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            b.Freeze();
            return b;
        }
    }
}
