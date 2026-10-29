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

        public LateralLoadCase() { }

        public LateralLoadCase(string name, double vx, double vy, double mt = 0, bool applyMinimumEccentricity = false)
        {
            Name = name; Vx = vx; Vy = vy; Mt = mt; ApplyMinimumEccentricity = applyMinimumEccentricity;
        }
    }
}
