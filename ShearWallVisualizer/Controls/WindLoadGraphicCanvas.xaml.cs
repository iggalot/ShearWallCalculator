using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace ShearWallVisualizer.Controls
{
    /// <summary>
    /// Interaction logic for WindLoadGraphicCanvas.xaml
    /// </summary>
    public partial class WindLoadGraphicCanvas : UserControl
    {
        private double _margin = 0; 

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
            double ht = _parameters.BuildingHeight;
            double length = _parameters.BuildingLength;
            double width = _parameters.BuildingWidth;
            double ridge_ht = ht + length * 0.5 * Math.Tan(_parameters.RoofPitch * 2.0 * Math.PI / 360);


            Canvas cnv_plan = cnvPlanView;
            Canvas cnv_elev = cnvElevationView;

            double cnv_ht_elev = cnv_elev.ActualHeight;   // <-- Use ActualHeight
            double cnv_width_elev = cnv_elev.ActualWidth; // <-- Use ActualWidth

            double cnv_ht_plan = cnv_plan.ActualHeight;   // <-- Use ActualHeight
            double cnv_width_plan = cnv_plan.ActualWidth; // <-- Use ActualWidth

            SCALE_X = (cnv_width_elev - 2 * _margin) / length;
            SCALE_Y = Math.Min((cnv_ht_elev - 2 * _margin) / ridge_ht, (cnv_ht_plan - 2 * _margin) / width);


            DrawElevationWindLoads(cnv_elev, cnv_ht_elev);
            DrawPlanWindLoads(cnv_plan, cnv_ht_plan);
        }

        private void DrawPlanWindLoads(Canvas cnv_plan, double cnv_ht_plan)
        {
            double offset = _margin;
            // Draw the plan frame
            Point p1 = new Point(offset, cnv_ht_plan - offset);
            Point p2 = new Point(offset + SCALE_X * _parameters.BuildingLength, cnv_ht_plan - offset);
            Point p3 = new Point(offset + SCALE_X * _parameters.BuildingLength, cnv_ht_plan - offset - SCALE_Y * _parameters.BuildingWidth);
            Point p4 = new Point(offset, cnv_ht_plan - offset - SCALE_Y * _parameters.BuildingWidth);
           
            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnv_plan, p1.X, p1.Y, p2.X, p2.Y, System.Windows.Media.Brushes.Black, 2.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnv_plan, p2.X, p2.Y, p3.X, p3.Y, System.Windows.Media.Brushes.Black, 2.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnv_plan, p3.X, p3.Y, p4.X, p4.Y, System.Windows.Media.Brushes.Black, 2.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnv_plan, p4.X, p4.Y, p1.X, p1.Y, System.Windows.Media.Brushes.Black, 2.0);

            // Draw the windward wall wind load
            double WW_pressure = GetWallPressure("Windward Wall - z=h");
            Point a = (new Point(offset - WW_pressure, p1.Y));
            Point b = (new Point(offset - WW_pressure, p4.Y));
            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnv_plan, a.X, a.Y, b.X, b.Y, System.Windows.Media.Brushes.Blue, 2.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawText(cnv_plan, a.X, a.Y, 0, WW_pressure.ToString(), System.Windows.Media.Brushes.Blue, 8.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowRight(cnv_plan, p1.X, p1.Y, System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Blue, 2.0, Math.Abs(WW_pressure), Math.Abs(0.25 * WW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowRight(cnv_plan, p4.X, p4.Y, System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Blue, 2.0, Math.Abs(WW_pressure), Math.Abs(0.25 * WW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowRight(cnv_plan, 0.5 * (p4.X + p1.X), p1.Y + 0.25 * (p4.Y - p1.Y), System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Blue, 2.0, Math.Abs(WW_pressure), Math.Abs(0.25 * WW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowRight(cnv_plan, 0.5 * (p4.X + p1.X), p1.Y + 0.5 * (p4.Y - p1.Y), System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Blue, 2.0, Math.Abs(WW_pressure), Math.Abs(0.25 * WW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowRight(cnv_plan, 0.5 * (p4.X + p1.X), p1.Y + 0.75 * (p4.Y - p1.Y), System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Blue, 2.0, Math.Abs(WW_pressure), Math.Abs(0.25 * WW_pressure));

            // Draw the leeward wall wind load
            double LW_pressure = GetWallPressure("Leeward Wall");
            Point c = (new Point(p2.X - LW_pressure, p2.Y));
            Point d = (new Point(p3.X - LW_pressure, p3.Y));
            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnv_plan, c.X, c.Y, d.X, d.Y, System.Windows.Media.Brushes.Red, 2.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawText(cnv_plan, c.X, c.Y, 0, LW_pressure.ToString(), System.Windows.Media.Brushes.Red, 8.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowRight(cnv_plan, c.X, c.Y, System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Red, 2.0, Math.Abs(LW_pressure), 0.25 * Math.Abs(LW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowRight(cnv_plan, d.X, d.Y, System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Red, 2.0, Math.Abs(LW_pressure), 0.25 * Math.Abs(LW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowRight(cnv_plan, 0.5 * (c.X + d.X), c.Y + 0.25 * (d.Y - c.Y), System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Red, 2.0, Math.Abs(LW_pressure), 0.25 * Math.Abs(LW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowRight(cnv_plan, 0.5 * (c.X + d.X), c.Y + 0.5 * (d.Y - c.Y), System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Red, 2.0, Math.Abs(LW_pressure), 0.25 * Math.Abs(LW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowRight(cnv_plan, 0.5 * (c.X + d.X), c.Y +0.75 * (d.Y - c.Y), System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Red, 2.0, Math.Abs(LW_pressure), 0.25 * Math.Abs(LW_pressure));

            // Draw the sidewall wall wind load -- Upper plan view wall
            double SW_pressure = GetWallPressure("Sidewall");
            Point e = (new Point(p3.X, p3.Y + SW_pressure));
            Point f = (new Point(p4.X, p4.Y + SW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnv_plan, e.X, e.Y, f.X, f.Y, System.Windows.Media.Brushes.Green, 2.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawText(cnv_plan, e.X, e.Y, 0, SW_pressure.ToString(), System.Windows.Media.Brushes.Green, 8.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowUp(cnv_plan, e.X, e.Y, System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Green, 2.0, Math.Abs(SW_pressure), 0.25 * Math.Abs(SW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowUp(cnv_plan, f.X, f.Y, System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Green, 2.0, Math.Abs(SW_pressure), 0.25 * Math.Abs(SW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowUp(cnv_plan, 0.25 * (e.X + f.X), 0.5 * (e.Y + f.Y), System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Green, 2.0, Math.Abs(SW_pressure), 0.25 * Math.Abs(SW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowUp(cnv_plan, 0.5 * (e.X + f.X), 0.5 * (e.Y + f.Y), System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Green, 2.0, Math.Abs(SW_pressure), 0.25 * Math.Abs(SW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowUp(cnv_plan, 0.75 * (e.X + f.X), 0.5 * (e.Y + f.Y), System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Green, 2.0, Math.Abs(SW_pressure), 0.25 * Math.Abs(SW_pressure));

            Point g = (new Point(p1.X, p1.Y - SW_pressure));
            Point h = (new Point(p2.X, p2.Y - SW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnv_plan, g.X, g.Y, h.X, h.Y, System.Windows.Media.Brushes.Green, 2.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawText(cnv_plan, g.X, g.Y, 0, SW_pressure.ToString(), System.Windows.Media.Brushes.Green, 8.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowDown(cnv_plan, g.X, g.Y, System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Green, 2.0, Math.Abs(SW_pressure), 0.25 * Math.Abs(SW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowDown(cnv_plan, h.X, h.Y, System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Green, 2.0, Math.Abs(SW_pressure), 0.25 * Math.Abs(SW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowDown(cnv_plan, 0.25 * (g.X + h.X), 0.5 * (g.Y + h.Y), System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Green, 2.0, Math.Abs(SW_pressure), 0.25 * Math.Abs(SW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowDown(cnv_plan, 0.5 * (g.X + h.X), 0.5 * (g.Y + h.Y), System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Green, 2.0, Math.Abs(SW_pressure), 0.25 * Math.Abs(SW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowDown(cnv_plan, 0.75 * (g.X + h.X), 0.5 * (g.Y + h.Y), System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Green, 2.0, Math.Abs(SW_pressure), 0.25 * Math.Abs(SW_pressure));
        }

        private double GetWallPressure(string v)
        {
            foreach (var result in _wall_results)
            {
                if (result.Surface == v) return result.PressBase;
            }
            return 0;
        }

        private double GetRoofPressure(string v, string wind_case)
        {
            foreach (var result in _roof_results)
            {
                if (result.Surface == v) return (wind_case == "A" ? result.PressBaseA : result.PressBaseB);
            }
            return 0;
        }

        private void DrawElevationWindLoads(Canvas cnv_elev, double cnv_ht_elev)
        {
            double offset = _margin;

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

            // Draw the windward wall wind load
            double WW_pressure_z0 = GetWallPressure("Windward Wall - z=0ft");
            double WW_pressure_z15 = WW_pressure_z0;

            if (_parameters.BuildingHeight >= 15.0)
            {
                WW_pressure_z15 = GetWallPressure("Windward Wall - z=15ft");
            } 
            double WW_pressure_zh = GetWallPressure("Windward Wall - z=h");

            Point a = (new Point(p1.X - WW_pressure_z0, p1.Y));
            Point b = (new Point(p1.X - WW_pressure_z15, p1.Y - SCALE_Y * 15.0));
            Point c = (new Point(p2.X - WW_pressure_zh, p2.Y));


            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnv_elev, a.X, a.Y, b.X, b.Y, System.Windows.Media.Brushes.Blue, 2.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnv_elev, b.X, b.Y, c.X, c.Y, System.Windows.Media.Brushes.Blue, 2.0);

            DrawingHelpersLibrary.DrawingHelpers.DrawText(cnv_elev, a.X, a.Y, 0, WW_pressure_z0.ToString(), System.Windows.Media.Brushes.Blue, 8.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawText(cnv_elev, b.X, b.Y, 0, WW_pressure_z15.ToString(), System.Windows.Media.Brushes.Blue, 8.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawText(cnv_elev, c.X, c.Y, 0, WW_pressure_zh.ToString(), System.Windows.Media.Brushes.Blue, 8.0);

            DrawingHelpersLibrary.DrawingHelpers.DrawArrowRight(cnv_elev, p1.X, p1.Y, System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Blue, 2.0, Math.Abs(WW_pressure_z0), Math.Abs(0.25 * WW_pressure_z0));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowRight(cnv_elev, p1.X, p1.Y - SCALE_Y * 15.0, System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Blue, 2.0, Math.Abs(WW_pressure_z0), Math.Abs(0.25 * WW_pressure_z0));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowRight(cnv_elev, p2.X, p2.Y, System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Blue, 2.0, Math.Abs(WW_pressure_zh), Math.Abs(0.25 * WW_pressure_zh));


            // Draw the leeward wall wind load
            double LW_pressure = GetWallPressure("Leeward Wall");

            Point e = (new Point(p4.X - LW_pressure, p4.Y));
            Point d = (new Point(p5.X - LW_pressure, p5.Y));


            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnv_elev, d.X, d.Y, e.X, e.Y, System.Windows.Media.Brushes.Red, 2.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawText(cnv_elev, e.X, e.Y, 0, LW_pressure.ToString(), System.Windows.Media.Brushes.Red, 8.0);

            DrawingHelpersLibrary.DrawingHelpers.DrawArrowRight(cnv_elev, d.X, d.Y, System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Red, 2.0, Math.Abs(LW_pressure), 0.25 * Math.Abs(LW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowRight(cnv_elev, e.X, e.Y, System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Red, 2.0, Math.Abs(LW_pressure), 0.25 * Math.Abs(LW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowRight(cnv_elev, 0.5 * (e.X + d.X), d.Y + 0.25 * (e.Y - d.Y), System.Windows.Media.Brushes.Red, System.Windows.Media.Brushes.Red, 2.0, Math.Abs(LW_pressure), 0.25 * Math.Abs(LW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowRight(cnv_elev, 0.5 * (e.X + d.X), d.Y + 0.5 * (e.Y - d.Y), System.Windows.Media.Brushes.Red, System.Windows.Media.Brushes.Red, 2.0, Math.Abs(LW_pressure), 0.25 * Math.Abs(LW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowRight(cnv_elev, 0.5 * (e.X + d.X), d.Y + 0.75 * (e.Y - d.Y), System.Windows.Media.Brushes.Red, System.Windows.Media.Brushes.Red, 2.0, Math.Abs(LW_pressure), 0.25 * Math.Abs(LW_pressure));

            if(_parameters.RidgeDirection == "Perpendicular to Wind" && _parameters.RoofPitch >= 10.0)
            {
                DrawRoofWind_PerpToRidge(cnv_elev, cnv_ht_elev, p2, p3, p4);
            } else
            {
                DrawRoofWind_Flat(cnv_elev, cnv_ht_elev);
            }
        }

        private void DrawRoofWind_Flat(Canvas cnv_elev, double cnv_ht_elev)
        {

        }

        private void DrawRoofWind_PerpToRidge(Canvas cnv_elev, double cnv_ht_elev, Point p2, Point p3, Point p4)
        {
            // Draw the windward wall wind load
            double WW_roof_pressure = GetRoofPressure("Windward Roof","A");
            double LW_roof_pressure = GetRoofPressure("Leeward Roof", "A");

            double roof_angle_deg_ww = _parameters.RoofPitch;
            double roof_angle_rad_ww = roof_angle_deg_ww * Math.PI / 180.0;

            double roof_angle_deg_lw = 180.0 - _parameters.RoofPitch;
            double roof_angle_rad_lw = roof_angle_deg_lw * Math.PI / 180.0;

            // find point that is perpendicular to line from p2 to p3 at a distance of WW_roof_pressure

            var delta_x_ww = -Math.Cos(roof_angle_rad_ww + Math.PI / 2.0) * WW_roof_pressure;
            var delta_y_ww = Math.Sin(roof_angle_rad_ww + Math.PI / 2.0) * WW_roof_pressure;

            var delta_x_lw = -Math.Cos(roof_angle_rad_lw - Math.PI / 2.0) * LW_roof_pressure;
            var delta_y_lw = Math.Sin(roof_angle_rad_lw - Math.PI / 2.0) * LW_roof_pressure;

            var a = new Point(p2.X + delta_x_ww, p2.Y + delta_y_ww);
            var b1 = new Point(p3.X + delta_x_ww, p3.Y + delta_y_ww);
            var b2 = new Point(p3.X + delta_x_lw, p3.Y + delta_y_lw);
            var c = new Point(p4.X + delta_x_lw, p4.Y + delta_y_lw);

            // Windward roof
            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnv_elev, a.X, a.Y, b1.X, b1.Y, System.Windows.Media.Brushes.Purple, 2.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnv_elev, a.X, a.Y, p2.X, p2.Y, System.Windows.Media.Brushes.Purple, 2.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnv_elev, p3.X, p3.Y, b1.X, b1.Y, System.Windows.Media.Brushes.Purple, 2.0);

            DrawingHelpersLibrary.DrawingHelpers.DrawText(cnv_elev, a.X, a.Y, 0, WW_roof_pressure.ToString(), System.Windows.Media.Brushes.Purple, 8.0);



            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnv_elev, b2.X, b2.Y, c.X, c.Y, System.Windows.Media.Brushes.Purple, 2.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnv_elev, b2.X, b2.Y, p3.X, p3.Y, System.Windows.Media.Brushes.Purple, 2.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnv_elev, p4.X, p4.Y, c.X, c.Y, System.Windows.Media.Brushes.Purple, 2.0);

            DrawingHelpersLibrary.DrawingHelpers.DrawText(cnv_elev, c.X, c.Y, 0, LW_roof_pressure.ToString(), System.Windows.Media.Brushes.Purple, 8.0);
        }
    }
}
