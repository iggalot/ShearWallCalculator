using calculator;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ShearWallCalculator;
using ShearWallCalculator.BuildingInfo;
using ShearWallCalculator.Helpers;
using ShearWallCalculator.Interfaces;
using ShearWallCalculator.WindLoadCalculations;
using ShearWallCalculator.WindLoadCalculations.Chapter30.Figure30_3;
using ShearWallCalculator.WindLoadCalculations.WindLoadCalculators;
using ShearWallVisualizer.Controls;
using ShearWallVisualizer.Dialogs;
using ShearWallVisualizer.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static ShearWallVisualizer.Controls.DiaphragmDataControl;
using static ShearWallVisualizer.Controls.WallDataControl;

namespace ShearWallVisualizer
{
    /// <summary>
    /// The enum that controls the ordering of the information tabs in the main application
    /// </summary>
    public enum MenuTabs
    {
        BuildingData = 0,
        WindLoadInput = 1,
        ShearWallInfo = 2,
        WindLoadResultsMWFRS = 3,
        WindLoadResultsCC = 4,
        ShearWallCalculations = 5

    }

    public partial class MainWindow : Window
    {
        public ShearWallCalculatorBase Calculator = new ShearWallCalculator_RigidDiaphragm();
        public WindParameters_Base windLoadParams { get; set; }
        public ASCE7_Versions windVersion { get; set;
        }
        public BuildingData buildingData { get; set; } = null;

        /// <summary>
        /// The three calculators for this project
        /// </summary>
        public WindLoadCalculator_Base windLoadCalculator_MWFRS_Length { get; set; }
        public WindLoadCalculator_Base windLoadCalculator_MWFRS_Width { get; set; }
        public WindLoadCalculator_Base windLoadCalculator_CC { get; set; }


        //public AreaCalculator_Base roofAreaCalculator { get; set; }
        //public WindLoadCalculator_Base windLoadCalculator_RidgeIsParallelToBuildingLength { get; set; }
        //public WindLoadCalculator_Base windLoadCalculator_RidgeIsPerpToBuildingLength { get; set; }

        public SimpsonCatalog simpsonCatalog { get; set; } = new SimpsonCatalog();  // contains the Simposon catalog connector and holddown data

        private JsonDrawingSerializer _serializer = new JsonDrawingSerializer();

        private Dictionary<MenuTabs, string> menuTabNames = new Dictionary<MenuTabs, string>();

        //// data for the image overlay
        //string selectedImageFilePath = null;
        //double pixelScaleX = 1.0;  // the scale factor for pixels to real-world coords
        //double pixelScaleY = 1.0;  // the scale factor for pixels to real-world coords

        // grid stuff
        bool gridNeedsUpdate = true;
        bool imageNeedsUpdate = true;
        DrawingVisual gridVisual = null;
        DrawingVisual imageVisual = null;
        private RenderTargetBitmap gridBitmap;
        private RenderTargetBitmap imageBitmap;
        private BitmapImage cachedReferenceImageBitmap;
        private string cachedImagePath;
        private Rect cachedReferenceImageScreenRect;
        private bool hasCachedReferenceImageScreenRect = false;

        double majorGridSpacing = 5.0;  // Major grid lines in world units
        double minorGridSpacing = 1.0;  // Minor grid lines in world units

        public EventHandler OnUpdated;  // the event that signals that the drawing has been updated -- controls will listen for this at the time they are created.

        bool hideImage = false;
        bool hideGrid = false;
        bool hideShapes = false;

        private double defaultWallHeight = 9.0;

        private enum DrawMode { None, Line, Rectangle }
        private DrawMode currentMode = DrawMode.Line;
        private bool snapMode = false;
        private double snapThreshold = 50; // Pixels
        private bool debugMode = false;

        private Point? startPoint_world = null;  // the first click in world coordinates
        private Point? endPoint_world = null;    // the second click in world coordinates

        private double zoomFactorX = 1.0;
        private double zoomFactorY = 1.0;
        private double panOffsetX = -25.0;
        private double panOffsetY = -25.0;

        private Shape previewShape = null;

        private double worldWidth = 120;
        private double worldHeight = 120;

        private Point currentMouseScreenPosition = new Point(0, 0);

        public MainWindow()
        {
            InitializeComponent();
            LoadRecentFilesMenu();

            this.KeyDown += MainWindow_KeyDown;

            // Set focus and canvas background
            this.Focusable = true;
            this.Focus();

            // the function to run once the app has loaded.
            this.Loaded += (s, e) =>
            {
                var ctrol_bldg_input = new BuildingDataInputControl(buildingData);
                ctrol_bldg_input.BuildingDataInputComplete += BuildingDataInputControl_BuildingDataInputComplete;
                tabBuildingDataControlTabItem.Content = ctrol_bldg_input;

                // create the wind load input control
                var ctrol_wind_input = new WindLoadInputControl(buildingData, windVersion, windLoadParams);
                ctrol_wind_input.WindInputComplete += WindLoadInputControl_WindInputComplete;
                tabWindInputControlTabItem.Content = ctrol_wind_input;

                // create the wind load results controls
                WindLoadResultsControl_CC ccControl1;
                WindLoadResultsControl_MWFRS mwfrsControl1, mwfrsControl2;
                CreateAndAssignResultControls(out ccControl1, out mwfrsControl1, out mwfrsControl2);
                tabWindResultsTabItem_MWFRS_BldgLength.Content = mwfrsControl1;
                tabWindResultsTabItem_MWFRS_BldgWidth.Content = mwfrsControl1;
                tabWindResultsTabItem_CC.Content = ccControl1;

                ResetView(); // reset the view so that origin 0,0 is at lower left of the corner screen and the model is zoomed to fill the entire window
                LoadRecentFilesMenu();  // recent files menu
                CreateGridVisual();
                CreateLayers();

                // load the Simpson catalog
                simpsonCatalog = new SimpsonCatalog();

                UpdateShearWallUI();

                // Now clean up and remove the tabs for the results
                TabControlManager.RemoveAllTabs(MainTabControl);
                TabControlManager.ReAddTab(MainTabControl, "tabBuildingDataControlTabItem");
                MainTabControl.SelectedIndex = 0;
            };
        }

        public void UpdateShearWallUI()
        {
            // clear the tabs
            sp_DimPanel_Diaphragms.Children.Clear();
            sp_DimPanel_Walls.Children.Clear();
            sp_RigidCalcPanel.Children.Clear();
            sp_FlexibleCalcPanel.Children.Clear();

            if (Calculator != null)
            {
                Calculator.PerformCalculations();
                // update the calculator
                if (Calculator.IsValidForCalculation is true)
                {
                    CreateCalculationResultsControls_Rigid();
                    CreateCalculationResultsControls_Flexible();
                }

                // list the type of calculator
                tbCalculatorType.Text = Calculator.GetType().Name;

                string img_str = Calculator.selectedImageFilePath;
                if (Calculator.selectedImageFilePath == null || Calculator.selectedImageFilePath == String.Empty)
                {
                    img_str = "Image File: <No file selected>";
                }
                tbImageFileName.Text = img_str;

                // recreate the data controls
                CreateWallDataControls();
                CreateDiaphragmDataControls();

                // notify controls that we have updated
                OnUpdated?.Invoke(this, EventArgs.Empty); // signal that the window has been updated -- so that subcontrols can refresh

                // update the load info display
                LoadInfoTextBlock.Text = $"X: {Calculator.V_x} | Y: {Calculator.V_y}";
            }

            // UpdateShearWallUI the button appearances
            SetButtonModes();

            // redraw the scene
            Draw(ChangeType.Redraw);
        }

        /// <summary>
        /// Reset the view so that the full model scales to the drawing context area and the origin of the world coordinates is at the lower left corner
        /// </summary>
        private void ResetView()
        {
            // Set zoom factors based on the visible area and world dimensions
            zoomFactorX = dockpanel.ActualWidth / worldWidth;
            zoomFactorY = dockpanel.ActualHeight / worldHeight;

            // Set the scale factors so both are the same -- for square grids
            zoomFactorX = Math.Min(zoomFactorX, zoomFactorY);
            zoomFactorY = zoomFactorX; 

            // Compute current screen position of world (0,0)
            Point screenOrigin = WorldToScreen(new Point(0, 0), dockpanel);

            // Adjust pan offset so (0,0) appears in bottom-left corner (screen X=0, Y=height)
            panOffsetX += screenOrigin.X / zoomFactorX;
            panOffsetY += (dockpanel.ActualHeight - screenOrigin.Y) / zoomFactorY;
        }


        private Point GetSnappedPoint(Point worldPoint)
        {
            foreach (var wall in Calculator._wall_system._walls)
            {
                if (IsWithinSnapThreshold(worldPoint, wall.Value.Start)) return wall.Value.Start;
                if (IsWithinSnapThreshold(worldPoint, wall.Value.End)) return wall.Value.End;
            }

            foreach (var dia in Calculator._diaphragm_system._diaphragms)
            {
                if (IsWithinSnapThreshold(worldPoint, dia.Value.P1)) return dia.Value.P1;
                if (IsWithinSnapThreshold(worldPoint, dia.Value.P2)) return dia.Value.P2;
                if (IsWithinSnapThreshold(worldPoint, dia.Value.P3)) return dia.Value.P3;
                if (IsWithinSnapThreshold(worldPoint, dia.Value.P4)) return dia.Value.P4;
            }

            return worldPoint;
        }

        private bool IsWithinSnapThreshold(Point p1, Point p2)
        {
            double dx = p1.X - p2.X;
            double dy = p1.Y - p2.Y;
            return Math.Sqrt(dx * dx + dy * dy) <= snapThreshold * (worldWidth / m_layers.ActualWidth);
        }

        private Point WorldToScreen(Point p, FrameworkElement element)
        {
            // Convert world coordinates to screen coordinates
            double u = (p.X - panOffsetX) * zoomFactorX;
            double v = (element.ActualHeight - (p.Y - panOffsetY)) * zoomFactorY; // Invert Y for screen space

            return new Point(u, v);
        }

        private Point ScreenToWorld(Point p, FrameworkElement element)
        {
            // Convert screen coordinates to world coordinates
            double x = (p.X / zoomFactorX) + panOffsetX;
            double y = ((element.ActualHeight - (p.Y / zoomFactorY)) + panOffsetY);

            return new Point(x, y);
        }

        private Point GetConstrainedPoint(Point endPoint, Point startPoint)
        {
            double dx = Math.Abs(endPoint.X - startPoint.X);
            double dy = Math.Abs(endPoint.Y - startPoint.Y);

            if (dx > dy)
            {
                // Snap to horizontal
                return new Point(endPoint.X, startPoint.Y);
            }
            else
            {
                // Snap to vertical
                return new Point(startPoint.X, endPoint.Y);
            }
        }

        /// <summary>
        /// Helper function to determine if a point is within the bounds of a framework element (ActualWidth and ActualHeight)
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="element"></param>
        /// <returns></returns>
        private bool PointIsWithinBounds(Point p1, FrameworkElement element)
        {
            return (p1.X > 0 && p1.X < element.ActualWidth && p1.Y > 0 && p1.Y < element.ActualHeight);
        }

        /// <summary>
        /// Returns a point that is bounded by the framework element -- used for when a point is out of bounds 
        /// and needs to be shifted back to the elements boundary when drawing
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="element"></param>
        /// <returns></returns>
        private Point GetConstrainedScreenPoint(Point p1, FrameworkElement element)
        {
            Point temp = p1;
            if (temp.X < 0)
            {
                temp.X = 0;
            }
            if (temp.X > element.ActualWidth)
            {
                temp.X = element.ActualWidth;
            }
            if (temp.Y < 0)
            {
                temp.Y = 0;
            }
            if (temp.Y > element.ActualHeight)
            {
                temp.Y = element.ActualHeight;
            }
            return temp;
        }

        private void CreatePreviewShape()
        {
            if (currentMode == DrawMode.Line)
                previewShape = new Line { Stroke = Brushes.DarkGreen, StrokeThickness = 5 };
            else if (currentMode == DrawMode.Rectangle)
                previewShape = new Rectangle { Stroke = Brushes.DarkGreen, StrokeThickness = 5, Fill = Brushes.Transparent };
        }

