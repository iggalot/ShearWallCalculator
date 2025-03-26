using calculator;
using ShearWallCalculator;
using ShearWallVisualizer.Helpers;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;

namespace ShearWallVisualizer
{
    /// <summary>
    /// A drawing helper class for a main WPF canvas
    /// </summary>
    public class CanvasDrawer
    {
        // constants for properties of drawing objects on canvas
        public double rect_boundary_line_thickness = 0.5;

        private Canvas _canvas;
        private double SCALE_X = 1.0f, SCALE_Y = 1.0f;  // the scale factor at which the objects are to be drawn

        public CanvasDrawer(Canvas canvas, double scale_x, double scale_y)
        {
            _canvas = canvas;
            SCALE_X = scale_x;
            SCALE_Y = scale_y;
        }

        #region Drawing functions
        /// <summary>
        /// Draws a wall object on a wpf canvas
        /// </summary>
        /// <param name="wall"></param>
        /// 
        public void DrawWall(WallData wall)
        {
            WallDirs walldir = wall.WallDir;
            // Get the screen coords based on the world coords in the model
            Point canvas_start_pt = MathHelpers.WorldCoord_ToScreen(_canvas.Height, wall.Start, SCALE_X, SCALE_Y);
            Point canvas_end_pt = MathHelpers.WorldCoord_ToScreen(_canvas.Height, wall.End, SCALE_X, SCALE_Y);

            float screen_height = (float)(canvas_end_pt.Y - canvas_start_pt.Y);
            float screen_width = (float)(canvas_end_pt.X - canvas_start_pt.X);

            // set the width and height for the drawing object
            float width = (walldir == WallDirs.EastWest ? (float)Math.Abs(screen_width) : 1.0f);
            float height = (walldir == WallDirs.NorthSouth ? (float)Math.Abs(screen_height) : 1.0f);

            // Get the screen coords based on the world coords of the model
            Point p1_canvas_pt = MathHelpers.WorldCoord_ToScreen(_canvas.Height, wall.Start, SCALE_X, SCALE_Y);
            Point p2_canvas_pt = MathHelpers.WorldCoord_ToScreen(_canvas.Height, wall.End, SCALE_X, SCALE_Y);

            // add the rectangle using P4 (upper left) since rectangles are drawn by default from the upper left
            // the rectangular region object
            Rectangle rect = new Rectangle
            {
                Width = width,
                Height = height,
                Fill = Brushes.Red,
                Stroke = Brushes.Red,
                StrokeThickness = rect_boundary_line_thickness,
                Opacity = 0.5f
            };
            Canvas.SetLeft(rect, p1_canvas_pt.X);
            Canvas.SetTop(rect, p1_canvas_pt.Y);
            _canvas.Children.Add(rect);

        }

        /// <summary>
        /// Draws a diaphragm object on a wpf canvas
        /// </summary>
        /// <param name="diaphragm"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        public void DrawDiaphragm(DiaphragmData_Rectangular diaphragm)
        {
            // P3 (upper right) and P1 (lower left) are opposite corners of the diaphragm definition
            // Get the screen coords based on the world coords of the model
            Point p1_canvas_pt = MathHelpers.WorldCoord_ToScreen(_canvas.Height, diaphragm.P1, SCALE_X, SCALE_Y);
            Point p2_canvas_pt = MathHelpers.WorldCoord_ToScreen(_canvas.Height, diaphragm.P2, SCALE_X, SCALE_Y);
            Point p3_canvas_pt = MathHelpers.WorldCoord_ToScreen(_canvas.Height, diaphragm.P3, SCALE_X, SCALE_Y);
            Point p4_canvas_pt = MathHelpers.WorldCoord_ToScreen(_canvas.Height, diaphragm.P4, SCALE_X, SCALE_Y);

            // calculate the width and height of the diaphragm
            float diaphragm_height_screen = Math.Abs((float)(p3_canvas_pt.Y - p1_canvas_pt.Y));
            float diaphragm_width_screen = Math.Abs((float)(p3_canvas_pt.X - p1_canvas_pt.X));

            // add the rectangle using P4 (upper left) since rectangles are drawn by default from the upper left
            // the rectangular region object
            Rectangle rect = new Rectangle
            {
                Width = diaphragm_width_screen,
                Height = diaphragm_height_screen,
                Fill = Brushes.Red,
                Stroke = Brushes.Red,
                StrokeThickness = rect_boundary_line_thickness,
                Opacity = 0.5f,
                IsHitTestVisible = false
            };
            Canvas.SetLeft(rect, p4_canvas_pt.X);
            Canvas.SetTop(rect, p4_canvas_pt.Y);
            _canvas.Children.Add(rect);

            // marker for center of the rectangle -- center of area / mass
            Ellipse centerCircle = new Ellipse { Width = 4, Height = 4, Fill = Brushes.Red, Opacity = 1.0f };
            Canvas.SetLeft(centerCircle, p4_canvas_pt.X + diaphragm_width_screen / 2 - 2.0);
            Canvas.SetTop(centerCircle, p4_canvas_pt.Y + diaphragm_height_screen / 2 - 2.0);
            _canvas.Children.Add(centerCircle);
        }

