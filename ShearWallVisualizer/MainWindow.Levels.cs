using calculator;
using ShearWallCalculator;
using ShearWallCalculator.Diagnostics;
using ShearWallCalculator.Drawing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ShearWallVisualizer
{
    /// <summary>
    /// Multi-story (1 to 3 floors, set in the building inputs). Each level is one calculator: the walls of that story and the
    /// diaphragm over them (level 1 = the first story and the floor over it; the top level = the roof). The plan shows one
    /// level at a time, with the level below in gray to draw on; the analysis runs from the roof down, each level carrying
    /// the walls above it (<see cref="StoryCascade"/>). The levels share one set of design settings.
    /// </summary>
    public partial class MainWindow
    {
        /// <summary>Bottom to top; <see cref="Calculator"/> is <c>levels[currentLevel]</c>.</summary>
        private List<ShearWallCalculatorBase> levels = new List<ShearWallCalculatorBase>();
        private int currentLevel;

        /// <summary>What to check about the levels (from the last analysis): walls above with no wall under them, approximations.</summary>
        private List<string> levelNotes = new List<string>();

        private ShearWallCalculatorBase LevelBelow => currentLevel > 0 && currentLevel < levels.Count ? levels[currentLevel - 1] : null;

        private string LevelName(int i) => i == levels.Count - 1 && levels.Count > 1 ? $"Level {i + 1} (roof)" : $"Level {i + 1}";

        /// <summary>A new or opened project: these levels (bottom first), showing <paramref name="active"/>.</summary>
        private void SetLevels(IEnumerable<ShearWallCalculatorBase> all, int active = 0)
        {
            levels = (all ?? Enumerable.Empty<ShearWallCalculatorBase>()).Where(c => c != null).ToList();
            if (levels.Count == 0) levels.Add(new ShearWallCalculator_RigidDiaphragm());
            ShareDesignSettings(levels[0].DesignSettings);
            currentLevel = Math.Max(0, Math.Min(active, levels.Count - 1));
            Calculator = levels[currentLevel];
            histories.Clear();
            selectedKind = null;
            selectedId = -1;
            selectionCardOpen = false;
            UpdateLevelButtons();
        }

        /// <summary>The design method, edition, hold-down list etc. are the project's: every level uses the same object.</summary>
        private void ShareDesignSettings(ShearWallCalculator.Design.DesignSettings settings)
        {
            if (settings == null) return;
            foreach (var l in levels) l.DesignSettings = settings;
        }

        /// <summary>
        /// The building inputs say how many floors: adds empty levels, or removes the top ones (asking first when they have
        /// walls or diaphragms; No puts the number of floors back).
        /// </summary>
        private void MatchLevelsToStories(int stories)
        {
            if (levels.Count == 0 || !levels.Contains(Calculator)) SetLevels(new[] { Calculator });
            stories = Math.Max(1, Math.Min(ASCE7WindLoadCalculator.BuildingData.MaxStories, stories));
            if (stories == levels.Count) return;
            if (stories > levels.Count)
            {
                while (levels.Count < stories)
                    levels.Add(new ShearWallCalculator_RigidDiaphragm { DesignSettings = levels[0].DesignSettings });
                Log.Info($"{levels.Count} levels: draw each one with the LEVEL buttons (Copy ↑ starts a level from the one below).");
            }
            else
            {
                var drop = levels.Skip(stories).ToList();
                int items = drop.Sum(l => l._wall_system._walls.Count + l._diaphragm_system._diaphragms.Count);
                if (items > 0 && MessageBox.Show(this,
                        $"{(stories == 1 ? "One floor" : stories + " floors")}: delete {string.Join(" and ", drop.Select(l => LevelName(levels.IndexOf(l))))}, " +
                        $"with {items} wall{(items == 1 ? "" : "s")} and diaphragm{(items == 1 ? "" : "s")}?",
                        "Number of floors", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                {
                    if (buildingData != null) buildingData.NumberOfStories = levels.Count;
                    SyncWindSetup(); // the floors go back to the levels there are
                    return;
                }
                foreach (var l in drop) histories.Remove(l);
                levels.RemoveRange(stories, levels.Count - stories);
                if (currentLevel >= levels.Count) ShowLevel(levels.Count - 1, update: false);
            }
            UpdateLevelButtons();
        }

        /// <summary>Shows and edits level <paramref name="i"/> (0 = level 1).</summary>
        private void ShowLevel(int i, bool update = true)
        {
            if (i < 0 || i >= levels.Count) return;
            if (i != currentLevel || !ReferenceEquals(Calculator, levels[i]))
            {
                ResetInputMode();
                selectedKind = null;
                selectedId = -1;
                selectionCardOpen = false;
                currentLevel = i;
                Calculator = levels[i];
                ImageForNewProject(); // each level has its own plan image
            }
            UpdateLevelButtons();
            if (!update) return;
            UpdateShearWallUI();
            ShowStatusMessage(LogLevel.Info, $"Editing {LevelName(i)}." + (LevelBelow != null ? $" Level {i} shows in gray and its walls snap." : ""));
        }

        private void LevelButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b && int.TryParse(b.CommandParameter as string, out int n)) ShowLevel(n - 1);
        }

        /// <summary>Copy ↑: puts a copy of every wall (with its design) and diaphragm of the level below on this level.</summary>
        private void CopyFromBelow_Click(object sender, RoutedEventArgs e)
        {
            var below = LevelBelow;
            if (below == null) return;
            int n = DrawingEdits.CopyFloor(below, Calculator);
            UpdateShearWallUI();
            ShowStatusMessage(LogLevel.Info, n == 0 ? $"Level {currentLevel} has nothing to copy."
                : $"Copied {n} walls and diaphragms from level {currentLevel} to {LevelName(currentLevel)}. Ctrl+Z undoes it.");
        }

        private void UpdateLevelButtons()
        {
            if (levelGroup == null) return;
            var buttons = new[] { btnLevel1, btnLevel2, btnLevel3 };
            for (int k = 0; k < buttons.Length; k++)
            {
                buttons[k].Visibility = k < levels.Count ? Visibility.Visible : Visibility.Collapsed;
                buttons[k].Tag = k == currentLevel ? "on" : null;
                buttons[k].Content = k == levels.Count - 1 && levels.Count > 1 ? $"{k + 1} Roof" : $"{k + 1}";
                buttons[k].ToolTip = k < levels.Count ? $"Show and edit {LevelName(k)}: the walls of story {k + 1} and the " +
                                                        (k == levels.Count - 1 ? "roof" : $"floor over them") : null;
            }
            miCopyFromBelow.IsEnabled = currentLevel > 0;
            btnAddFloor.IsEnabled = levels.Count < FloorEdits.MaxFloors;
            btnAddFloor.ToolTip = levels.Count < FloorEdits.MaxFloors
                ? $"ADD FLOOR: level {levels.Count + 1} on top. Asks for its plate height (which sets the floor elevations and the eave), and can start from a copy of level {levels.Count}."
                : $"This app handles up to {FloorEdits.MaxFloors} floors.";
            btnDeleteFloor.ToolTip = levels.Count > 1
                ? $"DELETE FLOOR: removes {LevelName(currentLevel)} with its walls and diaphragms" + (currentLevel < levels.Count - 1 ? "; the floors above move down one." : ".") + " You are told first what it does."
                : "The building keeps one floor: this clears level 1 (Ctrl+Z undoes it).";
            UpdateLevelBanners();
        }

        /// <summary>Every level, roof first, each carrying the walls above it. Replaces a single calculation.</summary>
        private void AnalyzeLevels()
        {
            if (levels.Count == 0 || !levels.Contains(Calculator)) SetLevels(new[] { Calculator });
            ShareDesignSettings(Calculator.DesignSettings); // an undo restores a copy of the settings on this level
            levelNotes = StoryCascade.Analyze(levels);
            UpdateLevelBanners(); // the plate heights may have changed in the building inputs
            // walls drawn before a plate height changed keep their own height: say so
            var plates = PlateHeights();
            for (int i = 0; i < levels.Count && i < plates.Count; i++)
            {
                int off = levels[i]._wall_system._walls.Values.Count(w => Math.Abs(w.WallHeight - plates[i]) > 0.01);
                if (off > 0)
                    levelNotes.Add($"Level {i + 1}: {off} wall{(off == 1 ? " is" : "s are")} not the story's {plates[i]:0.##} ft plate height (set each wall's height on the Walls & diaphragms tab).");
            }
        }

        /// <summary>Linked wind: each level gets the wind on its strip of the building (mid-story to mid-story).</summary>
        private void SetWindLoadsPerLevel(ASCE7WindLoadCalculator.WindAnalysisResult analysis, bool onlyCases1And3, out string message)
        {
            var r = ASCE7WindLoadCalculator.StoryWindLoads.Compute(analysis, onlyCases1And3, levels.Count);
            for (int i = 0; i < levels.Count; i++)
                levels[i].SetLoadCases(r.Levels[i].LoadCases
                    .Select(c => new LateralLoadCase(c.Name, c.Vx, c.Vy, c.Mt, applyMinimumEccentricity: false)));
            var b = r.Building;
            message = $"{b.LoadCases.Count} load cases (from W {b.AlongLength.DesignKips:0.0} k, from S {b.AlongWidth.DesignKips:0.0} k). ";
            if (levels.Count > 1)
                message += $"Split over {levels.Count} floors ({r.StoryHeight:0.#} ft stories), from W / from S: " +
                           string.Join(",  ", r.Levels.Select((l, i) => $"{LevelName(i)} {l.AlongLengthDesignKips:0.0} / {l.AlongWidthDesignKips:0.0} k")) + ". ";
            message += "Updated automatically. Per-wall results: 2 Shear walls, Calculations tab.";
        }

        // ------------------------------------------------------------------ the level below, in gray

        private static readonly Pen GhostWall = FrozenDashed(Color.FromArgb(0x90, 0x80, 0x80, 0x80), 3);
        private static readonly Pen GhostEdge = FrozenDashed(Color.FromArgb(0x70, 0x80, 0x80, 0x80), 1);

        private static Pen FrozenDashed(Color c, double thickness)
        {
            var p = new Pen(new SolidColorBrush(c), thickness) { DashStyle = new DashStyle(new double[] { 3, 2 }, 0) };
            p.Freeze();
            return p;
        }

        /// <summary>The walls and diaphragms of the level below, dashed gray under this level's (when shown).</summary>
        private void DrawLevelBelow(DrawingContext ctx)
        {
            var below = LevelBelow;
            if (below == null) return;
            if (!hideDiaphragms)
                foreach (var d in below._diaphragm_system._diaphragms.Values)
                {
                    Point a = WorldToScreen(d.P1, m_layers), c = WorldToScreen(d.P3, m_layers);
                    ctx.DrawRectangle(null, GhostEdge, new Rect(a, c));
                }
            if (hideWalls) return;
            foreach (var w in below._wall_system._walls.Values)
                ctx.DrawLine(GhostWall, WorldToScreen(w.Start, m_layers), WorldToScreen(w.End, m_layers));
            var label = new FormattedText($"Level {currentLevel} below (gray)", CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight,
                new Typeface("Segoe UI"), 12, Brushes.Gray, VisualTreeHelper.GetDpi(this).PixelsPerDip);
            ctx.DrawText(label, new Point(8, m_layers.ActualHeight - 22));
        }

        // ------------------------------------------------------------------ add and delete floors

        /// <summary>The plate height of each story in use, bottom first (ft), from the building inputs.</summary>
        private List<double> PlateHeights() =>
            buildingData != null ? buildingData.StoryPlateHeights.ToList() : levels.Select(_ => defaultWallHeight).ToList();

        /// <summary>The plate height of the level being drawn (ft): new walls get it.</summary>
        private double CurrentPlateHeight()
        {
            var plates = PlateHeights();
            double h = currentLevel < plates.Count ? plates[currentLevel] : defaultWallHeight;
            return h > 0 ? h : defaultWallHeight;
        }

        /// <summary>The floors changed here: the building inputs get the new stories and plate heights, the wind is shared again.</summary>
        private void ApplyFloors(List<double> plates)
        {
            if (buildingData != null)
            {
                buildingData.NumberOfStories = levels.Count;
                buildingData.PlateHeights = plates;
                SyncWindSetup();
            }
            histories.Keys.Where(k => !levels.Contains(k)).ToList().ForEach(k => histories.Remove(k));
            RecalculateWind(); // re-sends the linked wind loads, one set per level
            UpdateLevelButtons();
            UpdateShearWallUI();
        }

        /// <summary>ADD FLOOR: a new top level, after asking for its plate height (and whether to copy the level below).</summary>
        private void AddFloor_Click(object sender, RoutedEventArgs e)
        {
            if (levels.Count == 0 || !levels.Contains(Calculator)) SetLevels(new[] { Calculator });
            if (levels.Count >= FloorEdits.MaxFloors)
            {
                MessageBox.Show(this, $"This app handles up to {FloorEdits.MaxFloors} floors.", "Add floor", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var plates = PlateHeights();
            var top = levels[levels.Count - 1];
            bool topHasContent = top._wall_system._walls.Count + top._diaphragm_system._diaphragms.Count > 0;
            var dialog = new Dialogs.AddFloorDialog(levels.Count + 1, plates.Sum(), plates.Count > 0 ? plates[plates.Count - 1] : defaultWallHeight, topHasContent) { Owner = this };
            if (dialog.ShowDialog() != true) return;

            var level = new ShearWallCalculator_RigidDiaphragm { DesignSettings = levels[0].DesignSettings };
            levels.Add(level);
            if (dialog.CopyBelow)
            {
                DrawingEdits.CopyFloor(top, level);
                foreach (var w in level._wall_system._walls.Values) w.WallHeight = dialog.PlateHeight; // the new story's walls
            }
            ApplyFloors(FloorEdits.PlatesAfterAdd(plates, dialog.PlateHeight));
            ShowLevel(levels.Count - 1);
            ShowStatusMessage(LogLevel.Info, $"Added {LevelName(levels.Count - 1)}: floor at {plates.Sum():0.##} ft, plate height {dialog.PlateHeight:0.##} ft, eave {plates.Sum() + dialog.PlateHeight:0.##} ft." +
                                             (dialog.CopyBelow ? $" Started from a copy of level {levels.Count - 1}." : " Draw its walls and diaphragm."));
        }

        /// <summary>
        /// DELETE FLOOR: removes the level being shown, after saying what it does (the levels above move down onto the one
        /// below, which may leave walls without a wall under them). The only floor is cleared instead.
        /// </summary>
        private void DeleteFloor_Click(object sender, RoutedEventArgs e)
        {
            if (levels.Count == 0 || !levels.Contains(Calculator)) SetLevels(new[] { Calculator });
            int k = currentLevel;
            if (levels.Count == 1)
            {
                int items = levels[0]._wall_system._walls.Count + levels[0]._diaphragm_system._diaphragms.Count;
                if (items == 0) { ShowStatusMessage(LogLevel.Info, "Level 1 is already empty."); return; }
                if (MessageBox.Show(this, $"The building keeps at least one floor. Clear level 1 instead ({items} walls and diaphragms)?\n\nCtrl+Z can undo the clearing.",
                        "Delete floor", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
                ResetInputMode();
                selectedKind = null;
                selectedId = -1;
                FloorEdits.Clear(levels[0]);
                UpdateShearWallUI(); // recorded in the undo history
                return;
            }
            var plates = PlateHeights();
            var lines = FloorEdits.DeleteConsequences(levels, k, plates);
            if (MessageBox.Show(this, $"Delete {LevelName(k)}?\n\n" + string.Join("\n", lines.Select(l => "• " + l)) + "\n\nThis cannot be undone (save first to keep a copy).",
                    "Delete floor", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) != MessageBoxResult.Yes) return;

            ResetInputMode();
            selectedKind = null;
            selectedId = -1;
            selectionCardOpen = false;
            string name = LevelName(k);
            FloorEdits.Delete(levels, k);
            currentLevel = Math.Min(k, levels.Count - 1);
            Calculator = levels[currentLevel];
            ImageForNewProject();
            ApplyFloors(FloorEdits.PlatesAfterDelete(plates, k));
            ShowLevel(currentLevel);
            ShowStatusMessage(LogLevel.Info, $"Deleted {name}. Now {levels.Count} floor{(levels.Count == 1 ? "" : "s")}, eave {PlateHeights().Sum():0.##} ft; showing {LevelName(currentLevel)}.");
        }

        // ------------------------------------------------------------------ which level is shown

        private readonly List<TextBlock> levelBanners = new List<TextBlock>();

        /// <summary>The level-specific tabs (walls &amp; diaphragms, calculations, design) with a banner naming the level shown.</summary>
        private UIElement WithLevelBanner(UIElement content)
        {
            var text = new TextBlock { FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White, TextTrimming = TextTrimming.CharacterEllipsis };
            levelBanners.Add(text);
            var banner = new Border { Background = new SolidColorBrush(Color.FromRgb(0x2A, 0x78, 0xD6)), Padding = new Thickness(10, 5, 10, 5), Child = text };
            DockPanel.SetDock(banner, Dock.Top);
            var dock = new DockPanel { LastChildFill = true };
            dock.Children.Add(banner);
            dock.Children.Add(content);
            UpdateLevelBanners();
            return dock;
        }

        /// <summary>"LEVEL 2 of 3 · story 2 walls and the floor over them · floor at 10 ft, plate 9 ft".</summary>
        private string LevelDescription()
        {
            int n = Math.Max(1, levels.Count);
            var plates = PlateHeights();
            double floor = plates.Take(currentLevel).Sum();
            string what = currentLevel == n - 1 ? "walls and the roof" : "walls and the floor over them";
            string heights = currentLevel < plates.Count ? $" · on {(currentLevel == 0 ? "the foundation" : $"the floor at {floor:0.##} ft")}, plate {plates[currentLevel]:0.##} ft" : "";
            return $"LEVEL {currentLevel + 1} of {n}{(currentLevel == n - 1 && n > 1 ? " (roof)" : "")} · story {currentLevel + 1} {what}{heights}";
        }

        private void UpdateLevelBanners()
        {
            string text = LevelDescription();
            foreach (var t in levelBanners) t.Text = text;
        }

        /// <summary>The level on the drawing itself, top left.</summary>
        private void DrawLevelTag(DrawingContext ctx)
        {
            if (levels.Count == 0) return;
            var label = new FormattedText(levels.Count > 1 ? $"LEVEL {currentLevel + 1} of {levels.Count}{(currentLevel == levels.Count - 1 ? " (roof)" : "")}" : "LEVEL 1",
                CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
                16, Brushes.White, VisualTreeHelper.GetDpi(this).PixelsPerDip);
            var box = new Rect(8, 8, label.Width + 16, label.Height + 6);
            ctx.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(0xE0, 0x2A, 0x78, 0xD6)), null, box, 4, 4);
            ctx.DrawText(label, new Point(16, 11));
        }

        // ------------------------------------------------------------------ show walls / diaphragms (separately)

        private bool hideWalls, hideDiaphragms;

        private void btnHideWalls_Click(object sender, RoutedEventArgs e)
        {
            hideWalls = !hideWalls;
            btnHideWalls.Tag = hideWalls ? null : "on";
            UpdateShearWallUI();
        }

        private void btnHideDiaphragms_Click(object sender, RoutedEventArgs e)
        {
            hideDiaphragms = !hideDiaphragms;
            btnHideDiaphragms.Tag = hideDiaphragms ? null : "on";
            UpdateShearWallUI();
        }
    }
}
