using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace ShearWallVisualizer.Helpers
{
    /// <summary>
    /// Helper for managing tabs in a WPF application
    ///             // Remove a tab
    /// TabControlManager.RemoveTab(myTabControl, tabAdvanced);

    //// Re-add the tab later
    /// TabControlManager.ReAddTab(myTabControl, "tabAdvanced");

    //// Add a brand new tab
    /// var newTab = new TabItem { Header = "Help", Content = new TextBlock { Text = "Support info..." }, Name = "tabHelp" };
    /// TabControlManager.AddNewTab(myTabControl, newTab);

    //// Remove or restore all
    /// TabControlManager.RemoveAllTabs(myTabControl);
    /// TabControlManager.RestoreAllTabs(myTabControl);
    /// </summary>
    public static class TabControlManager
    {
        // Stores removed tabs per TabControl instance
        private static readonly Dictionary<TabControl, Dictionary<string, Tuple<TabItem, int>>> _removedTabsMap
            = new Dictionary<TabControl, Dictionary<string, Tuple<TabItem, int>>>();

        private static Dictionary<string, Tuple<TabItem, int>> GetTabMap(TabControl tabControl)
        {
            if (!_removedTabsMap.ContainsKey(tabControl))
            {
                _removedTabsMap[tabControl] = new Dictionary<string, Tuple<TabItem, int>>();
            }
            return _removedTabsMap[tabControl];
        }

        public static void AddNewTab(TabControl tabControl, TabItem newTab)
        {
            if (newTab == null || string.IsNullOrEmpty(newTab.Name))
                return;

            if (!tabControl.Items.Contains(newTab))
                tabControl.Items.Add(newTab);
        }

        public static void RemoveTab(TabControl tabControl, TabItem tabItem)
        {
            if (tabItem == null || string.IsNullOrEmpty(tabItem.Name))
                return;

            if (tabControl.Items.Contains(tabItem))
            {
                Dictionary<string, Tuple<TabItem, int>> tabMap = GetTabMap(tabControl);
                int index = tabControl.Items.IndexOf(tabItem);
                tabMap[tabItem.Name] = Tuple.Create(tabItem, index);
                tabControl.Items.Remove(tabItem);
            }
        }

        public static void ReAddTab(TabControl tabControl, string tabName)
        {
            Dictionary<string, Tuple<TabItem, int>> tabMap = GetTabMap(tabControl);

            if (tabMap.ContainsKey(tabName))
            {
                Tuple<TabItem, int> entry = tabMap[tabName];
                TabItem tab = entry.Item1;
                int index = entry.Item2;

                if (!tabControl.Items.Contains(tab))
                {
                    int insertIndex = Math.Min(index, tabControl.Items.Count);
                    tabControl.Items.Insert(insertIndex, tab);
                }
            }
        }

        public static void RemoveAllTabs(TabControl tabControl)
        {
            Dictionary<string, Tuple<TabItem, int>> tabMap = GetTabMap(tabControl);

            for (int i = tabControl.Items.Count - 1; i >= 0; i--)
            {
                TabItem tabItem = tabControl.Items[i] as TabItem;
                if (tabItem != null && !string.IsNullOrEmpty(tabItem.Name))
                {
                    int index = i;
                    tabMap[tabItem.Name] = Tuple.Create(tabItem, index);
                    tabControl.Items.RemoveAt(i);
                }
            }
        }

        public static void RestoreAllTabs(TabControl tabControl)
        {
            Dictionary<string, Tuple<TabItem, int>> tabMap = GetTabMap(tabControl);

            foreach (KeyValuePair<string, Tuple<TabItem, int>> pair in tabMap.OrderBy(p => p.Value.Item2))
            {
                TabItem tab = pair.Value.Item1;
                int index = pair.Value.Item2;

                if (!tabControl.Items.Contains(tab))
                {
                    int insertIndex = Math.Min(index, tabControl.Items.Count);
                    tabControl.Items.Insert(insertIndex, tab);
                }
            }
        }
    }
}
