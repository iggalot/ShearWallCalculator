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
            // The plans under the levels are the building's: the sketch draws them (and snaps to CAD linework); this app
            // only shows or hides them (the Image button above). Importing, scaling and placing them is the editor's.
            sketchView.ShowPlanButton = false;
            sketchView.PlanEditing = false;
            sketchView.ShowBuildingTools = false; // From size... and Roof...: the building's size and roof are edited only in the Building Model editor (Edit building)
            Sketch.ShowWallLabels = false; // a wall picked on the plan opens its card (name, length and the rest)
            foreach (var t in new[] { SketchTool.Calibrate, SketchTool.PlanStraighten, SketchTool.PlanMove }) sketchView.HideTool(t);
            // The building is drawn and changed only in the Building Model (Model > Edit building): here it is shown and
            // designed. The drawing tools are hidden, and any edit that would move, add or delete something is put back.
            // A wall's design is data on the wall, so it still changes (and is what this app saves).
            foreach (var t in new[] { SketchTool.Wall, SketchTool.Diaphragm, SketchTool.Opening, SketchTool.Note, SketchTool.Move, SketchTool.Copy,
                                      SketchTool.Split, SketchTool.Mirror, SketchTool.Cut, SketchTool.Beam, SketchTool.Column })
                sketchView.HideTool(t);
            Sketch.GeometryLocked = true;
            Sketch.GeometryLockedMessage = "The building is drawn and changed in the Building Model: Model > Edit building opens it on this project.";
            var s = Sketch;
            s.DrawOver += (o, e) =>
            {
                labelsTaken.Clear(); // the overlays keep their labels off each other, this frame
                labelReach = Rect.Empty;
                if (!foundationView) ReservePlanMarks(); // and off the centers of mass and rigidity and the wall labels
                if (!foundationView)
                {
                    DrawBracedWallLines(e.Context);
                    DrawShearParts(e.Context); // the declared part of each shear wall, by its check; the rest of the wall stays plain
                    DrawPlanHoldowns(e.Context); // the markers first: the tags and labels keep off them
                    DrawWallDesign(e.Context);
                    DrawCOMandCOR(e.Context);
                }
                DrawLandings(e.Context); // hold-downs from the story above, or the foundation anchors
                DrawReactions(e.Context); // the foundation: what it takes at each point (SHOW Reactions)
                DrawLoads(e.Context);    // one load case: the shears from above and what each wall top receives (off unless asked for)
                DrawDebug(e.Context);
                UpdateDrawingKey(); // the schedule and the Loads legend, beside the plan
                UpdateShowing();    // in a word or two, what the plan shows
                MeasureLabelRoom(); // what Zoom to the drawing leaves around the plan for these labels
            };
            s.WallStyle = w => null; // every wall plain: a shear wall's declared part is drawn over it (DrawShearParts)
            s.InterceptClick = ClickOnTag; // a wall's design tag picks the wall, as a click on the wall does
            s.ExtraSnapPoints = HoldownSnapPoints;
            s.LevelCaption = l => ""; // no tag on the plan: the strip above it says the level and what is shown
            s.FitPadding = () => labelRoom; // room for the labels outside the building (the foundation's anchors, a level's hold-down landings)
            s.Escaped += (o, e) =>
            {
                if (selectionCardOpen) CloseSelectionCard();
            };
            s.ModelChanged += (o, e) => { if (!storing) ModelToCalculators(); };
            s.ActiveLevelChanged += (o, e) => ActiveLevelChanged();
            s.SelectionChanged += (o, e) => { SelectionFromSketch(); UpdateDrawingKey(); };
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
            // the shear walls are declared here; a project from before keeps the walls it marked, each over its whole length
            int adopted = ShearWallAdapter.Adopt(building);
            if (adopted > 0) Log.Info("Design", $"A project from before: its {adopted} marked shear wall{(adopted == 1 ? "" : "s")} declared over the whole length. Pick a wall to change its shear part.");
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
            // again once drawn: the labels' room is measured on the first frame
            Dispatcher.BeginInvoke(new Action(() => Sketch.ZoomExtents()), System.Windows.Threading.DispatcherPriority.ContextIdle);
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
            bool onGround = foundationView && Sketch.ActiveLevel != null && Sketch.ActiveLevel.Elevation == 0;
            if (i < 0 && stories.Count > 0 && !onGround) { storing = true; try { Sketch.ShowLevel(stories[currentLevel].Level.Id); } finally { storing = false; } }
            Calculator = levels.Count > 0 ? levels[currentLevel] : new ShearWallCalculator_RigidDiaphragm();
            ModelToWind(); // the wind calculation's building is the model's
            UpdateLevelButtons();
            UpdateFromSketch();
        }

        /// <summary>The level shown in the sketch changed (LEVEL buttons, or a selected issue on another level).</summary>
        private void ActiveLevelChanged()
        {
            if (Sketch.ActiveLevel != null && Sketch.ActiveLevel.Elevation == 0 && stories.Count > 0)
            {
                // the ground: the foundation view (the first story's hold-down anchors); the first story stays the one calculated
                foundationView = true;
                selectedKind = null;
                selectedId = -1;
                selectionCardOpen = false;
                currentLevel = 0;
                Calculator = levels[0];
                UpdateLevelButtons();
                UpdateFromSketch();
                ShowStatusMessage(LogLevel.Info, FoundationCaption() + ". Pick a level to edit walls again.");
                return;
            }
            bool wasFoundation = foundationView;
            foundationView = false;
            int i = stories.FindIndex(s => s.Level.Id == Sketch.ActiveLevel?.Id);
            if (wasFoundation && i >= 0)
            {
                currentLevel = -1; // so the level below is shown again even when it is the first story
            }
            if (i < 0 || (i == currentLevel && ReferenceEquals(Calculator, levels[i]))) return;
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

        /// <summary>
        /// An edit on the Members tab: a type typed there is design data and is kept; coordinates are not (the geometry is the
        /// Building Model's, and the rows show each shear wall's declared part): the rows go back to the model's.
        /// </summary>
        private void GeometryTyped(int story)
        {
            if (CurrentStory == null) { UpdateShearWallUI(); return; }
            if (ShearWallAdapter.StoreDesign(building, stories)) Sketch.Commit("Design changed on the Members tab.");
            else { ModelToCalculators(); ShowStatusMessage(LogLevel.Info, Sketch.GeometryLockedMessage); }
        }

        /// <summary>A member to delete from the Members tab: members are deleted in the Building Model (the geometry is edited only there).</summary>
        private void DeleteMember(string modelId)
        {
            if (modelId == null || building?.Find(modelId) == null) return;
            ShowStatusMessage(LogLevel.Info, $"{modelId}: {Sketch.GeometryLockedMessage}");
        }

        // ------------------------------------------------------------------ selection: model IDs ↔ calculator keys

        /// <summary>The sketch's selection drives the selection card and the tables (one wall or diaphragm).</summary>
        private void SelectionFromSketch()
        {
            string id = Sketch.SelectedId;
            var o = building.Find(id);
            int? key = CurrentStory?.Key(id);
            selectedModelId = null;
            selectedWallIds = Sketch.Selection.Count > 1 ? Sketch.SelectedOf<Wall>().Select(w => w.Id).ToList() : new List<string>(); // the core's selection
            if (selectedWallIds.Count > 0) o = null; // several picked: one card for the walls among them
            if (selectedWallIds.Count > 0) { selectedKind = MultiWallKind; selectedId = -1; }
            else if (o is Wall && key != null) { selectedKind = "Wall"; selectedId = key.Value; }
            else if (o is Wall) { selectedKind = PlainWallKind; selectedId = -1; selectedModelId = id; } // not a shear wall here: its card offers to declare it
            else if (o is Diaphragm && key != null) { selectedKind = "Diaphragm"; selectedId = key.Value; }
            else if (o is Beam || o is Column) { selectedKind = o is Beam ? "Beam" : "Column"; selectedId = -1; selectedModelId = id; }
            else { selectedKind = null; selectedId = -1; }
            selectionCardOpen = selectedKind != null && Sketch.Tool == SketchTool.Select;
            UpdateFromSketch();
            RefreshDetail(); // an open detail follows the selection
        }

        /// <summary>
        /// A row picked in a table: the same wall or diaphragm is selected on the plan (brought into view) and its card opens,
        /// as a click on it on the plan does; also when it was selected already (its card may have been closed).
        /// </summary>
        private void SelectFromTable(string kind, int id)
        {
            string modelId = CurrentStory?.ModelId(kind, id);
            selectedKind = kind;
            selectedId = id;
            if (modelId != null && Sketch.SelectedId != modelId) Sketch.SelectAndShow(modelId); // SelectionChanged opens the card and updates the rest
            else
            {
                selectionCardOpen = Sketch.Tool == SketchTool.Select;
                UpdateShearWallUI();
            }
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
            if ((selectedKind == "Beam" || selectedKind == "Column" || selectedKind == PlainWallKind) && building?.Find(selectedModelId) != null) return;
            if (selectedKind == MultiWallKind && selectedWallIds.Any(x => building?.Find(x) != null)) return;
            bool there = selectedKind == "Wall" ? Calculator._wall_system._walls.ContainsKey(selectedId)
                       : selectedKind == "Diaphragm" && Calculator._diaphragm_system._diaphragms.ContainsKey(selectedId);
            if (there) return;
            selectedKind = null;
            selectedId = -1;
            selectionCardOpen = false;
        }

        private void Delete_Click(object sender, RoutedEventArgs e) => DeleteSelected();

        /// <summary>Delete on a wall or diaphragm: they are deleted in the Building Model (the geometry is edited only there).</summary>
        private void DeleteSelected() => ShowStatusMessage(LogLevel.Info, Sketch.GeometryLockedMessage);

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

        // ------------------------------------------------------------------ the shear walls: declared here

        /// <summary>The selection kind of a wall that is not a shear wall (its card offers to declare it).</summary>
        private const string PlainWallKind = "PlainWall";

        /// <summary>The selection kind when several walls are picked (<see cref="selectedWallIds"/>): one card for all of them.</summary>
        private const string MultiWallKind = "Walls";

        /// <summary>The walls picked together (two or more objects selected), or none.</summary>
        private List<string> selectedWallIds = new List<string>();

        /// <summary>True when the wall's start is its west (W-E) or south (S-N) end: the card gives distances from that end.</summary>
        private static bool StartIsLow(Wall w) => w.Direction == WallDirection.SouthNorth ? w.Start.Y <= w.End.Y : w.Start.X <= w.End.X;

        private static string LowEnd(Wall w) => w.Direction == WallDirection.SouthNorth ? "south" : "west";

        /// <summary>After the event that asked for it (a box or a button on the card): the card is rebuilt by the change.</summary>
        private void Later(Action a) => Dispatcher.BeginInvoke(a, System.Windows.Threading.DispatcherPriority.Input);

        private Wall ShearWallByKey(int key) => building?.Find(CurrentStory?.ModelId("Wall", key)) as Wall;

        /// <summary>The card's shear part row: the declared part, in ft from the wall's west or south end.</summary>
        private Controls.SelectionCard.ShearPartInfo ShearPartFor(int key)
        {
            var w = ShearWallByKey(key);
            var p = w == null ? null : ShearWallAdapter.ShearPartOf(building, w);
            if (p == null) return null;
            double len = w.Length;
            bool low = StartIsLow(w);
            return new Controls.SelectionCard.ShearPartInfo
            {
                From = low ? p.From : len - p.To, To = low ? p.To : len - p.From, WallLength = len, FromEnd = LowEnd(w),
            };
        }

        /// <summary>A shear part typed on the card (ft from the wall's west or south end): declared, saved with the design, undoable.</summary>
        private void SetShearPart(int key, double a, double e)
        {
            var w = ShearWallByKey(key);
            if (w == null) return;
            double len = w.Length, lo = Math.Min(a, e), hi = Math.Max(a, e);
            bool low = StartIsLow(w);
            if (!ShearWallAdapter.Declare(building, w, low ? lo : len - hi, low ? hi : len - lo, Calculator?.DesignSettings))
            {
                ShowStatusMessage(LogLevel.Info, $"{w.Id}: a shear part is at least {ShearWallAdapter.MinShearLength:0.#} ft, inside the wall's {len:0.##} ft.");
                UpdateShearWallUI();
                return;
            }
            var p = ShearWallAdapter.ShearPartOf(building, w);
            double f = low ? p.From : len - p.To, t = low ? p.To : len - p.From;
            Sketch.Commit($"{w.Id}: shear part {f:0.##} to {t:0.##} ft from its {LowEnd(w)} end ({t - f:0.##} of {len:0.##} ft); the hold-downs at its ends.");
            SelectionFromSketch();
        }

        /// <summary>A plain wall made a shear wall: over its whole length to start with, with the project's sheathing for where it is.</summary>
        private void MakeShearWall(string id)
        {
            if (!(building?.Find(id) is Wall w)) return;
            if (!ShearWallAdapter.Declare(building, w, 0, w.Length, Calculator?.DesignSettings)) return;
            Sketch.Commit($"{w.Id} declared a shear wall over its whole length ({w.Length:0.##} ft). Its shear part and sheathing are on its card.");
            SelectionFromSketch();
        }

        private List<Wall> SelectedWalls() => selectedWallIds.Select(x => building?.Find(x)).OfType<Wall>().ToList();

        /// <summary>The card for several walls picked together: how many are shear walls, make them all shear walls or plain, one sheathing for all.</summary>
        private void ShowWallsCard()
        {
            var walls = SelectedWalls();
            var shear = walls.Where(w => ShearWallAdapter.IsShearWall(building, w)).ToList();
            int others = Sketch.Selection.Count - walls.Count;
            // what the shear walls among them have in common, on every story: one face 1 sheathing, one hold-down type
            var designs = shear.SelectMany(w => ShearWallAdapter.StoriesOf(building, w).Select(l =>
                              ShearWallAdapter.ReadWall(w).Design.TryGetValue(l.Id, out var d) && d != null ? d : ShearWallAdapter.NewWallDesign(building, w, Calculator?.DesignSettings))).ToList();
            var faces = designs.Select(d => designer?.FindOption(d.EffectiveFace1())?.Name).Distinct().ToList();
            var types = designs.Select(d => d.Holdown).Distinct().ToList();
            selectionCard.ShowWalls($"{walls.Count} walls selected",
                $"{CurrentStory?.Level.Name}" + (others > 0 ? $" · {others} other object{(others == 1 ? "" : "s")} selected, left as they are" : "") + " · Shift+click to add or take out",
                shear.Count, walls.Count - shear.Count,
                () => Later(MakeShearWalls), () => Later(MakePlainWalls),
                faces.Count == 1 && faces[0] != null ? designs[0].EffectiveFace1() : null, types.Count == 1 ? types[0] : (HoldownFamilies?)null,
                (what, edit) => Later(() => EditSelectedWalls(what, edit)));
        }

        private void MakeShearWalls()
        {
            int n = ShearWallAdapter.DeclareAll(building, SelectedWalls(), Calculator?.DesignSettings);
            if (n == 0) return;
            Sketch.Commit($"{n} wall{(n == 1 ? "" : "s")} declared shear walls over the whole length.");
            SelectionFromSketch();
        }

        private void MakePlainWalls()
        {
            int n = ShearWallAdapter.UndeclareAll(building, SelectedWalls());
            if (n == 0) return;
            Sketch.Commit($"{n} wall{(n == 1 ? "" : "s")} no longer shear walls: they carry their gravity load only.");
            SelectionFromSketch();
        }

        /// <summary>One design edit from the card for several walls, on every story of each shear wall among them: one step to undo.</summary>
        private void EditSelectedWalls(string what, Action<WallDesignInputs> edit)
        {
            int n = ShearWallAdapter.EditDesign(building, SelectedWalls(), Calculator?.DesignSettings, edit);
            if (n == 0) { UpdateShearWallUI(); return; }
            Sketch.Commit($"{n} shear wall{(n == 1 ? "" : "s")}: {what}.");
            SelectionFromSketch();
        }

        /// <summary>A shear wall made a plain wall again: its shear part and sheathing are taken off (undo puts them back).</summary>
        private void MakePlainWall(int key)
        {
            var w = ShearWallByKey(key);
            if (w == null) return;
            ShearWallAdapter.Undeclare(w);
            Sketch.Commit($"{w.Id} is no longer a shear wall: it carries its gravity load only.");
            SelectionFromSketch();
        }

        // ------------------------------------------------------------------ design colours, level caption

        private static readonly Pen ClosePen = FrozenPen(((SolidColorBrush)Controls.Ui.CloseStripe).Color, 4);
        private static readonly Pen FailPen = DashedPen(((SolidColorBrush)Controls.Ui.FailStripe).Color, 5);
        private static readonly Pen PassPen = FrozenPen(((SolidColorBrush)Controls.Ui.PassStripe).Color, 4);

        private static Pen DashedPen(Color c, double thickness)
        {
            var b = new SolidColorBrush(c);
            b.Freeze();
            var p = new Pen(b, thickness) { DashStyle = new DashStyle(new double[] { 2.2, 0.8 }, 0), DashCap = PenLineCap.Flat };
            p.Freeze();
            return p;
        }

        /// <summary>
        /// A shear wall of the level shown, by its design check: the usual blue when it passes (or before a check), yellow when
        /// close to its limit, crimson and dashed when it fails. The dash tells a fail without colour (colours in Ui).
        /// </summary>
        private Pen DesignPen(int key) =>
            !designResults.TryGetValue(key, out var check) ? PassPen : check.State == DesignState.Fail ? FailPen : check.State == DesignState.Close ? ClosePen : PassPen;

        /// <summary>Each shear wall's declared part on the level shown (the calculator's wall is that part), over the plain wall.</summary>
        private void DrawShearParts(DrawingContext ctx)
        {
            if (Calculator?._wall_system == null) return;
            foreach (var kv in Calculator._wall_system._walls)
                ctx.DrawLine(DesignPen(kv.Key), Sketch.ToScreen(new Point2(kv.Value.Start.X, kv.Value.Start.Y)), Sketch.ToScreen(new Point2(kv.Value.End.X, kv.Value.End.Y)));
        }


        // ------------------------------------------------------------------ errors and warnings on the plan

        /// <summary>The issues shown on the plan and in the issue list: the model checks, then the design of each wall.</summary>
        private List<ModelIssue> issues = new List<ModelIssue>();

        /// <summary>Model checks (overlaps, walls no diaphragm reaches, bad references...) and failing walls, as markers on the plan.</summary>
        private void UpdateIssues()
        {
            var list = ModelChecks.Run(building);
            list.AddRange(ShearWallAdapter.Checks(building)); // the shear walls: declared here, so checked here
            var s = CurrentStory;
            if (s != null)
            {
                foreach (var kv in designResults.Where(kv => !kv.Value.Pass))
                {
                    if (!s.Walls.TryGetValue(kv.Key, out var id) || !(building.Find(id) is Wall w)) continue;
                    var r = kv.Value;
                    string why = !r.AspectOk ? $"aspect ratio {r.AspectRatio:0.##} over {r.MaxAspectRatio:0.##}"
                               : !r.ShearOk ? $"shear {r.Demand:0} plf over the capacity {r.Capacity:0} plf"
                               : r.Status;
                    list.Add(new ModelIssue { Severity = Severity.Error, Code = "WallFails", Message = $"{id} fails on {s.Level.Name}: {why}.", Ids = { id }, LevelId = s.Level.Id, At = w.Middle });
                }
                AddLandingIssues(list);
                AddOffFloorIssues(list);
                if (Calculator != null && !Calculator.IsValidForCalculation && (s.Walls.Count > 0 || s.Diaphragms.Count > 0))
                    list.Add(new ModelIssue { Severity = Severity.Warning, Code = "NotCalculated", Message = $"{s.Level.Name} cannot be calculated yet: " + (CalculationTodo() ?? "check the walls and diaphragm."), LevelId = s.Level.Id });
            }
            issues = list;
            LogIssues(list); // what the user is warned about, and where
            Sketch.Issues = list;
            int errors = list.Count(i => i.Severity == Severity.Error), warnings = list.Count - errors;
            ShowIssueState(errors, warnings);

            // the Design tab lists what stops or bends the design, each going to its element on the plan: model errors and the
            // load path (hold-downs with nothing under them, a replaced type, beams and columns not connected). Failing walls are
            // the summary's "✗ n fail" link there.
            var forDesign = list.Where(i => i.Code != "WallFails" && (i.Severity == Severity.Error || DesignCodes.Contains(i.Code)))
                                .OrderBy(i => i.Severity == Severity.Error ? 0 : 1).ToList();
            designPanel?.SetWarnings(forDesign.Select(i => new Controls.WallDesignPanel.DesignWarning
            {
                Text = i.Message, IsError = i.Severity == Severity.Error, LinkText = "Show on the plan", Go = () => GoToIssue(i),
            }).ToList(), () => { rbWorkspaceWalls.IsChecked = true; ShowIssueList(btnIssues); });
        }

        // The issues button in the top bar, on every workspace and tab: solid red with errors (it flashes when there are
        // more than before), solid amber with warnings only, green when clear.
        private static readonly Brush IssueErrorFill = Controls.Ui.Frozen("#C62828"), IssueWarnFill = Controls.Ui.Frozen("#F2B233"), IssueWarnInk = Controls.Ui.Frozen("#3D2A00"),
                                      IssueOkFill = Controls.Ui.PassFill, IssueOkInk = Controls.Ui.PassInk;
        private int shownErrors;


        private void ShowIssueState(int errors, int warnings)
        {
            string E(int n) => n == 1 ? "1 error" : $"{n} errors";
            string W(int n) => n == 1 ? "1 warning" : $"{n} warnings";
            btnIssues.Content = errors > 0 ? "✗ " + E(errors) + (warnings > 0 ? "  ·  ⚠ " + W(warnings) : "")
                              : warnings > 0 ? "⚠ " + W(warnings) : "✓ No issues";
            btnIssues.Background = errors > 0 ? IssueErrorFill : warnings > 0 ? IssueWarnFill : IssueOkFill;
            btnIssues.BorderBrush = errors > 0 ? IssueErrorFill : warnings > 0 ? IssueWarnFill : IssueOkInk;
            btnIssues.Foreground = errors > 0 ? Brushes.White : warnings > 0 ? IssueWarnInk : IssueOkInk;
            btnIssues.ToolTip = errors + warnings == 0 ? "No errors or warnings in the model or the design."
                              : $"{E(errors)}, {W(warnings)} in the model and the design: click for the list (click one to find it on the plan).";
            if (errors > shownErrors)
            {
                // a few flashes to catch the eye, then it stays solid red
                var flash = new System.Windows.Media.Animation.DoubleAnimation(1, 0.25, TimeSpan.FromMilliseconds(350))
                { AutoReverse = true, RepeatBehavior = new System.Windows.Media.Animation.RepeatBehavior(4) };
                btnIssues.BeginAnimation(OpacityProperty, flash);
                Log.Info("Issues", $"{E(errors)} now (was {shownErrors}), {W(warnings)}: the issues button flashes.");
            }
            shownErrors = errors;
        }

        /// <summary>The warnings that matter to the design (besides every error).</summary>
        private static readonly HashSet<string> DesignCodes = new HashSet<string>
        {
            "HoldownUnsupported", "HoldownTypeReplaced", "HoldownTooWeak", "BeamCrossesColumn", "BeamMissesColumn", "NotCalculated", "WallOffFloor",
        };

        /// <summary>
        /// An upper wall that runs past the edge of the floor under it: that part's shear has no diaphragm to go into (a beam or
        /// column under it carries its weight and uplift, not its shear). One warning per wall, on the wall's own level.
        /// </summary>
        private void AddOffFloorIssues(List<ModelIssue> list)
        {
            for (int k = 1; k < stories.Count; k++)
            {
                var upper = stories[k];
                foreach (var kv in StoryCascade.OffFloor(upper.Calculator, stories[k - 1].Calculator))
                {
                    if (!upper.Walls.TryGetValue(kv.Key, out var id) || !(building.Find(id) is Wall w)) continue;
                    bool all = kv.Value >= w.Length - 0.1;
                    list.Add(new ModelIssue
                    {
                        Severity = Severity.Warning, Code = "WallOffFloor", LevelId = upper.Level.Id, Ids = { id }, At = w.Middle,
                        Message = $"{id} ({upper.Level.Name}): {(all ? "all of it is" : $"{kv.Value:0.#} of its {w.Length:0.#} ft is")} beyond the edge of the " +
                                  $"{stories[k - 1].Level.Name} floor, so its shear has no diaphragm to go into. Extend the floor's diaphragm under it " +
                                  "(a beam under the wall carries its weight and hold-downs, not its shear).",
                    });
                }
            }
        }

        /// <summary>An issue followed from the Design tab: the plan, its level, the members selected and in view.</summary>
        private void GoToIssue(ModelIssue i)
        {
            rbWorkspaceWalls.IsChecked = true;
            Sketch.ShowIssue(i);
            Log.Info("Issues", $"Shown on the plan: {i.Code} {string.Join(", ", i.Ids)}");
        }

        private void Issues_Click(object sender, RoutedEventArgs e) => ShowIssueList(btnIssues);

        private void CheckModel_Click(object sender, RoutedEventArgs e)
        {
            rbWorkspaceWalls.IsChecked = true;
            ShowIssueList(btnIssues);
        }

        private void ShowIssueList(UIElement target)
        {
            // its own ink: a popup inherits from its placement target, and the button's text is white on red
            var list = new IssueList(Sketch) { Width = 420, MaxHeight = 360, Margin = new Thickness(8) };
            System.Windows.Documents.TextElement.SetForeground(list, Controls.Ui.Ink);
            list.Show(issues);
            // from any workspace: a row goes to the plan, so show it first (the Shear walls workspace, the PLAN side open)
            list.PreviewMouseLeftButtonUp += (s, a) =>
            {
                rbWorkspaceWalls.IsChecked = true;
                if (!planOpen) { planOpen = true; ApplyPanels(); }
            };
            var popup = new Popup
            {
                PlacementTarget = target,
                Placement = PlacementMode.Bottom,
                StaysOpen = false,
                AllowsTransparency = true,
                Child = new Border { Background = Brushes.White, BorderBrush = new SolidColorBrush(Color.FromRgb(0xC3, 0xC2, 0xB7)), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(4), Child = list },
            };
            list.Picked += (s, i) => { popup.IsOpen = false; Log.Info("Issues", $"Picked from the list: {i.Code} {string.Join(", ", i.Ids)} (zoomed to it on the plan)"); };
            popup.IsOpen = true;
        }

        // ------------------------------------------------------------------ 3D

        private Window window3D;
        private Building3DView view3D;

        private void Show3D_Click(object sender, RoutedEventArgs e)
        {
            if (window3D == null)
            {
                view3D = new Building3DView();
                window3D = BuildingModel.Sketch.WindowFit.Fit(new Window { Title = "Building 3D", Owner = this, Width = 900, Height = 640, WindowStartupLocation = WindowStartupLocation.CenterOwner, Content = view3D });
                window3D.Closed += (s, a) => { window3D = null; view3D = null; };
            }
            Update3D();
            window3D.Show();
            window3D.Activate();
        }

        /// <summary>The Wind Setup's Building page: the model in 3D (Building3DView, as the 3D button shows it).</summary>
        private Building3DView windPreview;

        private void ShowWindPreview()
        {
            if (windSetup == null || building == null) return;
            if (windPreview == null) windPreview = new Building3DView();
            windPreview.Show(building);
            if (windSetup.BuildingPreview != windPreview) windSetup.BuildingPreview = windPreview;
        }

        private void Update3D()
        {
            if (view3D == null) return;
            view3D.HighlightLevelId = CurrentStory?.Level.Id;
            view3D.Show(building);
        }

        // ------------------------------------------------------------------ Model menu: the building for the other apps

        private const string ChangesFilter = "Change set (*.changes.json)|*.changes.json|JSON (*.json)|*.json|All files (*.*)|*.*";

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
