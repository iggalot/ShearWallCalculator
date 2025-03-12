using calculator;
using ShearWallCalculator;
using ShearWallVisualizer.Controls;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using static ShearWallVisualizer.Controls.ShearWallData;

namespace ShearWallVisualizer
{
    enum InputModes
    {
        None = 0,
        Rigidity = 1,
        Mass = 2
    }
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<Shape> StructuralObjects = new List<Shape>(); // a list of the structural (non-canvas drawing details) objects
        private List<Shape> CanvasDetails = new List<Shape>(); // a kist of objects drawn on the canvas but not needed to be recomputed frequently.

        private int _project_extents = 300;  // the dimensions of the canvas from one side to the other -- measured in feet
        private float default_gridline_spacing_major = 10;  // the spacing of the major gridlines -- measured in feet
        private float default_gridline_spacing_minor = 5;  // the spacing of the minor gridlines -- measured in feet

        // constants for drawing objects on canvas
        private const double rect_boundary_line_thickness = 0.5;

        private Line _currentPreviewLine = null;


        // variables for controlling canvas zooming and panning
        private ScaleTransform _scaleTransform;
        private double _zoomFactor = 1.1;  // amount of zoom when middle mouse button is scrolled
        private Point _lastMousePosition; // last mouse position in canvas coords
        private bool _isPanning;
        private TransformGroup _transformGroup;
        private TranslateTransform _translateTransform;


        // scale factors for the canvas and layout of the visiualizer
        private const double SCALE_X = 1;
        private const double SCALE_Y = 1;
        private float DEFAULT_WALL_HT = 9;  // ft

        // variables for handling input
        private InputModes CurrentInputMode { get; set; } = InputModes.None;
        private bool _startClickSet = false;
        private bool _endClickSet = false;

        public System.Windows.Point CurrentStartPoint { get; set; } // contains the current set canvas start point for walls and diaphragms
        public System.Windows.Point CurrentEndPoint { get; set; } // contains the current set canvas end point for walls and diaphragms

        public ShearWallCalculatorBase Calculator { get; set; } = null; // the main source of our calculations
        public WallData CurrentWall { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            Calculator = new ShearWallCalculator_RigidDiaphragm();

            _scaleTransform = new ScaleTransform(SCALE_X, SCALE_Y);
            _translateTransform = new TranslateTransform();
            _transformGroup = new TransformGroup();
            _transformGroup.Children.Add(_scaleTransform);
            _transformGroup.Children.Add(_translateTransform);
            cnvMainCanvas.RenderTransform = _transformGroup;

            CreateGridLines();

            // Create a some test data
            Calculator._diaphragm_system.AddDiaphragm(new DiaphragmData_Rectangular(new Point(50, 50), new Point(100, 100)));

            //            Calculator._diaphragm_system.AddDiaphragm(new DiaphragmData_Rectangular(new Point(100, 100), new Point(40, 60)));


            Update();
        }

        /// <summary>
        /// Helper function to draw a rectangle with a border and a center point and add it to our list of structural objects to be drawn
        /// </summary>
        /// <param name="left"></param>
        /// <param name="top"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="fill"></param>
        private void AddRectangleWithBorderAndCenter(double left, double top, double width, double height, Brush fill)
        {
            Rectangle rect = new Rectangle { Width = width, Height = height, Fill = fill, Stroke = Brushes.Black, StrokeThickness = rect_boundary_line_thickness };
            Canvas.SetLeft(rect, left);
            Canvas.SetTop(rect, top);
            StructuralObjects.Add(rect);

            Ellipse centerCircle = new Ellipse { Width = 5, Height = 5, Fill = Brushes.Black };
            Canvas.SetLeft(centerCircle, left + width / 2 - 2.5);
            Canvas.SetTop(centerCircle, top + height / 2 - 2.5);
            StructuralObjects.Add(centerCircle);

            Ellipse upperleft = new Ellipse { Width = 2, Height = 2, Fill = Brushes.Red };
            Canvas.SetLeft(upperleft, left);
            Canvas.SetTop(upperleft, top);
            StructuralObjects.Add(upperleft);

            Ellipse lowerright = new Ellipse { Width = 2, Height = 2, Fill = Brushes.Blue };
            Canvas.SetLeft(lowerright, left + width);
            Canvas.SetTop(lowerright, top + height);
            StructuralObjects.Add(lowerright);
        }

