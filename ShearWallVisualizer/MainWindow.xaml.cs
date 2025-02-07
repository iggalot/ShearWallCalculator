using calculator;
using DrawingHelpersLibrary;
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


            // Draw the Center of Rigidity Point
            DrawingHelpersLibrary.DrawingHelpers.DrawCircle(
                MainCanvas,
                calculator.CtrRigidity.X * SCALE_X + MARGIN,
                MainCanvas.Height - calculator.CtrRigidity.Y * SCALE_Y - MARGIN,
                System.Windows.Media.Brushes.Blue,
                System.Windows.Media.Brushes.Blue,
                10,
                1);

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


        }
    }
}
