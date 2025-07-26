namespace ShearWallCalculator.WindLoadCalculations.Core.Interfaces
{
    public interface IWindLoadCalculator
    {
        double CalculateDynamicWindPressure(double z);

        void CalculateExternalPressures();

        void CalculateNetPressures();

    }
}
