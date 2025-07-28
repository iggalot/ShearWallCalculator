using ShearWallCalculator.WindLoadCalculations.Chapter30.AreaCalculator;
using System;

namespace ShearWallCalculator.WindLoadCalculations.WindLoadCalculators
{
    public class WindLoadCalculator_MWFRS_Base : WindLoadCalculator_Base
    {
        public override double CalculateDynamicWindPressure(double z)
        {
            throw new NotImplementedException();
        }

        public override void CalculateExternalPressures()
        {
            throw new NotImplementedException();
        }

        public override void CalculateNetPressures()
        {
            throw new NotImplementedException();
        }
    }
}
