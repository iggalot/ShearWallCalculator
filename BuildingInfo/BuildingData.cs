using System;

namespace ShearWallCalculator.BuildingInfo
{
    public enum BuildingEnclosures
    {
        BLDG_ENCLOSED = 0,
        BLDG_PARTIALLY_ENCLOSED = 1,
        BLDG_PARTIALLY_OPEN = 2,
        BLDG_OPEN = 3
    }

    public enum RidgeDirections
    {
        RIDGE_DIR_NONE = 0,
        RIDGE_DIR_PERP_TO_BLDGLENGTH = 1,
        RIDGE_DIR_PARALLEL_TO_BLDGLENGTH = 2
    }

    public enum RoofTypes
    {
        ROOF_TYPE_FLAT = 0,
        ROOF_TYPE_GABLE = 1,
        ROOF_TYPE_HIP = 2
    }

    // <summary>
    /// A class for storing the building data
    /// </summary>
    public class BuildingData
    {
        public double BuildingLength { get; set; } = 60; // L
        public double BuildingWidth { get; set; } = 40;  // B
        public double BuildingHeight { get; set; } = 15;
        public double RoofPitch { get; set; } = 15;
        public RidgeDirections RidgeDirection { get; set; } = RidgeDirections.RIDGE_DIR_NONE;
        public RoofTypes RoofType { get; set; } = RoofTypes.ROOF_TYPE_FLAT;
        public BuildingEnclosures EnclosureType { get; set; } = BuildingEnclosures.BLDG_ENCLOSED;

        /// <summary>
        /// The mean roof height of the building, h per ASCE7
        /// </summary>
        public double MeanRoofHeight { get => ComputeMeanRoofHeight(); }

        /// <summary>
        /// Validates the ridge direction.  
        /// -- For flat roof, ridge direction is RIDGE_DIR_NONE
        /// -- For hip roof, the ridge direction is parallel to the longest side
        /// -- For gable roof, the ridge direction is user defined
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public void ValidateRidgeDirection()
        {
            switch (RoofType)
            {
                case RoofTypes.ROOF_TYPE_FLAT:
                    RidgeDirection = RidgeDirections.RIDGE_DIR_NONE;
                    break;

                case RoofTypes.ROOF_TYPE_HIP:
                    // Automatically assign based on longest dimension
                    RidgeDirection = (BuildingLength >= BuildingWidth)
                        ? RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH
                        : RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH;
                    break;

                case RoofTypes.ROOF_TYPE_GABLE:
                    // Must be user-defined and not NONE
                    if (RidgeDirection == RidgeDirections.RIDGE_DIR_NONE)
                    {
                        // Reset it to a default being parallel to the shortest dimension
                        if(BuildingLength >= BuildingWidth)
                        {
                            RidgeDirection = RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH;
                        } else
                        {
                            RidgeDirection = RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH;
                        }
                    }
                    // Otherwise, use as-is
                    break;

                default:
                    throw new NotImplementedException($"Ridge direction validation not implemented for roof type {RoofType}");
            }
        }


        private double ComputeMeanRoofHeight()
        {
            if (RoofType == RoofTypes.ROOF_TYPE_HIP)
            {
                double h1 = Math.Tan(RoofPitch * Math.PI / 180.0) * BuildingLength / 2.0;
                double h2 = Math.Tan(RoofPitch * Math.PI / 180.0) * BuildingWidth / 2.0;
                return 0.5*(BuildingHeight + BuildingHeight + Math.Min(h1, h2))  // mean roof height is average of the peak height and the wall height
                    ;
            }
            else if (RoofType == RoofTypes.ROOF_TYPE_GABLE)
            {
                double h1 = Math.Tan(RoofPitch * Math.PI / 180.0) * BuildingLength / 2.0;
                double h2 = Math.Tan(RoofPitch * Math.PI / 180.0) * BuildingWidth / 2.0;
                return 0.5 * (BuildingHeight + BuildingHeight + Math.Min(h1, h2)); // // mean roof height is average of the peak height and the wall height
            } 
            else if (RoofType == RoofTypes.ROOF_TYPE_FLAT)
            {
                return BuildingHeight;
            }
            else
            {
                throw new NotImplementedException("ERROR: In ComputeMeanRoofHeight(), Roof type " + RoofType + " is not implemented");

            }
        }

        public double ComputeRidgeHeight()
        {
            if(RoofType == RoofTypes.ROOF_TYPE_FLAT) return BuildingHeight;
            if(RoofType == RoofTypes.ROOF_TYPE_GABLE)
            {
                if (RidgeDirection == RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH)
                {
                    return BuildingHeight + Math.Tan(RoofPitch * Math.PI / 180.0) * BuildingLength / 2.0;
                }
                else if (RidgeDirection == RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH)
                {
                    return BuildingHeight + Math.Tan(RoofPitch * Math.PI / 180.0) * BuildingWidth / 2.0;
                }
                if (RidgeDirection == RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH)
                {
                    return BuildingHeight + Math.Tan(RoofPitch * Math.PI / 180.0) * BuildingLength / 2.0;
                }
                else if (RidgeDirection == RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH)
                {
                    return BuildingHeight + Math.Tan(RoofPitch * Math.PI / 180.0) * BuildingWidth / 2.0;
                }
            }
            if(RoofType == RoofTypes.ROOF_TYPE_HIP)
            {
                if (RidgeDirection == RidgeDirections.RIDGE_DIR_NONE)
                {
                    return BuildingHeight;
                }
                else
                {
                    return BuildingHeight + Math.Tan(RoofPitch * Math.PI / 180.0) * Math.Min(BuildingLength, BuildingWidth) / 2.0;
                }
            }
            return BuildingHeight;

        }

        public bool RoofTypeIsSloped()
        {
            return (RoofType == RoofTypes.ROOF_TYPE_HIP || RoofType == RoofTypes.ROOF_TYPE_GABLE);
        }

        public bool RoofTypeIsFlat()
        {
            return (RoofType == RoofTypes.ROOF_TYPE_FLAT);
        }
    }
}
