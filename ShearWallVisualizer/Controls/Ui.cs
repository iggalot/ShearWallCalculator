using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ShearWallVisualizer.Controls
{
    /// <summary>
    /// The look shared by the code-built panels (Design, Walls &amp; diaphragms, Calculations): colors, a table with a
    /// shaded header and hairline rows, cards, and number boxes that commit on Enter or when focus leaves.
    /// </summary>
    internal static class Ui
    {
        public static readonly Brush Ink = Frozen("#0B0B0B"), InkSecondary = Frozen("#52514E"), InkMuted = Frozen("#7A7974"),
                                     Hairline = Frozen("#D5DAE1"), HeaderFill = Frozen("#EEF1F5"), Surface = Frozen("#F7F8FA"),
                                     SelectedFill = Frozen("#E4EDFA"), PassInk = Frozen("#1E6B33"), FailInk = Frozen("#B3261E"),
                                     WarnInk = Frozen("#8A5300"), WarnFill = Frozen("#FDF3E1"), InfoFill = Frozen("#EEF4FC"), InfoLine = Frozen("#2A78D6");

        public static Brush Frozen(string hex)
        {
            var b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            b.Freeze();
            return b;
        }

        public static TextBlock Heading(string text) => new TextBlock
        {
            Text = text, FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = Ink, Margin = new Thickness(0, 0, 0, 6),
        };

        public static TextBlock Note(string text) => new TextBlock
        {
            Text = text, TextWrapping = TextWrapping.Wrap, Foreground = InkSecondary, Margin = new Thickness(0, 0, 0, 8),
        };

        /// <summary>A white card with a hairline border.</summary>
        public static Border Card(UIElement child, Thickness? margin = null) => new Border
        {
            Background = Brushes.White, BorderBrush = Hairline, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(6),
            Padding = new Thickness(12, 8, 12, 10), Margin = margin ?? new Thickness(0), Child = child,
        };

        /// <summary>A blue (info) or amber (warning) message box.</summary>
        public static Border Banner(string text, bool warning) => new Border
        {
            Background = warning ? WarnFill : InfoFill, BorderBrush = warning ? WarnInk : InfoLine, BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6), Padding = new Thickness(12, 8, 12, 8), Margin = new Thickness(0, 0, 0, 8),
            Child = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap, Foreground = warning ? WarnInk : Ink, FontSize = 13 },
        };

        /// <summary>A label above a value, for the summary strips.</summary>
        public static StackPanel Figure(string label, string value, string tip = null)
        {
            var sp = new StackPanel { Margin = new Thickness(0, 0, 22, 0), ToolTip = tip };
            sp.Children.Add(new TextBlock { Text = label, Foreground = InkMuted, FontSize = 11 });
            sp.Children.Add(new TextBlock { Text = value, Foreground = Ink, FontSize = 14, FontWeight = FontWeights.SemiBold });
            return sp;
        }

        public static string Num(double v, string format = "0.00") =>
            double.IsNaN(v) || double.IsInfinity(v) ? "–" : v.ToString(format, CultureInfo.CurrentCulture);
    }

    /// <summary>
    /// A grid table: a header row (column titles with tooltips) and body rows separated by hairlines. A row can be
    /// selected by clicking anywhere on it; <see cref="RowClicked"/> reports its key.
    /// </summary>
    internal sealed class UiTable
    {
        public readonly Grid Grid = new Grid();
        public event Action<int> RowClicked;
        private int rows;

        public UiTable(params (string header, double width, string tip)[] columns)
        {
            Grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            for (int c = 0; c < columns.Length; c++)
            {
                Grid.ColumnDefinitions.Add(new ColumnDefinition { Width = columns[c].width > 0 ? new GridLength(columns[c].width) : GridLength.Auto });
                var h = new Border
                {
                    Background = Ui.HeaderFill, BorderBrush = Ui.Hairline, BorderThickness = new Thickness(0, 0, 0, 1), Padding = new Thickness(5, 5, 5, 5),
                    Child = new TextBlock { Text = columns[c].header, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap },
                    ToolTip = string.IsNullOrEmpty(columns[c].tip) ? null : columns[c].tip,
                };
                Grid.SetColumn(h, c);
                Grid.Children.Add(h);
            }
        }

        public int Columns => Grid.ColumnDefinitions.Count;

        /// <summary>Starts a body row; returns its index for <see cref="Put"/>.</summary>
        public int AddRow(int key, bool selected)
        {
            rows++;
            Grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(27) });
            var back = new Border
            {
                Background = selected ? Ui.SelectedFill : Brushes.Transparent, BorderBrush = Ui.Hairline, BorderThickness = new Thickness(0, 0, 0, 1), Tag = key,
            };
            back.MouseLeftButtonDown += (s, e) => RowClicked?.Invoke(key);
            Grid.SetRow(back, rows);
            Grid.SetColumnSpan(back, Columns);
            Grid.Children.Add(back);
            return rows;
        }

        public void Select(int key)
        {
            foreach (UIElement e in Grid.Children)
                if (e is Border b && b.Tag is int k && Grid.GetColumnSpan(b) == Columns)
                    b.Background = k == key ? Ui.SelectedFill : Brushes.Transparent;
        }

        public TextBlock Text(int row, int col, string text, int key, Brush ink = null, bool bold = false, bool right = true, string tip = null)
        {
            var t = new TextBlock
            {
                Text = text, Foreground = ink ?? Ui.Ink, FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal, Margin = new Thickness(5, 0, 5, 0),
                VerticalAlignment = VerticalAlignment.Center, TextAlignment = right ? TextAlignment.Right : TextAlignment.Left,
                TextTrimming = TextTrimming.CharacterEllipsis, Background = Brushes.Transparent, ToolTip = tip,
            };
            t.MouseLeftButtonDown += (s, e) => RowClicked?.Invoke(key);
            Put(row, col, t);
            return t;
        }

        /// <summary>
        /// A number box: Enter or leaving the box commits; a value that is not a number, or that <paramref name="commit"/>
        /// refuses (returns false), puts the last good value back.
        /// </summary>
        public TextBox Number(int row, int col, double value, Func<double, bool> commit, int key, string format = "0.##")
        {
            var tb = new TextBox
            {
                Text = value.ToString(format, CultureInfo.CurrentCulture), Margin = new Thickness(2, 2, 2, 2), Padding = new Thickness(2, 0, 2, 0),
                VerticalContentAlignment = VerticalAlignment.Center, TextAlignment = TextAlignment.Right, BorderBrush = Ui.Hairline,
            };
            double last = value;
            void Commit()
            {
                if (double.TryParse(tb.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out double v) && !double.IsNaN(v) && !double.IsInfinity(v))
                {
                    if (Math.Abs(v - last) < 1e-9) return;
                    if (commit(v)) { last = v; return; }
                }
                tb.Text = last.ToString(format, CultureInfo.CurrentCulture);
            }
            tb.KeyDown += (s, e) => { if (e.Key == Key.Enter) { Commit(); e.Handled = true; } };
            tb.LostKeyboardFocus += (s, e) => Commit();
            tb.GotKeyboardFocus += (s, e) => { RowClicked?.Invoke(key); tb.SelectAll(); };
            Put(row, col, tb);
            return tb;
        }

        public void Put(int row, int col, UIElement e)
        {
            Grid.SetRow(e, row);
            Grid.SetColumn(e, col);
            Grid.Children.Add(e);
        }

        /// <summary>The table in a white bordered box that scrolls when it is taller than the space.</summary>
        public ScrollViewer InFrame() => new ScrollViewer
        {
            Content = new Border { Background = Brushes.White, BorderBrush = Ui.Hairline, BorderThickness = new Thickness(1), Child = Grid, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top },
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        };
    }
}
