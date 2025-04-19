using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ShearWallVisualizer.Controls
{
    /// <summary>
    /// Interaction logic for WindLoadGraphicCanvas.xaml
    /// </summary>
    public partial class WindLoadGraphicCanvas : UserControl
    {
        private double SCALE_X = 3.0;
        private double SCALE_Y = 3.0;

        WindLoadParameters _parameters;
        List<WindPressurResult_Wall> _wall_results;
        List<WindPressurResult_Roof> _roof_results;

        public WindLoadGraphicCanvas()
        {
            InitializeComponent();
        }

        public WindLoadGraphicCanvas( WindLoadParameters parameters, List<WindPressurResult_Wall> wall_results, List<WindPressurResult_Roof> roof_results)
        {
            _parameters = parameters;
            _wall_results = wall_results;
            _roof_results = roof_results;

            InitializeComponent();

            this.Loaded += WindLoadGraphicsCanvas_Loaded;





        }

        private void WindLoadGraphicsCanvas_Loaded(object sender, RoutedEventArgs e)
        {
            double offset = 10;
            Canvas cnv_plan = cnvPlanView;
            Canvas cnv_elev = cnvElevationView;

            double cnv_ht_elev = cnv_elev.ActualHeight;   // <-- Use ActualHeight
            double cnv_width_elev = cnv_elev.ActualWidth; // <-- Use ActualWidth

            double cnv_ht_plan = cnv_plan.ActualHeight;   // <-- Use ActualHeight
            double cnv_width_plan = cnv_plan.ActualWidth; // <-- Use ActualWidth

            DrawElevationWindLoads(offset, cnv_elev, cnv_ht_elev);
            DrawPlanWindLoads(offset, cnv_plan, cnv_ht_plan);
        }

        private void DrawPlanWindLoads(double offset, Canvas cnv_plan, double cnv_ht_plan)
        {
            // Draw the plan frame
            Point p1 = new Point(offset, cnv_ht_plan - offset);
            Point p2 = new Point(offset + SCALE_X * _parameters.BuildingLength, cnv_ht_plan - offset);
            Point p3 = new Point(offset + SCALE_X * _parameters.BuildingLength, cnv_ht_plan - offset - SCALE_Y * _parameters.BuildingWidth);
            Point p4 = new Point(offset, cnv_ht_plan - offset - SCALE_Y * _parameters.BuildingWidth);
           
            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnv_plan, p1.X, p1.Y, p2.X, p2.Y, System.Windows.Media.Brushes.Black, 2.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnv_plan, p2.X, p2.Y, p3.X, p3.Y, System.Windows.Media.Brushes.Black, 2.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnv_plan, p3.X, p3.Y, p4.X, p4.Y, System.Windows.Media.Brushes.Black, 2.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnv_plan, p4.X, p4.Y, p1.X, p1.Y, System.Windows.Media.Brushes.Black, 2.0);
        }

        private void DrawElevationWindLoads(double offset, Canvas cnv_elev, double cnv_ht_elev)
        {
            // Draw the elevation frame
            Point p1 = new Point(offset, cnv_ht_elev - offset);
            Point p2 = new Point(offset, cnv_ht_elev - offset - SCALE_Y * _parameters.BuildingHeight);
            Point p3 = new Point(offset + SCALE_X * _parameters.BuildingLength * 0.5, cnv_ht_elev - offset - SCALE_Y * (_parameters.BuildingHeight + _parameters.BuildingLength * 0.5 * Math.Sin(_parameters.RoofPitch * 2.0 * Math.PI / 360)));
            Point p4 = new Point(offset + SCALE_X * _parameters.BuildingLength, cnv_ht_elev - offset - SCALE_Y * _parameters.BuildingHeight);
            Point p5 = new Point(offset + SCALE_X * _parameters.BuildingLength, cnv_ht_elev - offset);

            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnv_elev, p1.X, p1.Y, p2.X, p2.Y, System.Windows.Media.Brushes.Black, 2.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnv_elev, p2.X, p2.Y, p3.X, p3.Y, System.Windows.Media.Brushes.Black, 2.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnv_elev, p3.X, p3.Y, p4.X, p4.Y, System.Windows.Media.Brushes.Black, 2.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnv_elev, p4.X, p4.Y, p5.X, p5.Y, System.Windows.Media.Brushes.Black, 2.0);
        }
    }
}
