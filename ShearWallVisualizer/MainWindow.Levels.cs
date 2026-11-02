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

        /// <summary>The design method, edition, hold-down list etc. are the project's: every level uses the same object.</summary>
        private void ShareDesignSettings(ShearWallCalculator.Design.DesignSettings settings)
        {
            if (settings == null) return;
            foreach (var l in levels) l.DesignSettings = settings;
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
                buttons[k].Tag = k == currentLevel ? "on" : null;
                buttons[k].Content = k < stories.Count ? stories[k].Level.Name : $"{k + 1}";
                buttons[k].ToolTip = k < stories.Count ? $"Show and edit {LevelName(k)} at {stories[k].Level.Elevation:0.##} ft: the walls of story {k + 1} and the " +
                                                         (k == stories.Count - 1 ? "roof" : "floor") + " on them" : null;
            }
            miCopyFromBelow.IsEnabled = currentLevel > 0;
            btnFloors.ToolTip = $"FLOORS: {stories.Count} in the model. Floors are added, deleted and given their plate heights in the Building Model editor (the building's geometry is edited only there); this opens it on the project.";
            UpdateLevelBanners();
        }

        /// <summary>Every level, roof first, each carrying the walls above it. Replaces a single calculation.</summary>
        private void AnalyzeLevels()
        {
            if (levels.Count == 0) return;
            ShareDesignSettings(Calculator.DesignSettings);
            levelNotes = StoryCascade.Analyze(levels);
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
            foreach (var t in levelBanners) t.Text = text;
        }
    }
}
