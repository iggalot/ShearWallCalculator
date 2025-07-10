using ShearWallCalculator.BuildingInfo;
using ShearWallCalculator.WindLoadCalculations;
using System;
using System.Windows;
using System.Windows.Controls;

namespace ShearWallVisualizer.Controls
{
    /// <summary>
    /// Interaction logic for BuildingDataInputControl.xaml
    /// </summary>
    public partial class BuildingDataInputControl : UserControl
    {
        public event EventHandler<OnBuildingDataInputCompleteEventArgs> BuildingDataInputComplete;  // the event that signals that the drawing has been updated -- controls will listen for this at the time they are created.

        public class OnBuildingDataInputCompleteEventArgs : EventArgs
        {
            public BuildingData _bldg_data { get; }

            public OnBuildingDataInputCompleteEventArgs(BuildingData bldg_data)
            {
                _bldg_data = bldg_data;
            }
        }

        public BuildingData bldgData { get; set; } = null;
        public BuildingData bldgData_temp { get; set; }  // stores the building data while it is being edited

        public BuildingDataInputControl()
        {
            InitializeComponent();

            this.Loaded += BuildingDataInputControl_Loaded;
        }

        public BuildingDataInputControl(BuildingData bldg_data)
        {
            InitializeComponent();

            bldgData = bldg_data;
            

            
            this.Loaded += BuildingDataInputControl_Loaded;
        }

        private void BuildingDataInputControl_Loaded(object sender, RoutedEventArgs e)
        {
            stackPanelButtons.Visibility = Visibility.Collapsed;

            cmbRoofType.Items.Clear();

            foreach (var value in Enum.GetValues(typeof(RoofTypes)))
            {
                cmbRoofType.Items.Add(value);
            }

            cmbRoofType.SelectedIndex = 0;

            if(bldgData == null)
            {
                bldgData = new BuildingData();
            }

            BuildingHeightTextBox.Text = bldgData.BuildingHeight.ToString();
            BuildingLengthTextBox.Text = bldgData.BuildingLength.ToString();
            BuildingWidthTextBox.Text = bldgData.BuildingWidth.ToString();
            RoofPitchTextBox.Text = bldgData.RoofPitch.ToString();

            switch (bldgData.RidgeDirection)
            {
                case "Perpendicular to Wind": RidgeDirectionComboBox.SelectedIndex = 0; break;
                case "Parallel to Wind": RidgeDirectionComboBox.SelectedIndex = 1; break;
            }

            switch (bldgData.RoofType)
            {
                case RoofTypes.ROOF_TYPE_FLAT: cmbRoofType.SelectedIndex = 0; break;
                case RoofTypes.ROOF_TYPE_GABLE: cmbRoofType.SelectedIndex = 1; break;
                case RoofTypes.ROOF_TYPE_HIP: cmbRoofType.SelectedIndex = 2; break;
            }
        }

        public virtual void OnBuildingDataInputComplete(BuildingData bldg_data)
        {
            BuildingDataInputComplete?.Invoke(this, new OnBuildingDataInputCompleteEventArgs(bldg_data));
        }

        // Event handler for the Compute Button click
        private void ComputeButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Building created");
            double buildingHeight = double.Parse(BuildingHeightTextBox.Text);
            double length = double.Parse(BuildingLengthTextBox.Text);
            double width = double.Parse(BuildingWidthTextBox.Text);
            double pitch = double.Parse(RoofPitchTextBox.Text);
            string ridgeDir = ((ComboBoxItem)RidgeDirectionComboBox.SelectedItem).Content.ToString();
            RoofTypes roof_type = (RoofTypes)cmbRoofType.SelectedIndex;

            bldgData_temp = new BuildingData()
            {
                BuildingHeight = buildingHeight,
                BuildingLength = length,
                BuildingWidth = width,
                RoofPitch = pitch,
                RidgeDirection = ridgeDir,
                RoofType = roof_type
            };

            // reveal the buttons
            stackPanelButtons.Visibility = Visibility.Visible;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("OK button clicked");
            this.bldgData = bldgData_temp;
            OnBuildingDataInputComplete(bldgData); // raise the event where input has been completed
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            bldgData_temp = bldgData;
        }
    }
}
