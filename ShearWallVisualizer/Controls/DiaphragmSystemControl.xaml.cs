using ShearWallCalculator;
using System;
using System.Windows;
using System.Windows.Controls;

namespace ShearWallVisualizer.Controls
{
    /// <summary>
    /// Interaction logic for DiaphragmSystemControl.xaml
    /// </summary>
    public partial class DiaphragmSystemControl : UserControl
    {
        public EventHandler OnDiaphragmSubControlDeleted;
        Window MainWin { get; set; }
        DiaphragmSystem Data;

        public DiaphragmSystemControl(MainWindow window, DiaphragmSystem data)
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
            foreach(var child in sp_Diaphragms.Children)
            {
                DiaphragmDataControl diaphragm = child as DiaphragmDataControl;
                diaphragm.DeleteDiaphragm -= DiaphragmDeleted;
            }

            // clear the children
            sp_Diaphragms.Children.Clear();

            // recreate the children
            foreach (var diaphragm in Data._diaphragms)
            {
                DiaphragmDataControl diaphragm_control = new DiaphragmDataControl(diaphragm.Key, diaphragm.Value);
                diaphragm_control.DeleteDiaphragm += DiaphragmDeleted;
                sp_Diaphragms.Children.Add(diaphragm_control);
            }
        }

        private void DiaphragmDeleted(object sender, DiaphragmDataControl.DeleteDiaphragmEventArgs e)
        {
            //MessageBox.Show("Diaphragm deleted at control level");
            OnDiaphragmSubControlDeleted?.Invoke(this, e); // signal up the chain that a diaphragm_control has been deleted
        }
    }
}
