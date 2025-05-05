using System.Windows.Controls;

namespace ShearWallVisualizer.Controls
{
    /// <summary>
    /// Interaction logic for ShearWallResults.xaml
    /// </summary>
    public partial class ShearWallResultsControl_Flexible : UserControl
    {
        public ShearWallResultsControl_Flexible()
        {
            InitializeComponent();
        }

        public ShearWallResultsControl_Flexible(int id,double vix, double viy)
        {
            InitializeComponent();

            ID.Content = id;
            Vix_TOT.Content = vix.ToString("0.00");
            Viy_TOT.Content = viy.ToString("0.00");
        }
    }
}
