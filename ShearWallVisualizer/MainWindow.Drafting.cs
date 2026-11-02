using BuildingModel.Diagnostics;
using BuildingModel.Checks;
using BuildingModel.Edits;
using BuildingModel.Geometry;
using BuildingModel.Model;
using BuildingModel.Serialization;
using ShearWallCalculator.ModelAdapter;
using BuildingModel.Sketch;
using calculator;
using Microsoft.Win32;
using ShearWallCalculator;
using ShearWallCalculator.Design;
using ShearWallCalculator.Diagnostics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace ShearWallVisualizer
{
    /// <summary>
    /// The plan is drawn in the shared sketcher (BuildingModel.Sketch) and kept in a building model (BuildingModel.Core),
    /// the source of truth for the geometry: levels, walls, diaphragms, openings and notes. After every change the model
    /// is put into the calculators, one per story (<see cref="ShearWallAdapter.ToCalculators"/>); what the design tabs
    /// change (sheathing, nailing, hold-downs, diaphragm type, design settings) is written back into the model's data
    /// bags (<see cref="ShearWallAdapter.StoreDesign"/>), so one undo history covers drawing and design. The sketch has
    /// the drawing tools, snaps, typed points, undo and the status line; this file adds the designer's overlays (design
    /// colours and marks, braced wall lines, COM / COR, the plan image and its commands) and the error markers.
    /// </summary>
    public partial class MainWindow
    {
        /// <summary>The building: every level, wall, diaphragm, opening and note, with the designer's data in their bags.</summary>
        private Building building = Building.New();

        /// <summary>The designer's stories (bottom up): level, calculator, and calculator key ↔ model ID.</summary>
        private List<StoryMap> stories = new List<StoryMap>();

        /// <summary>Each model level's calculator, so its loads and plan image stay with the level when others are added or deleted.</summary>
        private readonly Dictionary<string, ShearWallCalculatorBase> calcByLevel = new Dictionary<string, ShearWallCalculatorBase>();

        /// <summary>True while the designer's data is written into the model (that commit does not rebuild the calculators).</summary>
        private bool storing;

        /// <summary>
        /// True while the panels update after the sketch changed the model, the level or the selection: design data filled in
        /// then (a new wall's default design) amends that step instead of being a step of its own, so an undo never loses its redo.
        /// </summary>
        private bool amendDesign;

        private void UpdateFromSketch()
        {
            amendDesign = true;
            try { UpdateShearWallUI(); }
            finally { amendDesign = false; }
        }

        /// <summary>The model as opened or saved: Model > Export changes compares with it.</summary>
        private string baseModelJson;

        private SketchCanvas m_layers => sketchView.Canvas;
        private SketchCanvas Sketch => sketchView.Canvas;
        private StoryMap CurrentStory => currentLevel >= 0 && currentLevel < stories.Count ? stories[currentLevel] : null;

        private static Point ToPoint(Point2 p) => new Point(p.X, p.Y);
        private static Point2 ToPoint2(Point p) => new Point2(p.X, p.Y);

        private void InitSketch()
        {
            sketchView.ShowLevelBar = false;    // the LEVEL buttons above choose the level
            sketchView.MaxStories = FloorEdits.MaxFloors; // New from size: the designer handles up to this many floors
            sketchView.ShowPlanButton = false;  // Image ▾ above imports the plan (PDF, DXF, picture) per level
            sketchView.HideTool(SketchTool.Calibrate);
            var s = Sketch;
            s.DrawUnder += (o, e) => DrawReferenceImage(e.Context);
            s.DrawOver += (o, e) =>
            {
                DrawBracedWallLines(e.Context);
                DrawWallDesign(e.Context);
                DrawCOMandCOR(e.Context);
                DrawImageTool(e.Context);
                DrawDebug(e.Context);
            };
            s.WallStyle = DesignPen;
            s.LevelCaption = LevelCaption;
            s.InterceptClick = p =>
            {
                if (!imageTool.Active || Calculator?.Image == null) return false;
                ImageToolClick(ToPoint(p));
                return true;
            };
            s.HostWantsSnaps = () => imageTool.Active && imageTool.WantsSnaps;
            s.HostFrom = () => imageTool.Active && imageTool.Points.Count > 0 ? ToPoint2(imageTool.Points[imageTool.Points.Count - 1]) : (Point2?)null;
            s.ExtraSnapLines = PlanLinesNear;
            s.InterceptTyped = text =>
            {
                if (!imageTool.WaitingForDistance) return false;
                EnterImageDistance(text);
                return true;
            };
            s.Escaped += (o, e) =>
            {
                if (imageTool.Active) { imageTool.Cancel(); Draw(); }
                else if (selectionCardOpen) CloseSelectionCard();
            };
            s.ModelChanged += (o, e) => { if (!storing) ModelToCalculators(); };
            s.ActiveLevelChanged += (o, e) => ActiveLevelChanged();
            s.SelectionChanged += (o, e) => SelectionFromSketch();
            s.ObjectDoubleClicked += (o, id) =>
            {
                var key = CurrentStory?.Key(id);
                if (key == null || !(building.Find(id) is Wall)) return;
                SelectionCard_OpenDesign("Wall", key.Value);
            };
            s.SetBuilding(building, ShearWallAdapter.Stories(building).FirstOrDefault()?.Id);
        }

        /// <summary>Redraws the plan (the overlays read the calculators; nothing is recalculated).</summary>
        private void Draw() => Sketch?.InvalidateVisual();

        private void RedrawDrawing() => Draw();

        // ------------------------------------------------------------------ model → calculators

        /// <summary>
        /// Puts a building into the app (New, Open, an imported model): the calculators of <paramref name="existing"/> stay
        /// with the stories in order (their loads and plan images), and the undo history starts afresh.
        /// </summary>
        private void SetBuilding(Building b, IList<ShearWallCalculatorBase> existing, int active = 0)
        {
            building = b ?? Building.New();
            var levelsUp = ShearWallAdapter.Stories(building);
            if (levelsUp.Count == 0) building.AddLevel("Level 2", defaultWallHeight); // the designer needs a story
            levelsUp = ShearWallAdapter.Stories(building);
            calcByLevel.Clear();
            for (int i = 0; i < levelsUp.Count && existing != null && i < existing.Count; i++)
                if (existing[i] != null) calcByLevel[levelsUp[i].Id] = existing[i];
            selectedKind = null;
            selectedId = -1;
            selectionCardOpen = false;
            currentLevel = Math.Max(0, Math.Min(active, levelsUp.Count - 1));
            storing = true;
            try { Sketch.SetBuilding(building, levelsUp[currentLevel].Id); }
            finally { storing = false; }
            baseModelJson = ModelFile.BuildingToJson(building);
            ModelToCalculators();
            Sketch.ZoomExtents();
        }

        /// <summary>After New or Open: the undo history starts here, and Model > Export changes compares with this model.</summary>
        private void StartHistory()
        {
            Sketch.History.Reset(building);
            sketchView.UpdateButtons();
            baseModelJson = ModelFile.BuildingToJson(building);
        }

        /// <summary>After any change to the model: the calculators are filled again from it and everything is recalculated.</summary>
        private void ModelToCalculators()
        {
            var levelsUp = ShearWallAdapter.Stories(building);
            var existing = levelsUp.Select(l => calcByLevel.TryGetValue(l.Id, out var c) ? c : null).ToList();
            stories = ShearWallAdapter.ToCalculators(building, existing);
            foreach (var s in stories) calcByLevel[s.Level.Id] = s.Calculator;
            levels = stories.Select(s => s.Calculator).ToList();
            int i = stories.FindIndex(s => s.Level.Id == Sketch.ActiveLevel?.Id);
            currentLevel = i >= 0 ? i : Math.Max(0, Math.Min(currentLevel, levels.Count - 1));
            if (i < 0 && stories.Count > 0) { storing = true; try { Sketch.ShowLevel(stories[currentLevel].Level.Id); } finally { storing = false; } }
            Calculator = levels.Count > 0 ? levels[currentLevel] : new ShearWallCalculator_RigidDiaphragm();
            FloorsToBuildingData();
            UpdateLevelButtons();
            UpdateFromSketch();
        }

        /// <summary>The level shown in the sketch changed (LEVEL buttons, or a selected issue on another level).</summary>
        private void ActiveLevelChanged()
        {
            int i = stories.FindIndex(s => s.Level.Id == Sketch.ActiveLevel?.Id);
            if (i < 0 || (i == currentLevel && ReferenceEquals(Calculator, levels[i]))) return;
            imageTool.Cancel();
            selectedKind = null;
            selectedId = -1;
            selectionCardOpen = false;
            currentLevel = i;
            Calculator = levels[i];
            ImageForNewProject(); // each level has its own plan image
            UpdateLevelButtons();
            UpdateFromSketch();
            ShowStatusMessage(LogLevel.Info, $"Editing {LevelName(i)}." + (i > 0 ? $" {LevelName(i - 1)} shows in gray and its walls snap." : ""));
        }

        /// <summary>
        /// Writes what the design tabs changed into the model's bags (and typed coordinates, after a table edit); a change
        /// is one undo step. Called after every update, like the old drawing history.
        /// </summary>
        private void StoreDesignInModel()
        {
            if (stories.Count == 0 || storing) return;
            if (!ShearWallAdapter.StoreDesign(building, stories)) return;
            if (amendDesign) { Sketch.History.Amend(building); return; } // filled in after a change, an undo or a redo: part of that step
            storing = true;
            try { Sketch.Commit("Design changed."); }
            finally { storing = false; }
        }

        /// <summary>Coordinates typed on the Walls &amp; diaphragms tab go to the model (then everything follows from it).</summary>
        private void GeometryTyped()
        {
            var s = CurrentStory;
            if (s == null) { UpdateShearWallUI(); return; }
            ShearWallAdapter.StoreDesign(building, stories); // a height or type typed there as well
            if (!ShearWallAdapter.StoreGeometry(building, s)) { UpdateShearWallUI(); return; }
            Sketch.Commit("Coordinates typed on the Walls & diaphragms tab.");
        }

        // ------------------------------------------------------------------ selection: model IDs ↔ calculator keys

        /// <summary>The sketch's selection drives the selection card and the tables (one wall or diaphragm).</summary>
        private void SelectionFromSketch()
        {
            string id = Sketch.SelectedId;
            var o = building.Find(id);
            int? key = CurrentStory?.Key(id);
            if (o is Wall && key != null) { selectedKind = "Wall"; selectedId = key.Value; }
            else if (o is Diaphragm && key != null) { selectedKind = "Diaphragm"; selectedId = key.Value; }
            else { selectedKind = null; selectedId = -1; }
            selectionCardOpen = selectedKind != null && Sketch.Tool == SketchTool.Select;
            UpdateFromSketch();
        }

        /// <summary>A row picked in a table: the same wall or diaphragm is selected on the plan (brought into view).</summary>
        private void SelectFromTable(string kind, int id)
        {
            string modelId = CurrentStory?.ModelId(kind, id);
            selectedKind = kind;
            selectedId = id;
            if (modelId != null && Sketch.SelectedId != modelId) Sketch.SelectAndShow(modelId); // SelectionChanged updates the rest
            else UpdateShearWallUI();
        }

        private void DeleteFromTable(string kind, int id)
        {
            selectedKind = kind;
            selectedId = id;
            DeleteSelected();
        }

        /// <summary>After an undo or a delete the selection may name something that is gone.</summary>
        private void DropMissingSelection()
        {
            if (selectedKind == null) return;
            bool there = selectedKind == "Wall" ? Calculator._wall_system._walls.ContainsKey(selectedId)
                       : selectedKind == "Diaphragm" && Calculator._diaphragm_system._diaphragms.ContainsKey(selectedId);
            if (there) return;
            selectedKind = null;
            selectedId = -1;
            selectionCardOpen = false;
        }

        private void Delete_Click(object sender, RoutedEventArgs e) => DeleteSelected();

        /// <summary>Deletes the selected wall or diaphragm from the model (the whole diaphragm when openings split it).</summary>
        private void DeleteSelected()
        {
            string modelId = selectedKind == null ? null : CurrentStory?.ModelId(selectedKind, selectedId);
            if (modelId == null)
            {
                if (Sketch.Selection.Count > 0) { Sketch.DeleteSelected(); return; }
                ShowStatusMessage(LogLevel.Info, "Pick a wall or diaphragm first (Select), then Delete.");
                return;
            }
            selectionCardOpen = false;
            selectedKind = null;
            selectedId = -1;
            Sketch.ClearSelection();
            Sketch.Edit($"Deleted {modelId}.  Ctrl+Z brings it back.", b => BuildingEdits.Delete(b, modelId));
        }

        // ------------------------------------------------------------------ edit menu → the sketch's tools

        private void Undo_Click(object sender, RoutedEventArgs e) => Sketch.Undo();
        private void Redo_Click(object sender, RoutedEventArgs e) => Sketch.Redo();
        private void btnMove_Click(object sender, RoutedEventArgs e) => Sketch.SetTool(SketchTool.Move);
        private void btnCopy_Click(object sender, RoutedEventArgs e) => Sketch.SetTool(SketchTool.Copy);
        private void btnMeasure_Click(object sender, RoutedEventArgs e) => Sketch.SetTool(SketchTool.Measure);
        private void btnNote_Click(object sender, RoutedEventArgs e) => Sketch.SetTool(SketchTool.Note);
        private void btnCut_Click(object sender, RoutedEventArgs e) => Sketch.SetTool(SketchTool.Cut);
        private void btnOrtho_Click(object sender, RoutedEventArgs e) => Sketch.ToggleOrtho();
        private void btnZoomExtents_Click(object sender, RoutedEventArgs e) => Sketch.ZoomExtents();

        /// <summary>Ctrl+Z / Ctrl+Y (and Ctrl+Shift+Z) on the drawing; a text box keeps its own undo.</summary>
        private bool UndoShortcut(KeyEventArgs e)
        {
            if (InWindWorkspace || e.OriginalSource is TextBoxBase) return false;
            bool shift = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;
            if (e.Key == Key.Z && !shift) { Sketch.Undo(); return true; }
            if (e.Key == Key.Y || (e.Key == Key.Z && shift)) { Sketch.Redo(); return true; }
            return false;
        }

        // ------------------------------------------------------------------ design colours, level caption

        private static readonly Pen ClosePen = FrozenPen(Color.FromRgb(0xE6, 0x9F, 0x00), 4);
        private static readonly Pen FailPen = DashedPen(Color.FromRgb(0xD5, 0x5E, 0x00), 5);

        private static Pen DashedPen(Color c, double thickness)
        {
            var b = new SolidColorBrush(c);
            b.Freeze();
            var p = new Pen(b, thickness) { DashStyle = new DashStyle(new double[] { 2.2, 0.8 }, 0), DashCap = PenLineCap.Flat };
            p.Freeze();
            return p;
        }

        /// <summary>
        /// A shear wall of the level shown, by its design check: the usual blue when it passes (or before a check), amber when
        /// close to its limit, vermilion and dashed when it fails. No green or red: the dash tells a fail without colour.
        /// </summary>
        private Pen DesignPen(Wall w)
        {
            var key = CurrentStory?.Key(w.Id);
            if (key == null || !designResults.TryGetValue(key.Value, out var check)) return null;
            return check.State == DesignState.Fail ? FailPen : check.State == DesignState.Close ? ClosePen : null;
        }

        private string LevelCaption(Level l)
        {
            int i = stories.FindIndex(s => s.Level.Id == l.Id);
            if (i < 0) return null;
            var s0 = stories[i];
            return $"{LevelName(i)}  ·  story {i + 1} of {stories.Count}  ·  plate {s0.PlateHeight:0.##} ft" +
                   (designResults.Count > 0 ? $"  ·  {designResults.Values.Count(r => !r.Pass)} of {designResults.Count} walls fail" : "");
        }

        // ------------------------------------------------------------------ errors and warnings on the plan

        /// <summary>The issues shown on the plan and in the issue list: the model checks, then the design of each wall.</summary>
        private List<ModelIssue> issues = new List<ModelIssue>();

        /// <summary>Model checks (overlaps, walls no diaphragm reaches, bad references...) and failing walls, as markers on the plan.</summary>
        private void UpdateIssues()
        {
            var list = ModelChecks.Run(building);
            var s = CurrentStory;
            if (s != null)
            {
                foreach (var kv in designResults.Where(kv => !kv.Value.Pass))
                {
                    if (!s.Walls.TryGetValue(kv.Key, out var id) || !(building.Find(id) is Wall w)) continue;
                    var r = kv.Value;
                    string why = !r.AspectOk ? $"aspect ratio {r.AspectRatio:0.##} over {r.MaxAspectRatio:0.##}"
                               : !r.ShearOk ? $"shear {r.Demand:0} plf over the capacity {r.Capacity:0} plf"
                               : "no hold-down strong enough";
                    list.Add(new ModelIssue { Severity = Severity.Error, Code = "WallFails", Message = $"{id} fails on {s.Level.Name}: {why}.", Ids = { id }, LevelId = s.Level.Id, At = w.Middle });
                }
                if (Calculator != null && !Calculator.IsValidForCalculation && (s.Walls.Count > 0 || s.Diaphragms.Count > 0))
                    list.Add(new ModelIssue { Severity = Severity.Warning, Code = "NotCalculated", Message = $"{s.Level.Name} cannot be calculated yet: " + (CalculationTodo() ?? "check the walls and diaphragm."), LevelId = s.Level.Id });
            }
            issues = list;
            Sketch.Issues = list;
            int errors = list.Count(i => i.Severity == Severity.Error), warnings = list.Count - errors;
            btnIssues.Content = errors > 0 ? $"✗ {errors}" + (warnings > 0 ? $"  ⚠ {warnings}" : "") : warnings > 0 ? $"⚠ {warnings}" : "✓";
            btnIssues.Foreground = errors > 0 ? StatusErrorInk : warnings > 0 ? StatusWarnInk : Controls.Ui.PassInk;
            btnIssues.ToolTip = list.Count == 0 ? "No errors or warnings in the model or the design." : $"{errors} errors, {warnings} warnings: click for the list (click one to find it on the plan).";
        }

        private void Issues_Click(object sender, RoutedEventArgs e) => ShowIssueList(btnIssues);

        private void CheckModel_Click(object sender, RoutedEventArgs e)
        {
            rbWorkspaceWalls.IsChecked = true;
            ShowIssueList(btnIssues);
        }

        private void ShowIssueList(UIElement target)
        {
            var list = new IssueList(Sketch) { Width = 420, MaxHeight = 360, Margin = new Thickness(8) };
            list.Show(issues);
            var popup = new Popup
            {
                PlacementTarget = target,
                Placement = PlacementMode.Bottom,
                StaysOpen = false,
                AllowsTransparency = true,
                Child = new Border { Background = Brushes.White, BorderBrush = new SolidColorBrush(Color.FromRgb(0xC3, 0xC2, 0xB7)), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(4), Child = list },
            };
            popup.IsOpen = true;
        }

        // ------------------------------------------------------------------ the plan image's commands and snaps

        /// <summary>The CAD plan's lines near the cursor, for the sketch's snaps.</summary>
        private IEnumerable<Segment2> PlanLinesNear(Point2 p, double reach)
        {
            var img = Calculator?.Image;
            if (img == null || !img.IsVector || !img.Visible || hideImage) return null;
            return img.SegmentsNear(ToPoint(p), reach).Select(s => new Segment2(ToPoint2(s.A), ToPoint2(s.B)));
        }

        private void EnterImageDistance(string text)
        {
            if (imageTool.EnterDistance(Calculator?.Image, text, out string done))
            {
                Keyboard.Focus(Sketch);
                ShowStatusMessage(LogLevel.Info, done);
                ImageChanged();
            }
            else
            {
                ShowStatusMessage(LogLevel.Warning, done);
                sketchView.FocusCommand(text);
            }
        }

        // ------------------------------------------------------------------ 3D

        private Window window3D;
        private Building3DView view3D;

        private void Show3D_Click(object sender, RoutedEventArgs e)
        {
            if (window3D == null)
            {
                view3D = new Building3DView();
                window3D = new Window { Title = "Building 3D", Owner = this, Width = 900, Height = 640, WindowStartupLocation = WindowStartupLocation.CenterOwner, Content = view3D };
                window3D.Closed += (s, a) => { window3D = null; view3D = null; };
            }
            Update3D();
            window3D.Show();
            window3D.Activate();
        }

        private void Update3D()
        {
            if (view3D == null) return;
            view3D.HighlightLevelId = CurrentStory?.Level.Id;
            view3D.Show(building);
        }

        // ------------------------------------------------------------------ Model menu: the building for the other apps

        private const string ModelFilter = "Building model (*.bldg.json)|*.bldg.json|JSON (*.json)|*.json|All files (*.*)|*.*";
        private const string ChangesFilter = "Change set (*.changes.json)|*.changes.json|JSON (*.json)|*.json|All files (*.*)|*.*";

        /// <summary>Replaces the plan with a building from the Building Model app; the design inputs of walls with the same IDs stay.</summary>
        private void ImportModel_Click(object sender, RoutedEventArgs e)
        {
            var open = new OpenFileDialog { InitialDirectory = ProjectFolder.DialogFolder, Title = "Open a building model", Filter = ModelFilter };
            if (open.ShowDialog(this) != true) return;
            Building b;
            try { b = ModelFile.Load(open.FileName).Building; }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is InvalidDataException || ex is System.Runtime.Serialization.SerializationException)
            {
                MessageBox.Show(this, "The building model could not be read:\n" + ex.Message, "Open a building model", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (b == null) return;
            if (MessageBox.Show(this, $"Replace the walls, diaphragms and openings of this project with {Path.GetFileName(open.FileName)} " +
                                      $"({ShearWallAdapter.Stories(b).Count} stories, {b.Walls.Count} walls)?\n\nWalls keep their design where the model has their design data. Ctrl+Z undoes it.",
                                "Open a building model", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            Sketch.Edit($"Building model {Path.GetFileName(open.FileName)} opened.", target =>
            {
                string settings = target.GetData(ShearWallAdapter.SettingsKey);
                ModelHistory.Assign(target, b);
                if (target.GetData(ShearWallAdapter.SettingsKey) == null) target.SetData(ShearWallAdapter.SettingsKey, settings); // keep the project's design settings
            });
            Sketch.ZoomExtents();
            Log.Info("Building model opened: " + open.FileName);
        }

        private void ExportModel_Click(object sender, RoutedEventArgs e)
        {
            var save = new SaveFileDialog { InitialDirectory = ProjectFolder.DialogFolder, Title = "Save the building model", Filter = ModelFilter, FileName = Path.GetFileNameWithoutExtension(ProjectName) + ".bldg.json" };
            if (save.ShowDialog(this) != true) return;
            try { ModelFile.Save(save.FileName, new ModelDocument { Building = ModelFile.Clone(building) }); }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                MessageBox.Show(this, "The building model could not be saved:\n" + ex.Message, "Save the building model", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            Log.Info("Building model saved: " + save.FileName);
            ShowStatusMessage(LogLevel.Info, "Building model saved: " + save.FileName);
        }

        private void ExportChanges_Click(object sender, RoutedEventArgs e)
        {
            var before = baseModelJson == null ? Building.New() : ModelFile.BuildingFromJson(baseModelJson);
            var changes = ChangeSet.Between(before, building);
            if (changes.IsEmpty)
            {
                MessageBox.Show(this, "Nothing has changed in the building since the project was opened or saved.", "Export changes", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            changes.Author = "Shear Wall Calculator";
            var save = new SaveFileDialog { InitialDirectory = ProjectFolder.DialogFolder, Title = "Export changes", Filter = ChangesFilter, FileName = Path.GetFileNameWithoutExtension(ProjectName) + ".changes.json" };
            if (save.ShowDialog(this) != true) return;
            try { File.WriteAllText(save.FileName, changes.ToJson()); }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                MessageBox.Show(this, "The changes could not be saved:\n" + ex.Message, "Export changes", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            Log.Info($"Changes exported ({changes.Changes.Count}): {save.FileName}");
            ShowStatusMessage(LogLevel.Info, $"{changes.Changes.Count} changes exported: {changes.Summary()}");
        }

        /// <summary>Applies a change set from another copy of the model; conflicts are listed and can be forced or skipped.</summary>
        private void ApplyChanges_Click(object sender, RoutedEventArgs e)
        {
            var open = new OpenFileDialog { InitialDirectory = ProjectFolder.DialogFolder, Title = "Apply a change set", Filter = ChangesFilter };
            if (open.ShowDialog(this) != true) return;
            ChangeSet changes;
            try { changes = ChangeSet.FromJson(File.ReadAllText(open.FileName)); }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is InvalidDataException || ex is System.Runtime.Serialization.SerializationException)
            {
                MessageBox.Show(this, "The change set could not be read:\n" + ex.Message, "Apply a change set", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var trial = ModelFile.Clone(building);
            var conflicts = changes.ApplyTo(trial);
            bool force = false;
            if (conflicts.Count > 0)
            {
                var answer = MessageBox.Show(this, $"{conflicts.Count} of the {changes.Changes.Count} changes conflict with this model:\n\n" +
                                                   string.Join("\n", conflicts.Take(12).Select(c => "• " + c)) + (conflicts.Count > 12 ? "\n• ..." : "") +
                                                   "\n\nYes: apply them anyway (theirs wins).  No: apply only the others.  Cancel: apply nothing.",
                                             "Apply a change set", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
                if (answer == MessageBoxResult.Cancel) return;
                force = answer == MessageBoxResult.Yes;
            }
            Sketch.Edit($"Change set applied: {changes.Summary()}", b => changes.ApplyTo(b, force));
            Log.Info($"Change set applied ({changes.Changes.Count} changes, {conflicts.Count} conflicts{(force ? ", forced" : "")}): {open.FileName}");
        }
    }
}
