using ShearWallCalculator.BuildingInfo;
using ShearWallCalculator.WindLoadCalculations;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using static ShearWallCalculator.WindLoadCalculations.WindLoadCalculator_Base;

namespace ShearWallVisualizer.Controls
{
    public partial class WindLoadResultsControl_MWFRS : UserControl
    {
        public event EventHandler<OnWindCalculatedEventArgs> WindCalculated;  // the event that signals that the drawing has been updated -- controls will listen for this at the time they are created.

        public class OnWindCalculatedEventArgs : EventArgs
        {
            public WindParameters_Base _parameters { get; }
            public BuildingData _bldg_data { get; }

            public OnWindCalculatedEventArgs(WindParameters_Base parameters, BuildingData bldg_data)
            {
                _parameters = parameters;
                _bldg_data = bldg_data;
            }
        }

        protected virtual void OnWindCalculated(WindParameters_Base parameters, BuildingData bldg_data)
        {
            WindCalculated?.Invoke(this, new OnWindCalculatedEventArgs(parameters, bldg_data));
        }

        private WindParameters_Base _parameters;

        public WindLoadResultsControl_MWFRS()
        {
            
        }

        public WindLoadResultsControl_MWFRS(WindParameters_Base parameters, BuildingData bldg_data)
        {
            InitializeComponent();

            _parameters = parameters;

            this.Loaded += WindLoadResultsControl_MWFRS_Loaded;
        }

        private void WindLoadResultsControl_MWFRS_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}
