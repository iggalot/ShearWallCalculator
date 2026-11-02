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
        public event EventHandler Closed;

        private readonly StackPanel body = new StackPanel();
        private string kind;
        private int id;

        public SelectionCard()
        {
            FontFamily = new FontFamily("Segoe UI");
            FontSize = 12;
            Width = 290;
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

            if (calc.WallShearRange.TryGetValue(id, out var range))
                Row("Design shear V", $"{range.Design:0.00} k", range.DesignCase);
            else
                Row("Design shear V", "not calculated yet", "Draw a diaphragm and walls in both directions");

            if (design != null && design.TryGetValue(id, out var r))
            {
                Row("Sheathing", r.Panel, r.Faces.Count > 1 ? r.CombineRule : null);
                Row("Unit shear v", double.IsNaN(r.Capacity) ? $"{r.Demand:0} plf (no capacity)" : $"{r.Demand:0} / {r.Capacity:0} plf  ({r.ShearRatio:0.00})",
                    ink: r.ShearOk ? Ui.Ink : Ui.FailInk);
                Row("Hold-down", !r.HoldownNeeded ? "no uplift" : r.Holdown == null ? $"none fits T = {r.TensionAsd:0} lb" : $"{r.Holdown.Model}  (T = {r.TensionAsd:0} lb)",
                    ink: r.HoldownOk ? Ui.Ink : Ui.FailInk);
                body.Children.Add(new Border
                {
                    Background = Ui.StateFill(r.State), CornerRadius = new CornerRadius(4), Padding = new Thickness(8, 3, 8, 3),
                    Margin = new Thickness(0, 6, 0, 0), HorizontalAlignment = HorizontalAlignment.Left,
                    Child = new TextBlock { Text = Ui.Mark(r.State) + " " + (r.State == ShearWallCalculator.Design.DesignState.Close ? $"OK, shear ratio {r.ShearRatio:0.00} (close to the limit)" : r.Status),
                                            Foreground = Ui.StateInk(r.State), FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap },
                });
            }
            Buttons(("Detail", () => OpenDetail?.Invoke(kind, id)), ("Design", () => OpenDesign?.Invoke(kind, id)), ("Edit", () => EditGeometry?.Invoke(kind, id)), ("Delete", () => DeleteRequested?.Invoke(kind, id)));
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

        private void Buttons(params (string text, Action click)[] buttons)
        {
            var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
            foreach (var (text, click) in buttons)
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
