using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ShearWallVisualizer.Controls
{
    public enum WindLoadCases
    {
        WLC_BaseA,
        WLC_BaseB,
        WLC_Balloon1,
        WLC_Balloon2,
        WLC_Suction1,
        WLC_Suction2
    }
    /// <summary>
    /// Interaction logic for WindLoadGraphicCanvas.xaml
    /// </summary>
    public partial class WindLoadGraphicCanvas : UserControl
    {
        private double _margin = 5; 

        private double SCALE_X = 3.0;
        private double SCALE_Y = 3.0;

        private double cnv_ht_elev, cnv_width_elev, cnv_ht_plan, cnv_width_plan;

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

            cnv_ht_elev = cnv_elev.ActualHeight;   // <-- Use ActualHeight
            cnv_width_elev = cnv_elev.ActualWidth; // <-- Use ActualWidth

            cnv_ht_plan = cnv_plan.ActualHeight;   // <-- Use ActualHeight
            cnv_width_plan = cnv_plan.ActualWidth; // <-- Use ActualWidth

            SCALE_X = (cnv_width_elev - 2 * _margin) / length;
            SCALE_Y = Math.Min((cnv_ht_elev - 2 * _margin) / ridge_ht, (cnv_ht_plan - 2 * _margin) / width);

            RedrawCanvas();

        }

        private void DrawPlanWindLoads(WindLoadCases load_case_num)
        {
            double offset = _margin;
            // Draw the plan frame
            Point p1 = new Point(offset, cnv_ht_plan - offset);
            Point p2 = new Point(offset + SCALE_X * _parameters.BuildingLength, cnv_ht_plan - offset);
            Point p3 = new Point(offset + SCALE_X * _parameters.BuildingLength, cnv_ht_plan - offset - SCALE_Y * _parameters.BuildingWidth);
            Point p4 = new Point(offset, cnv_ht_plan - offset - SCALE_Y * _parameters.BuildingWidth);
           
            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvPlanView, p1.X, p1.Y, p2.X, p2.Y, System.Windows.Media.Brushes.Black, 2.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvPlanView, p2.X, p2.Y, p3.X, p3.Y, System.Windows.Media.Brushes.Black, 2.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvPlanView, p3.X, p3.Y, p4.X, p4.Y, System.Windows.Media.Brushes.Black, 2.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvPlanView, p4.X, p4.Y, p1.X, p1.Y, System.Windows.Media.Brushes.Black, 2.0);

            // Draw the windward wall wind load
            double WW_pressure = GetWallPressure("Windward Wall - z=h", load_case_num);
            Point a = (new Point(offset - WW_pressure, p1.Y));
            Point b = (new Point(offset - WW_pressure, p4.Y));
            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvPlanView, a.X, a.Y, b.X, b.Y, System.Windows.Media.Brushes.Blue, 2.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawText(cnvPlanView, a.X, a.Y, 0, WW_pressure.ToString(), System.Windows.Media.Brushes.Blue, 8.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowRight(cnvPlanView, p1.X, p1.Y, System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Blue, 2.0, Math.Abs(WW_pressure), Math.Abs(0.25 * WW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowRight(cnvPlanView, p4.X, p4.Y, System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Blue, 2.0, Math.Abs(WW_pressure), Math.Abs(0.25 * WW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowRight(cnvPlanView, 0.5 * (p4.X + p1.X), p1.Y + 0.25 * (p4.Y - p1.Y), System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Blue, 2.0, Math.Abs(WW_pressure), Math.Abs(0.25 * WW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowRight(cnvPlanView, 0.5 * (p4.X + p1.X), p1.Y + 0.5 * (p4.Y - p1.Y), System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Blue, 2.0, Math.Abs(WW_pressure), Math.Abs(0.25 * WW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowRight(cnvPlanView, 0.5 * (p4.X + p1.X), p1.Y + 0.75 * (p4.Y - p1.Y), System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Blue, 2.0, Math.Abs(WW_pressure), Math.Abs(0.25 * WW_pressure));

            // Draw the leeward wall wind load
            double LW_pressure = GetWallPressure("Leeward Wall", load_case_num);
            Point c = (new Point(p2.X - LW_pressure, p2.Y));
            Point d = (new Point(p3.X - LW_pressure, p3.Y));
            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvPlanView, c.X, c.Y, d.X, d.Y, System.Windows.Media.Brushes.Red, 2.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawText(cnvPlanView, c.X, c.Y, 0, LW_pressure.ToString(), System.Windows.Media.Brushes.Red, 8.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowRight(cnvPlanView, c.X, c.Y, System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Red, 2.0, Math.Abs(LW_pressure), 0.25 * Math.Abs(LW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowRight(cnvPlanView, d.X, d.Y, System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Red, 2.0, Math.Abs(LW_pressure), 0.25 * Math.Abs(LW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowRight(cnvPlanView, 0.5 * (c.X + d.X), c.Y + 0.25 * (d.Y - c.Y), System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Red, 2.0, Math.Abs(LW_pressure), 0.25 * Math.Abs(LW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowRight(cnvPlanView, 0.5 * (c.X + d.X), c.Y + 0.5 * (d.Y - c.Y), System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Red, 2.0, Math.Abs(LW_pressure), 0.25 * Math.Abs(LW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowRight(cnvPlanView, 0.5 * (c.X + d.X), c.Y +0.75 * (d.Y - c.Y), System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Red, 2.0, Math.Abs(LW_pressure), 0.25 * Math.Abs(LW_pressure));

            // Draw the sidewall wall wind load -- Upper plan view wall
            double SW_pressure = GetWallPressure("Sidewall", load_case_num);
            Point e = (new Point(p3.X, p3.Y + SW_pressure));
            Point f = (new Point(p4.X, p4.Y + SW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvPlanView, e.X, e.Y, f.X, f.Y, System.Windows.Media.Brushes.Green, 2.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawText(cnvPlanView, e.X, e.Y, 0, SW_pressure.ToString(), System.Windows.Media.Brushes.Green, 8.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowUp(cnvPlanView, e.X, e.Y, System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Green, 2.0, Math.Abs(SW_pressure), 0.25 * Math.Abs(SW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowUp(cnvPlanView, f.X, f.Y, System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Green, 2.0, Math.Abs(SW_pressure), 0.25 * Math.Abs(SW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowUp(cnvPlanView, 0.25 * (e.X + f.X), 0.5 * (e.Y + f.Y), System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Green, 2.0, Math.Abs(SW_pressure), 0.25 * Math.Abs(SW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowUp(cnvPlanView, 0.5 * (e.X + f.X), 0.5 * (e.Y + f.Y), System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Green, 2.0, Math.Abs(SW_pressure), 0.25 * Math.Abs(SW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowUp(cnvPlanView, 0.75 * (e.X + f.X), 0.5 * (e.Y + f.Y), System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Green, 2.0, Math.Abs(SW_pressure), 0.25 * Math.Abs(SW_pressure));

            Point g = (new Point(p1.X, p1.Y - SW_pressure));
            Point h = (new Point(p2.X, p2.Y - SW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvPlanView, g.X, g.Y, h.X, h.Y, System.Windows.Media.Brushes.Green, 2.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawText(cnvPlanView, g.X, g.Y, 0, SW_pressure.ToString(), System.Windows.Media.Brushes.Green, 8.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowDown(cnvPlanView, g.X, g.Y, System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Green, 2.0, Math.Abs(SW_pressure), 0.25 * Math.Abs(SW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowDown(cnvPlanView, h.X, h.Y, System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Green, 2.0, Math.Abs(SW_pressure), 0.25 * Math.Abs(SW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowDown(cnvPlanView, 0.25 * (g.X + h.X), 0.5 * (g.Y + h.Y), System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Green, 2.0, Math.Abs(SW_pressure), 0.25 * Math.Abs(SW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowDown(cnvPlanView, 0.5 * (g.X + h.X), 0.5 * (g.Y + h.Y), System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Green, 2.0, Math.Abs(SW_pressure), 0.25 * Math.Abs(SW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowDown(cnvPlanView, 0.75 * (g.X + h.X), 0.5 * (g.Y + h.Y), System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Green, 2.0, Math.Abs(SW_pressure), 0.25 * Math.Abs(SW_pressure));
        }

        private double GetWallPressure(string v, WindLoadCases load_case_num)
        {
            foreach (var result in _wall_results)
            {
                if (result.Surface == v)
                {
                    switch (load_case_num)
                    {
                        case WindLoadCases.WLC_BaseA:
                            return result.PressBaseA;
                        case WindLoadCases.WLC_Balloon1:
                            return result.Balloon1;
                        case WindLoadCases.WLC_Suction1:
                            return result.Suction1;
                        default:
                            return 0;
                    }
                }
            }
            return 0;
        }

        private double GetRoofPressure(string v, WindLoadCases load_case_num)
        {
            foreach (var result in _roof_results)
            {
                if (result.Surface == v)
                {
                    switch (load_case_num)
                    {
                        case WindLoadCases.WLC_BaseA:
                            return result.PressBaseA;
                        case WindLoadCases.WLC_BaseB:
                            return result.PressBaseB;
                        case WindLoadCases.WLC_Balloon1:
                            return result.Balloon1;
                        case WindLoadCases.WLC_Balloon2:
                            return result.Balloon2;
                        case WindLoadCases.WLC_Suction1:
                            return result.Suction1;
                        case WindLoadCases.WLC_Suction2:
                            return result.Suction2;
                        default:
                            return 0;
                    }
                        
                }
            }
            return 0;
        }

        private void DrawElevationWindLoads(WindLoadCases load_case_num)
        {
            double offset = _margin;

            // Draw the elevation frame
            Point p1 = new Point(offset, cnv_ht_elev - offset);
            Point p2 = new Point(offset, cnv_ht_elev - offset - SCALE_Y * _parameters.BuildingHeight);
            Point p3 = new Point(offset + SCALE_X * _parameters.BuildingLength * 0.5, cnv_ht_elev - offset - SCALE_Y * (_parameters.BuildingHeight + _parameters.BuildingLength * 0.5 * Math.Sin(_parameters.RoofPitch * 2.0 * Math.PI / 360)));
            Point p4 = new Point(offset + SCALE_X * _parameters.BuildingLength, cnv_ht_elev - offset - SCALE_Y * _parameters.BuildingHeight);
            Point p5 = new Point(offset + SCALE_X * _parameters.BuildingLength, cnv_ht_elev - offset);

            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvElevationView, p1.X, p1.Y, p2.X, p2.Y, System.Windows.Media.Brushes.Black, 2.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvElevationView, p2.X, p2.Y, p3.X, p3.Y, System.Windows.Media.Brushes.Black, 2.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvElevationView, p3.X, p3.Y, p4.X, p4.Y, System.Windows.Media.Brushes.Black, 2.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvElevationView, p4.X, p4.Y, p5.X, p5.Y, System.Windows.Media.Brushes.Black, 2.0);

            // Draw the windward wall wind load
            double WW_pressure_z0 = GetWallPressure("Windward Wall - z=0ft", load_case_num);
            double WW_pressure_z15 = WW_pressure_z0;

            if (_parameters.BuildingHeight >= 15.0)
            {
                WW_pressure_z15 = GetWallPressure("Windward Wall - z=15ft", load_case_num);
            } 
            double WW_pressure_zh = GetWallPressure("Windward Wall - z=h", load_case_num);

            Point a = (new Point(p1.X - WW_pressure_z0, p1.Y));
            Point b = (new Point(p1.X - WW_pressure_z15, p1.Y - SCALE_Y * 15.0));
            Point c = (new Point(p2.X - WW_pressure_zh, p2.Y));


            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvElevationView, a.X, a.Y, b.X, b.Y, System.Windows.Media.Brushes.Blue, 2.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvElevationView, b.X, b.Y, c.X, c.Y, System.Windows.Media.Brushes.Blue, 2.0);

            DrawingHelpersLibrary.DrawingHelpers.DrawText(cnvElevationView, a.X, a.Y, 0, WW_pressure_z0.ToString(), System.Windows.Media.Brushes.Blue, 8.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawText(cnvElevationView, b.X, b.Y, 0, WW_pressure_z15.ToString(), System.Windows.Media.Brushes.Blue, 8.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawText(cnvElevationView, c.X, c.Y, 0, WW_pressure_zh.ToString(), System.Windows.Media.Brushes.Blue, 8.0);

            DrawingHelpersLibrary.DrawingHelpers.DrawArrowRight(cnvElevationView, p1.X, p1.Y, System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Blue, 2.0, Math.Abs(WW_pressure_z0), Math.Abs(0.25 * WW_pressure_z0));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowRight(cnvElevationView, p1.X, p1.Y - SCALE_Y * 15.0, System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Blue, 2.0, Math.Abs(WW_pressure_z0), Math.Abs(0.25 * WW_pressure_z0));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowRight(cnvElevationView, p2.X, p2.Y, System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Blue, 2.0, Math.Abs(WW_pressure_zh), Math.Abs(0.25 * WW_pressure_zh));


            // Draw the leeward wall wind load
            double LW_pressure = GetWallPressure("Leeward Wall", load_case_num);

            Point e = (new Point(p4.X - LW_pressure, p4.Y));
            Point d = (new Point(p5.X - LW_pressure, p5.Y));


            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvElevationView, d.X, d.Y, e.X, e.Y, System.Windows.Media.Brushes.Red, 2.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawText(cnvElevationView, e.X, e.Y, 0, LW_pressure.ToString(), System.Windows.Media.Brushes.Red, 8.0);

            DrawingHelpersLibrary.DrawingHelpers.DrawArrowRight(cnvElevationView, d.X, d.Y, System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Red, 2.0, Math.Abs(LW_pressure), 0.25 * Math.Abs(LW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowRight(cnvElevationView, e.X, e.Y, System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Red, 2.0, Math.Abs(LW_pressure), 0.25 * Math.Abs(LW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowRight(cnvElevationView, 0.5 * (e.X + d.X), d.Y + 0.25 * (e.Y - d.Y), System.Windows.Media.Brushes.Red, System.Windows.Media.Brushes.Red, 2.0, Math.Abs(LW_pressure), 0.25 * Math.Abs(LW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowRight(cnvElevationView, 0.5 * (e.X + d.X), d.Y + 0.5 * (e.Y - d.Y), System.Windows.Media.Brushes.Red, System.Windows.Media.Brushes.Red, 2.0, Math.Abs(LW_pressure), 0.25 * Math.Abs(LW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowRight(cnvElevationView, 0.5 * (e.X + d.X), d.Y + 0.75 * (e.Y - d.Y), System.Windows.Media.Brushes.Red, System.Windows.Media.Brushes.Red, 2.0, Math.Abs(LW_pressure), 0.25 * Math.Abs(LW_pressure));

            DrawRoofWind(load_case_num, p2, p3, p4);
        }

        private void DrawRoofWind_Flat(WindLoadCases load_case_num)
        {

        }

        private void DrawRoofWind(WindLoadCases load_case_num, Point p2, Point p3, Point p4)
        {
            if (_parameters.RidgeDirection == "Perpendicular to Wind" && _parameters.RoofPitch >= 10.0)
            {
                DrawRoofWind_PerpToRidge(load_case_num, p2, p3, p4);
            }
            else
            {
                DrawRoofWind_Flat(load_case_num);
            }
        }

        private void DrawRoofWind_PerpToRidge(WindLoadCases load_case_num, Point p2, Point p3, Point p4)
        {
            // Draw the windward wall wind load
            double WW_roof_pressure = GetRoofPressure("Windward Roof", load_case_num);
            double LW_roof_pressure = GetRoofPressure("Leeward Roof", load_case_num);

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
            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvElevationView, a.X, a.Y, b1.X, b1.Y, System.Windows.Media.Brushes.Purple, 2.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvElevationView, a.X, a.Y, p2.X, p2.Y, System.Windows.Media.Brushes.Purple, 2.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvElevationView, p3.X, p3.Y, b1.X, b1.Y, System.Windows.Media.Brushes.Purple, 2.0);

            DrawingHelpersLibrary.DrawingHelpers.DrawText(cnvElevationView, a.X, a.Y, 0, WW_roof_pressure.ToString(), System.Windows.Media.Brushes.Purple, 8.0);



            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvElevationView, b2.X, b2.Y, c.X, c.Y, System.Windows.Media.Brushes.Purple, 2.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvElevationView, b2.X, b2.Y, p3.X, p3.Y, System.Windows.Media.Brushes.Purple, 2.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvElevationView, p4.X, p4.Y, c.X, c.Y, System.Windows.Media.Brushes.Purple, 2.0);

            DrawingHelpersLibrary.DrawingHelpers.DrawText(cnvElevationView, c.X, c.Y, 0, LW_roof_pressure.ToString(), System.Windows.Media.Brushes.Purple, 8.0);
        }

        private void LoadCase_CheckedChanged(object sender, RoutedEventArgs e)
        {
            RedrawCanvas();
        }

        private void RedrawCanvas()
        {
            cnvPlanView.Children.Clear();
            cnvElevationView.Children.Clear();

            if (chkLC1.IsChecked == true)
            {
                DrawPlanWindLoads(WindLoadCases.WLC_BaseA);
                DrawElevationWindLoads(WindLoadCases.WLC_BaseA);
            }
            if (chkLC2.IsChecked == true)
            { 
                DrawPlanWindLoads(WindLoadCases.WLC_BaseB);
                DrawElevationWindLoads(WindLoadCases.WLC_BaseB);
            }
            if (chkLC3.IsChecked == true)
            {
                DrawPlanWindLoads(WindLoadCases.WLC_Balloon1);
                DrawElevationWindLoads(WindLoadCases.WLC_Balloon1);
            }
            if (chkLC4.IsChecked == true)
            {
                DrawPlanWindLoads(WindLoadCases.WLC_Balloon2);
                DrawElevationWindLoads(WindLoadCases.WLC_Balloon2);
            }
            if (chkLC5.IsChecked == true)
            {
                DrawPlanWindLoads(WindLoadCases.WLC_Suction1);
                DrawElevationWindLoads(WindLoadCases.WLC_Suction1);
            }
            if (chkLC6.IsChecked == true)
            {
                DrawPlanWindLoads(WindLoadCases.WLC_Suction2);
                DrawElevationWindLoads(WindLoadCases.WLC_Suction2);
            }
        }

        private Brush GetColorForLoadCase(int loadCaseNumber)
        {
            // Simple color mapping
            switch (loadCaseNumber)
            {
                case 1: return Brushes.Red;
                case 2: return Brushes.Blue;
                case 3: return Brushes.Green;
                case 4: return Brushes.Orange;
                case 5: return Brushes.Purple;
                case 6: return Brushes.Teal;
                default: return Brushes.Black;
            }
        }
    }
}