        /// <summary>
        /// Calculates the bounding box of the elementas on the canvas
        /// </summary>
        private void DrawBoundingBox()
        {
            // look for the extreme values in the wall system lines
            if (Calculator == null)
            {
                return;
            }

            System.Windows.Point bb_min_pt = Calculator.Boundary_Min_Point;
            System.Windows.Point bb_max_pt = Calculator.Boundary_Max_Point;

            System.Windows.Point screen_bb_min_pt = WorldCoord_ToScreen(bb_min_pt);
            System.Windows.Point screen_bb_max_pt = WorldCoord_ToScreen(bb_max_pt);

            float screen_bb_left   = (float)screen_bb_min_pt.X;
            float screen_bb_top    = (float)screen_bb_max_pt.Y;
            float screen_bb_right  = (float)screen_bb_max_pt.X;
            float screen_bb_bottom = (float)screen_bb_min_pt.Y;

            float left = screen_bb_left;
            float top = screen_bb_top;
            float right = screen_bb_right;
            float bottom = screen_bb_bottom;

            if (Calculator._diaphragm_system._diaphragms.Count > 0 || Calculator._wall_system._walls.Count > 0)
            {
                Line topLine = new Line { X1 = left, Y1 = top, X2 = right, Y2 = top, Stroke = Brushes.Black, StrokeThickness = 2 * rect_boundary_line_thickness, StrokeDashArray = new DoubleCollection { 1, 1 } };
                Line bottomLine = new Line { X1 = left, Y1 = bottom, X2 = right, Y2 = bottom, Stroke = Brushes.Black, StrokeThickness = 2 * rect_boundary_line_thickness, StrokeDashArray = new DoubleCollection { 1, 1 } };
                Line leftLine = new Line { X1 = left, Y1 = top, X2 = left, Y2 = bottom, Stroke = Brushes.Black, StrokeThickness = 2 * rect_boundary_line_thickness, StrokeDashArray = new DoubleCollection { 1, 1 } };
                Line rightLine = new Line { X1 = right, Y1 = top, X2 = right, Y2 = bottom, Stroke = Brushes.Black, StrokeThickness = 2 * rect_boundary_line_thickness, StrokeDashArray = new DoubleCollection { 1, 1 } };

                cnvMainCanvas.Children.Add(topLine);
                cnvMainCanvas.Children.Add(bottomLine);
                cnvMainCanvas.Children.Add(leftLine);
                cnvMainCanvas.Children.Add(rightLine);

                lblXMin.Content = left.ToString("0.00");
                lblYMin.Content = bottom.ToString("0.00");
                lblXMax.Content = right.ToString("0.00");
                lblYMax.Content = top.ToString("0.00");
            }

        }

        /// <summary>
        /// Function handle drawing minor and major gridlines on our canvas
        /// </summary>
        private void CreateGridLines()
        {
            // for the major gridlines
            for (int i = (int)-cnvMainCanvas.Width; i < (int)cnvMainCanvas.Width; i += (int)default_gridline_spacing_major) // Large arbitrary bounds
            {
                // draw the minor gridlines
                Line verticalLine = new Line { X1 = i, Y1 = 0, X2 = i, Y2 = cnvMainCanvas.Height, Stroke = Brushes.DarkGray, StrokeDashArray = new DoubleCollection { 2, 2 }, StrokeThickness = 0.4 };
                Line horizontalLine = new Line { X1 = 0, Y1 = i, X2 = cnvMainCanvas.Width, Y2 = i, Stroke = Brushes.DarkGray, StrokeDashArray = new DoubleCollection { 2, 2 }, StrokeThickness = 0.4 };
                CanvasDetails.Add(verticalLine);
                CanvasDetails.Add(horizontalLine);
            }

            // for the minor gridlines
            for (int i = (int)-cnvMainCanvas.Width; i < (int)cnvMainCanvas.Width; i += (int)default_gridline_spacing_minor) // Large arbitrary bounds
            {
                // check if we already have a major gridline by detemining if i is a multiple of the major gridline spacing
                if(i % default_gridline_spacing_major == 0)
                {
                    continue;
                }

                // draw the minor gridlines
                Line verticalLine = new Line { X1 = i, Y1 = 0, X2 = i, Y2 = cnvMainCanvas.Height, Stroke = Brushes.LightGray, StrokeDashArray = new DoubleCollection { 2, 2 }, StrokeThickness = 0.2 };
                Line horizontalLine = new Line { X1 = 0, Y1 = i, X2 = cnvMainCanvas.Width, Y2 = i, Stroke = Brushes.LightGray, StrokeDashArray = new DoubleCollection { 2, 2 }, StrokeThickness = 0.2 };
                CanvasDetails.Add(verticalLine);
                CanvasDetails.Add(horizontalLine);
            }
        }


