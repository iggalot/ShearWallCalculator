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
            double buildingHeight,
            string enclosureClassification,
            double kd,
            double kzt,
            double importanceFactor,
            double buildingLength,
            double buildingWidth,
            double roofPitch,
            string ridgeDirection,
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
            parameters.BuildingHeight = buildingHeight;
            parameters.EnclosureClassification = enclosureClassification;
            parameters.Kd = kd;
            parameters.Kzt = kzt;
            parameters.ImportanceFactor = importanceFactor;
            parameters.BuildingLength = buildingLength;
            parameters.BuildingWidth = buildingWidth;
            parameters.RoofPitch = roofPitch;
            parameters.RidgeDirection = ridgeDirection;
            parameters.AnalysisType = analysisType;

            return parameters;
        }
    }
