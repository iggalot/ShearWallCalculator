using ShearWallCalculator;
using ShearWallCalculator.Design;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace ShearWallVisualizer.Dialogs
{
    /// <summary>
    /// Technical data for every hold-down the design can select, laid out like a manufacturer's catalog table: one row per
    /// model and fastener option. The rows come from <see cref="ShearWallDesigner.CatalogRows"/>, which reads the same catalog
    /// objects the hold-down selection uses, so what is shown is what is used. Walls currently using an option are listed.
    /// The "Standard" column edits the company standard list (<see cref="ShearWallDesigner.StandardList"/>); the host saves
    /// it and re-runs the design when it changes.
    /// </summary>
    public sealed class HoldownCatalogWindow : Window
    {
        private readonly ShearWallDesigner designer;
        private readonly ComboBox family = new ComboBox { Width = 230, Margin = new Thickness(0, 0, 12, 0) };
        private readonly TextBox search = new TextBox { Width = 180 };
        private readonly CheckBox standardOnly = new CheckBox { Content = "Only the standard list", Margin = new Thickness(12, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        private readonly CheckBox usedOnly = new CheckBox { Content = "Only options used by the design", Margin = new Thickness(12, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        private readonly DataGrid grid;
        private readonly TextBlock count = new TextBlock { Foreground = Brushes.DimGray, Margin = new Thickness(0, 6, 0, 0), TextWrapping = TextWrapping.Wrap };
        private List<Row> rows;
        private ICollectionView view;
        private StrapHoldownConditions sthd;
        private IEnumerable<KeyValuePair<string, IReadOnlyDictionary<int, WallDesignResult>>> lastDesign;

        private static readonly (string label, HoldownFamilies? value)[] Families =
        {
            ("All families", null),
            ("HDUE / DTT hold-downs to foundation", HoldownFamilies.HDU_ToFoundation),
            ("LTT / HTT / HTTH tension ties to foundation", HoldownFamilies.HTT_ToFoundation),
            ("Strap ties to wall below", HoldownFamilies.Strap_ToWallBelow),
            ("LSTHD / STHD strap-tie hold-downs to foundation", HoldownFamilies.STHD_ToFoundation),
            ("HDU hold-downs to foundation (discontinued, C-C-2021)", HoldownFamilies.HDU_Legacy_ToFoundation),
        };

        /// <param name="sthd">The project's foundation conditions for the LSTHD / STHD loads (null: the defaults).</param>
        public HoldownCatalogWindow(ShearWallDesigner designer, StrapHoldownConditions sthd = null)
        {
            this.designer = designer ?? throw new ArgumentNullException(nameof(designer));
            this.sthd = sthd ?? new StrapHoldownConditions();
            Title = "Hold-down catalog";
            Width = 1280;
            Height = 760;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = Brushes.White;

            var root = new DockPanel { Margin = new Thickness(16) };

            var header = new StackPanel();
            header.Children.Add(new TextBlock { Text = "Hold-down catalog", FontSize = 20, FontWeight = FontWeights.SemiBold });
            header.Children.Add(new TextBlock
            {
                Text = "Source: " + SimpsonCatalog.Citation + ". HDUE/DTT " + SimpsonCatalog.PagesHduDtt + ", LTT/HTT/HTTH " + SimpsonCatalog.PagesHtt +
                       ", LSTHD/STHD " + SimpsonCatalog.PagesSthd + ", strap ties " + SimpsonCatalog.PagesStraps + ". A copy is kept with the source code (docs/references/C-C-2026.pdf). " + LegacyNote,
                FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap, MaxWidth = 1180, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 4, 0, 0),
            });
            header.Children.Add(new TextBlock
            {
                TextWrapping = TextWrapping.Wrap, Foreground = Brushes.DimGray, Margin = new Thickness(0, 4, 0, 10), MaxWidth = 1180, HorizontalAlignment = HorizontalAlignment.Left,
                Text = "Read directly from the app's catalog data (ShearWallCalculator, NDSProvisions/SimpsonCatalog): the same objects the wall " +
                       "design selects hold-downs from, so these are the values in use. Allowable tension loads are ASD (lb) for each framing " +
                       "species; dimensions and fastener lengths in inches. The design picks the lightest option whose allowable load is at " +
                       "least the required tension, and uses it at both ends of the wall (a pair). \"Standard\" marks your standard list (kept by " +
                       "the app for every project; the Design tab can limit the selection to it); \"Company\" marks the company default shipped with the app. Columns: W, H or L, B, CL, " +
                       "SO and bolt diameter per the catalog figure (in); fastener length (in); minimum wood member h × w (in); DF/SP and SPF/HF " +
                       "allowable tension (lb); deflection at the allowable load (in).",
            });
            var filters = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
            foreach (var f in Families) family.Items.Add(f.label);
            family.SelectedIndex = 0;
            filters.Children.Add(family);
            filters.Children.Add(new TextBlock { Text = "Model contains ", VerticalAlignment = VerticalAlignment.Center });
            filters.Children.Add(search);
            filters.Children.Add(standardOnly);
            filters.Children.Add(usedOnly);
            header.Children.Add(filters);
            DockPanel.SetDock(header, Dock.Top);
            root.Children.Add(header);
            var footer = new DockPanel { Margin = new Thickness(0, 6, 0, 0) };
            var restore = new Button { Content = "Restore company default", Padding = new Thickness(10, 3, 10, 3), Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Top };
            var saveCompany = new Button { Content = "Save as company default...", Padding = new Thickness(10, 3, 10, 3), Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Top };
            var print = new Button
            {
                Content = "Print / save as PDF...", Padding = new Thickness(10, 3, 10, 3), Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Top,
                ToolTip = "The options shown (the filters above apply) on printable pages, for review. In the preview, Print (Ctrl+P) and choose " +
                          "\"Microsoft Print to PDF\" to save a PDF.",
            };
            print.Click += (s, e) => PrintCatalog();
            restore.Click += (s, e) => RestoreCompanyDefault();
            saveCompany.Click += (s, e) => SaveAsCompanyDefault();
            DockPanel.SetDock(saveCompany, Dock.Right);
            DockPanel.SetDock(restore, Dock.Right);
            DockPanel.SetDock(print, Dock.Right);
            footer.Children.Add(saveCompany);
            footer.Children.Add(restore);
            footer.Children.Add(print);
            count.Margin = new Thickness(0);
            footer.Children.Add(count);
            DockPanel.SetDock(footer, Dock.Bottom);
            root.Children.Add(footer);

            grid = new DataGrid
            {
                AutoGenerateColumns = false, IsReadOnly = true, CanUserAddRows = false, HeadersVisibility = DataGridHeadersVisibility.Column,
                GridLinesVisibility = DataGridGridLinesVisibility.Horizontal, HorizontalGridLinesBrush = new SolidColorBrush(Color.FromRgb(0xE4, 0xE2, 0xDC)),
                AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(0xFA, 0xFA, 0xF8)), FontSize = 13, RowHeight = 24,
                SelectionUnit = DataGridSelectionUnit.FullRow, BorderThickness = new Thickness(0),
            };
            // "Standard": a live check box (one click, no edit mode) bound two-way to the row, which updates the list.
            var check = new FrameworkElementFactory(typeof(CheckBox));
            check.SetBinding(ToggleButton_IsChecked, new Binding(nameof(Row.Standard)) { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            check.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
            check.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
            grid.Columns.Add(new DataGridTemplateColumn { Header = "Standard", CellTemplate = new DataTemplate { VisualTree = check } });
            grid.Columns.Add(new DataGridTextColumn { Header = "Company", Binding = new Binding(nameof(Row.Company)), ElementStyle = new Style(typeof(TextBlock)) { Setters = { new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center) } } });
            void Col(string title, string path, bool number = false)
            {
                var c = new DataGridTextColumn { Header = title, Binding = new Binding(path) };
                if (number) c.ElementStyle = new Style(typeof(TextBlock)) { Setters = { new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right), new Setter(MarginProperty, new Thickness(6, 0, 6, 0)) } };
                grid.Columns.Add(c);
            }
            Col("Family", nameof(Row.Family));
            Col("Model", nameof(Row.Model));
            Col("Ga.", nameof(Row.Gauge), true);
            Col("W", nameof(Row.W), true);
            Col("H or L", nameof(Row.HL), true);
            Col("B", nameof(Row.B), true);
            Col("CL", nameof(Row.CL), true);
            Col("SO", nameof(Row.SO), true);
            Col("Bolt dia.", nameof(Row.Bolt), true);
            Col("Fasteners", nameof(Row.Fasteners));
            Col("Length", nameof(Row.Length), true);
            Col("Min. wood", nameof(Row.Wood));
            Col("DF/SP (lb)", nameof(Row.DF), true);
            Col("SPF/HF (lb)", nameof(Row.SPF), true);
            Col("Defl. (in)", nameof(Row.Defl), true);
            Col("Code", nameof(Row.Code));
            Col("Source", nameof(Row.Source2));
            Col("Conditions (STHD)", nameof(Row.Conditions));
            Col("Used on walls", nameof(Row.Used));
            root.Children.Add(grid);
            Content = root;

            rows = designer.CatalogRows(this.sthd).Select(r => new Row(r, designer.StandardList, designer.CompanyList)).ToList();
            view = CollectionViewSource.GetDefaultView(rows);
            view.Filter = o => Keep((Row)o);
            grid.ItemsSource = view;

            family.SelectionChanged += (s, e) => Refilter();
            search.TextChanged += (s, e) => Refilter();
            foreach (var cb in new[] { usedOnly, standardOnly })
            {
                cb.Checked += (s, e) => Refilter();
                cb.Unchecked += (s, e) => Refilter();
            }
            designer.StandardList.Changed += OnStandardListChanged;
            Closed += (s, e) => designer.StandardList.Changed -= OnStandardListChanged;
            Refilter();
        }

        private static readonly DependencyProperty ToggleButton_IsChecked = System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty;

        /// <summary>Marks the options one floor's design uses (the rows stay in place, so the scroll position is kept).</summary>
        public void SetDesign(IReadOnlyDictionary<int, WallDesignResult> designResults) =>
            SetDesign(new[] { new KeyValuePair<string, IReadOnlyDictionary<int, WallDesignResult>>("", designResults) });

        /// <summary>
        /// Marks the options the design of every floor uses: "Level 2: 1, 3 · Roof: 2" (wall numbers by floor; the floor's
        /// name is left out when there is one floor).
        /// </summary>
        public void SetDesign(IEnumerable<KeyValuePair<string, IReadOnlyDictionary<int, WallDesignResult>>> byFloor)
        {
            lastDesign = byFloor;
            var floors = (byFloor ?? Enumerable.Empty<KeyValuePair<string, IReadOnlyDictionary<int, WallDesignResult>>>())
                         .Where(f => f.Value != null).ToList();
            bool named = floors.Count > 1;
            foreach (var r in rows)
                r.Used = string.Join("  ·  ", floors
                    .Select(f => (f.Key, walls: f.Value.Where(kv => kv.Value.Holdown != null && kv.Value.Holdown.Key == r.Key).Select(kv => kv.Key).OrderBy(k => k).ToList()))
                    .Where(f => f.walls.Count > 0)
                    .Select(f => (named ? f.Key + ": " : "") + string.Join(", ", f.walls)));
            if (usedOnly.IsChecked == true) view.Refresh();
            UpdateCount();
        }

        /// <summary>The conditions the LSTHD / STHD rows are shown for.</summary>
        public string StrapConditions => sthd.Describe();

        /// <summary>The project's STHD conditions changed: the LSTHD / STHD rows show the loads for the new ones.</summary>
        public void SetStrapConditions(StrapHoldownConditions conditions)
        {
            sthd = conditions?.Clone() ?? new StrapHoldownConditions();
            rows = designer.CatalogRows(sthd).Select(r => new Row(r, designer.StandardList, designer.CompanyList)).ToList();
            view = CollectionViewSource.GetDefaultView(rows);
            view.Filter = o => Keep((Row)o);
            grid.ItemsSource = view;
            if (lastDesign != null) SetDesign(lastDesign);
            Refilter();
        }

        /// <summary>
        /// The rows shown (the filters apply) as printable pages: a note of what is listed, then the catalog table, with the
        /// same columns as the window. Printed from the preview (choose "Microsoft Print to PDF" for a PDF).
        /// </summary>
        public System.Windows.Documents.FixedDocument BuildDocument(string project)
        {
            var shownRows = view.Cast<Row>().ToList();
            var filters = new List<string>();
            if (family.SelectedIndex > 0) filters.Add(Families[family.SelectedIndex].label);
            if (!string.IsNullOrWhiteSpace(search.Text)) filters.Add($"model contains \"{search.Text.Trim()}\"");
            if (standardOnly.IsChecked == true) filters.Add("only the standard list");
            if (usedOnly.IsChecked == true) filters.Add("only options used by the design");

            var blocks = new List<FrameworkElement>
            {
                ASCE7WindLoadCalculator.WindReport.Heading("Hold-down catalog"),
                ASCE7WindLoadCalculator.WindReport.Para("Source: " + SimpsonCatalog.Citation + ". HDUE/DTT " + SimpsonCatalog.PagesHduDtt + ", LTT/HTT/HTTH " + SimpsonCatalog.PagesHtt +
                    ", LSTHD/STHD " + SimpsonCatalog.PagesSthd + ", strap ties " + SimpsonCatalog.PagesStraps + ". " + LegacyNote),
                ASCE7WindLoadCalculator.WindReport.Para(
                    $"{shownRows.Count} fastener option{(shownRows.Count == 1 ? "" : "s")} " +
                    (filters.Count == 0 ? "(the whole catalog)" : "(" + string.Join("; ", filters) + ")") + ". " +
                    "Values read from the app's catalog data, the same objects the wall design selects hold-downs from. Allowable tension " +
                    "loads are ASD (lb) for each framing species; dimensions and fastener lengths in inches; minimum wood member h × w (in); " +
                    "deflection at the allowable load (in). Std: in your standard list. Co.: in the company default. Used on walls: wall " +
                    "numbers by floor in the current design."),
                ASCE7WindLoadCalculator.WindReport.Para(count.Text),
            };
            string[] headers = { "Std", "Co.", "Family", "Model", "Ga.", "W", "H or L", "B", "CL", "SO", "Bolt dia.", "Fasteners", "Length",
                                 "Min. wood", "DF/SP (lb)", "SPF/HF (lb)", "Defl. (in)", "Code", "Source", "Conditions (STHD)", "Used on walls" };
            var data = shownRows.Select(r => new[]
            {
                r.Standard ? "✓" : "", r.Company, r.Family, r.Model, r.Gauge, r.W, r.HL, r.B, r.CL, r.SO, r.Bolt, r.Fasteners, r.Length,
                r.Wood, r.DF, r.SPF, r.Defl, r.Code, r.Source2, r.Conditions, r.Used,
            }).ToList();
            blocks.AddRange(ASCE7WindLoadCalculator.WindReport.Tables(headers, data, rowsPerTable: 24));
            var info = new ASCE7WindLoadCalculator.ReportInfo { Project = string.IsNullOrWhiteSpace(project) ? "Untitled project" : project, Program = "Shear Wall Calculator" };
            return ASCE7WindLoadCalculator.WindReport.Document(info, "Simpson Strong-Tie hold-downs, " + SimpsonCatalog.Edition + " (catalog data in the app)",
                new[] { new ASCE7WindLoadCalculator.ReportSection("Hold-down catalog", blocks) });
        }

        /// <summary>The host's project name for the printed pages.</summary>
        public Func<string> ProjectName { get; set; }

        private void PrintCatalog()
        {
            var doc = BuildDocument(ProjectName?.Invoke());
            new ASCE7WindLoadCalculator.ReportPreviewWindow(doc, "Hold-down catalog") { Owner = this }.Show();
        }

        private void OnStandardListChanged(object sender, EventArgs e)
        {
            foreach (var r in rows) r.SyncStandard();
            if (standardOnly.IsChecked == true) view.Refresh();
            UpdateCount();
        }

        private bool Keep(Row r)
        {
            var fam = Families[Math.Max(0, family.SelectedIndex)].value;
            string text = (search.Text ?? "").Trim();
            if (fam.HasValue && r.Source.Family != fam.Value) return false;
            if (text.Length > 0 && r.Model.IndexOf(text, StringComparison.OrdinalIgnoreCase) < 0) return false;
            if (standardOnly.IsChecked == true && !r.Standard) return false;
            if (usedOnly.IsChecked == true && string.IsNullOrEmpty(r.Used)) return false;
            return true;
        }

        private void Refilter()
        {
            view.Refresh();
            UpdateCount();
        }

        private void UpdateCount()
        {
            int shown = view.Cast<object>().Count();
            int models = rows.Select(r => r.Model).Distinct().Count();
            var missing = designer.StandardKeysNotInCatalog();
            var mine = designer.StandardList;
            var company = designer.CompanyList;
            string companyText = company.CompanyVersion == 0 && company.Count == 0
                ? "No company default yet (version 0, empty)."
                : $"Company default v{company.CompanyVersion}" + (string.IsNullOrEmpty(company.CompanyDate) ? "" : $" ({company.CompanyDate})") + $": {company.Count} options.";
            int differ = mine.Keys.Count(k => !company.Contains(k)) + company.Keys.Count(k => !mine.Contains(k));
            string mineText = $"Your standard list: {mine.Count} option{(mine.Count == 1 ? "" : "s")}, " +
                              (differ == 0 ? "same as the company default" : $"{differ} different from the company default");
            count.Text = $"{shown} of {rows.Count} fastener options shown ({models} models). {companyText} {mineText}" +
                         (missing.Count == 0 ? "." : $"; not in this catalog (kept in the list, never selected): {string.Join(", ", missing)}.");
        }

        /// <summary>Replaces the user's list by the company default shipped with the app.</summary>
        private void RestoreCompanyDefault()
        {
            var company = designer.CompanyList;
            if (designer.StandardList.SameKeysAs(company) && designer.StandardList.CompanyVersion == company.CompanyVersion) return;
            if (MessageBox.Show(this, $"Replace your standard list ({designer.StandardList.Count} options) with the company default " +
                                      $"(version {company.CompanyVersion}, {company.Count} options)?", "Restore company default",
                                MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK) return;
            designer.StandardList.RestoreFrom(company);
        }

        /// <summary>
        /// Saves the current standard list as the next company default version. To ship it, the file is
        /// ShearWallVisualizer\HoldownStandardList.default.json in the source (the dialog opens there when it can find it):
        /// commit it and rebuild; each user's copy then follows it (or asks, if the user has changed theirs).
        /// </summary>
        private void SaveAsCompanyDefault()
        {
            var mine = designer.StandardList;
            int next = Math.Max(designer.CompanyList.CompanyVersion, mine.CompanyVersion) + 1;
            string date = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = $"Save company default hold-down list, version {next}",
                FileName = HoldownStandardList.PackagedFileName,
                Filter = "Hold-down list (*.json)|*.json",
                InitialDirectory = SourceFolder() ?? AppDomain.CurrentDomain.BaseDirectory,
            };
            if (dialog.ShowDialog(this) != true) return;

            var company = new HoldownStandardList();
            company.RestoreFrom(mine);
            company.CompanyVersion = next;
            company.CompanyDate = date;
            try { company.Save(dialog.FileName); }
            catch (Exception ex) when (ex is System.IO.IOException || ex is UnauthorizedAccessException)
            {
                MessageBox.Show(this, "The company default could not be saved:\n" + ex.Message, "Save as company default", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            designer.CompanyList = company;
            foreach (var r in rows) r.SetCompany(company);
            mine.MarkAsCompanyVersion(next, date);   // raises Changed: the host saves the user's copy and re-runs the design
            MessageBox.Show(this, $"Saved company default version {next} ({mine.Count} options) to:\n{dialog.FileName}\n\n" +
                                  "To ship it with the app, this file must be ShearWallVisualizer\\HoldownStandardList.default.json in the " +
                                  "source. Commit it and rebuild. On start-up, each user's list then follows the new version, or the user " +
                                  "is asked if they have changed their own list.", "Save as company default", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>The ShearWallVisualizer source folder when the program runs from a build inside the source tree, else null.</summary>
        private static string SourceFolder()
        {
            var dir = new System.IO.DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            for (int i = 0; i < 6 && dir != null; i++, dir = dir.Parent)
            {
                if (System.IO.File.Exists(System.IO.Path.Combine(dir.FullName, "ShearWallVisualizer.csproj"))) return dir.FullName;
                var sub = System.IO.Path.Combine(dir.FullName, "ShearWallVisualizer");
                if (System.IO.File.Exists(System.IO.Path.Combine(sub, "ShearWallVisualizer.csproj"))) return sub;
            }
            return null;
        }

        /// <summary>Where the discontinued HDU rows come from (they are not in the current edition).</summary>
        private static readonly string LegacyNote = "Discontinued HDU-SDS2.5 (used only for walls set to that family): " + SimpsonCatalog.LegacyHduCitation + "; copy in docs/references/" + SimpsonCatalog.LegacyHduLetter + ".pdf.";

        private static string FamilyName(HoldownFamilies f) =>
            f == HoldownFamilies.HDU_Legacy_ToFoundation ? "HDU (discontinued)" :
            f == HoldownFamilies.HDU_ToFoundation ? "HDUE / DTT" : f == HoldownFamilies.HTT_ToFoundation ? "LTT / HTT" :
            f == HoldownFamilies.STHD_ToFoundation ? "STHD" : "Strap";

        private static string N(double? v) => v.HasValue ? v.Value.ToString("0.####", CultureInfo.InvariantCulture) : "";

        /// <summary>One catalog option as shown. Model data are repeated only on the model's first option.</summary>
        private sealed class Row : INotifyPropertyChanged
        {
            private readonly HoldownStandardList list;
            private string used = "";
            private bool standard;

            public Row(HoldownSpecRow r, HoldownStandardList list, HoldownStandardList company)
            {
                Source = r;
                this.list = list;
                bool first = r.FirstOfModel;
                Key = r.Key;
                Family = first ? FamilyName(r.Family) : "";
                Model = r.Model;
                Gauge = first ? r.Gauge.ToString(CultureInfo.InvariantCulture) : "";
                W = first ? N(r.W) : ""; HL = first ? N(r.HeightOrLength) : ""; B = first ? N(r.B) : ""; CL = first ? N(r.CL) : "";
                SO = first ? N(r.SO) : ""; Bolt = first ? N(r.AnchorBoltDia) : "";
                Fasteners = r.Fasteners; Length = N(r.FastenerLength);
                Wood = r.WoodMinHeight.HasValue || r.WoodMinWidth.HasValue ? $"{N(r.WoodMinHeight)} × {N(r.WoodMinWidth)}" : "";
                DF = r.AllowableDF_SP?.ToString("0", CultureInfo.InvariantCulture) ?? "";
                SPF = r.AllowableSPF_HF?.ToString("0", CultureInfo.InvariantCulture) ?? "";
                Defl = N(r.DeflectionAtAllowable); Code = first ? r.CodeRef : "";
                Conditions = r.Conditions ?? "";
                Source2 = first ? r.Source : "";
                standard = list.Contains(Key);
                SetCompany(company);
            }

            public HoldownSpecRow Source { get; }
            public string Key { get; }
            public string Family { get; }
            public string Model { get; }
            public string Gauge { get; }
            public string W { get; }
            public string HL { get; }
            public string B { get; }
            public string CL { get; }
            public string SO { get; }
            public string Bolt { get; }
            public string Fasteners { get; }
            public string Length { get; }
            public string Wood { get; }
            public string DF { get; }
            public string SPF { get; }
            public string Defl { get; }
            public string Code { get; }
            public string Conditions { get; }
            /// <summary>Catalog edition and page, e.g. "C-C-2026 p. 61" (on the model's first option).</summary>
            public string Source2 { get; }

            public string Used
            {
                get => used;
                set { if (used != value) { used = value; Raise(nameof(Used)); } }
            }

            /// <summary>Checked = in the company standard list. Setting it edits the list (which raises its Changed event).</summary>
            public bool Standard
            {
                get => standard;
                set
                {
                    if (standard == value) return;
                    standard = value;
                    Raise(nameof(Standard));
                    list.Set(Key, value);
                }
            }

            /// <summary>"✓" when the option is in the company default shipped with the app.</summary>
            public string Company { get; private set; } = "";

            public void SetCompany(HoldownStandardList company)
            {
                string now = company != null && company.Contains(Key) ? "✓" : "";
                if (now != Company) { Company = now; Raise(nameof(Company)); }
            }

            public void SyncStandard()
            {
                bool now = list.Contains(Key);
                if (now != standard) { standard = now; Raise(nameof(Standard)); }
            }

            public event PropertyChangedEventHandler PropertyChanged;
            private void Raise(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
