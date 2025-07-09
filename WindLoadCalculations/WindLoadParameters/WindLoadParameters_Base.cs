using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace ShearWallCalculator.WindLoadCalculations
{
    // Wind Load Parameters class
    public abstract class WindLoadParameters_Base
    {
        public string RiskCategory { get; set; }
        public double WindSpeed { get; set; }
        public WindExposureCategories ExposureCategory { get; set; }
        public double BuildingHeight { get; set; }
        public string EnclosureClassification { get; set; }
        public double Kd { get; set; }
        public double Kzt { get; set; } = 1.0;
        public double GustFactor { get; set; } = 0.85;
        public double ImportanceFactor { get; set; }
        public double BuildingLength { get; set; }
        public double BuildingWidth { get; set; }
        public double RoofPitch { get; set; }
        public string RidgeDirection { get; set; }
        public RoofTypes RoofType { get; set; } = RoofTypes.ROOF_TYPE_HIP;
        public WindLoadCalculationTypes AnalysisType { get; set; } = WindLoadCalculationTypes.COMPONENT_AND_CLADDING;

        /// <summary>
        /// The mean roof height of the building, h per ASCE7
        /// </summary>
        public abstract double MeanRoofHeight { get; }

        /// <summary>
        /// The critical width dimenstion "a" used throughout chapter 30
        /// -- minimum of 0.4 * building height and 0.1 * min(building Length, building width)
        /// </summary>
        public double CritDim_a
        {
            get
            {
                return Math.Min(0.4 * MeanRoofHeight, 0.1 * Math.Min(BuildingLength, BuildingWidth));
            }
        }
        public Dictionary<int, double> GCp_Values { get; set; } = new Dictionary<int, double>();

        public abstract void ComputeEffectiveWindAreas_Roof();
    }
}
