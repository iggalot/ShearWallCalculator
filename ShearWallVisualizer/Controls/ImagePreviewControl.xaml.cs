using Microsoft.Win32;
using ShearWallVisualizer.Dialogs;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ShearWallVisualizer.Controls
{
    public class ImageMeasurementEventArgs : EventArgs
    {
        public string FilePath { get; set; }
        public double PixelDistance { get; set; }
        public double RealWorldDistance { get; set; }
        public double ScaleFactor => RealWorldDistance / PixelDistance;

        public ImageMeasurementEventArgs(string filePath, double pixelDistance, double realWorldDistance)
        {
            FilePath = filePath;
            PixelDistance = pixelDistance;
            RealWorldDistance = realWorldDistance;
        }
    }

    public partial class ImagePreviewControl : UserControl
    {
        public event EventHandler<ImageMeasurementEventArgs> MeasurementCompleted;

        private string currentFilePath;
        private BitmapImage bitmap;
        private Point? firstPoint = null;
        private Point? secondPoint = null;

        public ImagePreviewControl()
        {
            InitializeComponent();
        }

        private void btnLoadImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Filter = "Image Files (*.jpg;*.png)|*.jpg;*.png"
            };

            if (dlg.ShowDialog() == true)
            {
                currentFilePath = dlg.FileName;
                bitmap = new BitmapImage(new Uri(currentFilePath));
                imageDisplay.Source = bitmap;

                // Set image display size to match actual image pixel dimensions
                imageDisplay.Width = bitmap.PixelWidth;
                imageDisplay.Height = bitmap.PixelHeight;

                // Position the image at (0,0) on the canvas
                Canvas.SetLeft(imageDisplay, 0);
                Canvas.SetTop(imageDisplay, 0);

                // Ensure the canvas size matches the image
                canvasImage.Width = bitmap.PixelWidth;
                canvasImage.Height = bitmap.PixelHeight;

                // Clear the canvas and add the image back
                canvasImage.Children.Clear();
                canvasImage.Children.Add(imageDisplay);

                // Set image size info in the UI
                txtImageSize.Text = $"{bitmap.PixelWidth} x {bitmap.PixelHeight}";

                // Reset previous measurement points and distance text
                firstPoint = null;
                secondPoint = null;
                txtMeasuredDistance.Text = "";
            }
        }

        private void imageDisplay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Console.WriteLine("mouse clicked.");
            // Guard clause to prevent clicks if bitmap is null
            if (bitmap == null) return;

            Point clickPoint = e.GetPosition(imageDisplay);

            // Debugging: Print which step we're on
            Console.WriteLine($"Mouse clicked at: {clickPoint}, FirstPoint: {firstPoint}, SecondPoint: {secondPoint}");

            // Handle the first click (initial point)
            if (firstPoint == null)
            {
                firstPoint = clickPoint;
                DrawMarker(firstPoint.Value, Brushes.Red);
                Console.WriteLine("First point set.");
            }
            else if (secondPoint == null)
            {
                // Ensure second click is processed only when secondPoint is null
                secondPoint = clickPoint;
                Console.WriteLine("Second point set.");

                //// Check if points are aligned horizontally or vertically
                //bool isHorizontal = Math.Abs(firstPoint.Value.Y - secondPoint.Value.Y) < 0.01;
                //bool isVertical = Math.Abs(firstPoint.Value.X - secondPoint.Value.X) < 0.01;

                //if (!isHorizontal && !isVertical)
                //{
                //    // Show error message if not horizontal or vertical
                //    MessageBox.Show("Points must be strictly horizontal or vertical.", "Invalid Selection");
                //    ResetMeasurement(); // Reset the measurement
                //    return;
                //}

                // Draw second marker
                DrawMarker(secondPoint.Value, Brushes.Blue);

                //// Calculate pixel distance
                //double pixelDistance = isHorizontal
                //    ? Math.Abs(firstPoint.Value.X - secondPoint.Value.X)
                //    : Math.Abs(firstPoint.Value.Y - secondPoint.Value.Y);
                double pixelDistance = Distance(firstPoint.Value, secondPoint.Value);

                // Draw the line connecting the points
                DrawLine(firstPoint.Value, secondPoint.Value, Brushes.Green);

                // **Only after second click, show the measurement dialog**
                var dialog = new EnterMeasurementDialog();
                dialog.Owner = Window.GetWindow(this);

                if (dialog.ShowDialog() == true && dialog.RealWorldDistance.HasValue)
                {
                    double realDistance = dialog.RealWorldDistance.Value;
                    txtMeasuredDistance.Text = $"{realDistance} (pixels: {pixelDistance:F2})";

                    // Notify parent window of the measurement completion
                    MeasurementCompleted?.Invoke(this,
                        new ImageMeasurementEventArgs(currentFilePath, pixelDistance, realDistance));
                }

                Console.WriteLine("Measurement dialog completed.");
            }

            // If both points are selected, reset the measurement (optional, depending on your flow)
            else if (firstPoint != null && secondPoint != null)
            {
                ResetMeasurement();
                Console.WriteLine("Measurement reset.");
            }

            // If we're selecting the second point, show the preview line (optional)
            if (firstPoint != null && secondPoint == null)
            {
                DrawPreviewLine(firstPoint.Value, clickPoint, Brushes.Gray);
                Console.WriteLine("Preview line drawn.");
            }
        }

        private void DrawPreviewLine(Point p1, Point p2, Brush color)
        {
            // Remove previous preview line if it exists
            foreach (var child in canvasImage.Children.OfType<Line>())
            {
                if (child.Stroke == Brushes.Gray) // Or another way to identify the preview line
                {
                    canvasImage.Children.Remove(child);
                }
            }

            // Draw the new preview line
            Line previewLine = new Line
            {
                X1 = p1.X,
                Y1 = p1.Y,
                X2 = p2.X,
                Y2 = p2.Y,
                Stroke = color,
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection() { 2, 2 } // dashed line
            };

            canvasImage.Children.Add(previewLine);
        }

        private void ResetMeasurement()
        {
            // Clear points and markers, reset canvas, and clear the display
            firstPoint = null;
            secondPoint = null;
            canvasImage.Children.Clear(); // Remove all child elements
            canvasImage.Children.Add(imageDisplay); // Re-add the image itself
            txtMeasuredDistance.Text = ""; // Clear the measurement display
            Console.WriteLine("Measurement reset.");
        }

        private void DrawMarker(Point point, Brush color)
        {
            Ellipse marker = new Ellipse
            {
                Fill = color,
                Width = 6,
                Height = 6
            };
            Canvas.SetLeft(marker, point.X - 3);
            Canvas.SetTop(marker, point.Y - 3);
            canvasImage.Children.Add(marker);
        }

        private void DrawLine(Point p1, Point p2, Brush color)
        {
            Line line = new Line
            {
                X1 = p1.X,
                Y1 = p1.Y,
                X2 = p2.X,
                Y2 = p2.Y,
                Stroke = color,
                StrokeThickness = 2
            };

            canvasImage.Children.Add(line);
        }

        private double Distance(Point p1, Point p2)
        {
            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
