using System;
using System.Collections.Generic;
using System.Windows;
using ShearWallCalculator.Drawing;

namespace ShearWallVisualizer.Controls
{
    public enum ImageCommand { None, Calibrate, Straighten, Move }

    /// <summary>
    /// The two-point image commands on the drawing: Calibrate (two points a known distance apart, then type the
    /// distance), Straighten (two points along a line that should run E-W or N-S) and Move (base point, then
    /// destination, with object snaps). The window routes clicks, Enter and Esc here while a command is active.
    /// </summary>
    internal sealed class ImageTool
    {
        private readonly List<Point> points = new List<Point>();

        public ImageCommand Command { get; private set; }
        public bool Active => Command != ImageCommand.None;
        public Point? First => points.Count > 0 ? points[0] : (Point?)null;
        public IReadOnlyList<Point> Points => points;

        /// <summary>Both calibration points are picked: the next Enter in the Point box is the real distance.</summary>
        public bool WaitingForDistance => Command == ImageCommand.Calibrate && points.Count == 2;

        /// <summary>Snaps help put the image on the drawing (Move); on the image itself they would pull off the picked feature.</summary>
        public bool WantsSnaps => Command == ImageCommand.Move && points.Count == 1;

        public void Begin(ImageCommand command)
        {
            Command = command;
            points.Clear();
        }

        public void Cancel() => Begin(ImageCommand.None);

        public string Prompt
        {
            get
            {
                switch (Command)
                {
                    case ImageCommand.Calibrate:
                        return points.Count == 0 ? "Image scale: click the first end of a known dimension on the image  (Esc cancels)"
                             : points.Count == 1 ? "Image scale: click the other end"
                             : "Image scale: type the real distance in the Point box, e.g. 24'-6\" or 24.5, and press Enter";
                    case ImageCommand.Straighten:
                        return points.Count == 0 ? "Straighten image: click a point on a wall line that should run E-W or N-S  (Esc cancels)"
                             : "Straighten image: click a second point on the same line";
                    case ImageCommand.Move:
                        return points.Count == 0 ? "Move image: click a base point on the image  (Esc cancels)"
                             : "Move image: click where the base point goes (snaps to the walls)";
                    default:
                        return null;
                }
            }
        }

        /// <summary>A picked point. Returns a message when the command has finished (or a warning), otherwise null.</summary>
        public string Click(ReferenceImage image, Point p, out bool warning)
        {
            warning = false;
            if (!Active || image == null) return null;
            if (points.Count > 0 && (p - points[points.Count - 1]).Length < 1e-9)
            {
                warning = true;
                return "Pick a different point.";
            }
            if (WaitingForDistance) return null;
            points.Add(p);
            if (points.Count < 2) return null;

            switch (Command)
            {
                case ImageCommand.Straighten:
                    double turn = image.Straighten(points[0], points[1]);
                    Cancel();
                    return Math.Abs(turn) < 0.005 ? "The line already runs square: the image was not turned." : $"Image turned {turn:+0.##;-0.##}°.";
                case ImageCommand.Move:
                    Vector d = points[1] - points[0];
                    image.Move(points[0], points[1]);
                    Cancel();
                    return $"Image moved {d.X:0.##} ft, {d.Y:0.##} ft.";
                default:
                    return null; // Calibrate: wait for the distance
            }
        }

        /// <summary>The typed real distance for Calibrate. False (with the reason) when it cannot be used.</summary>
        public bool EnterDistance(ReferenceImage image, string text, out string message)
        {
            if (!WaitingForDistance || image == null)
            {
                message = null;
                return false;
            }
            if (!CoordinateInput.TryLength(text, out double feet) || !(feet > 0))
            {
                message = "Type the distance in feet, or feet and inches: 24.5, 24'6\" or 24'-6\".";
                return false;
            }
            double picked = (points[1] - points[0]).Length;
            image.Calibrate(points[0], points[1], feet);
            Cancel();
            message = $"Image scale set: the picked length {picked:0.##} ft is now {feet:0.##} ft ({image.FeetPerPixel * 12:0.###} in per pixel).";
            return true;
        }
    }
}
