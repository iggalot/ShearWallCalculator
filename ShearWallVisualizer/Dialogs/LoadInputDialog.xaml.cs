using ASCE7WindLoadCalculator;
using System;
using System.Windows;
using System.Windows.Media;

namespace ShearWallVisualizer.Dialogs
{
    /// <summary>
    /// This class comptues the ASCE7 wind loads on our shear wall building, or allows manual override of the loads to be used in the shear wall calculator
    /// </summary>
    public partial class LoadInputDialog : Window
    {
        private enum LoadInputModes
        {
            MODE_MANUAL = 0,
            MODE_ASCEWIND = 1
        }

        /// the event that signals that the loads have been computed -- controls will listen for this at the time they are created.
        public event EventHandler<OnLoadsComputedCompleteEventArgs> LoadsComputedComplete;  
        public class OnLoadsComputedCompleteEventArgs : EventArgs
        {
            public double _magnitude_x { get; set;  }
            public double _magnitude_y { get; set; }

            public OnLoadsComputedCompleteEventArgs(double x, double y)
            {
                _magnitude_x = x;
                _magnitude_y = y;
            }
        }

        private LoadInputModes inputMode;
        private BuildingData _buildingData;
        private WindParameters_Base _parameters;
        private ASCE7_Versions _version;

        public double MagnitudeX { get; private set; }
        public double MagnitudeY { get; private set; }

        public LoadInputDialog(
            BuildingData buildingData,
            WindParameters_Base parameters = null,
            ASCE7_Versions version = ASCE7_Versions.ASCE_VER_7_16,
            double initialMagX = 0, double initialLocX = 0,
                               double initialMagY = 0, double initialLocY = 0)
        {
            InitializeComponent();

            _buildingData = buildingData;
            _parameters = parameters;
            _version = version;

            MagnitudeXBox.Text = initialMagX.ToString();
            MagnitudeYBox.Text = initialMagY.ToString();

            this.Loaded += (s, e) =>
            {
                var buildingControl = new BuildingDataInputControl(buildingData);
                //buildingControl.BuildingDataInputComplete += BuildingInputCompleted;
                ctrBuildingDataInputControl.Content = buildingControl;
                buildingControl.BuildingDataInputComplete += BuildingInputCompleted;

                var windControl = new WindLoadInputControl(buildingData, version, parameters); // pass the initial values to the ctrBuildingDataInputControl.
                windControl.WindInputComplete += WindCalculated; // the listener event for the ASCE wind load calcs
                ctrlWindLoadInputControl.Content = windControl;
            };
        }

        /// <summary>
        /// The task to do when the building data has been entered
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BuildingInputCompleted(object sender, BuildingDataInputControl.OnBuildingDataInputCompleteEventArgs e)
        {
            _buildingData = e._bldg_data;

            //MessageBox.Show("Building data has been changed.");
            if(ctrlWindLoadInputControl.Content is WindLoadInputControl oldControl)
            {
                oldControl.WindInputComplete -= WindCalculated;  // clear the old event
                ctrlWindLoadInputControl.Content = null;
            }

            // rebuild the wind control event
            var windControl = new WindLoadInputControl(_buildingData, _version, _parameters); // pass the initial values to the ctrBuildingDataInputControl.
            windControl.WindInputComplete += WindCalculated; // the listener event for the ASCE wind load calcs
            ctrlWindLoadInputControl.Content = windControl;
        }

