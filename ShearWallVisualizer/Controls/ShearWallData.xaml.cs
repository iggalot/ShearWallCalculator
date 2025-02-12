using calculator;
using System;
using System.Windows;
using System.Windows.Controls;

namespace ShearWallVisualizer.Controls
{

    /// <summary>
    /// Interaction logic for ShearWallData.xaml
    /// </summary>
    public partial class ShearWallData : UserControl
    {
        // events for other programs to connect to when a wall is deleted
        public event EventHandler<DeleteWallEventArgs> DeleteWall;

        protected virtual void OnWallDeleted(DeleteWallEventArgs e)
        {
            EventHandler<DeleteWallEventArgs> handler = DeleteWall;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public class DeleteWallEventArgs : EventArgs
        {
            public int Id { get; set; }
        }

        public WallData Data { get; set; }
        public int Id { get; set; }

        public ShearWallData(int id, WallData wall)
        {
            InitializeComponent();

            Id = id;
            Data = wall;

            lbl_ID.Content = Id.ToString();
            lbl_Type.Content = wall.WallType.ToString();
            lbl_Length.Content = wall.WallLength.ToString("0.00");
            lbl_Height.Content = wall.WallHeight.ToString("0.00");
            lbl_StartPt.Content = "<" + wall.Start.X.ToString("0.00") + ", " + wall.Start.Y.ToString("0.00") + ">";
            lbl_EndPt.Content = "<" + wall.End.X.ToString("0.00") + ", " + wall.End.Y.ToString("0.00") + ">";
        }

        private void btn_Delete_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Button btn = sender as Button;
            OnWallDeleted(new DeleteWallEventArgs() { Id = this.Id });
        }



    }


}
