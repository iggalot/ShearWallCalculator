using System;

namespace ShearWallCalculator.WindLoadCalculations
{
    public static class WindLoadParametersFactory
    {
        public static WindLoadParameters_Base Create(
            RoofTypes roofType,
            string riskCategory,
            double windSpeed,
            WindExposureCategories exposureCategory,
            double kd,
            double kzt,
            double importanceFactor,
            WindLoadCalculationTypes analysisType)
        {
            WindLoadParameters_Base parameters;

            if (roofType == RoofTypes.ROOF_TYPE_FLAT)
            {
                parameters = new FlatRoofWindLoadParameters();
            }
            else if (roofType == RoofTypes.ROOF_TYPE_GABLE)
            {
                parameters = new GableRoofWindLoadParameters();
            }
            else if (roofType == RoofTypes.ROOF_TYPE_HIP)
            {
                parameters = new HipRoofWindLoadParameters();
            }
            else
            {
                throw new NotSupportedException("Unsupported roof type: " + roofType);
            }

            parameters.RiskCategory = riskCategory;
            parameters.WindSpeed = windSpeed;
            parameters.ExposureCategory = exposureCategory;
            parameters.Kd = kd;
            parameters.Kzt = kzt;
            parameters.ImportanceFactor = importanceFactor;
            parameters.AnalysisType = analysisType;

            return parameters;
        }
    }
}