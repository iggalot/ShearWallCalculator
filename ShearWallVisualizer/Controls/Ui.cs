using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ShearWallVisualizer.Controls
{
    /// <summary>
    /// The look shared by the code-built panels (Design, Members, Calculations): colors, a table with a
    /// shaded header and hairline rows, cards, and number boxes that commit on Enter or when focus leaves.
    /// </summary>
    internal static class Ui
    {
        // Status colours: pass (acceptable) is green, close to the limit a light golden yellow, fail a strong crimson red. Each
        // has a mark (✓ ▲ ✗) and failing walls are drawn dashed, so a status never depends on colour alone (red and green look
        // alike to many colour-blind users). The drawing pens (MainWindow.Drafting), the issues button and the status-line
        // inks read these brushes: change a colour here only.
        public static readonly Brush Ink = Frozen("#0B0B0B"), InkSecondary = Frozen("#52514E"), InkMuted = Frozen("#7A7974"),
                                     Hairline = Frozen("#D5DAE1"), HeaderFill = Frozen("#EEF1F5"), Surface = Frozen("#F7F8FA"),
                                     SelectedFill = Frozen("#E4EDFA"), PassInk = Frozen("#1E7B34"), FailInk = Frozen("#A50E1F"),
                                     PassFill = Frozen("#E6F4EA"), PassStripe = Frozen("#2E9E4F"), FailFill = Frozen("#FAD4D8"), CloseInk = Frozen("#6E5200"), CloseFill = Frozen("#FFF5C7"),
                                     FailStripe = Frozen("#C8102E"), CloseStripe = Frozen("#F2C200"), FailRowFill = Frozen("#FDEDEF"),
                                     WarnInk = Frozen("#8A5300"), WarnFill = Frozen("#FDF3E1"), InfoFill = Frozen("#EEF4FC"), InfoLine = Frozen("#2A78D6");

        /// <summary>The mark of a design state: ✓ pass, ▲ close to the limit, ✗ fail.</summary>
        public static string Mark(ShearWallCalculator.Design.DesignState s) =>
            s == ShearWallCalculator.Design.DesignState.Fail ? "✗" : s == ShearWallCalculator.Design.DesignState.Close ? "▲" : "✓";

        public static Brush StateInk(ShearWallCalculator.Design.DesignState s) =>
            s == ShearWallCalculator.Design.DesignState.Fail ? FailInk : s == ShearWallCalculator.Design.DesignState.Close ? CloseInk : PassInk;

        public static Brush StateFill(ShearWallCalculator.Design.DesignState s) =>
            s == ShearWallCalculator.Design.DesignState.Fail ? FailFill : s == ShearWallCalculator.Design.DesignState.Close ? CloseFill : PassFill;

        /// <summary>The edge stripe of a table row: crimson for a fail, yellow when close, none when it passes.</summary>
        public static Brush StateStripe(ShearWallCalculator.Design.DesignState s) =>
            s == ShearWallCalculator.Design.DesignState.Fail ? FailStripe : s == ShearWallCalculator.Design.DesignState.Close ? CloseStripe : Brushes.Transparent;

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

        /// <summary>The red × at the end of a table row.</summary>
        public static Button DeleteButton(Action delete, string tip)
        {
            var b = new Button
            {
                Content = "×", ToolTip = tip + "  (Ctrl+Z brings it back)", Width = 22, Height = 20, Padding = new Thickness(0), Margin = new Thickness(3, 0, 3, 0),
                Foreground = FailInk, Background = Brushes.White, BorderBrush = Hairline, FontSize = 14, Cursor = Cursors.Hand,
                VerticalContentAlignment = VerticalAlignment.Center, Focusable = false, VerticalAlignment = VerticalAlignment.Center,
            };
            b.Click += (s, e) => delete();
            return b;
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

        private readonly GridSorter sorter;

        public UiTable(params (string header, double width, string tip)[] columns) : this(null, columns) { }

        /// <param name="sort">The panel's sort for this table (kept across rebuilds); the headers are clickable.</param>
        public UiTable(SortState sort, params (string header, double width, string tip)[] columns)
        {
            sorter = new GridSorter(Grid, sort);
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
                sorter.Header(c, h);
            }
        }

        /// <summary>Sets what a cell sorts by when it is not its text (Text and Number cells sort by themselves).</summary>
        public void SortKey(int row, int col, IComparable value) => sorter.Key(row, col, value);

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
            sorter.Key(row, col, GridSorter.FromText(text));
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
            sorter.Key(row, col, value);
            return tb;
        }

        public void Put(int row, int col, UIElement e)
        {
            Grid.SetRow(e, row);
            Grid.SetColumn(e, col);
            Grid.Children.Add(e);
        }

        /// <summary>The table (sorted) in a white bordered box that scrolls when it is taller than the space.</summary>
        public ScrollViewer InFrame()
        {
            sorter.Apply();
            return new ScrollViewer
            {
                Content = new Border { Background = Brushes.White, BorderBrush = Ui.Hairline, BorderThickness = new Thickness(1), Child = Grid, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top },
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            };
        }
    }
}

