using ASCE7WindLoadCalculator;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;

namespace ShearWallVisualizer.Dialogs
{
    /// <summary>
    /// Chooses the shear-wall loads: the ASCE 7 wind loads from the current wind inputs (the default, kept up
    /// to date automatically by the main window), or a manual load per direction. The wind inputs themselves
    /// are edited on the Wind loads screen ("Change the wind inputs" closes this dialog and goes there).
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
            public double _magnitude_x { get; set; }
            public double _magnitude_y { get; set; }

            public OnLoadsComputedCompleteEventArgs(double x, double y)
            {
                _magnitude_x = x;
                _magnitude_y = y;
            }
        }

        private LoadInputModes inputMode = LoadInputModes.MODE_ASCEWIND;
        private readonly WindAnalysisResult wind;
        private bool settingMode;

        public double MagnitudeX { get; private set; }
        public double MagnitudeY { get; private set; }

        /// <summary>
        /// Design load cases for the shear walls when ASCE wind is used (ASCE 7 Fig. 27.3-8), or null for a
        /// manually entered load.
        /// </summary>
        public List<ShearWallCalculator.LateralLoadCase> LoadCases { get; private set; }

        /// <summary>Fig. 27.3-8 exception chosen in this dialog (load cases 1 and 3 only).</summary>
        public bool OnlyCases1And3 => cbOnlyCases1And3?.IsChecked == true;

        /// <summary>True when the user closed the dialog to change the wind inputs.</summary>
        public bool EditWindRequested { get; private set; }

        /// <param name="manualNow">Open on Manual (the walls currently use a manual load).</param>
        public LoadInputDialog(
            BuildingData buildingData,
            WindParameters_Base parameters = null,
            ASCE7_Versions version = ASCE7_Versions.ASCE_VER_7_16,
            double initialMagX = 0, double initialMagY = 0,
            bool onlyCases1And3 = false, bool manualNow = false)
        {
            InitializeComponent();
            BuildingModel.Sketch.WindowFit.Fit(this); // on a smaller screen: fits, title bar on screen

            MagnitudeXBox.Text = initialMagX.ToString("0.##", CultureInfo.CurrentCulture);
            MagnitudeYBox.Text = initialMagY.ToString("0.##", CultureInfo.CurrentCulture);

            try
            {
                if (buildingData != null && parameters != null)
                {
                    var r = WindAnalysis.Run(version, parameters, buildingData);
                    if (r.MwfrsAlongLength != null && r.MwfrsAlongWidth != null) wind = r;
                }
            }
            catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException || ex is NotSupportedException || ex is NotImplementedException)
            {
                wind = null;
            }

            settingMode = true;
            cbOnlyCases1And3.IsChecked = onlyCases1And3;
            inputMode = manualNow ? LoadInputModes.MODE_MANUAL : LoadInputModes.MODE_ASCEWIND;
            (manualNow ? rbManual : rbASCEWind).IsChecked = true;
            settingMode = false;
            Update();
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
            if (wind == null)
            {
                if (inputMode == LoadInputModes.MODE_ASCEWIND) MagnitudeX = MagnitudeY = 0;
                LoadCases = null;
                tbWindLoadSummary.Text = "The wind inputs cannot be calculated right now. Use \"Change the wind inputs\" to fix them.";
                return;
            }

            var lateral = WindLateralLoads.Compute(wind, cbOnlyCases1And3.IsChecked == true);
            if (inputMode == LoadInputModes.MODE_ASCEWIND)
            {
                MagnitudeX = lateral.AlongLength.DesignKips;
                MagnitudeY = lateral.AlongWidth.DesignKips;
            }
            LoadCases = lateral.LoadCases
                .Select(c => new ShearWallCalculator.LateralLoadCase(c.Name, c.Vx, c.Vy, c.Mt, applyMinimumEccentricity: false))
                .ToList();

            string Line(DirectionalWindForce f, string axis) =>
                $"Wind from the {f.Direction} ({axis}): {f.DesignKips:0.0} kips" +
                $"  (walls {f.WallsKips:0.0} + roof {f.RoofKips:0.0} = {f.TotalKips:0.0}; minimum {f.MinimumKips:0.0}" +
                (f.MinimumGoverns ? " governs" : "") + ")";
            tbWindLoadSummary.Text =
                Line(lateral.AlongLength, "X") + "\n" + Line(lateral.AlongWidth, "Y") + "\n" +
                $"{LoadCases.Count} load cases (Fig. 27.3-8); each wall uses its worst case.";
        }

        private void Update()
        {
            bool manual = inputMode == LoadInputModes.MODE_MANUAL;
            gridManual.Visibility = manual ? Visibility.Visible : Visibility.Collapsed;
            gridASCEWind.Visibility = manual ? Visibility.Collapsed : Visibility.Visible;
        }

        private void Mode_Checked(object sender, RoutedEventArgs e)
        {
            if (settingMode || !IsInitialized) return;
            if (sender == rbManual)
            {
                // Manual loads stop the walls following the wind; make sure that is intended.
                var answer = MessageBox.Show(this,
                    "Manual loads replace the ASCE 7 wind loads.\n\n" +
                    "The shear walls will no longer update when you change the wind inputs, and the Wind loads " +
                    "screen will show a warning until you switch back.\n\nUse manual loads?",
                    "Use manual loads?", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
                if (answer != MessageBoxResult.Yes)
                {
                    settingMode = true;
                    rbASCEWind.IsChecked = true;
                    settingMode = false;
                    return;
                }
                inputMode = LoadInputModes.MODE_MANUAL;
                MagnitudeXBox.Focus();
            }
            else
            {
                inputMode = LoadInputModes.MODE_ASCEWIND;
            }
            Update();
            UpdateWindLoadCases();
        }

        private void EditWind_Click(object sender, RoutedEventArgs e)
        {
            EditWindRequested = true;
            DialogResult = false;
            Close();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (inputMode == LoadInputModes.MODE_MANUAL)
            {
                if (!double.TryParse(MagnitudeXBox.Text, out double magX))
                {
                    MessageBox.Show("Invalid input for Load Magnitude X", "Input Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                if (!double.TryParse(MagnitudeYBox.Text, out double magY))
                {
                    MessageBox.Show("Invalid input for Load Magnitude Y", "Input Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                MagnitudeX = magX;
                MagnitudeY = magY;
                LoadCases = null; // a single manual load
            }
            else if (LoadCases == null)
            {
                MessageBox.Show(this, "The wind loads cannot be calculated for the current wind inputs. Use \"Change the wind inputs\" to fix them.",
                    "No wind loads", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
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

        public virtual void OnLoadsComputed(double x, double y)
        {
            LoadsComputedComplete?.Invoke(this, new OnLoadsComputedCompleteEventArgs(x, y));
        }
    }
}
