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
using BuildingModel.Serialization;
using BuildingModel.ShearWalls;
using Building = BuildingModel.Model.Building;
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


        // plan image: the decoded file (kept while the path is the same) and the two-point commands
        private BitmapSource planBitmap;
        private string planBitmapPath;
        private byte[] planBitmapData;   // the stored image the bitmap was decoded from
        private Geometry planVector;           // CAD linework of the plan, image units
        private List<VectorPath> planVectorPaths; // the paths it was built from
        private readonly ImageTool imageTool = new ImageTool();

        public EventHandler OnUpdated;  // the event that signals that the drawing has been updated -- controls will listen for this at the time they are created.

        bool hideImage = false;
        bool showDesign = true; // schedule marks, hold-downs and the schedule on the plan
        bool imageDrawWarned = false;   // log a reference-image drawing failure once, not every frame

        private double defaultWallHeight = 9.0;

        private bool selectionCardOpen;                    // the card shows the picked wall or diaphragm
        private bool debugMode = false;


        public MainWindow()
        {
            InitializeComponent();
            LoadRecentFilesMenu();

            this.KeyDown += MainWindow_KeyDown;
            this.PreviewKeyDown += FileShortcuts; // Ctrl+N, O, S, P
            this.Closing += MainWindow_Closing; // asks to save unsaved changes
            Log.Written += OnLogWritten;      // warnings and errors show on the status line
            this.TextInput += RouteTypingToCommand;  // typing a number while drawing goes to the Point box
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


                InitSketch();
                SetBuilding(ShearWallAdapter.NewBuilding(buildingData?.StoryPlateHeights), null);
                LoadRecentFilesMenu();  // recent files menu

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
                StartHistory();
                MarkSaved(); // a new, untitled project
            };
        }

        public void UpdateShearWallUI()
        {
            if (Calculator != null)
            {
                AnalyzeLevels(); // every level, roof first (one level: the same as before)
                UpdateDesign();
                LogModelChanges();
                StoreDesignInModel(); // design changes become model changes: one undo history with the drawing
                DropMissingSelection();

                ShowImageName();

                geometryPanel?.Show(Calculator, selectedKind, selectedId);
                if (resultsPanel != null) resultsPanel.Notes = levelNotes;
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
            UpdateIssues(); // errors and warnings on the plan and in the issue list
            Update3D();
            Draw();
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
            geometryPanel = new GeometryPanel { HeightsFromLevels = true }; // the building model sets them
            geometryPanel.GeometryChanged += (s, e) => GeometryTyped(); // typed coordinates go to the model
            geometryPanel.SelectionChanged += SelectFromTable;
            geometryPanel.DeleteRequested += DeleteFromTable;
            tabDimensionsTabItem.Content = WithLevelBanner(geometryPanel);
            resultsPanel = new ShearResultsPanel();
            resultsPanel.WallSelected += id => SelectFromTable("Wall", id);
            resultsPanel.WallDeleteRequested += id => DeleteFromTable("Wall", id);
            tabCalculationsTabItem.Content = WithLevelBanner(resultsPanel);

            designPanel = new WallDesignPanel();
            designPanel.DesignChanged += (s, e) => UpdateShearWallUI();
            designPanel.AutoSelectRequested += (s, e) => AutoSelectAllWalls();
            designPanel.WallSelected += id => SelectFromTable("Wall", id);
            designPanel.WallDeleteRequested += id => DeleteFromTable("Wall", id);
            tabDesignTabItem.Content = WithLevelBanner(designPanel);
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
                designResults = CheckWalls(Calculator);
                if (designResults.Count == 0) message = "No wall shears yet: add loads (LOADS button) or link the wind loads.";
            }
            designPanel.Show(Calculator, designResults, designer, windLoadsLinked, message);
            if (selectedKind == "Wall") designPanel.SelectWall(selectedId); // the same wall as on the drawing and the other tabs
            holdownCatalog?.SetDesign(designResults);
        }

        /// <summary>
        /// The check of each wall of one level for its governing shear (empty when the level cannot be calculated). With
        /// several floors the levels above are checked first, roof down, so each hold-down carries the walls stacked on it.
        /// </summary>
        private Dictionary<int, WallDesignResult> CheckWalls(ShearWallCalculatorBase calc)
        {
            int i = levels.IndexOf(calc);
            if (i < 0) return CheckLevel(calc, null, null, 0);
            Dictionary<int, WallDesignResult> above = null;
            for (int k = levels.Count - 1; k >= i; k--)
                above = CheckLevel(levels[k], k + 1 < levels.Count ? levels[k + 1] : null, above, k + 2);
            return above;
        }

        private Dictionary<int, WallDesignResult> CheckLevel(ShearWallCalculatorBase calc, ShearWallCalculatorBase upper,
            Dictionary<int, WallDesignResult> upperDesign, int upperLevel)
        {
            var results = new Dictionary<int, WallDesignResult>();
            if (calc == null || designer == null || !calc.IsValidForCalculation) return results;
            foreach (var kv in calc._wall_system._walls)
            {
                if (!calc.TotalWallShear.TryGetValue(kv.Key, out double v)) continue;
                calc.GoverningLoadCase.TryGetValue(kv.Key, out string governing);
                var above = upper == null ? null : StoryCascade.HoldownForcesOn(kv.Value, upper._wall_system._walls, upperDesign, upperLevel);
                results[kv.Key] = designer.Check(kv.Key, kv.Value, v, governing, calc.DesignSettings, null, above);
            }
            return results;
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


        /// <summary>World length of a distance on screen (the drawing is square: one zoom factor).</summary>
        private double PixelsToWorld(double pixels) => Sketch.PixelsToFeet(pixels);

        /// <summary>World (ft) to pixels on the drawing, through the sketch's view (<paramref name="element"/> is not used).</summary>
        private Point WorldToScreen(Point p, FrameworkElement element) => Sketch.ToScreen(ToPoint2(p));

        private Point ScreenToWorld(Point p, FrameworkElement element) => ToPoint(Sketch.ToWorld(p));

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
            MatchLevelsToStories(buildingData.NumberOfStories); // Floors: 1 to 3 levels

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
                foreach (var level in levels.DefaultIfEmpty(Calculator))
                    level.SetLoadCases(new List<ShearWallCalculator.LateralLoadCase>()); // no stale loads
                windSetup?.SetShearWallLink(true,
                    "But the wind inputs cannot be calculated right now, so the walls have no wind load. Fix the inputs and the walls update automatically.");
                return;
            }

            SetWindLoadsPerLevel(new WindAnalysisResult(windLoadCalculator_CC, windLoadCalculator_MWFRS_Length, windLoadCalculator_MWFRS_Width),
                windSetup?.OnlyCases1And3 ?? false, out string message); // one floor: the building's load cases, as before
            windSetup?.SetShearWallLink(true, message);
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

        /// <summary>Schedule marks, hold-downs and the schedule over the plan (SHOW Design).</summary>
        private void DrawWallDesign(DrawingContext ctx)
        {
            if (!showDesign || Calculator?._wall_system == null) return;
            DesignOverlay.Draw(ctx, Calculator._wall_system._walls, designResults, p => WorldToScreen(p, m_layers),
                               new Size(m_layers.ActualWidth, m_layers.ActualHeight), VisualTreeHelper.GetDpi(this).PixelsPerDip);
        }

        private static Pen FrozenPen(Color c, double thickness)
        {
            var p = new Pen(new SolidColorBrush(c), thickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
            p.Freeze();
            return p;
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
                    if (PointIsWithinBounds(p1_screen, m_layers) is true)
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
                    if (PointIsWithinBounds(p2_screen, m_layers) is true)
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
                    if (PointIsWithinBounds(p1_screen, m_layers) is true)
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
                    if (PointIsWithinBounds(p2_screen, m_layers) is true)
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
                    if (PointIsWithinBounds(p3_screen, m_layers) is true)
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
                    if (PointIsWithinBounds(p4_screen, m_layers) is true)
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

                    Point p1_world = new Point(ScreenToWorld(new Point(44, 0), m_layers).X, center); // across the view, the label at its left
                    Point p1_screen = GetConstrainedScreenPoint(WorldToScreen(p1_world, m_layers), m_layers); ;
                    Point p2_world = new Point(ScreenToWorld(new Point(m_layers.ActualWidth, 0), m_layers).X, center);
                    Point p2_screen = GetConstrainedScreenPoint(WorldToScreen(p2_world, m_layers), m_layers); ;

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

                    Point p1_world = new Point(center, ScreenToWorld(new Point(0, m_layers.ActualHeight - 60), m_layers).Y); // up the view, the label at its foot
                    Point p1_screen = GetConstrainedScreenPoint(WorldToScreen(p1_world, m_layers), m_layers); ;
                    Point p2_world = new Point(center, ScreenToWorld(new Point(0, 0), m_layers).Y);
                    Point p2_screen = GetConstrainedScreenPoint(WorldToScreen(p2_world, m_layers), m_layers); ;

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

                    double x_screen = WorldToScreen(com, m_layers).X;
                    double y_screen = WorldToScreen(com, m_layers).Y;

                    // draw a vertical line since the Y is valid
                    if ((double.IsNaN(com.X) is false) && (double.IsNaN(com.Y) is true))
                    {
                        Pen pen = new Pen(Brushes.Red, 2);
                        pen.DashStyle = new DashStyle(new double[] { 3, 3 }, 0);

                        x_screen = 10; // draw it justbelow the top
                        ctx.DrawLine(pen, new Point(x_screen, 0), new Point(x_screen, m_layers.ActualHeight));
                    }

                    // draw a vertical line since if the X is valid
                    else if ((double.IsNaN(com.Y) is false) && (double.IsNaN(com.X) is true))
                    {
                        Pen pen = new Pen(Brushes.Red, 2);
                        pen.DashStyle = new DashStyle(new double[] { 3, 3 }, 0);

                        y_screen = m_layers.ActualHeight - 10;

                        ctx.DrawLine(pen, new Point(0, y_screen), new Point(m_layers.ActualHeight, y_screen));
                    }

                    // draw two lines since neither value is valid
                    else if ((double.IsNaN(com.X) is true) && (double.IsNaN(com.Y) is true))
                    {
                        Pen pen = new Pen(Brushes.Red, 2);
                        pen.DashStyle = new DashStyle(new double[] { 3, 3 }, 0);

                        y_screen = m_layers.ActualHeight - 10;
                        x_screen = 10;

                        ctx.DrawLine(pen, new Point(0, y_screen), new Point(m_layers.ActualWidth, y_screen));
                        ctx.DrawLine(pen, new Point(x_screen, 0), new Point(x_screen, m_layers.ActualHeight));
                    }

                    // Draw a marker
                    Point pt = new Point(x_screen, y_screen);

                    if (PointIsWithinBounds(pt, m_layers))
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
                double x_screen = WorldToScreen(cor, m_layers).X;
                double y_screen = WorldToScreen(cor, m_layers).Y;

                // draw a vertical line since the Y is valid
                if ((double.IsNaN(cor.X) is false) && (double.IsNaN(cor.Y) is true))
                {

                    Pen pen = new Pen(Brushes.MediumBlue, 2);
                    pen.DashStyle = new DashStyle(new double[] { 3, 3 }, 0);

                    y_screen = 10; // draw it justbelow the top
                    ctx.DrawLine(pen, new Point(x_screen, 0), new Point(x_screen, m_layers.ActualHeight));
                }

                // draw a horizontal line since if the X is valid
                else if ((double.IsNaN(cor.Y) is false) && (double.IsNaN(cor.X) is true))
                {
                    Pen pen = new Pen(Brushes.MediumBlue, 2);
                    pen.DashStyle = new DashStyle(new double[] { 3, 3 }, 0);

                    x_screen = m_layers.ActualWidth - 10;

                    ctx.DrawLine(pen, new Point(0, y_screen), new Point(m_layers.ActualWidth, y_screen));
                }

                // draw two lines since neither value is valid
                else if ((double.IsNaN(cor.X) is true) && (double.IsNaN(cor.Y) is true))
                {
                    Pen pen = new Pen(Brushes.MediumBlue, 2);
                    pen.DashStyle = new DashStyle(new double[] { 3, 3 }, 0);

                    y_screen = 10;
                    x_screen = m_layers.ActualWidth - 10;

                    ctx.DrawLine(pen, new Point(x_screen, 0), new Point(x_screen, m_layers.ActualHeight));
                    ctx.DrawLine(pen, new Point(0, y_screen), new Point(m_layers.ActualWidth, y_screen));
                }

                // Draw a marker
                Point pt = new Point(x_screen, y_screen);

                if (PointIsWithinBounds(pt, m_layers))
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

        /// <summary>The plan under the drawing: image units → world (scale, rotation, position) → screen.</summary>
        private void DrawReferenceImage(DrawingContext ctx)
        {
            var img = Calculator?.Image;
            if (hideImage || img == null || !img.Visible) return;
            try
            {
                Point o = WorldToScreen(new Point(0, 0), m_layers), ex = WorldToScreen(new Point(1, 0), m_layers), ey = WorldToScreen(new Point(0, 1), m_layers);
                var m = img.PixelToWorldMatrix();
                m.Append(new Matrix(ex.X - o.X, ex.Y - o.Y, ey.X - o.X, ey.Y - o.Y, o.X, o.Y));
                double opacity = Math.Max(0.05, Math.Min(1, img.Opacity));
                if (img.IsVector)
                {
                    // the geometry is transformed, not the pen: lines stay one pixel wide at any zoom
                    var lines = new GeometryGroup { Transform = new MatrixTransform(m) };
                    lines.Children.Add(PlanVector(img));
                    ctx.PushOpacity(opacity);
                    ctx.DrawGeometry(null, PlanPen, lines);
                    ctx.Pop();
                    return;
                }
                var bmp = PlanBitmap(img);
                if (bmp == null) return;
                ctx.PushTransform(new MatrixTransform(m));
                ctx.PushOpacity(opacity);
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

        private static readonly Pen PlanPen = FrozenPen(Color.FromRgb(0x4A, 0x55, 0x68), 1.0);

        /// <summary>The CAD linework as one geometry in image units, built once per import.</summary>
        private Geometry PlanVector(ReferenceImage img)
        {
            if (planVector != null && planVectorPaths == img.Paths) return planVector;
            var g = new StreamGeometry();
            using (var c = g.Open())
                foreach (var p in img.Paths)
                {
                    var xy = p.Xy;
                    if (xy == null || xy.Length < 4) continue;
                    c.BeginFigure(new Point(xy[0], xy[1]), false, p.Closed);
                    var pts = new List<Point>(xy.Length / 2 - 1);
                    for (int i = 2; i + 1 < xy.Length; i += 2) pts.Add(new Point(xy[i], xy[i + 1]));
                    c.PolyLineTo(pts, true, false);
                }
            g.Freeze();
            planVector = g;
            planVectorPaths = img.Paths;
            return g;
        }

        /// <summary>
        /// The decoded plan image: from the copy stored in the project when there is one, otherwise from the file
        /// (which is then stored, so the next save carries it). Loaded once; null (with one warning) when it cannot be read.
        /// </summary>
        private BitmapSource PlanBitmap(ReferenceImage img)
        {
            if (img.Data == null)
            {
                string path = img.Resolve(currentProjectPath);
                if (path == null || !img.Embed(path))
                {
                    if (planBitmapPath != img.Path)
                    {
                        planBitmapPath = img.Path;
                        planBitmap = null;
                        Log.Warning("The plan image was not found: " + img.Path + " (import it again; it is then stored in the project file).");
                    }
                    return null;
                }
                Log.Info($"Plan image stored in the project: {path} ({img.Data.Length / 1024.0:0} kB; saved with the next save)");
            }
            if (planBitmap != null && planBitmapData == img.Data) return planBitmap;
            planBitmapData = img.Data;
            planBitmapPath = img.Path;
            planBitmap = null;
            try
            {
                var b = new BitmapImage();
                b.BeginInit();
                b.StreamSource = new System.IO.MemoryStream(img.Data);
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
                Log.Warning("Could not read the plan image " + img.Path, ex);
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

            Item(img == null ? "Import plan (DXF, PDF, image)..." : "Replace plan (DXF, PDF, image)...", ImportPlanImage);
            Item(hideImage ? "Show image layer" : "Hide image layer", () => btnHideImage_Click(btnHideImage, null), img != null,
                 "The image is a layer under the drawing: it is never selected by clicks, and it is saved inside the project file.");
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

        /// <summary>
        /// Import a plan under the drawing: a DXF (true size from its units, linework you can snap to), a PDF sheet
        /// (the printed drawing scale; the paper size is in the file) or a picture (drawing scale and dpi, or set the
        /// scale on the drawing). The plan is stored in the project file.
        /// </summary>
        private async void ImportPlanImage()
        {
            if (Calculator == null) return;
            var open = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Import plan",
                Filter = "Plans (*.dxf;*.pdf;*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff;*.gif)|*.dxf;*.pdf;*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff;*.gif|" +
                         "CAD drawing (*.dxf)|*.dxf|PDF sheet (*.pdf)|*.pdf|Images (*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff;*.gif|" +
                         "All files (*.*)|*.*",
            };
            if (open.ShowDialog(this) != true) return;
            string file = open.FileName;
            string ext = System.IO.Path.GetExtension(file).ToLowerInvariant();

            ReferenceImage result = null;
            bool calibrateOnDrawing = false;
            Cursor = Cursors.Wait;
            try
            {
                if (ext == ".dwg")
                {
                    MessageBox.Show(this, "DWG files are not read directly. In AutoCAD, save the drawing as DXF (SAVEAS > DXF, or DXFOUT) and import the DXF: " +
                                          "it comes in at true size.", "Import plan", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                if (ext == ".dxf")
                {
                    var drawing = await PdfPages.InBackground(() => DxfReader.ReadFile(file));
                    if (drawing.Paths.Count == 0) throw new System.IO.InvalidDataException("No lines, polylines or arcs were found in model space.");
                    Cursor = null;
                    var dialog = new ImportDxfDialog(file, drawing) { Owner = this };
                    if (dialog.ShowDialog() != true) return;
                    result = dialog.Result;
                }
                else if (ext == ".pdf")
                {
                    var pages = await PdfPages.InBackground(() => PdfPages.PaperSizes(file));
                    if (pages.Count == 0) throw new System.IO.InvalidDataException("The PDF has no pages.");
                    Cursor = null;
                    var dialog = new ImportPdfDialog(file, pages, DrawingWidthFeet()) { Owner = this };
                    if (dialog.ShowDialog() != true) return;
                    result = dialog.Result;
                    calibrateOnDrawing = dialog.CalibrateOnDrawing;
                }
                else
                {
                    Cursor = null;
                    var dialog = new ImportImageDialog(file, DrawingWidthFeet()) { Owner = this };
                    if (dialog.ShowDialog() != true) return;
                    result = dialog.Result;
                    result.Source = "Image";
                    calibrateOnDrawing = dialog.CalibrateOnDrawing;
                    if (!result.Embed(file)) Log.Warning("The plan image could not be stored in the project: " + file);
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Could not import the plan " + file, ex);
                MessageBox.Show(this, "This file could not be imported.\n\n" + ex.Message, "Import plan", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            finally
            {
                Cursor = null;
            }

            Calculator.Image = result;
            planBitmap = null;
            planBitmapData = null;
            planVector = null;
            imageDrawWarned = false;
            hideImage = false;
            btnHideImage.Tag = "on";
            Log.Info($"Plan imported ({result.Source}): {file}");
            ZoomToImage();
            if (calibrateOnDrawing) StartImageCommand(ImageCommand.Calibrate);
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
            Sketch.SetTool(BuildingModel.Sketch.SketchTool.Select);
            if (selectionCardOpen) CloseSelectionCard();
            imageTool.Begin(command);
            hideImage = false;
            Calculator.Image.Visible = true;
            btnHideImage.Tag = "on";
            ShowStatusMessage(LogLevel.Info, imageTool.Prompt);
            RedrawDrawing();
        }

        /// <summary>A click while an image command is active.</summary>
        private void ImageToolClick(Point p)
        {
            string message = imageTool.Click(Calculator.Image, p, out bool warning);
            if (message != null) ShowStatusMessage(warning ? LogLevel.Warning : LogLevel.Info, message);
            if (imageTool.WaitingForDistance) sketchView.FocusCommand();
            else if (imageTool.Active) ShowStatusMessage(LogLevel.Info, imageTool.Prompt);
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
                ctx.DrawLine(pen, last.Value, WorldToScreen(ToPoint(Sketch.CursorPoint), m_layers));
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
            tbImageFileName.Text = "Plan: " + System.IO.Path.GetFileName(img.Path) + (string.IsNullOrEmpty(img.Source) ? "" : $" ({img.Source})") + (img.Calibrated ? "" : "  (scale not set)") + (img.Locked ? "  (locked)" : "");
            tbImageFileName.ToolTip = img.Path + Environment.NewLine +
                                      (img.IsVector ? $"CAD linework, {img.Paths.Count} paths, " : $"{img.FeetPerPixel * 12:0.###} in per pixel, ") + $"turned {img.RotationDegrees:0.##}°, opacity {img.Opacity * 100:0} %";
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

        /// <summary>Zooms so the whole image shows, with a margin.</summary>
        private void ZoomToImage()
        {
            var img = Calculator?.Image;
            if (img == null || img.PixelWidth <= 0) return;
            Rect b = img.WorldBounds();
            if (b.Width <= 0 || b.Height <= 0) return;
            Sketch.ZoomTo(b);
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
            planBitmap = null;
            planBitmapPath = null;
            planBitmapData = null;
            planVector = null;
            imageDrawWarned = false;
            loggedModel = null; // a different project: start the change log afresh
            imageTool.Cancel();
            hideImage = Calculator?.Image != null && !Calculator.Image.Visible;
            btnHideImage.Tag = hideImage ? null : "on";
        }




        #endregion

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
            // no question: Ctrl+Z brings it back
            selectedKind = kind;
            selectedId = id;
            DeleteSelected();
        }

        // ------------------------------------------------------------------ typed points

        /// <summary>Typing a number, @, - or . while drawing (and not in a box) goes to the sketch's point box.</summary>
        private void RouteTypingToCommand(object sender, TextCompositionEventArgs e)
        {
            if (InWindWorkspace || string.IsNullOrEmpty(e.Text)) return;
            if (Sketch.Tool == BuildingModel.Sketch.SketchTool.Select && !imageTool.WaitingForDistance) return;
            if (e.OriginalSource is System.Windows.Controls.Primitives.TextBoxBase) return;
            if ("0123456789.-@".IndexOf(e.Text[0]) < 0) return;
            sketchView.FocusCommand(e.Text);
            e.Handled = true;
        }

        #region UI Events
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
        /// <summary>
        /// The drawing keys go to the sketch (W wall, D diaphragm, O opening, N note, M move, C copy, I mirror, T measure,
        /// X split, J join, Z fit, Del, Esc, F3 snaps, F8 ortho), with the designer's keys from before: L wall, R diaphragm,
        /// S snaps; F12 debug drawing. Never a key typed into a box, and only in the Shear walls workspace.
        /// </summary>
        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Handled || InWindWorkspace) return;
            if (e.OriginalSource is System.Windows.Controls.Primitives.TextBoxBase || e.OriginalSource is PasswordBox || (e.OriginalSource is ComboBox cb && cb.IsEditable)) return;
            var mods = Keyboard.Modifiers;
            Key key = e.Key == Key.System ? e.SystemKey : e.Key;
            if (mods == ModifierKeys.None)
            {
                switch (key)
                {
                    case Key.L: key = Key.W; break;
                    case Key.R: key = Key.D; break;
                    case Key.S: key = Key.F3; break;
                    case Key.F12: SetDebugMode(); Draw(); e.Handled = true; return;
                }
            }
            e.Handled = Sketch.HandleKey(key, mods);
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

        private void TwoFaceRule_Click(object sender, RoutedEventArgs e) =>
            MessageBox.Show(this, SheathingRules.TwoFaceRuleNote, "Two-face sheathing rule", MessageBoxButton.OK, MessageBoxImage.Information);

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

        /// <summary>The project file text: the wind inputs, each story's calculator (loads, plan image) and the building model.</summary>
        private string CurrentProjectJson() => levels.Count == 0 ? ProjectSerializer.ToJson(Calculator, CurrentWindInputs(), windLoadsLinked, null, ModelFile.BuildingToJson(building))
            : ProjectSerializer.ToJson(levels[0], CurrentWindInputs(), windLoadsLinked, levels.Skip(1), ModelFile.BuildingToJson(building));

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
            if ((Keyboard.Modifiers & ModifierKeys.Control) == 0) return;
            if ((e.Key == Key.Z || e.Key == Key.Y) && UndoShortcut(e)) { e.Handled = true; return; } // Ctrl+Z, Ctrl+Y, Ctrl+Shift+Z
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
            currentProjectPath = null;
            Log.Info("New project");
            windSetup.SetInputs(null, ASCE7_Versions.ASCE_VER_7_22, null);
            windSetup.SetOnlyCases1And3(false);
            buildingData = windSetup.Building.Clone();
            windLoadParams = windSetup.Parameters.Clone();
            windVersion = windSetup.Version;
            windLoadsLinked = true;
            SetBuilding(ShearWallAdapter.NewBuilding(buildingData.StoryPlateHeights), null);
            Calculator.DesignSettings.HoldownsFromStandardList = designer != null && designer.StandardList.Count > 0;
            ImageForNewProject();
            RecalculateWind();
            UpdateShearWallUI();
            StartHistory();
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
                ProjectSerializer.Save(path, levels[0], CurrentWindInputs(), windLoadsLinked, levels.Skip(1), ModelFile.BuildingToJson(building));
                baseModelJson = ModelFile.BuildingToJson(building); // Model > Export changes: since this save
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

            // The building model is the source of truth; an older file (no model) becomes one from its calculators. The
            // calculators stay with the stories in order: their loads and plan images.
            var calcs = new[] { contents.Calculator }.Concat(contents.UpperLevels ?? new List<ShearWallCalculatorBase>()).ToList();
            Building opened = null;
            if (contents.Model != null)
            {
                try { opened = ModelFile.BuildingFromJson(contents.Model); }
                catch (Exception ex) when (ex is System.Runtime.Serialization.SerializationException || ex is InvalidDataException)
                {
                    Log.Warning("The building model in the project could not be read; it is made again from the walls and diaphragms", ex);
                }
            }
            bool migrated = opened == null;
            if (migrated)
                opened = ShearWallAdapter.FromCalculators(calcs, contents.Wind?.ToBuilding().StoryPlateHeights);
            SetBuilding(opened, calcs);
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
                if (buildingData != null && buildingData.NumberOfStories != levels.Count)
                {
                    buildingData.NumberOfStories = levels.Count; // one story
                    SyncWindSetup();
                }
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
                FloorsToBuildingData(); // the model's floors and plate heights

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
            StartHistory();
            if (migrated && calcs.Any(c => c._wall_system._walls.Count + c._diaphragm_system._diaphragms.Count > 0))
                Log.Info("The project's walls and diaphragms were put into a building model (saved with the project from now on).");
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
            var level1 = levels.Count > 0 ? levels[0] : Calculator; // the record is checked on opening, which shows level 1
            Calculator.DesignSettings.HoldownList = HoldownListRecord.Make(ReferenceEquals(level1, Calculator) ? designResults : CheckWalls(level1),
                designer.StandardList, Calculator.DesignSettings.HoldownsFromStandardList);
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
            var report = WindReport.Build(info, windVersion, windLoadParams, buildingData, analysis, ShearWallReport.Levels(levels.Count > 0 ? levels : new List<ShearWallCalculatorBase> { Calculator }, windLoadsLinked,
                c => ReferenceEquals(c, Calculator) ? designResults : CheckWalls(c), designer, levelNotes));
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
            btnShowDesign.Tag = showDesign ? "on" : null;
            btnHideImage.Tag = hideImage ? null : "on";
        }

        private void SetDebugMode()
        {
            debugMode = !debugMode;
            ShowStatusMessage(LogLevel.Info, $"Debug drawing {(debugMode ? "on" : "off")} (F12)");
            Log.Info($"Debug mode: {(debugMode ? "Enabled" : "Disabled")}");
        }

        /// <summary>Cancels the plan-image command and the sketch's command in progress.</summary>
        private void ResetInputMode()
        {
            imageTool.Cancel();
            Sketch.ResetCommand();
        }
        #endregion

        // ------------------------------------------------------------------ status line

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
            drawingSplitter.Visibility = wind ? Visibility.Collapsed : Visibility.Visible;
            Grid.SetColumn(MainTabControl, wind ? 0 : 2);
            Grid.SetColumnSpan(MainTabControl, wind ? 3 : 1);
            spDrawingToolbar.Visibility = wind ? Visibility.Collapsed : Visibility.Visible;

            if (!(MainTabControl.SelectedItem is TabItem current) || current.Visibility != Visibility.Visible)
                MainTabControl.SelectedItem = wind ? tabWindSetupTabItem : (TabItem)tabCalculationsTabItem;
            if (!wind) Draw();
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