namespace ShearWallVisualizer.Controls
{
    /// <summary>Which column a table is sorted by; kept by the panel so the order survives the table being rebuilt.</summary>
    internal sealed class SortState
    {
        public int Column = -1;          // -1: the order the rows were added in
        public bool Descending;
    }

    /// <summary>
    /// Click-to-sort for a grid table built row by row: row 0 holds the column headers (borders with a text block), each
    /// body row's cells sit in grid row 1, 2, ... A header click sorts by that column (again: reverses; a third time: the
    /// original order). The values to sort by are given per cell (<see cref="Key"/>); numbers sort as numbers, blanks and
    /// "–" last.
    /// </summary>
    internal sealed class GridSorter
    {
        private readonly Grid grid;
        private readonly SortState state;
        private readonly Dictionary<int, Dictionary<int, IComparable>> keys = new Dictionary<int, Dictionary<int, IComparable>>();
        private readonly Dictionary<UIElement, int> rowOf = new Dictionary<UIElement, int>();
        private readonly Dictionary<int, (Border border, TextBlock text, string title)> headers = new Dictionary<int, (Border, TextBlock, string)>();

        public GridSorter(Grid grid, SortState state)
        {
            this.grid = grid;
            this.state = state ?? new SortState();
        }

        /// <summary>Makes a column header clickable.</summary>
        public void Header(int col, Border border)
        {
            if (!(border.Child is TextBlock text) || string.IsNullOrEmpty(text.Text)) return;
            headers[col] = (border, text, text.Text);
            border.Cursor = Cursors.Hand;
            border.ToolTip = (border.ToolTip == null ? "" : border.ToolTip + "\n") + "Click to sort";
            border.MouseLeftButtonDown += (s, e) =>
            {
                if (state.Column != col) { state.Column = col; state.Descending = false; }
                else if (!state.Descending) state.Descending = true;
                else state.Column = -1;
                Apply();
                e.Handled = true;
            };
        }

        /// <summary>The value a cell sorts by: a number, text, or null (last).</summary>
        public void Key(int row, int col, IComparable value)
        {
            if (!keys.TryGetValue(row, out var r)) keys[row] = r = new Dictionary<int, IComparable>();
            r[col] = value;
        }

        /// <summary>Text shown in a cell, sorted as a number when it reads as one.</summary>
        public static IComparable FromText(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || text == "–" || text == "-") return null;
            string t = text.Trim();
            int sp = t.IndexOf(' ');
            string first = (sp > 0 ? t.Substring(0, sp) : t).TrimEnd('"'); // "3 (E-W)" sorts by 3, 6" by 6
            if (double.TryParse(first.Replace('−', '-'), NumberStyles.Float, CultureInfo.CurrentCulture, out double v)) return v;
            return t;
        }

        /// <summary>Sorts the rows by the current column (call after the rows are built, and on a header click).</summary>
        public void Apply()
        {
            if (rowOf.Count == 0)
                foreach (UIElement e in grid.Children)
                {
                    int r = Grid.GetRow(e);
                    if (r > 0) rowOf[e] = r;
                }
            var rows = rowOf.Values.Distinct().OrderBy(r => r).ToList();
            var order = state.Column < 0 ? rows : rows.OrderBy(r => r, Comparer<int>.Create(Compare)).ToList();
            var place = new Dictionary<int, int>();
            for (int i = 0; i < order.Count; i++) place[order[i]] = i + 1;
            foreach (var kv in rowOf) Grid.SetRow(kv.Key, place[kv.Value]);
            foreach (var h in headers)
                h.Value.text.Text = h.Value.title + (h.Key != state.Column ? "" : state.Descending ? " ▼" : " ▲");
        }

        private IComparable KeyOf(int row) => keys.TryGetValue(row, out var r) && r.TryGetValue(state.Column, out var k) ? k : null;

        private int Compare(int a, int b)
        {
            IComparable x = KeyOf(a), y = KeyOf(b);
            if (x is double dx && double.IsNaN(dx)) x = null;
            if (y is double dy && double.IsNaN(dy)) y = null;
            int c;
            if (x == null || y == null) c = x == null && y == null ? 0 : x == null ? 1 : -1; // blanks last either way
            else
            {
                if (x is double != y is double) c = x is double ? -1 : 1;                  // numbers before text
                else if (x is string sx) c = string.Compare(sx, (string)y, StringComparison.CurrentCultureIgnoreCase);
                else c = x.CompareTo(y);
                if (state.Descending) c = -c;
            }
            return c != 0 ? c : a.CompareTo(b); // ties keep the original order
        }
    }
}
