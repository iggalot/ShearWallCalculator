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

        private ScaleTransform imageScaleTransform = new ScaleTransform(1.0, 1.0);
        private TranslateTransform imageTranslateTransform = new TranslateTransform(0, 0);
        private Point lastPanPoint;
        private bool isPanning = false;

        // Class-level fields for storing the offset of the image after centering
        private double imageOffsetX = 0;
        private double imageOffsetY = 0;

        public ImagePreviewControl()
        {
            InitializeComponent();

            // Zooming and panning input events
            this.MouseWheel += ImagePreviewControl_MouseWheel;
            canvasImage.MouseDown += CanvasImage_MouseDown;
            canvasImage.MouseMove += CanvasImage_MouseMove;
            canvasImage.MouseUp += CanvasImage_MouseUp;
            canvasImage.MouseLeave += CanvasImage_MouseUp;
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

                // Define a maximum width for the preview control (e.g., 800px)
                double maxWidth = 800;
                double minWidth = 700;

                // Dynamically adjust the ImagePreviewControl width based on the image width
                double previewWidth = Math.Min(bitmap.PixelWidth, maxWidth); // Ensure the image doesn't exceed maxWidth

                // If the image width is less than the minimum size, scale it up to the minimum width
                if (previewWidth < minWidth)
                {
                    previewWidth = minWidth;
                }

                // Calculate the corresponding height to maintain aspect ratio
                double previewHeight = bitmap.PixelHeight * (previewWidth / bitmap.PixelWidth);

                // Set the size of the control (this is the ImagePreviewControl itself)
                this.Width = previewWidth + 20; // Add some padding/margin if necessary
                this.Height = previewHeight + 50; // Adjust height accordingly, with some padding

                // Apply scaling transform to the image
                var transformGroup = new TransformGroup();

                // Scaling transform
                imageScaleTransform.ScaleX = previewWidth / bitmap.PixelWidth;
                imageScaleTransform.ScaleY = previewHeight / bitmap.PixelHeight;
                transformGroup.Children.Add(imageScaleTransform);

                // Apply the transform to the image
                imageDisplay.RenderTransform = transformGroup;
                imageDisplay.RenderTransformOrigin = new Point(0, 0);

                // Center the image on the canvas using TranslateTransform
                double centerX = (this.Width - previewWidth) / 2;
                double centerY = (this.Height - previewHeight) / 2;

                // Apply the offset directly to the image (not the canvas)
                TranslateTransform translateTransform = new TranslateTransform(centerX, centerY);
                imageDisplay.RenderTransform = new TransformGroup
                {
                    Children = { imageScaleTransform, translateTransform }
                };

                // Ensure canvas size matches the image size (not affected by the translation)
                canvasImage.Width = bitmap.PixelWidth;
                canvasImage.Height = bitmap.PixelHeight;

                // Clear the canvas and add the image back
                canvasImage.Children.Clear();
                canvasImage.Children.Add(imageDisplay);

                // Update image size info in the UI
                txtImageSize.Text = $"{bitmap.PixelWidth} x {bitmap.PixelHeight}";

                // Reset previous measurement points and distance text
                firstPoint = null;
                secondPoint = null;
                txtMeasuredDistance.Text = "";
            }
        }

        private void imageDisplay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (bitmap == null) return;

            // Get the mouse position relative to the canvas
            Point mousePositionOnCanvas = e.GetPosition(canvasImage);

            // Proceed with the point selection logic
            if (firstPoint == null)
            {
                firstPoint = mousePositionOnCanvas;
                DrawMarker(firstPoint.Value, Brushes.Red);
            }
            else if (secondPoint == null)
            {
                secondPoint = mousePositionOnCanvas;
                DrawMarker(secondPoint.Value, Brushes.Blue);

                // Calculate pixel distance on the canvas (no transforms required here)
                double pixelDistance = Distance(firstPoint.Value, secondPoint.Value);
                DrawLine(firstPoint.Value, secondPoint.Value, Brushes.Green);

                var dialog = new EnterMeasurementDialog();
                dialog.Owner = Window.GetWindow(this);

                if (dialog.ShowDialog() == true && dialog.RealWorldDistance.HasValue)
                {
                    double realDistance = dialog.RealWorldDistance.Value;
                    txtMeasuredDistance.Text = $"{realDistance} (pixels: {pixelDistance:F2})";

                    MeasurementCompleted?.Invoke(this,
                        new ImageMeasurementEventArgs(currentFilePath, pixelDistance, realDistance));
                }
            }
            else
            {
                ResetMeasurement();
            }

            if (firstPoint != null && secondPoint == null)
            {
                DrawPreviewLine(firstPoint.Value, mousePositionOnCanvas, Brushes.Gray);
            }
        }


        private void ImagePreviewControl_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (bitmap == null) return;

            double zoomFactor = e.Delta > 0 ? 1.1 : 0.9;
            Point mousePos = e.GetPosition(canvasImage);

            double absX = (mousePos.X - imageTranslateTransform.X) / imageScaleTransform.ScaleX;
            double absY = (mousePos.Y - imageTranslateTransform.Y) / imageScaleTransform.ScaleY;

            imageScaleTransform.ScaleX *= zoomFactor;
            imageScaleTransform.ScaleY *= zoomFactor;

            imageTranslateTransform.X = mousePos.X - absX * imageScaleTransform.ScaleX;
            imageTranslateTransform.Y = mousePos.Y - absY * imageScaleTransform.ScaleY;
        }

        private void CanvasImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.MiddleButton == MouseButtonState.Pressed ||
                (e.LeftButton == MouseButtonState.Pressed && Keyboard.IsKeyDown(Key.LeftShift)))
            {
                isPanning = true;
                lastPanPoint = e.GetPosition(this);
                canvasImage.CaptureMouse();
            }
        }

        private void CanvasImage_MouseMove(object sender, MouseEventArgs e)
        {
            if (isPanning)
            {
                Point currentPoint = e.GetPosition(this);
                Vector delta = currentPoint - lastPanPoint;

                imageTranslateTransform.X += delta.X;
                imageTranslateTransform.Y += delta.Y;

                lastPanPoint = currentPoint;
            }
        }

        private void CanvasImage_MouseUp(object sender, MouseEventArgs e)
        {
            isPanning = false;
            canvasImage.ReleaseMouseCapture();
        }

        private void DrawPreviewLine(Point p1, Point p2, Brush color)
        {
            foreach (var child in canvasImage.Children.OfType<Line>().ToList())
            {
                if (child.Stroke == Brushes.Gray)
                {
                    canvasImage.Children.Remove(child);
                }
            }

            Line previewLine = new Line
            {
                X1 = p1.X,
                Y1 = p1.Y,
                X2 = p2.X,
                Y2 = p2.Y,
                Stroke = color,
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection() { 2, 2 }
            };

            canvasImage.Children.Add(previewLine);
        }

        private void ResetMeasurement()
        {
            firstPoint = null;
            secondPoint = null;
            canvasImage.Children.Clear();
            canvasImage.Children.Add(imageDisplay);
            txtMeasuredDistance.Text = "";
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
