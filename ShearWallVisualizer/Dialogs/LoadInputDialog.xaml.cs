using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ShearWallVisualizer.Dialogs
{
    public partial class LoadInputDialog : Window
    {
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

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
