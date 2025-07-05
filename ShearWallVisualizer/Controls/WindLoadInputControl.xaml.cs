using ShearWallCalculator.WindLoadCalculations;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ShearWallVisualizer.Controls
{
    public partial class WindLoadInputControl : UserControl
    {
        private WindLoadParameters parameters;

        public event EventHandler<OnWindInputCompleteEventArgs> WindInputComplete;  // the event that signals that the drawing has been updated -- controls will listen for this at the time they are created.

        public class OnWindInputCompleteEventArgs : EventArgs
        {
            public WindLoadParameters _parameters { get; }

            public OnWindInputCompleteEventArgs(WindLoadParameters parameters)
            {
                _parameters = parameters;
            }
        }

        public WindLoadInputControl()
        {
            InitializeComponent();

            this.Loaded += WindLoadInputControl_Loaded;
        }

        private void WindLoadInputControl_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (var value in Enum.GetValues(typeof(RoofTypes)))
            {
                cmbRoofType.Items.Add(value);
            }

            cmbRoofType.SelectedIndex = 0;
        }

        public virtual void OnWindInputComplete(WindLoadParameters parameters)
        {
            cnvCanvas.Children.Clear();
            foreach (var kvp in parameters.effWindAreas_Roof)
            {
                try
                {
                    DrawEffectiveWindArea(cnvCanvas, kvp.Value, 5);
                } catch
                {
                    MessageBox.Show("Error in area: " + kvp.Key.ToString());
                }
            }

            WindInputComplete?.Invoke(this, new OnWindInputCompleteEventArgs(parameters));
        }

        public static Brush GetColorForRegion(string region)
        {
            switch (region)
            {
                case "1":
                    return Brushes.Red;
                case "1'":
                    return Brushes.IndianRed;
                case "2":
                    return Brushes.Yellow;
                case "2e":
                    return Brushes.LightYellow;
                case "2r":
                    return Brushes.Goldenrod;
                case "2n":
                    return Brushes.YellowGreen;
                case "3":
                    return Brushes.Green;
                case "3e":
                    return Brushes.GreenYellow;
                case "3r":
                    return Brushes.LightGreen;
                case "4":
                    return Brushes.MediumOrchid;
                case "5":
                    return Brushes.Purple;
                default:
                    return Brushes.Black;
            }
        }

        public static void DrawEffectiveWindArea(Canvas canvas, EffectiveWindArea_Roof area, double scaleFactor)
        {
            if (canvas == null || area == null)
                return;

            //canvas.Children.Clear();

            // Helper function to create a WPF polygon
            Polygon CreatePolygon(IEnumerable<Point> pts, Brush stroke, Brush fill)
            {


                var polygon = new Polygon
                {
                    Stroke = stroke,
                    Fill = fill,
                    StrokeThickness = 1,
                    Points = new PointCollection(),
                    Opacity = 0.5
                };

                foreach (var pt in pts)
                    polygon.Points.Add(new Point(pt.X * scaleFactor, pt.Y * scaleFactor));

                return polygon;
            }

            // Draw outer boundary (light blue fill, blue border)
            var outerPolygon = CreatePolygon(
                area.OuterBoundary,
                Brushes.Black,
                GetColorForRegion(area.Label)
            );
            canvas.Children.Add(outerPolygon);

            // Draw each hole (transparent fill, red border)
            foreach (var hole in area.Holes)
            {
                var holePolygon = CreatePolygon(
                    hole,
                    Brushes.Red,
                    Brushes.Transparent
                );
                canvas.Children.Add(holePolygon);
            }

            // Optional: Draw centroid as a small ellipse
            var center = area.Centroid;
            double radius = 3;

            var centroidDot = new Ellipse
            {
                Width = radius * 2,
                Height = radius * 2,
                Fill = Brushes.Black
            };

            Canvas.SetLeft(centroidDot, center.X * scaleFactor - radius);
            Canvas.SetTop(centroidDot, center.Y * scaleFactor - radius);
            canvas.Children.Add(centroidDot);
        }


        // Event handler for the Compute Button click
        private void ComputeButton_Click(object sender, RoutedEventArgs e)
        {
            WindLoadParameters parameters = GetWindLoadParameters();
            parameters.ComputeEffectiveWindAreas();
            OnWindInputComplete(parameters); // raise the event where input has been completed
        }

        // Method to retrieve parameters from the input fields
        private WindLoadParameters GetWindLoadParameters()
        {
            double windSpeed = double.Parse(WindSpeedTextBox.Text);
            double buildingHeight = double.Parse(BuildingHeightTextBox.Text);
            double kd = double.Parse(KdTextBox.Text);
            double kzt = double.Parse(KztTextBox.Text);
            double importance = double.Parse(ImportanceFactorTextBox.Text);
            double length = double.Parse(BuildingLengthTextBox.Text);
            double width = double.Parse(BuildingWidthTextBox.Text);
            double pitch = double.Parse(RoofPitchTextBox.Text);

            string risk = ((ComboBoxItem)RiskCategoryComboBox.SelectedItem).Content.ToString();
            string enclosure = ((ComboBoxItem)EnclosureComboBox.SelectedItem).Content.ToString();
            string ridgeDir = ((ComboBoxItem)RidgeDirectionComboBox.SelectedItem).Content.ToString();
            RoofTypes roof_type = (RoofTypes)cmbRoofType.SelectedIndex;

            string exposure_string = ((ComboBoxItem)ExposureCategoryComboBox.SelectedItem).Content.ToString();
            WindExposureCategories exposure;

            switch (exposure_string)
            {
                case "B":
                    exposure = WindExposureCategories.WIND_EXP_CAT_B;
                    break;
                case "C":
                    exposure = WindExposureCategories.WIND_EXP_CAT_C;
                    break;
                case "D":
                    exposure = WindExposureCategories.WIND_EXP_CAT_D;
                    break;
                default:
                    exposure = WindExposureCategories.WIND_EXP_CAT_C;
                    break;
            }


            return new WindLoadParameters
            {
                RiskCategory = risk,
                WindSpeed = windSpeed,
                ExposureCategory = exposure,
                BuildingHeight = buildingHeight,
                EnclosureClassification = enclosure,
                GustFactor = 0.85,
                Kd = kd,
                Kzt = kzt,
                ImportanceFactor = importance,
                BuildingLength = length,
                BuildingWidth = width,
                RoofPitch = pitch,
                RidgeDirection = ridgeDir,
                RoofType = roof_type
            };
        }
    }
}
