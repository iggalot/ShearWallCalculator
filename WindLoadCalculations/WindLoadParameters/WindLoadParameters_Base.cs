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
        public BuildingEnclosures EnclosureClassification { get; set; }
        public double Kd { get; set; }
        public double Kzt { get; set; } = 1.0;
        public double GustFactor { get; set; } = 0.85;
        public double ImportanceFactor { get; set; }
        public WindLoadCalculationTypes AnalysisType { get; set; } = WindLoadCalculationTypes.COMPONENT_AND_CLADDING;

        public Dictionary<int, double> GCp_Values { get; set; } = new Dictionary<int, double>();

        /// <summary>
        /// Contains the calculator that will be used to calculate the effective wind areas on the roof
        /// </summary>
        public abstract AreaCalculator_Base RoofAreaCalculator { get; set; }

        /// <summary>
        /// Contains the calculator for the wall loads acting on the BuildingLength dimension
        /// </summary>
        public abstract AreaCalculator_Base WallAreaCalculator_BldgLength { get; set; }
        /// <summary>
        /// Contains the calculator for the wall loads acting on the BuildingWidth dimension
        /// </summary>
        public abstract AreaCalculator_Base WallAreaCalculator_BldgWidth { get; set; }


        public abstract void ComputeEffectiveWindAreas(BuildingData buildingData, ASCE7_Versions version);
    }
}
