using ShearWallCalculator.BuildingInfo;
using ShearWallCalculator.WindLoadCalculations.Chapter30.AreaCalculator;
using ShearWallCalculator.WindLoadCalculations.Chapter30_CC.AreaCalculator.ASCE7_22;
using System;

namespace ShearWallCalculator.WindLoadCalculations
{
    public static class WallAreaCalculatorFactory
    {

        public static AreaCalculator_Base Create(
            BuildingData bldg_data,
            WindParameters_Base parameters,
            ASCE7_Versions version,
            bool isGable
            )
        {
            if (bldg_data == null)
            {
                return null;
            }

            // figure out the wall arrangements
            if (bldg_data.RoofType == RoofTypes.ROOF_TYPE_FLAT)
            {
                if(version == ASCE7_Versions.ASCE_VER_7_16)
                {
                    return new WallAreaCalculator_NonGableEnd_CC_ASCE7_16(bldg_data);
                } else if (version == ASCE7_Versions.ASCE_VER_7_22)
                {
                    return new WallAreaCalculator_NonGableEnd_CC_ASCE7_22(bldg_data);
                }
            }
            else if (bldg_data.RoofType == RoofTypes.ROOF_TYPE_GABLE || bldg_data.RoofType == RoofTypes.ROOF_TYPE_HIP)
            {
                if(isGable is true)
                {
                    if (version == ASCE7_Versions.ASCE_VER_7_16)
                    {
                        return new WallAreaCalculator_GableEnd_CC_ASCE7_16(bldg_data);
                    }
                    else if (version == ASCE7_Versions.ASCE_VER_7_22)
                    {
                        return new WallAreaCalculator_GableEnd_CC_ASCE7_22(bldg_data);
                    }
                    else
                    {
                        throw new NotImplementedException("Wall calculator not implemented for version " + version);
                    }
                } else
                {
                    if (version == ASCE7_Versions.ASCE_VER_7_16)
                    {
                        return new WallAreaCalculator_NonGableEnd_CC_ASCE7_16(bldg_data);
                    }
                    else if (version == ASCE7_Versions.ASCE_VER_7_22)
                    {
                        return new WallAreaCalculator_NonGableEnd_CC_ASCE7_22(bldg_data);
                    } else
                    {
                        throw new NotImplementedException("Wall calculator not implemented for version " + version);
                    }
                }
            }
            else
            {
                throw new NotSupportedException("Unsupported roof type: " + bldg_data.RoofType);
            }

            return null;
        }

    }
}
