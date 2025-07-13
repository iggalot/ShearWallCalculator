using System;
using System.Collections.Generic;
using System.Linq;
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
            double canvasWidth = canvas.ActualWidth > 0 ? canvas.ActualWidth : canvas.Width;
            double canvasHeight = canvas.ActualHeight > 0 ? canvas.ActualHeight : canvas.Height;

            double xMin = 1;
            double xMax = 1000;
            double yMinNeg = -4;
            double yMaxPos = 1;

            canvas.Children.Clear();

            List<double> xTickValues = new List<double> { 1, 10, 20, 50, 100, 200, 500, 1000 };
            DrawGrid(canvas, canvasWidth, canvasHeight, xMin, xMax, yMinNeg, yMaxPos, xTickValues);

            // Combine both positive and negative curves with polarity
            var allCurves = new List<(string label, ExternalGCpCurve curve, bool isNegative)>();
            foreach (var kvp in figure.RoofCurves_Neg)
                allCurves.Add((kvp.Key, kvp.Value, true));
            foreach (var kvp in figure.RoofCurves_Pos)
                allCurves.Add((kvp.Key, kvp.Value, false));

            // Group by curve shape (not label)
            var groupedByShape = allCurves
                .GroupBy(item => GetCurveShapeKey(item.curve))
                .ToDictionary(g => g.Key, g => g.ToList());

            // Draw each group with horizontal spread based on per-group zone index
            foreach (var group in groupedByShape)
            {
                var curveGroup = group.Value;

                for (int i = 0; i < curveGroup.Count; i++)
                {
                    var (label, curve, isNegative) = curveGroup[i];
                    Brush color = isNegative ? Brushes.Red : Brushes.Blue;

                    DrawCurve(canvas, curve, label, color, isNegative,
                              canvasWidth, canvasHeight, xMin, xMax, yMinNeg, yMaxPos,
                              zoneIndex: i);
                }
            }
        }


        private static string GetCurveShapeKey(ExternalGCpCurve curve)
        {
            // Use rounded values of key points to group similar curves
            double x1 = Math.Round(curve.X1, 3);
            double x2 = Math.Round(curve.X2, 3);

            double y1 = Math.Round(curve.Evaluate(x1), 3);
            double y2 = Math.Round(curve.Evaluate(x2), 3);

            return $"{x1:F3}_{x2:F3}_{y1:F3}_{y2:F3}";
        }


        private static string ExtractGroupingKey(string label)
        {
            // For example: "Gable Perpendicular Zone 1" => "Gable Perpendicular"
            int zoneIndex = label.IndexOf("Zone", StringComparison.OrdinalIgnoreCase);
            if (zoneIndex >= 0)
            {
                return label.Substring(0, zoneIndex).Trim();
            }

            return label.Trim(); // fallback
        }

        // Helper to extract a line key from zone label
        // You will want to customize this based on your label format and how zones are assigned to curves
        private static string ExtractLineKey(string label)
        {
            // For example: label = "Zone 3" -> return "Zone"
            // Or if labels are like "LineA Zone 3" you could parse differently
            var parts = label.Split(' ');
            if (parts.Length >= 2)
            {
                // Return all except last part (assumed zone number)
                return string.Join(" ", parts, 0, parts.Length - 1);
            }
            return label; // fallback
        }


        private static void DrawCurve(Canvas canvas, ExternalGCpCurve curve, string label, Brush color, bool isNegative,
                                      double canvasWidth, double canvasHeight,
                                      double xMin, double xMax, double yMin, double yMax,
                                      int zoneIndex)  // local index of this zone within the curve’s zones
        {
            Polyline line = new Polyline
            {
                Stroke = color,
                StrokeThickness = 2
            };

            double plotMin = Math.Max(1.0, curve.LowerBoundX);
            double plotMax = Math.Min(1200.0, curve.UpperBoundX);

            List<double> xPoints = new List<double>();
            if (plotMin < curve.X1) xPoints.Add(plotMin);
            xPoints.Add(curve.X1);
            xPoints.Add(curve.X2);
            if (plotMax > curve.X2) xPoints.Add(plotMax);

            List<Point> linePoints = new List<Point>();

            foreach (double x in xPoints)
            {
                double y = curve.Evaluate(x);
                double px = ((Math.Log10(x) - Math.Log10(xMin)) / (Math.Log10(xMax) - Math.Log10(xMin))) * canvasWidth;
                px = Math.Max(0, Math.Min(canvasWidth, px));
                double py = ((y - yMin) / (yMax - yMin)) * canvasHeight;
                linePoints.Add(new Point(px, py));
            }

            foreach (Point pt in linePoints)
                line.Points.Add(pt);

            canvas.Children.Add(line);

            // === Label: strip "Zone" and show the rest ===
            string displayLabel = label.StartsWith("Zone", StringComparison.OrdinalIgnoreCase)
                ? label.Substring(4).TrimStart()
                : label;

            TextBlock labelText = new TextBlock
            {
                Text = displayLabel,
                Foreground = color,
                FontSize = 10,
                Background = Brushes.White
            };

            labelText.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double labelW = labelText.DesiredSize.Width;
            double labelH = labelText.DesiredSize.Height;

            // Padding for circle
            double padding = 1;
            double circleDiameter = Math.Max(labelW, labelH) + padding * 2;

            // Position labels starting near x=3, spaced horizontally by zoneIndex
            double horizontalSpacing = 18; // pixels between labels
            double startX = 3;
            startX = Math.Max(xMin, Math.Min(xMax, startX));
            double pxStart = ((Math.Log10(startX) - Math.Log10(xMin)) / (Math.Log10(xMax) - Math.Log10(xMin))) * canvasWidth;

            double yAtStartX = curve.Evaluate(startX);
            double pyAtStartX = ((yAtStartX - yMin) / (yMax - yMin)) * canvasHeight;

            double circleCenterX = pxStart + zoneIndex * horizontalSpacing;
            double circleCenterY = pyAtStartX - labelH / 2 - 2.5;

            // Draw the circle behind the label
            Ellipse circle = new Ellipse
            {
                Width = circleDiameter,
                Height = circleDiameter,
                Stroke = color,
                StrokeThickness = 1,
                Fill = Brushes.White
            };
            Canvas.SetLeft(circle, circleCenterX - circleDiameter / 2);
            Canvas.SetTop(circle, circleCenterY - circleDiameter / 2);
            canvas.Children.Add(circle);

            // Draw the label centered in the circle
            Canvas.SetLeft(labelText, circleCenterX - labelW / 2);
            Canvas.SetTop(labelText, circleCenterY - labelH / 2);
            canvas.Children.Add(labelText);

            // === Left Y-value label ===
            double leftXVal = xPoints[0];
            double leftYVal = curve.Evaluate(leftXVal);
            Point leftPt = linePoints[0];

            TextBlock leftYLabel = new TextBlock
            {
                Text = leftYVal.ToString("0.00"),
                Foreground = color,
                FontSize = 10,
                Background = Brushes.White
            };
            leftYLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double lw = leftYLabel.DesiredSize.Width;
            double lh = leftYLabel.DesiredSize.Height;
            Canvas.SetLeft(leftYLabel, leftPt.X - lw / 2);
            Canvas.SetTop(leftYLabel, leftPt.Y - lh / 2);
            canvas.Children.Add(leftYLabel);

            // === Right Y-value label ===
            double rightXVal = xPoints[xPoints.Count - 1];
            double rightYVal = curve.Evaluate(rightXVal);
            Point rightPt = linePoints[linePoints.Count - 1];

            TextBlock rightYLabel = new TextBlock
            {
                Text = rightYVal.ToString("0.00"),
                Foreground = color,
                FontSize = 10,
                Background = Brushes.White
            };
            rightYLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double rw = rightYLabel.DesiredSize.Width;
            double rh = rightYLabel.DesiredSize.Height;
            Canvas.SetLeft(rightYLabel, rightPt.X - rw / 2);
            Canvas.SetTop(rightYLabel, rightPt.Y - rh / 2);
            canvas.Children.Add(rightYLabel);
        }

        private static void DrawGrid(Canvas canvas, double canvasWidth, double canvasHeight,
                                     double xMin, double xMax, double yMin, double yMax,
                                     IEnumerable<double> xValuesToLabel)
        {
            // === Draw X-axis ticks, dashed vertical lines, and centered labels ===
            foreach (double xVal in xValuesToLabel)
            {
                if (xVal <= 0) continue;

                double px = ((Math.Log10(xVal) - Math.Log10(xMin)) / (Math.Log10(xMax) - Math.Log10(xMin))) * canvasWidth;

                // Dashed vertical grid line
                Line vLine = new Line
                {
                    X1 = px,
                    Y1 = 0,
                    X2 = px,
                    Y2 = canvasHeight,
                    Stroke = Brushes.Gray,
                    StrokeThickness = 0.5,
                    StrokeDashArray = new DoubleCollection() { 4, 2 }
                };
                canvas.Children.Add(vLine);

                // Tick mark at bottom
                Line tick = new Line
                {
                    X1 = px,
                    Y1 = canvasHeight - 6,
                    X2 = px,
                    Y2 = canvasHeight,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1
                };
                canvas.Children.Add(tick);

                // X-axis label centered on tick
                TextBlock xLabel = new TextBlock
                {
                    Text = xVal >= 1000 ? $"{xVal / 1000:0.#}k" : xVal.ToString("0"),
                    FontSize = 10,
                    Foreground = Brushes.Black
                };
                // Measure label width to center it
                xLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                double labelWidth = xLabel.DesiredSize.Width;
                Canvas.SetLeft(xLabel, px - labelWidth / 2);
                Canvas.SetTop(xLabel, canvasHeight - 18);
                canvas.Children.Add(xLabel);
            }

            // === Draw horizontal lines at every 0.2 with labels ===
            double yStep = 0.2;
            int yDivisions = (int)Math.Round((yMax - yMin) / yStep);
            for (int i = 0; i <= yDivisions; i++)
            {
                double y = yMin + i * yStep;
                double normY = (y - yMin) / (yMax - yMin);
                double py = normY * canvasHeight;

                // Draw line
                Line yLine = new Line
                {
                    X1 = 0,
                    Y1 = py,
                    X2 = canvasWidth,
                    Y2 = py,
                    Stroke = Brushes.LightGray,
                    StrokeThickness = 0.5
                };
                canvas.Children.Add(yLine);

                // Y label
                TextBlock yLabel = new TextBlock
                {
                    Text = y.ToString("0.0"),
                    FontSize = 10,
                    Foreground = Brushes.Black
                };
                // Center vertically: label height is about 12 px, shift up by ~6
                yLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                double labelHeight = yLabel.DesiredSize.Height;
                Canvas.SetLeft(yLabel, 2);
                Canvas.SetTop(yLabel, py - labelHeight / 2);
                canvas.Children.Add(yLabel);
            }

            // === Draw dashed horizontal lines at special Y-values ===
            double[] dashedYVals = new double[] { -4, -3, -2, -1, 0, 1 };
            foreach (double y in dashedYVals)
            {
                if (y < yMin || y > yMax) continue;

                double normY = (y - yMin) / (yMax - yMin);
                double py = normY * canvasHeight;

                Line dashedLine = new Line
                {
                    X1 = 0,
                    Y1 = py,
                    X2 = canvasWidth,
                    Y2 = py,
                    Stroke = Brushes.Gray,
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection() { 4, 2 }
                };
                canvas.Children.Add(dashedLine);
            }
        }
    }
}
