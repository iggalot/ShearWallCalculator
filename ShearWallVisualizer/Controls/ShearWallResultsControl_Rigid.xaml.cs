using System.Windows.Controls;

namespace ShearWallVisualizer.Controls
{
    /// <summary>
    /// Interaction logic for ShearWallResults.xaml
    /// </summary>
    public partial class ShearWallResultsControl_Rigid : UserControl
    {
        public ShearWallResultsControl_Rigid()
        {
            InitializeComponent();
        }

        /// <summary>A wall that takes no load in a direction has no value there: show a dash, not "NaN".</summary>
        private static string Show(double v) => double.IsNaN(v) ? "-" : v.ToString("0.00");

        public ShearWallResultsControl_Rigid(int id, double rigidity, double xbar, double ybar, double vix, double viy, double v_ecc, double v_tot)
        {
            InitializeComponent();

            ID.Content = id;
            Rgidity.Content = Show(rigidity);
            Xbar.Content = Show(xbar);
            Ybar.Content = Show(ybar);
            Vix.Content = Show(vix);
            Viy.Content = Show(viy);
            V_ecc.Content = Show(v_ecc);
            V_tot.Content = Show(v_tot);
        }
    }
}
