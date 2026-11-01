using ShearWallCalculator.Diagnostics;
using ShearWallCalculator;
using ShearWallCalculator.Drawing;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace ShearWallVisualizer
{
    /// <summary>
    /// Drafting on the plan: undo / redo, delete, move, copy, measure, ortho, zoom to the drawing and panning.
    /// The edits themselves are in <see cref="DrawingEdits"/> and the history in <see cref="DrawingHistory"/> (both tested).
    /// </summary>
    public partial class MainWindow
    {
        /// <summary>One undo history per level (a new or opened project starts afresh).</summary>
        private readonly Dictionary<ShearWallCalculatorBase, DrawingHistory> histories = new Dictionary<ShearWallCalculatorBase, DrawingHistory>();
        private DrawingHistory history => histories.TryGetValue(Calculator, out var h) ? h : new DrawingHistory();
        private bool orthoMode;                    // F8: move, copy and measure go straight across or straight up
        private (Point a, Point b)? lastMeasure;   // the last measured distance, drawn until the next command
        private DrawMode lastDrawMode = DrawMode.Line; // Space or Enter repeats it
        private Point? panFrom;                    // middle-button drag

        private bool IsEditMode => currentMode == DrawMode.Move || currentMode == DrawMode.Copy || currentMode == DrawMode.Measure;
        private bool HasSelection => selectedKind != null && selectedId >= 0;

        // ------------------------------------------------------------------ undo / redo

        /// <summary>Called after every update: adds an undo step when the drawing changed.</summary>
        private void RecordHistory()
        {
            if (Calculator == null) return;
            if (!histories.ContainsKey(Calculator))
            {
                histories[Calculator] = new DrawingHistory();
                histories[Calculator].Reset(Calculator);
            }
            else history.Record(Calculator);
        }

        private void Undo_Click(object sender, RoutedEventArgs e) => UndoRedo(undo: true);
        private void Redo_Click(object sender, RoutedEventArgs e) => UndoRedo(undo: false);

        private void UndoRedo(bool undo)
        {
            if (Calculator == null) return;
            ResetInputMode();
            string what = undo ? history.Undo(Calculator) : history.Redo(Calculator);
            if (what == null)
            {
                ShowStatusMessage(LogLevel.Info, undo ? "Nothing to undo." : "Nothing to redo.");
                return;
            }
            if (HasSelection && DrawingEdits.BasePoint(Calculator, selectedKind, selectedId) == null) CloseSelectionCard();
            UpdateShearWallUI();
            ShowStatusMessage(LogLevel.Info, (undo ? "Undone: " : "Redone: ") + what +
                              $"   ({history.UndoSteps} to undo, {history.RedoSteps} to redo)");
        }

        // ------------------------------------------------------------------ the tables (Walls & diaphragms, Calculations, Design)

        /// <summary>A row picked in a table: the same wall or diaphragm is highlighted on the drawing (brought into view if it is off screen).</summary>
        private void SelectFromTable(string kind, int id)
        {
            selectedKind = kind;
            selectedId = id;
            Point? c = DrawingEdits.Center(Calculator, kind, id);
            if (c != null)
            {
                Point s = WorldToScreen(c.Value, m_layers);
                if (s.X < 0 || s.Y < 0 || s.X > m_layers.ActualWidth || s.Y > m_layers.ActualHeight)
                {
                    // centre it: u = (x − panX)·zoom, v = (H − (y − panY))·zoom
                    panOffsetX = c.Value.X - m_layers.ActualWidth / (2 * zoomFactorX);
                    panOffsetY = c.Value.Y - m_layers.ActualHeight + m_layers.ActualHeight / (2 * zoomFactorY);
                    InvalidateGrid();
                }
            }
            SetButtonModes();
            RedrawDrawing();
        }

        /// <summary>The × on a table row.</summary>
        private void DeleteFromTable(string kind, int id)
        {
            selectedKind = kind;
            selectedId = id;
            DeleteSelected();
        }

        /// <summary>After an undo, a delete in a table or an edit, the selection may name something that is gone.</summary>
        private void DropMissingSelection()
        {
            if (!HasSelection || DrawingEdits.Center(Calculator, selectedKind, selectedId) != null) return;
            selectedKind = null;
            selectedId = -1;
            selectionCardOpen = false;
        }

        // ------------------------------------------------------------------ delete

        private void Delete_Click(object sender, RoutedEventArgs e) => DeleteSelected();

        private void DeleteSelected()
        {
            if (!HasSelection)
            {
                ShowStatusMessage(LogLevel.Info, "Pick a wall or diaphragm first (Select), then Delete.");
                return;
            }
            string name = $"{selectedKind.ToLowerInvariant()} {selectedId}";
            if (!DrawingEdits.Delete(Calculator, selectedKind, selectedId)) return;
            ResetInputMode();
            CloseSelectionCard();
            ShowStatusMessage(LogLevel.Info, $"Deleted {name}.  Ctrl+Z brings it back.");
        }

        // ------------------------------------------------------------------ move, copy, measure

        private void btnMove_Click(object sender, RoutedEventArgs e) => StartEditMode(DrawMode.Move);
        private void btnCopy_Click(object sender, RoutedEventArgs e) => StartEditMode(DrawMode.Copy);
        private void btnMeasure_Click(object sender, RoutedEventArgs e) => StartEditMode(DrawMode.Measure);

        private void StartEditMode(DrawMode mode)
        {
            ResetInputMode();
            currentMode = mode;
            selectionCardOpen = false;
            selectionCard.Show(Calculator, null, selectedId, designResults);
            SetButtonModes();
            RedrawDrawing();
        }

        /// <summary>A clicked or typed point for Move, Copy or Measure.</summary>
        /// <param name="raw">The cursor without snaps (used to pick the wall or diaphragm), or null for a typed point.</param>
        private void EditPoint(Point p, Point? raw)
        {
            if (currentMode != DrawMode.Measure && !HasSelection)
            {
                if (raw == null)
                {
                    ShowStatusMessage(LogLevel.Warning, "Pick the wall or diaphragm first.");
                    return;
                }
                var (kind, id) = Snapper.Pick(Calculator, raw.Value, PixelsToWorld(6));
                if (kind == null) { ShowStatusMessage(LogLevel.Info, "Nothing there: click on a wall or diaphragm."); return; }
                selectedKind = kind;
                selectedId = id;
                UpdateShearWallUI();
                return;
            }
            if (startPoint_world == null)
            {
                startPoint_world = p;
                lastMeasure = null;
                currentSnap = FindSnap(currentSnap.Point);
                RedrawDrawing();
                return;
            }

            Point from = startPoint_world.Value;
            Point to = orthoMode ? DrawingEdits.Ortho(p, from) : p;
            Vector by = to - from;
            switch (currentMode)
            {
                case DrawMode.Measure:
                    lastMeasure = (from, to);
                    startPoint_world = null;
                    ShowStatusMessage(LogLevel.Info, $"Distance {by.Length:0.00} ft   (Δx {by.X:0.00}, Δy {by.Y:0.00})");
                    RedrawDrawing();
                    return;
                case DrawMode.Move:
                    DrawingEdits.Move(Calculator, selectedKind, selectedId, by);
                    ShowStatusMessage(LogLevel.Info, $"Moved {selectedKind.ToLowerInvariant()} {selectedId} by ({by.X:0.##}, {by.Y:0.##}) ft.");
                    ResetInputMode();
                    currentMode = DrawMode.None;
                    break;
                case DrawMode.Copy:
                    int id = DrawingEdits.Copy(Calculator, selectedKind, selectedId, by);
                    ShowStatusMessage(LogLevel.Info, $"Copied {selectedKind.ToLowerInvariant()} {selectedId} to {selectedKind.ToLowerInvariant()} {id}.  " +
                                                     "Click again for another copy; Esc ends.");
                    break; // stays in Copy from the same base point, for more copies
            }
            UpdateShearWallUI();
        }

        /// <summary>The second point of Move, Copy or Measure under the cursor (ortho applied).</summary>
        private Point EditSecondPoint(Point cursor) =>
            orthoMode && startPoint_world != null ? DrawingEdits.Ortho(cursor, startPoint_world.Value) : cursor;

        private static readonly Pen GhostPen = DashedPen(Color.FromRgb(0x2A, 0x78, 0xD6), 2);
        private static readonly Pen RubberPen = DashedPen(Color.FromRgb(0x52, 0x51, 0x4E), 1);
        private static readonly Pen MeasurePen = FrozenPen(Color.FromRgb(0xC2, 0x41, 0x0C), 1.5);

        private static Pen DashedPen(Color c, double thickness)
        {
            var pen = new Pen(new SolidColorBrush(c), thickness) { DashStyle = new DashStyle(new[] { 4.0, 3.0 }, 0) };
            pen.Freeze();
            return pen;
        }

        /// <summary>The preview of Move / Copy (the moved outline) and Measure (the line and its length). True when drawn.</summary>
        private bool DrawEditPreview(DrawingContext ctx)
        {
            if (lastMeasure != null) DrawMeasure(ctx, lastMeasure.Value.a, lastMeasure.Value.b);
            if (!IsEditMode || startPoint_world == null) return IsEditMode;

            Point from = startPoint_world.Value;
            Point to = EditSecondPoint(currentSnap.Point);
            if (currentMode == DrawMode.Measure)
            {
                DrawMeasure(ctx, from, to);
                return true;
            }
            ctx.DrawLine(RubberPen, WorldToScreen(from, m_layers), WorldToScreen(to, m_layers));
            Vector by = to - from;
            if (selectedKind == "Wall" && Calculator._wall_system._walls.TryGetValue(selectedId, out var w))
                ctx.DrawLine(GhostPen, WorldToScreen(w.Start + by, m_layers), WorldToScreen(w.End + by, m_layers));
            else if (selectedKind == "Diaphragm" && Calculator._diaphragm_system._diaphragms.TryGetValue(selectedId, out var d))
                ctx.DrawRectangle(null, GhostPen, new Rect(WorldToScreen(d.P1 + by, m_layers), WorldToScreen(d.P3 + by, m_layers)));
            return true;
        }

        private void DrawMeasure(DrawingContext ctx, Point a, Point b)
        {
            Point sa = WorldToScreen(a, m_layers), sb = WorldToScreen(b, m_layers);
            ctx.DrawLine(MeasurePen, sa, sb);
            ctx.DrawEllipse(null, MeasurePen, sa, 3, 3);
            ctx.DrawEllipse(null, MeasurePen, sb, 3, 3);
            var text = new FormattedText($"{(b - a).Length:0.00} ft", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                                         new Typeface("Segoe UI"), 12, MeasurePen.Brush, VisualTreeHelper.GetDpi(this).PixelsPerDip);
            Point mid = new Point((sa.X + sb.X) / 2 + 6, (sa.Y + sb.Y) / 2 - text.Height - 2);
            ctx.DrawRectangle(Brushes.White, null, new Rect(mid.X - 2, mid.Y, text.Width + 4, text.Height));
            ctx.DrawText(text, mid);
        }

        // ------------------------------------------------------------------ ortho, zoom, pan

        private void SetOrthoMode()
        {
            orthoMode = !orthoMode;
            ShowStatusMessage(LogLevel.Info, $"Ortho {(orthoMode ? "on" : "off")} (F8): move, copy and measure go straight across or straight up. Walls are always square.");
            SetButtonModes();
            RedrawDrawing();
        }

        private void btnOrtho_Click(object sender, RoutedEventArgs e) => SetOrthoMode();
        private void btnZoomExtents_Click(object sender, RoutedEventArgs e) => ZoomExtents();

        /// <summary>Fits the walls, diaphragms and the shown plan in the drawing area; the starting view when there is nothing yet.</summary>
        private void ZoomExtents()
        {
            Rect r = DrawingEdits.Extents(Calculator, includePlan: !hideImage);
            double width = m_layers.ActualWidth, height = m_layers.ActualHeight;
            if (r.IsEmpty || width <= 0 || height <= 0)
            {
                ResetView();
            }
            else
            {
                double w = Math.Max(r.Width, 1), h = Math.Max(r.Height, 1);
                double zoom = Math.Min(width / (w * 1.15), height / (h * 1.15));
                zoomFactorX = zoomFactorY = zoom;
                Point c = new Point(r.X + r.Width / 2, r.Y + r.Height / 2);
                // WorldToScreen: u = (x - panX)·zoom, v = (H - (y - panY))·zoom; put the centre of the box in the middle
                panOffsetX = c.X - width / (2 * zoom);
                panOffsetY = c.Y - height + height / (2 * zoom);
            }
            InvalidateGrid();
            ShowCursor(currentSnap.Point);
            RedrawDrawing();
        }

        /// <summary>The window or the divider changed the drawing's size: keep the top-left of the view where it was.</summary>
        private void DrawingResized(object sender, SizeChangedEventArgs e)
        {
            if (!e.HeightChanged && !e.WidthChanged) return;
            // v = (H − (y − panY))·zoom: a taller area would push the plan down; move the pan by the change so it stays put
            if (e.PreviousSize.Height > 0) panOffsetY -= e.NewSize.Height - e.PreviousSize.Height;
            InvalidateGrid();
            if (IsLoaded) Draw(ChangeType.Redraw);
        }

        /// <summary>Middle button: drag to pan; double-click to zoom to the drawing.</summary>
        private bool PanMouseDown(MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Middle) return false;
            if (e.ClickCount == 2) { ZoomExtents(); return true; }
            panFrom = e.GetPosition(m_layers);
            m_layers.CaptureMouse();
            Cursor = Cursors.SizeAll;
            return true;
        }

        private bool PanMouseMove(MouseEventArgs e)
        {
            if (panFrom == null) return false;
            if (e.MiddleButton != MouseButtonState.Pressed) { EndPan(); return false; }
            Point now = e.GetPosition(m_layers);
            Vector d = now - panFrom.Value;
            panOffsetX -= d.X / zoomFactorX;
            panOffsetY += d.Y / zoomFactorY;
            panFrom = now;
            InvalidateGrid();
            return true;
        }

        private void PanMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle) EndPan();
        }

        private void EndPan()
        {
            if (panFrom == null) return;
            panFrom = null;
            m_layers.ReleaseMouseCapture();
            Cursor = null;
        }

        // ------------------------------------------------------------------ keys

        /// <summary>Ctrl+Z / Ctrl+Y (and Ctrl+Shift+Z) on the drawing; a text box keeps its own undo.</summary>
        private bool UndoShortcut(KeyEventArgs e)
        {
            if (InWindWorkspace || e.OriginalSource is TextBoxBase) return false;
            bool shift = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;
            if (e.Key == Key.Z && !shift) { UndoRedo(undo: true); return true; }
            if (e.Key == Key.Y || (e.Key == Key.Z && shift)) { UndoRedo(undo: false); return true; }
            return false;
        }

        /// <summary>The drafting keys; true when the key was used.</summary>
        private bool DraftingKey(Key key)
        {
            switch (key)
            {
                case Key.Delete: DeleteSelected(); return true;
                case Key.M: StartEditMode(DrawMode.Move); return true;
                case Key.C: StartEditMode(DrawMode.Copy); return true;
                case Key.T: StartEditMode(DrawMode.Measure); return true;
                case Key.F8: SetOrthoMode(); return true;
                case Key.Z: ResetInputMode(); ZoomExtents(); return true;
                case Key.Space:
                case Key.Enter:
                    if (currentMode != DrawMode.None || startPoint_world != null) return false;
                    ResetInputMode();
                    currentMode = lastDrawMode; // repeat the last command
                    SetButtonModes();
                    RedrawDrawing();
                    return true;
            }
            return false;
        }
    }
}
