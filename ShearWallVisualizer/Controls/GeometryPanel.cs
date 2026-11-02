using calculator;
using ShearWallCalculator;
using System;
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

            var right = new DockPanel();
            DockPanel.SetDock(tbDiaphragmsTitle, Dock.Top);
            right.Children.Add(tbDiaphragmsTitle);
            right.Children.Add(diaphragmsHost);

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
                    wallTable.Text(r, c++, Ui.Num(w.WallHeight, "0.##"), id, Ui.InkSecondary, tip: "From the levels: change the story height (Add floor, or the plate heights in Wind Setup)");
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