        /// <summary>
        /// Adds a circular markers at specified point
        /// </summary>
        /// <param name="p1">Point at which to draw the marker</param>
        public void DrawCircles(Point p1, float dia, Brush color, float opacity = 1.0f)
        {
            Ellipse centerCircle = new Ellipse { Width = dia, Height = dia, Fill = color, Opacity = opacity };
            Canvas.SetLeft(centerCircle, p1.X - dia / 2.0); // shift the ellipse from center to upper left of ellipse
            Canvas.SetTop(centerCircle, p1.Y - dia / 2.0); // shift the ellipse from center to upper left of ellipse
            _canvas.Children.Add(centerCircle);
        }

        /// <summary>
        /// Draw the bounding box of the elements on the canvas
        /// <paramref name="world_p1">lower left coordinate of bounding box in world coordinates
        /// <paramref name="world_p2">upper right coordinate of bounding box in world coordinates
        /// <paramref name="SCALE_X">horizontal scaling factor
        /// <paramref name="SCALE_Y">vertical scaling factor
        /// </summary>
        public void DrawBoundingBox(Point world_p1, Point world_p2)
        {
            if(world_p1 == null || world_p2 == null)
            {
                return;
            }
            // convert the bounding box points to screen coordinates
            System.Windows.Point screen_bb_min_pt = MathHelpers.WorldCoord_ToScreen(_canvas.Height, world_p1, SCALE_X, SCALE_Y);
            System.Windows.Point screen_bb_max_pt = MathHelpers.WorldCoord_ToScreen(_canvas.Height, world_p2, SCALE_X, SCALE_Y);

            // retrieve the min and max values for the x and y coordinates of the bounding box
            double screen_bb_left = screen_bb_min_pt.X;  // x-min
            double screen_bb_top = screen_bb_max_pt.Y;  // y-max
            double screen_bb_right = screen_bb_max_pt.X;  // x-max
            double screen_bb_bottom = screen_bb_min_pt.Y;  // y- max

            Line topLine = new Line { X1 = screen_bb_left, Y1 = screen_bb_top, X2 = screen_bb_right, Y2 = screen_bb_top, Stroke = Brushes.Black, StrokeThickness = 2 * rect_boundary_line_thickness, StrokeDashArray = new DoubleCollection { 1, 1 } };
            Line bottomLine = new Line { X1 = screen_bb_left, Y1 = screen_bb_bottom, X2 = screen_bb_right, Y2 = screen_bb_bottom, Stroke = Brushes.Black, StrokeThickness = 2 * rect_boundary_line_thickness, StrokeDashArray = new DoubleCollection { 1, 1 } };
            Line leftLine = new Line { X1 = screen_bb_left, Y1 = screen_bb_top, X2 = screen_bb_left, Y2 = screen_bb_bottom, Stroke = Brushes.Black, StrokeThickness = 2 * rect_boundary_line_thickness, StrokeDashArray = new DoubleCollection { 1, 1 } };
            Line rightLine = new Line { X1 = screen_bb_right, Y1 = screen_bb_top, X2 = screen_bb_right, Y2 = screen_bb_bottom, Stroke = Brushes.Black, StrokeThickness = 2 * rect_boundary_line_thickness, StrokeDashArray = new DoubleCollection { 1, 1 } };

            // add the lines to the canvas control
            _canvas.Children.Add(topLine);
            _canvas.Children.Add(bottomLine);
            _canvas.Children.Add(leftLine);
            _canvas.Children.Add(rightLine);
        }

