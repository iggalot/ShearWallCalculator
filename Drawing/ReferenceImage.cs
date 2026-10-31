using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace ShearWallCalculator.Drawing
{
    /// <summary>
    /// A plan image under the drawing. Image pixels (u right, v down from the top-left corner) map to world feet
    /// (x right, y up) by a scale, a rotation and the world position of the top-left corner:
    /// world = Origin + R(θ) · (s·u, −s·v). Saved with the project.
    /// </summary>
    public sealed class ReferenceImage
    {
        /// <summary>Full path of the image file when it was imported.</summary>
        public string Path { get; set; }

        /// <summary>Feet per image pixel.</summary>
        public double FeetPerPixel { get; set; } = 1.0 / 12.0;

        /// <summary>Counter-clockwise rotation of the image on the plan, degrees.</summary>
        public double RotationDegrees { get; set; }

        /// <summary>World position (ft) of the image's top-left corner.</summary>
        public Point Origin { get; set; }

        /// <summary>0 (invisible) to 1 (opaque).</summary>
        public double Opacity { get; set; } = 0.6;

        /// <summary>A locked image cannot be moved, rotated or rescaled.</summary>
        public bool Locked { get; set; }

        public bool Visible { get; set; } = true;

        /// <summary>Image size in pixels (filled in when the image is loaded; used to place it and for the extents).</summary>
        public int PixelWidth { get; set; }
        public int PixelHeight { get; set; }

        /// <summary>
        /// The image file itself, stored in the project file (base64 in the JSON) so the image comes back at its scale
        /// when the project is opened on another computer or after the original file is moved. Null in older projects
        /// until the image is found and loaded once.
        /// </summary>
        public byte[] Data { get; set; }

        /// <summary>
        /// CAD linework (from a DXF), in the image's own coordinates (u right, v down, one unit = <see cref="VectorUnitFeet"/>
        /// at import). Null for a picture (PNG, JPG, a PDF page...).
        /// </summary>
        public List<VectorPath> Paths { get; set; }

        /// <summary>What the image came from, for the status line and the log (e.g. "DXF, inches", "PDF page 2 of 5").</summary>
        public string Source { get; set; }

        public bool IsVector => Paths != null && Paths.Count > 0;

        /// <summary>True once the scale has been set by calibration or a drawing scale (not just the import guess).</summary>
        public bool Calibrated { get; set; }

        public ReferenceImage Clone() => (ReferenceImage)MemberwiseClone();

        private double Theta => RotationDegrees * Math.PI / 180.0;

        /// <summary>World ft of image pixel (u, v).</summary>
        public Point PixelToWorld(Point px)
        {
            double x = FeetPerPixel * px.X, y = -FeetPerPixel * px.Y;
            double c = Math.Cos(Theta), s = Math.Sin(Theta);
            return new Point(Origin.X + c * x - s * y, Origin.Y + s * x + c * y);
        }

        /// <summary>Image pixel of world point <paramref name="w"/>.</summary>
        public Point WorldToPixel(Point w)
        {
            double dx = w.X - Origin.X, dy = w.Y - Origin.Y;
            double c = Math.Cos(Theta), s = Math.Sin(Theta);
            double x = c * dx + s * dy, y = -s * dx + c * dy;
            return new Point(x / FeetPerPixel, -y / FeetPerPixel);
        }

        /// <summary>The affine map from image pixels to world ft, for drawing (combine with world → screen).</summary>
        public Matrix PixelToWorldMatrix()
        {
            Point o = PixelToWorld(new Point(0, 0)), ux = PixelToWorld(new Point(1, 0)), vy = PixelToWorld(new Point(0, 1));
            return new Matrix(ux.X - o.X, ux.Y - o.Y, vy.X - o.X, vy.Y - o.Y, o.X, o.Y);
        }

        /// <summary>
        /// Two-point scale: the two world points (picked on the image as it is drawn now) are <paramref name="realFeet"/>
        /// apart. The image is rescaled about the first point, which stays where it is.
        /// </summary>
        /// <exception cref="ArgumentException">The points are the same or the distance is not positive.</exception>
        public void Calibrate(Point a, Point b, double realFeet)
        {
            double picked = (b - a).Length;
            if (picked < 1e-9) throw new ArgumentException("Pick two different points.");
            if (!(realFeet > 0)) throw new ArgumentException("The distance must be greater than zero.");
            ScaleAbout(a, realFeet / picked);
            Calibrated = true;
        }

        /// <summary>Scales the image by <paramref name="factor"/> about world point <paramref name="fixedPoint"/>.</summary>
        public void ScaleAbout(Point fixedPoint, double factor)
        {
            Point px = WorldToPixel(fixedPoint);
            FeetPerPixel *= factor;
            Point moved = PixelToWorld(px);
            Origin += fixedPoint - moved;
        }

        /// <summary>
        /// Straighten: rotates the image about <paramref name="a"/> so the line a→b (picked on the image) runs E-W or N-S,
        /// whichever is nearer. Returns the rotation applied (degrees, CCW).
        /// </summary>
        public double Straighten(Point a, Point b)
        {
            Vector d = b - a;
            if (d.Length < 1e-9) throw new ArgumentException("Pick two different points.");
            double angle = Math.Atan2(d.Y, d.X) * 180.0 / Math.PI;
            double target = Math.Round(angle / 90.0) * 90.0;
            double turn = target - angle;
            RotateAbout(a, turn);
            return turn;
        }

        /// <summary>Rotates the image by <paramref name="degrees"/> (CCW) about world point <paramref name="pivot"/>.</summary>
        public void RotateAbout(Point pivot, double degrees)
        {
            Point px = WorldToPixel(pivot);
            RotationDegrees = NormalizeDegrees(RotationDegrees + degrees);
            Point moved = PixelToWorld(px);
            Origin += pivot - moved;
        }

        /// <summary>Moves the image so world point <paramref name="from"/> lands on <paramref name="to"/>.</summary>
        public void Move(Point from, Point to) => Origin += to - from;

        /// <summary>
        /// Feet per pixel from the printed drawing scale and the image resolution: at <paramref name="dpi"/> pixels per inch
        /// of paper, and <paramref name="feetPerPaperInch"/> ft of building per inch of paper (1/4" = 1'-0" → 4).
        /// </summary>
        public static double FeetPerPixelFromDrawingScale(double feetPerPaperInch, double dpi)
        {
            if (!(feetPerPaperInch > 0) || !(dpi > 0)) throw new ArgumentException("The drawing scale and the resolution must be greater than zero.");
            return feetPerPaperInch / dpi;
        }

        /// <summary>Common architectural and engineering scales: label and feet of building per inch of paper.</summary>
        public static readonly (string label, double feetPerInch)[] DrawingScales =
        {
            ("1/16\" = 1'-0\"", 16), ("3/32\" = 1'-0\"", 32.0 / 3.0), ("1/8\" = 1'-0\"", 8), ("3/16\" = 1'-0\"", 16.0 / 3.0),
            ("1/4\" = 1'-0\"", 4), ("3/8\" = 1'-0\"", 8.0 / 3.0), ("1/2\" = 1'-0\"", 2),
            ("1\" = 10'", 10), ("1\" = 20'", 20), ("1\" = 30'", 30), ("1\" = 40'", 40),
        };

        /// <summary>
        /// Placement on import: the image's bottom-left corner at the world origin, unrotated, at
        /// <paramref name="feetPerPixel"/>.
        /// </summary>
        public static ReferenceImage Imported(string path, int pixelWidth, int pixelHeight, double feetPerPixel)
        {
            if (pixelWidth <= 0 || pixelHeight <= 0) throw new ArgumentException("The image has no pixels.");
            return new ReferenceImage
            {
                Path = path, PixelWidth = pixelWidth, PixelHeight = pixelHeight, FeetPerPixel = feetPerPixel,
                Origin = new Point(0, pixelHeight * feetPerPixel),
            };
        }

        /// <summary>
        /// The file to open: the saved path if it exists, otherwise a file of the same name next to the project file
        /// (projects moved with their images). Null when neither exists.
        /// </summary>
        public string Resolve(string projectPath)
        {
            if (!string.IsNullOrEmpty(Path) && File.Exists(Path)) return Path;
            if (string.IsNullOrEmpty(Path) || string.IsNullOrEmpty(projectPath)) return null;
            string beside = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(projectPath) ?? "", System.IO.Path.GetFileName(Path));
            return File.Exists(beside) ? beside : null;
        }

        /// <summary>Stores the image file in the project (see <see cref="Data"/>). False when the file cannot be read.</summary>
        public bool Embed(string file)
        {
            try
            {
                Data = File.ReadAllBytes(file);
                return true;
            }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
        }

        /// <summary>World extents of the image (for zoom to fit).</summary>
        public Rect WorldBounds()
        {
            var r = Rect.Empty;
            foreach (var p in new[] { new Point(0, 0), new Point(PixelWidth, 0), new Point(PixelWidth, PixelHeight), new Point(0, PixelHeight) })
                r.Union(PixelToWorld(p));
            return r;
        }

        private static double NormalizeDegrees(double d)
        {
            d %= 360.0;
            if (d > 180) d -= 360;
            if (d <= -180) d += 360;
            return d;
        }

        /// <summary>Size of one image unit of CAD linework at import, ft (fine enough that rounding to 0.01 unit is invisible).</summary>
        public const double VectorUnitFeet = 0.01;

        /// <summary>
        /// CAD linework placed like an imported picture: at true size (<paramref name="feetPerUnit"/> ft per drawing unit),
        /// its lower-left corner at (0, 0), only the paths on <paramref name="layers"/>.
        /// </summary>
        /// <exception cref="ArgumentException">Nothing to import on those layers, or the units are not positive.</exception>
        public static ReferenceImage FromDxf(string path, DxfDrawing drawing, ICollection<string> layers, double feetPerUnit, string source)
        {
            if (!(feetPerUnit > 0)) throw new ArgumentException("Choose the drawing units.");
            var kept = drawing.Paths.Where(p => layers == null || layers.Contains(p.Layer)).ToList();
            var box = drawing.Bounds(layers);
            if (kept.Count == 0 || box.IsEmpty) throw new ArgumentException("There is no linework on the chosen layers.");
            double k = feetPerUnit / VectorUnitFeet; // image units per drawing unit
            var paths = kept.Select(p => new VectorPath
            {
                Layer = p.Layer, Closed = p.Closed,
                Xy = p.Points.SelectMany(q => new[] { Math.Round((q.X - box.Left) * k, 2), Math.Round((box.Bottom - q.Y) * k, 2) }).ToArray(),
            }).ToList();
            int w = Math.Max(1, (int)Math.Ceiling(box.Width * k)), h = Math.Max(1, (int)Math.Ceiling(box.Height * k));
            var img = Imported(path, w, h, VectorUnitFeet);
            img.Origin = new Point(0, box.Height * feetPerUnit); // exact, not the rounded-up height
            img.Paths = paths;
            img.Source = source;
            img.Calibrated = true;
            img.Opacity = 0.8;
            return img;
        }

        /// <summary>
        /// The CAD segments within <paramref name="tolerance"/> ft of a world point, in world ft (for object snaps).
        /// Empty for a picture.
        /// </summary>
        public List<SnapSegment> SegmentsNear(Point world, double tolerance)
        {
            var list = new List<SnapSegment>();
            if (!IsVector || !(FeetPerPixel > 0)) return list;
            Point c = WorldToPixel(world);
            double t = tolerance / FeetPerPixel;
            foreach (var p in Paths)
            {
                var xy = p.Xy;
                if (xy == null) continue;
                int n = xy.Length / 2;
                for (int i = 0; i < (p.Closed ? n : n - 1); i++)
                {
                    int j = (i + 1) % n;
                    double x1 = xy[2 * i], y1 = xy[2 * i + 1], x2 = xy[2 * j], y2 = xy[2 * j + 1];
                    if (c.X < Math.Min(x1, x2) - t || c.X > Math.Max(x1, x2) + t || c.Y < Math.Min(y1, y2) - t || c.Y > Math.Max(y1, y2) + t) continue;
                    list.Add(new SnapSegment(PixelToWorld(new Point(x1, y1)), PixelToWorld(new Point(x2, y2))));
                }
            }
            return list;
        }
    }

    /// <summary>One polyline of CAD linework in a <see cref="ReferenceImage"/>: x, y pairs in image units.</summary>
    public sealed class VectorPath
    {
        public string Layer { get; set; }
        public bool Closed { get; set; }
        public double[] Xy { get; set; }
    }
}