        /// <summary>
        /// The tasks to do when the wind load data has been entered
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WindCalculated(object sender, WindLoadInputControl.OnWindInputCompleteEventArgs e)
        {
            var calculator_cc = e._windLoadCalculator_CC; ;
            var calculator_mwfrs_length = e._windLoadCalculator_MWFRS_Length;
            var calculator_mwfrs_width = e._windLoadCalculator_MWFRS_Width;

            double magX = 0;
            if (calculator_mwfrs_length != null)
            {
                foreach (var areaEntry in calculator_mwfrs_length.WallAreaCalculator_BldgLength.effWindAreas)
                {
                    // look for the ID to a "WW" windward wall and add this to the total
                    if (areaEntry.Value.Label_Short == "WW")
                    {
                        int id = areaEntry.Key;
                        var area = areaEntry.Value.Area;
                        magX += calculator_mwfrs_length.windPressureWall_Pos_External_MWFRS[id].NetPressure * area;
                    }

                    // look for the ID to a "LW" windward wall and subtract this value
                    if (areaEntry.Value.Label_Short == "LW")
                    {
                        int id = areaEntry.Key;
                        var area = areaEntry.Value.Area;
                        magX += (-1.0) * calculator_mwfrs_length.windPressureWall_Neg_External_MWFRS[id].NetPressure * area;

                    }
                }
            }

            double magY = 0;
            if (calculator_mwfrs_width != null)
            {
                foreach (var areaEntry in calculator_mwfrs_width.WallAreaCalculator_BldgLength.effWindAreas)
                {
                    // look for the ID to a "WW" windward wall and add this to the total
                    if (areaEntry.Value.Label_Short == "WW")
                    {
                        int id = areaEntry.Key;
                        var area = areaEntry.Value.Area;
                        magY += calculator_mwfrs_width.windPressureWall_Pos_External_MWFRS[id].NetPressure * area;
                    }

                    // look for the ID to a "LW" windward wall and subtract this value
                    if (areaEntry.Value.Label_Short == "LW")
                    {
                        int id = areaEntry.Key;
                        var area = areaEntry.Value.Area;
                        magY += (-1.0) * calculator_mwfrs_width.windPressureWall_Neg_External_MWFRS[id].NetPressure * area;
                    }
                }
            }

            MagnitudeX = magX;
            MagnitudeY = magY;
        }

        private void Update()
        {
            switch (inputMode)
            {
                case LoadInputModes.MODE_MANUAL:
                    gridManual.Visibility = Visibility.Visible;
                    gridASCEWind.Visibility = Visibility.Collapsed;
                    stackPanelButtons.Visibility = Visibility.Visible;

                    btnManual.BorderThickness = new Thickness(3);
                    btnManual.Background = new SolidColorBrush(Colors.SeaGreen);
                    btnASCEWind.BorderThickness = new Thickness(0);
                    btnASCEWind.Background = new SolidColorBrush(Colors.Transparent);
                    break;
                case LoadInputModes.MODE_ASCEWIND:
                    gridManual.Visibility = Visibility.Collapsed;
                    gridASCEWind.Visibility = Visibility.Visible;
                    stackPanelButtons.Visibility = Visibility.Visible;

                    btnManual.BorderThickness = new Thickness(0);
                    btnManual.Background = new SolidColorBrush(Colors.Transparent);
                    btnASCEWind.BorderThickness = new Thickness(3);
                    btnASCEWind.Background = new SolidColorBrush(Colors.SeaGreen);
                    break;
                default:
                    throw new NotImplementedException("ERROR: In LoadInputDialog.cs in Update() -- LoadInputModes: " + inputMode + " not implemented.");
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            double magX = MagnitudeX;
            double magY = MagnitudeY;
            if(inputMode == LoadInputModes.MODE_MANUAL)
            {
                if (!double.TryParse(MagnitudeXBox.Text, out magX))
                {
                    MessageBox.Show("Invalid input for Load Magnitude X", "Input Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                if (!double.TryParse(MagnitudeYBox.Text, out magY))
                {
                    MessageBox.Show("Invalid input for Load Magnitude Y", "Input Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                MagnitudeX = magX;
                MagnitudeY = magY;
            }
            
            // parse the ASCE wind load calculations
            else if (inputMode == LoadInputModes.MODE_ASCEWIND)
            {

            }

            OnLoadsComputed(MagnitudeX, MagnitudeY);

            DialogResult = true; // signal that the input is complete
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void btnASCEWind_Click(object sender, RoutedEventArgs e)
        {
            inputMode = LoadInputModes.MODE_ASCEWIND;

            Update();
        }

        private void btnManual_Click(object sender, RoutedEventArgs e)
        {
            inputMode = LoadInputModes.MODE_MANUAL;
            Update();
        }

        public virtual void OnLoadsComputed(double x, double y)
        {
            LoadsComputedComplete?.Invoke(this, new OnLoadsComputedCompleteEventArgs(x, y));
        }
    }
}
