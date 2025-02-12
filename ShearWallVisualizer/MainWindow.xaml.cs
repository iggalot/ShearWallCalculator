using calculator;
using ShearWallVisualizer.Controls;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
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
        // scale factors for the canvas and layout of the visiualizer
        private const double SCALE_X = 5;
        private const double SCALE_Y = 5;
        private float DEFAULT_WALL_HT = 9;  // ft

        private InputModes CurrentInputMode { get; set; } = InputModes.None;
        private bool StartClickIsSet { get; set; } = false;
        private bool EndClickIsSet { get; set; } = false;

        public System.Windows.Point CurrentStartPoint { get; set; }
        public System.Windows.Point CurrentEndPoint { get; set; }

        public ShearWallCalculator Calculator { get; set; } = new ShearWallCalculator();
        public WallData CurrentWall { get; set; }

        public Dictionary<int, WallData> Walls { get; set; } = new Dictionary<int, WallData>();
        public List<System.Windows.Point> DiaphragmPoints { get; set; } = new List<System.Windows.Point>();

        public MainWindow()
        {
            InitializeComponent();

            Update();
        }

        private void DrawResults()
        {
            if (Calculator == null)
            {
                return;
            }

            // Clear the MainCanvas before redrawing.
            MainCanvas.Children.Clear();

            //Draw the diaphragm boundary
            if (Calculator.DiaphragmPoints.Count > 0)
            {
                foreach (var point in Calculator.DiaphragmPoints)
                {
                    DrawingHelpersLibrary.DrawingHelpers.DrawCircle(
                        MainCanvas,
                        point.X * SCALE_X,
                        MainCanvas.Height - point.Y * SCALE_Y,
                        System.Windows.Media.Brushes.Red,
                        System.Windows.Media.Brushes.Red,
                        3, 1);
                }

                for(int i = 0; i < Calculator.DiaphragmPoints.Count; i++)
                {
                    DrawingHelpersLibrary.DrawingHelpers.DrawLine(
                        MainCanvas,
                        Calculator.DiaphragmPoints[i].X * SCALE_X,
                        MainCanvas.Height - Calculator.DiaphragmPoints[i].Y * SCALE_Y,
                        Calculator.DiaphragmPoints[(i + 1) % Calculator.DiaphragmPoints.Count].X * SCALE_X,
                        MainCanvas.Height - Calculator.DiaphragmPoints[(i + 1) % Calculator.DiaphragmPoints.Count].Y * SCALE_Y,
                        System.Windows.Media.Brushes.Red,
                        2);
                }

            }

            //Draw the walls
            if (Calculator.EW_Walls.Count > 0)
            {
                foreach (var wall in Calculator.EW_Walls)
                {
                    DrawingHelpersLibrary.DrawingHelpers.DrawLine(
                        MainCanvas,
                        wall.Value.Start.X * SCALE_X,
                        MainCanvas.Height - wall.Value.Start.Y * SCALE_Y,
                        wall.Value.End.X * SCALE_X,
                        MainCanvas.Height - wall.Value.End.Y * SCALE_Y,
                        System.Windows.Media.Brushes.Black,
                        3);

                    DrawingHelpersLibrary.DrawingHelpers.DrawText(
                        MainCanvas,
                        wall.Value.Center.X * SCALE_X,
                        MainCanvas.Height - wall.Value.Center.Y * SCALE_Y,
                        0,
                        wall.Key.ToString(),
                        System.Windows.Media.Brushes.Black,
                        12
                        );
                }
            }
            //Draw the walls
            if (Calculator.NS_Walls.Count > 0)
            {
                foreach (var wall in Calculator.NS_Walls)
                {
                    DrawingHelpersLibrary.DrawingHelpers.DrawLine(
                        MainCanvas,
                        wall.Value.Start.X * SCALE_X,
                        MainCanvas.Height - wall.Value.Start.Y * SCALE_Y ,
                        wall.Value.End.X * SCALE_X,
                        MainCanvas.Height - wall.Value.End.Y * SCALE_Y,
                        System.Windows.Media.Brushes.Black,
                        3);

                    DrawingHelpersLibrary.DrawingHelpers.DrawText(
                        MainCanvas,
                        (wall.Value.Center.X + 1) * SCALE_X,
                        MainCanvas.Height - wall.Value.Center.Y * SCALE_Y,
                        0,
                        wall.Key.ToString(),
                        System.Windows.Media.Brushes.Black,
                        12
                        );
                }
            }

            // Draw the Center of Mass Point
            DrawingHelpersLibrary.DrawingHelpers.DrawCircle(
                MainCanvas,
                Calculator.CtrMass.X * SCALE_X,
                MainCanvas.Height - Calculator.CtrMass.Y * SCALE_Y,
                System.Windows.Media.Brushes.Red,
                System.Windows.Media.Brushes.Red,
                10,
                1);
            DrawingHelpersLibrary.DrawingHelpers.DrawText(
                MainCanvas,
                Calculator.CtrMass.X * SCALE_X,
                MainCanvas.Height - Calculator.CtrMass.Y * SCALE_Y - 20,
                0,
                "C.M",
                System.Windows.Media.Brushes.Red,
                12
                );


            // Draw the Center of Rigidity Point
            DrawingHelpersLibrary.DrawingHelpers.DrawCircle(
                MainCanvas,
                Calculator.CtrRigidity.X * SCALE_X,
                MainCanvas.Height - Calculator.CtrRigidity.Y * SCALE_Y,
                System.Windows.Media.Brushes.Blue,
                System.Windows.Media.Brushes.Blue,
                10,
                1);
            DrawingHelpersLibrary.DrawingHelpers.DrawText(
                MainCanvas,
                Calculator.CtrRigidity.X * SCALE_X,
                MainCanvas.Height - Calculator.CtrRigidity.Y * SCALE_Y,
                0,
                "C.R",
                System.Windows.Media.Brushes.Blue,
                12
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



            // Create Table of Results
            ShearWallResults.Children.Clear();

            foreach (var result in Calculator.TotalWallShear)
            {
                int id = result.Key;
                float rigidity = Calculator.EW_Walls.ContainsKey(id) ? Calculator.EW_Walls[id].WallRigidity : Calculator.NS_Walls[id].WallRigidity;
                float direct_shear_x = Calculator.DirectShear_X.ContainsKey(id) ? Calculator.DirectShear_X[id] : 0.0f;
                float direct_shear_y = Calculator.DirectShear_Y.ContainsKey(id) ? Calculator.DirectShear_Y[id] : 0.0f;
                float eccentric_shear = Calculator.EccentricShear.ContainsKey(id) ? Calculator.EccentricShear[id] : 0.0f;
                float total_shear = Calculator.TotalWallShear.ContainsKey(id) ? Calculator.TotalWallShear[id] : 0.0f;

                ShearWallResults control = new ShearWallResults(
                    id,
                    rigidity,
                    Calculator.X_bar_walls[id],
                    Calculator.Y_bar_walls[id],
                    direct_shear_x,
                    direct_shear_y,
                    eccentric_shear,
                    total_shear
                    );


                ShearWallResults.Children.Add(control);
            }

            // Create Table of Wall Data
            ShearWallData_EW.Children.Clear();
            ShearWallData_NS.Children.Clear();

            /// Create the shearwall data controls
            foreach (var result in Calculator.TotalWallShear)
            {
                int id = result.Key;
                WallData wall = Calculator.EW_Walls.ContainsKey(id) ? Calculator.EW_Walls[id] : Calculator.NS_Walls[id];
                ShearWallData control = new ShearWallData(id, wall);

                if(wall.WallDir == WallDirs.EastWest)
                {
                    ShearWallData_EW.Children.Add(control);
                } else if (wall.WallDir == WallDirs.NorthSouth)
                {
                    ShearWallData_NS.Children.Add(control);
                } else
                {
                    throw new Exception("Invalid wall direction " + wall.WallDir.ToString() + " in wall #" + id.ToString());
                }

                control.DeleteWall += OnWallDeleted;
            }
        }

        private void OnWallDeleted(object sender, DeleteWallEventArgs e)
        {
            if(Walls.ContainsKey(e.Id) == true)
            {
                Walls.Remove(e.Id);

                MessageBox.Show(e.Id.ToString() + " has been deleted");
            } else
            {
                MessageBox.Show(e.Id.ToString() + " does not exist in Walls");
            }
            
            Update();




        }

        private void MainCanvas_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if(CurrentInputMode == InputModes.Rigidity)
            {
                if (StartClickIsSet == false)
                {
                    System.Windows.Point p = Mouse.GetPosition(MainCanvas);
                    StartClickCoord.Content = "(" + p.X.ToString("0.00") + ", " + p.Y.ToString("0.00") + ")";
                    CurrentStartPoint = p;
                    StartClickIsSet = true;
                    return;
                }
                else
                {
                    System.Windows.Point p = Mouse.GetPosition(MainCanvas);
                    EndClickCoord.Content = "(" + p.X.ToString("0.00") + ", " + p.Y.ToString("0.00") + ")";
                    CurrentEndPoint = p;
                    EndClickIsSet = true;
                }

                if (StartClickIsSet && EndClickIsSet)
                {
                    // process the points clicked
                    float start_x = ((float)CurrentStartPoint.X) / (float)SCALE_X;
                    float start_y = ((float)MainCanvas.Height - (float)CurrentStartPoint.Y) / (float)SCALE_Y;
                    float end_x = ((float)CurrentEndPoint.X) / (float)SCALE_X;
                    float end_y = ((float)MainCanvas.Height - ((float)CurrentEndPoint.Y)) / (float)SCALE_Y;

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

                    // Add to the list of wall segments
                    CurrentWall = new WallData(GetNextWallID(), DEFAULT_WALL_HT, start_x, start_y, end_x, end_y, dir);
                    Walls.Add(CurrentWall.Id, CurrentWall);

                    StartClickIsSet = false;
                    EndClickIsSet = false;

                    Update();
                }

            } else if (CurrentInputMode == InputModes.Mass)
            {
                System.Windows.Point p = Mouse.GetPosition(MainCanvas);

                System.Windows.Point point = new System.Windows.Point(p.X / (float)SCALE_X, (MainCanvas.Height - p.Y) / (float)SCALE_Y);
                DiaphragmPoints.Add(point);

                

                Update();
            }

        }

        private void Update()
        {
            // Create the new calculator
            Calculator = new ShearWallCalculator(Walls, DiaphragmPoints);

            DrawResults();

            CenterOfMass.Content = "(" + Calculator.CtrMass.X.ToString("0.00") + ", " + Calculator.CtrMass.Y.ToString("0.00") + ")";
            CenterOfRigidity.Content = "(" + Calculator.CtrRigidity.X.ToString("0.00") + ", " + Calculator.CtrRigidity.Y.ToString("0.00") + ")";
        }

        /// <summary>
        /// Scans the list of walls currently stored to find the next available wall id.  This should ensure a unique identifier
        /// </summary>
        /// <returns></returns>
        private int GetNextWallID()
        {
            int i = 0;
            while (true)
            {
                if (!Walls.ContainsKey(i))
                {
                    return i;
                } else
                {
                    i++;
                }
            }
        }

        private void MainCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            MousePosition.Content = "(" + e.GetPosition(MainCanvas).X.ToString("0.00") + ", " + e.GetPosition(MainCanvas).Y.ToString("0.00") + ")";
        }

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
                        Walls.Clear();
                    }
                    if(CurrentInputMode == InputModes.Mass)
                    {
                        DiaphragmPoints.Clear();
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
