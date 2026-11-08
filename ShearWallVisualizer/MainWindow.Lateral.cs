using ShearWallCalculator.ModelAdapter;

namespace ShearWallVisualizer
{
    public partial class MainWindow
    {
        /// <summary>
        /// The project's Wind section as this window read it (the walls are designed with it): this window keeps the wind in
        /// its wind screen and takes the section off its own copy of the building, so the stamp at save takes it from here.
        /// </summary>
        private string windJsonInUse;

        /// <summary>
        /// What the lateral design puts on the foundation (the model's "Lateral" section, written on every save), from this
        /// window's design: the shared builder (<see cref="ShearWallCheck.Lateral"/>, the hub's re-check makes the same),
        /// stamped with what it was found from (<see cref="BasedOnNow"/>). Null when there is no design yet, or the walls take
        /// manual loads.
        /// </summary>
        private BuildingModel.Model.LateralSection LateralForFoundation(int revision, BuildingModel.Model.Building basedOn = null)
        {
            if (!windLoadsLinked) return null;
            var s = ShearWallCheck.Lateral(levels, stories, allDesignResults, foundationAnchors, revision, loadPath?.ByLevel, building?.Ground?.Id, building?.Defaults);
            if (s != null && (basedOn ?? building) != null) s.BasedOn = BasedOnNow(basedOn ?? building);
            return s;
        }

        /// <summary>
        /// The fingerprints of what this design was found from: the geometry and the walls' design inputs of
        /// <paramref name="mine"/> (the building as saved), and the wind this window used (<see cref="windJsonInUse"/>), not
        /// <paramref name="mine"/>'s, which has no Wind section here. The hub and the Load Manager compare them with the file.
        /// </summary>
        private System.Collections.Generic.Dictionary<string, string> BasedOnNow(BuildingModel.Model.Building mine)
        {
            var on = ShearWallCheck.InputsNow(mine);
            on[ShearWallCheck.WindPart] = BuildingModel.Serialization.ModelFingerprint.Of(windJsonInUse);
            return on;
        }
    }
}
