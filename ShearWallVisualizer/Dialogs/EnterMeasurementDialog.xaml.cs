using System.Windows;

namespace ShearWallVisualizer.Dialogs
{
    public partial class EnterMeasurementDialog : Window
    {
        public double? RealWorldDistance { get; private set; }

        public EnterMeasurementDialog()
        {
            InitializeComponent();
            txtDistance.Focus();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(txtDistance.Text, out double value))
            {
                RealWorldDistance = value;
                DialogResult = true;
            }
            else
            {
                MessageBox.Show("Please enter a valid number.");
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