        public void CreateStructuralObjects()
        {
            foreach (var item in Calculator._wall_system._walls)
            {
                WallDirs walldir = item.Value.WallDir;

                // Get the screen coords based on the world coords of the model
                Point canvas_start_pt = WorldCoord_ToScreen(item.Value.Start);
                Point canvas_end_pt = WorldCoord_ToScreen(item.Value.End);

                float canvas_height = (float)(canvas_end_pt.Y - canvas_start_pt.Y);
                float canvas_width = (float)(canvas_end_pt.X - canvas_start_pt.X);

                if(walldir == WallDirs.EastWest)
                {
                    // if the width measures as negative, swap the start and end points for drawing
                    if (canvas_width < 0)
                    {
                        Point temp = canvas_start_pt;
                        canvas_start_pt = canvas_end_pt;
                        canvas_end_pt = temp;
                    }
                }

                if (walldir == WallDirs.NorthSouth)
                {
                    // if the height measures as negative, swap the start and end points for drawing
                    if (canvas_height < 0)
                    {
                        Point temp = canvas_start_pt;
                        canvas_start_pt = canvas_end_pt;
                        canvas_end_pt = temp;
                    }
                }

                float width = (walldir == WallDirs.EastWest ? (float)Math.Abs(canvas_width) : 1.0f);
                float height = (walldir == WallDirs.NorthSouth ? (float)Math.Abs(canvas_height) : 1.0f);

                AddRectangleWithBorderAndCenter(
                    canvas_start_pt.X,
                    canvas_start_pt.Y,
                    width,
                    height,
                    Brushes.Black);
            }


            foreach (var item in Calculator._diaphragm_system._diaphragms)
            {
                // P3 and P1 are opposite corners of the diaphragm definition
                // Get the screen coords based on the world coords of the model
                Point p1_canvas_start_pt = WorldCoord_ToScreen(item.Value.P1);
                Point p1_canvas_end_pt = WorldCoord_ToScreen(item.Value.P1);

                Point p3_canvas_start_pt = WorldCoord_ToScreen(item.Value.P3);
                Point p3_canvas_end_pt = WorldCoord_ToScreen(item.Value.P3);

                float canvas_height = Math.Abs((float)(p3_canvas_end_pt.Y - p1_canvas_start_pt.Y));
                float canvas_width = Math.Abs((float)(p3_canvas_end_pt.X - p1_canvas_start_pt.X));

                Point p4_canvas_start_pt = WorldCoord_ToScreen(item.Value.P4);
                Point p4_canvas_end_pt = WorldCoord_ToScreen(item.Value.P4);

                AddRectangleWithBorderAndCenter(
                    p4_canvas_start_pt.X,
                    p4_canvas_start_pt.Y,
                    canvas_width,
                    canvas_height,
                    Brushes.Red);
            }
        }

        public void DrawGridLines()
        {
            foreach (Line line in CanvasDetails)
            {
                cnvMainCanvas.Children.Add(line);
            }
        }

        public void DrawStructuralObjects()
        {
            foreach (var item in StructuralObjects)
            {
                cnvMainCanvas.Children.Add(item);
            }
        }


