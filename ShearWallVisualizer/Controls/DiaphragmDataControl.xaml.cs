using ShearWallCalculator;
using System;
using System.Windows.Controls;

namespace ShearWallVisualizer.Controls
{
    /// <summary>
    /// Interaction logic for DiaphragmData.xaml
    /// </summary>
    public partial class DiaphragmDataControl : UserControl
    {
        // events for other programs to connect to when a wall is deleted
        public event EventHandler<DeleteDiaphragmEventArgs> DeleteDiaphragm;

        protected virtual void OnDiaphragmDeleted(DeleteDiaphragmEventArgs e)
        {
            EventHandler<DeleteDiaphragmEventArgs> handler = DeleteDiaphragm;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public class DeleteDiaphragmEventArgs : EventArgs
        {
            public int Id { get; set; }
        }

        public DiaphragmData_Rectangular Data { get; set; }
        public int Id { get; set; }

        public DiaphragmDataControl(int id, DiaphragmData_Rectangular diaphragm)
        {
            InitializeComponent();
            Id = id;
            Data = diaphragm;

            lbl_ID.Content = Id.ToString();
            lbl_Type.Content = diaphragm.DiaphragmType.ToString();
            lbl_HorizX.Content = diaphragm.HorizDim_X.ToString("0.00");
            lbl_HorizY.Content = diaphragm.HorizDim_Y.ToString("0.00");
            lbl_P1.Content = "<" + diaphragm.P1.X.ToString("0.00") + ", " + diaphragm.P1.Y.ToString("0.00") + ">";
            lbl_P2.Content = "<" + diaphragm.P2.X.ToString("0.00") + ", " + diaphragm.P2.Y.ToString("0.00") + ">";
            lbl_P3.Content = "<" + diaphragm.P3.X.ToString("0.00") + ", " + diaphragm.P3.Y.ToString("0.00") + ">";
            lbl_P4.Content = "<" + diaphragm.P4.X.ToString("0.00") + ", " + diaphragm.P4.Y.ToString("0.00") + ">";
        }

        private void btn_Delete_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Button btn = sender as Button;
            OnDiaphragmDeleted(new DeleteDiaphragmEventArgs() { Id = this.Id });
        }
    }
}
