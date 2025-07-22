using ShearWallCalculator.BuildingInfo;
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
        public event EventHandler<OnWindInputCompleteEventArgs> WindInputComplete;  // the event that signals that the drawing has been updated -- controls will listen for this at the time they are created.

        public class OnWindInputCompleteEventArgs : EventArgs
        {
            public WindLoadParameters_Base _parameters { get; }
            public ASCE7_Versions _version { get; }
            public BuildingData _bldg_data { get; }

            public OnWindInputCompleteEventArgs(WindLoadParameters_Base parameters, ASCE7_Versions version)
            {
                _parameters = parameters;
                _version = version;
            }
        }

        public BuildingData bldgData { get; set; } = null;
        public WindLoadParameters_Base Parameters { get; set; } = null;
        public ASCE7_Versions Version { get; set; }


        public WindLoadInputControl()
        {
            InitializeComponent();

            this.Loaded += WindLoadInputControl_Loaded;
        }

        public WindLoadInputControl(BuildingData bldg_data, WindLoadParameters_Base parameters = null)
        {
            InitializeComponent();

            this.Parameters = parameters;
            this.bldgData = bldg_data;

            this.Loaded += WindLoadInputControl_Loaded;
        }

        public WindLoadInputControl(BuildingData bldg_data, ASCE7_Versions version, WindLoadParameters_Base parameters = null)
        {
            InitializeComponent();

            this.bldgData = bldg_data;
            this.Version = version;
            this.Parameters = parameters;

            this.Loaded += WindLoadInputControl_Loaded;
        }

        private void WindLoadInputControl_Loaded(object sender, RoutedEventArgs e)
        {
            // populate the bulding data summary
            if(this.bldgData == null)
            {
                spBuildingData.Visibility = Visibility.Collapsed;
            } else
            {
                spBuildingData.Visibility = Visibility.Visible;
                tbRoofType.Text = this.bldgData.RoofType.ToString();
                tbBuildingLength.Text = this.bldgData.BuildingLength.ToString("F2");
                tbBuildingWidth.Text = this.bldgData.BuildingWidth.ToString("F2");
                tbBuildingHeight.Text = this.bldgData.BuildingHeight.ToString("F2");
                tbRoofPitch.Text = this.bldgData.RoofPitch.ToString("F2");
                tbMeanRoofHeight.Text = this.bldgData.MeanRoofHeight.ToString("F2");
            }

            // populate the combo boxes.
            cmbWindAnalysisType.Items.Clear();
            cmbASCEVersion.Items.Clear();


            foreach (var value in Enum.GetValues(typeof(ASCE7_Versions)))
            {
                cmbASCEVersion.Items.Add(value);
            }


            foreach (var value in Enum.GetValues(typeof(WindLoadCalculationTypes)))
            {
                cmbWindAnalysisType.Items.Add(value);
            }

            // populate existing parameters if any
            if (this.Parameters != null)
            {
                WindSpeedTextBox.Text = Parameters.WindSpeed.ToString();
                KztTextBox.Text = Parameters.Kzt.ToString();
                KdTextBox.Text = Parameters.Kd.ToString();
                ImportanceFactorTextBox.Text = Parameters.ImportanceFactor.ToString();

                bool found_analysis = false;
                foreach (WindLoadCalculationTypes item in Enum.GetValues(typeof(WindLoadCalculationTypes)))
                {
                    if (item == Parameters.AnalysisType)
                    {
                        cmbWindAnalysisType.SelectedIndex = (int)item;
                        found_analysis = true;
                        break;
                    }
                }
                if (found_analysis == false)
                {
                    throw new Exception("ERROR:  In WindLoadInputControl_Loaded() -- AnalysisType " + Parameters.AnalysisType.ToString() + " not found.");
                }

                bool found_version = false;
                foreach (ASCE7_Versions item in Enum.GetValues(typeof(ASCE7_Versions)))
                {
                    if (item == Version)
                    {
                        cmbASCEVersion.SelectedIndex = (int)item;
                        found_version = true;
                        break;
                    }
                }
                if (found_version == false)
                {
                    throw new Exception("ERROR:  In WindLoadInputControl_Loaded() -- Version " + Version.ToString() + " not found.");
                }

            } else
            {
                cmbWindAnalysisType.SelectedIndex = (int)WindLoadCalculationTypes.COMPONENT_AND_CLADDING;
                cmbASCEVersion.SelectedIndex = (int)ASCE7_Versions.ASCE_VER_7_16;
            }
        }

        public virtual void OnWindInputComplete(WindLoadParameters_Base parameters, ASCE7_Versions version)
        {
            WindInputComplete?.Invoke(this, new OnWindInputCompleteEventArgs(parameters, version));
        }

        public static void DrawEffectiveWindArea(Canvas canvas, EffectiveWindArea area, double scaleFactor, Brush fill_color)
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
                fill_color
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
            if(bldgData == null)
            {
                MessageBox.Show("Please input building data first");
                return;
            }

            var version_index = cmbASCEVersion.SelectedIndex;
            ASCE7_Versions version;
            switch (version_index)
            {
                case (int)ASCE7_Versions.ASCE_VER_7_16:
                    version = ASCE7_Versions.ASCE_VER_7_16;
                    break;
                case (int)ASCE7_Versions.ASCE_VER_7_22:
                    version = ASCE7_Versions.ASCE_VER_7_22;
                    break;
                default:
                    throw new Exception("ERROR:  In WindLoadInputControl_Loaded() -- Version " + version_index.ToString() + " not found.");

            }
            Parameters = GetWindLoadParameters(bldgData.RoofType, version);
            Parameters.ComputeEffectiveWindAreas(bldgData, version);

            OnWindInputComplete(Parameters, version); // raise the event where input has been completed
        }

        // Method to retrieve parameters from the input fields
        private WindLoadParameters_Base GetWindLoadParameters(RoofTypes roof_type, ASCE7_Versions version)
        {
            Version = version;

            double windSpeed = double.Parse(WindSpeedTextBox.Text);
            double kd = double.Parse(KdTextBox.Text);
            double kzt = double.Parse(KztTextBox.Text);
            double importance = double.Parse(ImportanceFactorTextBox.Text);
            string risk = ((ComboBoxItem)RiskCategoryComboBox.SelectedItem).Content.ToString();
            WindLoadCalculationTypes analysis_type = (WindLoadCalculationTypes)cmbWindAnalysisType.SelectedIndex;
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

            var windParams = WindLoadParametersFactory.Create(
                roof_type, 
                risk, 
                windSpeed, 
                exposure, 
                kd, 
                kzt, 
                importance, 
                analysis_type
                );
            if(bldgData != null)
            {
                windParams.ComputeEffectiveWindAreas(bldgData, version);
            } else
            {
                windParams = null;
            }

            return windParams;
        }
    }
}
