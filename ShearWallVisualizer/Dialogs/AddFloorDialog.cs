using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ShearWallVisualizer.Dialogs
{
    /// <summary>
    /// ADD FLOOR: the plate height of the new story (its floor to the top plate of its walls), and whether to start it
    /// from a copy of the floor below. The new floor's elevation and the new eave height are shown as the height is typed.
    /// </summary>
    public class AddFloorDialog : Window
    {
        private readonly TextBox tbHeight;
        private readonly CheckBox cbCopy;
        private readonly TextBlock tbInfo, tbError;
        private readonly Button ok;
        private readonly double floorElevation;

        /// <summary>The plate height entered (ft).</summary>
        public double PlateHeight { get; private set; }

        /// <summary>True to start the new level with a copy of the walls and diaphragms of the level below.</summary>
        public bool CopyBelow => cbCopy.IsChecked == true;

        /// <param name="newLevel">The number of the new level (2 or 3).</param>
        /// <param name="floorElevation">Its floor elevation: the top of the walls below (ft).</param>
        /// <param name="suggested">The plate height offered (the story below's).</param>
        /// <param name="belowHasContent">The level below has walls or diaphragms to copy.</param>
        public AddFloorDialog(int newLevel, double floorElevation, double suggested, bool belowHasContent)
        {
            this.floorElevation = floorElevation;
            Title = "Add floor";
            SizeToContent = SizeToContent.WidthAndHeight;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ShowInTaskbar = false;
            FontFamily = new FontFamily("Segoe UI");
            FontSize = 13;

            var panel = new StackPanel { Margin = new Thickness(18), Width = 380 };
            panel.Children.Add(new TextBlock { Text = $"Level {newLevel}: story {newLevel} and the roof over it", FontSize = 15, FontWeight = FontWeights.SemiBold });
            panel.Children.Add(new TextBlock
            {
                Text = $"The new story stands on the floor at {Ft(floorElevation)} (the top of the walls of level {newLevel - 1}), which becomes a floor diaphragm. " +
                       "The wind is shared again between the floors.",
                TextWrapping = TextWrapping.Wrap, Foreground = Brushes.DimGray, Margin = new Thickness(0, 4, 0, 12),
            });

            var row = new StackPanel { Orientation = Orientation.Horizontal };
            row.Children.Add(new TextBlock { Text = "Plate height of story " + newLevel, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) });
            tbHeight = new TextBox
            {
                Text = suggested.ToString("0.###", CultureInfo.CurrentCulture), Width = 70, Padding = new Thickness(4, 2, 4, 2),
                VerticalContentAlignment = VerticalAlignment.Center, HorizontalContentAlignment = HorizontalAlignment.Right,
            };
            tbHeight.TextChanged += (s, e) => Validate();
            row.Children.Add(tbHeight);
            row.Children.Add(new TextBlock { Text = "ft", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0, 0, 0) });
            panel.Children.Add(row);

            tbInfo = new TextBlock { Margin = new Thickness(0, 6, 0, 0), Foreground = Brushes.DimGray, TextWrapping = TextWrapping.Wrap };
            tbError = new TextBlock { Margin = new Thickness(0, 4, 0, 0), Foreground = new SolidColorBrush(Color.FromRgb(0xC6, 0x28, 0x28)), TextWrapping = TextWrapping.Wrap };
            panel.Children.Add(tbInfo);
            panel.Children.Add(tbError);

            cbCopy = new CheckBox
            {
                Content = $"Start from a copy of level {newLevel - 1} (its walls, with their design, and its diaphragms)",
                IsChecked = belowHasContent, IsEnabled = belowHasContent, Margin = new Thickness(0, 12, 0, 0),
            };
            panel.Children.Add(cbCopy);

            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 16, 0, 0) };
            ok = new Button { Content = "Add floor", IsDefault = true, Padding = new Thickness(14, 4, 14, 4), Margin = new Thickness(0, 0, 8, 0) };
            ok.Click += (s, e) => { DialogResult = true; };
            var cancel = new Button { Content = "Cancel", IsCancel = true, Padding = new Thickness(14, 4, 14, 4) };
            buttons.Children.Add(ok);
            buttons.Children.Add(cancel);
            panel.Children.Add(buttons);

            Content = panel;
            Loaded += (s, e) => { tbHeight.Focus(); tbHeight.SelectAll(); };
            Validate();
        }

        /// <summary>The smallest and largest plate height accepted (ft).</summary>
        public const double MinPlate = 6, MaxPlate = 30;

        private void Validate()
        {
            bool okNumber = double.TryParse(tbHeight.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out double h);
            string error = !okNumber ? "Enter the plate height in feet."
                : h < MinPlate || h > MaxPlate ? $"A story plate height is {MinPlate:0} to {MaxPlate:0} ft." : null;
            tbError.Text = error ?? "";
            ok.IsEnabled = error == null;
            if (error == null)
            {
                PlateHeight = h;
                tbInfo.Text = $"Floor at {Ft(floorElevation)}; the new eave height is {Ft(floorElevation + h)}.";
            }
            else tbInfo.Text = "";
        }

        private static string Ft(double v) => v.ToString("0.##", CultureInfo.CurrentCulture) + " ft";
    }
}
