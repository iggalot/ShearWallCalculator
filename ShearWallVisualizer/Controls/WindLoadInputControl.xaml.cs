using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace ShearWallVisualizer.Controls
{
    public partial class WindLoadInputControl : UserControl
    {
        public event EventHandler<OnWindCalculatedEventArgs> WindCalculated;  // the event that signals that the drawing has been updated -- controls will listen for this at the time they are created.

        public class OnWindCalculatedEventArgs : EventArgs 
        {
            public WindLoadParameters _parameters { get; }

            public OnWindCalculatedEventArgs(WindLoadParameters parameters)
            {
                _parameters = parameters;
            }
        }

        protected virtual void OnWindCalculated(WindLoadParameters parameters)
        {
            WindCalculated?.Invoke(this, new OnWindCalculatedEventArgs(parameters));
        }


        public WindLoadInputControl()
        {
            InitializeComponent();
        }

        // Event handler for the Compute Button click
        private void ComputeButton_Click(object sender, RoutedEventArgs e)
        {
            WindLoadParameters parameters = GetWindLoadParameters();
            OnWindCalculated(parameters); // raise the event where input has been completed
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
            string exposure = ((ComboBoxItem)ExposureCategoryComboBox.SelectedItem).Content.ToString();
            string enclosure = ((ComboBoxItem)EnclosureComboBox.SelectedItem).Content.ToString();
            string ridgeDir = ((ComboBoxItem)RidgeDirectionComboBox.SelectedItem).Content.ToString();

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
                RidgeDirection = ridgeDir
            };
        }
    }

    // Wind Load Parameters class
    public class WindLoadParameters
    {
        public string RiskCategory { get; set; }
        public double WindSpeed { get; set; }
        public string ExposureCategory { get; set; }
        public double BuildingHeight { get; set; }
        public string EnclosureClassification { get; set; }
        public double Kd { get; set; }
        public double Kzt { get; set; }
        public double GustFactor { get; set; } = 0.85;
        public double ImportanceFactor { get; set; }
        public double BuildingLength { get; set; }
        public double BuildingWidth { get; set; }
        public double RoofPitch { get; set; }
        public string RidgeDirection { get; set; }
    }
}
