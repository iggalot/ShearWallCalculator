using System.Collections.Generic;

namespace ShearWallCalculator.WindLoadCalculations
{
    public abstract class RoofAreaCalculator_Base
    {
        /// <summary>
        /// Effective wind areas for roof
        /// </summary>
        public Dictionary<int, EffectiveWindArea_Roof> effWindAreas_Roof { get; set; } = new Dictionary<int, EffectiveWindArea_Roof>();
    }
}
