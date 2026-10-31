using ShearWallCalculator.Design;
using System;
using System.Collections.Generic;
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
    /// </summary>
    public sealed class HoldownCatalogWindow : Window
    {
        private readonly ShearWallDesigner designer;
        private readonly ComboBox family = new ComboBox { Width = 230, Margin = new Thickness(0, 0, 12, 0) };
        private readonly TextBox search = new TextBox { Width = 180 };
        private readonly CheckBox usedOnly = new CheckBox { Content = "Only options used by the design", Margin = new Thickness(12, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        private readonly DataGrid grid;
        private readonly TextBlock count = new TextBlock { Foreground = Brushes.DimGray, Margin = new Thickness(0, 6, 0, 0) };
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
                       "least the required tension, and uses it at both ends of the wall (a pair). Columns: W, H or L, B, CL, SO and bolt diameter " +
                       "per the catalog figure (in); fastener length (in); minimum wood member h × w (in); DF/SP and SPF/HF allowable tension " +
                       "(lb); deflection at the allowable load (in).",
            });
            var filters = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
            foreach (var f in Families) family.Items.Add(f.label);
            family.SelectedIndex = 0;
            filters.Children.Add(family);
            filters.Children.Add(new TextBlock { Text = "Model contains ", VerticalAlignment = VerticalAlignment.Center });
            filters.Children.Add(search);
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
            void Col(string title, string path, bool number = false)
            {
                var c = new DataGridTextColumn { Header = title, Binding = new Binding(path) };
                if (number) c.ElementStyle = new Style(typeof(TextBlock)) { Setters = { new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right), new Setter(MarginProperty, new Thickness(6, 0, 6, 0)) } };
                grid.Columns.Add(c);
            }
            Col("Family", "Family");
            Col("Model", "Model");
            Col("Ga.", "Gauge", true);
            Col("W", "W", true);
            Col("H or L", "HL", true);
            Col("B", "B", true);
            Col("CL", "CL", true);
            Col("SO", "SO", true);
            Col("Bolt dia.", "Bolt", true);
            Col("Fasteners", "Fasteners");
            Col("Length", "Length", true);
            Col("Min. wood", "Wood");
            Col("DF/SP (lb)", "DF", true);
            Col("SPF/HF (lb)", "SPF", true);
            Col("Defl. (in)", "Defl", true);
            Col("Code", "Code");
            Col("Used on walls", "Used");
            root.Children.Add(grid);
            Content = root;

            family.SelectionChanged += (s, e) => Refresh();
            search.TextChanged += (s, e) => Refresh();
            usedOnly.Checked += (s, e) => Refresh();
            usedOnly.Unchecked += (s, e) => Refresh();
            Refresh();
        }

        /// <summary>Re-reads the catalog and marks the options the current design uses.</summary>
        public void SetDesign(IReadOnlyDictionary<int, WallDesignResult> designResults)
        {
            design = designResults ?? new Dictionary<int, WallDesignResult>();
            Refresh();
        }

        private void Refresh()
        {
            var fam = Families[Math.Max(0, family.SelectedIndex)].value;
            string text = (search.Text ?? "").Trim();
            var all = designer.CatalogRows();
            var view = new List<object>();
            foreach (var r in all)
            {
                if (fam.HasValue && r.Family != fam.Value) continue;
                if (text.Length > 0 && r.Model.IndexOf(text, StringComparison.OrdinalIgnoreCase) < 0) continue;
                var walls = design.Where(kv => kv.Value.Holdown != null && kv.Value.Holdown.Model == r.Model && kv.Value.Holdown.Fasteners == r.Fasteners)
                                  .Select(kv => kv.Key).OrderBy(k => k).ToList();
                if (usedOnly.IsChecked == true && walls.Count == 0) continue;
                bool first = r.FirstOfModel;
                view.Add(new
                {
                    Family = first ? FamilyName(r.Family) : "",
                    Model = r.Model,
                    Gauge = first ? r.Gauge.ToString(CultureInfo.InvariantCulture) : "",
                    W = first ? N(r.W) : "", HL = first ? N(r.HeightOrLength) : "", B = first ? N(r.B) : "", CL = first ? N(r.CL) : "",
                    SO = first ? N(r.SO) : "", Bolt = first ? N(r.AnchorBoltDia) : "",
                    r.Fasteners, Length = N(r.FastenerLength),
                    Wood = r.WoodMinHeight.HasValue || r.WoodMinWidth.HasValue ? $"{N(r.WoodMinHeight)} × {N(r.WoodMinWidth)}" : "",
                    DF = r.AllowableDF_SP?.ToString("0", CultureInfo.InvariantCulture) ?? "",
                    SPF = r.AllowableSPF_HF?.ToString("0", CultureInfo.InvariantCulture) ?? "",
                    Defl = N(r.DeflectionAtAllowable), Code = first ? r.CodeRef : "",
                    Used = string.Join(", ", walls),
                });
            }
            grid.ItemsSource = view;
            int models = all.Select(r => r.Model).Distinct().Count();
            count.Text = $"{view.Count} of {all.Count} fastener options shown ({models} models in the catalog).";
        }

        private static string FamilyName(HoldownFamilies f) =>
            f == HoldownFamilies.HDU_ToFoundation ? "HDU / DTT" : f == HoldownFamilies.HTT_ToFoundation ? "HTT" : "Strap";

        private static string N(double? v) => v.HasValue ? v.Value.ToString("0.####", CultureInfo.InvariantCulture) : "";
    }
}
