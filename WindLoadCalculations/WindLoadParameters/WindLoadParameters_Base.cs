using ShearWallCalculator.BuildingInfo;
using ShearWallCalculator.WindLoadCalculations.Chapter30.AreaCalculator;
using System.Collections.Generic;

namespace ShearWallCalculator.WindLoadCalculations
{
    // Wind Load Parameters class
    public abstract class WindLoadParameters_Base
    {
        public string RiskCategory { get; set; }
        public double WindSpeed { get; set; }
        public WindExposureCategories ExposureCategory { get; set; }
        public string EnclosureClassification { get; set; }
        public double Kd { get; set; }
        public double Kzt { get; set; } = 1.0;
        public double GustFactor { get; set; } = 0.85;
        public double ImportanceFactor { get; set; }
        public WindLoadCalculationTypes AnalysisType { get; set; } = WindLoadCalculationTypes.COMPONENT_AND_CLADDING;

        public Dictionary<int, double> GCp_Values { get; set; } = new Dictionary<int, double>();

        /// <summary>
        /// Contains the calculator that will be used to calculate the effective wind areas
        /// </summary>
        public abstract RoofAreaCalculator_Base RoofAreaCalculator { get; set; }

        public abstract void ComputeEffectiveWindAreas_Roof(BuildingData buildingData, ASCE7_Versions version);
    }
}
