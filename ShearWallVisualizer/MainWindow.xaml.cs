using calculator;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ShearWallCalculator;
using ShearWallCalculator.Interfaces;
using ShearWallCalculator.WindLoadCalculations;
using ShearWallVisualizer.Controls;
using ShearWallVisualizer.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static ShearWallVisualizer.Controls.DiaphragmDataControl;
using static ShearWallVisualizer.Controls.WallDataControl;

namespace ShearWallVisualizer
{
    public partial class MainWindow : Window
    {
        public ShearWallCalculatorBase Calculator = new ShearWallCalculator_RigidDiaphragm(null, null, 0, 0);

        public SimpsonCatalog simpsonCatalog { get; set; } = new SimpsonCatalog();  // contains the Simposon catalog connector and holddown data

        private JsonDrawingSerializer _serializer = new JsonDrawingSerializer();

        /// <summary>
        /// Load data for the calculator
        /// </summary>
        private double currentMagX = 0.0;
        private double currentLocX = 0.0;
        private double currentMagY = 0.0;
        private double currentLocY = 0.0;

        //// data for the image overlay
        //string selectedImageFilePath = null;
        //double pixelScaleX = 1.0;  // the scale factor for pixels to real-world coords
        //double pixelScaleY = 1.0;  // the scale factor for pixels to real-world coords

        // grid stuff
        bool gridNeedsUpdate = true;
        DrawingVisual gridVisual = null;
        private RenderTargetBitmap gridBitmap;
        double majorGridSpacing = 5.0;  // Major grid lines in world units
        double minorGridSpacing = 1.0;  // Minor grid lines in world units

        public EventHandler OnUpdated;  // the event that signals that the drawing has been updated -- controls will listen for this at the time they are created.

        bool hideImage = false;
        bool hideGrid = false;
        bool hideShapes = false;

        private double defaultWallHeight = 9.0;

        private enum DrawMode { None, Line, Rectangle }
        private DrawMode currentMode = DrawMode.Line;
        private bool snapMode = false;
        private double snapThreshold = 50; // Pixels
        private bool debugMode = false;

        private Point? startPoint_world = null;  // the first click in world coordinates
        private Point? endPoint_world = null;    // the second click in world coordinates

        private double zoomFactorX = 1.0;
        private double zoomFactorY = 1.0;
        private double panOffsetX = -25.0;
        private double panOffsetY = -25.0;

        private Shape previewShape = null;

        private double worldWidth = 120;
        private double worldHeight = 120;

        private Point currentMouseScreenPosition = new Point(0, 0);

        public MainWindow()
        {
            InitializeComponent();
            LoadRecentFilesMenu();

            this.KeyDown += MainWindow_KeyDown;

            // Set focus and canvas background
            this.Focusable = true;
            this.Focus();

            // the function to run once the app has loaded.
            this.Loaded += (s, e) =>
            {

                ResetView(); // reset the view so that origin 0,0 is at lower left of the corner screen and the model is zoomed to fill the entire window
                LoadRecentFilesMenu();  // recent files menu
                CreateGridVisual();
                CreateLayers();

                // load the Simpson catalog
                simpsonCatalog = new SimpsonCatalog();

                Update();

                MainTabControl.SelectedIndex = 0; // Show Dimensions tab by default

                // Events for wind load calculation
                ctrlWindLoadResultsControl_MWFRS.WindCalculated += WindLoadResultsControl_MWFRS_WindCalculated;
                WindLoadInputControl.WindInputComplete += WindLoadInputControl_WindInputComplete;
            };
        }

        public void Update()
        {
            // clear the tabs
            sp_DimPanel_Diaphragms.Children.Clear();
            sp_DimPanel_Walls.Children.Clear();
            sp_RigidCalcPanel.Children.Clear();
            sp_FlexibleCalcPanel.Children.Clear();

            if (Calculator != null)
            {
                Calculator.PerformCalculations();
                // update the calculator
                if (Calculator.IsValidForCalculation is true)
                {
                    CreateCalculationResultsControls_Rigid();
                    CreateCalculationResultsControls_Flexible();
                }

                // list the type of calculator
                tbCalculatorType.Text = Calculator.GetType().Name;

                string img_str = Calculator.selectedImageFilePath;
                if(Calculator.selectedImageFilePath == null || Calculator.selectedImageFilePath == String.Empty)
                {
                    img_str = "Image File: <No file selected>";
                }
                tbImageFileName.Text = img_str;

                // recreate the data controls
                CreateWallDataControls();
                CreateDiaphragmDataControls();

                // notify controls that we have updated
                OnUpdated?.Invoke(this, EventArgs.Empty); // signal that the window has been updated -- so that subcontrols can refresh
            }

            // update the load info display
            LoadInfoTextBlock.Text = $"X: {currentMagX} @ {currentLocX} | Y: {currentMagY} @ {currentLocY}";

            // Update the button appearances
            SetButtonModes();

            // redraw the scene
            Draw(ChangeType.Redraw);
        }

        /// <summary>
        /// Reset the view so that the full model scales to the drawing context area and the origin of the world coordinates is at the lower left corner
        /// </summary>
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