        private void DrawResults()
        {

            if (Calculator == null)
            {
                return;
            }

            //// Clear the MainCanvas before redrawing.
            cnvMainCanvas.Children.Clear();

            // Draw the gridlines on the canvas
            DrawGridLines();
            DrawStructuralObjects();
            DrawBoundingBox();

            // Draw a preview Line 
            if (_currentPreviewLine != null)
            {
                if (cnvMainCanvas.Children.Contains(_currentPreviewLine) != true)
                {
                    cnvMainCanvas.Children.Add(_currentPreviewLine);
                }
            }

            ////Draw the diaphragm subregions
            //if (Calculator._diaphragm_system != null && Calculator._diaphragm_system._diaphragms.Count > 0)
            //{
            //    foreach (var diaphragm in Calculator._diaphragm_system._diaphragms)
            //    {
            //        // draw centroid points
            //        DrawingHelpersLibrary.DrawingHelpers.DrawCircle(
            //            MainCanvas,
            //            diaphragm.Value.Centroid.X * SCALE_X,
            //            MainCanvas.Height - diaphragm.Value.Centroid.Y * SCALE_Y,
            //            System.Windows.Media.Brushes.Red,
            //            System.Windows.Media.Brushes.Red,
            //            3, 1);

            //        System.Windows.Point p1 = diaphragm.Value.P1;
            //        System.Windows.Point p2 = diaphragm.Value.P2;
            //        System.Windows.Point p3 = diaphragm.Value.P3;
            //        System.Windows.Point p4 = diaphragm.Value.P4;

            //        DrawingHelpersLibrary.DrawingHelpers.DrawLine(
            //            MainCanvas,
            //            p1.X * SCALE_X,
            //            MainCanvas.Height - p1.Y * SCALE_Y,
            //            p2.X * SCALE_X,
            //            MainCanvas.Height - p2.Y * SCALE_Y,
            //            System.Windows.Media.Brushes.Red,
            //            2);
            //        DrawingHelpersLibrary.DrawingHelpers.DrawLine(
            //            MainCanvas,
            //            p2.X * SCALE_X,
            //            MainCanvas.Height - p2.Y * SCALE_Y,
            //            p3.X * SCALE_X,
            //            MainCanvas.Height - p3.Y * SCALE_Y,
            //            System.Windows.Media.Brushes.Red,
            //            2);
            //        DrawingHelpersLibrary.DrawingHelpers.DrawLine(
            //            MainCanvas,
            //            p3.X * SCALE_X,
            //            MainCanvas.Height - p3.Y * SCALE_Y,
            //            p4.X * SCALE_X,
            //            MainCanvas.Height - p4.Y * SCALE_Y,
            //            System.Windows.Media.Brushes.Red,
            //            2);
            //        DrawingHelpersLibrary.DrawingHelpers.DrawLine(
            //            MainCanvas,
            //            p4.X * SCALE_X,
            //            MainCanvas.Height - p4.Y * SCALE_Y,
            //            p1.X * SCALE_X,
            //            MainCanvas.Height - p1.Y * SCALE_Y,
            //            System.Windows.Media.Brushes.Red,
            //            2);

            //        // Shade the rectangular region
            //        Rectangle rect = new Rectangle() { Stroke = Brushes.Red };
            //        rect.Width = Math.Abs(p3.X * SCALE_X - p1.X * SCALE_X);
            //        rect.Height = Math.Abs(p3.Y * SCALE_Y - p1.Y * SCALE_Y);
            //        Canvas.SetLeft(rect, Math.Min(p4.X * SCALE_X, p3.X * SCALE_X));
            //        Canvas.SetTop(rect, Math.Min(p4.Y * SCALE_Y, p3.Y * SCALE_Y));
            //        rect.Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(50, 255, 0, 0));
            //        MainCanvas.Children.Add(rect);

            //    }
            //}


            //}

            // if we have a defined diaphragm, draw the centroid in screen coords
            System.Windows.Point p1 = WorldCoord_ToScreen(Calculator._diaphragm_system.CtrMass);
            if (Calculator._diaphragm_system != null)
            {
                // Draw the Center of Rigidity Point
                DrawingHelpersLibrary.DrawingHelpers.DrawCircle(
                    cnvMainCanvas,
                    p1.X, p1.Y,
                    System.Windows.Media.Brushes.Red,
                    System.Windows.Media.Brushes.Red,
                    5,
                    1);
                DrawingHelpersLibrary.DrawingHelpers.DrawText(
                    cnvMainCanvas,
                    p1.X, p1.Y,
                    0,
                    "C.R",
                    System.Windows.Media.Brushes.Red,
                    12
                    );
            }


            //Draw the additional wall info including labels for the center point
            if (Calculator._wall_system != null)
            {

                if (Calculator._wall_system.EW_Walls.Count > 0)
                {
                    foreach (var wall in Calculator._wall_system.EW_Walls)
                    {
                        System.Windows.Point cp = WorldCoord_ToScreen(wall.Value.Center);

                        // number label for the wall ID
                        DrawingHelpersLibrary.DrawingHelpers.DrawText
                            (
                            cnvMainCanvas,
                            cp.X + 1,
                            cp.Y + 1,
                            0,
                            wall.Key.ToString(),
                            System.Windows.Media.Brushes.Black,
                            12
                            );
                    }
                }
                //Draw the walls
                if (Calculator._wall_system.NS_Walls.Count > 0)
                {
                    foreach (var wall in Calculator._wall_system.NS_Walls)
                    {
                        System.Windows.Point cp = WorldCoord_ToScreen(wall.Value.Center);

                        DrawingHelpersLibrary.DrawingHelpers.DrawText
                            (
                            cnvMainCanvas,
                            cp.X + 1,
                            cp.Y + 1,
                            0,
                            wall.Key.ToString(),
                            System.Windows.Media.Brushes.Black,
                            12
                            );
                    }
                }

                // Draw the Center of Rigiidity Point
                System.Windows.Point cor_pt = WorldCoord_ToScreen(Calculator._wall_system.CtrRigidity);
                DrawingHelpersLibrary.DrawingHelpers.DrawCircle(
                    cnvMainCanvas,
                    cor_pt.X,
                    cor_pt.Y,
                    System.Windows.Media.Brushes.Blue,
                    System.Windows.Media.Brushes.Blue,
                    5,
                    0.5);
                DrawingHelpersLibrary.DrawingHelpers.DrawText(
                    cnvMainCanvas,
                    cor_pt.X -3,
                    cor_pt.Y -8,
                    0,
                    "C.R",
                    System.Windows.Media.Brushes.Black,
                    6
                    );






                //// Draw the Loads
                //if (Calculator.V_x != 0)
                //{
                //    ArrowDirections dir = Calculator.V_x < 0 ? ArrowDirections.ARROW_LEFT : ArrowDirections.ARROW_RIGHT;
                //    DrawingHelpersLibrary.DrawingHelpers.DrawArrow(
                //        MainCanvas,
                //        Calculator.CtrRigidity.X * SCALE_X,
                //        MainCanvas.Height - Calculator.CtrRigidity.Y * SCALE_Y,
                //        System.Windows.Media.Brushes.Black,
                //        System.Windows.Media.Brushes.Black,
                //        dir,
                //        4
                //        );
                //}

                //if (Calculator.V_y != 0)
                //{
                //    ArrowDirections dir = Calculator.V_y < 0 ? ArrowDirections.ARROW_DOWN : ArrowDirections.ARROW_UP;
                //    DrawingHelpersLibrary.DrawingHelpers.DrawArrow(
                //        MainCanvas,
                //        Calculator.CtrRigidity.X * SCALE_X,
                //        MainCanvas.Height - Calculator.CtrRigidity.Y * SCALE_Y,
                //        System.Windows.Media.Brushes.Black,
                //        System.Windows.Media.Brushes.Black,
                //        dir,
                //        4
                //        );
                //}



                // Draw the moment arrow
                //if (calculator.Mt_comb != 0)
                //{
                //    ArrowDirections dir = calculator.Mt_comb < 0 ? ArrowDirections.ARROW_CLOCKWISE : ArrowDirections.ARROW_COUNTERCLOCKWISE;
                //    DrawingHelpersLibrary.DrawingHelpers.DrawCircularArrow(
                //        MainCanvas,
                //        calculator.CtrRigidity.X * SCALE_X + MARGIN,
                //        MainCanvas.Height - calculator.CtrRigidity.Y * SCALE_Y - MARGIN,
                //        System.Windows.Media.Brushes.Black,
                //        System.Windows.Media.Brushes.Black,
                //        dir,
                //        3,
                //        32 * SCALE_X,
                //        Math.PI / 2.0,
                //        (-1) * Math.PI / 2.0,
                //        8
                //        );
                //}



                //// Create Table of Results
                //ShearWallResults.Children.Clear();

                if (Calculator.GetType() == typeof(ShearWallCalculator_RigidDiaphragm))
                {

                    // Create Table of Wall Data
                    ShearWallData_EW.Children.Clear();
                    ShearWallData_NS.Children.Clear();
                    ShearWallResults.Children.Clear();

                    foreach (var result in ((ShearWallCalculator_RigidDiaphragm)Calculator).TotalWallShear)
                    {
                        int id = result.Key;
                        float rigidity = ((ShearWallCalculator_RigidDiaphragm)Calculator)._wall_system.EW_Walls.ContainsKey(id) ? ((ShearWallCalculator_RigidDiaphragm)Calculator)._wall_system.EW_Walls[id].WallRigidity : Calculator._wall_system.NS_Walls[id].WallRigidity;
                        float direct_shear_x = ((ShearWallCalculator_RigidDiaphragm)Calculator).DirectShear_X.ContainsKey(id) ? ((ShearWallCalculator_RigidDiaphragm)Calculator).DirectShear_X[id] : 0.0f;
                        float direct_shear_y = ((ShearWallCalculator_RigidDiaphragm)Calculator).DirectShear_Y.ContainsKey(id) ? ((ShearWallCalculator_RigidDiaphragm)Calculator).DirectShear_Y[id] : 0.0f;
                        float eccentric_shear = ((ShearWallCalculator_RigidDiaphragm)Calculator).EccentricShear.ContainsKey(id) ? ((ShearWallCalculator_RigidDiaphragm)Calculator).EccentricShear[id] : 0.0f;
                        float total_shear = ((ShearWallCalculator_RigidDiaphragm)Calculator).TotalWallShear.ContainsKey(id) ? ((ShearWallCalculator_RigidDiaphragm)Calculator).TotalWallShear[id] : 0.0f;

                        ShearWallResults control = new ShearWallResults(
                            id,
                            rigidity,
                            Calculator._wall_system.X_bar_walls[id],
                            Calculator._wall_system.Y_bar_walls[id],
                            direct_shear_x,
                            direct_shear_y,
                            eccentric_shear,
                            total_shear
                            );


                        ShearWallResults.Children.Add(control);
                    }


                    /// Create the shearwall data controls
                    foreach (var result in ((ShearWallCalculator_RigidDiaphragm)Calculator).TotalWallShear)
                    {
                        int id = result.Key;
                        WallData wall = Calculator._wall_system.EW_Walls.ContainsKey(id) ? Calculator._wall_system.EW_Walls[id] : Calculator._wall_system.NS_Walls[id];
                        ShearWallData control = new ShearWallData(id, wall);

                        if (wall.WallDir == WallDirs.EastWest)
                        {
                            ShearWallData_EW.Children.Add(control);
                        }
                        else if (wall.WallDir == WallDirs.NorthSouth)
                        {
                            ShearWallData_NS.Children.Add(control);
                        }
                        else
                        {
                            throw new Exception("Invalid wall direction " + wall.WallDir.ToString() + " in wall #" + id.ToString());
                        }

                        control.DeleteWall += OnWallDeleted;
                    }
                }
                else if (Calculator.GetType() == typeof(ShearWallCalculator_FlexibleDiaphragm))
                {
                    throw new NotImplementedException("\nFlexible Diaphragms Calculator not implemented yet.");
                }
                else
                {
                    throw new NotImplementedException("\nInvalid Calaculator type received.");
                }
            }
        }

