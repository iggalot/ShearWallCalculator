using calculator;
using ShearWallCalculator;
using ShearWallVisualizer.Controls;
using ShearWallVisualizer.Tabs;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using static ShearWallVisualizer.Controls.DiaphragmDataControl;

namespace ShearWallVisualizer
{
    public partial class MainWindow : Window
    {
        public EventHandler OnUpdated;  // the event that signals that the drawing has been updated -- controls will listen for this at the time they are created.

        TabItem dimTab;
        TabItem calcTab;

        CalculationResultsTab calcResultsTab;

        private bool initialized = false;  // is the initial window setup complete?

        bool hide_Text = false;
        bool hide_Grid = false;

        private double defaultWallHeight = 9.0;

        private enum DrawMode { None, Line, Rectangle }
        private DrawMode currentMode = DrawMode.None;
        private bool snapMode = false;
        private double snapThreshold = 50; // Pixels
        private bool debugMode = false;

        private Point? startPoint_world = null;  // the first click in world coordinates
        private Point? endPoint_world = null;    // the second click in world coordinates

        private List<DiaphragmSystem> diaphragmSystems = new List<DiaphragmSystem>();
        private List<WallSystem> wallSystems = new List<WallSystem>();

        private double zoomFactorX = 1.0;
        private double zoomFactorY = 1.0;
        private double panOffsetX = 0.0;
        private double panOffsetY = 0.0;

        private Shape previewShape = null;
        private Shape cursorShape = null;

        private TextBlock previewLengthLabel = null;
        private TextBlock previewCoordinateLabel = null;
        private Point lastPanPoint;
        private bool isPanning = false;

        private double worldWidth = 200;
        private double worldHeight = 200;

        private Point currentMouseScreenPosition = new Point(0, 0);

        public MainWindow()
        {
            InitializeComponent();

            this.KeyDown += MainWindow_KeyDown;

            // Set focus and canvas background
            this.Focusable = true;
            this.Focus();


            this.Loaded += (s, e) =>
            {
                if (initialized) return;

                initialized = true;

                ResetView(); // reset the view so that origin 0,0 is at lower left of the corner screen and the model is zoomed to fill the entire window

                Draw(ChangeType.Redraw);
            };

            CreateTabs();

            CreateDiaphragmDataControls();
        }

        private void CreateDiaphragmDataControls()
        {
            
            foreach(var dia_sys in diaphragmSystems)
            {
                DiaphragmSystemControl sysControl = new DiaphragmSystemControl(this, dia_sys);
                dimTab.Content = sysControl;
                sysControl.OnDiaphragmSubControlDeleted += DiaphragmDeleted;
            }
        }

        private void DiaphragmDeleted(object sender, EventArgs e)
        {
            DiaphragmDataControl control = sender as DiaphragmDataControl;
            DeleteDiaphragmEventArgs args = e as DeleteDiaphragmEventArgs;

            foreach (DiaphragmSystem dia_sys in diaphragmSystems)
            {
                foreach (var dia in dia_sys._diaphragms)
                {
                    if (dia.Key == args.Id)
                    {
                        dia_sys._diaphragms.Remove(dia.Key);
                        //MessageBox.Show("Diaphragm #" + args.Id + " deleted.");

                        Update();
                        return;
                    }
                }
            }
        }

        public void Update()
        {
            dimTab.Content = null;  // clear the tabs
            calcTab.Content = null;

            CreateDiaphragmDataControls();
            
            OnUpdated?.Invoke(this, EventArgs.Empty); // signal that the window has been updated -- so that subcontrols can refresh

            Draw(ChangeType.Redraw);

        }

        private void ResetView()
        {
            // Set zoom factors based on the visible area and world dimensions
            zoomFactorX = dockpanel.ActualWidth / worldWidth;
            zoomFactorY = dockpanel.ActualHeight / worldHeight;

            // Set the scale factors so both are the same -- for square grids
            zoomFactorX = Math.Min(zoomFactorX, zoomFactorY);
            zoomFactorY = zoomFactorX;

            // Compute current screen position of world (0,0)
            Point screenOrigin = WorldToScreen(new Point(0, 0), dockpanel);

            // Adjust pan offset so (0,0) appears in bottom-left corner (screen X=0, Y=height)
            panOffsetX += screenOrigin.X / zoomFactorX;
            panOffsetY += (dockpanel.ActualHeight - screenOrigin.Y) / zoomFactorY;
        }

        private void CreateTabs()
        {
            MainTabControl.Items.Clear();

            // Tab 1: Dimensions UserControl
            var dimensionsTab = new TabItem
            {
                Header = "Dimensions",
                Content = new DimensionsTab()
            };

            dimTab = dimensionsTab;

            // Tab 2: Simple Calculation Tab
            var calculationTab = new TabItem
            {
                Header = "Calculations",
                Content = new CalculationResultsTab()
            };

            calcTab = calculationTab;

            MainTabControl.Items.Add(dimensionsTab);
            MainTabControl.Items.Add(calculationTab);

            MainTabControl.SelectedIndex = 0; // Show Dimensions by default
        }

        #region MODE setters


        private void SetLineMode()
        {
            ResetUIButtons();
            btnRigidityMode.BorderThickness = new Thickness(3);
            btnRigidityMode.BorderBrush = new SolidColorBrush(Colors.Black);

            currentMode = DrawMode.Line;  // Set to Line drawing mode
            MessageBox.Show("Line mode activated.");
            Console.WriteLine("Line mode activated.");
        }

        private void SetRectangleMode()
        {
            ResetUIButtons();
            btnMassMode.BorderThickness = new Thickness(3);
            btnMassMode.BorderBrush = new SolidColorBrush(Colors.Black);

            currentMode = DrawMode.Rectangle;  // Set to Rectangle drawing mode
            MessageBox.Show("Rectangle mode activated.");
            Console.WriteLine("Rectangle mode activated.");
        }

        private void SetDebugMode()
        {
            ResetUIButtons();

            debugMode = !debugMode;
            MessageBox.Show($"Debug Mode {(debugMode ? "Enabled" : "Disabled")}");
            Console.WriteLine($"Debug Mode: {(debugMode ? "Enabled" : "Disabled")}");
        }
        private void SetSnapMode()
        {
            ResetUIButtons();
            btnSnapToNearest.BorderThickness = new Thickness(3);
            btnSnapToNearest.BorderBrush = new SolidColorBrush(Colors.Black);

            snapMode = !snapMode;
            MessageBox.Show($"Snap Mode {(snapMode ? "Enabled" : "Disabled")}");
            Console.WriteLine($"Snap Mode: {(snapMode ? "Enabled" : "Disabled")}");
        }

        /// <summary>
        /// Cancels input mode and clears the previews
        /// </summary>
        private void ResetInputMode()
        {
            // cancel the input by setting the start point to null
            startPoint_world = null;
            endPoint_world = null;
            previewShape = null;
        }

        private void ResetUIButtons()
        {
            btnRigidityMode.BorderThickness = new Thickness(0);
            btnMassMode.BorderThickness = new Thickness(0);
            btnSnapToNearest.BorderThickness = new Thickness(0);

            if (snapMode is true)
            {
                btnSnapToNearest.BorderThickness = new Thickness(3);
            }

            if (debugMode is true)
            {
                // TODO:: how should the UI be set in debug mode?
            }
        }
        #endregion



        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            Console.WriteLine($"Key Pressed: {e.Key}");