        private Point GetSnappedPoint(Point worldPoint)
        {
            foreach (var wall in Calculator._wall_system._walls)
            {
                if (IsWithinSnapThreshold(worldPoint, wall.Value.Start)) return wall.Value.Start;
                if (IsWithinSnapThreshold(worldPoint, wall.Value.End)) return wall.Value.End;
            }

            foreach (var dia in Calculator._diaphragm_system._diaphragms)
            {
                if (IsWithinSnapThreshold(worldPoint, dia.Value.P1)) return dia.Value.P1;
                if (IsWithinSnapThreshold(worldPoint, dia.Value.P2)) return dia.Value.P2;
                if (IsWithinSnapThreshold(worldPoint, dia.Value.P3)) return dia.Value.P3;
                if (IsWithinSnapThreshold(worldPoint, dia.Value.P4)) return dia.Value.P4;
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

        private void CreatePreviewShape()
        {
            if (currentMode == DrawMode.Line)
                previewShape = new Line { Stroke = Brushes.DarkGreen, StrokeThickness = 5 };
            else if (currentMode == DrawMode.Rectangle)
                previewShape = new Rectangle { Stroke = Brushes.DarkGreen, StrokeThickness = 5, Fill = Brushes.Transparent };
        }

        // Create the layers
        private void CreateLayers()
        {
            m_layers.AddLayer(0, DrawBackground, ChangeType.Resize);
            m_layers.AddLayer(1, DrawGridInformation);
            m_layers.AddLayer(2, DrawReferenceImage);
            m_layers.AddLayer(3, DrawBoundingBox);
            m_layers.AddLayer(4, DrawLoads);

            m_layers.AddLayer(41, DrawShapes);
            m_layers.AddLayer(52, DrawCursor);
            m_layers.AddLayer(51, DrawPreview);
            m_layers.AddLayer(43, DrawSnapMarkers);
            m_layers.AddLayer(50, DrawBracedWallLines);
            m_layers.AddLayer(80, DrawCOMandCOR);
            m_layers.AddLayer(100, DrawDebug);
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

                if (Calculator._wall_system == null)
                {
                    Calculator._wall_system = new WallSystem();
                }

                Calculator._wall_system.AddWall(new WallData(defaultWallHeight, startPoint.X, startPoint.Y, endPoint.X, endPoint.Y));
            }
            else if (currentMode == DrawMode.Rectangle)
            {
                if (Calculator._diaphragm_system == null)
                {
                        Calculator._diaphragm_system = new DiaphragmSystem();
                }

                    Calculator._diaphragm_system.AddDiaphragm(new DiaphragmData_Rectangular(startPoint_world.Value, endPoint_world.Value));
            } else
            {
                throw new NotImplementedException("Error: FinalizeShape() received an invalid DrawMode variable.");
            }

            // Recreate the calculator with the new items added
            if(Calculator is ShearWallCalculator_RigidDiaphragm)
            {
                Calculator = new ShearWallCalculator_RigidDiaphragm(Calculator._wall_system, Calculator._diaphragm_system, currentMagX, currentMagX);
            } else
            {
                Calculator = new ShearWallCalculator_FlexibleDiaphragm(Calculator._wall_system, Calculator._diaphragm_system, currentMagX, currentMagX);
            }
            Calculator.PerformCalculations();  // perform the calculations with the new Calculator

            // Clear the preview shape from the screen.
            previewShape = null;
            startPoint_world = null;
            endPoint_world = null;
        }

        public void PrintList<T>(List<T> lst)
        {
            if (lst == null)
                return;

            foreach (var item in lst)
            {
                Console.WriteLine(item.ToString());
            }
        }

        #region UI Control Related Events
        /// <summary>
        /// Event listener for when input of the wind loads as been completed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WindLoadInputControl_WindInputComplete(object sender, WindLoadInputControl.OnWindInputCompleteEventArgs e)
        {
            if (ctrlWindLoadResultsControl_MWFRS != null)
            {
                ctrlWindLoadResultsControl_MWFRS.WindCalculated -= WindLoadResultsControl_MWFRS_WindCalculated;
            }

            WindLoadResultsControl_MWFRS ctrl = new WindLoadResultsControl_MWFRS(e._parameters);

            ctrl.WindCalculated += WindLoadResultsControl_MWFRS_WindCalculated;
            ctrlWindLoadResultsControl_MWFRS.Content = ctrl;

            ctrlWindLoadResultsControl_MWFRS = ctrl;

            tabWindResults.Visibility = Visibility.Visible;
            tabWindResults.IsSelected = true;
        }

        /// <summary>
        /// Event listener for when wind load calculations have been completed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WindLoadResultsControl_MWFRS_WindCalculated(object sender, WindLoadResultsControl_MWFRS.OnWindCalculatedEventArgs e)
        {
            List<WindLoadCalculator_MWFRS.WindPressureResult_Wall_MWFRS> wall_results = e._wall_results;
            List<WindLoadCalculator_MWFRS.WindPressureResult_Roof_MWFRS> roof_results = e._roof_results;
            WindLoadCalculator_MWFRS.WindLoadParameters parameters = e._parameters;

            // Get the internal suction windward case
            double ww = 0;
            double lw = 0;

            foreach (var wall in wall_results)
            {
                if (wall.Surface == "Windward Wall - z=h")
                {
                    ww = wall.PressBaseA;
                }

                if (wall.Surface == "Leeward Wall")
                {
                    lw = wall.PressBaseA;
                }
            }

            // worst x case will be +WW and -LW -- internal suction should offset each other.
            currentMagX = (ww - lw) * parameters.BuildingHeight * parameters.BuildingWidth / 1000; // net sum at elevation h
            currentMagY = 0;

            Update();
        }

        private void CreateWallDataControls()
        {
            if (Calculator == null || Calculator._wall_system == null)
            {
                return;
            }

            WallSystemControl sysControl = new WallSystemControl(this, Calculator._wall_system);
            sp_DimPanel_Walls.Children.Add(sysControl);
            sysControl.OnWallSubControlDeleted += WallDeleted;
        }

        private void WallDeleted(object sender, EventArgs e)
        {

            if (Calculator == null || Calculator._wall_system == null)
            {
                return;
            }

            WallDataControl control = sender as WallDataControl;
            DeleteWallEventArgs args = e as DeleteWallEventArgs;

            foreach (var wall in Calculator._wall_system._walls)
            {
                if (wall.Key == args.Id)
                {
                    Calculator._wall_system._walls.Remove(wall.Key);
                    Update();
                    return;
                }
            }
        }

        private void DiaphragmDeleted(object sender, EventArgs e)
        {
            if (Calculator == null || Calculator._diaphragm_system == null)
            {
                return;
            }

            DiaphragmDataControl control = sender as DiaphragmDataControl;
            DeleteDiaphragmEventArgs args = e as DeleteDiaphragmEventArgs;

            foreach (var dia in Calculator._diaphragm_system._diaphragms)
            {
                if (dia.Key == args.Id)
                {
                    Calculator._diaphragm_system._diaphragms.Remove(dia.Key);
                    Update();
                    return;
                }
            }
        }

        private void CreateDiaphragmDataControls()
        {
            if (Calculator == null || Calculator._diaphragm_system == null)
            {
                return;
            }

            DiaphragmSystemControl sysControl = new DiaphragmSystemControl(this, Calculator._diaphragm_system);
            sp_DimPanel_Diaphragms.Children.Add(sysControl);
            sysControl.OnDiaphragmSubControlDeleted += DiaphragmDeleted;
        }


        private void CreateCalculationResultsControls_Rigid()
        {
            if (Calculator == null || Calculator._wall_system == null)
            {
                foreach (var wall in Calculator._wall_system._walls)
                {
                    int id = wall.Key;
                    var rigidity = wall.Value.WallRigidity;

                    double xbar = double.NaN;
                    double ybar = double.NaN;

                    if (Calculator._wall_system.X_bar_walls.ContainsKey(id) is true)
                    {
                        xbar = Calculator._wall_system.X_bar_walls[id];
                    }

                    if (Calculator._wall_system.Y_bar_walls.ContainsKey(id) is true)
                    {
                        ybar = Calculator._wall_system.Y_bar_walls[id];
                    }

                    if (Calculator is ShearWallCalculator_RigidDiaphragm)
                    {
                        ShearWallCalculator_RigidDiaphragm calc = Calculator as ShearWallCalculator_RigidDiaphragm;
                        // Must check validity of numbers since some walls may be in X direction and others in Y direction
                        double vi_x = double.NaN;
                        if (calc.DirectShear_X.ContainsKey(id) is true)
                        {
                            vi_x = calc.DirectShear_X[id];
                        }

                        var vi_y = double.NaN;
                        if (calc.DirectShear_Y.ContainsKey(id) is true)
                        {
                            vi_y = calc.DirectShear_Y[id];
                        }

                        var v_ecc = double.NaN;
                        if (calc.EccentricShear.ContainsKey(id) is true)
                        {
                            v_ecc = calc.EccentricShear[id];
                        }

                        var v_tot = double.NaN;
                        if (calc.TotalWallShear.ContainsKey(id) is true)
                        {
                            v_tot = calc.TotalWallShear[id];
                        }

                        ShearWallResultsControl_Rigid control = new ShearWallResultsControl_Rigid(id, rigidity, xbar, ybar, vi_x, vi_y, v_ecc, v_tot);
                        sp_RigidCalcPanel.Children.Add(control);
                    }
                }
            }
        }

        private void CreateCalculationResultsControls_Flexible()
        {
            if (Calculator == null || Calculator._wall_system == null)
            {

                foreach (var wall in Calculator._wall_system._walls)
                {
                    int id = wall.Key;
                    var rigidity = wall.Value.WallRigidity;

                    double xbar = double.NaN;
                    double ybar = double.NaN;

                    if (Calculator._wall_system.X_bar_walls.ContainsKey(id) is true)
                    {
                        xbar = Calculator._wall_system.X_bar_walls[id];
                    }

                    if (Calculator._wall_system.Y_bar_walls.ContainsKey(id) is true)
                    {
                        ybar = Calculator._wall_system.Y_bar_walls[id];
                    }
                    if (Calculator is ShearWallCalculator_FlexibleDiaphragm)
                    {
                        ShearWallCalculator_FlexibleDiaphragm calc = Calculator as ShearWallCalculator_FlexibleDiaphragm;
                        // Must check validity of numbers since some walls may be in X direction and others in Y direction
                        double vi_x = double.NaN;
                        if (calc.DirectShear_X.ContainsKey(id) is true)
                        {
                            vi_x = calc.DirectShear_X[id];
                        }

                        var vi_y = double.NaN;
                        if (calc.DirectShear_Y.ContainsKey(id) is true)
                        {
                            vi_y = calc.DirectShear_Y[id];
                        }

                        var v_tot = double.NaN;
                        if (calc.TotalWallShear.ContainsKey(id) is true)
                        {
                            v_tot = calc.TotalWallShear[id];
                        }

                        ShearWallResultsControl_Flexible control = new ShearWallResultsControl_Flexible(id, vi_x, vi_y);
                        sp_FlexibleCalcPanel.Children.Add(control);
                    }
                }
            }
        }

        #endregion

        #region Drawing functions

        private void DrawBackground(DrawingContext ctx)
        {
            var pen = new Pen(Brushes.Black, 1);
            var rect = new Rect(0, 0, m_layers.ActualWidth, m_layers.ActualHeight);
            ctx.DrawRoundedRectangle(Brushes.White, pen, rect, 0, 0);
        }

        /// <summary>
        /// Creates the grid in screen coordinates.  Called by CreateGridVisual which stores it in a bitmap.
        /// </summary>
        /// <param name="ctx"></param>
        private void ConstructVisualGrid(DrawingContext ctx)
        {
            double major_gridline_thickness = 0.5;
            double minor_gridline_thickness = 0.25;

            if (hideGrid is true)
            {
                return;
            }

            // thickness of the minor gridlines
            Pen pen;

            double layerWidth = dockpanel.ActualWidth;
            double layerHeight = dockpanel.ActualHeight;

            // Draw vertical grid lines (major and minor) in world coordinates
            for (double x = 0; x <= layerWidth; x += minorGridSpacing)
            {
                Point p1 = WorldToScreen(new Point(x, 0), m_layers);
                Point p2 = WorldToScreen(new Point(x, layerHeight), m_layers);

                if (p1.X < 0 || p1.X > layerWidth || p2.X < 0 || p2.X > layerWidth)
                {
                    continue;  // if we are outside of the view box, no need to draw the grid
                }

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

                if (x % (2 * majorGridSpacing) == 0)
                {
                    // Add a marker at every two major gridlines
                    // display the com as a text
                    FormattedText idLabel = new FormattedText(
                        $"{x}",
                        CultureInfo.GetCultureInfo("en-us"),
                        FlowDirection.LeftToRight,
                        new Typeface("Consolas"),
                        9,
                        Brushes.Gray,
                        VisualTreeHelper.GetDpi(this).PixelsPerDip);

                    
                    ctx.DrawText(idLabel, new Point(p1.X-5, p1.Y));
                }
            }

            // Draw horizontal grid lines (major and minor)
            for (double y = 0; y <= layerHeight; y += minorGridSpacing)
            {
                Point p1 = WorldToScreen(new Point(0, y), m_layers);
                Point p2 = WorldToScreen(new Point(layerWidth, y), m_layers);

                if (p1.Y < 0 || p1.Y > layerHeight || p2.Y < 0 || p2.Y > layerHeight)
                {
                    continue;  // if we are outside of the view box, no need to draw the grid
                }

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

                if (y % (2 * majorGridSpacing) == 0)
                {
                    // Add a marker at every two major gridlines
                    // display the com as a text
                    FormattedText idLabel = new FormattedText(
                        $"{y}",
                        CultureInfo.GetCultureInfo("en-us"),
                        FlowDirection.LeftToRight,
                        new Typeface("Consolas"),
                        9,
                        Brushes.Gray,
                        VisualTreeHelper.GetDpi(this).PixelsPerDip);

                    ctx.DrawText(idLabel, new Point(p1.X-12, p1.Y-5));
                }
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

        private void DrawLoads(DrawingContext ctx)
        {

        }

        private void DrawBoundingBox(DrawingContext ctx)
        {
            if (Calculator is null) return;

            Rect rect = Calculator.BoundingBoxWorld;

            var p1 = WorldToScreen(rect.BottomLeft, m_layers);
            var p2 = WorldToScreen(rect.BottomRight, m_layers);
            var p3 = WorldToScreen(rect.TopRight, m_layers);
            var p4 = WorldToScreen(rect.TopLeft, m_layers);

            ctx.DrawLine(new Pen(Brushes.Green, 1), p1, p2);
            ctx.DrawLine(new Pen(Brushes.Green, 1), p2, p3);
            ctx.DrawLine(new Pen(Brushes.Green, 1), p3, p4);
            ctx.DrawLine(new Pen(Brushes.Green, 1), p4, p1);
        }

        /// <summary>
        /// Draws the preview shapes which constructing walls or diaphragms
        /// </summary>
        /// <param name="ctx"></param>
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
                // For the line to be horizontal or vertical only
                preview_endPoint_world = GetConstrainedPoint(preview_endPoint_world, startPoint_world.Value); // Ensure alignment

                SolidColorBrush lineStrokeBrush = new SolidColorBrush(Colors.Green);
                lineStrokeBrush.Opacity = 0.5;
                Pen pen = new Pen(lineStrokeBrush, 4);
                Point p1 = WorldToScreen(startPoint_world.Value, m_layers);
                Point p2 = WorldToScreen(preview_endPoint_world, m_layers);
                ctx.DrawLine(pen, p1, p2);
            }
            else if (previewShape is Rectangle rect)
            {
                SolidColorBrush rectFillBrush = new SolidColorBrush(Colors.Green);
                rectFillBrush.Opacity = 0.5;
                Pen pen = new Pen(rectFillBrush, 4);
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
            if(hideShapes is true)
            {
                return;
            }
            double center_pt_dia = 5;

            if ((Calculator != null) && (Calculator._wall_system != null))
            {
                // Redraw all the shapes in world coordinates
                foreach (var wall in Calculator._wall_system._walls)
                {
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
                    Pen pen = new Pen(lineStrokeBrush, 4);
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

                        idLabel = new FormattedText(
                            wall.Key.ToString(),
                            CultureInfo.GetCultureInfo("en-us"),
                            FlowDirection.LeftToRight,
                            new Typeface("Consolas"),
                            14,
                            Brushes.Black,
                            VisualTreeHelper.GetDpi(this).PixelsPerDip);
                        ctx.DrawText(idLabel, center_screen);  // id label
                    }
                }
            }

            // Redraw all the shapes in world coordinates
            if ((Calculator != null) && (Calculator._diaphragm_system != null))
            {
                foreach (var rect in Calculator._diaphragm_system._diaphragms)
                {
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

                        idLabel = new FormattedText(
                            rect.Key.ToString(),
                            CultureInfo.GetCultureInfo("en-us"),
                            FlowDirection.LeftToRight,
                            new Typeface("Consolas"),
                            14,
                            Brushes.Black,
                            VisualTreeHelper.GetDpi(this).PixelsPerDip);
                        ctx.DrawText(idLabel, centerPoint);  // id label
                    }
                }
            }
        }

