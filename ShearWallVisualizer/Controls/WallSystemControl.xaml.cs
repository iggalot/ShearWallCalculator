﻿using ShearWallCalculator;
using System;
using System.Windows;
using System.Windows.Controls;

namespace ShearWallVisualizer.Controls
{
    /// <summary>
    /// Interaction logic for WallSystemControl.xaml
    /// </summary>
    public partial class WallSystemControl : UserControl
    {
        public EventHandler OnWallSubControlDeleted;
        Window MainWin { get; set; }
        WallSystem Data;

        public WallSystemControl(MainWindow window, WallSystem data)
        {
            InitializeComponent();

            MainWin = window;
            Data = data;

            CreateSubcontrols();

            // window.OnUpdated += RefreshAll;
        }


        private void CreateSubcontrols()
        {
            // clear previous events
            foreach(var child in sp_Walls.Children)
            {
                WallDataControl wall = child as WallDataControl;
                wall.DeleteWall -= WallDeleted;
            }

            // clear the children
            sp_Walls.Children.Clear();

            // recreate the children
            foreach (var wall in Data._walls)
            {
                WallDataControl wall_control = new WallDataControl(wall.Key, wall.Value);
                wall_control.DeleteWall += WallDeleted;
                sp_Walls.Children.Add(wall_control);
            }
        }

        private void WallDeleted(object sender, WallDataControl.DeleteWallEventArgs e)
        {
            //MessageBox.Show("Diaphragm deleted at control level");
            OnWallSubControlDeleted?.Invoke(this, e); // signal up the chain that a diaphragm_control has been deleted
        }
    }
}
