using ShearWallVisualizer.Controls;
using System;
using System.Windows;

namespace ShearWallVisualizer.Dialogs
{
    public partial class ImageMeasurementWindow : Window
    {
        public event EventHandler<ImageMeasurementEventArgs> MeasurementCompleted;

        public ImageMeasurementWindow()
        {
            InitializeComponent();

            // Create and add ImagePreviewControl to the window
            var imagePreviewControl = new ImagePreviewControl();
            imagePreviewControl.MeasurementCompleted += OnMeasurementCompleted;
            imagePreviewControl.HorizontalAlignment = HorizontalAlignment.Stretch;
            imagePreviewControl.VerticalAlignment = VerticalAlignment.Stretch;

            // Add the control to the grid in the window
            layoutGrid.Children.Add(imagePreviewControl);
        }

        // Event handler for when measurement is completed
        private void OnMeasurementCompleted(object sender, ImageMeasurementEventArgs e)
        {
            // Raise the MeasurementCompleted event so MainWindow can capture the data
            MeasurementCompleted?.Invoke(this, e);

            // Optionally close the window after the measurement is completed
            this.Close();
        }
    }
}
