using ShearWallCalculator.WindLoadCalculations;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ShearWallVisualizer.Controls
{
    public partial class WindLoadResultsControl_CC : UserControl
    {
        public event EventHandler<OnWindCalculatedEventArgs> WindCalculated;  // the event that signals that the drawing has been updated -- controls will listen for this at the time they are created.

        public class OnWindCalculatedEventArgs : EventArgs
        {
            public WindLoadParameters_Base _parameters { get; }
            public List<WindLoadCalculator_MWFRS_ASCE7_10.WindPressureResult_Wall_MWFRS> _wall_results { get; }
            public List<WindLoadCalculator_MWFRS_ASCE7_10.WindPressureResult_Roof_MWFRS> _roof_results { get; }

            public OnWindCalculatedEventArgs(WindLoadParameters_Base parameters, List<WindLoadCalculator_MWFRS_ASCE7_10.WindPressureResult_Wall_MWFRS> wall_results, List<WindLoadCalculator_MWFRS_ASCE7_10.WindPressureResult_Roof_MWFRS> roof_results)
            {
                _parameters = parameters;
                _wall_results = wall_results;
                _roof_results = roof_results;
            }
        }

        protected virtual void OnWindCalculated(WindLoadParameters_Base parameters, List<WindLoadCalculator_MWFRS_ASCE7_10.WindPressureResult_Wall_MWFRS> wall_results, List<WindLoadCalculator_MWFRS_ASCE7_10.WindPressureResult_Roof_MWFRS> roof_results)
        {
            WindCalculated?.Invoke(this, new OnWindCalculatedEventArgs(parameters, wall_results, roof_results));
        }

        WindLoadCalculator_Base windLoadCalculator { get; set; } = null; // the calculator for whic this control is based

        public List<WindLoadCalculator_MWFRS_ASCE7_10.WindPressureResult_Wall_MWFRS> wall_results = new List<WindLoadCalculator_MWFRS_ASCE7_10.WindPressureResult_Wall_MWFRS>();
        public List<WindLoadCalculator_MWFRS_ASCE7_10.WindPressureResult_Roof_MWFRS> roof_results = new List<WindLoadCalculator_MWFRS_ASCE7_10.WindPressureResult_Roof_MWFRS>();

        public WindLoadResultsControl_CC()
        {
            
        }

        public WindLoadResultsControl_CC(WindLoadCalculator_Base calculator)
        {
            InitializeComponent();

            windLoadCalculator = calculator;

            this.Loaded += WindLoadResultsControl_CC_Loaded;
        }


        private void WindLoadResultsControl_CC_Loaded(object sender, RoutedEventArgs e)
        {
            if (windLoadCalculator != null)
            {
                tbVersion.Text = windLoadCalculator.ASCEVersion.ToString();
                tbl_theta.Text = windLoadCalculator.buildingData.RoofPitch.ToString("F2");
                tbl_h.Text = windLoadCalculator.buildingData.MeanRoofHeight.ToString("F2");
                tbl_hOverB.Text = (windLoadCalculator.buildingData.MeanRoofHeight / windLoadCalculator.buildingData.BuildingWidth).ToString("F2");
                tbl_hOverL.Text = (windLoadCalculator.buildingData.MeanRoofHeight / windLoadCalculator.buildingData.BuildingLength).ToString("F2");
                tbl_roof_type.Text = windLoadCalculator.buildingData.RoofType.ToString();
            }
        }
    }
}
