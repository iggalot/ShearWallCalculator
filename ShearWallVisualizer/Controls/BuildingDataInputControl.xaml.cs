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
            if (bldgData == null)
            {
                bldgData = new BuildingData();
            }

            cmbRoofType.Items.Clear();
            cmbEnclosure.Items.Clear();
            cmbRidgeDirection.Items.Clear();

            // Ridge Direction
            foreach (var value in Enum.GetValues(typeof(RidgeDirections)))
            {
                cmbRidgeDirection.Items.Add(value);
            }

            switch (bldgData.RidgeDirection)
            {
                case RidgeDirections.RIDGE_DIR_NONE: cmbRidgeDirection.SelectedIndex = (int)RidgeDirections.RIDGE_DIR_NONE; break;
                case RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH: cmbRidgeDirection.SelectedIndex = (int)RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH; break;
                case RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH: cmbRidgeDirection.SelectedIndex = (int)RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH; break;
                default: cmbRidgeDirection.SelectedIndex = (int)RidgeDirections.RIDGE_DIR_NONE; break;
            }

            // Building Enclosure type
            foreach (var value in Enum.GetValues(typeof(BuildingEnclosures)))
            {
                cmbEnclosure.Items.Add(value);
            }

            switch (bldgData.EnclosureType)
            {
                case BuildingEnclosures.BLDG_ENCLOSED: cmbEnclosure.SelectedIndex = (int)BuildingEnclosures.BLDG_ENCLOSED; break;
                case BuildingEnclosures.BLDG_PARTIALLY_ENCLOSED: cmbEnclosure.SelectedIndex = (int)BuildingEnclosures.BLDG_PARTIALLY_ENCLOSED; break;
                case BuildingEnclosures.BLDG_PARTIALLY_OPEN: cmbEnclosure.SelectedIndex = (int)BuildingEnclosures.BLDG_PARTIALLY_OPEN; break;
                case BuildingEnclosures.BLDG_OPEN: cmbEnclosure.SelectedIndex = (int)BuildingEnclosures.BLDG_OPEN; break;
                default: cmbEnclosure.SelectedIndex = (int)BuildingEnclosures.BLDG_ENCLOSED; break;
            }


            // Building Enclosure type
            foreach (var value in Enum.GetValues(typeof(RoofTypes)))
            {
                cmbRoofType.Items.Add(value);
            }

            switch (bldgData.RoofType)
            {
                case RoofTypes.ROOF_TYPE_FLAT: cmbRoofType.SelectedIndex = (int)RoofTypes.ROOF_TYPE_FLAT; break;
                case RoofTypes.ROOF_TYPE_GABLE: cmbRoofType.SelectedIndex = (int)RoofTypes.ROOF_TYPE_GABLE; break;
                case RoofTypes.ROOF_TYPE_HIP: cmbRoofType.SelectedIndex = (int)RoofTypes.ROOF_TYPE_HIP; break;
                default: cmbRoofType.SelectedIndex = (int)RoofTypes.ROOF_TYPE_FLAT; break;
            }

            BuildingHeightTextBox.Text = bldgData.BuildingHeight.ToString();
            BuildingLengthTextBox.Text = bldgData.BuildingLength.ToString();
            BuildingWidthTextBox.Text = bldgData.BuildingWidth.ToString();

            if (bldgData.RoofType == RoofTypes.ROOF_TYPE_FLAT)
            {
                bldgData.RoofPitch = 0;
                spRoofPitch.Visibility = Visibility.Collapsed;
            }
            else
            {
                spRoofPitch.Visibility = Visibility.Visible;
            }
                
            tbRoofPitch.Text = bldgData.RoofPitch.ToString();

        }

        public virtual void OnBuildingDataInputComplete(BuildingData bldg_data)
        {
            BuildingDataInputComplete?.Invoke(this, new OnBuildingDataInputCompleteEventArgs(bldg_data));
        }

        // Event handler for the Compute Button click
        private void ComputeButton_Click(object sender, RoutedEventArgs e)
        {
            double buildingHeight = double.Parse(BuildingHeightTextBox.Text);
            double length = double.Parse(BuildingLengthTextBox.Text);
            double width = double.Parse(BuildingWidthTextBox.Text);
            double pitch = double.Parse(tbRoofPitch.Text);
            RidgeDirections ridgeDir = (RidgeDirections)cmbRidgeDirection.SelectedIndex;
            RoofTypes roof_type = (RoofTypes)cmbRoofType.SelectedIndex;
            BuildingEnclosures enclosure = (BuildingEnclosures)cmbEnclosure.SelectedIndex;

            bldgData_temp = new BuildingData()
            {
                BuildingHeight = buildingHeight,
                BuildingLength = length,
                BuildingWidth = width,
                RoofPitch = pitch,
                RidgeDirection = ridgeDir,
                RoofType = roof_type,
                EnclosureType = enclosure
            };

            this.bldgData = bldgData_temp;
            OnBuildingDataInputComplete(bldgData); // raise the event where input has been completed
        }
    }
}
