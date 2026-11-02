using System.Collections.Generic;
using System.Linq;
using BuildingModel.Checks;
using BuildingModel.Diagnostics;
using BuildingModel.Model;
using ShearWallCalculator.Design;

namespace ShearWallVisualizer
{
    /// <summary>
    /// The log of what the user is warned about: every error and warning (the issue list, the Design tab's warnings) and
    /// every message, failing wall and close wall on the Design tab, with the level, the members and the location of each.
    /// The whole list is written when a model is loaded; after that only what appears ("new") and what goes ("resolved").
    /// </summary>
    public partial class MainWindow
    {
        private object issuesLoggedFor, designLoggedFor;
        private HashSet<string> loggedIssues = new HashSet<string>(), loggedDesign = new HashSet<string>();

        private string LevelNameOf(string levelId) =>
            levelId == null ? "no level" : building?.Levels.FirstOrDefault(l => l.Id == levelId)?.Name ?? levelId;

        /// <summary>One issue as a log line: severity, code, level, members, location, message.</summary>
        private string IssueLine(ModelIssue i) =>
            $"{(i.Severity == Severity.Error ? "ERROR" : i.Severity == Severity.Warning ? "WARNING" : i.Severity.ToString().ToUpper())} {i.Code}" +
            $" on {LevelNameOf(i.LevelId)}" +
            (i.Ids.Count > 0 ? $" [{string.Join(", ", i.Ids)}]" : "") +
            (i.At != null ? $" at ({i.At.Value.X:0.###}, {i.At.Value.Y:0.###})" : "") +
            $": {i.Message}";

        /// <summary>After <see cref="UpdateIssues"/>: the issues, all of them for a newly loaded model, else the changes.</summary>
        private void LogIssues(List<ModelIssue> list)
        {
            var now = new HashSet<string>(list.Select(IssueLine));
            bool fresh = !ReferenceEquals(issuesLoggedFor, building);
            if (!fresh && now.SetEquals(loggedIssues)) return;
            int errors = list.Count(i => i.Severity == Severity.Error), warnings = list.Count - errors;
            string design = string.Join(", ", list.Where(i => i.Code != "WallFails" && (i.Severity == Severity.Error || DesignCodes.Contains(i.Code)))
                                                  .GroupBy(i => i.Code).Select(g => $"{g.Count()} {g.Key}"));
            Log.Info("Issues", (fresh ? "Model loaded: " : "Now ") + $"{errors} error{(errors == 1 ? "" : "s")}, {warnings} warning{(warnings == 1 ? "" : "s")}" +
                               (design.Length > 0 ? $"; on the Design tab: {design}" : "; none on the Design tab") + ".");
            foreach (var line in fresh ? now : now.Except(loggedIssues))
                Log.Info("Issues", (fresh ? "" : "new: ") + line);
            if (!fresh)
                foreach (var line in loggedIssues.Except(now)) Log.Info("Issues", "resolved: " + line);
            loggedIssues = now;
            issuesLoggedFor = building;
        }

        /// <summary>After <see cref="UpdateDesign"/>: each level's message and its failing and close walls, as above.</summary>
        private void LogDesign(IList<Controls.WallDesignPanel.DesignStory> list)
        {
            var now = new HashSet<string>(levelNotes.Select(n => "note: " + n)); // the Calculations tab's notes on the levels
            foreach (var s in list)
            {
                if (s.Message != null) now.Add($"{s.Name}: {s.Message}");
                if (s.Results == null) continue;
                var map = s.Index >= 0 && s.Index < stories.Count ? stories[s.Index] : null;
                foreach (var kv in s.Results.OrderBy(k => k.Key))
                {
                    var r = kv.Value;
                    if (r.State == DesignState.Pass) continue;
                    string id = map != null && map.Walls.TryGetValue(kv.Key, out var w) ? w : $"wall {kv.Key}";
                    var why = new List<string>();
                    if (!r.AspectOk) why.Add($"aspect {r.AspectRatio:0.##} over {r.MaxAspectRatio:0.##}");
                    why.Add($"shear {r.Demand:0} / {r.Capacity:0} plf ({r.ShearRatio:P0})");
                    why.Add(r.HoldownNeeded ? $"hold-down {r.TensionAsd:0} lb → {r.Holdown?.Model ?? "none strong enough"}" : "no hold-down needed");
                    now.Add($"{s.Name} {id}: {r.State.ToString().ToUpper()}: {string.Join("; ", why)}");
                }
            }
            bool fresh = !ReferenceEquals(designLoggedFor, building);
            if (!fresh && now.SetEquals(loggedDesign)) return;
            int fail = now.Count(x => x.Contains(": FAIL:")), close = now.Count(x => x.Contains(": CLOSE:"));
            Log.Info("Design", (fresh ? "Model loaded: " : "Now ") + $"{fail} failing wall{(fail == 1 ? "" : "s")}, {close} close, " +
                               $"{list.Count(s => s.Message != null)} level message{(list.Count(s => s.Message != null) == 1 ? "" : "s")}, " +
                               $"{levelNotes.Count} note{(levelNotes.Count == 1 ? "" : "s")}.");
            foreach (var line in (fresh ? now : now.Except(loggedDesign)).OrderBy(x => x))
                Log.Info("Design", (fresh ? "" : "new: ") + line);
            if (!fresh)
                foreach (var line in loggedDesign.Except(now).OrderBy(x => x)) Log.Info("Design", "resolved: " + line);
            loggedDesign = now;
            designLoggedFor = building;
        }
    }
}
