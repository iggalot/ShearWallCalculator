namespace ShearWallCalculator
{
    /// <summary>
    /// One lateral design load case applied to the diaphragm: forces at the center of mass plus an optional
    /// torsional moment. Wind load cases (ASCE 7 Fig. 27.3-8) carry their own torsion and do not use the
    /// seismic-style minimum eccentricity; a manually entered load keeps the original behavior.
    /// </summary>
    public class LateralLoadCase
    {
        public string Name { get; set; } = "Load";

        /// <summary>Force in the +x direction (kips).</summary>
        public double Vx { get; set; }

        /// <summary>Force in the +y direction (kips).</summary>
        public double Vy { get; set; }

        /// <summary>Additional torsional moment about the vertical axis, counter-clockwise positive (kip-ft).</summary>
        public double Mt { get; set; }

        /// <summary>Raise the center-of-rigidity eccentricity to the calculator's minimum (manual loads).</summary>
        public bool ApplyMinimumEccentricity { get; set; }

        /// <summary>Multi-story: the part of <see cref="Vx"/>, <see cref="Vy"/> and <see cref="Mt"/> (about the center of mass) that comes from the
        /// walls of the story above; 0 for this level's own wind. Set on the cases a calculation uses, never saved.</summary>
        [Newtonsoft.Json.JsonIgnore] public double AboveVx { get; set; }
        [Newtonsoft.Json.JsonIgnore] public double AboveVy { get; set; }
        [Newtonsoft.Json.JsonIgnore] public double AboveMt { get; set; }

        public LateralLoadCase() { }

        public LateralLoadCase(string name, double vx, double vy, double mt = 0, bool applyMinimumEccentricity = false)
        {
            Name = name; Vx = vx; Vy = vy; Mt = mt; ApplyMinimumEccentricity = applyMinimumEccentricity;
        }
    }

    /// <summary>
    /// One wall's shear over all load cases: the most positive and most negative total shear (kips, the wall's force on the
    /// diaphragm, signed in the building axes) and the case that gives each. Hold-downs come in pairs, one at each end of
    /// the wall, so both are sized for the design shear, the larger magnitude of the two.
    /// </summary>
    public sealed class ShearRange
    {
        public double MaxPositive { get; set; }
        public string MaxPositiveCase { get; set; }
        public double MaxNegative { get; set; }
        public string MaxNegativeCase { get; set; }

        /// <summary>max(|most positive|, |most negative|) (kips).</summary>
        public double Design => System.Math.Max(System.Math.Abs(MaxPositive), System.Math.Abs(MaxNegative));

        /// <summary>The case giving the design shear.</summary>
        public string DesignCase => System.Math.Abs(MaxPositive) >= System.Math.Abs(MaxNegative) ? MaxPositiveCase : MaxNegativeCase;
    }
}

namespace ShearWallCalculator
{
    /// <summary>
    /// A horizontal force on a diaphragm at a point (kips, ft): the reaction of a wall of the story above, which stands on this
    /// floor. Its force on this diaphragm is minus its shear (the shear is the wall's force on the diaphragm above it).
    /// </summary>
    public sealed class PointLoad
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Fx { get; set; }
        public double Fy { get; set; }

        /// <summary>The upper wall it comes from ("level 2, wall 3").</summary>
        public string From { get; set; }
    }
}
