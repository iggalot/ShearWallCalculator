using calculator;
using ShearWallCalculator;
using ShearWallVisualizer.Controls;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;

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
        private float DEFAULT_WALL_HT = 9;  // ft

        // Containers for Canvas drawing objects
        private List<Shape> StructuralObjects = new List<Shape>(); // a list of the structural (non-canvas drawing details) objects
        private List<Shape> CanvasDetailsObjects = new List<Shape>(); // a list of objects drawn on the canvas but not needed to be recomputed frequently.
        private List<Shape> PreviewObjects = new List<Shape>(); // a list of objects containing any temporary preview objects to be drawn tocanvas

        // constants for gridlines
        private float default_gridline_spacing_major = 10;  // the spacing of the major gridlines -- measured in feet
        private float default_gridline_spacing_minor = 5;  // the spacing of the minor gridlines -- measured in feet

        // constants for properties of drawing objects on canvas
        private const double rect_boundary_line_thickness = 0.5;

        private Shape _PreviewShape = null;
        private Line _currentPreviewLine = null; // contains the points for first and second selection

        // current mouse position
        private Point _currentMousePosition = new Point();

        // cross hairs
        private Line _crosshairVertical = null;
        private Line _crosshairHorizontal = null;


        // variables for controlling canvas zooming and panning
        private ScaleTransform _scaleTransform;
        private double _zoomFactor = 1.1;  // amount of zoom when middle mouse button is scrolled
        private Point _lastMousePosition; // last mouse position in canvas coords -- used for panning
        private bool _isPanning; // flag for panning state
        private TransformGroup _transformGroup;
        private TranslateTransform _translateTransform;
        private const double SCALE_X = 1;  // scale factor for x-dir
        private const double SCALE_Y = 1;  // scale factor for y-dir

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

            // create our Calculator object 
            // TODO: This will need to be switchable between RigidDiaphragm and FlexibileDiaphragm or both
            Calculator = new ShearWallCalculator_RigidDiaphragm();

            // set up scaling parameters associated with the canvas
            _scaleTransform = new ScaleTransform(SCALE_X, SCALE_Y);
            _translateTransform = new TranslateTransform();
            _transformGroup = new TransformGroup();
            _transformGroup.Children.Add(_scaleTransform);
            _transformGroup.Children.Add(_translateTransform);
            cnvMainCanvas.RenderTransform = _transformGroup;

            // create the gridlines
            CreateGridLines();

            // creates the crosshairs for point selection
            _crosshairVertical = new Line { 
                X1 = _currentMousePosition.X, 
                Y1 = 0, 
                X2 = _currentMousePosition.X, 
                Y2 = cnvMainCanvas.Height, Stroke = Brushes.Black, StrokeThickness = 0.25 };

            // creates the crosshairs for point selection
            _crosshairHorizontal = new Line
            {
                Y1 = _currentMousePosition.Y,
                X1 = 0,
                Y2 = _currentMousePosition.Y,
                X2 = cnvMainCanvas.Width,
                Stroke = Brushes.Black,
                StrokeThickness = 0.25
            };


            // Create a some test data
            // Calculator._diaphragm_system.AddDiaphragm(new DiaphragmData_Rectangular(new Point(50, 50), new Point(100, 100)));
            //            Calculator._diaphragm_system.AddDiaphragm(new DiaphragmData_Rectangular(new Point(100, 100), new Point(40, 60)));

            Update();
        }

        public void CreatePreviewShape()
        {
            Shape shape = null;
            PreviewObjects.Clear();

            if(_currentPreviewLine == null)
            {
                return;
            }   

            switch (CurrentInputMode)
            {
                case InputModes.None:
                    //no input mode so do nothing
                    return;
                case InputModes.Rigidity:
                    // draw preview as a line
                    shape = _currentPreviewLine;
                    shape.Opacity = 0.3f;
                    PreviewObjects.Add(shape);
                    break;
                case InputModes.Mass:
                    float x1 = (float)_currentPreviewLine.X1;
                    float y1 = (float)_currentPreviewLine.Y1;
                    float x2 = (float)_currentPreviewLine.X2;
                    float y2 = (float)_currentPreviewLine.Y2;

                    // Sort the points in to P1, P2, P3, P4 order
                    ///              
                    /// P4 --- P3
                    /// |       |
                    /// P1 --- P2 
                    /// 
                    Point first_pt = new Point(x1, y1);
                    Point second_pt = new Point(x2, y2);
                    Console.WriteLine($"\nfirst pt: {first_pt}  second pt: {second_pt}");

                    Point P1, P2, P3, P4;

                    // first point is either P1 or P4
                    if (first_pt.X < second_pt.X)
                    {
                        // Cases:
                        // (A) first point is P1 and second point is P3
                        if (first_pt.Y > second_pt.Y)
                        {
                            Console.WriteLine("\nA");
                            P1 = first_pt;
                            P3 = second_pt;

                            P2 = new System.Windows.Point(P3.X, P1.Y);
                            P4 = new System.Windows.Point(P1.X, P3.Y);
                        }
                        // (B) first point is P4 and second point is P2
                        else
                        {
                            Console.WriteLine("\nB");
                            P2 = second_pt;
                            P4 = first_pt;

                            P1 = new System.Windows.Point(P4.X, P2.Y);
                            P3 = new System.Windows.Point(P2.X, P4.Y);
                        }
                    }

                    // first point is either P2 or P3
                    else
                    {
                        // Cases:
                        // (A) first point is P2 and second point is P4
                        if (first_pt.Y > second_pt.Y)
                        {
                            Console.WriteLine("\nC");
                            P2 = first_pt;
                            P4 = second_pt;

                            P1 = new System.Windows.Point(P4.X, P2.Y);
                            P3 = new System.Windows.Point(P2.X, P4.Y);
                        }
                        // (B) first point is P3 and second point is P1
                        else
                        {
                            Console.WriteLine("\nD");
                            P1 = second_pt;
                            P3 = first_pt;

                            P2 = new System.Windows.Point(P3.X, P1.Y);
                            P4 = new System.Windows.Point(P1.X, P3.Y);
                        }
                    }

                    // the rectangular region object
                    shape = new Rectangle
                    {
                        Width = Math.Abs(P2.X-P1.X),
                        Height = Math.Abs(P4.Y-P1.Y),
                        Fill = Brushes.Green,
                        Stroke = Brushes.Green,
                        StrokeThickness = rect_boundary_line_thickness,
                        Opacity = 0.3f
                    };
                    Console.WriteLine($"P1: {P1}  P2: {P2}  P3: {P3}  P4: {P4}");
                    Console.WriteLine($"\nX: {P1.X} Y: {P1.Y}   Width: {shape.Width}   Height: {shape.Height}"  );
                    Canvas.SetLeft(shape, P4.X);
                    Canvas.SetTop(shape, P4.Y);
                    PreviewObjects.Add(shape);

                    break;
                default:
                    throw new NotImplementedException("Unknown input mode in CreatePreviewShape: " + CurrentInputMode.ToString());
                    return;
            }
        }

        /// <summary>
        /// Helper function to draw a rectangle with a border and a center point and add it to our list of structural objects to be drawn
        /// </summary>
        /// <param name="left"></param>
        /// <param name="top"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="fill"></param>
        private void AddRectangleWithBorderAndCenter(double left, double top, double width, double height, Brush fill, float opacity = 1.0f)
        { 
            // the rectangular region object
            Rectangle rect = new Rectangle
            {
                Width = width,
                Height = height,
                Fill = fill,
                Stroke = fill,
                StrokeThickness = rect_boundary_line_thickness,
                Opacity = opacity
            };
            Canvas.SetLeft(rect, left);
            Canvas.SetTop(rect, top);
            StructuralObjects.Add(rect);

            // marker for center of the rectangle -- center of area / mass
            Ellipse centerCircle = new Ellipse { Width = 4, Height = 4, Fill = fill, Opacity = opacity };
            Canvas.SetLeft(centerCircle, left + width / 2 - 2.0);
            Canvas.SetTop(centerCircle, top + height / 2 - 2.0);
            StructuralObjects.Add(centerCircle);
        }

        /// <summary>
        /// Draw the bounding box of the elements on the canvas
        /// </summary>
        private void DrawBoundingBox()
        {
            // if no calculator defined, cancel this operation
            if (Calculator == null)
            {
                return;
            }

            // retrieve the bounding box points fro the calculator (in World Coordinates_
            // --bb_min_pt is the lower left
            // --bb_max_pt is the upper right
            System.Windows.Point bb_min_pt = Calculator.Boundary_Min_Point;
            System.Windows.Point bb_max_pt = Calculator.Boundary_Max_Point;

            // convert the bounding box points to screen coordinates
            System.Windows.Point screen_bb_min_pt = WorldCoord_ToScreen(bb_min_pt);
            System.Windows.Point screen_bb_max_pt = WorldCoord_ToScreen(bb_max_pt);

            // retrieve the min and max values for the x and y coordinates of the bounding box
            float screen_bb_left   = (float)screen_bb_min_pt.X;  // x-min
            float screen_bb_top    = (float)screen_bb_max_pt.Y;  // y-max
            float screen_bb_right  = (float)screen_bb_max_pt.X;  // x-max
            float screen_bb_bottom = (float)screen_bb_min_pt.Y;  // y- max

            if (Calculator._diaphragm_system._diaphragms.Count > 0 || Calculator._wall_system._walls.Count > 0)
            {
                Line topLine = new Line { X1 = screen_bb_left, Y1 = screen_bb_top, X2 = screen_bb_right, Y2 = screen_bb_top, Stroke = Brushes.Black, StrokeThickness = 2 * rect_boundary_line_thickness, StrokeDashArray = new DoubleCollection { 1, 1 } };
                Line bottomLine = new Line { X1 = screen_bb_left, Y1 = screen_bb_bottom, X2 = screen_bb_right, Y2 = screen_bb_bottom, Stroke = Brushes.Black, StrokeThickness = 2 * rect_boundary_line_thickness, StrokeDashArray = new DoubleCollection { 1, 1 } };
                Line leftLine = new Line { X1 = screen_bb_left, Y1 = screen_bb_top, X2 = screen_bb_left, Y2 = screen_bb_bottom, Stroke = Brushes.Black, StrokeThickness = 2 * rect_boundary_line_thickness, StrokeDashArray = new DoubleCollection { 1, 1 } };
                Line rightLine = new Line { X1 = screen_bb_right, Y1 = screen_bb_top, X2 = screen_bb_right, Y2 = screen_bb_bottom, Stroke = Brushes.Black, StrokeThickness = 2 * rect_boundary_line_thickness, StrokeDashArray = new DoubleCollection { 1, 1 } };

                // add the lines to the canvas control
                cnvMainCanvas.Children.Add(topLine);
                cnvMainCanvas.Children.Add(bottomLine);
                cnvMainCanvas.Children.Add(leftLine);
                cnvMainCanvas.Children.Add(rightLine);

                // update the bounding box limits display on the UI
                lblXMin.Content = screen_bb_left.ToString("0.00");
                lblYMin.Content = screen_bb_bottom.ToString("0.00");
                lblXMax.Content = screen_bb_right.ToString("0.00");
                lblYMax.Content = screen_bb_top.ToString("0.00");
            }
        }

        /// <summary>
        /// Function to handle drawing minor and major gridlines on our canvas
        /// </summary>
        private void CreateGridLines()
        {
            // for the major gridlines
            for (int i = (int)-cnvMainCanvas.Width; i < (int)cnvMainCanvas.Width; i += (int)default_gridline_spacing_major) // Large arbitrary bounds
            {
                // draw the major gridlines
                Line verticalLine = new Line { X1 = i, Y1 = 0, X2 = i, Y2 = cnvMainCanvas.Height, Stroke = Brushes.DarkGray, StrokeDashArray = new DoubleCollection { 2, 2 }, StrokeThickness = 0.4 };
                Line horizontalLine = new Line { X1 = 0, Y1 = i, X2 = cnvMainCanvas.Width, Y2 = i, Stroke = Brushes.DarkGray, StrokeDashArray = new DoubleCollection { 2, 2 }, StrokeThickness = 0.4 };
                CanvasDetailsObjects.Add(verticalLine);
                CanvasDetailsObjects.Add(horizontalLine);
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
                CanvasDetailsObjects.Add(verticalLine);
                CanvasDetailsObjects.Add(horizontalLine);
            }
        }


        /// <summary>
        /// Create the drawing objects (in canvas screen coordinates) from the structural model objects in world coordinates
        /// </summary>
        public void CreateStructuralObjects()
        {
            // Loop through the wall system in the model and create drawing objects for the UI in screen coordinates
            foreach (var item in Calculator._wall_system._walls)
            {
                WallDirs walldir = item.Value.WallDir;

                // Get the screen coords based on the world coords in the model
                Point canvas_start_pt = WorldCoord_ToScreen(item.Value.Start);
                Point canvas_end_pt = WorldCoord_ToScreen(item.Value.End);

                float canvas_height = (float)(canvas_end_pt.Y - canvas_start_pt.Y);
                float canvas_width = (float)(canvas_end_pt.X - canvas_start_pt.X);

                // for hoirzontal walls
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

                // for vertical walls
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

                // set the width and height for the drawing object
                float width = (walldir == WallDirs.EastWest ? (float)Math.Abs(canvas_width) : 1.0f);
                float height = (walldir == WallDirs.NorthSouth ? (float)Math.Abs(canvas_height) : 1.0f);

                // add the drawing objects to the Canvas
                AddRectangleWithBorderAndCenter(
                    canvas_start_pt.X,
                    canvas_start_pt.Y,
                    width,
                    height,
                    Brushes.Blue,
                    1.0f);
            }

            // Loop through the diaphragm system in the model and create drawing objects for the UI
            foreach (var item in Calculator._diaphragm_system._diaphragms)
            {
                // P3 (upper right) and P1 (lower left) are opposite corners of the diaphragm definition
                // Get the screen coords based on the world coords of the model
                Point p1_canvas_start_pt = WorldCoord_ToScreen(item.Value.P1);
                Point p1_canvas_end_pt = WorldCoord_ToScreen(item.Value.P1);

                Point p3_canvas_start_pt = WorldCoord_ToScreen(item.Value.P3);
                Point p3_canvas_end_pt = WorldCoord_ToScreen(item.Value.P3);

                Point p4_canvas_start_pt = WorldCoord_ToScreen(item.Value.P4);
                Point p4_canvas_end_pt = WorldCoord_ToScreen(item.Value.P4);

                // calculate the width and height of the diaphragm
                float canvas_height = Math.Abs((float)(p3_canvas_end_pt.Y - p1_canvas_start_pt.Y));
                float canvas_width = Math.Abs((float)(p3_canvas_end_pt.X - p1_canvas_start_pt.X));

                // add the rectangle using P4 (upper left) since rectangles are drawn by default from the upper left
                AddRectangleWithBorderAndCenter(
                    p4_canvas_start_pt.X,
                    p4_canvas_start_pt.Y,
                    canvas_width,
                    canvas_height,
                    Brushes.Red,
                    0.5f);
            }
        }

        /// <summary>
        /// Draw the non model canvas details 
        /// Use the function to add precalculated items to the canvas -- eliminates continuous recalculation
        /// -- gridlines
        /// </summary>
        public void DrawCanvasDetails()
        {
            if (CanvasDetailsObjects == null || CanvasDetailsObjects.Count == 0)
            {
                return;
            }
            foreach (var item in CanvasDetailsObjects)
            {
                if (item == null) continue;

                if (cnvMainCanvas.Children.Contains(item) != true)
                {
                    cnvMainCanvas.Children.Add(item);
                }
            }
        }
        /// <summary>
        /// Draw the structural model objects
        /// Use this function to add structural model objects to the canvas
        /// -- walls and wall system
        /// -- diaphragms and diaphragm system
        /// </summary>
        public void DrawStructuralObjects()
        {
            if (StructuralObjects == null || StructuralObjects.Count == 0)
            {
                return;
            }
            foreach (var item in StructuralObjects)
            {
                if (item == null) continue;

                if (cnvMainCanvas.Children.Contains(item) != true)
                {
                    cnvMainCanvas.Children.Add(item);
                }
            }
        }

        /// <summary>
        /// Draw the preview shapes for point collection
        /// -- preview cursor image
        /// </summary>
        public void DrawPreviewObjects()
        {
            if(PreviewObjects == null || PreviewObjects.Count == 0)
            {
                return;
            }

            foreach (Shape item in PreviewObjects)
            {
                if (item == null) continue;

                if(cnvMainCanvas.Children.Contains(item) != true)
                {
                    cnvMainCanvas.Children.Add(item);
                }
            }
        }

        /// <summary>
        /// The primary draw functions for items on the canvas and the summary controls
        /// </summary>
        /// <exception cref="Exception"></exception>
        /// <exception cref="NotImplementedException"></exception>
        private void DrawResults()
        {
            // No calculator?  Nothing to draw so return
            if (Calculator == null)
            {
                return;
            }

            //// Clear the MainCanvas before redrawing.
            cnvMainCanvas.Children.Clear();

            // Draw the gridlines and other non-model objects on the canvas
            DrawCanvasDetails();

            // draw the structural model objects -- do this before any added details and highlights
            DrawStructuralObjects();

            // draw the bounding box around all structural model objects
            DrawBoundingBox();

            // Draw the preview object (line or rectangle)
            if (_currentPreviewLine != null)
            {
                CreatePreviewShape();
                //if (cnvMainCanvas.Children.Contains(_currentPreviewLine) != true)
                //{
                //    cnvMainCanvas.Children.Add(_currentPreviewLine);
                //}
            }
            DrawPreviewObjects();

            // Draw extra information for walls -- label numbers etc.
            DrawWallsInfo();

            // Draw extra information for diaphragms -- label numbers etc.
            DrawDiaphragmsInfo();

            // Draw center of mass and center of rigidity
            DrawCOMandCOR();

            // Draw crosshairs
            cnvMainCanvas.Children.Add(_crosshairVertical);
            cnvMainCanvas.Children.Add(_crosshairHorizontal);




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

            // Create the diaphragm data controls
            DiaphragmData.Children.Clear();  // clear the stack panel controls for the diaphragm data
            foreach (var diaphragm in Calculator._diaphragm_system._diaphragms)
            {
                int id = diaphragm.Key;
                DiaphragmData_Rectangular dia = diaphragm.Value;

                DiaphragmDataControl control = new DiaphragmDataControl(id, dia);

                DiaphragmData.Children.Add(control);

                control.DeleteDiaphragm += OnDiaphragmDeleted;

            }

            //// Create Table of Results
            if (Calculator.GetType() == typeof(ShearWallCalculator_RigidDiaphragm))
            {

                // Create Table of Wall Data
                ShearWallData_EW.Children.Clear(); // clear the stack panel controls for the wall data
                ShearWallData_NS.Children.Clear(); // clear the stack panel controls for the wall data
                ShearWallResults.Children.Clear(); // clear the stack panel controls for the calculation results data

                foreach (var result in ((ShearWallCalculator_RigidDiaphragm)Calculator).TotalWallShear)
                {
                    int id = result.Key;
                    float rigidity = ((ShearWallCalculator_RigidDiaphragm)Calculator)._wall_system.EW_Walls.ContainsKey(id) ? ((ShearWallCalculator_RigidDiaphragm)Calculator)._wall_system.EW_Walls[id].WallRigidity : Calculator._wall_system.NS_Walls[id].WallRigidity;
                    float direct_shear_x = ((ShearWallCalculator_RigidDiaphragm)Calculator).DirectShear_X.ContainsKey(id) ? ((ShearWallCalculator_RigidDiaphragm)Calculator).DirectShear_X[id] : 0.0f;
                    float direct_shear_y = ((ShearWallCalculator_RigidDiaphragm)Calculator).DirectShear_Y.ContainsKey(id) ? ((ShearWallCalculator_RigidDiaphragm)Calculator).DirectShear_Y[id] : 0.0f;
                    float eccentric_shear = ((ShearWallCalculator_RigidDiaphragm)Calculator).EccentricShear.ContainsKey(id) ? ((ShearWallCalculator_RigidDiaphragm)Calculator).EccentricShear[id] : 0.0f;
                    float total_shear = ((ShearWallCalculator_RigidDiaphragm)Calculator).TotalWallShear.ContainsKey(id) ? ((ShearWallCalculator_RigidDiaphragm)Calculator).TotalWallShear[id] : 0.0f;

                    ShearWallResultsControl control = new ShearWallResultsControl(
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
                    ShearWallDataControl control = new ShearWallDataControl(id, wall);

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
                // Create Table of Wall Data
                ShearWallData_EW.Children.Clear();
                ShearWallData_NS.Children.Clear();
                ShearWallResults.Children.Clear();

                throw new NotImplementedException("\nFlexible Diaphragms Calculator not implemented yet.");
            }
            else
            {
                throw new NotImplementedException("\nInvalid Calaculator type received.");
            }
        }

        /// <summary>
        /// For drawing text labels and other info for walls
        /// </summary>
        private void DrawWallsInfo()
        {
            //Draw the additional wall info including labels for the center point
            if (Calculator._wall_system != null)
            {
                // East-West walls
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
                            6
                            );
                    }
                }
                //North-South walls
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
                            6
                            );
                    }
                }
            }
        }


        /// <summary>
        /// For drawing text labels and other info for walls
        /// </summary>
        private void DrawDiaphragmsInfo()
        {
            //Draw the additional wall info including labels for the center point
            if (Calculator._diaphragm_system != null)
            {
                // East-West walls
                if (Calculator._diaphragm_system._diaphragms.Count > 0)
                {
                    foreach (var diaphragm in Calculator._diaphragm_system._diaphragms)
                    {
                        System.Windows.Point cp = WorldCoord_ToScreen(diaphragm.Value.Centroid);

                        // number label for the wall ID
                        DrawingHelpersLibrary.DrawingHelpers.DrawText
                            (
                            cnvMainCanvas,
                            cp.X + 1,
                            cp.Y + 1,
                            0,
                            diaphragm.Key.ToString(),
                            System.Windows.Media.Brushes.Red,
                            6
                            );
                    }
                }
            }
        }

        /// <summary>
        /// Handles drawing the Center of Mass and Center of Rigidity points
        /// </summary>
        private void DrawCOMandCOR()
        {
            // Draw the Center of Rigiidity Point
            System.Windows.Point cor_pt = WorldCoord_ToScreen(Calculator._wall_system.CtrRigidity);
            if (Calculator._wall_system != null)
            {
                DrawingHelpersLibrary.DrawingHelpers.DrawCircle(
                    cnvMainCanvas,
                    cor_pt.X,
                    cor_pt.Y,
                    System.Windows.Media.Brushes.Blue,
                    System.Windows.Media.Brushes.Blue,
                    7,
                    0.5);
                DrawingHelpersLibrary.DrawingHelpers.DrawText(
                    cnvMainCanvas,
                    cor_pt.X + 1.5,
                    cor_pt.Y - 10,
                    0,
                    "C.R",
                    System.Windows.Media.Brushes.Black,
                    6
                    );
            }

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
                    7,
                    1);
                DrawingHelpersLibrary.DrawingHelpers.DrawText(
                    cnvMainCanvas,
                    p1.X + 1.5,
                    p1.Y + 2,
                    0,
                    "C.M",
                    System.Windows.Media.Brushes.Black,
                    6
                    );
            }
        }

        private void OnDiaphragmDeleted(object sender, DiaphragmDataControl.DeleteDiaphragmEventArgs e)
        {
            if (Calculator._diaphragm_system._diaphragms.ContainsKey(e.Id) == true)
            {
                Calculator._diaphragm_system._diaphragms.Remove(e.Id);

                MessageBox.Show(e.Id.ToString() + " has been deleted");
            }
            else
            {
                MessageBox.Show(e.Id.ToString() + " does not exist in Walls");
            }

            Update();
        }

        private void OnWallDeleted(object sender, ShearWallDataControl.DeleteWallEventArgs e)
        {
            if (Calculator._wall_system._walls.ContainsKey(e.Id) == true)
            {
                Calculator._wall_system._walls.Remove(e.Id);

                MessageBox.Show(e.Id.ToString() + " has been deleted");
            }
            else
            {
                MessageBox.Show(e.Id.ToString() + " does not exist in Walls");
            }

            Update();
        }

        private void Update()
        {
            // Update the new calculator
            Calculator.Update();

            cnvMainCanvas.Children.Clear(); // clear the canvas 

            // Recompute the structural objects to be drawn
            StructuralObjects.Clear();
            CreateStructuralObjects();

            // Update the crosshairs
            _crosshairVertical.X1 = _currentMousePosition.X;
            _crosshairVertical.X2 = _currentMousePosition.X;

            _crosshairHorizontal.Y1 = _currentMousePosition.Y;
            _crosshairHorizontal.Y2 = _currentMousePosition.Y;

            CreatePreviewShape();

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
            // Right button click to CANCEL
            if (e.RightButton == MouseButtonState.Pressed)
            {
                ResetPointInputInfo();
                Update();
                return;
            }

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

                    // The first click
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

                    // This is the second click
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


                    // Both points have been clicked
                    if (_startClickSet && _endClickSet)
                    {
                        // process the canvas screen coords clicked
                        float start_x = ((float)CurrentStartPoint.X);
                        float start_y = ((float)CurrentStartPoint.Y);
                        float end_x = ((float)CurrentEndPoint.X);
                        float end_y = ((float)CurrentEndPoint.Y);

                        // Set the preview line values
                        _currentPreviewLine.X1 = start_x;
                        _currentPreviewLine.Y1 = start_y;
                        _currentPreviewLine.X2 = end_x;
                        _currentPreviewLine.Y2 = end_y;

                        if (CurrentInputMode == InputModes.Rigidity)
                        {
                            // Determine if this wall segment should be horizontal or vertical by looking at the difference between the x
                            // and y coordinates of the start and end points.  Whichever difference is larger will be the direction
                            //  -- larger X direction = horizontal
                            //  -- larger Y direction = vertical
                            // this function draws the corresponding line through the center point of the actual line
                            WallDirs dir = WallDirs.EastWest;
                            if (LineIsHorizontal(_currentPreviewLine))
                            {
                                dir = WallDirs.EastWest;

                                // move the y-coords of the end points to make the line horizontal
                                end_y = start_y;
                            }
                            else
                            {
                                dir = WallDirs.NorthSouth;

                                // move the end point to make the line vertical
                                end_x = start_x;
                            }

                            // create adjusted start and end points based on the new center point location
                            Point adj_start_x = new Point(start_x, start_y);
                            Point adj_end_x = new Point(end_x, end_y);

                            // convert screen coords to world coords to make the true wall structure
                            Point world_p1 = ScreenCoord_ToWorld(adj_start_x);
                            Point world_p2 = ScreenCoord_ToWorld(adj_end_x);

                            // Add to the list of wall segments
                            Calculator._wall_system.AddWall(new WallData(DEFAULT_WALL_HT, 
                                (float)world_p1.X, (float)world_p1.Y, (float)world_p2.X, (float)world_p2.Y, dir));

                            _currentPreviewLine = null;  // clear the preview line
                            status = "Wall added";
                            Update();
                        }
                        else if (CurrentInputMode == InputModes.Mass)
                        {
                            // Create a diaphragm section by dragging opposite corners of a rectangular region
                            Point world_p1 = ScreenCoord_ToWorld(CurrentStartPoint);
                            Point world_p2 = ScreenCoord_ToWorld(CurrentEndPoint);

                            // Add to the list of diaphragm segments
                            Calculator._diaphragm_system.AddDiaphragm(new DiaphragmData_Rectangular(world_p1, world_p2));
                            _currentPreviewLine = null;  // clear the preview line
                            status = "Diaphragm added";
                            Update();
                        }
                        else
                        {
                            throw new Exception("Invalid input mode -- should be rigidity or mass mode.");
                        }

                        // then clear the variables.
                        _startClickSet = false;
                        _endClickSet = false;
                        CurrentStartPoint = new Point();
                        CurrentEndPoint = new Point();
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
            _currentMousePosition = Mouse.GetPosition(cnvMainCanvas);

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

                    // force the line to be horizontal or vertical only
                    if (LineIsHorizontal(_currentPreviewLine))
                    {
                        _currentPreviewLine.Y2 = _currentPreviewLine.Y1;
                    } else
                    {
                        _currentPreviewLine.X2 = _currentPreviewLine.X1; 
                    }
                }

                // Update the mouse position label
                MousePosition.Content = "(" + _currentMousePosition.X.ToString("0.00") + ", " + _currentMousePosition.Y.ToString("0.00") + ")";

                Update();
            }

        }

        /// <summary>
        /// Function to determine if a line object is more horizontal than vertical by comparing the x and y distances between the start and end points
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private bool LineIsHorizontal(Line line)
        {
            if (line == null) return false;

            return (Math.Abs(line.X1 - line.X2) > Math.Abs(line.Y1 - line.Y2));
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
                case Key.Escape:
                    ResetPointInputInfo();
                    break;
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
        /// Function to handle clearing point and cursor input variables
        /// </summary>
        private void ResetPointInputInfo()
        {
            _startClickSet = false;
            _endClickSet = false;
            CurrentStartPoint = new Point();
            CurrentEndPoint = new Point();
            _currentPreviewLine = null;

            // update the infor labels
            lblScreenEndCoord.Content = "";
            lblScreenStartCoord.Content = "";
            lblWorldEndCoord.Content = "";
            lblWorldStartCoord.Content = "";
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
