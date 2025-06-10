using calculator;
using ShearWallCalculator;
using System;
using System.Windows;
using System.Windows.Controls;

namespace ShearWallVisualizer.Controls
{
    /// <summary>
    /// Interaction logic for WallDesignControl.xaml
    /// </summary>
    public partial class WallDesignControl : UserControl
    {
        WallData Data { get; set; }
        public WallDesignControl(ShearWallSelector selector)
        {
            InitializeComponent();

            Data = selector.Data;

            this.Loaded += WallDesignControl_Loaded;
        }

        private void WallDesignControl_Loaded(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
