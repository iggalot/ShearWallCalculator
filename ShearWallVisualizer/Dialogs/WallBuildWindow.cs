using calculator;
using ShearWallCalculator;
using ShearWallCalculator.Design;
using ShearWallCalculator.NDSProvisions;
using ShearWallVisualizer.Controls;
using NDS_WoodLibrary.NDS2018;
using WoodFraming;
using WoodFraming.Wpf;
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
        /// <summary>The wall as every app shows it (WoodFraming.Wpf): the whole wall from the model, with this app's design.</summary>
        public WallView View { get; set; }
        public string Footnote { get; set; }
        public bool FirstStory => Story == 0;
        public bool EastWest => Wall?.WallDir == WallDirs.EastWest;

        /// <summary>A plan point's distance (in) along the wall from its low end (west or south).</summary>
        public double AlongIn(Point p)
        {
            if (Wall == null) return 0;
            return EastWest ? (p.X - Math.Min(Wall.Start.X, Wall.End.X)) * 12 : (p.Y - Math.Min(Wall.Start.Y, Wall.End.Y)) * 12;
        }

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

        public WallFraming Framing() =>
            WallFraming.Build(Wall?.WallLength ?? 0, Wall?.WallHeight ?? 0, StudSize, StudSpacing, WallSchedule.PostAlongWall(Result?.Holdown),
                              Above.Select(a => a.Along));

        /// <summary>The NDS species for the calculator's species group (the lower G of the pair, as before: DF-L, SPF).</summary>
        public static Species NdsSpecies(WoodTypes w) => w == WoodTypes.WOODTYPE_SPF_HF ? Species.SprucePineFir : Species.DouglasFirLarch;

        /// <summary>
        /// A first-story wall's sill anchor bolts (null above the first story or before a design): for the ASD wind shear
        /// 0.6 V / L, the wall's nominal capacity, aspect ratio (the hold-downs never count on dead load here: the washer exception can apply).
        /// </summary>
        public SillAnchorage Anchors()
        {
            var r = Result;
            if (!FirstStory || r == null || r.Length <= 0) return null;
            double vAsd = 0.6 * r.V * 1000.0 / r.Length;
            double nominal = r.CombinedNominal > 0 ? r.CombinedNominal : r.Nominal;
            return SillAnchorage.Design(r.Length, vAsd, NdsSpecies(r.Inputs?.Species ?? WoodTypes.WOODTYPE_DF_SP), nominal, r.AspectRatio, false); // the hold-downs are sized without dead load
        }

        /// <summary>The connection defaults (the floor sheathing and the sill nail for an upper story's sill): details, not the model's.</summary>
        public WoodFraming.Connections.ConnectionDefaults Connections { get; set; }
        /// <summary>The line of shear walls in the diaphragm on top of it, as designed: its unit shear, boundary nailing and collector (null: none).</summary>
        public WoodFraming.Connections.DiaphragmLine Line { get; set; }
        /// <summary>An upper wall's floor under it along its line, as designed (null: the first story, or none).</summary>
        public WoodFraming.Connections.DiaphragmLine LineBelow { get; set; }
        /// <summary>The diaphragm's chords on top of it that its double top plate is (owner 2026-11-08: simple spans between the lines).</summary>
        public List<WoodFraming.Connections.DiaphragmChord> Chords { get; set; } = new List<WoodFraming.Connections.DiaphragmChord>();

        /// <summary>An upper-story wall's sill nailing (null on the first story or before a design): the saved design's own routine.</summary>
        public SillNailing SillNails() => FirstStory ? null : ShearWallCalculator.ModelAdapter.ShearWallCheck.SillNailingOf(Result, Connections);

        /// <summary>The story on top of it is the roof (blocking over it, not a rim).</summary>
        public bool Roof { get; set; }

        /// <summary>Its rim or blocking toe-nailed into its top plates (null before a design): the saved design's own routine.</summary>
        public RimToPlate TopToeNails() => ShearWallCalculator.ModelAdapter.ShearWallCheck.RimToPlateOf(Result, Connections, Roof);

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
        /// <summary>The wall's views, the same in every app (WoodFraming.Wpf): its framing and 3D, the Shear wall toggle.</summary>
        private readonly WallPanel panel = new WallPanel();
        private readonly ScrollViewer pieces = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        private readonly ScrollViewer calc = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        /// <summary>The shared views (for the tests).</summary>
        public WallPanel Panel => panel;
        private readonly CheckBox follow = new CheckBox { Content = "Open when a wall, beam or column is picked", VerticalAlignment = VerticalAlignment.Center, Foreground = Ui.InkSecondary };
        private WallBuildData data;
        /// <summary>The wall shown (null for a beam or column, or nothing).</summary>
        public WallBuildData Data => data;
        private WallFraming framing;
        private readonly StackPanel studBar;
        private readonly ComboBox studs = new ComboBox { MinWidth = 150 };
        private void NameStuds() => System.Windows.Automation.AutomationProperties.SetName(studs, "Studs");
        private bool fillingStuds;

        /// <summary>The engineer picked a stud size for the shown wall (null: the story's default). The host saves it.</summary>
        public event Action<string> StudSizeChosen;

        /// <summary>Ctrl+Z (false) or Ctrl+Y (true) pressed here: the host undoes or redoes the model.</summary>
        public event Action<bool> UndoRedo;

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

            // the views every app shows (Elevation, 3D: WoodFraming.Wpf), then this app's own: the pieces and the calculation
            panel.AddTab("Pieces", pieces);
            panel.AddTab("Calculation", calc);
            root.Children.Add(panel);
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
                // sizes under the default are not offered (the studs are not designed here)
                studs.Items.Add(new ComboBoxItem { Content = $"{def} (project default, {(d.FirstStory ? "first story" : "upper story")})", Tag = null });
                int min = Array.IndexOf(WallFraming.StudSizes, def);
                foreach (var s in WallFraming.StudSizes.Skip(Math.Max(0, min)))
                    studs.Items.Add(new ComboBoxItem { Content = s, Tag = s });
                studs.SelectedItem = studs.Items.Cast<ComboBoxItem>().FirstOrDefault(i => (i.Tag as string) == chosen) ?? studs.Items[0];
                studs.ToolTip = "The wall's studs, plates and posts; the plates are as wide as the studs. Saved with the wall in the building model (Ctrl+Z undoes it).";
            }
            finally { fillingStuds = false; }
        }

        /// <summary>Pick a tab: 0 Elevation, 1 3D, 2 Pieces, 3 Calculation.</summary>
        public void ShowTab(int i) { if (i >= 0 && i < panel.Tabs.Items.Count) panel.Tabs.SelectedIndex = i; }

        /// <summary>A beam or a column: its own elevation, 3D model, pieces and calculation (the wall's toolbars hidden).</summary>
        public void ShowMember(MemberView v)
        {
            data = null;
            framing = null;
            studBar.Visibility = Visibility.Collapsed;
            if (v == null)
            {
                Title = "Member";
                title.Text = "Pick a wall, beam or column on the plan";
                subtitle.Text = "";
                pieces.Content = null;
                calc.Content = null;
                panel.ShowOther(null, new Model3DGroup(), null);
                return;
            }
            Title = v.WindowTitle;
            title.Text = v.Title;
            subtitle.Text = v.Subtitle;
            panel.ShowOther(v.Elevation?.Invoke(), v.Model?.Invoke() ?? new Model3DGroup(), v.Key, v.North, v.FromPlan);
            pieces.Content = v.Pieces?.Invoke();
            var cp = new StackPanel { Margin = new Thickness(4) };
            cp.Children.Add(CalcTable.Build(v.Lines ?? new List<CalcLine>()));
            if (!string.IsNullOrEmpty(v.Footnote)) cp.Children.Add(new TextBlock { Text = v.Footnote, Foreground = Ui.InkMuted, FontSize = 11, Margin = new Thickness(0, 10, 0, 0), TextWrapping = TextWrapping.Wrap });
            calc.Content = cp;
        }

        public void Show(WallBuildData d)
        {
            data = d;
            framing = d?.Wall == null ? null : d.Framing(); // the shear part's pieces (the Pieces tab)
            FillStuds(d);
            if (d?.Wall == null)
            {
                Title = "Wall";
                title.Text = "Pick a wall on the plan";
                subtitle.Text = "";
                panel.Show(null);
                pieces.Content = null;
                calc.Content = null;
                return;
            }
            var r = d.Result;
            // the views: the whole wall from the model, with this app's design over it (the same as in every app)
            panel.Show(d.View);
            var whole = panel.Framing ?? framing;
            Title = $"Wall {d.Name}: elevation, 3D and pieces";
            title.Text = $"{d.Name} · {d.LevelName}";
            subtitle.Text = $"{(d.EastWest ? "East-west" : "North-south")} wall, {FtIn.Of(whole.Length)} long × {FtIn.Of(whole.Height)} high as built" +
                            (Math.Abs(whole.Length - framing.Length) > 0.01 ? $", the shear wall {FtIn.Of(framing.Length)} of it" : "") + $" (story {d.Story + 1} of {d.Stories}). " +
                            (r == null ? "Not designed yet (no shear on it)." :
                             $"{r.Status}: v = {r.Demand:0} plf of {r.Capacity:0} plf" + (r.HoldownNeeded ? $", hold-downs {r.Holdown?.Model ?? "none strong enough"} for T = {r.TensionAsd:N0} lb (ASD)." : ", no uplift."));
            subtitle.Text += $" Studs {framing.StudSize} at {framing.Spacing:0}\" o.c. (not designed here).";
            pieces.Content = Pieces();
            var cp = new StackPanel { Margin = new Thickness(4) };
            cp.Children.Add(CalcTable.Build(d.Lines ?? new List<CalcLine>()));
            if (!string.IsNullOrEmpty(d.Footnote)) cp.Children.Add(new TextBlock { Text = d.Footnote, Foreground = Ui.InkMuted, FontSize = 11, Margin = new Thickness(0, 10, 0, 0), TextWrapping = TextWrapping.Wrap });
            calc.Content = cp;
        }

        /// <summary>The wall's sheathed faces (one or two), as designed.</summary>
        private List<FaceResult> Faces() => data?.Result?.Faces?.Where(f => f?.Face != null && f.Face.Table != SheathingTable.None).ToList() ?? new List<FaceResult>();

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
                new[] { "Sill plate (flat)", sz, "1", FtIn.Of(f.Length), data.FirstStory ? "On the foundation: preservative-treated; anchor bolts below (Sill anchors)." : "On the floor sheathing of the story below, nailed through it into the rim or blocking (Sill nailing)." },
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
            ShearWallFramingSection(p, f, Faces());

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
            var sn = data.SillNails();
            if (sn != null) SillNailSection(p, sn);
            var toe = data.TopToeNails();
            if (toe != null) RimToPlateSection(p, toe);
            if (data.Line != null) DiaphragmLineSection(p, data.Line);
            if (data.LineBelow != null && !ReferenceEquals(data.LineBelow, data.Line)) DiaphragmLineSection(p, data.LineBelow, under: true);
            if (data.Chords != null && data.Chords.Count > 0) ChordSection(p, data.Chords);

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

        /// <summary>
        /// SDPWS 4.3.7.1's framing of a shear wall (a wider face at the panel joints, the stud spacing), with the quoted text.
        /// The studs themselves are not designed here (gravity, wind on the wall's face): that belongs to a framing design app.
        /// </summary>
        private static void ShearWallFramingSection(Panel p, WallFraming f, List<FaceResult> faces)
        {
            bool wide = f.Spacing > ShearWallFraming.MaxStudSpacingIn + 1e-9;
            Section(p, "Shear wall framing", wide ? $"The studs at {f.Spacing:0}\" o.c. are over SDPWS 4.3.7.1's {ShearWallFraming.MaxStudSpacingIn:0}\" for a shear wall."
                                                  : "SDPWS 4.3.7.1's framing for a shear wall. The studs are not designed here (gravity loads, wind on the wall's face).");
            if (wide) ((TextBlock)p.Children[p.Children.Count - 1]).Foreground = Ui.FailInk;
            var rows = new List<string[]>
            {
                new[] { "Shear wall studs", $"{f.Spacing:0}\" o.c.", $"{(wide ? "Over" : "Within")} SDPWS 4.3.7.1 item 6's {ShearWallFraming.MaxStudSpacingIn:0}\" on centre." },
            };
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
            p.Children.Add(Table(new[] { "Item", "Value", "Notes" }, rows));
            var src = new Expander { Header = "Sources (quoted)", IsExpanded = false, Margin = new Thickness(0, 4, 0, 0) };
            var sp = new StackPanel();
            sp.Children.Add(new TextBlock { Text = "•  " + ShearWallFraming.Citation, TextWrapping = TextWrapping.Wrap, Foreground = Ui.InkSecondary, FontSize = 11.5, Margin = new Thickness(6, 2, 0, 2) });
            src.Content = sp;
            p.Children.Add(src);
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

        /// <summary>An upper story's sill nailing: what to install, how it was found (TR12, the sheathing as the gap), and the code text.</summary>
        private static void SillNailSection(Panel p, SillNailing n)
        {
            Section(p, "Sill nailing (to the floor below)", n.Fails ?? $"{n.Nail} nails at {FtIn.Of(n.SpacingIn)} o.c., through the {FtIn.Inches(n.SheathingIn)} in. floor sheathing into the rim or blocking, one row along the sill.");
            if (n.Z <= 0) return;
            string spacing = double.IsInfinity(n.SpacingByShearIn) ? "no limit" : FtIn.Of(n.SpacingByShearIn);
            p.Children.Add(Table(new[] { "Check (ASD)", "Value" }, new List<string[]>
            {
                new[] { "Wall shear", $"v = 0.6 V / L = {n.ShearAsdPlf:0} plf" },
                new[] { "Nail", $"{n.Nail}: {FtIn.Inches(n.NailLengthIn)} × {n.NailDiaIn:0.000} in. (NDS Table L4); through the {FtIn.Inches(n.SillIn)} in. sill and {FtIn.Inches(n.SheathingIn)} in. sheathing, {n.PenetrationIn:0.###} in. into the rim (6D = {6 * n.NailDiaIn:0.###} in. the least, NDS 12.1.6.4)" },
                new[] { "Nail value", $"Z = {n.Z:0.0} lb (AWC TR12 Table 1-1, single shear, the sheathing as the gap g = {n.SheathingIn:0.###} in., {n.SpeciesGroup}; mode {n.Mode}); Z' = Z × CD = {n.Z:0.0} × {n.CD:0.0} = {n.ZPrime:0.0} lb" },
                new[] { "Spacing", $"Z'/v = {n.ZPrime:0.0} / {n.ShearAsdPlf:0} plf = {spacing}; used {FtIn.Of(n.SpacingIn)} (to the 1/2 in. below; the app's most {FtIn.Of(n.MaxSpacingIn)}, least {FtIn.Of(n.MinSpacingIn)}: not code values)" },
                new[] { "Capacity", n.SpacingIn > 0 ? $"{n.CapacityPlf:0} plf ≥ {n.ShearAsdPlf:0} plf ({(n.CapacityPlf > 0 ? n.ShearAsdPlf / n.CapacityPlf : 0):0.00})" : "" },
            }));
            var src = new Expander { Header = "Sources (quoted)", IsExpanded = false, Margin = new Thickness(0, 4, 0, 0) };
            var sp = new StackPanel();
            foreach (var c in SillNailing.Citations)
                sp.Children.Add(new TextBlock { Text = "•  " + c, TextWrapping = TextWrapping.Wrap, Foreground = Ui.InkSecondary, FontSize = 11.5, Margin = new Thickness(6, 2, 0, 2) });
            src.Content = sp;
            p.Children.Add(src);
        }

        /// <summary>
        /// The rim (or the roof's blocking) toe-nailed into the top plates: what to install, how it was found (NDS Figure 12A's
        /// geometry, Ctn), and the code text (owner 2026-11-08).
        /// </summary>
        private static void RimToPlateSection(Panel p, RimToPlate n)
        {
            Section(p, $"The {n.Member} to the top plates (toe-nailed)", n.Fails ?? $"The {n.Member} on the top plates toe-nailed down with {n.Nail} at {FtIn.Of(n.SpacingIn)} o.c., one row along the wall, each nail at 30° started 1/3 of its length up the {n.Member}'s face.");
            if (n.Z <= 0) return;
            string spacing = double.IsInfinity(n.SpacingByShearIn) ? "no limit" : FtIn.Of(n.SpacingByShearIn);
            p.Children.Add(Table(new[] { "Check (ASD)", "Value" }, new List<string[]>
            {
                new[] { "Shear at the top", $"v = 0.6 V / L = {n.ShearAsdPlf:0} plf, over the shear part (conservative: the owner's choice)" },
                new[] { "Nail", $"{n.Nail}: {FtIn.Inches(n.NailLengthIn)} × {n.NailDiaIn:0.000} in. (NDS Table L4); along it, {n.SideIn:0.###} in. in the {n.Member} = (L/3)/cos 30°, {n.PenetrationIn:0.###} in. into the plate (6D = {6 * n.NailDiaIn:0.###} in. the least, NDS 12.1.6.4)" },
                new[] { "Nail value", $"Z = {n.Z:0.0} lb (NDS 12.3.1, single shear, {n.SpeciesGroup}; mode {n.Mode}); Z' = Z × CD × Ctn = {n.Z:0.0} × {n.CD:0.0} × {RimToPlate.Ctn:0.00} = {n.ZPrime:0.0} lb" },
                new[] { "Spacing", $"Z'/v = {n.ZPrime:0.0} / {n.ShearAsdPlf:0} plf = {spacing}; used {FtIn.Of(n.SpacingIn)} (to the 1/2 in. below; the app's most {FtIn.Of(n.MaxSpacingIn)}, least {FtIn.Of(n.MinSpacingIn)}: not code values)" },
                new[] { "Capacity", n.SpacingIn > 0 ? $"{n.CapacityPlf:0} plf ≥ {n.ShearAsdPlf:0} plf ({(n.CapacityPlf > 0 ? n.ShearAsdPlf / n.CapacityPlf : 0):0.00})" : "" },
            }));
            var src = new Expander { Header = "Sources (quoted)", IsExpanded = false, Margin = new Thickness(0, 4, 0, 0) };
            var sp = new StackPanel();
            foreach (var c in RimToPlate.Citations)
                sp.Children.Add(new TextBlock { Text = "•  " + c, TextWrapping = TextWrapping.Wrap, Foreground = Ui.InkSecondary, FontSize = 11.5, Margin = new Thickness(6, 2, 0, 2) });
            src.Content = sp;
            p.Children.Add(src);
        }

        /// <summary>
        /// A diaphragm along the wall's line (owner 2026-11-07: the line shear and the collectors): the one on top of it
        /// (<paramref name="under"/> false: the boundary nailing over its top plates, the plates a collector), or an upper
        /// wall's floor under it (true: where no wall is under the line, the floor's rim is the collector, owner 2026-11-08).
        /// </summary>
        private static void DiaphragmLineSection(Panel p, WoodFraming.Connections.DiaphragmLine l, bool under = false)
        {
            string collector = l.StrapLb > 0 ? "the floor's rim" : "the top plates";
            Section(p, under ? "The floor under it along its line: boundary nailing and collector" : "Diaphragm boundary nailing and collector (at the top)",
                    l.Fails ?? $"{l.Nailing}; {collector} a collector of {l.CollectorLb:0} lb" + (l.StrapLb > 0 ? $", a strap across each rim splice for {l.StrapLb:0} lb." : "."));
            if (l.DepthFt <= 0) return;
            int walls = l.WallIds?.Count ?? 0;
            p.Children.Add(Table(new[] { "Check (ASD)", "Value" }, new List<string[]>
            {
                new[] { "Line", $"{(l.Roof ? "The roof" : "The floor")} along this wall's line: {walls} shear wall{(walls == 1 ? "" : "s")} on it (under it, and standing on it), the diaphragm {l.DepthFt:0.##} ft deep along it" },
                new[] { "Unit shear", $"v = R / depth, R the walls' shear on the line per wind case (the rigid and flexible runs each); worst 0.6 × {l.ShearAsdPlf / 0.6:0.0} = {l.ShearAsdPlf:0.0} plf" },
                new[] { "Sheathing", $"{l.Sheathing}; {(l.Blocked ? "blocked (SDPWS Table 4.2A)" : "unblocked (SDPWS Table 4.2C, Cases 2-6: the panel layout is not in the model)")}" },
                new[] { "Nailing", $"{l.Nailing}: {l.AllowablePlf:0.0} plf ≥ {l.ShearAsdPlf:0.0} plf ({(l.AllowablePlf > 0 ? l.ShearAsdPlf / l.AllowablePlf : 0):0.00})" + (l.Note != null ? $". {l.Note}" : "") },
                new[] { "Collector", $"{l.CollectorLb:0} lb at {l.CollectorAtFt:0.##} ft from the line's west or south end: the diaphragm's even delivery less the walls' shear parts. {l.Collector}" },
            }));
            var src = new Expander { Header = "Sources (quoted)", IsExpanded = false, Margin = new Thickness(0, 4, 0, 0) };
            var sp = new StackPanel();
            foreach (var c in ShearWallCalculator.Design.DiaphragmNailing.Citations.Concat(l.StrapLb > 0 ? WoodFraming.RimCollector.Citations : WoodFraming.TopPlateCollector.Citations))
                sp.Children.Add(new TextBlock { Text = "•  " + c, TextWrapping = TextWrapping.Wrap, Foreground = Ui.InkSecondary, FontSize = 11.5, Margin = new Thickness(6, 2, 0, 2) });
            src.Content = sp;
            p.Children.Add(src);
        }

        /// <summary>The diaphragm's chords its top plates are: each span between the lines of shear walls, M = w L² / 8 (a cantilever w a² / 2), T = M / d.</summary>
        private static void ChordSection(Panel p, List<WoodFraming.Connections.DiaphragmChord> chords)
        {
            var c0 = chords.OrderByDescending(c => c.ForceLb).First();
            Section(p, "Diaphragm chord (at the top)",
                    $"Its double top plate is a chord of the {(c0.Roof ? "roof" : "floor")} on top of it: {c0.ForceLb:0} lb at most (ASD), tension or compression with the wind's direction.");
            var rows = chords.Select(c => new[]
            {
                $"Wind along {(c.WindAlongX ? "x" : "y")}: {(c.Cantilever ? "cantilever" : "span")} {c.FromFt:0.##} to {c.ToFt:0.##} ft",
                $"w = {c.WPlf:0.0} plf (W); M = {(c.Cantilever ? "w a² / 2" : "w L² / 8")} = {c.MomentLbFt:0} lb·ft (ASD) at {c.AtFt:0.##} ft",
                $"T = M / d = {c.MomentLbFt:0} / {c.DepthFt:0.##} = {c.ForceLb:0} lb",
                (c.Chord ?? "") + (c.Fails != null ? $" {c.Fails}" : ""),
            }).ToList();
            p.Children.Add(Table(new[] { "Span", "Moment", "Chord force", "Chord" }, rows));
            var src = new Expander { Header = "Sources (quoted)", IsExpanded = false, Margin = new Thickness(0, 4, 0, 0) };
            var sp = new StackPanel();
            foreach (var c in new[] { "Simple spans between the lines of shear walls, M = w L² / 8, a cantilever past the last line w a² / 2, T = C = M / d (owner's choice, 2026-11-08)." }
                         .Concat(chords.Any(c => c.StrapLb > 0) ? WoodFraming.RimCollector.Citations : WoodFraming.TopPlateCollector.Citations))
                sp.Children.Add(new TextBlock { Text = "•  " + c, TextWrapping = TextWrapping.Wrap, Foreground = Ui.InkSecondary, FontSize = 11.5, Margin = new Thickness(6, 2, 0, 2) });
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
}
