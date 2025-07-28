namespace ShearWallCalculator.WindLoadCalculations.WindLoadCalculators
{
    /// <summary>
    ///  In ASCE7-16, the dynamic wind pressure coefficient includes Kd in the dynamic wind calculation.  
    ///  In ASCE7_22 it does not -- instead applying Kd to the external and net pressure calculations separately.  
    ///  In the end the calculations are the same.  This now better matches these codes.  
    ///  But dyanmic wind pressure qz and qh will be drastically different.
    /// </summary>
    public class WindLoadCalculator_ASCE7_16_Base : WindLoadCalculator_Base
    {
        public override double CalculateDynamicWindPressure(double z)
        {
            if (Parameters == null)
                return -1000;

            WindParameters_Base p = Parameters;

            double V = p.WindSpeed;
            double Kd = p.Kd;
            double Kzt = p.Kzt;
            double I = p.ImportanceFactor;
            double Kz = GetKz(z, p.ExposureCategory);
            double qz = 0.00256 * Kz * Kzt * Kd * V * V * I;
            return qz;
        }

        /// <summary>
        /// Calculates the qh * GCP for external pressures
        /// </summary>
        public override void CalculateExternalPressures()
        {

        }

        /// <summary>
        /// Calculates the qh * GCP for external pressures minus the internal pressure qh * GCpi
        /// </summary>
        public override void CalculateNetPressures()
        {

        }
    }
}
