using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;

namespace ShearWallVisualizer
{
    public class ImageLayer
    {
        public BitmapImage Bitmap { get; private set; }
        public Rect TargetRect { get; set; }
        public DrawingVisual Visual { get; private set; }
        public double Opacity { get; private set; } = 1.0;

        private double zoomX = 1.0;
        private double zoomY = 1.0;

        public ImageLayer(string imagePath, double scale_x, double scale_y)
        {
            Bitmap = new BitmapImage(new Uri(imagePath, UriKind.Absolute));
            TargetRect = new Rect(0, 0, Bitmap.PixelWidth, Bitmap.PixelHeight);
            Visual = new DrawingVisual();
            Redraw();

            zoomX = scale_x;
            zoomY = scale_y;
        }

        public void Resize(double width, double height)
        {
            TargetRect = new Rect(TargetRect.X, TargetRect.Y, width, height);
            Redraw();
        }

        public void ApplyZoom(double scale_x, double scale_y)
        {
            TargetRect = new Rect(TargetRect.X * scale_x / zoomX, TargetRect.Y * scale_y / zoomY, TargetRect.Width * scale_x / zoomX, TargetRect.Height * scale_y / zoomY);

            // now save the current zoom levels
            zoomX = scale_x;
            zoomY = scale_y;
            Redraw();
        }

        public void SetPosition(double x, double y)
        {
            TargetRect = new Rect(x, y, TargetRect.Width, TargetRect.Height);
            Redraw();
        }

        public void SetOpacity(double opacity)
        {
            Opacity = Clamp(opacity, 0.0, 1.0);
            Redraw();
        }

        private void Redraw()
        {
            using (DrawingContext dc = Visual.RenderOpen())
            {
                dc.PushOpacity(Opacity);
                dc.DrawImage(Bitmap, TargetRect);
                dc.Pop();
            }
        }


        public static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
