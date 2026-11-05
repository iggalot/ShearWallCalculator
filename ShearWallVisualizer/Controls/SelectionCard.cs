using calculator;
using ShearWallCalculator;
using ShearWallCalculator.Design;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ShearWallVisualizer.Controls
{
    /// <summary>
    /// The card over the drawing for the wall or diaphragm picked with the Select tool: its size and position, and for a
    /// wall the design shear and the Design-tab check (sheathing, nailing, unit shear, hold-down, pass/fail).
    /// </summary>
    public class SelectionCard : UserControl
    {
        public event Action<string, int> OpenDesign;      // go to the Design tab for this wall
        public event Action<string, int> EditGeometry;    // go to the Members tab
        public event Action<string, int> DeleteRequested;
        public event Action<string, int> OpenDetail;      // the wall's elevation and calculation
        public event Action<int, double, double> ShearPartChanged; // a shear wall's declared part typed: from, to (ft from its west or south end)
        public event Action<int> NotShearWall;                // the wall is to be a plain wall again
        public event EventHandler Closed;

        private readonly StackPanel body = new StackPanel();
        private string kind;
        private int id;

        public SelectionCard()
        {
            FontFamily = new FontFamily("Segoe UI");
            FontSize = 12;
            Width = 340; // room for the sheathing and hold-down boxes
            Content = new Border
            {
                Background = Brushes.White, BorderBrush = Ui.Hairline, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 8, 12, 10), Child = body,
                Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 10, ShadowDepth = 2, Opacity = 0.18 },
            };
        }

        public void Show(ShearWallCalculatorBase calc, string selectedKind, int selectedId, IReadOnlyDictionary<int, WallDesignResult> design)
        {
            kind = selectedKind;
            id = selectedId;
            body.Children.Clear();
            if (calc == null || kind == null) { Visibility = Visibility.Collapsed; return; }

            if (kind == "Wall" && calc._wall_system._walls.TryGetValue(id, out var w)) ShowWall(calc, w, design);
            else if (kind == "Diaphragm" && calc._diaphragm_system._diaphragms.TryGetValue(id, out var d)) ShowDiaphragm(d);
            else { Visibility = Visibility.Collapsed; return; }
            Visibility = Visibility.Visible;
        }

        private void ShowWall(ShearWallCalculatorBase calc, WallData w, IReadOnlyDictionary<int, WallDesignResult> design)
        {
            bool ew = w.WallDir == WallDirs.EastWest;
            Header($"Wall {id}", $"{(ew ? "E-W" : "N-S")} · {w.WallLength:0.##} ft long · {w.WallHeight:0.##} ft high");
            Row("From – to", $"({w.Start.X:0.##}, {w.Start.Y:0.##}) – ({w.End.X:0.##}, {w.End.Y:0.##}) ft");
            var part = ShearPartOf?.Invoke(id);
            if (part != null) ShearPartRow(part);

            if (calc.WallShearRange.TryGetValue(id, out var range))
                Row("Design shear V", $"{range.Design:0.00} k", range.DesignCase);
            else
                Row("Design shear V", "not calculated yet", "Draw a diaphragm and walls in both directions");

            if (design != null && design.TryGetValue(id, out var r))
            {
                if (Designer != null) DesignRows(w, r);
                else Row("Sheathing", r.Panel, r.Faces.Count > 1 ? r.CombineRule : null);
                Row("Unit shear v", double.IsNaN(r.Capacity) ? $"{r.Demand:0} plf (no capacity)" : $"{r.Demand:0} / {r.Capacity:0} plf  ({r.ShearRatio:0.00})",
                    ink: r.ShearOk ? Ui.Ink : Ui.FailInk);
                if (Designer != null)
                {
                    var d0 = w.Design ?? new WallDesignInputs();
                    ComboRow("Hold-down type", WallDesignPanel.HoldownTypes.Select(WallDesignPanel.ShortHoldownName).Cast<object>(), WallDesignPanel.ShortHoldownName(d0.Holdown),
                        o => Edit($"hold-down type {o}", d => WallDesignEdits.SetHoldownType(d, WallDesignPanel.HoldownTypes.First(f => WallDesignPanel.ShortHoldownName(f) == (string)o))),
                        tip: WallDesignPanel.HoldownName(d0.Holdown) + ". Changing the type sets the hold-down back to Auto.");
                }
                var picks = HoldownItems?.Invoke(w, r);
                if (picks == null || (!r.HoldownNeeded && !r.HoldownChosen))
                    Row("Hold-down", !r.HoldownNeeded ? "no uplift" : r.Holdown == null ? $"none fits T = {r.TensionAsd:0} lb" : $"{r.Holdown.Model}  (T = {r.TensionAsd:0} lb)",
                        ink: r.HoldownOk ? Ui.Ink : Ui.FailInk);
                else
                    HoldownRow(w, r, picks);
                body.Children.Add(new Border
                {
                    Background = Ui.StateFill(r.State), CornerRadius = new CornerRadius(4), Padding = new Thickness(8, 3, 8, 3),
                    Margin = new Thickness(0, 6, 0, 0), HorizontalAlignment = HorizontalAlignment.Left,
                    Child = new TextBlock { Text = Ui.Mark(r.State) + " " + (r.State == ShearWallCalculator.Design.DesignState.Close ? $"OK, shear ratio {r.ShearRatio:0.00} (close to the limit)" : r.Status),
                                            Foreground = Ui.StateInk(r.State), FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap },
                });
            }
            Buttons(("Detail", () => OpenDetail?.Invoke(kind, id)), ("Design", () => OpenDesign?.Invoke(kind, id)), ("Edit", () => EditGeometry?.Invoke(kind, id)),
                    ("Not a shear wall", NotShearWall == null ? (Action)null : () => NotShearWall(id)));
        }

        /// <summary>A shear wall's declared part, for the card: from and to (ft from its west or south end), of the wall's length.</summary>
        public sealed class ShearPartInfo
        {
            public double From { get; set; }
            public double To { get; set; }
            public double WallLength { get; set; }
            /// <summary>"west" or "south": the end the distances are from.</summary>
            public string FromEnd { get; set; }
        }

        /// <summary>The declared part of a shear wall by its key (set by the window); null: the card shows no part row.</summary>
        public Func<int, ShearPartInfo> ShearPartOf { get; set; }

        /// <summary>"Shear part  [from] to [to] ft": the part of the wall that counts, its ends the hold-downs'. Enter or leaving a box applies it.</summary>
        private void ShearPartRow(ShearPartInfo p)
        {
            var g = new Grid { Margin = new Thickness(0, 2, 0, 1), ToolTip = $"The part of the {p.WallLength:0.##} ft wall that is a shear wall, in ft from its {p.FromEnd} end. " +
                                                                           "The hold-downs go at its two ends. Type and press Enter." };
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.Children.Add(new TextBlock { Text = "Shear part", Foreground = Ui.InkMuted, VerticalAlignment = VerticalAlignment.Center });
            var sp = new StackPanel { Orientation = Orientation.Horizontal };
            TextBox Box(double v) => new TextBox { Text = v.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture), Width = 48, FontSize = 12, Padding = new Thickness(2, 0, 2, 0) };
            var from = Box(p.From);
            var to = Box(p.To);
            void Apply()
            {
                var inv = System.Globalization.CultureInfo.InvariantCulture;
                if (!double.TryParse(from.Text, System.Globalization.NumberStyles.Float, inv, out double a) ||
                    !double.TryParse(to.Text, System.Globalization.NumberStyles.Float, inv, out double e)) { from.Text = p.From.ToString("0.##", inv); to.Text = p.To.ToString("0.##", inv); return; }
                if (Math.Abs(a - p.From) < 1e-6 && Math.Abs(e - p.To) < 1e-6) return;
                ShearPartChanged?.Invoke(id, a, e);
            }
            foreach (var t in new[] { from, to })
            {
                t.KeyDown += (s, e) => { if (e.Key == System.Windows.Input.Key.Enter) { Apply(); e.Handled = true; } };
                t.LostKeyboardFocus += (s, e) => Apply();
            }
            sp.Children.Add(from);
            sp.Children.Add(new TextBlock { Text = " to ", VerticalAlignment = VerticalAlignment.Center });
            sp.Children.Add(to);
            sp.Children.Add(new TextBlock { Text = $" ft of {p.WallLength:0.##}, from the {p.FromEnd} end", Foreground = Ui.InkSecondary, VerticalAlignment = VerticalAlignment.Center });
            Grid.SetColumn(sp, 1);
            g.Children.Add(sp);
            body.Children.Add(g);
        }

        /// <summary>A wall that is not a shear wall: it carries its gravity load only. One button declares it a shear wall.</summary>
        public void ShowPlainWall(string title, string subtitle, IEnumerable<(string label, string value)> rows, Action makeShearWall)
        {
            kind = null;
            id = -1;
            body.Children.Clear();
            Header(title, subtitle);
            foreach (var (label, value) in rows) Row(label, value);
            Buttons(("Make it a shear wall", makeShearWall));
            Visibility = Visibility.Visible;
        }

        /// <summary>A beam or a column (from the building model): a few rows and its buttons.</summary>
        public void ShowMember(string title, string subtitle, IEnumerable<(string label, string value, bool fails)> rows, Action detail, Action delete)
        {
            kind = null;
            id = -1;
            body.Children.Clear();
            Header(title, subtitle);
            foreach (var (label, value, fails) in rows) Row(label, value, ink: fails ? Ui.FailInk : null);
            Buttons(("Detail", detail), ("Delete", delete));
            Visibility = Visibility.Visible;
        }

        private void ShowDiaphragm(DiaphragmData_Rectangular d)
        {
            double wid = Math.Abs(d.P3.X - d.P1.X), dep = Math.Abs(d.P3.Y - d.P1.Y);
            Header($"Diaphragm {id}", $"{wid:0.##} × {dep:0.##} ft · {d.Area:0} ft²");
            Row("Lower-left corner", $"({Math.Min(d.P1.X, d.P3.X):0.##}, {Math.Min(d.P1.Y, d.P3.Y):0.##}) ft");
            Row("Centroid", $"({d.Centroid.X:0.##}, {d.Centroid.Y:0.##}) ft");
            Buttons(("Edit", () => EditGeometry?.Invoke(kind, id)), ("Delete", () => DeleteRequested?.Invoke(kind, id)));
        }

        private void Header(string title, string subtitle)
        {
            var close = new Button
            {
                Content = "×", Width = 22, Height = 22, Padding = new Thickness(0), Background = Brushes.Transparent, BorderThickness = new Thickness(0),
                FontSize = 15, Foreground = Ui.InkMuted, Cursor = System.Windows.Input.Cursors.Hand, ToolTip = "Close (Esc)", Focusable = false,
            };
            close.Click += (s, e) => Closed?.Invoke(this, EventArgs.Empty);
            var head = new DockPanel { Margin = new Thickness(0, 0, 0, 4) };
            DockPanel.SetDock(close, Dock.Right);
            head.Children.Add(close);
            head.Children.Add(new TextBlock { Text = title, FontSize = 15, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center });
            body.Children.Add(head);
            body.Children.Add(new TextBlock { Text = subtitle, Foreground = Ui.InkSecondary, Margin = new Thickness(0, 0, 0, 6) });
        }

        private void Row(string label, string value, string tip = null, Brush ink = null)
        {
            var g = new Grid { Margin = new Thickness(0, 1, 0, 1), ToolTip = tip };
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.Children.Add(new TextBlock { Text = label, Foreground = Ui.InkMuted });
            var v = new TextBlock { Text = value, Foreground = ink ?? Ui.Ink, TextWrapping = TextWrapping.Wrap };
            Grid.SetColumn(v, 1);
            g.Children.Add(v);
            body.Children.Add(g);
        }

        /// <summary>The wall's hold-down choices (<see cref="HoldownPicks.For"/>), set by the window; null: the card shows the hold-down as text.</summary>
        public Func<WallData, WallDesignResult, List<HoldownPicks.Item>> HoldownItems { get; set; }

        /// <summary>The sheathing choices (set by the window); null: the card shows the wall's design as text.</summary>
        public ShearWallDesigner Designer { get; set; }

        /// <summary>
        /// An edit of the wall's design made on the card: the wall, what changed (for the log) and the edit (one of
        /// <see cref="WallDesignEdits"/>). The window applies it as an edit from the Design table: recalculated, saved, undoable.
        /// </summary>
        public event Action<int, string, Action<WallDesignInputs>> DesignEdit;

        private void Edit(string what, Action<WallDesignInputs> edit) => DesignEdit?.Invoke(id, what, edit);

        private const string NoFace = "None", SameFace = "Same as face 1";

        /// <summary>Face 1 (sheathing and edge nailing) and face 2 (none, the same, or its own sheathing and nailing).</summary>
        private void DesignRows(WallData w, WallDesignResult r)
        {
            var d = w.Design ?? new WallDesignInputs();
            var f1 = d.EffectiveFace1();
            var f2 = d.EffectiveFace2();
            bool same = WallDesignEdits.SecondIsSame(d);
            var o1 = Designer.FindOption(f1);
            ComboRow("Face 1", Designer.SheathingOptions.Cast<object>(), o1, o => Edit($"face 1 {((SheathingOption)o).Name}", x => WallDesignEdits.SetFace1(x, (SheathingOption)o)),
                     Spacing(o1, f1, s => Edit($"face 1 edge nailing {s}\"", x => WallDesignEdits.SetFace1Spacing(x, s))), o1?.Description);

            var items = new List<object> { NoFace, SameFace };
            items.AddRange(Designer.SheathingOptions);
            var o2 = f2 == null || same ? null : Designer.FindOption(f2);
            ComboRow("Face 2", items, f2 == null ? NoFace : same ? SameFace : (object)o2, o =>
                {
                    if (Equals(o, NoFace)) Edit("face 2 none", WallDesignEdits.SetNoFace2);
                    else if (Equals(o, SameFace)) Edit("face 2 same as face 1", WallDesignEdits.SetFace2Same);
                    else Edit($"face 2 {((SheathingOption)o).Name}", x => WallDesignEdits.SetFace2(x, (SheathingOption)o));
                },
                o2 == null ? null : Spacing(o2, f2, s => Edit($"face 2 edge nailing {s}\"", x => WallDesignEdits.SetFace2Spacing(x, s))),
                r.Faces.Count > 1 ? r.CombineRule : "One sheathed face. Pick a second face to sheathe both sides.");
        }

        /// <summary>The edge nailing box of a Table 4.3A/B face (only the spacings the table has); null otherwise.</summary>
        private static ComboBox Spacing(SheathingOption o, FaceSheathing f, Action<int> pick)
        {
            if (o == null || !o.UsesEdgeSpacing) return null;
            var cb = new ComboBox { FontSize = 12, Width = 46, Margin = new Thickness(4, 0, 0, 0), Padding = new Thickness(4, 1, 2, 1), ToolTip = "Panel edge nail spacing" };
            foreach (int s in ShearWallDesigner.EdgeSpacings.Where(s => !double.IsNaN(o.NominalWind(s)))) cb.Items.Add($"{s}\"");
            cb.SelectedItem = $"{f.EdgeSpacing}\"";
            cb.SelectionChanged += (s, e) =>
            {
                if (e.RemovedItems.Count > 0 && cb.SelectedItem is string v) pick(int.Parse(v.TrimEnd('"'), System.Globalization.CultureInfo.InvariantCulture));
            };
            return cb;
        }

        /// <summary>A label and a box (and an optional second box beside it, e.g. the nailing).</summary>
        private ComboBox ComboRow(string label, IEnumerable<object> items, object selected, Action<object> pick, UIElement extra = null, string tip = null)
        {
            var g = new Grid { Margin = new Thickness(0, 2, 0, 1) };
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            g.Children.Add(new TextBlock { Text = label, Foreground = Ui.InkMuted, VerticalAlignment = VerticalAlignment.Center });
            var cb = new ComboBox { FontSize = 12, MaxDropDownHeight = 420, Padding = new Thickness(4, 1, 2, 1), ToolTip = tip };
            foreach (var i in items) cb.Items.Add(i);
            cb.SelectedItem = cb.Items.Cast<object>().FirstOrDefault(i => Equals(i, selected));
            cb.SelectionChanged += (s, e) => { if (e.RemovedItems.Count > 0 && cb.SelectedItem != null) pick(cb.SelectedItem); };
            Grid.SetColumn(cb, 1);
            g.Children.Add(cb);
            if (extra != null) { Grid.SetColumn(extra, 2); g.Children.Add(extra); }
            body.Children.Add(g);
            return cb;
        }

        /// <summary>"Hold-down" with a box of the choices, the tension under it.</summary>
        private void HoldownRow(WallData w, WallDesignResult r, List<HoldownPicks.Item> picks)
        {
            var d = w.Design ?? new WallDesignInputs();
            var cb = ComboRow("Hold-down", picks.Cast<object>(), HoldownPicks.Selected(picks, d, r),
                              o => Edit($"hold-down {((HoldownPicks.Item)o).Key ?? "Auto"}", x => x.HoldownKey = ((HoldownPicks.Item)o).Key), tip: HoldownPicks.Tip(d, r));
            cb.Foreground = r.HoldownOk ? Ui.Ink : Ui.FailInk;
            cb.FontWeight = r.HoldownChosen ? FontWeights.SemiBold : FontWeights.Normal;
            if (r.HoldownNeeded)
                Row("", $"T = {r.TensionAsd:0} lb" + (r.Holdown != null ? $" · allowable {r.Holdown.Allowable:0} lb" : ""), ink: r.HoldownOk ? Ui.InkSecondary : Ui.FailInk);
        }

        private void Buttons(params (string text, Action click)[] buttons)
        {
            var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
            foreach (var (text, click) in buttons.Where(x => x.click != null)) // an action this host does not offer: no button
            {
                var b = new Button { Content = text, Padding = new Thickness(10, 2, 10, 2), Margin = new Thickness(0, 0, 6, 0), Focusable = false };
                if (text == "Delete") b.Foreground = Ui.FailInk;
                b.Click += (s, e) => click();
                sp.Children.Add(b);
            }
            body.Children.Add(sp);
        }
    }
}
