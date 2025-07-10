using ShearWallCalculator.BuildingInfo;
using System;

namespace ShearWallCalculator.WindLoadCalculations.Chapter30.AreaCalculator
{
    public static class RoofAreaCalculatorFactory
    {
        public static RoofAreaCalculator_Base Create(
            BuildingData bldg_data,
            WindLoadParameters_Base parameters
            )
        {
            if(bldg_data == null)
            {
                return null;
            }

            if (parameters.AnalysisType == WindLoadCalculationTypes.COMPONENT_AND_CLADDING)
            {
                if (bldg_data.RoofType == RoofTypes.ROOF_TYPE_FLAT)
                {
                    return new FlatRoofAreaCalculator();
                }
                else if (bldg_data.RoofType == RoofTypes.ROOF_TYPE_GABLE)
                {
                    return new GableRoofAreaCalculator();
                }
                else if (bldg_data.RoofType == RoofTypes.ROOF_TYPE_HIP)
                {
                    return new HipRoofAreaCalculator();
                }
            } else if (parameters.AnalysisType == WindLoadCalculationTypes.MWFRS)
            {
                // TODO:  implement the area calculators
                throw new NotImplementedException("ERROR: MWFRS not yet implemented.");
            }


            throw new NotImplementedException("ERROR: Invalid roof type: " + bldg_data.RoofType + " in RoofAreaCalculatorFactory.");
        }
    }
}