        private void DrawDebug(DrawingContext ctx)
        {
            if (debugMode == false)
                return;

            if (Calculator == null) return;

            if (Calculator._wall_system == null && Calculator._wall_system._walls != null)
            {
                // Redraw all the shapes in world coordinates
                FormattedText idLabel = null;
                foreach (var wall in Calculator._wall_system._walls)
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

            if (Calculator._diaphragm_system != null && Calculator._diaphragm_system._diaphragms != null)
            {
                FormattedText idLabel = null;

                foreach (var dia in Calculator._diaphragm_system._diaphragms)
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

        private void DrawReferenceImage(DrawingContext context)
        {
            if (hideImage is true)
            {
                m_layers.RemoveImageLayer(0);
                return;
            }

            if (Calculator != null)
            {

                if (Calculator.selectedImageFilePath is null)
                {
                    return;
                }

                m_layers.AddImageLayer(Calculator.selectedImageFilePath, Calculator.pixelScaleX, Calculator.pixelScaleY); // scale the image so that pixel scale aligns with world scale.
                m_layers.currentReferenceImageLayer.SetOpacity(0.30); // fade the image a bit

                double width = m_layers.currentReferenceImageLayer.TargetRect.Width;
                double height = m_layers.currentReferenceImageLayer.TargetRect.Height;

                Rect imageWorldRect = new Rect(0, 0, width, height);

                double y_offset = height;

                Point p4_world = new Point(imageWorldRect.TopLeft.X, imageWorldRect.TopLeft.Y + y_offset);
                Point p3_world = new Point(imageWorldRect.TopRight.X, imageWorldRect.TopRight.Y + y_offset);
                Point p2_world = new Point(imageWorldRect.BottomRight.X, imageWorldRect.BottomRight.Y + y_offset);
                Point p1_world = new Point(imageWorldRect.BottomLeft.X, imageWorldRect.BottomLeft.Y + y_offset);

                Point p1_screen = WorldToScreen(p1_world, m_layers);
                Point p2_screen = WorldToScreen(p2_world, m_layers);
                Point p3_screen = WorldToScreen(p3_world, m_layers);
                Point p4_screen = WorldToScreen(p4_world, m_layers);

                double width_screen = p3_screen.X - p1_screen.X;
                double height_screen = p3_screen.Y - p1_screen.Y;

                Rect imageScreenRect = new Rect(p1_screen.X, p1_screen.Y, width_screen, height_screen);

                m_layers.currentReferenceImageLayer.TargetRect = imageScreenRect;
                m_layers.currentReferenceImageLayer.SetPosition(p4_screen.X, p4_screen.Y);
            }
        }
        private void DrawBracedWallLines(DrawingContext ctx)
        {
            if (Calculator == null) return;
            // do we have a wall system or BWL manager created yet?
            if(Calculator._wall_system is null || Calculator._wall_system.BWL_Manager is null)
            {
                return;
            }

            // the counter for uniquely numbering the brace wall lines
            // TODO should this be handled by the Calculator instead of when its being drawn?
            int bwl_count = 1;

            for (int i = 0; i < Calculator._wall_system.BWL_Manager.BracedWallLines.Count; i++)
            {
                BracedWallLine bwl = Calculator._wall_system.BWL_Manager.BracedWallLines[i];
                int bwl_id = bwl.GroupNumber;

                if (bwl.WallDir == WallDirs.EastWest)
                {
                    double center = bwl.Center.Y;

                    Point p1_world = new Point(-10, center);
                    Point p1_screen = GetConstrainedScreenPoint(WorldToScreen(p1_world, dockpanel), dockpanel); ;
                    Point p2_world = new Point(dockpanel.ActualWidth, center);
                    Point p2_screen = GetConstrainedScreenPoint(WorldToScreen(p2_world, dockpanel), dockpanel); ;

                    Pen pen = new Pen(Brushes.Black, 2);
                    pen.DashStyle = new DashStyle(new double[] { 3, 1, 3 }, 0);
                    ctx.DrawLine(pen, p1_screen, p2_screen);

                    // display the com as a text
                    FormattedText idLabel = new FormattedText(
                        $"BWL{bwl_id.ToString()}",
                        CultureInfo.GetCultureInfo("en-us"),
                        FlowDirection.LeftToRight,
                        new Typeface("Consolas"),
                        14,
                        Brushes.Black,
                        VisualTreeHelper.GetDpi(this).PixelsPerDip);

                    ctx.DrawText(idLabel, new Point(p1_screen.X - 40, p1_screen.Y - 7));

                    bwl_count++;
                }
                else if (bwl.WallDir == WallDirs.NorthSouth)
                {
                    double center = bwl.Center.X;

                    Point p1_world = new Point(center, -10);
                    Point p1_screen = GetConstrainedScreenPoint(WorldToScreen(p1_world, dockpanel), dockpanel); ;
                    Point p2_world = new Point(center, dockpanel.ActualHeight);
                    Point p2_screen = GetConstrainedScreenPoint(WorldToScreen(p2_world, dockpanel), dockpanel); ;

                    Pen pen = new Pen(Brushes.Black, 2);
                    pen.DashStyle = new DashStyle(new double[] { 3, 1, 3 }, 0);
                    ctx.DrawLine(pen, p1_screen, p2_screen);

                    // display the com as a text
                    FormattedText idLabel = new FormattedText(
                        $"BWL{bwl_id.ToString()}",
                        CultureInfo.GetCultureInfo("en-us"),
                        FlowDirection.LeftToRight,
                        new Typeface("Consolas"),
                        14,
                        Brushes.Black,
                        VisualTreeHelper.GetDpi(this).PixelsPerDip);

                    ctx.PushTransform(new RotateTransform(90, p1_screen.X, p1_screen.Y + 10));
                    ctx.DrawText(idLabel, new Point(p1_screen.X, p1_screen.Y));
                    ctx.Pop();  // remember to pop the transform after its been used.
                }
            }
        }
        private void DrawSnapMarkers(DrawingContext ctx)
        {
            if (snapMode == false)
            {
                return;
            }

            if (Calculator == null) return;

            if (Calculator._wall_system != null)
            {
                foreach (var wall in Calculator._wall_system._walls)
                {
                    Point p1_world = wall.Value.Start;
                    Point p2_world = wall.Value.End;
                    Point p1_screen = WorldToScreen(p1_world, m_layers);
                    Point p2_screen = WorldToScreen(p2_world, m_layers);

                    ctx.DrawEllipse(Brushes.MediumBlue, new Pen(Brushes.MediumBlue, 1), p1_screen, 3, 3);
                    ctx.DrawEllipse(Brushes.MediumBlue, new Pen(Brushes.MediumBlue, 1), p2_screen, 3, 3);
                }
            }

            if (Calculator._diaphragm_system != null)
            {
                foreach (var dia in Calculator._diaphragm_system._diaphragms)
                {
                    Point p1_world = dia.Value.P1;
                    Point p2_world = dia.Value.P2;
                    Point p3_world = dia.Value.P3;
                    Point p4_world = dia.Value.P4;
                    Point p1_screen = WorldToScreen(p1_world, m_layers);
                    Point p2_screen = WorldToScreen(p2_world, m_layers);
                    Point p3_screen = WorldToScreen(p3_world, m_layers);
                    Point p4_screen = WorldToScreen(p4_world, m_layers);

                    ctx.DrawEllipse(Brushes.Red, new Pen(Brushes.Red, 1), p1_screen, 3, 3);
                    ctx.DrawEllipse(Brushes.Red, new Pen(Brushes.Red, 1), p2_screen, 3, 3);
                    ctx.DrawEllipse(Brushes.Red, new Pen(Brushes.Red, 1), p3_screen, 3, 3);
                    ctx.DrawEllipse(Brushes.Red, new Pen(Brushes.Red, 1), p4_screen, 3, 3);
                }
            }
        }

        private void DrawCOMandCOR(DrawingContext ctx)
        {
            if (Calculator is null) return;

            // Draw a marker for the center of mass
            if (Calculator != null)
            {
                if(Calculator._diaphragm_system != null)
                {
                    // Draw the center of mass and center of rigidity
                    var com = Calculator._diaphragm_system.CtrMass;

                    // display the com as a text
                    FormattedText idLabel = new FormattedText(
                        $"COM ({com.X.ToString("F2")}, {com.Y.ToString("F2")})",
                        CultureInfo.GetCultureInfo("en-us"),
                        FlowDirection.LeftToRight,
                        new Typeface("Consolas"),
                        14,
                        Brushes.Black,
                        VisualTreeHelper.GetDpi(this).PixelsPerDip);

                    ctx.DrawText(idLabel, new Point(5, 5));

                    double x_screen = WorldToScreen(com, dockpanel).X;
                    double y_screen = WorldToScreen(com, dockpanel).Y;

                    // draw a vertical line since the Y is valid
                    if ((double.IsNaN(com.X) is false) && (double.IsNaN(com.Y) is true))
                    {
                        Pen pen = new Pen(Brushes.Red, 2);
                        pen.DashStyle = new DashStyle(new double[] { 3, 3 }, 0);

                        x_screen = 10; // draw it justbelow the top
                        ctx.DrawLine(pen, new Point(x_screen, 0), new Point(x_screen, dockpanel.ActualHeight));
                    }

                    // draw a vertical line since if the X is valid
                    else if ((double.IsNaN(com.Y) is false) && (double.IsNaN(com.X) is true))
                    {
                        Pen pen = new Pen(Brushes.Red, 2);
                        pen.DashStyle = new DashStyle(new double[] { 3, 3 }, 0);

                        y_screen = dockpanel.ActualHeight - 10;

                        ctx.DrawLine(pen, new Point(0, y_screen), new Point(dockpanel.ActualHeight, y_screen));
                    }

                    // draw two lines since neither value is valid
                    else if ((double.IsNaN(com.X) is true) && (double.IsNaN(com.Y) is true))
                    {
                        Pen pen = new Pen(Brushes.Red, 2);
                        pen.DashStyle = new DashStyle(new double[] { 3, 3 }, 0);

                        y_screen = dockpanel.ActualHeight - 10;
                        x_screen = 10;

                        ctx.DrawLine(pen, new Point(0, y_screen), new Point(dockpanel.ActualWidth, y_screen));
                        ctx.DrawLine(pen, new Point(x_screen, 0), new Point(x_screen, dockpanel.ActualHeight));
                    }

                    // Draw a marker
                    Point pt = new Point(x_screen, y_screen);

                    if (PointIsWithinBounds(pt, dockpanel))
                    {
                        ctx.DrawEllipse(Brushes.Red, new Pen(Brushes.Black, 2), pt, 8, 8);
                        ctx.DrawEllipse(Brushes.Red, new Pen(Brushes.Black, 2), pt, 5, 5);

                        idLabel = new FormattedText(
                            $"COM",
                            CultureInfo.GetCultureInfo("en-us"),
                            FlowDirection.LeftToRight,
                            new Typeface("Consolas"),
                            14,
                            Brushes.Black,
                            VisualTreeHelper.GetDpi(this).PixelsPerDip);

                        ctx.DrawText(idLabel, new Point(x_screen + 5, y_screen - 20));
                    }
                }




            }

            if (Calculator != null && Calculator._wall_system != null)
            {
                // display the cor as a text
                var cor = Calculator._wall_system.CtrRigidity;

                FormattedText idLabel = new FormattedText(
                    $"COR ({cor.X.ToString("F2")}, {cor.Y.ToString("F2")})",
                    CultureInfo.GetCultureInfo("en-us"),
                    FlowDirection.LeftToRight,
                    new Typeface("Consolas"),
                    14,
                    Brushes.Black,
                    VisualTreeHelper.GetDpi(this).PixelsPerDip);

                ctx.DrawText(idLabel, new Point(5, 19));
                double x_screen = WorldToScreen(cor, dockpanel).X;
                double y_screen = WorldToScreen(cor, dockpanel).Y;

                // draw a vertical line since the Y is valid
                if ((double.IsNaN(cor.X) is false) && (double.IsNaN(cor.Y) is true))
                {

                    Pen pen = new Pen(Brushes.MediumBlue, 2);
                    pen.DashStyle = new DashStyle(new double[] { 3, 3 }, 0);

                    y_screen = 10; // draw it justbelow the top
                    ctx.DrawLine(pen, new Point(x_screen, 0), new Point(x_screen, dockpanel.ActualHeight));
                }

                // draw a horizontal line since if the X is valid
                else if ((double.IsNaN(cor.Y) is false) && (double.IsNaN(cor.X) is true))
                {
                    Pen pen = new Pen(Brushes.MediumBlue, 2);
                    pen.DashStyle = new DashStyle(new double[] { 3, 3 }, 0);

                    x_screen = dockpanel.ActualWidth - 10;

                    ctx.DrawLine(pen, new Point(0, y_screen), new Point(dockpanel.ActualWidth, y_screen));
                }

                // draw two lines since neither value is valid
                else if ((double.IsNaN(cor.X) is true) && (double.IsNaN(cor.Y) is true))
                {
                    Pen pen = new Pen(Brushes.MediumBlue, 2);
                    pen.DashStyle = new DashStyle(new double[] { 3, 3 }, 0);

                    y_screen = 10;
                    x_screen = dockpanel.ActualWidth - 10;

                    ctx.DrawLine(pen, new Point(x_screen, 0), new Point(x_screen, dockpanel.ActualHeight));
                    ctx.DrawLine(pen, new Point(0, y_screen), new Point(dockpanel.ActualWidth, y_screen));
                }

                // Draw a marker
                Point pt = new Point(x_screen, y_screen);

                if (PointIsWithinBounds(pt, dockpanel))
                {
                    ctx.DrawEllipse(Brushes.MediumBlue, new Pen(Brushes.Black, 2), pt, 8, 8);
                    ctx.DrawEllipse(Brushes.MediumBlue, new Pen(Brushes.Black, 2), pt, 5, 5);

                    idLabel = new FormattedText(
                        $"COR",
                        CultureInfo.GetCultureInfo("en-us"),
                        FlowDirection.LeftToRight,
                        new Typeface("Consolas"),
                        14,
                        Brushes.Black,
                        VisualTreeHelper.GetDpi(this).PixelsPerDip);

                    ctx.DrawText(idLabel, new Point(x_screen - 30, y_screen + 5));
                }
            }
        }

        private void DrawGridInformation(DrawingContext ctx)
        {
            if (hideGrid)
            {
                return;  // Skip drawing if the grid is hidden
            }

            // Check if zoom or pan has changed
            if (gridNeedsUpdate || gridVisual == null || gridBitmap == null)
            {
                // Recreate the grid visual when necessary (zoom or pan has changed)
                CreateGridVisual();
                gridNeedsUpdate = false;
            }

            // Draw the cached grid bitmap
            ctx.DrawImage(gridBitmap, new Rect(0, 0, dockpanel.ActualWidth, dockpanel.ActualHeight));
        }

        private void CreateGridVisual()
        {
            // Define the size of the render target bitmap (same size as your drawing area)
            double width = dockpanel.ActualWidth;
            double height = dockpanel.ActualHeight;

            // Create a new RenderTargetBitmap with the same size as the drawing area
            gridBitmap = new RenderTargetBitmap((int)width, (int)height, 96, 96, PixelFormats.Pbgra32);

            // Create a DrawingVisual to draw the grid
            gridVisual = new DrawingVisual();

            using (DrawingContext ctx = gridVisual.RenderOpen())
            {
                ConstructVisualGrid(ctx);  // This is the method that draws the grid
            }

            // Render the DrawingVisual to the RenderTargetBitmap
            gridBitmap.Render(gridVisual);
        }

        private void InvalidateGrid()
        {
            // Mark the grid as needing an update (this should be called when zoom or pan changes)
            gridNeedsUpdate = true;
        }


        private void Draw(ChangeType change)
        {
            m_layers.Draw(change);
        }

        #endregion

        #region Drawing Layer Events

        private void m_layers_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            //MessageBox.Show("Drawing Visual Clicked at " + e.GetPosition(m_layers).ToString());

            if (e.RightButton == MouseButtonState.Pressed)
            {
                ResetInputMode();
                Update();
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

            Update();
        }


        private void m_layers_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            currentMouseScreenPosition = e.GetPosition(m_layers);
            Point currentMouseWorldPosition = ScreenToWorld(currentMouseScreenPosition, m_layers);

            tbScreenCoords.Text = e.GetPosition(m_layers).ToString();
            tbWorldCoords.Text = "World Coords: (" + currentMouseWorldPosition.X.ToString("F2") + ", " + currentMouseWorldPosition.Y.ToString("F2") + ")";  // changed this one too

            Update();
        }

        private void m_layers_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Determine the zoom factor
            double zoomFactor = (e.Delta > 0) ? 1.2 : 0.8;

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

            InvalidateGrid();               // signal that the grid needs updating

            Update();
        }

        private void m_layers_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Right button click to CANCEL
            if (e.RightButton == MouseButtonState.Pressed)
            {
                ResetInputMode();
                Update();
                return;
            }
        }

