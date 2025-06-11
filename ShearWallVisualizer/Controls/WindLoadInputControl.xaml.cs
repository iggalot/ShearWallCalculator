using System;
using System.Windows;
using System.Windows.Controls;
using static ShearWallCalculator.WindLoadCalculations.WindLoadCalculator_Base;

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

        public virtual void OnWindInputComplete(WindLoadParameters parameters)
        {
            WindInputComplete?.Invoke(this, new OnWindInputCompleteEventArgs(parameters));
        }

        public WindLoadInputControl()
        {
            InitializeComponent();
        }

        // Event handler for the Compute Button click
        private void ComputeButton_Click(object sender, RoutedEventArgs e)
        {
            WindLoadParameters parameters = GetWindLoadParameters();
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


}
