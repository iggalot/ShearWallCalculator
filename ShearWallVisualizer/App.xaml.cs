using ShearWallCalculator.Diagnostics;
using BuildingModel.Diagnostics;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace ShearWallVisualizer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            // Safety net: an unexpected error is logged with its full detail and the user gets a plain message; the
            // action is cancelled but the work stays open. The log is %AppData%\ShearWallCalculator\logs (Help menu).
            DispatcherUnhandledException += OnUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                Log.Error("Unhandled error (the program is closing)" + (e.IsTerminating ? "" : " (not terminating)"), e.ExceptionObject as Exception);
            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                Log.Error("Unobserved background task error", e.Exception);
                e.SetObserved();
            };
            Log.Start("ShearWallCalculator", "swc"); // the shared log: %AppData%\ShearWallCalculator\logs
            Exit += (s, e) => Log.Info("Shear Wall Calculator closed.");
        }

        private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Log.Error("Unexpected error; the action was cancelled", e.Exception);
            MessageBox.Show(
                "Something went wrong and that action was cancelled, but your work is still open.\n\n" +
                e.Exception.Message + "\n\nThe details are in the log (Help > Open log folder):\n" + Log.TodayFile,
                "Unexpected problem", MessageBoxButton.OK, MessageBoxImage.Warning);
            e.Handled = true;
        }
    }
}