        #endregion

        #region UI Events
        private void btnTestDesign_Click(object sender, RoutedEventArgs e)
        {
            if (Calculator == null || Calculator._wall_system == null || Calculator._diaphragm_system == null)
            {
                MessageBox.Show("No valid calculator found in btnTEstDesign_Click.");
                return;
            }
            else
            {
                Calculator = new ShearWallCalculator_RigidDiaphragm(Calculator._wall_system, Calculator._diaphragm_system, 15, 0);

                // test wall key
                int wall_id = 0;

                if (Calculator._wall_system._walls.ContainsKey(wall_id) is true)
                {
                    WallData test_wall = Calculator._wall_system._walls[wall_id];
                    ShearWallSelector selector = new ShearWallSelector(Calculator.TotalWallShear[wall_id], test_wall, simpsonCatalog, ConnectorTypes.CONNECTOR_STRAP_TIES, WoodTypes.WOODTYPE_DF_SP);
                    Console.WriteLine("--------------------------");
                    Console.WriteLine("Shear: " + Calculator.TotalWallShear[wall_id]);
                    foreach (var key in selector.selectedConnectors)
                    {

                        Console.WriteLine(key.Model);
                    }
                    Console.WriteLine("--------------------------");
                }

                Update();
            }
        }

        private void btnHideShapes_Click(object sender, RoutedEventArgs e)
        {
            hideShapes = !hideShapes;
            Update();
            btnHideShapes.Content = hideShapes ? "Show Shapes" : "Hide Shapes";

        }