            if (e.Key == Key.L)
            {
                ResetInputMode();
                SetLineMode();
            }
            else if (e.Key == Key.R)
            {
                ResetInputMode();
                SetRectangleMode();
            }
            else if (e.Key == Key.S)
            {
                SetSnapMode();
            }
            else if (e.Key == Key.D)
            {
                SetDebugMode();
            }
            else
            {
                 if (e.Key == Key.Z)
                {
                    ResetInputMode();
                    ResetView();
                }
            }

            Draw(ChangeType.Redraw);
        }

        private Point GetSnappedPoint(Point worldPoint)
        {
            foreach (var wall_sys in wallSystems)
            {
                foreach (var wall in wall_sys._walls)
                {
                    if (IsWithinSnapThreshold(worldPoint, wall.Value.Start)) return wall.Value.Start;
                    if (IsWithinSnapThreshold(worldPoint, wall.Value.End)) return wall.Value.End;
                }
            }

            foreach (var dia_sys in diaphragmSystems)
            {
                foreach (var dia in dia_sys._diaphragms)
                {
                    if (IsWithinSnapThreshold(worldPoint, dia.Value.P1)) return dia.Value.P1;
                    if (IsWithinSnapThreshold(worldPoint, dia.Value.P2)) return dia.Value.P2;
                    if (IsWithinSnapThreshold(worldPoint, dia.Value.P3)) return dia.Value.P2;
                    if (IsWithinSnapThreshold(worldPoint, dia.Value.P4)) return dia.Value.P2;
                }
            }

            return worldPoint;
        }

        private bool IsWithinSnapThreshold(Point p1, Point p2)
        {
            double dx = p1.X - p2.X;
            double dy = p1.Y - p2.Y;
            return Math.Sqrt(dx * dx + dy * dy) <= snapThreshold * (worldWidth / m_layers.ActualWidth);
        }

        private Point WorldToScreen(Point p, FrameworkElement element)
        {
            // Convert world coordinates to screen coordinates
            double u = (p.X - panOffsetX) * zoomFactorX;
            double v = (element.ActualHeight - (p.Y - panOffsetY)) * zoomFactorY; // Invert Y for screen space

            return new Point(u, v);
        }

        private Point ScreenToWorld(Point p, FrameworkElement element)
        {
            // Convert screen coordinates to world coordinates
            double x = (p.X / zoomFactorX) + panOffsetX;
            double y = ((element.ActualHeight - (p.Y / zoomFactorY)) + panOffsetY);

            return new Point(x, y);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.Focus();
        }

        private Point GetConstrainedPoint(Point endPoint, Point startPoint)
        {
            double dx = Math.Abs(endPoint.X - startPoint.X);
            double dy = Math.Abs(endPoint.Y - startPoint.Y);

            if (dx > dy)
            {
                // Snap to horizontal
                return new Point(endPoint.X, startPoint.Y);
            }
            else
            {
                // Snap to vertical
                return new Point(startPoint.X, endPoint.Y);
            }
        }

        #region LayerManager Test stuff
        private void DrawText(DrawingContext ctx)
        {
            //if (hide_Text) return;

            //var pen = new Pen(Brushes.Black, 1);
            //var rect = new Rect(20, m_scroll.Value, 15, 25);
            //ctx.DrawRectangle(Brushes.Teal, pen, rect);

            //var txt = new FormattedText(
            //    "qazwsxedcrfvtgbyhnujmik,ol.p;/[']\r\nqwertyuiop\r\n\r\nasdfghjkl\r\nzxcvbnm\r\n0987654321",
            //    CultureInfo.GetCultureInfo("en-us"),
            //    FlowDirection.LeftToRight,
            //    new Typeface("Consolas"),
            //    14,
            //    Brushes.White);

            //ctx.DrawText(txt, new Point(20, m_scroll.Value));

            //Log("text");
        }

        private void DrawBackground(DrawingContext ctx)
        {
            var pen = new Pen(Brushes.Black, 1);
            var rect = new Rect(0, 0, m_layers.ActualWidth, m_layers.ActualHeight);
            ctx.DrawRoundedRectangle(Brushes.White, pen, rect, 0, 0);

            Log("background");
        }

        private void DrawBackgroundBlock(DrawingContext ctx)
        {
            //var pen = new Pen(Brushes.DarkOliveGreen, 1);
            //var rect = new Rect(20, 60, 200, 50);
            //ctx.DrawRoundedRectangle(Brushes.DarkOliveGreen, pen, rect, 50, 50);

            //Log("background block");
        }


        private void DrawForeground(DrawingContext ctx)
        {
            //var pen = new Pen(Brushes.Black, 1);
            //var rect = new Rect(20, 20, 50, 55);
            //ctx.DrawRectangle(Brushes.Red, pen, rect);

            //Log("foreground");
        }

        private void DrawStaticForeground(DrawingContext ctx)
        {
            //var pen = new Pen(Brushes.Black, 1);
            //var rect = new Rect(70, 20, 100, 100);
            //ctx.DrawRectangle(Brushes.Blue, pen, rect);

            //Log("foreground");
        }

        private void DrawVisualGrid(DrawingContext ctx)
        {
            double major_gridline_thickness = 0.5;
            double minor_gridline_thickness = 0.25;

            if (hide_Grid is true)
            {
                return;
            }

            // thickness of the minor gridlines
            Pen pen;

            double majorGridSpacing = 10.0;  // Major grid lines in world units
            double minorGridSpacing = 5.0;  // Minor grid lines in world units

            double layerWidth = dockpanel.ActualWidth;
            double layerHeight = dockpanel.ActualHeight;

            // Draw vertical grid lines (major and minor) in world coordinates
            for (double x = 0; x <= layerWidth; x += minorGridSpacing)
            {
                // If its a major grid line, make it thicker
                if (x % majorGridSpacing == 0)
                {
                    pen = new Pen(Brushes.Black, 1.0);
                    pen.Thickness = major_gridline_thickness;
                    pen.DashStyle = new DashStyle(new double[] { 5, 5 }, 0);
                }
                else
                {
                    pen = new Pen(Brushes.Black, 0.3);
                    pen.Thickness = minor_gridline_thickness;
                    pen.DashStyle = new DashStyle(new double[] { 5, 5 }, 0);

                }

                Point p1 = WorldToScreen(new Point(x, 0), m_layers);
                Point p2 = WorldToScreen(new Point(x, layerHeight), m_layers);

                // Check if line is outside of viewbox
                if (p1.X < 0 || p1.X > layerWidth || p2.X < 0 || p2.X > layerWidth)
                {
                    continue;
                }

                // check if p1 point is outside of viewbox
                if (p1.Y < 0)
                {
                    p1.Y = 0;
                }
                if (p1.Y > layerHeight)
                {
                    p1.Y = layerHeight;
                }
                if (p2.Y < 0)
                {
                    p2.Y = 0;
                }
                if (p2.Y > layerHeight)
                {
                    p2.Y = layerHeight;
                }

                ctx.DrawLine(pen, p1, p2);
            }

            // Draw horizontal grid lines (major and minor)
            for (double y = 0; y <= layerHeight; y += minorGridSpacing)
            {
                // If its a major grid line, make it thicker
                if (y % majorGridSpacing == 0)
                {
                    pen = new Pen(Brushes.Black, 1.0);
                    pen.Thickness = major_gridline_thickness;
                    pen.DashStyle = new DashStyle(new double[] { 5, 5 }, 0);
                }
                else
                {
                    pen = new Pen(Brushes.Black, 0.3);
                    pen.Thickness = minor_gridline_thickness;
                    pen.DashStyle = new DashStyle(new double[] { 5, 5 }, 0);

                }

                Point p1 = WorldToScreen(new Point(0, y), m_layers);
                Point p2 = WorldToScreen(new Point(layerWidth, y), m_layers);

                // Check if line is outside of viewbox
                if (p1.Y < 0 || p1.Y > layerHeight || p2.Y < 0 || p2.Y > layerHeight)
                {
                    continue;
                }

                // check if p1 point is outside of viewbox
                if (p1.X < 0)
                {
                    p1.X = 0;
                }
                if (p1.X > layerWidth)
                {
                    p1.X = layerWidth;
                }
                if (p2.X < 0)
                {
                    p2.X = 0;
                }
                if (p2.X > layerWidth)
                {
                    p2.X = layerWidth;
                }

                ctx.DrawLine(pen, p1, p2);
            }

            // Draw an origin marker
            DrawMarkers(ctx, WorldToScreen(new Point(0, 0), m_layers), 5, new Pen(Brushes.Black, 1), Brushes.DarkRed);
        }


