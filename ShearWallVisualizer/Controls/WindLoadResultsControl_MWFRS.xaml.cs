using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using static ShearWallCalculator.WindLoadCalculations.WindLoadCalculator_Base;
using static ShearWallCalculator.WindLoadCalculations.WindLoadCalculator_MWFRS;

namespace ShearWallVisualizer.Controls
{
    public partial class WindLoadResultsControl_MWFRS : UserControl
    {
        public event EventHandler<OnWindCalculatedEventArgs> WindCalculated;  // the event that signals that the drawing has been updated -- controls will listen for this at the time they are created.

        public class OnWindCalculatedEventArgs : EventArgs
        {
            public WindLoadParameters _parameters { get; }
            public List<WindPressureResult_Wall_MWFRS> _wall_results { get; }
            public List<WindPressureResult_Roof_MWFRS> _roof_results { get; }

            public OnWindCalculatedEventArgs(WindLoadParameters parameters, List<WindPressureResult_Wall_MWFRS> wall_results, List<WindPressureResult_Roof_MWFRS> roof_results)
            {
                _parameters = parameters;
                _wall_results = wall_results;
                _roof_results = roof_results;
            }
        }

        protected virtual void OnWindCalculated(WindLoadParameters parameters, List<WindPressureResult_Wall_MWFRS> wall_results, List<WindPressureResult_Roof_MWFRS> roof_results)
        {
            WindCalculated?.Invoke(this, new OnWindCalculatedEventArgs(parameters, wall_results, roof_results));
        }

        private WindLoadParameters _parameters;

        public List<WindPressureResult_Wall_MWFRS> wall_results = new List<WindPressureResult_Wall_MWFRS>();
        public List<WindPressureResult_Roof_MWFRS> roof_results = new List<WindPressureResult_Roof_MWFRS>();

        public WindLoadResultsControl_MWFRS()
        {
            
        }

        public WindLoadResultsControl_MWFRS(WindLoadParameters parameters)
        {
            InitializeComponent();

            _parameters = parameters;

            this.Loaded += WindLoadResultsControl_MWFRS_Loaded;
        }

        private void WindLoadResultsControl_MWFRS_Loaded(object sender, RoutedEventArgs e)
        {
            Dictionary<WindZones_Walls_MWFRS, double> wall_zones = Calculate_WallZones_MWFRS(_parameters);
            Dictionary<WindZones_Roof_MWFRS, double> roof_zones = CalculateMWFRS_RoofZones(_parameters);

            // compute the wind load results tables
            wall_results = CalculateWallPressureResults_MWFRS(_parameters, wall_zones);
            roof_results = CalculateRoofPressureResults_MWFRS(_parameters, roof_zones);

            tbl_qh.Text = Math.Round(ShearWallCalculator.WindLoadCalculations.WindLoadCalculator_MWFRS.CalculateDynamicWindPressure(_parameters, _parameters.BuildingHeight), 2).ToString();
            tbl_theta.Text = Math.Round(_parameters.RoofPitch, 2).ToString();
            tbl_hOverL.Text = Math.Round(_parameters.BuildingHeight / _parameters.BuildingLength, 2).ToString();
            tbl_h.Text = Math.Round(_parameters.BuildingHeight, 2).ToString();
            tbl_windOrientation.Text = _parameters.RidgeDirection;

            // Display wall results in the DataGrids
            WallResultsDataGrid.ItemsSource = null;
            WallResultsDataGrid.ItemsSource = wall_results;

            RoofResultsDataGrid.ItemsSource = null;
            RoofResultsDataGrid.ItemsSource = roof_results;

            spResultsAndCanvas.Children.Add(new WindLoadGraphicCanvas(_parameters, wall_results, roof_results));

            OnWindCalculated(_parameters, wall_results, roof_results);
        }
    }
}
