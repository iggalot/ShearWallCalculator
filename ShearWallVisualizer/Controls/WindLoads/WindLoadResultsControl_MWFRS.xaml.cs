using ShearWallCalculator.BuildingInfo;
using ShearWallCalculator.Helpers;
using ShearWallCalculator.WindLoadCalculations;
using ShearWallCalculator.WindLoadCalculations.ASCE7;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

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

                BuildingDrawer.DrawPlan(cnvMWFRSPlan, windLoadCalculator.buildingData);
                BuildingDrawer.DrawElevation_BuildingLength(cnvMWFRSElevation_BuildingLength, windLoadCalculator.buildingData);
                BuildingDrawer.DrawElevation_BuildingWidth(cnvMWFRSElevation_BuildingWidth, windLoadCalculator.buildingData);

                CreateMWFRS_DataGrid_Walls(MWFRS_WallResultsDataGrid, windLoadCalculator.WallAreaCalculator_BldgLength.effWindAreas, windLoadCalculator);
            }

        }

        private void CreateMWFRS_DataGrid_Walls(DataGrid data_grid, Dictionary<int, EffectiveWindArea> areas, WindLoadCalculator_Base calc)
        {
            // Get the datagrid from the results control
            data_grid.ItemsSource = null;
            data_grid.Columns.Clear();
            data_grid.AutoGenerateColumns = false;

            // create our data object
            var col_name = new DataGridTextColumn
            {
                Header = "Name",
                Binding = new Binding("Name")
            };
            data_grid.Columns.Add(col_name);

            var col_rectangle = new DataGridTemplateColumn
            {
                Header = ""
            };

            // Define the DataTemplate in code for drawing a colored rectangle
            var factory = new FrameworkElementFactory(typeof(Rectangle));
            factory.SetValue(Rectangle.WidthProperty, 15.0);
            factory.SetValue(Rectangle.HeightProperty, 15.0);
            factory.SetValue(Rectangle.StrokeProperty, Brushes.Black);
            factory.SetBinding(Rectangle.FillProperty, new Binding("RectColor"));

            col_rectangle.CellTemplate = new DataTemplate { VisualTree = factory };
            data_grid.Columns.Add(col_rectangle);

            var col_area = new DataGridTextColumn
            {
                Header = "Area\n(sq ft)",
                Binding = new Binding("Area") { StringFormat = "0" }
            };
            data_grid.Columns.Add(col_area);

            var col_qh = new DataGridTextColumn
            {
                Header = "qh\n(psf)",
                Binding = new Binding("qh") { StringFormat = "0.0" }
            };
            data_grid.Columns.Add(col_qh);

            var col_gcp_pos = new DataGridTextColumn
            {
                Header = "Cp",
                Binding = new Binding("Cp") { StringFormat = "0.00" }
            };
            data_grid.Columns.Add(col_gcp_pos);


            var col_pos_press = new DataGridTextColumn
            {
                Header = "Press\n(psf)",
                Binding = new Binding("Press") { StringFormat = "0.0" }
            };
            data_grid.Columns.Add(col_pos_press);

            var col_pos_net_press = new DataGridTextColumn
            {
                Header = "Net\n(psf)",
                Binding = new Binding("NetPress") { StringFormat = "0.0" }
            };
            data_grid.Columns.Add(col_pos_net_press);

            var windLoadResults = new List<MWFRS_WindLoadResultsDataGrid>();
            foreach (var area in areas)
            {
                MWFRS_WindLoadResultsDataGrid data = new MWFRS_WindLoadResultsDataGrid();
                data.Name = area.Value.Label_Short;
                data.Region = area.Value.Label_Short;
                data.Area = area.Value.Area;

                // positive max external pressure
                PressureData ext_pressure;
                if (calc.windPressureWall_Pos_External_MWFRS.TryGetValue(area.Key, out ext_pressure))
                {
                    data.qh = ext_pressure.qh;
                    data.Cp = ext_pressure.GCp;
                    data.Press = ext_pressure.ExternalPressure;
                    data.NetPress = ext_pressure.NetPressure;
                }

                windLoadResults.Add(data);
            }

            data_grid.ItemsSource = windLoadResults;
        }

        public class MWFRS_WindLoadResultsDataGrid
        {
            public string Name { get; set; }
            public double Area { get; set; }
            public string Region { get; set; } // used to store the regions name so we can draw the rectangle from it
            public double qh { get; set; }
            public double Cp { get; set; }

            public double Press { get; set; }
            public double NetPress { get; set; }


            public Brush RectColor => GetColorForRegion(Region);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="region">Should be in the form of "1", "2e", etc. The regions will need a full name of with a prefix of "Zone"
        /// so "Zone1" becomes "1"</param>
        /// <returns></returns>
        public static Brush GetColorForRegion(string region)
        {
            switch (region)
            {
                case "1":
                    return Brushes.Red;
                case "1'":
                    return Brushes.IndianRed;
                case "2":
                    return Brushes.Yellow;
                case "2e":
                    return Brushes.LightYellow;
                case "2r":
                    return Brushes.Goldenrod;
                case "2n":
                    return Brushes.YellowGreen;
                case "3":
                    return Brushes.Green;
                case "3e":
                    return Brushes.GreenYellow;
                case "3r":
                    return Brushes.LightGreen;
                case "4":
                    return Brushes.MediumOrchid;
                case "5":
                    return Brushes.Purple;
                case "WWR":
                    return Brushes.LightGray;
                case "LWR":
                    return Brushes.Gray;
                case "WW":
                    return Brushes.LightGray;
                case "LW":
                    return Brushes.Gray;
                case "SW":
                    return Brushes.DarkGray;
                default:
                    return Brushes.Black;
            }
        }
    }
}
