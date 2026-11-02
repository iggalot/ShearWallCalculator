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
    /// The "Walls &amp; diaphragms" tab: every drawn wall and diaphragm with its coordinates, which can be typed in. A wall
    /// keeps its direction: its start point and length are edited and the end follows (east of the start for an E-W wall,
    /// north for an N-S wall). A diaphragm is edited by its lower-left corner and its size. Clicking a row selects the
    /// shape (it is highlighted on the drawing); × deletes it.
    /// </summary>
    public class GeometryPanel : UserControl
    {
        /// <summary>A wall or diaphragm was edited or deleted; the host recalculates and calls <see cref="Show"/>.</summary>
        public event EventHandler GeometryChanged;

        /// <summary>A row was clicked: ("Wall" or "Diaphragm", id).</summary>
        public event Action<string, int> SelectionChanged;

        /// <summary>The × on a row: ("Wall" or "Diaphragm", id).</summary>
        public event Action<string, int> DeleteRequested;

        /// <summary>
        /// True when the wall heights come from the building model's levels (the story height, or from the level that braces a
        /// taller wall): the column is shown but not edited here.
        /// </summary>
        public bool HeightsFromLevels { get; set; }

        private readonly SortState wallSort = new SortState(), diaphragmSort = new SortState();

        private readonly Grid wallsHost = new Grid();
        private readonly Grid diaphragmsHost = new Grid();
        private readonly TextBlock tbWallsTitle = Ui.Heading("Walls");
        private readonly TextBlock tbDiaphragmsTitle = Ui.Heading("Diaphragms");
        private readonly Grid beamsHost = new Grid(), columnsHost = new Grid();
        private readonly TextBlock tbBeamsTitle = Ui.Heading("Beams"), tbColumnsTitle = Ui.Heading("Columns");
        private readonly SortState beamSort = new SortState(), columnSort = new SortState();
        private UiTable beamTable, columnTable;
        private IList<FramingRow> beamRows = new List<FramingRow>(), columnRows = new List<FramingRow>();
        private string selectedFraming;

        /// <summary>A beam or column row was clicked: its model ID (BM1, C2...).</summary>
        public event Action<string> FramingSelected;

        /// <summary>One beam or column of this level, as listed (the drawing and the Building Model editor edit them).</summary>
        public sealed class FramingRow
        {
            public string Id { get; set; }
            /// <summary>A beam's start, or a column's location.</summary>
            public Point Start { get; set; }
            public Point End { get; set; }
            public double Length { get; set; }
            /// <summary>Column: "the foundation to Level 2".</summary>
            public string Span { get; set; }
            /// <summary>What it carries from the load path (hold-downs, reactions), or empty.</summary>
            public string Carries { get; set; }
        }
        private ShearWallCalculatorBase calc;
        private UiTable wallTable, diaphragmTable;

        public GeometryPanel()
        {
            FontFamily = new FontFamily("Segoe UI");
            FontSize = 12;
            Background = Ui.Surface;

            var left = new DockPanel { Margin = new Thickness(0, 0, 14, 0) };
            DockPanel.SetDock(tbWallsTitle, Dock.Top);
            left.Children.Add(tbWallsTitle);
            left.Children.Add(wallsHost);

            // the right column: the diaphragms, then the beams and columns of this level (each table takes its share)
            var right = new Grid();
            foreach (var (title, host) in new (TextBlock, Grid)[] { (tbDiaphragmsTitle, diaphragmsHost), (tbBeamsTitle, beamsHost), (tbColumnsTitle, columnsHost) })
            {
                right.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                right.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                Grid.SetRow(title, right.RowDefinitions.Count - 2);
                Grid.SetRow(host, right.RowDefinitions.Count - 1);
                host.Margin = new Thickness(0, 0, 0, 10);
                right.Children.Add(title);
                right.Children.Add(host);
            }

            var columns = new Grid();
            columns.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            columns.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            Grid.SetColumn(right, 1);
            columns.Children.Add(left);
            columns.Children.Add(right);

            var note = Ui.Note("Coordinates in feet from the drawing origin (x to the right, y up). Type a value and press Enter. " +
                               "A wall keeps its direction: edit its start point and length; the end follows. Click a row to highlight it on the drawing.");
            var root = new DockPanel { Margin = new Thickness(12, 10, 12, 10) };
            DockPanel.SetDock(note, Dock.Top);
            root.Children.Add(note);
            root.Children.Add(columns);
            Content = root;
        }

        public string SelectedKind { get; private set; }
        public int SelectedId { get; private set; } = -1;

        public void Show(ShearWallCalculatorBase calculator, string selectedKind, int selectedId)
        {
            calc = calculator;
            SelectedKind = selectedKind;
            SelectedId = selectedId;
            BuildWalls();
            BuildDiaphragms();
            BuildFraming();
        }

        /// <summary>The beams in this level's framing and the columns standing under it, with the selected one (model ID).</summary>
        public void ShowFraming(IList<FramingRow> beams, IList<FramingRow> columns, string selectedId)
        {
            beamRows = beams ?? new List<FramingRow>();
            columnRows = columns ?? new List<FramingRow>();
            selectedFraming = selectedId;
            BuildFraming();
        }

        private void BuildFraming()
        {
            beamsHost.Children.Clear();
            columnsHost.Children.Clear();
            beamTable = columnTable = null;
            tbBeamsTitle.Text = $"Beams ({beamRows.Count})";
            tbColumnsTitle.Text = $"Columns ({columnRows.Count})";
            const string edit = "Click a row to select it on the drawing; move or edit it there (or in the Building Model editor).";
            if (beamRows.Count == 0)
                beamsHost.Children.Add(Ui.Banner("No beams in this level's framing. Draw them in the Building Model editor (Beam, B).", warning: false));
            else
            {
                beamTable = new UiTable(beamSort,
                    ("Beam", 52, "Beam ID (shown on the drawing). " + edit),
                    ("Start x", 60, "Start point (ft)"), ("Start y", 60, ""),
                    ("End x", 60, "End point (ft)"), ("End y", 60, ""),
                    ("Length", 58, "Span (ft)"),
                    ("Carries", 0, "The hold-downs that land on it, and its end reactions (ASD)"));
                beamTable.RowClicked += i => SelectFraming(beamRows, i);
                for (int i = 0; i < beamRows.Count; i++)
                {
                    var b = beamRows[i];
                    int r = beamTable.AddRow(i, b.Id == selectedFraming);
                    int c = 0;
                    beamTable.Text(r, c++, b.Id, i, bold: true, right: false);
                    beamTable.Text(r, c++, Ui.Num(b.Start.X, "0.##"), i, Ui.InkSecondary);
                    beamTable.Text(r, c++, Ui.Num(b.Start.Y, "0.##"), i, Ui.InkSecondary);
                    beamTable.Text(r, c++, Ui.Num(b.End.X, "0.##"), i, Ui.InkSecondary);
                    beamTable.Text(r, c++, Ui.Num(b.End.Y, "0.##"), i, Ui.InkSecondary);
                    beamTable.Text(r, c++, Ui.Num(b.Length, "0.##"), i, Ui.InkSecondary);
                    beamTable.Text(r, c++, b.Carries ?? "", i, Ui.InkSecondary, right: false);
                }
                beamsHost.Children.Add(beamTable.InFrame());
            }
            if (columnRows.Count == 0)
                columnsHost.Children.Add(Ui.Banner("No columns under this level. Draw them in the Building Model editor (Column, P).", warning: false));
            else
            {
                columnTable = new UiTable(columnSort,
                    ("Column", 56, "Column ID (shown on the drawing). " + edit),
                    ("x", 60, "Location (ft)"), ("y", 60, ""),
                    ("Runs", 170, "From its base up to its top"),
                    ("Carries", 0, "The hold-downs and beam ends that land on it (ASD)"));
                columnTable.RowClicked += i => SelectFraming(columnRows, i);
                for (int i = 0; i < columnRows.Count; i++)
                {
                    var p = columnRows[i];
                    int r = columnTable.AddRow(i, p.Id == selectedFraming);
                    int c = 0;
                    columnTable.Text(r, c++, p.Id, i, bold: true, right: false);
                    columnTable.Text(r, c++, Ui.Num(p.Start.X, "0.##"), i, Ui.InkSecondary);
                    columnTable.Text(r, c++, Ui.Num(p.Start.Y, "0.##"), i, Ui.InkSecondary);
                    columnTable.Text(r, c++, p.Span ?? "", i, Ui.InkSecondary, right: false);
                    columnTable.Text(r, c++, p.Carries ?? "", i, Ui.InkSecondary, right: false);
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

        private void BuildWalls()
        {
            wallsHost.Children.Clear();
            var walls = calc?._wall_system?._walls;
            tbWallsTitle.Text = $"Walls ({walls?.Count ?? 0})";
            if (walls == null || walls.Count == 0)
            {
                wallsHost.Children.Add(Ui.Banner("No walls yet. Pick Wall (L) on the toolbar and click each wall's two ends on the drawing.", warning: false));
                return;
            }

            wallTable = new UiTable(wallSort,
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
            wallTable.RowClicked += id => Select("Wall", id);

            foreach (var kv in walls.OrderBy(k => k.Key))
            {
                int id = kv.Key;
                var w = kv.Value;
                bool ew = w.WallDir == WallDirs.EastWest;
                int r = wallTable.AddRow(id, SelectedKind == "Wall" && SelectedId == id);
                int c = 0;
                wallTable.Text(r, c++, id.ToString(), id, bold: true, right: false);
                wallTable.Text(r, c++, ew ? "E-W" : "N-S", id, Ui.InkSecondary, right: false);
                wallTable.Number(r, c++, w.Start.X, v => MoveWall(w, v, w.Start.Y, w.WallLength), id);
                wallTable.Number(r, c++, w.Start.Y, v => MoveWall(w, w.Start.X, v, w.WallLength), id);
                wallTable.Number(r, c++, w.WallLength, v => v > 0 && MoveWall(w, w.Start.X, w.Start.Y, v), id);
                if (HeightsFromLevels)
                    wallTable.Text(r, c++, Ui.Num(w.WallHeight, "0.##"), id, Ui.InkSecondary, tip: "From the building model's levels: change the plate heights in the Building Model editor (Floors… or Edit building)");
                else
                    wallTable.Number(r, c++, w.WallHeight, v => { if (v <= 0) return false; w.WallHeight = v; Changed(); return true; }, id);
                wallTable.Text(r, c++, Ui.Num(w.End.X, "0.##"), id, Ui.InkSecondary);
                wallTable.Text(r, c++, Ui.Num(w.End.Y, "0.##"), id, Ui.InkSecondary);
                wallTable.Text(r, c++, Ui.Num(w.WallRigidity, "0.000"), id, Ui.InkSecondary);
                wallTable.Put(r, c++, Ui.DeleteButton(() => Delete("Wall", id), $"Delete wall {id}"));
            }
            wallsHost.Children.Add(wallTable.InFrame());
        }

        private void BuildDiaphragms()
        {
            diaphragmsHost.Children.Clear();
            var dias = calc?._diaphragm_system?._diaphragms;
            tbDiaphragmsTitle.Text = $"Diaphragms ({dias?.Count ?? 0})";
            if (dias == null || dias.Count == 0)
            {
                diaphragmsHost.Children.Add(Ui.Banner("No diaphragm yet. Pick Diaphragm (R) on the toolbar and click two opposite corners of the floor or roof.", warning: false));
                return;
            }

            diaphragmTable = new UiTable(diaphragmSort,
                ("#", 34, "Diaphragm number (shown on the drawing)"),
                ("x", 62, "Lower-left corner x (ft)"),
                ("y", 62, "Lower-left corner y (ft)"),
                ("Width", 62, "Size in x (ft)"),
                ("Depth", 62, "Size in y (ft)"),
                ("Area", 64, "Plan area (ft²)"),
                ("", 30, ""));
            diaphragmTable.RowClicked += id => Select("Diaphragm", id);

            foreach (var kv in dias.OrderBy(k => k.Key))
            {
                int id = kv.Key;
                var d = kv.Value;
                double x = d.P1.X, y = d.P1.Y, wid = d.P3.X - d.P1.X, dep = d.P3.Y - d.P1.Y;
                int r = diaphragmTable.AddRow(id, SelectedKind == "Diaphragm" && SelectedId == id);
                int c = 0;
                diaphragmTable.Text(r, c++, id.ToString(), id, bold: true, right: false);
                diaphragmTable.Number(r, c++, x, v => Resize(d, v, y, wid, dep), id);
                diaphragmTable.Number(r, c++, y, v => Resize(d, x, v, wid, dep), id);
                diaphragmTable.Number(r, c++, wid, v => v > 0 && Resize(d, x, y, v, dep), id);
                diaphragmTable.Number(r, c++, dep, v => v > 0 && Resize(d, x, y, wid, v), id);
                diaphragmTable.Text(r, c++, Ui.Num(d.Area, "0"), id, Ui.InkSecondary);
                diaphragmTable.Put(r, c++, Ui.DeleteButton(() => Delete("Diaphragm", id), $"Delete diaphragm {id}"));
            }
            diaphragmsHost.Children.Add(diaphragmTable.InFrame());
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

        private bool MoveWall(WallData w, double x, double y, double length)
        {
            if (!SetWallPosition(w, x, y, length)) return false;
            Changed();
            return true;
        }

        private bool Resize(DiaphragmData_Rectangular d, double x, double y, double width, double depth)
        {
            if (!ResizeDiaphragm(d, x, y, width, depth)) return false;
            Changed();
            return true;
        }

        private void Select(string kind, int id)
        {
            SelectedKind = kind;
            SelectedId = id;
            wallTable?.Select(kind == "Wall" ? id : -1);
            diaphragmTable?.Select(kind == "Diaphragm" ? id : -1);
            selectedFraming = null;
            beamTable?.Select(-1);
            columnTable?.Select(-1);
            SelectionChanged?.Invoke(kind, id);
        }

        /// <summary>Raised after the current input event finishes, so the host can rebuild this panel safely.</summary>
        private void Changed()
        {
            calc?._wall_system?.Update();
            calc?._diaphragm_system?.Update();
            Dispatcher.BeginInvoke(new Action(() => GeometryChanged?.Invoke(this, EventArgs.Empty)), DispatcherPriority.Background);
        }

        /// <summary>The host deletes (one place for delete, undo and the selection), after this click finishes.</summary>
        private void Delete(string kind, int id) =>
            Dispatcher.BeginInvoke(new Action(() => DeleteRequested?.Invoke(kind, id)), DispatcherPriority.Background);
    }
}
