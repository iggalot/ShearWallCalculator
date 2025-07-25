using ShearWallCalculator.BuildingInfo;
using System.Collections.Generic;

namespace ShearWallCalculator.WindLoadCalculations
{
    // Wind Load Parameters class
    public abstract class WindParameters_Base
    {
        public string RiskCategory { get; set; } 
        public double WindSpeed { get; set; } = 150.0;
        public WindExposureCategories ExposureCategory { get; set; }
        public BuildingEnclosures EnclosureClassification { get; set; }
        public double Kd { get; set; } = 0.85;
        public double Kzt { get; set; } = 1.0;
        public double GustFactor { get; set; } = 0.85;
        public double ImportanceFactor { get; set; } = 1.0;
        public WindLoadCalculationTypes AnalysisType { get; set; } = WindLoadCalculationTypes.COMPONENT_AND_CLADDING;

        public Dictionary<int, double> GCp_Values { get; set; } = new Dictionary<int, double>();
    }
}
