using calculator;
using System.Windows.Controls;

namespace ShearWallVisualizer.Controls
{

    /// <summary>
    /// Interaction logic for ShearWallData.xaml
    /// </summary>
    public partial class ShearWallData : UserControl
    {
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
    }
}