        private void DrawMarkers(DrawingContext ctx, Point p, double dia, Pen pen, Brush fill)
        {
            ctx.DrawEllipse(fill, pen, p, dia, dia);

            return;
        }

        private void DrawCursor(DrawingContext ctx)
        {
            Point cross_pt = currentMouseScreenPosition;

            if ((snapMode is true))
            {
                // draw markers and coordinate data at end points
                ctx.DrawEllipse(Brushes.DarkRed, new Pen(Brushes.Black, 1), currentMouseScreenPosition, 5, 5);

                ctx.DrawLine(new Pen(Brushes.Red, 1), cross_pt, new Point(0, cross_pt.Y));
                ctx.DrawLine(new Pen(Brushes.Red, 1), cross_pt, new Point(dockpanel.Width, cross_pt.Y));
                ctx.DrawLine(new Pen(Brushes.Red, 1), cross_pt, new Point(cross_pt.X, 0));
                ctx.DrawLine(new Pen(Brushes.Red, 1), cross_pt, new Point(cross_pt.X, dockpanel.Height));
            }
            else
            {
                ctx.DrawLine(new Pen(Brushes.Black, 1), cross_pt, new Point(0, cross_pt.Y));
                ctx.DrawLine(new Pen(Brushes.Black, 1), cross_pt, new Point(dockpanel.Width, cross_pt.Y));
                ctx.DrawLine(new Pen(Brushes.Black, 1), cross_pt, new Point(cross_pt.X, 0));
                ctx.DrawLine(new Pen(Brushes.Black, 1), cross_pt, new Point(cross_pt.X, dockpanel.Height));
            }
        }

        private void CreatePreviewShape()
        {
            if (currentMode == DrawMode.Line)
                previewShape = new Line { Stroke = Brushes.DarkGreen, StrokeThickness = 2 };
            else if (currentMode == DrawMode.Rectangle)
                previewShape = new Rectangle { Stroke = Brushes.DarkGreen, StrokeThickness = 2, Fill = Brushes.Transparent };
        }

