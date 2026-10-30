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
            btnAuto.Click += (s, e) => AutoSelectRequested?.Invoke(this, EventArgs.Empty);

            var bar = new WrapPanel { Margin = new Thickness(0, 0, 0, 2) };
            bar.Children.Add(Label("Design method:"));
            bar.Children.Add(rbAsd);
            bar.Children.Add(rbLrfd);
            bar.Children.Add(Label("Wood code:"));
            bar.Children.Add(cbEdition);
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
            loading = false;

            tbNote.Text = (loadsFromWind ? "Wall shears from the ASCE 7 wind load cases (strength level). " : "Wall shears from MANUAL loads, taken as strength-level wind. ") +
                          (settings.Method == DesignMethods.ASD ? "ASD: walls get 0.6W, capacity = nominal / 2.0. " : "LRFD: walls get 1.0W, capacity = 0.80 × nominal. ") +
                          "Hold-downs are always checked at ASD (0.6W) against Simpson allowable loads. Blocked wood structural panels, " +
                          "SDPWS Table 4.3A; each drawn wall is one full-height segment. Rigid diaphragm. " + SdpwsSource.TableSource;

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
            ("L (ft)", 46, "Wall length"),
            ("V (k)", 48, "Largest shear on the wall over all load cases, strength level"),
            ("Sheathing (Table 4.3A)", 152, "Wood structural panel and nail size"),
            ("Edge nails", 58, "Panel edge nail spacing"),
            ("Sides", 42, "Sheathed faces"),
            ("Framing", 72, "Framing species group (Table 4.3A footnote)"),
            ("Dead (plf)", 50, "Dead load on the wall that resists overturning (0 = none)"),
            ("Hold-down type", 74, "HDU or HTT anchors the wall end to the foundation; a strap ties it to the wall below"),
            ("v (plf)", 52, "Unit shear demand"),
            ("Allowed", 54, "Design unit shear capacity"),
            ("Ratio", 46, "Demand / capacity"),
            ("T (lb)", 54, "Hold-down tension at ASD"),
            ("Hold-down", 98, "Lightest Simpson model at or above T"),
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
                Put(row, col++, Combo(designer.Panels.Cast<object>(), designer.FindPanel(d), o =>
                {
                    var p = (PanelOption)o;
                    d.Sheathing = p.Sheathing; d.Thickness = p.Thickness; d.Nail = p.Nail;
                }, id));
                Put(row, col++, Combo(ShearWallDesigner.EdgeSpacings.Select(s => (object)$"{s}\" o.c."), $"{d.EdgeSpacing}\" o.c.",
                    o => d.EdgeSpacing = int.Parse(((string)o).Split('"')[0], CultureInfo.InvariantCulture), id));
                Put(row, col++, Combo(new object[] { "1", "2" }, d.Sides.ToString(), o => d.Sides = int.Parse((string)o), id));
                Put(row, col++, Combo(new object[] { "DF / SP", "SPF / HF" }, d.Species == WoodTypes.WOODTYPE_SPF_HF ? "SPF / HF" : "DF / SP",
                    o => d.Species = (string)o == "SPF / HF" ? WoodTypes.WOODTYPE_SPF_HF : WoodTypes.WOODTYPE_DF_SP, id));
                Put(row, col++, DeadLoadBox(d, id));
                Put(row, col++, Combo(new object[] { ShortHoldownName(HoldownFamilies.HDU_ToFoundation), ShortHoldownName(HoldownFamilies.HTT_ToFoundation), ShortHoldownName(HoldownFamilies.Strap_ToWallBelow) },
                    ShortHoldownName(d.Holdown), o => d.Holdown = Enum.GetValues(typeof(HoldownFamilies)).Cast<HoldownFamilies>().First(f => ShortHoldownName(f) == (string)o), id));

                Cell(row, col++, double.IsInfinity(r.Demand) ? "-" : r.Demand.ToString("0"), id);
                Cell(row, col++, double.IsNaN(r.Capacity) ? "-" : r.Capacity.ToString("0"), id);
                Cell(row, col++, double.IsNaN(r.Capacity) || double.IsInfinity(r.ShearRatio) ? "-" : r.ShearRatio.ToString("0.00"), id, r.ShearOk ? Ink : FailInk, bold: !r.ShearOk);
                Cell(row, col++, r.TensionAsd.ToString("0"), id);
                Cell(row, col++, !r.HoldownNeeded ? "none (no uplift)" : r.Holdown?.Model ?? "none strong enough", id, r.HoldownOk ? Ink : FailInk);
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
            yield return $"Capacity: v_w = {(double.IsNaN(r.Nominal) ? "-" : r.Nominal.ToString("0"))} plf ({r.Panel}, {d.EdgeSpacing}\" edge nails, {SdpwsSource.EditionName(r.Edition)} Table 4.3A)" +
                         $" × {r.SpeciesFactor:0.00} species" + (r.InsideFaceFactor < 1 ? $" × {r.InsideFaceFactor:0.00} (footnote 10)" : "") +
                         $" × {r.AspectFactor:0.000} × {(d.Sides == 2 ? 2 : 1)} side{(d.Sides == 2 ? "s" : "")} {divisor} = {(double.IsNaN(r.Capacity) ? "-" : r.Capacity.ToString("0"))} plf" +
                         $" → ratio {(double.IsNaN(r.Capacity) ? "-" : r.ShearRatio.ToString("0.00"))} {mark(r.ShearOk)}";
            yield return $"Hold-down (always ASD): T = M / L = 0.6 × {r.V:0.00} k × 1000 × {r.Height:0.0} ft / {r.Length:0.00} ft" +
                         (d.DeadLoad > 0 ? $" − 0.6 × {d.DeadLoad:0} plf × {r.Length:0.00} ft / 2" : "") + $" = {r.TensionAsd:0} lb" +
                         $"  (LRFD 1.0W" + (d.DeadLoad > 0 ? " − 0.9D" : "") + $": {r.TensionLrfd:0} lb, for reference)";
            yield return !r.HoldownNeeded ? "No uplift: no hold-down needed for overturning."
                : r.Holdown == null ? $"✗ No {HoldownName(d.Holdown)} in the catalog reaches {r.TensionAsd:0} lb: lengthen the wall, add dead load or use another type."
                : $"{HoldownName(d.Holdown)}: {r.Holdown} — allowable {r.Holdown.Allowable:0} lb ({(d.Species == WoodTypes.WOODTYPE_SPF_HF ? "SPF/HF" : "DF/SP")}) → ratio {r.HoldownRatio:0.00} {mark(true)}";
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
