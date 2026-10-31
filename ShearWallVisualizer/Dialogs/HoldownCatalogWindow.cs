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
        private readonly List<Row> rows;
        private readonly ICollectionView view;
        private IReadOnlyDictionary<int, WallDesignResult> design = new Dictionary<int, WallDesignResult>();

        private static readonly (string label, HoldownFamilies? value)[] Families =
        {
            ("All families", null),
            ("HDU / DTT hold-downs to foundation", HoldownFamilies.HDU_ToFoundation),
            ("HTT tension ties to foundation", HoldownFamilies.HTT_ToFoundation),
            ("Strap ties to wall below", HoldownFamilies.Strap_ToWallBelow),
        };

        public HoldownCatalogWindow(ShearWallDesigner designer)
        {
            this.designer = designer ?? throw new ArgumentNullException(nameof(designer));
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
                TextWrapping = TextWrapping.Wrap, Foreground = Brushes.DimGray, Margin = new Thickness(0, 4, 0, 10), MaxWidth = 1180, HorizontalAlignment = HorizontalAlignment.Left,
                Text = "Read directly from the app's catalog data (ShearWallCalculator, NDSProvisions/SimpsonCatalog): the same objects the wall " +
                       "design selects hold-downs from, so these are the values in use. Allowable tension loads are ASD (lb) for each framing " +
                       "species; dimensions and fastener lengths in inches. The design picks the lightest option whose allowable load is at " +
                       "least the required tension, and uses it at both ends of the wall (a pair). \"Standard\" marks the company standard " +
                       "hardware (saved with the app, not the project); the Design tab can limit the selection to it. Columns: W, H or L, B, CL, " +
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
            DockPanel.SetDock(count, Dock.Bottom);
            root.Children.Add(count);

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
            Col("Used on walls", nameof(Row.Used));
            root.Children.Add(grid);
            Content = root;

            rows = designer.CatalogRows().Select(r => new Row(r, designer.StandardList)).ToList();
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

        /// <summary>Marks the options the current design uses (the rows stay in place, so the scroll position is kept).</summary>
        public void SetDesign(IReadOnlyDictionary<int, WallDesignResult> designResults)
        {
            design = designResults ?? new Dictionary<int, WallDesignResult>();
            foreach (var r in rows)
                r.Used = string.Join(", ", design.Where(kv => kv.Value.Holdown != null && kv.Value.Holdown.Key == r.Key).Select(kv => kv.Key).OrderBy(k => k));
            if (usedOnly.IsChecked == true) view.Refresh();
            UpdateCount();
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
            count.Text = $"{shown} of {rows.Count} fastener options shown ({models} models in the catalog). Standard list: {designer.StandardList.Count} option" +
                         (designer.StandardList.Count == 1 ? "" : "s") +
                         (missing.Count == 0 ? "." : $"; not in this catalog (kept in the list, never selected): {string.Join(", ", missing)}.");
        }

        private static string FamilyName(HoldownFamilies f) =>
            f == HoldownFamilies.HDU_ToFoundation ? "HDU / DTT" : f == HoldownFamilies.HTT_ToFoundation ? "HTT" : "Strap";

        private static string N(double? v) => v.HasValue ? v.Value.ToString("0.####", CultureInfo.InvariantCulture) : "";

        /// <summary>One catalog option as shown. Model data are repeated only on the model's first option.</summary>
        private sealed class Row : INotifyPropertyChanged
        {
            private readonly HoldownStandardList list;
            private string used = "";
            private bool standard;

            public Row(HoldownSpecRow r, HoldownStandardList list)
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
                standard = list.Contains(Key);
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
