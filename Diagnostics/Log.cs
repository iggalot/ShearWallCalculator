using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace ShearWallCalculator.Diagnostics
{
    public enum LogLevel { Debug, Info, Warning, Error }

    /// <summary>
    /// The app's log: one file per day, %AppData%\ShearWallCalculator\logs\swc-yyyy-MM-dd.log, kept for
    /// <see cref="KeepDays"/> days. Each line is "time  LEVEL  message"; an exception adds its full detail below.
    /// Logging never throws: if the file cannot be written, the entry is dropped.
    /// </summary>
    public static class Log
    {
        private static readonly object Gate = new object();
        private static string folder = DefaultFolder;
        private static bool purged;

        public const int KeepDays = 14;

        public static string DefaultFolder =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ShearWallCalculator", "logs");

        /// <summary>Where the files go (tests point it at a temporary folder).</summary>
        public static string Folder
        {
            get => folder;
            set { lock (Gate) { folder = value; purged = false; } }
        }

        /// <summary>Entries below this level are not written (Debug is off unless asked for).</summary>
        public static LogLevel MinimumLevel { get; set; } = LogLevel.Info;

        /// <summary>Raised for every entry written (the app shows warnings and errors in its status bar).</summary>
        public static event Action<LogLevel, string> Written;

        public static string TodayFile => Path.Combine(Folder, $"swc-{DateTime.Now:yyyy-MM-dd}.log");

        public static void Debug(string message) => Write(LogLevel.Debug, message, null);
        public static void Info(string message) => Write(LogLevel.Info, message, null);
        public static void Warning(string message, Exception ex = null) => Write(LogLevel.Warning, message, ex);
        public static void Error(string message, Exception ex = null) => Write(LogLevel.Error, message, ex);

        public static void Write(LogLevel level, string message, Exception ex)
        {
            if (level < MinimumLevel) return;
            string line = $"{DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture)}  {level.ToString().ToUpperInvariant(),-7}  {message}";
            if (ex != null) line += Environment.NewLine + "    " + ex.ToString().Replace(Environment.NewLine, Environment.NewLine + "    ");
            lock (Gate)
            {
                try
                {
                    Directory.CreateDirectory(Folder);
                    if (!purged) PurgeOld();
                    File.AppendAllText(TodayFile, line + Environment.NewLine);
                }
                catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is NotSupportedException || e is ArgumentException)
                {
                    // logging must never cause a second failure
                }
            }
            try { Written?.Invoke(level, message); }
            catch (Exception e) when (!(e is OutOfMemoryException)) { /* a listener's failure is not the caller's */ }
        }

        /// <summary>Deletes log files older than <see cref="KeepDays"/> days (once per session).</summary>
        private static void PurgeOld()
        {
            purged = true;
            var cutoff = DateTime.Now.Date.AddDays(-KeepDays);
            foreach (var f in Directory.GetFiles(Folder, "swc-*.log").Where(f => File.GetLastWriteTime(f) < cutoff))
            {
                try { File.Delete(f); }
                catch (Exception e) when (e is IOException || e is UnauthorizedAccessException) { }
            }
        }
    }
}