        private void btnHideImage_Click(object sender, RoutedEventArgs e)
        {
            hideImage = !hideImage;
            Update();

            btnHideImage.Content = hideImage ? "Show Image" : "Hide Image";
        }

        private void btnHideGrid_Click(object sender, RoutedEventArgs e)
        {
            hideGrid = !hideGrid;
            Update();
            btnHideGrid.Content = hideGrid ? "Show Grid" : "Hide Grid";
        }

        private void btnLineMode_Click(object sender, RoutedEventArgs e)
        {
            currentMode = DrawMode.Line;
        }

        private void btnRectangleMode_Click(object sender, RoutedEventArgs e)
        {
            currentMode = DrawMode.Rectangle;
        }

        private void btnSnapMode_Click(object sender, RoutedEventArgs e)
        {
            SetSnapMode();
        }

        private void btnOpenLoadDialog_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new LoadInputDialog(currentMagX, currentLocX, currentMagY, currentLocY)
            {
                Owner = this
            };

            if (dialog.ShowDialog() == true)
            {
                currentMagX = dialog.MagnitudeX;
                currentLocX = dialog.LocationX;
                currentMagY = dialog.MagnitudeY;
                currentLocY = dialog.LocationY;

                // Do something with the updated values
                MessageBox.Show($"Updated Load Info:\nX: {currentMagX} @ {currentLocX}\nY: {currentMagY} @ {currentLocY}");

                Update();  // update the calculator
            }
        }