        // Create the layers
        private void CreateLayers()
        {
            m_layers.AddLayer(0, DrawBackground, ChangeType.Resize);
            m_layers.AddLayer(1, DrawGridInformation);
            m_layers.AddLayer(2, DrawReferenceImage);
            m_layers.AddLayer(3, DrawBoundingBox);
            m_layers.AddLayer(4, DrawLoads);

            m_layers.AddLayer(41, DrawShapes);
            m_layers.AddLayer(52, DrawCursor);
            m_layers.AddLayer(51, DrawPreview);
            m_layers.AddLayer(43, DrawSnapMarkers);
            m_layers.AddLayer(50, DrawBracedWallLines);
            m_layers.AddLayer(80, DrawCOMandCOR);
            m_layers.AddLayer(100, DrawDebug);
        }

        private void FinalizeShape(Point worldPoint)
        {
            if (currentMode == DrawMode.Line)
            {
                // For the line to be horizontal or vertical only
                worldPoint = GetConstrainedPoint(worldPoint, startPoint_world.Value); // Ensure alignment

                // Create the line in world space and store it in the worldShapes list
                Point startPoint = startPoint_world.Value;
                Point endPoint = worldPoint;

                if (Calculator._wall_system == null)
                {
                    Calculator._wall_system = new WallSystem();
                }

                Calculator._wall_system.AddWall(new WallData(defaultWallHeight, startPoint.X, startPoint.Y, endPoint.X, endPoint.Y));
            }
            else if (currentMode == DrawMode.Rectangle)
            {
                if (Calculator._diaphragm_system == null)
                {
                        Calculator._diaphragm_system = new DiaphragmSystem();
                }

                Calculator._diaphragm_system.AddDiaphragm(new DiaphragmData_Rectangular(startPoint_world.Value, endPoint_world.Value));
            } else
            {
                throw new NotImplementedException("Error: FinalizeShape() received an invalid DrawMode variable.");
            }

            Calculator.PerformCalculations();  // perform the calculations with the new windLoadCalculator_RidgeIsParallelToBuildingLength

            // Clear the preview shape from the screen.
            previewShape = null;
            startPoint_world = null;
            endPoint_world = null;
        }

        public void PrintList<T>(List<T> lst)
        {
            if (lst == null)
                return;

            foreach (var item in lst)
            {
                Console.WriteLine(item.ToString());
            }
        }

        #region UI Control Related Events
        private void BuildingDataInputControl_BuildingDataInputComplete(object sender, BuildingDataInputControl.OnBuildingDataInputCompleteEventArgs e)
        {
            buildingData = e._bldg_data;
            buildingData.ValidateRidgeDirection();  // check that the ridge direction is valid

            // Add the tabWindInputControlTabItem
            TabControlManager.ReAddTab(MainTabControl, "tabWindInputControlTabItem");
            WindLoadInputControl temp = tabWindInputControlTabItem.Content as WindLoadInputControl;
            tabWindInputControlTabItem.Content = new WindLoadInputControl(buildingData, temp.Version, temp.Parameters);


            // reset the building data control to use saved values
            var ctrol_bldg_input = new BuildingDataInputControl(buildingData);
            ctrol_bldg_input.BuildingDataInputComplete += BuildingDataInputControl_BuildingDataInputComplete;
            tabBuildingDataControlTabItem.Content = ctrol_bldg_input;

            
            // get the canvas from the building input control
            var plan_canvas = (tabBuildingDataControlTabItem.Content as BuildingDataInputControl).cnvBuildingPlanCanvas;
            var bldG_length_elev_canvas = (tabBuildingDataControlTabItem.Content as BuildingDataInputControl).cnvBuildingLengthCanvas;
            var bldG_width_elev_canvas = (tabBuildingDataControlTabItem.Content as BuildingDataInputControl).cnvBuildingWidthCanvas;

            BuildingDrawer.DrawPlan(plan_canvas, buildingData);
            BuildingDrawer.DrawElevation_BuildingLength(bldG_length_elev_canvas, buildingData);
            BuildingDrawer.DrawElevation_BuildingWidth(bldG_width_elev_canvas, buildingData);

            // create the wind load input control
            var ctrol_wind_input = new WindLoadInputControl(buildingData, windVersion, windLoadParams);
            ctrol_wind_input.WindInputComplete += WindLoadInputControl_WindInputComplete;
            tabWindInputControlTabItem.Content = ctrol_wind_input;




            UpdateShearWallUI();
        }
        /// <summary>
        /// Event listener for when input of the wind loads as been completed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WindLoadInputControl_WindInputComplete(object sender, WindLoadInputControl.OnWindInputCompleteEventArgs e)
        {
            windVersion = e._version;
            windLoadParams = e._parameters;

            // create our calculators for each of the directions
            MakeCalculators();

            WindLoadResultsControl_CC ccControl1;
            WindLoadResultsControl_MWFRS mwfrsControl1, mwfrsControl2;
            CreateAndAssignResultControls(out ccControl1, out mwfrsControl1, out mwfrsControl2);

            DrawEffectiveAreas_CC_OnWindLoadParameterInputCanvas();
            DrawEffectiveAreas_MWFRS_Length_OnWindLoadParameterInputCanvas();
            DrawEffectiveAreas_MWFRS_Width_OnWindLoadParameterInputCanvas();

            var ctrol_wind_input = tabWindInputControlTabItem.Content as WindLoadInputControl;
            BuildingDrawer.DrawPlan(ctrol_wind_input.cnvBuildingPlan_CC, windLoadCalculator_CC.buildingData);
            BuildingDrawer.DrawPlan(ctrol_wind_input.cnvBuildingPlan_MWFRS_Length, windLoadCalculator_MWFRS_Length.buildingData);
            BuildingDrawer.DrawPlan(ctrol_wind_input.cnvBuildingplan_MWFRS_Width, windLoadCalculator_MWFRS_Width.buildingData);


            SetupMWFRSResultTab();
            

            // set up the component and cladding tabs
            if(windLoadCalculator_CC.Parameters.AnalysisType == WindLoadCalculationTypes.COMPONENT_AND_CLADDING)
            {
                SetupComponentAndCladdingFigures(ccControl1);
                DrawEffectiveAreas_OnCCResultCanvas(ccControl1);
                PopulateComponentAndCladdingDataGrids(ccControl1);
            }

            
            UpdateShearWallUI();
        }

        /// <summary>
        /// Creates the two wind load calculators...one for where the wind is acting on the BuildingWidth wall 
        /// and the other for where the wind is acting on the BuildingLength wall
        /// </summary>
        private void MakeCalculators()
        {
            var bldg_data1 = buildingData;
            var bldg_data2 = bldg_data1.Clone();
            bldg_data2.FlipBuilding();

            // Create MWFRS calculators
            var mwfrs_calc_building_length = CreateAndComputeCalculator(bldg_data1, WindLoadCalculationTypes.MWFRS);
            var mwfrs_calc_building_width = CreateAndComputeCalculator(bldg_data2, WindLoadCalculationTypes.MWFRS);

            // Create CC calculator using building data #1
            var cc_calc_building_length = CreateAndComputeCalculator(bldg_data1, WindLoadCalculationTypes.COMPONENT_AND_CLADDING);

            // Assign based on ridge direction
            if (buildingData.RidgeDirection == RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH)
            {
                windLoadCalculator_MWFRS_Length = mwfrs_calc_building_length;
                windLoadCalculator_MWFRS_Width = mwfrs_calc_building_width;
            }
            else
            {
                windLoadCalculator_MWFRS_Length = mwfrs_calc_building_width;
                windLoadCalculator_MWFRS_Width = mwfrs_calc_building_length;
            }

            windLoadCalculator_CC = cc_calc_building_length;
        }

        private WindLoadCalculator_Base CreateAndComputeCalculator(BuildingData building, WindLoadCalculationTypes type)
        {
            var parameters = windLoadParams.Clone();
            parameters.AnalysisType = type;

            var calculator = WindLoadCalculatorFactory.Create(windVersion, type, parameters, building);
            calculator.CreateAreaCalculators();
            calculator.CalculatePressures();
            return calculator;
        }



        /// <summary>
        /// Assigns the two calculators to the appropriate tabs.
        /// Since CC is the same for both, only the first calculator is used on that tab.
        /// </summary>
        /// <param name="ccControl1"></param>
        /// <param name="mwfrsControl1"></param>
        /// <param name="mwfrsControl2"></param>
        private void CreateAndAssignResultControls(out WindLoadResultsControl_CC ccControl1, out WindLoadResultsControl_MWFRS mwfrsControl1, out WindLoadResultsControl_MWFRS mwfrsControl2)
        {
            mwfrsControl1 = new WindLoadResultsControl_MWFRS(windLoadCalculator_MWFRS_Length);
            tabWindResultsTabItem_MWFRS_BldgLength.Content = mwfrsControl1;

            mwfrsControl2 = new WindLoadResultsControl_MWFRS(windLoadCalculator_MWFRS_Width);
            tabWindResultsTabItem_MWFRS_BldgWidth.Content = mwfrsControl2;

            ccControl1 = new WindLoadResultsControl_CC(windLoadCalculator_CC);
            tabWindResultsTabItem_CC.Content = ccControl1;

            TabControlManager.ReAddTab(MainTabControl, "tabWindResultsTabItem_CC");
            TabControlManager.ReAddTab(MainTabControl, "tabWindResultsTabItem_MWFRS_BldgLength");
            TabControlManager.ReAddTab(MainTabControl, "tabWindResultsTabItem_MWFRS_BldgWidth");
        }

        /// <summary>
        /// Draws the effective wind areas on the wind load parameter input canvas
        /// </summary>
        private void DrawEffectiveAreas_CC_OnWindLoadParameterInputCanvas()
        {
            if (tabWindInputControlTabItem.Content is WindLoadInputControl inputControl)
            {
                var canvas = inputControl.cnvEffectiveRoofAreas_CC;

                if (canvas == null) return;

                canvas.Children.Clear();
                double scale = Math.Min(canvas.ActualWidth / buildingData.BuildingWidth, canvas.ActualHeight / buildingData.BuildingLength);
                
                foreach (var area in windLoadCalculator_CC.RoofAreaCalculator.effWindAreas)
                {
                    WindLoadInputControl.DrawEffectiveWindArea(canvas, area.Value, scale, GetColorForRegion(area.Value.Label_Short));
                }
            }
        }

        /// <summary>
        /// Draws the effective wind areas on the wind load parameter input canvas
        /// </summary>
        private void DrawEffectiveAreas_MWFRS_Length_OnWindLoadParameterInputCanvas()
        {
            if (tabWindInputControlTabItem.Content is WindLoadInputControl inputControl)
            {
                var canvas = inputControl.cnvEffectiveRoofAreas_MWFRS_Length;

                if (canvas == null) return;

                canvas.Children.Clear();
                double scale = Math.Min(canvas.ActualWidth / buildingData.BuildingWidth, canvas.ActualHeight / buildingData.BuildingLength);

                foreach (var area in windLoadCalculator_MWFRS_Length.RoofAreaCalculator.effWindAreas)
                {
                    WindLoadInputControl.DrawEffectiveWindArea(canvas, area.Value, scale, GetColorForRegion(area.Value.Label_Short));
                }
            }
        }

        /// <summary>
        /// Draws the effective wind areas on the wind load parameter input canvas
        /// </summary>
        private void DrawEffectiveAreas_MWFRS_Width_OnWindLoadParameterInputCanvas()
        {
            if (tabWindInputControlTabItem.Content is WindLoadInputControl inputControl)
            {
                var canvas = inputControl.cnvEffectiveRoofAreas_MWFRS_Width;

                if (canvas == null) return;

                canvas.Children.Clear();
                double scale = Math.Min(canvas.ActualWidth / buildingData.BuildingWidth, canvas.ActualHeight / buildingData.BuildingLength);

                foreach (var area in windLoadCalculator_MWFRS_Width.RoofAreaCalculator.effWindAreas)
                {
                    WindLoadInputControl.DrawEffectiveWindArea(canvas, area.Value, scale, GetColorForRegion(area.Value.Label_Short));
                }
            }
        }

