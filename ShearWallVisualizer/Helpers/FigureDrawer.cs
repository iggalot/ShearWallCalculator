using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ShearWallCalculator.WindLoadCalculations.Chapter30.Figure30_3
{
    /// <summary>
    /// Helper that draws a Chapter30 Figure to a specified Canvas
    /// </summary>
    public static class FigureDrawer
    {
        public static void DrawCurvesOnCanvas(Canvas canvas, Chapter30_BaseFigure figure)
        {
            double canvasWidth = canvas.ActualWidth > 0 ? canvas.ActualWidth : canvas.Width;  // fallback default
            double canvasHeight = canvas.ActualHeight > 0 ? canvas.ActualHeight : canvas.Height;  // fallback default

            double xMin = 1;
            double xMax = 1000;

            double yMinNeg = -4; // negative GCp top
            double yMaxNeg = 0;

            double yMinPos = 0;
            double yMaxPos = 1;

            int samplePoints = 100;

            canvas.Children.Clear();

            // Background grid (optional)
            DrawGrid(canvas, canvasWidth, canvasHeight, xMin, xMax, yMinNeg, yMaxPos);

            foreach (var kvp in figure.RoofCurves_Neg)
            {
                DrawCurve(canvas, kvp.Value, kvp.Key, Brushes.Red, true, canvasWidth, canvasHeight, xMin, xMax, yMinNeg, yMaxPos, samplePoints);
            }

            foreach (var kvp in figure.RoofCurves_Pos)
            {
                DrawCurve(canvas, kvp.Value, kvp.Key, Brushes.Blue, false, canvasWidth, canvasHeight, xMin, xMax, yMinNeg, yMaxPos, samplePoints);
            }
        }

        private static void DrawCurve(Canvas canvas, ExternalGCpCurve curve, string label, Brush color, bool isNegative,
                               double canvasWidth, double canvasHeight,
                               double xMin, double xMax, double yMin, double yMax, int samplePoints)
        {
            Polyline line = new Polyline
            {
                Stroke = color,
                StrokeThickness = 2
            };

            // Sample from xMin (1) to 1200 regardless of curve.X1/X2
            double plotMin = 1;
            double plotMax = 1200;

            for (int i = 0; i <= samplePoints; i++)
            {
                double x = plotMin + i * (plotMax - plotMin) / samplePoints;
                double y = curve.Evaluate(x);

                // Normalize to pixel positions
                double px = ((x - xMin) / (xMax - xMin)) * canvasWidth;
                px = Math.Max(0, Math.Min(canvasWidth, px)); // Clamp to canvas

                double normY = (y - yMin) / (yMax - yMin); // yMin = -4.0, yMax = 1.0
                double py = normY * canvasHeight;

                line.Points.Add(new Point(px, py));
            }

            canvas.Children.Add(line);

            // Label at end of line
            TextBlock labelText = new TextBlock
            {
                Text = label,
                Foreground = color,
                FontSize = 10
            };

            double labelX = line.Points[line.Points.Count - 1].X + 4;
            double labelY = line.Points[line.Points.Count - 1].Y;

            Canvas.SetLeft(labelText, labelX);
            Canvas.SetTop(labelText, labelY);
            canvas.Children.Add(labelText);
        }

        private static void DrawGrid(Canvas canvas, double canvasWidth, double canvasHeight,
                             double xMin, double xMax, double yMin, double yMax)
        {
            // yMin should be -4 and yMax should be 1
            // Vertical grid lines at specified x-values
            double[] verticalXs = new double[] { 10, 20, 50, 100, 200, 500, 1000 };
            foreach (double xVal in verticalXs)
            {
                // Skip x-values outside the defined domain (if any)
                if (xVal < xMin || xVal > xMax)
                    continue;

                double normX = (xVal - xMin) / (xMax - xMin);
                double px = normX * canvasWidth;

                // Create a dashed vertical line
                Line vLine = new Line
                {
                    X1 = px,
                    Y1 = 0,
                    X2 = px,
                    Y2 = canvasHeight,
                    Stroke = Brushes.LightGray,
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection { 2, 2 }
                };
                canvas.Children.Add(vLine);

                // Add a label at the bottom (or center-bottom of the plot area)
                TextBlock xLabel = new TextBlock
                {
                    Text = xVal.ToString(),
                    FontSize = 10,
                    Foreground = Brushes.Gray
                };
                // Position label near the bottom of the canvas.
                Canvas.SetLeft(xLabel, px + 2);
                // For vertical position, you may choose to place it at canvasHeight - 15
                Canvas.SetTop(xLabel, canvasHeight - 15);
                canvas.Children.Add(xLabel);
            }

            // Horizontal grid lines: from y = -4.0 to y = 1.0 in increments of 0.2
            for (double yVal = yMin; yVal <= yMax; yVal += 0.2)
            {
                double normY = (yVal - yMin) / (yMax - yMin);
                double py = normY * canvasHeight;

                // Create a dashed horizontal line
                Line hLine = new Line
                {
                    X1 = 0,
                    Y1 = py,
                    X2 = canvasWidth,
                    Y2 = py,
                    Stroke = Brushes.LightGray,
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection { 2, 2 }
                };
                canvas.Children.Add(hLine);

                // Add a label on the left side for each horizontal tick
                TextBlock yLabel = new TextBlock
                {
                    Text = yVal.ToString("F1"),
                    FontSize = 10,
                    Foreground = Brushes.Gray
                };
                // Position the label with some left margin (e.g., 2 pixels) and offset vertically.
                Canvas.SetLeft(yLabel, 2);
                Canvas.SetTop(yLabel, py - 10);
                canvas.Children.Add(yLabel);
            }
        }



    }
}
