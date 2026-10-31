using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShearWallCalculator.Diagnostics;

namespace ShearWallCalculator.Tests
{
    /// <summary>The app log: one file per day, levels, the status-line event, old files purged, never throws.</summary>
    [TestClass]
    public class TestLogFolder
    {
        /// <summary>Code under test logs (deleting a wall, for example); keep that out of the user's own log folder.</summary>
        [AssemblyInitialize]
        public static void Redirect(TestContext _) => Log.Folder = Path.Combine(Path.GetTempPath(), "swc-test-logs");
    }

    [TestClass]
    [DoNotParallelize] // Log is static
    public class LogTests
    {
        private string dir;
        private string savedFolder;
        private LogLevel savedLevel;

        [TestInitialize]
        public void Init()
        {
            savedFolder = Log.Folder;
            savedLevel = Log.MinimumLevel;
            dir = Path.Combine(Path.GetTempPath(), "swc-log-" + Guid.NewGuid().ToString("N"));
            Log.Folder = dir;
        }

        [TestCleanup]
        public void Cleanup()
        {
            Log.Folder = savedFolder;
            Log.MinimumLevel = savedLevel;
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }

        [TestMethod]
        public void WritesTodaysFileWithLevelAndMessage()
        {
            Log.Warning("Wall 3 has no length");
            Assert.AreEqual(Path.Combine(dir, $"swc-{DateTime.Now:yyyy-MM-dd}.log"), Log.TodayFile);
            string text = File.ReadAllText(Log.TodayFile);
            StringAssert.Contains(text, "WARNING");
            StringAssert.Contains(text, "Wall 3 has no length");
        }

        [TestMethod]
        public void DebugIsSkippedUnlessAskedFor()
        {
            Log.Debug("hidden");
            Log.Info("shown");
            string text = File.ReadAllText(Log.TodayFile);
            Assert.IsFalse(text.Contains("hidden"));
            StringAssert.Contains(text, "shown");

            Log.MinimumLevel = LogLevel.Debug;
            Log.Debug("now shown");
            StringAssert.Contains(File.ReadAllText(Log.TodayFile), "now shown");
        }

        [TestMethod]
        public void ExceptionDetailIsWrittenBelowTheEntry()
        {
            try { throw new InvalidOperationException("bad input"); }
            catch (Exception ex) { Log.Error("Calculation failed", ex); }
            string text = File.ReadAllText(Log.TodayFile);
            StringAssert.Contains(text, "ERROR");
            StringAssert.Contains(text, "Calculation failed");
            StringAssert.Contains(text, "System.InvalidOperationException: bad input");
        }

        [TestMethod]
        public void WrittenEventCarriesLevelAndMessage_AndAFailingListenerIsIgnored()
        {
            var seen = new List<(LogLevel, string)>();
            Action<LogLevel, string> ok = (l, m) => seen.Add((l, m));
            Action<LogLevel, string> bad = (l, m) => throw new InvalidOperationException("listener");
            Log.Written += ok;
            Log.Written += bad;
            try
            {
                Log.Error("boom"); // must not throw
            }
            finally
            {
                Log.Written -= ok;
                Log.Written -= bad;
            }
            // other tests run in parallel and may log too
            CollectionAssert.Contains(seen, (LogLevel.Error, "boom"));
        }

        [TestMethod]
        public void FilesOlderThanTheKeepPeriodArePurged()
        {
            Directory.CreateDirectory(dir);
            string old = Path.Combine(dir, "swc-2000-01-01.log");
            string recent = Path.Combine(dir, $"swc-{DateTime.Now.AddDays(-2):yyyy-MM-dd}.log");
            File.WriteAllText(old, "x");
            File.WriteAllText(recent, "x");
            File.SetLastWriteTime(old, DateTime.Now.AddDays(-(Log.KeepDays + 5)));
            File.SetLastWriteTime(recent, DateTime.Now.AddDays(-2));
            Log.Folder = dir; // a new folder purges again on the first entry

            Log.Info("first entry");

            Assert.IsFalse(File.Exists(old));
            Assert.IsTrue(File.Exists(recent));
        }

        [TestMethod]
        public void AnUnwritableFolderDoesNotThrow()
        {
            Log.Folder = dir + @"\bad<name>"; // invalid path characters
            Log.Error("still fine");
        }
    }
}
