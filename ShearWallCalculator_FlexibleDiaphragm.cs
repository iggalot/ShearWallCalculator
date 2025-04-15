using calculator;

namespace ShearWallCalculator
{
    public class ShearWallCalculator_FlexibleDiaphragm : ShearWallCalculatorBase
    {
        public new string CalculatorType { get => "Flexible Diaphragm"; }

        /// <summary>
        /// copy constructor
        /// </summary>
        /// <param name="copy_calc"></param>
        public ShearWallCalculator_FlexibleDiaphragm(ShearWallCalculator_FlexibleDiaphragm copy_calc)
        {
            _diaphragm_system = copy_calc._diaphragm_system;
            _wall_system = copy_calc._wall_system;

            Update();

        }
        /// <summary>
        /// Function to update calculations.  Should be called everytime data is added, removed, or changed.
        /// </summary>
        public override void Update() { }
    }
}
