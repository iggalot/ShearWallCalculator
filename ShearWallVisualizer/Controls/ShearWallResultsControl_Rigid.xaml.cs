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

        public ShearWallResultsControl_Rigid(int id, double rigidity, double xbar, double ybar, double vix, double viy, double v_ecc, double v_tot)
        {
            InitializeComponent();

            ID.Content = id;
            Rgidity.Content = rigidity.ToString("0.00");
            Xbar.Content = xbar.ToString("0.00");
            Ybar.Content = ybar.ToString("0.00");
            Vix.Content = vix.ToString("0.00");
            Viy.Content = viy.ToString("0.00");
            V_ecc.Content = v_ecc.ToString("0.00");
            V_tot.Content = v_tot.ToString("0.00");
        }
    }
}
