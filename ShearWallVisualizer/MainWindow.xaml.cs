using calculator;
using DrawingHelpersLibrary;
using ShearWallVisualizer.Controls;
using System;
using System.Windows;

namespace ShearWallVisualizer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            double SCALE_X = 5;
            double SCALE_Y = 5;

            double MARGIN = 50;

            InitializeComponent();

            ShearWallCalculator calculator = new ShearWallCalculator();

            //Draw the walls
            foreach (var wall in calculator.EW_Walls)
            {
                DrawingHelpersLibrary.DrawingHelpers.DrawLine(
                    MainCanvas,
                    wall.Value.Start.X * SCALE_X + MARGIN,
                    MainCanvas.Height - wall.Value.Start.Y * SCALE_Y - MARGIN,
                    wall.Value.End.X * SCALE_X + MARGIN,
                    MainCanvas.Height - wall.Value.End.Y * SCALE_Y - MARGIN,
                    System.Windows.Media.Brushes.Black,
                    3);

                DrawingHelpersLibrary.DrawingHelpers.DrawText(
                    MainCanvas,
                    wall.Value.Center.X * SCALE_X + MARGIN,
                    MainCanvas.Height - wall.Value.Center.Y * SCALE_Y - MARGIN,
                    0,
                    wall.Key.ToString(),
                    System.Windows.Media.Brushes.Black,
                    12
                    );
            }
            //Draw the walls
            foreach (var wall in calculator.NS_Walls)
            {
                DrawingHelpersLibrary.DrawingHelpers.DrawLine(
                    MainCanvas,
                    wall.Value.Start.X * SCALE_X + MARGIN,
                    MainCanvas.Height - wall.Value.Start.Y * SCALE_Y - MARGIN,
                    wall.Value.End.X * SCALE_X + MARGIN,
                    MainCanvas.Height - wall.Value.End.Y * SCALE_Y - MARGIN,
                    System.Windows.Media.Brushes.Black,
                    3);

                DrawingHelpersLibrary.DrawingHelpers.DrawText(
                    MainCanvas,
                    (wall.Value.Center.X + 1) * SCALE_X + MARGIN,
                    MainCanvas.Height - wall.Value.Center.Y * SCALE_Y - MARGIN,
                    0,
                    wall.Key.ToString(),
                    System.Windows.Media.Brushes.Black,
                    12
                    );
            }

            // Draw the Center of Mass Point
            DrawingHelpersLibrary.DrawingHelpers.DrawCircle(
                MainCanvas,
                calculator.CtrMass.X * SCALE_X + MARGIN,
                MainCanvas.Height - calculator.CtrMass.Y * SCALE_Y - MARGIN,
                System.Windows.Media.Brushes.Red,
                System.Windows.Media.Brushes.Red,
                10,
                1);
            DrawingHelpersLibrary.DrawingHelpers.DrawText(
                MainCanvas,
                calculator.CtrMass.X * SCALE_X + MARGIN,
                MainCanvas.Height - calculator.CtrMass.Y * SCALE_Y - MARGIN -20,
                0,
                "C.M",
                System.Windows.Media.Brushes.Red,
                12
                );


            // Draw the Center of Rigidity Point
            DrawingHelpersLibrary.DrawingHelpers.DrawCircle(
                MainCanvas,
                calculator.CtrRigidity.X * SCALE_X + MARGIN,
                MainCanvas.Height - calculator.CtrRigidity.Y * SCALE_Y - MARGIN,
                System.Windows.Media.Brushes.Blue,
                System.Windows.Media.Brushes.Blue,
                10,
                1);
            DrawingHelpersLibrary.DrawingHelpers.DrawText(
                MainCanvas,
                calculator.CtrRigidity.X * SCALE_X + MARGIN,
                MainCanvas.Height - calculator.CtrRigidity.Y * SCALE_Y - MARGIN,
                0,
                "C.R",
                System.Windows.Media.Brushes.Blue,
                12
                );


            // Draw the Loads
            if (calculator.V_x != 0)
            {
                ArrowDirections dir = calculator.V_x < 0 ? ArrowDirections.ARROW_LEFT : ArrowDirections.ARROW_RIGHT;
                DrawingHelpersLibrary.DrawingHelpers.DrawArrow(
                    MainCanvas,
                    calculator.CtrRigidity.X * SCALE_X + MARGIN,
                    MainCanvas.Height - calculator.CtrRigidity.Y * SCALE_Y - MARGIN,
                    System.Windows.Media.Brushes.Black,
                    System.Windows.Media.Brushes.Black,
                    dir,
                    4
                    );
            }

            if(calculator.V_y != 0)
            {
                ArrowDirections dir = calculator.V_y < 0 ? ArrowDirections.ARROW_DOWN : ArrowDirections.ARROW_UP;
                DrawingHelpersLibrary.DrawingHelpers.DrawArrow(
                    MainCanvas,
                    calculator.CtrRigidity.X * SCALE_X + MARGIN,
                    MainCanvas.Height - calculator.CtrRigidity.Y * SCALE_Y - MARGIN,
                    System.Windows.Media.Brushes.Black,
                    System.Windows.Media.Brushes.Black,
                    dir,
                    4
                    );
            }



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
            foreach (var result in calculator.TotalWallShear)
            {
                int id = result.Key;
                float rigidity = calculator.EW_Walls.ContainsKey(id) ? calculator.EW_Walls[id].WallRigidity : calculator.NS_Walls[id].WallRigidity;
                float direct_shear_x = calculator.DirectShear_X.ContainsKey(id) ? calculator.DirectShear_X[id] : 0.0f;
                float direct_shear_y = calculator.DirectShear_Y.ContainsKey(id) ? calculator.DirectShear_Y[id] : 0.0f;
                float eccentric_shear = calculator.EccentricShear.ContainsKey(id) ? calculator.EccentricShear[id] : 0.0f;
                float total_shear = calculator.TotalWallShear.ContainsKey(id) ? calculator.TotalWallShear[id] : 0.0f;

                ShearWallResults control = new ShearWallResults(
                    id, 
                    rigidity, 
                    calculator.X_bar_walls[id],
                    calculator.Y_bar_walls[id],
                    direct_shear_x,
                    direct_shear_y,
                    eccentric_shear,
                    total_shear
                    );
                

                ShearWallResults.Children.Add(control);
            }
        }
    }
}
