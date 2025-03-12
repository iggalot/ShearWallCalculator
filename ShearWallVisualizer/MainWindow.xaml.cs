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
        private int _project_extents = 300;  // the dimensions of the canvas from one side to the other -- measured in feet
        private float default_gridline_spacing_major = 10;  // the spacing of the major gridlines -- measured in feet
        private float default_gridline_spacing_minor = 5;  // the spacing of the minor gridlines -- measured in feet

        private Line _currentPreviewLine = null;


        // variables for controlling canvas zooming and panning
        //Canvas _canvas = new Canvas();

        private ScaleTransform _scaleTransform;
        private double _zoomFactor = 1.1;
        private Point _lastMousePosition;
        private bool _isPanning;
        private TransformGroup _transformGroup;
        private TranslateTransform _translateTransform;


        // scale factors for the canvas and layout of the visiualizer
        private const double SCALE_X = 5;
        private const double SCALE_Y = 5;
        private float DEFAULT_WALL_HT = 9;  // ft

        private InputModes CurrentInputMode { get; set; } = InputModes.None;
        private bool StartClickIsSet { get; set; } = false;
        private bool EndClickIsSet { get; set; } = false;

        public System.Windows.Point CurrentStartPoint { get; set; }
        public System.Windows.Point CurrentEndPoint { get; set; }

        public ShearWallCalculatorBase Calculator { get; set; } = null;
        public WallData CurrentWall { get; set; }


        //public Dictionary<int, WallData> Walls { get; set; } = new Dictionary<int, WallData>();
        //public List<System.Windows.Point> DiaphragmPoints { get; set; } = new List<System.Windows.Point>();

        public MainWindow()
        {

            InitializeComponent();

            Calculator = new ShearWallCalculator_RigidDiaphragm();

            _scaleTransform = new ScaleTransform(SCALE_X, SCALE_Y);
            _translateTransform = new TranslateTransform();
            _transformGroup = new TransformGroup();
            _transformGroup.Children.Add(_scaleTransform);
            _transformGroup.Children.Add(_translateTransform);
            MainCanvas.RenderTransform = _transformGroup;

            Update();
        }

        private void MainCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            //MessageBox.Show("MouseWheel");
            Point mousePosition = e.GetPosition(MainCanvas);
            double zoomDelta = e.Delta > 0 ? _zoomFactor : 1 / _zoomFactor;
            double oldScaleX = _scaleTransform.ScaleX;
            double oldScaleY = _scaleTransform.ScaleY; _scaleTransform.ScaleX *= zoomDelta;
            _scaleTransform.ScaleY *= zoomDelta;
            double scaleChangeX = _scaleTransform.ScaleX - oldScaleX;
            double scaleChangeY = _scaleTransform.ScaleY - oldScaleY;
            _translateTransform.X -= (mousePosition.X * scaleChangeX);
            _translateTransform.Y -= (mousePosition.Y * scaleChangeY);
        }

        
        private void MainCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.MiddleButton == MouseButtonState.Pressed)
            {
                //MessageBox.Show("MouseWheelDown");

                _isPanning = true;
                _lastMousePosition = e.GetPosition(CanvasScrollViewer);
                MainCanvas.Cursor = Cursors.Hand;
            }
        }

        private void MainCanvas_MouseMove(object sender, MouseEventArgs e)
        {

            if (_isPanning && e.MiddleButton == MouseButtonState.Pressed)
            {
                //MessageBox.Show("Panning");
                Point currentPosition = e.GetPosition(CanvasScrollViewer);
                double offsetX = currentPosition.X - _lastMousePosition.X;
                double offsetY = currentPosition.Y - _lastMousePosition.Y;
                CanvasScrollViewer.ScrollToHorizontalOffset(CanvasScrollViewer.HorizontalOffset + offsetX);
                CanvasScrollViewer.ScrollToVerticalOffset(CanvasScrollViewer.VerticalOffset + offsetY);

                foreach (UIElement element in MainCanvas.Children)
                {
                    if (element is FrameworkElement fe)
                    {
                        fe.RenderTransform = new TranslateTransform(offsetX + fe.RenderTransform.Value.OffsetX, offsetY + fe.RenderTransform.Value.OffsetY);
                    }
                }

                _lastMousePosition = currentPosition;
            }
        }

        private void MainCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            //MessageBox.Show("MouseUp");
            if (e.MiddleButton == MouseButtonState.Released)
            {
                _isPanning = false;
                MainCanvas.Cursor = Cursors.Arrow;
            }
        }

        private void AddRectangleWithBorderAndCenter(double left, double top, double width, double height, Brush fill)
        {
            Rectangle rect = new Rectangle { Width = width, Height = height, Fill = fill, Stroke = Brushes.Black, StrokeThickness = 2 };
            Canvas.SetLeft(rect, left);
            Canvas.SetTop(rect, top);
            MainCanvas.Children.Add(rect);
            Ellipse centerCircle = new Ellipse { Width = 5, Height = 5, Fill = Brushes.Black };
            Canvas.SetLeft(centerCircle, left + width / 2 - 2.5);
            Canvas.SetTop(centerCircle, top + height / 2 - 2.5);
            MainCanvas.Children.Add(centerCircle);
        }

        private void DrawBoundingBox()
        {
            Console.WriteLine("Mumber of canvas items: " + MainCanvas.Children.Count);

            double left = 50; double top = 50;
            double right = 300;
            double bottom = 250;
            Line topLine = new Line { X1 = left, Y1 = top, X2 = right, Y2 = top, Stroke = Brushes.Black, StrokeThickness = 2 };
            Line bottomLine = new Line { X1 = left, Y1 = bottom, X2 = right, Y2 = bottom, Stroke = Brushes.Black, StrokeThickness = 2 };
            Line leftLine = new Line { X1 = left, Y1 = top, X2 = left, Y2 = bottom, Stroke = Brushes.Black, StrokeThickness = 2 };
            Line rightLine = new Line { X1 = right, Y1 = top, X2 = right, Y2 = bottom, Stroke = Brushes.Black, StrokeThickness = 2 };
            MainCanvas.Children.Add(topLine);
            MainCanvas.Children.Add(bottomLine);
            MainCanvas.Children.Add(leftLine);
            MainCanvas.Children.Add(rightLine);

            Console.WriteLine("Mumber of canvas items: " + MainCanvas.Children.Count);
        }

        /// <summary>
        /// Function handle drawing minor and major gridlines on our canvas
        /// </summary>
        private void DrawGridLines()
        {
            // for the major gridlines
            for (int i = (int)-MainCanvas.Width; i < (int)MainCanvas.Width; i += (int)default_gridline_spacing_major) // Large arbitrary bounds
            {
                // draw the minor gridlines
                Line verticalLine = new Line { X1 = i, Y1 = 0, X2 = i, Y2 = MainCanvas.Height, Stroke = Brushes.DarkGray, StrokeDashArray = new DoubleCollection { 2, 2 }, StrokeThickness = 0.4 };
                Line horizontalLine = new Line { X1 = 0, Y1 = i, X2 = MainCanvas.Width, Y2 = i, Stroke = Brushes.DarkGray, StrokeDashArray = new DoubleCollection { 2, 2 }, StrokeThickness = 0.4 };
                MainCanvas.Children.Add(verticalLine);
                MainCanvas.Children.Add(horizontalLine);
            }

            // for the minor gridlines
            for (int i = (int)-MainCanvas.Width; i < (int)MainCanvas.Width; i += (int)default_gridline_spacing_minor) // Large arbitrary bounds
            {
                // check if we already have a major gridline by detemining if i is a multiple of the major gridline spacing
                if(i % default_gridline_spacing_major == 0)
                {
                    continue;
                }

                // draw the minor gridlines
                Line verticalLine = new Line { X1 = i, Y1 = 0, X2 = i, Y2 = MainCanvas.Height, Stroke = Brushes.LightGray, StrokeDashArray = new DoubleCollection { 2, 2 }, StrokeThickness = 0.2 };
                Line horizontalLine = new Line { X1 = 0, Y1 = i, X2 = MainCanvas.Width, Y2 = i, Stroke = Brushes.LightGray, StrokeDashArray = new DoubleCollection { 2, 2 }, StrokeThickness = 0.2 };
                MainCanvas.Children.Add(verticalLine);
                MainCanvas.Children.Add(horizontalLine);
            }

        }


        private void DrawResults()
        {

            //if (Calculator == null)
            //{
            //    return;
            //}

            // Draw the gridlines on the canvas
//            DrawGridLines();

            AddRectangleWithBorderAndCenter(25, 25, 50, 50, Brushes.Red);
            AddRectangleWithBorderAndCenter(100, 100, 40, 60, Brushes.Blue);

            DrawBoundingBox();


            //// Clear the MainCanvas before redrawing.
            //MainCanvas.Children.Clear();

            //// Draw a preview Line 
            //if(_currentPreviewLine != null)
            //{
            //    if (MainCanvas.Children.Contains(_currentPreviewLine) != true)
            //    {
            //        MainCanvas.Children.Add(_currentPreviewLine);
            //    }
            //}

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

            ////Draw the walls
            //if (Calculator._wall_system != null)
            //{

            //    if (Calculator._wall_system.EW_Walls.Count > 0)
            //    {
            //        foreach (var wall in Calculator._wall_system.EW_Walls)
            //        {
            //            DrawingHelpersLibrary.DrawingHelpers.DrawLine(
            //                MainCanvas,
            //                wall.Value.Start.X * SCALE_X,
            //                MainCanvas.Height - wall.Value.Start.Y * SCALE_Y,
            //                wall.Value.End.X * SCALE_X,
            //                MainCanvas.Height - wall.Value.End.Y * SCALE_Y,
            //                System.Windows.Media.Brushes.Black,
            //                3);

            //            DrawingHelpersLibrary.DrawingHelpers.DrawText(
            //                MainCanvas,
            //                wall.Value.Center.X * SCALE_X,
            //                MainCanvas.Height - wall.Value.Center.Y * SCALE_Y,
            //                0,
            //                wall.Key.ToString(),
            //                System.Windows.Media.Brushes.Black,
            //                12
            //                );
            //        }
            //    }
            //    //Draw the walls
            //    if (Calculator._wall_system.NS_Walls.Count > 0)
            //    {
            //        foreach (var wall in Calculator._wall_system.NS_Walls)
            //        {
            //            DrawingHelpersLibrary.DrawingHelpers.DrawLine(
            //                MainCanvas,
            //                wall.Value.Start.X * SCALE_X,
            //                MainCanvas.Height - wall.Value.Start.Y * SCALE_Y,
            //                wall.Value.End.X * SCALE_X,
            //                MainCanvas.Height - wall.Value.End.Y * SCALE_Y,
            //                System.Windows.Media.Brushes.Black,
            //                3);

            //            DrawingHelpersLibrary.DrawingHelpers.DrawText(
            //                MainCanvas,
            //                (wall.Value.Center.X + 1) * SCALE_X,
            //                MainCanvas.Height - wall.Value.Center.Y * SCALE_Y,
            //                0,
            //                wall.Key.ToString(),
            //                System.Windows.Media.Brushes.Black,
            //                12
            //                );
            //        }
            //    }

            //    // Draw the Center of Mass Point
            //    DrawingHelpersLibrary.DrawingHelpers.DrawCircle(
            //        MainCanvas,
            //        Calculator._diaphragm_system.CtrMass.X * SCALE_X,
            //        MainCanvas.Height - Calculator._diaphragm_system.CtrMass.Y * SCALE_Y,
            //        System.Windows.Media.Brushes.Red,
            //        System.Windows.Media.Brushes.Red,
            //        10,
            //        1);
            //    DrawingHelpersLibrary.DrawingHelpers.DrawText(
            //        MainCanvas,
            //        Calculator._diaphragm_system.CtrMass.X * SCALE_X,
            //        MainCanvas.Height - Calculator._diaphragm_system.CtrMass.Y * SCALE_Y - 20,
            //        0,
            //        "C.M",
            //        System.Windows.Media.Brushes.Red,
            //        12
            //        );
            //}

            //// if we have a defined diaphragm, draw the centroid
            //if (Calculator._diaphragm_system != null)
            //{
            //    // Draw the Center of Rigidity Point
            //    DrawingHelpersLibrary.DrawingHelpers.DrawCircle(
            //        MainCanvas,
            //        Calculator._wall_system.CtrRigidity.X * SCALE_X,
            //        MainCanvas.Height - Calculator._wall_system.CtrRigidity.Y * SCALE_Y,
            //        System.Windows.Media.Brushes.Blue,
            //        System.Windows.Media.Brushes.Blue,
            //        10,
            //        1);
            //    DrawingHelpersLibrary.DrawingHelpers.DrawText(
            //        MainCanvas,
            //        Calculator._wall_system.CtrRigidity.X * SCALE_X,
            //        MainCanvas.Height - Calculator._wall_system.CtrRigidity.Y * SCALE_Y,
            //        0,
            //        "C.R",
            //        System.Windows.Media.Brushes.Blue,
            //        12
            //        );
            //}


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

            //if (Calculator.GetType() == typeof(ShearWallCalculator_RigidDiaphragm))
            //{
            //    foreach (var result in ((ShearWallCalculator_RigidDiaphragm)Calculator).TotalWallShear)
            //    {
            //        int id = result.Key;
            //        float rigidity = ((ShearWallCalculator_RigidDiaphragm)Calculator)._wall_system.EW_Walls.ContainsKey(id) ? ((ShearWallCalculator_RigidDiaphragm)Calculator)._wall_system.EW_Walls[id].WallRigidity : Calculator._wall_system.NS_Walls[id].WallRigidity;
            //        float direct_shear_x = ((ShearWallCalculator_RigidDiaphragm)Calculator).DirectShear_X.ContainsKey(id) ? ((ShearWallCalculator_RigidDiaphragm)Calculator).DirectShear_X[id] : 0.0f;
            //        float direct_shear_y = ((ShearWallCalculator_RigidDiaphragm)Calculator).DirectShear_Y.ContainsKey(id) ? ((ShearWallCalculator_RigidDiaphragm)Calculator).DirectShear_Y[id] : 0.0f;
            //        float eccentric_shear = ((ShearWallCalculator_RigidDiaphragm)Calculator).EccentricShear.ContainsKey(id) ? ((ShearWallCalculator_RigidDiaphragm)Calculator).EccentricShear[id] : 0.0f;
            //        float total_shear = ((ShearWallCalculator_RigidDiaphragm)Calculator).TotalWallShear.ContainsKey(id) ? ((ShearWallCalculator_RigidDiaphragm)Calculator).TotalWallShear[id] : 0.0f;

            //        ShearWallResults control = new ShearWallResults(
            //            id,
            //            rigidity,
            //            Calculator._wall_system.X_bar_walls[id],
            //            Calculator._wall_system.Y_bar_walls[id],
            //            direct_shear_x,
            //            direct_shear_y,
            //            eccentric_shear,
            //            total_shear
            //            );


            //        ShearWallResults.Children.Add(control);
            //    }

            //    // Create Table of Wall Data
            //    ShearWallData_EW.Children.Clear();
            //    ShearWallData_NS.Children.Clear();

            //    /// Create the shearwall data controls
            //    foreach (var result in ((ShearWallCalculator_RigidDiaphragm)Calculator).TotalWallShear)
            //    {
            //        int id = result.Key;
            //        WallData wall = Calculator._wall_system.EW_Walls.ContainsKey(id) ? Calculator._wall_system.EW_Walls[id] : Calculator._wall_system.NS_Walls[id];
            //        ShearWallData control = new ShearWallData(id, wall);

            //        if (wall.WallDir == WallDirs.EastWest)
            //        {
            //            ShearWallData_EW.Children.Add(control);
            //        }
            //        else if (wall.WallDir == WallDirs.NorthSouth)
            //        {
            //            ShearWallData_NS.Children.Add(control);
            //        }
            //        else
            //        {
            //            throw new Exception("Invalid wall direction " + wall.WallDir.ToString() + " in wall #" + id.ToString());
            //        }

            //        control.DeleteWall += OnWallDeleted;
            //    }
            //} else if (Calculator.GetType() == typeof(ShearWallCalculator_FlexibleDiaphragm))
            //{
            //    throw new NotImplementedException("\nFlexible Diaphragms Calculator not implemented yet.");
            //} else
            //{
            //    throw new NotImplementedException("\nInvalid Calaculator type received.");
            //}
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

//        private void MainCanvas_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
//        {
//            string status = "";
//            if((CurrentInputMode == InputModes.Rigidity) || (CurrentInputMode == InputModes.Mass))
//            {
//                _currentPreviewLine = new Line() { Stroke = Brushes.Green };
////               MainCanvas.Children.Add(_currentPreviewLine);


//                //var p = e.GetPosition(MainCanvas);
//                System.Windows.Point p = Mouse.GetPosition(MainCanvas);

//                if (StartClickIsSet == false)
//                {
//                    StartClickCoord.Content = "(" + p.X.ToString("0.00") + ", " + p.Y.ToString("0.00") + ")";
//                    CurrentStartPoint = p;
//                    StartClickIsSet = true;
//                    _currentPreviewLine.X1 = CurrentStartPoint.X;
//                    _currentPreviewLine.Y1 = CurrentStartPoint.Y;
//                    _currentPreviewLine.X2 = CurrentStartPoint.X;
//                    _currentPreviewLine.Y2 = CurrentStartPoint.Y;

//                    status = "First point selected";
//                    return;
//                }
//                else
//                {
//                    EndClickCoord.Content = "(" + p.X.ToString("0.00") + ", " + p.Y.ToString("0.00") + ")";
//                    CurrentEndPoint = p;
//                    EndClickIsSet = true;
//                    _currentPreviewLine.X2 = CurrentEndPoint.X;
//                    _currentPreviewLine.Y2 = CurrentEndPoint.Y;
//                    status = "Second point selected";
//                }

//                lblStatus.Content = status;

//                if (StartClickIsSet && EndClickIsSet)
//                {
//                    // process the points clicked
//                    float start_x = ((float)CurrentStartPoint.X) / (float)SCALE_X;
//                    float start_y = ((float)MainCanvas.Height - (float)CurrentStartPoint.Y) / (float)SCALE_Y;
//                    float end_x = ((float)CurrentEndPoint.X) / (float)SCALE_X;
//                    float end_y = ((float)MainCanvas.Height - ((float)CurrentEndPoint.Y)) / (float)SCALE_Y;

//                    if(CurrentInputMode == InputModes.Rigidity)
//                    {
//                        // Determine if this wall segment should be horizontal or vertical by looking at the difference between the x
//                        // and y coordinates of the start and end points.  Whichever difference is larger will be the direction
//                        //  -- larger X direction = horizontal
//                        //  -- larger Y direction = vertical
//                        WallDirs dir = WallDirs.EastWest;
//                        if ((Math.Abs(CurrentStartPoint.X - CurrentEndPoint.X)) >= Math.Abs(CurrentStartPoint.Y - CurrentEndPoint.Y))
//                        {
//                            dir = WallDirs.EastWest;
//                            end_y = start_y; // move the end point to make the line horizontal
//                        }
//                        else
//                        {
//                            dir = WallDirs.NorthSouth;
//                            end_x = start_x; // move the end point to make the line vertical
//                        }

//                        // Add to the list of wall segments
//                        Calculator._wall_system.AddWall(new WallData(DEFAULT_WALL_HT, start_x, start_y, end_x, end_y, dir));
//                        _currentPreviewLine = null;  // clear the preview line
//                        status = "Wall added";
//                    }
//                    else if (CurrentInputMode == InputModes.Mass)
//                    {
//                        // Add to the list of diaphragm segments
//                        Calculator._diaphragm_system.AddDiaphragm(new DiaphragmData_Rectangular(
//                            new System.Windows.Point(CurrentStartPoint.X / SCALE_X, CurrentStartPoint.Y / SCALE_Y),
//                            new System.Windows.Point(CurrentEndPoint.X / SCALE_X, CurrentEndPoint.Y / SCALE_Y)));
//                        lblStatus.Content = "Diaphragm added";
//                    } else
//                    {
//                        throw new Exception("Invalid input mode -- should be rigidity or mass mode.");
//                    }

//                    Update();  // force the update

//                    // then clear the variables.
//                    StartClickIsSet = false;
//                    EndClickIsSet = false;

//                }



//            }
//            //else if (CurrentInputMode == InputModes.Mass)
//            //{
//            //    System.Windows.Point p = Mouse.GetPosition(MainCanvas);

//            //    System.Windows.Point point = new System.Windows.Point(p.X / (float)SCALE_X, (MainCanvas.Height - p.Y) / (float)SCALE_Y);
//            //    DiaphragmPoints.Add(point);



//            //    Update();
//            //}
//            lblStatus.Content = status;
//        }

        private void Update()
        {
            // Create the new calculator
            Calculator.Update();

            DrawResults();

            CenterOfMass.Content = "(" + Calculator._diaphragm_system.CtrMass.X.ToString("0.00") + ", " + Calculator._diaphragm_system.CtrMass.Y.ToString("0.00") + ")";
            CenterOfRigidity.Content = "(" + Calculator._wall_system.CtrRigidity.X.ToString("0.00") + ", " + Calculator._wall_system.CtrRigidity.Y.ToString("0.00") + ")";
        }



        //private void MainCanvas_MouseMove(object sender, MouseEventArgs e)
        //{
        //    // Update the preview lines
        //    if (_currentPreviewLine != null)
        //    {
        //        var p = e.GetPosition(MainCanvas);
        //        _currentPreviewLine.X2 = p.X;
        //        _currentPreviewLine.Y2 = p.Y;
        //    }

        //    // Update the mouse position label
        //    MousePosition.Content = "(" + e.GetPosition(MainCanvas).X.ToString("0.00") + ", " + e.GetPosition(MainCanvas).Y.ToString("0.00") + ")";

        //    Update();
        //}

        /// <summary>
        /// Set up the event to detect key presses within the window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            window.KeyDown += HandleKeyPress;
        }

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

        private void FinishCommand()
        {
        }
    }
}