        #endregion

        #region Menu and Key Events
        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            Console.WriteLine($"Key Pressed: {e.Key}");

            // line (wall) mode
            if (e.Key == Key.L)
            {
                ResetInputMode();
                currentMode = DrawMode.Line;
            }

            // rectangle (diaphragm) mode
            else if (e.Key == Key.R)
            {
                ResetInputMode();
                currentMode = DrawMode.Rectangle;
            }
            // snap mode
            else if (e.Key == Key.S)
            {
                SetSnapMode();
            }
            // debug mode
            else if (e.Key == Key.D)
            {
                SetDebugMode();
            }
            // reset view 
            else if (e.Key == Key.Z)
            {
                ResetInputMode();
                ResetView();
                InvalidateGrid();
            }

            Update();
        }

        private void OpenImageTool_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ImageMeasurementWindow
            {
                Owner = this
            };

            dialog.MeasurementCompleted += OnMeasurementCompleted;
            dialog.ShowDialog();

            //if (dialog.ShowDialog() == true && dialog.Result != null)
            //{
            //    var result = dialog.Result;

            //    MessageBox.Show(
            //        $"Image: {result.FilePath}\n" +
            //        $"Pixel Distance: {result.PixelDistance:F2}\n" +
            //        $"Real Distance: {result.RealWorldDistance:F2}\n" +
            //        $"Scale Factor: {result.ScaleFactor:F4} units/pixel",
            //        "Measurement Result");
            //}
        }
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            // You can refresh your data-bound controls here.
            MessageBox.Show("Refresh triggered!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LoadImageFile(string filename, double pixel_scale_x=1.0, double pixel_scale_y=1.0)
        {
            if (Calculator != null)
            {

                // Open document
                Calculator.selectedImageFilePath = filename;
                Calculator.pixelScaleX = pixel_scale_x;
                Calculator.pixelScaleY = pixel_scale_y;

                // You ca now use selectedFilePath as needed
                MessageBox.Show($"You selected: {Calculator.selectedImageFilePath}");

                AddToRecentFiles(Calculator.selectedImageFilePath); // add to recent files list
            }
        }

        private void MenuItem_Save_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "JSON Drawing (*.json)|*.json",
                FileName = "drawing.json"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                _serializer.Save(saveFileDialog.FileName, Calculator);
                MessageBox.Show("Drawing saved!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void MenuItem_Load_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "JSON Drawing (*.json)|*.json"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                JsonSerializerSettings _settings = new JsonSerializerSettings();

                var json = _serializer.Load(openFileDialog.FileName);

                // Parse just enough to extact a fieldJObject obj = JObject.Parse(json);
                JObject obj = JObject.Parse(json);

                // Get the calculator type value
                string calculatorType = (string)obj["CalculatorType"];

                // Load the appropriate calculator
                if (calculatorType == "Rigid Diaphragm")
                {
                    var rigid_calc = JsonConvert.DeserializeObject<ShearWallCalculator_RigidDiaphragm>(json, _settings);
                    Calculator = new ShearWallCalculator_RigidDiaphragm(rigid_calc);
                }
                else if (calculatorType == "Flexible Diaphragm")
                {
                    var flex_calc = JsonConvert.DeserializeObject<ShearWallCalculator_FlexibleDiaphragm>(json, _settings);
                    Calculator = new ShearWallCalculator_FlexibleDiaphragm(flex_calc);
                }
                else
                {
                    throw new Exception("Invalid calculator type in MenuItem_Load_Click()");
                }

                Update();
                MessageBox.Show("Drawing loaded!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        #endregion

        #region File Handling
        private void OpenFile(string path)
        {
            // TODO: Replace with your file loading logic
            MessageBox.Show($"Opening file:\n{path}", "Open File", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AddToRecentFiles(string path)
        {
            var recent = Properties.Settings.Default.RecentFiles ?? new StringCollection();

            if (recent.Contains(path))
                recent.Remove(path); // Move to top

            recent.Insert(0, path);

            // Limit to 10 recent entries
            while (recent.Count > 10)
                recent.RemoveAt(recent.Count - 1);

            Properties.Settings.Default.RecentFiles = recent;
            Properties.Settings.Default.Save();

            LoadRecentFilesMenu(); // Refresh menu
        }

        private void LoadRecentFilesMenu()
        {
            RecentFilesMenu.Items.Clear();

            var recent = Properties.Settings.Default.RecentFiles ?? new StringCollection();
            bool cleaned = false;

            int index = 1;

            foreach (var file in recent.Cast<string>().ToList())
            {
                if (!File.Exists(file))
                {
                    recent.Remove(file);
                    cleaned = true;
                    continue;
                }

                string label = $"{index}. {System.IO.Path.GetFileName(file)}";

                var menu_item = new MenuItem
                {
                    Header = label,
                    ToolTip = file,
                    Tag = file
                };

                // try to show a small tooltip of the image
                try
                {
                    // Create image preview (larger size for tooltip)
                    var image = new System.Windows.Controls.Image
                    {
                        Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(file)),
                        Width = 150,  // Larger size
                        Height = 150,
                        Margin = new Thickness(0)
                    };

                    // Assign image preview as tooltip content
                    var tooltip = new ToolTip
                    {
                        Content = image
                    };

                    menu_item.ToolTip = tooltip;
                }
                catch
                {
                    // Skip preview if image load fails
                }

                menu_item.Click += (s, e) =>
                {
                    string filePath = (string)((MenuItem)s).Tag;
                    OpenFile(filePath);
                    AddToRecentFiles(filePath); // Move to top again

                    if(Calculator != null)
                    {
                        Calculator.selectedImageFilePath = filePath;
                    }
                };

                RecentFilesMenu.Items.Add(menu_item);
                index++;
            }

            if (cleaned)
            {
                Properties.Settings.Default.RecentFiles = recent;
                Properties.Settings.Default.Save();
            }

            if (RecentFilesMenu.Items.Count == 0)
            {
                RecentFilesMenu.Items.Add(new MenuItem
                {
                    Header = "No recent files",
                    IsEnabled = false
                });
            }
            else
            {
                RecentFilesMenu.Items.Add(new Separator());

                var clearItem = new MenuItem
                {
                    Header = "Clear Recent Files"
                };

                clearItem.Click += (s, e) => ClearRecentFiles();
                RecentFilesMenu.Items.Add(clearItem);
            }
        }

        /// <summary>
        /// 1. RecentFiles needs to be set in Properties > Settings.settings
        /// 2.  Add a setting Name: RecentFiles, Type: StringCollection, Scope: User, Default Value: <empty>
        /// </summary>
        private void ClearRecentFiles()
        {
            Properties.Settings.Default.RecentFiles = new StringCollection();
            Properties.Settings.Default.Save();
            LoadRecentFilesMenu();
        }


        private void OnMeasurementCompleted(object sender, ImageMeasurementEventArgs e)
        {
            if (Calculator != null)
            {

                // set the image parameters so that the DrawRerenceImage function has the items it needs to draw the true image.
                Calculator.selectedImageFilePath = e.FilePath;
                Calculator.pixelScaleX = e.ScaleFactor;
                Calculator.pixelScaleY = e.ScaleFactor;

                // Handle the measurement data here
                MessageBox.Show($"Measurement completed!\n" +
                                $"File: {e.FilePath}\n" +
                                $"Pixel Distance: {e.PixelDistance:F2}\n" +
                                $"Real-World Distance: {e.RealWorldDistance:F2}\n" +
                                $"Scale Factor: {e.ScaleFactor:F6}");

                Update();


                // You can update the MainWindow with the measurement result, if needed
                // For example, showing the scale factor or other data in the UI

                // TODO:  We now have the data from the measurement scaler window, but we need to know automatically load
                // to the window at the appropriate scale as we did before.

                // TODO:  Also, need to find a way to save this file and its scale factors to the recent files list.
            }
        }

        #endregion

        #region MODE setters
        public void SetButtonModes()
        {
            switch (currentMode)
            {
                case DrawMode.Line:
                    SetLineMode();
                    break;
                case DrawMode.Rectangle:
                    SetRectangleMode();
                    break;
                default:
                    return;
            }
        }

        private void SetLineMode()
        {
            ResetUIButtons();
            btnRigidityMode.BorderThickness = new Thickness(3);
            btnRigidityMode.BorderBrush = new SolidColorBrush(Colors.White);
            btnRigidityMode.Background = new SolidColorBrush(Colors.YellowGreen);

            currentMode = DrawMode.Line;  // Set to Line drawing mode
        }

        private void SetRectangleMode()
        {
            ResetUIButtons();
            btnMassMode.BorderThickness = new Thickness(3);
            btnMassMode.BorderBrush = new SolidColorBrush(Colors.White);
            btnMassMode.Background = new SolidColorBrush(Colors.YellowGreen);

            currentMode = DrawMode.Rectangle;  // Set to Rectangle drawing mode
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
            btnSnapToNearest.BorderThickness = new Thickness(3);
            btnSnapToNearest.BorderBrush = new SolidColorBrush(Colors.White);

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
            btnRigidityMode.Background = new SolidColorBrush(Colors.MediumBlue);
            btnMassMode.Background = new SolidColorBrush(Colors.Red);
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
    }
}
