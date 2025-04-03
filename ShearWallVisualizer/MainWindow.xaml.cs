using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ShearWallVisualizer
{

    public partial class MainWindow : Window
    {
        bool hide_Text = false;
        bool hide_Grid = false;

        private enum DrawMode { None, Line, Rectangle }
        private DrawMode currentMode = DrawMode.None;
        private bool snapMode = false;
        private double snapThreshold = 50; // Pixels
        private bool debugMode = false;

        private Point? startPoint_world = null;  // the first click in world coordinates
        private Point? endPoint_world = null;    // the second click in world coordinates
        private List<WorldShape> worldShapes = new List<WorldShape>();

        private double zoomFactorX = 2.0;
        private double zoomFactorY = 2.0;
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


 //           this.MouseLeftButtonDown += Canvas_MouseLeftButtonDown;
 //           this.MouseMove += Canvas_MouseMove;
            this.MouseWheel += Canvas_MouseWheel;
            this.MouseDown += Canvas_MouseDown;
            this.MouseUp += Canvas_MouseUp;

            myCanvas.SizeChanged += Canvas_SizeChanged; // Handle canvas size changes
            myCanvas.MouseRightButtonDown += Canvas_MouseRightButtonDown;  // Right-click to cancel drawing

            // Set focus and canvas background
            this.Focusable = true;
            this.Focus();
            myCanvas.Background = Brushes.White;

            //// Draw the grid immediately on startup
            //DrawGrid();
        }

        #region MODE setters
        private void SetLineMode()
        {
            currentMode = DrawMode.Line;  // Set to Line drawing mode
            MessageBox.Show("Line mode activated.");
            Console.WriteLine("Line mode activated.");
        }

        private void SetRectangleMode()
        {
            currentMode = DrawMode.Rectangle;  // Set to Rectangle drawing mode
            MessageBox.Show("Rectangle mode activated.");
            Console.WriteLine("Rectangle mode activated.");
        }

        private void SetDebugMode()
        {
            debugMode = !debugMode;
            MessageBox.Show($"Debug Mode {(debugMode ? "Enabled" : "Disabled")}");
            Console.WriteLine($"Debug Mode: {(debugMode ? "Enabled" : "Disabled")}");
        }
        private void SetSnapMode()
        {
            snapMode = !snapMode;
            MessageBox.Show($"Snap Mode {(snapMode ? "Enabled" : "Disabled")}");
            Console.WriteLine($"Snap Mode: {(snapMode ? "Enabled" : "Disabled")}");
        }
        #endregion

        #region Canvas Event Handlers

        private void Canvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Only cancel if currently in drawing mode (Line or Rectangle)
            if (currentMode == DrawMode.Line || currentMode == DrawMode.Rectangle)
            {
                // Clear any preview shape that may be drawn
                if (previewShape != null)
                {
                    myCanvas.Children.Remove(previewShape);
                    previewShape = null;
                }

                // Reset the start point for drawing
                startPoint_world = null;

                // Optionally, reset the current drawing mode if desired
                currentMode = DrawMode.None;  // Stop drawing mode

                // Log to indicate cancellation
                Console.WriteLine("Drawing canceled.");
            }
            else
            {
                // If not in drawing mode, don't perform any action
                Console.WriteLine("No drawing operation to cancel.");
            }
        }

        private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Redraw the grid when the canvas size changes
            DrawGrid();
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //if (currentMode == DrawMode.None)
            //{
            //    MessageBox.Show("No drawing mode selected.  Try selecting L (line mode) or R (rectangle) mode first.");
            //    return;  // Ignore if not in drawing mode
            //}

            //Point screenPoint = e.GetPosition(myCanvas);
            //Point worldPoint = ScreenToWorld(screenPoint, myCanvas);

            //if (snapMode)
            //{
            //    worldPoint = GetSnappedPoint(worldPoint);
            //}

            //if (startPoint_world == null)
            //{
            //    startPoint_world = worldPoint;
            //    CreatePreviewShape();
            //}
            //else
            //{
            //    FinalizeShape(worldPoint);
            //}
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            // update the current mouse position
            currentMouseScreenPosition = e.GetPosition(myCanvas);

            // Get new mouse position_screen
            if (isPanning)
            {
                Point newPanPoint = e.GetPosition(myCanvas);

                // Calculate the offset
                double offsetX = newPanPoint.X - lastPanPoint.X;
                double offsetY = newPanPoint.Y - lastPanPoint.Y;

                // Accumulate the pan offset
                panOffsetX -= offsetX;  // pan to the right "+x" is a negative offset
                panOffsetY += offsetY;

                // Store the current point for the next move
                lastPanPoint = newPanPoint;

                // Redraw grid and shapes with pan offsets
                DrawGrid();
                DrawShapes();
            }


            if (startPoint_world != null && previewShape != null)
            {
                Point screenPoint = e.GetPosition(myCanvas);
                Point worldPoint = ScreenToWorld(screenPoint, myCanvas);

                if (snapMode)
                {
                    worldPoint = GetSnappedPoint(worldPoint);
                }

                UpdatePreviewShape(worldPoint);
            }


            if (previewCoordinateLabel != null)
            {
                myCanvas.Children.Remove(previewCoordinateLabel);
            }

            // create the current mouse position_screen world coords on screen
            Point position = e.GetPosition(myCanvas);  // screen
            Point previewCoordPoint = new Point(position.X - panOffsetX, position.Y - panOffsetY);
            previewCoordinateLabel = CreateCoordinateLabel(ScreenToWorld(position, myCanvas));  // changed this one too
            Canvas.SetLeft(previewCoordinateLabel, previewCoordPoint.X + panOffsetX);
            Canvas.SetTop(previewCoordinateLabel, previewCoordPoint.Y + panOffsetY);
            myCanvas.Children.Add(previewCoordinateLabel);

            // Display current parameters
            tbPan.Text = "Pan: (" + panOffsetX.ToString("F2") + ", " + panOffsetY.ToString("F2") + ")";
            tbZoom.Text = "Zoom: (" + zoomFactorX.ToString("F2") + ", " + zoomFactorY.ToString("F2") + ")";
            tbWorldCoords.Text = "World Coords: (" + ScreenToWorld(e.GetPosition(myCanvas), myCanvas); // changed this one too
            tbScreenCoords.Text = "Screen Coords: (" + e.GetPosition(myCanvas).X + ", " + e.GetPosition(myCanvas).Y + ")"; // changed this one too
        }

        private void Canvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Determine the zoom factor
            double zoomFactor = (e.Delta > 0) ? 1.1 : 0.9;

            // Get the mouse position_screen relative to the canvas
            Point mousePosition = e.GetPosition(myCanvas);

            // Transform the mouse position_screen to world coordinates before zooming
            Point beforeZoom = ScreenToWorld(mousePosition, myCanvas);

            // Apply zoom
            zoomFactorX *= zoomFactor;
            zoomFactorY *= zoomFactor;

            // Transform the mouse position_screen to world coordinates after zooming
            Point afterZoom = ScreenToWorld(mousePosition, myCanvas);

            // Adjust pan offset to keep the mouse position_screen centered
            panOffsetX -= (afterZoom.X - beforeZoom.X);
            panOffsetY -= (afterZoom.Y - beforeZoom.Y);

            // Redraw grid and objects
            DrawGrid();
            DrawShapes();


            // Display current parameters on screen
            tbPan.Text = "Pan: (" + panOffsetX.ToString("F2") + ", " + panOffsetY.ToString("F2") + ")";
            tbZoom.Text = "Zoom: (" + zoomFactorX.ToString("F2") + ", " + zoomFactorY.ToString("F2") + ")";
            Point position_screen = e.GetPosition(myCanvas);
            tbWorldCoords.Text = "World Coords: (" + ScreenToWorld(position_screen, myCanvas);  // changed this one too
            tbScreenCoords.Text = "Screen Coords: (" + position_screen.X + ", " + position_screen.Y + ")"; // changed this one too
        }

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.MiddleButton == MouseButtonState.Pressed)
            {
                isPanning = true;
                lastPanPoint = e.GetPosition(myCanvas); // changed this one too
                myCanvas.Cursor = Cursors.Hand;
            }
        }

        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.MiddleButton == MouseButtonState.Released)
            {
                isPanning = false;
                myCanvas.Cursor = Cursors.Arrow;
            }
        }

        #endregion

        /// <summary>
        /// The primary routine to draw all the world shapes
        /// </summary>
        private void DrawShapes()
        {
            // Clear previous shapes
            myCanvas.Children.Clear();

            // Redraw grid (again, to keep it consistent with zoom/pan)
            DrawGrid();

            // Redraw all the shapes in world coordinates
            foreach (var shape in worldShapes)
            {
                Shape shapeToDraw = null;
                Ellipse centerPointMarker = null;
                TextBlock idLabel = null;

                if (shape is WorldLine line)
                {
                    Point p1_world = line.Start;
                    Point p2_world = line.End;
                    Point p1_screen = WorldToScreen(p1_world, myCanvas);
                    Point p2_screen = WorldToScreen(p2_world, myCanvas);

                    shapeToDraw = new Line
                    {
                        X1 = p1_screen.X,
                        Y1 = p1_screen.Y,
                        X2 = p2_screen.X,
                        Y2 = p2_screen.Y,
                        Stroke = Brushes.Blue,
                        StrokeThickness = 2
                    };

                    Point center_world = new Point((line.Start.X + line.End.X) / 2, (line.Start.Y + line.End.Y) / 2);
                    Point center_screen = WorldToScreen(center_world, myCanvas);

                    if (shapeToDraw != null)
                    {
                        centerPointMarker = CreateCircularMarker();
                        idLabel = CreateIdLabel(shape.Id);

                        // adjust positioning based on whether line is horizontal or vertical
                        if (line.Start.X == line.End.X)
                        {
                            // the center marker
                            Canvas.SetLeft(centerPointMarker, center_screen.X - centerPointMarker.Width / 2);
                            Canvas.SetTop(centerPointMarker, center_screen.Y - centerPointMarker.Height / 2);

                            // the id label
                            Canvas.SetLeft(idLabel, center_screen.X);
                            Canvas.SetTop(idLabel, center_screen.Y);
                        }
                        // line is horizontal
                        else
                        {
                            // the  center marker
                            Canvas.SetLeft(centerPointMarker, center_screen.X - centerPointMarker.Width / 2);
                            Canvas.SetTop(centerPointMarker, center_screen.Y - centerPointMarker.Height / 2);

                            // the id label
                            Canvas.SetLeft(idLabel, center_screen.X);
                            Canvas.SetTop(idLabel, center_screen.Y);

                        }

                        myCanvas.Children.Add(shapeToDraw);
                        myCanvas.Children.Add(centerPointMarker);
                        myCanvas.Children.Add(idLabel);


                        if (debugMode is true)
                        {
                            // draw markers and coordinate data at end points
                            Ellipse startMarker = CreateCircularMarker();
                            Canvas.SetLeft(startMarker, p1_screen.X);
                            Canvas.SetTop(startMarker, p1_screen.Y);
                            myCanvas.Children.Add(startMarker);

                            Ellipse endMarker = CreateCircularMarker();
                            Canvas.SetLeft(endMarker, p2_screen.X);
                            Canvas.SetTop(endMarker, p2_screen.Y);
                            myCanvas.Children.Add(endMarker);

                            TextBlock startCoord = CreateCoordinateLabel(line.Start);
                            Canvas.SetLeft(startCoord, p1_screen.X + 3);
                            Canvas.SetTop(startCoord, p1_screen.Y + 3);
                            myCanvas.Children.Add(startCoord);

                            TextBlock endCoord = CreateCoordinateLabel(line.End);
                            Canvas.SetLeft(endCoord, p2_screen.X + 3);
                            Canvas.SetTop(endCoord, p2_screen.Y + 3);
                            myCanvas.Children.Add(endCoord);
                        }
                    }
                }
                else if (shape is WorldRectangle rect)
                {
                    Point p1_world = rect.BottomLeft;
                    Point p2_world = rect.TopRight;
                    Point p1_screen = WorldToScreen(p1_world, myCanvas);
                    Point p2_screen = WorldToScreen(p2_world, myCanvas);

                    shapeToDraw = new Rectangle
                    {
                        Width = Math.Abs(p2_screen.X - p1_screen.X),
                        Height = Math.Abs(p2_screen.Y - p1_screen.Y),
                        Stroke = Brushes.Black,
                        StrokeThickness = 2,
                        Fill = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0)) // Red with 50% opacity
                    };

                    Canvas.SetLeft(shapeToDraw, Math.Min(p1_screen.X, p2_screen.X));
                    Canvas.SetTop(shapeToDraw, Math.Min(p1_screen.Y, p2_screen.Y));

                    Point center_world = new Point((p1_world.X + p2_world.X) / 2, (p1_world.Y + p2_world.Y) / 2);
                    Point center_screen = WorldToScreen(center_world, myCanvas);

                    if (shapeToDraw != null)
                    {
                        myCanvas.Children.Add(shapeToDraw);

                        centerPointMarker = CreateCircularMarker();

                        // the  center marker
                        Canvas.SetLeft(centerPointMarker, center_screen.X - centerPointMarker.Width / 2);
                        Canvas.SetTop(centerPointMarker, center_screen.Y - centerPointMarker.Height / 2);
                        myCanvas.Children.Add(centerPointMarker);

                        // the id label
                        idLabel = CreateIdLabel(shape.Id);
                        Canvas.SetLeft(idLabel, center_screen.X);
                        Canvas.SetTop(idLabel, center_screen.Y);
                        myCanvas.Children.Add(idLabel);

                        if (debugMode is true)
                        {
                            // draw markers and coordinate date at end points
                            Ellipse startMarker = CreateCircularMarker();
                            Canvas.SetLeft(startMarker, p1_screen.X - startMarker.Width / 2);
                            Canvas.SetTop(startMarker, p1_screen.Y - startMarker.Height / 2);
                            myCanvas.Children.Add(startMarker);

                            Ellipse endMarker = CreateCircularMarker();
                            Canvas.SetLeft(endMarker, p2_screen.X - endMarker.Width / 2);
                            Canvas.SetTop(endMarker, p2_screen.Y - endMarker.Height / 2);
                            myCanvas.Children.Add(endMarker);

                            TextBlock startCoord = CreateCoordinateLabel(rect.BottomLeft);
                            Canvas.SetLeft(startCoord, p1_screen.X + 3);
                            Canvas.SetTop(startCoord, p1_screen.Y + 3);
                            myCanvas.Children.Add(startCoord);

                            TextBlock endCoord = CreateCoordinateLabel(rect.TopRight);
                            Canvas.SetLeft(endCoord, p2_screen.X + 3);
                            Canvas.SetTop(endCoord, p2_screen.Y + 3);
                            myCanvas.Children.Add(endCoord);

                        }
                    }
                }
            }
        }

        //private void DrawCursor()
        //{
        //    if ((snapMode is true))
        //    {
        //        // draw markers and coordinate data at end points
        //        Ellipse cursor = CreateCircularMarker(10);
        //        cursor.Stroke = Brushes.Red;
        //        cursor.StrokeThickness = 2;
        //        cursor.Fill = Brushes.Transparent;
        //        cursor.IsHitTestVisible = false;
        //        Canvas.SetLeft(cursor, currentMouseScreenPosition.X - cursor.Width / 2);
        //        Canvas.SetTop(cursor, currentMouseScreenPosition.Y - cursor.Height / 2);
        //        myCanvas.Children.Add(cursor);
        //    }
        //}

        private void DrawGrid()
        {
            myCanvas.Children.Clear(); // Clear previous gridlines

            double majorGridSpacing = 5.0;  // Major grid lines every 5 world units
            double minorGridSpacing = 2.0;  // Minor grid lines every 2 world units

            double canvasWidth = myCanvas.ActualWidth;
            double canvasHeight = myCanvas.ActualHeight;

            // Draw vertical grid lines (major and minor) in world coordinates
            for (double x = 0; x <= worldWidth; x += minorGridSpacing)
            {
                Point p1 = WorldToScreen(new Point(x, 0), myCanvas);
                Point p2 = WorldToScreen(new Point(x, canvasHeight), myCanvas);
                Line gridLine = new Line
                {
                    X1 = p1.X,
                    Y1 = p1.Y,
                    X2 = p2.X,
                    Y2 = p2.Y,
                    Stroke = (x % majorGridSpacing == 0) ? Brushes.Black : Brushes.Gray,
                    StrokeThickness = (x % majorGridSpacing == 0) ? 1.0 : 0.75,
                    StrokeDashArray = (x % majorGridSpacing == 0) ? new DoubleCollection { 3, 3 } : new DoubleCollection { 1, 1 },
                    Opacity = 0.5f,
                    IsHitTestVisible = false
                };
                myCanvas.Children.Add(gridLine);
            }

            // Draw horizontal grid lines (major and minor)
            for (double y = 0; y <= worldHeight; y += minorGridSpacing)
            {
                Point p1 = WorldToScreen(new Point(0, y), myCanvas);
                Point p2 = WorldToScreen(new Point(canvasWidth, y), myCanvas);

                Line gridLine = new Line
                {
                    X1 = p1.X,
                    Y1 = p1.Y,
                    X2 = p2.X,
                    Y2 = p2.Y,
                    Stroke = (y % majorGridSpacing == 0) ? Brushes.Black : Brushes.Gray,
                    StrokeThickness = (y % majorGridSpacing == 0) ? 1.0 : 0.75,
                    StrokeDashArray = (y % majorGridSpacing == 0) ? new DoubleCollection { 3, 3 } : new DoubleCollection { 1, 1 },
                    Opacity = 0.5f,
                    IsHitTestVisible = false
                };
                myCanvas.Children.Add(gridLine);
            }

            // Draw the origin marker at world (0, 0)
            DrawWorldOriginMarker();
        }




        private void ResetInputMode()
        {
            // cancel the input by setting the start point to null
            startPoint_world = null;
            previewShape = null;
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            ResetInputMode();

            Console.WriteLine($"Key Pressed: {e.Key}");

            if (e.Key == Key.L)
            {
                SetLineMode();
            }
            else if (e.Key == Key.R)
            {
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

            Draw(ChangeType.Redraw);
        }






        private void CreatePreviewShape()
        {
            if (currentMode == DrawMode.Line)
                previewShape = new Line { Stroke = Brushes.DarkGreen, StrokeThickness = 2 };
            else if (currentMode == DrawMode.Rectangle)
                previewShape = new Rectangle { Stroke = Brushes.DarkGreen, StrokeThickness = 2, Fill = Brushes.Transparent };

            myCanvas.Children.Add(previewShape);

            // Add a TextBlock to display the length/width
            previewLengthLabel = new TextBlock
            {
                Foreground = Brushes.Black,
                FontSize = 12,
                FontWeight = FontWeights.Bold
            };
            myCanvas.Children.Add(previewLengthLabel);
        }

        //private void UpdatePreviewShape(Point worldPoint)
        //{
        //    if (previewShape is Line line)
        //    {
        //        // force the wall line to be horizontal or vertical only
        //        worldPoint = GetConstrainedPoint(worldPoint, startPoint_world.Value);

        //        Point p1_world = startPoint_world.Value;
        //        Point p2_world = worldPoint;
        //        Point p1_screen = WorldToScreen(p1_world, myCanvas);
        //        Point p2_screen = WorldToScreen(p2_world, myCanvas);

        //        line.X1 = p1_screen.X;
        //        line.Y1 = p1_screen.Y;
        //        line.X2 = p2_screen.X;
        //        line.Y2 = p2_screen.Y;

        //        // Calculate the length of the line
        //        double world_length = Math.Sqrt(Math.Pow(worldPoint.X - startPoint_world.Value.X, 2) + Math.Pow(worldPoint.Y - startPoint_world.Value.Y, 2));

        //        previewLengthLabel.Text = $"Length: {world_length:F2}";  // Display length with 2 decimal places

        //        // Position the label in the middle of the line
        //        double labelX = (line.X1 + line.X2) / 2;
        //        double labelY = (line.Y1 + line.Y2) / 2;
        //        Canvas.SetLeft(previewLengthLabel, labelX + 5);
        //        Canvas.SetTop(previewLengthLabel, labelY - 20);
        //    }
        //    else if (previewShape is Rectangle rect)
        //    {
        //        Point p1_world = startPoint_world.Value;
        //        Point p2_world = worldPoint;
        //        Point p1_screen = WorldToScreen(p1_world, myCanvas);
        //        Point p2_screen = WorldToScreen(p2_world, myCanvas);

        //        double x_min = Math.Min(p1_screen.X, p2_screen.X);
        //        double y_min = Math.Min(p1_screen.Y, p2_screen.Y);
        //        rect.Width = Math.Abs(p2_screen.X - p1_screen.X);
        //        rect.Height = Math.Abs(p2_screen.Y - p1_screen.Y);
        //        Canvas.SetLeft(rect, x_min);
        //        Canvas.SetTop(rect, y_min);

        //        // Calculate the width and height of the rectangle in world coordinates
        //        double world_width = Math.Abs(p2_world.X - p1_world.X);
        //        double world_height = Math.Abs(p2_world.Y - p1_world.Y);

        //        // Display width and height with 2 decimal places
        //        previewLengthLabel.Text = $"Width: {world_width:F2}, Height: {world_height:F2}";

        //        // Position the label in the center_world of the rectangle
        //        double labelX = x_min + rect.Width / 2;
        //        double labelY = y_min + rect.Height / 2;
        //        Canvas.SetLeft(previewLengthLabel, labelX + 5);
        //        Canvas.SetTop(previewLengthLabel, labelY - 20);
        //    }
        //}
        //private void FinalizeShape(Point worldPoint)
        //{
        //    int newId = GetNextAvailableID();  // Increment and use as unique ID for each shape

        //    if (currentMode == DrawMode.Line)
        //    {
        //        // For the line to be horizontal or vertical only
        //        worldPoint = GetConstrainedPoint(worldPoint, startPoint_world.Value); // Ensure alignment

        //        // Create the line in world space and store it in the worldShapes list
        //        WorldLine worldLine = new WorldLine(newId, startPoint_world.Value, worldPoint);
        //        worldShapes.Add(worldLine);
        //    }
        //    else if (currentMode == DrawMode.Rectangle)
        //    {
        //        WorldRectangle worldRect = new WorldRectangle(newId, startPoint_world.Value, worldPoint);
        //        worldShapes.Add(worldRect);
        //    }

        //    DrawShapes(); // Redraw the shapes on the canvas

        //    // Clear the preview shape from the screen.
        //    myCanvas.Children.Remove(previewShape);
        //    previewShape = null;
        //    startPoint_world = null;

        //    // Remove the preview dimension label after finalizing the shape
        //    if (previewLengthLabel != null)
        //    {
        //        myCanvas.Children.Remove(previewLengthLabel);
        //        previewLengthLabel = null;
        //    }
        //}

        private int GetNextAvailableID()
        {
            int shapeCount = 0;

            while (true)
            {
                bool idExists = false;

                // loop throgh all the existing shapes
                foreach (var shape in worldShapes)
                {
                    if (shape.Id == shapeCount)
                    {
                        idExists = true;
                    }
                }

                // no match found
                if (idExists is false)
                {
                    return shapeCount;
                }
                else
                {
                    shapeCount++;
                }
            }
        }

        private Point GetSnappedPoint(Point worldPoint)
        {
            foreach (var shape in worldShapes)
            {
                if (shape is WorldLine line)
                {
                    if (IsWithinSnapThreshold(worldPoint, line.Start)) return line.Start;
                    if (IsWithinSnapThreshold(worldPoint, line.End)) return line.End;
                }
                else if (shape is WorldRectangle rect)
                {
                    foreach (var corner in rect.GetCorners())
                    {
                        if (IsWithinSnapThreshold(worldPoint, corner)) return corner;
                    }
                }
            }
            return worldPoint;
        }

        private bool IsWithinSnapThreshold(Point p1, Point p2)
        {
            double dx = p1.X - p2.X;
            double dy = p1.Y - p2.Y;
            return Math.Sqrt(dx * dx + dy * dy) <= snapThreshold * (worldWidth / myCanvas.ActualWidth);
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

        private Ellipse CreateCircularMarker(double dia = 6)
        {
            Ellipse ellipse = new Ellipse
            {
                Width = dia,
                Height = dia,
                Fill = Brushes.Black
            };

            return ellipse;
        }

        private TextBlock CreateCoordinateLabel(Point worldPoint)
        {
            TextBlock label = new TextBlock
            {
                Text = $"X: {(worldPoint.X).ToString("F2")}, Y: {(worldPoint.Y).ToString("F2")}",
                Foreground = Brushes.Black,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                IsHitTestVisible = false
            };
            return label;
        }

        private TextBlock CreateIdLabel(int id)
        {
            TextBlock label = new TextBlock
            {
                Text = $"ID: {id}",
                Foreground = Brushes.Black,
                FontSize = 12,
                FontWeight = FontWeights.Bold
            };

            return label;
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
        /// Draws a marker at the specifid world coordinates (x,y) on the screen
        /// </summary>
        /// <param name="world_x"></param>
        /// <param name="world_y"></param>
        private void DrawWorldOriginMarker(double world_x = 0, double world_y = 0)
        {
            // Define the marker's size and appearance
            double markerSize = 8;
            Ellipse originMarker = new Ellipse
            {
                Width = markerSize,
                Height = markerSize,
                Fill = Brushes.Red,  // Red color for visibility
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };

            // Convert world coordinates (0, 0) to screen coordinates
            Point origin_screen = WorldToScreen(new Point(world_x, world_y), myCanvas);

            // Position the marker at the origin on the canvas
            Canvas.SetLeft(originMarker, origin_screen.X - markerSize / 2);
            Canvas.SetTop(originMarker, origin_screen.Y - markerSize / 2);

            // Add the origin marker to the canvas
            myCanvas.Children.Add(originMarker);
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
            if(hide_Grid is true)
            {
                return;
            }

            // thickness of the minor gridlines
            Pen pen; 

            double majorGridSpacing = 10.0;  // Major grid lines every 5 world units
            double minorGridSpacing = 2.0;  // Minor grid lines every 2 world units

            double layerWidth = dockpanel.Width;
            double layerHeight = dockpanel.Height;

            // Draw vertical grid lines (major and minor) in world coordinates
            for (double x = 0; x <= worldWidth; x += minorGridSpacing)
            {
                // If its a major grid line, make it thicker
                if(x % majorGridSpacing == 0)
                {
                    pen = new Pen(Brushes.Black, 1.0);
                    pen.Thickness = 1;
                    pen.DashStyle = new DashStyle(new double[] { 5, 5 }, 0);
                } else
                {
                    pen = new Pen(Brushes.Black, 0.3);
                    pen.Thickness = 0.5;
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
            for (double y = 0; y <= worldHeight; y += minorGridSpacing)
            {
                // If its a major grid line, make it thicker
                if (y % majorGridSpacing == 0)
                {
                    pen = new Pen(Brushes.Black, 1.0);
                    pen.Thickness = 1;
                    pen.DashStyle = new DashStyle(new double[] { 5, 5 }, 0);
                }
                else
                {
                    pen = new Pen(Brushes.Black, 0.3);
                    pen.Thickness = 0.5;
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
                if(p1.X < 0)
                {
                    p1.X = 0;
                }
                if(p1.X > layerWidth)
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
            } else
            {
                ctx.DrawLine(new Pen(Brushes.Black, 1), cross_pt, new Point(0, cross_pt.Y));
                ctx.DrawLine(new Pen(Brushes.Black, 1), cross_pt, new Point(dockpanel.Width, cross_pt.Y));
                ctx.DrawLine(new Pen(Brushes.Black, 1), cross_pt, new Point(cross_pt.X, 0));
                ctx.DrawLine(new Pen(Brushes.Black, 1), cross_pt, new Point(cross_pt.X, dockpanel.Height));
            }
        }

        private void DrawPreview(DrawingContext ctx)
        {
            if(previewShape == null)
            {
                return;
            }

            if(previewShape is Line line)
            {
                SolidColorBrush lineStrokeBrush = new SolidColorBrush(Colors.Green);
                lineStrokeBrush.Opacity = 0.5;
                Pen pen = new Pen(lineStrokeBrush, 1);
                Point p1 = WorldToScreen(startPoint_world.Value, m_layers);
                Point p2 = WorldToScreen(endPoint_world.Value, m_layers);
                ctx.DrawLine(pen, p1, p2);
            } else if (previewShape is Rectangle rect)
            {
                SolidColorBrush rectFillBrush = new SolidColorBrush(Colors.Red);
                rectFillBrush.Opacity = 0.5;

                // find lower left corner point of rectangle bounded by startPoint_world and endPoint_world
                Point insertPoint_world = new Point(Math.Min(startPoint_world.Value.X, endPoint_world.Value.X), Math.Min(startPoint_world.Value.Y, endPoint_world.Value.Y));
                Point insertPoint_screen = WorldToScreen(insertPoint_world, m_layers);
                double width = Math.Abs(startPoint_world.Value.X - endPoint_world.Value.X);
                double height = Math.Abs(startPoint_world.Value.Y - endPoint_world.Value.Y);
                ctx.DrawRectangle(rectFillBrush, new Pen(Brushes.Black, 1), new Rect(insertPoint_screen.X, insertPoint_world.Y, width, height));
            }
        }



        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);

            m_scroll.Minimum = 0;
            m_scroll.Maximum = m_layers.ActualHeight - 70;
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
            m_log.Text = text + "\r\n" + m_log.Text;

            if (m_log.Text.Length > 1000)
            {
                m_log.Text = m_log.Text.Substring(0, 1000);
            }
        }


        private void UpdatePreviewShape(Point worldPoint)
        {
            if (previewShape is Line line)
            {
                // force the wall line to be horizontal or vertical only
                worldPoint = GetConstrainedPoint(worldPoint, startPoint_world.Value);

                Point p1_world = startPoint_world.Value;
                Point p2_world = worldPoint;
                Point p1_screen = WorldToScreen(p1_world, myCanvas);
                Point p2_screen = WorldToScreen(p2_world, myCanvas);

                line.X1 = p1_screen.X;
                line.Y1 = p1_screen.Y;
                line.X2 = p2_screen.X;
                line.Y2 = p2_screen.Y;

                // Calculate the length of the line
                double world_length = Math.Sqrt(Math.Pow(worldPoint.X - startPoint_world.Value.X, 2) + Math.Pow(worldPoint.Y - startPoint_world.Value.Y, 2));

                previewLengthLabel.Text = $"Length: {world_length:F2}";  // Display length with 2 decimal places

                // Position the label in the middle of the line
                double labelX = (line.X1 + line.X2) / 2;
                double labelY = (line.Y1 + line.Y2) / 2;
                Canvas.SetLeft(previewLengthLabel, labelX + 5);
                Canvas.SetTop(previewLengthLabel, labelY - 20);
            }
            else if (previewShape is Rectangle rect)
            {
                Point p1_world = startPoint_world.Value;
                Point p2_world = worldPoint;
                Point p1_screen = WorldToScreen(p1_world, myCanvas);
                Point p2_screen = WorldToScreen(p2_world, myCanvas);

                double x_min = Math.Min(p1_screen.X, p2_screen.X);
                double y_min = Math.Min(p1_screen.Y, p2_screen.Y);
                rect.Width = Math.Abs(p2_screen.X - p1_screen.X);
                rect.Height = Math.Abs(p2_screen.Y - p1_screen.Y);
                Canvas.SetLeft(rect, x_min);
                Canvas.SetTop(rect, y_min);

                // Calculate the width and height of the rectangle in world coordinates
                double world_width = Math.Abs(p2_world.X - p1_world.X);
                double world_height = Math.Abs(p2_world.Y - p1_world.Y);

                // Display width and height with 2 decimal places
                previewLengthLabel.Text = $"Width: {world_width:F2}, Height: {world_height:F2}";

                // Position the label in the center_world of the rectangle
                double labelX = x_min + rect.Width / 2;
                double labelY = y_min + rect.Height / 2;
                Canvas.SetLeft(previewLengthLabel, labelX + 5);
                Canvas.SetTop(previewLengthLabel, labelY - 20);
            }
        }
        private void FinalizeShape(Point worldPoint)
        {
            int newId = GetNextAvailableID();  // Increment and use as unique ID for each shape

            if (currentMode == DrawMode.Line)
            {
                // For the line to be horizontal or vertical only
                worldPoint = GetConstrainedPoint(worldPoint, startPoint_world.Value); // Ensure alignment

                // Create the line in world space and store it in the worldShapes list
                WorldLine worldLine = new WorldLine(newId, startPoint_world.Value, worldPoint);
                worldShapes.Add(worldLine);
            }
            else if (currentMode == DrawMode.Rectangle)
            {
                WorldRectangle worldRect = new WorldRectangle(newId, startPoint_world.Value, worldPoint);
                worldShapes.Add(worldRect);
            }

            DrawShapes(); // Redraw the shapes on the canvas

            // Clear the preview shape from the screen.
            myCanvas.Children.Remove(previewShape);
            previewShape = null;
            startPoint_world = null;

            // Remove the preview dimension label after finalizing the shape
            if (previewLengthLabel != null)
            {
                myCanvas.Children.Remove(previewLengthLabel);
                previewLengthLabel = null;
            }

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
            m_layers.AddLayer(52, DrawCursor);
            m_layers.AddLayer(51, DrawPreview);



            Draw(ChangeType.Redraw);
        }

        #endregion

        private void m_layers_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show("Drawing Visual Clicked at " + e.GetPosition(myCanvas).ToString());


            if (currentMode == DrawMode.None)
            {
                MessageBox.Show("No drawing mode selected.  Try selecting L (line mode) or R (rectangle) mode first.");
                return;  // Ignore if not in drawing mode
            }

            Point screenPoint = e.GetPosition(myCanvas);
            Point worldPoint = ScreenToWorld(screenPoint, myCanvas);

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
                FinalizeShape(worldPoint);
            }
        }



        private void m_layers_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            currentMouseScreenPosition = e.GetPosition(m_layers);

            tbScreenCoords.Text = e.GetPosition(m_layers).ToString();

            Draw(ChangeType.Redraw);
        }

        private void m_layers_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Determine the zoom factor
            double zoomFactor = (e.Delta > 0) ? 1.1 : 0.9;

            // Get the mouse position_screen relative to the canvas
            Point mousePosition = e.GetPosition(m_layers);

            // Transform the mouse position_screen to world coordinates before zooming
            Point beforeZoom = ScreenToWorld(mousePosition, m_layers);

            // Apply zoom
            zoomFactorX *= zoomFactor;
            zoomFactorY *= zoomFactor;

            // Transform the mouse position_screen to world coordinates after zooming
            Point afterZoom = ScreenToWorld(mousePosition, m_layers);

            // Adjust pan offset to keep the mouse position_screen centered
            panOffsetX -= (afterZoom.X - beforeZoom.X);
            panOffsetY -= (afterZoom.Y - beforeZoom.Y);

            // Display current parameters on screen
            tbPan.Text = "Pan: (" + panOffsetX.ToString("F2") + ", " + panOffsetY.ToString("F2") + ")";
            tbZoom.Text = "Zoom: (" + zoomFactorX.ToString("F2") + ", " + zoomFactorY.ToString("F2") + ")";
            tbWorldCoords.Text = "World Coords: (" + afterZoom.X.ToString("F2") + ", " + afterZoom.Y.ToString("F2") + ")";  // changed this one too
            tbScreenCoords.Text = "Screen Coords: (" + mousePosition.X.ToString("F2") + ", " + mousePosition.Y.ToString("F2") + ")"; // changed this one too

            Draw(ChangeType.Redraw);
        }

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

    }


    public class WorldShape
    {
        public int Id { get; }
        public WorldShape(int id)
        {
            Id = id;
        }
    }

    public class WorldLine : WorldShape
    {
        public Point Start { get; }
        public Point End { get; }

        public WorldLine(int id, Point start, Point end) : base(id)
        {
            Start = start;
            End = end;
        }

        public Line ToScreenLine(double scale, Point worldOrigin)
        {
            var startScreen = WorldToScreen(Start, scale, worldOrigin);
            var endScreen = WorldToScreen(End, scale, worldOrigin);

            var line = new Line
            {
                X1 = startScreen.X,
                Y1 = startScreen.Y,
                X2 = endScreen.X,
                Y2 = endScreen.Y,
                Stroke = Brushes.Black
            };

            return line;
        }

        private Point WorldToScreen(Point worldPoint, double scale, Point worldOrigin)
        {
            var screenX = (worldPoint.X - worldOrigin.X) * scale;
            var screenY = (worldPoint.Y - worldOrigin.Y) * scale;
            return new Point(screenX, screenY);
        }
    }

    public class WorldRectangle : WorldShape
    {
        public Point BottomLeft { get; }
        public Point TopRight { get; }

        public WorldRectangle(int id, Point bottomLeft, Point topRight) : base(id)
        {
            BottomLeft = bottomLeft;
            TopRight = topRight;
        }

        // Method to return the rectangle corners for snapping
        public IEnumerable<Point> GetCorners()
        {
            yield return BottomLeft;
            yield return new Point(BottomLeft.X, TopRight.Y);  // Top Left
            yield return new Point(TopRight.X, BottomLeft.Y);  // Bottom Right
            yield return TopRight;
        }

        public List<Rect> ToScreenRectangles(double scale, Point worldOrigin)
        {
            List<Rect> rectangles = new List<Rect>();
            var corners = GetCorners().ToList();

            // Transform all corners to screen space
            var bottomLeftScreen = WorldToScreen(corners[0], scale, worldOrigin);
            var topRightScreen = WorldToScreen(corners[3], scale, worldOrigin);

            var rect = new Rect(bottomLeftScreen.X, bottomLeftScreen.Y,
                                topRightScreen.X - bottomLeftScreen.X,
                                topRightScreen.Y - bottomLeftScreen.Y);
            rectangles.Add(rect);

            return rectangles;
        }

        private Point WorldToScreen(Point worldPoint, double scale, Point worldOrigin)
        {
            var screenX = (worldPoint.X - worldOrigin.X) * scale;
            var screenY = (worldPoint.Y - worldOrigin.Y) * scale;
            return new Point(screenX, screenY);
        }
    }
}




        //// create our Calculator object 
        //// TODO: This will need to be switchable between RigidDiaphragm and FlexibileDiaphragm or both
        //Calculator = new ShearWallCalculator_RigidDiaphragm();

        //// setup the canvas
        //SetCanvasDimensions(DEFAULT_CANVAS_WIDTH, DEFAULT_CANVAS_HEIGHT);

        //// set scroll viewer dimensions to intially be slight larger than the canvas
        //CanvasScrollViewer.Width = DEFAULT_CANVAS_WIDTH + 50.0f;
        //CanvasScrollViewer.Height = DEFAULT_CANVAS_HEIGHT + 50.0f;

        //// setup the initial canvas scaling
        //SetupViewScaling(1.0f, 1.0f); // scale it the first time to 1:1
        //if (cnvMainCanvas.Width / DEFAULT_MODEL_EXITENTS_HORIZONTAL != SCALE_X || cnvMainCanvas.Height / DEFAULT_MODEL_EXITENTS_VERTICAL != SCALE_Y)
        //{
        //    SCALE_X = cnvMainCanvas.Width / DEFAULT_MODEL_EXITENTS_HORIZONTAL;
        //    SCALE_Y = cnvMainCanvas.Height / DEFAULT_MODEL_EXITENTS_VERTICAL;
        //    SetupViewScaling((float)SCALE_X, (float)SCALE_Y); // apply the view scaling
        //}

        //// Setup the canvas drawer helper
        //_canvas_drawer = new CanvasDrawer(cnvMainCanvas, SCALE_X, SCALE_Y);

        //// create the gridlines
        //CreateGridLines();

        //Update();

    //    private void SetCanvasDimensions(float width, float height)
    //    {
    //        _canvas_width = width;
    //        _canvas_height = height;

    //        // set the size on the screen
    //        cnvMainCanvas.Width = width;
    //        cnvMainCanvas.Height = height;
    //    }

    //    private void SetupViewScaling(float scale_x, float scale_y, float trans_x = 0, float trans_y = 0)
    //    {
    //        // set up scaling parameters associated with the canvas
    //        _scaleTransform = new ScaleTransform(scale_x, scale_y);
    //        _translateTransform = new TranslateTransform(-trans_x, -trans_y);
    //        _transformGroup = new TransformGroup();
    //        _transformGroup.Children.Add(_scaleTransform);
    //        _transformGroup.Children.Add(_translateTransform);
    //        cnvMainCanvas.RenderTransform = _transformGroup;
    //    }

    //    /// <summary>
    //    /// Function to update the UI -- call this when the calculator or UI needs to be updated
    //    /// </summary>
    //    private void Update()
    //    {
    //        // Update the new calculator
    //        Calculator.Update();

    //        DrawCanvas();
    //    }

    //    /// <summary>
    //    /// The primary draw functions for items on the canvas and the summary controls
    //    /// </summary>
    //    /// <exception cref="Exception"></exception>
    //    /// <exception cref="NotImplementedException"></exception>
    //    public void DrawCanvas()
    //    {
    //        // No calculator?  Nothing to draw so return
    //        if (Calculator == null)
    //        {
    //            return;
    //        }

    //        //// Clear the MainCanvas before redrawing.
    //        cnvMainCanvas.Children.Clear();

    //        // Draw the gridlines and other non-model objects on the canvas
    //        _canvas_drawer.DrawCanvasDetails(CanvasDetailsObjects);

    //        // draw the structural model objects for the diaphragms -- do this before any added details and highlights
    //        foreach (var diaphragm in Calculator._diaphragm_system._diaphragms)
    //        {
    //            _canvas_drawer.DrawDiaphragm(diaphragm.Value);
    //        }

    //        if (Calculator._diaphragm_system != null && Calculator._diaphragm_system._diaphragms.Count > 0)
    //        {
    //            // Draw extra information for diaphragms -- label numbers etc.
    //            _canvas_drawer.DrawDiaphragmsInfo(Calculator._diaphragm_system);
    //        }

    //        // draw the structural model objects for the walls -- do this before any added details and highlights
    //        foreach (var wall in Calculator._wall_system._walls)
    //        {
    //            _canvas_drawer.DrawWall(wall.Value);
    //        }

    //        // draw extra information for walls -- label numbers etc.
    //        if (Calculator._wall_system != null && Calculator._wall_system._walls.Count > 0)
    //        {
    //            _canvas_drawer.DrawWallsInfo(Calculator._wall_system);
    //        }

    //        //// draw the bounding box around all structural model objects
    //        //_canvas_drawer.DrawBoundingBox(Calculator.Boundary_Min_Point, Calculator.Boundary_Max_Point);

    //        // Draw the preview object (line or rectangle)
    //        if (_currentPreviewLine != null)
    //        {
    //            DrawPreviewShape();
    //        }

    //        // Update text boxes and display info
    //        CenterOfMass.Content = "(" + Calculator._diaphragm_system.CtrMass.X.ToString("0.00") + ", " + Calculator._diaphragm_system.CtrMass.Y.ToString("0.00") + ")";
    //        CenterOfRigidity.Content = "(" + Calculator._wall_system.CtrRigidity.X.ToString("0.00") + ", " + Calculator._wall_system.CtrRigidity.Y.ToString("0.00") + ")";

    //        // Draw center_world of mass and center_world of rigidity markers
    //        _canvas_drawer.DrawCOM(Calculator._diaphragm_system);
    //        _canvas_drawer.DrawCOR(Calculator._wall_system);

    //        // Draw crosshairs
    //        _canvas_drawer.DrawCrosshairs(
    //            new Point(
    //                Mouse.GetPosition(cnvMainCanvas).X,
    //                Mouse.GetPosition(cnvMainCanvas).Y
    //            ),
    //            _crosshair_color
    //            );

    //        // Draw the braced wall line data.
    //        _canvas_drawer.DrawBracedWallLines(Calculator._wall_system);

    //        // If snapping mode is enabled, draw the extra markers
    //        if (_shouldSnapToNearest is true)
    //        {
    //            Point crosshair_intersection = new Point(Mouse.GetPosition(cnvMainCanvas).X, Mouse.GetPosition(cnvMainCanvas).Y);

    //            // add marker at cross hair intersection point
    //            // marker for center_world of the rectangle -- center_world of area / mass
    //            Ellipse snapCircle = new Ellipse
    //            {
    //                Width = _snapDistance,
    //                Height = _snapDistance,
    //                StrokeThickness = 1.5f,
    //                Stroke = Brushes.Red,
    //                Fill = Brushes.Transparent,
    //                IsHitTestVisible = false
    //            };
    //            Canvas.SetLeft(snapCircle, crosshair_intersection.X - snapCircle.Width / 2.0f);
    //            Canvas.SetTop(snapCircle, crosshair_intersection.Y - snapCircle.Height / 2.0f);
    //            cnvMainCanvas.Children.Add(snapCircle);

    //            // Draw the markers for the diaphragm corners and wall end points
    //            foreach (DiaphragmData_Rectangular d in Calculator._diaphragm_system._diaphragms.Values)
    //            {
    //                Point p1_screen = MathHelpers.WorldCoord_ToScreen(cnvMainCanvas.Height, d.P1, SCALE_X, SCALE_Y);
    //                Point p2_screen = MathHelpers.WorldCoord_ToScreen(cnvMainCanvas.Height, d.P2, SCALE_X, SCALE_Y);
    //                Point p3 = MathHelpers.WorldCoord_ToScreen(cnvMainCanvas.Height, d.P3, SCALE_X, SCALE_Y);
    //                Point p4 = MathHelpers.WorldCoord_ToScreen(cnvMainCanvas.Height, d.P4, SCALE_X, SCALE_Y);

    //                _canvas_drawer.DrawCircles(p1_screen, 2.0f, Brushes.Red);
    //                _canvas_drawer.DrawCircles(p2_screen, 2.0f, Brushes.Red);
    //                _canvas_drawer.DrawCircles(p3, 2.0f, Brushes.Red);
    //                _canvas_drawer.DrawCircles(p4, 2.0f, Brushes.Red);
    //            }

    //            // Draw the markers for the wall end points
    //            foreach (WallData d in Calculator._wall_system._walls.Values)
    //            {
    //                Point start = MathHelpers.WorldCoord_ToScreen(cnvMainCanvas.Height, d.Start, SCALE_X, SCALE_Y);
    //                Point end = MathHelpers.WorldCoord_ToScreen(cnvMainCanvas.Height, d.End, SCALE_X, SCALE_Y);
    //                _canvas_drawer.DrawCircles(start, 2.0f, Brushes.Blue);
    //                _canvas_drawer.DrawCircles(end, 2.0f, Brushes.Blue);
    //            }
    //        }






    //        //// Draw the Loads
    //        //if (Calculator.V_x != 0)
    //        //{
    //        //    ArrowDirections dir = Calculator.V_x < 0 ? ArrowDirections.ARROW_LEFT : ArrowDirections.ARROW_RIGHT;
    //        //    DrawingHelpersLibrary.DrawingHelpers.DrawArrow(
    //        //        MainCanvas,
    //        //        Calculator.CtrRigidity.X * SCALE_X,
    //        //        MainCanvas.Height - Calculator.CtrRigidity.Y * SCALE_Y,
    //        //        System.Windows.Media.Brushes.Black,
    //        //        System.Windows.Media.Brushes.Black,
    //        //        dir,
    //        //        4
    //        //        );
    //        //}

    //        //if (Calculator.V_y != 0)
    //        //{
    //        //    ArrowDirections dir = Calculator.V_y < 0 ? ArrowDirections.ARROW_DOWN : ArrowDirections.ARROW_UP;
    //        //    DrawingHelpersLibrary.DrawingHelpers.DrawArrow(
    //        //        MainCanvas,
    //        //        Calculator.CtrRigidity.X * SCALE_X,
    //        //        MainCanvas.Height - Calculator.CtrRigidity.Y * SCALE_Y,
    //        //        System.Windows.Media.Brushes.Black,
    //        //        System.Windows.Media.Brushes.Black,
    //        //        dir,
    //        //        4
    //        //        );
    //        //}



    //        // Draw the moment arrow
    //        //if (calculator.Mt_comb != 0)
    //        //{
    //        //    ArrowDirections dir = calculator.Mt_comb < 0 ? ArrowDirections.ARROW_CLOCKWISE : ArrowDirections.ARROW_COUNTERCLOCKWISE;
    //        //    DrawingHelpersLibrary.DrawingHelpers.DrawCircularArrow(
    //        //        MainCanvas,
    //        //        calculator.CtrRigidity.X * SCALE_X + MARGIN,
    //        //        MainCanvas.Height - calculator.CtrRigidity.Y * SCALE_Y - MARGIN,
    //        //        System.Windows.Media.Brushes.Black,
    //        //        System.Windows.Media.Brushes.Black,
    //        //        dir,
    //        //        3,
    //        //        32 * SCALE_X,
    //        //        Math.PI / 2.0,
    //        //        (-1) * Math.PI / 2.0,
    //        //        8
    //        //        );
    //        //}

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

    //    #region Creating objects for the UI
    //    /// <summary>
    //    /// Function that creates the appropriate previewe shap for inputting the point selections
    //    /// -- wall point selection creates a line
    //    /// -- diaphragm point selection creates a rectangle
    //    /// </summary>
    //    /// <exception cref="NotImplementedException"></exception>
    //    public void DrawPreviewShape()
    //    {
    //        Shape shape = null;
    //        PreviewObjects.Clear();

    //        if(_currentPreviewLine == null)
    //        {
    //            return;
    //        }   

    //        switch (CurrentInputMode)
    //        {
    //            case InputModes.None:
    //                //no input mode so do nothing
    //                return;
    //            case InputModes.Diaphragm:
    //                // draw preview as a line
    //                shape = _currentPreviewLine;
    //                shape.Opacity = 0.3f;
    //                PreviewObjects.Add(shape);

    //                Point start = new Point((float)_currentPreviewLine.X1, (float)_currentPreviewLine.Y1);
    //                Point end = new Point((float)_currentPreviewLine.X2, (float)_currentPreviewLine.Y2);

    //                // Add dimension text
    //                TextBlock text = new TextBlock();
    //                if (MathHelpers.LineIsHorizontal(_currentPreviewLine))
    //                {
    //                    text.Text = $"{Math.Abs(end.X - start.X):0.0} ft";
    //                } else
    //                {
    //                    text.Text = $"{Math.Abs(end.Y - start.Y):0.0} ft";
    //                }
    //                Canvas.SetTop(text, (start.X + end.X) / 2.0);
    //                Canvas.SetTop(text, (start.Y + end.Y) / 2.0);
    //                cnvMainCanvas.Children.Add(text);

    //                // add marker at first point
    //                // marker for center_world of the rectangle -- center_world of area / mass
    //                Ellipse centerCircle = new Ellipse { Width = 6, Height = 6, Fill = Brushes.Green, Opacity = 0.4f };
    //                Canvas.SetLeft(centerCircle, start.X - centerCircle.Width / 2.0f);
    //                Canvas.SetTop(centerCircle, start.Y - centerCircle.Height / 2.0f);
    //                cnvMainCanvas.Children.Add(centerCircle);
    //                break;
    //            case InputModes.Wall:
    //                float x1 = (float)_currentPreviewLine.X1;
    //                float y1 = (float)_currentPreviewLine.Y1;
    //                float x2 = (float)_currentPreviewLine.X2;
    //                float y2 = (float)_currentPreviewLine.Y2;

    //                // Sort the points in to P1, P2, P3, P4 order
    //                ///              
    //                /// P4 --- P3
    //                /// |       |
    //                /// P1 --- P2 
    //                /// 
    //                Point first_pt = new Point(x1, y1);
    //                Point second_pt = new Point(x2, y2);
    //                Point P1, P2, P3, P4;

    //                // first point is either P1 or P4
    //                if (first_pt.X < second_pt.X)
    //                {
    //                    // Cases:
    //                    // (A) first point is P1 and second point is P3
    //                    if (first_pt.Y > second_pt.Y)
    //                    {
    //                        P1 = first_pt;
    //                        P3 = second_pt;

    //                        P2 = new System.Windows.Point(P3.X, P1.Y);
    //                        P4 = new System.Windows.Point(P1.X, P3.Y);
    //                    }
    //                    // (B) first point is P4 and second point is P2
    //                    else
    //                    {
    //                        P2 = second_pt;
    //                        P4 = first_pt;

    //                        P1 = new System.Windows.Point(P4.X, P2.Y);
    //                        P3 = new System.Windows.Point(P2.X, P4.Y);
    //                    }
    //                }

    //                // first point is either P2 or P3
    //                else
    //                {
    //                    // Cases:
    //                    // (A) first point is P2 and second point is P4
    //                    if (first_pt.Y > second_pt.Y)
    //                    {
    //                        P2 = first_pt;
    //                        P4 = second_pt;

    //                        P1 = new System.Windows.Point(P4.X, P2.Y);
    //                        P3 = new System.Windows.Point(P2.X, P4.Y);
    //                    }
    //                    // (B) first point is P3 and second point is P1
    //                    else
    //                    {
    //                        P1 = second_pt;
    //                        P3 = first_pt;

    //                        P2 = new System.Windows.Point(P3.X, P1.Y);
    //                        P4 = new System.Windows.Point(P1.X, P3.Y);
    //                    }
    //                }

    //                // the rectangular region object
    //                shape = new Rectangle
    //                {
    //                    Width = Math.Abs(P2.X - P1.X),
    //                    Height = Math.Abs(P4.Y - P1.Y),
    //                    Fill = Brushes.Green,
    //                    Stroke = Brushes.Green,
    //                    StrokeThickness = _canvas_drawer.rect_boundary_line_thickness,
    //                    Opacity = 0.3f,
    //                    IsHitTestVisible = false
    //                };
    //                Canvas.SetLeft(shape, P4.X);
    //                Canvas.SetTop(shape, P4.Y);
    //                cnvMainCanvas.Children.Add(shape);

    //                // add marker at first point
    //                // marker for center_world of the rectangle -- center_world of area / mass
    //                centerCircle = new Ellipse { Width = 6, Height = 6, Fill = Brushes.Green, Opacity = 0.4f, IsHitTestVisible = false };
    //                Canvas.SetLeft(centerCircle, first_pt.X - centerCircle.Width / 2.0f);
    //                Canvas.SetTop(centerCircle, first_pt.Y - centerCircle.Height / 2.0f);
    //                cnvMainCanvas.Children.Add(centerCircle);

    //                // Add dimension text
    //                TextBlock text2 = new TextBlock();
    //                text2.Text = $"{Math.Abs(P2.X - P1.X):0.0} x {Math.Abs(P4.Y - P1.Y):0.0} ft";
    //                text2.IsHitTestVisible = false;
    //                Canvas.SetTop(text2, (P1.X + P3.X) / 2.0);
    //                Canvas.SetTop(text2, (P1.Y + P3.Y) / 2.0);
    //                cnvMainCanvas.Children.Add(text2);
    //                break;
    //            default:
    //                throw new NotImplementedException("Unknown input mode in DrawPreviewShape: " + CurrentInputMode.ToString());
    //        }
    //    }

    //    /// <summary>
    //    /// Helper function to draw a rectangle with a border and a center_world point and add it to our list of structural objects to be drawn
    //    /// </summary>
    //    /// <param name="left"></param>
    //    /// <param name="top"></param>
    //    /// <param name="width"></param>
    //    /// <param name="height"></param>
    //    /// <param name="fill"></param>
    //    private void AddRectangleWithBorderAndCenter(double left, double top, double width, double height, Brush fill, float opacity = 1.0f)
    //    { 
    //        // the rectangular region object
    //        Rectangle rect = new Rectangle
    //        {
    //            Width = width,
    //            Height = height,
    //            Fill = fill,
    //            Stroke = fill,
    //            StrokeThickness = _canvas_drawer.rect_boundary_line_thickness,
    //            Opacity = opacity
    //        };
    //        Canvas.SetLeft(rect, left);
    //        Canvas.SetTop(rect, top);
    //        StructuralObjects.Add(rect);

    //        // marker for center_world of the rectangle -- center_world of area / mass
    //        Ellipse centerCircle = new Ellipse { Width = 4, Height = 4, Fill = fill, Opacity = opacity };
    //        Canvas.SetLeft(centerCircle, left + width / 2 - 2.0);
    //        Canvas.SetTop(centerCircle, top + height / 2 - 2.0);
    //        StructuralObjects.Add(centerCircle);
    //    }

    //    /// <summary>
    //    /// Function to handle drawing minor and major gridlines on our canvas
    //    /// </summary>
    //    private void CreateGridLines()
    //    {
    //        // limits of the drawing in model space
    //        System.Windows.Point bb_min_pt = new System.Windows.Point(-0.5 * DEFAULT_MODEL_EXITENTS_HORIZONTAL, -0.5 * DEFAULT_MODEL_EXITENTS_VERTICAL);
    //        System.Windows.Point bb_max_pt = new System.Windows.Point(1.5 * DEFAULT_MODEL_EXITENTS_HORIZONTAL, 1.5 * DEFAULT_MODEL_EXITENTS_VERTICAL);

    //        //if (Calculator != null)
    //        //{
    //        //    bb_min_pt = Calculator.Boundary_Min_Point;
    //        //    bb_max_pt = Calculator.Boundary_Max_Point;
    //        //}

    //        // for the vertical major gridlines
    //        for (int i = (int)bb_min_pt.Y; i < (int)bb_max_pt.Y; i += (int)DEFAULT_GRIDLINE_SPACING_MAJOR) // Large arbitrary bounds
    //        {
    //            Point p1_screen = new Point(i, 0);
    //            Point p2_screen = new Point(i, bb_max_pt.Y);
    //            Point scr_p1 = MathHelpers.WorldCoord_ToScreen(cnvMainCanvas.Height, p1_screen, SCALE_X, SCALE_Y);
    //            Point scr_p2 = MathHelpers.WorldCoord_ToScreen(cnvMainCanvas.Height, p2_screen, SCALE_X, SCALE_Y);

    //            // create the vertical major gridlines in screen coords
    //            Line verticalLine = new Line { X1 = p1_screen.X, Y1 = p1_screen.Y, X2 = p2_screen.X, Y2 = p2_screen.Y, Stroke = Brushes.DarkGray, StrokeDashArray = new DoubleCollection { 2, 2 }, StrokeThickness = 0.2 };
    //            CanvasDetailsObjects.Add(verticalLine);
    //        }

    //        // for the vertical minor gridlines
    //        for (int i = (int)bb_min_pt.Y; i < (int)bb_max_pt.Y; i += (int)DEFAULT_GRIDLINE_SPACING_MINOR) // Large arbitrary bounds
    //        {
    //            Point p1_screen = new Point(i, 0);
    //            Point p2_screen = new Point(i, bb_max_pt.Y);
    //            Point scr_p1 = MathHelpers.WorldCoord_ToScreen(cnvMainCanvas.Height, p1_screen, SCALE_X, SCALE_Y);
    //            Point scr_p2 = MathHelpers.WorldCoord_ToScreen(cnvMainCanvas.Height, p2_screen, SCALE_X, SCALE_Y);

    //            // check if we already have a major gridline by detemining if i is a multiple of the major gridline spacing
    //            if (i % DEFAULT_GRIDLINE_SPACING_MAJOR == 0)
    //            {
    //                continue;
    //            }

    //            // draw the minor gridlines
    //            Line verticalLine = new Line { X1 = p1_screen.X, Y1 = p1_screen.Y, X2 = p2_screen.X, Y2 = p2_screen.Y, Stroke = Brushes.DarkGray, StrokeDashArray = new DoubleCollection { 2, 2 }, StrokeThickness = 0.1 };
    //            CanvasDetailsObjects.Add(verticalLine);
    //        }

    //        // for the horizontal major gridlines
    //        for (int i = (int)bb_min_pt.X; i < (int)bb_max_pt.X; i += (int)DEFAULT_GRIDLINE_SPACING_MAJOR) // Large arbitrary bounds
    //        {
    //            Point p1_screen = new Point(0, i);
    //            Point p2_screen = new Point(bb_max_pt.X, i);
    //            Point scr_p1 = MathHelpers.WorldCoord_ToScreen(cnvMainCanvas.Height, p1_screen, SCALE_X, SCALE_Y);
    //            Point scr_p2 = MathHelpers.WorldCoord_ToScreen(cnvMainCanvas.Height, p2_screen, SCALE_X, SCALE_Y);

    //            // draw the major gridlines
    //            Line horizontalLine = new Line { X1 = p1_screen.X, Y1 = p1_screen.Y, X2 = p2_screen.X, Y2 = p2_screen.Y, Stroke = Brushes.DarkGray, StrokeDashArray = new DoubleCollection { 2, 2 }, StrokeThickness = 0.2 };

    //            CanvasDetailsObjects.Add(horizontalLine);
    //        }

    //        // for the horizontal minor gridlines
    //        for (int i = (int)bb_min_pt.X; i < (int)bb_max_pt.X; i += (int)DEFAULT_GRIDLINE_SPACING_MINOR) // Large arbitrary bounds
    //        {

    //            // check if we already have a major gridline by detemining if i is a multiple of the major gridline spacing
    //            if (i % DEFAULT_GRIDLINE_SPACING_MAJOR == 0)
    //            {
    //                continue;
    //            }

    //            Point p1_screen = new Point(0, i);
    //            Point p2_screen = new Point(bb_max_pt.X, i);
    //            Point scr_p1 = MathHelpers.WorldCoord_ToScreen(cnvMainCanvas.Height, p1_screen, SCALE_X, SCALE_Y);
    //            Point scr_p2 = MathHelpers.WorldCoord_ToScreen(cnvMainCanvas.Height, p2_screen, SCALE_X, SCALE_Y);

    //            // draw the minor gridlines
    //            Line horizontalLine = new Line { X1 = p1_screen.X, Y1 = p1_screen.Y, X2 = p2_screen.X, Y2 = p2_screen.Y, Stroke = Brushes.DarkGray, StrokeDashArray = new DoubleCollection { 2, 2 }, StrokeThickness = 0.1 };
    //            CanvasDetailsObjects.Add(horizontalLine);
    //        }
    //    }

    //    #endregion

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
    //        Point mousePosition = e.GetPosition(cnvMainCanvas);
    //        double zoomDelta = e.Delta > 0 ? _zoomFactor : 1 / _zoomFactor;
    //        double oldScaleX = _scaleTransform.ScaleX;
    //        double oldScaleY = _scaleTransform.ScaleY;

    //        SCALE_X = _scaleTransform.ScaleX * zoomDelta;
    //        SCALE_Y =_scaleTransform.ScaleY * zoomDelta;
    //        double scaleChangeX = _scaleTransform.ScaleX - oldScaleX;
    //        double scaleChangeY = _scaleTransform.ScaleY - oldScaleY;
    //        TRANS_X =(float)(_translateTransform.X - (mousePosition.X * scaleChangeX));
    //        TRANS_Y = (float)(_translateTransform.Y - (mousePosition.Y * scaleChangeY));
    //        _translateTransform.Y -= (mousePosition.Y * scaleChangeY);

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