        /// <summary>
        /// Draw the non model canvas details 
        /// Use the function to add precalculated items to the canvas -- eliminates continuous recalculation
        /// -- gridlines
        /// </summary>
        public void DrawCanvasDetails(List<Shape> shapes)
        {
            if (shapes == null || shapes.Count == 0)
            {
                return;
            }
            foreach (var item in shapes)
            {
                if (item == null) continue;

                if (_canvas.Children.Contains(item) != true)
                {
                    _canvas.Children.Add(item);
                }
            }
        }



        /// <summary>
        /// Draw the preview shapes for point collection
        /// -- preview cursor image
        /// </summary>
        public void DrawPreviewObjects(List<Shape> prev_obj_shapes)
        {
            if (prev_obj_shapes == null || prev_obj_shapes.Count == 0)
            {
                return;
            }

            foreach (Shape item in prev_obj_shapes)
            {
                if (item == null) continue;

                if (_canvas.Children.Contains(item) != true)
                {
                    _canvas.Children.Add(item);
                }
            }
        }

        public void DrawBracedWallLines(WallSystem wall_system)
        {
            if(wall_system == null)
            {
                if (wall_system.BracedWallGroups_EW != null && wall_system.BracedWallGroups_EW.groupedValues != null)
                {
                    // search the EW braced wall lines
                    foreach (var wall in wall_system.BracedWallGroups_EW.groupedValues)
                    {

                        // draw a line at the center of all of the values in this group
                        double first = wall[0];
                        double last = wall[wall.Count - 1];
                        double center = (first + last) / 2;
                        Point p1_world = new Point(-3000, center);
                        Point p2_world = new Point(3000, center);
                        Point p1_screen = MathHelpers.WorldCoord_ToScreen(_canvas.Height, p1_world, SCALE_X, SCALE_Y);
                        Point p2_screen = MathHelpers.WorldCoord_ToScreen(_canvas.Height, p2_world, SCALE_X, SCALE_Y);

                        Line line = new Line { X1 = p1_screen.X, Y1 = p1_screen.Y, X2 = p2_screen.X, Y2 = p2_screen.Y, Stroke = Brushes.Red, StrokeDashArray = new DoubleCollection { 4, 2 }, StrokeThickness = 0.5 };
                        _canvas.Children.Add(line);
                    }
                }
                if (wall_system.BracedWallGroups_NS != null && wall_system.BracedWallGroups_NS.groupedValues != null)
                {
                    // Draw the east west braced wall lines
                    foreach (var wall in wall_system.BracedWallGroups_NS.groupedValues)
                    {
                        // draw a line at the center of all of the values in this group
                        float first = (float)wall[0];
                        float last = (float)wall[wall.Count - 1];
                        float center = (first + last) / 2;
                        Point p1_world = new Point(center, -3000);
                        Point p2_world = new Point(center, 3000);
                        Point p1_screen = MathHelpers.WorldCoord_ToScreen(_canvas.Height, p1_world, SCALE_X, SCALE_Y);
                        Point p2_screen = MathHelpers.WorldCoord_ToScreen(_canvas.Height, p2_world, SCALE_X, SCALE_Y);

                        Line line = new Line { X1 = p1_screen.X, Y1 = p1_screen.Y, X2 = p2_screen.X, Y2 = p2_screen.Y, Stroke = Brushes.Red, StrokeDashArray = new DoubleCollection { 4, 2 }, StrokeThickness = 0.5 };
                        _canvas.Children.Add(line);
                    }
                }
            }
        }

