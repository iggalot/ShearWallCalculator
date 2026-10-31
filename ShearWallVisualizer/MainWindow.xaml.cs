using ASCE7WindLoadCalculator;
using calculator;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ShearWallCalculator;
using ShearWallCalculator.Design;
using ShearWallCalculator.Interfaces;
using ShearWallVisualizer.Controls;
using ShearWallVisualizer.Dialogs;
using ShearWallVisualizer.Helpers;
using ShearWallCalculator.Diagnostics;
using ShearWallCalculator.Drawing;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

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
        public ASCE7_Versions windVersion { get; set; } = ASCE7_Versions.ASCE_VER_7_22;
        public BuildingData buildingData { get; set; } = null;

        /// <summary>
        /// The three wind load calculators for this project
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
        DrawingVisual gridVisual = null;
        private RenderTargetBitmap gridBitmap;

        // plan image: the decoded file (kept while the path is the same) and the two-point commands
        private BitmapSource planBitmap;
        private string planBitmapPath;
        private readonly ImageTool imageTool = new ImageTool();

        double majorGridSpacing = 5.0;  // Major grid lines in world units
        double minorGridSpacing = 1.0;  // Minor grid lines in world units

        public EventHandler OnUpdated;  // the event that signals that the drawing has been updated -- controls will listen for this at the time they are created.

        bool hideImage = false;
        bool hideGrid = false;
        bool hideShapes = false;
        bool showDesign = true; // schedule marks, hold-downs and the schedule on the plan
        bool imageDrawWarned = false;   // log a reference-image drawing failure once, not every frame

        private double defaultWallHeight = 9.0;

        private enum DrawMode { None, Line, Rectangle }
        private DrawMode currentMode = DrawMode.Line;
        private bool snapMode = true;                     // object snaps on (F3 / S)
        private SnapModes snapModes = SnapModes.Default;   // which snaps (the ▾ menu)
        private SnapResult currentSnap;                    // the snap under the cursor; Point is the cursor when none
        private const double SnapPixels = 10;              // snap reach on screen
        private bool selectionCardOpen;                    // the card shows the picked wall or diaphragm
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
            this.PreviewKeyDown += FileShortcuts; // Ctrl+N, O, S, P
            this.Closing += MainWindow_Closing; // asks to save unsaved changes
            Log.Written += OnLogWritten;      // warnings and errors show on the status line
            this.TextInput += RouteTypingToCommand;  // typing a number while drawing goes to the Point box
            m_layers.MouseLeave += (s, e) => RedrawDrawing(); // drop the crosshair and snap marker
            selectionCard.OpenDesign += SelectionCard_OpenDesign;
            selectionCard.EditGeometry += SelectionCard_EditGeometry;
            selectionCard.DeleteRequested += SelectionCard_Delete;
            selectionCard.Closed += (s, e) => CloseSelectionCard();
            this.Closed += (s, e) => Log.Written -= OnLogWritten;

            // Set focus and canvas background
            this.Focusable = true;
            this.Focus();

            // the function to run once the app has loaded.
            this.Loaded += (s, e) =>
            {
                // Guided wind input. It starts the shared building and wind state, so every tab shows results
                // from the start instead of waiting for the BuildingData tab to be completed.
                windSetup = new WindSetupControl(buildingData, windVersion, windLoadParams);
                windSetup.InputsChanged += WindSetup_InputsChanged;
                windSetup.SendLoadsRequested += WindSetup_SendLoadsRequested;
                windSetup.ShearWallsRequested += (s2, e2) => rbWorkspaceWalls.IsChecked = true;
                tabWindSetupTabItem.Content = windSetup;
                buildingData = windSetup.Building.Clone();
                windLoadParams = windSetup.Parameters.Clone();
                windVersion = windSetup.Version;

                var ctrol_bldg_input = new BuildingDataInputControl(buildingData);
                ctrol_bldg_input.BuildingDataInputComplete += BuildingDataInputControl_BuildingDataInputComplete;
                ctrol_bldg_input.PreviewKeyDown += RunBuildingDataInputCalculations;
                tabBuildingDataControlTabItem.Content = ctrol_bldg_input;

                // create the wind load input control
                var ctrol_wind_input = new WindLoadInputControl(buildingData, windVersion, windLoadParams);
                ctrol_wind_input.WindInputComplete += WindLoadInputControl_WindInputComplete;
                tabWindInputControlTabItem.Content = ctrol_wind_input;

                // create the wind load results controls
                CreateAndAssignResultControls();


                ResetView(); // reset the view so that origin 0,0 is at lower left of the corner screen and the model is zoomed to fill the entire window
                LoadRecentFilesMenu();  // recent files menu
                CreateGridVisual();
                CreateLayers();

                // load the Simpson catalog
                simpsonCatalog = new SimpsonCatalog();
                CreateDesignPanel();

                UpdateShearWallUI();

                // Now clean up and remove the tabs for the results
                TabControlManager.RemoveAllTabs(MainTabControl);
                TabControlManager.ReAddTab(MainTabControl, "tabWindSetupTabItem");
                // The BuildingData and WIND Input tabs are retired: Wind Setup holds the same inputs. Their
                // TabItems and controls remain (re-adding them here would bring them back).
                RecalculateWind(); // adds the results tabs for the starting inputs (and links the wind loads)
                UpdateShearWallUI();
                TabControlManager.ReAddTab(MainTabControl, "tabDimensionsTabItem");
                TabControlManager.ReAddTab(MainTabControl, "tabCalculationsTabItem");
                TabControlManager.ReAddTab(MainTabControl, "tabDesignTabItem");
                ApplyWorkspace();
                MarkSaved(); // a new, untitled project
            };
        }

        public void UpdateShearWallUI()
        {
            if (Calculator != null)
            {
                Calculator.PerformCalculations();
                UpdateDesign();
                LogModelChanges();

                ShowImageName();

                geometryPanel?.Show(Calculator, selectedKind, selectedId);
                resultsPanel?.Show(Calculator, CalculationTodo(), selectedKind == "Wall" ? selectedId : -1);
                selectionCard.Show(Calculator, selectionCardOpen ? selectedKind : null, selectedId, designResults);

                // notify controls that we have updated
                OnUpdated?.Invoke(this, EventArgs.Empty); // signal that the window has been updated -- so that subcontrols can refresh

                // update the load info display
                LoadInfoTextBlock.Text = (windLoadsLinked ? "Wind loads (auto)" : "Manual loads") + $"   X {Calculator.V_x:0.0} k,  Y {Calculator.V_y:0.0} k";
                tbWorkspaceLoads.Text = windLoadsLinked
                    ? $"Shear walls use the wind loads (X {Calculator.V_x:0.0} k, Y {Calculator.V_y:0.0} k), updated automatically."
                    : $"Shear walls use MANUAL loads (X {Calculator.V_x:0.0} k, Y {Calculator.V_y:0.0} k), not the wind loads.";
                tbWorkspaceLoads.Foreground = windLoadsLinked ? Brushes.DarkGreen : Brushes.DarkOrange;
            }

            // UpdateShearWallUI the button appearances
            SetButtonModes();

            // redraw the scene
            Draw(ChangeType.Redraw);
            UpdateTitle();
        }

        // ------------------------------------------------------------------ shear-wall design (Design tab)

        private WallDesignPanel designPanel;
        private GeometryPanel geometryPanel;
        private ShearResultsPanel resultsPanel;

        /// <summary>The wall or diaphragm picked in a table ("Wall" / "Diaphragm", id); it is highlighted on the drawing.</summary>
        private string selectedKind;
        private int selectedId = -1;
        private ShearWallDesigner designer;

        /// <summary>The latest check of each wall (rigid diaphragm), keyed by wall id. Empty when there is nothing to check.</summary>
        private Dictionary<int, WallDesignResult> designResults = new Dictionary<int, WallDesignResult>();

        private void CreateDesignPanel()
        {
            designer = new ShearWallDesigner(null, simpsonCatalog);
            // The standard hold-down list belongs to the app (one list for every project). The company default ships with the
            // program; each user's copy follows it unless the user has changed theirs.
            try
            {
                var (user, company, update) = HoldownStandardList.Startup(HoldownStandardList.DefaultPath, HoldownStandardList.PackagedPath);
                designer.StandardList = user;
                designer.CompanyList = company;
                if (update == HoldownStandardList.CompanyUpdate.NeedsChoice)
                {
                    var answer = MessageBox.Show(this,
                        $"The company hold-down list has been updated to version {company.CompanyVersion}" +
                        (string.IsNullOrEmpty(company.CompanyDate) ? "" : $" ({company.CompanyDate})") + $" ({company.Count} options).\n\n" +
                        $"You have changed your own list ({user.Count} options).\n\n" +
                        "Yes: use the new company list (your changes are replaced).\nNo: keep your list (you can restore the company list " +
                        "later from View > Hold-down catalog).",
                        "Company hold-down list updated", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (answer == MessageBoxResult.Yes) user.RestoreFrom(company);
                    else user.KeepMineOver(company);
                    user.Save(HoldownStandardList.DefaultPath);
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is JsonException)
            {
                MessageBox.Show(this, "The standard hold-down list could not be read and starts empty:\n" + ex.Message, "Hold-down standard list");
            }
            // A new project uses the standard list whenever there is one.
            Calculator.DesignSettings.HoldownsFromStandardList = designer.StandardList.Count > 0;
            designer.StandardList.Changed += (s, e) =>
            {
                try { designer.StandardList.Save(HoldownStandardList.DefaultPath); }
                catch (Exception ex) { MessageBox.Show(this, "The standard hold-down list could not be saved:\n" + ex.Message, "Hold-down standard list"); }
                UpdateShearWallUI();
            };
            geometryPanel = new GeometryPanel();
            geometryPanel.GeometryChanged += (s, e) => UpdateShearWallUI();
            geometryPanel.SelectionChanged += (kind, id) => { selectedKind = kind; selectedId = id; RedrawDrawing(); };
            tabDimensionsTabItem.Content = geometryPanel;
            resultsPanel = new ShearResultsPanel();
            resultsPanel.WallSelected += id => { selectedKind = "Wall"; selectedId = id; RedrawDrawing(); };
            tabCalculationsTabItem.Content = resultsPanel;

            designPanel = new WallDesignPanel();
            designPanel.DesignChanged += (s, e) => UpdateShearWallUI();
            designPanel.AutoSelectRequested += (s, e) => AutoSelectAllWalls();
            tabDesignTabItem.Content = designPanel;
        }

        /// <summary>Checks every wall for its governing shear (called after the wall calculations).</summary>
        private void UpdateDesign()
        {
            designResults = new Dictionary<int, WallDesignResult>();
            if (designPanel == null) return;

            string message = null;
            if (Calculator == null || !Calculator.IsValidForCalculation)
                message = "Draw a diaphragm and walls in both directions first (see the Calculations tab).";
            else
            {
                foreach (var kv in Calculator._wall_system._walls)
                {
                    if (!Calculator.TotalWallShear.TryGetValue(kv.Key, out double v)) continue;
                    Calculator.GoverningLoadCase.TryGetValue(kv.Key, out string governing);
                    designResults[kv.Key] = designer.Check(kv.Key, kv.Value, v, governing, Calculator.DesignSettings);
                }
                if (designResults.Count == 0) message = "No wall shears yet: add loads (LOADS button) or link the wind loads.";
            }
            designPanel.Show(Calculator, designResults, designer, windLoadsLinked, message);
            holdownCatalog?.SetDesign(designResults);
        }

        private Dialogs.HoldownCatalogWindow holdownCatalog;

        /// <summary>The hold-down catalog the design selects from, with the walls using each option (kept current).</summary>
        private void OpenHoldownCatalog_Click(object sender, RoutedEventArgs e)
        {
            if (designer == null) return;
            if (holdownCatalog == null)
            {
                holdownCatalog = new Dialogs.HoldownCatalogWindow(designer) { Owner = this };
                holdownCatalog.Closed += (s, a) => holdownCatalog = null;
            }
            holdownCatalog.SetDesign(designResults);
            holdownCatalog.Show();
            holdownCatalog.Activate();
        }

        /// <summary>The lightest passing edge nailing (then both sides) for every wall, keeping its panel and hold-down type.</summary>
        private void AutoSelectAllWalls()
        {
            if (Calculator == null) return;
            foreach (var kv in Calculator._wall_system._walls)
                if (Calculator.TotalWallShear.TryGetValue(kv.Key, out double v))
                    kv.Value.Design = designer.AutoSelect(kv.Value, v, Calculator.DesignSettings);
            UpdateShearWallUI();
        }

        /// <summary>
        /// Calculations tab: only the table for this calculator type, and until there is something to
        /// calculate, a note saying what to draw (the tables used to stay empty with no explanation).
        /// </summary>
        /// <summary>What to draw next before the walls can be calculated, or null when they can.</summary>
        private string CalculationTodo()
        {
            int walls = Calculator._wall_system?._walls?.Count ?? 0;
            int diaphragms = Calculator._diaphragm_system?._diaphragms?.Count ?? 0;
            int xWalls = Calculator._wall_system?._walls?.Values.Count(w => Math.Abs(w.End.Y - w.Start.Y) < 1e-9) ?? 0;
            int yWalls = walls - xWalls;
            string todo = null;
            if (diaphragms == 0)
                todo = "Draw the floor or roof (diaphragm): click Diaphragm, then click two opposite corners on the drawing.";
            else if (xWalls == 0 || yWalls == 0)
                todo = $"Draw the shear walls: click Wall, then click each wall's start and end. You need walls in both directions, usually two or more each way " +
                       $"(now {xWalls} running W-E and {yWalls} running S-N).";
            else if (Calculator.IsValidForCalculation != true)
                todo = "The walls and diaphragm cannot be calculated yet. Check that the walls sit inside the diaphragm.";
            return todo;
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


        /// <summary>World length of a distance on screen (the drawing is square: one zoom factor).</summary>
        private double PixelsToWorld(double pixels) => pixels / Math.Max(zoomFactorX, 1e-9);

        /// <summary>The object snap at a world point, or the point itself when snaps are off or nothing is in reach.</summary>
        private SnapResult FindSnap(Point world) =>
            snapMode && Calculator != null && (currentMode != DrawMode.None || imageTool.WantsSnaps)
                ? Snapper.Find(world, PixelsToWorld(SnapPixels), Snapper.SegmentsOf(Calculator), snapModes, startPoint_world)
                : new SnapResult { Kind = SnapModes.None, Point = world };

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
            m_layers.AddLayer(42, DrawWallDesign);
            m_layers.AddLayer(52, DrawCursor);
            m_layers.AddLayer(51, DrawPreview);
            m_layers.AddLayer(53, DrawImageTool);
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

                // a new wall starts with the project's default sheathing, if one was set in the Design tab
                Calculator._wall_system.AddWall(new WallData(defaultWallHeight, startPoint.X, startPoint.Y, endPoint.X, endPoint.Y)
                {
                    Design = Calculator.DesignSettings?.NewWallDesign?.Clone(),
                });
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
                Log.Debug(item.ToString());
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

            // create the wind load input control
            var ctrol_wind_input = new WindLoadInputControl(buildingData, windVersion, windLoadParams);
            ctrol_wind_input.WindInputComplete += WindLoadInputControl_WindInputComplete;
            tabWindInputControlTabItem.Content = ctrol_wind_input;
            
            // Recalculate wind for the changed building. (Previously the old calculators were re-sent, so
            // the results tabs and 3D view kept showing the previous building.)
            RecalculateWind();
            SyncWindSetup();

            UpdateShearWallUI();
        }

        private WindSetupControl windSetup;

        /// <summary>The guided wind screen changed: it now holds the inputs for every other tab.</summary>
        private void WindSetup_InputsChanged(object sender, EventArgs e)
        {
            // Unusable input (e.g. a blank width): clear the results everywhere rather than leave old numbers
            // showing; the setup screen says what to fix. The last good inputs are kept.
            if (WindInputValidator.HasErrors(windSetup.Issues) || windSetup.Analysis == null)
            {
                windLoadCalculator_CC = windLoadCalculator_MWFRS_Length = windLoadCalculator_MWFRS_Width = null;
                CreateAndAssignResultControls();
                ApplyLinkedWindLoads();
                UpdateShearWallUI();
                return;
            }

            buildingData = windSetup.Building.Clone();
            windLoadParams = windSetup.Parameters.Clone();
            windVersion = windSetup.Version;

            RecalculateWind();
            UpdateShearWallUI();
        }

        /// <summary>Puts edits made on the classic tabs back into the guided screen (without echoing an event).</summary>
        private void SyncWindSetup()
        {
            if (windSetup == null || buildingData == null || windLoadParams == null) return;
            windSetup.SetInputs(buildingData, windVersion, windLoadParams);
        }

        /// <summary>"Send loads to shear walls": every Fig. 27.3-8 load case goes to the calculator.</summary>
        private void WindSetup_SendLoadsRequested(object sender, WindLateralResult lateral)
        {
            if (Calculator == null || lateral == null) return;
            windLoadsLinked = true;
            ApplyLinkedWindLoads();
            UpdateShearWallUI();
        }

        /// <summary>
        /// True while the shear walls take their loads from the wind calculation (the default). Every wind or
        /// building change then re-sends the load cases, so the walls never keep loads from an earlier
        /// building. Entering a manual load, or opening a file, ends the link; the Wind Setup screen then
        /// shows a warning with a button to link again.
        /// </summary>
        private bool windLoadsLinked = true;

        /// <summary>When linked, replaces the shear-wall load cases with those for the current wind results.</summary>
        private void ApplyLinkedWindLoads()
        {
            if (!windLoadsLinked || Calculator == null) return;

            if (windLoadCalculator_MWFRS_Length == null || windLoadCalculator_MWFRS_Width == null)
            {
                Calculator.SetLoadCases(new List<ShearWallCalculator.LateralLoadCase>()); // no stale loads
                windSetup?.SetShearWallLink(true,
                    "But the wind inputs cannot be calculated right now, so the walls have no wind load. Fix the inputs and the walls update automatically.");
                return;
            }

            var lateral = WindLateralLoads.Compute(
                new WindAnalysisResult(windLoadCalculator_CC, windLoadCalculator_MWFRS_Length, windLoadCalculator_MWFRS_Width),
                windSetup?.OnlyCases1And3 ?? false);
            Calculator.SetLoadCases(lateral.LoadCases
                .Select(c => new ShearWallCalculator.LateralLoadCase(c.Name, c.Vx, c.Vy, c.Mt, applyMinimumEccentricity: false)));
            windSetup?.SetShearWallLink(true,
                $"{lateral.LoadCases.Count} load cases (from W {lateral.AlongLength.DesignKips:0.0} k, from S {lateral.AlongWidth.DesignKips:0.0} k). " +
                "Updated automatically. Per-wall results: 2 Shear walls, Calculations tab.");
        }

        /// <param name="reason">Shown in the warning on the Wind Setup screen.</param>
        private void UnlinkWindLoads(string reason)
        {
            windLoadsLinked = false;
            windSetup?.SetShearWallLink(false, reason);
        }
        /// <summary>
        /// Event listener for when input of the wind loads as been completed -- to save our wind calculators and update the UI
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WindLoadInputControl_WindInputComplete(object sender, WindLoadInputControl.OnWindInputCompleteEventArgs e)
        {
            if(e._windLoadCalculator_CC != null)
            {
                windLoadCalculator_CC = e._windLoadCalculator_CC;
                windVersion = e._windLoadCalculator_CC.ASCEVersion;
                windLoadParams = e._windLoadCalculator_CC.Parameters;
            }

            if(e._windLoadCalculator_MWFRS_Length != null)
            {
                windLoadCalculator_MWFRS_Length = e._windLoadCalculator_MWFRS_Length;
            }

            if(e._windLoadCalculator_MWFRS_Width != null)
            {
                windLoadCalculator_MWFRS_Width = e._windLoadCalculator_MWFRS_Width;
            }

            CreateAndAssignResultControls();
            SyncWindSetup();
            ApplyLinkedWindLoads();

            UpdateShearWallUI();
        }

        /// <summary>
        /// Assigns the three calculators to the appropriate tabs.
        /// Since CC is the same for both, only the CC calculator is used on that tab.
        /// </summary>
        /// <param name="ccControl1"></param>
        /// <param name="mwfrsControl1"></param>
        /// <param name="mwfrsControl2"></param>
        private void CreateAndAssignResultControls()
        {
            // The CC Wind Calcs / MWFRS 1 / MWFRS 2 tabs are retired: the Detailed calculations tab shows the
            // same tables and graphs. (Their TabItems remain in MainWindow.xaml; re-adding the old
            // WindLoadResultsControl_CC / _MWFRS controls here would bring them back.)
            var details = tabWindDetailsTabItem.Content as WindDetailsView ?? new WindDetailsView();
            bool calculated = windLoadCalculator_MWFRS_Length != null && windLoadCalculator_MWFRS_Width != null;
            details.SetResults(windVersion, windLoadParams?.Clone(), buildingData?.Clone(),
                calculated ? new WindAnalysisResult(windLoadCalculator_CC, windLoadCalculator_MWFRS_Length, windLoadCalculator_MWFRS_Width) : null);
            tabWindDetailsTabItem.Content = details;
            TabControlManager.ReAddTab(MainTabControl, "tabWindDetailsTabItem");

            // 3D view: keep one viewer so the camera and selected view survive input changes.
            var viewer = tabWind3DTabItem.Content as Wind3DViewerControl ?? new Wind3DViewerControl();
            viewer.SetResults(buildingData, windLoadCalculator_CC, windLoadCalculator_MWFRS_Length, windLoadCalculator_MWFRS_Width);
            tabWind3DTabItem.Content = viewer;
            TabControlManager.ReAddTab(MainTabControl, "tabWind3DTabItem");
        }

        /// <summary>
        /// Runs all wind calculations for the current building and inputs and refreshes every results tab.
        /// Invalid input (e.g. a zero dimension) clears the results instead of crashing.
        /// </summary>
        private void RecalculateWind()
        {
            if (buildingData == null) return;
            try
            {
                var r = WindAnalysis.Run(windVersion, windLoadParams, buildingData);
                windLoadCalculator_CC = r.Cladding;
                windLoadCalculator_MWFRS_Length = r.MwfrsAlongLength;
                windLoadCalculator_MWFRS_Width = r.MwfrsAlongWidth;
            }
            catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException || ex is NotSupportedException || ex is NotImplementedException)
            {
                windLoadCalculator_CC = windLoadCalculator_MWFRS_Length = windLoadCalculator_MWFRS_Width = null;
            }
            CreateAndAssignResultControls();
            ApplyLinkedWindLoads();
        }

        /// <summary>Opens the 3D wind view in its own resizable window.</summary>
        private void Open3DWindWindow_Click(object sender, RoutedEventArgs e)
        {
            var viewer = new Wind3DViewerControl();
            viewer.SetResults(buildingData, windLoadCalculator_CC, windLoadCalculator_MWFRS_Length, windLoadCalculator_MWFRS_Width);
            new Window
            {
                Title = "3D Wind View",
                Owner = this,
                Width = 1280,
                Height = 800,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = viewer,
            }.Show();
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


                    ctx.DrawText(idLabel, new Point(p1.X - 5, p1.Y));
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

                    ctx.DrawText(idLabel, new Point(p1.X - 12, p1.Y - 5));
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

        private static readonly Pen CrosshairPen = FrozenPen(Color.FromArgb(0x90, 0x52, 0x51, 0x4E), 1);
        private static readonly Pen SnapGlyphPen = FrozenPen(Color.FromRgb(0xE0, 0x8A, 0x00), 2);

        private void DrawCursor(DrawingContext ctx)
        {
            if (!m_layers.IsMouseOver) return;
            Point c = currentMouseScreenPosition;
            ctx.DrawLine(CrosshairPen, new Point(0, c.Y), new Point(dockpanel.ActualWidth, c.Y));
            ctx.DrawLine(CrosshairPen, new Point(c.X, 0), new Point(c.X, dockpanel.ActualHeight));
            if (currentMode == DrawMode.None)
                ctx.DrawRectangle(null, CrosshairPen, new Rect(c.X - 5, c.Y - 5, 10, 10)); // pick box
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
                preview_endPoint_world = currentSnap.Point; // the cursor, or the snap under it
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
        /// <summary>Schedule marks, hold-downs and the schedule over the plan (SHOW Design).</summary>
        private void DrawWallDesign(DrawingContext ctx)
        {
            if (!showDesign || hideShapes || Calculator?._wall_system == null) return;
            DesignOverlay.Draw(ctx, Calculator._wall_system._walls, designResults, p => WorldToScreen(p, m_layers),
                               new Size(m_layers.ActualWidth, m_layers.ActualHeight), VisualTreeHelper.GetDpi(this).PixelsPerDip);
        }

        private static readonly Pen SelectionHalo =FrozenPen(Color.FromArgb(0x70, 0x2A, 0x78, 0xD6), 12);
        private static readonly Pen SelectionOutline = FrozenPen(Color.FromRgb(0x2A, 0x78, 0xD6), 3);

        private static Pen FrozenPen(Color c, double thickness)
        {
            var p = new Pen(new SolidColorBrush(c), thickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
            p.Freeze();
            return p;
        }

        private void DrawShapes(DrawingContext ctx)
        {
            if (hideShapes is true)
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
                    // Blue; on the Shear walls side, green or red once the wall has been checked (Design tab).
                    SolidColorBrush lineStrokeBrush = new SolidColorBrush(
                        designResults.TryGetValue(wall.Key, out var check) ? (check.Pass ? Color.FromRgb(0x1E, 0x8E, 0x3E) : Color.FromRgb(0xC6, 0x28, 0x28)) : Colors.Blue);
                    Pen pen = new Pen(lineStrokeBrush, 4);
                    if (selectedKind == "Wall" && selectedId == wall.Key)
                        ctx.DrawLine(SelectionHalo, p1_screen, p2_screen); // picked in a table
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
                    if (selectedKind == "Diaphragm" && selectedId == rect.Key)
                        ctx.DrawRectangle(null, SelectionOutline, new Rect(insertPoint_screen.X, insertPoint_screen.Y, width, height));

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
            if (Calculator._wall_system is null || Calculator._wall_system.BWL_Manager is null)
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
        /// <summary>The active object snap: an AutoCAD-style glyph at the snapped point and its name.</summary>
        private void DrawSnapMarkers(DrawingContext ctx)
        {
            if (!currentSnap.Found || !m_layers.IsMouseOver) return;
            Point p = WorldToScreen(currentSnap.Point, m_layers);
            const double h = 6;
            switch (currentSnap.Kind)
            {
                case SnapModes.Endpoint:
                    ctx.DrawRectangle(null, SnapGlyphPen, new Rect(p.X - h, p.Y - h, 2 * h, 2 * h));
                    break;
                case SnapModes.Midpoint:
                    var tri = new StreamGeometry();
                    using (var g = tri.Open())
                    {
                        g.BeginFigure(new Point(p.X, p.Y - h - 1), false, true);
                        g.LineTo(new Point(p.X + h + 1, p.Y + h), true, false);
                        g.LineTo(new Point(p.X - h - 1, p.Y + h), true, false);
                    }
                    ctx.DrawGeometry(null, SnapGlyphPen, tri);
                    break;
                case SnapModes.Intersection:
                    ctx.DrawLine(SnapGlyphPen, new Point(p.X - h, p.Y - h), new Point(p.X + h, p.Y + h));
                    ctx.DrawLine(SnapGlyphPen, new Point(p.X - h, p.Y + h), new Point(p.X + h, p.Y - h));
                    break;
                case SnapModes.Perpendicular:
                    ctx.DrawLine(SnapGlyphPen, new Point(p.X - h, p.Y + h), new Point(p.X + h, p.Y + h));
                    ctx.DrawLine(SnapGlyphPen, new Point(p.X - h, p.Y + h), new Point(p.X - h, p.Y - h));
                    ctx.DrawLine(SnapGlyphPen, new Point(p.X - h, p.Y), new Point(p.X, p.Y));
                    ctx.DrawLine(SnapGlyphPen, new Point(p.X, p.Y), new Point(p.X, p.Y + h));
                    break;
                case SnapModes.Nearest:
                    ctx.DrawLine(SnapGlyphPen, new Point(p.X - h, p.Y - h), new Point(p.X + h, p.Y - h));
                    ctx.DrawLine(SnapGlyphPen, new Point(p.X - h, p.Y + h), new Point(p.X + h, p.Y + h));
                    ctx.DrawLine(SnapGlyphPen, new Point(p.X - h, p.Y - h), new Point(p.X + h, p.Y + h));
                    ctx.DrawLine(SnapGlyphPen, new Point(p.X + h, p.Y - h), new Point(p.X - h, p.Y + h));
                    break;
            }
            var label = new FormattedText(SnapResult.Describe(currentSnap.Kind), CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                new Typeface("Segoe UI"), 11, Brushes.White, VisualTreeHelper.GetDpi(this).PixelsPerDip);
            var box = new Rect(p.X + 10, p.Y + 8, label.Width + 8, label.Height + 2);
            ctx.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(0xE6, 0x30, 0x30, 0x30)), null, box, 3, 3);
            ctx.DrawText(label, new Point(box.X + 4, box.Y + 1));
        }

        private void DrawCOMandCOR(DrawingContext ctx)
        {
            if (Calculator is null) return;

            // Draw a marker for the center of mass
            if (Calculator != null)
            {
                if (Calculator._diaphragm_system != null)
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
        // ------------------------------------------------------------------ plan image

        /// <summary>The plan image under the drawing: image pixels → world (scale, rotation, position) → screen.</summary>
        private void DrawReferenceImage(DrawingContext ctx)
        {
            var img = Calculator?.Image;
            if (hideImage || img == null || !img.Visible) return;
            var bmp = PlanBitmap(img);
            if (bmp == null) return;
            try
            {
                Point o = WorldToScreen(new Point(0, 0), m_layers), ex = WorldToScreen(new Point(1, 0), m_layers), ey = WorldToScreen(new Point(0, 1), m_layers);
                var m = img.PixelToWorldMatrix();
                m.Append(new Matrix(ex.X - o.X, ex.Y - o.Y, ey.X - o.X, ey.Y - o.Y, o.X, o.Y));
                ctx.PushTransform(new MatrixTransform(m));
                ctx.PushOpacity(Math.Max(0.05, Math.Min(1, img.Opacity)));
                ctx.DrawImage(bmp, new Rect(0, 0, img.PixelWidth, img.PixelHeight));
                ctx.Pop();
                ctx.Pop();
            }
            catch (Exception ex)
            {
                if (!imageDrawWarned) Log.Warning("Could not draw the plan image", ex);
                imageDrawWarned = true;
            }
        }

        /// <summary>The decoded image file, loaded once per path; null (with one warning) when it cannot be found or read.</summary>
        private BitmapSource PlanBitmap(ReferenceImage img)
        {
            string path = img.Resolve(currentProjectPath);
            if (path == null)
            {
                if (planBitmapPath != img.Path)
                {
                    planBitmapPath = img.Path;
                    planBitmap = null;
                    Log.Warning("The plan image was not found: " + img.Path + " (put it next to the project file, or import it again).");
                }
                return null;
            }
            if (planBitmap != null && planBitmapPath == path) return planBitmap;
            planBitmapPath = path;
            planBitmap = null;
            try
            {
                var b = new BitmapImage();
                b.BeginInit();
                b.UriSource = new Uri(path, UriKind.Absolute);
                b.CacheOption = BitmapCacheOption.OnLoad;
                b.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                b.EndInit();
                b.Freeze();
                planBitmap = b;
                img.PixelWidth = b.PixelWidth;
                img.PixelHeight = b.PixelHeight;
            }
            catch (Exception ex)
            {
                Log.Warning("Could not read the plan image " + path, ex);
            }
            return planBitmap;
        }

        /// <summary>The image ▾ menu: import, the two-point commands, opacity, lock, remove.</summary>
        private void btnImageOptions_Click(object sender, RoutedEventArgs e)
        {
            var img = Calculator?.Image;
            bool movable = img != null && !img.Locked;
            var menu = new ContextMenu { PlacementTarget = (UIElement)sender, Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom };
            void Item(string header, Action act, bool enabled = true, string tip = null)
            {
                var mi = new MenuItem { Header = header, IsEnabled = enabled, ToolTip = tip };
                mi.Click += (s, a) => act();
                menu.Items.Add(mi);
            }

            Item(img == null ? "Import plan image..." : "Replace plan image...", ImportPlanImage);
            menu.Items.Add(new Separator());
            Item("Set scale: click two points, type the distance", () => StartImageCommand(ImageCommand.Calibrate), movable,
                 "Pick the two ends of a long dimension on the image; the image is rescaled about the first point.");
            Item("Straighten: click two points on a wall line", () => StartImageCommand(ImageCommand.Straighten), movable,
                 "Turns the image so that line runs exactly E-W or N-S.");
            Item("Move: base point, then destination", () => StartImageCommand(ImageCommand.Move), movable,
                 "The destination snaps to the walls, e.g. to line up a building corner with a wall end.");
            Item("Rotate 90°", () => { img.RotateAbout(Center(img.WorldBounds()), 90); ImageChanged(); }, movable);
            menu.Items.Add(new Separator());
            var opacity = new MenuItem { Header = "Opacity", IsEnabled = img != null };
            foreach (double v in new[] { 0.25, 0.4, 0.6, 0.8, 1.0 })
            {
                var mi = new MenuItem { Header = $"{v * 100:0} %", IsCheckable = true, IsChecked = img != null && Math.Abs(img.Opacity - v) < 0.01 };
                mi.Click += (s, a) => { img.Opacity = v; ImageChanged(); };
                opacity.Items.Add(mi);
            }
            menu.Items.Add(opacity);
            Item(img?.Locked == true ? "Unlock" : "Lock (no moving or rescaling)", () => { img.Locked = !img.Locked; ImageChanged(); }, img != null);
            Item("Zoom to image", ZoomToImage, img != null);
            menu.Items.Add(new Separator());
            Item("Remove image", RemovePlanImage, img != null);
            menu.IsOpen = true;
        }

        private void ImportPlanImage_Click(object sender, RoutedEventArgs e) => ImportPlanImage();

        private void ImportPlanImage()
        {
            if (Calculator == null) return;
            var open = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Import plan image",
                Filter = "Images (*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff;*.gif|All files (*.*)|*.*",
            };
            if (open.ShowDialog(this) != true) return;

            ImportImageDialog dialog;
            try
            {
                dialog = new ImportImageDialog(open.FileName, DrawingWidthFeet()) { Owner = this };
            }
            catch (Exception ex)
            {
                Log.Warning("Could not read the image " + open.FileName, ex);
                MessageBox.Show(this, "This file could not be read as an image.\n\n" + ex.Message, "Import plan image", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (dialog.ShowDialog() != true) return;

            Calculator.Image = dialog.Result;
            planBitmap = null;
            imageDrawWarned = false;
            hideImage = false;
            btnHideImage.Tag = "on";
            Log.Info("Plan image imported: " + open.FileName);
            ZoomToImage();
            if (dialog.CalibrateOnDrawing) StartImageCommand(ImageCommand.Calibrate);
            ImageChanged();
        }

        private void RemovePlanImage()
        {
            if (Calculator?.Image == null) return;
            if (MessageBox.Show(this, "Remove the plan image from this project? (The image file is not deleted.)", "Remove image",
                                MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes) return;
            Calculator.Image = null;
            planBitmap = null;
            imageTool.Cancel();
            ImageChanged();
        }

        private void StartImageCommand(ImageCommand command)
        {
            if (Calculator?.Image == null) return;
            if (Calculator.Image.Locked)
            {
                ShowStatusMessage(LogLevel.Warning, "The image is locked: Image ▾ > Unlock first.");
                return;
            }
            ResetInputMode();
            currentMode = DrawMode.None;
            if (selectionCardOpen) CloseSelectionCard();
            SetButtonModes();
            imageTool.Begin(command);
            hideImage = false;
            Calculator.Image.Visible = true;
            btnHideImage.Tag = "on";
            RedrawDrawing();
        }

        /// <summary>A click while an image command is active.</summary>
        private void ImageToolClick(Point world)
        {
            Point p = imageTool.WantsSnaps ? FindSnap(world).Point : world;
            string message = imageTool.Click(Calculator.Image, p, out bool warning);
            if (message != null) ShowStatusMessage(warning ? LogLevel.Warning : LogLevel.Info, message);
            if (imageTool.WaitingForDistance)
            {
                tbCommand.Clear();
                tbCommand.Focus();
            }
            ImageChanged();
        }

        /// <summary>Picked points and the rubber band of an image command.</summary>
        private void DrawImageTool(DrawingContext ctx)
        {
            if (!imageTool.Active) return;
            var pen = new Pen(new SolidColorBrush(Color.FromRgb(0xD9, 0x5F, 0x00)), 1.5) { DashStyle = DashStyles.Dash };
            var dot = new SolidColorBrush(Color.FromRgb(0xD9, 0x5F, 0x00));
            Point? last = null;
            foreach (var p in imageTool.Points)
            {
                Point s = WorldToScreen(p, m_layers);
                ctx.DrawEllipse(dot, null, s, 4, 4);
                if (last != null) ctx.DrawLine(pen, last.Value, s);
                last = s;
            }
            if (last != null && !imageTool.WaitingForDistance)
                ctx.DrawLine(pen, last.Value, WorldToScreen(currentSnap.Point, m_layers));
        }

        private void ImageChanged()
        {
            LogModelChanges();
            ShowImageName();
            RedrawDrawing();
        }

        private void ShowImageName()
        {
            var img = Calculator?.Image;
            if (img == null)
            {
                tbImageFileName.Text = "";
                tbImageFileName.ToolTip = null;
                return;
            }
            tbImageFileName.Text = "Image: " + System.IO.Path.GetFileName(img.Path) + (img.Calibrated ? "" : "  (scale not set)") + (img.Locked ? "  (locked)" : "");
            tbImageFileName.ToolTip = img.Path + Environment.NewLine +
                                      $"{img.FeetPerPixel * 12:0.###} in per pixel, turned {img.RotationDegrees:0.##}°, opacity {img.Opacity * 100:0} %";
        }

        /// <summary>Width of what is drawn (ft), for the first size of an uncalibrated image; 60 ft when nothing is drawn.</summary>
        private double DrawingWidthFeet()
        {
            var r = Rect.Empty;
            if (Calculator?._diaphragm_system != null)
                foreach (var d in Calculator._diaphragm_system._diaphragms.Values) { r.Union(d.P1); r.Union(d.P3); }
            if (Calculator?._wall_system != null)
                foreach (var w in Calculator._wall_system._walls.Values) { r.Union(w.Start); r.Union(w.End); }
            return r.IsEmpty || r.Width < 1 ? 60 : r.Width;
        }

        private static Point Center(Rect r) => new Point(r.X + r.Width / 2, r.Y + r.Height / 2);

        /// <summary>Zooms and pans so the whole image shows, with a margin.</summary>
        private void ZoomToImage()
        {
            var img = Calculator?.Image;
            if (img == null || img.PixelWidth <= 0 || m_layers.ActualWidth <= 0) return;
            Rect b = img.WorldBounds();
            if (b.Width <= 0 || b.Height <= 0) return;
            double W = m_layers.ActualWidth, H = m_layers.ActualHeight;
            double z = 0.92 * Math.Min(W / b.Width, H / b.Height);
            zoomFactorX = zoomFactorY = z;
            Point c = Center(b);
            panOffsetX = c.X - W / (2 * z);
            panOffsetY = H / (2 * z) - H + c.Y;
            InvalidateGrid();
            RedrawDrawing();
        }

        /// <summary>After New or Open: the project's image (older projects moved over), shown as it was saved.</summary>
        /// <summary>The model as last logged; each change is logged as its differences from it.</summary>
        private ModelSnapshot loggedModel;

        /// <summary>
        /// Logs what changed in the model since the last call (walls and diaphragms added, deleted or resized; sheathing,
        /// nailing, framing, dead load, hold-down type; design settings; loads; the plan image), whichever panel changed it.
        /// </summary>
        private void LogModelChanges()
        {
            try
            {
                var now = ModelSnapshot.Take(Calculator, designer);
                if (loggedModel == null)
                    Log.Info($"Model: {now.WallGeometry.Count} walls, {now.Diaphragms.Count} diaphragms" + (now.Image.Count > 0 ? ", plan image " + now.Image["file"] : ""));
                else
                    foreach (string line in ModelChangeLog.Differences(loggedModel, now))
                        Log.Info(line);
                loggedModel = now;
            }
            catch (Exception ex)
            {
                Log.Warning("Could not log the model changes", ex);
            }
        }

        private void ImageForNewProject()
        {
            MigrateOlderImage();
            planBitmap = null;
            planBitmapPath = null;
            imageDrawWarned = false;
            loggedModel = null; // a different project: start the change log afresh
            imageTool.Cancel();
            hideImage = Calculator?.Image != null && !Calculator.Image.Visible;
            btnHideImage.Tag = hideImage ? null : "on";
        }

        /// <summary>Older projects kept the image as a path and one scale; move them into the plan image.</summary>
        private void MigrateOlderImage()
        {
            if (Calculator == null || Calculator.Image != null || string.IsNullOrEmpty(Calculator.selectedImageFilePath)) return;
            string path = Calculator.selectedImageFilePath;
            int w = 0, h = 0;
            try
            {
                var f = BitmapFrame.Create(new Uri(path, UriKind.Absolute), BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
                w = f.PixelWidth;
                h = f.PixelHeight;
            }
            catch (Exception ex)
            {
                Log.Warning("The plan image of this older project could not be read: " + path, ex);
            }
            Calculator.Image = ReferenceImage.FromOlderProject(path, Calculator.pixelScaleX, h);
            Calculator.Image.PixelWidth = w;
            Calculator.selectedImageFilePath = null;
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
            // While a wind tab hides the drawing area it has no actual size; use its fixed design size instead.
            double width = dockpanel.ActualWidth > 0 ? dockpanel.ActualWidth : dockpanel.Width;
            double height = dockpanel.ActualHeight > 0 ? dockpanel.ActualHeight : dockpanel.Height;
            if (!(width >= 1 && height >= 1)) return;

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
            if (e.RightButton == MouseButtonState.Pressed)
            {
                ResetInputMode(); // right-click cancels the shape in progress
                RedrawDrawing();
                return;
            }
            if (e.ChangedButton != MouseButton.Left || Calculator == null) return;

            Point world = ScreenToWorld(e.GetPosition(m_layers), m_layers);
            if (imageTool.Active)
            {
                ImageToolClick(world);
                return;
            }
            if (currentMode == DrawMode.None)
            {
                PickAt(world);
                return;
            }
            ApplyPoint(FindSnap(world).Point);
        }

        /// <summary>A clicked or typed point for the Wall or Diaphragm tool: the first point starts the shape, the second finishes it.</summary>
        private void ApplyPoint(Point p)
        {
            if (startPoint_world == null)
            {
                startPoint_world = p;
                CreatePreviewShape();
                currentSnap = FindSnap(currentSnap.Point); // perpendicular now has a start point
                RedrawDrawing();
                return;
            }

            Point start = startPoint_world.Value;
            if (currentMode == DrawMode.Line)
            {
                Point squared = GetConstrainedPoint(p, start);
                if ((squared - start).Length < 1e-6)
                {
                    ShowStatusMessage(LogLevel.Warning, "A wall needs two different ends.");
                    return;
                }
                if ((squared - p).Length > 1e-6)
                    ShowStatusMessage(LogLevel.Info, $"Walls run E-W or N-S: the end was squared to ({squared.X:0.##}, {squared.Y:0.##}).");
            }
            else if (Math.Abs(p.X - start.X) < 1e-6 || Math.Abs(p.Y - start.Y) < 1e-6)
            {
                ShowStatusMessage(LogLevel.Warning, "A diaphragm needs two opposite corners (different x and y).");
                return;
            }

            endPoint_world = p;
            try
            {
                FinalizeShape(p);
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException))
            {
                Log.Warning("The shape could not be added", ex);
                ResetInputMode();
            }
            UpdateShearWallUI();
        }

        /// <summary>Select tool: picks the wall or diaphragm under the point and shows its card.</summary>
        private void PickAt(Point world)
        {
            var (kind, id) = Snapper.Pick(Calculator, world, PixelsToWorld(6));
            selectedKind = kind;
            selectedId = id;
            selectionCardOpen = kind != null;
            UpdateShearWallUI(); // the tables follow the selection
        }

        private void CloseSelectionCard()
        {
            selectionCardOpen = false;
            selectedKind = null;
            selectedId = -1;
            UpdateShearWallUI();
        }

        private void SelectionCard_OpenDesign(string kind, int id)
        {
            MainTabControl.SelectedItem = tabDesignTabItem;
            designPanel?.SelectWall(id);
        }

        private void SelectionCard_EditGeometry(string kind, int id) => MainTabControl.SelectedItem = tabDimensionsTabItem;

        private void SelectionCard_Delete(string kind, int id)
        {
            string name = kind == "Wall" ? $"wall {id}" : $"diaphragm {id}";
            if (MessageBox.Show(this, $"Delete {name}?", "Delete", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes)
                return;
            if (kind == "Wall") Calculator._wall_system.DeleteWall(id);
            else Calculator._diaphragm_system.DeleteDiaphragm(id);
            CloseSelectionCard();
        }

        // ------------------------------------------------------------------ typed points

        /// <summary>Typing a number, @, - or . while drawing (and not in a box) goes to the Point box.</summary>
        private void RouteTypingToCommand(object sender, TextCompositionEventArgs e)
        {
            if (InWindWorkspace || currentMode == DrawMode.None || string.IsNullOrEmpty(e.Text)) return;
            if (e.OriginalSource is System.Windows.Controls.Primitives.TextBoxBase) return;
            if ("0123456789.-@".IndexOf(e.Text[0]) < 0) return;
            tbCommand.Focus();
            tbCommand.Text += e.Text;
            tbCommand.CaretIndex = tbCommand.Text.Length;
            e.Handled = true;
        }

        private void Command_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                tbCommand.Clear();
                ResetInputMode();
                RedrawDrawing();
                Keyboard.ClearFocus();
                e.Handled = true;
                return;
            }
            if (e.Key != Key.Enter) return;
            e.Handled = true;
            if (imageTool.WaitingForDistance)
            {
                if (imageTool.EnterDistance(Calculator?.Image, tbCommand.Text, out string done))
                {
                    tbCommand.Clear();
                    Keyboard.ClearFocus();
                    ShowStatusMessage(LogLevel.Info, done);
                    ImageChanged();
                }
                else
                {
                    ShowStatusMessage(LogLevel.Warning, done);
                    tbCommand.SelectAll();
                }
                return;
            }
            if (currentMode == DrawMode.None)
            {
                ShowStatusMessage(LogLevel.Warning, "Pick Wall (L) or Diaphragm (R) first, then type its points.");
                return;
            }
            if (!CoordinateInput.TryParse(tbCommand.Text, startPoint_world, currentSnap.Point, out Point p, out string error))
            {
                ShowStatusMessage(LogLevel.Warning, error);
                tbCommand.SelectAll();
                return;
            }
            tbCommand.Clear();
            ApplyPoint(p);
        }

        private void m_layers_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            currentMouseScreenPosition = e.GetPosition(m_layers);
            currentSnap = FindSnap(ScreenToWorld(currentMouseScreenPosition, m_layers));
            ShowCursor(currentSnap.Point);
            RedrawDrawing();
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

            ShowCursor(afterZoom);
            InvalidateGrid();               // signal that the grid needs updating
            RedrawDrawing();
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
        private void btnHideShapes_Click(object sender, RoutedEventArgs e)
        {
            hideShapes = !hideShapes;
            UpdateShearWallUI();
            btnHideShapes.Tag = hideShapes ? null : "on";

        }

        private void btnShowDesign_Click(object sender, RoutedEventArgs e)
        {
            showDesign = !showDesign;
            btnShowDesign.Tag = showDesign ? "on" : null;
            RedrawDrawing();
        }

        private void btnHideImage_Click(object sender, RoutedEventArgs e)
        {
            hideImage = !hideImage;
            if (Calculator?.Image != null) Calculator.Image.Visible = !hideImage; // saved with the project
            btnHideImage.Tag = hideImage ? null : "on";
            RedrawDrawing();
        }

        private void btnHideGrid_Click(object sender, RoutedEventArgs e)
        {
            hideGrid = !hideGrid;
            UpdateShearWallUI();
            btnHideGrid.Tag = hideGrid ? null : "on";
        }

        private void btnLineMode_Click(object sender, RoutedEventArgs e)
        {
            currentMode = DrawMode.Line;
            ResetInputMode();
            SetButtonModes();
        }

        private void btnRectangleMode_Click(object sender, RoutedEventArgs e)
        {
            currentMode = DrawMode.Rectangle;
            ResetInputMode();
            SetButtonModes();
        }

        private void btnSnapMode_Click(object sender, RoutedEventArgs e)
        {
            SetSnapMode();
        }

        private void btnSelectMode_Click(object sender, RoutedEventArgs e)
        {
            currentMode = DrawMode.None;
            ResetInputMode();
            SetButtonModes();
            RedrawDrawing();
        }

        /// <summary>The ▾ next to Snap: tick the object snaps to use.</summary>
        private void btnSnapOptions_Click(object sender, RoutedEventArgs e)
        {
            var menu = new ContextMenu { PlacementTarget = btnSnapOptions, Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom };
            var on = new MenuItem { Header = "Snaps on  (F3)", IsCheckable = true, IsChecked = snapMode, StaysOpenOnClick = true };
            on.Click += (s, a) => SetSnapMode();
            menu.Items.Add(on);
            menu.Items.Add(new Separator());
            foreach (var kind in new[] { SnapModes.Endpoint, SnapModes.Midpoint, SnapModes.Intersection, SnapModes.Perpendicular, SnapModes.Nearest })
            {
                var k = kind;
                var item = new MenuItem { Header = SnapResult.Describe(k) + "   " + SnapGlyphHint(k), IsCheckable = true, IsChecked = (snapModes & k) != 0, StaysOpenOnClick = true };
                item.Click += (s, a) =>
                {
                    snapModes = item.IsChecked ? snapModes | k : snapModes & ~k;
                    if (snapModes != SnapModes.None && !snapMode) { snapMode = true; on.IsChecked = true; SetButtonModes(); }
                };
                menu.Items.Add(item);
            }
            menu.IsOpen = true;
        }

        private static string SnapGlyphHint(SnapModes k) =>
            k == SnapModes.Endpoint ? "□" : k == SnapModes.Midpoint ? "△" : k == SnapModes.Intersection ? "×" : k == SnapModes.Perpendicular ? "⊥" : "⧗";

        private void btnOpenLoadDialog_Click(object sender, RoutedEventArgs e)
        {
            if (Calculator == null)
            {
                Log.Warning("Loads dialog: no calculator is open.");
                return;
            }
            windSetup?.FlushPendingChange();
            var dialog = new LoadInputDialog(buildingData, windLoadParams, windVersion, Calculator.V_x, Calculator.V_y,
                                             windSetup?.OnlyCases1And3 ?? false, manualNow: !windLoadsLinked)
            {
                Owner = this
            };

            bool? ok = dialog.ShowDialog();
            if (dialog.EditWindRequested)
            {
                rbWorkspaceWind.IsChecked = true;
                MainTabControl.SelectedItem = tabWindSetupTabItem;
                return;
            }
            if (ok == true)
            {
                // ASCE wind: all Fig. 27.3-8 load cases (each wall uses its governing case); manual: one load.
                if (dialog.LoadCases != null && dialog.LoadCases.Count > 0)
                {
                    // ASCE wind: stays linked, so later wind changes update the walls automatically.
                    Calculator.SetLoadCases(dialog.LoadCases);
                    windSetup?.SetOnlyCases1And3(dialog.OnlyCases1And3);
                    windLoadsLinked = true;
                    ApplyLinkedWindLoads();
                }
                else
                {
                    // A manual load is the user's own number; never overwrite it, but say so on the wind screen.
                    Calculator.AddLoads(dialog.MagnitudeX, dialog.MagnitudeY);
                    UnlinkWindLoads($"They use your manual load (X = {dialog.MagnitudeX:0.##} k, Y = {dialog.MagnitudeY:0.##} k). " +
                                    "Click the button below to use the wind loads instead.");
                }
                UpdateShearWallUI();  // update the calculator
            }
        }

        #endregion

        #region Menu and Key Events
        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            // The drawing shortcuts belong to the Shear walls workspace, and never to a key typed into a box
            // (typing "d" in a wind input used to toggle debug mode with a message box).
            if (e.Handled || InWindWorkspace || Keyboard.Modifiers != ModifierKeys.None) return;
            if (e.OriginalSource is System.Windows.Controls.Primitives.TextBoxBase || e.OriginalSource is PasswordBox || (e.OriginalSource is ComboBox cb && cb.IsEditable)) return;
            if (e.Key != Key.L && e.Key != Key.R && e.Key != Key.S && e.Key != Key.D && e.Key != Key.Z && e.Key != Key.Escape && e.Key != Key.F3) return;
            Log.Debug($"Key pressed: {e.Key}");

            // Esc: cancel the shape in progress; then close the card; then the Select tool
            if (e.Key == Key.Escape)
            {
                if (imageTool.Active) { imageTool.Cancel(); RedrawDrawing(); }
                else                 if (startPoint_world != null) { ResetInputMode(); RedrawDrawing(); }
                else if (selectionCardOpen) CloseSelectionCard();
                else { currentMode = DrawMode.None; currentSnap = FindSnap(currentSnap.Point); SetButtonModes(); RedrawDrawing(); }
                return;
            }
            if (e.Key == Key.F3)
            {
                SetSnapMode();
                RedrawDrawing();
                return;
            }

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

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close(); // through Closing, so unsaved changes are offered for saving
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            UpdateShearWallUI();
        }

        private void OpenLogFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Directory.CreateDirectory(Log.Folder);
                System.Diagnostics.Process.Start("explorer.exe", "\"" + Log.Folder + "\"");
            }
            catch (Exception ex)
            {
                Log.Warning("Could not open the log folder", ex);
                MessageBox.Show("The log folder is:\n" + Log.Folder, "Log folder", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                $"Shear Wall Calculator {typeof(App).Assembly.GetName().Version}\n\n" +
                "Wind: ASCE 7 (linked Wind Load Calculator)\nWood shear walls: AWC SDPWS (tables 2015 edition)\nHold-downs: Simpson Strong-Tie catalog values\n\n" +
                "Log: " + Log.Folder,
                "About", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ------------------------------------------------------------------ project files

        private const string ProjectFilter = "Shear wall project (*.swc.json)|*.swc.json|Older drawing (*.json)|*.json|All files (*.*)|*.*";

        /// <summary>The open project file, or null for a new project that has not been saved.</summary>
        private string currentProjectPath;

        /// <summary>The project text when last opened or saved; any difference means unsaved changes.</summary>
        private string savedProjectJson;

        private WindInputs CurrentWindInputs() => windSetup == null ? null
            : WindInputs.From(windSetup.Version, windSetup.Parameters, windSetup.Building, windSetup.OnlyCases1And3,
                              windSetup.RoofComponent, windSetup.WallComponent);

        private string CurrentProjectJson() => ProjectSerializer.ToJson(Calculator, CurrentWindInputs(), windLoadsLinked);

        private bool HasUnsavedChanges => savedProjectJson != null && Calculator != null && CurrentProjectJson() != savedProjectJson;

        private string ProjectName => currentProjectPath == null
            ? "Untitled"
            : System.IO.Path.GetFileName(currentProjectPath).Replace(".swc.json", "");

        /// <summary>Title bar: the project name, with * when there are unsaved changes.</summary>
        private void UpdateTitle()
        {
            if (windSetup == null) return;
            Title = $"{ProjectName}{(HasUnsavedChanges ? " *" : "")} - Shear Wall Calculator";
        }

        /// <summary>Remembers the current state as saved (after New, Open and Save).</summary>
        private void MarkSaved()
        {
            savedProjectJson = CurrentProjectJson();
            UpdateTitle();
        }

        /// <summary>Asks to save unsaved changes. False means the user cancelled.</summary>
        private bool ConfirmDiscardOrSave()
        {
            if (!HasUnsavedChanges) return true;
            var answer = MessageBox.Show(this, $"Save the changes to {ProjectName}?", "Shear Wall Calculator",
                                         MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (answer == MessageBoxResult.Cancel) return false;
            if (answer == MessageBoxResult.Yes) return SaveProject(saveAs: false);
            return true;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!ConfirmDiscardOrSave()) e.Cancel = true;
        }

        private void FileShortcuts(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers != ModifierKeys.Control) return;
            switch (e.Key)
            {
                case Key.N: MenuItem_New_Click(this, null); break;
                case Key.O: MenuItem_Load_Click(this, null); break;
                case Key.S: MenuItem_Save_Click(this, null); break;
                case Key.P: MenuItem_PrintReport_Click(this, null); break;
                default: return;
            }
            e.Handled = true;
        }

        private void MenuItem_New_Click(object sender, RoutedEventArgs e)
        {
            if (!ConfirmDiscardOrSave()) return;
            Calculator = new ShearWallCalculator_RigidDiaphragm();
            Calculator.DesignSettings.HoldownsFromStandardList = designer != null && designer.StandardList.Count > 0;
            currentProjectPath = null;
            ImageForNewProject();
            Log.Info("New project");
            windSetup.SetInputs(null, ASCE7_Versions.ASCE_VER_7_22, null);
            windSetup.SetOnlyCases1And3(false);
            buildingData = windSetup.Building.Clone();
            windLoadParams = windSetup.Parameters.Clone();
            windVersion = windSetup.Version;
            windLoadsLinked = true;
            RecalculateWind();
            UpdateShearWallUI();
            MarkSaved();
        }

        private void MenuItem_Save_Click(object sender, RoutedEventArgs e) => SaveProject(saveAs: false);

        private void MenuItem_SaveAs_Click(object sender, RoutedEventArgs e) => SaveProject(saveAs: true);

        /// <summary>Saves the wind inputs, the drawing and the loads in one file. False if cancelled or failed.</summary>
        private bool SaveProject(bool saveAs)
        {
            windSetup?.FlushPendingChange(); // a value still being typed counts
            string path = currentProjectPath;
            // An older drawing file is saved as a new project file rather than overwritten in the new format.
            if (saveAs || path == null || !path.EndsWith(".swc.json", StringComparison.OrdinalIgnoreCase))
            {
                var dialog = new SaveFileDialog
                {
                    Filter = ProjectFilter,
                    FileName = (currentProjectPath == null ? "project" : System.IO.Path.GetFileNameWithoutExtension(ProjectName)) + ".swc.json",
                    AddExtension = false,
                };
                if (dialog.ShowDialog(this) != true) return false;
                path = dialog.FileName;
                if (!path.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) path += ".swc.json";
            }

            try
            {
                RecordHoldowns();
                ProjectSerializer.Save(path, Calculator, CurrentWindInputs(), windLoadsLinked);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                MessageBox.Show(this, "The project could not be saved:\n" + ex.Message, "Save", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            currentProjectPath = path;
            AddToRecentFiles(path);
            Log.Info("Project saved: " + path);
            MarkSaved();
            return true;
        }

        private void MenuItem_Load_Click(object sender, RoutedEventArgs e)
        {
            if (!ConfirmDiscardOrSave()) return;
            var dialog = new OpenFileDialog { Filter = "Shear wall files (*.json)|*.json|All files (*.*)|*.*" };
            if (dialog.ShowDialog(this) == true)
                OpenProject(dialog.FileName);
        }

        /// <summary>
        /// Opens a project file (wind inputs, drawing, loads) or an older drawing-only file. A project whose walls
        /// followed the wind loads is re-linked; if the wind calculation now gives different loads (for example
        /// after a program correction), the user is told.
        /// </summary>
        private void OpenProject(string path)
        {
            ProjectContents contents;
            try
            {
                contents = ProjectSerializer.Load(path);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is InvalidDataException || ex is JsonException)
            {
                MessageBox.Show(this, "The file could not be opened:\n" + ex.Message, "Open", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Calculator = contents.Calculator;
            currentProjectPath = path;
            ImageForNewProject();
            Log.Info("Project opened: " + path);
            string note = null;

            if (contents.IsOlderDrawingFile)
            {
                // Older files have no wind inputs: keep the walls on the loads saved in the file.
                UnlinkWindLoads("They use the loads saved in this older drawing file, which has no wind inputs. " +
                                "Enter the wind inputs here and click the button below to use the wind loads instead.");
                note = "This is an older drawing file: it has the walls and loads but no wind inputs. " +
                       "Saving it creates a project file that keeps both.";
            }
            else
            {
                var wind = contents.Wind;
                windSetup.SetInputs(wind.ToBuilding(), wind.Edition, wind.ToParameters());
                windSetup.SetComponents(wind.RoofComponent, wind.WallComponent);
                windSetup.SetOnlyCases1And3(wind.OnlyCases1And3);
                buildingData = windSetup.Building.Clone();
                windLoadParams = windSetup.Parameters.Clone();
                windVersion = windSetup.Version;

                var saved = contents.Loads ?? new LoadsInfo();
                windLoadsLinked = saved.LinkedToWind;
                if (!windLoadsLinked)
                    UnlinkWindLoads($"They use the manual load saved in this project (X = {Calculator.V_x:0.##} k, Y = {Calculator.V_y:0.##} k). " +
                                    "Click the button below to use the wind loads instead.");
                RecalculateWind(); // when linked, this re-sends the wind load cases

                if (windLoadsLinked && (Changed(saved.Vx, Calculator.V_x) || Changed(saved.Vy, Calculator.V_y)))
                    note = $"The wind loads differ from when this project was saved: X {saved.Vx:0.00} → {Calculator.V_x:0.00} k, " +
                           $"Y {saved.Vy:0.00} → {Calculator.V_y:0.00} k. The walls now use the new loads " +
                           "(usually a correction in the wind calculation since then). Save to keep them.";
            }

            AddToRecentFiles(path);
            UpdateShearWallUI();
            string holdownNote = HoldownChangesSinceSave();
            if (holdownNote != null) note = note == null ? holdownNote : note + "\n\n" + holdownNote;
            // An older file, or loads or hold-downs that changed, count as unsaved so the user is asked before closing.
            savedProjectJson = note == null ? CurrentProjectJson() : string.Empty;
            UpdateTitle();
            if (note != null)
                MessageBox.Show(this, note, "Open", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>Stores in the project which hold-down list (version) the design used and each wall's hold-down.</summary>
        private void RecordHoldowns()
        {
            if (Calculator?.DesignSettings == null || designer == null) return;
            Calculator.DesignSettings.HoldownList = HoldownListRecord.Make(designResults, designer.StandardList, Calculator.DesignSettings.HoldownsFromStandardList);
        }

        /// <summary>
        /// After opening: the walls whose hold-down differs from the one saved in the project (the standard list or the catalog
        /// changed since), with both list versions; null when nothing changed or the project predates the record.
        /// </summary>
        private string HoldownChangesSinceSave()
        {
            var record = Calculator?.DesignSettings?.HoldownList;
            if (record == null || designer == null) return null;
            var lines = record.Differences(designResults);
            if (lines.Count == 0) return null;
            var list = designer.StandardList;
            string pool(bool std, int v, string d, bool c) => std ? "standard list, " + HoldownListRecord.Describe(v, d, c) : "full catalog";
            return $"Hold-downs differ from when this project was saved.\nSaved with: {pool(record.FromStandardList, record.CompanyVersion, record.CompanyDate, record.Customized)}." +
                   $"\nNow: {pool(Calculator.DesignSettings.HoldownsFromStandardList, list.CompanyVersion, list.CompanyDate, list.Customized)}.\n\n" +
                   string.Join("\n", lines) +
                   "\n\nThe design now uses the current list. Save to keep it, or close without saving to leave the file as it was " +
                   "(the Design tab can also switch between the standard list and the full catalog).";
        }

        /// <summary>More than 0.5% (or 0.01 k) apart.</summary>
        private static bool Changed(double before, double now) =>
            Math.Abs(before - now) > Math.Max(0.01, 0.005 * Math.Abs(before));

        private void MenuItem_PrintReport_Click(object sender, RoutedEventArgs e)
        {
            windSetup?.FlushPendingChange();
            var analysis = windLoadCalculator_MWFRS_Length != null && windLoadCalculator_MWFRS_Width != null
                ? new WindAnalysisResult(windLoadCalculator_CC, windLoadCalculator_MWFRS_Length, windLoadCalculator_MWFRS_Width)
                : null;
            var info = new ReportInfo { Project = ProjectName, Program = "Shear Wall Calculator" };
            var report = WindReport.Build(info, windVersion, windLoadParams, buildingData, analysis, ShearWallReport.Sections(Calculator, windLoadsLinked, designResults, designer));
            new ReportPreviewWindow(report, ProjectName) { Owner = this }.Show();
        }
        #endregion

        #region File Handling
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

                // Recent project files (they used to be image files, opened by a placeholder).
                menu_item.Click += (s, e) =>
                {
                    if (ConfirmDiscardOrSave())
                        OpenProject((string)((MenuItem)s).Tag);
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



        #endregion

        #region MODE setters
        public void SetButtonModes()
        {
            btnSelectMode.Tag = currentMode == DrawMode.None ? "on" : null;
            btnRigidityMode.Tag = currentMode == DrawMode.Line ? "on" : null;
            btnMassMode.Tag = currentMode == DrawMode.Rectangle ? "on" : null;
            btnSnapToNearest.Tag = snapMode ? "on" : null;
            ShowStatusMode();
        }

        private void SetLineMode()
        {
            currentMode = DrawMode.Line;
            SetButtonModes();
        }

        private void SetRectangleMode()
        {
            currentMode = DrawMode.Rectangle;
            SetButtonModes();
        }

        private void SetDebugMode()
        {
            debugMode = !debugMode;
            ShowStatusMessage(LogLevel.Info, $"Debug drawing {(debugMode ? "on" : "off")} (D)");
            Log.Info($"Debug mode: {(debugMode ? "Enabled" : "Disabled")}");
        }

        private void SetSnapMode()
        {
            snapMode = !snapMode;
            SetButtonModes();
            Log.Info($"Snap mode: {(snapMode ? "Enabled" : "Disabled")}");
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
            imageTool.Cancel();
        }
        #endregion

        // ------------------------------------------------------------------ status line

        /// <summary>Redraws the drawing only (cursor moves, zoom, preview); the calculations and panels are not rebuilt.</summary>
        private void RedrawDrawing()
        {
            ShowStatusMode();
            Draw(ChangeType.Redraw);
        }

        private void ShowCursor(Point world)
        {
            string snap = currentSnap.Found ? "   " + SnapResult.Describe(currentSnap.Kind).ToLowerInvariant() : "";
            string len = startPoint_world.HasValue && currentMode == DrawMode.Line
                ? $"   L = {(GetConstrainedPoint(world, startPoint_world.Value) - startPoint_world.Value).Length:0.00} ft"
                : startPoint_world.HasValue && currentMode == DrawMode.Rectangle
                    ? $"   {Math.Abs(world.X - startPoint_world.Value.X):0.##} × {Math.Abs(world.Y - startPoint_world.Value.Y):0.##} ft" : "";
            tbWorldCoords.Text = $"x {world.X:0.00},  y {world.Y:0.00} ft{snap}{len}";
            tbZoom.Text = $"Zoom {zoomFactorX:0.##}×";
        }

        /// <summary>What the drawing tool expects next.</summary>
        private void ShowStatusMode()
        {
            if (imageTool.Active)
            {
                tbStatusMode.Text = imageTool.Prompt;
                return;
            }
            string snap = snapMode ? "" : "   (snaps off)";
            switch (currentMode)
            {
                case DrawMode.Line:
                    tbStatusMode.Text = (startPoint_world == null ? "Wall: click or type the first end" : "Wall: click or type the second end  (Esc cancels)") + snap;
                    break;
                case DrawMode.Rectangle:
                    tbStatusMode.Text = (startPoint_world == null ? "Diaphragm: click or type a corner" : "Diaphragm: click or type the opposite corner  (Esc cancels)") + snap;
                    break;
                default:
                    tbStatusMode.Text = "Select: click a wall or diaphragm to see it.  Draw with Wall (L) or Diaphragm (R).";
                    break;
            }
        }

        private static readonly Brush StatusWarnInk = new SolidColorBrush(Color.FromRgb(0x9A, 0x5B, 0x00));
        private static readonly Brush StatusErrorInk = new SolidColorBrush(Color.FromRgb(0xB4, 0x23, 0x18));

        /// <summary>Shows a message on the right of the status line; warnings and errors from the log arrive here too.</summary>
        private void ShowStatusMessage(LogLevel level, string message)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(() => ShowStatusMessage(level, message)));
                return;
            }
            string mark = level == LogLevel.Error ? "✗ " : level == LogLevel.Warning ? "⚠ " : "";
            tbStatusMessage.Text = $"{mark}{message}   {DateTime.Now:HH:mm}";
            tbStatusMessage.Foreground = level == LogLevel.Error ? StatusErrorInk : level == LogLevel.Warning ? StatusWarnInk : (Brush)new SolidColorBrush(Color.FromRgb(0x52, 0x51, 0x4E));
        }

        private void OnLogWritten(LogLevel level, string message)
        {
            if (level >= LogLevel.Warning) ShowStatusMessage(level, message);
        }

        private void StatusMessage_Click(object sender, MouseButtonEventArgs e) => OpenLogFolder_Click(sender, e);

        // Add an effect where you can press enter or spacebar to commit the building data
        private void RunBuildingDataInputCalculations(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Space)
            {
                BuildingDataInputControl ctrl = tabBuildingDataControlTabItem.Content as BuildingDataInputControl;
                ctrl.ParseBuildingData();
                ctrl.OnBuildingDataInputComplete(ctrl.buildingData);
            }
        }

        private bool InWindWorkspace => rbWorkspaceWind.IsChecked == true;

        /// <summary>Tabs that belong to the Wind loads workspace; the rest belong to Shear walls.</summary>
        private bool IsWindTab(TabItem tab) =>
            tab == tabWindSetupTabItem || tab == tabWindDetailsTabItem || tab == tabWind3DTabItem || tab == tabBuildingDataControlTabItem ||
            tab == tabWindInputControlTabItem || tab == tabWindResultsTabItem_CC ||
            tab == tabWindResultsTabItem_MWFRS_BldgLength || tab == tabWindResultsTabItem_MWFRS_BldgWidth;

        private void Workspace_Checked(object sender, RoutedEventArgs e)
        {
            if (windSetup == null) return; // still loading
            ApplyWorkspace();
        }

        /// <summary>
        /// Shows one workspace. Wind loads: the guided setup and the wind tabs use the whole window.
        /// Shear walls: the drawing canvas, its toolbar, and the Dimensions / Calculations tabs.
        /// </summary>
        private void ApplyWorkspace()
        {
            bool wind = InWindWorkspace;
            if (!wind) windSetup?.FlushPendingChange(); // the walls get the latest wind loads first

            foreach (var tab in MainTabControl.Items.OfType<TabItem>())
                tab.Visibility = IsWindTab(tab) == wind ? Visibility.Visible : Visibility.Collapsed;

            // The canvas is Hidden (not Collapsed) so it keeps its size: its zoom/pan math reads
            // ActualWidth/ActualHeight. The tab area simply stretches over it.
            dockpanel.Visibility = wind ? Visibility.Hidden : Visibility.Visible;
            Grid.SetColumn(MainTabControl, wind ? 0 : 1);
            Grid.SetColumnSpan(MainTabControl, wind ? 2 : 1);
            spDrawingToolbar.Visibility = wind ? Visibility.Collapsed : Visibility.Visible;

            if (!(MainTabControl.SelectedItem is TabItem current) || current.Visibility != Visibility.Visible)
                MainTabControl.SelectedItem = wind ? tabWindSetupTabItem : (TabItem)tabCalculationsTabItem;
            if (!wind) Draw(ChangeType.Redraw);
        }

        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Opening the 3D tab always shows the latest results (never a stale building).
            if (ReferenceEquals(e.OriginalSource, MainTabControl) && tabWind3DTabItem.IsSelected &&
                tabWind3DTabItem.Content is Wind3DViewerControl viewer)
            {
                viewer.SetResults(buildingData, windLoadCalculator_CC, windLoadCalculator_MWFRS_Length, windLoadCalculator_MWFRS_Width, keepCamera: true);
            }

            if (tabWindInputControlTabItem.IsSelected &&
            tabWindInputControlTabItem.Content is UIElement content)
            {
                // Ensure focus happens after layout is complete
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    FocusFirstFocusableChild(content);
                }), DispatcherPriority.Input);
            }
        }

        public static void FocusFirstFocusableChild(DependencyObject parent)
        {
            var scope = FocusManager.GetFocusScope(parent);
            var firstFocusable = GetFirstFocusableChild(parent);

            if (firstFocusable != null)
            {
                Keyboard.Focus(firstFocusable);
            }
        }

        private static IInputElement GetFirstFocusableChild(DependencyObject parent)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is UIElement ui && ui.Focusable && ui.IsEnabled && ui.IsVisible)
                    return ui;

                var focusableChild = GetFirstFocusableChild(child);
                if (focusableChild != null)
                    return focusableChild;
            }

            return null;
        }
    }
}