        private void DrawPreview(DrawingContext ctx)
        {
            // Do we have a preview shape?
            if (previewShape == null)
            {
                return;
            }

            Point preview_endPoint_world;
            if (endPoint_world == null)  // true if second point hasnt been selected
            {
                preview_endPoint_world = ScreenToWorld(currentMouseScreenPosition, m_layers);
            }
            else
            {
                preview_endPoint_world = endPoint_world.Value;
            }

            if (previewShape is Line line)
            {
                SolidColorBrush lineStrokeBrush = new SolidColorBrush(Colors.Green);
                lineStrokeBrush.Opacity = 0.5;
                Pen pen = new Pen(lineStrokeBrush, 1);
                Point p1 = WorldToScreen(startPoint_world.Value, m_layers);
                Point p2 = WorldToScreen(preview_endPoint_world, m_layers);
                ctx.DrawLine(pen, p1, p2);
            }
            else if (previewShape is Rectangle rect)
            {
                SolidColorBrush rectFillBrush = new SolidColorBrush(Colors.Green);
                rectFillBrush.Opacity = 0.5;
                Pen pen = new Pen(rectFillBrush, 1);
                Point p1 = WorldToScreen(startPoint_world.Value, m_layers);
                Point p2 = WorldToScreen(preview_endPoint_world, m_layers);

                // find lower left corner point of rectangle bounded by startPoint_world and endPoint_world
                Point insertPoint_screen = new Point(Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y));

                double width = Math.Abs(p2.X - p1.X);
                double height = Math.Abs(p2.Y - p1.Y);
                ctx.DrawRectangle(rectFillBrush, pen, new Rect(insertPoint_screen.X, insertPoint_screen.Y, width, height));
            }
        }

        /// <summary>
        /// The primary routine to draw all the world shapes
        /// </summary>
        private void DrawShapes(DrawingContext ctx)
        {
            double center_pt_dia = 5;

            // Redraw all the shapes in world coordinates
            foreach (var wall_sys in wallSystems)
            {
                foreach (var wall in wall_sys._walls)
                {
                    Shape shapeToDraw = null;
                    Ellipse centerPointMarker = null;
                    FormattedText idLabel = null;

                    Point p1_world = wall.Value.Start;
                    Point p2_world = wall.Value.End;
                    Point p1_screen = GetConstrainedScreenPoint(WorldToScreen(p1_world, m_layers), m_layers);
                    Point p2_screen = GetConstrainedScreenPoint(WorldToScreen(p2_world, m_layers), m_layers);

                    // If the points are the same, then the object was out of bounds and doesn't need to be drawn.
                    if (p1_screen == p2_screen)
                        continue;

                    // Draw the line object
                    SolidColorBrush lineStrokeBrush = new SolidColorBrush(Colors.Blue);
                    Pen pen = new Pen(lineStrokeBrush, 2);
                    ctx.DrawLine(pen, p1_screen, p2_screen);

                    Point center_world = new Point((wall.Value.Start.X + wall.Value.End.X) / 2, (wall.Value.Start.Y + wall.Value.End.Y) / 2);
                    Point center_screen = WorldToScreen(center_world, m_layers);

                    // draw the center point and label
                    if (PointIsWithinBounds(center_screen, dockpanel) is false)
                    {
                        continue;
                    }
                    else
                    {
                        Point centerPoint = new Point(center_screen.X,
                                center_screen.Y);

                        ctx.DrawEllipse(lineStrokeBrush, pen, centerPoint, center_pt_dia, center_pt_dia); // center point marker

                        idLabel = new FormattedText(wall.Key.ToString(),
                            CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Consolas"), 14, Brushes.Black);
                        ctx.DrawText(idLabel, center_screen);  // id label
                    }
                }
            }

            // Redraw all the shapes in world coordinates
            foreach (var dia_sys in diaphragmSystems)
            {
                foreach (var rect in dia_sys._diaphragms)
                {
                    Shape shapeToDraw = null;
                    Ellipse centerPointMarker = null;
                    FormattedText idLabel = null;

                    Point p1_world = rect.Value.P1;
                    Point p3_world = rect.Value.P3;
                    Point p1_screen = GetConstrainedScreenPoint(WorldToScreen(p1_world, m_layers), m_layers);
                    Point p3_screen = GetConstrainedScreenPoint(WorldToScreen(p3_world, m_layers), m_layers);

                    // If the points are the same, then the object was out of bounds and doesn't need to be drawn.
                    if (p1_screen == p3_screen)
                        continue;

                    SolidColorBrush rectFillBrush = new SolidColorBrush(Colors.Red);
                    rectFillBrush.Opacity = 0.5;
                    Pen pen = new Pen(rectFillBrush, 1);

                    Point insertPoint_screen = new Point(p1_screen.X, p3_screen.Y);  // TODO: Need a better way to get P4 point of rectangle

                    double width = Math.Abs(p3_screen.X - p1_screen.X);
                    double height = Math.Abs(p3_screen.Y - p1_screen.Y);
                    ctx.DrawRectangle(rectFillBrush, pen, new Rect(insertPoint_screen.X, insertPoint_screen.Y, width, height));

                    Point center_world = new Point((rect.Value.P1.X + rect.Value.P3.X) / 2, (rect.Value.P1.Y + rect.Value.P3.Y) / 2);
                    Point center_screen = WorldToScreen(center_world, m_layers);

                    // draw the center point and label
                    if (PointIsWithinBounds(center_screen, dockpanel) is false)
                        continue;
                    else
                    {
                        Point centerPoint = new Point(center_screen.X,
                            center_screen.Y);

                        ctx.DrawEllipse(rectFillBrush, pen, centerPoint, center_pt_dia, center_pt_dia); // center point marker

                        idLabel = new FormattedText(rect.Key.ToString(),
                            CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Consolas"), 14, Brushes.Black);
                        ctx.DrawText(idLabel, centerPoint);  // id label
                    }
                }
            }
        }

        /// <summary>
        /// Returns a point that is bounded by the framework element -- used for when a point is out of bounds 
        /// and needs to be shifted back to the elements boundary when drawing
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="element"></param>
        /// <returns></returns>
        private Point GetConstrainedScreenPoint(Point p1, FrameworkElement element)
        {
            Point temp = p1;
            if (temp.X < 0)
            {
                temp.X = 0;
            }
            if (temp.X > element.ActualWidth)
            {
                temp.X = element.ActualWidth;
            }
            if (temp.Y < 0)
            {
                temp.Y = 0;
            }
            if (temp.Y > element.ActualHeight)
            {
                temp.Y = element.ActualHeight;
            }
            return temp;
        }

        private void DrawDebug(DrawingContext ctx)
        {
            if (debugMode == false)
                return;

            // Redraw all the shapes in world coordinates
            FormattedText idLabel = null;
            foreach (var wall_sys in wallSystems)
            {
                foreach (var wall in wall_sys._walls)
                {
                    Point p1_world = wall.Value.Start;
                    Point p2_world = wall.Value.End;
                    Point p1_screen = WorldToScreen(p1_world, m_layers);
                    Point p2_screen = WorldToScreen(p2_world, m_layers);

                    // START POINT
                    if (PointIsWithinBounds(p1_screen, dockpanel) is true)
                    {
                        ctx.DrawEllipse(Brushes.MediumBlue, new Pen(Brushes.Black, 1), p1_screen, 5, 5);
                        idLabel = new FormattedText(
                                $"({p1_world.X:F2}, {p1_world.Y:F2})",
                                CultureInfo.GetCultureInfo("en-us"),
                                FlowDirection.LeftToRight,
                                new Typeface("Consolas"),
                                14,
                                Brushes.Black,
                                VisualTreeHelper.GetDpi(this).PixelsPerDip);

                        ctx.DrawText(idLabel, p1_screen);  // id label
                    }

                    // END POINT
                    if (PointIsWithinBounds(p2_screen, dockpanel) is true)
                    {
                        ctx.DrawEllipse(Brushes.MediumBlue, new Pen(Brushes.Black, 1), p2_screen, 5, 5);
                        idLabel = new FormattedText(
                                $"({p2_world.X:F2}, {p2_world.Y:F2})",
                                CultureInfo.GetCultureInfo("en-us"),
                                FlowDirection.LeftToRight,
                                new Typeface("Consolas"),
                                14,
                                Brushes.Black,
                                VisualTreeHelper.GetDpi(this).PixelsPerDip);
                        ctx.DrawText(idLabel, p2_screen);  // id label
                    }
                }
            }

            foreach (var dia_sys in diaphragmSystems)
            {
                foreach (var dia in dia_sys._diaphragms)
                {
                    Point p1_world = dia.Value.P1;
                    Point p2_world = dia.Value.P2;
                    Point p3_world = dia.Value.P3;
                    Point p4_world = dia.Value.P4;
                    Point p1_screen = WorldToScreen(p1_world, m_layers);
                    Point p2_screen = WorldToScreen(p2_world, m_layers);
                    Point p3_screen = WorldToScreen(p3_world, m_layers);
                    Point p4_screen = WorldToScreen(p4_world, m_layers);

                    // P1
                    if (PointIsWithinBounds(p1_screen, dockpanel) is true)
                    {

                        ctx.DrawEllipse(Brushes.Red, new Pen(Brushes.Black, 1), p1_screen, 5, 5);
                        idLabel = new FormattedText(
                                $"({p1_world.X:F2}, {p1_world.Y:F2})",
                                CultureInfo.GetCultureInfo("en-us"),
                                FlowDirection.LeftToRight,
                                new Typeface("Consolas"),
                                14,
                                Brushes.Black,
                                VisualTreeHelper.GetDpi(this).PixelsPerDip);

                        ctx.DrawText(idLabel, p1_screen);  // id label
                    }

                    // P2
                    if (PointIsWithinBounds(p2_screen, dockpanel) is true)
                    {
                        ctx.DrawEllipse(Brushes.Red, new Pen(Brushes.Black, 1), p2_screen, 5, 5);
                        idLabel = new FormattedText(
                                $"({p2_world.X:F2}, {p2_world.Y:F2})",
                                CultureInfo.GetCultureInfo("en-us"),
                                FlowDirection.LeftToRight,
                                new Typeface("Consolas"),
                                14,
                                Brushes.Black,
                                VisualTreeHelper.GetDpi(this).PixelsPerDip);
                        ctx.DrawText(idLabel, p2_screen);  // id label
                    }

                    // P3
                    if (PointIsWithinBounds(p3_screen, dockpanel) is true)
                    {
                        ctx.DrawEllipse(Brushes.Red, new Pen(Brushes.Black, 1), p3_screen, 5, 5);
                        idLabel = new FormattedText(
                                $"({p3_world.X:F2}, {p3_world.Y:F2})",
                                CultureInfo.GetCultureInfo("en-us"),
                                FlowDirection.LeftToRight,
                                new Typeface("Consolas"),
                                14,
                                Brushes.Black,
                                VisualTreeHelper.GetDpi(this).PixelsPerDip);
                        ctx.DrawText(idLabel, p3_screen);  // id label
                    }

                    // P4
                    if (PointIsWithinBounds(p4_screen, dockpanel) is true)
                    {
                        ctx.DrawEllipse(Brushes.Red, new Pen(Brushes.Black, 1), p4_screen, 5, 5);
                        idLabel = new FormattedText(
                                $"({p4_world.X:F2}, {p4_world.Y:F2})",
                                CultureInfo.GetCultureInfo("en-us"),
                                FlowDirection.LeftToRight,
                                new Typeface("Consolas"),
                                14,
                                Brushes.Black,
                                VisualTreeHelper.GetDpi(this).PixelsPerDip);

                        ctx.DrawText(idLabel, p4_screen);  // id label
                    }

                }
            }
        }

        /// <summary>
        /// Helper function to determine if a point is within the bounds of a framework element (ActualWidth and ActualHeight)
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="element"></param>
        /// <returns></returns>
        private bool PointIsWithinBounds(Point p1, FrameworkElement element)
        {
            return (p1.X > 0 && p1.X < element.ActualWidth && p1.Y > 0 && p1.Y < element.ActualHeight);
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);

            //m_scroll.Minimum = 0;
            //m_scroll.Maximum = m_layers.ActualHeight - 70;
            Draw(ChangeType.Resize);
        }

        private void OnScroll(object sender, ScrollEventArgs e)
        {
            Draw(ChangeType.Scroll);
        }

        private void Draw(ChangeType change)
        {
            m_layers.Draw(change);
        }

        private void btnHideText_Click(object sender, RoutedEventArgs e)
        {
            hide_Text = !hide_Text;
            Draw(ChangeType.Redraw);
        }

        private void btnHideGrid_Click(object sender, RoutedEventArgs e)
        {
            hide_Grid = !hide_Grid;
            Draw(ChangeType.Redraw);
        }

        private void Log(string text)
        {
            //m_log.Text = text + "\r\n" + m_log.Text;

            //if (m_log.Text.Length > 1000)
            //{
            //    m_log.Text = m_log.Text.Substring(0, 1000);
            //}
        }

        private void FinalizeShape(Point worldPoint)
        {
            if (currentMode == DrawMode.Line)
            {
                // For the line to be horizontal or vertical only
                worldPoint = GetConstrainedPoint(worldPoint, startPoint_world.Value); // Ensure alignment

                // Create the line in world space and store it in the worldShapes list
                Point startPoint = startPoint_world.Value;
                Point endPoint = worldPoint;

                if(wallSystems == null || wallSystems.Count == 0)
                {
                    wallSystems.Add(new WallSystem());
                }

                wallSystems[0].AddWall(new WallData(defaultWallHeight, startPoint.X, startPoint.Y, endPoint.X, endPoint.Y));
                Update();
            }
            else if (currentMode == DrawMode.Rectangle)
            {
                if(diaphragmSystems == null || diaphragmSystems.Count == 0)
                {
                    diaphragmSystems.Add(new DiaphragmSystem());
                }

                diaphragmSystems[0].AddDiaphragm(new DiaphragmData_Rectangular(startPoint_world.Value, endPoint_world.Value));
                Update();
            }

            // Clear the preview shape from the screen.
            previewShape = null;
            startPoint_world = null;
            endPoint_world = null;

            Draw(ChangeType.Redraw);
        }

        // Create the layers
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            m_layers.AddLayer(5, DrawBackground, ChangeType.Resize);
            m_layers.AddLayer(11, DrawBackgroundBlock);
            m_layers.AddLayer(20, DrawStaticForeground);
            m_layers.AddLayer(21, DrawText, ChangeType.Scroll);
            m_layers.AddLayer(30, DrawForeground);

            m_layers.AddLayer(40, DrawVisualGrid);
            m_layers.AddLayer(41, DrawShapes);
            m_layers.AddLayer(52, DrawCursor);
            m_layers.AddLayer(51, DrawPreview);
            m_layers.AddLayer(42, DrawDebug);

            // Now draw everything
            Draw(ChangeType.Redraw);
        }

        #endregion

        #region Drawing Layer Events

        private void m_layers_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            //MessageBox.Show("Drawing Visual Clicked at " + e.GetPosition(m_layers).ToString());

            if (e.RightButton == MouseButtonState.Pressed)
            {
                ResetInputMode();
                Draw(ChangeType.Redraw);
                return;
            }


            if (currentMode == DrawMode.None)
            {
                MessageBox.Show("No drawing mode selected.  Try selecting L (line mode) or R (rectangle) mode first.");
                return;  // Ignore if not in drawing mode
            }

            Point screenPoint = e.GetPosition(m_layers);
            Point worldPoint = ScreenToWorld(screenPoint, m_layers);

            if (snapMode)
            {
                worldPoint = GetSnappedPoint(worldPoint);
            }

            if (startPoint_world == null)
            {
                startPoint_world = worldPoint;
                CreatePreviewShape();
            }
            else
            {
                endPoint_world = worldPoint;
                FinalizeShape(endPoint_world.Value);
            }

            Draw(ChangeType.Redraw);
        }


        private void m_layers_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            currentMouseScreenPosition = e.GetPosition(m_layers);
            Point currentMouseWorldPosition = ScreenToWorld(currentMouseScreenPosition, m_layers);

            tbScreenCoords.Text = e.GetPosition(m_layers).ToString();
            tbWorldCoords.Text = "World Coords: (" + currentMouseWorldPosition.X.ToString("F2") + ", " + currentMouseWorldPosition.Y.ToString("F2") + ")";  // changed this one too


            Draw(ChangeType.Redraw);
        }

        private void m_layers_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Determine the zoom factor
            double zoomFactor = (e.Delta > 0) ? 1.1 : 0.9;

            // Get the mouse position_screen relative to the canvas
            Point mousePosition_screen = e.GetPosition(m_layers);

            // Transform the mouse position_screen to world coordinates before zooming
            Point beforeZoom = ScreenToWorld(mousePosition_screen, m_layers);

            // Apply zoom
            zoomFactorX *= zoomFactor;
            zoomFactorY *= zoomFactor;

            // Transform the mouse position_screen to world coordinates after zooming
            Point afterZoom = ScreenToWorld(mousePosition_screen, m_layers);

            // Adjust pan offset to keep the mouse position_screen centered
            panOffsetX -= (afterZoom.X - beforeZoom.X);
            panOffsetY -= (afterZoom.Y - beforeZoom.Y);

            // Display current parameters on screen
            tbPan.Text = "Pan: (" + panOffsetX.ToString("F2") + ", " + panOffsetY.ToString("F2") + ")";
            tbZoom.Text = "Zoom: (" + zoomFactorX.ToString("F2") + ", " + zoomFactorY.ToString("F2") + ")";
            tbWorldCoords.Text = "World Coords: (" + afterZoom.X.ToString("F2") + ", " + afterZoom.Y.ToString("F2") + ")";  // changed this one too
            tbScreenCoords.Text = "Screen Coords: (" + mousePosition_screen.X.ToString("F2") + ", " + mousePosition_screen.Y.ToString("F2") + ")"; // changed this one too

            Draw(ChangeType.Redraw);
        }

        private void m_layers_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Right button click to CANCEL
            if (e.RightButton == MouseButtonState.Pressed)
            {
                ResetInputMode();
                Draw(ChangeType.Redraw);

                return;
            }
        }

        #endregion

        #region UI Events

        private void btnLineMode_Click(object sender, RoutedEventArgs e)
        {
            SetLineMode();
        }

        private void btnRectangleMode_Click(object sender, RoutedEventArgs e)
        {
            SetRectangleMode();
        }

        private void btnSnapMode_Click(object sender, RoutedEventArgs e)
        {
            SetSnapMode();
        }

        #endregion

        #region Menu Events
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            // You can refresh your data-bound controls here.
            MessageBox.Show("Refresh triggered!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        #endregion
    }
}



    //        ////// hide the results controls
    //        ////HideAllDataControls();

    //        //// Create the diaphragm data controls
    //        //if (Calculator._diaphragm_system._diaphragms.Count > 0)
    //        //{
    //        //    // turn on the controls stackpanel visibility
    //        //    spDiaphragmDataControls.Visibility = Visibility.Visible;

    //        //    DiaphragmData.Children.Clear();  // clear the stack panel controls for the diaphragm data
    //        //    foreach (var diaphragm in Calculator._diaphragm_system._diaphragms)
    //        //    {
    //        //        int id = diaphragm.Key;
    //        //        DiaphragmData_Rectangular dia = diaphragm.Value;

    //        //        DiaphragmDataControl control = new DiaphragmDataControl(id, dia);

    //        //        DiaphragmData.Children.Add(control);

    //        //        control.DeleteDiaphragm += OnDiaphragmDeleted;
    //        //    }
    //        //}

    //        ////// Create Table of Results
    //        //if (Calculator._wall_system._walls.Count > 0)
    //        //{
    //        //    // turn on the results and wall data controls visibility
    //        //    spWallDataControls.Visibility = Visibility.Visible;
    //        //    spCalcResultsControls.Visibility = Visibility.Visible;

    //        //    if (Calculator.GetType() == typeof(ShearWallCalculator_RigidDiaphragm))
    //        //    {

    //        //        // Create Table of Wall Data
    //        //        ShearWallData_EW.Children.Clear(); // clear the stack panel controls for the wall data
    //        //        ShearWallData_NS.Children.Clear(); // clear the stack panel controls for the wall data
    //        //        ShearWallResults.Children.Clear(); // clear the stack panel controls for the calculation results data

    //        //        foreach (var result in ((ShearWallCalculator_RigidDiaphragm)Calculator).TotalWallShear)
    //        //        {
    //        //            int id = result.Key;
    //        //            float rigidity = ((ShearWallCalculator_RigidDiaphragm)Calculator)._wall_system.EW_Walls.ContainsKey(id) ? ((ShearWallCalculator_RigidDiaphragm)Calculator)._wall_system.EW_Walls[id].WallRigidity : Calculator._wall_system.NS_Walls[id].WallRigidity;
    //        //            float direct_shear_x = ((ShearWallCalculator_RigidDiaphragm)Calculator).DirectShear_X.ContainsKey(id) ? ((ShearWallCalculator_RigidDiaphragm)Calculator).DirectShear_X[id] : 0.0f;
    //        //            float direct_shear_y = ((ShearWallCalculator_RigidDiaphragm)Calculator).DirectShear_Y.ContainsKey(id) ? ((ShearWallCalculator_RigidDiaphragm)Calculator).DirectShear_Y[id] : 0.0f;
    //        //            float eccentric_shear = ((ShearWallCalculator_RigidDiaphragm)Calculator).EccentricShear.ContainsKey(id) ? ((ShearWallCalculator_RigidDiaphragm)Calculator).EccentricShear[id] : 0.0f;
    //        //            float total_shear = ((ShearWallCalculator_RigidDiaphragm)Calculator).TotalWallShear.ContainsKey(id) ? ((ShearWallCalculator_RigidDiaphragm)Calculator).TotalWallShear[id] : 0.0f;

    //        //            ShearWallResultsControl control = new ShearWallResultsControl(
    //        //                id,
    //        //                rigidity,
    //        //                Calculator._wall_system.X_bar_walls[id],
    //        //                Calculator._wall_system.Y_bar_walls[id],
    //        //                direct_shear_x,
    //        //                direct_shear_y,
    //        //                eccentric_shear,
    //        //                total_shear
    //        //                );

    //        //            ShearWallResults.Children.Add(control);
    //        //        }

    //        //        /// Create the shearwall data controls
    //        //        foreach (var result in ((ShearWallCalculator_RigidDiaphragm)Calculator).TotalWallShear)
    //        //        {
    //        //            int id = result.Key;
    //        //            WallData wall = Calculator._wall_system.EW_Walls.ContainsKey(id) ? Calculator._wall_system.EW_Walls[id] : Calculator._wall_system.NS_Walls[id];
    //        //            ShearWallDataControl control = new ShearWallDataControl(id, wall);

    //        //            if (wall.WallDir == WallDirs.EastWest)
    //        //            {
    //        //                ShearWallData_EW.Children.Add(control);
    //        //            }
    //        //            else if (wall.WallDir == WallDirs.NorthSouth)
    //        //            {
    //        //                ShearWallData_NS.Children.Add(control);
    //        //            }
    //        //            else
    //        //            {
    //        //                throw new Exception("Invalid wall direction " + wall.WallDir.ToString() + " in wall #" + id.ToString());
    //        //            }

    //        //            control.DeleteWall += OnWallDeleted;
    //        //        }
    //        //    }
    //        //    else if (Calculator.GetType() == typeof(ShearWallCalculator_FlexibleDiaphragm))
    //        //    {
    //        //        // Create Table of Wall Data
    //        //        ShearWallData_EW.Children.Clear();
    //        //        ShearWallData_NS.Children.Clear();
    //        //        ShearWallResults.Children.Clear();

    //        //        throw new NotImplementedException("\nFlexible Diaphragms Calculator not implemented yet.");
    //        //    }
    //        //    else
    //        //    {
    //        //        throw new NotImplementedException("\nInvalid Calaculator type received.");
    //        //    }
    //        //}
    //    }


    //    #region Controls
    //    private void HideAllDataControls()
    //    {
    //        spCalcResultsControls.Visibility = Visibility.Collapsed;
    //        spWallDataControls.Visibility = Visibility.Collapsed;
    //        spDiaphragmDataControls.Visibility = Visibility.Collapsed;
    //    }

    //    private void ShowAllDataControls()
    //    {
    //        spCalcResultsControls.Visibility = Visibility.Visible;
    //        spWallDataControls.Visibility = Visibility.Visible;
    //        spDiaphragmDataControls.Visibility = Visibility.Visible;
    //    }

    //    private void ClearCoordinateDisplayData()
    //    {
    //        lblScreenEndCoord.Content = "";
    //        lblScreenStartCoord.Content = "";
    //        lblWorldEndCoord.Content = "";
    //        lblWorldStartCoord.Content = "";
    //    }
    //    /// <summary>
    //    /// Function to handle clearing point and cursor input variables
    //    /// </summary>
    //    private void ResetPointInputInfo()
    //    {
    //        _startClickSet = false;
    //        _endClickSet = false;
    //        CurrentStartPoint = new Point();
    //        CurrentEndPoint = new Point();
    //        _currentPreviewLine = null;

    //        // update the infor labels
    //        lblScreenEndCoord.Content = "";
    //        lblScreenStartCoord.Content = "";
    //        lblWorldEndCoord.Content = "";
    //        lblWorldStartCoord.Content = "";
    //    }
    //    #endregion 

    //    #region Mouse and Window Events
    //    /// <summary>
    //    /// What happens when the middle mouse wheel is scrolled
    //    /// </summary>
    //    /// <param name="sender"></param>
    //    /// <param name="e"></param>
    //    private void MainCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
    //    {
    //        Point mousePosition_screen = e.GetPosition(cnvMainCanvas);
    //        double zoomDelta = e.Delta > 0 ? _zoomFactor : 1 / _zoomFactor;
    //        double oldScaleX = _scaleTransform.ScaleX;
    //        double oldScaleY = _scaleTransform.ScaleY;

    //        SCALE_X = _scaleTransform.ScaleX * zoomDelta;
    //        SCALE_Y =_scaleTransform.ScaleY * zoomDelta;
    //        double scaleChangeX = _scaleTransform.ScaleX - oldScaleX;
    //        double scaleChangeY = _scaleTransform.ScaleY - oldScaleY;
    //        TRANS_X =(float)(_translateTransform.X - (mousePosition_screen.X * scaleChangeX));
    //        TRANS_Y = (float)(_translateTransform.Y - (mousePosition_screen.Y * scaleChangeY));
    //        _translateTransform.Y -= (mousePosition_screen.Y * scaleChangeY);

    //        SetupViewScaling((float)SCALE_X, (float)SCALE_Y, (float)TRANS_X, (float)TRANS_Y);

    //        // recreate the canvas drawer with the new zoom factor
    //        _canvas_drawer = new CanvasDrawer(cnvMainCanvas, SCALE_X, SCALE_Y);

    //        Update();
    //    }

    //    /// <summary>
    //    /// what happens when a mouse button is pressed
    //    /// </summary>
    //    /// <param name="sender"></param>
    //    /// <param name="e"></param>
    //    private void MainCanvas_MouseDown(object sender, MouseButtonEventArgs e)
    //    {
    //        // Right button click to CANCEL
    //        if (e.RightButton == MouseButtonState.Pressed)
    //        {
    //            ResetPointInputInfo();
    //            DrawCanvas();
    //            return;
    //        }

    //        // Middle mouse button for panning and zooming
    //        if (e.MiddleButton == MouseButtonState.Pressed)
    //        {
    //            _isPanning = true;
    //            _lastMousePosition = e.GetPosition(CanvasScrollViewer);
    //            cnvMainCanvas.Cursor = Cursors.Hand;
    //            return;
    //        }

    //        HandleLeftClick(e);
    //        DrawCanvas();
    //    }

    //    private void HandleLeftClick(MouseButtonEventArgs e)
    //    {
    //        if(CurrentInputMode != InputModes.Wall && CurrentInputMode != InputModes.Diaphragm)
    //        {
    //            MessageBox.Show("Please select the 'Wall' or 'Diaphragm' input mode.");
    //            return;
    //        }

    //        Point screenPoint = e.GetPosition(cnvMainCanvas);
    //        Point worldPoint = MathHelpers.ScreenCoord_ToWorld(cnvMainCanvas.Height, screenPoint, SCALE_X, SCALE_Y);

    //        bool snapResult;
    //        Point nearestPoint = MathHelpers.FindNearestSnapPoint(Calculator._wall_system, Calculator._diaphragm_system,
    //            worldPoint, out snapResult);

    //        if (!_startClickSet)
    //        {
    //            _currentPreviewLine = new Line { Stroke = Brushes.Green };

    //            if (_shouldSnapToNearest && snapResult && MathHelpers.PointIsWithinRange(worldPoint, nearestPoint, _snapDistance))
    //            {
    //                screenPoint = MathHelpers.WorldCoord_ToScreen(cnvMainCanvas.Height, nearestPoint, SCALE_X, SCALE_Y);
    //            }

    //            CurrentStartPoint = screenPoint;
    //            _startClickSet = true;
    //            _canvas_drawer.DrawPreviewLine(_currentPreviewLine, CurrentStartPoint, CurrentStartPoint);
    //            return;
    //        }

    //        if (_shouldSnapToNearest && snapResult && MathHelpers.PointIsWithinRange(worldPoint, nearestPoint, _snapDistance))
    //        {
    //            screenPoint = MathHelpers.WorldCoord_ToScreen(cnvMainCanvas.Height, nearestPoint, SCALE_X, SCALE_Y);
    //        }

    //        if (screenPoint == CurrentStartPoint)
    //        {
    //            return;
    //        }

    //        CurrentEndPoint = screenPoint;
    //        _endClickSet = true;
    //        _canvas_drawer.DrawPreviewLine(_currentPreviewLine, CurrentStartPoint, CurrentEndPoint);

    //        ProcessFinalInput();
    //    }

    //    private void ProcessFinalInput()
    //    {
    //        if (_startClickSet && _endClickSet)
    //        {
    //            float startX = (float)CurrentStartPoint.X;
    //            float startY = (float)CurrentStartPoint.Y;
    //            float endX = (float)CurrentEndPoint.X;
    //            float endY = (float)CurrentEndPoint.Y;

    //            if (CurrentInputMode == InputModes.Diaphragm)
    //            {
    //                WallDirs dir = MathHelpers.LineIsHorizontal(_currentPreviewLine) ? WallDirs.EastWest : WallDirs.NorthSouth;

    //                if (dir == WallDirs.EastWest) endY = startY;
    //                else endX = startX;

    //                Point worldStart = MathHelpers.ScreenCoord_ToWorld(cnvMainCanvas.Height, new Point(startX, startY), SCALE_X, SCALE_Y);
    //                Point worldEnd = MathHelpers.ScreenCoord_ToWorld(cnvMainCanvas.Height, new Point(endX, endY),SCALE_X, SCALE_Y);

    //                Calculator._wall_system.AddWall(new WallData(DEFAULT_WALL_HT,
    //                    (float)worldStart.X, (float)worldStart.Y, (float)worldEnd.X, (float)worldEnd.Y, dir));

    //                _currentPreviewLine = null;
    //            }
    //            else if (CurrentInputMode == InputModes.Wall)
    //            {
    //                Point worldStart = MathHelpers.ScreenCoord_ToWorld(cnvMainCanvas.Height, CurrentStartPoint, SCALE_X, SCALE_Y);
    //                Point worldEnd = MathHelpers.ScreenCoord_ToWorld(cnvMainCanvas.Height, CurrentEndPoint, SCALE_X, SCALE_Y);

    //                Calculator._diaphragm_system.AddDiaphragm(new DiaphragmData_Rectangular(worldStart, worldEnd));

    //                _currentPreviewLine = null;
    //            }

    //            ResetPointInputInfo();
    //        }
    //    }


    //    /// <summary>
    //    /// What happens when the mouse is moved
    //    /// </summary>
    //    /// <param name="sender"></param>
    //    /// <param name="e"></param>
    //    private void MainCanvas_MouseMove(object sender, MouseEventArgs e)
    //    {
    //        _currentMousePosition = Mouse.GetPosition(cnvMainCanvas);

    //        // Check if middle mouse button is pressed and we are in panning mode
    //        if (_isPanning && e.MiddleButton == MouseButtonState.Pressed)
    //        {
    //            Point currentPosition = e.GetPosition(CanvasScrollViewer);
    //            double offsetX = currentPosition.X - _lastMousePosition.X;
    //            double offsetY = currentPosition.Y - _lastMousePosition.Y;
    //            CanvasScrollViewer.ScrollToHorizontalOffset(CanvasScrollViewer.HorizontalOffset + offsetX);
    //            CanvasScrollViewer.ScrollToVerticalOffset(CanvasScrollViewer.VerticalOffset + offsetY);

    //            cnvMainCanvas.RenderTransform = new TranslateTransform(offsetX + cnvMainCanvas.RenderTransform.Value.OffsetX, offsetY + cnvMainCanvas.RenderTransform.Value.OffsetY);


    //            //foreach (UIElement element in cnvMainCanvas.Children)
    //            //{
    //            //    if (element is FrameworkElement fe)
    //            //    {
    //            //        fe.RenderTransform = new TranslateTransform(offsetX + fe.RenderTransform.Value.OffsetX, offsetY + fe.RenderTransform.Value.OffsetY);
    //            //    }
    //            //}

    //            _lastMousePosition = currentPosition;
    //            Update();
    //        }

    //        else
    //        {
    //            // Update the preview lines
    //            if (_currentPreviewLine != null)
    //            {
    //                var p = e.GetPosition(cnvMainCanvas);
    //                _currentPreviewLine.X2 = p.X;
    //                _currentPreviewLine.Y2 = p.Y;

    //                // for wall entry mode, force the preview lines to be horizontal or vertical from the first point clicked
    //                if (CurrentInputMode == InputModes.Diaphragm)
    //                {
    //                    // force the line to be horizontal or vertical only
    //                    if (MathHelpers.LineIsHorizontal(_currentPreviewLine))
    //                    {
    //                        _currentPreviewLine.Y2 = _currentPreviewLine.Y1;
    //                    }
    //                    else
    //                    {
    //                        _currentPreviewLine.X2 = _currentPreviewLine.X1;
    //                    }
    //                }
    //            }

    //            // Update the mouse position_screen label
    //            MousePosition.Content = "(" + _currentMousePosition.X.ToString("0.00") + ", " + _currentMousePosition.Y.ToString("0.00") + ")";

    //        }
    //        Update();

    //    }

    //    /// <summary>
    //    /// Function to determine if a line object is more horizontal than vertical by comparing the x and y distances between the start and end points
    //    /// </summary>
    //    /// <param name="line"></param>
    //    /// <returns></returns>

    //    /// <summary>
    //    /// What happens when a mouse button is released
    //    /// </summary>
    //    /// <param name="sender"></param>
    //    /// <param name="e"></param>
    //    private void MainCanvas_MouseUp(object sender, MouseButtonEventArgs e)
    //    {
    //        // Middle mouse button is released -- stops panning
    //        if (e.MiddleButton == MouseButtonState.Released)
    //        {
    //            _isPanning = false;
    //            cnvMainCanvas.Cursor = Cursors.Arrow;
    //        }
    //    }

    //    private void Window_Loaded(object sender, RoutedEventArgs e)
    //    {
    //        var window = Window.GetWindow(this);
    //        window.KeyDown += HandleKeyPress;
    //    }

    //    /// <summary>
    //    /// Set up the event to detect key presses within the window
    //    /// </summary>
    //    /// <param name="sender"></param>
    //    /// <param name="e"></param>
    //    private void HandleKeyPress(object sender, KeyEventArgs e)
    //    {
    //        switch (e.Key)
    //        {
    //            // Clear all the input data
    //            case Key.Escape:
    //                ResetPointInputInfo();
    //                break;
    //            case Key.C:
    //                if (CurrentInputMode == InputModes.Diaphragm)
    //                {
    //                    Calculator._wall_system._walls.Clear();
    //                }
    //                if (CurrentInputMode == InputModes.Wall)
    //                {
    //                    Calculator._diaphragm_system._diaphragms.Clear();
    //                }
    //                break;
    //            // Enter points to define the diaphragm
    //            case Key.M:
    //                CurrentInputMode = InputModes.Wall;
    //                CurrentMode.Content = "DIAPHRAGM ENTRY (MASS MODE)";
    //                break;
    //            case Key.R:
    //                CurrentInputMode = InputModes.Diaphragm;
    //                CurrentMode.Content = "SHEAR WALL ENTRY (RIGIDITY MODE)";
    //                break;
    //            default:
    //                break;
    //        }

    //        Update();
    //    }

    //    private void OnDiaphragmDeleted(object sender, DiaphragmDataControl.DeleteDiaphragmEventArgs e)
    //    {
    //        if (Calculator._diaphragm_system._diaphragms.ContainsKey(e.Id) == true)
    //        {
    //            Calculator._diaphragm_system._diaphragms.Remove(e.Id);

    //            MessageBox.Show(e.Id.ToString() + " has been deleted");
    //        }
    //        else
    //        {
    //            MessageBox.Show(e.Id.ToString() + " does not exist in Walls");
    //        }

    //        Update();
    //    }

    //    private void OnWallDeleted(object sender, ShearWallDataControl.DeleteWallEventArgs e)
    //    {
    //        if (Calculator._wall_system._walls.ContainsKey(e.Id) == true)
    //        {
    //            Calculator._wall_system._walls.Remove(e.Id);

    //            MessageBox.Show(e.Id.ToString() + " has been deleted");
    //        }
    //        else
    //        {
    //            MessageBox.Show(e.Id.ToString() + " does not exist in Walls");
    //        }

    //        Update();
    //    }
    //    #endregion

    //    #region Controls and Button Clicks
    //    /// <summary>
    //    /// The SNAP MODE button
    //    /// </summary>
    //    /// <param name="sender"></param>
    //    /// <param name="e"></param>
    //    private void btnSnapToNearest_Click(object sender, RoutedEventArgs e)
    //    {
    //        _shouldSnapToNearest = !_shouldSnapToNearest;

    //        if (_shouldSnapToNearest is true)
    //        {
    //            btnSnapToNearest.BorderBrush = Brushes.Black;
    //            btnSnapToNearest.BorderThickness = new Thickness(3);

    //            _crosshair_color = new SolidColorBrush(Colors.Red); // change the cross hair colors
    //        }
    //        else
    //        {
    //            btnSnapToNearest.BorderBrush = Brushes.Transparent;
    //            btnSnapToNearest.BorderThickness = new Thickness(0);

    //            _crosshair_color = new SolidColorBrush(Colors.Black);  // change the cross hair colors
    //        }

    //        Update();
    //    }

    //    /// <summary>
    //    /// Diaphragm (wall) input mode
    //    /// </summary>
    //    /// <param name="sender"></param>
    //    /// <param name="e"></param>
    //    private void btnRigidityMode_Click(object sender, RoutedEventArgs e)
    //    {
    //        CurrentInputMode = InputModes.Diaphragm;
    //        CurrentMode.Content = "SHEAR WALL ENTRY (RIGIDITY MODE)";
    //        Update();
    //    }

    //    /// <summary>
    //    /// Wall (diaphragm) input mode
    //    /// </summary>
    //    /// <param name="sender"></param>
    //    /// <param name="e"></param>
    //    private void btnMassMode_Click(object sender, RoutedEventArgs e)
    //    {
    //        CurrentInputMode = InputModes.Wall;
    //        CurrentMode.Content = "DIAPHRAGM ENTRY (MASS MODE)";
    //        Update();
    //    }
    //    #endregion
