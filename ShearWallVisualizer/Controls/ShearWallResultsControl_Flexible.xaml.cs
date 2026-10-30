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

        /// <summary>A wall that takes no load in a direction has no value there: show a dash, not "NaN".</summary>
        private static string Show(double v) => double.IsNaN(v) ? "-" : v.ToString("0.00");

        public ShearWallResultsControl_Flexible(int id,double vix, double viy)
        {
            InitializeComponent();

            ID.Content = id;
            Vix_TOT.Content = Show(vix);
            Viy_TOT.Content = Show(viy);
        }
    }
}
