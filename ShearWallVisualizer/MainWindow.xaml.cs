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
using ShearWallVisualizer.Helpers;

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
        private CanvasDrawer _canvas_drawer;

        // constants for default values
        private float DEFAULT_WALL_HT = 9;  // ft
        private float DEFAULT_GRIDLINE_SPACING_MAJOR = 10;  // the spacing of the major gridlines -- measured in feet
        private float DEFAULT_GRIDLINE_SPACING_MINOR = 5;  // the spacing of the minor gridlines -- measured in feet

        // constants for canvas dimensions
        private float DEFAULT_CANVAS_WIDTH = 400; // width of the canvas in pixels
        private float DEFAULT_CANVAS_HEIGHT = 400; // height of the canvas in pixels

        private float DEFAULT_MODEL_EXITENTS_HORIZONTAL = 100; // the horizontal extents of the model in feet
        private float DEFAULT_MODEL_EXITENTS_VERTICAL = 100; // the vertical extents of the model in feet

        // Containers for Canvas drawing objects
        private List<Shape> StructuralObjects = new List<Shape>(); // a list of the structural (non-canvas drawing details) objects

        private List<Shape> CanvasDetailsObjects = new List<Shape>(); // a list of objects drawn on the canvas but not needed to be recomputed frequently.
        private List<Shape> PreviewObjects = new List<Shape>(); // a list of objects containing any temporary preview objects to be drawn tocanvas

        private double _canvas_height = 1.0;
        private double _canvas_width = 1.0;


        Brush _crosshair_color { get; set; } = new SolidColorBrush(Colors.Black);

        private Shape _PreviewShape = null;
        private Line _currentPreviewLine = null; // contains the points for first and second selection -- stored as a line object
        public System.Windows.Point CurrentStartPoint { get; set; } // contains the current set canvas start point for walls and diaphragms
        public System.Windows.Point CurrentEndPoint { get; set; } // contains the current set canvas end point for walls and diaphragms

        // current mouse position
        private Point _currentMousePosition = new Point();

        // variables for controlling canvas zooming and panning
        private ScaleTransform _scaleTransform;
        private double _zoomFactor = 1.025;  // amount of zoom when middle mouse button is scrolled
        private Point _lastMousePosition; // last mouse position in canvas coords -- used for panning
        private bool _isPanning; // flag for panning state
        private TransformGroup _transformGroup;
        private TranslateTransform _translateTransform;
        private double SCALE_X = 1;  // scale factor for x-dir
        private double SCALE_Y = 1;  // scale factor for y-dir
        private double TRANS_X = 0;  // translation offset for x-dir
        private double TRANS_Y = 0;  // translation offset for y-dir

        // variables for handling input
        private InputModes CurrentInputMode { get; set; } = InputModes.None;
        private bool _startClickSet = false;
        private bool _endClickSet = false;

        // snap mode toggles
        private bool _shouldSnapToNearest = false;
        private float _snapDistance = 10;  // distance in feet to check snap
        private Point _nearestDiaphragmCornerPoint = new Point();


        /// <summary>
        /// The main object that contains the structural model information and the calculations
        /// </summary>
        private ShearWallCalculatorBase Calculator { get; set; } = null; // the main source of our calculations

        /// <summary>
        /// Default constructor
        /// </summary>
        public MainWindow()
        {
            InitializeComponent(); 

            // create our Calculator object 
            // TODO: This will need to be switchable between RigidDiaphragm and FlexibileDiaphragm or both
            Calculator = new ShearWallCalculator_RigidDiaphragm();

            // setup the canvas
            SetCanvasDimensions(DEFAULT_CANVAS_WIDTH, DEFAULT_CANVAS_HEIGHT);

            // set scroll viewer dimensions to intially be slight larger than the canvas
            CanvasScrollViewer.Width = DEFAULT_CANVAS_WIDTH + 50.0f;
            CanvasScrollViewer.Height = DEFAULT_CANVAS_HEIGHT + 50.0f;

            // setup the initial canvas scaling
            SetupViewScaling(1.0f, 1.0f); // scale it the first time to 1:1
            if (cnvMainCanvas.Width / DEFAULT_MODEL_EXITENTS_HORIZONTAL != SCALE_X || cnvMainCanvas.Height / DEFAULT_MODEL_EXITENTS_VERTICAL != SCALE_Y)
            {
                SCALE_X = cnvMainCanvas.Width / DEFAULT_MODEL_EXITENTS_HORIZONTAL;
                SCALE_Y = cnvMainCanvas.Height / DEFAULT_MODEL_EXITENTS_VERTICAL;
                SetupViewScaling((float)SCALE_X, (float)SCALE_Y); // apply the view scaling
            }

            // Setup the canvas drawer helper
            _canvas_drawer = new CanvasDrawer(cnvMainCanvas, SCALE_X, SCALE_Y);

            // create the gridlines
            CreateGridLines();

            Update();
        }

        private void SetCanvasDimensions(float width, float height)
        {
            _canvas_width = width;
            _canvas_height = height;

            // set the size on the screen
            cnvMainCanvas.Width = width;
            cnvMainCanvas.Height = height;
        }

        private void SetupViewScaling(float scale_x, float scale_y, float trans_x = 0, float trans_y = 0)
        {
            // set up scaling parameters associated with the canvas
            _scaleTransform = new ScaleTransform(scale_x, scale_y);
            _translateTransform = new TranslateTransform(-trans_x, -trans_y);
            _transformGroup = new TransformGroup();
            _transformGroup.Children.Add(_scaleTransform);
            _transformGroup.Children.Add(_translateTransform);
            cnvMainCanvas.RenderTransform = _transformGroup;
        }

        /// <summary>
        /// Function to update the UI -- call this when the calculator or UI needs to be updated
        /// </summary>
        private void Update()
        {
            // Update the new calculator
            Calculator.Update();

            DrawCanvas();
        }

        /// <summary>
        /// The primary draw functions for items on the canvas and the summary controls
        /// </summary>
        /// <exception cref="Exception"></exception>
        /// <exception cref="NotImplementedException"></exception>
        public void DrawCanvas()
        {
            // No calculator?  Nothing to draw so return
            if (Calculator == null)
            {
                return;
            }

            //// Clear the MainCanvas before redrawing.
            cnvMainCanvas.Children.Clear();

            // Draw the gridlines and other non-model objects on the canvas
            _canvas_drawer.DrawCanvasDetails(CanvasDetailsObjects);

            // draw the structural model objects for the diaphragms -- do this before any added details and highlights
            foreach (var diaphragm in Calculator._diaphragm_system._diaphragms)
            {
                _canvas_drawer.DrawDiaphragm(diaphragm.Value);
            }

            if (Calculator._diaphragm_system != null && Calculator._diaphragm_system._diaphragms.Count > 0)
            {
                // Draw extra information for diaphragms -- label numbers etc.
                _canvas_drawer.DrawDiaphragmsInfo(Calculator._diaphragm_system);
            }

            // draw the structural model objects for the walls -- do this before any added details and highlights
            foreach (var wall in Calculator._wall_system._walls)
            {
                _canvas_drawer.DrawWall(wall.Value);
            }

            // draw extra information for walls -- label numbers etc.
            if (Calculator._wall_system != null && Calculator._wall_system._walls.Count > 0)
            {
                _canvas_drawer.DrawWallsInfo(Calculator._wall_system);
            }

            //// draw the bounding box around all structural model objects
            //_canvas_drawer.DrawBoundingBox(Calculator.Boundary_Min_Point, Calculator.Boundary_Max_Point);

            // Draw the preview object (line or rectangle)
            if (_currentPreviewLine != null)
            {
                DrawPreviewShape();
            }


            CenterOfMass.Content = "(" + Calculator._diaphragm_system.CtrMass.X.ToString("0.00") + ", " + Calculator._diaphragm_system.CtrMass.Y.ToString("0.00") + ")";
            CenterOfRigidity.Content = "(" + Calculator._wall_system.CtrRigidity.X.ToString("0.00") + ", " + Calculator._wall_system.CtrRigidity.Y.ToString("0.00") + ")";

            // Draw center of mass and center of rigidity markers
            _canvas_drawer.DrawCOM(Calculator._diaphragm_system);
            _canvas_drawer.DrawCOR(Calculator._wall_system);


            ////// Draw the braced wall line data.
            ////_canvas_drawer.DrawBracedWallLines(Calculator._wall_system);

            // Draw crosshairs
            _canvas_drawer.DrawCrosshairs(
                new Point(
                    Mouse.GetPosition(cnvMainCanvas).X,
                    Mouse.GetPosition(cnvMainCanvas).Y
                ),
                _crosshair_color
                );

            // If snapping mode is enabled, draw the extra markers
            if (_shouldSnapToNearest is true)
            {
                Point crosshair_intersection = new Point(Mouse.GetPosition(cnvMainCanvas).X, Mouse.GetPosition(cnvMainCanvas).Y);

                // add marker at cross hair intersection point
                // marker for center of the rectangle -- center of area / mass
                Ellipse snapCircle = new Ellipse
                {
                    Width = _snapDistance,
                    Height = _snapDistance,
                    StrokeThickness = 1.5f,
                    Stroke = Brushes.Red,
                    Fill = Brushes.Transparent,
                    IsHitTestVisible = false
                };
                Canvas.SetLeft(snapCircle, crosshair_intersection.X - snapCircle.Width / 2.0f);
                Canvas.SetTop(snapCircle, crosshair_intersection.Y - snapCircle.Height / 2.0f);
                cnvMainCanvas.Children.Add(snapCircle);

                // Draw the markers for the diaphragm corners and wall end points
                foreach (DiaphragmData_Rectangular d in Calculator._diaphragm_system._diaphragms.Values)
                {
                    Point p1 = MathHelpers.WorldCoord_ToScreen(cnvMainCanvas.Height, d.P1, SCALE_X, SCALE_Y);
                    Point p2 = MathHelpers.WorldCoord_ToScreen(cnvMainCanvas.Height, d.P2, SCALE_X, SCALE_Y);
                    Point p3 = MathHelpers.WorldCoord_ToScreen(cnvMainCanvas.Height, d.P3, SCALE_X, SCALE_Y);
                    Point p4 = MathHelpers.WorldCoord_ToScreen(cnvMainCanvas.Height, d.P4, SCALE_X, SCALE_Y);

                    _canvas_drawer.DrawCircles(p1, 2.0f, Brushes.Red);
                    _canvas_drawer.DrawCircles(p2, 2.0f, Brushes.Red);
                    _canvas_drawer.DrawCircles(p3, 2.0f, Brushes.Red);
                    _canvas_drawer.DrawCircles(p4, 2.0f, Brushes.Red);
                }

                // Draw the markers for the wall end points
                foreach (WallData d in Calculator._wall_system._walls.Values)
                {
                    Point start = MathHelpers.WorldCoord_ToScreen(cnvMainCanvas.Height, d.Start, SCALE_X, SCALE_Y);
                    Point end = MathHelpers.WorldCoord_ToScreen(cnvMainCanvas.Height, d.End, SCALE_X, SCALE_Y);
                    _canvas_drawer.DrawCircles(start, 2.0f, Brushes.Blue);
                    _canvas_drawer.DrawCircles(end, 2.0f, Brushes.Blue);
                }
            }






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

            //// hide the results controls
            //HideAllDataControls();

            // Create the diaphragm data controls
            if (Calculator._diaphragm_system._diaphragms.Count > 0)
            {
                // turn on the controls stackpanel visibility
                spDiaphragmDataControls.Visibility = Visibility.Visible;

                DiaphragmData.Children.Clear();  // clear the stack panel controls for the diaphragm data
                foreach (var diaphragm in Calculator._diaphragm_system._diaphragms)
                {
                    int id = diaphragm.Key;
                    DiaphragmData_Rectangular dia = diaphragm.Value;

                    DiaphragmDataControl control = new DiaphragmDataControl(id, dia);

                    DiaphragmData.Children.Add(control);

                    control.DeleteDiaphragm += OnDiaphragmDeleted;
                }
            }

            ////// Create Table of Results
            //if (Calculator._wall_system._walls.Count > 0)
            //{
            //    // turn on the results and wall data controls visibility
            //    spWallDataControls.Visibility = Visibility.Visible;
            //    spCalcResultsControls.Visibility = Visibility.Visible;

            //    if (Calculator.GetType() == typeof(ShearWallCalculator_RigidDiaphragm))
            //    {

            //        // Create Table of Wall Data
            //        ShearWallData_EW.Children.Clear(); // clear the stack panel controls for the wall data
            //        ShearWallData_NS.Children.Clear(); // clear the stack panel controls for the wall data
            //        ShearWallResults.Children.Clear(); // clear the stack panel controls for the calculation results data

            //        foreach (var result in ((ShearWallCalculator_RigidDiaphragm)Calculator).TotalWallShear)
            //        {
            //            int id = result.Key;
            //            float rigidity = ((ShearWallCalculator_RigidDiaphragm)Calculator)._wall_system.EW_Walls.ContainsKey(id) ? ((ShearWallCalculator_RigidDiaphragm)Calculator)._wall_system.EW_Walls[id].WallRigidity : Calculator._wall_system.NS_Walls[id].WallRigidity;
            //            float direct_shear_x = ((ShearWallCalculator_RigidDiaphragm)Calculator).DirectShear_X.ContainsKey(id) ? ((ShearWallCalculator_RigidDiaphragm)Calculator).DirectShear_X[id] : 0.0f;
            //            float direct_shear_y = ((ShearWallCalculator_RigidDiaphragm)Calculator).DirectShear_Y.ContainsKey(id) ? ((ShearWallCalculator_RigidDiaphragm)Calculator).DirectShear_Y[id] : 0.0f;
            //            float eccentric_shear = ((ShearWallCalculator_RigidDiaphragm)Calculator).EccentricShear.ContainsKey(id) ? ((ShearWallCalculator_RigidDiaphragm)Calculator).EccentricShear[id] : 0.0f;
            //            float total_shear = ((ShearWallCalculator_RigidDiaphragm)Calculator).TotalWallShear.ContainsKey(id) ? ((ShearWallCalculator_RigidDiaphragm)Calculator).TotalWallShear[id] : 0.0f;

            //            ShearWallResultsControl control = new ShearWallResultsControl(
            //                id,
            //                rigidity,
            //                Calculator._wall_system.X_bar_walls[id],
            //                Calculator._wall_system.Y_bar_walls[id],
            //                direct_shear_x,
            //                direct_shear_y,
            //                eccentric_shear,
            //                total_shear
            //                );

            //            ShearWallResults.Children.Add(control);
            //        }

            //        /// Create the shearwall data controls
            //        foreach (var result in ((ShearWallCalculator_RigidDiaphragm)Calculator).TotalWallShear)
            //        {
            //            int id = result.Key;
            //            WallData wall = Calculator._wall_system.EW_Walls.ContainsKey(id) ? Calculator._wall_system.EW_Walls[id] : Calculator._wall_system.NS_Walls[id];
            //            ShearWallDataControl control = new ShearWallDataControl(id, wall);

            //            if (wall.WallDir == WallDirs.EastWest)
            //            {
            //                ShearWallData_EW.Children.Add(control);
            //            }
            //            else if (wall.WallDir == WallDirs.NorthSouth)
            //            {
            //                ShearWallData_NS.Children.Add(control);
            //            }
            //            else
            //            {
            //                throw new Exception("Invalid wall direction " + wall.WallDir.ToString() + " in wall #" + id.ToString());
            //            }

            //            control.DeleteWall += OnWallDeleted;
            //        }
            //    }
            //    else if (Calculator.GetType() == typeof(ShearWallCalculator_FlexibleDiaphragm))
            //    {
            //        // Create Table of Wall Data
            //        ShearWallData_EW.Children.Clear();
            //        ShearWallData_NS.Children.Clear();
            //        ShearWallResults.Children.Clear();

            //        throw new NotImplementedException("\nFlexible Diaphragms Calculator not implemented yet.");
            //    }
            //    else
            //    {
            //        throw new NotImplementedException("\nInvalid Calaculator type received.");
            //    }
            //}
        }

        #region Creating objects for the UI
        /// <summary>
        /// Function that creates the appropriate previewe shap for inputting the point selections
        /// -- wall point selection creates a line
        /// -- diaphragm point selection creates a rectangle
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public void DrawPreviewShape()
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

                    Point start = new Point((float)_currentPreviewLine.X1, (float)_currentPreviewLine.Y1);
                    Point end = new Point((float)_currentPreviewLine.X2, (float)_currentPreviewLine.Y2);

                    // Add dimension text
                    TextBlock text = new TextBlock();
                    if (MathHelpers.LineIsHorizontal(_currentPreviewLine))
                    {
                        text.Text = $"{Math.Abs(end.X - start.X):0.0} ft";
                    } else
                    {
                        text.Text = $"{Math.Abs(end.Y - start.Y):0.0} ft";
                    }
                    Canvas.SetTop(text, (start.X + end.X) / 2.0);
                    Canvas.SetTop(text, (start.Y + end.Y) / 2.0);
                    cnvMainCanvas.Children.Add(text);

                    // add marker at first point
                    // marker for center of the rectangle -- center of area / mass
                    Ellipse centerCircle = new Ellipse { Width = 6, Height = 6, Fill = Brushes.Green, Opacity = 0.4f };
                    Canvas.SetLeft(centerCircle, start.X - centerCircle.Width / 2.0f);
                    Canvas.SetTop(centerCircle, start.Y - centerCircle.Height / 2.0f);
                    cnvMainCanvas.Children.Add(centerCircle);
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
                    Point P1, P2, P3, P4;

                    // first point is either P1 or P4
                    if (first_pt.X < second_pt.X)
                    {
                        // Cases:
                        // (A) first point is P1 and second point is P3
                        if (first_pt.Y > second_pt.Y)
                        {
                            P1 = first_pt;
                            P3 = second_pt;

                            P2 = new System.Windows.Point(P3.X, P1.Y);
                            P4 = new System.Windows.Point(P1.X, P3.Y);
                        }
                        // (B) first point is P4 and second point is P2
                        else
                        {
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
                            P2 = first_pt;
                            P4 = second_pt;

                            P1 = new System.Windows.Point(P4.X, P2.Y);
                            P3 = new System.Windows.Point(P2.X, P4.Y);
                        }
                        // (B) first point is P3 and second point is P1
                        else
                        {
                            P1 = second_pt;
                            P3 = first_pt;

                            P2 = new System.Windows.Point(P3.X, P1.Y);
                            P4 = new System.Windows.Point(P1.X, P3.Y);
                        }
                    }

                    // the rectangular region object
                    shape = new Rectangle
                    {
                        Width = Math.Abs(P2.X - P1.X),
                        Height = Math.Abs(P4.Y - P1.Y),
                        Fill = Brushes.Green,
                        Stroke = Brushes.Green,
                        StrokeThickness = _canvas_drawer.rect_boundary_line_thickness,
                        Opacity = 0.3f,
                        IsHitTestVisible = false
                    };
                    Canvas.SetLeft(shape, P4.X);
                    Canvas.SetTop(shape, P4.Y);
                    cnvMainCanvas.Children.Add(shape);

                    // add marker at first point
                    // marker for center of the rectangle -- center of area / mass
                    centerCircle = new Ellipse { Width = 6, Height = 6, Fill = Brushes.Green, Opacity = 0.4f, IsHitTestVisible = false };
                    Canvas.SetLeft(centerCircle, first_pt.X - centerCircle.Width / 2.0f);
                    Canvas.SetTop(centerCircle, first_pt.Y - centerCircle.Height / 2.0f);
                    cnvMainCanvas.Children.Add(centerCircle);

                    // Add dimension text
                    TextBlock text2 = new TextBlock();
                    text2.Text = $"{Math.Abs(P2.X - P1.X):0.0} x {Math.Abs(P4.Y - P1.Y):0.0} ft";
                    text2.IsHitTestVisible = false;
                    Canvas.SetTop(text2, (P1.X + P3.X) / 2.0);
                    Canvas.SetTop(text2, (P1.Y + P3.Y) / 2.0);
                    cnvMainCanvas.Children.Add(text2);
                    break;
                default:
                    throw new NotImplementedException("Unknown input mode in DrawPreviewShape: " + CurrentInputMode.ToString());
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
                StrokeThickness = _canvas_drawer.rect_boundary_line_thickness,
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
        /// Function to handle drawing minor and major gridlines on our canvas
        /// </summary>
        private void CreateGridLines()
        {
            // limits of the drawing in model space
            System.Windows.Point bb_min_pt = new System.Windows.Point(-0.5 * DEFAULT_MODEL_EXITENTS_HORIZONTAL, -0.5 * DEFAULT_MODEL_EXITENTS_VERTICAL);
            System.Windows.Point bb_max_pt = new System.Windows.Point(1.5 * DEFAULT_MODEL_EXITENTS_HORIZONTAL, 1.5 * DEFAULT_MODEL_EXITENTS_VERTICAL);

            //if (Calculator != null)
            //{
            //    bb_min_pt = Calculator.Boundary_Min_Point;
            //    bb_max_pt = Calculator.Boundary_Max_Point;
            //}

            // for the vertical major gridlines
            for (int i = (int)bb_min_pt.Y; i < (int)bb_max_pt.Y; i += (int)DEFAULT_GRIDLINE_SPACING_MAJOR) // Large arbitrary bounds
            {
                Point p1 = new Point(i, 0);
                Point p2 = new Point(i, bb_max_pt.Y);
                Point scr_p1 = MathHelpers.WorldCoord_ToScreen(cnvMainCanvas.Height, p1, SCALE_X, SCALE_Y);
                Point scr_p2 = MathHelpers.WorldCoord_ToScreen(cnvMainCanvas.Height, p2, SCALE_X, SCALE_Y);

                // create the vertical major gridlines in screen coords
                Line verticalLine = new Line { X1 = p1.X, Y1 = p1.Y, X2 = p2.X, Y2 = p2.Y, Stroke = Brushes.DarkGray, StrokeDashArray = new DoubleCollection { 2, 2 }, StrokeThickness = 0.2 };
                CanvasDetailsObjects.Add(verticalLine);
            }

            // for the vertical minor gridlines
            for (int i = (int)bb_min_pt.Y; i < (int)bb_max_pt.Y; i += (int)DEFAULT_GRIDLINE_SPACING_MINOR) // Large arbitrary bounds
            {
                Point p1 = new Point(i, 0);
                Point p2 = new Point(i, bb_max_pt.Y);
                Point scr_p1 = MathHelpers.WorldCoord_ToScreen(cnvMainCanvas.Height, p1, SCALE_X, SCALE_Y);
                Point scr_p2 = MathHelpers.WorldCoord_ToScreen(cnvMainCanvas.Height, p2, SCALE_X, SCALE_Y);

                // check if we already have a major gridline by detemining if i is a multiple of the major gridline spacing
                if (i % DEFAULT_GRIDLINE_SPACING_MAJOR == 0)
                {
                    continue;
                }

                // draw the minor gridlines
                Line verticalLine = new Line { X1 = p1.X, Y1 = p1.Y, X2 = p2.X, Y2 = p2.Y, Stroke = Brushes.DarkGray, StrokeDashArray = new DoubleCollection { 2, 2 }, StrokeThickness = 0.1 };
                CanvasDetailsObjects.Add(verticalLine);
            }

            // for the horizontal major gridlines
            for (int i = (int)bb_min_pt.X; i < (int)bb_max_pt.X; i += (int)DEFAULT_GRIDLINE_SPACING_MAJOR) // Large arbitrary bounds
            {
                Point p1 = new Point(0, i);
                Point p2 = new Point(bb_max_pt.X, i);
                Point scr_p1 = MathHelpers.WorldCoord_ToScreen(cnvMainCanvas.Height, p1, SCALE_X, SCALE_Y);
                Point scr_p2 = MathHelpers.WorldCoord_ToScreen(cnvMainCanvas.Height, p2, SCALE_X, SCALE_Y);

                // draw the major gridlines
                Line horizontalLine = new Line { X1 = p1.X, Y1 = p1.Y, X2 = p2.X, Y2 = p2.Y, Stroke = Brushes.DarkGray, StrokeDashArray = new DoubleCollection { 2, 2 }, StrokeThickness = 0.2 };

                CanvasDetailsObjects.Add(horizontalLine);
            }

            // for the horizontal minor gridlines
            for (int i = (int)bb_min_pt.X; i < (int)bb_max_pt.X; i += (int)DEFAULT_GRIDLINE_SPACING_MINOR) // Large arbitrary bounds
            {

                // check if we already have a major gridline by detemining if i is a multiple of the major gridline spacing
                if (i % DEFAULT_GRIDLINE_SPACING_MAJOR == 0)
                {
                    continue;
                }

                Point p1 = new Point(0, i);
                Point p2 = new Point(bb_max_pt.X, i);
                Point scr_p1 = MathHelpers.WorldCoord_ToScreen(cnvMainCanvas.Height, p1, SCALE_X, SCALE_Y);
                Point scr_p2 = MathHelpers.WorldCoord_ToScreen(cnvMainCanvas.Height, p2, SCALE_X, SCALE_Y);

                // draw the minor gridlines
                Line horizontalLine = new Line { X1 = p1.X, Y1 = p1.Y, X2 = p2.X, Y2 = p2.Y, Stroke = Brushes.DarkGray, StrokeDashArray = new DoubleCollection { 2, 2 }, StrokeThickness = 0.1 };
                CanvasDetailsObjects.Add(horizontalLine);
            }
        }

        #endregion

        #region Controls
        private void HideAllDataControls()
        {
            spCalcResultsControls.Visibility = Visibility.Collapsed;
            spWallDataControls.Visibility = Visibility.Collapsed;
            spDiaphragmDataControls.Visibility = Visibility.Collapsed;
        }

        private void ShowAllDataControls()
        {
            spCalcResultsControls.Visibility = Visibility.Visible;
            spWallDataControls.Visibility = Visibility.Visible;
            spDiaphragmDataControls.Visibility = Visibility.Visible;
        }

        private void ClearCoordinateDisplayData()
        {
            lblScreenEndCoord.Content = "";
            lblScreenStartCoord.Content = "";
            lblWorldEndCoord.Content = "";
            lblWorldStartCoord.Content = "";
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
        #endregion 

        #region Mouse and Window Events
        /// <summary>
        /// What happens when the middle mouse wheel is scrolled
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            Point mousePosition = e.GetPosition(cnvMainCanvas);
            double zoomDelta = e.Delta > 0 ? _zoomFactor : 1 / _zoomFactor;
            double oldScaleX = _scaleTransform.ScaleX;
            double oldScaleY = _scaleTransform.ScaleY;

            SCALE_X = _scaleTransform.ScaleX * zoomDelta;
            SCALE_Y =_scaleTransform.ScaleY * zoomDelta;
            double scaleChangeX = _scaleTransform.ScaleX - oldScaleX;
            double scaleChangeY = _scaleTransform.ScaleY - oldScaleY;
            TRANS_X =(float)(_translateTransform.X - (mousePosition.X * scaleChangeX));
            TRANS_Y = (float)(_translateTransform.Y - (mousePosition.Y * scaleChangeY));
            _translateTransform.Y -= (mousePosition.Y * scaleChangeY);

            SetupViewScaling((float)SCALE_X, (float)SCALE_Y, (float)TRANS_X, (float)TRANS_Y);

            // recreate the canvas drawer with the new zoom factor
            _canvas_drawer = new CanvasDrawer(cnvMainCanvas, SCALE_X, SCALE_Y);

            Update();
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
                DrawCanvas();
                return;
            }

            // Middle mouse button for panning and zooming
            if (e.MiddleButton == MouseButtonState.Pressed)
            {
                _isPanning = true;
                _lastMousePosition = e.GetPosition(CanvasScrollViewer);
                cnvMainCanvas.Cursor = Cursors.Hand;
                return;
            }

            HandleLeftClick(e);
            DrawCanvas();
        }

        private void HandleLeftClick(MouseButtonEventArgs e)
        {
            if(CurrentInputMode != InputModes.Mass && CurrentInputMode != InputModes.Rigidity)
            {
                MessageBox.Show("Please select the 'Mass' or 'Rigidity' input mode.");
                return;
            }

            Point screenPoint = e.GetPosition(cnvMainCanvas);
            Point worldPoint = MathHelpers.ScreenCoord_ToWorld(cnvMainCanvas.Height, screenPoint, SCALE_X, SCALE_Y);

            bool snapResult;
            Point nearestPoint = MathHelpers.FindNearestSnapPoint(Calculator._wall_system, Calculator._diaphragm_system,
                worldPoint, out snapResult);

            if (!_startClickSet)
            {
                _currentPreviewLine = new Line { Stroke = Brushes.Green };

                if (_shouldSnapToNearest && snapResult && MathHelpers.PointIsWithinRange(worldPoint, nearestPoint, _snapDistance))
                {
                    screenPoint = MathHelpers.WorldCoord_ToScreen(cnvMainCanvas.Height, nearestPoint, SCALE_X, SCALE_Y);
                }

                CurrentStartPoint = screenPoint;
                _startClickSet = true;
                _canvas_drawer.DrawPreviewLine(_currentPreviewLine, CurrentStartPoint, CurrentStartPoint);
                return;
            }

            if (_shouldSnapToNearest && snapResult && MathHelpers.PointIsWithinRange(worldPoint, nearestPoint, _snapDistance))
            {
                screenPoint = MathHelpers.WorldCoord_ToScreen(cnvMainCanvas.Height, nearestPoint, SCALE_X, SCALE_Y);
            }

            if (screenPoint == CurrentStartPoint)
            {
                return;
            }

            CurrentEndPoint = screenPoint;
            _endClickSet = true;
            _canvas_drawer.DrawPreviewLine(_currentPreviewLine, CurrentStartPoint, CurrentEndPoint);

            ProcessFinalInput();
        }

        private void ProcessFinalInput()
        {
            if (_startClickSet && _endClickSet)
            {
                float startX = (float)CurrentStartPoint.X;
                float startY = (float)CurrentStartPoint.Y;
                float endX = (float)CurrentEndPoint.X;
                float endY = (float)CurrentEndPoint.Y;

                if (CurrentInputMode == InputModes.Rigidity)
                {
                    WallDirs dir = MathHelpers.LineIsHorizontal(_currentPreviewLine) ? WallDirs.EastWest : WallDirs.NorthSouth;

                    if (dir == WallDirs.EastWest) endY = startY;
                    else endX = startX;

                    Point worldStart = MathHelpers.ScreenCoord_ToWorld(cnvMainCanvas.Height, new Point(startX, startY), SCALE_X, SCALE_Y);
                    Point worldEnd = MathHelpers.ScreenCoord_ToWorld(cnvMainCanvas.Height, new Point(endX, endY),SCALE_X, SCALE_Y);

                    Calculator._wall_system.AddWall(new WallData(DEFAULT_WALL_HT,
                        (float)worldStart.X, (float)worldStart.Y, (float)worldEnd.X, (float)worldEnd.Y, dir));

                    _currentPreviewLine = null;
                }
                else if (CurrentInputMode == InputModes.Mass)
                {
                    Point worldStart = MathHelpers.ScreenCoord_ToWorld(cnvMainCanvas.Height, CurrentStartPoint, SCALE_X, SCALE_Y);
                    Point worldEnd = MathHelpers.ScreenCoord_ToWorld(cnvMainCanvas.Height, CurrentEndPoint, SCALE_X, SCALE_Y);

                    Calculator._diaphragm_system.AddDiaphragm(new DiaphragmData_Rectangular(worldStart, worldEnd));

                    _currentPreviewLine = null;
                }

                ResetPointInputInfo();
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

                cnvMainCanvas.RenderTransform = new TranslateTransform(offsetX + cnvMainCanvas.RenderTransform.Value.OffsetX, offsetY + cnvMainCanvas.RenderTransform.Value.OffsetY);


                //foreach (UIElement element in cnvMainCanvas.Children)
                //{
                //    if (element is FrameworkElement fe)
                //    {
                //        fe.RenderTransform = new TranslateTransform(offsetX + fe.RenderTransform.Value.OffsetX, offsetY + fe.RenderTransform.Value.OffsetY);
                //    }
                //}

                _lastMousePosition = currentPosition;
                Update();
            }

            else
            {
                // Update the preview lines
                if (_currentPreviewLine != null)
                {
                    var p = e.GetPosition(cnvMainCanvas);
                    _currentPreviewLine.X2 = p.X;
                    _currentPreviewLine.Y2 = p.Y;

                    // for wall entry mode, force the preview lines to be horizontal or vertical from the first point clicked
                    if (CurrentInputMode == InputModes.Rigidity)
                    {
                        // force the line to be horizontal or vertical only
                        if (MathHelpers.LineIsHorizontal(_currentPreviewLine))
                        {
                            _currentPreviewLine.Y2 = _currentPreviewLine.Y1;
                        }
                        else
                        {
                            _currentPreviewLine.X2 = _currentPreviewLine.X1;
                        }
                    }
                }

                // Update the mouse position label
                MousePosition.Content = "(" + _currentMousePosition.X.ToString("0.00") + ", " + _currentMousePosition.Y.ToString("0.00") + ")";

            }
            Update();

        }

        /// <summary>
        /// Function to determine if a line object is more horizontal than vertical by comparing the x and y distances between the start and end points
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>

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
                    if (CurrentInputMode == InputModes.Rigidity)
                    {
                        Calculator._wall_system._walls.Clear();
                    }
                    if (CurrentInputMode == InputModes.Mass)
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
                default:
                    break;
            }

            Update();
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
        #endregion

        #region Controls and Button Clicks
        /// <summary>
        /// The SNAP MODE button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSnapToNearest_Click(object sender, RoutedEventArgs e)
        {
            _shouldSnapToNearest = !_shouldSnapToNearest;

            if (_shouldSnapToNearest is true)
            {
                btnSnapToNearest.BorderBrush = Brushes.Black;
                btnSnapToNearest.BorderThickness = new Thickness(3);

                _crosshair_color = new SolidColorBrush(Colors.Red); // change the cross hair colors
            }
            else
            {
                btnSnapToNearest.BorderBrush = Brushes.Transparent;
                btnSnapToNearest.BorderThickness = new Thickness(0);

                _crosshair_color = new SolidColorBrush(Colors.Black);  // change the cross hair colors
            }

            Update();
        }

        /// <summary>
        /// Rigidity (wall) input mode
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnRigidityMode_Click(object sender, RoutedEventArgs e)
        {
            CurrentInputMode = InputModes.Rigidity;
            CurrentMode.Content = "SHEAR WALL ENTRY (RIGIDITY MODE)";
            Update();
        }

        /// <summary>
        /// Mass (diaphragm) input mode
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnMassMode_Click(object sender, RoutedEventArgs e)
        {
            CurrentInputMode = InputModes.Mass;
            CurrentMode.Content = "DIAPHRAGM ENTRY (MASS MODE)";
            Update();
        }
        #endregion
    }
}
