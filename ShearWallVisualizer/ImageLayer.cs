using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ShearWallVisualizer
{
    public class ImageLayer
    {
        public BitmapImage Bitmap { get; private set; }
        public Rect TargetRect { get; private set; }
        public DrawingVisual Visual { get; private set; }
        public double Opacity { get; private set; } = 1.0;

        public ImageLayer(string imagePath)
        {
            Bitmap = new BitmapImage(new Uri(imagePath, UriKind.Absolute));
            TargetRect = new Rect(0, 0, Bitmap.PixelWidth, Bitmap.PixelHeight);
            Visual = new DrawingVisual();
            Redraw();
        }

        public void Resize(double width, double height)
        {
            TargetRect = new Rect(TargetRect.X, TargetRect.Y, width, height);
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
