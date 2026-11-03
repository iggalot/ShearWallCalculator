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
        /// <summary>One story's walls for the table: its calculator, the check of each wall, or why it cannot be designed yet.</summary>
        public sealed class DesignStory
        {
            /// <summary>0 = the first story (the host's level index).</summary>
            public int Index { get; set; }
            public string Name { get; set; }
            public ShearWallCalculatorBase Calculator { get; set; }
            public IReadOnlyDictionary<int, WallDesignResult> Results { get; set; } = new Dictionary<int, WallDesignResult>();
            /// <summary>Why this story has nothing to design yet (null when it has).</summary>
            public string Message { get; set; }
            /// <summary>What fixes the message (show the floor's plan, open the loads, list the model errors), and its link text.</summary>
            public Action Fix { get; set; }
            public string FixLabel { get; set; }
        }

        /// <summary>A warning listed on the Design tab with a link that goes to it (the element on its plan, or the screen that fixes it).</summary>
        public sealed class DesignWarning
        {
            public string Text { get; set; }
            public bool IsError { get; set; }
            public string LinkText { get; set; } = "Show me";
            public Action Go { get; set; }
        }

        public event EventHandler DesignChanged;

        /// <summary>Auto-select for the stories shown (their indexes).</summary>
        public event Action<IList<int>> AutoSelectRequested;

        /// <summary>A wall row was clicked (story index, wall id): the host shows that story and highlights the wall.</summary>
        public event Action<int, int> WallSelected;

        /// <summary>The × on a wall row (story index, wall id); the host deletes it.</summary>
        public event Action<int, int> WallDeleteRequested;

        /// <summary>A row's key: the story and the wall (wall ids repeat from story to story: a tall wall is in each).</summary>
        private const int KeyStride = 1000000;
        internal static int RowKey(int story, int wall) => story * KeyStride + wall;
        private static int StoryOf(int key) => key / KeyStride;
        private static int WallOf(int key) => key % KeyStride;

        private List<DesignStory> stories = new List<DesignStory>();
        private int currentStory;

        /// <summary>The story shown (index), or -1 for every floor (the default).</summary>
        private int floorFilter = -1;
        private readonly ComboBox cbFloors = new ComboBox
        {
            MinWidth = 110, Margin = new Thickness(0, 0, 20, 0),
            ToolTip = "Which floors the table lists: every floor (the default) or one. The drawing shows the floor you are editing.",
        };

        private readonly SortState sort = new SortState();
        private GridSorter sorter;

        private static readonly Brush Ink = Ui.Ink, InkSecondary = Ui.InkSecondary, Hairline = Ui.Hairline, HeaderFill = Ui.HeaderFill,
                                      PassInk = Ui.PassInk, FailInk = Ui.FailInk, FailFill = Ui.FailFill, SelectedFill = Ui.SelectedFill, Surface = Ui.Surface;

        /// <summary>The row fill of each wall when it is not selected (a light tint for a failing wall).</summary>
        private readonly Dictionary<int, Brush> rowFill = new Dictionary<int, Brush>();

        /// <summary>How many columns fit: every column, or fewer (the hidden numbers are in the details below the table).</summary>
        internal enum Density { Full, Medium, Narrow }
        private Density density = Density.Full;
        private readonly CheckBox cbAllColumns = new CheckBox
        {
            Content = "All columns", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(16, 0, 0, 0),
            ToolTip = "Show every column even when the panel is narrow (the table then scrolls sideways). Unticked, the table drops " +
                      "the columns it has no room for; their numbers are in the details of the selected wall.",
        };
        private readonly ScrollViewer tableScroll;

        private readonly RadioButton rbAsd = new RadioButton { Content = "ASD (0.6W)", Margin = new Thickness(0, 0, 12, 0), VerticalAlignment = VerticalAlignment.Center };
        private readonly RadioButton rbLrfd = new RadioButton { Content = "LRFD (1.0W)", Margin = new Thickness(0, 0, 20, 0), VerticalAlignment = VerticalAlignment.Center };
        private readonly ComboBox cbEdition = new ComboBox { Width = 130, Margin = new Thickness(0, 0, 20, 0) };
        /// <summary>Which diaphragm calculation the wall shears come from: chosen on the Calculations tab, named here.</summary>
        private readonly TextBlock tbDiaphragm = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 20, 0), FontWeight = FontWeights.SemiBold,
            ToolTip = "The wall shears designed here come from this diaphragm calculation. Change it on the Calculations tab.",
        };
        private readonly Button btnAuto = new Button { Content = "Auto-select nailing (all walls)", Padding = new Thickness(10, 3, 10, 3) };
        private readonly Button btnSthd = new Button { Padding = new Thickness(8, 2, 8, 2), Margin = new Thickness(0, 0, 20, 0), VerticalAlignment = VerticalAlignment.Center };
        private readonly ComboBox cbPool = new ComboBox { Width = 150, Margin = new Thickness(0, 0, 20, 0), ToolTip = "Standard list: only the hold-downs checked as company standard in View > Hold-down catalog. Full catalog: every option." };
        private readonly TextBlock tbTwoFaces = new TextBlock
        {
            Text = "Two faces: max(stronger, 2 × weaker) ⓘ", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 16, 0),
            Cursor = System.Windows.Input.Cursors.Help,
            ToolTip = new ToolTip { Content = new TextBlock { Text = SheathingRules.TwoFaceRuleNote, TextWrapping = TextWrapping.Wrap, MaxWidth = 420 } },
        };
        private readonly TextBlock tbSummary = new TextBlock { FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(16, 0, 0, 0) };
        private readonly TextBlock tbNote = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = InkSecondary, Margin = new Thickness(0, 6, 0, 8) };
        private readonly Grid table = new Grid();
        private readonly StackPanel details = new StackPanel();
        private readonly Border detailsCard;

        /// <summary>The stories listed (all, or the one picked in Floors).</summary>
        private List<DesignStory> shown = new List<DesignStory>();

        /// <summary>Every wall listed, by row key.</summary>
        private Dictionary<int, WallDesignResult> results = new Dictionary<int, WallDesignResult>();
        private ShearWallDesigner designer;
        private int selectedWall = -1; // a row key
        private bool loading;

        /// <summary>The Level column is shown when more than one story is listed.</summary>
        private bool showLevel;

        private DesignStory Story(int key) => stories.FirstOrDefault(s => s.Index == StoryOf(key));

        private ShearWallCalculatorBase CalcOf(int key) => Story(key)?.Calculator;

        /// <summary>The settings shown in the bar (one set for the project, shared by every story).</summary>
        private ShearWallCalculatorBase SettingsCalc => stories.FirstOrDefault(s => s.Index == currentStory)?.Calculator ?? stories.FirstOrDefault()?.Calculator;

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
            btnSthd.Click += (s, e) => EditStrapConditions();
            btnAuto.Click += (s, e) => AutoSelectRequested?.Invoke(shown.Select(x => x.Index).ToList());
            cbFloors.SelectionChanged += (s, e) =>
            {
                if (loading || !(cbFloors.SelectedItem is ComboBoxItem item)) return;
                floorFilter = (int)item.Tag;
                Rebuild();
            };

            var bar = new WrapPanel { Margin = new Thickness(0, 0, 0, 2) };
            bar.Children.Add(Label("Floors:"));
            bar.Children.Add(cbFloors);
            bar.Children.Add(Label("Diaphragm:"));
            bar.Children.Add(tbDiaphragm);
            bar.Children.Add(Label("Design method:"));
            bar.Children.Add(rbAsd);
            bar.Children.Add(rbLrfd);
            bar.Children.Add(Label("Wood code:"));
            bar.Children.Add(cbEdition);
            bar.Children.Add(Label("Hold-downs:"));
            bar.Children.Add(cbPool);
            bar.Children.Add(btnSthd);
            bar.Children.Add(tbTwoFaces);
            bar.Children.Add(btnAuto);
            bar.Children.Add(tbSummary);
            bar.Children.Add(cbAllColumns);
            cbAllColumns.Click += (s, e) => UpdateDensity(force: true);

            // The table comes first: it fills the height and never gets less than MinTableHeight; the details of the selected
            // wall (and the catalog footnote) scroll inside a box capped at DetailsShare of the panel. Before, the details were
            // docked under the table with no limit: a long explanation (two faces, forces from above) left the table one row,
            // or none at all, and nothing said so.
            var footnote = new TextBlock { Text = "¹ " + ShearWallDesigner.CatalogFootnote, TextWrapping = TextWrapping.Wrap, FontSize = 11, Foreground = InkSecondary, Margin = new Thickness(0, 6, 0, 0) };
            var detailsBody = new StackPanel();
            detailsBody.Children.Add(details);
            detailsBody.Children.Add(footnote);
            detailsCard = new Border
            {
                Background = Brushes.White, BorderBrush = Hairline, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 8, 4, 10), Margin = new Thickness(0, 10, 0, 0),
                Child = detailsScroll = new ScrollViewer { Content = detailsBody, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Padding = new Thickness(0, 0, 8, 0) },
            };

            var root = new Grid { Margin = new Thickness(12, 10, 12, 10) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });                                          // settings bar
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });                                          // note
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star), MinHeight = MinTableHeight }); // the table
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });                                          // details
            tableScroll = new ScrollViewer
            {
                Content = new Border { Background = Brushes.White, BorderBrush = Hairline, BorderThickness = new Thickness(1), Child = table, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top },
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            };
            tableScroll.SizeChanged += (s, e) =>
            {
                if (e.WidthChanged) UpdateDensity(force: false);
                if (e.HeightChanged) CheckTableRoom();
            };
            Grid.SetRow(bar, 0);
            var noteArea = new StackPanel();
            noteArea.Children.Add(tbNote);
            noteArea.Children.Add(warningList);
            Grid.SetRow(noteArea, 1);
            Grid.SetRow(tableScroll, 2);
            Grid.SetRow(detailsCard, 3);
            root.Children.Add(bar);
            root.Children.Add(noteArea);
            root.Children.Add(tableScroll);
            root.Children.Add(detailsCard);
            root.SizeChanged += (s, e) => FitDetails(e.NewSize.Height);
            Content = root;
        }

        /// <summary>The table never gets less than this (DIPs): about six rows.</summary>
        internal const double MinTableHeight = 170;

        /// <summary>The details box takes at most this share of the panel's height; it scrolls beyond that.</summary>
        internal const double DetailsShare = 0.4;

        private readonly ScrollViewer detailsScroll;

        // ------------------------------------------------------------------ warnings that go to what they are about

        /// <summary>The warnings under the note: the floors that cannot be designed yet, then the host's (model errors, the load path).</summary>
        private readonly StackPanel warningList = new StackPanel { Margin = new Thickness(0, 0, 0, 6) };
        private List<DesignWarning> hostWarnings = new List<DesignWarning>();

        /// <summary>At most this many warnings are listed; the rest are counted (the ✗ list on the plan has them all).</summary>
        internal const int MaxWarningsShown = 6;

        /// <summary>The host's warnings about the design (model errors, hold-downs with nothing under them...), each with where it goes.</summary>
        public void SetWarnings(IList<DesignWarning> list, Action showAll = null)
        {
            hostWarnings = list?.ToList() ?? new List<DesignWarning>();
            showAllWarnings = showAll;
            ShowWarnings();
        }

        private Action showAllWarnings;

        /// <summary>The warnings shown now (for tests): the floors' messages first, then the host's.</summary>
        internal List<DesignWarning> Warnings =>
            stories.Where(s => s.Message != null && (floorFilter < 0 || s.Index == floorFilter))
                   .Select(s => new DesignWarning { Text = (stories.Count > 1 ? s.Name + ": " : "") + s.Message, IsError = true, LinkText = s.FixLabel ?? "Show me", Go = s.Fix })
                   .Concat(hostWarnings).ToList();

        private void ShowWarnings()
        {
            warningList.Children.Clear();
            var all = Warnings;
            foreach (var w in all.Take(MaxWarningsShown))
            {
                var text = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = w.IsError ? FailInk : Ui.WarnInk };
                text.Inlines.Add(new Run((w.IsError ? "● " : "▲ ") + w.Text + "  "));
                if (w.Go != null)
                {
                    var link = new Hyperlink(new Run(w.LinkText)) { FontWeight = FontWeights.SemiBold };
                    var go = w.Go;
                    link.Click += (s, e) =>
                    {
                        BuildingModel.Diagnostics.Log.Info("Design", $"Warning followed: {w.Text}");
                        go();
                    };
                    text.Inlines.Add(link);
                }
                warningList.Children.Add(text);
            }
            if (all.Count > MaxWarningsShown)
            {
                var more = new TextBlock { Foreground = InkSecondary };
                more.Inlines.Add(new Run($"and {all.Count - MaxWarningsShown} more  "));
                if (showAllWarnings != null)
                {
                    var link = new Hyperlink(new Run("List them all"));
                    link.Click += (s, e) => showAllWarnings();
                    more.Inlines.Add(link);
                }
                warningList.Children.Add(more);
            }
        }

        /// <summary>The next wall (after the selected one, round the table) in a state: the summary's "✗ n fail" and "close" links.</summary>
        private void NextWall(Func<WallDesignResult, bool> which)
        {
            var keys = results.Keys.OrderBy(k => k).Where(k => which(results[k])).ToList();
            if (keys.Count == 0) return;
            int next = keys.FirstOrDefault(k => k > selectedWall);
            if (!keys.Contains(next)) next = keys[0];
            Select(next);
            var row = table.Children.OfType<Border>().FirstOrDefault(b => b.Tag is int t && t == next);
            row?.BringIntoView();
        }

        private void FitDetails(double panelHeight)
        {
            if (!(panelHeight > 0)) return;
            detailsScroll.MaxHeight = Math.Max(90, panelHeight * DetailsShare);
        }

        private double lastWarnedHeight = -1;

        /// <summary>Logs (once per size) when the table is squeezed below its minimum: the layout problem shows in the log.</summary>
        private void CheckTableRoom()
        {
            double h = tableScroll.ActualHeight;
            if (results.Count == 0 || !(h > 0) || h >= MinTableHeight - 1 || Math.Abs(h - lastWarnedHeight) < 1) return;
            lastWarnedHeight = h;
            BuildingModel.Diagnostics.Log.Warning($"[Design tab] The wall table has only {h:0} px (panel {ActualHeight:0} px, details {detailsCard.ActualHeight:0} px, note {tbNote.ActualHeight:0} px): the window is too short for it.");
        }

        /// <summary>
        /// The columns that fit the width the table has (any monitor, any scaling): all of them when there is room, or the
        /// medium or narrow set. The wall, its check mark and the edit boxes always stay; the numbers dropped are in the details.
        /// </summary>
        private void UpdateDensity(bool force)
        {
            double room = tableScroll.ActualWidth - SystemParameters.VerticalScrollBarWidth - 4;
            var d = cbAllColumns.IsChecked == true || room <= 0 ? Density.Full : DensityFor(room, showLevel);
            if (d == density && !force) return;
            density = d;
            if (designer != null && results.Count > 0)
            {
                ClearTable();
                BuildTable();
            }
        }

        /// <summary>The fullest set of columns that fits in <paramref name="room"/> (DIPs).</summary>
        internal static Density DensityFor(double room, bool withLevel) =>
            room >= TableWidth(Density.Full, withLevel) ? Density.Full : room >= TableWidth(Density.Medium, withLevel) ? Density.Medium : Density.Narrow;

        /// <summary>The width of the table's columns in a density (the status column at its least).</summary>
        internal static double TableWidth(Density d, bool withLevel) =>
            Columns.Select((c, i) => (c, i)).Where(x => (x.i != LevelColumn || withLevel) && Shows(x.c.level, d)).Sum(x => WidthIn(x.c, d));

        private static bool Shows(int level, Density d) => level <= (d == Density.Full ? 2 : d == Density.Medium ? 1 : 0);

        /// <summary>A column shown at this width (the Level column only when several stories are listed).</summary>
        private bool Visible(int col) => (col != LevelColumn || showLevel) && Shows(Columns[col].level, density);

        private static double WidthIn((string header, double width, string tip, int level) c, Density d) =>
            d == Density.Narrow && c.header.StartsWith("Face") ? 112 : c.width;

        private void ClearTable()
        {
            table.Children.Clear();
            table.RowDefinitions.Clear();
            table.ColumnDefinitions.Clear();
            rowFill.Clear();
        }

        /// <summary>
        /// Shows the walls and results of every story (bottom up); <paramref name="current"/> is the story being drawn. A
        /// story's message says why it has nothing to design yet.
        /// </summary>
        public void Show(IList<DesignStory> designStories, int current, ShearWallDesigner shearWallDesigner, bool loadsFromWind)
        {
            stories = (designStories ?? new List<DesignStory>()).OrderBy(s => s.Index).ToList();
            currentStory = current;
            designer = shearWallDesigner;
            if (floorFilter >= 0 && !stories.Any(s => s.Index == floorFilter)) floorFilter = -1; // the floor picked is gone

            loading = true;
            cbFloors.Items.Clear();
            cbFloors.Items.Add(new ComboBoxItem { Content = "All floors", Tag = -1 });
            foreach (var s in stories) cbFloors.Items.Add(new ComboBoxItem { Content = s.Name, Tag = s.Index });
            cbFloors.SelectedItem = cbFloors.Items.OfType<ComboBoxItem>().First(i => (int)i.Tag == floorFilter);
            cbFloors.IsEnabled = stories.Count > 1;
            loading = false;

            this.loadsFromWind = loadsFromWind;
            Rebuild();
        }

        private bool loadsFromWind;

        /// <summary>
        /// What the table lists, for the tab's banner: its own Floors filter ("ALL FLOORS · 10 walls"), not the level shown on the
        /// plan (the level buttons change the plan only).
        /// </summary>
        public string ScopeDescription
        {
            get
            {
                int walls = shown.Sum(s => s.Results?.Count ?? 0);
                string what = floorFilter < 0 ? (stories.Count > 1 ? "ALL FLOORS" : (stories.FirstOrDefault()?.Name ?? "").ToUpperInvariant())
                                              : (stories.FirstOrDefault(s => s.Index == floorFilter)?.Name ?? "").ToUpperInvariant() + " ONLY";
                return $"{what} · {walls} wall{(walls == 1 ? "" : "s")} · Floors picks what this table lists";
            }
        }

        /// <summary>The floors listed changed (the banner follows).</summary>
        public event EventHandler ScopeChanged;

        /// <summary>The table again for the floors picked (after Show or a change of Floors).</summary>
        private void Rebuild()
        {
            shown = stories.Where(s => floorFilter < 0 || s.Index == floorFilter).ToList();
            ScopeChanged?.Invoke(this, EventArgs.Empty);
            showLevel = shown.Count > 1;
            results = new Dictionary<int, WallDesignResult>();
            foreach (var s in shown)
                foreach (var kv in s.Results ?? new Dictionary<int, WallDesignResult>())
                    results[RowKey(s.Index, kv.Key)] = kv.Value;
            var calc = SettingsCalc;
            string message = shown.Count == 0 ? null
                           : string.Join("\n", shown.Where(s => s.Message != null).Select(s => (showLevel ? s.Name + ": " : "") + s.Message));
            if (string.IsNullOrEmpty(message)) message = null;
            density = cbAllColumns.IsChecked == true || !(tableScroll.ActualWidth > 0) ? Density.Full
                    : DensityFor(tableScroll.ActualWidth - SystemParameters.VerticalScrollBarWidth - 4, showLevel);

            loading = true;
            var settings = calc?.DesignSettings ?? new DesignSettings();
            (settings.Method == DesignMethods.LRFD ? rbLrfd : rbAsd).IsChecked = true;
            cbEdition.SelectedItem = cbEdition.Items.OfType<ComboBoxItem>().First(i => (SdpwsEditions)i.Tag == settings.Edition);
            tbDiaphragm.Text = DiaphragmAnalysis.Name(settings.Diaphragm) + "  (set on Calculations)";
            int listed = designer?.StandardList.Count ?? 0;
            cbPool.Items[1] = $"Standard list ({listed})";
            cbPool.SelectedIndex = settings.HoldownsFromStandardList ? 1 : 0;
            ShowStrapConditions(settings.StrapHoldownsOrDefault());
            loading = false;

            tbNote.Text = (loadsFromWind ? "Wall shears from the ASCE 7 wind load cases (strength level). " : "Wall shears from MANUAL loads, taken as strength-level wind. ") +
                          (settings.Method == DesignMethods.ASD ? "ASD: walls get 0.6W, capacity = nominal / 2.0. " : "LRFD: walls get 1.0W, capacity = 0.80 × nominal. ") +
                          "Hold-downs are always checked at ASD (0.6W) against Simpson allowable loads. Sheathing from SDPWS Tables 4.3A, 4.3B and 4.3C, " +
                          "one or two faces (different materials: the larger of the stronger face and 2 × the weaker, never added); each drawn wall is one full-height segment. " +
                          (shown.Any(s => s.Calculator is ShearWallCalculator_Envelope)
                              ? "Envelope: each wall is designed for the larger of its rigid-diaphragm shear (by wall rigidity, plus torsion) and its flexible-diaphragm shear (tributary to its wall line, plus torsion); the governing case says which. "
                          : shown.Any(s => s.Calculator is ShearWallCalculator_FlexibleDiaphragm)
                              ? "Flexible diaphragm: each wall line takes its tributary load, plus the case torsion by equivalent tributary weights, split among its walls by rigidity. "
                              : "Rigid diaphragm: load shared by wall rigidity, plus torsion. ") + SdpwsSource.TableSource;

            ClearTable();
            details.Children.Clear();
            ShowWarnings(); // the floors that cannot be designed yet, each with a link to what fixes it (and the host's warnings)

            if (results.Count == 0 || calc == null || designer == null)
            {
                tbSummary.Text = string.Empty;
                btnAuto.IsEnabled = false;
                table.Children.Add(new TextBlock { Text = message != null ? "Nothing to design yet: see the warnings above (each link goes to what fixes it)." : "Nothing to design yet.",
                                                   Margin = new Thickness(12), TextWrapping = TextWrapping.Wrap, MaxWidth = 700 });
                detailsCard.Visibility = Visibility.Collapsed;
                return;
            }

            btnAuto.IsEnabled = true;
            int pass = results.Values.Count(r => r.Pass);
            int close = results.Values.Count(r => r.State == DesignState.Close);
            // "✗ n fail" and "close to the limit" are links: each click selects the next such wall (row, details and plan)
            tbSummary.Inlines.Clear();
            tbSummary.Inlines.Add(new Run($"{pass} of {results.Count} walls pass"));
            if (close > 0)
            {
                tbSummary.Inlines.Add(new Run("  ·  "));
                var link = new Hyperlink(new Run($"{Ui.Mark(DesignState.Close)} {close} close to the limit")) { Foreground = Ui.CloseInk, ToolTip = "Click: the next wall close to its limit" };
                link.Click += (s, e) => NextWall(r => r.State == DesignState.Close);
                tbSummary.Inlines.Add(link);
            }
            if (pass < results.Count)
            {
                tbSummary.Inlines.Add(new Run("  ·  "));
                var link = new Hyperlink(new Run($"{Ui.Mark(DesignState.Fail)} {results.Count - pass} fail")) { Foreground = FailInk, ToolTip = "Click: the next wall that fails (its row, its details and its place on the plan)" };
                link.Click += (s, e) => NextWall(r => !r.Pass);
                tbSummary.Inlines.Add(link);
            }
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

        /// <summary>
        /// The table's columns. Level: 0 always shown, 1 dropped when the panel is narrow, 2 shown only when every column
        /// fits (the numbers dropped are in the details of the selected wall).
        /// </summary>
        private static readonly (string header, double width, string tip, int level)[] Columns =
        {
            ("Level", 64, "The floor or roof the wall holds up (its story). Pick one floor in Floors to list only its walls.", 0),
            ("Wall", 50, "Wall number and direction", 0),
            ("Mark", 54, "The wall's mark in the shear wall schedule, as tagged on the plan: SW2-1 is construction 1 on the Level 2 plan " +
                         "(walls with the same sheathing and nailing share a mark; numbered weakest first)", 0),
            ("Check", 62, "✓ passes · ▲ passes at 90 % or more of its shear capacity · ✗ fails. The number is the shear ratio (demand / capacity); " +
                          "h/b: the wall is too slender; HD: no hold-down is strong enough. Why it fails is in the Status column.", 0),
            ("L (ft)", 42, "Wall length", 2),
            ("V (k)", 44, "Largest shear on the wall over all load cases, strength level", 2),
            ("Face 1", 134, "Sheathing on one face: any row of SDPWS Table 4.3A (wood panels), 4.3B (wood over gypsum) or 4.3C (gypsum, plaster). " +
                            "New walls start with Structural I on an exterior wall, gypsum wallboard on an interior wall.", 0),
            ("Nails", 46, "Edge fastener spacing (Tables 4.3A/B); Table 4.3C rows carry their own spacing", 0),
            ("Face 2", 134, "The other face: none, the same as face 1, or another material (two-face rule in the details below)", 0),
            ("Nails", 46, "Edge fastener spacing of face 2", 0),
            ("Framing", 64, "Framing species group (Table 4.3A footnote)", 1),
            ("Dead (plf)", 46, "Dead load on the wall that resists overturning (0 = none)", 1),
            ("Hold-down type", 70, "HDUE, HTT or a cast-in STHD strap anchors the wall end to the foundation; a strap ties it to the wall below, a column or a beam. " +
                                   "Where this type cannot anchor to what is under a hold-down, a valid one is picked automatically (shown in the Hold-down column); choose here to override", 0),
            ("v (plf)", 48, "Unit shear demand", 2),
            ("Allowed", 56, "Design unit shear capacity", 2),
            ("Ratio", 44, "Demand / capacity", 1),
            ("T (lb)", 50, "Hold-down tension at ASD", 2),
            ("Hold-down¹", 150, "Auto: the lightest Simpson model at or above T (" + SimpsonCatalog.Edition + ", see the footnote). Pick a model to use your own " +
                                "(e.g. one size up); a model weaker than T fails the wall. Changing the hold-down type goes back to Auto", 0),
            ("Status", 80, "", 0),
            ("", 30, "", 0),
        };

        /// <summary>The short text of the check column: the mark and the shear ratio, or what fails when it is not the shear.</summary>
        internal static string CheckText(WallDesignResult r)
        {
            string mark = Ui.Mark(r.State);
            if (!r.AspectOk) return mark + " h/b";
            if (double.IsNaN(r.Capacity) || double.IsInfinity(r.ShearRatio)) return mark + " –";
            if (r.ShearOk && !r.HoldownOk) return mark + " HD";
            return $"{mark} {r.ShearRatio:0.00}";
        }

        private const int LevelColumn = 0;

        private void BuildTable()
        {
            sorter = new GridSorter(table, sort);
            for (int i = 0; i < Columns.Length; i++)
            {
                var c = Columns[i];
                if (!Visible(i)) table.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0) });
                else if (c.header == "Status") // the status text is not cut off, except when the panel is narrow (then its tooltip has it)
                    table.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto, MinWidth = c.width, MaxWidth = density == Density.Narrow ? 200 : double.PositiveInfinity });
                else table.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(WidthIn(c, density)) });
            }
            table.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            for (int c = 0; c < Columns.Length; c++)
            {
                var h = new Border
                {
                    Background = HeaderFill, BorderBrush = Hairline, BorderThickness = new Thickness(0, 0, 0, 1), Padding = new Thickness(4, 5, 4, 5),
                    Child = new TextBlock { Text = Columns[c].header, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap },
                    ToolTip = string.IsNullOrEmpty(Columns[c].tip) ? null : Columns[c].tip,
                    Visibility = Visible(c) ? Visibility.Visible : Visibility.Collapsed,
                };
                Grid.SetColumn(h, c);
                table.Children.Add(h);
                sorter.Header(c, h);
            }

            // the schedule marks, by story: the same as the plan's tags (one rule, WallSchedule)
            var markOf = stories.ToDictionary(s => s.Index, s => WallSchedule.MarksByWall(WallSchedule.Build(s.Results, WallSchedule.LevelTag(s.Name, s.Index))));
            int row = 1;
            foreach (var id in results.Keys.OrderBy(k => k)) // bottom story first, then by wall number
            {
                var r = results[id];
                var story = Story(id);
                var calc = story.Calculator;
                int wallId = WallOf(id);
                if (!calc._wall_system._walls.TryGetValue(wallId, out var wall)) continue;
                var state = r.State;
                table.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                rowFill[id] = state == DesignState.Fail ? Ui.FailRowFill : Brushes.Transparent;
                var back = new Border { Background = id == selectedWall ? SelectedFill : rowFill[id], BorderBrush = Hairline, BorderThickness = new Thickness(0, 0, 0, 1), Tag = id };
                Grid.SetRow(back, row);
                Grid.SetColumnSpan(back, Columns.Length);
                back.MouseLeftButtonDown += (s, e) => Select((int)((Border)s).Tag);
                table.Children.Add(back);

                // the stripe at the row's left edge: crimson (fail) or yellow (close); not hit-testable, it moves with the row when sorted
                var stripe = new Border { Width = 4, HorizontalAlignment = HorizontalAlignment.Left, Background = Ui.StateStripe(state), IsHitTestVisible = false, Margin = new Thickness(0, 0, 0, 1) };
                Grid.SetRow(stripe, row);
                table.Children.Add(stripe);

                int col = 0;
                Cell(row, col, story.Name, id); // the Floors filter decides what is listed; the plan's level is not marked here
                sorter.Key(row, col++, story.Index); // sorts by story, not by name
                Cell(row, col++, $"{wallId} ({(wall.WallDir == WallDirs.EastWest ? "E-W" : "N-S")})", id);
                string mark = markOf.TryGetValue(story.Index, out var mk) && mk.TryGetValue(wallId, out var m) ? m : "–";
                Cell(row, col++, mark, id, bold: true);
                var badge = new Border
                {
                    Background = Ui.StateFill(state), CornerRadius = new CornerRadius(4), Margin = new Thickness(3, 3, 3, 3), Padding = new Thickness(5, 1, 5, 1),
                    Child = new TextBlock { Text = CheckText(r), Foreground = Ui.StateInk(state), FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis },
                    ToolTip = state == DesignState.Fail ? r.Status : state == DesignState.Close ? $"Passes, at {r.ShearRatio:0.00} of the shear capacity (close to the limit)" : "Passes",
                    Tag = id,
                };
                badge.MouseLeftButtonDown += (s, e) => Select((int)((Border)s).Tag);
                Put(row, col, badge);
                sorter.Key(row, col++, (int)state * 100 + (double.IsNaN(r.ShearRatio) || double.IsInfinity(r.ShearRatio) ? 99 : Math.Min(99, r.ShearRatio))); // sorts fails, then close, by ratio
                Cell(row, col++, r.Length.ToString("0.00"), id);
                Cell(row, col++, r.V.ToString("0.00"), id);

                var d = wall.Design ?? (wall.Design = new WallDesignInputs());
                var f1 = d.EffectiveFace1();
                var f2 = d.EffectiveFace2();
                bool same = f2 != null && f1.SameConstructionAs(f2);
                var o1 = designer.FindOption(f1);
                // the edits are WallDesignEdits: the same as on the plan's wall card
                Put(row, col++, FaceCombo(designer.SheathingOptions.Cast<object>(), o1, o => WallDesignEdits.SetFace1(d, (SheathingOption)o), id));
                Put(row, col++, SpacingCell(o1, f1, s => WallDesignEdits.SetFace1Spacing(d, s), id));

                var face2Items = new List<object> { NoFace, SameFace };
                face2Items.AddRange(designer.SheathingOptions);
                var o2 = f2 == null ? null : designer.FindOption(f2);
                Put(row, col++, FaceCombo(face2Items, f2 == null ? (object)NoFace : same ? SameFace : (object)o2, o =>
                {
                    if (Equals(o, NoFace)) WallDesignEdits.SetNoFace2(d);
                    else if (Equals(o, SameFace)) WallDesignEdits.SetFace2Same(d);
                    else WallDesignEdits.SetFace2(d, (SheathingOption)o);
                }, id));
                if (f2 == null || same) Cell(row, col++, same ? "same" : "", id, InkSecondary);
                else Put(row, col++, SpacingCell(o2, f2, s => WallDesignEdits.SetFace2Spacing(d, s), id));
                Put(row, col++, Combo(new object[] { "DF / SP", "SPF / HF" }, d.Species == WoodTypes.WOODTYPE_SPF_HF ? "SPF / HF" : "DF / SP",
                    o => d.Species = (string)o == "SPF / HF" ? WoodTypes.WOODTYPE_SPF_HF : WoodTypes.WOODTYPE_DF_SP, id));
                Put(row, col++, DeadLoadBox(d, id));
                Put(row, col++, Combo(HoldownTypes.Select(ShortHoldownName).Cast<object>(),
                    ShortHoldownName(d.Holdown), o => WallDesignEdits.SetHoldownType(d, HoldownTypes.First(f => ShortHoldownName(f) == (string)o)), id));

                Cell(row, col++, double.IsInfinity(r.Demand) ? "-" : r.Demand.ToString("0"), id);
                Cell(row, col++, double.IsNaN(r.Capacity) ? "-" : r.Capacity.ToString("0"), id);
                if (!r.ShearOk) Tint(row, col, FailFill);
                Cell(row, col++, double.IsNaN(r.Capacity) || double.IsInfinity(r.ShearRatio) ? "-" : r.ShearRatio.ToString("0.00"), id, r.ShearOk ? Ink : FailInk, bold: !r.ShearOk);
                Cell(row, col++, r.TensionAsd.ToString("0"), id);
                if (!r.HoldownOk) Tint(row, col, FailFill);
                if (!r.HoldownNeeded && string.IsNullOrEmpty(d.HoldownKey))
                    Cell(row, col++, "no uplift", id);
                else
                {
                    var hd = HoldownCombo(d, r, id);
                    if (r.HoldownElsewhere.Count > 0)
                        hd.ToolTip = $"{r.Holdown?.Model ?? "None"} ({HoldownName(d.Holdown)}) where it can anchor; where it lands on something it cannot anchor to, a valid type is picked automatically: " +
                                     string.Join(", ", r.HoldownElsewhere) + ". Choose another Hold-down type for this wall to override.";
                    Put(row, col++, hd);
                }
                var status = new Border
                {
                    Background = Ui.StateFill(state), CornerRadius = new CornerRadius(4), Margin = new Thickness(3, 3, 3, 3), Padding = new Thickness(6, 2, 6, 2),
                    Child = new TextBlock { Text = Ui.Mark(state) + " " + r.Status, Foreground = Ui.StateInk(state), FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis },
                    ToolTip = r.Status, Tag = id,
                };
                status.MouseLeftButtonDown += (s, e) => Select((int)((Border)s).Tag);
                Put(row, col++, status);
                int delStory = story.Index, delWall = wallId;
                Put(row, col++, Ui.DeleteButton(() => Dispatcher.BeginInvoke(new Action(() => WallDeleteRequested?.Invoke(delStory, delWall)),
                    System.Windows.Threading.DispatcherPriority.Background), $"Delete wall {wallId}" + (showLevel ? $" ({story.Name})" : "")));
                row++;
            }
            sorter.Apply();
        }

        /// <summary>Selects a wall from outside (the drawing's card); ignored if the wall has no result.</summary>
        public void SelectWall(int story, int id)
        {
            int key = RowKey(story, id);
            if (results.ContainsKey(key)) Select(key, notify: false);
        }

        private void Select(int id, bool notify = true)
        {
            if (selectedWall == id)
            {
                // the row already picked, clicked again: its card on the plan opens again (it may have been closed)
                if (notify) WallSelected?.Invoke(StoryOf(id), WallOf(id));
                return;
            }
            selectedWall = id;
            if (notify) WallSelected?.Invoke(StoryOf(id), WallOf(id));
            foreach (var b in table.Children.OfType<Border>().Where(b => Grid.GetColumnSpan(b) == Columns.Length && b.Tag is int))
                b.Background = (int)b.Tag == id ? SelectedFill : rowFill.TryGetValue((int)b.Tag, out var fill) ? fill : Brushes.Transparent;
            ShowDetails();
        }

        private TextBlock Cell(int row, int col, string text, int id, Brush brush = null, bool bold = false)
        {
            var t = new TextBlock
            {
                Text = text, Foreground = brush ?? Ink, FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal, Margin = new Thickness(4, 0, 4, 0),
                VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis, Tag = id, Background = Brushes.Transparent,
            };
            t.MouseLeftButtonDown += (s, e) => Select(id);
            Put(row, col, t);
            return t;
        }

        /// <summary>Tints a whole cell (a failing value): a fill under the cell's content, over the row's own fill.</summary>
        private void Tint(int row, int col, Brush fill)
        {
            var b = new Border { Background = fill, IsHitTestVisible = false, Margin = new Thickness(0, 0, 0, 1),
                                 Visibility = Visible(col) ? Visibility.Visible : Visibility.Collapsed };
            Grid.SetRow(b, row);
            Grid.SetColumn(b, col);
            var back = table.Children.OfType<Border>().First(x => Grid.GetRow(x) == row && Grid.GetColumnSpan(x) == Columns.Length);
            table.Children.Insert(table.Children.IndexOf(back) + 1, b);
        }

        /// <summary>What a cell sorts by: its text, the choice in a box, or the status text.</summary>
        private static IComparable SortValue(UIElement e) =>
            e is TextBlock t ? GridSorter.FromText(t.Text)
            : e is TextBox tb ? GridSorter.FromText(tb.Text)
            : e is ComboBox cb ? GridSorter.FromText(cb.SelectedItem?.ToString())
            : e is Border b && b.Child is TextBlock bt ? GridSorter.FromText(bt.Text)
            : null;

        private void Put(int row, int col, UIElement e)
        {
            if (row > 0) sorter?.Key(row, col, SortValue(e));
            if (!Visible(col)) e.Visibility = Visibility.Collapsed; // a column with no room at this width
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
                string was = e.RemovedItems.Count > 0 ? e.RemovedItems[0]?.ToString() : "-";
                apply(cb.SelectedItem);
                selectedWall = id;
                var st = Story(id);
                BuildingModel.Diagnostics.Log.Info("Design", $"{st?.Name} wall {id % KeyStride}: {was} → {cb.SelectedItem}");
                // the table is rebuilt after this event, not inside it: the box that raised it is one of the table's cells
                Dispatcher.BeginInvoke(new Action(() => DesignChanged?.Invoke(this, EventArgs.Empty)), System.Windows.Threading.DispatcherPriority.Input);
            };
            return cb;
        }

        /// <summary>The wall's hold-down box (<see cref="HoldownPicks"/>: the same choices as the plan's wall card).</summary>
        private ComboBox HoldownCombo(WallDesignInputs d, WallDesignResult r, int id)
        {
            var sthd = (SettingsCalc?.DesignSettings ?? new DesignSettings()).StrapHoldownsOrDefault();
            var items = HoldownPicks.For(designer, d, r, sthd);
            var cb = Combo(items.Cast<object>(), HoldownPicks.Selected(items, d, r), o => d.HoldownKey = ((HoldownPicks.Item)o).Key, id);
            cb.MaxDropDownHeight = 420;
            cb.Foreground = r.HoldownOk ? Ink : FailInk;
            cb.FontWeight = r.HoldownChosen ? FontWeights.SemiBold : FontWeights.Normal;
            cb.ToolTip = HoldownPicks.Tip(d, r);
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
            if (o == null)
            {
                NoNailing(id, $"its face ({f?.Table} {f?.Material} {f?.Thickness}, {f?.Nail}, row {f?.Row}) matches no sheathing option. Pick a sheathing to fix it.");
                return new TextBlock { Text = "-", Margin = new Thickness(4, 0, 4, 0), VerticalAlignment = VerticalAlignment.Center, ToolTip = "This face matches no sheathing option: pick a sheathing" };
            }
            if (!o.UsesEdgeSpacing)
            {
                NoNailing(id, $"{o.Name} is a Table 4.3C row: its fastener spacing ({o.RowSpacing}) is fixed by the row. Pick a wood panel (Table 4.3A/B) to choose the edge nailing.");
                return new TextBlock
                {
                    Text = (string.IsNullOrEmpty(o.RowSpacing) ? "-" : o.RowSpacing.Split(' ', '\n')[0] + (o.RowSpacing.All(ch => char.IsDigit(ch) || ch == '.') ? "\"" : "")) + " fixed",
                    Foreground = InkSecondary, Margin = new Thickness(4, 0, 4, 0), VerticalAlignment = VerticalAlignment.Center,
                    ToolTip = $"{o.Name}: a Table 4.3C row, its fastener spacing is set by the row ({o.RowSpacing}), so there is no edge-nailing choice. " +
                              "Pick a wood panel (Table 4.3A/B) as the sheathing to choose the edge nailing.",
                };
            }
            var spacings = ShearWallDesigner.EdgeSpacings.Where(s => !double.IsNaN(o.NominalWind(s))).Select(s => (object)$"{s}\"").ToList();
            if (spacings.Count == 0) NoNailing(id, $"{o.Name} has no capacity at any edge spacing ({string.Join(", ", ShearWallDesigner.EdgeSpacings)}\").");
            return Combo(spacings, $"{f.EdgeSpacing}\"", v => apply(int.Parse(((string)v).TrimEnd('"'), CultureInfo.InvariantCulture)), id);
        }

        /// <summary>The walls whose nailing cell has no choice, with why, already logged (logged again only when the reason changes).</summary>
        private readonly Dictionary<int, string> noNailingLogged = new Dictionary<int, string>();

        /// <summary>Diagnostics: a wall's nailing cell offers no choice; logs why, once per wall and reason.</summary>
        private void NoNailing(int id, string why)
        {
            if (noNailingLogged.TryGetValue(id, out var was) && was == why) return;
            noNailingLogged[id] = why;
            BuildingModel.Diagnostics.Log.Info("Design", $"{Story(id)?.Name} wall {id % KeyStride}: no edge-nailing choice: {why}");
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
            if (loading || stories.Count == 0) return;
            foreach (var calc in stories.Select(s => s.Calculator).Where(c => c != null)) // one set for the project: every story
            {
                calc.DesignSettings = calc.DesignSettings ?? new DesignSettings();
                calc.DesignSettings.Method = rbLrfd.IsChecked == true ? DesignMethods.LRFD : DesignMethods.ASD;
                if (cbEdition.SelectedItem is ComboBoxItem item) calc.DesignSettings.Edition = (SdpwsEditions)item.Tag;
                calc.DesignSettings.HoldownsFromStandardList = cbPool.SelectedIndex == 1;
            }
            DesignChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ShowStrapConditions(StrapHoldownConditions c)
        {
            btnSthd.Content = $"STHD: {c.Stemwall:0.#}\" stemwall, {(c.CrackedConcrete ? "cracked" : "uncracked")}, {c.Position.ToString().ToLowerInvariant()}...";
            btnSthd.ToolTip = "Foundation conditions for the LSTHD / STHD strap-tie hold-downs (Simpson " + ShearWallDesigner.CatalogSource(HoldownFamilies.STHD_ToFoundation) + "), one set for the project: " +
                              c.Describe() + ". Click to change.";
        }

        /// <summary>
        /// Asks for the project's LSTHD / STHD foundation conditions (stemwall, concrete, position, seismic row) and gives them
        /// to every story; the design is then redone.
        /// </summary>
        private void EditStrapConditions()
        {
            if (stories.Count == 0) return;
            var now = (SettingsCalc?.DesignSettings ?? new DesignSettings()).StrapHoldownsOrDefault();
            var stem = new ComboBox { Width = 260, Margin = new Thickness(0, 0, 0, 8) };
            stem.Items.Add("6\" minimum stemwall (6\" to under 8\")");
            stem.Items.Add("8\" minimum stemwall (or slab on grade, note 11)");
            stem.SelectedIndex = now.Stemwall >= 8 ? 1 : 0;
            var concrete = new ComboBox { Width = 260, Margin = new Thickness(0, 0, 0, 8) };
            concrete.Items.Add("Cracked concrete");
            concrete.Items.Add("Uncracked concrete");
            concrete.SelectedIndex = now.CrackedConcrete ? 0 : 1;
            var position = new ComboBox { Width = 260, Margin = new Thickness(0, 0, 0, 8) };
            position.Items.Add("Midwall (1.5 le or more from the end)");
            position.Items.Add("Corner");
            position.Items.Add("Endwall (lowest loads)");
            position.SelectedIndex = (int)now.Position;
            var seismic = new ComboBox { Width = 260, Margin = new Thickness(0, 0, 0, 8) };
            seismic.Items.Add("Wind and SDC A & B");
            seismic.Items.Add("SDC C – F");
            seismic.SelectedIndex = (int)now.Seismic;

            var grid = new Grid { Margin = new Thickness(16) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            void RowOf(int i, string label, Control c)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                var t = new TextBlock { Text = label, Margin = new Thickness(0, 3, 10, 8) };
                Grid.SetRow(t, i); Grid.SetRow(c, i); Grid.SetColumn(c, 1);
                grid.Children.Add(t); grid.Children.Add(c);
            }
            RowOf(0, "Stemwall:", stem);
            RowOf(1, "Concrete:", concrete);
            RowOf(2, "Position:", position);
            RowOf(3, "Load row:", seismic);
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var note = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap, MaxWidth = 400, Foreground = InkSecondary, Margin = new Thickness(0, 4, 0, 10),
                Text = "Simpson " + ShearWallDesigner.CatalogSource(HoldownFamilies.STHD_ToFoundation) + ", \"Tension Loads for STHD Installations\". One set for the whole project; every wall with " +
                       "an STHD gets the allowable load of this column. Wind loads use the \"Wind and SDC A & B\" row. Not applied: " +
                       "the LSTHD8 0.9 factor for 1\"–4\" spalls, two-pour downturn footings, brick ledges (see the catalog).",
            };
            Grid.SetRow(note, 4); Grid.SetColumnSpan(note, 2); grid.Children.Add(note);
            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var ok = new Button { Content = "OK", Width = 80, IsDefault = true, Margin = new Thickness(0, 0, 8, 0) };
            var cancel = new Button { Content = "Cancel", Width = 80, IsCancel = true };
            buttons.Children.Add(ok); buttons.Children.Add(cancel);
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Grid.SetRow(buttons, 5); Grid.SetColumnSpan(buttons, 2); grid.Children.Add(buttons);
            var dlg = new Window
            {
                Title = "STHD foundation conditions", Content = grid, SizeToContent = SizeToContent.WidthAndHeight, ResizeMode = ResizeMode.NoResize,
                WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = Window.GetWindow(this), ShowInTaskbar = false,
            };
            BuildingModel.Sketch.WindowFit.Fit(dlg);
            ok.Click += (s, e) => dlg.DialogResult = true;
            if (dlg.ShowDialog() != true) return;

            var c2 = new StrapHoldownConditions
            {
                Stemwall = stem.SelectedIndex == 1 ? 8 : 6, CrackedConcrete = concrete.SelectedIndex == 0,
                Position = (SthdPositions)position.SelectedIndex, Seismic = (SthdSeismic)seismic.SelectedIndex,
            };
            foreach (var calc in stories.Select(s => s.Calculator).Where(x => x != null)) // one set for the project: every story
            {
                calc.DesignSettings = calc.DesignSettings ?? new DesignSettings();
                calc.DesignSettings.StrapHoldowns = c2.Clone();
            }
            BuildingModel.Diagnostics.Log.Info("Design", $"STHD conditions: {now.Describe()} -> {c2.Describe()}");
            ShowStrapConditions(c2);
            DesignChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>The table's short label (the column tooltip says where each one anchors).</summary>
        /// <summary>The hold-down types in the order offered (table and wall card).</summary>
        public static readonly HoldownFamilies[] HoldownTypes =
        {
            HoldownFamilies.HDU_ToFoundation, HoldownFamilies.HTT_ToFoundation, HoldownFamilies.STHD_ToFoundation,
            HoldownFamilies.Strap_ToWallBelow, HoldownFamilies.HDU_Legacy_ToFoundation,
        };

        public static string ShortHoldownName(HoldownFamilies f) =>
            f == HoldownFamilies.HDU_Legacy_ToFoundation ? "HDU (disc.)" :
            f == HoldownFamilies.HDU_ToFoundation ? "HDUE" : f == HoldownFamilies.HTT_ToFoundation ? "HTT" :
            f == HoldownFamilies.STHD_ToFoundation ? "STHD" : "Strap";

        public static string HoldownName(HoldownFamilies f) =>
            f == HoldownFamilies.HDU_Legacy_ToFoundation ? "HDU to foundation (discontinued; C-C-2021 p. 53)" :
            f == HoldownFamilies.HDU_ToFoundation ? "HDUE / DTT to foundation" :
            f == HoldownFamilies.HTT_ToFoundation ? "LTT / HTT / HTTH to foundation" :
            f == HoldownFamilies.STHD_ToFoundation ? "STHD cast in foundation" : "Strap to wall below";

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
            Button UseForNew(bool exterior)
            {
                string which = exterior ? "exterior" : "interior";
                var b = new Button
                {
                    Content = $"Use for new {which} walls", Padding = new Thickness(8, 2, 8, 2), Margin = new Thickness(0, 0, 6, 0),
                    ToolTip = $"{(exterior ? "Exterior" : "Interior")} walls drawn from now on start with this wall's faces, nailing, framing and hold-down type " +
                              $"(saved with the project). An exterior wall has any part of its length on the building's outside edge. " +
                              $"Program default: {(exterior ? "15/32\" Structural I, 8d at 6\", one face" : "1/2\" gypsum wallboard both faces, 7\" edges, blocked")}.",
                };
                b.Click += (s, e) =>
                {
                    var d = r.Inputs.Clone();
                    d.DeadLoad = 0;
                    d.HoldownKey = null; // a new wall sizes its own hold-down
                    foreach (var calc in stories.Select(x => x.Calculator).Where(c => c?.DesignSettings != null))
                    {
                        if (exterior) calc.DesignSettings.NewWallDesign = d.Clone();
                        else calc.DesignSettings.NewInteriorWallDesign = d.Clone();
                    }
                    DesignChanged?.Invoke(this, EventArgs.Empty);
                };
                return b;
            }
            string listed = showLevel ? "every wall listed" : "every wall of " + (Story(selectedWall)?.Name ?? "this floor");
            var applyAll = new Button { Content = "Same sheathing on every wall listed", Padding = new Thickness(8, 2, 8, 2),
                                        ToolTip = $"Copies this wall's faces, nailing and framing to {listed} (dead loads and hold-down types stay). " +
                                                  "Pick one floor in Floors to change only its walls." };
            applyAll.Click += (s, e) =>
            {
                if (MessageBox.Show(Window.GetWindow(this), $"Give {listed} the sheathing of wall {r.WallId}?\n{r.Panel}", "Same sheathing on every wall listed",
                                    MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes) return;
                foreach (var w in shown.SelectMany(x => x.Calculator._wall_system._walls.Values))
                {
                    w.Design = w.Design ?? new WallDesignInputs();
                    w.Design.SetFaces(r.Inputs.EffectiveFace1(), r.Inputs.EffectiveFace2());
                    w.Design.Species = r.Inputs.Species;
                }
                DesignChanged?.Invoke(this, EventArgs.Empty);
            };
            tools.Children.Add(UseForNew(exterior: true));
            tools.Children.Add(UseForNew(exterior: false));
            tools.Children.Add(applyAll);
            details.Children.Add(tools);
            if (showLevel) details.Children.Add(new TextBlock { Text = Story(selectedWall)?.Name, Foreground = InkSecondary, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 2, 0, 0) });
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
            yield return $"Hold-down arm: d = L − post = {r.Length:0.00} ft − {r.PostAlongWall:0.##} in = {r.HoldownArm:0.000} ft (hold-downs on the post centerlines)";
            yield return $"Hold-down (always ASD): T = M / d = 0.6 × {r.V:0.00} k × 1000 × {r.Height:0.0} ft / {r.HoldownArm:0.000} ft" +
                         (d.DeadLoad > 0 ? $" − 0.6 × {d.DeadLoad:0} plf × {r.Length:0.00} ft / 2" : "") +
                         (r.FromAbove.Count > 0 ? $" + walls above (next line) = {r.TensionAsd:0} lb" : $" = {r.TensionAsd:0} lb") +
                         $"  (LRFD 1.0W" + (d.DeadLoad > 0 ? " − 0.9D" : "") + $": {r.TensionLrfd:0} lb, for reference)";
            if (r.FromAbove.Count > 0)
                yield return $"Walls above standing on this wall: {string.Join(", ", r.FromAbove.Select(f => f.From).Distinct())}. Their hold-down " +
                             $"forces (± {r.FromAbove.Max(f => Math.Abs(f.Asd)):0} lb at their posts) add Σ F × (distance to the compression post) / d = " +
                             $"{r.TensionFromAboveAsd:0} lb (own {r.OwnTensionAsd:0} lb). Tie each upper hold-down down to this wall (strap or rod), not just to the floor.";
            yield return !r.HoldownNeeded ? "No uplift: no hold-down needed for overturning."
                : r.Holdown == null ? $"✗ No {HoldownName(d.Holdown)} in the {(r.HoldownFromStandardList ? "company standard list" : "catalog")} reaches {r.TensionAsd:0} lb: lengthen the wall, add dead load or use another type" +
                                      (r.HoldownFromStandardList ? " (or add a stronger option to the standard list, or switch to the full catalog)." : ".")
                : $"{HoldownName(d.Holdown)}{(r.HoldownChosen ? " (engineer's choice)" : r.HoldownFromStandardList ? " (standard list)" : "")}: {r.Holdown} — allowable {r.Holdown.Allowable:0} lb ({(d.Species == WoodTypes.WOODTYPE_SPF_HF ? "SPF/HF" : "DF/SP")}) → ratio {r.HoldownRatio:0.00} {mark(r.HoldownOk)}" +
                  (r.Holdown.Conditions != null ? $" [{r.Holdown.Conditions}]" : "") +
                  (r.HoldownChosen && !r.HoldownOk ? $" — weaker than T {r.TensionAsd:0} lb: pick a larger model or Auto." : "");
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
