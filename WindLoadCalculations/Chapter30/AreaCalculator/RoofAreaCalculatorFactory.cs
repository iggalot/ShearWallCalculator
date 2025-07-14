using ShearWallCalculator.BuildingInfo;
using System;

namespace ShearWallCalculator.WindLoadCalculations.Chapter30.AreaCalculator
{
    public static class RoofAreaCalculatorFactory
    {
        public static RoofAreaCalculator_Base Create(
            BuildingData bldg_data,
            WindLoadParameters_Base parameters,
            ASCE7_Versions version
            )
        {
            if(bldg_data == null)
            {
                return null;
            }

            if (version == ASCE7_Versions.ASCE_VER_7_10)
            {
                throw new NotImplementedException("ERROR: ASCE7_10 not yet implemented.");
            }
            else if (version == ASCE7_Versions.ASCE_VER_7_16)
            {
                if (parameters.AnalysisType == WindLoadCalculationTypes.COMPONENT_AND_CLADDING)
                {
                    if (bldg_data.RoofType == RoofTypes.ROOF_TYPE_FLAT)
                    {
                        return new FlatRoofAreaCalculator_CC_ASCE7_16();
                    }
                    else if (bldg_data.RoofType == RoofTypes.ROOF_TYPE_GABLE)
                    {
                        if (bldg_data.RoofPitch < 7) return new FlatRoofAreaCalculator_CC_ASCE7_16();
                        else return new GableRoofAreaCalculator_CC_ASCE7_16();
                    }
                    else if (bldg_data.RoofType == RoofTypes.ROOF_TYPE_HIP)
                    {
                        if (bldg_data.RoofPitch < 7) return new FlatRoofAreaCalculator_CC_ASCE7_16();
                        else return new HipRoofAreaCalculator_CC_ASCE7_16();
                    }
                    else
                    {
                        throw new NotImplementedException("ERROR: Invalid roof type: " + bldg_data.RoofType + " in RoofAreaCalculatorFactory.");
                    }
                }
                else if (parameters.AnalysisType == WindLoadCalculationTypes.MWFRS)
                {
                    // TODO:  implement the area calculators
                    if (bldg_data.RoofType == RoofTypes.ROOF_TYPE_FLAT)
                    {
                        return new FlatRoofAreaCalculator_MWFRS_ASCE7_16();
                    }
                    else if (bldg_data.RoofType == RoofTypes.ROOF_TYPE_GABLE)
                    {
                        if (bldg_data.RoofPitch < 7) return new FlatRoofAreaCalculator_MWFRS_ASCE7_16();
                        else return new GableRoofAreaCalculator_MWFRS_ASCE7_16();
                    }
                    else if (bldg_data.RoofType == RoofTypes.ROOF_TYPE_HIP)
                    {
                        if (bldg_data.RoofPitch < 7) return new FlatRoofAreaCalculator_MWFRS_ASCE7_16();
                        else return new HipRoofAreaCalculator_MWFRS_ASCE7_16();
                    }
                    else
                    {
                        throw new NotImplementedException("ERROR: Invalid roof type: " + bldg_data.RoofType + " in RoofAreaCalculatorFactory.");
                    }
                }
            } else if (version == ASCE7_Versions.ASCE_VER_7_22)
            {
                if (parameters.AnalysisType == WindLoadCalculationTypes.COMPONENT_AND_CLADDING)
                {
                    if (bldg_data.RoofType == RoofTypes.ROOF_TYPE_FLAT)
                    {
                        return new FlatRoofAreaCalculator_CC_ASCE7_22();
                    }
                    else if (bldg_data.RoofType == RoofTypes.ROOF_TYPE_GABLE)
                    {
                        if (bldg_data.RoofPitch < 7) return new FlatRoofAreaCalculator_CC_ASCE7_22();
                        else return new GableRoofAreaCalculator_CC_ASCE7_22();
                    }
                    else if (bldg_data.RoofType == RoofTypes.ROOF_TYPE_HIP)
                    {
                        if (bldg_data.RoofPitch < 7) return new FlatRoofAreaCalculator_CC_ASCE7_22();
                        else return new HipRoofAreaCalculator_CC_ASCE7_22();
                    }
                    else
                    {
                        throw new NotImplementedException("ERROR: Invalid roof type: " + bldg_data.RoofType + " in RoofAreaCalculatorFactory.");
                    }
                }
                else if (parameters.AnalysisType == WindLoadCalculationTypes.MWFRS)
                {
                    // TODO:  implement the area calculators
                    if (bldg_data.RoofType == RoofTypes.ROOF_TYPE_FLAT)
                    {
                        return new FlatRoofAreaCalculator_MWFRS_ASCE7_22();
                    }
                    else if (bldg_data.RoofType == RoofTypes.ROOF_TYPE_GABLE)
                    {
                        if (bldg_data.RoofPitch < 7) return new FlatRoofAreaCalculator_MWFRS_ASCE7_22();
                        else return new GableRoofAreaCalculator_MWFRS_ASCE7_22();
                    }
                    else if (bldg_data.RoofType == RoofTypes.ROOF_TYPE_HIP)
                    {
                        if (bldg_data.RoofPitch < 7) return new FlatRoofAreaCalculator_MWFRS_ASCE7_22();
                        else return new HipRoofAreaCalculator_MWFRS_ASCE7_22();
                    } else
                    {
                        throw new NotImplementedException("ERROR: Invalid roof type: " + bldg_data.RoofType + " in RoofAreaCalculatorFactory.");
                    }
                } else
                {
                    throw new NotImplementedException("ERROR: Invalid analysis type: " + parameters.AnalysisType + " in RoofAreaCalculatorFactory.");
                }
            } else
            {
                throw new NotImplementedException("ERROR: Invalid ASCE version: " + version + " in RoofAreaCalculatorFactory.");
            }
            return null;
        }
    }
}
