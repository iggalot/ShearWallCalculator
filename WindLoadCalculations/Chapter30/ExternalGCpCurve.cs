using System;
using System.Collections.Generic;
using System.Linq;

namespace ShearWallCalculator.WindLoadCalculations.Chapter30
{
    public class ExternalGCpCurve
    {
        public double X1 { get; }
        public double Y1 { get; }
        public double X2 { get; }
        public double Y2 { get; }

        // Optional bounds for interpolation (default to no bounds)
        public double LowerBoundX { get; }
        public double UpperBoundX { get; }

        // Constructor without bounds
        public ExternalGCpCurve(double x1, double y1, double x2, double y2)
        {
            X1 = x1;
            Y1 = y1;
            X2 = x2;
            Y2 = y2;

            LowerBoundX = double.NegativeInfinity;
            UpperBoundX = double.PositiveInfinity;
        }

        // Constructor with bounds
        public ExternalGCpCurve(double x1, double y1, double x2, double y2, double lowerBoundX, double upperBoundX)
        {
            X1 = x1;
            Y1 = y1;
            X2 = x2;
            Y2 = y2;

            LowerBoundX = lowerBoundX;
            UpperBoundX = upperBoundX;
        }

        /// <summary>
        /// Evaluate the trilinear curve at given x.
        /// Clamps x to bounds, and linearly interpolates Y.
        /// </summary>
        public double Evaluate(double x)
        {
            double clampedX = Math.Max(LowerBoundX, Math.Min(UpperBoundX, x));

            if (clampedX <= X1)
                return Y1;
            if (clampedX >= X2)
                return Y2;

            // Linear interpolation
            double t = (clampedX - X1) / (X2 - X1);
            return Y1 + t * (Y2 - Y1);
        }
    }
}
