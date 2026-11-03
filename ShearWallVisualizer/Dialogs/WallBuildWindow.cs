using calculator;
using ShearWallCalculator;
using ShearWallCalculator.Design;
using ShearWallCalculator.NDSProvisions;
using ShearWallVisualizer.Controls;
using NDS_WoodLibrary.NDS2018;
using WoodFraming;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace ShearWallVisualizer.Dialogs
{
    /// <summary>A hold-down (or a beam end or column base) of the story above standing on this wall.</summary>
    public sealed class LoadFromAbove
    {
        public HoldownLanding Landing { get; set; }
        /// <summary>What it comes from: "SW3-2", "beam BM2".</summary>
        public string From { get; set; }
        /// <summary>in, along this wall from its low end.</summary>
        public double Along { get; set; }
    }

    /// <summary>What the wall's construction window shows: the wall, its design, its hold-downs and what lands on it.</summary>
    public sealed class WallBuildData
    {
        public string Name { get; set; }
        public string LevelName { get; set; }
        public int Story { get; set; }
        public int Stories { get; set; }
        public WallData Wall { get; set; }
        public WallDesignResult Result { get; set; }
        /// <summary>Its own two hold-downs as traced: what each anchors to.</summary>
        public List<HoldownLanding> Own { get; set; } = new List<HoldownLanding>();
        public List<LoadFromAbove> Above { get; set; } = new List<LoadFromAbove>();
        public List<CalcLine> Lines { get; set; } = new List<CalcLine>();
        public string Footnote { get; set; }
        public bool FirstStory => Story == 0;
        public bool EastWest => Wall?.WallDir == WallDirs.EastWest;

        /// <summary>A plan point's distance (in) along the wall from its low end (west or south).</summary>
        public double AlongIn(Point p)
        {
            if (Wall == null) return 0;
            return EastWest ? (p.X - Math.Min(Wall.Start.X, Wall.End.X)) * 12 : (p.Y - Math.Min(Wall.Start.Y, Wall.End.Y)) * 12;
        }

        /// <summary>The design wind speed (ultimate, mph) and exposure ("B", "C", "D") when the loads come from the wind model.</summary>
        public double? WindMph { get; set; }
        public string Exposure { get; set; }

        /// <summary>The building model's wall and the level on top of its story (the key of its framing there).</summary>
        public string ModelWallId { get; set; }
        public string TopLevelId { get; set; }

        /// <summary>The wall's stud size and spacing (in) from the building model: its own, else the project's for the story.</summary>
        public string StudSize { get; set; }
        public double StudSpacing { get; set; }
        /// <summary>The project's stud size for this story (the model's defaults), what "default" in the picker means.</summary>
        public string DefaultStudSize { get; set; }
        /// <summary>The wall has a size of its own (not the project's default).</summary>
        public bool OwnStudSize { get; set; }

        /// <summary>On the building's outside edge (the building model's): wind on its studs, else the interior 5 psf.</summary>
        public bool Exterior { get; set; }
        /// <summary>The C&amp;C wind pressure on a stud (psf, strength) and how it was found; null with manual loads.</summary>
        public double? StudWindPsf { get; set; }
        public string StudWindNote { get; set; }
        /// <summary>A function of the stud's length and spacing (in) giving its wind (psf, note); null with manual loads.</summary>
        public Func<double, double, (double psf, string note)?> StudWind { get; set; }
        /// <summary>The gravity loads on the wall's top (the building model's, plf); null: none entered.</summary>
        public BuildingModel.Model.WallTopLoad TopLoad { get; set; }
        /// <summary>The project's stud lumber and wall deflection limit (the building model's defaults).</summary>
        public Species StudSpecies { get; set; }
        public Grade StudGrade { get; set; }
        public double DeflectionLimit { get; set; }

        /// <summary>
        /// The NDS design of the wall's studs (NDS-2018 3.9.2), with the top loads and the wind on them; null for an
        /// exterior wall without the wind model (no pressure to design for).
        /// </summary>
        public StudDesign NdsStuds(WallFraming f)
        {
            if (f == null || !WallFraming.IsStudSize(f.StudSize)) return null;
            double len = f.StudTop - WallFraming.Ply;
            if (!(len > 0)) return null;
            double? psf = null;
            if (Exterior)
            {
                var w = StudWind?.Invoke(len, f.Spacing);
                if (w == null) return null;
                psf = w.Value.psf;
                StudWindNote = w.Value.note;
            }
            StudWindPsf = psf;
            var t = TopLoad;
            return StudDesign.Check(new StudDesignInput
            {
                Species = StudSpecies, Grade = StudGrade, Size = f.StudSize, SpacingIn = f.Spacing, LengthIn = len,
                Loads = new WallTopLoads { Dead = t?.Dead ?? 0, Live = t?.Live ?? 0, RoofLive = t?.RoofLive ?? 0, Snow = t?.Snow ?? 0 },
                WindPressurePsf = psf, WindNote = StudWindNote, DeflectionLimit = DeflectionLimit,
            });
        }

        /// <summary>The code check of the wall's studs: carrying the floors above it and the roof, studs the wall's height less the plates.</summary>
        public StudCheck Studs(WallFraming f) => StudsOf(f.StudSize, f);

        /// <summary>The code check as if the studs were <paramref name="size"/> (the same spacing and length as <paramref name="f"/>).</summary>
        public StudCheck StudsOf(string size, WallFraming f) =>
            StudRules.Check(size, f.Spacing, f.StudTop / 12.0 - WallFraming.Ply / 12.0, Math.Max(0, Stories - 1 - Story), WindMph, Exposure, WallFraming.StudSizes);

        public WallFraming Framing() =>
            WallFraming.Build(Wall?.WallLength ?? 0, Wall?.WallHeight ?? 0, StudSize, StudSpacing, WallSchedule.PostAlongWall(Result?.Holdown),
                              Above.Select(a => a.Along));

        /// <summary>The NDS species for the calculator's species group (the lower G of the pair, as before: DF-L, SPF).</summary>
        public static Species NdsSpecies(WoodTypes w) => w == WoodTypes.WOODTYPE_SPF_HF ? Species.SprucePineFir : Species.DouglasFirLarch;

        /// <summary>
        /// A first-story wall's sill anchor bolts (null above the first story or before a design): for the ASD wind shear
        /// 0.6 V / L, the wall's nominal capacity, aspect ratio and whether its hold-downs count on dead load (the washer).
        /// </summary>
        public SillAnchorage Anchors()
        {
            var r = Result;
            if (!FirstStory || r == null || r.Length <= 0) return null;
            double vAsd = 0.6 * r.V * 1000.0 / r.Length;
            double nominal = r.CombinedNominal > 0 ? r.CombinedNominal : r.Nominal;
            return SillAnchorage.Design(r.Length, vAsd, NdsSpecies(r.Inputs?.Species ?? WoodTypes.WOODTYPE_DF_SP), nominal, r.AspectRatio, r.DeadLoadLb > 0);
        }

        /// <summary>The hold-down at the low (left) or high (right) end, as traced.</summary>
        public HoldownLanding OwnAt(bool low) =>
            Own.FirstOrDefault(l => (DetailCalcs.EndName(Wall, l.At) == (EastWest ? "West" : "South")) == low);
    }


    /// <summary>
    /// The selected wall, built: its framing and sheathing in elevation with the dimensions, the hold-downs at its bottom and
    /// the connections coming down onto its top, the same in 3D with either face of sheathing cut away, every piece it takes
    /// (framing, sheets, nails, hold-downs, connections) and its calculation. It follows the selection and the design.
    /// The framing is laid out by <see cref="WallFraming"/> (16 in. on centre, 2x6 on the first story, 2x4 above, 4 × 8 sheets).
    /// </summary>
    public sealed class WallBuildWindow : Window
    {
        private readonly TextBlock title = new TextBlock { FontSize = 18, FontWeight = FontWeights.SemiBold };
        private readonly TextBlock subtitle = new TextBlock { Foreground = Ui.InkSecondary, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 6) };
        private readonly TabControl tabs = new TabControl();
        private readonly Grid elevationHost = new Grid();
        private readonly Framing3DView view3D = new Framing3DView();
        private readonly ScrollViewer pieces = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        private readonly ScrollViewer calc = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        private readonly ComboBox face = new ComboBox { Width = 170, Margin = new Thickness(0, 0, 12, 0) };
        private readonly CheckBox nails = new CheckBox { Content = "Nails", IsChecked = true, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 12, 0) };
        private readonly CheckBox face1In3D = new CheckBox { Content = "Face 1 sheathing", IsChecked = true, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 12, 0) };
        private readonly CheckBox face2In3D = new CheckBox { Content = "Face 2 sheathing", IsChecked = true, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 12, 0) };
        private readonly CheckBox cutaway = new CheckBox { Content = "Cut away the right half", IsChecked = true, VerticalAlignment = VerticalAlignment.Center };
        private readonly CheckBox follow = new CheckBox { Content = "Open when a wall, beam or column is picked", VerticalAlignment = VerticalAlignment.Center, Foreground = Ui.InkSecondary };
        private WallBuildData data;
        /// <summary>The wall shown (null for a beam or column, or nothing).</summary>
        public WallBuildData Data => data;
        private WallFraming framing;
        private readonly StackPanel elevBar, bar3, studBar;
        private readonly ComboBox studs = new ComboBox { MinWidth = 150 };
        private void NameStuds() => System.Windows.Automation.AutomationProperties.SetName(studs, "Studs");
        private bool fillingStuds;

        /// <summary>The engineer picked a stud size for the shown wall (null: the story's default). The host saves it.</summary>
        public event Action<string> StudSizeChosen;

        /// <summary>Ctrl+Z (false) or Ctrl+Y (true) pressed here: the host undoes or redoes the model.</summary>
        public event Action<bool> UndoRedo;

        /// <summary>The engineer entered the loads on the shown wall's top (plf). The host saves them in the building model.</summary>
        public event Action<BuildingModel.Model.WallTopLoad> TopLoadChanged;

        /// <summary>Open this window whenever a wall is picked (the host reads it).</summary>
        public bool OpenOnPick { get => follow.IsChecked == true; set => follow.IsChecked = value; }

        public WallBuildWindow()
        {
            Title = "Wall";
            Width = 1180;
            Height = 860;
            BuildingModel.Sketch.WindowFit.Fit(this);
            FontFamily = new FontFamily("Segoe UI");
            FontSize = 12;
            Background = Brushes.White;
            ShowInTaskbar = false;
            ShowActivated = false; // picking a wall on the plan keeps the plan active
            // Esc closes it and goes back to the calculator (an open drop-down takes its own Esc first: KeyDown bubbles)
            KeyDown += (s, e) =>
            {
                // Ctrl+Z / Ctrl+Y (Ctrl+Shift+Z) here are the model's undo and redo, as on the plan
                var mods = System.Windows.Input.Keyboard.Modifiers;
                if (!e.Handled && (mods & System.Windows.Input.ModifierKeys.Control) != 0 && (e.Key == System.Windows.Input.Key.Z || e.Key == System.Windows.Input.Key.Y))
                {
                    e.Handled = true;
                    UndoRedo?.Invoke(e.Key == System.Windows.Input.Key.Y || (mods & System.Windows.Input.ModifierKeys.Shift) != 0);
                    return;
                }
                if (e.Key != System.Windows.Input.Key.Escape || e.Handled) return;
                e.Handled = true;
                var owner = Owner;
                Close();
                owner?.Activate();
            };

            var root = new DockPanel { Margin = new Thickness(14, 10, 14, 12) };
            var head = new DockPanel();
            // the wall's stud size: the story's default, or larger (the engineer's choice, saved with the wall's design)
            studs.SelectionChanged += (s, e) =>
            {
                if (fillingStuds || data?.Wall == null || !(studs.SelectedItem is ComboBoxItem it)) return;
                StudSizeChosen?.Invoke(it.Tag as string);
            };
            studBar = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 6, 0, 0) };
            studBar.Children.Add(new TextBlock { Text = "Studs ", VerticalAlignment = VerticalAlignment.Center });
            NameStuds();
            studBar.Children.Add(studs);
            var right = new StackPanel { Children = { follow, studBar } };
            DockPanel.SetDock(right, Dock.Right);
            head.Children.Add(right);
            head.Children.Add(new StackPanel { Children = { title, subtitle } });
            DockPanel.SetDock(head, Dock.Top);
            root.Children.Add(head);

            // Elevation: which face of sheathing, and the nails
            foreach (var s in new[] { "Face 1 sheathing", "Face 2 sheathing", "Framing only" }) face.Items.Add(s);
            face.SelectedIndex = 0;
            face.SelectionChanged += (s, e) => ShowElevation();
            nails.Checked += (s, e) => ShowElevation();
            nails.Unchecked += (s, e) => ShowElevation();
            elevBar = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 6) };
            elevBar.Children.Add(new TextBlock { Text = "Show ", VerticalAlignment = VerticalAlignment.Center });
            elevBar.Children.Add(face);
            elevBar.Children.Add(nails);
            elevBar.Children.Add(new TextBlock { Text = "Elevation to scale, the low end (west or south) on the left, seen from the face 1 side.", Foreground = Ui.InkMuted, VerticalAlignment = VerticalAlignment.Center });
            var elevPage = new DockPanel();
            DockPanel.SetDock(elevBar, Dock.Top);
            elevPage.Children.Add(elevBar);
            elevPage.Children.Add(elevationHost);

            bar3 = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 6) };
            foreach (var cb in new[] { face1In3D, face2In3D, cutaway })
            {
                cb.Checked += (s, e) => Show3D();
                cb.Unchecked += (s, e) => Show3D();
                bar3.Children.Add(cb);
            }
            var page3 = new DockPanel();
            DockPanel.SetDock(bar3, Dock.Top);
            page3.Children.Add(bar3);
            page3.Children.Add(new Border { Child = view3D, BorderBrush = Ui.Hairline, BorderThickness = new Thickness(1) });

            tabs.Items.Add(new TabItem { Header = "Elevation", Content = elevPage });
            tabs.Items.Add(new TabItem { Header = "3D", Content = page3 });
            tabs.Items.Add(new TabItem { Header = "Pieces", Content = pieces });
            tabs.Items.Add(new TabItem { Header = "Calculation", Content = calc });
            root.Children.Add(tabs);
            Content = root;
        }

        /// <summary>The stud choices for the shown wall: the story's default, then each size not smaller than it.</summary>
        private void FillStuds(WallBuildData d)
        {
            fillingStuds = true;
            try
            {
                studs.Items.Clear();
                studBar.Visibility = d?.Wall == null ? Visibility.Collapsed : Visibility.Visible;
                if (d?.Wall == null) return;
                // the project's size for the story (the building model's defaults)
                string def = d.DefaultStudSize;
                string chosen = d.OwnStudSize ? d.StudSize : null;
                // the code's smallest here (IRC Table R602.3(5)); sizes under it and under the default are not offered
                var check = framing == null || !WallFraming.IsStudSize(def) ? null : d.StudsOf(def, framing);
                bool defFails = check != null && !check.DesignRequired && !check.Passes;
                studs.Items.Add(new ComboBoxItem
                {
                    Content = $"{def} (project default, {(d.FirstStory ? "first story" : "upper story")}){(defFails ? $": under the code's {check.Minimum}" : "")}",
                    Tag = null, Foreground = defFails ? Ui.FailInk : Ui.Ink,
                });
                int min = Math.Max(Array.IndexOf(WallFraming.StudSizes, def), check?.Minimum == null ? 0 : Array.IndexOf(WallFraming.StudSizes, check.Minimum));
                foreach (var s in WallFraming.StudSizes.Skip(Math.Max(0, min)))
                    studs.Items.Add(new ComboBoxItem { Content = s, Tag = s });
                studs.SelectedItem = studs.Items.Cast<ComboBoxItem>().FirstOrDefault(i => (i.Tag as string) == chosen) ?? studs.Items[0];
                studs.ToolTip = "The wall's studs, plates and posts; the plates are as wide as the studs. Saved with the wall in the building model (Ctrl+Z undoes it).";
            }
            finally { fillingStuds = false; }
        }

        /// <summary>Pick a tab: 0 Elevation, 1 3D, 2 Pieces, 3 Calculation.</summary>
        public void ShowTab(int i) { if (i >= 0 && i < tabs.Items.Count) tabs.SelectedIndex = i; }

        /// <summary>A beam or a column: its own elevation, 3D model, pieces and calculation (the wall's toolbars hidden).</summary>
        public void ShowMember(MemberView v)
        {
            data = null;
            framing = null;
            elevBar.Visibility = bar3.Visibility = studBar.Visibility = Visibility.Collapsed;
            elevationHost.Children.Clear();
            if (v == null)
            {
                Title = "Member";
                title.Text = "Pick a wall, beam or column on the plan";
                subtitle.Text = "";
                pieces.Content = null;
                calc.Content = null;
                view3D.ShowModel(new Model3DGroup(), null);
                return;
            }
            Title = v.WindowTitle;
            title.Text = v.Title;
            subtitle.Text = v.Subtitle;
            var e = v.Elevation?.Invoke();
            if (e != null) elevationHost.Children.Add(e);
            view3D.ShowModel(v.Model?.Invoke() ?? new Model3DGroup(), v.Key);
            pieces.Content = v.Pieces?.Invoke();
            var cp = new StackPanel { Margin = new Thickness(4) };
            cp.Children.Add(CalcTable.Build(v.Lines ?? new List<CalcLine>()));
            if (!string.IsNullOrEmpty(v.Footnote)) cp.Children.Add(new TextBlock { Text = v.Footnote, Foreground = Ui.InkMuted, FontSize = 11, Margin = new Thickness(0, 10, 0, 0), TextWrapping = TextWrapping.Wrap });
            calc.Content = cp;
        }

        public void Show(WallBuildData d)
        {
            elevBar.Visibility = bar3.Visibility = Visibility.Visible;
            data = d;
            framing = d?.Wall == null ? null : d.Framing();
            FillStuds(d);
            if (d?.Wall == null)
            {
                Title = "Wall";
                title.Text = "Pick a wall on the plan";
                subtitle.Text = "";
                elevationHost.Children.Clear();
                pieces.Content = null;
                calc.Content = null;
                view3D.Show(null, null, false, false, false);
                return;
            }
            var w = d.Wall;
            var r = d.Result;
            Title = $"Wall {d.Name}: elevation, 3D and pieces";
            title.Text = $"{d.Name} · {d.LevelName}";
            subtitle.Text = $"{(d.EastWest ? "East-west" : "North-south")} wall, {FtIn.Of(framing.Length)} long × {FtIn.Of(framing.Height)} high (story {d.Story + 1} of {d.Stories}). " +
                            (r == null ? "Not designed yet (no shear on it)." :
                             $"{r.Status}: v = {r.Demand:0} plf of {r.Capacity:0} plf" + (r.HoldownNeeded ? $", hold-downs {r.Holdown?.Model ?? "none strong enough"} for T = {r.TensionAsd:N0} lb (ASD)." : ", no uplift."));
            var sc = d.Studs(framing);
            string nds = "";
            if (sc.DesignRequired)
            {
                StudDesign sd = null;
                try { sd = d.NdsStuds(framing); } catch (Exception) { }
                nds = sd == null ? "outside the code's stud table, not designed (see Pieces)."
                    : sd.Passes ? $"outside the code's stud table; NDS 3.9 OK, {sd.Governing?.Ratio:0.00} (see Pieces)."
                    : "outside the code's stud table; NDS 3.9 fails: " + Worst(sd) + " (see Pieces)";
            }
            subtitle.Text += $" Studs {framing.StudSize}: " + (sc.DesignRequired ? nds : sc.Passes ? "meet the code's stud table." : $"under the code's stud table, use {sc.Minimum}.");
            int faces = Faces().Count;
            face2In3D.IsEnabled = faces > 1;
            ShowElevation();
            Show3D();
            pieces.Content = Pieces();
            var cp = new StackPanel { Margin = new Thickness(4) };
            cp.Children.Add(CalcTable.Build(d.Lines ?? new List<CalcLine>()));
            if (!string.IsNullOrEmpty(d.Footnote)) cp.Children.Add(new TextBlock { Text = d.Footnote, Foreground = Ui.InkMuted, FontSize = 11, Margin = new Thickness(0, 10, 0, 0), TextWrapping = TextWrapping.Wrap });
            calc.Content = cp;
        }

        /// <summary>The wall's sheathed faces (one or two), as designed.</summary>
        private List<FaceResult> Faces() => data?.Result?.Faces?.Where(f => f?.Face != null && f.Face.Table != SheathingTable.None).ToList() ?? new List<FaceResult>();

        private void ShowElevation()
        {
            elevationHost.Children.Clear();
            if (data?.Wall == null || framing == null) return;
            var faces = Faces();
            int pick = face.SelectedIndex;
            FaceResult shown = pick == 0 && faces.Count > 0 ? faces[0] : pick == 1 && faces.Count > 1 ? faces[1] : null;
            elevationHost.Children.Add(new FramingElevation(data, framing, shown, pick == 1 && faces.Count < 2 ? "This wall has one sheathed face." : null, nails.IsChecked == true));
        }

        private void Show3D()
        {
            if (data?.Wall == null || framing == null) { view3D.Show(null, null, false, false, false); return; }
            var faces = Faces();
            view3D.Show(data, framing, face1In3D.IsChecked == true && faces.Count > 0, face2In3D.IsChecked == true && faces.Count > 1, cutaway.IsChecked == true, faces);
        }

        // ---------------------------------------------------------------- the pieces

        private FrameworkElement Pieces()
        {
            var p = new StackPanel { Margin = new Thickness(4, 4, 16, 16) };
            var f = framing;
            var r = data.Result;
            string sz = f.StudSize;

            Section(p, "Framing", $"Studs and plates {sz} ({(data.FirstStory ? "first story" : "above the first story")}); the plates are laid flat, as wide as the studs ({FtIn.Inches(f.StudDepth)} in).");
            var rows = new List<string[]>
            {
                new[] { "Sill plate (flat)", sz, "1", FtIn.Of(f.Length), data.FirstStory ? "On the foundation: preservative-treated; anchor bolts below (Sill anchors)." :"On the floor sheathing of the story below." },
                new[] { "Top plates (flat, double)", sz, "2", FtIn.Of(f.Length), "Splices in the two plies 4 ft apart at least." },
            };
            var studs = f.Pieces.Where(x => x.Kind == FramingKind.Stud).ToList();
            if (studs.Count > 0) rows.Add(new[] { $"Studs @ {f.Spacing:0}\" o.c.", sz, studs.Count.ToString(), FtIn.Of(studs[0].Length), "Laid out from the low (left) end." });
            rows.Add(new[] { "Hold-down posts, at the ends", $"{f.PostPlies}-{sz}", "2", FtIn.Of(f.StudTop - WallFraming.Ply),
                             $"Built up of {f.PostPlies} plies ({FtIn.Inches(f.PostPlies * WallFraming.Ply)} in along the wall)" +
                             (r?.Holdown?.WoodMinHeight is double mh && r.Holdown.WoodMinWidth is double mw ? $"; the hold-down needs {FtIn.Inches(mh)} × {FtIn.Inches(mw)} in at least." : ".") });
            int under = f.Pieces.Count(x => x.Kind == FramingKind.PostUnder) / 2;
            if (under > 0) rows.Add(new[] { "Posts under hold-downs from above", $"2-{sz}", under.ToString(), FtIn.Of(f.StudTop - WallFraming.Ply), "Under each hold-down of the story above that lands along the wall (see Connections at the top)." });
            var blocks = f.Pieces.Where(x => x.Kind == FramingKind.Blocking).ToList();
            if (blocks.Count > 0) rows.Add(new[] { "Blocking at the horizontal sheet joint", sz, blocks.Count.ToString(), $"{FtIn.Of(blocks.Sum(b => b.Length))} total", $"At {FtIn.Of(WallFraming.SheetHeight)} up: every sheet edge is blocked (the capacities are for blocked walls)." });
            p.Children.Add(Table(new[] { "Piece", "Size", "Qty", "Length (each)", "Notes" }, rows));
            var studCheck = data.Studs(f);
            StudSection(p, studCheck, f, Faces());
            if (studCheck.DesignRequired) NdsStudSection(p, f);

            var faces = Faces();
            for (int i = 0; i < faces.Count; i++)
            {
                var fr = faces[i];
                var fs = fr.Face;
                var o = fr.Option;
                Section(p, $"Sheathing, face {i + 1}", o?.Description ?? "");
                var groups = f.Sheets.GroupBy(s => (Math.Round(s.Width, 2), Math.Round(s.Height, 2))).OrderByDescending(g => g.Key.Item1 * g.Key.Item2);
                var srows = groups.Select(g => new[] { g.First().Full ? "Whole sheet" : "Cut piece", $"{FtIn.Of(g.Key.Item1)} × {FtIn.Of(g.Key.Item2)}", g.Count().ToString(),
                                                         g.First().Full ? "Long side up (vertical)." : "Cut from a 4 × 8 sheet." }).ToList();
                srows.Add(new[] { "Sheets to buy (4 × 8)", "", f.SheetsToBuy().ToString(), "" });
                p.Children.Add(Table(new[] { "Piece", "Size", "Qty", "Notes" }, srows));

                var nrows = new List<string[]>();
                if (o != null && o.UsesEdgeSpacing)
                {
                    var (ne, nf) = f.NailCount(fs.EdgeSpacing);
                    string nail = $"{ShearWallDesigner.NailName(fs.Nail)} common ({NailSize(fs.Nail)})";
                    nrows.Add(new[] { "Edge nailing", nail, $"{fs.EdgeSpacing}\" o.c.", $"≈ {ne:N0}", "Round every sheet's edges, 3/8\" in from the edge, into the studs, posts, plates and blocking." });
                    nrows.Add(new[] { "Field nailing", nail, "12\" o.c.", $"≈ {nf:N0}", "Along the studs inside each sheet (SDPWS 4.3.6.3)." });
                }
                else
                {
                    nrows.Add(new[] { "Fasteners", o?.Fasteners ?? "", o?.RowSpacing ?? "", "", "As the Table 4.3C row (its spacing, studs and blocking)." });
                }
                p.Children.Add(Table(new[] { "Nailing", "Nail", "Spacing", "Count", "Notes" }, nrows));
            }
            if (faces.Count == 0) Section(p, "Sheathing", "Not designed yet.");

            Section(p, "Hold-downs at the bottom", r == null ? "Not designed yet." : !r.HoldownNeeded ? "No uplift: none needed." :
                    $"One at each end (wind reverses), on the end post. Required T = {r.TensionAsd:N0} lb (ASD, 0.6W)" + (r.TensionFromAboveAsd > 0 ? $", of which {r.TensionFromAboveAsd:N0} lb comes from the story above." : "."));
            if (r != null && r.HoldownNeeded)
            {
                var hrows = new List<string[]>();
                foreach (bool low in new[] { true, false })
                {
                    var l = data.OwnAt(low);
                    string end = low ? (data.EastWest ? "West (left)" : "South (left)") : (data.EastWest ? "East (right)" : "North (right)");
                    string model = l?.Model ?? r.Holdown?.Model ?? "none strong enough";
                    string fasteners = Fast(l?.Choice?.Fasteners ?? r.Holdown?.Fasteners);
                    double allow = l?.Choice?.Allowable ?? r.Holdown?.Allowable ?? 0;
                    string to = l == null ? (data.FirstStory ? "the foundation" : "") : DetailCalcs.Support(l);
                    hrows.Add(new[] { end, model, fasteners, allow > 0 ? $"{allow:N0}" : "", $"{(l?.TensionAsd ?? r.TensionAsd):N0}", to, Anchor(l) });
                }
                p.Children.Add(Table(new[] { "End", "Hold-down", "Fasteners into the post", "Allowable (lb)", "T (lb)", "Anchors to", "Anchor" }, hrows));
            }

            var an = data.Anchors();
            if (an != null) SillAnchorSection(p, an, f);

            Section(p, "Connections at the top (from the story above)", data.Above.Count == 0 ? "Nothing from above stands on this wall." :
                    "Each hold-down above pulls up on this wall's top at its post: it is carried by the post under it to this wall's bottom.");
            if (data.Above.Count > 0)
            {
                var arows = data.Above.OrderBy(a => a.Along).Select(a => new[]
                {
                    FtIn.Of(a.Along), a.From, a.Landing.Model ?? "", $"{a.Landing.TensionAsd:N0}",
                    a.Along < f.PostPlies * WallFraming.Ply + WallFraming.Ply || a.Along > f.Length - f.PostPlies * WallFraming.Ply - WallFraming.Ply
                        ? "Over this wall's end post: it carries it down." : $"Over a 2-{sz} post added under it.",
                }).ToList();
                p.Children.Add(Table(new[] { "From the low end", "From", "Its hold-down", "Uplift (lb, ASD)", "Under it" }, arows));
            }

            if (r != null)
            {
                Section(p, "Design", "");
                p.Children.Add(Table(new[] { "Check", "Value" }, new List<string[]>
                {
                    new[] { "Shear", $"v = {r.Demand:0} plf, capacity {r.Capacity:0} plf ({r.ShearRatio:0.00})" },
                    new[] { "Aspect ratio h/b", $"{r.AspectRatio:0.00} (limit {r.MaxAspectRatio:0.0})" },
                    new[] { "Governing case", r.GoverningCase ?? "" },
                    new[] { "Hold-down arm d", $"{r.HoldownArm:0.##} ft" },
                }));
            }

            Section(p, "Assumed (confirm)", "");
            foreach (var s in new[]
            {
                $"Studs {f.StudSize} at {f.Spacing:0}\" o.c. (the building model's: the wall's own, else the project's defaults); plates flat, as wide as the studs.",
                "4 × 8 sheets long side up from the low end, so the vertical joints fall on studs; a wall over 8 ft blocked at the horizontal joint (or use 9 or 10 ft sheets).",
                "Field nailing 12\" o.c.; edge nails 3/8\" from the sheet edges; nails into the framing behind every edge.",
                data.FirstStory ? "Sill anchors: dry, normal-temperature service (CM = Ct = 1), full NDS end and edge distances (CΔ = 1), f'c ≥ 2,500 psi; the hold-down anchor bolts and the foundation are not designed here."
                                : "The sill's nailing to the floor below and the hold-down anchorage are not designed here.",
            })
                p.Children.Add(new TextBlock { Text = "•  " + s, TextWrapping = TextWrapping.Wrap, Foreground = Ui.InkSecondary, Margin = new Thickness(6, 1, 0, 1) });
            return p;
        }

        /// <summary>The studs against the code's table (IRC R602.3(5) / IBC 2308.9.1) and SDPWS's shear wall framing, with the quoted text.</summary>
        private static void StudSection(Panel p, StudCheck c, WallFraming f, List<FaceResult> faces)
        {
            string head = c.DesignRequired ? $"Design required: {c.Size} studs are outside the prescriptive table here."
                        : c.Passes ? $"{c.Size} at {f.Spacing:0}\" o.c. meets the table (smallest allowed here: {c.Minimum})."
                        : $"{c.Size} at {f.Spacing:0}\" o.c. does not meet the table: use {c.Minimum ?? "a designed stud"} (Studs, top right).";
            Section(p, "Studs: code check", head);
            var rows = new List<string[]>
            {
                new[] { "Stud length (unsupported)", FtIn.Of(c.StudHeightFt * 12), $"Table limit for bearing walls {StudRules.TableHeightFt:0} ft (note a: points of lateral support; sheathed on one side at least)." },
                new[] { "Carries", c.Carries, "The stories above this one (the roof on top), as a bearing wall." },
                new[] { "Spacing", $"{f.Spacing:0}\" o.c.", c.MaxSpacingIn is double m ? (m == 0 ? $"{c.Size}: not permitted for this load" : $"{c.Size}: up to {m:0}\" o.c.") : "outside the table" },
            };
            foreach (var r in c.Reasons) rows.Add(new[] { "", "", r });
            foreach (var fr in faces)
            {
                var fs = fr.Face;
                if (fs == null || fr.Option == null || !fr.Option.UsesEdgeSpacing) continue;
                bool ten = fs.Nail == CommonNailSizes.NAILSIZE_10D;
                if (ShearWallFraming.WideFaceAtPanelJoints(fs.EdgeSpacing, ten))
                {
                    rows.Add(new[] { "Panel joints", "3x (or 2-2x fastened)", $"SDPWS 4.3.7.1 item 5: {ShearWallDesigner.NailName(fs.Nail)} at {fs.EdgeSpacing}\" o.c. needs a 3\" nominal nailed face at adjoining panel edges, nailing staggered; two 2x fastened together per 4.3.6.1.1 may be used." });
                    break;
                }
            }
            if (c.SpacingIn > ShearWallFraming.MaxStudSpacingIn + 1e-9)
                rows.Add(new[] { "Shear wall studs", $"{c.SpacingIn:0}\" o.c.", $"Over SDPWS 4.3.7.1 item 6's {ShearWallFraming.MaxStudSpacingIn:0}\" on centre." });
            p.Children.Add(Table(new[] { "Item", "Value", "Notes" }, rows));
            var src = new Expander { Header = "Sources (quoted)", IsExpanded = false, Margin = new Thickness(0, 4, 0, 0) };
            var sp = new StackPanel();
            foreach (var s in StudRules.Citations.Concat(new[] { ShearWallFraming.Citation }))
                sp.Children.Add(new TextBlock { Text = "•  " + s, TextWrapping = TextWrapping.Wrap, Foreground = Ui.InkSecondary, FontSize = 11.5, Margin = new Thickness(6, 2, 0, 2) });
            sp.Children.Add(new TextBlock { Text = "Text from the 2025 California Residential and Building Codes (the 2024 IRC and IBC); the stud table cells are the same in the 2018 and 2021 editions (not checked against those texts here). Where the table does not apply, the studs are designed per NDS 3.9 below.", TextWrapping = TextWrapping.Wrap, Foreground = Ui.InkMuted, FontSize = 11, Margin = new Thickness(6, 4, 0, 2) });
            src.Content = sp;
            p.Children.Add(src);
        }

        /// <summary>
        /// The studs designed per NDS-2018 3.9.2 (outside the prescriptive table): the loads on the wall's top (entered
        /// here, saved in the building model), the wind on the studs, each IBC 1605.2 combination, and the other checks.
        /// </summary>
        private void NdsStudSection(Panel p, WallFraming f)
        {
            StudDesign d;
            try { d = data.NdsStuds(f); }
            catch (Exception ex) { Section(p, "Studs: NDS design", "Not designed: " + ex.Message); return; }
            string lumber = $"{SpeciesInfo.Abbreviation(data.StudSpecies)} {SpeciesInfo.Name(data.StudGrade)}";
            if (d == null)
            {
                Section(p, "Studs: NDS design", data.Exterior
                    ? "An exterior wall: the studs need the wind's components and cladding pressure, which comes from the wind model. Use the wind loads (manual loads have no pressure on the studs)."
                    : "Not designed.");
                return;
            }
            var g = d.Governing;
            bool loads = data.TopLoad != null && !data.TopLoad.IsEmpty;
            string verdict = !d.SlendernessOk ? $"{f.StudSize} is too slender (le/d = {d.Slenderness:0.0} > 50): use a deeper stud (Studs, top right)."
                           : d.Passes ? $"{lumber} {f.StudSize} at {f.Spacing:0}\" o.c. works: {g?.Name} governs at {g?.Ratio:0.00}."
                           : $"{lumber} {f.StudSize} at {f.Spacing:0}\" o.c. does not work: " + Worst(d) + " Use a deeper stud or closer spacing.";
            Section(p, "Studs: NDS design", verdict + (loads ? "" : " No loads on the wall's top entered yet: wind (or 5 psf) and the studs' own capacity only."));
            if (!d.Passes) ((TextBlock)p.Children[p.Children.Count - 1]).Foreground = Ui.FailInk;

            // the loads on the wall's top, entered here, saved in the model (the gravity load path will fill them in later)
            var bar = new WrapPanel { Margin = new Thickness(0, 4, 0, 6) };
            bar.Children.Add(new TextBlock { Text = "Loads on the wall's top (plf, service):", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0), Foreground = Ui.InkSecondary });
            var t = data.TopLoad;
            var boxes = new List<TextBox>();
            foreach (var (label, v) in new[] { ("D", t?.Dead ?? 0), ("L", t?.Live ?? 0), ("Lr", t?.RoofLive ?? 0), ("S", t?.Snow ?? 0) })
            {
                bar.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0, 4, 0) });
                var box = new TextBox { Text = v > 0 ? v.ToString("0.##", CultureInfo.InvariantCulture) : "", Width = 60, VerticalAlignment = VerticalAlignment.Center };
                System.Windows.Automation.AutomationProperties.SetName(box, "Top load " + label);
                boxes.Add(box);
                bar.Children.Add(box);
            }
            void Apply()
            {
                double Num(TextBox b) => double.TryParse(b.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var x) && x > 0 ? x : 0;
                var next = new BuildingModel.Model.WallTopLoad { Dead = Num(boxes[0]), Live = Num(boxes[1]), RoofLive = Num(boxes[2]), Snow = Num(boxes[3]) };
                if (next.SameAs(data.TopLoad ?? new BuildingModel.Model.WallTopLoad())) return;
                TopLoadChanged?.Invoke(next);
            }
            foreach (var b in boxes)
            {
                b.LostFocus += (o, e) => Apply();
                b.KeyDown += (o, e) => { if (e.Key == System.Windows.Input.Key.Enter) { Apply(); e.Handled = true; } };
            }
            bar.Children.Add(new TextBlock { Text = "Enter or leave a box to apply (saved with the wall in the building model; Ctrl+Z undoes it).", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 0, 0), Foreground = Ui.InkMuted, FontSize = 11 });
            p.Children.Add(bar);

            var s = d.Section;
            var info = new List<string[]>
            {
                new[] { "Stud", $"{lumber} {f.StudSize} ({FtIn.Inches(s.B)} × {FtIn.Inches(s.D)}): A {s.A:0.00} in², S {s.Sx:0.00} in³, I {s.Ix:0.00} in⁴", "NDS Supplement Table 1B." },
                new[] { "Reference values", $"Fb {d.Values.Fb:0}, Fc {d.Values.Fc:0}, Fv {d.Values.Fv:0}, Fc⊥ {d.Values.FcPerp:0}, E {d.Values.E / 1e6:0.0#}M, Emin {d.Values.Emin:N0} psi; CF Fb {d.CF.Fb:0.0#}, Fc {d.CF.Fc:0.0#}", d.Values.Source },
                new[] { "Length", $"ℓu = {FtIn.Of(d.Input.LengthIn)}, le = Ke ℓu = {FtIn.Of(d.Le)}, le/d = {d.Slenderness:0.0}" + (d.SlendernessOk ? " ≤ 50" : " > 50: not allowed"), "Pinned at both plates (Ke = 1.0); the sheathing braces the weak axis, so d is the depth." },
                new[] { "FcE", $"0.822 × {d.EminPrime:N0} / {d.Slenderness:0.0}² = {(g?.FcE ?? 0):0} psi", "Eq. 3.7-1; Emin' = Emin (dry, normal temperature)." },
                new[] { "Lateral", d.Exterior ? $"Wind {d.Input.WindPressurePsf:0.0} psf (C&C, strength)" : $"Interior {d.Input.InteriorPressurePsf:0} psf (IBC 1607.16)",
                        d.Exterior ? data.StudWindNote ?? "" : "A live-load duration (CD 1.0)." },
                new[] { "Top loads per stud", loads ? $"D {data.TopLoad.Dead:0}, L {data.TopLoad.Live:0}, Lr {data.TopLoad.RoofLive:0}, S {data.TopLoad.Snow:0} plf × {f.Spacing:0}/12" : "none entered", "Service loads on the wall's top, carried by each stud over its spacing." },
            };
            p.Children.Add(Table(new[] { "Item", "Value", "Notes" }, info));

            var rows = d.Combinations.Select(c => new[]
            {
                c.Name, c.Equation, $"{c.CD:0.00}", $"{c.P:N0}", $"{c.fc:0}", $"{c.CP:0.000}", $"{c.FcPrime:0}", $"{c.fb:0}", $"{c.FbPrime:0}",
                double.IsInfinity(c.Ratio) ? "fc ≥ FcE" : $"{c.Ratio:0.00}" + (ReferenceEquals(c, g) ? "  ◀" : ""),
            }).ToList();
            p.Children.Add(Table(new[] { "Combination (IBC 1605.2)", "Eq.", "CD", "P (lb)", "fc", "CP", "Fc'", "fb", "Fb'", "Eq. 3.9-3" }, rows));

            double sh = d.MaxShearRatio;
            var checks = new List<string[]>
            {
                new[] { "Bending and compression", g == null ? "" : double.IsInfinity(g.Ratio) ? "fc ≥ FcE: buckles" : $"{g.Ratio:0.00} ≤ 1.0 ({g.Name})", Adjustments.CombinedSource },
                new[] { "Bearing on the plates", $"fc = {d.BearingFc:0} psi ≤ Fc⊥' = {d.FcPerpPrime:0} psi ({(d.FcPerpPrime > 0 ? d.BearingFc / d.FcPerpPrime : 0):0.00})", "NDS 3.10.1; Cb = 1.0 (conservative); no CD on Fc⊥ (Table 4.3.1)." },
                new[] { "Shear", $"{sh:0.00} ≤ 1.0", "fv = 1.5 V / A, V = w ℓ / 2; Fv' = Fv CD." },
                new[] { "Deflection", $"{d.DeflectionIn:0.000}\" ≤ ℓ/{d.Input.DeflectionLimit:0} = {d.DeflectionAllowedIn:0.000}\"",
                        $"5 w ℓ⁴ / (384 E' I), w = {d.DeflectionPressurePsf:0.0} psf" + (d.Exterior ? " (0.42 × C&C, IBC Table 1604.3 note f)." : " (the interior 5 psf).") + " The limit is the project's (building model defaults)." },
            };
            p.Children.Add(Table(new[] { "Check", "Result", "Notes" }, checks));
            foreach (var n in d.Notes)
                p.Children.Add(new TextBlock { Text = "•  " + n, TextWrapping = TextWrapping.Wrap, Foreground = Ui.InkSecondary, Margin = new Thickness(6, 1, 0, 1) });

            var src = new Expander { Header = "Sources (quoted)", IsExpanded = false, Margin = new Thickness(0, 4, 0, 0) };
            var sp = new StackPanel();
            foreach (var c in StudDesign.Citations)
                sp.Children.Add(new TextBlock { Text = "•  " + c, TextWrapping = TextWrapping.Wrap, Foreground = Ui.InkSecondary, FontSize = 11.5, Margin = new Thickness(6, 2, 0, 2) });
            sp.Children.Add(new TextBlock { Text = "NDS-2018 and its Supplement from AWC's PDFs; IBC text from the 2025 California Building Code (2024 IBC). ASCE 7 wind: the wind model's (the same calculator as the wind loads).", TextWrapping = TextWrapping.Wrap, Foreground = Ui.InkMuted, FontSize = 11, Margin = new Thickness(6, 4, 0, 2) });
            src.Content = sp;
            p.Children.Add(src);
        }

        /// <summary>What fails, in a few words.</summary>
        private static string Worst(StudDesign d)
        {
            var bad = new List<string>();
            if (d.MaxRatio > 1) bad.Add(double.IsInfinity(d.MaxRatio) ? "it buckles (fc ≥ FcE)" : $"bending and compression {d.MaxRatio:0.00}");
            if (d.BearingFc > d.FcPerpPrime) bad.Add($"bearing {d.BearingFc / d.FcPerpPrime:0.00}");
            if (d.MaxShearRatio > 1) bad.Add($"shear {d.MaxShearRatio:0.00}");
            if (d.DeflectionIn > d.DeflectionAllowedIn) bad.Add($"deflection {d.DeflectionIn:0.00}\" > {d.DeflectionAllowedIn:0.00}\"");
            return string.Join(", ", bad) + ".";
        }

        /// <summary>The sill plate's anchor bolts: what to install, how it was found, and the code text it rests on.</summary>
        private static void SillAnchorSection(Panel p, SillAnchorage a, WallFraming f)
        {
            string dia = a.BoltDia == 0.5 ? "1/2\"" : "5/8\"";
            Section(p, "Sill anchors (to the foundation)", a.Count == 0 ? a.Note :
                    $"{a.Count} anchor bolts, {dia} dia., at {FtIn.Of(a.SpacingIn)} o.c. at most, " +
                    (a.ShearGoverns ? "set by the wall's shear (NDS)." : "set by the prescriptive 6 ft (the wall's shear needs less)."));
            if (a.Count == 0) return;
            var gaps = a.PositionsIn.Zip(a.PositionsIn.Skip(1), (x, y) => y - x).ToList();
            var rows = new List<string[]>
            {
                new[] { "Anchor bolts", $"{dia} dia., headed or J, with nut", a.Count.ToString(),
                        "Into the concrete " + FtIn.Of(a.EmbedmentIn) + " at least (IRC R403.1.6); in the middle third of the plate's " + FtIn.Inches(f.StudDepth) + " in width." },
                new[] { "Spacing", gaps.Count > 0 ? $"{FtIn.Of(gaps.Max())} as laid out" : "", "",
                        $"{FtIn.Of(a.SpacingIn)} o.c. at most: the less of 6'-0\" (IRC R403.1.6, IBC 2308.3.1) and Z'/v = {a.ZPrime:N0} / {a.ShearAsdPlf:0} plf = {(double.IsInfinity(a.SpacingByShearIn) ? "no limit" : FtIn.Of(a.SpacingByShearIn))} (SDPWS 4.3.6.4.1)." },
                new[] { "End bolts", $"{FtIn.Of(a.PositionsIn[0])} from each end", "",
                        $"Not more than {FtIn.Of(a.EndMaxIn)}" + (a.EndMaxIn < 12 ? " (half the spacing: the end bolt takes its share)" : "") + $" nor less than 7 diameters ({FtIn.Of(a.EndMinIn)}) from each end (IRC R403.1.6)." },
                new[] { "Washers", a.PlateWasher ? "0.229\" × 3\" × 3\" plate" : "standard cut (plate optional)", a.Count.ToString(), a.WasherReason },
            };
            if (a.Note != null) rows.Add(new[] { "Note", "", "", a.Note });
            p.Children.Add(Table(new[] { "Piece", "Size", "Qty", "Notes" }, rows));
            p.Children.Add(Table(new[] { "Check (ASD)", "Value" }, new List<string[]>
            {
                new[] { "Wall shear", $"v = 0.6 V / L = {a.ShearAsdPlf:0} plf; over {FtIn.Of(a.LengthFt * 12)}: {a.DemandLb:N0} lb" },
                new[] { "Bolt value", $"Z = {a.Z:0} lb (NDS-2018 Table 12E, {dia} bolt, 1-1/2\" sill, {a.SpeciesGroup}, Z∥); Z' = Z × CD = {a.Z:0} × {a.CD:0.0} = {a.ZPrime:N0} lb" },
                new[] { "Bolt group", $"{a.Count} × {a.ZPrime:N0} = {a.CapacityLb:N0} lb ≥ {a.DemandLb:N0} lb ({(a.DemandLb > 0 ? a.DemandLb / a.CapacityLb : 0):0.00})" },
            }));
            var src = new Expander { Header = "Sources (quoted)", IsExpanded = false, Margin = new Thickness(0, 4, 0, 0) };
            var sp = new StackPanel();
            foreach (var c in SillAnchorage.Citations)
                sp.Children.Add(new TextBlock { Text = "•  " + c, TextWrapping = TextWrapping.Wrap, Foreground = Ui.InkSecondary, FontSize = 11.5, Margin = new Thickness(6, 2, 0, 2) });
            sp.Children.Add(new TextBlock { Text = "Over two stories in SDC D0–D2 (IRC R403.1.6.1), bolts go to 4 ft o.c. with 3 × 3 plate washers on braced wall lines: seismic, not checked here.", TextWrapping = TextWrapping.Wrap, Foreground = Ui.InkSecondary, FontSize = 11.5, Margin = new Thickness(6, 2, 0, 2) });
            src.Content = sp;
            p.Children.Add(src);
        }

        private static string NailSize(CommonNailSizes n) =>
            n == CommonNailSizes.NAILSIZE_6D ? "0.113\" × 2\"" : n == CommonNailSizes.NAILSIZE_8D ? "0.131\" × 2 1/2\"" : n == CommonNailSizes.NAILSIZE_10D ? "0.148\" × 3\"" : "";

        private string Anchor(HoldownLanding l)
        {
            if (l == null) return "";
            switch (l.Family)
            {
                case HoldownFamilies.Strap_ToWallBelow: return "Strap nailed to the wall below.";
                case HoldownFamilies.STHD_ToFoundation: return "Strap cast into the concrete.";
                default: return l.Support == SupportKind.Foundation ? "Anchor bolt into the foundation (per the hold-down's installation; not designed here)." : $"Fastened to {DetailCalcs.Support(l)}.";
            }
        }

        /// <summary>A catalog's fastener text for the drawing: a bare nail diameter ("28 0.162") gets its unit and kind.</summary>
        internal static string Fast(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            var m = System.Text.RegularExpressions.Regex.Match(s.Trim(), @"^(\d+)\s+(0?\.\d+)$");
            return m.Success ? $"{m.Groups[1].Value} {m.Groups[2].Value}\" nails" : s;
        }

        internal static void Section(Panel p, string head, string note)
        {
            p.Children.Add(new TextBlock { Text = head, FontSize = 14, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 14, 0, 2) });
            if (!string.IsNullOrEmpty(note)) p.Children.Add(new TextBlock { Text = note, Foreground = Ui.InkSecondary, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 4) });
        }

        internal static FrameworkElement Table(string[] head, List<string[]> rows)
        {
            var g = new Grid { Margin = new Thickness(0, 2, 0, 2) };
            for (int c = 0; c < head.Length; c++)
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = c == head.Length - 1 ? new GridLength(1, GridUnitType.Star) : GridLength.Auto });
            void Cell(string text, int row, int col, bool bold)
            {
                var t = new TextBlock { Text = text ?? "", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(4, 2, 12, 2), FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal, Foreground = bold ? Ui.InkSecondary : Ui.Ink };
                Grid.SetRow(t, row);
                Grid.SetColumn(t, col);
                g.Children.Add(t);
            }
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            for (int c = 0; c < head.Length; c++) Cell(head[c], 0, c, true);
            for (int i = 0; i < rows.Count; i++)
            {
                g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                if (i % 2 == 0)
                {
                    var band = new Border { Background = Ui.Frozen("#F6F7F9") };
                    Grid.SetRow(band, i + 1);
                    Grid.SetColumnSpan(band, head.Length);
                    g.Children.Add(band);
                }
                for (int c = 0; c < head.Length && c < rows[i].Length; c++) Cell(rows[i][c], i + 1, c, false);
            }
            return g;
        }
    }

    /// <summary>
    /// The wall's framing in elevation, to scale and fitted to the space: plates, studs, posts and blocking, one face of
    /// sheathing over them with its nails, the hold-downs at the bottom with their anchors and labels, the hold-downs from
    /// above at the top with their posts, the dimensions (length, height, sheet widths, stud spacing) and callouts.
    /// </summary>
    public sealed class FramingElevation : FrameworkElement
    {
        private readonly WallBuildData d;
        private readonly WallFraming f;
        private readonly FaceResult face;
        private readonly string note;
        private readonly bool nails;

        private static readonly Brush StudFill = Ui.Frozen("#EAD7B5"), PlateFill = Ui.Frozen("#D9B98C"), PostFill = Ui.Frozen("#C99A5B"),
                                      UnderFill = Ui.Frozen("#E9A15C"), SheetFill = Ui.Frozen("#4D9DB8D9"), GypsumFill = Ui.Frozen("#55E5E7EB"),
                                      EdgeNail = Ui.Frozen("#B42318"), FieldNail = Ui.Frozen("#3F3F46"), AboveInk = Ui.Frozen("#C2410C"), AnchorInk = Ui.Frozen("#1F5F8B");
        private static readonly Pen Edge = DetailInk.P(DetailInk.Timber, 0.7), SheetEdge = DetailInk.P(Ui.Frozen("#3B6EA8"), 1.3);

        public FramingElevation(WallBuildData data, WallFraming framing, FaceResult shown, string note, bool showNails)
        {
            d = data;
            f = framing;
            face = shown;
            this.note = note;
            nails = showNails;
            SnapsToDevicePixels = true;
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            InvalidateVisual();
        }

        /// <summary>
        /// The sill bolts' dimension chain at <paramref name="y"/>: each end to its bolt, and the bolt spacing, once with
        /// "TYP." when every space is the same (else each one). An end too short for its text has it just outside the wall.
        /// </summary>
        private void AnchorChain(DrawingContext dc, SillAnchorage a, double L, Func<double, double> X, double y)
        {
            var pen = DetailInk.P(AnchorInk, 1);
            var ticks = new List<double> { 0 };
            ticks.AddRange(a.PositionsIn);
            ticks.Add(L);
            dc.DrawLine(pen, new Point(X(0), y), new Point(X(L), y));
            foreach (double t in ticks)
            {
                bool bolt = t > 0 && t < L;
                dc.DrawLine(pen, new Point(X(t) - 3, y + 3), new Point(X(t) + 3, y - 3)); // architectural tick
                if (bolt) dc.DrawLine(DetailInk.P(AnchorInk, 0.6), new Point(X(t), y - 6), new Point(X(t), y + 4));
            }
            var gaps = a.PositionsIn.Zip(a.PositionsIn.Skip(1), (p, q) => (p, q)).ToList();
            bool same = gaps.Count > 0 && gaps.All(g => Math.Abs((g.q - g.p) - (gaps[0].q - gaps[0].p)) < 1.0 / 16);
            void Put(double from, double to, string text, int outside)
            {
                double w = DetailInk.Text(this, text, 10, AnchorInk, false).Width;
                double px = X(to) - X(from);
                if (w + 6 <= px || outside == 0) DetailInk.Label(dc, this, text, new Point((X(from) + X(to)) / 2, y - 8), AnchorInk, 10, centre: true);
                else if (outside < 0) DetailInk.Label(dc, this, text, new Point(X(from) - 6, y - 15), AnchorInk, 10, rightAligned: true);
                else DetailInk.Label(dc, this, text, new Point(X(to) + 6, y - 15), AnchorInk, 10);
            }
            Put(0, a.PositionsIn[0], FtIn.Of(a.PositionsIn[0]), -1);
            Put(a.PositionsIn.Last(), L, FtIn.Of(L - a.PositionsIn.Last()), +1);
            if (same)
            {
                var mid = gaps[gaps.Count / 2];
                Put(mid.p, mid.q, $"{FtIn.Of(mid.q - mid.p)} TYP.", 0);
            }
            else foreach (var g in gaps) Put(g.p, g.q, FtIn.Of(g.q - g.p), 0);
            DetailInk.Label(dc, this, $"Sill anchor bolts ({a.Count})", new Point(X(0), y + 5), AnchorInk, 9.5);
        }

        protected override void OnRender(DrawingContext dc)
        {
            double W = ActualWidth, Hh = ActualHeight;
            dc.DrawRectangle(DetailInk.Paper, null, new Rect(0, 0, W, Hh));
            if (f == null || f.Length <= 0 || f.Height <= 0) return;
            double L = f.Length, H = f.Height;
            double left = 96, right = 230, top = 120, bottom = 206;
            double s = Math.Min((W - left - right) / L, (Hh - top - bottom) / H);
            if (s <= 0) return;
            double x0 = left + Math.Max(0, ((W - left - right) - L * s) / 2), yb = top + H * s + Math.Max(0, ((Hh - top - bottom) - H * s) / 2);
            double X(double x) => x0 + x * s;
            double Y(double y) => yb - y * s;
            Rect R(double a, double b, double c, double e) => new Rect(new Point(X(a), Y(c)), new Point(X(b), Y(e)));
            var r = d.Result;

            // what it stands on
            if (d.FirstStory) DetailInk.Hatch(dc, new Rect(X(0) - 24, yb, L * s + 48, 22), DetailInk.Concrete);
            else dc.DrawRectangle(DetailInk.Joint, DetailInk.P(DetailInk.Dim, 0.8), new Rect(X(0) - 14, yb, L * s + 28, 10));
            DetailInk.Label(dc, this, d.FirstStory ? "Foundation" : "Floor of the story below", new Point(X(L / 2), yb + (d.FirstStory ? 11 : 5)), DetailInk.Dim, 10, centre: true);

            // the framing
            foreach (var p in f.Pieces)
            {
                Brush fill = p.Kind == FramingKind.Stud ? StudFill : p.Kind == FramingKind.EndPost ? PostFill : p.Kind == FramingKind.PostUnder ? UnderFill : PlateFill;
                dc.DrawRectangle(fill, Edge, R(p.X0, p.X1, p.Y1, p.Y0));
            }

            // one face of sheathing and its nails
            if (face != null)
            {
                bool gyp = face.Option?.Table == SheathingTable.GypsumAndPlaster;
                foreach (var sh in f.Sheets)
                {
                    var rr = R(sh.X0, sh.X1, sh.Y1, sh.Y0);
                    rr.Inflate(-0.6, -0.6);
                    dc.DrawRectangle(gyp ? GypsumFill : SheetFill, SheetEdge, rr);
                    if (rr.Width > 60 && rr.Height > 30)
                        DetailInk.Label(dc, this, $"{FtIn.Of(sh.Width)} × {FtIn.Of(sh.Height)}", new Point(rr.X + rr.Width / 2, rr.Y + Math.Min(rr.Height / 2, 40)), Ui.Frozen("#3B6EA8"), 9.5, centre: true);
                    if (nails && face.Face != null && face.Option != null && face.Option.UsesEdgeSpacing)
                    {
                        var (edge, field) = f.Nails(sh, face.Face.EdgeSpacing);
                        double re = Math.Max(1.1, Math.Min(2.2, 0.28 * s)), rf = re * 0.8;
                        foreach (var n in edge) dc.DrawEllipse(EdgeNail, null, new Point(X(n.X), Y(n.Y)), re, re);
                        foreach (var n in field) dc.DrawEllipse(FieldNail, null, new Point(X(n.X), Y(n.Y)), rf, rf);
                    }
                }
            }
            if (note != null) DetailInk.Label(dc, this, note, new Point(X(L / 2), Y(H / 2)), Ui.InkMuted, 12, centre: true);

            // the hold-downs at the bottom, on the end posts' inner faces, with the anchor
            double post = f.PostPlies * WallFraming.Ply;
            if (r != null && r.HoldownNeeded)
                foreach (bool low in new[] { true, false })
                {
                    var l = d.OwnAt(low);
                    double hx0 = low ? post : L - post - 2.9, hx1 = hx0 + 2.9;
                    var box = R(hx0, hx1, WallFraming.Ply + 12, WallFraming.Ply);
                    var fam = l?.Family ?? r.Inputs?.Holdown ?? HoldownFamilies.HDU_ToFoundation;
                    if (fam == HoldownFamilies.Strap_ToWallBelow || fam == HoldownFamilies.STHD_ToFoundation)
                    {
                        // a strap up the post's face, nailed, down past the sill into what is under it
                        double sx = low ? post / 2 : L - post / 2;
                        dc.DrawRectangle(DetailInk.Hardware, null, R(sx - 0.625, sx + 0.625, 30, -14));
                    }
                    else
                    {
                        dc.DrawRectangle(DetailInk.Hardware, null, box);
                        double rx = (hx0 + hx1) / 2;
                        dc.DrawLine(DetailInk.P(DetailInk.Hardware, 2.4), new Point(X(rx), Y(WallFraming.Ply)), new Point(X(rx), yb + (d.FirstStory ? 20 : 8)));
                    }
                    var ink = l == null ? Ui.Ink : !l.Supported || l.Model == null ? Ui.FailInk : l.Note != null ? Ui.CloseInk : Ui.Ink;
                    string model = l?.Model ?? r.Holdown?.Model ?? "none strong enough";
                    string fast = WallBuildWindow.Fast(l?.Choice?.Fasteners ?? r.Holdown?.Fasteners);
                    string to = l == null ? "" : !l.Supported ? "→ nothing under it" : $"→ {DetailCalcs.Support(l)}";
                    double tx = low ? X(0) : X(L);
                    double ty = yb + 34;
                    DetailInk.Label(dc, this, model, new Point(tx, ty), ink, 12, bold: true, rightAligned: !low);
                    if (fast != "") DetailInk.Label(dc, this, "fasteners " + fast, new Point(tx, ty + 16), ink, 10.5, rightAligned: !low);
                    DetailInk.Label(dc, this, $"T = {(l?.TensionAsd ?? r.TensionAsd):N0} lb", new Point(tx, ty + 30), ink, 10.5, rightAligned: !low);
                    if (to != "") DetailInk.Label(dc, this, to, new Point(tx, ty + 44), ink, 10.5, rightAligned: !low);
                }

            // the sill's anchor bolts (first story): each down into the concrete, a washer and nut on the sill
            var an = d.Anchors();
            if (an != null)
                foreach (double ax in an.PositionsIn)
                {
                    dc.DrawLine(DetailInk.P(AnchorInk, 2), new Point(X(ax), Y(WallFraming.Ply) - 3), new Point(X(ax), yb + 17));
                    dc.DrawLine(DetailInk.P(AnchorInk, 2), new Point(X(ax), yb + 17), new Point(X(ax) + 5, yb + 17)); // the hook
                    dc.DrawRectangle(AnchorInk, null, new Rect(X(ax) - Math.Max(3, 1.5 * s), Y(WallFraming.Ply) - 1.5, 2 * Math.Max(3, 1.5 * s), 1.5));
                }

            // what comes down from above: the hold-downs of the story above standing on the top plates, each over its post
            int k = 0;
            foreach (var a in d.Above.OrderBy(x => x.Along))
            {
                double x = X(Math.Max(0, Math.Min(L, a.Along)));
                var pen = DetailInk.P(AboveInk, 1.8);
                DetailInk.Arrow(dc, pen, new Point(x, Y(H)), new Point(x, Y(H) - 30));
                double ly = Y(H) - 46 - 30 * (k++ % 2);
                DetailInk.Label(dc, this, $"↑ {a.Landing.TensionAsd / 1000:0.0}k  {a.Landing.Model ?? ""}", new Point(x, ly), AboveInk, 10.5, bold: true, centre: true);
                DetailInk.Label(dc, this, $"from {a.From} above", new Point(x, ly + 14), AboveInk, 10, centre: true);
            }

            // dimensions: the sheet widths along the top, the length and the height
            double yd = Y(H) - (d.Above.Count > 0 ? 96 : 26);
            foreach (var sh in f.Sheets.Where(x => x.Y0 < 0.01))
                DetailInk.Dimension(dc, this, new Point(X(sh.X0), yd), new Point(X(sh.X1), yd), FtIn.Of(sh.Width));
            if (an != null && an.Count > 0) AnchorChain(dc, an, L, X, yb + 108);
            DetailInk.Dimension(dc, this, new Point(X(0), yb + 130), new Point(X(L), yb + 130), FtIn.Of(L));
            DetailInk.Dimension(dc, this, new Point(X(0) - 48, Y(H)), new Point(X(0) - 48, Y(0)), FtIn.Of(H), vertical: true);
            var studs = f.Pieces.Where(p => p.Kind == FramingKind.Stud).OrderBy(p => p.X0).ToList();
            if (studs.Count >= 2)
            {
                double ys = Y(WallFraming.Ply + 24); // low in the wall, clear of the sheet labels
                DetailInk.Dimension(dc, this, new Point(X(studs[0].CenterX), ys), new Point(X(studs[1].CenterX), ys), $"{f.Spacing:0}\" o.c.");
            }
            if (r != null && r.HoldownNeeded)
                DetailInk.Dimension(dc, this, new Point(X(post + 1.45), yb + 152), new Point(X(L - post - 1.45), yb + 152), $"d = {FtIn.Of(r.HoldownArm * 12)}");
            DetailInk.Label(dc, this, d.EastWest ? "W" : "S", new Point(X(0) - 16, Y(H) - 2), DetailInk.Dim, 11, bold: true);
            DetailInk.Label(dc, this, d.EastWest ? "E" : "N", new Point(X(L) + 6, Y(H) - 2), DetailInk.Dim, 11, bold: true);

            // callouts at the right
            double cx = X(L) + 46;
            void Callout(string text, double y, double atX)
            {
                dc.DrawLine(DetailInk.P(DetailInk.Dim, 0.8), new Point(cx - 4, y), new Point(atX, y));
                DetailInk.Label(dc, this, text, new Point(cx, y - 8), Ui.Ink, 10.5);
            }
            string sz = f.StudSize;
            Callout($"Double top plate, 2-{sz} flat", Y(H - WallFraming.Ply), X(L - 2));
            if (studs.Count > 0) Callout($"{sz} studs @ {f.Spacing:0}\" o.c., {FtIn.Of(studs[0].Length)}", Y(H * 0.62), X(studs.Last().CenterX));
            Callout($"Hold-down post, {f.PostPlies}-{sz}", Y(H * 0.44), X(L - post / 2));
            var blk = f.Pieces.FirstOrDefault(p => p.Kind == FramingKind.Blocking);
            if (blk != null) Callout($"{sz} blocking at the sheet joint", Y(blk.Y0 + 0.75), X(f.Pieces.Last(p => p.Kind == FramingKind.Blocking).CenterX));
            Callout($"Sill plate, {sz} flat", Y(WallFraming.Ply / 2), X(L - 2));
            if (an != null && an.Count > 0)
                DetailInk.Label(dc, this, $"{an.Count} anchor bolts {(an.BoltDia == 0.5 ? "1/2\"" : "5/8\"")} @ {FtIn.Of(an.SpacingIn)} max, {FtIn.Of(an.PositionsIn[0])} from the ends", new Point(cx, Y(WallFraming.Ply / 2) + 8), AnchorInk, 10);
            if (face != null)
            {
                var o = face.Option;
                Callout(o == null ? "Sheathing" : $"{ShearWallDesigner.MaterialName(o.Material)} {ShearWallDesigner.ThicknessName(o.Thickness)}", Y(H * 0.80), X(L - 6));
                if (face.Face != null && o != null && o.UsesEdgeSpacing)
                    DetailInk.Label(dc, this, $"{ShearWallDesigner.NailName(face.Face.Nail)} @ {face.Face.EdgeSpacing}\" edges / 12\" field", new Point(cx, Y(H * 0.80) + 8), Ui.InkSecondary, 10);
            }
            if (nails && face != null)
            {
                dc.DrawEllipse(EdgeNail, null, new Point(cx + 4, yb + 72), 2, 2);
                DetailInk.Label(dc, this, "edge nail", new Point(cx + 10, yb + 64), Ui.InkSecondary, 10);
                dc.DrawEllipse(FieldNail, null, new Point(cx + 4, yb + 88), 1.6, 1.6);
                DetailInk.Label(dc, this, "field nail", new Point(cx + 10, yb + 80), Ui.InkSecondary, 10);
            }
            if (f.Pieces.Any(p => p.Kind == FramingKind.PostUnder))
            {
                dc.DrawRectangle(UnderFill, Edge, new Rect(cx, yb + 98, 10, 10));
                DetailInk.Label(dc, this, "post under a hold-down above", new Point(cx + 14, yb + 96), Ui.InkSecondary, 10);
            }
        }
    }

    /// <summary>
    /// The wall in 3D: every framing piece, the hold-downs with their anchors, the posts and straps from above, the
    /// foundation (first story), and each face's sheets, either face off, or the right half cut away to show the framing.
    /// Drag to turn, wheel to zoom, double-click to fit. Inches: x along the wall, y up, z through it (face 1 at +z).
    /// </summary>
    public sealed class Framing3DView : Border
    {
        private readonly Viewport3D viewport = new Viewport3D();
        private readonly PerspectiveCamera camera = new PerspectiveCamera { FieldOfView = 40 };
        private readonly ModelVisual3D content = new ModelVisual3D();
        private Point3D target;
        private double yaw = 28, pitch = 16, distance = 400, radius = 100;
        private Point? dragFrom;
        private bool moved;
        private WallFraming shownFraming;

        public Framing3DView()
        {
            Background = new LinearGradientBrush(Color.FromRgb(0xF7, 0xF9, 0xFC), Color.FromRgb(0xE4, 0xE9, 0xF0), 90);
            viewport.Camera = camera;
            var lights = new Model3DGroup();
            lights.Children.Add(new AmbientLight(Color.FromRgb(0x78, 0x78, 0x78)));
            lights.Children.Add(new DirectionalLight(Color.FromRgb(0xA8, 0xA8, 0xA8), new Vector3D(-0.4, -0.8, -0.9)));
            lights.Children.Add(new DirectionalLight(Color.FromRgb(0x50, 0x50, 0x50), new Vector3D(0.6, -0.2, 0.8)));
            viewport.Children.Add(new ModelVisual3D { Content = lights });
            viewport.Children.Add(content);
            Child = new Grid { Children = { viewport, new TextBlock { Text = "Drag to turn · wheel to zoom · double-click to fit", Foreground = Ui.InkMuted, FontSize = 11, Margin = new Thickness(10, 8, 0, 0), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top } } };
            MouseLeftButtonDown += (s, e) =>
            {
                if (e.ClickCount == 2) { moved = false; Fit(); return; }
                dragFrom = e.GetPosition(this);
                CaptureMouse();
            };
            MouseMove += (s, e) =>
            {
                if (dragFrom == null) return;
                var p = e.GetPosition(this);
                yaw -= (p.X - dragFrom.Value.X) * 0.4;
                pitch = Math.Max(-60, Math.Min(85, pitch + (p.Y - dragFrom.Value.Y) * 0.3));
                dragFrom = p;
                moved = true;
                PlaceCamera();
            };
            MouseLeftButtonUp += (s, e) => { dragFrom = null; ReleaseMouseCapture(); };
            MouseWheel += (s, e) => { distance *= e.Delta > 0 ? 0.88 : 1 / 0.88; moved = true; PlaceCamera(); };
            SizeChanged += (s, e) => { if (!moved) Fit(); };
        }

        private static readonly Material Stud = Mat("#E6CFA3"), Plate = Mat("#D2AE78"), Post = Mat("#C08D4E"), Under = Mat("#E39A50"),
                                         Osb = Mat("#C9A66B"), Gyp = Mat("#ECEEF0"), Steel = Mat("#4B5563"), Concrete = Mat("#C9CDD2"), Strap = Mat("#C2410C");

        private static Material Mat(string hex)
        {
            var m = new DiffuseMaterial(Ui.Frozen(hex));
            m.Freeze();
            return m;
        }

        public void Show(WallBuildData d, WallFraming f, bool face1, bool face2, bool cut, List<FaceResult> faces = null)
        {
            var g = new Model3DGroup();
            bool newWall = !ReferenceEquals(f, shownFraming) && (shownFraming == null || f == null || Math.Abs(shownFraming.Length - f.Length) > 0.01 || Math.Abs(shownFraming.Height - f.Height) > 0.01);
            shownFraming = f;
            if (f == null || d == null) { content.Content = g; return; }
            double D = f.StudDepth, L = f.Length, H = f.Height;
            foreach (var p in f.Pieces)
            {
                var m = p.Kind == FramingKind.Stud || p.Kind == FramingKind.Blocking ? Stud : p.Kind == FramingKind.EndPost ? Post : p.Kind == FramingKind.PostUnder ? Under : Plate;
                g.Children.Add(Box(p.X0 + 0.04, p.Y0 + 0.04, 0, p.X1 - 0.04, p.Y1 - 0.04, D, m));
            }
            void Sheets(int i, double z0, double z1)
            {
                var fr = faces != null && i < faces.Count ? faces[i] : null;
                var m = fr?.Option?.Table == SheathingTable.GypsumAndPlaster ? Gyp : Osb;
                foreach (var s in f.Sheets)
                {
                    if (cut && s.X0 >= L / 2 - 0.01) continue; // the right half cut away: the framing shows
                    g.Children.Add(Box(s.X0 + 0.06, s.Y0 + 0.06, z0, s.X1 - 0.06, s.Y1 - 0.06, z1, m));
                }
            }
            double t1 = Thickness(faces, 0), t2 = Thickness(faces, 1);
            if (face1) Sheets(0, D, D + t1);
            if (face2) Sheets(1, -t2, 0);

            // hold-downs on the end posts' inner faces, the anchor rod down into the foundation
            var r = d.Result;
            double post = f.PostPlies * WallFraming.Ply;
            if (r != null && r.HoldownNeeded)
                foreach (bool low in new[] { true, false })
                {
                    double x0 = low ? post : L - post - 2.9;
                    var fam = d.OwnAt(low)?.Family ?? r.Inputs?.Holdown ?? HoldownFamilies.HDU_ToFoundation;
                    if (fam == HoldownFamilies.Strap_ToWallBelow || fam == HoldownFamilies.STHD_ToFoundation)
                    {
                        double sx = low ? post / 2 : L - post / 2;
                        g.Children.Add(Box(sx - 0.625, -14, -0.1, sx + 0.625, 30, -0.02, Steel));
                    }
                    else
                    {
                        g.Children.Add(Box(x0, WallFraming.Ply, D / 2 - 1.5, x0 + 2.9, WallFraming.Ply + 12, D / 2 + 1.5, Steel));
                        g.Children.Add(Box(x0 + 1.15, d.FirstStory ? -10 : -6, D / 2 - 0.3, x0 + 1.75, WallFraming.Ply + 3, D / 2 + 0.3, Steel));
                    }
                }
            // straps from above at their posts
            foreach (var a in d.Above)
                g.Children.Add(Box(a.Along - 0.625, H - 2, D + 0.05, a.Along + 0.625, H + 18, D + 0.15, Strap));
            // the sill's anchor bolts: a rod through the sill's middle, its washer and nut on top
            var an = d.Anchors();
            if (an != null)
                foreach (double ax in an.PositionsIn)
                {
                    double r2 = an.BoltDia / 2;
                    g.Children.Add(Box(ax - r2, -7, D / 2 - r2, ax + r2, WallFraming.Ply + 1.2, D / 2 + r2, Steel));
                    double w = an.PlateWasher ? 1.5 : 0.9;
                    g.Children.Add(Box(ax - w, WallFraming.Ply, D / 2 - w, ax + w, WallFraming.Ply + 0.23, D / 2 + w, Steel));
                }
            if (d.FirstStory) g.Children.Add(Box(-8, -14, -4, L + 8, 0, D + 4, Concrete));
            content.Content = g;
            target = new Point3D(L / 2, H / 2, D / 2);
            radius = 0.5 * Math.Sqrt(L * L + H * H + 400);
            if (newWall) moved = false;
            if (!moved) Fit(); else PlaceCamera();
        }

        /// <summary>Any model (a beam, a column): shown fitted, unless the same member is shown again and the view was turned.</summary>
        public void ShowModel(Model3DGroup g, string key)
        {
            shownFraming = null;
            if (key != shownKey) moved = false;
            shownKey = key;
            content.Content = g;
            var b = g?.Bounds ?? Rect3D.Empty;
            if (b.IsEmpty) return;
            target = new Point3D(b.X + b.SizeX / 2, b.Y + b.SizeY / 2, b.Z + b.SizeZ / 2);
            radius = 0.5 * Math.Sqrt(b.SizeX * b.SizeX + b.SizeY * b.SizeY + b.SizeZ * b.SizeZ);
            if (!moved) Fit(); else PlaceCamera();
        }

        private string shownKey;

        // the pieces a member model is built of (shared with MemberViews)
        internal static Material StudMaterial => Stud;
        internal static Material PostMaterial => Post;
        internal static Material SteelMaterial => Steel;
        internal static Material ConcreteMaterial => Concrete;
        internal static Material StrapMaterial => Strap;
        internal static Material PlateMaterial => Plate;
        internal static GeometryModel3D BoxModel(double x0, double y0, double z0, double x1, double y1, double z1, Material m) => Box(x0, y0, z0, x1, y1, z1, m);

        /// <summary>A face's sheathing thickness (in), from its nominal thickness ("15/32\"" → 0.469); 1/2 in. when not known.</summary>
        private static double Thickness(List<FaceResult> faces, int i)
        {
            var o = faces != null && i < faces.Count ? faces[i]?.Option : null;
            if (o == null) return 0.5;
            string s = ShearWallDesigner.ThicknessName(o.Thickness).Replace("\"", "");
            var parts = s.Split('/');
            return parts.Length == 2 && double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double a) &&
                   double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double b) && b > 0 ? a / b : 0.5;
        }

        private void Fit()
        {
            double aspect = ActualWidth > 0 && ActualHeight > 0 ? ActualHeight / ActualWidth : 0.6;
            double half = camera.FieldOfView / 2 * Math.PI / 180;
            double halfV = Math.Atan(Math.Tan(half) * aspect);
            distance = 1.1 * radius / Math.Sin(Math.Min(half, halfV));
            PlaceCamera();
        }

        private void PlaceCamera()
        {
            double y = yaw * Math.PI / 180, p = pitch * Math.PI / 180;
            var dir = new Vector3D(Math.Cos(p) * Math.Sin(y), Math.Sin(p), Math.Cos(p) * Math.Cos(y));
            camera.Position = target + dir * distance;
            camera.LookDirection = -dir * distance;
            camera.UpDirection = new Vector3D(0, 1, 0);
            camera.NearPlaneDistance = 1;
            camera.FarPlaneDistance = distance * 10;
        }

        private static GeometryModel3D Box(double x0, double y0, double z0, double x1, double y1, double z1, Material m)
        {
            var c = new[] { new Point3D(x0, y0, z0), new Point3D(x1, y0, z0), new Point3D(x1, y1, z0), new Point3D(x0, y1, z0),
                            new Point3D(x0, y0, z1), new Point3D(x1, y0, z1), new Point3D(x1, y1, z1), new Point3D(x0, y1, z1) };
            var mesh = new MeshGeometry3D();
            int[][] faces = { new[] { 0, 3, 2, 1 }, new[] { 4, 5, 6, 7 }, new[] { 0, 1, 5, 4 }, new[] { 1, 2, 6, 5 }, new[] { 2, 3, 7, 6 }, new[] { 3, 0, 4, 7 } };
            foreach (var fc in faces)
            {
                int i = mesh.Positions.Count;
                foreach (var k in fc) mesh.Positions.Add(c[k]);
                mesh.TriangleIndices.Add(i); mesh.TriangleIndices.Add(i + 1); mesh.TriangleIndices.Add(i + 2);
                mesh.TriangleIndices.Add(i); mesh.TriangleIndices.Add(i + 2); mesh.TriangleIndices.Add(i + 3);
            }
            mesh.Freeze();
            return new GeometryModel3D(mesh, m) { BackMaterial = m };
        }
    }
}
