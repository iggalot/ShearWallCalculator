using calculator;
using ShearWallCalculator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace ShearWallVisualizer.Controls
{
    /// <summary>
    /// The "Members" tab: every wall, diaphragm, beam and column of the building, on every level, each with its level (the
    /// tables start sorted by level; any header sorts). Wall and diaphragm coordinates can be typed in: a wall keeps its
    /// direction (its start point and length are edited and the end follows, east of the start for an E-W wall, north for
    /// an N-S wall); a diaphragm is edited by its lower-left corner and its size. Beams and columns are listed with what they
    /// carry and are edited on the drawing. Clicking a row shows its level and selects the member on the drawing; × deletes.
    /// </summary>
    public class GeometryPanel : UserControl
    {
        /// <summary>One level's walls and diaphragms (its calculator).</summary>
        public sealed class LevelSet
        {
            public int Story { get; set; }
            public string Name { get; set; }
            public double Elevation { get; set; }
            public ShearWallCalculatorBase Calculator { get; set; }
        }

        /// <summary>One beam or column, as listed (the drawing and the Building Model editor edit them).</summary>
        public sealed class FramingRow
        {
            public string Id { get; set; }
            /// <summary>The level it is listed under: a beam's level; a column's top.</summary>
            public string Level { get; set; }
            public double Elevation { get; set; }
            /// <summary>A beam's start, or a column's location.</summary>
            public Point Start { get; set; }
            public Point End { get; set; }
            public double Length { get; set; }
            /// <summary>Column: "foundation to Level 2".</summary>
            public string Span { get; set; }
            /// <summary>The stories it is part of (a beam: its own; a column: base to top), for the level filter.</summary>
            public int FromStory { get; set; }
            public int ToStory { get; set; }
            /// <summary>What it carries from the load path (hold-downs, reactions), or empty.</summary>
            public string Carries { get; set; }
        }

        /// <summary>A wall or diaphragm of that story was edited; the host stores it and calls <see cref="Show"/>.</summary>
        public event Action<int> GeometryChanged;

        /// <summary>A wall or diaphragm row was clicked: ("Wall" or "Diaphragm", story, key).</summary>
        public event Action<string, int, int> SelectionChanged;

        /// <summary>The × on a row: ("Wall" or "Diaphragm", story, key).</summary>
        public event Action<string, int, int> DeleteRequested;

        /// <summary>A beam or column row was clicked: its model ID (BM1, C2...).</summary>
        public event Action<string> FramingSelected;

        /// <summary>The × on a beam or column row: its model ID.</summary>
        public event Action<string> FramingDeleteRequested;

        /// <summary>The level filter: <see cref="ThisLevel"/> (follows the level shown), <see cref="AllLevels"/>, or a story.</summary>
        private const int ThisLevel = -1, AllLevels = -2;
        private int filter = ThisLevel;
        private readonly ComboBox cbFilter = new ComboBox { MinWidth = 170, Margin = new Thickness(6, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        private bool fillingFilter;

        /// <summary>
        /// True when the wall heights come from the building model's levels (the story height, or from the level that braces a
        /// taller wall): the column is shown but not edited here.
        /// </summary>
        public bool HeightsFromLevels { get; set; }

        /// <summary>
        /// The geometry is the Building Model's (the shear wall app's sketch is locked): every coordinate is shown, not edited
        /// here, and no row has a delete button; the rows still pick their member on the drawing.
        /// </summary>
        public bool GeometryLocked { get; set; }

        private const string LockedTip = "Drawn in the Building Model: move or resize it there (Model > Edit building).";

        /// <summary>A coordinate cell: a number to type in, or (the geometry locked) the value shown.</summary>
        private void Coordinate(UiTable t, int r, int c, double v, Func<double, bool> set, int key)
        {
            if (GeometryLocked) t.Text(r, c, Ui.Num(v, "0.##"), key, Ui.InkSecondary, tip: LockedTip);
            else t.Number(r, c, v, set, key);
        }

        // the tables start sorted by the level column (0), and keep the user's sort across rebuilds
        private readonly SortState wallSort = new SortState { Column = 0 }, diaphragmSort = new SortState { Column = 0 },
                                   beamSort = new SortState { Column = 0 }, columnSort = new SortState { Column = 0 };

        private readonly Grid wallsHost = new Grid(), diaphragmsHost = new Grid(), beamsHost = new Grid(), columnsHost = new Grid();
        private readonly TextBlock tbWallsTitle = Ui.Heading("Walls"), tbDiaphragmsTitle = Ui.Heading("Diaphragms"),
                                   tbBeamsTitle = Ui.Heading("Beams"), tbColumnsTitle = Ui.Heading("Columns");
        private IList<LevelSet> levels = new List<LevelSet>();
        private IList<FramingRow> beamRows = new List<FramingRow>(), columnRows = new List<FramingRow>();
        private UiTable wallTable, diaphragmTable, beamTable, columnTable;
        private int selectedStory = -1;
        private string selectedFraming;

        /// <summary>A row's key in a table: the story and the calculator key (a wall spanning stories is on each).</summary>
        private const int StoryStride = 1000000;
        private static int RowKey(int story, int id) => story * StoryStride + id;

        public GeometryPanel()
        {
            FontFamily = new FontFamily("Segoe UI");
            FontSize = 12;
            Background = Ui.Surface;

            // the four tables one under the other, each its full height, in one scroll: they fit any window width
            var stack = new StackPanel { Orientation = Orientation.Vertical };
            foreach (var (title, host) in new (TextBlock, Grid)[] { (tbWallsTitle, wallsHost), (tbDiaphragmsTitle, diaphragmsHost), (tbBeamsTitle, beamsHost), (tbColumnsTitle, columnsHost) })
            {
                host.Margin = new Thickness(0, 0, 0, 14);
                host.HorizontalAlignment = HorizontalAlignment.Left;
                stack.Children.Add(title);
                stack.Children.Add(host);
            }
            var columns = new ScrollViewer
            {
                Content = stack, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            };
            // the tables' own scroll boxes never need to scroll here (they get their full height): the wheel scrolls the page
            columns.PreviewMouseWheel += (s, e) =>
            {
                columns.ScrollToVerticalOffset(columns.VerticalOffset - e.Delta / 3.0);
                e.Handled = true;
            };

            var note = Ui.Note("Every member of the building, on every level. Click a column header to sort (Level sorts bottom to top). " +
                               "Coordinates in feet from the drawing origin (x to the right, y up): type a wall's or diaphragm's value and press Enter; " +
                               "a wall keeps its direction (edit its start point and length; the end follows). Beams and columns are edited on the drawing. " +
                               "Click a row to show its level and select it on the drawing.");
            var bar = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
            bar.Children.Add(new TextBlock { Text = "Show:", VerticalAlignment = VerticalAlignment.Center, Foreground = Ui.InkSecondary });
            bar.Children.Add(cbFilter);
            cbFilter.SelectionChanged += (s, e) =>
            {
                if (fillingFilter || !(cbFilter.SelectedItem is ComboBoxItem it) || !(it.Tag is int f)) return;
                filter = f;
                BuildWalls();
                BuildDiaphragms();
                BuildFraming();
            };
            var root = new DockPanel { Margin = new Thickness(12, 10, 12, 10) };
            DockPanel.SetDock(note, Dock.Top);
            DockPanel.SetDock(bar, Dock.Top);
            root.Children.Add(note);
            root.Children.Add(bar);
            root.Children.Add(columns);
            Content = root;
        }

        public string SelectedKind { get; private set; }
        public int SelectedId { get; private set; } = -1;

        /// <summary>Every level's walls and diaphragms; the selection is a wall or diaphragm of <paramref name="story"/>.</summary>
        public void Show(IList<LevelSet> levelSets, int story, string selectedKind, int selectedId)
        {
            levels = levelSets ?? new List<LevelSet>();
            selectedStory = story;
            SelectedKind = selectedKind;
            SelectedId = selectedId;
            FillFilter();
            BuildWalls();
            BuildDiaphragms();
        }

        /// <summary>Every beam and column, with the selected one (model ID).</summary>
        public void ShowFraming(IList<FramingRow> beams, IList<FramingRow> columns, string selectedId)
        {
            beamRows = beams ?? new List<FramingRow>();
            columnRows = columns ?? new List<FramingRow>();
            selectedFraming = selectedId;
            BuildFraming();
        }

        private bool IsSelected(string kind, int story, int id) => SelectedKind == kind && SelectedId == id && selectedStory == story;

        /// <summary>The story the filter shows, or null for every level.</summary>
        private int? Shown => filter == AllLevels ? (int?)null : filter == ThisLevel ? selectedStory : filter;

        private bool ShowsStory(int story) => Shown == null || Shown == story;
        private bool ShowsSpan(int from, int to) => Shown == null || (Shown >= from && Shown <= to);

        /// <summary>The filter's choices: this level (named), all levels, each level; the choice is kept.</summary>
        private void FillFilter()
        {
            fillingFilter = true;
            try
            {
                cbFilter.Items.Clear();
                string current = levels.FirstOrDefault(l => l.Story == selectedStory)?.Name;
                cbFilter.Items.Add(new ComboBoxItem { Content = current == null ? "This level" : $"This level ({current})", Tag = ThisLevel, ToolTip = "Follows the level shown on the drawing" });
                cbFilter.Items.Add(new ComboBoxItem { Content = "All levels", Tag = AllLevels });
                foreach (var l in levels.OrderBy(l => l.Elevation)) cbFilter.Items.Add(new ComboBoxItem { Content = l.Name, Tag = l.Story });
                if (filter >= 0 && !levels.Any(l => l.Story == filter)) filter = ThisLevel; // that level is gone
                cbFilter.SelectedItem = cbFilter.Items.Cast<ComboBoxItem>().First(i => (int)i.Tag == filter);
            }
            finally { fillingFilter = false; }
        }

        private void LevelCell(UiTable t, int r, string name, double elevation, int key)
        {
            t.Text(r, 0, name ?? "", key, Ui.InkSecondary, right: false);
            t.SortKey(r, 0, elevation); // bottom to top, not alphabetical (Level 10 after Level 9)
        }

        private void BuildWalls()
        {
            wallsHost.Children.Clear();
            wallTable = null;
            int count = levels.Where(l => ShowsStory(l.Story)).Sum(l => l.Calculator?._wall_system?._walls.Count ?? 0);
            tbWallsTitle.Text = $"Walls ({count})";
            if (count == 0)
            {
                wallsHost.Children.Add(Ui.Banner(GeometryLocked ? "No walls on this level. They are drawn in the Building Model (Model > Edit building)." : "No walls yet. Pick Wall (L) on the toolbar and click each wall's two ends on the drawing.", warning: false));
                return;
            }

            wallTable = new UiTable(wallSort,
                ("Level", 76, "The level on top of the wall (the walls drawn on that level's plan)"),
                ("#", 34, "Wall number (shown on the drawing)"),
                ("Dir", 44, "E-W walls run left-right and resist loads in x; N-S walls run up-down and resist loads in y"),
                ("Start x", 66, "Start point x (ft): the left end of an E-W wall, the bottom end of an N-S wall"),
                ("Start y", 66, "Start point y (ft)"),
                ("Length", 62, "Wall length (ft)"),
                ("Height", 58, "Wall height (ft); used for the rigidity and the overturning"),
                ("End x", 56, "End point (follows the start and length)"),
                ("End y", 56, ""),
                ("Rigidity", 60, "Relative rigidity k = 1 / (0.4 (h/L)³ + 0.3 (h/L))"),
                ("", 30, ""));
            wallTable.RowClicked += k => Select("Wall", k / StoryStride, k % StoryStride);

            foreach (var lv in levels.Where(l => ShowsStory(l.Story)))
            {
                var walls = lv.Calculator?._wall_system?._walls;
                if (walls == null) continue;
                int story = lv.Story;
                foreach (var kv in walls.OrderBy(k => k.Key))
                {
                    int id = kv.Key, key = RowKey(story, id);
                    var w = kv.Value;
                    var calc = lv.Calculator;
                    bool ew = w.WallDir == WallDirs.EastWest;
                    int r = wallTable.AddRow(key, IsSelected("Wall", story, id));
                    int c = 1;
                    LevelCell(wallTable, r, lv.Name, lv.Elevation, key);
                    wallTable.Text(r, c++, id.ToString(), key, bold: true, right: false);
                    wallTable.Text(r, c++, ew ? "E-W" : "N-S", key, Ui.InkSecondary, right: false);
                    Coordinate(wallTable, r, c++, w.Start.X, v => MoveWall(story, calc, w, v, w.Start.Y, w.WallLength), key);
                    Coordinate(wallTable, r, c++, w.Start.Y, v => MoveWall(story, calc, w, w.Start.X, v, w.WallLength), key);
                    Coordinate(wallTable, r, c++, w.WallLength, v => v > 0 && MoveWall(story, calc, w, w.Start.X, w.Start.Y, v), key);
                    if (HeightsFromLevels || GeometryLocked)
                        wallTable.Text(r, c++, Ui.Num(w.WallHeight, "0.##"), key, Ui.InkSecondary, tip: "From the building model's levels: change the plate heights in the Building Model editor (Floors… or Edit building)");
                    else
                        wallTable.Number(r, c++, w.WallHeight, v => { if (v <= 0) return false; w.WallHeight = v; Changed(story, calc); return true; }, key);
                    wallTable.Text(r, c++, Ui.Num(w.End.X, "0.##"), key, Ui.InkSecondary);
                    wallTable.Text(r, c++, Ui.Num(w.End.Y, "0.##"), key, Ui.InkSecondary);
                    wallTable.Text(r, c++, Ui.Num(w.WallRigidity, "0.000"), key, Ui.InkSecondary);
                    if (!GeometryLocked) wallTable.Put(r, c, Ui.DeleteButton(() => Delete("Wall", story, id), $"Delete wall {id} ({lv.Name})"));
                }
            }
            wallsHost.Children.Add(wallTable.InFrame());
        }

        private void BuildDiaphragms()
        {
            diaphragmsHost.Children.Clear();
            diaphragmTable = null;
            int count = levels.Where(l => ShowsStory(l.Story)).Sum(l => l.Calculator?._diaphragm_system?._diaphragms.Count ?? 0);
            tbDiaphragmsTitle.Text = $"Diaphragms ({count})";
            if (count == 0)
            {
                diaphragmsHost.Children.Add(Ui.Banner(GeometryLocked ? "No diaphragm on this level. Floors and roofs are drawn in the Building Model (Model > Edit building)." : "No diaphragm yet. Pick Diaphragm (R) on the toolbar and click two opposite corners of the floor or roof.", warning: false));
                return;
            }

            diaphragmTable = new UiTable(diaphragmSort,
                ("Level", 76, "The floor or roof it is"),
                ("#", 34, "Diaphragm number (shown on the drawing)"),
                ("x", 62, "Lower-left corner x (ft)"),
                ("y", 62, "Lower-left corner y (ft)"),
                ("Width", 62, "Size in x (ft)"),
                ("Depth", 62, "Size in y (ft)"),
                ("Area", 64, "Plan area (ft²)"),
                ("", 30, ""));
            diaphragmTable.RowClicked += k => Select("Diaphragm", k / StoryStride, k % StoryStride);

            foreach (var lv in levels.Where(l => ShowsStory(l.Story)))
            {
                var dias = lv.Calculator?._diaphragm_system?._diaphragms;
                if (dias == null) continue;
                int story = lv.Story;
                var calc = lv.Calculator;
                foreach (var kv in dias.OrderBy(k => k.Key))
                {
                    int id = kv.Key, key = RowKey(story, id);
                    var d = kv.Value;
                    double x = d.P1.X, y = d.P1.Y, wid = d.P3.X - d.P1.X, dep = d.P3.Y - d.P1.Y;
                    int r = diaphragmTable.AddRow(key, IsSelected("Diaphragm", story, id));
                    int c = 1;
                    LevelCell(diaphragmTable, r, lv.Name, lv.Elevation, key);
                    diaphragmTable.Text(r, c++, id.ToString(), key, bold: true, right: false);
                    Coordinate(diaphragmTable, r, c++, x, v => Resize(story, calc, d, v, y, wid, dep), key);
                    Coordinate(diaphragmTable, r, c++, y, v => Resize(story, calc, d, x, v, wid, dep), key);
                    Coordinate(diaphragmTable, r, c++, wid, v => v > 0 && Resize(story, calc, d, x, y, v, dep), key);
                    Coordinate(diaphragmTable, r, c++, dep, v => v > 0 && Resize(story, calc, d, x, y, wid, v), key);
                    diaphragmTable.Text(r, c++, Ui.Num(d.Area, "0"), key, Ui.InkSecondary);
                    if (!GeometryLocked) diaphragmTable.Put(r, c, Ui.DeleteButton(() => Delete("Diaphragm", story, id), $"Delete diaphragm {id} ({lv.Name})"));
                }
            }
            diaphragmsHost.Children.Add(diaphragmTable.InFrame());
        }

        private void BuildFraming()
        {
            beamsHost.Children.Clear();
            columnsHost.Children.Clear();
            beamTable = columnTable = null;
            var beams = beamRows.Where(b => ShowsSpan(b.FromStory, b.ToStory)).ToList();
            var cols = columnRows.Where(c => ShowsSpan(c.FromStory, c.ToStory)).ToList();
            tbBeamsTitle.Text = $"Beams ({beams.Count})";
            tbColumnsTitle.Text = $"Columns ({cols.Count})";
            const string edit = "Click a row to select it on the drawing; move or edit it there (or in the Building Model editor).";
            if (beams.Count == 0)
                beamsHost.Children.Add(Ui.Banner(beamRows.Count == 0 ? "No beams. Draw them in the Building Model editor (Beam, B)." : "No beams on this level.", warning: false));
            else
            {
                beamTable = new UiTable(beamSort,
                    ("Level", 76, "The level whose floor framing it is in"),
                    ("Beam", 52, "Beam ID (shown on the drawing). " + edit),
                    ("Start x", 60, "Start point (ft)"), ("Start y", 60, ""),
                    ("End x", 60, "End point (ft)"), ("End y", 60, ""),
                    ("Length", 58, "Span (ft)"),
                    ("Carries", 180, "The hold-downs that land on it, and its end reactions (ASD)"),
                    ("", 30, ""));
                beamTable.RowClicked += i => SelectFraming(beamRows, i);
                for (int i = 0; i < beamRows.Count; i++)
                {
                    var b = beamRows[i];
                    if (!beams.Contains(b)) continue;
                    int r = beamTable.AddRow(i, b.Id == selectedFraming);
                    int c = 1;
                    LevelCell(beamTable, r, b.Level, b.Elevation, i);
                    beamTable.Text(r, c++, b.Id, i, bold: true, right: false);
                    beamTable.Text(r, c++, Ui.Num(b.Start.X, "0.##"), i, Ui.InkSecondary);
                    beamTable.Text(r, c++, Ui.Num(b.Start.Y, "0.##"), i, Ui.InkSecondary);
                    beamTable.Text(r, c++, Ui.Num(b.End.X, "0.##"), i, Ui.InkSecondary);
                    beamTable.Text(r, c++, Ui.Num(b.End.Y, "0.##"), i, Ui.InkSecondary);
                    beamTable.Text(r, c++, Ui.Num(b.Length, "0.##"), i, Ui.InkSecondary);
                    beamTable.Text(r, c++, b.Carries ?? "", i, Ui.InkSecondary, right: false);
                    string bid = b.Id;
                    if (!GeometryLocked) beamTable.Put(r, c, Ui.DeleteButton(() => DeleteFraming(bid), $"Delete beam {bid}"));
                }
                beamsHost.Children.Add(beamTable.InFrame());
            }
            if (cols.Count == 0)
                columnsHost.Children.Add(Ui.Banner(columnRows.Count == 0 ? "No columns. Draw them in the Building Model editor (Column, P)." : "No columns on this level.", warning: false));
            else
            {
                columnTable = new UiTable(columnSort,
                    ("Level", 76, "The level at its top (the plan it is drawn under)"),
                    ("Column", 56, "Column ID (shown on the drawing). " + edit),
                    ("x", 60, "Location (ft)"), ("y", 60, ""),
                    ("Runs", 170, "From its base up to its top"),
                    ("Carries", 180, "The hold-downs and beam ends that land on it (ASD)"),
                    ("", 30, ""));
                columnTable.RowClicked += i => SelectFraming(columnRows, i);
                for (int i = 0; i < columnRows.Count; i++)
                {
                    var p = columnRows[i];
                    if (!cols.Contains(p)) continue;
                    int r = columnTable.AddRow(i, p.Id == selectedFraming);
                    int c = 1;
                    LevelCell(columnTable, r, p.Level, p.Elevation, i);
                    columnTable.Text(r, c++, p.Id, i, bold: true, right: false);
                    columnTable.Text(r, c++, Ui.Num(p.Start.X, "0.##"), i, Ui.InkSecondary);
                    columnTable.Text(r, c++, Ui.Num(p.Start.Y, "0.##"), i, Ui.InkSecondary);
                    columnTable.Text(r, c++, p.Span ?? "", i, Ui.InkSecondary, right: false);
                    columnTable.Text(r, c++, p.Carries ?? "", i, Ui.InkSecondary, right: false);
                    string cid = p.Id;
                    if (!GeometryLocked) columnTable.Put(r, c, Ui.DeleteButton(() => DeleteFraming(cid), $"Delete column {cid} (every story it runs through)"));
                }
                columnsHost.Children.Add(columnTable.InFrame());
            }
        }

        private void SelectFraming(IList<FramingRow> rows, int i)
        {
            if (i < 0 || i >= rows.Count) return;
            selectedFraming = rows[i].Id;
            SelectedKind = null;
            SelectedId = -1;
            wallTable?.Select(-1);
            diaphragmTable?.Select(-1);
            beamTable?.Select(ReferenceEquals(rows, beamRows) ? i : -1);
            columnTable?.Select(ReferenceEquals(rows, columnRows) ? i : -1);
            FramingSelected?.Invoke(selectedFraming);
        }

        /// <summary>Moves a wall to a new start point and length, keeping its direction.</summary>
        internal static bool SetWallPosition(WallData w, double x, double y, double length)
        {
            if (!(length > 0)) return false;
            w.Start = new Point(x, y);
            w.End = w.WallDir == WallDirs.EastWest ? new Point(x + length, y) : new Point(x, y + length);
            return true;
        }

        /// <summary>Sets a rectangular diaphragm from its lower-left corner and size.</summary>
        internal static bool ResizeDiaphragm(DiaphragmData_Rectangular d, double x, double y, double width, double depth)
        {
            if (!(width > 0) || !(depth > 0)) return false;
            d.P1 = new Point(x, y);
            d.P2 = new Point(x + width, y);
            d.P3 = new Point(x + width, y + depth);
            d.P4 = new Point(x, y + depth);
            d.Update();
            return true;
        }

        private bool MoveWall(int story, ShearWallCalculatorBase calc, WallData w, double x, double y, double length)
        {
            if (!SetWallPosition(w, x, y, length)) return false;
            Changed(story, calc);
            return true;
        }

        private bool Resize(int story, ShearWallCalculatorBase calc, DiaphragmData_Rectangular d, double x, double y, double width, double depth)
        {
            if (!ResizeDiaphragm(d, x, y, width, depth)) return false;
            Changed(story, calc);
            return true;
        }

        private void Select(string kind, int story, int id)
        {
            SelectedKind = kind;
            SelectedId = id;
            selectedStory = story;
            wallTable?.Select(kind == "Wall" ? RowKey(story, id) : -1);
            diaphragmTable?.Select(kind == "Diaphragm" ? RowKey(story, id) : -1);
            selectedFraming = null;
            beamTable?.Select(-1);
            columnTable?.Select(-1);
            SelectionChanged?.Invoke(kind, story, id);
        }

        /// <summary>Raised after the current input event finishes, so the host can rebuild this panel safely.</summary>
        private void Changed(int story, ShearWallCalculatorBase calc)
        {
            calc?._wall_system?.Update();
            calc?._diaphragm_system?.Update();
            Dispatcher.BeginInvoke(new Action(() => GeometryChanged?.Invoke(story)), DispatcherPriority.Background);
        }

        /// <summary>A beam or column: deleted from the model by the host, after this click finishes.</summary>
        private void DeleteFraming(string id) =>
            Dispatcher.BeginInvoke(new Action(() => FramingDeleteRequested?.Invoke(id)), DispatcherPriority.Background);

        /// <summary>The host deletes (one place for delete, undo and the selection), after this click finishes.</summary>
        private void Delete(string kind, int story, int id) =>
            Dispatcher.BeginInvoke(new Action(() => DeleteRequested?.Invoke(kind, story, id)), DispatcherPriority.Background);
    }
}