        private void OnWallDeleted(object sender, DeleteWallEventArgs e)
        {
            if(Calculator._wall_system._walls.ContainsKey(e.Id) == true)
            {
                Calculator._wall_system._walls.Remove(e.Id);

                MessageBox.Show(e.Id.ToString() + " has been deleted");
            } else
            {
                MessageBox.Show(e.Id.ToString() + " does not exist in Walls");
            }
            
            Update();




        }


        private void Update()
        {
            // Create the new calculator
            Calculator.Update();

            cnvMainCanvas.Children.Clear(); // clear the canvas 

            // Recompute the structural objects to be drawn
            StructuralObjects.Clear();
            CreateStructuralObjects();

            DrawResults();

            CenterOfMass.Content = "(" + Calculator._diaphragm_system.CtrMass.X.ToString("0.00") + ", " + Calculator._diaphragm_system.CtrMass.Y.ToString("0.00") + ")";
            CenterOfRigidity.Content = "(" + Calculator._wall_system.CtrRigidity.X.ToString("0.00") + ", " + Calculator._wall_system.CtrRigidity.Y.ToString("0.00") + ")";
        }

        /// <summary>
        /// What happens when the middle mouse wheel is scrolled
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            //MessageBox.Show("MouseWheel");
            Point mousePosition = e.GetPosition(cnvMainCanvas);
            double zoomDelta = e.Delta > 0 ? _zoomFactor : 1 / _zoomFactor;
            double oldScaleX = _scaleTransform.ScaleX;
            double oldScaleY = _scaleTransform.ScaleY; _scaleTransform.ScaleX *= zoomDelta;
            _scaleTransform.ScaleY *= zoomDelta;
            double scaleChangeX = _scaleTransform.ScaleX - oldScaleX;
            double scaleChangeY = _scaleTransform.ScaleY - oldScaleY;
            _translateTransform.X -= (mousePosition.X * scaleChangeX);
            _translateTransform.Y -= (mousePosition.Y * scaleChangeY);
        }

        /// <summary>
        /// what happens when a mouse button is pressed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Middle mouse button
            if (e.MiddleButton == MouseButtonState.Pressed)
            {
                _isPanning = true;
                _lastMousePosition = e.GetPosition(CanvasScrollViewer);
                cnvMainCanvas.Cursor = Cursors.Hand;
            }
            else
            {
                string status = "";
                if ((CurrentInputMode == InputModes.Rigidity) || (CurrentInputMode == InputModes.Mass))
                {
                    _currentPreviewLine = new Line() { Stroke = Brushes.Green };
                    System.Windows.Point p = Mouse.GetPosition(cnvMainCanvas);
                    System.Windows.Point wp = ScreenCoord_ToWorld(Mouse.GetPosition(cnvMainCanvas));

                    if (_startClickSet == false)
                    {
                        lblScreenStartCoord.Content = "(" + p.X.ToString("0.00") + ", " + p.Y.ToString("0.00") + ")";
                        lblWorldStartCoord.Content = "(" + wp.X.ToString("0.00") + ", " + wp.Y.ToString("0.00") + ")";

                        CurrentStartPoint = p;
                        _startClickSet = true;
                        _currentPreviewLine.X1 = CurrentStartPoint.X;
                        _currentPreviewLine.Y1 = CurrentStartPoint.Y;
                        _currentPreviewLine.X2 = CurrentStartPoint.X;
                        _currentPreviewLine.Y2 = CurrentStartPoint.Y;

                        status = "First point selected";
                        return;
                    }
                    else
                    {
                        lblScreenEndCoord.Content = "(" + p.X.ToString("0.00") + ", " + p.Y.ToString("0.00") + ")";
                        lblWorldEndCoord.Content = "(" + wp.X.ToString("0.00") + ", " + wp.Y.ToString("0.00") + ")";
                        CurrentEndPoint = p;
                        _endClickSet = true;
                        _currentPreviewLine.X2 = CurrentEndPoint.X;
                        _currentPreviewLine.Y2 = CurrentEndPoint.Y;
                        status = "Second point selected";
                    }

                    lblStatus.Content = status;

                    if (_startClickSet && _endClickSet)
                    {
                        // process the canvas screen coords clicked
                        float start_x = ((float)CurrentStartPoint.X);
                        float start_y = ((float)CurrentStartPoint.Y);
                        float end_x = ((float)CurrentEndPoint.X);
                        float end_y = ((float)CurrentEndPoint.Y);

                        if (CurrentInputMode == InputModes.Rigidity)
                        {
                            // Determine if this wall segment should be horizontal or vertical by looking at the difference between the x
                            // and y coordinates of the start and end points.  Whichever difference is larger will be the direction
                            //  -- larger X direction = horizontal
                            //  -- larger Y direction = vertical
                            WallDirs dir = WallDirs.EastWest;
                            if ((Math.Abs(CurrentStartPoint.X - CurrentEndPoint.X)) >= Math.Abs(CurrentStartPoint.Y - CurrentEndPoint.Y))
                            {
                                dir = WallDirs.EastWest;
                                end_y = start_y; // move the end point to make the line horizontal
                            }
                            else
                            {
                                dir = WallDirs.NorthSouth;
                                end_x = start_x; // move the end point to make the line vertical
                            }

                            Point p1 = ScreenCoord_ToWorld(CurrentStartPoint);
                            Point p2 = ScreenCoord_ToWorld(CurrentEndPoint);

                            // Add to the list of wall segments
                            Calculator._wall_system.AddWall(new WallData(DEFAULT_WALL_HT, 
                                (float)p1.X, (float)p1.Y, (float)p2.X, (float)p2.Y, dir));
                            _currentPreviewLine = null;  // clear the preview line
                            status = "Wall added";
                            Update();
                        }
                        else if (CurrentInputMode == InputModes.Mass)
                        {
                            Point p1 = ScreenCoord_ToWorld(CurrentStartPoint);
                            Point p2 = ScreenCoord_ToWorld(CurrentEndPoint);

                            // Add to the list of diaphragm segments
                            Calculator._diaphragm_system.AddDiaphragm(new DiaphragmData_Rectangular(p1, p2));
                            lblStatus.Content = "Diaphragm added";
                        }
                        else
                        {
                            throw new Exception("Invalid input mode -- should be rigidity or mass mode.");
                        }

                        // then clear the variables.
                        _startClickSet = false;
                        _endClickSet = false;
                    }

                    else if (CurrentInputMode == InputModes.Mass)
                    {
                        //System.Windows.Point p = Mouse.GetPosition(MainCanvas);

                        //System.Windows.Point point = new System.Windows.Point(p.X / (float)SCALE_X, (MainCanvas.Height - p.Y) / (float)SCALE_Y);
                        //DiaphragmPoints.Add(point);



                        //Update();
                    }
                    lblStatus.Content = status;
                }
            }
        }

        /// <summary>
        /// What happens when the mouse is moved
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            // Check if middle mouse button is pressed and we are in panning mode
            if (_isPanning && e.MiddleButton == MouseButtonState.Pressed)
            {
                Point currentPosition = e.GetPosition(CanvasScrollViewer);
                double offsetX = currentPosition.X - _lastMousePosition.X;
                double offsetY = currentPosition.Y - _lastMousePosition.Y;
                CanvasScrollViewer.ScrollToHorizontalOffset(CanvasScrollViewer.HorizontalOffset + offsetX);
                CanvasScrollViewer.ScrollToVerticalOffset(CanvasScrollViewer.VerticalOffset + offsetY);

                foreach (UIElement element in cnvMainCanvas.Children)
                {
                    if (element is FrameworkElement fe)
                    {
                        fe.RenderTransform = new TranslateTransform(offsetX + fe.RenderTransform.Value.OffsetX, offsetY + fe.RenderTransform.Value.OffsetY);
                    }
                }

                _lastMousePosition = currentPosition;
            }

            else
            {
                // Update the preview lines
                if (_currentPreviewLine != null)
                {
                    var p = e.GetPosition(cnvMainCanvas);
                    _currentPreviewLine.X2 = p.X;
                    _currentPreviewLine.Y2 = p.Y;
                }

                // Update the mouse position label
                MousePosition.Content = "(" + e.GetPosition(cnvMainCanvas).X.ToString("0.00") + ", " + e.GetPosition(cnvMainCanvas).Y.ToString("0.00") + ")";

                Update();
            }

        }

        /// <summary>
        /// What happens when a mouse button is released
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            // Middle mouse button is released -- stops panning
            if (e.MiddleButton == MouseButtonState.Released)
            {
                _isPanning = false;
                cnvMainCanvas.Cursor = Cursors.Arrow;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            window.KeyDown += HandleKeyPress;
        }

        /// <summary>
        /// Set up the event to detect key presses within the window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleKeyPress(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                // Clear all the input data
                case Key.C:
                    if(CurrentInputMode == InputModes.Rigidity)
                    {
                        Calculator._wall_system._walls.Clear();
                    }
                    if(CurrentInputMode == InputModes.Mass)
                    {
                        Calculator._diaphragm_system._diaphragms.Clear();
                    }
                    break;
                // Enter points to define the diaphragm
                case Key.M:
                    CurrentInputMode = InputModes.Mass;
                    CurrentMode.Content = "DIAPHRAGM ENTRY (MASS MODE)";
                    break;
                case Key.R:
                    CurrentInputMode = InputModes.Rigidity;
                    CurrentMode.Content = "SHEAR WALL ENTRY (RIGIDITY MODE)";
                    break;
                case Key.Enter:
                    FinishCommand();
                    CurrentMode.Content = "NONE";
                    break;
                default:
                    break;
            }

            Update();
        }

        /// <summary>
        /// converts coordinates from the screen (canvas) to the world coordinates
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        private Point ScreenCoord_ToWorld(Point p)
        {
            return new Point(p.X / (float)SCALE_X, (cnvMainCanvas.Height - p.Y) / (float)SCALE_Y);
        }

        /// <summary>
        /// converts coordinates from the world to the screen coordinates (canvas) system
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        private Point WorldCoord_ToScreen(Point p)
        {
            return new Point(p.X * (float)SCALE_X, cnvMainCanvas.Height - (p.Y * (float)SCALE_Y));
        }

        private void FinishCommand()
        {
        }
    }
}
