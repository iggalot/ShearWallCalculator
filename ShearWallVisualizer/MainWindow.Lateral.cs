using ShearWallCalculator.ModelAdapter;

namespace ShearWallVisualizer
{
    public partial class MainWindow
    {
        /// <summary>
        /// What the lateral design puts on the foundation (the model's "Lateral" section, written on every save), from this
        /// window's design: the shared builder (<see cref="ShearWallCheck.Lateral"/>, the hub's re-check makes the same),
        /// stamped with what it was based on (<paramref name="basedOn"/>: the building as saved). Null when there is no design yet, or the walls take manual loads.
        /// </summary>
        private BuildingModel.Model.LateralSection LateralForFoundation(int revision, BuildingModel.Model.Building basedOn = null)
        {
            if (!windLoadsLinked) return null;
            var s = ShearWallCheck.Lateral(levels, stories, allDesignResults, foundationAnchors, revision);
            if (s != null && (basedOn ?? building) != null) s.BasedOn = ShearWallCheck.InputsNow(basedOn ?? building); // the geometry the design used, its inputs as saved
            return s;
        }
    }
}