        /// <summary>
        /// For drawing text labels and other info for walls
        /// </summary>
        public void DrawWallsInfo(WallSystem wall_system)
        {
            if (wall_system != null)
            {
                if (wall_system.EW_Walls != null && wall_system.EW_Walls.Count > 0)
                {
                    // East-West walls
                    foreach (var wall in wall_system.EW_Walls)
                    {
                        System.Windows.Point cp = MathHelpers.WorldCoord_ToScreen(_canvas.Height, wall.Value.Center, SCALE_X, SCALE_Y);

                        // number label for the wall ID
                        DrawingHelpersLibrary.DrawingHelpers.DrawText
                            (
                            _canvas,
                            cp.X + 1,
                            cp.Y + 1,
                            0,
                            wall.Key.ToString(),
                            System.Windows.Media.Brushes.Black,
                            6
                            );
                    }
                }
                if (wall_system.NS_Walls != null && wall_system.NS_Walls.Count > 0)
                {
                    //North-South walls
                    foreach (var wall in wall_system.NS_Walls)
                    {
                        System.Windows.Point cp = MathHelpers.WorldCoord_ToScreen(_canvas.Height, wall.Value.Center, SCALE_X, SCALE_Y);

                        DrawingHelpersLibrary.DrawingHelpers.DrawText
                            (
                            _canvas,
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
        public void DrawDiaphragmsInfo(DiaphragmSystem diaphragm_system)
        {
            //Draw the additional wall info including labels for the center point
            if (diaphragm_system != null)
            {
                // East-West walls
                if (diaphragm_system._diaphragms.Count > 0)
                {
                    foreach (var diaphragm in diaphragm_system._diaphragms)
                    {
                        System.Windows.Point cp = MathHelpers.WorldCoord_ToScreen(_canvas.Height, diaphragm.Value.Centroid, SCALE_X, SCALE_Y);


                        // number label for the wall ID
                        DrawingHelpersLibrary.DrawingHelpers.DrawText
                            (
                            _canvas,
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
        public void DrawCOR(WallSystem wall_system)
        {
            // Draw the Center of Rigiidity Point
            System.Windows.Point cor_pt = MathHelpers.WorldCoord_ToScreen(_canvas.Height, wall_system.CtrRigidity, SCALE_X, SCALE_Y);
            if (wall_system != null)
            {
                DrawingHelpersLibrary.DrawingHelpers.DrawCircle(
                    _canvas,
                    cor_pt.X,
                    cor_pt.Y,
                    System.Windows.Media.Brushes.Blue,
                    System.Windows.Media.Brushes.Blue,
                    4,
                    0.5);
                DrawingHelpersLibrary.DrawingHelpers.DrawText(
                    _canvas,
                    cor_pt.X + 1.5,
                    cor_pt.Y - 10,
                    0,
                    "C.R",
                    System.Windows.Media.Brushes.Black,
                    4
                    );
            }
        }

        /// <summary>
        /// Handles drawing the Center of Mass and Center of Rigidity points
        /// </summary>
        public void DrawCOM(DiaphragmSystem diaphragm_system)
        {
            // if we have a defined diaphragm, draw the centroid in screen coords
            System.Windows.Point p1 = MathHelpers.WorldCoord_ToScreen(_canvas.Height, diaphragm_system.CtrMass, SCALE_X, SCALE_Y);
            if (diaphragm_system != null)
            {
                // Draw the Center of Rigidity Point
                DrawingHelpersLibrary.DrawingHelpers.DrawCircle(
                    _canvas,
                    p1.X, p1.Y,
                    System.Windows.Media.Brushes.Red,
                    System.Windows.Media.Brushes.Red,
                    4,
                    1);
                DrawingHelpersLibrary.DrawingHelpers.DrawText(
                    _canvas,
                    p1.X + 1.5,
                    p1.Y + 2,
                    0,
                    "C.M",
                    System.Windows.Media.Brushes.Black,
                    4
                    );
            }
        }
        #endregion

        public void DrawPreviewLine(Line previewLine, Point start, Point end)
        {
            previewLine.X1 = start.X;
            previewLine.Y1 = start.Y;
            previewLine.X2 = end.X;
            previewLine.Y2 = end.Y;

            _canvas.Children.Add(previewLine);
        }

        public void DrawCrosshairs(Point point, Brush color)
        {
            // creates the crosshairs for point selection
            Line _crosshairVertical = new Line
            {
                X1 = point.X,
                Y1 = 0,
                X2 = point.X,
                Y2 = _canvas.Height,
                Stroke = color,
                StrokeThickness = 0.25,
                IsHitTestVisible = false
            };

            // creates the crosshairs for point selection
            Line _crosshairHorizontal = new Line
            {
                Y1 = point.Y,
                X1 = 0,
                Y2 = point.Y,
                X2 = _canvas.Width,
                Stroke = Brushes.Black,
                StrokeThickness = 0.25,
                IsHitTestVisible = false
            };

            _canvas.Children.Add(_crosshairVertical);
            _canvas.Children.Add(_crosshairHorizontal);
        }
    }
}
