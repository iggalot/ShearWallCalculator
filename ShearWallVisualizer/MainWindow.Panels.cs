// The text is turned -90° (reads bottom to top), so ▲ points left and ▼ right.
using BuildingModel.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace ShearWallVisualizer
{
    /// <summary>
    /// The flyout tabs on the window's edges: PLAN (left) closes or opens the plan, DATA (right) the tabs, so either one can
    /// take the whole width; both open is the usual layout. They are never both closed: closing one while the other is
    /// closed opens the other. The Wind loads workspace has no plan, so it has no tabs.
    /// </summary>
    public partial class MainWindow
    {
        private bool planOpen = true, dataOpen = true;
        private GridLength planWidth = new GridLength(750); // the plan's width with both open (the splitter's)

        private void EdgePlan_Click(object sender, RoutedEventArgs e)
        {
            if (planOpen && !dataOpen) dataOpen = true; // never both closed
            Remember();
            planOpen = !planOpen;
            Log.Info($"Layout: plan {(planOpen ? "opened" : "closed")}, data {(dataOpen ? "open" : "closed")}");
            ApplyPanels();
        }

        private void EdgeData_Click(object sender, RoutedEventArgs e)
        {
            if (dataOpen && !planOpen) planOpen = true;
            Remember();
            dataOpen = !dataOpen;
            Log.Info($"Layout: data {(dataOpen ? "opened" : "closed")}, plan {(planOpen ? "open" : "closed")}");
            ApplyPanels();
        }

        /// <summary>Keeps the splitter's width before one side closes, to restore it.</summary>
        private void Remember()
        {
            if (planOpen && dataOpen && drawingColumn.ActualWidth > 0) planWidth = new GridLength(drawingColumn.ActualWidth);
        }

        private void ApplyPanels()
        {
            if (drawingColumn == null || dataColumn == null) return;
            bool wind = InWindWorkspace;
            var edge = wind ? Visibility.Collapsed : Visibility.Visible;
            btnEdgePlan.Visibility = edge;
            btnEdgeData.Visibility = edge;
            bool plan = wind || planOpen, data = wind || dataOpen;

            drawingColumn.MinWidth = plan && data ? 400 : 0;
            drawingColumn.Width = !plan ? new GridLength(0) : data ? planWidth : new GridLength(1, GridUnitType.Star);
            dataColumn.MinWidth = data && plan ? 480 : 0;
            dataColumn.Width = data ? new GridLength(1, GridUnitType.Star) : new GridLength(0);

            if (!wind)
            {
                dockpanel.Visibility = plan ? Visibility.Visible : Visibility.Collapsed;
                if (!plan) selectionCard.Visibility = Visibility.Collapsed;
                drawingSplitter.Visibility = plan && data ? Visibility.Visible : Visibility.Collapsed;
            }
            MainTabControl.Visibility = data ? Visibility.Visible : Visibility.Collapsed;
            if (!data) drawingKey.Visibility = Visibility.Collapsed;

            btnEdgePlan.Content = planOpen ? "PLAN  ▲" : "PLAN  ▼";
            btnEdgePlan.ToolTip = planOpen ? "Close the plan: the data takes the whole width" : "Open the plan";
            btnEdgeData.Content = dataOpen ? "▼  DATA" : "▲  DATA";
            btnEdgeData.ToolTip = dataOpen ? "Close the data tabs: the plan takes the whole width" : "Open the data tabs";

            if (!wind && plan) Dispatcher.BeginInvoke(new System.Action(() => { Draw(); }), System.Windows.Threading.DispatcherPriority.Loaded);
            if (!wind && data) UpdateDrawingKey();
        }
    }
}
