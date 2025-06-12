using ShearWallVisualizer.Controls;
using System.Windows;
using System.Windows.Media;

namespace ShearWallVisualizer.Dialogs
{

    public partial class LoadInputDialog : Window
    {
        private enum LoadInputModes
        {
            MODE_MANUAL = 0,
            MODE_ASCEWIND = 1
        }

        private LoadInputModes inputMode;

        public double MagnitudeX { get; private set; }
        public double LocationX { get; private set; }
        public double MagnitudeY { get; private set; }
        public double LocationY { get; private set; }

        public LoadInputDialog(double initialMagX = 0, double initialLocX = 0,
                               double initialMagY = 0, double initialLocY = 0)
        {
            InitializeComponent();

            MagnitudeXBox.Text = initialMagX.ToString();
            LocationXBox.Text = initialLocX.ToString();
            MagnitudeYBox.Text = initialMagY.ToString();
            LocationYBox.Text = initialLocY.ToString();

            ctrlWindLoadInputControl.WindInputComplete += WindCalculated; // the listener event for the ASCE wind load calcs
        }

        private void WindCalculated(object sender, WindLoadInputControl.OnWindInputCompleteEventArgs e)
        {
            MagnitudeX = 100;
            MagnitudeY = 100;
            LocationX = 50;
            LocationY = 50;

            gridASCEWind.Visibility = Visibility.Visible;
            WindLoadResultsControl_MWFRS wlrc_MWFRS = new WindLoadResultsControl_MWFRS(e._parameters);
            gridASCEWind.Children.Add(wlrc_MWFRS);

            //DialogResult = true; // signal that the input is complete
            //Close();
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
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (!double.TryParse(MagnitudeXBox.Text, out double magX))
            {
                MessageBox.Show("Invalid input for Load Magnitude X", "Input Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (!double.TryParse(LocationXBox.Text, out double locX))
            {
                MessageBox.Show("Invalid input for Location X", "Input Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (!double.TryParse(MagnitudeYBox.Text, out double magY))
            {
                MessageBox.Show("Invalid input for Load Magnitude Y", "Input Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (!double.TryParse(LocationYBox.Text, out double locY))
            {
                MessageBox.Show("Invalid input for Location Y", "Input Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MagnitudeX = magX;
            LocationX = locX;
            MagnitudeY = magY;
            LocationY = locY;

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
    }
}
