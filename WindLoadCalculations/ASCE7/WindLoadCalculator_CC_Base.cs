namespace ShearWallCalculator.WindLoadCalculations.WindLoadCalculators
{
    /// <summary>
    ///  In ASCE7-16, the dynamic wind pressure coefficient includes Kd in the dynamic wind calculation.  
    ///  In ASCE7_22 it does not -- instead applying Kd to the external and net pressure calculations separately.  
    ///  In the end the calculations are the same.  This now better matches these codes.  
    ///  But dyanmic wind pressure qz and qh will be drastically different.
    /// </summary>
    public class WindLoadCalculator_CC_Base : WindLoadCalculator_Base
    {
        public override double CalculateDynamicWindPressure(double z)
        {
            throw new System.NotImplementedException();
        }

        public override void CalculateExternalPressures()
        {
            throw new System.NotImplementedException();
        }

        public override void CalculateNetPressures()
        {
            throw new System.NotImplementedException();
        }
    }
}
