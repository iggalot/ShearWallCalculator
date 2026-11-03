using ASCE7WindLoadCalculator;
using BuildingModel.Diagnostics;
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
using ShearWallCalculator.ModelAdapter;
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

        public EventHandler OnUpdated;  // the event that signals that the drawing has been updated -- controls will listen for this at the time they are created.

        bool hideImage = false;
        bool showDesign = true; // schedule marks, hold-downs and the schedule on the plan

        /// <summary>A new story's height: the building model's default (the one source).</summary>
        private double defaultWallHeight => (building?.Defaults ?? new BuildingModel.Model.ModelDefaults()).StoryHeight;

        private bool selectionCardOpen;                    // the card shows the picked wall or diaphragm
        private bool debugMode = false;


        public MainWindow()
        {
            InitializeComponent();
            BuildingModel.Sketch.WindowFit.Fit(this); // on a smaller screen: fits, title bar on screen
            LoadRecentFilesMenu();

            this.KeyDown += MainWindow_KeyDown;
            this.PreviewKeyDown += FileShortcuts; // Ctrl+N, O, S, P
            this.Closing += MainWindow_Closing; // asks to save unsaved changes
            Log.Written += OnLogWritten;      // warnings and errors show on the status line
            this.TextInput += RouteTypingToCommand;  // typing a number while drawing goes to the Point box
            selectionCard.OpenDesign += SelectionCard_OpenDesign;
            selectionCard.EditGeometry += SelectionCard_EditGeometry;
            selectionCard.DeleteRequested += SelectionCard_Delete;
            selectionCard.OpenDetail += (kind, id) => OpenDetail(kind, Math.Max(0, currentLevel), id, null);
            selectionCard.Closed += (s, e) => CloseSelectionCard();
            // the wall card's hold-down box: the Design table's choices; a pick is the wall's design input, as from the table
            selectionCard.HoldownItems = (w, r) => designer == null ? null
                : Controls.HoldownPicks.For(designer, w.Design ?? new WallDesignInputs(), r, (Calculator?.DesignSettings ?? new DesignSettings()).StrapHoldownsOrDefault());
            selectionCard.DesignEdit += (wallId, what, edit) =>
            {
                if (Calculator == null || !Calculator._wall_system._walls.TryGetValue(wallId, out var w)) return;
                w.Design = w.Design ?? new WallDesignInputs();
                edit(w.Design);
                Log.Info("Design", $"{CurrentStory?.Level.Name} wall {wallId}: {what} (from the plan card)");
                // rebuilt after this event: the box that raised it is on the card
                Dispatcher.BeginInvoke(new Action(UpdateShearWallUI), System.Windows.Threading.DispatcherPriority.Input);
            };
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
                windSetup.StepChanged += (s2, e2) => WindStepChanged(); // the result tabs on the Results page only
                windSetup.SendLoadsRequested += WindSetup_SendLoadsRequested;
                windSetup.ShearWallsRequested += (s2, e2) => rbWorkspaceWalls.IsChecked = true;
                windSetup.EditBuildingRequested += (s2, e2) => EditBuilding(); // the size and roof are edited in the Building Model editor
                tabWindSetupTabItem.Content = windSetup;
                windLoadParams = windSetup.Parameters.Clone();
                windVersion = windSetup.Version;
                // (the building's size, stories and roof come from the model only: the old BuildingData and WIND input tabs are gone)

                // create the wind load results controls
                CreateAndAssignResultControls();


                InitSketch();
                SetBuilding(ShearWallAdapter.NewBuilding(null), null); // behind the start screen until a project is open
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

                // nothing is designed without a project: open the one given (BuildingModel.exe's "Design shear walls", a file
                // opened from Explorer), else the start screen
                string arg = Environment.GetCommandLineArgs().Skip(1).FirstOrDefault(a => a.EndsWith(".json", StringComparison.OrdinalIgnoreCase));
                if (arg == null || !File.Exists(arg) || !OpenProject(arg)) ShowStartScreen();
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

                geometryPanel?.Show(MemberLevels(), currentLevel, selectedKind, selectedId);
                ShowFramingRows(); // the beams and columns of the level shown
                if (resultsPanel != null) resultsPanel.Notes = levelNotes;
                resultsPanel?.Show(Calculator, CalculationTodo(), selectedKind == "Wall" ? selectedId : -1);
                if (selectionCardOpen && (selectedKind == "Beam" || selectedKind == "Column")) ShowMemberCard();
                else selectionCard.Show(Calculator, selectionCardOpen ? selectedKind : null, selectedId, designResults);

                // notify controls that we have updated
                OnUpdated?.Invoke(this, EventArgs.Empty); // signal that the window has been updated -- so that subcontrols can refresh

                // update the load info display
                LoadInfoTextBlock.Text = foundationView
                    ? $"Base shear   X {levels.Sum(l => l.V_x):0.0} k,  Y {levels.Sum(l => l.V_y):0.0} k"
                    : (windLoadsLinked ? "Wind loads (auto)" : "Manual loads") + (levels.Count > 1 ? " on this level" : "") + $"   X {Calculator.V_x:0.0} k,  Y {Calculator.V_y:0.0} k";
                ShowWorkspaceLoads(); // the loads table beside the workspace buttons
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
            selectionCard.Designer = designer; // the wall card's sheathing boxes: the Design table's choices
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
            geometryPanel.GeometryChanged += GeometryTyped; // typed coordinates go to the model (that member's level)
            geometryPanel.SelectionChanged += (kind, story, id) => SelectOnLevel(story, kind, id);
            geometryPanel.DeleteRequested += (kind, story, id) => DeleteMember(story >= 0 && story < stories.Count ? stories[story].ModelId(kind, id) : CurrentStory?.ModelId(kind, id));
            geometryPanel.FramingDeleteRequested += DeleteMember; // from the model: every level's list follows
            geometryPanel.FramingSelected += id => Sketch.SelectAndShow(id); // SelectionChanged updates the rest
            tabDimensionsTabItem.Content = geometryPanel; // every level (its own filter), so no level banner
            resultsPanel = new ShearResultsPanel();
            resultsPanel.WallSelected += id => SelectFromTable("Wall", id);
            resultsPanel.WallDeleteRequested += id => DeleteFromTable("Wall", id);
            resultsPanel.CaseChosen += name => ShowLoadsCase(name, fromPanel: true); // the plan's Loads overlay follows the case shown
            resultsPanel.DiaphragmChosen += kind =>
            {
                // one setting for the project (every level shares the settings object); a model change, so Ctrl+Z undoes it
                if (Calculator?.DesignSettings == null || Calculator.DesignSettings.Diaphragm == kind) return;
                Log.Info($"Diaphragm: {Calculator.DesignSettings.Diaphragm} → {kind} (Calculations tab).");
                foreach (var l in levels) if (l.DesignSettings != null) l.DesignSettings.Diaphragm = kind;
                UpdateShearWallUI();
            };
            tabCalculationsTabItem.Content = WithLevelBanner(resultsPanel);

            designPanel = new WallDesignPanel();
            designPanel.DesignChanged += (s, e) => UpdateShearWallUI();
            designPanel.AutoSelectRequested += AutoSelectWalls;
            designPanel.WallSelected += (story, id) => SelectOnLevel(story, "Wall", id);
            designPanel.WallDeleteRequested += (story, id) => { SelectOnLevel(story, "Wall", id); DeleteFromTable("Wall", id); };
            designPanel.ScopeChanged += (s, e) => UpdateLevelBanners();
            tabDesignTabItem.Content = WithLevelBanner(designPanel, () => designPanel.ScopeDescription); // its own Floors filter, not the plan's level
        }

        /// <summary>A wall picked in a table of every floor: shows its floor first when it is another one.</summary>
        private void SelectOnLevel(int story, string kind, int id)
        {
            if (story != currentLevel && story >= 0 && story < stories.Count)
            {
                ShowLevel(story);
                selectedKind = kind;
                selectedId = id;
            }
            SelectFromTable(kind, id);
        }

        /// <summary>The check of every wall on every floor (the table lists them all); index = story.</summary>
        private List<Dictionary<int, WallDesignResult>> allDesignResults = new List<Dictionary<int, WallDesignResult>>();

        /// <summary>Checks every wall of every floor for its governing shear (called after the wall calculations).</summary>
        private void UpdateDesign()
        {
            designResults = new Dictionary<int, WallDesignResult>();
            if (designPanel == null) return;

            var calcs = levels.Count > 0 ? levels : new List<ShearWallCalculatorBase> { Calculator };
            string blocked = ModelErrorTodo(); // no project, or the model has errors: no design
            allDesignResults = blocked != null ? calcs.Select(_ => new Dictionary<int, WallDesignResult>()).ToList() : CheckAllLevels(calcs);
            var list = new List<WallDesignPanel.DesignStory>();
            for (int i = 0; i < calcs.Count; i++)
            {
                var c = calcs[i];
                string message = null, fixLabel = null;
                Action fix = null;
                int story = i;
                if (blocked != null)
                {
                    message = blocked;
                    if (HasProject) { fix = () => { rbWorkspaceWalls.IsChecked = true; ShowIssueList(btnIssues); }; fixLabel = "List the errors"; }
                }
                else if (c == null || !c.IsValidForCalculation)
                {
                    message = "draw a diaphragm and walls in both directions first.";
                    fix = () => { rbWorkspaceWalls.IsChecked = true; ShowLevel(story); };
                    fixLabel = $"Show {LevelName(i)}'s plan";
                }
                else if (allDesignResults[i].Count == 0)
                {
                    message = "no wall shears yet: add loads or link the wind loads.";
                    fix = () => btnOpenLoadDialog_Click(btnLoadEntry, new RoutedEventArgs());
                    fixLabel = "Open Loads";
                }
                list.Add(new WallDesignPanel.DesignStory { Index = i, Name = LevelName(i), Calculator = c, Results = allDesignResults[i], Message = message, Fix = fix, FixLabel = fixLabel });
                if (ReferenceEquals(c, Calculator)) designResults = allDesignResults[i];
            }
            if (list.Count == 1 && list[0].Message != null) list[0].Message = char.ToUpper(list[0].Message[0]) + list[0].Message.Substring(1);
            designPanel.Show(list, Math.Max(0, levels.IndexOf(Calculator)), designer, windLoadsLinked);
            LogDesign(list); // the levels' messages and the failing and close walls
            if (selectedKind == "Wall") designPanel.SelectWall(Math.Max(0, levels.IndexOf(Calculator)), selectedId); // the same wall as on the drawing and the other tabs
            var sthd = Calculator?.DesignSettings?.StrapHoldownsOrDefault() ?? new StrapHoldownConditions();
            if (holdownCatalog != null && holdownCatalog.StrapConditions != sthd.Describe()) holdownCatalog.SetStrapConditions(sthd); // the STHD loads follow the project
            holdownCatalog?.SetDesign(DesignByFloor());
            ComputeLandings(); // where every hold-down lands: on a wall below, the foundation, or nothing
        }

        /// <summary>Every floor's checks, roof down, so each hold-down carries the walls stacked on it; index = story.</summary>
        private List<Dictionary<int, WallDesignResult>> CheckAllLevels(IList<ShearWallCalculatorBase> calcs)
        {
            var all = new Dictionary<int, WallDesignResult>[calcs.Count];
            loadPath = DesignTopDown(calcs, 0, all);
            return all.ToList();
        }

        /// <summary>The last load-path trace of the hold-downs (beams, columns, the foundation), from <see cref="CheckAllLevels"/>.</summary>
        private LoadPathTrace loadPath;

        /// <summary>
        /// The stories from the top down to <paramref name="down"/>: each is checked with the forces from above (the walls on
        /// the same line, plus the beam ends and column bases standing on it), then its hold-downs are traced to what carries them.
        /// </summary>
        private LoadPathTrace DesignTopDown(IList<ShearWallCalculatorBase> calcs, int down, Dictionary<int, WallDesignResult>[] all)
        {
            var trace = NewLoadPath(calcs);
            Dictionary<int, WallDesignResult> above = null;
            for (int k = calcs.Count - 1; k >= down; k--)
            {
                all[k] = above = CheckLevel(calcs[k], k + 1 < calcs.Count ? calcs[k + 1] : null, above, k + 2, trace.ExtraOn(k));
                trace.Land(k, calcs[k]?._wall_system?._walls, above, k > 0 ? calcs[k - 1]?._wall_system?._walls : null);
            }
            return trace;
        }

        /// <summary>A trace with the model's beams and columns by story, sizing new connections from the project's catalog.</summary>
        private LoadPathTrace NewLoadPath(IList<ShearWallCalculatorBase> calcs)
        {
            var (beams, columns) = Framing();
            var settings = calcs.FirstOrDefault(c => c != null)?.DesignSettings ?? new DesignSettings();
            return new LoadPathTrace(beams, columns, (f, species, t) =>
                designer?.SelectHoldown(f, species, t, settings.HoldownsFromStandardList, settings.StrapHoldownsOrDefault()));
        }

        /// <summary>
        /// The check of each wall of one level for its governing shear (empty when the level cannot be calculated). With
        /// several floors the levels above are checked first, roof down, so each hold-down carries the walls stacked on it.
        /// </summary>
        private Dictionary<int, WallDesignResult> CheckWalls(ShearWallCalculatorBase calc)
        {
            int i = levels.IndexOf(calc);
            if (i < 0) return CheckLevel(calc, null, null, 0);
            var all = new Dictionary<int, WallDesignResult>[levels.Count];
            DesignTopDown(levels, i, all);
            return all[i];
        }

        private Dictionary<int, WallDesignResult> CheckLevel(ShearWallCalculatorBase calc, ShearWallCalculatorBase upper,
            Dictionary<int, WallDesignResult> upperDesign, int upperLevel, Dictionary<int, List<StackedForce>> extra = null)
        {
            var results = new Dictionary<int, WallDesignResult>();
            if (calc == null || designer == null || !calc.IsValidForCalculation) return results;
            // every wall's force in every case: its hold-down tension case by case (the load path adds those that share a support)
            var byWall = ShearWallCalculator.CaseBreakdown.Rows(calc).GroupBy(x => x.Wall).ToDictionary(g => g.Key, g => g.ToList());
            foreach (var kv in calc._wall_system._walls)
            {
                if (!calc.TotalWallShear.TryGetValue(kv.Key, out double v)) continue;
                calc.GoverningLoadCase.TryGetValue(kv.Key, out string governing);
                var above = upper == null ? null : StoryCascade.HoldownForcesOn(kv.Value, upper._wall_system._walls, upperDesign, upperLevel);
                if (extra != null && extra.TryGetValue(kv.Key, out var more)) (above = above ?? new List<StackedForce>()).AddRange(more);
                results[kv.Key] = designer.Check(kv.Key, kv.Value, v, governing, calc.DesignSettings, null, above);
                if (byWall.TryGetValue(kv.Key, out var rows))
                    results[kv.Key].SetCaseTensions(rows.GroupBy(x => x.Case).Select(g => new KeyValuePair<string, double>(g.Key, g.First().Total)));
            }
            return results;
        }

        private Dialogs.HoldownCatalogWindow holdownCatalog;

        /// <summary>Every floor's wall checks by floor name, bottom up (for the catalog's "Used on walls").</summary>
        private List<KeyValuePair<string, IReadOnlyDictionary<int, WallDesignResult>>> DesignByFloor() =>
            allDesignResults.Count == 0
                ? new List<KeyValuePair<string, IReadOnlyDictionary<int, WallDesignResult>>> { new KeyValuePair<string, IReadOnlyDictionary<int, WallDesignResult>>("", designResults) }
                : allDesignResults.Select((r, i) => new KeyValuePair<string, IReadOnlyDictionary<int, WallDesignResult>>(LevelName(i), r)).ToList();

        /// <summary>The hold-down catalog the design selects from, with the walls using each option (kept current).</summary>
        private void OpenHoldownCatalog_Click(object sender, RoutedEventArgs e)
        {
            if (designer == null) return;
            if (holdownCatalog == null)
            {
                holdownCatalog = new Dialogs.HoldownCatalogWindow(designer, Calculator?.DesignSettings?.StrapHoldownsOrDefault()) { Owner = this };
                holdownCatalog.ProjectName = () => ProjectName;
                holdownCatalog.Closed += (s, a) => holdownCatalog = null;
            }
            holdownCatalog.SetDesign(DesignByFloor());
            holdownCatalog.Show();
            holdownCatalog.Activate();
        }

        /// <summary>The lightest passing edge nailing (then both sides) for every wall of these floors, keeping its panel and hold-down type.</summary>
        private void AutoSelectWalls(IList<int> storyIndexes)
        {
            var calcs = levels.Count > 0 ? levels : new List<ShearWallCalculatorBase> { Calculator };
            foreach (int i in storyIndexes ?? Enumerable.Range(0, calcs.Count).ToList())
            {
                var c = i >= 0 && i < calcs.Count ? calcs[i] : null;
                if (c == null) continue;
                foreach (var kv in c._wall_system._walls)
                    if (c.TotalWallShear.TryGetValue(kv.Key, out double v))
                        kv.Value.Design = designer.AutoSelect(kv.Value, v, c.DesignSettings);
            }
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
            string todo = ModelErrorTodo(); // no project, or a model with errors: nothing is designed
            if (todo != null) { }
            else if (diaphragms == 0)
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
        private WindSetupControl windSetup;

        /// <summary>
        /// The guided wind screen changed (the site, the enclosure, the components: the wind app's own inputs). The building's
        /// size, stories and roof there are the model's (<see cref="ModelToWind"/>), so nothing here changes the model.
        /// </summary>
        private void WindSetup_InputsChanged(object sender, EventArgs e)
        {
            // Unusable input (e.g. a blank wind speed), or no building to calculate (no diaphragm yet): clear the results
            // everywhere rather than leave old numbers showing; the setup screen says what to fix.
            if (!windSetup.GeometryFromModel || WindInputValidator.HasErrors(windSetup.Issues) || windSetup.Analysis == null)
            {
                Log.Warning("Wind", "The wind inputs cannot be calculated: " + string.Join("; ", windSetup.Issues.Select(i => i.Message).DefaultIfEmpty("no building geometry from the model")), null);
                windLoadCalculator_CC = windLoadCalculator_MWFRS_Length = windLoadCalculator_MWFRS_Width = null;
                CreateAndAssignResultControls();
                ApplyLinkedWindLoads();
                UpdateShearWallUI();
                return;
            }

            buildingData = windSetup.Building.Clone();
            windLoadParams = windSetup.Parameters.Clone();
            windVersion = windSetup.Version;
            modelWindKey = null; // the enclosure is part of the wind geometry: send it again on the next model change

            RecalculateWind();
            UpdateShearWallUI();
            LogWindInputs();
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
            if (windSetup != null) WindStepChanged(); // re-added tabs follow the setup's page
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
            BuildingModel.Sketch.WindowFit.Fit(new Window
            {
                Title = "3D Wind View",
                Owner = this,
                Width = 1280,
                Height = 800,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = viewer,
            }).Show();
        }

        #endregion

        #region Drawing functions

        /// <summary>Where each wall's design tag was drawn this frame (screen), by wall key: a click on a tag picks its wall.</summary>
        private readonly Dictionary<int, Rect> tagHits = new Dictionary<int, Rect>();

        /// <summary>
        /// A left click on a wall's design tag (Select tool): the wall is picked and its card opens, as a click on the wall
        /// does (Shift or Ctrl adds it to the selection). True: the click was used.
        /// </summary>
        private bool ClickOnTag(BuildingModel.Geometry.Point2 world)
        {
            if (Sketch.Tool != BuildingModel.Sketch.SketchTool.Select || tagHits.Count == 0) return false;
            var p = Sketch.ToScreen(world);
            var hit = tagHits.Where(kv => kv.Value.Contains(p)).Select(kv => (int?)kv.Key).FirstOrDefault();
            string modelId = hit == null ? null : CurrentStory?.ModelId("Wall", hit.Value);
            if (modelId == null) return false;
            bool add = (Keyboard.Modifiers & (ModifierKeys.Shift | ModifierKeys.Control)) != 0;
            Sketch.Select(new[] { modelId }, add); // SelectionChanged opens the card
            Log.Info("Select", $"{modelId} picked by its design tag.");
            return true;
        }

        /// <summary>Schedule marks, hold-downs and the schedule over the plan (SHOW Design).</summary>
        private void DrawWallDesign(DrawingContext ctx)
        {
            tagHits.Clear();
            if (!showDesign || Calculator?._wall_system == null) return;
            DesignOverlay.Draw(ctx, Calculator._wall_system._walls, designResults, p => WorldToScreen(p, m_layers),
                               new Size(m_layers.ActualWidth, m_layers.ActualHeight), VisualTreeHelper.GetDpi(this).PixelsPerDip, LabelScale, labelsTaken,
                               schedule: false, markLevel: MarkLevel(currentLevel), tagBoxes: tagHits, holdowns: false); // (the hold-downs: DrawPlanHoldowns, each labelled) the schedule is in the drawing key, beside the plan
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
                    labelsTaken.Add(new Rect(p1_screen.X - 40, p1_screen.Y - 7, idLabel.Width, idLabel.Height)); // the hold-down labels keep off it

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
                    // turned a quarter about (x, y + 10): it runs down from there, its height to the left
                    labelsTaken.Add(new Rect(p1_screen.X - (idLabel.Height - 10), p1_screen.Y + 10, idLabel.Height, idLabel.Width));
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

                    // (its coordinates are on the Calculations card; the corner of the plan stays clear)

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

                // (its coordinates are on the Calculations card)
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
        // ------------------------------------------------------------------ the plan under the level

        // The plans are the building's (BuildingModel.Sketch.PlanImage): the sketch draws them, and snaps to CAD linework;
        // the Building Model editor imports, scales, places, moves and removes them. Here they are only shown or hidden.

        /// <summary>The Image ▾ menu: show or hide, zoom to the plan, and where plans are edited.</summary>
        private void btnImageOptions_Click(object sender, RoutedEventArgs e)
        {
            var plan = Sketch.PlanOf(Sketch.ActiveLevel);
            var menu = new ContextMenu { PlacementTarget = (UIElement)sender, Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom };
            void Item(string header, Action act, bool enabled = true, string tip = null)
            {
                var mi = new MenuItem { Header = header, IsEnabled = enabled, ToolTip = tip };
                mi.Click += (s, a) => act();
                menu.Items.Add(mi);
            }
            Item(hideImage ? "Show the plan" : "Hide the plan", () => btnHideImage_Click(btnHideImage, null), plan != null, "Only on this screen: the plan stays in the building model.");
            Item("Zoom to the plan", ZoomToImage, plan != null);
            menu.Items.Add(new Separator());
            Item("Import or place plans in the Building Model editor...", EditBuilding,
                 tip: "The plans are part of the building: they are imported, scaled, moved, copied to another floor and removed in the Building Model editor.");
            menu.IsOpen = true;
        }

        private void ShowImageName()
        {
            var p = Sketch?.PlanOf(Sketch.ActiveLevel);
            if (p == null)
            {
                tbImageFileName.Text = "";
                tbImageFileName.ToolTip = null;
                return;
            }
            tbImageFileName.Text = "Plan: " + System.IO.Path.GetFileName(p.File) + (string.IsNullOrEmpty(p.Source) ? "" : $" ({p.Source})") + (p.Calibrated ? "" : "  (scale not set)");
            tbImageFileName.ToolTip = p.FullPath(null) + Environment.NewLine + "Imported and placed in the Building Model editor (Image ▾ or Edit building).";
        }

        /// <summary>Zooms so the whole plan shows, with a margin.</summary>
        private void ZoomToImage()
        {
            var b = Sketch.PlanOf(Sketch.ActiveLevel)?.WorldBounds() ?? Rect.Empty;
            if (!b.IsEmpty && b.Width > 0 && b.Height > 0) Sketch.ZoomTo(b);
        }
        /// <summary>After New or Open: the project's image (older projects moved over), shown as it was saved.</summary>
        /// <summary>The model as last logged; each change is logged as its differences from it.</summary>
        private ModelSnapshot loggedModel;

        /// <summary>
        /// Logs what changed in the model since the last call (walls and diaphragms added, deleted or resized; sheathing,
        /// nailing, framing, dead load, hold-down type; design settings; loads), whichever panel changed it.
        /// </summary>
        private void LogModelChanges()
        {
            try
            {
                var now = ModelSnapshot.Take(Calculator, designer);
                if (loggedModel == null)
                    Log.Info($"Model: {now.WallGeometry.Count} walls, {now.Diaphragms.Count} diaphragms" + "");
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
            loggedModel = null; // a different project: start the change log afresh
            hideImage = !Sketch.ShowUnderlay;
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
            designPanel?.SelectWall(Math.Max(0, levels.IndexOf(Calculator)), id);
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
            if (Sketch.Tool == BuildingModel.Sketch.SketchTool.Select) return;
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
            Sketch.ShowUnderlay = !hideImage; // this screen only: the plan stays in the building model
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
                    // Manual loads only in a project without wind inputs: a project that has them always takes the wind loads.
                    if (windLoadsLinked && MessageBox.Show(this,
                            "This project takes its loads from its wind inputs, so manual loads are not used.\n\n" +
                            "Use manual loads instead? The wind inputs are then left out of the project when it is saved " +
                            "(the button on the Wind Setup screen brings them back while the project is open).",
                            "Manual loads", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes)
                        return;
                    // A manual load is the user's own number; never overwrite it, but say so on the wind screen.
                    Calculator.AddLoads(dialog.MagnitudeX, dialog.MagnitudeY);
                    Log.Info("Loads", $"Manual load on {LevelName(currentLevel)}: X {dialog.MagnitudeX:0.##} k, Y {dialog.MagnitudeY:0.##} k (the project has no wind inputs now).");
                    UnlinkWindLoads($"They use your manual load (X = {dialog.MagnitudeX:0.##} k, Y = {dialog.MagnitudeY:0.##} k); the project has no wind inputs. " +
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
            string blocked = ModelErrorTodo();
            if (blocked != null)
            {
                MessageBox.Show(this, blocked, "Print report", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            windSetup?.FlushPendingChange();
            var analysis = windLoadCalculator_MWFRS_Length != null && windLoadCalculator_MWFRS_Width != null
                ? new WindAnalysisResult(windLoadCalculator_CC, windLoadCalculator_MWFRS_Length, windLoadCalculator_MWFRS_Width)
                : null;
            var info = new ReportInfo { Project = ProjectName, Program = "Shear Wall Calculator" };
            var report = WindReport.Build(info, windVersion, windLoadParams, buildingData, analysis, ShearWallReport.Levels(levels.Count > 0 ? levels : new List<ShearWallCalculatorBase> { Calculator }, windLoadsLinked,
                c => ReferenceEquals(c, Calculator) ? designResults : CheckWalls(c), designer, levelNotes).Concat(DetailReportSections()));
            Log.Info("Report", $"Report previewed for {ProjectName}: {levels.Count} stor{(levels.Count == 1 ? "y" : "ies")}, {(windLoadsLinked ? "wind" : "manual")} loads.");
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
            Sketch.ResetCommand();
        }
        #endregion

        // ------------------------------------------------------------------ status line

        private static readonly Brush StatusWarnInk = new SolidColorBrush(Color.FromRgb(0x9A, 0x5B, 0x00));
        private static readonly Brush StatusErrorInk = Controls.Ui.FailInk;

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

        private bool InWindWorkspace => rbWorkspaceWind.IsChecked == true;

        /// <summary>Tabs that belong to the Wind loads workspace; the rest belong to Shear walls.</summary>
        private bool IsWindTab(TabItem tab) =>
            tab == tabWindSetupTabItem || tab == tabWindDetailsTabItem || tab == tabWind3DTabItem || tab == tabBuildingDataControlTabItem ||
            tab == tabWindInputControlTabItem || tab == tabWindResultsTabItem_CC ||
            tab == tabWindResultsTabItem_MWFRS_BldgLength || tab == tabWindResultsTabItem_MWFRS_BldgWidth;

        /// <summary>
        /// Detailed calculations and 3D Wind show the results of the inputs, so they appear on the setup's Results page only
        /// (after Site and Building have been gone through), not beside the first page.
        /// </summary>
        private bool WindTabReady(TabItem tab) =>
            !(tab == tabWindDetailsTabItem || tab == tabWind3DTabItem) || windSetup?.OnResults == true;

        /// <summary>Shows or hides Detailed calculations and 3D Wind (and the loads table) for the workspace and the setup's page.</summary>
        private void WindStepChanged()
        {
            bool wind = InWindWorkspace;
            foreach (var tab in new[] { tabWindDetailsTabItem, tabWind3DTabItem })
                tab.Visibility = wind && WindTabReady(tab) ? Visibility.Visible : Visibility.Collapsed;
            if (wind && MainTabControl.SelectedItem is TabItem t && t.Visibility != Visibility.Visible) MainTabControl.SelectedItem = tabWindSetupTabItem;
            ShowWorkspaceLoads();
            Log.Debug($"Wind setup page {windSetup?.Step + 1}: the result tabs and the loads table {(windSetup?.OnResults == true ? "shown" : "hidden")}");
        }

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
                tab.Visibility = IsWindTab(tab) == wind && WindTabReady(tab) ? Visibility.Visible : Visibility.Collapsed;
            WindStepChanged(); // the result tabs and the loads table follow the setup's page

            // The canvas is Hidden (not Collapsed) so it keeps its size: its zoom/pan math reads
            // ActualWidth/ActualHeight. The tab area simply stretches over it.
            dockpanel.Visibility = wind ? Visibility.Hidden : Visibility.Visible;
            drawingSplitter.Visibility = wind ? Visibility.Collapsed : Visibility.Visible;
            Grid.SetColumn(MainTabControl, wind ? 0 : 3);
            Grid.SetColumnSpan(MainTabControl, wind ? 5 : 1);
            spDrawingToolbar.Visibility = wind ? Visibility.Collapsed : Visibility.Visible;

            if (!(MainTabControl.SelectedItem is TabItem current) || current.Visibility != Visibility.Visible)
                MainTabControl.SelectedItem = wind ? tabWindSetupTabItem : (TabItem)tabCalculationsTabItem;
            ApplyPanels();      // the PLAN / DATA flyouts (shear walls only)
            if (!wind) Draw();
            UpdateDrawingKey(); // the drawing key belongs to the Shear walls workspace
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
