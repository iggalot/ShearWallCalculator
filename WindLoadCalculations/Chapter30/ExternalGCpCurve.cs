using System;
using System.Collections.Generic;
using System.Linq;

namespace ShearWallCalculator.WindLoadCalculations.Chapter30
{
    public class ExternalGCpCurve
    {
        public Dictionary<double, double> Curve = new Dictionary<double, double>();

        public ExternalGCpCurve(double x1, double y1, double x2, double y2)
        {
            Curve.Add(1, y1);
            Curve.Add(x1, y1);
            Curve.Add(x2, y2);
            Curve.Add(2000, y2);
        }

        public double GetGCP(double x)
        {
            if (Curve == null || Curve.Count == 0)
                throw new ArgumentException("Data points cannot be null or empty.");

            // Sort the dictionary by key
            var sorted = Curve.OrderBy(kv => kv.Key).ToList();

            // Handle out-of-bounds (clamp)
            if (x <= sorted[0].Key)
                return sorted[0].Value;

            if (x >= sorted[sorted.Count - 1].Key)
                return sorted[sorted.Count - 1].Value;

            // Linear interpolation between two points
            for (int i = 0; i < sorted.Count - 1; i++)
            {
                var x0 = sorted[i].Key;
                var y0 = sorted[i].Value;
                var x1 = sorted[i + 1].Key;
                var y1 = sorted[i + 1].Value;

                if (x >= x0 && x <= x1)
                {
                    double t = (x - x0) / (x1 - x0);
                    return y0 + t * (y1 - y0);
                }
            }

            // Should not reach here if input is valid
            throw new InvalidOperationException("Interpolation failed.");
        }
    }
}