        private void PopulateComponentAndCladdingDataGrids(WindLoadResultsControl_CC ccControl)
        {
            Chapter27and30_GCpCurveBase figureCC_Roof = null ;
            Chapter27and30_GCpCurveBase figureCC_Wall;
            if (windLoadCalculator_CC.ASCEVersion == ASCE7_Versions.ASCE_VER_7_16)
            {
                figureCC_Roof = ((WindLoadCalculator_CC_ASCE7_16)windLoadCalculator_CC).extGCpCurve_Roof;
                figureCC_Wall = ((WindLoadCalculator_CC_ASCE7_16)windLoadCalculator_CC).extGCpCurve_Wall;
            } else if (windLoadCalculator_CC.ASCEVersion == ASCE7_Versions.ASCE_VER_7_22)
            {
                figureCC_Roof = ((WindLoadCalculator_CC_ASCE7_22)windLoadCalculator_CC).extGCpCurve_Roof;
                figureCC_Wall = ((WindLoadCalculator_CC_ASCE7_22)windLoadCalculator_CC).extGCpCurve_Wall;
            } else
            {
                throw new Exception("ERROR:  In PopulateComponentAndCladdingDataGrids() -- Version " + windLoadCalculator_CC.ASCEVersion.ToString() + " not found.");
            }

            if (ccControl == null || figureCC_Roof == null || figureCC_Wall == null) return;

            CreateCC_DataGrid_Roof(figureCC_Roof, ccControl.RoofResultsDataGrid);

            // For the BuildingLength wall
            CreateCC_DataGrid_Walls(figureCC_Wall, ccControl.WallsResultsDataGrid_SideWall,
                windLoadCalculator_CC.WallAreaCalculator_BldgLength.effWindAreas, "building_length");

            // For the BuildingWidth wall
            CreateCC_DataGrid_Walls(figureCC_Wall, ccControl.WallsResultsDataGrid_EndWall,
                windLoadCalculator_CC.WallAreaCalculator_BldgWidth.effWindAreas, "building_width");
        }

        private void DrawEffectiveAreas_OnCCResultCanvas(WindLoadResultsControl_CC ccControl1)
        {
            Canvas resultCanvasCC = ccControl1.cnvWindLoadResultCanvasCC; ;

            if (resultCanvasCC == null) return;

            resultCanvasCC.Children.Clear();
            double scale = Math.Min(resultCanvasCC.Width / buildingData.BuildingWidth, resultCanvasCC.Height / buildingData.BuildingLength);

            foreach (var area in windLoadCalculator_CC.RoofAreaCalculator.effWindAreas)
            {
                WindLoadInputControl.DrawEffectiveWindArea(resultCanvasCC, area.Value, scale, GetColorForRegion(area.Value.Label_Short));
            }
        }

        private void SetupMWFRSResultTab()
        {
            //TabControlManager.RemoveTab(MainTabControl, tabWindResultsTabItem_CC);
        }

        private void SetupComponentAndCladdingFigures(WindLoadResultsControl_CC ccControl)
        {
            if (ccControl == null) return;

            //TabControlManager.RemoveTab(MainTabControl, tabWindResultsTabItem_MWFRS_BldgLength);
            //TabControlManager.RemoveTab(MainTabControl, tabWindResultsTabItem_MWFRS_BldgWidth);

            var roofCanvas = ccControl.cnvFigure30_3;
            var roofTitle = ccControl.txtFigureTitle_Roof;
            var roofCriteria = ccControl.txtFigureCriteria_Roof;

            var wallCanvas = ccControl.cnvFigure30_1;
            var wallTitle = ccControl.txtFigureTitle_Walls;
            var wallCriteria = ccControl.txtFigureCriteria_Walls;


            Chapter27and30_GCpCurveBase figureCC_Roof = null;
            Chapter27and30_GCpCurveBase figureCC_Wall;
            if (windLoadCalculator_CC.ASCEVersion == ASCE7_Versions.ASCE_VER_7_16)
            {
                figureCC_Roof = ((WindLoadCalculator_CC_ASCE7_16)windLoadCalculator_CC).extGCpCurve_Roof;
                figureCC_Wall = ((WindLoadCalculator_CC_ASCE7_16)windLoadCalculator_CC).extGCpCurve_Wall;
            }
            else if (windLoadCalculator_CC.ASCEVersion == ASCE7_Versions.ASCE_VER_7_22)
            {
                figureCC_Roof = ((WindLoadCalculator_CC_ASCE7_22)windLoadCalculator_CC).extGCpCurve_Roof;
                figureCC_Wall = ((WindLoadCalculator_CC_ASCE7_22)windLoadCalculator_CC).extGCpCurve_Wall;
            }
            else
            {
                throw new Exception("ERROR:  In PopulateComponentAndCladdingDataGrids() -- Version " + windLoadCalculator_CC.ASCEVersion.ToString() + " not found.");
            }

            roofTitle.Text = figureCC_Roof?.ChartTitle;
            roofCriteria.Text = figureCC_Roof?.ChartCriteria;
            wallTitle.Text = figureCC_Wall?.ChartTitle;
            wallCriteria.Text = figureCC_Wall?.ChartCriteria;

            FigureDrawer.DrawCurvesOnCanvas(roofCanvas, figureCC_Roof);
            FigureDrawer.DrawCurvesOnCanvas(wallCanvas, figureCC_Wall);
        }

        private void CreateCC_DataGrid_Roof(Chapter27and30_GCpCurveBase figureCC, DataGrid data_grid)
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
                Header = "GCp+",
                Binding = new Binding("GCp_pos") { StringFormat = "0.00" }
            };
            data_grid.Columns.Add(col_gcp_pos);

            var col_gcp_neg = new DataGridTextColumn
            {
                Header = "GCp-",
                Binding = new Binding("GCp_neg") { StringFormat = "0.00" }
            };
            data_grid.Columns.Add(col_gcp_neg);

            var col_pos_press = new DataGridTextColumn
            {
                Header = "+ Press\n(psf)",
                Binding = new Binding("PosPress") { StringFormat = "0.0" }
            };
            data_grid.Columns.Add(col_pos_press);

            var col_neg_press = new DataGridTextColumn
            {
                Header = "- Press\n(psf)",
                Binding = new Binding("NegPress") { StringFormat = "0.0" }
            };
            data_grid.Columns.Add(col_neg_press);

            //var col_overhang_press = new DataGridTextColumn
            //{
            //    Header = "Overhang Press\n(psf)",
            //    Binding = new Binding("OverhangPress") { StringFormat = "0.0" }
            //};
            //data_grid.Columns.Add(col_overhang_press);

            var windLoadResults = new List<CC_WindLoadResults>();
            foreach (KeyValuePair<int, EffectiveWindArea> area in windLoadCalculator_CC.RoofAreaCalculator.effWindAreas)
            {

                CC_WindLoadResults data = new CC_WindLoadResults();
                data.Name = area.Value.Label_Short;
                data.Region = area.Value.Label_Short;
                data.Area = area.Value.Area;
                data.qh = windLoadCalculator_CC.CalculateDynamicWindPressure(buildingData.MeanRoofHeight);

                double pressure_pos;
                
                // positive max net pressure
                if (windLoadCalculator_CC.TryGetPressureNet_Pos_Roof(area.Value, out pressure_pos))
                {
                    data.GCp_pos = figureCC.RoofCurves_Pos[area.Value.Label_Full].Evaluate(area.Value.Area);
                    data.PosPress = pressure_pos;
                }

                // negative max net pressure
                if (windLoadCalculator_CC.TryGetPressureNet_Neg_Roof(area.Value, out pressure_pos))
                {
                    data.GCp_neg = figureCC.RoofCurves_Neg[area.Value.Label_Full].Evaluate(area.Value.Area);
                    data.NegPress = pressure_pos;
                }

                // negative max net pressure
                if (windLoadCalculator_CC.TryGetPressureNet_Overhang_Roof(area.Value, out pressure_pos))
                {
                    data.OverhangPress = pressure_pos;
                }

                //if (figureCC.RoofCurves_Pos != null && figureCC.RoofCurves_Pos.ContainsKey(area.Value.Label_Full))
                //{
                //    data.GCp_pos = figureCC.RoofCurves_Pos[area.Value.Label_Full].Evaluate(area.Value.Area);
                //    data.PosPress = data.qh * data.GCp_pos;
                //}

                //if (figureCC.RoofCurves_Neg != null && figureCC.RoofCurves_Neg.ContainsKey(area.Value.Label_Full))
                //{
                //    data.GCp_neg = figureCC.RoofCurves_Neg[area.Value.Label_Full].Evaluate(area.Value.Area);
                //    data.NegPress = data.qh * data.GCp_neg;
                //}

                //if (figureCC.OverhangCurves != null && figureCC.OverhangCurves.ContainsKey(area.Value.Label_Full))
                //{
                //    data.OverhangPress = figureCC.OverhangCurves[area.Value.Label_Full].Evaluate(area.Value.Area);
                //}

                windLoadResults.Add(data);
            }

