using System;
using System.Collections.Generic;
using System.Linq;
using calculator;
using Newtonsoft.Json;

namespace ShearWallCalculator.Drawing
{
    /// <summary>
    /// Undo and redo for the drawing: the walls (with their design inputs), the diaphragms and the project design
    /// settings. A snapshot is taken after every change (<see cref="Record"/>); undo puts the previous snapshot back.
    /// Loads, wind inputs and the plan image are not part of it.
    /// </summary>
    public sealed class DrawingHistory
    {
        /// <summary>How many steps are kept; the oldest are dropped.</summary>
        public const int Limit = 200;

        private readonly List<string> states = new List<string>();
        private int current = -1;

        public bool CanUndo => current > 0;
        public bool CanRedo => current >= 0 && current < states.Count - 1;
        public int UndoSteps => Math.Max(0, current);
        public int RedoSteps => CanRedo ? states.Count - 1 - current : 0;

        /// <summary>Forgets every step; the drawing as it is now is the start (new or opened project).</summary>
        public void Reset(ShearWallCalculatorBase calc)
        {
            states.Clear();
            states.Add(Snapshot(calc));
            current = 0;
        }

        /// <summary>
        /// Adds a step when the drawing differs from the last step (nothing happens when it is the same).
        /// A new step drops the steps that were undone. Returns true when a step was added.
        /// </summary>
        public bool Record(ShearWallCalculatorBase calc)
        {
            string s = Snapshot(calc);
            if (current >= 0 && states[current] == s) return false;
            if (current < states.Count - 1) states.RemoveRange(current + 1, states.Count - current - 1);
            states.Add(s);
            current = states.Count - 1;
            if (states.Count > Limit)
            {
                states.RemoveAt(0);
                current--;
            }
            return true;
        }

        /// <summary>Puts the previous step back. Returns what was undone ("wall 3 added"), or null when there is nothing to undo.</summary>
        public string Undo(ShearWallCalculatorBase calc)
        {
            if (!CanUndo) return null;
            string what = Describe(states[current - 1], states[current]);
            current--;
            Apply(calc, states[current]);
            return what;
        }

        /// <summary>Puts the next undone step back. Returns what was redone, or null when there is nothing to redo.</summary>
        public string Redo(ShearWallCalculatorBase calc)
        {
            if (!CanRedo) return null;
            string what = Describe(states[current], states[current + 1]);
            current++;
            Apply(calc, states[current]);
            return what;
        }

        // ------------------------------------------------------------------ snapshots

        private sealed class State
        {
            // by id: the id itself is not written with a wall or diaphragm
            public SortedDictionary<int, WallData> Walls { get; set; } = new SortedDictionary<int, WallData>();
            public SortedDictionary<int, DiaphragmData_Rectangular> Diaphragms { get; set; } = new SortedDictionary<int, DiaphragmData_Rectangular>();
            public Design.DesignSettings DesignSettings { get; set; }
        }

        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.None,
            ObjectCreationHandling = ObjectCreationHandling.Replace,
        };

        /// <summary>The drawing as text (walls and diaphragms by id).</summary>
        public static string Snapshot(ShearWallCalculatorBase calc)
        {
            var s = new State
            {
                Walls = new SortedDictionary<int, WallData>(calc?._wall_system?._walls ?? new Dictionary<int, WallData>()),
                Diaphragms = new SortedDictionary<int, DiaphragmData_Rectangular>(calc?._diaphragm_system?._diaphragms ?? new Dictionary<int, DiaphragmData_Rectangular>()),
                DesignSettings = calc?.DesignSettings,
            };
            return JsonConvert.SerializeObject(s, Settings);
        }

        /// <summary>Replaces the calculator's walls, diaphragms and design settings with a snapshot.</summary>
        public static void Apply(ShearWallCalculatorBase calc, string snapshot)
        {
            var s = JsonConvert.DeserializeObject<State>(snapshot, Settings);
            if (calc._wall_system == null) calc._wall_system = new WallSystem();
            if (calc._diaphragm_system == null) calc._diaphragm_system = new DiaphragmSystem();
            calc._wall_system._walls.Clear();
            calc._wall_system.EW_Walls.Clear();
            calc._wall_system.NS_Walls.Clear();
            foreach (var w in s.Walls)
            {
                w.Value.ID = w.Key;
                calc._wall_system._walls[w.Key] = w.Value;
            }
            calc._diaphragm_system._diaphragms.Clear();
            foreach (var d in s.Diaphragms)
            {
                d.Value.ID = d.Key;
                calc._diaphragm_system._diaphragms[d.Key] = d.Value;
            }
            if (s.DesignSettings != null) calc.DesignSettings = s.DesignSettings;
            calc._wall_system.Update();
            calc._diaphragm_system.Update();
        }

        /// <summary>What changed between two snapshots, in words: "wall 3 added", "diaphragm 1 moved", "2 walls deleted".</summary>
        public static string Describe(string before, string after)
        {
            var a = JsonConvert.DeserializeObject<State>(before, Settings);
            var b = JsonConvert.DeserializeObject<State>(after, Settings);
            var parts = new List<string>();
            parts.AddRange(Changes("wall", "walls", Items(a.Walls), Items(b.Walls)));
            parts.AddRange(Changes("diaphragm", "diaphragms", Items(a.Diaphragms), Items(b.Diaphragms)));
            if (parts.Count == 0)
                return JsonConvert.SerializeObject(a.DesignSettings) != JsonConvert.SerializeObject(b.DesignSettings) ? "design settings changed" : "change";
            return string.Join(", ", parts);
        }

        private static Dictionary<int, string> Items<T>(SortedDictionary<int, T> items) =>
            items.ToDictionary(i => i.Key, i => JsonConvert.SerializeObject(i.Value));

        private static IEnumerable<string> Changes(string one, string many, Dictionary<int, string> a, Dictionary<int, string> b)
        {
            var added = b.Keys.Where(k => !a.ContainsKey(k)).OrderBy(k => k).ToList();
            var deleted = a.Keys.Where(k => !b.ContainsKey(k)).OrderBy(k => k).ToList();
            var changed = a.Keys.Where(k => b.ContainsKey(k) && a[k] != b[k]).OrderBy(k => k).ToList();
            string Say(List<int> ids, string verb) => ids.Count == 1 ? $"{one} {ids[0]} {verb}" : $"{ids.Count} {many} {verb}";
            if (added.Count > 0) yield return Say(added, "added");
            if (deleted.Count > 0) yield return Say(deleted, "deleted");
            if (changed.Count > 0) yield return Say(changed, "changed");
        }
    }
}
