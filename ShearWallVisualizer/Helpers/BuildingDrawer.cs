using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using ShearWallCalculator.BuildingInfo;

namespace ShearWallCalculator.Helpers
{
    public static class BuildingDrawer
    {
        /// <summary>
        /// Draws a plan view of the building to the provided Canvas.
        /// The building is drawn with the origin (0,0) in bottom-left corner.
        /// </summary>
        public static void DrawPlan(Canvas canvas, BuildingData buildingData, double scale = 5.0)
        {
            if (canvas == null) return;

            canvas.Children.Clear();

            double length = buildingData.BuildingLength;
            double width = buildingData.BuildingWidth;

            // Transform world -> screen (flip Y)
            double screenHeight = width * scale;

            Point ToCanvas(Point pt)
            {
                return new Point(pt.X * scale, screenHeight - pt.Y * scale);
            }

            // Building perimeter
            var corners = new[]
            {
                new Point(0, 0),
                new Point(length, 0),
                new Point(length, width),
                new Point(0, width),
            };

            // Draw perimeter
            Polygon buildingOutline = new Polygon
            {
                Stroke = Brushes.Black,
                StrokeThickness = 2,
                Fill = Brushes.Transparent
            };
            foreach (var pt in corners)
                buildingOutline.Points.Add(ToCanvas(pt));
            canvas.Children.Add(buildingOutline);

            // Optional: draw ridge line if roof is sloped
            if (buildingData.RoofTypeIsSloped())
            {
                Point ridgeStart, ridgeEnd;
                bool hasRidge = false;
                if (buildingData.RidgeDirection == RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH)
                {
                    // Ridge is along X-axis, halfway in Y
                    double y = width / 2.0;
                    ridgeStart = new Point(0, y);
                    ridgeEnd = new Point(length, y);
                    Line ridgeLine = new Line
                    {
                        X1 = ToCanvas(ridgeStart).X,
                        Y1 = ToCanvas(ridgeStart).Y,
                        X2 = ToCanvas(ridgeEnd).X,
                        Y2 = ToCanvas(ridgeEnd).Y,
                        Stroke = Brushes.DarkRed,
                        StrokeThickness = 2,
                        StrokeDashArray = new DoubleCollection { 4, 2 }
                    };

                    canvas.Children.Add(ridgeLine);
                }
                else if (buildingData.RidgeDirection == RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH)
                {
                    // Ridge is along Y-axis, halfway in X
                    double x = length / 2.0;
                    ridgeStart = new Point(x, 0);
                    ridgeEnd = new Point(x, width);
                    Line ridgeLine = new Line
                    {
                        X1 = ToCanvas(ridgeStart).X,
                        Y1 = ToCanvas(ridgeStart).Y,
                        X2 = ToCanvas(ridgeEnd).X,
                        Y2 = ToCanvas(ridgeEnd).Y,
                        Stroke = Brushes.DarkRed,
                        StrokeThickness = 2,
                        StrokeDashArray = new DoubleCollection { 4, 2 }
                    };

                    canvas.Children.Add(ridgeLine);
                }
                else if (buildingData.RidgeDirection == RidgeDirections.RIDGE_DIR_NONE)
                {
                } else
                {
                    throw new System.Exception("Invalid ridge direction" + buildingData.RidgeDirection.ToString());
                }
            }

            // Optional: draw origin marker
            Ellipse origin = new Ellipse
            {
                Width = 6,
                Height = 6,
                Fill = Brushes.Blue
            };
            Point originPt = ToCanvas(new Point(0, 0));
            Canvas.SetLeft(origin, originPt.X - 3);
            Canvas.SetTop(origin, originPt.Y - 3);
            canvas.Children.Add(origin);
        }
    }
}
