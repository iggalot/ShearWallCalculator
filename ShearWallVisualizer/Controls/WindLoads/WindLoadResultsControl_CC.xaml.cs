using ShearWallCalculator.BuildingInfo;
using ShearWallCalculator.WindLoadCalculations;
using System;
using System.Windows;
using System.Windows.Controls;

namespace ShearWallVisualizer.Controls
{
    public partial class WindLoadResultsControl_CC : UserControl
    {
        public event EventHandler<OnWindCalculatedEventArgs> WindCalculated;  // the event that signals that the drawing has been updated -- controls will listen for this at the time they are created.

        public class OnWindCalculatedEventArgs : EventArgs
        {
            public WindParameters_Base _parameters { get; }

            public OnWindCalculatedEventArgs(WindParameters_Base parameters)
            {
                _parameters = parameters;
            }
        }

        protected virtual void OnWindCalculated(WindParameters_Base parameters)
        {
            WindCalculated?.Invoke(this, new OnWindCalculatedEventArgs(parameters));
        }

        WindLoadCalculator_Base windLoadCalculator { get; set; } = null; // the calculator for whic this control is based

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

                if (windLoadCalculator.RoofAreaCalculator.HasCritDim)
                {
                    sp_a.Visibility = Visibility.Visible;
                    tbl_a.Text = windLoadCalculator.RoofAreaCalculator.CritDim_a.ToString("F2");
                } else {
                    sp_a.Visibility = Visibility.Collapsed;
                }

                // populate the bulding data summary
                if (windLoadCalculator.buildingData == null)
                {
                    spBuildingData.Visibility = Visibility.Collapsed;
                }
                else
                {
                    spBuildingData.Visibility = Visibility.Visible;

                    spBuildingData.Children.Clear();
                    BuildingInfoSummaryControl ctrl = new BuildingInfoSummaryControl(windLoadCalculator.buildingData);
                    spBuildingData.Children.Add(ctrl);
                }

                // populate the bulding data summary
                if (windLoadCalculator.buildingData == null)
                {
                    spBuildingData.Visibility = Visibility.Collapsed;
                }
                else
                {
                    spBuildingData.Visibility = Visibility.Visible;

                    spBuildingData.Children.Clear();
                    BuildingInfoSummaryControl ctrl = new BuildingInfoSummaryControl(windLoadCalculator.buildingData);
                    spBuildingData.Children.Add(ctrl);
                }

                txtTitle_BuildingLengthWalls.Text = "BuildingLength Wall -- " + windLoadCalculator.WallAreaCalculator_BldgLength.Note;
                txtTitle_BuildingWidthWalls.Text = "BuildingWidth Walls -- " + windLoadCalculator.WallAreaCalculator_BldgWidth.Note;

            }
        }
    }
}
