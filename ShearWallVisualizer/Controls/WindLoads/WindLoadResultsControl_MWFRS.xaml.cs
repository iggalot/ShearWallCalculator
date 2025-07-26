using ShearWallCalculator.BuildingInfo;
using ShearWallCalculator.Helpers;
using ShearWallCalculator.WindLoadCalculations;
using System;
using System.Windows;
using System.Windows.Controls;

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

        WindLoadCalculator_Base windLoadCalculator { get; set; } = null; // the calculator for whic this control is based


        public WindLoadResultsControl_MWFRS()
        {
            
        }

        public WindLoadResultsControl_MWFRS(WindLoadCalculator_Base calculator)
        {
            InitializeComponent();

            windLoadCalculator = calculator;

            this.Loaded += WindLoadResultsControl_MWFRS_Loaded;
        }

        private void WindLoadResultsControl_MWFRS_Loaded(object sender, RoutedEventArgs e)
        {
            if (windLoadCalculator != null)
            {
                tbVersion.Text = windLoadCalculator.ASCEVersion.ToString();
                tbl_theta.Text = windLoadCalculator.buildingData.RoofPitch.ToString("F2");
                tbl_h.Text = windLoadCalculator.buildingData.MeanRoofHeight.ToString("F2");

                if (windLoadCalculator.RoofAreaCalculator.HasCritDim)
                {
                    sp_a.Visibility = Visibility.Visible;
                    tbl_a.Text = windLoadCalculator.RoofAreaCalculator.CritDim_a.ToString("F2");
                }
                else
                {
                    sp_a.Visibility = Visibility.Collapsed;
                }

                tbl_hOverB.Text = (windLoadCalculator.buildingData.MeanRoofHeight / windLoadCalculator.buildingData.BuildingWidth).ToString("F2");
                tbl_hOverL.Text = (windLoadCalculator.buildingData.MeanRoofHeight / windLoadCalculator.buildingData.BuildingLength).ToString("F2");
                tbl_roof_type.Text = windLoadCalculator.buildingData.RoofType.ToString();

                txtTitle_BuildingLengthWalls.Text = "BuildingLength Wall -- " + windLoadCalculator.WallAreaCalculator_BldgLength.Note;
                txtTitle_BuildingWidthWalls.Text = "BuildingWidth Walls -- " + windLoadCalculator.WallAreaCalculator_BldgWidth.Note;

                BuildingDrawer.DrawPlan(cnvMWFRSPlan, windLoadCalculator.buildingData);
                BuildingDrawer.DrawElevation_BuildingLength(cnvMWFRSElevation_BuildingLength, windLoadCalculator.buildingData);
                BuildingDrawer.DrawElevation_BuildingWidth(cnvMWFRSElevation_BuildingWidth, windLoadCalculator.buildingData);
            }

        }
    }
}
