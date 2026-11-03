using System.Collections.Generic;
using System.Linq;
using ShearWallCalculator.Design;

namespace ShearWallVisualizer.Controls
{
    /// <summary>
    /// A wall's hold-down choices, the same in the Design table and on the plan's wall card: "Auto · HDUE5-SDS3" (the
    /// lightest that works) and every model of the wall's type, lightest first, each with its allowable load; those weaker
    /// than T are marked. Picking one sets <see cref="WallDesignInputs.HoldownKey"/> (Auto: null).
    /// </summary>
    public static class HoldownPicks
    {
        public sealed class Item
        {
            public string Key;   // the catalog option key, null for Auto
            public string Text;
            public override string ToString() => Text;
            public override bool Equals(object o) => o is Item p && p.Key == Key;
            public override int GetHashCode() => Key?.GetHashCode() ?? 0;
        }

        /// <summary>The choices for a wall, Auto first.</summary>
        public static List<Item> For(ShearWallDesigner designer, WallDesignInputs d, WallDesignResult r, StrapHoldownConditions sthd)
        {
            bool auto = !r.HoldownChosen;
            var items = new List<Item>
            {
                new Item { Key = null, Text = auto ? (r.Holdown != null ? $"Auto · {r.Holdown.Model}" : r.HoldownNeeded ? "Auto · ✗ none fits" : "Auto") : "Auto (lightest that works)" },
            };
            var options = designer.Options(d.Holdown, d.Species, sthd).OrderBy(o => o.Allowable).ToList();
            string Post(HoldownChoice o) => o.WoodMinHeight.HasValue && o.WoodMinWidth.HasValue ? $"min. {o.WoodMinHeight.Value:0.##} × {o.WoodMinWidth.Value:0.##}" : null;
            var repeated = new HashSet<string>(options.GroupBy(o => o.Model).Where(g => g.Count() > 1).Select(g => g.Key));
            var samePost = new HashSet<string>(options.GroupBy(o => o.Model + "|" + Post(o)).Where(g => g.Count() > 1).Select(g => g.Key));
            foreach (var o in options)
            {
                // a model listed more than once (fastener options): told apart by its minimum post, and its fasteners when
                // the post is the same too
                var parts = new List<string>();
                if (repeated.Contains(o.Model) && Post(o) != null) parts.Add(Post(o));
                if (repeated.Contains(o.Model) && (Post(o) == null || samePost.Contains(o.Model + "|" + Post(o))) && !string.IsNullOrEmpty(o.Fasteners)) parts.Add(o.Fasteners);
                string which = parts.Count == 0 ? "" : " · " + string.Join(", ", parts);
                items.Add(new Item { Key = o.Key, Text = $"{o.Model} · {o.Allowable:0} lb{which}" + (r.HoldownNeeded && o.Allowable < r.TensionAsd - 1e-9 ? "  ✗ < T" : "") });
            }
            return items;
        }

        /// <summary>The entry shown as chosen: the engineer's model, else Auto.</summary>
        public static Item Selected(List<Item> items, WallDesignInputs d, WallDesignResult r) =>
            items.FirstOrDefault(p => p.Key == (r.HoldownChosen ? d.HoldownKey : null)) ?? items[0];

        /// <summary>What the box says on hover.</summary>
        public static string Tip(WallDesignInputs d, WallDesignResult r) =>
            r.HoldownChosen && r.Holdown == null ? $"Your choice {d.HoldownKey}; no uplift on this wall now, so no hold-down is needed."
            : r.HoldownChosen
            ? $"Your choice: {r.Holdown} at {r.Holdown.Allowable:0} lb for T = {r.TensionAsd:0} lb" + (r.HoldownOk ? "." : " (too weak: the wall fails).") + " Auto goes back to the lightest that works."
            : (r.HoldownChoiceIgnored != null ? $"Your choice {r.HoldownChoiceIgnored} is not a {WallDesignPanel.HoldownName(d.Holdown)} option: the lightest that works is used. " : "") +
              "Auto: the lightest model at or above T. Pick one to use your own (e.g. a size up).";
    }
}
