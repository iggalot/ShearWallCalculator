using System.Collections.Generic;
using System.Linq;
using BuildingModel.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ShearWallCalculator.ModelAdapter
{
    /// <summary>A story's lateral load (kips) at its centre of mass.</summary>
    public sealed class StoryLoad
    {
        public double Vx { get; set; }
        public double Vy { get; set; }
    }

    /// <summary>
    /// The shear wall app's section of the building model file (the building's "ShearWall.Project" data): what the designer
    /// keeps about the project that is neither geometry (the model's) nor wind (the wind section's).
    /// <list type="bullet">
    /// <item><see cref="ManualLoads"/>: a project with no wind inputs takes the user's own load per story. A project with a
    /// wind section always takes the wind loads; manual loads are not used.</item>
    /// <item><see cref="LoadsWhenSaved"/>: each story's loads at the last save, so the user is told when they come out different
    /// on opening (a correction in the wind calculation since).</item>
    /// </list>
    /// The section is written into a copy of the building at save and taken out of the building being edited on opening (it is
    /// not part of its undo). The plans under the levels are the building's (BuildingModel.Sketch.PlanImage): only the Building
    /// Model editor changes them; this app shows them. Older projects kept them under "ShearWall.Image"; the sketch converts those.
    /// </summary>
    public sealed class ShearWallProject
    {
        public const string Key = "ShearWall.Project";

        /// <summary>The user's load per story (by level ID); used only when the project has no wind inputs.</summary>
        public Dictionary<string, StoryLoad> ManualLoads { get; set; } = new Dictionary<string, StoryLoad>();

        /// <summary>Each story's loads when the project was saved (by level ID).</summary>
        public Dictionary<string, StoryLoad> LoadsWhenSaved { get; set; } = new Dictionary<string, StoryLoad>();

        private static JsonSerializerSettings Json() => new JsonSerializerSettings
        {
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            NullValueHandling = NullValueHandling.Ignore,
            Converters = { new StringEnumConverter() },
        };

        /// <summary>The section stored in the building, or null when it has none.</summary>
        public static ShearWallProject Read(Building b) => Parse<ShearWallProject>(b?.GetData(Key));

        public void Write(Building b) => b.SetData(Key, JsonConvert.SerializeObject(this, Json()));

        /// <summary>
        /// Before a save, into the copy of the building that is saved: the design tabs' latest (<see cref="ShearWallAdapter.StoreDesign"/>),
        /// each story's plan image, the loads, and (with <paramref name="manualLoads"/>) each story's manual load. Returns the section written.
        /// </summary>
        public static ShearWallProject Save(Building b, IEnumerable<StoryMap> stories, bool manualLoads)
        {
            var list = stories.ToList();
            ShearWallAdapter.StoreDesign(b, list); // the maps name the model's IDs: the same in a copy
            var p = new ShearWallProject();
            foreach (var s in list)
            {
                var level = b.Level(s.Level.Id);
                if (level == null) continue;
                var c = s.Calculator;
                p.LoadsWhenSaved[level.Id] = new StoryLoad { Vx = c.V_x, Vy = c.V_y };
                if (manualLoads) p.ManualLoads[level.Id] = new StoryLoad { Vx = c.V_x, Vy = c.V_y };
            }
            p.Write(b);
            return p;
        }

        /// <summary>
        /// On opening: a calculator per story with (with <paramref name="manualLoads"/>) its manual load, for
        /// <see cref="ShearWallAdapter.ToCalculators"/>; the section is then taken out of the building.
        /// <paramref name="section"/> is null when the building has none (a model not designed yet).
        /// </summary>
        public static List<ShearWallCalculatorBase> Open(Building b, bool manualLoads, out ShearWallProject section)
        {
            section = Read(b);
            var calcs = new List<ShearWallCalculatorBase>();
            foreach (var level in ShearWallAdapter.Stories(b))
            {
                var c = new calculator.ShearWallCalculator_RigidDiaphragm();
                if (manualLoads && section != null && section.ManualLoads.TryGetValue(level.Id, out var load)) c.AddLoads(load.Vx, load.Vy);
                calcs.Add(c);
            }
            Strip(b);
            return calcs;
        }

        /// <summary>Takes the section out of the building (after reading it on opening).</summary>
        public static void Strip(Building b)
        {
            b.SetData(Key, null);
        }

        private static T Parse<T>(string s) where T : class
        {
            if (string.IsNullOrEmpty(s)) return null;
            try { return JsonConvert.DeserializeObject<T>(s, Json()); }
            catch (JsonException) { return null; }
        }
    }
}
