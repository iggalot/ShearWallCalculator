using ASCE7WindLoadCalculator;
using ASCE7WindLoadCalculator.ModelAdapter;
using BuildingModel.Checks;
using BuildingModel.Diagnostics;
using BuildingModel.Edits;
using BuildingModel.Model;
using BuildingModel.Serialization;
using BuildingModel.Sketch;
using calculator;
using ShearWallCalculator;
using ShearWallCalculator.ModelAdapter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace ShearWallVisualizer
{
    /// <summary>
    /// The project is one file, the building model (&lt;project folder&gt;\&lt;name&gt;.bldg.json), in a folder the user picks
    /// (<see cref="ProjectFolder"/>): the outputs and the logs go next to it. The file holds:
    /// <list type="bullet">
    /// <item>the building (levels, walls, diaphragms, openings, roof, defaults), with the designer's data in the bags of its walls and diaphragms;</item>
    /// <item>the wind inputs (<see cref="WindSection"/>, the wind app's section); none when the project uses manual loads;</item>
    /// <item>the designer's section (<see cref="ShearWallProject"/>: manual loads, the loads when saved) and each level's plan image.</item>
    /// </list>
    /// The size, stories and roof the wind calculation uses are the model's. Nothing is designed without a project, and the
    /// design and reports wait while the model has errors.
    /// </summary>
    public partial class MainWindow
    {
        /// <summary>The project's model file, or null before a project is opened or made.</summary>
        private string currentProjectPath;

        /// <summary>The document as opened or last saved (its revision goes up on each save).</summary>
        private ModelDocument projectDoc;

        /// <summary>The file text when last opened or saved; any difference means unsaved changes.</summary>
        private string savedProjectJson;

        private bool HasProject => currentProjectPath != null;

        private string ProjectName => currentProjectPath == null ? "No project"
            : Path.GetFileName(currentProjectPath).Replace(ProjectFolder.ModelExtension, "");

        // ------------------------------------------------------------------ the file's contents

        /// <summary>
        /// The building with this app's data written into it (the designer's section, the walls' and diaphragms' designs).
        /// Only that data is saved (<see cref="OwnsData"/>): the building is the Building Model's, the wind the Wind Load
        /// Calculator's, the gravity loads the Load Manager's.
        /// </summary>
        private Building BuildingToSave()
        {
            var b = ModelFile.Clone(building);
            ShearWallProject.Save(b, stories, manualLoads: !windLoadsLinked);
            return b;
        }

        /// <summary>
        /// The data keys this app writes into the project file: its section and design settings, each wall's and diaphragm's
        /// design ("ShearWall": the shear walls are declared there, the part of each that counts), the mark that it declares
        /// them ("ShearWall.Declared"), the forces it puts on the foundation ("Lateral", for the Load Manager) and the
        /// connections it designs ("Connections": the walls' nailing, the sills' bolts or nailing, the diaphragm lines) and the
        /// defaults they start from ("ConnectionDefaults", edited in Details > Connection defaults). Nothing else.
        /// </summary>
        private static bool OwnsData(string key) =>
            key == ShearWallProject.Key || key == ShearWallAdapter.SettingsKey || key == ShearWallAdapter.WallKey || key == ShearWallAdapter.DiaphragmKey || key == ShearWallAdapter.DeclaredKey ||
            key == BuildingModel.Model.LateralSection.Key || key == WoodFraming.Connections.ConnectionsSection.Key ||
            key == WoodFraming.Connections.ConnectionDefaults.Key;

        /// <summary>This app's hold on the open project (one Shear Wall Calculator per project).</summary>
        private ProjectLock projectLock;

        /// <summary>Takes this app's lock on the project; false (and says so) when another Shear Wall Calculator has it open.</summary>
        private bool HoldProject(string file)
        {
            if (projectLock != null && string.Equals(projectLock.ModelPath, file, StringComparison.OrdinalIgnoreCase)) return true;
            var taken = ProjectDialogs.TakeLock(this, file, "shearwalls", "Shear Wall Calculator", out bool refused);
            if (refused) return false;
            projectLock?.Dispose();
            projectLock = taken;
            return true;
        }

        // ------------------------------------------------------------------ the project file, watched while it is open

        private ProjectWatcher projectWatcher;

        /// <summary>
        /// The open project's file is watched the whole time it is open (also with no Edit building open): another app saving
        /// it (the wind, the loads, the building) is seen at once. Called after every open and save (this app's own save is
        /// then not taken for another's).
        /// </summary>
        private void WatchProject()
        {
            if (!HasProject) { projectWatcher?.Dispose(); projectWatcher = null; return; }
            if (projectWatcher != null && string.Equals(projectWatcher.ModelPath, currentProjectPath, StringComparison.OrdinalIgnoreCase)) { projectWatcher.SavedByMe(); return; }
            projectWatcher?.Dispose();
            projectWatcher = new ProjectWatcher(currentProjectPath);
            projectWatcher.ChangedOnDisk += (s, a) => ProjectChangedOnDisk();
        }

        /// <summary>
        /// Another app saved the project: with nothing unsaved here it is read again and the walls designed again, and the
        /// walls that fail now but did not before are named (the change elsewhere broke them). With unsaved changes here it
        /// is only said: a save puts this app's data into the file as it is now (<see cref="ModelFile.SaveOwnData"/>).
        /// Only reads: nothing is saved because the file changed (no app writes in answer to another's save).
        /// </summary>
        private void ProjectChangedOnDisk()
        {
            if (!HasProject || !File.Exists(currentProjectPath)) return;
            if (editorProcess != null && !editorProcess.HasExited) return; // Edit building / wind / loads: its own watcher reloads
            if (HasUnsavedChanges)
            {
                Log.Info("Project", "The project was saved by another app while this one has unsaved changes.");
                ShowStatusMessage(LogLevel.Warning, "Another app saved this project meanwhile. Save here to put your design into it (the building, wind and loads stay as it has them): the walls are then designed for it as it is now.");
                return;
            }
            var before = FailingWalls();
            OpenProject(currentProjectPath, reload: true);
            var newly = FailingWalls().Except(before).ToList();
            if (newly.Count == 0)
            {
                ShowStatusMessage(LogLevel.Info, "Another app saved this project: it was read again and the walls designed again.");
                return;
            }
            string list = string.Join("\n", newly.Take(12)) + (newly.Count > 12 ? $"\n(and {newly.Count - 12} more)" : "");
            Log.Warning("Project", "After another app's save these walls fail: " + string.Join("; ", newly), null);
            ShowStatusMessage(LogLevel.Warning, $"Another app saved this project: {newly.Count} wall{(newly.Count == 1 ? "" : "s")} fail now: {newly[0]}" + (newly.Count > 1 ? " …" : ""));
            MessageBox.Show(this, "Another app saved this project (the building, the wind or the loads changed). It was read again and the walls designed again; " +
                                  $"{(newly.Count == 1 ? "this wall fails" : "these walls fail")} now:\n\n{list}\n\nThey are red on the plan. Fix them and save.",
                            "Shear Wall Calculator", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        /// <summary>The walls that fail now, "W4 on Level 2".</summary>
        private HashSet<string> FailingWalls()
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < stories.Count && i < allDesignResults.Count; i++)
                foreach (var kv in allDesignResults[i].Where(kv => !kv.Value.Pass))
                    set.Add((stories[i].Walls.TryGetValue(kv.Key, out var id) ? id : "W" + kv.Key) + " on " + stories[i].Level.Name);
            return set;
        }

        private string CurrentProjectJson() => HasProject ? ModelFile.BuildingToJson(BuildingToSave()) : null;

        private bool HasUnsavedChanges => HasProject && savedProjectJson != null && CurrentProjectJson() != savedProjectJson;

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
            return ProjectDialogs.SaveChangesFirst(this, $"Save the changes to {ProjectName}?", "Shear Wall Calculator", () => SaveProject(saveAs: false));
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (editorProcess != null && !editorProcess.HasExited)
            {
                MessageBox.Show(this, $"The project is open in the {editorName}. Save and close it there first.", "Shear Wall Calculator",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                e.Cancel = true;
                return;
            }
            if (!ConfirmDiscardOrSave()) e.Cancel = true;
        }

        // ------------------------------------------------------------------ New, Open, Save

        /// <summary>
        /// New project: the user picks (or makes) its folder, then the building's size (length, width, stories, story heights,
        /// shear walls on the outside); the model is saved there at once and the wind loads use the engine's starting inputs.
        /// </summary>
        private void MenuItem_New_Click(object sender, RoutedEventArgs e) => NewProject();

        private bool NewProject()
        {
            if (!ConfirmDiscardOrSave()) return false;
            string path = ProjectDialogs.NewProjectPath(this, out bool existing);
            if (path == null) return false;
            if (existing) return OpenProject(path);

            string name = Path.GetFileName(Path.GetDirectoryName(path));
            var b = Building.New(name);
            var q = QuickBuildingDialog.Ask(b, FloorEdits.MaxFloors, this);
            if (q == null) return false;
            BuildingEdits.BuildFromSize(b, q);

            if (!HoldProject(path)) return false;
            ProjectFolder.OpenFor(path);
            Log.Info("Project", $"New project {path}: {q.Length:0.##} × {q.Width:0.##} ft, {q.StoryHeights.Count} stor{(q.StoryHeights.Count == 1 ? "y" : "ies")} " +
                                $"({string.Join(", ", q.StoryHeights.Select(h => h.ToString("0.##")))} ft).");
            projectDoc = new ModelDocument { Building = b };
            currentProjectPath = path;
            // no wind yet: it is set up in the Wind Load Calculator (Edit wind); this app makes none of its own
            LoadProject(b, new List<ShearWallCalculatorBase>(), null, new ShearWallProject());
            Calculator.DesignSettings.HoldownsFromStandardList = designer != null && designer.StandardList.Count > 0;
            if (!SaveProject(saveAs: false)) return false;
            AddToRecentFiles(path);
            return true;
        }

        private void MenuItem_Load_Click(object sender, RoutedEventArgs e)
        {
            if (!ConfirmDiscardOrSave()) return;
            string path = ProjectDialogs.OpenModelPath(this);
            if (path != null) OpenProject(path);
        }

        /// <summary>
        /// Opens a project (its building model file). A model without the designer's sections is a new design of that
        /// building: the wind starts from the engine's inputs and the walls take the wind loads.
        /// </summary>
        private bool OpenProject(string path, bool reload = false)
        {
            ModelDocument doc;
            if (!HoldProject(path)) return false;
            try
            {
                ProjectFolder.OpenFor(path); // first, so the opening is in the project's log
                doc = ModelFile.Load(path);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is InvalidDataException ||
                                       ex is System.Runtime.Serialization.SerializationException || ex is ArgumentException)
            {
                MessageBox.Show(this, "The project could not be opened:\n" + ex.Message, "Open", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            var b = doc.Building;
            var wind = WindSection.Read(b);
            var windNow = WindCheck.Run(b); // the wind calculator's wind for the building as it is now (the hub's check uses the same)
            // each story's calculator: its plan image and, without wind, its manual load (kept there and in the wind screen
            // while the project is open; written back at save)
            var calcs = ShearWallProject.Open(b, manualLoads: wind == null, out var section);
            bool designed = section != null;
            section = section ?? new ShearWallProject();
            // no Wind section: the wind is not set up yet. It is set up in the Wind Load Calculator (Edit wind); this app
            // makes no wind of its own (the walls wait, or use manual loads)
            windJsonInUse = b.GetData(WindSection.Key); // the wind the walls are designed with (its stamp at save)
            b.SetData(WindSection.Key, null);

            int keepLevel = reload ? currentLevel : 0;
            projectDoc = doc;
            currentProjectPath = path;
            string note = LoadProject(b, calcs, wind, section, keepLevel);
            if (!reload) AddToRecentFiles(path);
            Log.Info("Project", (reload ? "Reloaded " : "Opened ") + path + (designed ? "" : " (a new design: no shear wall data yet)") +
                                (wind == null ? "; manual loads." : $"; wind {wind.WindSpeed:0} mph, exposure {wind.Exposure}, {wind.Edition}."));

            string holdownNote = HoldownChangesSinceSave();
            if (holdownNote != null) note = note == null ? holdownNote : note + "\n\n" + holdownNote;
            // loads or hold-downs that changed count as unsaved so the user is asked before closing
            savedProjectJson = note == null ? CurrentProjectJson() : string.Empty;
            UpdateTitle();
            WatchProject();
            if (!reload) OpenOnWorkspace(windNow);
            if (note != null && !reload)
                MessageBox.Show(this, note, "Open", MessageBoxButton.OK, MessageBoxImage.Information);
            // what the hub marked for the shear walls when it last checked (its "Hub.Status"; the hub writes it, this app only reads)
            string hub = reload ? null : ProjectStatus.Read(b)?.Words("Shear walls");
            if (hub != null)
            {
                Log.Warning("Project", hub.Replace("\n", " "), null);
                MessageBox.Show(this, hub, "Open", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            return true;
        }

        /// <summary>Puts an opened or new project into the window. Returns a note for the user (loads that changed), or null.</summary>
        private string LoadProject(Building b, IList<ShearWallCalculatorBase> calcs, WindSection wind, ShearWallProject section, int active = 0)
        {
            windLoadsLinked = wind != null;
            var w = wind ?? WindSection.New();
            windSetup.SetInputs(new BuildingData { EnclosureType = w.Enclosure, ParapetEnclosure = w.ParapetEnclosure }, w.Edition, w.ToParameters());
            windSetup.SetComponents(w.RoofComponent, w.WallComponent);
            windSetup.SetOnlyCases1And3(w.OnlyCases1And3);
            windVersion = windSetup.Version;
            windLoadParams = windSetup.Parameters.Clone();
            buildingData = null;
            modelWindKey = null;

            SetBuilding(b, calcs, active); // the model's geometry goes to the wind screen (ModelToWind)
            ImageForNewProject();
            HideStartScreen();
            if (!windLoadsLinked)
                UnlinkWindLoads("This project has no wind yet: set it up in the Wind Load Calculator (Edit wind above). Until then the walls use manual loads.");
            RecalculateWind(); // when linked, this sends the wind load cases to every story
            UpdateWindBar();
            UpdateShearWallUI();
            StartHistory();
            LogWindInputs();

            if (!windLoadsLinked) return null;
            var changed = stories.Where(s => section.LoadsWhenSaved.TryGetValue(s.Level.Id, out var was) &&
                                             (Changed(was.Vx, s.Calculator.V_x) || Changed(was.Vy, s.Calculator.V_y)))
                                 .Select(s => { var was = section.LoadsWhenSaved[s.Level.Id]; return $"{s.Level.Name}: X {was.Vx:0.00} → {s.Calculator.V_x:0.00} k, Y {was.Vy:0.00} → {s.Calculator.V_y:0.00} k"; })
                                 .ToList();
            if (changed.Count == 0) return null;
            Log.Warning("Project", "The wind loads differ from when the project was saved: " + string.Join("; ", changed), null);
            return "The wind loads differ from when this project was saved:\n" + string.Join("\n", changed) +
                   "\n\nThe walls now use the new loads (usually a correction in the wind calculation since then). Save to keep them.";
        }

        private void MenuItem_Save_Click(object sender, RoutedEventArgs e) => SaveProject(saveAs: false);

        private void MenuItem_SaveAs_Click(object sender, RoutedEventArgs e) => SaveProject(saveAs: true);

        /// <summary>Saves the project file. Save as: a copy of the project in another folder (the user picks it), which becomes the open project.</summary>
        private bool SaveProject(bool saveAs)
        {
            if (!HasProject) return false;
            windSetup?.FlushPendingChange(); // a value still being typed counts
            string path = currentProjectPath;
            if (saveAs)
            {
                string file = ProjectDialogs.NewProjectPath(this, out bool existing);
                if (file == null) return false;
                if (existing && MessageBox.Show(this, $"Replace {Path.GetFileName(file)} in that folder with this project?", "Save as",
                                                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return false;
                path = file;
            }
            bool geometryMoved;
            List<string> lost = new List<string>();
            try
            {
                RecordHoldowns();
                var mine = BuildingToSave();
                // what the shear walls put on the foundation, for the load manager (not compared for unsaved changes: it is
                // rewritten from the design on every save), stamped with the revision this save gives the file
                // (and the connections designed from it: none, an old one is removed too)
                void Stamp(ModelDocument d)
                {
                    var lat = LateralForFoundation(d.Revision + 1, mine);
                    BuildingModel.Model.LateralSection.Write(d.Building, lat); // none: an old one is removed
                    WoodFraming.Connections.ConnectionsSection.Write(d.Building, lat == null ? null : ShearWallCheck.Connections(levels, stories, allDesignResults, d.Building, lat, d.Revision + 1));
                }
                ModelDocument saved;
                if (!saveAs && File.Exists(path))
                {
                    // the file is read again and only this app's data is replaced: what the other apps saved meanwhile stays
                    saved = ModelFile.SaveOwnData(path, mine, OwnsData, out lost, Stamp);
                }
                else
                {
                    // a new project, or a copy in another folder: the whole project (the other apps' sections as the file has them)
                    if (saveAs && !HoldProject(path)) return false;
                    saved = saveAs && File.Exists(currentProjectPath) ? ModelFile.Load(currentProjectPath) : new ModelDocument { Building = ModelFile.Clone(mine) };
                    if (saveAs) lost = ModelMerge.CopyOwnData(mine, saved.Building, OwnsData);
                    saved.Version = ModelDocument.CurrentVersion;
                    saved.Revision = 0;
                    Stamp(saved);
                    ModelFile.Save(path, saved);
                }
                geometryMoved = ModelMerge.GeometryJson(saved.Building) != ModelMerge.GeometryJson(building);
                projectDoc = saved;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is InvalidDataException ||
                                       ex is System.Runtime.Serialization.SerializationException)
            {
                MessageBox.Show(this, "The project could not be saved:\n" + ex.Message, "Save", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (path != currentProjectPath) { ProjectFolder.OpenFor(path); AddToRecentFiles(path); }
            currentProjectPath = path;
            Log.Info("Project", $"Saved {path} (revision {projectDoc.Revision}; {(windLoadsLinked ? "wind loads" : "manual loads")}; this app's data only).");
            MarkSaved();
            WatchProject(); // this save is not another app's
            if (lost.Count > 0)
                MessageBox.Show(this, "The design of " + string.Join(", ", lost) + " was not saved: the Building Model has deleted it since this project was opened.",
                                "Save", MessageBoxButton.OK, MessageBoxImage.Information);
            if (geometryMoved)
            {
                // the building was changed in the Building Model while this window had it open: show it as it is now
                OpenProject(path, reload: true);
                ShowStatusMessage(LogLevel.Info, "Saved. The building had been changed in the Building Model meanwhile: it is shown as it is now.");
            }
            return true;
        }

        // ------------------------------------------------------------------ the model's geometry → the wind calculation

        /// <summary>The model's wind geometry when last sent to the wind screen (to send it again only when it changed).</summary>
        private string modelWindKey;

        /// <summary>
        /// The wind calculation's building is the model's: its size, stories, plate heights and roof (the wind screen shows
        /// them, not editable), with the wind screen's enclosure. Recalculates when they changed.
        /// </summary>
        private void ModelToWind()
        {
            if (windSetup == null) return;
            BuildingData geometry;
            string note;
            try
            {
                var input = WindAdapter.From(building, new WindOptions { Enclosure = windSetup.Building.EnclosureType, ParapetEnclosure = windSetup.Building.ParapetEnclosure });
                geometry = input.Building;
                note = input.Notes.Count > 0 ? string.Join(" ", input.Notes) : null;
            }
            catch (InvalidOperationException ex)
            {
                geometry = null;
                note = ex.Message;
            }
            string key = geometry == null ? "none:" + note
                : string.Join("|", geometry.BuildingLength, geometry.BuildingWidth, geometry.NumberOfStories, string.Join(",", geometry.StoryPlateHeights),
                              geometry.RoofType, geometry.RoofPitch, geometry.RidgeDirection, geometry.OverhangWidth, geometry.ParapetHeight);
            if (key == modelWindKey) return;
            bool hadNone = modelWindKey == null || modelWindKey.StartsWith("none:", StringComparison.Ordinal);
            modelWindKey = key;
            if (geometry == null)
            {
                // an empty building (the start, before a project opens, or nothing drawn above the ground yet) is expected:
                // a note, not a warning that stays in the status bar after the project's geometry arrives
                bool empty = building == null || (building.Diaphragms?.Count ?? 0) == 0;
                if (empty) Log.Info("Wind", "No wind geometry yet: " + note);
                else Log.Warning("Wind", "No wind geometry from the model: " + note, null);
                buildingData = null;
                if (windSetup.BuildingPreview != null) windSetup.BuildingPreview = null; // nothing to show: the wind viewer's own message
                windLoadCalculator_CC = windLoadCalculator_MWFRS_Length = windLoadCalculator_MWFRS_Width = null;
                CreateAndAssignResultControls();
                ApplyLinkedWindLoads();
                return;
            }
            if (hadNone) ShowStatusMessage(LogLevel.Info, $"Wind geometry from the model: {geometry.BuildingLength:0.#} × {geometry.BuildingWidth:0.#} ft, {geometry.NumberOfStories} stor{(geometry.NumberOfStories == 1 ? "y" : "ies")}.");
            windSetup.SetModelGeometry(geometry, note);
            ShowWindPreview(); // the Building page shows the model in 3D (the Shear walls 3D view), roof included
            buildingData = windSetup.Building.Clone();
            Log.Info("Wind", $"Building from the model: L {geometry.BuildingLength:0.##} ft, B {geometry.BuildingWidth:0.##} ft, {geometry.NumberOfStories} stor{(geometry.NumberOfStories == 1 ? "y" : "ies")} " +
                             $"(plates {string.Join(", ", geometry.StoryPlateHeights.Select(h => h.ToString("0.##")))} ft), roof {geometry.RoofType} {geometry.RoofPitch:0.#}°, " +
                             $"h {geometry.MeanRoofHeight:0.##} ft" + (note == null ? "." : ". " + note));
            RecalculateWind();
        }

        /// <summary>The wind inputs and the loads they give, in the project's log.</summary>
        private void LogWindInputs()
        {
            if (!windLoadsLinked || windLoadParams == null) { Log.Info("Wind", "Manual loads: " + string.Join("; ", stories.Select(s => $"{s.Level.Name} X {s.Calculator.V_x:0.##} k, Y {s.Calculator.V_y:0.##} k"))); return; }
            var p = windLoadParams;
            Log.Info("Wind", $"{windVersion}, risk {p.RiskCategory}, V {p.WindSpeed:0} mph, exposure {p.ExposureCategory}, Kzt {p.Kzt:0.00}, G {p.GustFactor:0.00}, " +
                             $"{p.LateralProcedure}, enclosure {buildingData?.EnclosureType}" + (windSetup?.OnlyCases1And3 == true ? ", cases 1 and 3 only" : "") + ". Loads: " +
                             (windLoadCalculator_MWFRS_Length == null ? "cannot be calculated with these inputs." : string.Join("; ", stories.Select(s => $"{s.Level.Name} X {s.Calculator.V_x:0.##} k, Y {s.Calculator.V_y:0.##} k"))));
        }

        // ------------------------------------------------------------------ the model's errors stop the design

        /// <summary>The model's errors (not warnings): while there are any, nothing is designed or reported.</summary>
        private List<ModelIssue> ModelErrors() => ModelChecks.Run(building).Concat(ShearWallAdapter.Checks(building)).Where(i => i.Severity == Severity.Error).ToList();

        private string ModelErrorTodo()
        {
            if (!HasProject) return "Open or make a project first (File > New project or Open).";
            var errors = ModelErrors();
            if (errors.Count == 0) return null;
            return $"The building model or its shear walls have {errors.Count} error{(errors.Count == 1 ? "" : "s")}; fix {(errors.Count == 1 ? "it" : "them")} before the design " +
                   $"(the ✗ button lists them). First: {errors[0].Message}";
        }

        // ------------------------------------------------------------------ Edit building: the Building Model editor

        private Process editorProcess;
        private ProjectWatcher editorWatcher;
        private Border editorOverlay;

        /// <summary>The Building Model editor, next to this program (copied there by the build).</summary>
        private static string EditorPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BuildingModel.exe");

        /// <summary>The Load Manager, next to this program (copied there by the build).</summary>
        private static string LoadManagerPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LoadManagerApp.exe");

        /// <summary>The other app open on the project ("Building Model editor", "Load Manager"), while one is.</summary>
        private string editorName = "Building Model editor";

        private void EditBuilding_Click(object sender, RoutedEventArgs e) => EditBuilding();

        private void EditLoads_Click(object sender, RoutedEventArgs e) => EditLoads();

        /// <summary>
        /// Saves the project and opens it in the Building Model editor, the one place the building's levels, size and roof are
        /// edited. While it is open this window waits (so the two never change the file at once) and reloads the project each
        /// time the editor saves it.
        /// </summary>
        private void EditBuilding() => OpenInOtherApp(EditorPath, "Building Model editor", "Edit building",
            "The building is open in the Building Model editor.\n\nEdit the levels, size, roof, walls and diaphragms there and save: " +
            "this window updates each time. Close the editor to work here again.");

        /// <summary>
        /// Saves the project and opens it in the Load Manager, where the gravity loads are edited (this app only reads them): the
        /// floor and roof loads, zones, a wall's top load by hand; it also shows the foundation loads combined. This window waits
        /// and reloads the project each time the Load Manager saves.
        /// </summary>
        private void EditLoads() => OpenInOtherApp(LoadManagerPath, "Load Manager", "Edit loads",
            "The project is open in the Load Manager.\n\nEdit the floor and roof loads, load zones and wall loads there and save: " +
            "this window updates each time. Close the Load Manager to work here again.");

        /// <summary>The Wind Load Calculator, next to this program (copied there by the build).</summary>
        private static string WindAppPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WindLoadCalculator.exe");

        private void EditWind_Click(object sender, RoutedEventArgs e) => EditWind();

        /// <summary>
        /// Saves the project and opens it in the Wind Load Calculator, the one place the wind is set up (the site, speed,
        /// exposure, enclosure, components). This window shows the wind read-only, waits, and reloads the project each time
        /// the wind calculator saves: the walls then take its wind cases.
        /// </summary>
        private void EditWind() => OpenInOtherApp(WindAppPath, "Wind Load Calculator", "Edit wind",
            "The project is open in the Wind Load Calculator.\n\nSet up the wind there (site, speed, exposure, enclosure) and save: " +
            "this window updates each time and the walls take the new wind cases. Close the wind calculator to work here again.");

        /// <summary>
        /// The wind page: the wind calculator's own screen, read-only, under a bar that says where the wind comes from and
        /// opens the Wind Load Calculator to change it.
        /// </summary>
        private FrameworkElement WindPage(WindSetupControl setup)
        {
            setup.ReadOnly = true;
            windBarText = new TextBlock { TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center, Foreground = Controls.Ui.Ink };
            var edit = new Button
            {
                Content = "Edit wind in the Wind Load Calculator…", Padding = new Thickness(12, 4, 12, 4), Margin = new Thickness(12, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center, ToolTip = "Saves this project and opens it in the Wind Load Calculator, the one place the wind is set up. This window updates when it saves.",
            };
            edit.Click += EditWind_Click;
            var bar = new DockPanel { Margin = new Thickness(0) };
            DockPanel.SetDock(edit, Dock.Right);
            bar.Children.Add(edit);
            bar.Children.Add(windBarText);
            var border = new Border
            {
                Child = bar, Padding = new Thickness(12, 8, 12, 8), Background = new SolidColorBrush(Color.FromRgb(0xE8, 0xF4, 0xF5)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x8C, 0xC9, 0xCD)), BorderThickness = new Thickness(0, 0, 0, 1),
            };
            DockPanel.SetDock(border, Dock.Top);
            windSetupShown = setup;
            windNone = new TextBlock
            {
                Text = "No wind for this project yet.\n\nThe wind is set up in the Wind Load Calculator (Edit wind above): the site, speed, exposure and enclosure. " +
                       "This page then shows its results, read only, and the shear walls take every one of its wind cases.",
                TextWrapping = TextWrapping.Wrap, FontSize = 15, MaxWidth = 560, TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Foreground = Controls.Ui.Ink,
            };
            UpdateWindBar();
            return new DockPanel { Children = { border, new Grid { Children = { setup, windNone } } } };
        }

        private TextBlock windBarText, windNone;
        private WindSetupControl windSetupShown;

        /// <summary>
        /// The workspace a project opens on: the wind's results when the project has no wind, or its wind cannot be calculated
        /// for the building as it is now (the walls need it first); else the shear walls, on the current wind.
        /// </summary>
        private void OpenOnWorkspace(WindCheckResult wind)
        {
            bool current = wind != null && wind.Calculated;
            if (current) rbWorkspaceWalls.IsChecked = true;
            else rbWorkspaceWind.IsChecked = true;
            Log.Info("Project", current ? $"The wind is current ({wind.Cases.Count} cases): opened on the shear walls."
                                        : "No current wind (" + (wind?.NotChecked ?? string.Join(" ", wind?.Problems ?? new List<string>())) + "): opened on the wind.");
        }

        /// <summary>The wind bar's words: where the wind comes from, or that there is none yet (then the page says only that).</summary>
        private void UpdateWindBar()
        {
            if (windBarText == null) return;
            windBarText.Text = !HasProject ? "The wind is set up in the Wind Load Calculator, on a project."
                : windLoadsLinked ? "The wind is the Wind Load Calculator's results for this project, read only. The walls take every one of its wind cases. Edit wind changes them there."
                : "This project has no wind yet. Set it up in the Wind Load Calculator: the walls then take its wind cases. Until then they use manual loads.";
            bool none = HasProject && !windLoadsLinked; // no results to show: not the calculator's starting values as if they were the project's
            if (windNone != null) windNone.Visibility = none ? Visibility.Visible : Visibility.Collapsed;
            if (windSetupShown != null) windSetupShown.Visibility = none ? Visibility.Collapsed : Visibility.Visible;
        }

        private string editorOverlayText;

        private void OpenInOtherApp(string exe, string name, string caption, string overlay)
        {
            if (!HasProject) return;
            if (editorProcess != null && !editorProcess.HasExited) return;
            if (!File.Exists(exe))
            {
                Log.Error("Project", $"The {name} was not found: " + exe, null);
                MessageBox.Show(this, $"The {name} ({Path.GetFileName(exe)}) is not next to this program:\n" + exe, caption, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (HasUnsavedChanges && !SaveProject(saveAs: false)) return;
            try
            {
                editorProcess = Process.Start(new ProcessStartInfo(exe, "\"" + currentProjectPath + "\"") { UseShellExecute = false });
            }
            catch (Exception ex) when (ex is System.ComponentModel.Win32Exception || ex is InvalidOperationException)
            {
                Log.Error("Project", $"Could not start the {name}", ex);
                MessageBox.Show(this, $"The {name} could not be started:\n" + ex.Message, caption, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            editorName = name;
            editorOverlayText = overlay;
            Log.Info("Project", $"{name} opened on " + currentProjectPath);
            editorProcess.EnableRaisingEvents = true;
            editorProcess.Exited += (s, a) => Dispatcher.BeginInvoke(new Action(EditorClosed));

            editorWatcher = new ProjectWatcher(currentProjectPath);
            editorWatcher.ChangedOnDisk += (s, a) => ReloadFromEditor();
            ShowEditorOverlay(true);
        }

        private DateTime lastReloaded;

        private void ReloadFromEditor()
        {
            if (!HasProject || !File.Exists(currentProjectPath)) return;
            var stamp = File.GetLastWriteTimeUtc(currentProjectPath);
            if (stamp == lastReloaded) return;
            lastReloaded = stamp;
            OpenProject(currentProjectPath, reload: true);
            ShowStatusMessage(LogLevel.Info, $"The project was updated from the {editorName}.");
        }

        private void EditorClosed()
        {
            editorWatcher?.Dispose();
            editorWatcher = null;
            Log.Info("Project", $"{editorName} closed.");
            editorProcess = null;
            ReloadFromEditor(); // a save just before closing
            ShowEditorOverlay(false);
        }

        private void ShowEditorOverlay(bool show)
        {
            if (editorOverlay != null && show) ((TextBlock)((Border)editorOverlay.Child).Child).Text = editorOverlayText ?? "";
            if (editorOverlay == null)
            {
                var text = new TextBlock
                {
                    Text = editorOverlayText ?? "",
                    TextWrapping = TextWrapping.Wrap, FontSize = 15, MaxWidth = 520, TextAlignment = TextAlignment.Center,
                };
                editorOverlay = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(0xD8, 0xF7, 0xF8, 0xFA)),
                    Child = new Border
                    {
                        Background = Brushes.White, BorderBrush = new SolidColorBrush(Color.FromRgb(0x9C, 0xC0, 0xEC)), BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(8), Padding = new Thickness(28, 22, 28, 22), Child = text,
                        HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
                    },
                };
                AddOverlay(editorOverlay);
            }
            editorOverlay.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }

        // ------------------------------------------------------------------ the start screen: nothing without a project

        private Grid windowRoot;
        private Border startScreen;

        /// <summary>The window's content goes into a grid so the start screen and the editor notice can lie over it.</summary>
        private void AddOverlay(UIElement overlay)
        {
            if (windowRoot == null)
            {
                var content = (UIElement)Content;
                Content = null;
                windowRoot = new Grid();
                windowRoot.Children.Add(content);
                Content = windowRoot;
            }
            windowRoot.Children.Add(overlay);
        }

        /// <summary>Until a project is open the window shows only this: make a project, open one (or a recent one), or exit.</summary>
        private void ShowStartScreen()
        {
            if (startScreen == null)
            {
                var panel = new StackPanel { Width = 460 };
                panel.Children.Add(new TextBlock { Text = "Shear Wall Calculator", FontSize = 24, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 6) });
                panel.Children.Add(new TextBlock
                {
                    Text = "Each design project is a folder: the building model, the reports and the logs are kept there together. " +
                           "Start a project from the building's size, or open a building model.",
                    TextWrapping = TextWrapping.Wrap, Foreground = new SolidColorBrush(Color.FromRgb(0x52, 0x51, 0x4E)), Margin = new Thickness(0, 0, 0, 16),
                });
                panel.Children.Add(StartButton("New project…", "Pick a folder, then the building's length, width and stories", () => NewProject()));
                panel.Children.Add(StartButton("Open a project…", "A building model (.bldg.json)", () => { string p = ProjectDialogs.OpenModelPath(this); if (p != null) OpenProject(p); }));
                var recent = (Properties.Settings.Default.RecentFiles ?? new System.Collections.Specialized.StringCollection()).Cast<string>()
                             .Where(f => f.EndsWith(ProjectFolder.ModelExtension, StringComparison.OrdinalIgnoreCase) && File.Exists(f)).Take(6).ToList();
                if (recent.Count > 0)
                {
                    panel.Children.Add(new TextBlock { Text = "RECENT", FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = Brushes.Gray, Margin = new Thickness(0, 14, 0, 4) });
                    foreach (string f in recent)
                    {
                        string file = f;
                        panel.Children.Add(StartButton(Path.GetFileName(file).Replace(ProjectFolder.ModelExtension, ""), Path.GetDirectoryName(file), () => OpenProject(file)));
                    }
                }
                panel.Children.Add(StartButton("Exit", null, Close));
                startScreen = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(0xF7, 0xF8, 0xFA)),
                    Child = new Border
                    {
                        Background = Brushes.White, BorderBrush = new SolidColorBrush(Color.FromRgb(0xE1, 0xE0, 0xD9)), BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(10), Padding = new Thickness(30, 26, 30, 26), Child = panel,
                        HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
                    },
                };
                AddOverlay(startScreen);
            }
            startScreen.Visibility = Visibility.Visible;
            Title = "Shear Wall Calculator";
        }

        private void HideStartScreen()
        {
            if (startScreen != null) startScreen.Visibility = Visibility.Collapsed;
        }

        private static Button StartButton(string title, string detail, Action click)
        {
            var sp = new StackPanel();
            sp.Children.Add(new TextBlock { Text = title, FontSize = 14, FontWeight = FontWeights.SemiBold });
            if (!string.IsNullOrEmpty(detail))
                sp.Children.Add(new TextBlock { Text = detail, FontSize = 11.5, Foreground = Brushes.Gray, TextTrimming = TextTrimming.CharacterEllipsis });
            var b = new Button
            {
                Content = sp, HorizontalContentAlignment = HorizontalAlignment.Left, Padding = new Thickness(12, 7, 12, 7), Margin = new Thickness(0, 0, 0, 6),
                Background = Brushes.White, BorderBrush = new SolidColorBrush(Color.FromRgb(0xC3, 0xC2, 0xB7)), ToolTip = detail,
            };
            b.Click += (s, e) => click();
            return b;
        }
    }
}
