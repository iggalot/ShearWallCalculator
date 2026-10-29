using System;
using System.IO;
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
            // Safety net: an unexpected error shows a message and is logged instead of closing the app
            // without warning. Details go to errors.log next to the program.
            DispatcherUnhandledException += OnUnhandledException;
        }

        private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            string log = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "errors.log");
            try
            {
                File.AppendAllText(log, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}  {e.Exception}{Environment.NewLine}{Environment.NewLine}");
            }
            catch (IOException) { /* logging must never cause a second failure */ }
            catch (UnauthorizedAccessException) { }

            MessageBox.Show(
                "Something went wrong and that action was cancelled, but your work is still open.\n\n" +
                e.Exception.Message + "\n\nDetails were saved to:\n" + log,
                "Unexpected problem", MessageBoxButton.OK, MessageBoxImage.Warning);
            e.Handled = true;
        }
    }
}
