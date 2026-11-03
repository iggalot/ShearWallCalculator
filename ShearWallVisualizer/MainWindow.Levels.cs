using BuildingModel.Diagnostics;
using BuildingModel.Edits;
using BuildingModel.Model;
using ShearWallCalculator.ModelAdapter;
using calculator;
using ShearWallCalculator;
using ShearWallCalculator.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ShearWallVisualizer
{
    /// <summary>
    /// Multi-story. The building model has the levels (the ground and one level per story above it); each level above
    /// the ground is one designer story with its own calculator: the walls under that level and the diaphragm on it
    /// (the first story's walls and the floor over them; the top story's walls and the roof). The plan shows one level at
    /// a time, with the level below in gray to draw on; the analysis runs from the roof down, each story carrying the walls
    /// above it (<see cref="StoryCascade"/>). The stories share one set of design settings. Floors are added and deleted only in
    /// the Building Model editor (Edit building); copying the level below is a model edit here, so Ctrl+Z undoes it.
    /// </summary>
    public partial class MainWindow
    {
        /// <summary>The calculators, bottom to top (one per story of the model); <see cref="Calculator"/> is <c>levels[currentLevel]</c>.</summary>
        private List<ShearWallCalculatorBase> levels = new List<ShearWallCalculatorBase>();
        private int currentLevel;

        /// <summary>What to check about the levels (from the last analysis): walls above with no wall under them, approximations.</summary>
        private List<string> levelNotes = new List<string>();

        /// <summary>The model's name of story <paramref name="i"/>'s level (0 = the first story: "Level 2" in the model).</summary>
        private string LevelName(int i) => i >= 0 && i < stories.Count ? stories[i].Level.Name : $"Level {i + 2}";

        /// <summary>
        /// The level part of a schedule mark (SW2-1 is mark 1 on the Level 2 plan): the number ending the level's name, so the
        /// mark says which plan to look on; a level named otherwise gets its initial ("Roof": SWR-1), not its story number,
        /// which could be another level's name number.
        /// </summary>
        private string MarkLevel(int story) => ShearWallCalculator.Design.WallSchedule.LevelTag(LevelName(story), story);

        /// <summary>
        /// The loads table beside the workspace buttons, roof first: the wind on each level's strip of the building, what
        /// that level's walls carry (its strip plus every level above), and the foundation's total (the base shear, which
        /// is the wind calculator's building total). The level shown (or the foundation) is highlighted.
        /// </summary>
        private void ShowWorkspaceLoads()
        {
            var g = workspaceLoads;
            if (g == null) return;
            g.Children.Clear(); g.RowDefinitions.Clear(); g.ColumnDefinitions.Clear();
            for (int r = 0; r < 3; r++) g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            g.ToolTip = "Each level gets the wind on its strip of the building (mid-story to mid-story). A level's walls carry its own strip " +
                        "plus every level above, so the first story's walls, and the foundation, take the whole building's base shear " +
                        "(the wind calculator's total).";

            int n = levels.Count;
            int shown = foundationView ? -1 : Math.Max(0, levels.IndexOf(Calculator));
            string XY(double x, double y) => $"X {x:0.0}   Y {y:0.0} k";
            void Cell(int row, int c, string text, bool head = false, bool on = false, int span = 1, Brush ink = null)
            {
                var t = new TextBlock
                {
                    Text = text, FontSize = 12, Padding = new Thickness(8, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center,
                    FontWeight = head || on ? FontWeights.SemiBold : FontWeights.Normal,
                    Foreground = ink ?? (head ? Controls.Ui.InkSecondary : Controls.Ui.Ink),
                    HorizontalAlignment = c == 0 ? HorizontalAlignment.Left : HorizontalAlignment.Right,
                };
                var b = new Border { Child = t, Background = on ? Controls.Ui.SelectedFill : Brushes.Transparent };
                Grid.SetRow(b, row); Grid.SetColumn(b, c); Grid.SetRowSpan(b, span);
                g.Children.Add(b);
            }

            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Cell(0, 0, windLoadsLinked ? "Wind loads (auto)" : "MANUAL loads", head: true, ink: windLoadsLinked ? Controls.Ui.PassInk : Controls.Ui.WarnInk);
            Cell(1, 0, "Wind on the level");
            Cell(2, 0, "Its walls carry");

            int col = 1;
            for (int i = n - 1; i >= 0; i--, col++) // roof first: the order the load goes down
            {
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                bool on = i == shown;
                double cx = levels.Skip(i).Sum(l => l.V_x), cy = levels.Skip(i).Sum(l => l.V_y);
                Cell(0, col, LevelName(i), head: true, on: on);
                Cell(1, col, XY(levels[i].V_x, levels[i].V_y), on: on);
                Cell(2, col, XY(cx, cy), on: on);
            }
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Cell(0, col, "Foundation (total)", head: true, on: shown < 0);
            Cell(1, col, XY(levels.Sum(l => l.V_x), levels.Sum(l => l.V_y)), on: shown < 0, span: 2);
        }

        /// <summary>The design method, edition, hold-down list etc. are the project's: every level uses the same object.</summary>
        private void ShareDesignSettings(ShearWallCalculator.Design.DesignSettings settings)
        {
            if (settings == null) return;
            foreach (var l in levels) l.DesignSettings = settings;
            ApplyDiaphragmAssumption(settings.Diaphragm);
        }

        /// <summary>
        /// Rigid, flexible or envelope (the project setting): a level whose calculator is of another kind gets one of the right
        /// kind with the same walls, diaphragms and loads, everywhere the level's calculator is kept.
        /// </summary>
        private void ApplyDiaphragmAssumption(ShearWallCalculator.Design.DiaphragmAssumption kind)
        {
            bool changed = false;
            for (int i = 0; i < levels.Count; i++)
            {
                var now = ShearWallCalculator.DiaphragmAnalysis.Ensure(levels[i], kind);
                if (ReferenceEquals(now, levels[i])) continue;
                changed = true;
                if (ReferenceEquals(Calculator, levels[i])) Calculator = now;
                levels[i] = now;
                if (i < stories.Count)
                {
                    stories[i].Calculator = now;
                    calcByLevel[stories[i].Level.Id] = now;
                }
            }
            if (changed) Log.Info("Shear walls", $"Diaphragm: {ShearWallCalculator.DiaphragmAnalysis.Name(kind)} for every level.");
        }

        /// <summary>Shows and edits story <paramref name="i"/> (0 = the first story).</summary>
        private void ShowLevel(int i)
        {
            if (i < 0 || i >= stories.Count) return;
            Sketch.ShowLevel(stories[i].Level.Id); // ActiveLevelChanged does the rest
        }

        private void LevelButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b && int.TryParse(b.CommandParameter as string, out int n)) ShowLevel(n - 1);
        }

        /// <summary>Copy ↑: puts a copy of every wall (with its design), diaphragm and note of the story below on this one.</summary>
        private void CopyFromBelow_Click(object sender, RoutedEventArgs e)
        {
            if (currentLevel < 1 || currentLevel >= stories.Count) return;
            var from = stories[currentLevel - 1].Level;
            var to = stories[currentLevel].Level;
            int n = 0;
            Sketch.Edit($"Copied {from.Name} onto {to.Name}.", b =>
            {
                n = BuildingEdits.CopyStory(b, from, to);
                ShearWallAdapter.CopyDesignUp(b, from, to);
            });
            ShowStatusMessage(LogLevel.Info, n == 0 ? $"{from.Name} has nothing to copy." : $"Copied {n} walls, diaphragms and notes from {from.Name} to {to.Name}. Ctrl+Z undoes it.");
        }

        private void UpdateLevelButtons()
        {
            if (levelGroup == null) return;
            var buttons = new[] { btnLevel1, btnLevel2, btnLevel3 };
            for (int k = 0; k < buttons.Length; k++)
            {
                buttons[k].Visibility = k < stories.Count ? Visibility.Visible : Visibility.Collapsed;
                buttons[k].Tag = k == currentLevel && !foundationView ? "on" : null;
                // the short name, as in the marks (2 for Level 2: SW2-9); the full name in the tooltip
                buttons[k].Content = k < stories.Count ? MarkLevel(k) : $"{k + 1}";
                buttons[k].ToolTip = k < stories.Count ? $"View {LevelName(k)} at {stories[k].Level.Elevation:0.##} ft: the walls of story {k + 1} and the " +
                                                         (k == stories.Count - 1 ? "roof" : "floor") + " on them. The view only: the Design tab lists what its Floors filter picks." : null;
            }
            btnFoundation.Visibility = stories.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            btnFoundation.Tag = foundationView ? "on" : null;
            btnReactions.Visibility = foundationView ? Visibility.Visible : Visibility.Collapsed; // the foundation's own overlay
            miCopyFromBelow.IsEnabled = currentLevel > 0 && !foundationView;
            btnFloors.ToolTip = $"FLOORS: {stories.Count} in the model. Floors are added, deleted and given their plate heights in the Building Model editor (the building's geometry is edited only there); this opens it on the project.";
            UpdateLevelBanners();
        }

        /// <summary>Every level, roof first, each carrying the walls above it. Replaces a single calculation.</summary>
        private void AnalyzeLevels()
        {
            if (levels.Count == 0) return;
            ShareDesignSettings(Calculator.DesignSettings);
            var beams = Framing().beams; // a wall over a beam is carried
            levelNotes = StoryCascade.Analyze(levels, i => beams.Where(b => b.OnStory == i).Select(b => (b.Start, b.End)),
                i => i >= 0 && i < stories.Count ? stories[i].Level.Name : null); // the model's level names, as on the plans
            if (stories.Count > FloorEdits.MaxFloors)
                levelNotes.Add($"The model has {stories.Count} stories; the wind loads are split over the first {FloorEdits.MaxFloors} only.");
            UpdateLevelBanners();
        }

        /// <summary>Linked wind: each level gets the wind on its strip of the building (mid-story to mid-story).</summary>
        private void SetWindLoadsPerLevel(ASCE7WindLoadCalculator.WindAnalysisResult analysis, bool onlyCases1And3, out string message)
        {
            int n = Math.Max(1, Math.Min(levels.Count, ASCE7WindLoadCalculator.BuildingData.MaxStories));
            var r = ASCE7WindLoadCalculator.StoryWindLoads.Compute(analysis, onlyCases1And3, n);
            for (int i = 0; i < levels.Count; i++)
                levels[i].SetLoadCases(i < r.Levels.Count
                    ? r.Levels[i].LoadCases.Select(c => new LateralLoadCase(c.Name, c.Vx, c.Vy, c.Mt, applyMinimumEccentricity: false))
                    : Enumerable.Empty<LateralLoadCase>());
            var b = r.Building;
            message = $"{b.LoadCases.Count} load cases (from W {b.AlongLength.DesignKips:0.0} k, from S {b.AlongWidth.DesignKips:0.0} k). ";
            if (levels.Count > 1)
                message += $"Split over {levels.Count} floors ({r.StoryHeight:0.#} ft stories), from W / from S: " +
                           string.Join(",  ", r.Levels.Select((l, i) => $"{LevelName(i)} {l.AlongLengthDesignKips:0.0} / {l.AlongWidthDesignKips:0.0} k")) + ". ";
            message += "Updated automatically. Per-wall results: 2 Shear walls, Calculations tab.";
        }

        // ------------------------------------------------------------------ which level is shown

        private readonly List<(TextBlock text, Func<string> say)> levelBanners = new List<(TextBlock, Func<string>)>();

        /// <summary>
        /// A tab with a banner: by default naming the level shown (the Calculations tab: one level's calculation); <paramref name="say"/>
        /// gives a tab its own text (the Design tab: what its Floors filter lists).
        /// </summary>
        private UIElement WithLevelBanner(UIElement content, Func<string> say = null)
        {
            var text = new TextBlock { FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White, TextTrimming = TextTrimming.CharacterEllipsis };
            levelBanners.Add((text, say));
            var banner = new Border { Background = new SolidColorBrush(Color.FromRgb(0x2A, 0x78, 0xD6)), Padding = new Thickness(10, 5, 10, 5), Child = text };
            DockPanel.SetDock(banner, Dock.Top);
            var dock = new DockPanel { LastChildFill = true };
            dock.Children.Add(banner);
            dock.Children.Add(content);
            UpdateLevelBanners();
            return dock;
        }

        /// <summary>"LEVEL 3 · story 2 of 3 · walls and the floor on them · on the floor at 10 ft, plate 9 ft".</summary>
        private string LevelDescription()
        {
            if (stories.Count == 0) return "";
            int n = stories.Count;
            var s = stories[Math.Max(0, Math.Min(currentLevel, n - 1))];
            double floor = s.Level.Elevation - s.PlateHeight;
            string what = currentLevel == n - 1 ? "walls and the roof on them" : "walls and the floor on them";
            return $"{s.Level.Name.ToUpperInvariant()} · story {currentLevel + 1} of {n} · {what} · on {(floor <= 0 ? "the foundation" : $"the floor at {floor:0.##} ft")}, plate {s.PlateHeight:0.##} ft";
        }

        private void UpdateLevelBanners()
        {
            string text = LevelDescription();
            foreach (var (t, say) in levelBanners) t.Text = say?.Invoke() ?? text;
        }
    }
}