            data_grid.ItemsSource = windLoadResults;
        }


        private void CreateCC_DataGrid_Walls(Chapter27and30_GCpCurveBase figureCC, DataGrid data_grid, Dictionary<int, EffectiveWindArea> areas, string wall_type)
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
                Header = "GCp+",
                Binding = new Binding("GCp_pos") { StringFormat = "0.00" }
            };
            data_grid.Columns.Add(col_gcp_pos);

            var col_gcp_neg = new DataGridTextColumn
            {
                Header = "GCp-",
                Binding = new Binding("GCp_neg") { StringFormat = "0.00" }
            };
            data_grid.Columns.Add(col_gcp_neg);

            var col_pos_press = new DataGridTextColumn
            {
                Header = "+ Press\n(psf)",
                Binding = new Binding("PosPress") { StringFormat = "0.0" }
            };
            data_grid.Columns.Add(col_pos_press);

            var col_neg_press = new DataGridTextColumn
            {
                Header = "- Press\n(psf)",
                Binding = new Binding("NegPress") { StringFormat = "0.0" }
            };
            data_grid.Columns.Add(col_neg_press);

            var windLoadResults = new List<CC_WindLoadResults>();
            foreach (var area in areas)
            {
                CC_WindLoadResults data = new CC_WindLoadResults();
                data.Name = area.Value.Label_Short;
                data.Region = area.Value.Label_Short;
                data.Area = area.Value.Area;
                data.qh = windLoadCalculator_CC.CalculateDynamicWindPressure(buildingData.MeanRoofHeight);

                double pressure_pos;
                double pressure_neg;

                if (wall_type == "building_length")
                {
                    // positive max net pressure
                    if (windLoadCalculator_CC.TryGetPressureNet_Pos_BuildingLengthWall(area.Value, out pressure_pos))
                    {
                        data.GCp_pos = figureCC.WallCurves_Pos[area.Value.Label_Full].Evaluate(area.Value.Area);
                        data.PosPress = pressure_pos;
                    }

                    // negative max net pressure
                    if (windLoadCalculator_CC.TryGetPressureNet_Neg_BuildingLengthWall(area.Value, out pressure_neg))
                    {
                        data.GCp_neg = figureCC.WallCurves_Neg[area.Value.Label_Full].Evaluate(area.Value.Area);
                        data.NegPress = pressure_neg;
                    }
                }
                else if (wall_type == "building_width")
                {
                    // positive max net pressure
                    if (windLoadCalculator_CC.TryGetPressureNet_Pos_BuildingWidthWall(area.Value, out pressure_pos))
                    {
                        data.GCp_pos = figureCC.WallCurves_Pos[area.Value.Label_Full].Evaluate(area.Value.Area);
                        data.PosPress = pressure_pos;
                    }

                    // negative max net pressure
                    if (windLoadCalculator_CC.TryGetPressureNet_Neg_BuildingWidthWall(area.Value, out pressure_neg))
                    {
                        data.GCp_neg = figureCC.WallCurves_Neg[area.Value.Label_Full].Evaluate(area.Value.Area);
                        data.NegPress = pressure_neg;
                    }
                }
                else
                {
                    throw new Exception("ERROR:  In CreateCC_DataGrid_Walls(): Unknown wall type " + wall_type);
                }
           
                //if (figureCC.WallCurves_Pos != null && figureCC.WallCurves_Pos.ContainsKey(area.Value.Label_Full))
                //{
                //    data.GCp_pos = figureCC.WallCurves_Pos[area.Value.Label_Full].Evaluate(area.Value.Area);
                //    data.PosPress = data.qh * data.GCp_pos;
                //}

                //if (figureCC.WallCurves_Neg != null && figureCC.WallCurves_Neg.ContainsKey(area.Value.Label_Full))
                //{
                //    data.GCp_neg = figureCC.WallCurves_Neg[area.Value.Label_Full].Evaluate(area.Value.Area);
                //    data.NegPress = data.qh * data.GCp_neg;
                //}

                windLoadResults.Add(data);
            }

            data_grid.ItemsSource = windLoadResults;
        }

        public class CC_WindLoadResults
        {
            public string Name { get; set; }
            public double Area { get; set; }
            public string Region { get; set; } // used to store the regions name so we can draw the rectangle from it
            public double qh { get; set; }
            public double GCp_pos { get; set; }
            public double GCp_neg { get; set; }

            public double PosPress { get; set; }
            public double NegPress { get; set; }
            public double OverhangPress { get; set; }

            public Brush RectColor => GetColorForRegion(Region);
        }

        private void CreateWallDataControls()
        {
            if (Calculator == null || Calculator._wall_system == null)
            {
                return;
            }

            WallSystemControl sysControl = new WallSystemControl(this, Calculator._wall_system);
            sp_DimPanel_Walls.Children.Add(sysControl);
            sysControl.OnWallSubControlDeleted += WallDeleted;
        }
        private void CreateDiaphragmDataControls()
        {
            if (Calculator == null || Calculator._diaphragm_system == null)
            {
                return;
            }

            DiaphragmSystemControl sysControl = new DiaphragmSystemControl(this, Calculator._diaphragm_system);
            sp_DimPanel_Diaphragms.Children.Add(sysControl);
            sysControl.OnDiaphragmSubControlDeleted += DiaphragmDeleted;
        }

        private void WallDeleted(object sender, EventArgs e)
        {

            if (Calculator == null || Calculator._wall_system == null)
            {
                return;
            }

            DeleteWallEventArgs args = e as DeleteWallEventArgs;

            foreach (var wall in Calculator._wall_system._walls)
            {
                if (wall.Key == args.Id)
                {
                    Calculator._wall_system._walls.Remove(wall.Key);
                    UpdateShearWallUI();
                    return;
                }
            }
        }

        private void DiaphragmDeleted(object sender, EventArgs e)
        {
            if (Calculator == null || Calculator._diaphragm_system == null)
            {
                return;
            }

            DeleteDiaphragmEventArgs args = e as DeleteDiaphragmEventArgs;

            foreach (var dia in Calculator._diaphragm_system._diaphragms)
            {
                if (dia.Key == args.Id)
                {
                    Calculator._diaphragm_system._diaphragms.Remove(dia.Key);
                    UpdateShearWallUI();
                    return;
                }
            }
        }

        private void CreateCalculationResultsControls_Rigid()
        {
            if (Calculator == null || Calculator._wall_system == null)
            {
                foreach (var wall in Calculator._wall_system._walls)
                {
                    int id = wall.Key;
                    var rigidity = wall.Value.WallRigidity;

                    double xbar = double.NaN;
                    double ybar = double.NaN;

                    if (Calculator._wall_system.X_bar_walls.ContainsKey(id) is true)
                    {
                        xbar = Calculator._wall_system.X_bar_walls[id];
                    }

                    if (Calculator._wall_system.Y_bar_walls.ContainsKey(id) is true)
                    {
                        ybar = Calculator._wall_system.Y_bar_walls[id];
                    }

                    if (Calculator is ShearWallCalculator_RigidDiaphragm)
                    {
                        ShearWallCalculator_RigidDiaphragm calc = Calculator as ShearWallCalculator_RigidDiaphragm;
                        // Must check validity of numbers since some walls may be in X direction and others in Y direction
                        double vi_x = double.NaN;
                        if (calc.DirectShear_X.ContainsKey(id) is true)
                        {
                            vi_x = calc.DirectShear_X[id];
                        }

                        var vi_y = double.NaN;
                        if (calc.DirectShear_Y.ContainsKey(id) is true)
                        {
                            vi_y = calc.DirectShear_Y[id];
                        }

                        var v_ecc = double.NaN;
                        if (calc.EccentricShear.ContainsKey(id) is true)
                        {
                            v_ecc = calc.EccentricShear[id];
                        }

                        var v_tot = double.NaN;
                        if (calc.TotalWallShear.ContainsKey(id) is true)
                        {
                            v_tot = calc.TotalWallShear[id];
                        }

                        ShearWallResultsControl_Rigid control = new ShearWallResultsControl_Rigid(id, rigidity, xbar, ybar, vi_x, vi_y, v_ecc, v_tot);
                        sp_RigidCalcPanel.Children.Add(control);
                    }
                }
            }
        }

        private void CreateCalculationResultsControls_Flexible()
        {
            if (Calculator == null || Calculator._wall_system == null)
            {

                foreach (var wall in Calculator._wall_system._walls)
                {
                    int id = wall.Key;
                    var rigidity = wall.Value.WallRigidity;

                    double xbar = double.NaN;
                    double ybar = double.NaN;

                    if (Calculator._wall_system.X_bar_walls.ContainsKey(id) is true)
                    {
                        xbar = Calculator._wall_system.X_bar_walls[id];
                    }

                    if (Calculator._wall_system.Y_bar_walls.ContainsKey(id) is true)
                    {
                        ybar = Calculator._wall_system.Y_bar_walls[id];
                    }
                    if (Calculator is ShearWallCalculator_FlexibleDiaphragm)
                    {
                        ShearWallCalculator_FlexibleDiaphragm calc = Calculator as ShearWallCalculator_FlexibleDiaphragm;
                        // Must check validity of numbers since some walls may be in X direction and others in Y direction
                        double vi_x = double.NaN;
                        if (calc.DirectShear_X.ContainsKey(id) is true)
                        {
                            vi_x = calc.DirectShear_X[id];
                        }

                        var vi_y = double.NaN;
                        if (calc.DirectShear_Y.ContainsKey(id) is true)
                        {
                            vi_y = calc.DirectShear_Y[id];
                        }

                        var v_tot = double.NaN;
                        if (calc.TotalWallShear.ContainsKey(id) is true)
                        {
                            v_tot = calc.TotalWallShear[id];
                        }

                        ShearWallResultsControl_Flexible control = new ShearWallResultsControl_Flexible(id, vi_x, vi_y);
                        sp_FlexibleCalcPanel.Children.Add(control);
                    }
                }
            }
        }

        #endregion

        #region Drawing functions

        private void DrawBackground(DrawingContext ctx)
        {
            var pen = new Pen(Brushes.Black, 1);
            var rect = new Rect(0, 0, m_layers.ActualWidth, m_layers.ActualHeight);
            ctx.DrawRoundedRectangle(Brushes.White, pen, rect, 0, 0);
        }

        /// <summary>
        /// Creates the grid in screen coordinates.  Called by CreateGridVisual which stores it in a bitmap.
        /// </summary>
        /// <param name="ctx"></param>
        private void ConstructVisualGrid(DrawingContext ctx)
        {
            double major_gridline_thickness = 0.5;
            double minor_gridline_thickness = 0.25;

            if (hideGrid is true)
            {
                return;
            }

            // thickness of the minor gridlines
            Pen pen;

            double layerWidth = dockpanel.ActualWidth;
            double layerHeight = dockpanel.ActualHeight;

            // Draw vertical grid lines (major and minor) in world coordinates
            for (double x = 0; x <= layerWidth; x += minorGridSpacing)
            {
                Point p1 = WorldToScreen(new Point(x, 0), m_layers);
                Point p2 = WorldToScreen(new Point(x, layerHeight), m_layers);

                if (p1.X < 0 || p1.X > layerWidth || p2.X < 0 || p2.X > layerWidth)
                {
                    continue;  // if we are outside of the view box, no need to draw the grid
                }

                // If its a major grid line, make it thicker
                if (x % majorGridSpacing == 0)
                {
                    pen = new Pen(Brushes.Black, 1.0);
                    pen.Thickness = major_gridline_thickness;
                    pen.DashStyle = new DashStyle(new double[] { 5, 5 }, 0);
                }
                else
                {
                    pen = new Pen(Brushes.Black, 0.3);
                    pen.Thickness = minor_gridline_thickness;
                    pen.DashStyle = new DashStyle(new double[] { 5, 5 }, 0);

                }


                // Check if line is outside of viewbox
                if (p1.X < 0 || p1.X > layerWidth || p2.X < 0 || p2.X > layerWidth)
                {
                    continue;
                }

                // check if p1 point is outside of viewbox
                if (p1.Y < 0)
                {
                    p1.Y = 0;
                }
                if (p1.Y > layerHeight)
                {
                    p1.Y = layerHeight;
                }
                if (p2.Y < 0)
                {
                    p2.Y = 0;
                }
                if (p2.Y > layerHeight)
                {
                    p2.Y = layerHeight;
                }

                ctx.DrawLine(pen, p1, p2);

                if (x % (2 * majorGridSpacing) == 0)
                {
                    // Add a marker at every two major gridlines
                    // display the com as a text
                    FormattedText idLabel = new FormattedText(
                        $"{x}",
                        CultureInfo.GetCultureInfo("en-us"),
                        FlowDirection.LeftToRight,
                        new Typeface("Consolas"),
                        9,
                        Brushes.Gray,
                        VisualTreeHelper.GetDpi(this).PixelsPerDip);

                    
                    ctx.DrawText(idLabel, new Point(p1.X-5, p1.Y));
                }
            }

            // Draw horizontal grid lines (major and minor)
            for (double y = 0; y <= layerHeight; y += minorGridSpacing)
            {
                Point p1 = WorldToScreen(new Point(0, y), m_layers);
                Point p2 = WorldToScreen(new Point(layerWidth, y), m_layers);

                if (p1.Y < 0 || p1.Y > layerHeight || p2.Y < 0 || p2.Y > layerHeight)
                {
                    continue;  // if we are outside of the view box, no need to draw the grid
                }

                // If its a major grid line, make it thicker
                if (y % majorGridSpacing == 0)
                {
                    pen = new Pen(Brushes.Black, 1.0);
                    pen.Thickness = major_gridline_thickness;
                    pen.DashStyle = new DashStyle(new double[] { 5, 5 }, 0);
                }
                else
                {
                    pen = new Pen(Brushes.Black, 0.3);
                    pen.Thickness = minor_gridline_thickness;
                    pen.DashStyle = new DashStyle(new double[] { 5, 5 }, 0);

                }


                // Check if line is outside of viewbox
                if (p1.Y < 0 || p1.Y > layerHeight || p2.Y < 0 || p2.Y > layerHeight)
                {
                    continue;
                }

                // check if p1 point is outside of viewbox
                if (p1.X < 0)
                {
                    p1.X = 0;
                }
                if (p1.X > layerWidth)
                {
                    p1.X = layerWidth;
                }
                if (p2.X < 0)
                {
                    p2.X = 0;
                }
                if (p2.X > layerWidth)
                {
                    p2.X = layerWidth;
                }

                ctx.DrawLine(pen, p1, p2);

                if (y % (2 * majorGridSpacing) == 0)
                {
                    // Add a marker at every two major gridlines
                    // display the com as a text
                    FormattedText idLabel = new FormattedText(
                        $"{y}",
                        CultureInfo.GetCultureInfo("en-us"),
                        FlowDirection.LeftToRight,
                        new Typeface("Consolas"),
                        9,
                        Brushes.Gray,
                        VisualTreeHelper.GetDpi(this).PixelsPerDip);

                    ctx.DrawText(idLabel, new Point(p1.X-12, p1.Y-5));
                }
            }

            // Draw an origin marker
            DrawMarkers(ctx, WorldToScreen(new Point(0, 0), m_layers), 5, new Pen(Brushes.Black, 1), Brushes.DarkRed);
        }

        private void DrawMarkers(DrawingContext ctx, Point p, double dia, Pen pen, Brush fill)
        {
            ctx.DrawEllipse(fill, pen, p, dia, dia);

            return;
        }

        private void DrawCursor(DrawingContext ctx)
        {
            Point cross_pt = currentMouseScreenPosition;

            if ((snapMode is true))
            {
                // draw markers and coordinate data at end points
                ctx.DrawEllipse(Brushes.DarkRed, new Pen(Brushes.Black, 1), currentMouseScreenPosition, 5, 5);

                ctx.DrawLine(new Pen(Brushes.Red, 1), cross_pt, new Point(0, cross_pt.Y));
                ctx.DrawLine(new Pen(Brushes.Red, 1), cross_pt, new Point(dockpanel.Width, cross_pt.Y));
                ctx.DrawLine(new Pen(Brushes.Red, 1), cross_pt, new Point(cross_pt.X, 0));
                ctx.DrawLine(new Pen(Brushes.Red, 1), cross_pt, new Point(cross_pt.X, dockpanel.Height));
            }
            else
            {
                ctx.DrawLine(new Pen(Brushes.Black, 1), cross_pt, new Point(0, cross_pt.Y));
                ctx.DrawLine(new Pen(Brushes.Black, 1), cross_pt, new Point(dockpanel.Width, cross_pt.Y));
                ctx.DrawLine(new Pen(Brushes.Black, 1), cross_pt, new Point(cross_pt.X, 0));
                ctx.DrawLine(new Pen(Brushes.Black, 1), cross_pt, new Point(cross_pt.X, dockpanel.Height));
            }
        }

        private void DrawLoads(DrawingContext ctx)
        {

        }

        private void DrawBoundingBox(DrawingContext ctx)
        {
            if (Calculator is null) return;

            Rect rect = Calculator.BoundingBoxWorld;

            var p1 = WorldToScreen(rect.BottomLeft, m_layers);
            var p2 = WorldToScreen(rect.BottomRight, m_layers);
            var p3 = WorldToScreen(rect.TopRight, m_layers);
            var p4 = WorldToScreen(rect.TopLeft, m_layers);

            ctx.DrawLine(new Pen(Brushes.Green, 1), p1, p2);
            ctx.DrawLine(new Pen(Brushes.Green, 1), p2, p3);
            ctx.DrawLine(new Pen(Brushes.Green, 1), p3, p4);
            ctx.DrawLine(new Pen(Brushes.Green, 1), p4, p1);
        }

        /// <summary>
        /// Draws the preview shapes which constructing walls or diaphragms
        /// </summary>
        /// <param name="ctx"></param>
        private void DrawPreview(DrawingContext ctx)
        {
            // Do we have a preview shape?
            if (previewShape == null)
            {
                return;
            }

            Point preview_endPoint_world;
            if (endPoint_world == null)  // true if second point hasnt been selected
            {
                preview_endPoint_world = ScreenToWorld(currentMouseScreenPosition, m_layers);
            }
            else
            {
                preview_endPoint_world = endPoint_world.Value;
            }

            if (previewShape is Line line)
            {
                // For the line to be horizontal or vertical only
                preview_endPoint_world = GetConstrainedPoint(preview_endPoint_world, startPoint_world.Value); // Ensure alignment

                SolidColorBrush lineStrokeBrush = new SolidColorBrush(Colors.Green);
                lineStrokeBrush.Opacity = 0.5;
                Pen pen = new Pen(lineStrokeBrush, 4);
                Point p1 = WorldToScreen(startPoint_world.Value, m_layers);
                Point p2 = WorldToScreen(preview_endPoint_world, m_layers);
                ctx.DrawLine(pen, p1, p2);
            }
            else if (previewShape is Rectangle rect)
            {
                SolidColorBrush rectFillBrush = new SolidColorBrush(Colors.Green);
                rectFillBrush.Opacity = 0.5;
                Pen pen = new Pen(rectFillBrush, 4);
                Point p1 = WorldToScreen(startPoint_world.Value, m_layers);
                Point p2 = WorldToScreen(preview_endPoint_world, m_layers);

                // find lower left corner point of rectangle bounded by startPoint_world and endPoint_world
                Point insertPoint_screen = new Point(Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y));

                double width = Math.Abs(p2.X - p1.X);
                double height = Math.Abs(p2.Y - p1.Y);
                ctx.DrawRectangle(rectFillBrush, pen, new Rect(insertPoint_screen.X, insertPoint_screen.Y, width, height));
            }
        }

        /// <summary>
        /// The primary routine to draw all the world shapes
        /// </summary>
        private void DrawShapes(DrawingContext ctx)
        {
            if(hideShapes is true)
            {
                return;
            }
            double center_pt_dia = 5;

            if ((Calculator != null) && (Calculator._wall_system != null))
            {
                // Redraw all the shapes in world coordinates
                foreach (var wall in Calculator._wall_system._walls)
                {
                    FormattedText idLabel = null;

                    Point p1_world = wall.Value.Start;
                    Point p2_world = wall.Value.End;
                    Point p1_screen = GetConstrainedScreenPoint(WorldToScreen(p1_world, m_layers), m_layers);
                    Point p2_screen = GetConstrainedScreenPoint(WorldToScreen(p2_world, m_layers), m_layers);

                    // If the points are the same, then the object was out of bounds and doesn't need to be drawn.
                    if (p1_screen == p2_screen)
                        continue;

                    // Draw the line object
                    SolidColorBrush lineStrokeBrush = new SolidColorBrush(Colors.Blue);
                    Pen pen = new Pen(lineStrokeBrush, 4);
                    ctx.DrawLine(pen, p1_screen, p2_screen);

                    Point center_world = new Point((wall.Value.Start.X + wall.Value.End.X) / 2, (wall.Value.Start.Y + wall.Value.End.Y) / 2);
                    Point center_screen = WorldToScreen(center_world, m_layers);

                    // draw the center point and label
                    if (PointIsWithinBounds(center_screen, dockpanel) is false)
                    {
                        continue;
                    }
                    else
                    {
                        Point centerPoint = new Point(center_screen.X,
                                center_screen.Y);

                        ctx.DrawEllipse(lineStrokeBrush, pen, centerPoint, center_pt_dia, center_pt_dia); // center point marker

                        idLabel = new FormattedText(
                            wall.Key.ToString(),
                            CultureInfo.GetCultureInfo("en-us"),
                            FlowDirection.LeftToRight,
                            new Typeface("Consolas"),
                            14,
                            Brushes.Black,
                            VisualTreeHelper.GetDpi(this).PixelsPerDip);
                        ctx.DrawText(idLabel, center_screen);  // id label
                    }
                }
            }

            // Redraw all the shapes in world coordinates
            if ((Calculator != null) && (Calculator._diaphragm_system != null))
            {
                foreach (var rect in Calculator._diaphragm_system._diaphragms)
                {
                    FormattedText idLabel = null;

                    Point p1_world = rect.Value.P1;
                    Point p3_world = rect.Value.P3;
                    Point p1_screen = GetConstrainedScreenPoint(WorldToScreen(p1_world, m_layers), m_layers);
                    Point p3_screen = GetConstrainedScreenPoint(WorldToScreen(p3_world, m_layers), m_layers);

                    // If the points are the same, then the object was out of bounds and doesn't need to be drawn.
                    if (p1_screen == p3_screen)
                        continue;

                    SolidColorBrush rectFillBrush = new SolidColorBrush(Colors.Red);
                    rectFillBrush.Opacity = 0.5;
                    Pen pen = new Pen(rectFillBrush, 1);

                    Point insertPoint_screen = new Point(p1_screen.X, p3_screen.Y);  // TODO: Need a better way to get P4 point of rectangle

                    double width = Math.Abs(p3_screen.X - p1_screen.X);
                    double height = Math.Abs(p3_screen.Y - p1_screen.Y);
                    ctx.DrawRectangle(rectFillBrush, pen, new Rect(insertPoint_screen.X, insertPoint_screen.Y, width, height));

                    Point center_world = new Point((rect.Value.P1.X + rect.Value.P3.X) / 2, (rect.Value.P1.Y + rect.Value.P3.Y) / 2);
                    Point center_screen = WorldToScreen(center_world, m_layers);

                    // draw the center point and label
                    if (PointIsWithinBounds(center_screen, dockpanel) is false)
                        continue;
                    else
                    {
                        Point centerPoint = new Point(center_screen.X,
                            center_screen.Y);

                        ctx.DrawEllipse(rectFillBrush, pen, centerPoint, center_pt_dia, center_pt_dia); // center point marker

                        idLabel = new FormattedText(
                            rect.Key.ToString(),
                            CultureInfo.GetCultureInfo("en-us"),
                            FlowDirection.LeftToRight,
                            new Typeface("Consolas"),
                            14,
                            Brushes.Black,
                            VisualTreeHelper.GetDpi(this).PixelsPerDip);
                        ctx.DrawText(idLabel, centerPoint);  // id label
                    }
                }
            }
        }

        private void DrawDebug(DrawingContext ctx)
        {
            if (debugMode == false)
                return;

            if (Calculator == null) return;

            if (Calculator._wall_system == null && Calculator._wall_system._walls != null)
            {
                // Redraw all the shapes in world coordinates
                FormattedText idLabel = null;
                foreach (var wall in Calculator._wall_system._walls)
                {
                    Point p1_world = wall.Value.Start;
                    Point p2_world = wall.Value.End;
                    Point p1_screen = WorldToScreen(p1_world, m_layers);
                    Point p2_screen = WorldToScreen(p2_world, m_layers);

                    // START POINT
                    if (PointIsWithinBounds(p1_screen, dockpanel) is true)
                    {
                        ctx.DrawEllipse(Brushes.MediumBlue, new Pen(Brushes.Black, 1), p1_screen, 5, 5);
                        idLabel = new FormattedText(
                                $"({p1_world.X:F2}, {p1_world.Y:F2})",
                                CultureInfo.GetCultureInfo("en-us"),
                                FlowDirection.LeftToRight,
                                new Typeface("Consolas"),
                                14,
                                Brushes.Black,
                                VisualTreeHelper.GetDpi(this).PixelsPerDip);

                        ctx.DrawText(idLabel, p1_screen);  // id label
                    }

                    // END POINT
                    if (PointIsWithinBounds(p2_screen, dockpanel) is true)
                    {
                        ctx.DrawEllipse(Brushes.MediumBlue, new Pen(Brushes.Black, 1), p2_screen, 5, 5);
                        idLabel = new FormattedText(
                                $"({p2_world.X:F2}, {p2_world.Y:F2})",
                                CultureInfo.GetCultureInfo("en-us"),
                                FlowDirection.LeftToRight,
                                new Typeface("Consolas"),
                                14,
                                Brushes.Black,
                                VisualTreeHelper.GetDpi(this).PixelsPerDip);
                        ctx.DrawText(idLabel, p2_screen);  // id label
                    }
                }
            }

            if (Calculator._diaphragm_system != null && Calculator._diaphragm_system._diaphragms != null)
            {
                FormattedText idLabel = null;

                foreach (var dia in Calculator._diaphragm_system._diaphragms)
                {
                    Point p1_world = dia.Value.P1;
                    Point p2_world = dia.Value.P2;
                    Point p3_world = dia.Value.P3;
                    Point p4_world = dia.Value.P4;
                    Point p1_screen = WorldToScreen(p1_world, m_layers);
                    Point p2_screen = WorldToScreen(p2_world, m_layers);
                    Point p3_screen = WorldToScreen(p3_world, m_layers);
                    Point p4_screen = WorldToScreen(p4_world, m_layers);

                    // P1
                    if (PointIsWithinBounds(p1_screen, dockpanel) is true)
                    {

                        ctx.DrawEllipse(Brushes.Red, new Pen(Brushes.Black, 1), p1_screen, 5, 5);
                        idLabel = new FormattedText(
                                $"({p1_world.X:F2}, {p1_world.Y:F2})",
                                CultureInfo.GetCultureInfo("en-us"),
                                FlowDirection.LeftToRight,
                                new Typeface("Consolas"),
                                14,
                                Brushes.Black,
                                VisualTreeHelper.GetDpi(this).PixelsPerDip);

                        ctx.DrawText(idLabel, p1_screen);  // id label
                    }

                    // P2
                    if (PointIsWithinBounds(p2_screen, dockpanel) is true)
                    {
                        ctx.DrawEllipse(Brushes.Red, new Pen(Brushes.Black, 1), p2_screen, 5, 5);
                        idLabel = new FormattedText(
                                $"({p2_world.X:F2}, {p2_world.Y:F2})",
                                CultureInfo.GetCultureInfo("en-us"),
                                FlowDirection.LeftToRight,
                                new Typeface("Consolas"),
                                14,
                                Brushes.Black,
                                VisualTreeHelper.GetDpi(this).PixelsPerDip);
                        ctx.DrawText(idLabel, p2_screen);  // id label
                    }

                    // P3
                    if (PointIsWithinBounds(p3_screen, dockpanel) is true)
                    {
                        ctx.DrawEllipse(Brushes.Red, new Pen(Brushes.Black, 1), p3_screen, 5, 5);
                        idLabel = new FormattedText(
                                $"({p3_world.X:F2}, {p3_world.Y:F2})",
                                CultureInfo.GetCultureInfo("en-us"),
                                FlowDirection.LeftToRight,
                                new Typeface("Consolas"),
                                14,
                                Brushes.Black,
                                VisualTreeHelper.GetDpi(this).PixelsPerDip);
                        ctx.DrawText(idLabel, p3_screen);  // id label
                    }

                    // P4
                    if (PointIsWithinBounds(p4_screen, dockpanel) is true)
                    {
                        ctx.DrawEllipse(Brushes.Red, new Pen(Brushes.Black, 1), p4_screen, 5, 5);
                        idLabel = new FormattedText(
                                $"({p4_world.X:F2}, {p4_world.Y:F2})",
                                CultureInfo.GetCultureInfo("en-us"),
                                FlowDirection.LeftToRight,
                                new Typeface("Consolas"),
                                14,
                                Brushes.Black,
                                VisualTreeHelper.GetDpi(this).PixelsPerDip);

                        ctx.DrawText(idLabel, p4_screen);  // id label
                    }

                }
            }
        }
        private void DrawBracedWallLines(DrawingContext ctx)
        {
            if (Calculator == null) return;
            // do we have a wall system or BWL manager created yet?
            if(Calculator._wall_system is null || Calculator._wall_system.BWL_Manager is null)
            {
                return;
            }

            // the counter for uniquely numbering the brace wall lines
            // TODO should this be handled by the windLoadCalculator_RidgeIsParallelToBuildingLength instead of when its being drawn?
            int bwl_count = 1;

            for (int i = 0; i < Calculator._wall_system.BWL_Manager.BracedWallLines.Count; i++)
            {
                BracedWallLine bwl = Calculator._wall_system.BWL_Manager.BracedWallLines[i];
                int bwl_id = bwl.GroupNumber;

                if (bwl.WallDir == WallDirs.EastWest)
                {
                    double center = bwl.Center.Y;

                    Point p1_world = new Point(-10, center);
                    Point p1_screen = GetConstrainedScreenPoint(WorldToScreen(p1_world, dockpanel), dockpanel); ;
                    Point p2_world = new Point(dockpanel.ActualWidth, center);
                    Point p2_screen = GetConstrainedScreenPoint(WorldToScreen(p2_world, dockpanel), dockpanel); ;

                    Pen pen = new Pen(Brushes.Black, 2);
                    pen.DashStyle = new DashStyle(new double[] { 3, 1, 3 }, 0);
                    ctx.DrawLine(pen, p1_screen, p2_screen);

                    // display the com as a text
                    FormattedText idLabel = new FormattedText(
                        $"BWL{bwl_id.ToString()}",
                        CultureInfo.GetCultureInfo("en-us"),
                        FlowDirection.LeftToRight,
                        new Typeface("Consolas"),
                        14,
                        Brushes.Black,
                        VisualTreeHelper.GetDpi(this).PixelsPerDip);

                    ctx.DrawText(idLabel, new Point(p1_screen.X - 40, p1_screen.Y - 7));

                    bwl_count++;
                }
                else if (bwl.WallDir == WallDirs.NorthSouth)
                {
                    double center = bwl.Center.X;

                    Point p1_world = new Point(center, -10);
                    Point p1_screen = GetConstrainedScreenPoint(WorldToScreen(p1_world, dockpanel), dockpanel); ;
                    Point p2_world = new Point(center, dockpanel.ActualHeight);
                    Point p2_screen = GetConstrainedScreenPoint(WorldToScreen(p2_world, dockpanel), dockpanel); ;

                    Pen pen = new Pen(Brushes.Black, 2);
                    pen.DashStyle = new DashStyle(new double[] { 3, 1, 3 }, 0);
                    ctx.DrawLine(pen, p1_screen, p2_screen);

                    // display the com as a text
                    FormattedText idLabel = new FormattedText(
                        $"BWL{bwl_id.ToString()}",
                        CultureInfo.GetCultureInfo("en-us"),
                        FlowDirection.LeftToRight,
                        new Typeface("Consolas"),
                        14,
                        Brushes.Black,
                        VisualTreeHelper.GetDpi(this).PixelsPerDip);

                    ctx.PushTransform(new RotateTransform(90, p1_screen.X, p1_screen.Y + 10));
                    ctx.DrawText(idLabel, new Point(p1_screen.X, p1_screen.Y));
                    ctx.Pop();  // remember to pop the transform after its been used.
                }
            }
        }
        private void DrawSnapMarkers(DrawingContext ctx)
        {
            if (snapMode == false)
            {
                return;
            }

            if (Calculator == null) return;

            if (Calculator._wall_system != null)
            {
                foreach (var wall in Calculator._wall_system._walls)
                {
                    Point p1_world = wall.Value.Start;
                    Point p2_world = wall.Value.End;
                    Point p1_screen = WorldToScreen(p1_world, m_layers);
                    Point p2_screen = WorldToScreen(p2_world, m_layers);

                    ctx.DrawEllipse(Brushes.MediumBlue, new Pen(Brushes.MediumBlue, 1), p1_screen, 3, 3);
                    ctx.DrawEllipse(Brushes.MediumBlue, new Pen(Brushes.MediumBlue, 1), p2_screen, 3, 3);
                }
            }

            if (Calculator._diaphragm_system != null)
            {
                foreach (var dia in Calculator._diaphragm_system._diaphragms)
                {
                    Point p1_world = dia.Value.P1;
                    Point p2_world = dia.Value.P2;
                    Point p3_world = dia.Value.P3;
                    Point p4_world = dia.Value.P4;
                    Point p1_screen = WorldToScreen(p1_world, m_layers);
                    Point p2_screen = WorldToScreen(p2_world, m_layers);
                    Point p3_screen = WorldToScreen(p3_world, m_layers);
                    Point p4_screen = WorldToScreen(p4_world, m_layers);

                    ctx.DrawEllipse(Brushes.Red, new Pen(Brushes.Red, 1), p1_screen, 3, 3);
                    ctx.DrawEllipse(Brushes.Red, new Pen(Brushes.Red, 1), p2_screen, 3, 3);
                    ctx.DrawEllipse(Brushes.Red, new Pen(Brushes.Red, 1), p3_screen, 3, 3);
                    ctx.DrawEllipse(Brushes.Red, new Pen(Brushes.Red, 1), p4_screen, 3, 3);
                }
            }
        }
        private void DrawCOMandCOR(DrawingContext ctx)
        {
            if (Calculator is null) return;

            // Draw a marker for the center of mass
            if (Calculator != null)
            {
                if(Calculator._diaphragm_system != null)
                {
                    // Draw the center of mass and center of rigidity
                    var com = Calculator._diaphragm_system.CtrMass;

                    // display the com as a text
                    FormattedText idLabel = new FormattedText(
                        $"COM ({com.X.ToString("F2")}, {com.Y.ToString("F2")})",
                        CultureInfo.GetCultureInfo("en-us"),
                        FlowDirection.LeftToRight,
                        new Typeface("Consolas"),
                        14,
                        Brushes.Black,
                        VisualTreeHelper.GetDpi(this).PixelsPerDip);

                    ctx.DrawText(idLabel, new Point(5, 5));

                    double x_screen = WorldToScreen(com, dockpanel).X;
                    double y_screen = WorldToScreen(com, dockpanel).Y;

                    // draw a vertical line since the Y is valid
                    if ((double.IsNaN(com.X) is false) && (double.IsNaN(com.Y) is true))
                    {
                        Pen pen = new Pen(Brushes.Red, 2);
                        pen.DashStyle = new DashStyle(new double[] { 3, 3 }, 0);

                        x_screen = 10; // draw it justbelow the top
                        ctx.DrawLine(pen, new Point(x_screen, 0), new Point(x_screen, dockpanel.ActualHeight));
                    }

                    // draw a vertical line since if the X is valid
                    else if ((double.IsNaN(com.Y) is false) && (double.IsNaN(com.X) is true))
                    {
                        Pen pen = new Pen(Brushes.Red, 2);
                        pen.DashStyle = new DashStyle(new double[] { 3, 3 }, 0);

                        y_screen = dockpanel.ActualHeight - 10;

                        ctx.DrawLine(pen, new Point(0, y_screen), new Point(dockpanel.ActualHeight, y_screen));
                    }

                    // draw two lines since neither value is valid
                    else if ((double.IsNaN(com.X) is true) && (double.IsNaN(com.Y) is true))
                    {
                        Pen pen = new Pen(Brushes.Red, 2);
                        pen.DashStyle = new DashStyle(new double[] { 3, 3 }, 0);

                        y_screen = dockpanel.ActualHeight - 10;
                        x_screen = 10;

                        ctx.DrawLine(pen, new Point(0, y_screen), new Point(dockpanel.ActualWidth, y_screen));
                        ctx.DrawLine(pen, new Point(x_screen, 0), new Point(x_screen, dockpanel.ActualHeight));
                    }

                    // Draw a marker
                    Point pt = new Point(x_screen, y_screen);

                    if (PointIsWithinBounds(pt, dockpanel))
                    {
                        ctx.DrawEllipse(Brushes.Red, new Pen(Brushes.Black, 2), pt, 8, 8);
                        ctx.DrawEllipse(Brushes.Red, new Pen(Brushes.Black, 2), pt, 5, 5);

                        idLabel = new FormattedText(
                            $"COM",
                            CultureInfo.GetCultureInfo("en-us"),
                            FlowDirection.LeftToRight,
                            new Typeface("Consolas"),
                            14,
                            Brushes.Black,
                            VisualTreeHelper.GetDpi(this).PixelsPerDip);

                        ctx.DrawText(idLabel, new Point(x_screen + 5, y_screen - 20));
                    }
                }




            }

            if (Calculator != null && Calculator._wall_system != null)
            {
                // display the cor as a text
                var cor = Calculator._wall_system.CtrRigidity;

                FormattedText idLabel = new FormattedText(
                    $"COR ({cor.X.ToString("F2")}, {cor.Y.ToString("F2")})",
                    CultureInfo.GetCultureInfo("en-us"),
                    FlowDirection.LeftToRight,
                    new Typeface("Consolas"),
                    14,
                    Brushes.Black,
                    VisualTreeHelper.GetDpi(this).PixelsPerDip);

                ctx.DrawText(idLabel, new Point(5, 19));
                double x_screen = WorldToScreen(cor, dockpanel).X;
                double y_screen = WorldToScreen(cor, dockpanel).Y;

                // draw a vertical line since the Y is valid
                if ((double.IsNaN(cor.X) is false) && (double.IsNaN(cor.Y) is true))
                {

                    Pen pen = new Pen(Brushes.MediumBlue, 2);
                    pen.DashStyle = new DashStyle(new double[] { 3, 3 }, 0);

                    y_screen = 10; // draw it justbelow the top
                    ctx.DrawLine(pen, new Point(x_screen, 0), new Point(x_screen, dockpanel.ActualHeight));
                }

                // draw a horizontal line since if the X is valid
                else if ((double.IsNaN(cor.Y) is false) && (double.IsNaN(cor.X) is true))
                {
                    Pen pen = new Pen(Brushes.MediumBlue, 2);
                    pen.DashStyle = new DashStyle(new double[] { 3, 3 }, 0);

                    x_screen = dockpanel.ActualWidth - 10;

                    ctx.DrawLine(pen, new Point(0, y_screen), new Point(dockpanel.ActualWidth, y_screen));
                }

                // draw two lines since neither value is valid
                else if ((double.IsNaN(cor.X) is true) && (double.IsNaN(cor.Y) is true))
                {
                    Pen pen = new Pen(Brushes.MediumBlue, 2);
                    pen.DashStyle = new DashStyle(new double[] { 3, 3 }, 0);

                    y_screen = 10;
                    x_screen = dockpanel.ActualWidth - 10;

                    ctx.DrawLine(pen, new Point(x_screen, 0), new Point(x_screen, dockpanel.ActualHeight));
                    ctx.DrawLine(pen, new Point(0, y_screen), new Point(dockpanel.ActualWidth, y_screen));
                }

                // Draw a marker
                Point pt = new Point(x_screen, y_screen);

                if (PointIsWithinBounds(pt, dockpanel))
                {
                    ctx.DrawEllipse(Brushes.MediumBlue, new Pen(Brushes.Black, 2), pt, 8, 8);
                    ctx.DrawEllipse(Brushes.MediumBlue, new Pen(Brushes.Black, 2), pt, 5, 5);

                    idLabel = new FormattedText(
                        $"COR",
                        CultureInfo.GetCultureInfo("en-us"),
                        FlowDirection.LeftToRight,
                        new Typeface("Consolas"),
                        14,
                        Brushes.Black,
                        VisualTreeHelper.GetDpi(this).PixelsPerDip);

                    ctx.DrawText(idLabel, new Point(x_screen - 30, y_screen + 5));
                }
            }
        }
        private void DrawReferenceImage(DrawingContext ctx)
        {

            if (hideImage)
                return;

            // If no calculator, clear the cached image and return
            if (Calculator == null)
            {
                cachedReferenceImageBitmap = null;
                imageNeedsUpdate = true;
                return;
            }


            if (cachedReferenceImageBitmap == null || imageNeedsUpdate is true)
            {
                CreateReferenceImageVisual();
                imageNeedsUpdate = false;
            }

            // Check again in case image loading failed or no file selected
            if (cachedReferenceImageBitmap == null)
                return;



            ctx.DrawImage(cachedReferenceImageBitmap, CalculateImageScreenRect());

        }

        private Rect CalculateImageScreenRect()
        {
            // Draw image scaled to fit canvas
            double scale_x = Calculator.pixelScaleX;
            double scale_y = Calculator.pixelScaleY;

            Point p1_world = new Point(0, 0);
            Point p2_world = new Point(cachedReferenceImageBitmap.PixelWidth * scale_x, 0);
            Point p3_world = new Point(cachedReferenceImageBitmap.PixelWidth * scale_x, cachedReferenceImageBitmap.PixelHeight * scale_y);
            Point p4_world = new Point(0, cachedReferenceImageBitmap.PixelHeight * scale_y);

            Point p1_screen = WorldToScreen(p1_world, m_layers);
            Point p2_screen = WorldToScreen(p2_world, m_layers);
            Point p3_screen = WorldToScreen(p3_world, m_layers);
            Point p4_screen = WorldToScreen(p4_world, m_layers);

            double width_screen = Math.Abs(p3_screen.X - p1_screen.X);
            double height_screen = Math.Abs(p3_screen.Y - p1_screen.Y);

            return new Rect(p4_screen.X, p4_screen.Y, width_screen, height_screen);
        }

        private void InvalidateCachedImageIfChanged()
        {
            if (cachedImagePath != Calculator.selectedImageFilePath)
            {
                cachedReferenceImageBitmap = null;
                cachedImagePath = Calculator.selectedImageFilePath;
                hasCachedReferenceImageScreenRect = false;
            }
        }

        private void CreateReferenceImageVisual()
        {
            if (Calculator == null) return;

            if (hideImage || string.IsNullOrEmpty(Calculator.selectedImageFilePath) || !File.Exists(Calculator.selectedImageFilePath))
                return;

            InvalidateCachedImageIfChanged();

            double width = dockpanel.ActualWidth;
            double height = dockpanel.ActualHeight;

            imageBitmap = new RenderTargetBitmap((int)Math.Ceiling(width), (int)Math.Ceiling(height), 96, 96, PixelFormats.Pbgra32);
            imageVisual = new DrawingVisual();

            using (DrawingContext ctx = imageVisual.RenderOpen())
            {
                try
                {
                    // Always load image if not yet loaded
                    if (cachedReferenceImageBitmap == null)
                    {
                        cachedReferenceImageBitmap = new BitmapImage();
                        cachedReferenceImageBitmap.BeginInit();
                        cachedReferenceImageBitmap.UriSource = new Uri(Calculator.selectedImageFilePath, UriKind.Absolute);
                        cachedReferenceImageBitmap.CacheOption = BitmapCacheOption.OnLoad;
                        cachedReferenceImageBitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                        cachedReferenceImageBitmap.EndInit();
                        cachedReferenceImageBitmap.Freeze();
                    }

                    if (!hasCachedReferenceImageScreenRect)
                    {

                        // Always draw the image to the new bitmap
                        double scale_x = Calculator.pixelScaleX;
                        double scale_y = Calculator.pixelScaleY;

                        Point p1_world = new Point(0, 0);
                        Point p3_world = new Point(cachedReferenceImageBitmap.PixelWidth * scale_x, cachedReferenceImageBitmap.PixelHeight * scale_y);
                        Point p4_world = new Point(0, cachedReferenceImageBitmap.PixelHeight * scale_y);

                        Point p1_screen = WorldToScreen(p1_world, m_layers);
                        Point p3_screen = WorldToScreen(p3_world, m_layers);
                        Point p4_screen = WorldToScreen(p4_world, m_layers);

                        double width_screen = Math.Abs(p3_screen.X - p1_screen.X);
                        double height_screen = Math.Abs(p3_screen.Y - p1_screen.Y);

                        cachedReferenceImageScreenRect = new Rect(p4_screen.X, p4_screen.Y, width_screen, height_screen);
                        hasCachedReferenceImageScreenRect = true;
                    }

                    ctx.DrawImage(cachedReferenceImageBitmap, cachedReferenceImageScreenRect);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to load image: " + ex.Message);
                }
            }

            imageBitmap.Render(imageVisual);
        }

        private void DrawGridInformation(DrawingContext ctx)
        {
            if (hideGrid)
            {
                return;  // Skip drawing if the grid is hidden
            }

            // Check if zoom or pan has changed
            if (gridNeedsUpdate || gridVisual == null || gridBitmap == null)
            {
                // Recreate the grid visual when necessary (zoom or pan has changed)
                CreateGridVisual();
                gridNeedsUpdate = false;
            }

            // Draw the cached grid bitmap
            ctx.DrawImage(gridBitmap, new Rect(0, 0, dockpanel.ActualWidth, dockpanel.ActualHeight));
        }

        private void CreateGridVisual()
        {
            // Define the size of the render target bitmap (same size as your drawing area)
            double width = dockpanel.ActualWidth;
            double height = dockpanel.ActualHeight;

            // Create a new RenderTargetBitmap with the same size as the drawing area
            gridBitmap = new RenderTargetBitmap((int)width, (int)height, 96, 96, PixelFormats.Pbgra32);

            // Create a DrawingVisual to draw the grid
            gridVisual = new DrawingVisual();

            using (DrawingContext ctx = gridVisual.RenderOpen())
            {
                ConstructVisualGrid(ctx);  // This is the method that draws the grid
            }

            // Render the DrawingVisual to the RenderTargetBitmap
            gridBitmap.Render(gridVisual);
        }

        private void InvalidateGrid()
        {
            // Mark the grid as needing an update (this should be called when zoom or pan changes)
            gridNeedsUpdate = true;
        }


        private void Draw(ChangeType change)
        {
            m_layers.Draw(change);
        }

        #endregion

        #region Drawing Layer Events

        private void m_layers_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            //MessageBox.Show("Drawing Visual Clicked at " + e.GetPosition(m_layers).ToString());

            if (e.RightButton == MouseButtonState.Pressed)
            {
                ResetInputMode();
                UpdateShearWallUI();
                return;
            }


            if (currentMode == DrawMode.None)
            {
                MessageBox.Show("No drawing mode selected.  Try selecting L (line mode) or R (rectangle) mode first.");
                return;  // Ignore if not in drawing mode
            }

            Point screenPoint = e.GetPosition(m_layers);
            Point worldPoint = ScreenToWorld(screenPoint, m_layers);

            if (snapMode)
            {
                worldPoint = GetSnappedPoint(worldPoint);
            }

            if (startPoint_world == null)
            {
                startPoint_world = worldPoint;
                CreatePreviewShape();
            }
            else
            {
                endPoint_world = worldPoint;
                FinalizeShape(endPoint_world.Value);
            }

            UpdateShearWallUI();
        }


        private void m_layers_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            currentMouseScreenPosition = e.GetPosition(m_layers);
            Point currentMouseWorldPosition = ScreenToWorld(currentMouseScreenPosition, m_layers);

            tbScreenCoords.Text = e.GetPosition(m_layers).ToString();
            tbWorldCoords.Text = "World Coords: (" + currentMouseWorldPosition.X.ToString("F2") + ", " + currentMouseWorldPosition.Y.ToString("F2") + ")";  // changed this one too

            UpdateShearWallUI();
        }

        private void m_layers_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Determine the zoom factor
            double zoomFactor = (e.Delta > 0) ? 1.2 : 0.8;

            // Get the mouse position_screen relative to the canvas
            Point mousePosition_screen = e.GetPosition(m_layers);

            // Transform the mouse position_screen to world coordinates before zooming
            Point beforeZoom = ScreenToWorld(mousePosition_screen, m_layers);

            // Apply zoom
            zoomFactorX *= zoomFactor;
            zoomFactorY *= zoomFactor;

            // Transform the mouse position_screen to world coordinates after zooming
            Point afterZoom = ScreenToWorld(mousePosition_screen, m_layers);

            // Adjust pan offset to keep the mouse position_screen centered
            panOffsetX -= (afterZoom.X - beforeZoom.X);
            panOffsetY -= (afterZoom.Y - beforeZoom.Y);

            // Display current parameters on screen
            tbPan.Text = "Pan: (" + panOffsetX.ToString("F2") + ", " + panOffsetY.ToString("F2") + ")";
            tbZoom.Text = "Zoom: (" + zoomFactorX.ToString("F2") + ", " + zoomFactorY.ToString("F2") + ")";
            tbWorldCoords.Text = "World Coords: (" + afterZoom.X.ToString("F2") + ", " + afterZoom.Y.ToString("F2") + ")";  // changed this one too
            tbScreenCoords.Text = "Screen Coords: (" + mousePosition_screen.X.ToString("F2") + ", " + mousePosition_screen.Y.ToString("F2") + ")"; // changed this one too

            InvalidateGrid();               // signal that the grid needs updating

            UpdateShearWallUI();
        }

        private void m_layers_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Right button click to CANCEL
            if (e.RightButton == MouseButtonState.Pressed)
            {
                ResetInputMode();
                UpdateShearWallUI();
                return;
            }
        }

        #endregion

        #region UI Events
        private void btnTestDesign_Click(object sender, RoutedEventArgs e)
        {
            if (Calculator == null || Calculator._wall_system == null || Calculator._diaphragm_system == null)
            {
                MessageBox.Show("No valid calculator found in btnTEstDesign_Click.");
                return;
            }
            else
            {
                // test load data
                Calculator.AddLoads(15, 15);

                // test wall key
                int wall_id = 0;

                if (Calculator._wall_system._walls.ContainsKey(wall_id) is true)
                {
                    WallData test_wall = Calculator._wall_system._walls[wall_id];
                    ShearWallSelector selector = new ShearWallSelector(Calculator.TotalWallShear[wall_id], test_wall, simpsonCatalog, ConnectorTypes.CONNECTOR_STRAP_TIES, WoodTypes.WOODTYPE_DF_SP);
                    Console.WriteLine("--------------------------");
                    Console.WriteLine("Shear: " + Calculator.TotalWallShear[wall_id]);
                    foreach (var key in selector.selectedConnectors)
                    {

                        Console.WriteLine(key.Model);
                    }
                    Console.WriteLine("--------------------------");
                }

                UpdateShearWallUI();
            }
        }

        private void btnHideShapes_Click(object sender, RoutedEventArgs e)
        {
            hideShapes = !hideShapes;
            UpdateShearWallUI();
            btnHideShapes.Content = hideShapes ? "Show Shapes" : "Hide Shapes";

        }

        private void btnHideImage_Click(object sender, RoutedEventArgs e)
        {
            hideImage = !hideImage;
            UpdateShearWallUI();

            btnHideImage.Content = hideImage ? "Show Image" : "Hide Image";
        }

        private void btnHideGrid_Click(object sender, RoutedEventArgs e)
        {
            hideGrid = !hideGrid;
            UpdateShearWallUI();
            btnHideGrid.Content = hideGrid ? "Show Grid" : "Hide Grid";
        }

        private void btnLineMode_Click(object sender, RoutedEventArgs e)
        {
            currentMode = DrawMode.Line;
        }

        private void btnRectangleMode_Click(object sender, RoutedEventArgs e)
        {
            currentMode = DrawMode.Rectangle;
        }

        private void btnSnapMode_Click(object sender, RoutedEventArgs e)
        {
            SetSnapMode();
        }

        private void btnOpenLoadDialog_Click(object sender, RoutedEventArgs e)
        {
            if(Calculator == null)
            {
                Console.WriteLine("No valid calculator found in btnOpenLoadDialog_Click.");
                return;
            }
            var dialog = new LoadInputDialog(Calculator.V_x, Calculator.V_y)
            {
                Owner = this
            };

            if (dialog.ShowDialog() == true)
            {
                Calculator.AddLoads(dialog.MagnitudeX, dialog.MagnitudeY);
                UpdateShearWallUI();  // update the calculator
            }
        }

        #endregion

        #region Menu and Key Events
        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            Console.WriteLine($"Key Pressed: {e.Key}");

            // line (wall) mode
            if (e.Key == Key.L)
            {
                ResetInputMode();
                currentMode = DrawMode.Line;
            }

            // rectangle (diaphragm) mode
            else if (e.Key == Key.R)
            {
                ResetInputMode();
                currentMode = DrawMode.Rectangle;
            }
            // snap mode
            else if (e.Key == Key.S)
            {
                SetSnapMode();
            }
            // debug mode
            else if (e.Key == Key.D)
            {
                SetDebugMode();
            }
            // reset view 
            else if (e.Key == Key.Z)
            {
                ResetInputMode();
                ResetView();
                InvalidateGrid();
            }

            UpdateShearWallUI();
        }

        private void OpenImageTool_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ImageMeasurementWindow
            {
                Owner = this
            };

            dialog.MeasurementCompleted += OnMeasurementCompleted;
            dialog.ShowDialog();

            //if (dialog.ShowDialog() == true && dialog.Result != null)
            //{
            //    var result = dialog.Result;

            //    MessageBox.Show(
            //        $"Image: {result.FilePath}\n" +
            //        $"Pixel Distance: {result.PixelDistance:F2}\n" +
            //        $"Real Distance: {result.RealWorldDistance:F2}\n" +
            //        $"Scale Factor: {result.ScaleFactor:F4} units/pixel",
            //        "Measurement Result");
            //}
        }
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            // You can refresh your data-bound controls here.
            MessageBox.Show("Refresh triggered!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MenuItem_Save_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "JSON Drawing (*.json)|*.json",
                FileName = "drawing.json"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                _serializer.Save(saveFileDialog.FileName, Calculator);
                MessageBox.Show("Drawing saved!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void MenuItem_Load_Click(object sender, RoutedEventArgs e)
        {
            if (Calculator != null)
            {
                Calculator = null;  // delete any previous calculators we have
            }

            UpdateShearWallUI();

            var openFileDialog = new OpenFileDialog
            {
                Filter = "JSON Drawing (*.json)|*.json"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                JsonSerializerSettings _settings = new JsonSerializerSettings();

                var json = _serializer.Load(openFileDialog.FileName);

                // Parse just enough to extact a fieldJObject obj = JObject.Parse(json);
                JObject obj = JObject.Parse(json);

                // Get the calculator type value
                string calculatorType = (string)obj["CalculatorType"];

                // Load the appropriate calculator
                if (calculatorType == "Rigid Diaphragm")
                {
                    var rigid_calc = JsonConvert.DeserializeObject<ShearWallCalculator_RigidDiaphragm>(json, _settings);
                    Calculator = new ShearWallCalculator_RigidDiaphragm(rigid_calc);
                }
                else if (calculatorType == "Flexible Diaphragm")
                {
                    var flex_calc = JsonConvert.DeserializeObject<ShearWallCalculator_FlexibleDiaphragm>(json, _settings);
                    Calculator = new ShearWallCalculator_FlexibleDiaphragm(flex_calc);
                }
                else
                {
                    throw new Exception("Invalid calculator type in MenuItem_Load_Click()");
                }

                UpdateShearWallUI();
                MessageBox.Show("Drawing loaded!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        #endregion

        #region File Handling
        private void OpenFile(string path)
        {
            // TODO: Replace with your file loading logic
            MessageBox.Show($"Opening file:\n{path}", "Open File", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AddToRecentFiles(string path)
        {
            var recent = Properties.Settings.Default.RecentFiles ?? new StringCollection();

            if (recent.Contains(path))
                recent.Remove(path); // Move to top

            recent.Insert(0, path);

            // Limit to 10 recent entries
            while (recent.Count > 10)
                recent.RemoveAt(recent.Count - 1);

            Properties.Settings.Default.RecentFiles = recent;
            Properties.Settings.Default.Save();

            LoadRecentFilesMenu(); // Refresh menu
        }

        private void LoadRecentFilesMenu()
        {
            RecentFilesMenu.Items.Clear();

            var recent = Properties.Settings.Default.RecentFiles ?? new StringCollection();
            bool cleaned = false;

            int index = 1;

            foreach (var file in recent.Cast<string>().ToList())
            {
                if (!File.Exists(file))
                {
                    recent.Remove(file);
                    cleaned = true;
                    continue;
                }

                string label = $"{index}. {System.IO.Path.GetFileName(file)}";

                var menu_item = new MenuItem
                {
                    Header = label,
                    ToolTip = file,
                    Tag = file
                };

                // try to show a small tooltip of the image
                try
                {
                    // Create image preview (larger size for tooltip)
                    var image = new System.Windows.Controls.Image
                    {
                        Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(file)),
                        Width = 150,  // Larger size
                        Height = 150,
                        Margin = new Thickness(0)
                    };

                    // Assign image preview as tooltip content
                    var tooltip = new ToolTip
                    {
                        Content = image
                    };

                    menu_item.ToolTip = tooltip;
                }
                catch
                {
                    // Skip preview if image load fails
                }

                menu_item.Click += (s, e) =>
                {
                    string filePath = (string)((MenuItem)s).Tag;
                    OpenFile(filePath);
                    AddToRecentFiles(filePath); // Move to top again

                    if(Calculator != null)
                    {
                        Calculator.selectedImageFilePath = filePath;
                    }
                };

                RecentFilesMenu.Items.Add(menu_item);
                index++;
            }

            if (cleaned)
            {
                Properties.Settings.Default.RecentFiles = recent;
                Properties.Settings.Default.Save();
            }

            if (RecentFilesMenu.Items.Count == 0)
            {
                RecentFilesMenu.Items.Add(new MenuItem
                {
                    Header = "No recent files",
                    IsEnabled = false
                });
            }
            else
            {
                RecentFilesMenu.Items.Add(new Separator());

                var clearItem = new MenuItem
                {
                    Header = "Clear Recent Files"
                };

                clearItem.Click += (s, e) => ClearRecentFiles();
                RecentFilesMenu.Items.Add(clearItem);
            }
        }

        /// <summary>
        /// 1. RecentFiles needs to be set in Properties > Settings.settings
        /// 2.  Add a setting Name: RecentFiles, Type: StringCollection, Scope: User, Default Value: <empty>
        /// </summary>
        private void ClearRecentFiles()
        {
            Properties.Settings.Default.RecentFiles = new StringCollection();
            Properties.Settings.Default.Save();
            LoadRecentFilesMenu();
        }


        private void OnMeasurementCompleted(object sender, ImageMeasurementEventArgs e)
        {
            if (Calculator != null)
            {
                // set the image parameters so that the DrawRerenceImage function has the items it needs to draw the true image.
                Calculator.selectedImageFilePath = e.FilePath;
                Calculator.pixelScaleX = e.ScaleFactor;
                Calculator.pixelScaleY = e.ScaleFactor;

                // Handle the measurement data here
                MessageBox.Show($"Measurement completed!\n" +
                                $"File: {e.FilePath}\n" +
                                $"Pixel Distance: {e.PixelDistance:F2}\n" +
                                $"Real-World Distance: {e.RealWorldDistance:F2}\n" +
                                $"Scale Factor: {e.ScaleFactor:F6}");

                UpdateShearWallUI();
            }
        }

        #endregion

        #region MODE setters
        public void SetButtonModes()
        {
            switch (currentMode)
            {
                case DrawMode.Line:
                    SetLineMode();
                    break;
                case DrawMode.Rectangle:
                    SetRectangleMode();
                    break;
                default:
                    return;
            }
        }

        private void SetLineMode()
        {
            ResetUIButtons();
            btnRigidityMode.BorderThickness = new Thickness(3);
            btnRigidityMode.BorderBrush = new SolidColorBrush(Colors.White);
            btnRigidityMode.Background = new SolidColorBrush(Colors.YellowGreen);

            currentMode = DrawMode.Line;  // Set to Line drawing mode
        }

        private void SetRectangleMode()
        {
            ResetUIButtons();
            btnMassMode.BorderThickness = new Thickness(3);
            btnMassMode.BorderBrush = new SolidColorBrush(Colors.White);
            btnMassMode.Background = new SolidColorBrush(Colors.YellowGreen);

            currentMode = DrawMode.Rectangle;  // Set to Rectangle drawing mode
        }

        private void SetDebugMode()
        {
            ResetUIButtons();

            debugMode = !debugMode;
            MessageBox.Show($"Debug Mode {(debugMode ? "Enabled" : "Disabled")}");
            Console.WriteLine($"Debug Mode: {(debugMode ? "Enabled" : "Disabled")}");
        }
        private void SetSnapMode()
        {
            btnSnapToNearest.BorderThickness = new Thickness(3);
            btnSnapToNearest.BorderBrush = new SolidColorBrush(Colors.White);

            snapMode = !snapMode;
            MessageBox.Show($"Snap Mode {(snapMode ? "Enabled" : "Disabled")}");
            Console.WriteLine($"Snap Mode: {(snapMode ? "Enabled" : "Disabled")}");
        }

        /// <summary>
        /// Cancels input mode and clears the previews
        /// </summary>
        private void ResetInputMode()
        {
            // cancel the input by setting the start point to null
            startPoint_world = null;
            endPoint_world = null;
            previewShape = null;
        }

        private void ResetUIButtons()
        {
            btnRigidityMode.BorderThickness = new Thickness(0);
            btnRigidityMode.Background = new SolidColorBrush(Colors.MediumBlue);
            btnMassMode.Background = new SolidColorBrush(Colors.Red);
            btnMassMode.BorderThickness = new Thickness(0);
            btnSnapToNearest.BorderThickness = new Thickness(0);

            if (snapMode is true)
            {
                btnSnapToNearest.BorderThickness = new Thickness(3);
            }

            if (debugMode is true)
            {
                // TODO:: how should the UI be set in debug mode?
            }
        }
        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="region">Should be in the form of "Zone1", "Zone2e", etc. </param>
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
                default:
                    return Brushes.Black;
            }
        }
    }
}
