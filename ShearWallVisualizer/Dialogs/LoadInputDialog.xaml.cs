using ASCE7WindLoadCalculator;
using System;
using System.Linq;
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
        private WindAnalysisResult lastWind;

        /// <summary>
        /// Design load cases for the shear walls when ASCE wind is used (ASCE 7 Fig. 27.3-8), or null for a
        /// manually entered load.
        /// </summary>
        public System.Collections.Generic.List<ShearWallCalculator.LateralLoadCase> LoadCases { get; private set; }

        /// <summary>Fig. 27.3-8 exception chosen in this dialog (load cases 1 and 3 only).</summary>
        public bool OnlyCases1And3 => cbOnlyCases1And3?.IsChecked == true;

        private void WindCalculated(object sender, WindLoadInputControl.OnWindInputCompleteEventArgs e)
        {
            lastWind = e._windLoadCalculator_MWFRS_Length != null && e._windLoadCalculator_MWFRS_Width != null
                ? new WindAnalysisResult(e._windLoadCalculator_CC, e._windLoadCalculator_MWFRS_Length, e._windLoadCalculator_MWFRS_Width)
                : null;
            UpdateWindLoadCases();
        }

        private void LoadCaseOption_Changed(object sender, RoutedEventArgs e) => UpdateWindLoadCases();

        /// <summary>
        /// Turns the MWFRS results into the shear-wall load cases: each direction's force is walls + roof
        /// (external pressures; internal pressure cancels), not less than the 27.1.5 minimum, then the
        /// Fig. 27.3-8 load cases with their torsion.
        /// </summary>
        private void UpdateWindLoadCases()
        {
            if (lastWind == null)
            {
                MagnitudeX = MagnitudeY = 0;
                LoadCases = null;
                if (tbWindLoadSummary != null) tbWindLoadSummary.Text = "Enter the building and wind inputs to calculate the wind loads.";
                return;
            }

            var lateral = WindLateralLoads.Compute(lastWind, cbOnlyCases1And3?.IsChecked == true);
            MagnitudeX = lateral.AlongLength.DesignKips;
            MagnitudeY = lateral.AlongWidth.DesignKips;
            LoadCases = lateral.LoadCases
                .Select(c => new ShearWallCalculator.LateralLoadCase(c.Name, c.Vx, c.Vy, c.Mt, applyMinimumEccentricity: false))
                .ToList();

            string Line(DirectionalWindForce f, string axis) =>
                $"Wind {f.Direction} ({axis}): {f.DesignKips:0.0} kips" +
                $"  [walls {f.WallsKips:0.0} + roof {f.RoofKips:0.0} = {f.TotalKips:0.0}; 16/8 psf minimum {f.MinimumKips:0.0}" +
                (f.MinimumGoverns ? " governs" : "") + "]";
            if (tbWindLoadSummary != null)
                tbWindLoadSummary.Text =
                    Line(lateral.AlongLength, "X") + "\n" + Line(lateral.AlongWidth, "Y") + "\n" +
                    $"{LoadCases.Count} load cases (ASCE 7 Fig. 27.3-8 {(lateral.OnlyCases1And3 ? "cases 1 and 3" : "cases 1-4, with torsion")}) " +
                    "will be sent to the shear-wall calculator; each wall uses its governing case.";
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
                LoadCases = null; // a single manual load
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
